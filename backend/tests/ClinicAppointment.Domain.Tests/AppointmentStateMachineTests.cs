using System;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

/// <summary>
/// FEAT-049: Kiểm thử máy trạng thái ca khám (State transitions)
/// Tuân thủ quy tắc bất biến trong docs/SPEC.md v1.2, docs/BUSINESS_RULES.md và AGENTS.md:
/// - Chu trình vòng đời chuẩn: PendingConfirmation -> Confirmed -> CheckedIn -> InProgress -> Completed
/// - Nhánh hủy lịch: PendingConfirmation/Confirmed/CheckedIn -> Cancelled (trước giờ hẹn)
/// - Terminal states: Completed và Cancelled là trạng thái kết thúc bất biến, không thể đảo ngược
/// </summary>
public class AppointmentStateMachineTests
{
    private static Appointment CreateTestAppointment(AppointmentStatus initialStatus = AppointmentStatus.PendingConfirmation)
    {
        var apt = Appointment.CreateGuest(
            doctorId: Guid.NewGuid(),
            patientId: Guid.NewGuid(),
            serviceId: Guid.NewGuid(),
            serviceName: "Khám Tổng Quát",
            serviceDuration: 30,
            servicePrice: 250_000m,
            startAtUtc: DateTime.UtcNow.AddHours(2),
            reasonForVisit: "Kiểm tra sức khỏe định kỳ");

        apt.Status = initialStatus;
        return apt;
    }

    #region 1. Chu Trình Chuyển Đổi Trạng Thái Hợp Lệ (Happy Path Transitions)

    [Fact]
    public void FullLifecycle_FromPendingToCompleted_ShouldTransitionSmoothly()
    {
        // 1. Khởi tạo: PendingConfirmation
        var apt = CreateTestAppointment(AppointmentStatus.PendingConfirmation);
        apt.Status.Should().Be(AppointmentStatus.PendingConfirmation);

        // 2. Lễ tân xác nhận lịch -> Confirmed
        apt.Confirm();
        apt.Status.Should().Be(AppointmentStatus.Confirmed);
        apt.UpdatedAt.Should().NotBeNull();

        // 3. Bệnh nhân có mặt tại quầy, Lễ tân check-in -> CheckedIn
        apt.CheckIn();
        apt.Status.Should().Be(AppointmentStatus.CheckedIn);

        // 4. Bác sĩ gọi bệnh nhân vào phòng, bấm bắt đầu khám -> InProgress
        apt.StartConsultation();
        apt.Status.Should().Be(AppointmentStatus.InProgress);

        // 5. Bác sĩ hoàn tất khám và lưu chẩn đoán -> Completed
        apt.CompleteConsultation();
        apt.Status.Should().Be(AppointmentStatus.Completed);
    }

    [Fact]
    public void Cancellation_FromPendingConfirmation_ShouldTransitionToCancelled()
    {
        var apt = CreateTestAppointment(AppointmentStatus.PendingConfirmation);
        var cancelTime = DateTime.UtcNow;

        apt.Cancel("Bận đột xuất", cancelTime);

        apt.Status.Should().Be(AppointmentStatus.Cancelled);
        apt.CancellationReason.Should().Be("Bận đột xuất");
        apt.CancellationAcceptedAt.Should().Be(cancelTime);
    }

    [Fact]
    public void Cancellation_FromConfirmed_ShouldTransitionToCancelled()
    {
        var apt = CreateTestAppointment(AppointmentStatus.Confirmed);
        var cancelTime = DateTime.UtcNow;

        apt.Cancel("Đổi kế hoạch", cancelTime);

        apt.Status.Should().Be(AppointmentStatus.Cancelled);
        apt.CancellationReason.Should().Be("Đổi kế hoạch");
    }

    #endregion

    #region 2. Các Bước Chuyển Đổi Bất Hợp Lệ Từ Trạng Thái PendingConfirmation

