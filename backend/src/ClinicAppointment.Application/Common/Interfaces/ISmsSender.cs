using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Application.Common.Interfaces;

public interface ISmsSender
{
    /// <summary>
    /// Gửi mã xác thực OTP qua SMS. Nhà cung cấp thực tế là TBD.
    /// TUYỆT ĐỐI không ghi log chuỗi raw OTP (No OTP in logs).
    /// </summary>
    Task<bool> SendOtpAsync(
        string phoneNumber, 
        string otp, 
        OtpPurpose purpose, 
        CancellationToken cancellationToken = default);
}
