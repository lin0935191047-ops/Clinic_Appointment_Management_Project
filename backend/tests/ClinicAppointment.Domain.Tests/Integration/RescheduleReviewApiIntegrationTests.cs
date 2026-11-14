using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Appointments.Commands.ReviewRescheduleRequest;
using ClinicAppointment.Application.Features.Appointments.Commands.SubmitRescheduleRequest;
using ClinicAppointment.Application.Features.Auth.DTOs;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ClinicAppointment.Domain.Tests.Integration;

/// <summary>
/// FEAT-050: API Integration Tests — Luồng Yêu Cầu Đổi Lịch & Lễ Tân Duyệt/Từ Chối (Reschedule Review Flow)
/// Kiểm thử End-to-End:
/// - POST /api/v1/appointments/{bookingCode}/reschedule-requests (Yêu cầu grant RESCHEDULE, lịch cũ giữ nguyên 100%)
/// - GET /api/v1/reception/reschedule-requests (Phân quyền StaffOnly: chặn Anonymous 401, cho phép Receptionist)
/// - POST /api/v1/reception/reschedule-requests/{id}/approve (Duyệt đổi lịch trong DB transaction)
/// - POST /api/v1/reception/reschedule-requests/{id}/reject (Từ chối đổi lịch kèm lý do, bảo lưu slot cũ 100%)
/// </summary>
public class RescheduleReviewApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new() 
    { 
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public RescheduleReviewApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> GetReceptionistBearerTokenAsync()
    {
        var loginReq = new LoginRequest("reception@clinic.vn", "Reception@123");
        var res = await _client.PostAsJsonAsync("/api/v1/auth/login", loginReq);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        return body.Data!.AccessToken;
    }

    private async Task<(Appointment appointment, string grantToken)> SetupAppointmentWithRescheduleGrantAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var doctor = await db.Doctors.FirstAsync();
        var service = await db.Services.FirstAsync();

        var patient = Patient.Create(
            fullName: "Vũ Đình Trọng",
            phoneNumber: "0977665544",
            dateOfBirth: new DateOnly(1991, 5, 20),
            birthYear: 1991);
        db.Patients.Add(patient);

        var originalStartAtUtc = DateTime.UtcNow.Date.AddDays(4).AddHours(2); // 09:00 VN
        var appointment = Appointment.CreateGuest(
            doctorId: doctor.Id,
            patientId: patient.Id,
            serviceId: service.Id,
            serviceName: service.Name,
            serviceDuration: service.DurationMinutes,
            servicePrice: service.Price,
            startAtUtc: originalStartAtUtc,
            reasonForVisit: "Lịch hẹn ban đầu");

        db.Appointments.Add(appointment);

        // Cấp challenge OTP và Grant token với mục đích RESCHEDULE
        var challenge = OtpChallenge.Create(appointment.Id, patient.Id, patient.PhoneNumber, OtpPurpose.RESCHEDULE, "654321");
        challenge.Verify("654321", DateTime.UtcNow, out _);
        db.OtpChallenges.Add(challenge);

        var (grant, rawToken) = OtpVerificationGrant.Issue(challenge.Id, appointment.Id, OtpPurpose.RESCHEDULE, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(grant);
        await db.SaveChangesAsync();

        return (appointment, rawToken);
    }

    [Fact]
    public async Task SubmitReschedule_WithoutGrantToken_Returns401Unauthorized()
    {
        // Arrange
        var (appointment, _) = await SetupAppointmentWithRescheduleGrantAsync();

        var requestBody = new SubmitRescheduleRequestBody(
            ProposedStartAtUtc: DateTime.UtcNow.Date.AddDays(5).AddHours(3),
            GrantToken: null, // Không có token
            RequestedDoctorId: null,
            Reason: "Muốn đổi lịch");

        // Act
        var res = await _client.PostAsJsonAsync($"/api/v1/appointments/{appointment.BookingCode}/reschedule-requests", requestBody);

        // Assert
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReceptionRescheduleRequestsQueue_Unauthenticated_Returns401Unauthorized()
    {
        // Act: Gọi hàng đợi chờ duyệt mà không có header Bearer
        _client.DefaultRequestHeaders.Authorization = null;
        var res = await _client.GetAsync("/api/v1/reception/reschedule-requests");

        // Assert: Bị chặn bởi policy StaffOnly
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RescheduleReview_FullFlow_ApproveTransfersSlotToNewTime()
    {
        // 1. Arrange: Bệnh nhân có lịch hẹn và Grant token RESCHEDULE
        var (appointment, grantToken) = await SetupAppointmentWithRescheduleGrantAsync();
        var originalSlot = appointment.StartAt;
        var proposedSlotUtc = DateTime.UtcNow.Date.AddDays(5).AddHours(3); // 10:00 VN

        // 2. Bệnh nhân gửi yêu cầu đổi lịch
        var submitBody = new SubmitRescheduleRequestBody(
            ProposedStartAtUtc: proposedSlotUtc,
            GrantToken: grantToken,
            RequestedDoctorId: null,
            Reason: "Bận công tác, xin chuyển sang ngày hôm sau");

        var submitRes = await _client.PostAsJsonAsync($"/api/v1/appointments/{appointment.BookingCode}/reschedule-requests", submitBody);
        submitRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var submitData = await submitRes.Content.ReadFromJsonAsync<ApiResponse<SubmitRescheduleRequestResponse>>(JsonOptions);
        submitData.Should().NotBeNull();
        submitData!.Data.Should().NotBeNull();
        var requestId = submitData.Data!.RequestId;
        submitData.Data.Status.Should().Be(RescheduleStatus.PendingReview);

        // Kiểm tra bất biến: Lịch hẹn ban đầu của bệnh nhân vẫn giữ nguyên slot cũ trong lúc chờ duyệt
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var aptCheck = await db.Appointments.FindAsync(appointment.Id);
            aptCheck!.StartAt.Should().Be(originalSlot, "Lịch cũ phải giữ nguyên khi chưa duyệt");
        }

        // 3. Lễ tân đăng nhập lấy Bearer token
        var staffToken = await GetReceptionistBearerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        // 4. Lễ tân duyệt yêu cầu đổi lịch POST /api/v1/reception/reschedule-requests/{requestId}/approve
        var approveBody = new ApproveRescheduleRequestBody("Đồng ý chuyển ca khám");
        var approveRes = await _client.PostAsJsonAsync($"/api/v1/reception/reschedule-requests/{requestId}/approve", approveBody);

        approveRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var approveData = await approveRes.Content.ReadFromJsonAsync<ApiResponse<ReviewRescheduleResponse>>(JsonOptions);
        approveData.Should().NotBeNull();
        approveData!.Data!.Status.Should().Be(RescheduleStatus.Approved);

        // 5. Kiểm tra trong DB: Lịch hẹn đã được cập nhật sang giờ mới và yêu cầu đổi lịch ở trạng thái Approved
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedApt = await verifyDb.Appointments.FindAsync(appointment.Id);
            var updatedReq = await verifyDb.RescheduleRequests.FindAsync(requestId);

            updatedApt!.StartAt.Should().Be(proposedSlotUtc, "Sau khi duyệt, lịch khám phải được cập nhật sang giờ mới");
            updatedReq!.Status.Should().Be(RescheduleStatus.Approved);
            updatedReq.ReviewedByUserId.Should().NotBeNull();
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }

    [Fact]
    public async Task RescheduleReview_FullFlow_RejectHoldsOldSlotIntact()
    {
        // 1. Arrange: Bệnh nhân có lịch hẹn và Grant token RESCHEDULE
        var (appointment, grantToken) = await SetupAppointmentWithRescheduleGrantAsync();
        var originalSlot = appointment.StartAt;
        var proposedSlotUtc = DateTime.UtcNow.Date.AddDays(6).AddHours(4); // 11:00 VN

        // 2. Bệnh nhân gửi yêu cầu đổi lịch
        var submitBody = new SubmitRescheduleRequestBody(
            ProposedStartAtUtc: proposedSlotUtc,
            GrantToken: grantToken,
            RequestedDoctorId: null,
            Reason: "Đổi giờ");

        var submitRes = await _client.PostAsJsonAsync($"/api/v1/appointments/{appointment.BookingCode}/reschedule-requests", submitBody);
        submitRes.StatusCode.Should().Be(HttpStatusCode.Created);

        var submitData = await submitRes.Content.ReadFromJsonAsync<ApiResponse<SubmitRescheduleRequestResponse>>(JsonOptions);
        var requestId = submitData!.Data!.RequestId;

        // 3. Lễ tân đăng nhập
        var staffToken = await GetReceptionistBearerTokenAsync();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", staffToken);

        // 4. Lễ tân từ chối yêu cầu đổi lịch POST /api/v1/reception/reschedule-requests/{requestId}/reject
        var rejectBody = new RejectRescheduleRequestBody("Khung giờ này bác sĩ đã kín lịch khám, vui lòng chọn ngày khác");
        var rejectRes = await _client.PostAsJsonAsync($"/api/v1/reception/reschedule-requests/{requestId}/reject", rejectBody);

        rejectRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejectData = await rejectRes.Content.ReadFromJsonAsync<ApiResponse<ReviewRescheduleResponse>>(JsonOptions);
        rejectData!.Data!.Status.Should().Be(RescheduleStatus.Rejected);

        // 5. Kiểm tra trong DB: Lịch hẹn ban đầu của bệnh nhân tiếp tục giữ nguyên 100% (Old slot held)
        using (var verifyScope = _factory.Services.CreateScope())
        {
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var checkedApt = await verifyDb.Appointments.FindAsync(appointment.Id);
            var checkedReq = await verifyDb.RescheduleRequests.FindAsync(requestId);

            checkedApt!.StartAt.Should().Be(originalSlot, "Khi từ chối, lịch khám ban đầu phải được giữ nguyên 100% không suy suyển");
            checkedReq!.Status.Should().Be(RescheduleStatus.Rejected);
            checkedReq.ReviewNotes.Should().Contain("bác sĩ đã kín lịch khám");
        }

        _client.DefaultRequestHeaders.Authorization = null;
    }
}
