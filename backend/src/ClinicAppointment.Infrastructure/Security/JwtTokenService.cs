using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ClinicAppointment.Infrastructure.Security;

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly SymmetricSecurityKey _key;

    public JwtTokenService(IConfiguration configuration)
    {
        _jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
    }

    public string GenerateAccessToken(User user, IEnumerable<string> roles, Guid? patientProfileId = null, Guid? doctorProfileId = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (patientProfileId.HasValue)
        {
            claims.Add(new Claim("patient_profile_id", patientProfileId.Value.ToString()));
        }

        if (doctorProfileId.HasValue)
        {
            claims.Add(new Claim("doctor_profile_id", doctorProfileId.Value.ToString()));
        }

        var creds = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public (string RawToken, byte[] TokenHash) GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var rawToken = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var tokenHash = HashRefreshToken(rawToken);
        return (rawToken, tokenHash);
    }

    public byte[] HashRefreshToken(string rawRefreshToken)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(Encoding.UTF8.GetBytes(rawRefreshToken));
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = _jwtSettings.Audience,
            ValidateIssuer = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _key,
            ValidateLifetime = false // Bỏ qua hạn dùng để lấy claims từ token đã hết hạn
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
