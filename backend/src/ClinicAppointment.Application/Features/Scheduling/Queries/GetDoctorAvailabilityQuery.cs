using ClinicAppointment.Application.Features.Scheduling.DTOs;
using ClinicAppointment.Application.Features.Scheduling.Services;
using MediatR;

namespace ClinicAppointment.Application.Features.Scheduling.Queries;

public record GetDoctorAvailabilityQuery(
    Guid DoctorId,
    DateOnly Date,
    Guid? ServiceId = null,
    bool OnlyAvailable = false
) : IRequest<DoctorAvailabilityDto>;

public class GetDoctorAvailabilityQueryHandler : IRequestHandler<GetDoctorAvailabilityQuery, DoctorAvailabilityDto>
{
    private readonly IAvailabilityService _availabilityService;

    public GetDoctorAvailabilityQueryHandler(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    public async Task<DoctorAvailabilityDto> Handle(GetDoctorAvailabilityQuery request, CancellationToken cancellationToken)
    {
        return await _availabilityService.GetDoctorAvailabilityAsync(
            doctorId: request.DoctorId,
            date: request.Date,
            serviceId: request.ServiceId,
            onlyAvailable: request.OnlyAvailable,
            cancellationToken: cancellationToken
        );
    }
}
