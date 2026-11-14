namespace ClinicAppointment.Application.Common.Interfaces;

public interface IResourceAuthorizationService
{
    Task<bool> CanAccessPatientAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessAppointmentAsync(Guid appointmentId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessDoctorScheduleAsync(Guid doctorId, CancellationToken cancellationToken = default);
}
