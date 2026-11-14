using ClinicAppointment.Domain.Common;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string NormalizedEmail { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime? EmailVerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation
    public virtual ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual Patient? PatientProfile { get; set; }
    public virtual Doctor? DoctorProfile { get; set; }

    public static User Create(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email không được để trống.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash không được để trống.", nameof(passwordHash));

        var normalized = email.Trim().ToUpperInvariant();
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim(),
            NormalizedEmail = normalized,
            PasswordHash = passwordHash,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}

public class Role
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty; // ADMIN, DOCTOR, RECEPTIONIST, PATIENT
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<UserRoleAssignment> UserRoles { get; set; } = new List<UserRoleAssignment>();

    public static Role Create(string code, string name)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Code = code.Trim().ToUpperInvariant(),
            Name = name.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static readonly string AdminCode = "ADMIN";
    public static readonly string DoctorCode = "DOCTOR";
    public static readonly string ReceptionistCode = "RECEPTIONIST";
    public static readonly string PatientCode = "PATIENT";
}

public class UserRoleAssignment
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public Guid RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}

public class RefreshToken : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual User User { get; set; } = null!;

    public byte[] TokenHash { get; set; } = Array.Empty<byte>();
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByTokenId { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt != null;
    public bool IsActive => !IsRevoked && !IsExpired;
}
