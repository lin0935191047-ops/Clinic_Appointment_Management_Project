using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;

public class RejectRescheduleRequestCommandHandler : IRequestHandler<RejectRescheduleRequestCommand, Result<ReviewRescheduleResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public RejectRescheduleRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ReviewRescheduleResponse>> Handle(
        RejectRescheduleRequestCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Acceptance Criteria 1: Chỉ Receptionist hoặc Admin mới có quyền từ chối yêu cầu đổi lịch
        if (!_currentUserService.IsStaff)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                "Bạn không có quyền từ chối yêu cầu đổi lịch. Chức năng này chỉ dành cho nhân viên Lễ tân hoặc Quản trị viên.", 
                403);
        }

        // 2. Kiểm tra lý do từ chối
        if (string.IsNullOrWhiteSpace(request.ReviewNotes))
        {
            return Result<ReviewRescheduleResponse>.Failure(
                "Lý do từ chối yêu cầu đổi lịch là bắt buộc.", 
                400);
        }

        // 3. Tra cứu yêu cầu đổi lịch
        var rescheduleRequest = await _context.RescheduleRequests
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (rescheduleRequest == null)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                "Không tìm thấy yêu cầu đổi lịch cần xử lý.", 
                404);
        }

        // 4. Kiểm tra trạng thái: Chỉ từ chối khi đang ở trạng thái PendingReview
        if (rescheduleRequest.Status != RescheduleStatus.PendingReview)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                $"Không thể từ chối yêu cầu đổi lịch ở trạng thái {rescheduleRequest.Status}. Chỉ có thể xử lý yêu cầu đang Chờ duyệt (PendingReview).", 
                400);
        }

        var appointment = rescheduleRequest.Appointment;
        if (appointment == null)
        {
            appointment = await _context.Appointments.FindAsync(new object[] { rescheduleRequest.AppointmentId }, cancellationToken);
        }

        if (appointment == null)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                "Không tìm thấy ca khám liên kết với yêu cầu đổi lịch này.", 
                404);
        }

        // 5. Acceptance Criteria 2: Cập nhật trạng thái Rejected, TUYỆT ĐỐI GIỮ NGUYÊN appointment cũ (Old slot held)
        var reviewerUserId = _currentUserService.UserId ?? Guid.Empty;
        rescheduleRequest.Reject(reviewerUserId, request.ReviewNotes);

        // 6. Ghi nhận lịch sử kiểm toán (Audit Log)
        var jsonOptions = new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        var metadata = new
        {
            BookingCode = appointment.BookingCode,
            RescheduleRequestId = rescheduleRequest.Id,
            RejectedAt = rescheduleRequest.ReviewedAt,
            ReviewNotes = request.ReviewNotes,
            OldStartAt = appointment.StartAt,
            OldDoctorId = appointment.DoctorId
        };

        var auditLog = AuditLog.Create(
            actorType: _currentUserService.IsAdmin ? "ADMIN" : "RECEPTIONIST",
            actorUserId: reviewerUserId,
            action: "REJECT_RESCHEDULE_REQUEST",
            entityType: "Appointment",
            entityId: appointment.Id,
            metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
        );

        _context.AuditLogs.Add(auditLog);

        // 7. Lưu thay đổi
        await _context.SaveChangesAsync(cancellationToken);

        var response = new ReviewRescheduleResponse(
            RequestId: rescheduleRequest.Id,
            AppointmentId: appointment.Id,
            BookingCode: appointment.BookingCode,
            Status: rescheduleRequest.Status,
            CurrentStartAtUtc: appointment.StartAt,
            CurrentEndAtUtc: appointment.EndAt,
            DoctorId: appointment.DoctorId,
            ReviewedByUserId: rescheduleRequest.ReviewedByUserId,
            ReviewedAtUtc: rescheduleRequest.ReviewedAt,
            ReviewNotes: rescheduleRequest.ReviewNotes,
            Message: "Yêu cầu đổi lịch đã bị từ chối. Lịch hẹn ban đầu của bệnh nhân vẫn được giữ nguyên."
        );

        return Result<ReviewRescheduleResponse>.Success(response, 200);
    }
}
