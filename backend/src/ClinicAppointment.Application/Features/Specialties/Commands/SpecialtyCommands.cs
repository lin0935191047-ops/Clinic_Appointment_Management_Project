using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Specialties.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Specialties.Commands;

public record CreateSpecialtyCommand(CreateSpecialtyRequest Request) : IRequest<SpecialtyDto>;
public record UpdateSpecialtyCommand(Guid Id, UpdateSpecialtyRequest Request) : IRequest<SpecialtyDto>;
public record SetSpecialtyStatusCommand(Guid Id, bool IsActive) : IRequest<bool>;
public record DeleteSpecialtyCommand(Guid Id) : IRequest<bool>;

public class SpecialtyCommandHandlers :
    IRequestHandler<CreateSpecialtyCommand, SpecialtyDto>,
    IRequestHandler<UpdateSpecialtyCommand, SpecialtyDto>,
    IRequestHandler<SetSpecialtyStatusCommand, bool>,
    IRequestHandler<DeleteSpecialtyCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public SpecialtyCommandHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SpecialtyDto> Handle(CreateSpecialtyCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var code = req.Code.Trim().ToUpperInvariant();
        var slug = string.IsNullOrWhiteSpace(req.Slug)
            ? SlugHelper.GenerateSlug(req.Name)
            : req.Slug.Trim().ToLowerInvariant();

        // 1. Kiểm tra tính duy nhất của Mã chuyên khoa (Code)
        var codeExists = await _context.Specialties
            .AnyAsync(s => s.Code == code, cancellationToken);
        if (codeExists)
            throw new InvalidOperationException($"Mã chuyên khoa '{code}' đã tồn tại trong hệ thống.");

        // 2. Kiểm tra tính duy nhất của Đường dẫn thân thiện (Slug)
        var slugExists = await _context.Specialties
            .AnyAsync(s => s.Slug == slug, cancellationToken);
        if (slugExists)
            throw new InvalidOperationException($"Đường dẫn chuyên khoa (slug) '{slug}' đã tồn tại.");

        var specialty = Specialty.Create(
            code: code,
            name: req.Name,
            slug: slug,
            description: req.Description,
            iconUrl: req.IconUrl,
            isActive: req.IsActive
        );

        _context.Specialties.Add(specialty);
        await _context.SaveChangesAsync(cancellationToken);

        return new SpecialtyDto(
            Id: specialty.Id,
            Code: specialty.Code,
            Name: specialty.Name,
            Slug: specialty.Slug,
            Description: specialty.Description,
            IconUrl: specialty.IconUrl,
            IsActive: specialty.IsActive,
            ServiceCount: 0
        );
    }

    public async Task<SpecialtyDto> Handle(UpdateSpecialtyCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var specialty = await _context.Specialties
            .Include(s => s.Services)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (specialty == null)
            throw new KeyNotFoundException($"Không tìm thấy chuyên khoa với định danh '{request.Id}'.");

        var newSlug = string.IsNullOrWhiteSpace(req.Slug)
            ? specialty.Slug
            : req.Slug.Trim().ToLowerInvariant();

        // Nếu thay đổi slug, kiểm tra trùng với chuyên khoa khác
        if (!string.Equals(specialty.Slug, newSlug, StringComparison.OrdinalIgnoreCase))
        {
            var slugExists = await _context.Specialties
                .AnyAsync(s => s.Id != specialty.Id && s.Slug == newSlug, cancellationToken);
            if (slugExists)
                throw new InvalidOperationException($"Đường dẫn chuyên khoa (slug) '{newSlug}' đã được sử dụng.");
        }

        specialty.Update(
            name: req.Name,
            slug: newSlug,
            description: req.Description,
            iconUrl: req.IconUrl,
            isActive: req.IsActive
        );

        await _context.SaveChangesAsync(cancellationToken);

        var activeServiceCount = specialty.Services.Count(s => s.IsActive);
        return new SpecialtyDto(
            Id: specialty.Id,
            Code: specialty.Code,
            Name: specialty.Name,
            Slug: specialty.Slug,
            Description: specialty.Description,
            IconUrl: specialty.IconUrl,
            IsActive: specialty.IsActive,
            ServiceCount: activeServiceCount
        );
    }

    public async Task<bool> Handle(SetSpecialtyStatusCommand request, CancellationToken cancellationToken)
    {
        var specialty = await _context.Specialties
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (specialty == null)
            throw new KeyNotFoundException($"Không tìm thấy chuyên khoa với định danh '{request.Id}'.");

        specialty.SetStatus(request.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteSpecialtyCommand request, CancellationToken cancellationToken)
    {
        var specialty = await _context.Specialties
            .Include(s => s.Services)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (specialty == null)
            throw new KeyNotFoundException($"Không tìm thấy chuyên khoa với định danh '{request.Id}'.");

        // Kiểm tra xem chuyên khoa có dịch vụ hoặc bác sĩ liên kết không
        var hasActiveServices = specialty.Services.Any(s => s.IsActive);
        var hasDoctors = await _context.Doctors.AnyAsync(d => d.SpecialtyId == specialty.Id && d.IsActive, cancellationToken);

        if (hasActiveServices || hasDoctors)
        {
            // Để bảo toàn tính toàn vẹn dữ liệu, chuyển sang trạng thái Vô hiệu hóa (Deactivate / Soft Delete)
            specialty.SetStatus(false);
        }
        else
        {
            _context.Specialties.Remove(specialty);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
