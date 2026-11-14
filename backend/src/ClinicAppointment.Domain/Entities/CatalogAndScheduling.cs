using ClinicAppointment.Domain.Common;

namespace ClinicAppointment.Domain.Entities;

public class Specialty : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Service> Services { get; set; } = new List<Service>();
}

public class Service : BaseEntity
{
    public Guid SpecialtyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public decimal Price { get; set; }
    public string Currency { get; set; } = "VND";
    public bool IsActive { get; set; } = true;

    public virtual Specialty? Specialty { get; set; }
}

public class Doctor : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SpecialtyId { get; set; }
    public string DoctorCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Bio { get; set; }
    public string? RoomNumber { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual Specialty? Specialty { get; set; }
    public virtual ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
}

public class DoctorSchedule : BaseEntity
{
    public Guid DoctorId { get; set; }
    public short DayOfWeek { get; set; } // 0 = Sun..6 = Sat
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    public virtual Doctor? Doctor { get; set; }
}

