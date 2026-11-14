using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace ClinicAppointment.Domain.Tests.Integration;

public class SafeJsonOutputFormatter : TextOutputFormatter
{
    private readonly JsonSerializerOptions _options;

    public SafeJsonOutputFormatter(JsonSerializerOptions options)
    {
        _options = options;
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/json"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/json"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/*+json"));
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var httpContext = context.HttpContext;
        var objectType = context.ObjectType ?? context.Object?.GetType() ?? typeof(object);
        await JsonSerializer.SerializeAsync(httpContext.Response.Body, context.Object, objectType, _options, httpContext.RequestAborted);
    }
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"ClinicTestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Thay thế SystemTextJsonOutputFormatter bằng SafeJsonOutputFormatter để tương thích với TestHost trên .NET 10 (tránh PipeWriter.UnflushedBytes mismatch)
            services.Configure<MvcOptions>(options =>
            {
                var existingFormatter = options.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().FirstOrDefault();
                var jsonOptions = existingFormatter?.SerializerOptions ?? new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                options.OutputFormatters.RemoveType<SystemTextJsonOutputFormatter>();
                options.OutputFormatters.Insert(0, new SafeJsonOutputFormatter(jsonOptions));
            });

            // 1. Gỡ bỏ DbContext cũ cấu hình Npgsql
            services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));
            services.RemoveAll(typeof(ApplicationDbContext));
            services.RemoveAll(typeof(IApplicationDbContext));

            // 2. Đăng ký ApplicationDbContext sử dụng InMemory Database
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

            // 3. Khởi tạo schema và seed dữ liệu chuẩn cho Integration Tests
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            var passwordHasher = scopedServices.GetRequiredService<IPasswordHasher>();

            db.Database.EnsureCreated();
            SeedTestData(db, passwordHasher);
        });
    }

    protected override void ConfigureClient(HttpClient client)
    {
        base.ConfigureClient(client);
        client.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Encoding", "identity");
    }

    private static void SeedTestData(ApplicationDbContext db, IPasswordHasher passwordHasher)
    {
        // 1. Roles
        var adminRole = Role.Create(Role.AdminCode, "Quản trị viên");
        var doctorRole = Role.Create(Role.DoctorCode, "Bác sĩ");
        var receptionistRole = Role.Create(Role.ReceptionistCode, "Lễ tân");
        var patientRole = Role.Create(Role.PatientCode, "Bệnh nhân");

        db.Roles.AddRange(adminRole, doctorRole, receptionistRole, patientRole);

        // 2. Users
        // Admin
        var adminUser = User.Create("admin@clinic.vn", passwordHasher.HashPassword("Admin@123"));
        adminUser.UserRoles.Add(new UserRoleAssignment { RoleId = adminRole.Id, AssignedAt = DateTime.UtcNow });

        // Receptionist
        var receptionUser = User.Create("reception@clinic.vn", passwordHasher.HashPassword("Reception@123"));
        receptionUser.UserRoles.Add(new UserRoleAssignment { RoleId = receptionistRole.Id, AssignedAt = DateTime.UtcNow });

        // Doctor
        var doctorUser = User.Create("doctor@clinic.vn", passwordHasher.HashPassword("Doctor@123"));
        doctorUser.UserRoles.Add(new UserRoleAssignment { RoleId = doctorRole.Id, AssignedAt = DateTime.UtcNow });

        db.Users.AddRange(adminUser, receptionUser, doctorUser);

        // 3. Specialty
        var specialty = Specialty.Create("CARDIO", "Tim Mạch", "tim-mach", "Chuyên khoa Tim Mạch");
        db.Specialties.Add(specialty);

        // 4. Doctor Profile
        var doctor = Doctor.Create(
            userId: doctorUser.Id,
            specialtyId: specialty.Id,
            doctorCode: "DOC-CARDIO-01",
            fullName: "BS. Nguyễn Văn An",
            title: "BS. CKI",
            roomNumber: "P.101");
        db.Doctors.Add(doctor);

        // 5. Clinic Service
        var service = Service.Create(
            specialtyId: specialty.Id,
            code: "SRV-CARDIO-01",
            name: "Khám Tim Mạch Chuyên Sâu",
            durationMinutes: 30,
            price: 250_000m,
            description: "Dịch vụ khám và tư vấn tim mạch");
        db.Services.Add(service);

        // 6. Doctor Schedules (T2 - CN: 07:00 - 21:00)
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (short day = 1; day <= 7; day++)
        {
            var schedule = DoctorSchedule.Create(
                doctorId: doctor.Id,
                dayOfWeek: day,
                startTime: new TimeOnly(7, 0),
                endTime: new TimeOnly(21, 0),
                effectiveFrom: today.AddMonths(-1),
                effectiveTo: today.AddYears(1),
                slotDurationMinutes: 30);
            db.DoctorSchedules.Add(schedule);
        }

        db.SaveChanges();
    }
}
