namespace ClinicAppointment.Application.Features.Auth.DTOs;

public record RegisterPatientRequest(
    string Email,
    string Password,
    string FullName,
    string PhoneNumber,
    DateOnly? DateOfBirth = null,
    short? BirthYear = null,
    string? Gender = null,
    string? Address = null,
    string? NationalId = null
);

public record LoginRequest(
    string Email,
    string Password
);

public record RefreshTokenRequest(
    string RefreshToken
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ForgotPasswordRequest(
    string Email
);

public record ResetPasswordRequest(
    string Email,
    string ResetToken,
    string NewPassword
);

public record UserSummaryDto(
    Guid Id,
    string Email,
    IReadOnlyList<string> Roles,
    string? FullName,
    Guid? PatientProfileId,
    Guid? DoctorProfileId
);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    string TokenType,
    UserSummaryDto User
);
