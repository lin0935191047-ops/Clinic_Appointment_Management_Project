# Clinic Appointment Management System — Activity Diagrams

- **Phiên bản:** 1.2
- **Căn cứ trên:** [docs/SPEC.md](SPEC.md) (SRS Version 1.2 Baseline) và [docs/USE_CASE_SPECIFICATION.md](USE_CASE_SPECIFICATION.md)
- **Ký pháp:** PlantUML Activity Diagram

---

## 1. Activity Diagram – Đặt lịch khám không cần đăng nhập (Guest Booking)

Mô tả luồng đặt lịch công khai từ lúc Guest chọn dịch vụ đến khi nhận mã đặt lịch (`BookingCode`).

```plantuml
@startuml
|Guest|
start
:Mở trang đặt lịch khám;
:Xem danh mục chuyên khoa và bác sĩ;
:Chọn bác sĩ và dịch vụ khám;
:Chọn ngày khám;

|Frontend - React.js|
:Gửi yêu cầu lấy available slots;

|Backend - ASP.NET Core|
:Kiểm tra bác sĩ và dịch vụ đang hoạt động;
:Đọc ca trực, ngày nghỉ và lịch hẹn đã đặt;
:Tính toán danh sách khung giờ trống;

|Frontend - React.js|
:Hiển thị danh sách khung giờ trống;

|Guest|
if (Có khung giờ phù hợp?) then (Có)
  :Chọn khung giờ khám;
  :Nhập Họ và tên đầy đủ;
  :Nhập Ngày sinh HOẶC Năm sinh;
  :Nhập Số điện thoại liên hệ;
  :Nhập triệu chứng ban đầu;
  :Kiểm tra và xác nhận thông tin;
else (Không)
  :Chọn ngày hoặc bác sĩ khác;
  stop
endif

|Frontend - React.js|
:Gửi POST /api/v1/appointments/guest;

|Backend - ASP.NET Core|
:Validate dữ liệu đầu vào (FullName + DOB hoặc BirthYear);
if (Dữ liệu hợp lệ?) then (Có)
  :Kiểm tra lại bác sĩ, dịch vụ, ngày nghỉ và slot;
  if (Khung giờ vẫn còn trống?) then (Có)
    :Bắt đầu CSDL Transaction;
    :Tạo hồ sơ Patient (UserId = NULL);
    :Kiểm tra trùng lặp (nếu nghi ngờ: gắn cờ is_possible_duplicate, KHÔNG auto-merge);
    :Sinh BookingCode duy nhất khó đoán (không PII);
    :Chụp snapshot dịch vụ (tên, thời lượng, giá);
    :Tạo Appointment - trạng thái PendingConfirmation;
    :Ghi vết AppointmentHistory & AuditLog;
    if (Lưu tất cả thành công?) then (Có)
      :Commit Transaction;
      |Frontend - React.js|
      :Hiển thị màn hình thành công và BookingCode;
      |Guest|
      :Lưu mã BookingCode để tra cứu/đổi/hủy;
      stop
    else (Không)
      |Backend - ASP.NET Core|
      :Rollback Transaction;
      :Trả lỗi hệ thống 500;
      |Frontend - React.js|
      :Thông báo thử lại sau;
      stop
    endif
  else (Không)
    :Trả lỗi 409 Conflict - Slot no longer available;
    |Frontend - React.js|
    :Thông báo khung giờ vừa có người đặt;
    stop
  endif
else (Không)
  :Trả lỗi 400 Validation (thiếu trường bắt buộc);
  |Frontend - React.js|
  :Hiển thị lỗi tại trường dữ liệu tương ứng;
  stop
endif
@enduml
```

---

## 2. Activity Diagram – Tra cứu lịch hẹn bằng BookingCode và OTP

Mô tả luồng Guest tra cứu thông tin lịch hẹn an toàn qua xác thực mã OTP.

```plantuml
@startuml
|Guest|
start
:Mở trang Tra cứu lịch hẹn;
:Nhập BookingCode và Số điện thoại;
:Bấm Yêu cầu mã OTP;

|Frontend - React.js|
:Gửi POST /api/v1/otp/send (Purpose = LOOKUP);

|Backend - ASP.NET Core|
:Áp dụng Rate Limiting & Cooldown 60s;
:Kiểm tra BookingCode và Số điện thoại;
if (Thông tin khớp?) then (Có)
  :Sinh mã OTP dùng 1 lần (hạn 5 phút);
  :Gửi mã OTP về số điện thoại;
  |Frontend - React.js|
  :Hiển thị form nhập mã OTP;
  |Guest|
  :Nhập mã OTP nhận được;
  :Bấm Xác nhận OTP;
  |Frontend - React.js|
  :Gửi POST /api/v1/otp/verify;
  |Backend - ASP.NET Core|
  :Kiểm tra OTP, hạn dùng và số lần thử (tối đa 5);
  if (OTP hợp lệ?) then (Có)
    :Cấp otpChallengeToken (Purpose = LOOKUP);
    :Gửi POST /api/v1/appointments/lookup kèm Token;
    :Trả thông tin tóm tắt lịch hẹn (ẩn bớt PII);
    |Frontend - React.js|
    :Hiển thị bác sĩ, chuyên khoa, ngày giờ, trạng thái;
    stop
  else (Không)
    :Từ chối xác thực;
    |Frontend - React.js|
    :Thông báo mã OTP không chính xác hoặc hết hạn;
    stop
  endif
else (Không)
  :Trả thông báo chung (không lộ thông tin có tồn tại mã hay không);
  |Frontend - React.js|
  :Thông báo không tìm thấy thông tin phù hợp;
  stop
endif
@enduml
```

---

## 3. Activity Diagram – Quy trình Đổi lịch khám (Reschedule Workflow)

