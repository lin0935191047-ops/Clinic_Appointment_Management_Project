using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment;
using ClinicAppointment.Application.Features.Otp.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClinicAppointment.Domain.Tests.Integration;

/// <summary>
/// FEAT-050: API Integration Tests — Luồng Xác Thực OTP & Tra Cứu Lịch Hẹn (OTP & Lookup Flow)
/// Kiểm thử End-to-End:
/// - POST /api/v1/appointment-access/otp/request (Anti-enumeration, Challenge binding)
/// - POST /api/v1/appointment-access/otp/verify (Verify OTP, cấp Scoped Grant Token 'grnt_...')
/// - POST /api/v1/appointments/lookup (Bắt buộc Grant token LOOKUP, bảo vệ PII tuyệt đối)
/// </summary>
public class OtpAndLookupApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public OtpAndLookupApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Appointment appointment, Patient patient)> SeedAppointmentAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var doctor = await db.Doctors.FirstAsync();
        var service = await db.Services.FirstAsync();

        var patient = Patient.Create(
            fullName: "Hoàng Minh Trí",
            phoneNumber: "0966554433",
            dateOfBirth: new DateOnly(1987, 10, 15),
            birthYear: 1987);
        db.Patients.Add(patient);

        var startAtUtc = DateTime.UtcNow.AddDays(3);
        var appointment = Appointment.CreateGuest(
            doctorId: doctor.Id,
            patientId: patient.Id,
            serviceId: service.Id,
            serviceName: service.Name,
            serviceDuration: service.DurationMinutes,
            servicePrice: service.Price,
            startAtUtc: startAtUtc,
            reasonForVisit: "Tra cứu kết quả và lịch khám");
        appointment.PaidAmount = service.Price;

        db.Appointments.Add(appointment);
        await db.SaveChangesAsync();

        return (appointment, patient);
    }

    [Fact]
    public async Task RequestOtp_AntiEnumeration_AlwaysReturnsGenericSuccessMessage()
    {
        // 1. Gửi thông tin không tồn tại -> Vẫn trả về thông báo generic (chống tấn công dò tìm)
        var nonExistentRequest = new RequestOtpRequest(
            BookingCode: "APT-99999999-FAKE01",
            PhoneNumber: "0999999999",
            Purpose: OtpPurpose.LOOKUP);

        var response = await _client.PostAsJsonAsync("/api/v1/appointment-access/otp/request", nonExistentRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RequestOtpResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data!.Success.Should().BeTrue();
        body.Data.Message.Should().Contain("Nếu thông tin mã đặt lịch và số điện thoại trùng khớp");
        body.Data.ChallengeId.Should().NotBe(Guid.Empty, "Anti-enumeration trả về dummy ChallengeId ngẫu nhiên để client/kẻ tấn công không phân biệt được booking có tồn tại hay không");

        // Xác nhận cơ sở dữ liệu: Dummy ChallengeId này tuyệt đối không được ghi vào DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dbChallenge = await db.OtpChallenges.FindAsync(body.Data.ChallengeId);
        dbChallenge.Should().BeNull("Dummy challenge tuyệt đối không được lưu vào cơ sở dữ liệu");
    }

    [Fact]
    public async Task RequestOtp_ValidBookingAndPhone_CreatesChallengeAndReturnsChallengeId()
    {
        // Arrange
        var (appointment, patient) = await SeedAppointmentAsync();

        var request = new RequestOtpRequest(
            BookingCode: appointment.BookingCode,
            PhoneNumber: patient.PhoneNumber,
            Purpose: OtpPurpose.LOOKUP);

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/appointment-access/otp/request", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ApiResponse<RequestOtpResponse>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data!.Success.Should().BeTrue();
        body.Data.ChallengeId.Should().NotBe(Guid.Empty, "Khớp thông tin phải sinh ChallengeId hợp lệ");

        // Kiểm tra trong DB: OtpChallenge đã được tạo với mục đích LOOKUP
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var challenge = await db.OtpChallenges.FindAsync(body.Data.ChallengeId);

        challenge.Should().NotBeNull();
        challenge!.Purpose.Should().Be(OtpPurpose.LOOKUP);
        challenge.AppointmentId.Should().Be(appointment.Id);
    }

    [Fact]
    public async Task VerifyOtpAndLookup_EndToEndFlow_IssuesGrantAndReturnsSummaryWithoutPii()
    {
        // Arrange: Tạo cuộc hẹn mẫu và Challenge OTP
        var (appointment, patient) = await SeedAppointmentAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Giả lập cấp Challenge với OTP biết trước "123456"
        var challenge = OtpChallenge.Create(
            appointment.Id,
            patient.Id,
            patient.PhoneNumber,
            OtpPurpose.LOOKUP,
            rawOtp: "123456");
        db.OtpChallenges.Add(challenge);
        await db.SaveChangesAsync();

        // 1. Thử nhập sai OTP -> Báo lỗi 400 Bad Request
        var wrongOtpRequest = new VerifyOtpRequest(
            ChallengeId: challenge.Id,
            Otp: "999999",
            Purpose: OtpPurpose.LOOKUP);

        var wrongRes = await _client.PostAsJsonAsync("/api/v1/appointment-access/otp/verify", wrongOtpRequest);
        wrongRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // 2. Nhập đúng OTP "123456" -> Trả về Scoped Grant Token
        var correctOtpRequest = new VerifyOtpRequest(
            ChallengeId: challenge.Id,
            Otp: "123456",
            Purpose: OtpPurpose.LOOKUP);

        var verifyRes = await _client.PostAsJsonAsync("/api/v1/appointment-access/otp/verify", correctOtpRequest);
        verifyRes.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyBody = await verifyRes.Content.ReadFromJsonAsync<ApiResponse<VerifyOtpResponse>>(JsonOptions);
        verifyBody.Should().NotBeNull();
        verifyBody!.Data.Should().NotBeNull();

        var grantToken = verifyBody.Data!.GrantToken;
        grantToken.Should().NotBeNullOrEmpty();
        grantToken!.Should().StartWith("grnt_", "Grant token phải có tiền tố grnt_");
        verifyBody.Data.TokenType.Should().Be("ScopedAppointmentGrant");

        // 3. Tra cứu lịch hẹn POST /api/v1/appointments/lookup
        // 3a. Thử tra cứu mà không truyền Grant token -> HTTP 401 Unauthorized
        var noTokenReq = new LookupAppointmentRequest(appointment.BookingCode, GrantToken: null);
        var noTokenRes = await _client.PostAsJsonAsync("/api/v1/appointments/lookup", noTokenReq);
        noTokenRes.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // 3b. Tra cứu với Grant token hợp lệ vừa cấp
        var validLookupReq = new LookupAppointmentRequest(appointment.BookingCode, grantToken);
        var lookupRes = await _client.PostAsJsonAsync("/api/v1/appointments/lookup", validLookupReq);

        lookupRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var lookupBody = await lookupRes.Content.ReadFromJsonAsync<ApiResponse<PublicAppointmentSummaryDto>>(JsonOptions);
        lookupBody.Should().NotBeNull();
        lookupBody!.Data.Should().NotBeNull();

        var summary = lookupBody.Data!;
        summary.BookingCode.Should().Be(appointment.BookingCode);
        summary.PatientFullName.Should().Be(patient.FullName);
        summary.DoctorFullName.Should().Be("BS. Nguyễn Văn An");
        summary.ServiceName.Should().Be("Khám Tim Mạch Chuyên Sâu");
        summary.ServicePrice.Should().Be(250_000m);
        summary.CanCancel.Should().BeTrue();
        summary.CanReschedule.Should().BeTrue();

        // 4. Kiểm tra bảo vệ PII (Zero PII Leakage): DTO summary không chứa CCCD, PatientId, bệnh án
        summary.GetType().GetProperty("PatientId").Should().BeNull();
        summary.GetType().GetProperty("CitizenId").Should().BeNull();
        summary.GetType().GetProperty("Diagnosis").Should().BeNull();
    }

    [Fact]
    public async Task Lookup_WithInvalidOrCrossPurposeGrant_Returns403Forbidden()
    {
        // Arrange
        var (appointment, patient) = await SeedAppointmentAsync();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Tạo Grant cấp cho mục đích CANCEL (không phải LOOKUP)
        var challenge = OtpChallenge.Create(appointment.Id, patient.Id, patient.PhoneNumber, OtpPurpose.CANCEL, "888888");
        challenge.Verify("888888", DateTime.UtcNow, out _);
        db.OtpChallenges.Add(challenge);

        var (cancelGrant, cancelToken) = OtpVerificationGrant.Issue(challenge.Id, appointment.Id, OtpPurpose.CANCEL, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(cancelGrant);
        await db.SaveChangesAsync();

        // Act: Cố tình dùng grant CANCEL để tra cứu LOOKUP
        var crossPurposeReq = new LookupAppointmentRequest(appointment.BookingCode, cancelToken);
        var response = await _client.PostAsJsonAsync("/api/v1/appointments/lookup", crossPurposeReq);

        // Assert: Bị từ chối 403 Forbidden do sai mục đích (Purpose Binding)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
