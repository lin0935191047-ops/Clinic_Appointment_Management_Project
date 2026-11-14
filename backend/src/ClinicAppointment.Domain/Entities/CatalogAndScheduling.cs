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

    public static Specialty Create(string code, string name, string slug, string? description = null, string? iconUrl = null, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã chuyên khoa không được để trống.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên chuyên khoa không được để trống.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug chuyên khoa không được để trống.", nameof(slug));

        return new Specialty
        {
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            Slug = slug.Trim().ToLowerInvariant(),
            Description = description?.Trim(),
            IconUrl = iconUrl?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(string name, string slug, string? description = null, string? iconUrl = null, bool? isActive = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên chuyên khoa không được để trống.", nameof(name));

        if (string.IsNullOrWhiteSpace(slug))
            throw new ArgumentException("Slug chuyên khoa không được để trống.", nameof(slug));

        Name = name.Trim();
        Slug = slug.Trim().ToLowerInvariant();
        Description = description?.Trim();
        IconUrl = iconUrl?.Trim();
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
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

    public static Service Create(
        Guid specialtyId,
        string code,
        string name,
        int durationMinutes,
        decimal price,
        string currency = "VND",
        string? description = null,
        bool isActive = true)
    {
        if (specialtyId == Guid.Empty)
            throw new ArgumentException("Chuyên khoa liên kết không hợp lệ.", nameof(specialtyId));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Mã dịch vụ không được để trống.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên dịch vụ không được để trống.", nameof(name));

        if (durationMinutes <= 0)
            throw new ArgumentException("Thời lượng dịch vụ phải lớn hơn 0 phút.", nameof(durationMinutes));

        if (price < 0)
            throw new ArgumentException("Giá dịch vụ không được âm.", nameof(price));

        return new Service
        {
            SpecialtyId = specialtyId,
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            DurationMinutes = durationMinutes,
            Price = price,
            Currency = string.IsNullOrWhiteSpace(currency) ? "VND" : currency.Trim().ToUpperInvariant(),
            Description = description?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        Guid specialtyId,
        string name,
        int durationMinutes,
        decimal price,
        string? description = null,
        bool? isActive = null)
    {
        if (specialtyId == Guid.Empty)
            throw new ArgumentException("Chuyên khoa liên kết không hợp lệ.", nameof(specialtyId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Tên dịch vụ không được để trống.", nameof(name));

        if (durationMinutes <= 0)
            throw new ArgumentException("Thời lượng dịch vụ phải lớn hơn 0 phút.", nameof(durationMinutes));

        if (price < 0)
            throw new ArgumentException("Giá dịch vụ không được âm.", nameof(price));

        SpecialtyId = specialtyId;
        Name = name.Trim();
        DurationMinutes = durationMinutes;
        Price = price;
        Description = description?.Trim();
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
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

    public static Doctor Create(
        Guid userId, 
        Guid specialtyId, 
        string doctorCode, 
        string fullName, 
        string? title = null, 
        string? bio = null, 
        string? roomNumber = null, 
        bool isActive = true)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("Tài khoản người dùng liên kết không hợp lệ.", nameof(userId));

        if (specialtyId == Guid.Empty)
            throw new ArgumentException("Chuyên khoa liên kết không hợp lệ.", nameof(specialtyId));

        if (string.IsNullOrWhiteSpace(doctorCode))
            throw new ArgumentException("Mã bác sĩ không được để trống.", nameof(doctorCode));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bác sĩ không được để trống.", nameof(fullName));

        return new Doctor
        {
            UserId = userId,
            SpecialtyId = specialtyId,
            DoctorCode = doctorCode.Trim().ToUpperInvariant(),
            FullName = fullName.Trim(),
            Title = title?.Trim(),
            Bio = bio?.Trim(),
            RoomNumber = roomNumber?.Trim(),
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        Guid specialtyId, 
        string fullName, 
        string? title = null, 
        string? bio = null, 
        string? roomNumber = null, 
        bool? isActive = null)
    {
        if (specialtyId == Guid.Empty)
            throw new ArgumentException("Chuyên khoa liên kết không hợp lệ.", nameof(specialtyId));

        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bác sĩ không được để trống.", nameof(fullName));

        SpecialtyId = specialtyId;
        FullName = fullName.Trim();
        Title = title?.Trim();
        Bio = bio?.Trim();
        RoomNumber = roomNumber?.Trim();
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class DoctorSchedule : BaseEntity
{
    public Guid DoctorId { get; set; }
    public short DayOfWeek { get; set; } // ISO 8601: Monday = 1, ..., Sunday = 7
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public int SlotDurationMinutes { get; set; } = 30;
    public bool IsActive { get; set; } = true;

    public virtual Doctor? Doctor { get; set; }

    public static DoctorSchedule Create(
        Guid doctorId,
        short dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        int slotDurationMinutes = 30,
        bool isActive = true)
    {
        if (doctorId == Guid.Empty)
            throw new ArgumentException("Bác sĩ phụ trách không hợp lệ.", nameof(doctorId));

        if (dayOfWeek < 1 || dayOfWeek > 7)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Thứ trong tuần phải từ 1 (Thứ Hai) đến 7 (Chủ Nhật) theo chuẩn ISO.");

        if (startTime >= endTime)
            throw new ArgumentException("Giờ bắt đầu ca làm việc phải trước giờ kết thúc.", nameof(startTime));

        if (effectiveTo.HasValue && effectiveFrom > effectiveTo.Value)
            throw new ArgumentException("Ngày bắt đầu hiệu lực không được lớn hơn ngày kết thúc hiệu lực.", nameof(effectiveFrom));

        if (slotDurationMinutes <= 0)
            throw new ArgumentException("Thời lượng mỗi slot phải lớn hơn 0 phút.", nameof(slotDurationMinutes));

        return new DoctorSchedule
        {
            DoctorId = doctorId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            EffectiveFrom = effectiveFrom,
            EffectiveTo = effectiveTo,
            SlotDurationMinutes = slotDurationMinutes,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        short dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        DateOnly effectiveFrom,
        DateOnly? effectiveTo = null,
        int slotDurationMinutes = 30,
        bool? isActive = null)
    {
        if (dayOfWeek < 1 || dayOfWeek > 7)
            throw new ArgumentOutOfRangeException(nameof(dayOfWeek), "Thứ trong tuần phải từ 1 (Thứ Hai) đến 7 (Chủ Nhật) theo chuẩn ISO.");

        if (startTime >= endTime)
            throw new ArgumentException("Giờ bắt đầu ca làm việc phải trước giờ kết thúc.", nameof(startTime));

        if (effectiveTo.HasValue && effectiveFrom > effectiveTo.Value)
            throw new ArgumentException("Ngày bắt đầu hiệu lực không được lớn hơn ngày kết thúc hiệu lực.", nameof(effectiveFrom));

        if (slotDurationMinutes <= 0)
            throw new ArgumentException("Thời lượng mỗi slot phải lớn hơn 0 phút.", nameof(slotDurationMinutes));

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        SlotDurationMinutes = slotDurationMinutes;
        if (isActive.HasValue)
        {
            IsActive = isActive.Value;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}

