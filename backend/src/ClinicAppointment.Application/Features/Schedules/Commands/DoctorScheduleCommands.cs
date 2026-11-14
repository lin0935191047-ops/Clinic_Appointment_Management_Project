using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Schedules.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Schedules.Commands;

public record CreateDoctorScheduleCommand(CreateDoctorScheduleRequest Request) : IRequest<DoctorScheduleDto>;
public record UpdateDoctorScheduleCommand(Guid Id, UpdateDoctorScheduleRequest Request) : IRequest<DoctorScheduleDto>;
public record SetDoctorScheduleStatusCommand(Guid Id, bool IsActive) : IRequest<bool>;
public record DeleteDoctorScheduleCommand(Guid Id) : IRequest<bool>;

public class DoctorScheduleCommandHandlers :
    IRequestHandler<CreateDoctorScheduleCommand, DoctorScheduleDto>,
    IRequestHandler<UpdateDoctorScheduleCommand, DoctorScheduleDto>,
    IRequestHandler<SetDoctorScheduleStatusCommand, bool>,
    IRequestHandler<DeleteDoctorScheduleCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DoctorScheduleCommandHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DoctorScheduleDto> Handle(CreateDoctorScheduleCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        // 1. Kiểm tra Bác sĩ tồn tại và đang hoạt động
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == req.DoctorId, cancellationToken);

        if (doctor == null)
            throw new InvalidOperationException($"Bác sĩ với định danh '{req.DoctorId}' không tồn tại.");

        if (!doctor.IsActive)
            throw new InvalidOperationException($"Bác sĩ '{doctor.FullName}' hiện đang tạm ngừng hoạt động.");

        // 2. Kiểm tra chồng lấn (overlap) với các ca làm việc đang hoạt động của cùng bác sĩ
        if (req.IsActive)
        {
            var existingSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == req.DoctorId && s.DayOfWeek == req.DayOfWeek && s.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingSchedules)
            {
                if (CheckOverlap(
                    req.StartTime, req.EndTime, req.EffectiveFrom, req.EffectiveTo,
                    existing.StartTime, existing.EndTime, existing.EffectiveFrom, existing.EffectiveTo))
                {
                    throw new InvalidOperationException(
                        $"Lịch làm việc bị chồng lấn với ca làm việc hiện có ({existing.StartTime:HH\\:mm} - {existing.EndTime:HH\\:mm}) " +
                        $"trong cùng ngày {GetDayOfWeekName(req.DayOfWeek)}.");
                }
            }
        }

        var schedule = DoctorSchedule.Create(
            doctorId: doctor.Id,
            dayOfWeek: req.DayOfWeek,
            startTime: req.StartTime,
            endTime: req.EndTime,
            effectiveFrom: req.EffectiveFrom,
            effectiveTo: req.EffectiveTo,
            slotDurationMinutes: req.SlotDurationMinutes,
            isActive: req.IsActive
        );

        _context.DoctorSchedules.Add(schedule);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(schedule, doctor.FullName, doctor.DoctorCode);
    }

    public async Task<DoctorScheduleDto> Handle(UpdateDoctorScheduleCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;

        var schedule = await _context.DoctorSchedules
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (schedule == null)
            throw new KeyNotFoundException($"Không tìm thấy lịch làm việc với định danh '{request.Id}'.");

        var willBeActive = req.IsActive ?? schedule.IsActive;

        if (willBeActive)
        {
            var otherSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == schedule.DoctorId && s.DayOfWeek == req.DayOfWeek && s.IsActive && s.Id != schedule.Id)
                .ToListAsync(cancellationToken);

            foreach (var other in otherSchedules)
            {
                if (CheckOverlap(
                    req.StartTime, req.EndTime, req.EffectiveFrom, req.EffectiveTo,
                    other.StartTime, other.EndTime, other.EffectiveFrom, other.EffectiveTo))
                {
                    throw new InvalidOperationException(
                        $"Lịch làm việc cập nhật bị chồng lấn với ca làm việc hiện có ({other.StartTime:HH\\:mm} - {other.EndTime:HH\\:mm}) " +
                        $"trong cùng ngày {GetDayOfWeekName(req.DayOfWeek)}.");
                }
            }
        }

        schedule.Update(
            dayOfWeek: req.DayOfWeek,
            startTime: req.StartTime,
            endTime: req.EndTime,
            effectiveFrom: req.EffectiveFrom,
            effectiveTo: req.EffectiveTo,
            slotDurationMinutes: req.SlotDurationMinutes,
            isActive: req.IsActive
        );

        await _context.SaveChangesAsync(cancellationToken);

        var doctorName = schedule.Doctor?.FullName ?? string.Empty;
        var doctorCode = schedule.Doctor?.DoctorCode ?? string.Empty;

        return MapToDto(schedule, doctorName, doctorCode);
    }

    public async Task<bool> Handle(SetDoctorScheduleStatusCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (schedule == null)
            throw new KeyNotFoundException($"Không tìm thấy lịch làm việc với định danh '{request.Id}'.");

        if (request.IsActive && !schedule.IsActive)
        {
            // Kiểm tra xem khi kích hoạt lại có bị chồng lấn ca đang chạy không
            var otherSchedules = await _context.DoctorSchedules
                .Where(s => s.DoctorId == schedule.DoctorId && s.DayOfWeek == schedule.DayOfWeek && s.IsActive && s.Id != schedule.Id)
                .ToListAsync(cancellationToken);

            foreach (var other in otherSchedules)
            {
                if (CheckOverlap(
                    schedule.StartTime, schedule.EndTime, schedule.EffectiveFrom, schedule.EffectiveTo,
                    other.StartTime, other.EndTime, other.EffectiveFrom, other.EffectiveTo))
                {
                    throw new InvalidOperationException(
                        $"Không thể kích hoạt lại ca làm việc do bị chồng lấn với ca ({other.StartTime:HH\\:mm} - {other.EndTime:HH\\:mm}).");
                }
            }
        }

        schedule.SetStatus(request.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteDoctorScheduleCommand request, CancellationToken cancellationToken)
    {
        var schedule = await _context.DoctorSchedules
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (schedule == null)
            throw new KeyNotFoundException($"Không tìm thấy lịch làm việc với định danh '{request.Id}'.");

        _context.DoctorSchedules.Remove(schedule);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool CheckOverlap(
        TimeOnly startA, TimeOnly endA, DateOnly fromA, DateOnly? toA,
        TimeOnly startB, TimeOnly endB, DateOnly fromB, DateOnly? toB)
    {
        // 1. Kiểm tra ngày hiệu lực có giao nhau không
        var maxA = toA ?? DateOnly.MaxValue;
        var maxB = toB ?? DateOnly.MaxValue;
        var datesOverlap = fromA <= maxB && fromB <= maxA;
        if (!datesOverlap)
            return false;

        // 2. Kiểm tra khung giờ có giao nhau không (startA < endB && startB < endA)
        var timesOverlap = startA < endB && startB < endA;
        return timesOverlap;
    }

    public static string GetDayOfWeekName(short dayOfWeek) => dayOfWeek switch
    {
        1 => "Thứ Hai",
        2 => "Thứ Ba",
        3 => "Thứ Tư",
        4 => "Thứ Năm",
        5 => "Thứ Sáu",
        6 => "Thứ Bảy",
        7 => "Chủ Nhật",
        _ => $"Ngày {dayOfWeek}"
    };

    private static DoctorScheduleDto MapToDto(DoctorSchedule s, string doctorName, string doctorCode) => new(
        Id: s.Id,
        DoctorId: s.DoctorId,
        DoctorName: doctorName,
        DoctorCode: doctorCode,
        DayOfWeek: s.DayOfWeek,
        DayOfWeekName: GetDayOfWeekName(s.DayOfWeek),
        StartTime: s.StartTime,
        EndTime: s.EndTime,
        EffectiveFrom: s.EffectiveFrom,
        EffectiveTo: s.EffectiveTo,
        SlotDurationMinutes: s.SlotDurationMinutes,
        IsActive: s.IsActive
    );
}
