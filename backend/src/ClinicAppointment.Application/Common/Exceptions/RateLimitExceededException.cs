namespace ClinicAppointment.Application.Common.Exceptions;

public class RateLimitExceededException : Exception
{
    public RateLimitExceededException(string message = "Quý khách đã gửi yêu cầu quá nhiều lần trong thời gian ngắn. Vui lòng chờ ít phút trước khi thử lại.")
        : base(message)
    {
    }
}
