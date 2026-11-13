# Project Backlog & User Stories

> **Đồng bộ với:** [docs/SPEC.md](file:///d:/Clinic_Appointment_Management_Project/docs/SPEC.md) (Phiên bản SRS 1.2 Baseline)

---

## 1. Hướng dẫn & Quy ước Backlog

- **Định dạng User Story:** `Là một [Đối tượng], tôi muốn [Hành động] để [Lợi ích].`
- **Tiêu chí chấp nhận (Acceptance Criteria):** Kiểm chứng bằng kịch bản `Given - When - Then` hoặc checklist rõ ràng.
- **Mức độ ưu tiên:**
  - `P0`: Tối quan trọng (Must-have cho MVP).
  - `P1`: Quan trọng (Should-have cho bản chạy thử nghiệm).
  - `P2`: Nâng cao (Nice-to-have).

---

## 2. Danh sách User Stories chi tiết

### Epic 1: Hạ tầng & CSDL (Infrastructure & Database)

#### US-01: Khởi tạo Clean Architecture Solution (.NET 8)
- **Ưu tiên:** P0 | **Story Points:** 3
- **Mô tả:** Là một developer, tôi muốn có bộ khung Solution ASP.NET Core theo cấu trúc Clean Architecture (`Domain`, `Application`, `Infrastructure`, `Api`) để bắt đầu phát triển code có tổ chức.
- **Tiêu chí nghiệm thu:**
  - [ ] Solution compile thành công không có lỗi.
  - [ ] Các project tham chiếu đúng thứ tự quy định trong `docs/ARCHITECTURE.md`.
  - [ ] Cấu hình Swagger / OpenAPI hiển thị được khi khởi chạy API.

#### US-02: Thiết lập kết nối PostgreSQL & EF Core Migrations
- **Ưu tiên:** P0 | **Story Points:** 5
- **Mô tả:** Là một developer, tôi muốn cấu hình EF Core với PostgreSQL để tự động hóa việc tạo và cập nhật schema CSDL theo đúng SRS 1.2.
- **Tiêu chí nghiệm thu:**
  - [ ] Cấu hình Fluent API cho tất cả các bảng dữ liệu theo `docs/DATABASE_DESIGN.md`.
  - [ ] Bảng `patients` có ràng buộc `date_of_birth IS NOT NULL OR birth_year IS NOT NULL`.
  - [ ] Chạy lệnh `dotnet ef database update` tạo đầy đủ bảng, bảng `reschedule_requests`, `refunds`, `otp_challenges` và Partial Unique Indexes.

---

### Epic 2: Quản lý Bác sĩ, Chuyên khoa & Dịch vụ

#### US-03: Danh mục Chuyên khoa, Bác sĩ & Dịch vụ (Public Catalog)
- **Ưu tiên:** P0 | **Story Points:** 5
- **Mô tả:** Là một bệnh nhân/khách vãng lai, tôi muốn xem thông tin chuyên khoa, hồ sơ chuyên môn của bác sĩ và bảng giá các dịch vụ khám để đưa ra lựa chọn đặt lịch phù hợp.
- **Tiêu chí nghiệm thu:**
  - [ ] `GET /api/v1/specialties` và `GET /api/v1/specialties/{id}/services` hiển thị thông tin công khai.
  - [ ] `GET /api/v1/doctors` hỗ trợ lọc theo chuyên khoa và tìm kiếm từ khóa.
  - [ ] Không làm lộ dữ liệu nội bộ hoặc lịch sử khám của bệnh nhân khác.

#### US-04: Tính toán Khung giờ Khám Khả dụng (Available Slots Calculation)
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một người đặt lịch, tôi muốn chọn ngày và dịch vụ khám để thấy ngay các khung giờ khả dụng theo thời gian thực của bác sĩ.
- **Tiêu chí nghiệm thu:**
  - [ ] Sinh slot dựa vào lịch làm việc định kỳ và thời lượng dịch vụ (`slot_duration_minutes`).
  - [ ] Loại trừ các khoảng thời gian bác sĩ nghỉ phép / bận (`doctor_unavailabilities`).
  - [ ] Loại trừ các slot đã có lịch hẹn chưa bị hủy (`status NOT IN ('Cancelled', 'NoShow')`).
  - [ ] Slot cũ của các lịch hẹn đang có yêu cầu đổi lịch `PendingReview` vẫn được tính là đã có người đặt.
  - [ ] Thời gian phản hồi API < 250ms cho tải trung bình.

---

### Epic 3: Động cơ Đặt lịch & Chống Trùng lặp (Booking Engine)

#### US-05: Đặt lịch khám cho Khách vãng lai (Guest Booking)
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một khách vãng lai, tôi muốn đặt lịch khám nhanh chóng mà không cần đăng nhập hay tạo tài khoản người dùng.
- **Tiêu chí nghiệm thu:**
  - [ ] Endpoint `POST /api/v1/appointments/guest` tiếp nhận `fullName`, `phoneNumber`, cùng ít nhất một trong hai trường `dateOfBirth` hoặc `birthYear`.
  - [ ] Tạo bản ghi `patients` với `user_id = null`, tuyệt đối không tự ý tạo tài khoản `users`.
  - [ ] Sinh mã `booking_code` duy nhất, khó đoán, không chứa PII (VD: `APT-20261113-X9Y2`).
  - [ ] Chụp lại snapshot dịch vụ: `service_name_snapshot`, `service_duration_snapshot`, `service_price_snapshot`.
  - [ ] Trạng thái khởi tạo của lịch hẹn là `PendingConfirmation`.

#### US-06: Chống Đặt trùng Slot (Zero Double Booking Concurrency)
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một kỹ sư hệ thống, tôi muốn đảm bảo không bao giờ có 2 người đặt thành công cùng 1 slot của bác sĩ tại một thời điểm.
- **Tiêu chí nghiệm thu:**
  - [ ] Kiểm tra tính khả dụng trong transaction CSDL kết hợp Unique Constraint `idx_unique_doctor_active_slot`.
  - [ ] Test giả lập 10 requests đồng thời chỉ cho phép đúng 1 request thành công, 9 requests còn lại nhận 409 Conflict.

---

### Epic 4: Xác thực OTP cho Khách vãng lai (OTP Management)

#### US-07: Xác thực OTP Scoped theo Mục đích
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một khách vãng lai, tôi muốn nhận mã OTP về số điện thoại đã đặt lịch để thực hiện tra cứu, đổi lịch hoặc hủy lịch an toàn.
- **Tiêu chí nghiệm thu:**
  - [ ] `POST /api/v1/otp/send` nhận `phoneNumber`, `bookingCode` và `purpose` (`LOOKUP`, `RESCHEDULE`, `CANCEL`).
  - [ ] Mã OTP là dùng 1 lần, hết hạn sau 5 phút, tối đa 5 lần nhập sai.
  - [ ] `POST /api/v1/otp/verify` trả về token `otpChallengeToken` chỉ có hiệu lực cho đúng `bookingCode` và `purpose`.

#### US-08: Tra cứu Lịch hẹn bằng BookingCode và OTP
- **Ưu tiên:** P0 | **Story Points:** 5
- **Mô tả:** Là một khách vãng lai, tôi muốn tra cứu trạng thái và thông tin lịch hẹn của mình mà không bị lộ dữ liệu nhạy cảm ra ngoài internet.
- **Tiêu chí nghiệm thu:**
  - [ ] Yêu cầu cung cấp `bookingCode` và xác thực qua OTP (`purpose = LOOKUP`).
  - [ ] Chỉ hiển thị thông tin tóm tắt: Bác sĩ, chuyên khoa, ngày giờ khám, trạng thái; không hiển thị bệnh án chi tiết hay định danh đầy đủ.

---

### Epic 5: Quy trình Đổi lịch Khám (Reschedule Workflow)

#### US-09: Gửi Yêu cầu Đổi lịch & Phê duyệt của Lễ tân
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một người bệnh, tôi muốn gửi yêu cầu đề xuất ngày/giờ mới cho lịch hẹn để được phòng khám sắp xếp lại.
- **Tiêu chí nghiệm thu:**
  - [ ] Người bệnh gửi yêu cầu đổi lịch (sau OTP/session); tạo bản ghi `RescheduleRequest` trạng thái `PendingReview`.
  - [ ] **Lịch cũ và slot cũ giữ nguyên vẹn** trong suốt thời gian chờ duyệt.
  - [ ] Lễ tân xem danh sách yêu cầu đổi lịch và bấm Duyệt hoặc Từ chối kèm lý do.
  - [ ] Khi duyệt, hệ thống kiểm tra lại slot mới trong transaction. Nếu slot mới còn trống: đổi thành công và giải phóng slot cũ. Nếu slot mới đã bận: từ chối đổi và bảo toàn slot cũ.

---

### Epic 6: Hủy lịch & Hoàn phí theo Mốc 60 phút (Cancellation & Refund)

#### US-10: Bệnh nhân Tự Hủy lịch & Tính Hoàn phí Tự động
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một người bệnh, tôi muốn chủ động hủy lịch trước giờ khám mà không cần chờ nhân viên duyệt, đồng thời được tính toán mức hoàn phí minh bạch.
- **Tiêu chí nghiệm thu:**
  - [ ] Người bệnh hủy lịch sau khi xác thực OTP (`purpose = CANCEL`) hoặc đăng nhập. **Không cần lễ tân duyệt**.
  - [ ] Backend tính toán thời gian theo `Server Timestamp (UTC)`:
    - Cách giờ khám $\ge 60$ phút (gồm đúng mốc 60 phút): Hoàn tiền 100%, phí hủy 0%.
    - Cách giờ khám $< 60$ phút: Hoàn tiền 50%, phí hủy 50%.
    - Tại hoặc sau giờ khám: Bệnh nhân không thể tự hủy.
  - [ ] Lưu bản ghi `Refunds` với đầy đủ số tiền đã trả, tỷ lệ phí hủy, số tiền hoàn và trạng thái.

---

### Epic 7: Quản trị Hồ sơ Bệnh nhân & Chống Trùng lặp

#### US-11: Phát hiện Hồ sơ Nghi trùng lặp & Gộp Hồ sơ Thủ công
- **Ưu tiên:** P1 | **Story Points:** 5
- **Mô tả:** Là một nhân viên lễ tân/admin, tôi muốn phát hiện các hồ sơ bệnh nhân nghi trùng lặp để rà soát và gộp dữ liệu một cách an toàn.
- **Tiêu chí nghiệm thu:**
  - [ ] Trùng khớp Họ tên + Ngày sinh/Năm sinh chỉ gắn cờ `is_possible_duplicate = true`, **tuyệt đối không tự động gộp (No Auto-merge)**.
  - [ ] Lễ tân/Admin xem danh sách nghi trùng, đối soát định danh mạnh (CCCD, Hộ chiếu, BHYT) và thực hiện gộp thủ công.
  - [ ] Ghi log audit đầy đủ: người thực hiện, thời gian, lý do gộp hồ sơ.

---

### Epic 8: Tiếp đón & Thăm khám (Reception & Doctor Workspace)

#### US-12: Xác nhận Lịch hẹn & Check-in tại Quầy
- **Ưu tiên:** P1 | **Story Points:** 5
- **Mô tả:** Là một nhân viên lễ tân, tôi muốn duyệt xác nhận các lịch hẹn mới và thực hiện check-in cho bệnh nhân khi họ có mặt.
- **Tiêu chí nghiệm thu:**
  - [ ] Lễ tân chuyển trạng thái lịch hẹn từ `PendingConfirmation` sang `Confirmed`.
  - [ ] Khi bệnh nhân có mặt, lễ tân bấm Check-in chuyển sang `CheckedIn`.

#### US-13: Bác sĩ Thăm khám & Ghi nhận Kết luận
- **Ưu tiên:** P1 | **Story Points:** 5
- **Mô tả:** Là một bác sĩ, tôi muốn xem danh sách bệnh nhân được phân công, bắt đầu khám và lưu kết luận chuyên môn.
- **Tiêu chí nghiệm thu:**
  - [ ] Bác sĩ chuyển trạng thái từ `CheckedIn` sang `InProgress`.
  - [ ] Nhập thông tin triệu chứng ban đầu, kết quả khám lâm sàng, chẩn đoán sơ bộ và dặn dò.
  - [ ] Hoàn tất ca khám, chuyển trạng thái từ `InProgress` sang `Completed`.
