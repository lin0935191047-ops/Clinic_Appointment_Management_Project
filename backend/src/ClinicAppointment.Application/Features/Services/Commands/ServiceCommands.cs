using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Services.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Services.Commands;

public record CreateServiceCommand(CreateServiceRequest Request) : IRequest<ServiceDto>;
public record UpdateServiceCommand(Guid Id, UpdateServiceRequest Request) : IRequest<ServiceDto>;
public record SetServiceStatusCommand(Guid Id, bool IsActive) : IRequest<bool>;
public record DeleteServiceCommand(Guid Id) : IRequest<bool>;

public class ServiceCommandHandlers :
    IRequestHandler<CreateServiceCommand, ServiceDto>,
    IRequestHandler<UpdateServiceCommand, ServiceDto>,
    IRequestHandler<SetServiceStatusCommand, bool>,
    IRequestHandler<DeleteServiceCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public ServiceCommandHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var code = req.Code.Trim().ToUpperInvariant();

        // 1. Kiểm tra tính duy nhất của Mã dịch vụ
        var codeExists = await _context.Services
            .AnyAsync(s => s.Code == code, cancellationToken);
        if (codeExists)
            throw new InvalidOperationException($"Mã dịch vụ '{code}' đã tồn tại trong hệ thống.");

        // 2. Kiểm tra Chuyên khoa tồn tại và đang hoạt động
        var specialty = await _context.Specialties
            .FirstOrDefaultAsync(s => s.Id == req.SpecialtyId, cancellationToken);
        if (specialty == null)
            throw new InvalidOperationException($"Chuyên khoa với mã '{req.SpecialtyId}' không tồn tại.");

        if (!specialty.IsActive)
            throw new InvalidOperationException($"Chuyên khoa '{specialty.Name}' hiện đang bị vô hiệu hóa.");

        var service = Service.Create(
            specialtyId: specialty.Id,
            code: code,
            name: req.Name,
            durationMinutes: req.DurationMinutes,
            price: req.Price,
            currency: req.Currency,
            description: req.Description,
            isActive: req.IsActive
        );

        _context.Services.Add(service);
        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceDto(
            Id: service.Id,
            SpecialtyId: specialty.Id,
            SpecialtyName: specialty.Name,
            SpecialtySlug: specialty.Slug,
            Code: service.Code,
            Name: service.Name,
            Description: service.Description,
            DurationMinutes: service.DurationMinutes,
            Price: service.Price,
            Currency: service.Currency,
            IsActive: service.IsActive
        );
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var service = await _context.Services
            .Include(s => s.Specialty)
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null)
            throw new KeyNotFoundException($"Không tìm thấy dịch vụ khám với định danh '{request.Id}'.");

        var specialty = await _context.Specialties
            .FirstOrDefaultAsync(sp => sp.Id == req.SpecialtyId, cancellationToken);
        if (specialty == null)
            throw new InvalidOperationException($"Chuyên khoa với mã '{req.SpecialtyId}' không tồn tại.");

        service.Update(
            specialtyId: specialty.Id,
            name: req.Name,
            durationMinutes: req.DurationMinutes,
            price: req.Price,
            description: req.Description,
            isActive: req.IsActive
        );

        await _context.SaveChangesAsync(cancellationToken);

        return new ServiceDto(
            Id: service.Id,
            SpecialtyId: specialty.Id,
            SpecialtyName: specialty.Name,
            SpecialtySlug: specialty.Slug,
            Code: service.Code,
            Name: service.Name,
            Description: service.Description,
            DurationMinutes: service.DurationMinutes,
            Price: service.Price,
            Currency: service.Currency,
            IsActive: service.IsActive
        );
    }

    public async Task<bool> Handle(SetServiceStatusCommand request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null)
            throw new KeyNotFoundException($"Không tìm thấy dịch vụ khám với định danh '{request.Id}'.");

        service.SetStatus(request.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken cancellationToken)
    {
        var service = await _context.Services
            .FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);

        if (service == null)
            throw new KeyNotFoundException($"Không tìm thấy dịch vụ khám với định danh '{request.Id}'.");

        // Kiểm tra xem dịch vụ đã có ca khám nào trong hệ thống chưa
        var hasAppointments = await _context.Appointments
            .AnyAsync(a => a.ServiceId == service.Id, cancellationToken);

        if (hasAppointments)
        {
            // Bảo toàn snapshot và lịch sử y tế: Chuyển sang Inactive thay vì xóa vật lý
            service.SetStatus(false);
        }
        else
        {
            _context.Services.Remove(service);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
