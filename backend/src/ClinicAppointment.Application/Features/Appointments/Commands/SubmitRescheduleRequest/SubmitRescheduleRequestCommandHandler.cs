using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest;

public class SubmitRescheduleRequestCommandHandler : IRequestHandler<SubmitRescheduleRequestCommand, Result<SubmitRescheduleRequestResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly IOtpGrantService _grantService;

    public SubmitRescheduleRequestCommandHandler(
        IApplicationDbContext context,
        IOtpGrantService grantService)
    {
        _context = context;
        _grantService = grantService;
    }

    public async Task<Result<SubmitRescheduleRequestResponse>> Handle(
        SubmitRescheduleRequestCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Acceptance Criteria 1: Bắt buộc phải có Grant token từ xác thực OTP
        if (string.IsNullOrWhiteSpace(request.GrantToken))
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Yêu cầu xác thực OTP để gửi yêu cầu đổi lịch. Vui lòng cung cấp mã quyền truy cập (Grant token).", 
                401);
        }

        var normalizedCode = (request.BookingCode ?? string.Empty).Trim().ToUpperInvariant();

        // 2. Tra cứu ca khám
        var appointment = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.BookingCode == normalizedCode, cancellationToken);

        if (appointment == null)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Không tìm thấy thông tin ca khám cần đổi lịch.", 
                404);
        }

        // 3. Kiểm tra tính hợp lệ của cuộc hẹn hiện tại
        if (appointment.Status != AppointmentStatus.PendingConfirmation && appointment.Status != AppointmentStatus.Confirmed)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                $"Không thể yêu cầu đổi lịch cho ca khám ở trạng thái {appointment.Status}. Chỉ cho phép đổi lịch cho ca khám đang Chờ xác nhận hoặc Đã xác nhận.", 
                400);
        }

        if (appointment.StartAt <= DateTime.UtcNow)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Không thể yêu cầu đổi lịch cho ca khám đã hoặc đang diễn ra.", 
                400);
        }

        // 4. Acceptance Criteria 1: Kiểm tra tính hợp lệ và tiêu thụ Scoped Grant Token (Purpose: RESCHEDULE, One-time use)
        var grantValidation = await _grantService.ValidateAndConsumeGrantAsync(
            request.GrantToken,
            appointment.Id,
            OtpPurpose.RESCHEDULE,
            consume: true,
            cancellationToken);

        if (!grantValidation.IsSuccess)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                grantValidation.ErrorMessage ?? "Quyền truy cập (Grant token) không hợp lệ hoặc đã hết hạn.", 
                403);
        }

        // 5. Acceptance Criteria 4: Giới hạn tối đa 1 yêu cầu đổi lịch đang chờ duyệt (PendingReview)
        var hasPendingRequest = await _context.RescheduleRequests
            .AnyAsync(r => r.AppointmentId == appointment.Id && r.Status == RescheduleStatus.PendingReview, cancellationToken);

        if (hasPendingRequest)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Cuộc hẹn này đã có một yêu cầu đổi lịch đang chờ Lễ tân duyệt (PendingReview). Mỗi ca khám chỉ được có tối đa 1 yêu cầu đổi lịch chờ duyệt.", 
                409);
        }

        // 6. Kiểm tra khung giờ đề xuất
        var proposedStartAtUtc = request.ProposedStartAtUtc;
        if (proposedStartAtUtc <= DateTime.UtcNow)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Thời gian đề xuất đổi lịch phải ở tương lai.", 
                400);
        }

        var durationMinutes = appointment.ServiceDurationSnapshot > 0 ? appointment.ServiceDurationSnapshot : 30;
        var proposedEndAtUtc = proposedStartAtUtc.AddMinutes(durationMinutes);

        // Xác định bác sĩ mục tiêu (mặc định là bác sĩ của cuộc hẹn nếu không yêu cầu bác sĩ khác)
        var targetDoctorId = request.RequestedDoctorId ?? appointment.DoctorId;

        var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.Id == targetDoctorId && d.IsActive, cancellationToken);
        if (doctor == null)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Bác sĩ được yêu cầu cho ca khám không tồn tại hoặc đã ngừng hoạt động.", 
                400);
        }

        // 7. Kiểm tra sơ bộ tính khả dụng của slot đề xuất (FR-037)
        // 7.1 Kiểm tra ngày nghỉ của bác sĩ
        bool hasTimeOff = await _context.DoctorTimeOffs.AnyAsync(t =>
            t.DoctorId == targetDoctorId &&
            t.Status == TimeOffStatus.Active &&
            t.StartAt < proposedEndAtUtc &&
            t.EndAt > proposedStartAtUtc, cancellationToken);

        if (hasTimeOff)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Bác sĩ có lịch nghỉ đã được phê duyệt trong khung giờ đề xuất này. Vui lòng chọn khung giờ khác.", 
                409);
        }

        // 7.2 Kiểm tra lịch bận / khóa lịch của bác sĩ
        bool hasBlock = await _context.DoctorScheduleBlocks.AnyAsync(b =>
            b.DoctorId == targetDoctorId &&
            b.StartAt < proposedEndAtUtc &&
            b.EndAt > proposedStartAtUtc, cancellationToken);

        if (hasBlock)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Khung giờ đề xuất này đã bị khóa hoặc bác sĩ có lịch bận đột xuất. Vui lòng chọn khung giờ khác.", 
                409);
        }

        // 7.3 Kiểm tra trùng ca khám đang hoạt động khác của bác sĩ
        bool hasConflictAppointment = await _context.Appointments.AnyAsync(a =>
            a.Id != appointment.Id &&
            a.DoctorId == targetDoctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartAt < proposedEndAtUtc &&
            a.EndAt > proposedStartAtUtc, cancellationToken);

        if (hasConflictAppointment)
        {
            return Result<SubmitRescheduleRequestResponse>.Failure(
                "Khung giờ đề xuất đã có lịch hẹn khác của bác sĩ. Vui lòng chọn khung giờ khác.", 
                409);
        }

        // 8. Acceptance Criteria 2 & 3:
        // - Tạo RescheduleRequest trạng thái PendingReview
        // - TUYỆT ĐỐI GIỮ NGUYÊN appointment hiện tại (Old slot held: appointment.StartAt, appointment.EndAt, appointment.DoctorId, appointment.Status không đổi)
        var rescheduleRequest = RescheduleRequest.Create(
            appointmentId: appointment.Id,
            requestedDoctorId: targetDoctorId,
            proposedStartAtUtc: proposedStartAtUtc,
            proposedEndAtUtc: proposedEndAtUtc,
            reason: request.Reason);

        _context.RescheduleRequests.Add(rescheduleRequest);

        // 9. Ghi nhận lịch sử kiểm toán (Audit Log)
        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var metadata = new
        {
            BookingCode = appointment.BookingCode,
            RescheduleRequestId = rescheduleRequest.Id,
            CurrentStartAt = appointment.StartAt,
            ProposedStartAt = proposedStartAtUtc,
            RequestedDoctorId = targetDoctorId,
            Reason = request.Reason,
            SubmittedAt = DateTime.UtcNow
        };

        var auditLog = AuditLog.Create(
            actorType: appointment.IsGuest ? "GUEST" : "PATIENT",
            actorUserId: null,
            action: "SUBMIT_RESCHEDULE_REQUEST",
            entityType: "Appointment",
            entityId: appointment.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        // 10. Lưu dữ liệu vào CSDL
        await _context.SaveChangesAsync(cancellationToken);

        var response = new SubmitRescheduleRequestResponse(
            RequestId: rescheduleRequest.Id,
            AppointmentId: appointment.Id,
            BookingCode: appointment.BookingCode,
            Status: rescheduleRequest.Status,
            CurrentStartAtUtc: appointment.StartAt,
            CurrentEndAtUtc: appointment.EndAt,
            ProposedStartAtUtc: proposedStartAtUtc,
            ProposedEndAtUtc: proposedEndAtUtc,
            RequestedDoctorId: targetDoctorId,
            Reason: request.Reason,
            CreatedAtUtc: rescheduleRequest.CreatedAt,
            Message: "Yêu cầu đổi lịch của bạn đã được ghi nhận và đang chờ bộ phận Lễ tân duyệt. Lịch hẹn hiện tại của bạn vẫn được giữ nguyên cho đến khi có kết quả duyệt."
        );

        return Result<SubmitRescheduleRequestResponse>.Success(response, 201);
    }
}
