using ClinicAppointment.Application.Common.Interfaces;

namespace ClinicAppointment.Infrastructure.Security;

public class IdentifierMaskingService : IIdentifierMaskingService
{
    public string MaskPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return string.Empty;

        var cleaned = phone.Trim().Replace(" ", "").Replace("-", "");
        if (cleaned.Length <= 4)
            return new string('*', cleaned.Length);

        if (cleaned.Length <= 7)
            return string.Concat(cleaned.AsSpan(0, 2), "***", cleaned.AsSpan(cleaned.Length - 2));

        // Format chuẩn: ví dụ 0912345678 -> 091***5678 (3 đầu, 4 cuối)
        return string.Concat(cleaned.AsSpan(0, 3), "***", cleaned.AsSpan(cleaned.Length - 4));
    }

    public string MaskNationalId(string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId))
            return string.Empty;

        var cleaned = nationalId.Trim().Replace(" ", "").Replace("-", "");
        if (cleaned.Length <= 6)
            return new string('*', cleaned.Length);

        // CCCD 12 số: hiển thị 4 đầu, 2 cuối, che giữa (ví dụ: 0010******34)
        return string.Concat(cleaned.AsSpan(0, 4), new string('*', cleaned.Length - 6), cleaned.AsSpan(cleaned.Length - 2));
    }

    public string MaskPassport(string passport)
    {
        if (string.IsNullOrWhiteSpace(passport))
            return string.Empty;

        var cleaned = passport.Trim().Replace(" ", "");
        if (cleaned.Length <= 4)
            return new string('*', cleaned.Length);

        // Hộ chiếu: hiển thị 3 ký tự đầu, 2 cuối (ví dụ: B12***89)
        return string.Concat(cleaned.AsSpan(0, 3), "***", cleaned.AsSpan(cleaned.Length - 2));
    }

    public string MaskHealthInsurance(string healthInsurance)
    {
        if (string.IsNullOrWhiteSpace(healthInsurance))
            return string.Empty;

        var cleaned = healthInsurance.Trim().Replace(" ", "").Replace("-", "");
        if (cleaned.Length <= 6)
            return new string('*', cleaned.Length);

        // Thẻ BHYT 10-15 ký tự: hiển thị 5 ký tự đầu (mã đối tượng/tỉnh), 2 ký tự cuối (ví dụ: GD401******89)
        int visiblePrefix = Math.Min(5, cleaned.Length - 3);
        return string.Concat(cleaned.AsSpan(0, visiblePrefix), "******", cleaned.AsSpan(cleaned.Length - 2));
    }

    public string MaskIdentifier(string identifier, string type)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return string.Empty;

        return type.ToUpperInvariant() switch
        {
            "PHONE" => MaskPhone(identifier),
            "NATIONAL_ID" or "CCCD" or "CMND" => MaskNationalId(identifier),
            "PASSPORT" or "HO_CHIEU" => MaskPassport(identifier),
            "HEALTH_INSURANCE" or "BHYT" => MaskHealthInsurance(identifier),
            _ => DefaultMask(identifier)
        };
    }

    private static string DefaultMask(string val)
    {
        var cleaned = val.Trim();
        if (cleaned.Length <= 4)
            return new string('*', cleaned.Length);

        return string.Concat(cleaned.AsSpan(0, 2), new string('*', cleaned.Length - 4), cleaned.AsSpan(cleaned.Length - 2));
    }
}