    [Fact]
    public void PendingConfirmation_CannotCheckInDirectly_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.PendingConfirmation);

        var act = () => apt.CheckIn();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể Check-in cho lịch hẹn ở trạng thái PendingConfirmation*");
    }

    [Fact]
    public void PendingConfirmation_CannotStartConsultation_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.PendingConfirmation);

        var act = () => apt.StartConsultation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể bắt đầu ca khám cho lịch hẹn ở trạng thái PendingConfirmation*");
    }

    [Fact]
    public void PendingConfirmation_CannotCompleteConsultation_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.PendingConfirmation);

        var act = () => apt.CompleteConsultation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể hoàn thành ca khám cho lịch hẹn ở trạng thái PendingConfirmation*");
    }

    #endregion

    #region 3. Các Bước Chuyển Đổi Bất Hợp Lệ Từ Trạng Thái Confirmed

    [Fact]
    public void Confirmed_CannotConfirmAgain_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.Confirmed);

        var act = () => apt.Confirm();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể xác nhận lịch hẹn ở trạng thái Confirmed*");
    }

    [Fact]
    public void Confirmed_CannotStartConsultationWithoutCheckIn_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.Confirmed);

        var act = () => apt.StartConsultation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Chỉ có thể bắt đầu khi bệnh nhân đã được tiếp nhận (CheckedIn)*");
    }

    [Fact]
    public void Confirmed_CannotCompleteConsultation_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.Confirmed);

        var act = () => apt.CompleteConsultation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể hoàn thành ca khám cho lịch hẹn ở trạng thái Confirmed*");
    }

    #endregion

    #region 4. Các Bước Chuyển Đổi Bất Hợp Lệ Từ Trạng Thái CheckedIn

    [Fact]
    public void CheckedIn_CannotConfirm_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.CheckedIn);

        var act = () => apt.Confirm();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể xác nhận lịch hẹn ở trạng thái CheckedIn*");
    }

    [Fact]
    public void CheckedIn_CannotCheckInAgain_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.CheckedIn);

        var act = () => apt.CheckIn();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể Check-in cho lịch hẹn ở trạng thái CheckedIn*");
    }

    [Fact]
    public void CheckedIn_CannotCompleteConsultationDirectly_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.CheckedIn);

        var act = () => apt.CompleteConsultation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể hoàn thành ca khám cho lịch hẹn ở trạng thái CheckedIn*");
    }

    #endregion

    #region 5. Các Bước Chuyển Đổi Bất Hợp Lệ Từ Trạng Thái InProgress

    [Fact]
    public void InProgress_CannotConfirm_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.InProgress);

        var act = () => apt.Confirm();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể xác nhận lịch hẹn ở trạng thái InProgress*");
    }

    [Fact]
    public void InProgress_CannotCheckIn_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.InProgress);

        var act = () => apt.CheckIn();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể Check-in cho lịch hẹn ở trạng thái InProgress*");
    }

    [Fact]
    public void InProgress_CannotStartConsultationAgain_ThrowsInvalidOperationException()
    {
        var apt = CreateTestAppointment(AppointmentStatus.InProgress);

        var act = () => apt.StartConsultation();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể bắt đầu ca khám cho lịch hẹn ở trạng thái InProgress*");
    }

    #endregion

    #region 6. Terminal States Bất Biến (Completed & Cancelled)

    [Fact]
    public void Completed_IsTerminalState_CannotTransitionToAnyOtherState()
    {
        var apt = CreateTestAppointment(AppointmentStatus.Completed);

        // 1. Không thể confirm
        var actConfirm = () => apt.Confirm();
        actConfirm.Should().Throw<InvalidOperationException>();

        // 2. Không thể check-in
        var actCheckIn = () => apt.CheckIn();
        actCheckIn.Should().Throw<InvalidOperationException>();

        // 3. Không thể bắt đầu khám lại
        var actStart = () => apt.StartConsultation();
        actStart.Should().Throw<InvalidOperationException>();

        // 4. Không thể hoàn thành lại
        var actComplete = () => apt.CompleteConsultation();
        actComplete.Should().Throw<InvalidOperationException>();

        // 5. Tuyệt đối không thể hủy ca khám đã hoàn tất
        var actCancel = () => apt.Cancel("Muốn hủy", DateTime.UtcNow);
        actCancel.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể hủy lịch hẹn đã ở trạng thái Completed*");
    }

    [Fact]
    public void Cancelled_IsTerminalState_CannotTransitionToAnyOtherState()
    {
        var apt = CreateTestAppointment(AppointmentStatus.Cancelled);

        // 1. Không thể confirm
        var actConfirm = () => apt.Confirm();
        actConfirm.Should().Throw<InvalidOperationException>();

        // 2. Không thể check-in
        var actCheckIn = () => apt.CheckIn();
        actCheckIn.Should().Throw<InvalidOperationException>();

        // 3. Không thể bắt đầu khám
        var actStart = () => apt.StartConsultation();
        actStart.Should().Throw<InvalidOperationException>();

        // 4. Không thể hoàn thành
        var actComplete = () => apt.CompleteConsultation();
        actComplete.Should().Throw<InvalidOperationException>();

        // 5. Không thể hủy lại
        var actCancel = () => apt.Cancel("Hủy lần 2", DateTime.UtcNow);
        actCancel.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể hủy lịch hẹn đã ở trạng thái Cancelled*");
    }

    #endregion

    #region 7. RescheduleRequest Máy Trạng Thái

    [Fact]
    public void RescheduleRequest_Approve_TransitionsPendingReviewToApproved()
    {
        var req = RescheduleRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddMinutes(30),
            "Đổi sang ngày khác");
        var receptionistId = Guid.NewGuid();

        req.Status.Should().Be(RescheduleStatus.PendingReview);

        req.Approve(receptionistId, "Đồng ý chuyển slot");

        req.Status.Should().Be(RescheduleStatus.Approved);
        req.ReviewedByUserId.Should().Be(receptionistId);
        req.ReviewNotes.Should().Be("Đồng ý chuyển slot");
        req.ReviewedAt.Should().NotBeNull();
    }

    [Fact]
    public void RescheduleRequest_Reject_TransitionsPendingReviewToRejected()
    {
        var req = RescheduleRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddMinutes(30));
        var receptionistId = Guid.NewGuid();

        req.Reject(receptionistId, "Bác sĩ bận lịch phẫu thuật đột xuất");

        req.Status.Should().Be(RescheduleStatus.Rejected);
        req.ReviewedByUserId.Should().Be(receptionistId);
        req.ReviewNotes.Should().Be("Bác sĩ bận lịch phẫu thuật đột xuất");
    }

    [Fact]
    public void RescheduleRequest_RejectWithoutNotes_ThrowsArgumentException()
    {
        var req = RescheduleRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddMinutes(30));

        var act = () => req.Reject(Guid.NewGuid(), "   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Lý do từ chối yêu cầu đổi lịch là bắt buộc*");
    }

    [Fact]
    public void RescheduleRequest_CannotApproveAlreadyApprovedRequest_ThrowsInvalidOperationException()
    {
        var req = RescheduleRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddMinutes(30));
        req.Approve(Guid.NewGuid());

        var act = () => req.Approve(Guid.NewGuid());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể duyệt yêu cầu đổi lịch ở trạng thái Approved*");
    }

    [Fact]
    public void RescheduleRequest_CannotRejectAlreadyRejectedRequest_ThrowsInvalidOperationException()
    {
        var req = RescheduleRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow.AddDays(2),
            DateTime.UtcNow.AddDays(2).AddMinutes(30));
        req.Reject(Guid.NewGuid(), "Lý do 1");

        var act = () => req.Reject(Guid.NewGuid(), "Lý do 2");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Không thể từ chối yêu cầu đổi lịch ở trạng thái Rejected*");
    }

    #endregion
}
