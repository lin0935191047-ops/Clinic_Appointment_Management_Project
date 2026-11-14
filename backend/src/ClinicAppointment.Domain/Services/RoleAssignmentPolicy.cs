using ClinicAppointment.Domain.Entities;

namespace ClinicAppointment.Domain.Services;

public interface IRoleAssignmentPolicy
{
    bool CanAssignRole(User targetUser, Role targetRole, User? actingUser, out string failureReason);
    UserRoleAssignment CreateAssignment(User targetUser, Role targetRole, User? actingUser);
}

public class RoleAssignmentPolicy : IRoleAssignmentPolicy
{
    public bool CanAssignRole(User targetUser, Role targetRole, User? actingUser, out string failureReason)
    {
        failureReason = string.Empty;

        if (targetUser == null)
        {
            failureReason = "Người dùng đích không hợp lệ.";
            return false;
        }

        if (targetRole == null)
        {
            failureReason = "Vai trò đích không hợp lệ.";
            return false;
        }

        // Kiểm tra xem user đã có role này chưa
        if (targetUser.UserRoles.Any(ur => ur.RoleId == targetRole.Id || ur.Role?.Code == targetRole.Code))
        {
            failureReason = $"Người dùng đã được gán vai trò {targetRole.Code} trước đó.";
            return false;
        }

        // Policy: PATIENT có thể tự gán (đăng ký tài khoản bệnh nhân)
        if (targetRole.Code.Equals(Role.PatientCode, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Policy: ADMIN, DOCTOR, RECEPTIONIST bắt buộc phải do người dùng có vai trò ADMIN gán
        if (actingUser == null)
        {
            failureReason = $"Vai trò {targetRole.Code} yêu cầu quản trị viên (ADMIN) thực hiện gán.";
            return false;
        }

        bool isActingAdmin = actingUser.UserRoles.Any(ur => 
            ur.Role != null && ur.Role.Code.Equals(Role.AdminCode, StringComparison.OrdinalIgnoreCase));

        if (!isActingAdmin)
        {
            failureReason = $"Chỉ người dùng có vai trò ADMIN mới được phép gán vai trò {targetRole.Code}.";
            return false;
        }

        return true;
    }

    public UserRoleAssignment CreateAssignment(User targetUser, Role targetRole, User? actingUser)
    {
        if (!CanAssignRole(targetUser, targetRole, actingUser, out var failureReason))
        {
            throw new InvalidOperationException($"Không thể gán vai trò: {failureReason}");
        }

        var assignment = new UserRoleAssignment
        {
            UserId = targetUser.Id,
            User = targetUser,
            RoleId = targetRole.Id,
            Role = targetRole,
            AssignedAt = DateTime.UtcNow
        };

        targetUser.UserRoles.Add(assignment);
        return assignment;
    }
}
