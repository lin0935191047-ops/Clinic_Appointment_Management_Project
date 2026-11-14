namespace ClinicAppointment.Infrastructure.Security;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string SecretKey { get; set; } = "DefaultClinicAppointmentSuperSecretKey20261114Minimum32Bytes!";
    public string Issuer { get; set; } = "ClinicAppointmentApi";
    public string Audience { get; set; } = "ClinicAppointmentClient";
    public int AccessTokenExpirationMinutes { get; set; } = 60;
    public int RefreshTokenExpirationDays { get; set; } = 7;
}