Mô tả quy trình đổi lịch: Bệnh nhân gửi yêu cầu, lễ tân phê duyệt; **slot cũ giữ nguyên cho đến khi duyệt**.

```plantuml
@startuml
|Guest / Patient|
start
:Mở chức năng Đổi lịch khám;
:Xác thực quyền qua OTP (Purpose = RESCHEDULE) hoặc phiên đăng nhập;
:Chọn ngày và khung giờ mới muốn đề xuất;
:Nhập lý do đổi lịch;
:Bấm Gửi yêu cầu đổi lịch;

|Backend - ASP.NET Core|
:Kiểm tra lịch hẹn đang ở trạng thái hợp lệ;
:Kiểm tra chưa có request PendingReview nào khác;
:Tạo RescheduleRequest - trạng thái PendingReview;
note right
  Lịch cũ và slot cũ
  vẫn được giữ nguyên vẹn!
end note
|Frontend - React.js|
:Thông báo yêu cầu đã gửi, chờ lễ tân duyệt;

|Receptionist|
:Mở danh sách Yêu cầu đổi lịch;
:Xem chi tiết lịch cũ và slot mới đề xuất;
if (Quyết định phê duyệt?) then (Duyệt)
  :Bấm Duyệt đổi lịch;
  |Backend - ASP.NET Core|
  :Mở CSDL Transaction;
  :Kiểm tra slot mới đề xuất có còn trống không;
  if (Slot mới còn trống?) then (Có)
    :Cập nhật Appointment sang ngày/giờ mới;
    :Giải phóng slot cũ;
    :Đổi trạng thái RescheduleRequest -> Approved;
    :Commit Transaction;
    :Gửi thông báo đổi lịch thành công;
    |Receptionist|
    :Hiển thị đổi lịch thành công;
    stop
  else (Không)
    :Rollback Transaction;
    :Đổi request sang Rejected hoặc báo xung đột;
    :Bảo toàn slot cũ của lịch hẹn;
    |Receptionist|
    :Thông báo slot mới đã bị chiếm;
    stop
  endif
else (Từ chối)
  :Nhập lý do từ chối;
  :Bấm Từ chối yêu cầu;
  |Backend - ASP.NET Core|
  :Đổi trạng thái RescheduleRequest -> Rejected;
  :Ghi lý do từ chối;
  :Bảo toàn slot cũ của lịch hẹn;
  |Receptionist|
  :Thông báo đã từ chối yêu cầu;
  stop
endif
@enduml
```

---

## 4. Activity Diagram – Bệnh nhân Hủy lịch & Tính toán Hoàn phí (Cancellation & Refund)

Mô tả quy trình tự hủy lịch: **Không cần lễ tân duyệt**, tính hoàn phí theo mốc 60 phút.

```plantuml
@startuml
|Guest / Patient|
start
:Mở chức năng Hủy lịch hẹn;
:Xác thực quyền qua OTP (Purpose = CANCEL) hoặc phiên đăng nhập;
:Nhập lý do hủy lịch;
:Bấm Xác nhận hủy;

|Backend - ASP.NET Core|
:Ghi nhận Server Timestamp (UTC CancellationAcceptedAt);
:Tính toán minutesBeforeAppointment = (StartAt - CancellationAcceptedAt).TotalMinutes;
if (minutesBeforeAppointment > 0?) then (Có - Trước giờ khám)
  :Bắt đầu CSDL Transaction;
  if (minutesBeforeAppointment >= 60?) then (Từ 60 phút trở lên)
    :Áp dụng hoàn 100% phí đã thanh toán;
    :Phí hủy 0%;
  else (Dưới 60 phút)
    :Áp dụng hoàn 50% phí đã thanh toán;
    :Phí hủy 50%;
  endif
  :Cập nhật Appointment.Status = Cancelled;
  :Tạo bản ghi Refunds (kèm tỷ lệ, số tiền hoàn, trạng thái Pending);
  :Giải phóng khung giờ khám của bác sĩ;
  :Ghi vết AppointmentHistory & AuditLog;
  :Commit Transaction;
  |Frontend - React.js|
  :Hiển thị hủy lịch thành công và chi tiết mức hoàn phí;
  |Guest / Patient|
  :Xem thông tin hoàn phí đã ghi nhận;
  stop
else (Không - Đã đến hoặc qua giờ khám)
  :Từ chối tự hủy trên hệ thống;
  |Frontend - React.js|
  :Thông báo ca khám đã bắt đầu, vui lòng liên hệ trực tiếp lễ tân;
  stop
endif
@enduml
```

---

## 5. Activity Diagram – Bác sĩ Tiếp nhận & Hoàn tất Ca khám

```plantuml
@startuml
|Doctor|
start
:Đăng nhập hệ thống;
:Mở danh sách lịch khám được phân công;
:Chọn bệnh nhân đang ở trạng thái CheckedIn;
:Bấm Bắt đầu khám;

|Backend - ASP.NET Core|
:Kiểm tra token bác sĩ và phân công ca trực;
:Cập nhật Appointment.Status = InProgress;
:Ghi vết trạng thái;

|Doctor|
:Thực hiện thăm khám cho bệnh nhân;
:Nhập triệu chứng ban đầu;
:Nhập kết quả khám lâm sàng;
:Nhập chẩn đoán sơ bộ và hướng dẫn điều trị / dặn dò;
:Bấm Hoàn tất khám;

|Backend - ASP.NET Core|
:Validate dữ liệu chuyên môn;
:Lưu bản ghi MedicalNotes / Consultation;
:Cập nhật Appointment.Status = Completed;
:Ghi thời gian hoàn tất ca khám;
|Doctor|
:Thông báo ca khám hoàn thành thành công;
stop
@enduml
```
