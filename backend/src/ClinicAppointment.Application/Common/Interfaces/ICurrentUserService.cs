namespace ClinicAppointment.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
    Guid? PatientProfileId { get; }
    Guid? DoctorProfileId { get; }
    bool IsAdmin { get; }
    bool IsStaff { get; }
    bool IsDoctor { get; }
    bool IsPatient { get; }
}
