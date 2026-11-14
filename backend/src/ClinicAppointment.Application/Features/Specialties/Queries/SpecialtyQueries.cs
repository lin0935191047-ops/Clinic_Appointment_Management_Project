using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Specialties.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Specialties.Queries;

public record GetActiveSpecialtiesQuery : IRequest<List<SpecialtyDto>>;
public record GetSpecialtyBySlugOrIdQuery(string Identifier, bool IncludeInactive = false) : IRequest<SpecialtyDto?>;
public record GetAllSpecialtiesQuery(bool? IsActive = null) : IRequest<List<SpecialtyDto>>;

public class SpecialtyQueryHandlers :
    IRequestHandler<GetActiveSpecialtiesQuery, List<SpecialtyDto>>,
    IRequestHandler<GetSpecialtyBySlugOrIdQuery, SpecialtyDto?>,
    IRequestHandler<GetAllSpecialtiesQuery, List<SpecialtyDto>>
{
    private readonly IApplicationDbContext _context;

    public SpecialtyQueryHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<SpecialtyDto>> Handle(GetActiveSpecialtiesQuery request, CancellationToken cancellationToken)
    {
        // Public Catalog (FR-002): Chỉ hiển thị các chuyên khoa đang hoạt động (IsActive == true)
        var specialties = await _context.Specialties
            .Include(s => s.Services)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return specialties.Select(s => new SpecialtyDto(
            Id: s.Id,
            Code: s.Code,
            Name: s.Name,
            Slug: s.Slug,
            Description: s.Description,
            IconUrl: s.IconUrl,
            IsActive: s.IsActive,
            ServiceCount: s.Services.Count(srv => srv.IsActive)
        )).ToList();
    }

    public async Task<SpecialtyDto?> Handle(GetSpecialtyBySlugOrIdQuery request, CancellationToken cancellationToken)
    {
        var identifier = request.Identifier.Trim();
        var isGuid = Guid.TryParse(identifier, out var id);

        var query = _context.Specialties
            .Include(s => s.Services)
            .AsQueryable();

        if (!request.IncludeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var specialty = isGuid
            ? await query.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            : await query.FirstOrDefaultAsync(s => s.Slug == identifier.ToLowerInvariant(), cancellationToken);

        if (specialty == null)
            return null;

        return new SpecialtyDto(
            Id: specialty.Id,
            Code: specialty.Code,
            Name: specialty.Name,
            Slug: specialty.Slug,
            Description: specialty.Description,
            IconUrl: specialty.IconUrl,
            IsActive: specialty.IsActive,
            ServiceCount: specialty.Services.Count(srv => srv.IsActive)
        );
    }

    public async Task<List<SpecialtyDto>> Handle(GetAllSpecialtiesQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Specialties
            .Include(s => s.Services)
            .AsQueryable();

        if (request.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == request.IsActive.Value);
        }

        var list = await query
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return list.Select(s => new SpecialtyDto(
            Id: s.Id,
            Code: s.Code,
            Name: s.Name,
            Slug: s.Slug,
            Description: s.Description,
            IconUrl: s.IconUrl,
            IsActive: s.IsActive,
            ServiceCount: s.Services.Count(srv => srv.IsActive)
        )).ToList();
    }
}
