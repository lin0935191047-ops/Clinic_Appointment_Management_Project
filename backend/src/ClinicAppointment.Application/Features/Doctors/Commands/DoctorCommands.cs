using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Doctors.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Doctors.Commands;

public record CreateDoctorCommand(CreateDoctorRequest Request) : IRequest<DoctorDto>;
public record UpdateDoctorCommand(Guid Id, UpdateDoctorRequest Request) : IRequest<DoctorDto>;
public record SetDoctorStatusCommand(Guid Id, bool IsActive) : IRequest<bool>;
public record DeleteDoctorCommand(Guid Id) : IRequest<bool>;

public class DoctorCommandHandlers :
    IRequestHandler<CreateDoctorCommand, DoctorDto>,
    IRequestHandler<UpdateDoctorCommand, DoctorDto>,
    IRequestHandler<SetDoctorStatusCommand, bool>,
    IRequestHandler<DeleteDoctorCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public DoctorCommandHandlers(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<DoctorDto> Handle(CreateDoctorCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var doctorCode = req.DoctorCode.Trim().ToUpperInvariant();

        // 1. Kiểm tra tính duy nhất của DoctorCode
        var codeExists = await _context.Doctors
            .AnyAsync(d => d.DoctorCode == doctorCode, cancellationToken);
        if (codeExists)
            throw new InvalidOperationException($"Mã bác sĩ '{doctorCode}' đã tồn tại trong hệ thống.");

        // 2. Kiểm tra Chuyên khoa tồn tại và đang hoạt động
        var specialty = await _context.Specialties
            .FirstOrDefaultAsync(s => s.Id == req.SpecialtyId, cancellationToken);
        if (specialty == null)
            throw new InvalidOperationException($"Chuyên khoa với mã '{req.SpecialtyId}' không tồn tại.");

        if (!specialty.IsActive)
            throw new InvalidOperationException($"Chuyên khoa '{specialty.Name}' hiện đang bị vô hiệu hóa.");

        // 3. Xử lý tài khoản người dùng đăng nhập cho Bác sĩ (Staff Account)
        var normalizedEmail = req.Email.Trim().ToUpperInvariant();
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

        var doctorRole = await _context.Roles
            .FirstOrDefaultAsync(r => r.Code == Role.DoctorCode, cancellationToken);
        if (doctorRole == null)
        {
            doctorRole = Role.Create(Role.DoctorCode, "Bác sĩ");
            _context.Roles.Add(doctorRole);
        }

        if (user == null)
        {
            // Tạo User mới nếu chưa tồn tại
            var rawPassword = string.IsNullOrWhiteSpace(req.Password) ? "Doctor@2026" : req.Password;
            var passwordHash = _passwordHasher.HashPassword(rawPassword);
            user = User.Create(req.Email, passwordHash);

            user.UserRoles.Add(new UserRoleAssignment
            {
                UserId = user.Id,
                RoleId = doctorRole.Id,
                AssignedAt = DateTime.UtcNow
            });

            _context.Users.Add(user);
        }
        else
        {
            // Nếu User đã tồn tại, kiểm tra xem đã gắn với hồ sơ Doctor nào chưa
            var existingDoctor = await _context.Doctors
                .AnyAsync(d => d.UserId == user.Id, cancellationToken);
            if (existingDoctor)
                throw new InvalidOperationException($"Tài khoản email '{req.Email}' đã được liên kết với một hồ sơ bác sĩ khác.");

            // Đảm bảo User có Role DOCTOR
            var hasDoctorRole = user.UserRoles.Any(ur => ur.RoleId == doctorRole.Id);
            if (!hasDoctorRole)
            {
                user.UserRoles.Add(new UserRoleAssignment
                {
                    UserId = user.Id,
                    RoleId = doctorRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
            }
        }

        // 4. Tạo hồ sơ Bác sĩ
        var doctor = Doctor.Create(
            userId: user.Id,
            specialtyId: specialty.Id,
            doctorCode: doctorCode,
            fullName: req.FullName,
            title: req.Title,
            bio: req.Bio,
            roomNumber: req.RoomNumber,
            isActive: req.IsActive
        );

        _context.Doctors.Add(doctor);
        await _context.SaveChangesAsync(cancellationToken);

        return new DoctorDto(
            Id: doctor.Id,
            UserId: user.Id,
            Email: user.Email,
            SpecialtyId: specialty.Id,
            SpecialtyName: specialty.Name,
            SpecialtySlug: specialty.Slug,
            DoctorCode: doctor.DoctorCode,
            FullName: doctor.FullName,
            Title: doctor.Title,
            Bio: doctor.Bio,
            RoomNumber: doctor.RoomNumber,
            IsActive: doctor.IsActive
        );
    }

    public async Task<DoctorDto> Handle(UpdateDoctorCommand request, CancellationToken cancellationToken)
    {
        var req = request.Request;
        var doctor = await _context.Doctors
            .Include(d => d.Specialty)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doctor == null)
            throw new KeyNotFoundException($"Không tìm thấy hồ sơ bác sĩ với định danh '{request.Id}'.");

        // Kiểm tra Chuyên khoa mới
        var specialty = await _context.Specialties
            .FirstOrDefaultAsync(s => s.Id == req.SpecialtyId, cancellationToken);
        if (specialty == null)
            throw new InvalidOperationException($"Chuyên khoa với mã '{req.SpecialtyId}' không tồn tại.");

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == doctor.UserId, cancellationToken);

        doctor.Update(
            specialtyId: specialty.Id,
            fullName: req.FullName,
            title: req.Title,
            bio: req.Bio,
            roomNumber: req.RoomNumber,
            isActive: req.IsActive
        );

        await _context.SaveChangesAsync(cancellationToken);

        return new DoctorDto(
            Id: doctor.Id,
            UserId: doctor.UserId,
            Email: user?.Email ?? string.Empty,
            SpecialtyId: specialty.Id,
            SpecialtyName: specialty.Name,
            SpecialtySlug: specialty.Slug,
            DoctorCode: doctor.DoctorCode,
            FullName: doctor.FullName,
            Title: doctor.Title,
            Bio: doctor.Bio,
            RoomNumber: doctor.RoomNumber,
            IsActive: doctor.IsActive
        );
    }

    public async Task<bool> Handle(SetDoctorStatusCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doctor == null)
            throw new KeyNotFoundException($"Không tìm thấy hồ sơ bác sĩ với định danh '{request.Id}'.");

        doctor.SetStatus(request.IsActive);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> Handle(DeleteDoctorCommand request, CancellationToken cancellationToken)
    {
        var doctor = await _context.Doctors
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (doctor == null)
            throw new KeyNotFoundException($"Không tìm thấy hồ sơ bác sĩ với định danh '{request.Id}'.");

        // Kiểm tra xem bác sĩ có ca khám nào trong hệ thống không
        var hasAppointments = await _context.Appointments
            .AnyAsync(a => a.DoctorId == doctor.Id, cancellationToken);

        if (hasAppointments)
        {
            // Bảo toàn lịch sử y tế: Chuyển sang Inactive thay vì xóa vật lý
            doctor.SetStatus(false);
        }
        else
        {
            _context.Doctors.Remove(doctor);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
