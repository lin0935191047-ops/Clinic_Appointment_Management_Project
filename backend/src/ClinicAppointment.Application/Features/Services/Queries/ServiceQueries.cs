using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Services.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Services.Queries;

public record GetActiveServicesQuery(Guid? SpecialtyId = null, string? SpecialtySlug = null) : IRequest<List<ServiceDto>>;
public record GetServiceByIdQuery(Guid Id) : IRequest<ServiceDto?>;
public record GetAllServicesQuery(Guid? SpecialtyId = null, bool? IsActive = null) : IRequest<List<ServiceDto>>;

public class ServiceQueryHandlers :
    IRequestHandler<GetActiveServicesQuery, List<ServiceDto>>,
    IRequestHandler<GetServiceByIdQuery, ServiceDto?>,
    IRequestHandler<GetAllServicesQuery, List<ServiceDto>>
{
    private readonly IApplicationDbContext _context;

    public ServiceQueryHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ServiceDto>> Handle(GetActiveServicesQuery request, CancellationToken cancellationToken)
    {
        // Public Catalog (FR-004): Chỉ hiển thị các dịch vụ IsActive == true và chuyên khoa của chúng cũng IsActive == true
        var query = _context.Services
            .Include(s => s.Specialty)
            .Where(s => s.IsActive && s.Specialty != null && s.Specialty.IsActive)
            .AsQueryable();

        if (request.SpecialtyId.HasValue)
        {
            query = query.Where(s => s.SpecialtyId == request.SpecialtyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SpecialtySlug))
        {
            var slug = request.SpecialtySlug.Trim().ToLowerInvariant();
            query = query.Where(s => s.Specialty!.Slug == slug);
        }

        var services = await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return services.Select(s => new ServiceDto(
            Id: s.Id,
            SpecialtyId: s.SpecialtyId,
            SpecialtyName: s.Specialty?.Name ?? string.Empty,
            SpecialtySlug: s.Specialty?.Slug ?? string.Empty,
            Code: s.Code,
            Name: s.Name,
            Description: s.Description,
            DurationMinutes: s.DurationMinutes,
            Price: s.Price,
            Currency: s.Currency,
            IsActive: s.IsActive
        )).ToList();
    }

    public async Task<ServiceDto?> Handle(GetServiceByIdQuery request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .Include(s => s.Specialty)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null)
            return null;

        return new ServiceDto(
            Id: service.Id,
            SpecialtyId: service.SpecialtyId,
            SpecialtyName: service.Specialty?.Name ?? string.Empty,
            SpecialtySlug: service.Specialty?.Slug ?? string.Empty,
            Code: service.Code,
            Name: service.Name,
            Description: service.Description,
            DurationMinutes: service.DurationMinutes,
            Price: service.Price,
            Currency: service.Currency,
            IsActive: service.IsActive
        );
    }

    public async Task<List<ServiceDto>> Handle(GetAllServicesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Services
            .Include(s => s.Specialty)
            .AsQueryable();

        if (request.SpecialtyId.HasValue)
        {
            query = query.Where(s => s.SpecialtyId == request.SpecialtyId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        var list = await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return list.Select(s => new ServiceDto(
            Id: s.Id,
            SpecialtyId: s.SpecialtyId,
            SpecialtyName: s.Specialty?.Name ?? string.Empty,
            SpecialtySlug: s.Specialty?.Slug ?? string.Empty,
            Code: s.Code,
            Name: s.Name,
            Description: s.Description,
            DurationMinutes: s.DurationMinutes,
            Price: s.Price,
            Currency: s.Currency,
            IsActive: s.IsActive
        )).ToList();
    }
}
