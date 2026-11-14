using System.IO;
using System.Text.Json;
using ClinicAppointment.Api.Middlewares;
using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Application.Common.Helpers;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Domain.Services;
using ClinicAppointment.Infrastructure.Persistence;
using ClinicAppointment.Infrastructure.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class BookingIdempotencyAndAbuseTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Doctor Doctor, Service Service) SeedDoctorAndService(ApplicationDbContext db)
    {
        var user = User.Create("doctor.sec@clinic.vn", "hash");
        var spec = Specialty.Create("CK-MAT", "Chuyên Khoa Mắt", "chuyen-khoa-mat");
        db.Users.Add(user);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(user.Id, spec.Id, "DOC-SEC", "BS. Lê Thị Bảo Mật");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-MAT", "Khám mắt chuyên sâu", 30, 250000m);
        db.Services.Add(service);

        // Ca làm việc Thứ Hai - Thứ Sáu: 08:00 - 17:00 (giờ VN)
        for (short d = 1; d <= 5; d++)
        {
            var schedule = DoctorSchedule.Create(
                doctor.Id,
                d,
                new TimeOnly(8, 0),
                new TimeOnly(17, 0),
                new DateOnly(2026, 1, 1),
                new DateOnly(2026, 12, 31));
            db.DoctorSchedules.Add(schedule);
        }

        db.SaveChanges();
        return (doctor, service);
    }

    [Fact]
    public async Task Booking_WithIdempotencyKey_FirstCall_SucceedsAndPersistsRecord()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (doctor, service) = SeedDoctorAndService(db);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var abuseProtection = new AbuseProtectionService(memoryCache);
        var duplicateService = new PatientDuplicateDetectionService();
        var handler = new BookGuestAppointmentCommandHandler(db, duplicateService, abuseProtection);

        var mondayDate = new DateOnly(2026, 11, 16);
        var startAtUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));
        var idempotencyKey = "client-uuid-key-001";

        var command = new BookGuestAppointmentCommand(
            FullName: "Nguyễn Văn Một",
            PhoneNumber: "0911223344",
            DateOfBirth: new DateOnly(1991, 1, 1),
            BirthYear: 1991,
            Email: "one@example.com",
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: startAtUtc,
            ReasonForVisit: "Khám lần đầu",
            IdempotencyKey: idempotencyKey,
            ClientIp: "192.168.1.10");

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
        result.Value.Should().NotBeNull();

        // Kiểm tra Idempotency Record được ghi vào DB
        var keyHash = IdempotencyRecord.ComputeHash(idempotencyKey);
        var idempRecord = await db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Scope == "GuestBooking" && r.KeyHash == keyHash);

        idempRecord.Should().NotBeNull();
        idempRecord!.ResourceId.Should().Be(result.Value!.AppointmentId);
        idempRecord.ResponseStatusCode.Should().Be(201);
        idempRecord.ResponseBodyJson.Should().Contain(result.Value.BookingCode);
        idempRecord.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Booking_WithSameIdempotencyKey_AndSamePayload_ReturnsCachedResponse_WithoutCreatingDuplicateBooking()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (doctor, service) = SeedDoctorAndService(db);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var abuseProtection = new AbuseProtectionService(memoryCache);
        var duplicateService = new PatientDuplicateDetectionService();
        var handler = new BookGuestAppointmentCommandHandler(db, duplicateService, abuseProtection);

        var mondayDate = new DateOnly(2026, 11, 16);
        var startAtUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));
        var idempotencyKey = "client-uuid-retry-002";

        var command = new BookGuestAppointmentCommand(
            FullName: "Trần Trùng Lặp",
            PhoneNumber: "0933445566",
            DateOfBirth: new DateOnly(1993, 3, 3),
            BirthYear: 1993,
            Email: "dup@example.com",
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: startAtUtc,
            ReasonForVisit: "Kiểm tra idempotency",
            IdempotencyKey: idempotencyKey,
            ClientIp: "192.168.1.20");

        // Act 1: Lần gọi đầu tiên
        var result1 = await handler.Handle(command, CancellationToken.None);
        result1.IsSuccess.Should().BeTrue();

        // Act 2: Client retry cùng idempotencyKey và cùng thông tin payload (ví dụ timeout mạng hoặc click đúp)
        var result2 = await handler.Handle(command, CancellationToken.None);

        // Assert
        result2.IsSuccess.Should().BeTrue();
        result2.Value!.BookingCode.Should().Be(result1.Value!.BookingCode);
        result2.Value.AppointmentId.Should().Be(result1.Value.AppointmentId);

        // Invariant: Không tạo thêm ca khám hay bệnh nhân thứ 2
        var appointmentCount = await db.Appointments.CountAsync();
        var patientCount = await db.Patients.CountAsync();
        appointmentCount.Should().Be(1, "Retry cùng idempotency key tuyệt đối không tạo duplicate booking");
        patientCount.Should().Be(1, "Retry cùng idempotency key tuyệt đối không tạo duplicate patient");
    }

    [Fact]
    public async Task Booking_WithSameIdempotencyKey_ButDifferentPayload_ThrowsConflictException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (doctor, service) = SeedDoctorAndService(db);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var abuseProtection = new AbuseProtectionService(memoryCache);
        var duplicateService = new PatientDuplicateDetectionService();
        var handler = new BookGuestAppointmentCommandHandler(db, duplicateService, abuseProtection);

        var mondayDate = new DateOnly(2026, 11, 16);
        var startAtUtc1 = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));
        var startAtUtc2 = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(10, 0));
        var sharedKey = "reused-idempotency-key-003";

        var cmd1 = new BookGuestAppointmentCommand(
            FullName: "Bệnh Nhân Một",
            PhoneNumber: "0944556677",
            DateOfBirth: new DateOnly(1994, 4, 4),
            BirthYear: 1994,
            Email: null,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: startAtUtc1,
            ReasonForVisit: "Lần 1",
            IdempotencyKey: sharedKey,
            ClientIp: "192.168.1.30");

        // Gọi lần 1 thành công
        var res1 = await handler.Handle(cmd1, CancellationToken.None);
        res1.IsSuccess.Should().BeTrue();

        // Gọi lần 2 với CÙNG idempotency key nhưng thông tin khác (khác giờ khám và khác tên)
        var cmd2 = cmd1 with 
        { 
            FullName = "Bệnh Nhân Khác Nhau", 
            StartAtUtc = startAtUtc2 
        };

        // Act & Assert
        var act = async () => await handler.Handle(cmd2, CancellationToken.None);
        await act.Should().ThrowAsync<IdempotencyConflictException>()
            .WithMessage("*Idempotency key này đã được sử dụng cho một yêu cầu đặt lịch khác*");
    }

    [Fact]
    public async Task Booking_AbuseProtection_WhenExceedingPhoneRateLimit_ThrowsRateLimitExceededException()
    {
        // Arrange: 1 số điện thoại chỉ được đặt tối đa 3 lần / 5 phút
        using var db = CreateInMemoryDbContext();
        var (doctor, service) = SeedDoctorAndService(db);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var abuseProtection = new AbuseProtectionService(memoryCache);
        var duplicateService = new PatientDuplicateDetectionService();
        var handler = new BookGuestAppointmentCommandHandler(db, duplicateService, abuseProtection);

        var mondayDate = new DateOnly(2026, 11, 16);
        var spamPhone = "0988990011";

        // Gửi 3 yêu cầu đặt lịch hợp lệ vào 3 khung giờ khác nhau
        for (int i = 0; i < 3; i++)
        {
            var startUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(8 + i, 30));
            var cmd = new BookGuestAppointmentCommand(
                $"Khách Hàng {i}", spamPhone, new DateOnly(1990, 1, 1), null, null,
                doctor.Id, service.Id, startUtc, null, ClientIp: $"10.0.0.{i}");

            var res = await handler.Handle(cmd, CancellationToken.None);
            res.IsSuccess.Should().BeTrue();
        }

        // Yêu cầu thứ 4 vượt quá giới hạn tần suất (Rate limit exceeded)
        var startUtc4 = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(13, 0));
        var cmd4 = new BookGuestAppointmentCommand(
            "Khách Hàng 4", spamPhone, new DateOnly(1990, 1, 1), null, null,
            doctor.Id, service.Id, startUtc4, null, ClientIp: "10.0.0.4");

        // Act & Assert
        var act = async () => await handler.Handle(cmd4, CancellationToken.None);
        await act.Should().ThrowAsync<RateLimitExceededException>()
            .WithMessage("*Quý khách đã gửi yêu cầu quá nhiều lần*");
    }

    [Fact]
    public async Task Booking_AbuseProtection_WhenExceedingIpRateLimit_ThrowsRateLimitExceededException()
    {
        // Arrange: 1 IP chỉ được tối đa 10 lần / 5 phút
        using var db = CreateInMemoryDbContext();
        var (doctor, service) = SeedDoctorAndService(db);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var abuseProtection = new AbuseProtectionService(memoryCache);
        var duplicateService = new PatientDuplicateDetectionService();
        var handler = new BookGuestAppointmentCommandHandler(db, duplicateService, abuseProtection);

        var clientIp = "203.162.0.100";
        var mondayDate = new DateOnly(2026, 11, 16);

        // Gửi 10 lần ghi nhận attempt từ IP này
        for (int i = 0; i < 10; i++)
        {
            abuseProtection.RecordBookingAttempt(clientIp, $"091100000{i}");
        }

        // Lần thứ 11 đặt lịch từ IP này
        var startUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(14, 0));
        var cmd = new BookGuestAppointmentCommand(
            "Spam IP User", "0999999999", new DateOnly(1990, 1, 1), null, null,
            doctor.Id, service.Id, startUtc, null, ClientIp: clientIp);

        // Act & Assert
        var act = async () => await handler.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<RateLimitExceededException>();
    }

    [Fact]
    public async Task Booking_ExpiredIdempotencyRecord_AllowsNewBooking()
    {
        // Arrange: Bản ghi Idempotency đã hết hạn
        using var db = CreateInMemoryDbContext();
        var (doctor, service) = SeedDoctorAndService(db);
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var abuseProtection = new AbuseProtectionService(memoryCache);
        var duplicateService = new PatientDuplicateDetectionService();
        var handler = new BookGuestAppointmentCommandHandler(db, duplicateService, abuseProtection);

        var expiredKey = "expired-key-123";
        var expiredRecord = IdempotencyRecord.Create(
            "GuestBooking", expiredKey, "old-payload", "Appointment", Guid.NewGuid(), 201, "{}", TimeSpan.FromHours(-1));
        db.IdempotencyRecords.Add(expiredRecord);
        await db.SaveChangesAsync();

        var mondayDate = new DateOnly(2026, 11, 16);
        var startUtc = TimeZoneHelper.ToUtc(mondayDate, new TimeOnly(9, 0));

        var cmd = new BookGuestAppointmentCommand(
            "Người Dùng Mới", "0977665544", new DateOnly(1995, 5, 5), null, null,
            doctor.Id, service.Id, startUtc, null, IdempotencyKey: expiredKey);

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert: Key hết hạn được chấp nhận cho ca khám mới
        result.IsSuccess.Should().BeTrue();
        result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_MapsIdempotencyConflict_ToHttp409Conflict()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new IdempotencyConflictException("Xung đột Idempotency Key.");

        // Act
        await ExceptionHandlingMiddleware.HandleExceptionAsync(context, exception);

        // Assert
        context.Response.StatusCode.Should().Be(409);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(responseBody);
        doc.RootElement.GetProperty("statusCode").GetInt32().Should().Be(409);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("message").GetString().Should().Be("Xung đột Idempotency Key.");
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_MapsRateLimitExceeded_ToHttp429TooManyRequests()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new RateLimitExceededException();

        // Act
        await ExceptionHandlingMiddleware.HandleExceptionAsync(context, exception);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(responseBody);
        doc.RootElement.GetProperty("statusCode").GetInt32().Should().Be(429);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("message").GetString().Should().Contain("Quý khách đã gửi yêu cầu quá nhiều lần");
    }

    [Fact]
    public async Task ExceptionHandlingMiddleware_GenericSafeErrors_HidesInternalDetailsOnUnexpectedException()
    {
        // Arrange: Ngoại lệ nội bộ chứa thông tin nhạy cảm (database password / connection string)
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var sensitiveException = new InvalidTimeZoneException(
            "FATAL: Password authentication failed for user 'postgres' at Host=internal-prod-db.clinic.vn:5432;Password=SuperSecretP@ssword123!");

        // Act
        await ExceptionHandlingMiddleware.HandleExceptionAsync(context, sensitiveException);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(responseBody);
        doc.RootElement.GetProperty("statusCode").GetInt32().Should().Be(500);
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("message").GetString().Should().Be("Đã xảy ra lỗi nội bộ trên máy chủ. Vui lòng thử lại sau.");

        // Tuyệt đối không để lộ mật khẩu, chuỗi kết nối hay chi tiết exception nội bộ
        responseBody.Should().NotContain("SuperSecretP@ssword123!");
        responseBody.Should().NotContain("internal-prod-db.clinic.vn");
        responseBody.Should().NotContain("Password authentication failed");
    }

    [Fact]
    public void IdempotencyRecord_ComputeHash_MatchesSha256Deterministically()
    {
        var hash1 = IdempotencyRecord.ComputeHash("test-key-123");
        var hash2 = IdempotencyRecord.ComputeHash("test-key-123");
        var hash3 = IdempotencyRecord.ComputeHash("test-key-different");

        hash1.Should().Equal(hash2);
        hash1.Should().NotEqual(hash3);
    }
}
