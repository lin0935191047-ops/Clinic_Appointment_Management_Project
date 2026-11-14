namespace ClinicAppointment.Application.Common.Interfaces;

/// <summary>
/// Dịch vụ làm sạch và che giấu dữ liệu nhạy cảm (PII Redaction, Secret Redaction)
/// trước khi đưa vào hệ thống ghi log hoặc lưu trữ kiểm toán.
/// </summary>
public interface ILogRedactor
{
    /// <summary>
    /// Làm sạch chuỗi văn bản: che giấu số điện thoại, số CCCD/CMND, BHYT, OTP, mật khẩu, tokens, connection secrets.
    /// </summary>
    string Redact(string? input);

    /// <summary>
    /// Làm sạch một từ điển tham số/metadata.
    /// </summary>
    IDictionary<string, object?> RedactDictionary(IDictionary<string, object?>? dictionary);
}
