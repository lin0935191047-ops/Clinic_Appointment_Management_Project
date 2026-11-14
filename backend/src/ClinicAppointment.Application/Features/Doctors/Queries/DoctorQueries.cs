using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Doctors.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Doctors.Queries;

public record GetActiveDoctorsQuery(Guid? SpecialtyId = null, string? SpecialtySlug = null) : IRequest<List<DoctorDto>>;
public record GetDoctorByIdQuery(Guid Id) : IRequest<DoctorDto?>;
public record GetAllDoctorsQuery(Guid? SpecialtyId = null, bool? IsActive = null) : IRequest<List<DoctorDto>>;

public class DoctorQueryHandlers :
    IRequestHandler<GetActiveDoctorsQuery, List<DoctorDto>>,
    IRequestHandler<GetDoctorByIdQuery, DoctorDto?>,
    IRequestHandler<GetAllDoctorsQuery, List<DoctorDto>>
{
    private readonly IApplicationDbContext _context;

    public DoctorQueryHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<DoctorDto>> Handle(GetActiveDoctorsQuery request, CancellationToken cancellationToken)
    {
        // Public Catalog (FR-003): Chỉ hiển thị các bác sĩ IsActive == true và chuyên khoa của họ cũng IsActive == true
        var query = _context.Doctors
            .Include(d => d.Specialty)
            .Include(d => d.Specialty!.Services)
            .Where(d => d.IsActive && d.Specialty != null && d.Specialty.IsActive)
            .AsQueryable();

        if (request.SpecialtyId.HasValue)
        {
            query = query.Where(d => d.SpecialtyId == request.SpecialtyId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SpecialtySlug))
        {
            var slug = request.SpecialtySlug.Trim().ToLowerInvariant();
            query = query.Where(d => d.Specialty!.Slug == slug);
        }

        var doctors = await query
            .OrderBy(d => d.FullName)
            .ToListAsync(cancellationToken);

        // Lấy danh sách UserId để map email
        var userIds = doctors.Select(d => d.UserId).Distinct().ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        return doctors.Select(d => new DoctorDto(
            Id: d.Id,
            UserId: d.UserId,
            Email: users.GetValueOrDefault(d.UserId, string.Empty),
            SpecialtyId: d.SpecialtyId,
            SpecialtyName: d.Specialty?.Name ?? string.Empty,
            SpecialtySlug: d.Specialty?.Slug ?? string.Empty,
            DoctorCode: d.DoctorCode,
            FullName: d.FullName,
            Title: d.Title,
            Bio: d.Bio,
            RoomNumber: d.RoomNumber,
            IsActive: d.IsActive
        )).ToList();
    }

    public async Task<DoctorDto?> Handle(GetDoctorByIdQuery request, CancellationToken cancellationToken)
    {
        var doctor = await _context.Doctors
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doctor == null)
            return null;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctor.UserId, cancellationToken);

        return new DoctorDto(
            Id: doctor.Id,
            UserId: doctor.UserId,
            Email: user?.Email ?? string.Empty,
            SpecialtyId: doctor.SpecialtyId,
            SpecialtyName: doctor.Specialty?.Name ?? string.Empty,
            SpecialtySlug: doctor.Specialty?.Slug ?? string.Empty,
            DoctorCode: doctor.DoctorCode,
            FullName: doctor.FullName,
            Title: doctor.Title,
            Bio: doctor.Bio,
            RoomNumber: doctor.RoomNumber,
            IsActive: doctor.IsActive
        );
    }

    public async Task<List<DoctorDto>> Handle(GetAllDoctorsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Doctors
            .Include(d => d.Specialty)
            .AsQueryable();

        if (request.SpecialtyId.HasValue)
        {
            query = query.Where(d => d.SpecialtyId == request.SpecialtyId.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        }

        var doctors = await query
            .OrderBy(d => d.FullName)
            .ToListAsync(cancellationToken);

        var userIds = doctors.Select(d => d.UserId).Distinct().ToList();
        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        return doctors.Select(d => new DoctorDto(
            Id: d.Id,
            UserId: d.UserId,
            Email: users.GetValueOrDefault(d.UserId, string.Empty),
            SpecialtyId: d.SpecialtyId,
            SpecialtyName: d.Specialty?.Name ?? string.Empty,
            SpecialtySlug: d.Specialty?.Slug ?? string.Empty,
            DoctorCode: d.DoctorCode,
            FullName: d.FullName,
            Title: d.Title,
            Bio: d.Bio,
            RoomNumber: d.RoomNumber,
            IsActive: d.IsActive
        )).ToList();
    }
}
