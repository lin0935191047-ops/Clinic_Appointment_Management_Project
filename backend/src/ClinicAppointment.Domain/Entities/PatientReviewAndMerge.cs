using ClinicAppointment.Domain.Common;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Entities;

public class PatientIdentityReviewCase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public virtual Patient Patient { get; set; } = null!;

    public Guid CandidatePatientId { get; set; }
    public virtual Patient CandidatePatient { get; set; } = null!;

    public IdentityMatchReason MatchReason { get; set; }
    public IdentityReviewStatus Status { get; set; } = IdentityReviewStatus.OPEN;

    public Guid? ReviewedByUserId { get; set; }
    public virtual User? ReviewedByUser { get; set; }

    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public static PatientIdentityReviewCase Create(
        Guid patientId,
        Guid candidatePatientId,
        IdentityMatchReason matchReason)
    {
        if (patientId == candidatePatientId)
            throw new ArgumentException("Hồ sơ bệnh nhân và hồ sơ ứng viên không được trùng nhau.", nameof(candidatePatientId));

        return new PatientIdentityReviewCase
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            CandidatePatientId = candidatePatientId,
            MatchReason = matchReason,
            Status = IdentityReviewStatus.OPEN,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Resolve(IdentityReviewStatus resolution, Guid reviewedByUserId, string note)
    {
        if (Status != IdentityReviewStatus.OPEN && Status != IdentityReviewStatus.NEEDS_MORE_INFO)
            throw new InvalidOperationException("Ca rà soát này đã được kết luận trước đó.");

        if (resolution == IdentityReviewStatus.OPEN)
            throw new ArgumentException("Trạng thái kết luận không thể là OPEN.", nameof(resolution));

        Status = resolution;
        ReviewedByUserId = reviewedByUserId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNote = note?.Trim();
    }
}

public class PatientMergeEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourcePatientId { get; set; }
    public virtual Patient SourcePatient { get; set; } = null!;

    public Guid TargetPatientId { get; set; }
    public virtual Patient TargetPatient { get; set; } = null!;

    public Guid MergedByUserId { get; set; }
    public virtual User MergedByUser { get; set; } = null!;

    public string Reason { get; set; } = string.Empty;
    public DateTime MergedAt { get; set; } = DateTime.UtcNow;
    public string BeforeSnapshotJson { get; set; } = "{}";

    public static PatientMergeEvent Create(
        Guid sourcePatientId,
        Guid targetPatientId,
        Guid mergedByUserId,
        string reason,
        string beforeSnapshotJson)
    {
        if (sourcePatientId == targetPatientId)
            throw new ArgumentException("Hồ sơ nguồn và hồ sơ đích không được trùng nhau khi gộp.", nameof(targetPatientId));

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Lý do gộp hồ sơ bắt buộc phải được ghi rõ để phục vụ kiểm toán.", nameof(reason));

        return new PatientMergeEvent
        {
            Id = Guid.NewGuid(),
            SourcePatientId = sourcePatientId,
            TargetPatientId = targetPatientId,
            MergedByUserId = mergedByUserId,
            Reason = reason.Trim(),
            MergedAt = DateTime.UtcNow,
            BeforeSnapshotJson = string.IsNullOrWhiteSpace(beforeSnapshotJson) ? "{}" : beforeSnapshotJson
        };
    }
}
