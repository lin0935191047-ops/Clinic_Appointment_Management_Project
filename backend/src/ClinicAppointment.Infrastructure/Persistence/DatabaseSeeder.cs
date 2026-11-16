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
            (Code: "DENT", Name: "Răng Hàm Mặt", Slug: "rang-ham-mat", Desc: "Khám nha khoa, nhổ răng, lấy cao răng, phục hình"),
            (Code: "NEURO_RHEUM", Name: "Khoa Nội Thần kinh - Cơ xương khớp - Huyết học lâm sàng", Slug: "noi-than-kinh-co-xuong-khop", Desc: "Chẩn đoán và điều trị chuyên sâu bệnh lý Nội thần kinh, Cơ xương khớp và Huyết học lâm sàng")
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
        var neuroSpec = await _context.Specialties.FirstAsync(s => s.Code == "NEURO_RHEUM", cancellationToken);

        var servicesData = new[]
        {
            (Code: "SRV_INT_GEN", Name: "Khám Nội Tổng Quát", SpecId: internalMedSpec.Id, Min: 30, Price: 150000m, Desc: "Khám lâm sàng, tư vấn đơn thuốc và chỉ định xét nghiệm nếu cần"),
            (Code: "SRV_PED_GEN", Name: "Khám Nhi Tổng Quát", SpecId: pedSpec.Id, Min: 30, Price: 200000m, Desc: "Đánh giá phát triển thể chất và khám bệnh lý trẻ nhỏ"),
            (Code: "SRV_CARD_CON", Name: "Khám & Tư Vấn Tim Mạch", SpecId: cardSpec.Id, Min: 30, Price: 250000m, Desc: "Đo điện tim, tư vấn bệnh lý tăng huyết áp và xơ vữa"),
            (Code: "SRV_ENT_ENDO", Name: "Nội Soi Tai Mũi Họng", SpecId: entSpec.Id, Min: 30, Price: 180000m, Desc: "Nội soi chẩn đoán bằng ống soi mềm độ nét cao"),
            (Code: "SRV_DENT_GEN", Name: "Khám Nha Khoa Tổng Quát", SpecId: dentSpec.Id, Min: 30, Price: 150000m, Desc: "Kiểm tra răng miệng, tư vấn chăm sóc và cạo vôi răng"),
            (Code: "SRV_NEURO_GEN", Name: "Khám & Tư Vấn Nội Thần Kinh - Cơ Xương Khớp", SpecId: neuroSpec.Id, Min: 30, Price: 200000m, Desc: "Khám lâm sàng, chẩn đoán và tư vấn điều trị các bệnh lý thần kinh, cơ xương khớp và huyết học")
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

        // 5. Seed Doctors & Schedules
        var doctorRole = await _context.Roles.FirstAsync(r => r.Code == Role.DoctorCode, cancellationToken);
        var doctorsData = new[]
        {
            (Email: "dr.an@clinic.vn", Code: "DOC-INT-01", Name: "BS. CKI Nguyễn Văn An", SpecId: internalMedSpec.Id, Title: "Bác sĩ Chuyên khoa I", Room: "P.101", Bio: "Hơn 15 năm kinh nghiệm điều trị bệnh lý nội tổng quát tại các bệnh viện tuyến đầu."),
            (Email: "dr.mai@clinic.vn", Code: "DOC-PED-01", Name: "ThS. BS Trần Thị Mai", SpecId: pedSpec.Id, Title: "Thạc sĩ Bác sĩ", Room: "P.105", Bio: "Chuyên gia Nhi khoa giàu kinh nghiệm, tận tâm, thân thiện với các bé."),
            (Email: "dr.nam@clinic.vn", Code: "DOC-CARD-01", Name: "TS. BS Lê Hoàng Nam", SpecId: cardSpec.Id, Title: "Tiến sĩ Bác sĩ", Room: "P.201", Bio: "Chuyên gia Tim mạch can thiệp, nguyên Phó khoa Tim mạch BV Đa khoa."),
            (Email: "dr.trang@clinic.vn", Code: "DOC-ENT-01", Name: "BS. CKI Phạm Thu Trang", SpecId: entSpec.Id, Title: "Bác sĩ Chuyên khoa I", Room: "P.204", Bio: "Chuyên sâu nội soi Tai Mũi Họng và xử trí viêm xoang mạn tính."),
            (Email: "dr.minh@clinic.vn", Code: "DOC-DENT-01", Name: "BS. Vũ Đức Minh", SpecId: dentSpec.Id, Title: "Bác sĩ Răng Hàm Mặt", Room: "P.301", Bio: "Bác sĩ nha khoa thẩm mỹ và điều trị bệnh lý răng miệng tổng quát."),
            (Email: "dr.tuan@clinic.vn", Code: "DOC-GIST-01", Name: "BS. CKII Đặng Minh Tuấn", SpecId: neuroSpec.Id, Title: "Bác sĩ Chuyên khoa II", Room: "P.205", Bio: "Hơn 18 năm kinh nghiệm chuyên sâu trong điều trị Nội thần kinh, Cơ xương khớp và Huyết học lâm sàng tại các bệnh viện tuyến đầu.")
        };

        foreach (var d in doctorsData)
        {
            var doctorUser = await _context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == d.Email.ToUpperInvariant(), cancellationToken);
            if (doctorUser == null)
            {
                var docHash = _passwordHasher.HashPassword("Doctor@123");
                doctorUser = User.Create(d.Email, docHash);
                doctorUser.EmailVerifiedAt = DateTime.UtcNow;
                _context.Users.Add(doctorUser);
                await _context.SaveChangesAsync(cancellationToken);

                _context.UserRoles.Add(new UserRoleAssignment
                {
                    UserId = doctorUser.Id,
                    RoleId = doctorRole.Id,
                    AssignedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync(cancellationToken);
            }

            var doctor = await _context.Doctors.FirstOrDefaultAsync(doc => doc.DoctorCode == d.Code, cancellationToken);
            if (doctor == null)
            {
                doctor = Doctor.Create(doctorUser.Id, d.SpecId, d.Code, d.Name, d.Title, d.Bio, d.Room);
                _context.Doctors.Add(doctor);
                await _context.SaveChangesAsync(cancellationToken);
            }
            else if (doctor.UserId != doctorUser.Id)
            {
                doctor.UserId = doctorUser.Id;
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Seed Schedules từ Thứ 2 đến Thứ 7 (dayOfWeek 1..6)
            for (short day = 1; day <= 6; day++)
            {
                if (!await _context.DoctorSchedules.AnyAsync(s => s.DoctorId == doctor.Id && s.DayOfWeek == day, cancellationToken))
                {
                    _context.DoctorSchedules.Add(DoctorSchedule.Create(
                        doctorId: doctor.Id,
                        dayOfWeek: day,
                        startTime: new TimeOnly(8, 0),
                        endTime: new TimeOnly(17, 0),
                        effectiveFrom: new DateOnly(2026, 1, 1),
                        effectiveTo: null,
                        slotDurationMinutes: 30
                    ));
                }
            }
        }
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Seed dữ liệu nền tảng hoàn tất thành công.");
    }
}
