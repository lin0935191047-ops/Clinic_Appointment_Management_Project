# Clinic Appointment Management System — Use Case Specification

- **Phiên bản:** 1.2
- **Căn cứ trên:** [docs/SPEC.md](SPEC.md) (SRS Version 1.2 Baseline)
- **Trạng thái:** Baseline nghiệp vụ theo các quyết định đã xác nhận
- **Ngôn ngữ:** Tiếng Việt (các định danh kỹ thuật giữ tiếng Anh để đồng bộ mã nguồn)

---

## 1. Mục đích

Tài liệu này mô tả chi tiết các tác nhân (Actors) và Use Case chính của hệ thống Clinic Appointment Management System. Đây là cơ sở cho Activity Diagrams, thiết kế CSDL (ERD), đặc tả API và kịch bản kiểm thử (Test Cases).

**Quy tắc nghiệp vụ cốt lõi đã xác nhận:**
- Khách vãng lai (Guest) được xem danh bạ bác sĩ, dịch vụ, kiểm tra khung giờ trống và đặt lịch khám mà không cần tài khoản hay đăng nhập.
- Hồ sơ bệnh nhân (`Patient`) tồn tại độc lập với tài khoản người dùng (`User`). Trường `UserId` có thể nhận giá trị `NULL`.
- Thao tác Tra cứu, Đổi lịch và Hủy lịch của Guest bắt buộc phải có mã OTP gửi về số điện thoại theo đúng mục đích (`LOOKUP`, `RESCHEDULE`, `CANCEL`).
- Bệnh nhân có quyền chủ động hủy lịch trước giờ khám mà **không cần nhân viên lễ tân duyệt**. Mức hoàn phí tự động tính theo mốc 60 phút.
- Thao tác đổi lịch phải qua phê duyệt của Lễ tân; slot cũ của lịch hẹn được bảo toàn nguyên vẹn cho đến khi duyệt thành công.

---

## 2. Tác nhân (Actors)

| Actor | Mô tả | Quyền hạn / Phạm vi chính |
| :--- | :--- | :--- |
| **Guest** | Người dùng chưa đăng nhập | Xem thông tin công khai, kiểm tra khung giờ trống, đặt lịch không cần tài khoản; tra cứu, yêu cầu đổi lịch và hủy lịch sau khi xác thực OTP. |
| **Patient** | Bệnh nhân có tài khoản tùy chọn | Đầy đủ quyền của Patient trong phạm vi tài khoản của mình; quản lý hồ sơ và xem lịch sử khám của bản thân. |
| **Receptionist** | Nhân viên lễ tân phòng khám | Quản lý hồ sơ bệnh nhân, xác nhận lịch mới (`PendingConfirmation`), phê duyệt yêu cầu đổi lịch, check-in bệnh nhân và quản lý hàng đợi. |
| **Doctor** | Bác sĩ khám bệnh | Xem danh sách lịch hẹn được phân công, xem dữ liệu y tế cần thiết, bắt đầu khám và ghi nhận kết quả khám bệnh. |
| **Admin** | Quản trị viên hệ thống | Quản trị người dùng, vai trò, hồ sơ bác sĩ, danh mục chuyên khoa, dịch vụ, cấu hình ca trực/ngày nghỉ và xem báo cáo thống kê. |

---

## 3. Danh mục Use Case

| ID | Tên Use Case | Actor chính | Phân loại |
| :--- | :--- | :--- | :--- |
| **UC-01** | Đăng ký tài khoản tùy chọn | Guest | Authentication |
| **UC-02** | Đăng nhập & Đăng xuất | Patient, Receptionist, Doctor, Admin | Authentication |
| **UC-03** | Xem danh mục chuyên khoa | Guest, Patient | Public |
| **UC-04** | Tìm kiếm / xem hồ sơ bác sĩ | Guest, Patient | Public |
| **UC-05** | Xem khung giờ khám còn trống | Guest, Patient | Public |
| **UC-06** | Đặt lịch khám không cần đăng nhập | Guest, Patient | Public booking |
| **UC-07** | Tra cứu lịch bằng mã đặt lịch & OTP | Guest, Patient | Public + OTP |
| **UC-08** | Gửi yêu cầu đổi lịch khám | Guest, Patient | Public + OTP / Authenticated |
| **UC-09** | Bệnh nhân chủ động hủy lịch & nhận hoàn phí | Guest, Patient | Public + OTP / Authenticated |
| **UC-10** | Quản lý hồ sơ bác sĩ | Admin | Admin |
| **UC-11** | Quản lý chuyên khoa & dịch vụ | Admin | Admin |
| **UC-12** | Quản lý lịch làm việc & ngày nghỉ | Admin, Receptionist (được cấp quyền) | Admin/Staff |
| **UC-13** | Xác nhận lịch hẹn mới (`PendingConfirmation`) | Receptionist | Staff |
| **UC-14** | Check-in bệnh nhân đến khám | Receptionist | Staff |
| **UC-15** | Quản lý hàng đợi khám bệnh | Receptionist, Doctor | Staff |
| **UC-16** | Bắt đầu ca khám (`CheckedIn` -> `InProgress`) | Doctor | Staff |
| **UC-17** | Ghi nhận kết quả khám lâm sàng | Doctor | Staff |
| **UC-18** | Hoàn tất ca khám (`InProgress` -> `Completed`) | Doctor | Staff |
| **UC-19** | Xem báo cáo & thống kê quản trị | Admin | Admin |
| **UC-20** | Quản lý người dùng và phân quyền vai trò | Admin | Admin |
| **UC-21** | Quản lý hồ sơ cá nhân | Patient | Authenticated |
| **UC-22** | Xem lịch sử khám của bản thân | Patient | Authenticated |
| **UC-23** | Quản lý & rà soát hồ sơ nghi trùng lặp | Receptionist, Admin | Staff |
| **UC-24** | Tiếp nhận & tạo lịch tại quầy (Walk-in) | Receptionist | Staff |
| **UC-25** | Xem lịch được phân công trong ngày | Doctor | Staff |
| **UC-26** | Phê duyệt / Từ chối yêu cầu đổi lịch | Receptionist | Staff |

