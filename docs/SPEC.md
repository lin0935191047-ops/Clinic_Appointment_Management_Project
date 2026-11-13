# Software Requirements Specification (SRS)

## 1. Giới thiệu & Mục tiêu (Introduction & Objectives)

Hệ thống **Clinic Appointment Management** nhằm số hóa toàn bộ quy trình đặt lịch khám chữa bệnh, tối ưu hóa thời gian chờ đợi của bệnh nhân và nâng cao hiệu quả làm việc của đội ngũ y bác sĩ, nhân viên tiếp đón tại phòng khám.

### 1.1 Mục tiêu chính
- Cho phép bệnh nhân và khách vãng lai dễ dàng tra cứu bác sĩ, chuyên khoa và đặt lịch khám theo khung giờ thực tế.
- Tự động hóa việc phân bổ ca khám, loại trừ xung đột thời gian (Double Booking).
- Hỗ trợ nhân viên lễ tân tiếp nhận, check-in và điều phối hàng đợi khám bệnh trực tiếp.
- Cung cấp bảng điều khiển (Dashboard) cho bác sĩ và ban quản lý phòng khám theo dõi hiệu suất hoạt động.

---

## 2. Đối tượng người dùng & Vai trò (User Personas & Roles)

1. **Khách vãng lai (Guest):**
   - Chưa có tài khoản hoặc không muốn đăng nhập.
   - Có thể tra cứu lịch, đặt hẹn nhanh thông qua xác minh số điện thoại / Email (OTP hoặc link xác nhận).
2. **Bệnh nhân có tài khoản (Registered Patient):**
   - Đăng nhập vào hệ thống, quản lý thông tin cá nhân và người thân.
   - Theo dõi lịch sử khám, các lịch hẹn sắp tới, hủy hoặc đổi lịch theo chính sách.
3. **Bác sĩ (Doctor):**
   - Xem lịch hẹn trong ngày/tuần, quản lý danh sách bệnh nhân chờ khám.
   - Thiết lập khung giờ nghỉ phép hoặc thời gian không khả dụng (Unavailable Slots).
   - Cập nhật kết quả tóm tắt hoặc trạng thái sau ca khám.
4. **Nhân viên lễ tân / Tiếp đón (Receptionist / Clinic Staff):**
   - Tiếp đón bệnh nhân đến khám, thực hiện thao tác Check-in.
   - Tạo lịch hẹn trực tiếp (Walk-in Appointment) cho bệnh nhân đến khám không hẹn trước.
   - Điều phối hàng đợi, gọi bệnh nhân vào phòng khám.
5. **Quản trị viên hệ thống (Administrator):**
   - Quản lý danh mục chuyên khoa (Specialties), dịch vụ khám, phòng khám.
   - Quản lý tài khoản, phân quyền nhân viên và hồ sơ bác sĩ.
   - Xem thống kê lượt khám, tỷ lệ hủy lịch, doanh thu và tải hệ thống.

---

## 3. Yêu cầu chức năng (Functional Requirements)

### FR-01: Quản lý Chuyên khoa & Dịch vụ khám
- Thêm, sửa, ẩn danh mục chuyên khoa và các dịch vụ khám tương ứng.
- Gán bác sĩ vào các chuyên khoa chính và phụ.

### FR-02: Quản lý Lịch làm việc của Bác sĩ (Doctor Scheduling)
- Thiết lập lịch định kỳ (Recurring Schedule) theo thứ trong tuần và khung giờ làm việc.
- Cấu hình thời lượng mỗi ca khám (Slot Duration: ví dụ 15, 30 hoặc 45 phút).
- Hỗ trợ tạo ngoại lệ lịch: nghỉ phép, công tác, đột xuất không nhận bệnh nhân.

### FR-03: Động cơ Đặt lịch khám (Appointment Booking Engine)
- Tính toán các slot còn trống (Available Slots) theo thời gian thực dựa trên lịch làm việc của bác sĩ và các lịch hẹn đã được xác nhận.
- Đảm bảo tính nhất quán (ACID/Concurrency Lock): Không cho phép 2 người đặt cùng 1 slot.
- Gửi thông báo xác nhận lịch hẹn (Email/SMS/Web Notification).

### FR-04: Đặt lịch cho Khách vãng lai (Guest Booking)
- Cho phép đặt hẹn không cần tạo mật khẩu: thu thập Họ tên, Số điện thoại, Email, Ngày sinh, Triệu chứng ban đầu.
- Gửi mã xác thực OTP hoặc liên kết xác nhận lịch qua Email/SMS để hoàn tất việc giữ chỗ.

### FR-05: Quản lý Vòng đời Lịch hẹn (Appointment Lifecycle)
- Trạng thái lịch hẹn: `Pending` (Chờ xác nhận) -> `Confirmed` (Đã xác nhận) -> `CheckedIn` (Đã đến phòng khám) -> `InProgress` (Đang khám) -> `Completed` (Đã khám xong) / `Cancelled` (Đã hủy) / `NoShow` (Bỏ hẹn).
- Cho phép bệnh nhân hủy hoặc dời lịch nếu trước thời hạn quy định (ví dụ tối thiểu 2-4 tiếng trước giờ khám).

### FR-06: Tiếp đón & Check-in tại quầy (Reception & Check-in)
- Tra cứu nhanh lịch hẹn bằng Mã lịch hẹn (Appointment Code), Số điện thoại hoặc CCCD/CMND.
- Check-in chuyển bệnh nhân vào hàng đợi thực tế tại phòng khám.
- Đặt lịch khám khẩn cấp hoặc walk-in ngay tại quầy.

### FR-07: Bảng điều khiển Bác sĩ (Doctor Workspace)
- Danh sách bệnh nhân đã check-in, đang đợi khám theo thứ tự số thứ tự.
- Ghi nhận trạng thái hoàn tất ca khám và ghi chú dặn dò.

### FR-08: Báo cáo & Thống kê (Reports & Analytics)
- Báo cáo số lượng lịch hẹn theo ngày/tuần/tháng, tỷ lệ đến đúng hẹn vs bỏ hẹn (No-show Rate).
- Tải làm việc của từng bác sĩ và chuyên khoa được quan tâm nhất.

---

## 4. Yêu cầu phi chức năng (Non-Functional Requirements)

- **Hiệu năng (Performance):** Thời gian phản hồi của API truy vấn slot khả dụng < 250ms cho tải trung bình.
- **Tính toàn vẹn & Chống xung đột (Concurrency Control):** Hệ thống phải kiểm soát chặt chẽ Concurrency (Pessimistic / Optimistic Locking hoặc Distributed Lock) khi chốt slot khám.
- **Bảo mật dữ liệu y tế (Security & Privacy):**
  - Mật khẩu mã hóa bằng thuật toán an toàn (Argon2 / PBKDF2 / BCrypt).
  - Sử dụng HTTPS/TLS cho toàn bộ truyền thông.
  - Phân quyền nghiêm ngặt dựa trên vai trò (RBAC) và hạn chế truy cập dữ liệu bệnh nhân trái phép.
- **Khả năng mở rộng (Scalability):** Thiết kế Stateless API để dễ dàng mở rộng theo chiều ngang (Horizontal Scaling).

