using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace ClinicAppointment.Application.Common.Helpers;

public static class SlugHelper
{
    public static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1. Chuyển chữ hoa sang chữ thường
        var normalized = text.Trim().ToLowerInvariant();

        // 2. Chuyển ký tự tiếng Việt có dấu sang không dấu
        normalized = RemoveDiacritics(normalized);

        // 3. Thay thế ký tự đ/Đ
        normalized = normalized.Replace("đ", "d").Replace("Đ", "d");

        // 4. Thay thế các ký tự không phải chữ cái hoặc số bằng dấu gạch ngang
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", "");

        // 5. Thay thế nhiều khoảng trắng hoặc nhiều gạch ngang liên tiếp thành 1 gạch ngang
        normalized = Regex.Replace(normalized, @"[\s-]+", "-").Trim('-');

        return normalized;
    }

    private static string RemoveDiacritics(string text)
    {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder(capacity: normalizedString.Length);

        for (int i = 0; i < normalizedString.Length; i++)
        {
            char c = normalizedString[i];
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}
