namespace ClinicAppointment.Application.Features.Patients.DTOs;

public class PatientIdentityReviewCaseDto
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientMaskedPhone { get; set; } = string.Empty;

    public Guid CandidatePatientId { get; set; }
    public string CandidatePatientCode { get; set; } = string.Empty;
    public string CandidatePatientFullName { get; set; } = string.Empty;
    public string CandidateMaskedPhone { get; set; } = string.Empty;

    public string MatchReason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PatientMergeEventDto
{
    public Guid Id { get; set; }
    public Guid SourcePatientId { get; set; }
    public string SourcePatientCode { get; set; } = string.Empty;
    public Guid TargetPatientId { get; set; }
    public string TargetPatientCode { get; set; } = string.Empty;
    public Guid MergedByUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime MergedAt { get; set; }
    public string BeforeSnapshotJson { get; set; } = string.Empty;
}
