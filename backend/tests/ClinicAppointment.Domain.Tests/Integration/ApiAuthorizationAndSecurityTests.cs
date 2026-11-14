using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ClinicAppointment.Application.Common.Models;
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
/// Kiểm thử phân quyền truy cập Web API, kiểm soát vai trò (RBAC), chống IDOR và bảo vệ dữ liệu nhạy cảm (FEAT-051)
/// </summary>
public class ApiAuthorizationAndSecurityTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public ApiAuthorizationAndSecurityTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<string> LoginAndGetJwtTokenAsync(string email, string password)
    {
        var loginRequest = new LoginRequest(email, password);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK, "Đăng nhập nhân viên phải thành công");

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponseDto>>(JsonOptions);
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        return body.Data!.AccessToken;
    }

    [Theory]
    [InlineData("/api/v1/admin/reports/dashboard")]
    [InlineData("/api/v1/doctor/dashboard")]
    [InlineData("/api/v1/reception/reschedule-requests")]
    public async Task AnonymousAccess_ToProtectedEndpoints_Returns401Unauthorized(string endpoint)
    {
        // Act: Gửi request không có JWT Header
        var response = await _client.GetAsync(endpoint);

        // Assert: Bắt buộc bị từ chối 401 Unauthorized
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ReceptionistRole_AccessToDoctorOrAdminEndpoints_Returns403Forbidden()
    {
        // Arrange: Đăng nhập bằng tài khoản Lễ tân
        var receptionToken = await LoginAndGetJwtTokenAsync("reception@clinic.vn", "Reception@123");
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", receptionToken);

        // Act & Assert 1: Lễ tân cố truy cập Admin Dashboard
        var adminRes = await client.GetAsync("/api/v1/admin/reports/dashboard");
        adminRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Lễ tân không được phép xem Admin Dashboard");

        // Act & Assert 2: Lễ tân cố truy cập Doctor Dashboard
        var doctorRes = await client.GetAsync("/api/v1/doctor/dashboard");
        doctorRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Lễ tân không được phép xem Doctor Dashboard");
    }

    [Fact]
    public async Task DoctorRole_AccessToAdminEndpoints_Returns403Forbidden()
    {
        // Arrange: Đăng nhập bằng tài khoản Bác sĩ
        var doctorToken = await LoginAndGetJwtTokenAsync("doctor@clinic.vn", "Doctor@123");
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doctorToken);

        // Act & Assert: Bác sĩ cố truy cập Admin Dashboard
        var adminRes = await client.GetAsync("/api/v1/admin/reports/dashboard");
        adminRes.StatusCode.Should().Be(HttpStatusCode.Forbidden, "Bác sĩ không được phép truy cập Admin Dashboard");
    }

    [Fact]
    public async Task DoctorRole_AccessToDoctorDashboard_Returns200OkWithAssignedScope()
    {
        // Arrange: Đăng nhập Bác sĩ
        var doctorToken = await LoginAndGetJwtTokenAsync("doctor@clinic.vn", "Doctor@123");
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", doctorToken);

        // Act: Truy cập Doctor Dashboard hợp lệ
        var response = await client.GetAsync("/api/v1/doctor/dashboard");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("todayAppointments");
        body.Should().Contain("waitingQueue");
    }

    [Fact]
    public async Task SecurityAudit_ErrorResponsesAndPayloads_DoNotExposeSecretsOrSensitivePii()
    {
        // 1. Gửi request sai thông tin đăng nhập
        var badResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest("nonexistent@clinic.vn", "WrongPassword123"));
        badResponse.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);

        var body = await badResponse.Content.ReadAsStringAsync();
        
        // Assert: Không bao giờ được chứa password hash, OTP hay connection string
        body.Should().NotContain("PasswordHash");
        body.Should().NotContain("Password@123");
        body.Should().NotContain("Postgres@123");
        body.Should().NotContain("SELECT ");
        body.Should().NotContain("Exception: ");
    }
}
