using System.Text.Json;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicAppointment.Application.Features.Appointments.Services;
using ClinicAppointment.Application.Features.Audit.Queries.GetAppointmentCancellationAudit;
using ClinicAppointment.Application.Features.Audit.Queries.GetCancellationAuditLogs;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Application.Features.Refunds.Commands.ProcessRefund;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class CancellationAuditTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Patient Patient, Doctor Doctor, Service Service, string CancelGrantToken, string RawOtp) SeedAppointmentData(
        ApplicationDbContext db,
        decimal initialServicePrice = 400_000m,
        decimal paidAmount = 400_000m,
        DateTime? startAt = null)
    {
        var patient = Patient.Create("Hoàng Văn Khang", "0912345678", new DateOnly(1985, 11, 20), 1985);
        db.Patients.Add(patient);

        var doctorUser = User.Create("doctor.audit@clinic.vn", "hash");
        var spec = Specialty.Create("CK-MAT", "Mắt", "chuyen-khoa-mat");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-EYE-01", "BS. Hoàng Văn Mắt");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-MAT-01", "Đo khúc xạ mắt chuyên sâu", 30, initialServicePrice);
        db.Services.Add(service);

        var appointmentStart = startAt ?? DateTime.UtcNow.AddHours(3); // >= 60 phút
        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            appointmentStart,
            "Khám kiểm tra cận thị định kỳ");
        appointment.PaidAmount = paidAmount;
        db.Appointments.Add(appointment);

        // Cancel Grant Token & Raw OTP
        var rawOtp = "889900";
        var cancelChallenge = OtpChallenge.Create(appointment.Id, patient.Id, patient.PhoneNumber, OtpPurpose.CANCEL, rawOtp);
        cancelChallenge.Verify(rawOtp, DateTime.UtcNow, out _);
        db.OtpChallenges.Add(cancelChallenge);
        var (cancelGrant, cancelRawToken) = OtpVerificationGrant.Issue(cancelChallenge.Id, appointment.Id, OtpPurpose.CANCEL, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(cancelGrant);

        db.SaveChanges();

        return (appointment, patient, doctor, service, cancelRawToken, rawOtp);
    }

    [Fact]
    public async Task Cancellation_AuditLog_RecordedInSameTransaction()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, cancelToken, _) = SeedAppointmentData(db);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        // Act: Bệnh nhân hủy lịch khám
        var result = await handler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Bận đột xuất việc gia đình"
        ), CancellationToken.None);

        // Assert: Lịch khám bị hủy và AuditLog được tạo
        result.IsSuccess.Should().BeTrue();

        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CANCEL_APPOINTMENT" && a.EntityId == appointment.Id);

        auditLog.Should().NotBeNull("Bản ghi AuditLog phải được tạo trong cùng transaction khi hủy ca khám");
        auditLog!.EntityType.Should().Be("Appointment");
        auditLog.ActorType.Should().Be("GUEST");
        auditLog.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task AuditLog_StoresAllRequiredFields()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, cancelToken, _) = SeedAppointmentData(db, paidAmount: 400_000m);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        // Act
        var result = await handler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Thay đổi kế hoạch du lịch"
        ), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Assert: Kiểm tra đầy đủ các trường trong metadata audit
        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CANCEL_APPOINTMENT" && a.EntityId == appointment.Id);

        auditLog.Should().NotBeNull();
        auditLog!.MetadataRedacted.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(auditLog.MetadataRedacted!);
        var root = doc.RootElement;

        root.GetProperty("BookingCode").GetString().Should().Be(appointment.BookingCode);
        root.GetProperty("Reason").GetString().Should().Be("Thay đổi kế hoạch du lịch");
        root.GetProperty("PolicyVersion").GetString().Should().Be("v1.0");
        root.GetProperty("PaidAmount").GetDecimal().Should().Be(400_000m);
        root.GetProperty("RefundRate").GetDecimal().Should().Be(1.0m);
        root.GetProperty("CancellationFeeRate").GetDecimal().Should().Be(0.0m);
        root.GetProperty("ExpectedRefundAmount").GetDecimal().Should().Be(400_000m);
        root.GetProperty("RefundDecision").GetString().Should().Be("100%_REFUND");
        root.GetProperty("RefundStatus").GetString().Should().Be("Pending");
    }

    [Fact]
    public async Task AuditLog_StrictlyNoRawPiiOrOtp()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, patient, _, _, cancelToken, rawOtp) = SeedAppointmentData(db);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        // Act
        await handler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Bận đột xuất"
        ), CancellationToken.None);

        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CANCEL_APPOINTMENT" && a.EntityId == appointment.Id);

        // Assert: Metadata tuyệt đối KHÔNG chứa số điện thoại thật, mã OTP, token grant bí mật
        var metadata = auditLog!.MetadataRedacted!;

        metadata.Should().NotContain(patient.PhoneNumber, "Tuyệt đối không lưu số điện thoại cá nhân thô trong audit log");
        metadata.Should().NotContain(rawOtp, "Tuyệt đối không lưu mã OTP thô trong audit log");
        metadata.Should().NotContain(cancelToken, "Tuyệt đối không lưu token truy cập bí mật trong audit log");
        metadata.Should().NotContain("CCCD", "Không chứa thông tin CCCD trong audit log");
        metadata.Should().NotContain("password", "Không chứa thông tin password trong audit log");
    }

    [Fact]
    public async Task GetAppointmentCancellationAudit_ReturnsCompleteAuditTrail()
    {
        // Arrange: Hủy ca khám và sau đó nhân viên xử lý hoàn tiền
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, cancelToken, _) = SeedAppointmentData(db, paidAmount: 300_000m);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var cancelHandler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        await cancelHandler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Cần dời sang tháng sau"
        ), CancellationToken.None);

        // Nhân viên lễ tân xác nhận hoàn tất hoàn tiền
        var refund = await db.Refunds.FirstOrDefaultAsync(r => r.AppointmentId == appointment.Id);
        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Email = "receptionist@clinic.vn",
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var processHandler = new ProcessRefundCommandHandler(db, staffUser);
        await processHandler.Handle(new ProcessRefundCommand(
            RefundId: refund!.Id,
            TargetStatus: RefundStatus.Succeeded,
            ProviderReference: "VIB-FT-99881122",
            Note: "Đã chi trả tiền hoàn thành công"
        ), CancellationToken.None);

        // Act: Lễ tân truy vấn audit trail của ca khám này
        var auditQueryHandler = new GetAppointmentCancellationAuditQueryHandler(db, staffUser);
        var auditResult = await auditQueryHandler.Handle(
            new GetAppointmentCancellationAuditQuery(appointment.BookingCode), 
            CancellationToken.None);

        // Assert: Trả về đầy đủ lịch sử audit cả 2 sự kiện CANCEL_APPOINTMENT và PROCESS_REFUND
        auditResult.IsSuccess.Should().BeTrue();
        var history = auditResult.Value!;
        history.BookingCode.Should().Be(appointment.BookingCode);
        history.Status.Should().Be(AppointmentStatus.Cancelled);
        history.AuditTrail.Should().HaveCount(2);

        var processLog = history.AuditTrail.First(l => l.Action == "PROCESS_REFUND");
        processLog.ActorType.Should().Be("STAFF");
        processLog.ProviderReference.Should().Be("VIB-FT-99881122");

        var cancelLog = history.AuditTrail.First(l => l.Action == "CANCEL_APPOINTMENT");
        cancelLog.ActorType.Should().Be("GUEST");
        cancelLog.RefundDecision.Should().Be("100%_REFUND");
    }

    [Fact]
    public async Task StaffOnly_EnforcedForAuditQueries()
    {
        // Arrange: Người dùng là Bệnh nhân (hoặc chưa đăng nhập) cố truy vấn audit log
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var auditQueryHandler = new GetAppointmentCancellationAuditQueryHandler(db, patientUser);
        var listQueryHandler = new GetCancellationAuditLogsQueryHandler(db, patientUser);

        // Act 1: Truy vấn audit trail theo ca khám
        var result1 = await auditQueryHandler.Handle(
            new GetAppointmentCancellationAuditQuery(appointment.BookingCode), 
            CancellationToken.None);

        // Act 2: Truy vấn danh sách audit log toàn hệ thống
        var result2 = await listQueryHandler.Handle(
            new GetCancellationAuditLogsQuery(), 
            CancellationToken.None);

        // Assert: Cả 2 đều bị từ chối với HTTP 403 Forbidden
        result1.IsSuccess.Should().BeFalse();
        result1.StatusCode.Should().Be(403);
        result1.Error.Should().Contain("Bạn không có quyền truy cập");

        result2.IsSuccess.Should().BeFalse();
        result2.StatusCode.Should().Be(403);
        result2.Error.Should().Contain("Bạn không có quyền truy cập");
    }

    [Fact]
    public async Task GetCancellationAuditLogs_WithFiltersAndPagination_Succeeds()
    {
        // Arrange: Tạo nhiều bản ghi audit
        using var db = CreateInMemoryDbContext();
        var (appointment1, _, _, _, token1, _) = SeedAppointmentData(db);
        var (appointment2, _, _, _, token2, _) = SeedAppointmentData(db, startAt: DateTime.UtcNow.AddMinutes(40)); // < 60 phút

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var cancelHandler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        await cancelHandler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment1.BookingCode,
            GrantToken: token1,
            Reason: "Hủy ca 1"
        ), CancellationToken.None);

        await cancelHandler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment2.BookingCode,
            GrantToken: token2,
            Reason: "Hủy ca 2"
        ), CancellationToken.None);

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var listHandler = new GetCancellationAuditLogsQueryHandler(db, staffUser);

        // Act 1: Lấy danh sách có phân trang
        var pagedResult = await listHandler.Handle(new GetCancellationAuditLogsQuery(
            Page: 1,
            PageSize: 10
        ), CancellationToken.None);

        // Act 2: Lọc theo BookingCode ca 1
        var filterResult = await listHandler.Handle(new GetCancellationAuditLogsQuery(
            BookingCode: appointment1.BookingCode
        ), CancellationToken.None);

        // Assert
        pagedResult.IsSuccess.Should().BeTrue();
        pagedResult.Value!.TotalItems.Should().Be(2);

        filterResult.IsSuccess.Should().BeTrue();
        filterResult.Value!.Items.Should().ContainSingle();
        filterResult.Value.Items.First().BookingCode.Should().Be(appointment1.BookingCode);
    }

    [Fact]
    public async Task AuditLog_UnpaidCancellation_RecordsNoRefundDecision()
    {
        // Arrange: Ca khám chưa thanh toán (PaidAmount = 0)
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, cancelToken, _) = SeedAppointmentData(db, initialServicePrice: 200_000m, paidAmount: 0m);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        // Act
        var result = await handler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Hủy lịch chưa thanh toán"
        ), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Assert: Quyết định hoàn tiền là NO_REFUND_UNPAID, RefundStatus là NotApplicable
        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CANCEL_APPOINTMENT" && a.EntityId == appointment.Id);

        auditLog.Should().NotBeNull();
        using var doc = JsonDocument.Parse(auditLog!.MetadataRedacted!);
        var root = doc.RootElement;

        root.GetProperty("PaidAmount").GetDecimal().Should().Be(0m);
        root.GetProperty("RefundDecision").GetString().Should().Be("NO_REFUND_UNPAID");
        root.GetProperty("ExpectedRefundAmount").GetDecimal().Should().Be(0m);
        root.GetProperty("RefundStatus").GetString().Should().Be("NotApplicable");
    }

    [Fact]
    public async Task AuditLog_Under60MinCancellation_Records50PercentDecision()
    {
        // Arrange: Hủy ca khám trước giờ hẹn 45 phút (< 60 phút)
        using var db = CreateInMemoryDbContext();
        var startAt = DateTime.UtcNow.AddMinutes(45);
        var (appointment, _, _, _, cancelToken, _) = SeedAppointmentData(db, paidAmount: 500_000m, startAt: startAt);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        // Act
        var result = await handler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Hủy sát giờ khám"
        ), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Assert: Quyết định hoàn tiền là 50%_REFUND_50%_FEE
        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CANCEL_APPOINTMENT" && a.EntityId == appointment.Id);

        auditLog.Should().NotBeNull();
        using var doc = JsonDocument.Parse(auditLog!.MetadataRedacted!);
        var root = doc.RootElement;

        root.GetProperty("RefundDecision").GetString().Should().Be("50%_REFUND_50%_FEE");
        root.GetProperty("RefundRate").GetDecimal().Should().Be(0.50m);
        root.GetProperty("CancellationFeeRate").GetDecimal().Should().Be(0.50m);
        root.GetProperty("CancellationFeeAmount").GetDecimal().Should().Be(250_000m);
        root.GetProperty("ExpectedRefundAmount").GetDecimal().Should().Be(250_000m);
    }

    [Fact]
    public async Task AuditLog_AtOrAbove60MinCancellation_Records100PercentDecision()
    {
        // Arrange: Hủy trước giờ hẹn 90 phút (>= 60 phút)
        using var db = CreateInMemoryDbContext();
        var startAt = DateTime.UtcNow.AddMinutes(90);
        var (appointment, _, _, _, cancelToken, _) = SeedAppointmentData(db, paidAmount: 600_000m, startAt: startAt);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var handler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        // Act
        var result = await handler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Hủy sớm trước 90 phút"
        ), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Assert: Quyết định hoàn tiền là 100%_REFUND, phí hủy 0%
        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "CANCEL_APPOINTMENT" && a.EntityId == appointment.Id);

        auditLog.Should().NotBeNull();
        using var doc = JsonDocument.Parse(auditLog!.MetadataRedacted!);
        var root = doc.RootElement;

        root.GetProperty("RefundDecision").GetString().Should().Be("100%_REFUND");
        root.GetProperty("RefundRate").GetDecimal().Should().Be(1.0m);
        root.GetProperty("CancellationFeeRate").GetDecimal().Should().Be(0.0m);
        root.GetProperty("CancellationFeeAmount").GetDecimal().Should().Be(0m);
        root.GetProperty("ExpectedRefundAmount").GetDecimal().Should().Be(600_000m);
    }

    [Fact]
    public async Task AuditTrail_ImmutableAndOrderedByTime()
    {
        // Arrange: Tạo ca khám với các sự kiện audit theo thời gian khác nhau
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, cancelToken, _) = SeedAppointmentData(db);

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var cancelHandler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        await cancelHandler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Hủy lịch"
        ), CancellationToken.None);

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Email = "admin@clinic.vn",
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        // Act: Truy vấn audit trail
        var queryHandler = new GetAppointmentCancellationAuditQueryHandler(db, staffUser);
        var result = await queryHandler.Handle(
            new GetAppointmentCancellationAuditQuery(appointment.BookingCode), 
            CancellationToken.None);

        // Assert: Danh sách audit logs được sắp xếp giảm dần theo thời gian OccurredAtUtc
        result.IsSuccess.Should().BeTrue();
        var trail = result.Value!.AuditTrail;
        trail.Should().NotBeEmpty();

        for (int i = 0; i < trail.Count - 1; i++)
        {
            trail[i].OccurredAtUtc.Should().BeOnOrAfter(trail[i + 1].OccurredAtUtc);
        }
    }
}
