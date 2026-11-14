using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Appointments.Queries;

public record GetAppointmentByIdQuery(Guid Id) : IRequest<AppointmentDetailDto?>;
public record GetDoctorAssignedAppointmentsQuery(Guid DoctorId) : IRequest<List<AppointmentDetailDto>>;
public record GetPatientAppointmentsQuery(Guid PatientId) : IRequest<List<AppointmentDetailDto>>;

public class AppointmentQueryHandlers :
    IRequestHandler<GetAppointmentByIdQuery, AppointmentDetailDto?>,
    IRequestHandler<GetDoctorAssignedAppointmentsQuery, List<AppointmentDetailDto>>,
    IRequestHandler<GetPatientAppointmentsQuery, List<AppointmentDetailDto>>
{
    private readonly IApplicationDbContext _context;

    public AppointmentQueryHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppointmentDetailDto?> Handle(GetAppointmentByIdQuery request, CancellationToken cancellationToken)
    {
        var apt = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

        if (apt == null)
            return null;

        return new AppointmentDetailDto(
            Id: apt.Id,
            BookingCode: apt.BookingCode,
            DoctorId: apt.DoctorId,
            DoctorName: apt.Doctor?.FullName ?? "Bác sĩ",
            PatientId: apt.PatientId,
            PatientName: apt.Patient?.FullName ?? "Bệnh nhân",
            ServiceId: apt.ServiceId,
            ServiceName: apt.ServiceNameSnapshot,
            ServiceDuration: apt.ServiceDurationSnapshot,
            ServicePrice: apt.ServicePriceSnapshot,
            PaidAmount: apt.PaidAmount,
            StartAt: apt.StartAt,
            EndAt: apt.EndAt,
            Status: apt.Status,
            ReasonForVisit: apt.ReasonForVisit,
            IsGuest: apt.IsGuest
        );
    }

    public async Task<List<AppointmentDetailDto>> Handle(GetDoctorAssignedAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Where(a => a.DoctorId == request.DoctorId)
            .OrderByDescending(a => a.StartAt)
            .ToListAsync(cancellationToken);

        return appointments.Select(apt => new AppointmentDetailDto(
            Id: apt.Id,
            BookingCode: apt.BookingCode,
            DoctorId: apt.DoctorId,
            DoctorName: apt.Doctor?.FullName ?? "Bác sĩ",
            PatientId: apt.PatientId,
            PatientName: apt.Patient?.FullName ?? "Bệnh nhân",
            ServiceId: apt.ServiceId,
            ServiceName: apt.ServiceNameSnapshot,
            ServiceDuration: apt.ServiceDurationSnapshot,
            ServicePrice: apt.ServicePriceSnapshot,
            PaidAmount: apt.PaidAmount,
            StartAt: apt.StartAt,
            EndAt: apt.EndAt,
            Status: apt.Status,
            ReasonForVisit: apt.ReasonForVisit,
            IsGuest: apt.IsGuest
        )).ToList();
    }

    public async Task<List<AppointmentDetailDto>> Handle(GetPatientAppointmentsQuery request, CancellationToken cancellationToken)
    {
        var appointments = await _context.Appointments
            .Include(a => a.Doctor)
            .Include(a => a.Patient)
            .Where(a => a.PatientId == request.PatientId)
            .OrderByDescending(a => a.StartAt)
            .ToListAsync(cancellationToken);

        return appointments.Select(apt => new AppointmentDetailDto(
            Id: apt.Id,
            BookingCode: apt.BookingCode,
            DoctorId: apt.DoctorId,
            DoctorName: apt.Doctor?.FullName ?? "Bác sĩ",
            PatientId: apt.PatientId,
            PatientName: apt.Patient?.FullName ?? "Bệnh nhân",
            ServiceId: apt.ServiceId,
            ServiceName: apt.ServiceNameSnapshot,
            ServiceDuration: apt.ServiceDurationSnapshot,
            ServicePrice: apt.ServicePriceSnapshot,
            PaidAmount: apt.PaidAmount,
            StartAt: apt.StartAt,
            EndAt: apt.EndAt,
            Status: apt.Status,
            ReasonForVisit: apt.ReasonForVisit,
            IsGuest: apt.IsGuest
        )).ToList();
    }
}
