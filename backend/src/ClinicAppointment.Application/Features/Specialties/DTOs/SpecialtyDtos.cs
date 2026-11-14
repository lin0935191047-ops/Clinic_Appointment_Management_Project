namespace ClinicAppointment.Application.Features.Specialties.DTOs;

public record SpecialtyDto(
    Guid Id,
    string Code,
    string Name,
    string Slug,
    string? Description,
    string? IconUrl,
    bool IsActive,
    int ServiceCount
);

public record CreateSpecialtyRequest(
    string Code,
    string Name,
    string? Slug = null,
    string? Description = null,
    string? IconUrl = null,
    bool IsActive = true
);

public record UpdateSpecialtyRequest(
    string Name,
    string? Slug = null,
    string? Description = null,
    string? IconUrl = null,
    bool? IsActive = null
);

public record UpdateSpecialtyStatusRequest(
    bool IsActive
);