---

## 4. Đặc tả các Use Case cốt lõi

### UC-06 – Đặt lịch khám không cần đăng nhập (Guest Booking)
- **Actor chính:** Guest.
- **Tiền điều kiện:** Bác sĩ và dịch vụ đang hoạt động; khung giờ khám còn trống; hệ thống tiếp nhận request.
- **Dữ liệu bắt buộc:** `FullName`, `PhoneNumber`, cùng ít nhất một trong hai trường `DateOfBirth` hoặc `BirthYear`; `DoctorId`, `ServiceId`, `Date`, `StartTime`.
- **Luồng chính:**
  1. Guest chọn Chuyên khoa, Bác sĩ và Dịch vụ khám.
  2. Guest chọn Ngày khám và Khung giờ còn trống.
  3. Guest nhập thông tin bệnh nhân: Họ tên, Ngày sinh hoặc Năm sinh, Số điện thoại liên hệ, Triệu chứng.
  4. Guest kiểm tra lại thông tin và bấm Xác nhận.
  5. Backend thực hiện Atomic check: validate thông tin, kiểm tra tính khả dụng của slot trong CSDL transaction.
  6. Backend tạo bản ghi `Patient` (với `UserId = NULL`) nếu chưa có hồ sơ đã xác minh; gắn cờ nghi trùng nếu cần rà soát; **tuyệt đối không tự động tạo User**.
  7. Backend sinh mã `BookingCode` duy nhất khó đoán (không chứa PII) và chụp snapshot dịch vụ (tên, thời lượng, giá).
  8. Backend tạo bản ghi `Appointment` ở trạng thái `PendingConfirmation`.
  9. Hệ thống trả về thông tin xác nhận và mã `BookingCode` cho Guest.
- **Ngoại lệ:**
  - *Slot bị trùng (Double-booking):* Bắn lỗi 409 Conflict, yêu cầu chọn khung giờ khác.
  - *Dữ liệu thiếu trường bắt buộc (thiếu cả DOB và BirthYear):* Bắn lỗi 400 Validation.

### UC-07 – Tra cứu lịch hẹn bằng BookingCode và OTP
- **Actor chính:** Guest.
- **Tiền điều kiện:** Người dùng có mã `BookingCode` và số điện thoại đã đăng ký lịch.
- **Luồng chính:**
  1. Guest nhập `BookingCode` và yêu cầu gửi mã xác thực OTP.
  2. Backend sinh mã OTP với `Purpose = LOOKUP`, thời hạn 5 phút, gửi về số điện thoại.
  3. Guest nhập mã OTP; Backend xác minh mã, số lần thử và tính hợp lệ.
  4. Backend cấp quyền tạm thời, trả về thông tin tóm tắt lịch hẹn (bác sĩ, dịch vụ, ngày giờ, trạng thái).
- **Quy tắc an toàn:** Không hiển thị đầy đủ thông tin bệnh án chi tiết hay định danh đầy đủ ra màn hình tra cứu.

### UC-08 & UC-26 – Quy trình Đổi lịch khám (Reschedule)
- **Actor chính:** Guest (sau OTP `Purpose = RESCHEDULE`) hoặc Patient đăng nhập; Lễ tân phê duyệt.
- **Luồng chính:**
  1. Bệnh nhân gửi yêu cầu đổi lịch với ngày và khung giờ mới đề xuất.
  2. Backend kiểm tra tính hợp lệ và tạo bản ghi `RescheduleRequest` ở trạng thái `PendingReview`.
  3. **Lịch hẹn cũ và slot cũ của bệnh nhân vẫn được giữ nguyên vẹn**.
  4. Nhân viên lễ tân mở danh sách yêu cầu đổi lịch, rà soát và bấm Duyệt (`Approve`) hoặc Từ chối (`Reject`).
  5. Nếu lễ tân bấm Duyệt: Backend kiểm tra lại slot mới trong transaction. Nếu slot mới còn trống -> đổi lịch thành công, giải phóng slot cũ, chuyển request thành `Approved`. Nếu slot mới đã bị chiếm -> báo lỗi xung đột và giữ nguyên slot cũ.

### UC-09 – Bệnh nhân chủ động hủy lịch & Nhận hoàn phí
- **Actor chính:** Guest (sau OTP `Purpose = CANCEL`) hoặc Patient đăng nhập.
- **Tiền điều kiện:** Lịch hẹn chưa bắt đầu (`StartAt > now`).
- **Luồng chính:**
  1. Bệnh nhân yêu cầu hủy lịch; nhập lý do hủy.
  2. Backend ghi nhận thời điểm hủy theo dấu thời gian máy chủ (Server timestamp - UTC `CancellationAcceptedAt`).
  3. **Không cần nhân viên lễ tân phê duyệt**.
  4. Backend tính toán hoàn phí tự động theo công thức chuẩn:
     - $\ge 60$ phút: Hoàn 100%, phí hủy 0%.
     - $< 60$ phút: Hoàn 50%, phí hủy 50%.
  5. Cập nhật trạng thái `Appointment` sang `Cancelled`, tạo bản ghi `Refunds` và giải phóng khung giờ khám.
- **Ngoại lệ:** Ca khám đã đến giờ bắt đầu hoặc đã qua giờ -> Hệ thống từ chối luồng tự hủy của bệnh nhân.
