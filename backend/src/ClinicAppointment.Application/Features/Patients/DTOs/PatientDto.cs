namespace ClinicAppointment.Application.Features.Patients.DTOs;

public class PatientDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NormalizedFullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public short? BirthYear { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string RecordStatus { get; set; } = "ACTIVE";
    public bool IsPossibleDuplicate { get; set; }
    public DateTime CreatedAt { get; set; }
}
