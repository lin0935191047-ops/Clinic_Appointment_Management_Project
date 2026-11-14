namespace ClinicAppointment.Application.Features.Doctors.DTOs;

public record DoctorDto(
    Guid Id,
    Guid UserId,
    string Email,
    Guid SpecialtyId,
    string SpecialtyName,
    string SpecialtySlug,
    string DoctorCode,
    string FullName,
    string? Title,
    string? Bio,
    string? RoomNumber,
    bool IsActive
);

public record CreateDoctorRequest(
    string Email,
    string? Password,
    Guid SpecialtyId,
    string DoctorCode,
    string FullName,
    string? Title = null,
    string? Bio = null,
    string? RoomNumber = null,
    bool IsActive = true
);

public record UpdateDoctorRequest(
    Guid SpecialtyId,
    string FullName,
    string? Title = null,
    string? Bio = null,
    string? RoomNumber = null,
    bool? IsActive = null
);

public record UpdateDoctorStatusRequest(
    bool IsActive
);
