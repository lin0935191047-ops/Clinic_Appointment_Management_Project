using ClinicAppointment.Domain.Common;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Entities;

public class RescheduleRequest : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid RequestedDoctorId { get; set; }
    public DateTime ProposedStartAt { get; set; }
    public DateTime ProposedEndAt { get; set; }
    public string? Reason { get; set; }
    public RescheduleStatus Status { get; set; } = RescheduleStatus.PendingReview;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNotes { get; set; }

    public virtual Appointment? Appointment { get; set; }
}

public class Refund : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public decimal PaidServiceAmount { get; set; }
    public decimal CancellationFeeRate { get; set; }
    public decimal CancellationFeeAmount { get; set; }
    public decimal ExpectedRefundAmount { get; set; }
    public RefundStatus Status { get; set; } = RefundStatus.Pending;
    public string? Reason { get; set; }
    public DateTime? ProcessedAt { get; set; }

    public virtual Appointment? Appointment { get; set; }
}

public class OtpChallenge : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid PatientId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public OtpPurpose Purpose { get; set; }
    public string OtpHash { get; set; } = string.Empty;
    public short AttemptsCount { get; set; } = 0;
    public short MaxAttempts { get; set; } = 5;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; } = false;

    public bool IsValid(string inputOtpHash, DateTime currentUtc)
    {
        if (IsUsed) return false;
        if (currentUtc > ExpiresAt) return false;
        if (AttemptsCount >= MaxAttempts) return false;
        return OtpHash == inputOtpHash;
    }
}

