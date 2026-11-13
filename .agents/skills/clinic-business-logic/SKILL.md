---
name: clinic-business-logic
description: Hướng dẫn chi tiết về các thuật toán nghiệp vụ phòng khám cốt lõi: tính toán khung giờ khả dụng (available slots), chống trùng lặp, tính hoàn phí theo mốc 60 phút, quy trình đổi lịch và máy trạng thái vòng đời lịch hẹn.
---

# Clinic Business Logic Skill

> **Đồng bộ với:** [docs/SPEC.md](file:///d:/Clinic_Appointment_Management_Project/docs/SPEC.md) (Phiên bản SRS 1.2 Baseline)

Kỹ năng này cung cấp các nguyên tắc và thuật toán chuẩn để xử lý bài toán nghiệp vụ trung tâm của hệ thống phòng khám.

---

## 1. Thuật toán Tính toán Khung giờ Trống (Slot Calculator)

Quy trình tạo danh sách các khung giờ có thể đặt (`Available Slots`) cho một bác sĩ vào một ngày `D`:

```text
Input: DoctorId, Date D (ví dụ: 2026-11-20), ServiceId
Output: List<SlotDto> (mỗi slot có: StartTime, EndTime, IsAvailable)
```

### Các bước thực hiện:
1. **Xác định Thứ trong tuần:** Lấy `dayOfWeek = (int)D.DayOfWeek`.
2. **Lấy Lịch làm việc định kỳ:**
   - Truy vấn `doctor_schedules` với `DoctorId` và `DayOfWeek = dayOfWeek` đang hoạt động (`is_active = true`).
   - Nếu không có cấu hình ca trực -> Trả về danh sách rỗng (Bác sĩ không làm việc vào ngày này).
3. **Phân rã thành các Time Slots:**
   - Khởi tạo con trỏ `current = schedule.StartTime`.
   - Vòng lặp: trong khi `current + slotDuration <= schedule.EndTime`:
     - Tạo slot `[current, current + slotDuration]`.
     - Tăng `current += slotDuration`.
4. **Kiểm tra Ngoại lệ Nghỉ phép (Doctor Unavailabilities):**
   - Lấy toàn bộ bản ghi `doctor_unavailabilities` của bác sĩ trong ngày `D`.
   - Nếu slot giao cắt với khoảng thời gian nghỉ -> Đánh dấu `IsAvailable = false` hoặc loại khỏi danh sách.
5. **Kiểm tra Lịch hẹn đã đặt (Existing Appointments):**
   - Lấy toàn bộ lịch hẹn của bác sĩ trong ngày `D` có trạng thái khác `Cancelled` và `NoShow`.
   - Nếu slot trùng với `appointment.StartTime` -> Đánh dấu `IsAvailable = false`.
   - **Lưu ý:** Nếu lịch hẹn có yêu cầu đổi lịch ở trạng thái `PendingReview`, slot cũ của lịch hẹn đó vẫn được xem là bận và không được giải phóng.
6. **Kiểm tra Giờ hiện tại (Past Slots Filter):**
   - Nếu ngày `D` là ngày hôm nay -> Loại bỏ các slot có giờ bắt đầu nằm trong quá khứ hoặc cận giờ khám theo cấu hình.

---

## 2. Thuật toán Tính Hoàn phí Hủy lịch (Refund Calculation)

Mốc thời gian hủy được tính toán tự động dựa trên thời điểm máy chủ tiếp nhận hủy (`CancellationAcceptedAt`) và thời điểm bắt đầu ca khám (`StartAt`) theo chuẩn UTC:

```csharp
public record RefundCalculationResult(
    double MinutesBeforeAppointment,
    decimal CancellationFeeRate,
    decimal CancellationFeeAmount,
    decimal ExpectedRefundAmount);

public static class RefundCalculator
{
    public static RefundCalculationResult Calculate(
        DateTime startAtUtc, 
        DateTime cancellationAcceptedAtUtc, 
        decimal paidServiceAmount)
    {
        var minutesBefore = (startAtUtc - cancellationAcceptedAtUtc).TotalMinutes;

        if (minutesBefore <= 0)
        {
            throw new InvalidOperationException("Không thể tự hủy ca khám đã bắt đầu hoặc trong quá khứ.");
        }

        // Quy tắc: Từ 60 phút trở lên (bao gồm đúng mốc 60 phút) hoàn 100%, dưới 60 phút hoàn 50%
        decimal feeRate = minutesBefore >= 60.0 ? 0.0m : 0.50m;
        decimal feeAmount = paidServiceAmount * feeRate;
        decimal refundAmount = paidServiceAmount - feeAmount;

        return new RefundCalculationResult(
            minutesBefore,
            feeRate,
            feeAmount,
            refundAmount);
    }
}
```

---

## 3. Quy trình Đổi lịch Khám (Reschedule Atomic Workflow)

Khi bệnh nhân gửi yêu cầu đổi lịch:
1. Tạo bản ghi `RescheduleRequest` với trạng thái `PendingReview`.
2. Lịch cũ và slot cũ của `Appointment` vẫn giữ nguyên vẹn.
3. Khi nhân viên lễ tân bấm **Duyệt (`Approve`)**:
   - Mở Transaction CSDL mức cô lập `ReadCommitted`.
   - Kiểm tra xem slot mới đề xuất có còn trống không.
   - **Nếu slot mới trống:** Cập nhật `Appointment.AppointmentDate = requestedDate`, `Appointment.StartTime = requestedStartTime`, giải phóng slot cũ, đổi trạng thái request thành `Approved`.
   - **Nếu slot mới đã bị chiếm:** Hủy thao tác đổi lịch, giữ nguyên slot cũ của lịch hẹn, trả về lỗi xung đột (`409 Conflict`).

---

## 4. Máy Trạng thái Vòng đời Lịch hẹn (State Machine Validator)

Hàm kiểm tra chuyển đổi trạng thái hợp lệ theo chuẩn `SPEC.md`:

```csharp
public static class AppointmentStateMachine
{
    public static bool CanTransition(AppointmentStatus current, AppointmentStatus target)
    {
        return (current, target) switch
        {
            (AppointmentStatus.PendingConfirmation, AppointmentStatus.Confirmed) => true,
            (AppointmentStatus.PendingConfirmation, AppointmentStatus.Cancelled) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.CheckedIn) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.Cancelled) => true,
            (AppointmentStatus.Confirmed, AppointmentStatus.NoShow) => true,
            (AppointmentStatus.CheckedIn, AppointmentStatus.InProgress) => true,
            (AppointmentStatus.InProgress, AppointmentStatus.Completed) => true,
            _ => false
        };
    }
}
```

---

## 5. Quy tắc Đối sánh & Chống Trùng lặp Hồ sơ Bệnh nhân

1. **Validation bắt buộc:**
   - Hồ sơ bệnh nhân bắt buộc phải có `FullName` VÀ (`DateOfBirth` khác null HOẶC `BirthYear` khác null).
2. **Nguyên tắc No Auto-Merge:**
   - Trùng khớp Họ tên + Ngày sinh/Năm sinh **chỉ tạo ứng viên nghi trùng lặp** (`is_possible_duplicate = true`).
   - Tuyệt đối không tự động sáp nhập (merge) hai hồ sơ bệnh nhân trong code.
   - Thao tác gộp bắt buộc do Lễ tân hoặc Admin thực hiện có audit log ghi nhận rõ người thực hiện, thời gian và lý do.
