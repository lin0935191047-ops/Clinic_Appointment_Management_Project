using System.Text.Encodings.Web;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;

public class ApproveRescheduleRequestCommandHandler : IRequestHandler<ApproveRescheduleRequestCommand, Result<ReviewRescheduleResponse>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ApproveRescheduleRequestCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<Result<ReviewRescheduleResponse>> Handle(
        ApproveRescheduleRequestCommand request, 
        CancellationToken cancellationToken)
    {
        // 1. Acceptance Criteria 1: Chỉ Receptionist hoặc Admin mới có quyền duyệt yêu cầu đổi lịch
        if (!_currentUserService.IsStaff)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                "Bạn không có quyền duyệt yêu cầu đổi lịch. Chức năng này chỉ dành cho nhân viên Lễ tân hoặc Quản trị viên.", 
                403);
        }

        // 2. Tra cứu yêu cầu đổi lịch
        var rescheduleRequest = await _context.RescheduleRequests
            .Include(r => r.Appointment)
            .FirstOrDefaultAsync(r => r.Id == request.RequestId, cancellationToken);

        if (rescheduleRequest == null)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                "Không tìm thấy yêu cầu đổi lịch cần duyệt.", 
                404);
        }

        // 3. Kiểm tra trạng thái yêu cầu: Chỉ duyệt khi đang ở trạng thái PendingReview
        if (rescheduleRequest.Status != RescheduleStatus.PendingReview)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                $"Không thể duyệt yêu cầu đổi lịch ở trạng thái {rescheduleRequest.Status}. Chỉ có thể duyệt yêu cầu đang Chờ duyệt (PendingReview).", 
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

        if (appointment.Status == AppointmentStatus.Cancelled || appointment.Status == AppointmentStatus.Completed)
        {
            return Result<ReviewRescheduleResponse>.Failure(
                $"Không thể duyệt đổi lịch cho ca khám đã ở trạng thái {appointment.Status}.", 
                400);
        }

        // 4. Acceptance Criteria 3 & 4: Thực thi trong Database Transaction & Re-check Slot
        bool supportsTransaction = _context.Database.ProviderName != null &&
            !_context.Database.ProviderName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);

        IDbContextTransaction? transaction = null;
        if (supportsTransaction)
        {
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        try
        {
            var targetDoctorId = rescheduleRequest.RequestedDoctorId;
            var proposedStartAt = rescheduleRequest.ProposedStartAt;
            var proposedEndAt = rescheduleRequest.ProposedEndAt;

            // 4.1 Re-check: Kiểm tra lịch nghỉ của bác sĩ trong khung giờ mới
            bool hasTimeOff = await _context.DoctorTimeOffs.AnyAsync(t =>
                t.DoctorId == targetDoctorId &&
                t.Status == TimeOffStatus.Active &&
                t.StartAt < proposedEndAt &&
                t.EndAt > proposedStartAt, cancellationToken);

            if (hasTimeOff)
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                return Result<ReviewRescheduleResponse>.Failure(
                    "Khung giờ đề xuất hiện đã trùng với lịch nghỉ được duyệt của bác sĩ. Lịch hẹn cũ của bệnh nhân được giữ nguyên.", 
                    409);
            }

            // 4.2 Re-check: Kiểm tra lịch bận / khóa lịch của bác sĩ
            bool hasBlock = await _context.DoctorScheduleBlocks.AnyAsync(b =>
                b.DoctorId == targetDoctorId &&
                b.StartAt < proposedEndAt &&
                b.EndAt > proposedStartAt, cancellationToken);

            if (hasBlock)
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                return Result<ReviewRescheduleResponse>.Failure(
                    "Khung giờ đề xuất hiện đã bị khóa hoặc bác sĩ có lịch bận đột xuất. Lịch hẹn cũ của bệnh nhân được giữ nguyên.", 
                    409);
            }

            // 4.3 Re-check: Kiểm tra trùng với ca khám khác (ngoại trừ chính ca khám này)
            bool hasOverlap = await _context.Appointments.AnyAsync(a =>
                a.Id != appointment.Id &&
                a.DoctorId == targetDoctorId &&
                a.Status != AppointmentStatus.Cancelled &&
                a.StartAt < proposedEndAt &&
                a.EndAt > proposedStartAt, cancellationToken);

            if (hasOverlap)
            {
                if (transaction != null) await transaction.RollbackAsync(cancellationToken);
                return Result<ReviewRescheduleResponse>.Failure(
                    "Khung giờ đề xuất hiện đã có bệnh nhân khác đặt lịch hoặc đang giữ slot. Lịch hẹn cũ của bệnh nhân được giữ nguyên.", 
                    409);
            }

            // 5. Cập nhật thông tin ca khám sang slot mới
            appointment.DoctorId = targetDoctorId;
            appointment.StartAt = proposedStartAt;
            appointment.EndAt = proposedEndAt;
            appointment.UpdatedAt = DateTime.UtcNow;

            // 6. Cập nhật trạng thái yêu cầu đổi lịch sang Approved
            var reviewerUserId = _currentUserService.UserId ?? Guid.Empty;
            rescheduleRequest.Approve(reviewerUserId, request.ReviewNotes);

            // 7. Ghi nhận lịch sử kiểm toán (Audit Log)
            var jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var metadata = new
            {
                BookingCode = appointment.BookingCode,
                RescheduleRequestId = rescheduleRequest.Id,
                NewDoctorId = targetDoctorId,
                NewStartAt = proposedStartAt,
                NewEndAt = proposedEndAt,
                ReviewNotes = request.ReviewNotes,
                ApprovedAt = rescheduleRequest.ReviewedAt
            };

            var auditLog = AuditLog.Create(
                actorType: _currentUserService.IsAdmin ? "ADMIN" : "RECEPTIONIST",
                actorUserId: reviewerUserId,
                action: "APPROVE_RESCHEDULE_REQUEST",
                entityType: "Appointment",
                entityId: appointment.Id,
                metadataRedacted: JsonSerializer.Serialize(metadata, jsonOptions)
            );

            _context.AuditLogs.Add(auditLog);

            // 8. Lưu thay đổi và Commit transaction
            await _context.SaveChangesAsync(cancellationToken);

            if (transaction != null)
            {
                await transaction.CommitAsync(cancellationToken);
            }

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
                Message: "Yêu cầu đổi lịch đã được duyệt thành công. Lịch khám đã được cập nhật sang khung giờ mới."
            );

            return Result<ReviewRescheduleResponse>.Success(response, 200);
        }
        catch
        {
            if (transaction != null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            throw;
        }
    }
}
