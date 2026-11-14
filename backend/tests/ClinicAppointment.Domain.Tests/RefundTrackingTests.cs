using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Features.Appointments.Commands.CancelAppointment;
using ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment;
using ClinicAppointment.Application.Features.Appointments.Services;
using ClinicAppointment.Application.Features.Otp.Services;
using ClinicAppointment.Application.Features.Refunds.Commands.ProcessRefund;
using ClinicAppointment.Application.Features.Refunds.Queries.GetRefundById;
using ClinicAppointment.Application.Features.Refunds.Queries.GetRefundsList;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class RefundTrackingTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private (Appointment Appointment, Patient Patient, Doctor Doctor, Service Service, string CancelGrantToken, string LookupGrantToken) SeedAppointmentData(
        ApplicationDbContext db,
        decimal initialServicePrice = 300_000m,
        decimal paidAmount = 300_000m,
        DateTime? startAt = null)
    {
        var patient = Patient.Create("Trần Minh Quân", "0988112233", new DateOnly(1989, 8, 15), 1989);
        db.Patients.Add(patient);

        var doctorUser = User.Create("doctor.refund@clinic.vn", "hash");
        var spec = Specialty.Create("CK-TIM", "Tim Mạch", "tim-mach");
        db.Users.Add(doctorUser);
        db.Specialties.Add(spec);

        var doctor = Doctor.Create(doctorUser.Id, spec.Id, "DOC-CARD-02", "BS. Trần Tim Mạch");
        db.Doctors.Add(doctor);

        var service = Service.Create(spec.Id, "DV-TIM-01", "Siêu âm doppler tim", 30, initialServicePrice);
        db.Services.Add(service);

        var appointmentStart = startAt ?? DateTime.UtcNow.AddHours(2); // >= 60 phút
        var appointment = Appointment.CreateGuest(
            doctor.Id,
            patient.Id,
            service.Id,
            service.Name,
            service.DurationMinutes,
            service.Price,
            appointmentStart,
            "Khám kiểm tra định kỳ huyết áp tim mạch");
        appointment.PaidAmount = paidAmount;
        db.Appointments.Add(appointment);

        // Cancel Grant Token
        var cancelChallenge = OtpChallenge.Create(appointment.Id, patient.Id, patient.PhoneNumber, OtpPurpose.CANCEL, "123456");
        cancelChallenge.Verify("123456", DateTime.UtcNow, out _);
        db.OtpChallenges.Add(cancelChallenge);
        var (cancelGrant, cancelRawToken) = OtpVerificationGrant.Issue(cancelChallenge.Id, appointment.Id, OtpPurpose.CANCEL, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(cancelGrant);

        // Lookup Grant Token
        var lookupChallenge = OtpChallenge.Create(appointment.Id, patient.Id, patient.PhoneNumber, OtpPurpose.LOOKUP, "654321");
        lookupChallenge.Verify("654321", DateTime.UtcNow, out _);
        db.OtpChallenges.Add(lookupChallenge);
        var (lookupGrant, lookupRawToken) = OtpVerificationGrant.Issue(lookupChallenge.Id, appointment.Id, OtpPurpose.LOOKUP, TimeSpan.FromMinutes(15));
        db.OtpVerificationGrants.Add(lookupGrant);

        db.SaveChanges();

        return (appointment, patient, doctor, service, cancelRawToken, lookupRawToken);
    }

    [Fact]
    public async Task FeeSnapshot_UsedForRefundCalculationAndRecord()
    {
        // Arrange: Tạo lịch hẹn với giá dịch vụ snapshot 300.000 đ
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, service, cancelToken, _) = SeedAppointmentData(db, initialServicePrice: 300_000m, paidAmount: 300_000m);

        // Sau khi đặt lịch, quản trị viên tăng giá dịch vụ trong danh mục catalog lên 500.000 đ
        service.Update(service.SpecialtyId, "Siêu âm doppler tim", 30, 500_000m);
        await db.SaveChangesAsync();

        // Act: Bệnh nhân thực hiện hủy lịch hẹn trước 60 phút (hoàn 100%)
        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var policyService = new CancellationPolicyService();
        var cancelHandler = new CancelAppointmentCommandHandler(db, grantService, policyService);

        var cancelResult = await cancelHandler.Handle(new CancelAppointmentCommand(
            BookingCode: appointment.BookingCode,
            GrantToken: cancelToken,
            Reason: "Có việc đột xuất"
        ), CancellationToken.None);

        // Assert: Tính toán hoàn tiền phải căn cứ trên snapshot lúc đặt (300.000 đ), KHÔNG bị ảnh hưởng bởi giá mới (500.000 đ)
        cancelResult.IsSuccess.Should().BeTrue(cancelResult.Error);
        cancelResult.Value!.PaidAmount.Should().Be(300_000m);
        cancelResult.Value.ExpectedRefundAmount.Should().Be(300_000m);
        cancelResult.Value.CancellationFeeAmount.Should().Be(0m);

        var refund = await db.Refunds.FirstOrDefaultAsync(r => r.AppointmentId == appointment.Id);
        refund.Should().NotBeNull();
        refund!.PaidServiceAmount.Should().Be(300_000m, "Phải sử dụng giá snapshot lúc đặt khám");
        refund.ExpectedRefundAmount.Should().Be(300_000m);
        refund.CancellationFeeAmount.Should().Be(0m);
    }

    [Fact]
    public async Task PolicyVersion_AndRatesAndAmounts_StoredAccurately()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var appointmentId = Guid.NewGuid();

        // Act: Tạo bản ghi Refund với phiên bản chính sách v1.0
        var refund = Refund.Create(
            appointmentId: appointmentId,
            paidServiceAmount: 250_000m,
            cancellationFeeRate: 0.50m,
            cancellationFeeAmount: 125_000m,
            expectedRefundAmount: 125_000m,
            reason: "Hủy dưới 60 phút trước giờ hẹn",
            status: RefundStatus.Pending,
            policyVersion: "v1.0"
        );
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        // Assert
        var savedRefund = await db.Refunds.FirstOrDefaultAsync(r => r.Id == refund.Id);
        savedRefund.Should().NotBeNull();
        savedRefund!.PolicyVersion.Should().Be("v1.0");
        savedRefund.CancellationFeeRate.Should().Be(0.50m);
        savedRefund.CancellationFeeAmount.Should().Be(125_000m);
        savedRefund.ExpectedRefundAmount.Should().Be(125_000m);
        savedRefund.PaidServiceAmount.Should().Be(250_000m);
        savedRefund.Status.Should().Be(RefundStatus.Pending);
    }

    [Fact]
    public async Task PendingOrProcessing_NotShownAsSucceeded()
    {
        // Arrange: Tạo 2 bản ghi hoàn tiền (1 Pending, 1 Processing)
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var pendingRefund = Refund.Create(appointment.Id, 200_000m, 0m, 0m, 200_000m, "Chờ duyệt", RefundStatus.Pending, "v1.0");
        var staffId = Guid.NewGuid();
        var processingRefund = Refund.Create(appointment.Id, 300_000m, 0.5m, 150_000m, 150_000m, "Đang đối soát", RefundStatus.Pending, "v1.0");
        processingRefund.Process(staffId, "UNC-12345");

        db.Refunds.AddRange(pendingRefund, processingRefund);
        await db.SaveChangesAsync();

        // Act: Truy vấn danh sách qua GetRefundsListQuery
        var handler = new GetRefundsListQueryHandler(db);
        var result = await handler.Handle(new GetRefundsListQuery(), CancellationToken.None);

        // Assert: Cả Pending và Processing đều phải có IsCompleted == false và thông báo trạng thái rõ ràng
        result.IsSuccess.Should().BeTrue();
        var items = result.Value!.Items;

        var pendingDto = items.First(r => r.Id == pendingRefund.Id);
        pendingDto.IsCompleted.Should().BeFalse("Pending không được thể hiện là đã hoàn tất");
        pendingDto.Status.Should().Be(RefundStatus.Pending);
        pendingDto.StatusDescription.Should().Contain("Pending");

        var processingDto = items.First(r => r.Id == processingRefund.Id);
        processingDto.IsCompleted.Should().BeFalse("Processing không được thể hiện là đã hoàn tất");
        processingDto.Status.Should().Be(RefundStatus.Processing);
        processingDto.StatusDescription.Should().Contain("Processing");
    }

    [Fact]
    public async Task NoFakeGateway_ManualStaffProcessingWorkflow()
    {
        // Arrange: Luồng hoàn tiền được thực hiện bởi nhân viên Lễ tân/Kế toán, không phụ thuộc fake gateway
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var refund = Refund.Create(appointment.Id, 400_000m, 0m, 0m, 400_000m, "Hủy lịch trước 24h", RefundStatus.Pending, "v1.0");
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ProcessRefundCommandHandler(db, staffUser);

        // Act Bước 1: Nhân viên chuyển sang Processing kèm mã tham chiếu chuyển khoản nội bộ
        var step1 = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Processing,
            ProviderReference: "UNC-VCB-20261114-0012",
            Note: "Đã chuyển phiếu chi sang phòng tài chính kế toán"
        ), CancellationToken.None);

        step1.IsSuccess.Should().BeTrue();
        step1.Value!.Status.Should().Be(RefundStatus.Processing);
        step1.Value.IsCompleted.Should().BeFalse();

        // Act Bước 2: Kế toán đối soát thành công và xác nhận hoàn tiền Succeeded
        var step2 = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Succeeded,
            ProviderReference: "FT261114998877",
            Note: "Đã chuyển khoản thành công từ tài khoản ngân hàng phòng khám"
        ), CancellationToken.None);

        // Assert: Trạng thái cập nhật Succeeded, ghi nhận thời gian và mã giao dịch ngân hàng thực tế
        step2.IsSuccess.Should().BeTrue();
        step2.Value!.Status.Should().Be(RefundStatus.Succeeded);
        step2.Value.IsCompleted.Should().BeTrue();
        step2.Value.ProviderReference.Should().Be("FT261114998877");
        step2.Value.ProcessedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task StaffOnly_AuthorizationEnforced()
    {
        // Arrange: Người dùng có vai trò Bệnh nhân (hoặc chưa đăng nhập) cố gắng xử lý hoàn tiền
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var refund = Refund.Create(appointment.Id, 200_000m, 0m, 0m, 200_000m, null, RefundStatus.Pending, "v1.0");
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var patientUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.PatientCode },
            IsAuthenticated = true
        };

        var handler = new ProcessRefundCommandHandler(db, patientUser);

        // Act
        var result = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Succeeded
        ), CancellationToken.None);

        // Assert: Bị từ chối HTTP 403 Forbidden
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(403);
        result.Error.Should().Contain("Bạn không có quyền xử lý hoàn tiền");
    }

    [Fact]
    public async Task ProcessRefund_PendingToProcessing_Succeeds()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var refund = Refund.Create(appointment.Id, 180_000m, 0m, 0m, 180_000m, null, RefundStatus.Pending, "v1.0");
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var handler = new ProcessRefundCommandHandler(db, staffUser);

        // Act
        var result = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Processing,
            ProviderReference: "REF-001"
        ), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(RefundStatus.Processing);
        result.Value.ProcessedByUserId.Should().Be(staffUser.UserId);
        result.Value.ProviderReference.Should().Be("REF-001");
    }

    [Fact]
    public async Task ProcessRefund_ToSucceeded_SetsProcessedAtAndAuditLog()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var refund = Refund.Create(appointment.Id, 250_000m, 0m, 0m, 250_000m, null, RefundStatus.Pending, "v1.0");
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ProcessRefundCommandHandler(db, staffUser);

        // Act
        var result = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Succeeded,
            ProviderReference: "BANK-TX-998811",
            Note: "Đã chuyển trả tài khoản nguồn thành công"
        ), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(RefundStatus.Succeeded);
        result.Value.IsCompleted.Should().BeTrue();
        result.Value.ProcessedAtUtc.Should().NotBeNull();

        // Kiểm tra AuditLog
        var auditLog = await db.AuditLogs
            .FirstOrDefaultAsync(a => a.Action == "PROCESS_REFUND" && a.EntityId == refund.Id);
        auditLog.Should().NotBeNull();
        auditLog!.ActorType.Should().Be("STAFF");
        auditLog.ActorUserId.Should().Be(staffUser.UserId);
        auditLog.MetadataRedacted.Should().Contain("Succeeded");
        auditLog.MetadataRedacted.Should().Contain("BANK-TX-998811");
    }

    [Fact]
    public async Task ProcessRefund_AlreadySucceeded_CannotBeModified()
    {
        // Arrange: Khoản hoàn tiền đã Succeeded
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var staffUserId = Guid.NewGuid();
        var refund = Refund.Create(appointment.Id, 200_000m, 0m, 0m, 200_000m, null, RefundStatus.Pending, "v1.0");
        refund.MarkSucceeded(staffUserId, "ORIGINAL-REF-123");
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var staffUser = new TestCurrentUserService
        {
            UserId = staffUserId,
            Roles = new List<string> { Role.AdminCode },
            IsAuthenticated = true
        };

        var handler = new ProcessRefundCommandHandler(db, staffUser);

        // Act: Cố gắng sửa đổi khoản hoàn tiền đã Succeeded
        var result = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Processing
        ), CancellationToken.None);

        // Assert: Bị từ chối với HTTP 400 Bad Request
        result.IsSuccess.Should().BeFalse();
        result.StatusCode.Should().Be(400);
        result.Error.Should().Contain("không thể chỉnh sửa trạng thái");
    }

    [Fact]
    public async Task ProcessRefund_ToFailed_RequiresFailureReason()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, _) = SeedAppointmentData(db);

        var refund = Refund.Create(appointment.Id, 300_000m, 0m, 0m, 300_000m, null, RefundStatus.Pending, "v1.0");
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var staffUser = new TestCurrentUserService
        {
            UserId = Guid.NewGuid(),
            Roles = new List<string> { Role.ReceptionistCode },
            IsAuthenticated = true
        };

        var handler = new ProcessRefundCommandHandler(db, staffUser);

        // Act 1: Chuyển sang Failed nhưng không có failureReason
        var emptyReasonResult = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Failed,
            FailureReason: ""
        ), CancellationToken.None);

        emptyReasonResult.IsSuccess.Should().BeFalse();
        emptyReasonResult.StatusCode.Should().Be(400);
        emptyReasonResult.Error.Should().Contain("Lý do hoàn tiền thất bại là bắt buộc");

        // Act 2: Chuyển sang Failed kèm lý do rõ ràng
        var validResult = await handler.Handle(new ProcessRefundCommand(
            RefundId: refund.Id,
            TargetStatus: RefundStatus.Failed,
            FailureReason: "Số tài khoản nhận tiền của bệnh nhân không tồn tại tại ngân hàng đích"
        ), CancellationToken.None);

        validResult.IsSuccess.Should().BeTrue();
        validResult.Value!.Status.Should().Be(RefundStatus.Failed);
        validResult.Value.FailureReason.Should().Be("Số tài khoản nhận tiền của bệnh nhân không tồn tại tại ngân hàng đích");
    }

    [Fact]
    public async Task LookupAppointment_CancelledWithRefund_ReturnsSafeRefundSummaryWithAccurateStatus()
    {
        // Arrange: Ca khám bị hủy có khoản hoàn tiền đang Pending
        using var db = CreateInMemoryDbContext();
        var (appointment, _, _, _, _, lookupToken) = SeedAppointmentData(db, initialServicePrice: 350_000m, paidAmount: 350_000m);

        appointment.Cancel("Bệnh nhân có lịch công tác", DateTime.UtcNow);
        var refund = Refund.Create(
            appointmentId: appointment.Id,
            paidServiceAmount: 350_000m,
            cancellationFeeRate: 0m,
            cancellationFeeAmount: 0m,
            expectedRefundAmount: 350_000m,
            reason: "Hủy trước 60 phút",
            status: RefundStatus.Pending,
            policyVersion: "v1.0"
        );
        db.Refunds.Add(refund);
        await db.SaveChangesAsync();

        var grantService = new OtpGrantService(db, NullLogger<OtpGrantService>.Instance);
        var lookupHandler = new LookupAppointmentQueryHandler(db, grantService);

        // Act: Bệnh nhân tra cứu lịch khám đã hủy
        var lookupResult = await lookupHandler.Handle(new LookupAppointmentQuery(
            appointment.BookingCode,
            lookupToken
        ), CancellationToken.None);

        // Assert: Trả về PublicRefundSummaryDto an toàn và phản ánh đúng trạng thái Pending
        lookupResult.IsSuccess.Should().BeTrue();
        var summary = lookupResult.Value!;
        summary.Status.Should().Be(AppointmentStatus.Cancelled);
        summary.Refund.Should().NotBeNull();
        summary.Refund!.ExpectedRefundAmount.Should().Be(350_000m);
        summary.Refund.CancellationFeeAmount.Should().Be(0m);
        summary.Refund.PolicyVersion.Should().Be("v1.0");
        summary.Refund.Status.Should().Be(RefundStatus.Pending);
        summary.Refund.IsCompleted.Should().BeFalse("Tuyệt đối không hiển thị đã hoàn tất khi trạng thái là Pending");
        summary.Refund.StatusDescription.Should().Contain("Pending");
    }
}
