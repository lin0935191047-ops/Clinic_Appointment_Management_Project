using ClinicAppointment.Domain.Common;

namespace ClinicAppointment.Domain.Entities;

/// <summary>
/// Thực thể ghi nhận nội dung thăm khám y tế (Consultation)
/// Dữ liệu nhạy cảm theo quy định bảo mật y tế (Assigned Doctor Only).
/// </summary>
public class Consultation : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid DoctorId { get; set; }

    /// <summary>
    /// Triệu chứng ban đầu của bệnh nhân
    /// </summary>
    public string Symptoms { get; set; } = string.Empty;

    /// <summary>
    /// Ghi chép chi tiết quá trình thăm khám lâm sàng
    /// </summary>
    public string? ExaminationNotes { get; set; }

    /// <summary>
    /// Chẩn đoán y khoa của bác sĩ
    /// </summary>
    public string Diagnosis { get; set; } = string.Empty;

    /// <summary>
    /// Kết luận sau khi khám
    /// </summary>
    public string? Conclusion { get; set; }

    /// <summary>
    /// Hướng dẫn theo dõi và dặn dò của bác sĩ
    /// </summary>
    public string? FollowUpInstructions { get; set; }

    /// <summary>
    /// Thời điểm thực hiện khám
    /// </summary>
    public DateTime? ConsultedAt { get; set; }

    // Navigation properties
    public virtual Appointment? Appointment { get; set; }
    public virtual Doctor? Doctor { get; set; }

    public static Consultation Create(
        Guid appointmentId,
        Guid doctorId,
        string symptoms,
        string diagnosis,
        string? examinationNotes = null,
        string? conclusion = null,
        string? followUpInstructions = null,
        DateTime? consultedAtUtc = null)
    {
        if (appointmentId == Guid.Empty)
            throw new ArgumentException("AppointmentId không hợp lệ.", nameof(appointmentId));

        if (doctorId == Guid.Empty)
            throw new ArgumentException("DoctorId không hợp lệ.", nameof(doctorId));

        if (string.IsNullOrWhiteSpace(symptoms))
            throw new ArgumentException("Triệu chứng ban đầu là bắt buộc.", nameof(symptoms));

        if (string.IsNullOrWhiteSpace(diagnosis))
            throw new ArgumentException("Chẩn đoán y khoa là bắt buộc.", nameof(diagnosis));

        var now = consultedAtUtc ?? DateTime.UtcNow;

        return new Consultation
        {
            Id = Guid.NewGuid(),
            AppointmentId = appointmentId,
            DoctorId = doctorId,
            Symptoms = symptoms.Trim(),
            Diagnosis = diagnosis.Trim(),
            ExaminationNotes = string.IsNullOrWhiteSpace(examinationNotes) ? null : examinationNotes.Trim(),
            Conclusion = string.IsNullOrWhiteSpace(conclusion) ? null : conclusion.Trim(),
            FollowUpInstructions = string.IsNullOrWhiteSpace(followUpInstructions) ? null : followUpInstructions.Trim(),
            ConsultedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void Update(
        string symptoms,
        string diagnosis,
        string? examinationNotes = null,
        string? conclusion = null,
        string? followUpInstructions = null)
    {
        if (string.IsNullOrWhiteSpace(symptoms))
            throw new ArgumentException("Triệu chứng ban đầu là bắt buộc.", nameof(symptoms));

        if (string.IsNullOrWhiteSpace(diagnosis))
            throw new ArgumentException("Chẩn đoán y khoa là bắt buộc.", nameof(diagnosis));

        Symptoms = symptoms.Trim();
        Diagnosis = diagnosis.Trim();
        ExaminationNotes = string.IsNullOrWhiteSpace(examinationNotes) ? null : examinationNotes.Trim();
        Conclusion = string.IsNullOrWhiteSpace(conclusion) ? null : conclusion.Trim();
        FollowUpInstructions = string.IsNullOrWhiteSpace(followUpInstructions) ? null : followUpInstructions.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
