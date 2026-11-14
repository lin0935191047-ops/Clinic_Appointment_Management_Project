namespace ClinicAppointment.Application.Common.Helpers;

public static class TimeZoneHelper
{
    // Múi giờ chuẩn của phòng khám: Asia/Ho_Chi_Minh (UTC+7)
    public static readonly TimeSpan ClinicOffset = TimeSpan.FromHours(7);

    /// <summary>
    /// Chuyển đổi ngày và giờ địa phương tại phòng khám (ICT UTC+7) sang DateTime UTC
    /// </summary>
    public static DateTime ToUtc(DateOnly date, TimeOnly time)
    {
        var localDateTime = date.ToDateTime(time);
        var utcDateTime = localDateTime - ClinicOffset;
        return DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
    }

    /// <summary>
    /// Chuyển đổi DateTime UTC sang giờ địa phương phòng khám
    /// </summary>
    public static DateTime ToLocal(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return utc + ClinicOffset;
    }

    /// <summary>
    /// Lấy thứ trong tuần theo chuẩn ISO 8601 (Thứ Hai = 1, ..., Chủ Nhật = 7)
    /// </summary>
    public static short GetIsoDayOfWeek(DateOnly date)
    {
        return date.DayOfWeek switch
        {
            DayOfWeek.Monday => 1,
            DayOfWeek.Tuesday => 2,
            DayOfWeek.Wednesday => 3,
            DayOfWeek.Thursday => 4,
            DayOfWeek.Friday => 5,
            DayOfWeek.Saturday => 6,
            DayOfWeek.Sunday => 7,
            _ => 1
        };
    }

    /// <summary>
    /// Lấy khoảng thời gian UTC tương ứng với toàn bộ một ngày theo giờ địa phương phòng khám
    /// </summary>
    public static (DateTime StartUtc, DateTime EndUtc) GetDayUtcRange(DateOnly date)
    {
        var startUtc = ToUtc(date, TimeOnly.MinValue);
        var endUtc = ToUtc(date.AddDays(1), TimeOnly.MinValue);
        return (startUtc, endUtc);
    }
}
