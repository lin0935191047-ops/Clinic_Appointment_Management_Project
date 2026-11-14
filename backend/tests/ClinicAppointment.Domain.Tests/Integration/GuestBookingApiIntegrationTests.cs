using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClinicAppointment.Domain.Tests.Integration;

/// <summary>
/// FEAT-050: API Integration Tests — Luồng Đặt Lịch Khách Vãng Lai (Guest Booking Flow)
/// Kiểm thử End-to-End thông qua WebApplicationFactory:
/// - POST /api/v1/appointments/guest không yêu cầu đăng nhập/tài khoản (No auth required)
/// - Trả về mã BookingCode độc nhất format APT-yyyyMMdd-XXXXXX
/// - Tạo hồ sơ Patient độc lập với UserId = null (Strictly NO Auto-merge)
/// - Validation chặt chẽ: bắt buộc Họ tên, ngày sinh/năm sinh, số điện thoại chuẩn Việt Nam
/// - Cơ chế chống trùng lặp Idempotency-Key
/// </summary>
public class GuestBookingApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public GuestBookingApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GuestBooking_ValidRequest_SucceedsAndCreatesAppointmentWithoutAuth()
    {
        // Arrange: Lấy thông tin Bác sĩ và Dịch vụ từ DB mẫu
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await db.Doctors.FirstAsync();
        var service = await db.Services.FirstAsync();

        // Thời gian khám: 10:00 sáng ngày mai
        var tomorrow10AmUtc = DateTime.UtcNow.Date.AddDays(1).AddHours(3); // 10:00 VN = 03:00 UTC

        var command = new BookGuestAppointmentCommand(
            FullName: "Nguyễn Văn Khách",
            PhoneNumber: "0912345678",
            DateOfBirth: new DateOnly(1995, 6, 20),
            BirthYear: 1995,
            Email: "khach.test@gmail.com",
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: tomorrow10AmUtc,
            ReasonForVisit: "Khám định kỳ tổng quát",
            IdempotencyKey: Guid.NewGuid().ToString());

        // Act: Gửi request công khai không có JWT Header
        var response = await _client.PostAsJsonAsync("/api/v1/appointments/guest", command);

        // Assert: HTTP Status thành công
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<BookGuestAppointmentResponse>>(JsonOptions);
        apiResponse.Should().NotBeNull();
        apiResponse!.Data.Should().NotBeNull();

        var data = apiResponse.Data!;
        data.BookingCode.Should().StartWith("APT-");
        data.PatientFullName.Should().Be("Nguyễn Văn Khách");
        data.ServiceName.Should().Be(service.Name);
        data.ServicePrice.Should().Be(service.Price);
        data.Status.Should().Be(AppointmentStatus.PendingConfirmation);

        // Kiểm tra cơ sở dữ liệu: Patient độc lập được tạo và UserId == null
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var savedApt = await verifyDb.Appointments
            .Include(a => a.Patient)
            .FirstOrDefaultAsync(a => a.BookingCode == data.BookingCode);

        savedApt.Should().NotBeNull();
        savedApt!.IsGuest.Should().BeTrue();
        savedApt.Patient.Should().NotBeNull();
        savedApt.Patient!.UserId.Should().BeNull("Khách vãng lai đặt lịch không tự động tạo User");
        savedApt.Patient.PhoneNumber.Should().Be("0912345678");
    }

    [Fact]
    public async Task GuestBooking_MissingFullName_Returns400BadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await db.Doctors.FirstAsync();
        var service = await db.Services.FirstAsync();

        var invalidCommand = new BookGuestAppointmentCommand(
            FullName: "", // Thiếu họ tên
            PhoneNumber: "0912345678",
            DateOfBirth: new DateOnly(1990, 1, 1),
            BirthYear: 1990,
            Email: null,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: DateTime.UtcNow.AddDays(1),
            ReasonForVisit: null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/appointments/guest", invalidCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Họ và tên bệnh nhân là bắt buộc");
    }

    [Fact]
    public async Task GuestBooking_InvalidPhoneNumberFormat_Returns400BadRequest()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await db.Doctors.FirstAsync();
        var service = await db.Services.FirstAsync();

        var invalidPhoneCommand = new BookGuestAppointmentCommand(
            FullName: "Lê Văn Sai Số",
            PhoneNumber: "12345", // Số điện thoại không đúng chuẩn 10 số di động VN
            DateOfBirth: new DateOnly(1990, 1, 1),
            BirthYear: 1990,
            Email: null,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: DateTime.UtcNow.AddDays(1),
            ReasonForVisit: null);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/appointments/guest", invalidPhoneCommand);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Số điện thoại");
    }

    [Fact]
    public async Task GuestBooking_IdempotencyKey_SamePayload_ReturnsSameResultWithoutDuplication()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await db.Doctors.FirstAsync();
        var service = await db.Services.FirstAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var targetTimeUtc = DateTime.UtcNow.Date.AddDays(2).AddHours(2); // 09:00 VN

        var command = new BookGuestAppointmentCommand(
            FullName: "Trần Trùng Lặp",
            PhoneNumber: "0933333333",
            DateOfBirth: new DateOnly(1993, 3, 3),
            BirthYear: 1993,
            Email: null,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: targetTimeUtc,
            ReasonForVisit: "Test Idempotency",
            IdempotencyKey: idempotencyKey);

        // Act 1: Lần gọi thứ 1
        var res1 = await _client.PostAsJsonAsync("/api/v1/appointments/guest", command);
        res1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var body1 = await res1.Content.ReadFromJsonAsync<ApiResponse<BookGuestAppointmentResponse>>(JsonOptions);

        // Act 2: Lần gọi thứ 2 cùng IdempotencyKey và cùng payload (giả lập retry mạng hoặc double-click)
        var res2 = await _client.PostAsJsonAsync("/api/v1/appointments/guest", command);
        res2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        var body2 = await res2.Content.ReadFromJsonAsync<ApiResponse<BookGuestAppointmentResponse>>(JsonOptions);

        // Assert: Trả về cùng một mã BookingCode, không tạo cuộc hẹn trùng lặp
        body1!.Data!.BookingCode.Should().Be(body2!.Data!.BookingCode);

        // Kiểm tra trong DB chỉ có duy nhất 1 cuộc hẹn
        using var verifyScope = _factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var count = await verifyDb.Appointments
            .CountAsync(a => a.BookingCode == body1.Data.BookingCode);
        count.Should().Be(1, "Idempotency phải bảo vệ chỉ tạo 1 cuộc hẹn duy nhất");
    }

    [Fact]
    public async Task GuestBooking_IdempotencyKey_DifferentPayload_Returns409Conflict()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var doctor = await db.Doctors.FirstAsync();
        var service = await db.Services.FirstAsync();

        var idempotencyKey = Guid.NewGuid().ToString();
        var time1 = DateTime.UtcNow.Date.AddDays(2).AddHours(4);

        var command1 = new BookGuestAppointmentCommand(
            FullName: "Người A",
            PhoneNumber: "0944444444",
            DateOfBirth: new DateOnly(1994, 4, 4),
            BirthYear: 1994,
            Email: null,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: time1,
            ReasonForVisit: "Lần 1",
            IdempotencyKey: idempotencyKey);

        var command2 = new BookGuestAppointmentCommand(
            FullName: "Người B Đổi Payload", // Payload khác hoàn toàn
            PhoneNumber: "0955555555",
            DateOfBirth: new DateOnly(1995, 5, 5),
            BirthYear: 1995,
            Email: null,
            DoctorId: doctor.Id,
            ServiceId: service.Id,
            StartAtUtc: time1.AddHours(1),
            ReasonForVisit: "Lần 2",
            IdempotencyKey: idempotencyKey);

        // Act 1
        var res1 = await _client.PostAsJsonAsync("/api/v1/appointments/guest", command1);
        res1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);

        // Act 2: Tái sử dụng cùng IdempotencyKey với payload khác
        var res2 = await _client.PostAsJsonAsync("/api/v1/appointments/guest", command2);

        // Assert: Ném 409 Conflict
        res2.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
