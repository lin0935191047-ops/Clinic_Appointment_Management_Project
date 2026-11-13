# Project Backlog & User Stories

## 1. Hướng dẫn & Quy ước Backlog

- **Định dạng User Story:** `Là một [Đối tượng], tôi muốn [Hành động] để [Lợi ích].`
- **Tiêu chí chấp nhận (Acceptance Criteria):** Kiểm chứng bằng kịch bản `Given - When - Then` hoặc danh sách kiểm tra rõ ràng.
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
- **Mô tả:** Là một developer, tôi muốn cấu hình EF Core với PostgreSQL để tự động hóa việc tạo và cập nhật schema CSDL.
- **Tiêu chí nghiệm thu:**
  - [ ] File `docker-compose.yml` khởi chạy thành công container PostgreSQL 16.
  - [ ] Cấu hình Fluent API cho tất cả các bảng dữ liệu theo `docs/DATABASE_DESIGN.md`.
  - [ ] Chạy lệnh `dotnet ef database update` tạo đầy đủ bảng và Unique Index chống trùng lặp.

---

### Epic 2: Xác thực & Phân quyền (Authentication & RBAC)

#### US-03: Đăng nhập & Cấp phát JWT Access Token
- **Ưu tiên:** P0 | **Story Points:** 5
- **Mô tả:** Là một người dùng (Bác sĩ, Lễ tân, Bệnh nhân), tôi muốn đăng nhập bằng Email/SĐT và Mật khẩu để nhận JWT truy cập các tài nguyên được phân quyền.
- **Tiêu chí nghiệm thu:**
  - [ ] `POST /api/v1/auth/login` kiểm tra thông tin đăng nhập hợp lệ.
  - [ ] Trả về Access Token (JWT) chứa `UserId` và `Role`.
  - [ ] Trả về lỗi 401 nếu sai mật khẩu hoặc tài khoản bị khóa.

---

### Epic 3: Quản lý Bác sĩ & Lịch làm việc (Doctors & Schedules)

#### US-04: Quản lý Chuyên khoa & Hồ sơ Bác sĩ
- **Ưu tiên:** P0 | **Story Points:** 5
- **Mô tả:** Là một bệnh nhân, tôi muốn xem danh sách các chuyên khoa và danh bạ bác sĩ tương ứng để lựa chọn bác sĩ phù hợp với tình trạng sức khỏe.
- **Tiêu chí nghiệm thu:**
  - [ ] API `GET /api/v1/specialties` trả về danh sách chuyên khoa đang hoạt động.
  - [ ] API `GET /api/v1/doctors` hỗ trợ lọc theo `specialtyId` và tìm kiếm theo tên.
  - [ ] Hiển thị đầy đủ học hàm/học vị, giá khám và thông tin giới thiệu của bác sĩ.

#### US-05: Tính toán Khung giờ khám còn trống (Available Slots)
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một người đặt lịch, tôi muốn chọn một ngày cụ thể và thấy ngay các khung giờ khám còn trống của bác sĩ để lựa chọn.
- **Tiêu chí nghiệm thu:**
  - [ ] API `GET /api/v1/doctors/{id}/available-slots?date=YYYY-MM-DD` tính toán các khung giờ dựa vào ca làm việc định kỳ trong tuần của bác sĩ.
  - [ ] Tự động loại trừ các slot rơi vào ngày/giờ bác sĩ nghỉ phép (`doctor_unavailabilities`).
  - [ ] Tự động loại trừ các slot đã có lịch hẹn ở trạng thái `CONFIRMED` hoặc `PENDING`.
  - [ ] Thời gian phản hồi của API < 250ms.

---

### Epic 4: Động cơ Đặt lịch & Chống Trùng lặp (Booking Engine)

#### US-06: Đặt lịch khám cho Khách vãng lai (Guest Booking)
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một khách vãng lai không có tài khoản, tôi muốn đặt lịch khám nhanh chóng chỉ bằng cách điền Họ tên, Số điện thoại và triệu chứng.
- **Tiêu chí nghiệm thu:**
  - [ ] Endpoint `POST /api/v1/appointments/guest` xác thực dữ liệu đầu vào theo `docs/BUSINESS_RULES.md`.
  - [ ] Tạo bản ghi `patients` (với `user_id = null`) và bản ghi `appointments`.
  - [ ] Sinh mã lịch hẹn ngẫu nhiên không trùng lặp (VD: `APT-20261113-X9Y2`).
  - [ ] Trả về mã lịch hẹn để người dùng tra cứu về sau.

#### US-07: Chống Đặt trùng Slot (Anti-Double Booking Concurrency)
- **Ưu tiên:** P0 | **Story Points:** 8
- **Mô tả:** Là một nhà phát triển hệ thống, tôi muốn đảm bảo không bao giờ có 2 người đặt thành công cùng 1 slot của bác sĩ ngay cả khi gửi request cùng 1 tích tắc.
- **Tiêu chí nghiệm thu:**
  - [ ] Có Unit/Integration test kiểm tra 10 luồng đồng thời gửi request cùng 1 slot.
  - [ ] CSDL có Unique Constraint bảo vệ ở tầng thấp nhất.
  - [ ] Request bị trùng slot trả về mã lỗi HTTP 409 Conflict với thông báo người dùng rõ ràng.

---

### Epic 5: Tiếp đón & Check-in (Receptionist Portal)

#### US-08: Tra cứu và Check-in bệnh nhân tại quầy
- **Ưu tiên:** P1 | **Story Points:** 5
- **Mô tả:** Là một nhân viên lễ tân, tôi muốn tra cứu lịch hẹn theo Mã lịch hẹn hoặc Số điện thoại để nhấn Check-in khi bệnh nhân có mặt tại phòng khám.
- **Tiêu chí nghiệm thu:**
  - [ ] Màn hình danh sách hàng đợi theo ngày tự động cập nhật.
  - [ ] Nhấn nút Check-in chuyển trạng thái lịch hẹn từ `CONFIRMED` sang `CHECKED_IN`.
  - [ ] Ghi nhận thời điểm check-in thực tế.

