using System.Security.Claims;
using ClinicAppointment.Domain.Entities;

namespace ClinicAppointment.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles, Guid? patientProfileId = null, Guid? doctorProfileId = null);
    (string RawToken, byte[] TokenHash) GenerateRefreshToken();
    byte[] HashRefreshToken(string rawRefreshToken);
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
