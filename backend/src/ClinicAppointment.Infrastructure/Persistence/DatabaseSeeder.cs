using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Persistence;

public class DatabaseSeeder
{
    private readonly ApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        ApplicationDbContext context, 
        IPasswordHasher passwordHasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Bắt đầu thực thi Seed dữ liệu nền tảng...");

        // 1. Seed Roles (Idempotent)
        var rolesToSeed = new[]
        {
            (Code: Role.AdminCode, Name: "Quản trị viên"),
            (Code: Role.DoctorCode, Name: "Bác sĩ"),
            (Code: Role.ReceptionistCode, Name: "Lễ tân"),
            (Code: Role.PatientCode, Name: "Bệnh nhân")
        };

        foreach (var r in rolesToSeed)
        {
            if (!await _context.Roles.AnyAsync(role => role.Code == r.Code, cancellationToken))
            {
                _context.Roles.Add(Role.Create(r.Code, r.Name));
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        // 2. Seed Default Admin User
        var adminRole = await _context.Roles.FirstAsync(r => r.Code == Role.AdminCode, cancellationToken);
        var adminEmail = "admin@clinic.vn";
        var normalizedAdminEmail = adminEmail.ToUpperInvariant();

        var adminUser = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedAdminEmail, cancellationToken);

        if (adminUser == null)
        {
            var pwdHash = _passwordHasher.HashPassword("Admin@Clinic2026!");
            adminUser = User.Create(adminEmail, pwdHash);
            adminUser.EmailVerifiedAt = DateTime.UtcNow;
            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync(cancellationToken);

            _context.UserRoles.Add(new UserRoleAssignment
            {
                UserId = adminUser.Id,
                RoleId = adminRole.Id,
                AssignedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Đã khởi tạo tài khoản Quản trị viên mặc định: {Email}", adminEmail);
        }

        // 3. Seed Specialties
        var specialtiesData = new[]
        {
            (Code: "INT_MED", Name: "Nội Tổng Quát", Slug: "noi-tong-quat", Desc: "Khám và điều trị các bệnh lý nội khoa người lớn"),
            (Code: "PED", Name: "Nhi Khoa", Slug: "nhi-khoa", Desc: "Chăm sóc sức khỏe toàn diện và khám bệnh cho trẻ em"),
            (Code: "CARD", Name: "Tim Mạch", Slug: "tim-mach", Desc: "Chẩn đoán và theo dõi huyết áp, tim mạch, mạch máu"),
            (Code: "ENT", Name: "Tai Mũi Họng", Slug: "tai-mui-hong", Desc: "Khám nội soi và điều trị các bệnh tai mũi họng"),
            (Code: "DENT", Name: "Răng Hàm Mặt", Slug: "rang-ham-mat", Desc: "Khám nha khoa, nhổ răng, lấy cao răng, phục hình")
        };

        foreach (var s in specialtiesData)
        {
            if (!await _context.Specialties.AnyAsync(sp => sp.Code == s.Code, cancellationToken))
            {
                _context.Specialties.Add(new Specialty
                {
                    Code = s.Code,
                    Name = s.Name,
                    Slug = s.Slug,
                    Description = s.Desc,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Seed Services
        var internalMedSpec = await _context.Specialties.FirstAsync(s => s.Code == "INT_MED", cancellationToken);
        var pedSpec = await _context.Specialties.FirstAsync(s => s.Code == "PED", cancellationToken);
        var cardSpec = await _context.Specialties.FirstAsync(s => s.Code == "CARD", cancellationToken);
        var entSpec = await _context.Specialties.FirstAsync(s => s.Code == "ENT", cancellationToken);
        var dentSpec = await _context.Specialties.FirstAsync(s => s.Code == "DENT", cancellationToken);

        var servicesData = new[]
        {
            (Code: "SRV_INT_GEN", Name: "Khám Nội Tổng Quát", SpecId: internalMedSpec.Id, Min: 30, Price: 150000m, Desc: "Khám lâm sàng, tư vấn đơn thuốc và chỉ định xét nghiệm nếu cần"),
            (Code: "SRV_PED_GEN", Name: "Khám Nhi Tổng Quát", SpecId: pedSpec.Id, Min: 30, Price: 200000m, Desc: "Đánh giá phát triển thể chất và khám bệnh lý trẻ nhỏ"),
            (Code: "SRV_CARD_CON", Name: "Khám & Tư Vấn Tim Mạch", SpecId: cardSpec.Id, Min: 30, Price: 250000m, Desc: "Đo điện tim, tư vấn bệnh lý tăng huyết áp và xơ vữa"),
            (Code: "SRV_ENT_ENDO", Name: "Nội Soi Tai Mũi Họng", SpecId: entSpec.Id, Min: 30, Price: 180000m, Desc: "Nội soi chẩn đoán bằng ống soi mềm độ nét cao"),
            (Code: "SRV_DENT_GEN", Name: "Khám Nha Khoa Tổng Quát", SpecId: dentSpec.Id, Min: 30, Price: 150000m, Desc: "Kiểm tra răng miệng, tư vấn chăm sóc và cạo vôi răng")
        };

        foreach (var srv in servicesData)
        {
            if (!await _context.Services.AnyAsync(s => s.Code == srv.Code, cancellationToken))
            {
                _context.Services.Add(new Service
                {
                    SpecialtyId = srv.SpecId,
                    Code = srv.Code,
                    Name = srv.Name,
                    Description = srv.Desc,
                    DurationMinutes = srv.Min,
                    Price = srv.Price,
                    Currency = "VND",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seed dữ liệu nền tảng hoàn tất thành công.");
    }
}
