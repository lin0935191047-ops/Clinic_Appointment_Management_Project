namespace ClinicAppointment.Domain.Enums;

public enum AppointmentStatus
{
    PendingConfirmation,
    Confirmed,
    CheckedIn,
    InProgress,
    Completed,
    Cancelled,
    NoShow
}

public enum RescheduleStatus
{
    PendingReview,
    Approved,
    Rejected,
    Cancelled,
    Expired
}

public enum RefundStatus
{
    NotApplicable,
    Pending,
    Processing,
    Succeeded,
    Failed
}

public enum OtpPurpose
{
    LOOKUP,
    RESCHEDULE,
    CANCEL
}

public enum UserRole
{
    Admin,
    Doctor,
    Receptionist,
    Patient
}

