namespace ClinicAppointment.Application.Common.Exceptions;

public class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException(string message = "Idempotency key này đã được sử dụng cho một yêu cầu khác với nội dung khác. Vui lòng tạo key mới.")
        : base(message)
    {
    }
}
