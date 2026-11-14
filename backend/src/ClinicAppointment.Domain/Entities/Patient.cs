using ClinicAppointment.Domain.Common;

namespace ClinicAppointment.Domain.Entities;

public class Patient : BaseEntity
{
    public Guid? UserId { get; set; } // Nullable: Hỗ trợ Guest Booking không tài khoản
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NormalizedFullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public short? BirthYear { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneVerified { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string RecordStatus { get; set; } = "ACTIVE"; // ACTIVE, REVIEW_REQUIRED, INACTIVE, MERGED
    
    // Bảo vệ PII: Ciphertext và Keyed HMAC
    public byte[]? NationalIdCiphertext { get; set; }
    public string? NationalIdHmac { get; set; }
    public byte[]? PassportCiphertext { get; set; }
    public string? PassportHmac { get; set; }
    public byte[]? InsuranceCardCiphertext { get; set; }
    public string? InsuranceCardHmac { get; set; }
    public byte[]? PidCiphertext { get; set; }
    public string? PidHmac { get; set; }

    // Rà soát chống trùng lặp: Strictly No Auto-merge
    public bool IsPossibleDuplicate { get; set; }
    public Guid? MergedIntoPatientId { get; set; }

    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Patient? MergedIntoPatient { get; set; }
    public virtual ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public static Patient Create(
        string fullName, 
        string phoneNumber, 
        DateOnly? dateOfBirth = null, 
        short? birthYear = null, 
        Guid? userId = null,
        string? email = null,
        string? gender = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bệnh nhân không được để trống.", nameof(fullName));

        if (dateOfBirth is null && birthYear is null)
            throw new ArgumentException("Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh.", nameof(dateOfBirth));

        if (dateOfBirth.HasValue && birthYear.HasValue && dateOfBirth.Value.Year != birthYear.Value)
            throw new ArgumentException("Năm sinh và ngày sinh không khớp nhau.", nameof(birthYear));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Số điện thoại liên hệ là bắt buộc.", nameof(phoneNumber));

        var trimmedName = fullName.Trim();

        return new Patient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PatientCode = $"PAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            FullName = trimmedName,
            NormalizedFullName = NormalizeFullName(trimmedName),
            DateOfBirth = dateOfBirth,
            BirthYear = birthYear ?? (dateOfBirth.HasValue ? (short)dateOfBirth.Value.Year : null),
            PhoneNumber = phoneNumber.Trim(),
            Email = email?.Trim(),
            Gender = gender?.Trim(),
            Address = address?.Trim(),
            RecordStatus = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static string NormalizeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return string.Empty;

        // Chuẩn hóa khoảng trắng và chuyển thành chữ in hoa
        var parts = fullName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts).ToUpperInvariant();
    }

    public void LinkToUser(Guid userId)
    {
        if (UserId.HasValue && UserId.Value != userId)
            throw new InvalidOperationException("Hồ sơ bệnh nhân này đã được liên kết với một tài khoản khác.");

        UserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string fullName, 
        string phoneNumber, 
        DateOnly? dateOfBirth = null, 
        short? birthYear = null, 
        string? email = null,
        string? gender = null,
        string? address = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bệnh nhân không được để trống.", nameof(fullName));

        if (dateOfBirth is null && birthYear is null)
            throw new ArgumentException("Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh.", nameof(dateOfBirth));

        if (dateOfBirth.HasValue && birthYear.HasValue && dateOfBirth.Value.Year != birthYear.Value)
            throw new ArgumentException("Năm sinh và ngày sinh không khớp nhau.", nameof(birthYear));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Số điện thoại liên hệ là bắt buộc.", nameof(phoneNumber));

        var trimmedName = fullName.Trim();
        FullName = trimmedName;
        NormalizedFullName = NormalizeFullName(trimmedName);
        DateOfBirth = dateOfBirth;
        BirthYear = birthYear ?? (dateOfBirth.HasValue ? (short)dateOfBirth.Value.Year : null);
        PhoneNumber = phoneNumber.Trim();
        Email = email?.Trim();
        Gender = gender?.Trim();
        Address = address?.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
