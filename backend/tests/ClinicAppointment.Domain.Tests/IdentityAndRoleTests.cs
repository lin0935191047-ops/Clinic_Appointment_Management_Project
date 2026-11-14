using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Services;
using ClinicAppointment.Infrastructure.Security;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class IdentityAndRoleTests
{
    private readonly RoleAssignmentPolicy _policy = new();
    private readonly PasswordHasher _hasher = new();

    private readonly Role _adminRole = Role.Create(Role.AdminCode, "Quản trị viên");
    private readonly Role _doctorRole = Role.Create(Role.DoctorCode, "Bác sĩ");
    private readonly Role _receptionistRole = Role.Create(Role.ReceptionistCode, "Lễ tân");
    private readonly Role _patientRole = Role.Create(Role.PatientCode, "Bệnh nhân");

    [Fact]
    public void User_Create_ShouldNormalizeEmail_AndSetTimestamps()
    {
        var email = "  Doctor.Mai@Clinic.vn  ";
        var passwordHash = _hasher.HashPassword("SecretPassword123!");

        var user = User.Create(email, passwordHash);

        Assert.Equal("Doctor.Mai@Clinic.vn", user.Email);
        Assert.Equal("DOCTOR.MAI@CLINIC.VN", user.NormalizedEmail);
        Assert.True(user.IsActive);
        Assert.True(_hasher.VerifyPassword(user.PasswordHash, "SecretPassword123!"));
        Assert.False(_hasher.VerifyPassword(user.PasswordHash, "WrongPassword!"));
    }

    [Theory]
    [InlineData("", "passwordHash")]
    [InlineData("   ", "passwordHash")]
    [InlineData("valid@email.com", "")]
    public void User_Create_WithInvalidData_ShouldThrowArgumentException(string email, string hash)
    {
        Assert.Throws<ArgumentException>(() => User.Create(email, hash));
    }

    [Fact]
    public void RoleAssignment_PatientRole_CanBeSelfAssigned()
    {
        var user = User.Create("patient@example.com", "hash");

        bool canAssign = _policy.CanAssignRole(user, _patientRole, actingUser: null, out var reason);

        Assert.True(canAssign);
        Assert.Empty(reason);

        var assignment = _policy.CreateAssignment(user, _patientRole, actingUser: null);
        Assert.Equal(_patientRole.Id, assignment.RoleId);
        Assert.Single(user.UserRoles);
    }

    [Theory]
    [InlineData("ADMIN")]
    [InlineData("DOCTOR")]
    [InlineData("RECEPTIONIST")]
    public void RoleAssignment_StaffRoles_WithoutAdmin_ShouldBeRejected(string roleCode)
    {
        var targetUser = User.Create("staff@example.com", "hash");
        var role = Role.Create(roleCode, roleCode);

        // Trường hợp 1: Không có actingUser
        bool canAssignNoUser = _policy.CanAssignRole(targetUser, role, actingUser: null, out var reasonNoUser);
        Assert.False(canAssignNoUser);
        Assert.Contains("ADMIN", reasonNoUser);

        // Trường hợp 2: actingUser chỉ là Bệnh nhân thường
        var regularUser = User.Create("regular@example.com", "hash");
        _policy.CreateAssignment(regularUser, _patientRole, actingUser: null);

        bool canAssignRegular = _policy.CanAssignRole(targetUser, role, actingUser: regularUser, out var reasonRegular);
        Assert.False(canAssignRegular);
        Assert.Contains("vai trò ADMIN", reasonRegular);
    }

    [Fact]
    public void RoleAssignment_StaffRoles_ByAdmin_ShouldSucceed()
    {
        var adminUser = User.Create("admin@clinic.vn", "hash");
        adminUser.UserRoles.Add(new UserRoleAssignment
        {
            UserId = adminUser.Id,
            User = adminUser,
            RoleId = _adminRole.Id,
            Role = _adminRole,
            AssignedAt = DateTime.UtcNow
        });

        var doctorUser = User.Create("doctor@clinic.vn", "hash");

        bool canAssign = _policy.CanAssignRole(doctorUser, _doctorRole, actingUser: adminUser, out var reason);

        Assert.True(canAssign);
        Assert.Empty(reason);

        var assignment = _policy.CreateAssignment(doctorUser, _doctorRole, actingUser: adminUser);
        Assert.Equal(_doctorRole.Id, assignment.RoleId);
        Assert.Single(doctorUser.UserRoles);
    }

    [Fact]
    public void RoleAssignment_DuplicateRole_ShouldBeRejected()
    {
        var user = User.Create("patient@example.com", "hash");
        _policy.CreateAssignment(user, _patientRole, actingUser: null);

        // Thử gán lại lần 2
        bool canAssignDuplicate = _policy.CanAssignRole(user, _patientRole, actingUser: null, out var reason);
        Assert.False(canAssignDuplicate);
        Assert.Contains("đã được gán vai trò", reason);
    }

    [Fact]
    public void PasswordHasher_ShouldProduceDifferentHashes_ForSamePassword_DueToSalt()
    {
        var pwd = "SamePassword123!";
        var hash1 = _hasher.HashPassword(pwd);
        var hash2 = _hasher.HashPassword(pwd);

        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.VerifyPassword(hash1, pwd));
        Assert.True(_hasher.VerifyPassword(hash2, pwd));
    }
}
