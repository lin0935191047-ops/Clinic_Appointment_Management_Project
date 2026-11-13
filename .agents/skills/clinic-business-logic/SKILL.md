---
name: clinic-business-logic
description: Hướng dẫn chi tiết về các thuật toán nghiệp vụ phòng khám cốt lõi: tính toán khung giờ khả dụng (available slots), chống trùng lặp và máy trạng thái vòng đời lịch hẹn.
---

# Clinic Business Logic Skill

Kỹ năng này cung cấp các nguyên tắc và thuật toán chuẩn để xử lý bài toán nghiệp vụ trung tâm của hệ thống đặt lịch khám bệnh.

---

## 1. Thuật toán Tính toán Khung giờ Trống (Slot Calculator)

Quy trình tạo danh sách các khung giờ có thể đặt (`Available Slots`) cho một bác sĩ vào một ngày `D`:

```text
Input: DoctorId, Date D (ví dụ: 2026-11-20)
Output: List<SlotDto> (mỗi slot có: StartTime, EndTime, IsAvailable)
```

### Các bước thực hiện:
1. **Xác định Thứ trong tuần:** Lấy `dayOfWeek = (int)D.DayOfWeek`.
2. **Lấy Lịch làm việc định kỳ:**
   - Truy vấn `doctor_schedules` với `DoctorId` và `DayOfWeek = dayOfWeek` đang hoạt động (`is_active = true`).
   - Nếu không có cấu hình ca trực -> Trả về danh sách rỗng (Bác sĩ không làm việc vào thứ này).
3. **Phân rã thành các Time Slots:**
   - Khởi tạo con trỏ `current = schedule.StartTime`.
   - Vòng lặp: trong khi `current + slotDuration <= schedule.EndTime`:
     - Tạo slot `[current, current + slotDuration]`.
     - Tăng `current += slotDuration`.
4. **Kiểm tra Ngoại lệ Nghỉ phép (Doctor Unavailabilities):**
   - Lấy toàn bộ bản ghi `doctor_unavailabilities` của bác sĩ trong ngày `D`.
   - Nếu slot giao cắt với khoảng thời gian nghỉ -> Đánh dấu `IsAvailable = false` hoặc loại khỏi danh sách.
5. **Kiểm tra Lịch hẹn đã đặt (Existing Appointments):**
   - Lấy toàn bộ lịch hẹn của bác sĩ trong ngày `D` có trạng thái khác `CANCELLED` và `NO_SHOW`.
   - Nếu slot trùng với `appointment.StartTime` -> Đánh dấu `IsAvailable = false`.
6. **Kiểm tra Giờ hiện tại (Past Slots Filter):**
   - Nếu ngày `D` là ngày hôm nay -> Loại bỏ các slot có giờ bắt đầu sớm hơn giờ hiện tại cộng thêm 2 tiếng (`now + 2 hours`).

---

## 2. Quy tắc Sinh Mã Lịch hẹn (Appointment Code Generation)

- **Định dạng chuẩn:** `APT-YYYYMMDD-XXXX` (ví dụ: `APT-20261120-K7B2`).
- Trong đó:
  - `APT`: Tiền tố viết tắt của Appointment.
  - `YYYYMMDD`: Ngày hẹn khám (hoặc ngày đặt lịch).
  - `XXXX`: Chuỗi 4 ký tự ngẫu nhiên gồm chữ in hoa và chữ số (bỏ qua các ký tự dễ nhầm lẫn như `0`, `O`, `1`, `I`).
- Đảm bảo tính duy nhất bằng cách kiểm tra trùng lặp trước khi lưu hoặc cơ chế retry nếu trùng index.

---

## 3. Máy Trạng thái Vòng đời Lịch hẹn (State Machine Validator)

Trước khi thực hiện đổi trạng thái của một lịch hẹn, bắt buộc phải kiểm tra qua hàm kiểm duyệt:

```csharp
public static bool CanTransition(AppointmentStatus current, AppointmentStatus target)
{
    return (current, target) switch
    {
        (AppointmentStatus.Pending, AppointmentStatus.Confirmed) => true,
        (AppointmentStatus.Pending, AppointmentStatus.Cancelled) => true,
        (AppointmentStatus.Confirmed, AppointmentStatus.CheckedIn) => true,
        (AppointmentStatus.Confirmed, AppointmentStatus.Cancelled) => true,
        (AppointmentStatus.Confirmed, AppointmentStatus.NoShow) => true,
        (AppointmentStatus.CheckedIn, AppointmentStatus.InProgress) => true,
        (AppointmentStatus.InProgress, AppointmentStatus.Completed) => true,
        _ => false
    };
}
```

Nếu `CanTransition` trả về `false`, lập tức ném ra `InvalidStateTransitionException` và trả về HTTP 400.

