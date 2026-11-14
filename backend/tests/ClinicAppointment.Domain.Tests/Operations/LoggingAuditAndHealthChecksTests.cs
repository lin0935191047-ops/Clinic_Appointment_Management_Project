using System.Diagnostics;
using System.Net;
using ClinicAppointment.Api.Controllers;
using ClinicAppointment.Api.Middlewares;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Tests.Integration;
using ClinicAppointment.Infrastructure.Health;
using ClinicAppointment.Infrastructure.Logging;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace ClinicAppointment.Domain.Tests.Operations;

public class LoggingAuditAndHealthChecksTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ILogRedactor _redactor;

    public LoggingAuditAndHealthChecksTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _redactor = new LogRedactor();
    }

    #region 1. PII Redaction & No Secrets In Logs Tests

    [Fact]
    public void LogRedactor_RedactsVietnamesePhoneNumber_ProtectsPrivacy()
    {
        // Arrange
        var rawText = "Bệnh nhân Nguyễn Văn A với số điện thoại 0912345678 và 0987654321 đăng ký khám.";

        // Act
        var redacted = _redactor.Redact(rawText);

        // Assert: 10 số điện thoại được che giấu thành 091***678
        redacted.Should().NotContain("0912345678");
        redacted.Should().NotContain("0987654321");
        redacted.Should().Contain("091***678");
        redacted.Should().Contain("098***321");
    }

    [Fact]
    public void LogRedactor_RedactsNationalIdAndPassport_PreventsIdentityTheft()
    {
        // Arrange: 12 số CCCD
        var rawText = "Thông tin CCCD bệnh nhân là 079090001234 và 001200009999.";

        // Act
        var redacted = _redactor.Redact(rawText);

        // Assert
        redacted.Should().NotContain("079090001234");
        redacted.Should().NotContain("001200009999");
        redacted.Should().Contain("[REDACTED_CCCD]");
    }

    [Fact]
    public void LogRedactor_RedactsHealthInsuranceNumber_StripsInsuranceCardId()
    {
        // Arrange: Mã thẻ BHYT dạng DN4791234567890 (2 chữ cái + 13 số)
        var rawText = "Thẻ BHYT của bệnh nhân: DN4791234567890 có giá trị đến cuối năm.";

        // Act
        var redacted = _redactor.Redact(rawText);

        // Assert
        redacted.Should().NotContain("DN4791234567890");
        redacted.Should().Contain("[REDACTED_BHYT]");
    }

    [Fact]
    public void LogRedactor_RedactsOtpAndPasswords_NeverLogsPlaintextSecrets()
    {
        // Arrange: Mã OTP 6 chữ số và mật khẩu
        var rawLog = "Gửi OTP: 654321 thành công cho user. JSON payload: {\"otp\": \"123456\", \"password\": \"SuperSecret@123\"}";

        // Act
        var redacted = _redactor.Redact(rawLog);

        // Assert: Tuyệt đối không log OTP và mật khẩu
        redacted.Should().NotContain("654321");
        redacted.Should().NotContain("123456");
        redacted.Should().NotContain("SuperSecret@123");
        redacted.Should().Contain("[REDACTED_OTP]");
        redacted.Should().Contain("[REDACTED]");
    }

    [Fact]
    public void LogRedactor_RedactsBearerAndGrantTokens_StripsAuthenticationCredentials()
    {
        // Arrange: Bearer JWT và Scoped Grant Token
        var rawLog = "Header: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.xyz.123 và grant token: grnt_a1b2c3d4e5f6g7h8";

        // Act
        var redacted = _redactor.Redact(rawLog);

        // Assert
        redacted.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
        redacted.Should().NotContain("grnt_a1b2c3d4e5f6g7h8");
        redacted.Should().Contain("Bearer [REDACTED_TOKEN]");
        redacted.Should().Contain("grnt_[REDACTED]");
    }

    [Fact]
    public void LogRedactor_RedactsConnectionStringPassword_PreventsDatabaseCredentialLeakage()
    {
        // Arrange: Chuỗi kết nối PostgreSQL chứa mật khẩu
        var rawConn = "Host=localhost;Port=5432;Database=clinicappointmentdb;Username=postgres;Password=Postgres@123;";

        // Act
        var redacted = _redactor.Redact(rawConn);

        // Assert
        redacted.Should().NotContain("Postgres@123");
        redacted.Should().Contain("Password=[REDACTED]");
        redacted.Should().Contain("Database=clinicappointmentdb");
    }

    [Fact]
    public void LogRedactor_RedactDictionary_SanitizesSensitiveMetadataKeys()
    {
        // Arrange
        var dict = new Dictionary<string, object?>
        {
            ["BookingCode"] = "APT-20261115-ABCDEF",
            ["DoctorName"] = "BS. CKII Nguyễn Văn An",
            ["CitizenId"] = "079090001234",
            ["OtpCode"] = "123456",
            ["Password"] = "P@ssword123",
            ["ClinicalDiagnosis"] = "Cao huyết áp vô căn độ 2",
            ["Notes"] = "Bệnh nhân có số điện thoại 0912345678 cần tư vấn"
        };

        // Act
        var sanitized = _redactor.RedactDictionary(dict);

        // Assert: Các key nhạy cảm bị che thành [REDACTED]
        sanitized["BookingCode"].Should().Be("APT-20261115-ABCDEF");
        sanitized["CitizenId"].Should().Be("[REDACTED]");
        sanitized["OtpCode"].Should().Be("[REDACTED]");
        sanitized["Password"].Should().Be("[REDACTED]");
        sanitized["ClinicalDiagnosis"].Should().Be("[REDACTED]");
        sanitized["Notes"]?.ToString().Should().Contain("091***678");
    }

    #endregion

    #region 2. Correlation ID Middleware Tests

    [Fact]
    public async Task CorrelationId_WhenHeaderProvided_PreservesCorrelationIdInResponse()
    {
        // Arrange
        const string customCorrelationId = "test-corr-trace-999-xyz";
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", customCorrelationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        response.Headers.GetValues("X-Correlation-ID").FirstOrDefault().Should().Be(customCorrelationId);
    }

    [Fact]
    public async Task CorrelationId_WhenHeaderMissing_GeneratesNewValidCorrelationId()
    {
        // Arrange
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        var generatedId = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
        generatedId.Should().NotBeNullOrWhiteSpace();
        generatedId!.Length.Should().BeLessThanOrEqualTo(80, "Correlation ID phải <= 80 ký tự theo database schema");
    }

    [Fact]
    public async Task CorrelationId_WhenHeaderExceedsMaxLength_TruncatesTo80Chars()
    {
        // Arrange: Header dài 120 ký tự
        var longCorrelationId = new string('A', 120);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", longCorrelationId);

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        response.Headers.Contains("X-Correlation-ID").Should().BeTrue();
        var headerValue = response.Headers.GetValues("X-Correlation-ID").FirstOrDefault();
        headerValue.Should().NotBeNull();
        headerValue!.Length.Should().Be(80);
    }

    #endregion

    #region 3. DB Health Check Tests

    [Fact]
    public async Task HealthEndpoint_ComprehensiveCheck_Returns200OkAndHealthyStatus()
    {
        // Act: Gọi /health toàn diện
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Healthy");
        json.Should().Contain("version");
        json.Should().Contain("database");
    }

    [Fact]
    public async Task HealthLiveEndpoint_LivenessProbe_Returns200Ok()
    {
        // Act: Liveness probe cho container/Kubernetes
        var response = await _client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Healthy");
    }

    [Fact]
    public async Task HealthReadyEndpoint_ReadinessProbe_Returns200Ok()
    {
        // Act: Readiness probe kiểm tra DB kết nối
        var response = await _client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Healthy");
    }

    [Fact]
    public async Task DatabaseHealthCheck_DirectEvaluation_ReturnsHealthyResultWithLatency()
    {
        // Arrange: Tạo DbContext InMemory
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var db = new ApplicationDbContext(options);
        var healthCheck = new DatabaseHealthCheck(db, _redactor);

        // Act
        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("latencyMs");
        result.Data["database"].Should().Be("PostgreSQL");
    }

    [Fact]
    public async Task HealthController_WhenDatabaseFails_Returns503ServiceUnavailable()
    {
        // Arrange: Sử dụng UnhealthyHealthCheckService stub trả về Unhealthy
        var unhealthyService = new UnhealthyHealthCheckService();
        var controller = new HealthController(unhealthyService);

        // Act
        var result = await controller.Check(CancellationToken.None);

        // Assert: Bắt buộc trả về HTTP 503 Service Unavailable
        var objectResult = result as ObjectResult;
        objectResult.Should().NotBeNull();
        objectResult!.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        var apiResponse = objectResult.Value as ApiResponse<object>;
        apiResponse.Should().NotBeNull();
        apiResponse!.Success.Should().BeFalse();
        apiResponse.Message.Should().Contain("Cơ sở dữ liệu đang gặp sự cố");
    }

    #endregion

    #region 4. Exception Logging Redaction Tests

    [Fact]
    public async Task ExceptionLogging_RedactsSensitiveDataInExceptionMessage()
    {
        // Arrange
        var testLogger = new TestLogger<ExceptionHandlingMiddleware>();
        var redactor = new LogRedactor();
        var middleware = new ExceptionHandlingMiddleware(
            _ => throw new InvalidOperationException("Lỗi kết nối DB: Host=localhost;Password=SuperSecretPass! Với OTP 123456 và SĐT 0912345678"),
            testLogger,
            redactor);

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert: Middleware log message đã được redact, không lộ password, OTP hay sđt
        testLogger.LoggedMessages.Should().NotBeEmpty();
        var loggedText = string.Join(" | ", testLogger.LoggedMessages);

        loggedText.Should().NotContain("SuperSecretPass!");
        loggedText.Should().NotContain("123456");
        loggedText.Should().NotContain("0912345678");
        loggedText.Should().Contain("Password=[REDACTED]");
        loggedText.Should().Contain("[REDACTED_OTP]");
        loggedText.Should().Contain("091***678");
    }

    #endregion

    #region Test Stubs

    private class UnhealthyHealthCheckService : HealthCheckService
    {
        public override Task<HealthReport> CheckHealthAsync(
            Func<HealthCheckRegistration, bool>? predicate,
            CancellationToken cancellationToken = default)
        {
            var entries = new Dictionary<string, HealthReportEntry>
            {
                ["database_health_check"] = new HealthReportEntry(
                    HealthStatus.Unhealthy,
                    "Mất kết nối tới cơ sở dữ liệu PostgreSQL",
                    TimeSpan.FromMilliseconds(1500),
                    new Exception("Connection refused"),
                    new Dictionary<string, object> { ["latencyMs"] = 1500L, ["status"] = "Error" })
            };
            return Task.FromResult(new HealthReport(entries, TimeSpan.FromMilliseconds(1500)));
        }
    }

    private class TestLogger<T> : ILogger<T>
    {
        public List<string> LoggedMessages { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            LoggedMessages.Add(formatter(state, exception));
        }
    }

    #endregion
}
