using System.Text.RegularExpressions;

namespace ClinicAppointment.Application.Common.Notifications;

public class NotificationSafetyGuard : INotificationSafetyGuard
{
    private static readonly string[] ProhibitedMedicalKeywords = new[]
    {
        "chẩn đoán:",
        "chẩn đoán y khoa",
        "triệu chứng:",
        "bệnh án:",
        "diagnosis:",
        "symptoms:",
        "kết quả sinh thiết",
        "nội soi phát hiện",
        "ung thư",
        "viêm gan b",
        "viêm gan c",
        "hiv",
        "lao phổi"
    };

    private static readonly Regex CccdPattern = new Regex(@"\b(cccd|cmnd|định danh)?\s*[:\s-]?\s*\d{12}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex PasswordPattern = new Regex(@"\b(mật khẩu|password|mat khau)\s*[:=]\s*\S+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public void ValidateMessageSafety(NotificationMessage message)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        var textToCheck = $"{message.Subject} {message.Content}";

        if (string.IsNullOrWhiteSpace(textToCheck))
            return;

        // 1. Kiểm tra chẩn đoán y khoa / triệu chứng lâm sàng nhạy cảm
        foreach (var keyword in ProhibitedMedicalKeywords)
        {
            if (textToCheck.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                throw new NotificationSafetyViolationException(
                    $"Nội dung thông báo vi phạm chính sách bảo mật y tế: Không được chứa thông tin chẩn đoán hoặc bệnh án lâm sàng ({keyword}).");
            }
        }

        // 2. Kiểm tra số CCCD/CMND dạng thô 12 số
        if (CccdPattern.IsMatch(textToCheck))
        {
            throw new NotificationSafetyViolationException(
                "Nội dung thông báo vi phạm chính sách bảo mật: Không được chứa số định danh cá nhân/CCCD đầy đủ dạng thô.");
        }

        // 3. Kiểm tra mật khẩu dạng thô
        if (PasswordPattern.IsMatch(textToCheck))
        {
            throw new NotificationSafetyViolationException(
                "Nội dung thông báo vi phạm chính sách bảo mật: Không được gửi mật khẩu dạng văn bản thô.");
        }

        // 4. Kiểm tra trong Payload nếu có
        if (message.Payload != null)
        {
            foreach (var kvp in message.Payload)
            {
                var valStr = kvp.Value?.ToString() ?? string.Empty;
                var keyStr = kvp.Key;

                if (keyStr.Equals("Diagnosis", StringComparison.OrdinalIgnoreCase) ||
                    keyStr.Equals("Symptoms", StringComparison.OrdinalIgnoreCase) ||
                    keyStr.Equals("ExaminationNotes", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(valStr))
                    {
                        throw new NotificationSafetyViolationException(
                            $"Payload thông báo vi phạm chính sách an toàn: Không được đính kèm trường y tế nhạy cảm '{keyStr}'.");
                    }
                }
            }
        }
    }
}
