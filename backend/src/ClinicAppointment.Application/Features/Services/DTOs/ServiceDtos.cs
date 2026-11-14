namespace ClinicAppointment.Application.Features.Services.DTOs;

public record ServiceDto(
    Guid Id,
    Guid SpecialtyId,
    string SpecialtyName,
    string SpecialtySlug,
    string Code,
    string Name,
    string? Description,
    int DurationMinutes,
    decimal Price,
    string Currency,
    bool IsActive
);

public record CreateServiceRequest(
    Guid SpecialtyId,
    string Code,
    string Name,
    int DurationMinutes,
    decimal Price,
    string Currency = "VND",
    string? Description = null,
    bool IsActive = true
);

public record UpdateServiceRequest(
    Guid SpecialtyId,
    string Name,
    int DurationMinutes,
    decimal Price,
    string? Description = null,
    bool? IsActive = null
);

public record UpdateServiceStatusRequest(
    bool IsActive
);
