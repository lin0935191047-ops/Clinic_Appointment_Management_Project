using ClinicAppointment.Domain.Common;

namespace ClinicAppointment.Domain.Entities;

public class AuditLog : BaseEntity
{
    public string ActorType { get; set; } = string.Empty; // GUEST, PATIENT, DOCTOR, RECEPTIONIST, ADMIN, SYSTEM
    public Guid? ActorUserId { get; set; }
    public string Action { get; set; } = string.Empty;     // e.g. "CONFIRM_APPOINTMENT", "RESCHEDULE_APPOINTMENT"
    public string EntityType { get; set; } = string.Empty; // e.g. "Appointment"
    public Guid? EntityId { get; set; }
    public string? CorrelationId { get; set; }
    public string? MetadataRedacted { get; set; }          // JSON string chứa thông tin audit đã redaction (không PII)
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual User? ActorUser { get; set; }

    public static AuditLog Create(
        string actorType,
        Guid? actorUserId,
        string action,
        string entityType,
        Guid? entityId,
        string? metadataRedacted = null,
        string? correlationId = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorType = actorType,
            ActorUserId = actorUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            MetadataRedacted = metadataRedacted,
            CorrelationId = correlationId,
            OccurredAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }
}
