using System.Text.RegularExpressions;
using ClinicAppointment.Application.Common.Interfaces;

namespace ClinicAppointment.Infrastructure.Logging;

/// <summary>
/// Cài đặt dịch vụ che giấu thông tin nhạy cảm (PII & Secrets Redaction)
/// Tuân thủ nghiêm ngặt quy định bất biến:
/// - Không log OTP, access token, refresh token, mật khẩu, CCCD đầy đủ, hay nội dung bệnh án.
/// - Không log chuỗi kết nối chứa mật khẩu.
/// </summary>
public class LogRedactor : ILogRedactor
{
    // 1. Password trong connection string hoặc key-value (dừng ở dấu chấm phẩy hoặc khoảng trắng)
    private static readonly Regex ConnectionStringPasswordRegex = new(
        @"(?i)(password|pwd)\s*=\s*[^;\s]+",
        RegexOptions.Compiled);

    // 2. Mật khẩu trong query string / headers / json payload
    private static readonly Regex PasswordFieldRegex = new(
        @"(?i)(""(?:password|currentPassword|newPassword|confirmPassword|secret|mật khẩu)""\s*:\s*"")[^""]*("")|((?:password|pwd|secret)\s*[:=]\s*)([^\s,;&]+)",
        RegexOptions.Compiled);

    // 3. Bearer JWT Access Token
    private static readonly Regex BearerTokenRegex = new(
        @"Bearer\s+[A-Za-z0-9\-_\.]+",
        RegexOptions.Compiled);

    // 4. Scoped Grant Token (prefix grnt_)
    private static readonly Regex GrantTokenRegex = new(
        @"\bgrnt_[A-Za-z0-9_-]+\b",
        RegexOptions.Compiled);

    // 5. Mã xác thực OTP (6 chữ số) kèm nhãn nhận diện
    private static readonly Regex OtpRegex = new(
        @"(?i)(""(?:otp|otpCode|code|mã xác thực|mã otp)""\s*:\s*"")\d{6}("")|((?:otp|otpCode|code|mã xác thực|mã otp)[\s:=]+)\d{6}\b",
        RegexOptions.Compiled);

    // 6. Số CCCD (12 chữ số)
    private static readonly Regex CccdRegex = new(
        @"\b\d{12}\b",
        RegexOptions.Compiled);

    // 7. Số CMND cũ (9 chữ số)
    private static readonly Regex CmndRegex = new(
        @"\b\d{9}\b",
        RegexOptions.Compiled);

    // 8. Thẻ BHYT (2 chữ cái + 13 chữ số)
    private static readonly Regex BhytRegex = new(
        @"\b[A-Za-z]{2}\d{13}\b",
        RegexOptions.Compiled);

    // 9. Số điện thoại di động Việt Nam (10 số, đầu 03, 05, 07, 08, 09 hoặc +84)
    private static readonly Regex PhoneRegex = new(
        @"(?:\+84|0)(3[2-9]|5[2689]|7[06-9]|8[1-9]|9[0-9])\d{7}\b",
        RegexOptions.Compiled);

    public string Redact(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        var result = input;

        // 1. Redact connection string passwords
        result = ConnectionStringPasswordRegex.Replace(result, "$1=[REDACTED]");

        // 2. Redact password fields in JSON/prose
        result = PasswordFieldRegex.Replace(result, match =>
        {
            if (match.Groups[1].Success)
            {
                return $"{match.Groups[1].Value}[REDACTED]{match.Groups[2].Value}";
            }
            return $"{match.Groups[3].Value}[REDACTED]";
        });

        // 3. Redact Bearer tokens
        result = BearerTokenRegex.Replace(result, "Bearer [REDACTED_TOKEN]");

        // 4. Redact Grant tokens
        result = GrantTokenRegex.Replace(result, "grnt_[REDACTED]");

        // 5. Redact OTP (6 digits)
        result = OtpRegex.Replace(result, match =>
        {
            if (match.Groups[1].Success)
            {
                return $"{match.Groups[1].Value}[REDACTED_OTP]{match.Groups[2].Value}";
            }
            return $"{match.Groups[3].Value}[REDACTED_OTP]";
        });

        // 6. Redact CCCD 12 số
        result = CccdRegex.Replace(result, "[REDACTED_CCCD]");

        // 7. Redact BHYT 15 ký tự
        result = BhytRegex.Replace(result, "[REDACTED_BHYT]");

        // 8. Mask số điện thoại di động (giữ 3 số đầu và 3 số cuối: 091***789)
        result = PhoneRegex.Replace(result, match =>
        {
            var val = match.Value;
            if (val.Length >= 10)
            {
                return $"{val[..3]}***{val[^3..]}";
            }
            return "[REDACTED_PHONE]";
        });

        return result;
    }

    public IDictionary<string, object?> RedactDictionary(IDictionary<string, object?>? dictionary)
    {
        var sanitized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        if (dictionary == null)
        {
            return sanitized;
        }

        foreach (var (key, value) in dictionary)
        {
            if (IsSensitiveKey(key))
            {
                sanitized[key] = "[REDACTED]";
            }
            else if (value is string strVal)
            {
                sanitized[key] = Redact(strVal);
            }
            else
            {
                sanitized[key] = value;
            }
        }

        return sanitized;
    }

    private static bool IsSensitiveKey(string key)
    {
        var lower = key.ToLowerInvariant();
        return lower.Contains("password")
            || lower.Contains("secret")
            || lower.Contains("token")
            || lower.Contains("otp")
            || lower.Contains("cccd")
            || lower.Contains("citizenid")
            || lower.Contains("insurance")
            || lower.Contains("diagnosis")
            || lower.Contains("symptoms")
            || lower.Contains("clinical");
    }
}
