using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Common.Notifications;

public record NotificationMessage(
    string Recipient,
    NotificationChannel Channel,
    string TemplateCode,
    string Content,
    string? Subject = null,
    IDictionary<string, object>? Payload = null,
    Guid? AppointmentId = null,
    Guid? PatientId = null
);

public class NotificationSendResult
{
    public bool IsSuccess { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? SentAt { get; private set; }

    private NotificationSendResult() { }

    public static NotificationSendResult Success(string providerMessageId, DateTime? sentAtUtc = null)
    {
        return new NotificationSendResult
        {
            IsSuccess = true,
            ProviderMessageId = providerMessageId,
            SentAt = sentAtUtc ?? DateTime.UtcNow
        };
    }

    public static NotificationSendResult Failure(string errorMessage)
    {
        return new NotificationSendResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage
        };
    }
}
