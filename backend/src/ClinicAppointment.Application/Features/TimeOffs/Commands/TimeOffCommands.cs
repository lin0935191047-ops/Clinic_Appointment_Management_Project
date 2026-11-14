using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.TimeOffs.DTOs;
using ClinicAppointment.Application.Features.TimeOffs.Services;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.TimeOffs.Commands;

public record CreateDoctorTimeOffCommand(
    CreateDoctorTimeOffRequest Request,
    Guid CurrentUserId,
    bool IsStaff
) : IRequest<DoctorTimeOffDto>;

public record CancelDoctorTimeOffCommand(
    Guid Id,
    Guid CurrentUserId,
    bool IsStaff
) : IRequest<bool>;

public record CreateDoctorScheduleBlockCommand(
    CreateDoctorScheduleBlockRequest Request,
    Guid CurrentUserId,
    bool IsStaff
) : IRequest<DoctorScheduleBlockDto>;

public record DeactivateDoctorScheduleBlockCommand(
    Guid Id,
    Guid CurrentUserId,
    bool IsStaff
) : IRequest<bool>;

public class TimeOffCommandHandlers :
    IRequestHandler<CreateDoctorTimeOffCommand, DoctorTimeOffDto>,
    IRequestHandler<CancelDoctorTimeOffCommand, bool>,
    IRequestHandler<CreateDoctorScheduleBlockCommand, DoctorScheduleBlockDto>,
    IRequestHandler<DeactivateDoctorScheduleBlockCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IDoctorTimeOffService _timeOffService;

    public TimeOffCommandHandlers(
        IApplicationDbContext context,
        IDoctorTimeOffService timeOffService)
    {
        _context = context;
        _timeOffService = timeOffService;
    }

    public async Task<DoctorTimeOffDto> Handle(CreateDoctorTimeOffCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var utcStart = DateTime.SpecifyKind(req.StartAt, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(req.EndAt, DateTimeKind.Utc);

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == req.DoctorId, cancellationToken);

        if (doctor == null)
            throw new InvalidOperationException($"Bác sĩ với định danh '{req.DoctorId}' không tồn tại.");

        if (!doctor.IsActive)
            throw new InvalidOperationException($"Bác sĩ '{doctor.FullName}' hiện đang tạm ngừng hoạt động.");

        // Phân quyền: Bác sĩ chỉ có quyền đăng ký cho chính mình; Staff (Admin/Receptionist) có quyền đăng ký cho mọi bác sĩ
        if (!request.IsStaff && doctor.UserId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("Bác sĩ chỉ có quyền đăng ký ngày nghỉ cho chính mình.");
        }

        // Kiểm tra chồng lấn với ngày nghỉ khác đang hoạt động
        var hasOverlap = await _context.DoctorTimeOffs
            .AnyAsync(t => t.DoctorId == doctor.Id
                        && t.Status == TimeOffStatus.Active
                        && t.StartAt < utcEnd
                        && utcStart < t.EndAt, cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Bác sĩ đã có lịch nghỉ trùng hoặc giao nhau với khoảng thời gian này.");
        }

        // Invariant: Schedule change không âm thầm xóa appointments (FR-055)
        var affectedAppointments = await _timeOffService.GetAffectedAppointmentsAsync(
            doctor.Id, utcStart, utcEnd, cancellationToken);

        if (affectedAppointments.Count > 0 && !req.AllowWithExistingAppointments)
        {
            throw new InvalidOperationException(
                $"Khoảng thời gian xin nghỉ có {affectedAppointments.Count} cuộc hẹn đã được đặt trước. " +
                $"Hệ thống bảo toàn cuộc hẹn và không âm thầm xóa lịch. " +
                $"Vui lòng kiểm tra danh sách cuộc hẹn bị ảnh hưởng trước khi xác nhận cho phép đăng ký.");
        }

        var timeOff = DoctorTimeOff.Create(
            doctorId: doctor.Id,
            startAt: utcStart,
            endAt: utcEnd,
            createdByUserId: request.CurrentUserId,
            reason: req.Reason
        );

        _context.DoctorTimeOffs.Add(timeOff);
        await _context.SaveChangesAsync(cancellationToken);

        return new DoctorTimeOffDto(
            Id: timeOff.Id,
            DoctorId: doctor.Id,
            DoctorName: doctor.FullName,
            DoctorCode: doctor.DoctorCode,
            StartAt: timeOff.StartAt,
            EndAt: timeOff.EndAt,
            Reason: timeOff.Reason,
            Status: timeOff.Status,
            CreatedByUserId: timeOff.CreatedByUserId,
            CreatedAt: timeOff.CreatedAt,
            AffectedAppointments: affectedAppointments.Count > 0 ? affectedAppointments : null
        );
    }

    public async Task<bool> Handle(CancelDoctorTimeOffCommand request, CancellationToken cancellationToken)
    {
        var timeOff = await _context.DoctorTimeOffs
            .Include(t => t.Doctor)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (timeOff == null)
            throw new KeyNotFoundException($"Không tìm thấy lịch nghỉ với định danh '{request.Id}'.");

        if (!request.IsStaff && timeOff.Doctor?.UserId != request.CurrentUserId && timeOff.CreatedByUserId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền hủy lịch nghỉ này.");
        }

        timeOff.Cancel();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<DoctorScheduleBlockDto> Handle(CreateDoctorScheduleBlockCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var utcStart = DateTime.SpecifyKind(req.StartAt, DateTimeKind.Utc);
        var utcEnd = DateTime.SpecifyKind(req.EndAt, DateTimeKind.Utc);

        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == req.DoctorId, cancellationToken);

        if (doctor == null)
            throw new InvalidOperationException($"Bác sĩ với định danh '{req.DoctorId}' không tồn tại.");

        if (!doctor.IsActive)
            throw new InvalidOperationException($"Bác sĩ '{doctor.FullName}' hiện đang tạm ngừng hoạt động.");

        if (!request.IsStaff && doctor.UserId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("Bác sĩ chỉ có quyền thiết lập khung giờ bận cho chính mình.");
        }

        var hasOverlap = await _context.DoctorScheduleBlocks
            .AnyAsync(b => b.DoctorId == doctor.Id
                        && b.IsActive
                        && b.StartAt < utcEnd
                        && utcStart < b.EndAt, cancellationToken);

        if (hasOverlap)
        {
            throw new InvalidOperationException("Bác sĩ đã có khung giờ bận trùng hoặc giao nhau với khoảng thời gian này.");
        }

        // Invariant: Schedule change không âm thầm xóa appointments
        var affectedAppointments = await _timeOffService.GetAffectedAppointmentsAsync(
            doctor.Id, utcStart, utcEnd, cancellationToken);

        if (affectedAppointments.Count > 0 && !req.AllowWithExistingAppointments)
        {
            throw new InvalidOperationException(
                $"Khoảng thời gian bận có {affectedAppointments.Count} cuộc hẹn đã được đặt trước. " +
                $"Hệ thống bảo toàn cuộc hẹn và không âm thầm xóa lịch. " +
                $"Vui lòng kiểm tra danh sách cuộc hẹn bị ảnh hưởng trước khi xác nhận cho phép đăng ký.");
        }

        var block = DoctorScheduleBlock.Create(
            doctorId: doctor.Id,
            startAt: utcStart,
            endAt: utcEnd,
            createdByUserId: request.CurrentUserId,
            blockType: req.BlockType,
            reason: req.Reason
        );

        _context.DoctorScheduleBlocks.Add(block);
        await _context.SaveChangesAsync(cancellationToken);

        return new DoctorScheduleBlockDto(
            Id: block.Id,
            DoctorId: doctor.Id,
            DoctorName: doctor.FullName,
            DoctorCode: doctor.DoctorCode,
            StartAt: block.StartAt,
            EndAt: block.EndAt,
            BlockType: block.BlockType,
            Reason: block.Reason,
            IsActive: block.IsActive,
            CreatedByUserId: block.CreatedByUserId,
            CreatedAt: block.CreatedAt,
            AffectedAppointments: affectedAppointments.Count > 0 ? affectedAppointments : null
        );
    }

    public async Task<bool> Handle(DeactivateDoctorScheduleBlockCommand request, CancellationToken cancellationToken)
    {
        var block = await _context.DoctorScheduleBlocks
            .Include(b => b.Doctor)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken);

        if (block == null)
            throw new KeyNotFoundException($"Không tìm thấy khung giờ bận với định danh '{request.Id}'.");

        if (!request.IsStaff && block.Doctor?.UserId != request.CurrentUserId && block.CreatedByUserId != request.CurrentUserId)
        {
            throw new UnauthorizedAccessException("Bạn không có quyền vô hiệu hóa khung giờ bận này.");
        }

        block.Deactivate();
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
