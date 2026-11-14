namespace ClinicAppointment.Application.Common.Exceptions;

public class AppointmentConflictException : Exception
{
    public AppointmentConflictException(string message = "Khung giờ khám này vừa có người khác đặt trước. Vui lòng chọn khung giờ khám khác.")
        : base(message)
    {
    }

    public AppointmentConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
