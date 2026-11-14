using ClinicAppointment.Domain.Common;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Entities;

public class Appointment : BaseEntity
{
    public string BookingCode { get; set; } = string.Empty;
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ServiceId { get; set; }
    
    public DateTime StartAt { get; set; } // UTC
    public DateTime EndAt { get; set; }   // UTC
    
    public AppointmentStatus Status { get; set; } = AppointmentStatus.PendingConfirmation;
    
    // Snapshot dịch vụ tại thời điểm đặt lịch
    public string ServiceNameSnapshot { get; set; } = string.Empty;
    public int ServiceDurationSnapshot { get; set; }
    public decimal ServicePriceSnapshot { get; set; }
    
    public decimal PaidAmount { get; set; } = 0.0m;
    public string? ReasonForVisit { get; set; }
    
    // Quản lý hủy lịch theo server timestamp
    public DateTime? CancellationAcceptedAt { get; set; }
    public string? CancellationReason { get; set; }
    public bool IsGuest { get; set; }

    // Navigation properties
    public virtual Patient? Patient { get; set; }
    public virtual Doctor? Doctor { get; set; }
    public virtual Service? Service { get; set; }
    public virtual Consultation? Consultation { get; set; }

    public static Appointment CreateGuest(
        Guid doctorId,
        Guid patientId,
        Guid serviceId,
        string serviceName,
        int serviceDuration,
        decimal servicePrice,
        DateTime startAtUtc,
        string? reasonForVisit)
    {
        var endAtUtc = startAtUtc.AddMinutes(serviceDuration);
        var bookingCode = $"APT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

        return new Appointment
        {
            Id = Guid.NewGuid(),
            BookingCode = bookingCode,
            DoctorId = doctorId,
            PatientId = patientId,
            ServiceId = serviceId,
            ServiceNameSnapshot = serviceName,
            ServiceDurationSnapshot = serviceDuration,
            ServicePriceSnapshot = servicePrice,
            StartAt = startAtUtc,
            EndAt = endAtUtc,
            Status = AppointmentStatus.PendingConfirmation,
            ReasonForVisit = reasonForVisit,
            IsGuest = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Confirm()
    {
        if (Status != AppointmentStatus.PendingConfirmation)
            throw new InvalidOperationException($"Không thể xác nhận lịch hẹn ở trạng thái {Status}.");

        Status = AppointmentStatus.Confirmed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void CheckIn()
    {
        if (Status != AppointmentStatus.Confirmed)
            throw new InvalidOperationException($"Không thể Check-in cho lịch hẹn ở trạng thái {Status}.");

        Status = AppointmentStatus.CheckedIn;
        UpdatedAt = DateTime.UtcNow;
    }

    public void StartConsultation(DateTime? serverTimestampUtc = null)
    {
        if (Status != AppointmentStatus.CheckedIn)
            throw new InvalidOperationException($"Không thể bắt đầu ca khám cho lịch hẹn ở trạng thái {Status}. Chỉ có thể bắt đầu khi bệnh nhân đã được tiếp nhận (CheckedIn).");

        Status = AppointmentStatus.InProgress;
        UpdatedAt = serverTimestampUtc ?? DateTime.UtcNow;
    }

    public void CompleteConsultation(DateTime? serverTimestampUtc = null)
    {
        if (Status != AppointmentStatus.InProgress)
            throw new InvalidOperationException($"Không thể hoàn thành ca khám cho lịch hẹn ở trạng thái {Status}. Chỉ có thể hoàn thành khi ca khám đang diễn ra (InProgress).");

        Status = AppointmentStatus.Completed;
        UpdatedAt = serverTimestampUtc ?? DateTime.UtcNow;
    }

    public void Cancel(string reason, DateTime serverTimestampUtc)
    {
        if (Status == AppointmentStatus.Completed || Status == AppointmentStatus.Cancelled)
            throw new InvalidOperationException($"Không thể hủy lịch hẹn đã ở trạng thái {Status}.");

        Status = AppointmentStatus.Cancelled;
        CancellationReason = reason;
        CancellationAcceptedAt = serverTimestampUtc;
        UpdatedAt = serverTimestampUtc;
    }
}

