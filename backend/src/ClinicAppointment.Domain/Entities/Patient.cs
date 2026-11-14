using ClinicAppointment.Domain.Common;

namespace ClinicAppointment.Domain.Entities;

public class Patient : BaseEntity
{
    public Guid? UserId { get; set; } // Nullable: Hỗ trợ Guest Booking không tài khoản
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public short? BirthYear { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool PhoneVerified { get; set; }
    public string? Email { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    
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

    public static Patient Create(
        string fullName, 
        string phoneNumber, 
        DateOnly? dateOfBirth = null, 
        short? birthYear = null, 
        Guid? userId = null,
        string? email = null)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Họ và tên bệnh nhân không được để trống.", nameof(fullName));

        if (dateOfBirth is null && birthYear is null)
            throw new ArgumentException("Hồ sơ bệnh nhân bắt buộc phải có Ngày sinh hoặc Năm sinh.", nameof(dateOfBirth));

        if (dateOfBirth.HasValue && birthYear.HasValue && dateOfBirth.Value.Year != birthYear.Value)
            throw new ArgumentException("Năm sinh và ngày sinh không khớp nhau.", nameof(birthYear));

        if (string.IsNullOrWhiteSpace(phoneNumber))
            throw new ArgumentException("Số điện thoại liên hệ là bắt buộc.", nameof(phoneNumber));

        return new Patient
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PatientCode = $"PAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}",
            FullName = fullName.Trim(),
            DateOfBirth = dateOfBirth,
            BirthYear = birthYear ?? (dateOfBirth.HasValue ? (short)dateOfBirth.Value.Year : null),
            PhoneNumber = phoneNumber.Trim(),
            Email = email?.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}

