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

public enum IdentityMatchReason
{
    NAME_DOB,
    NAME_BIRTH_YEAR,
    VERIFIED_IDENTIFIER,
    VERIFIED_PERSONAL_PHONE,
    SHARED_PHONE_DIFFERENT_NAME,
    CONFLICT
}

public enum IdentityReviewStatus
{
    OPEN,
    CONFIRMED_SAME_PERSON,
    DIFFERENT_PERSON,
    NEEDS_MORE_INFO
}

public enum TimeOffStatus
{
    Active,
    Cancelled
}

public enum ScheduleBlockType
{
    Break,
    Meeting,
    Maintenance,
    Other
}

public enum NotificationChannel
{
    Sms,
    Email,
    InApp
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}

