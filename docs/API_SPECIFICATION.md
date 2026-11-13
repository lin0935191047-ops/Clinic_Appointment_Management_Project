# RESTful API Specification

> **Đồng bộ với:** [docs/SPEC.md](SPEC.md) (Phiên bản SRS 1.2 Baseline)

---

## 1. Tiêu chuẩn thiết kế API (API Standards)

- **Base URL:** `/api/v1`
- **Giao thức:** HTTPS, dữ liệu trao đổi định dạng JSON (UTF-8).
- **Quy chuẩn URL:** Chữ thường, phân cách bằng dấu gạch ngang (`kebab-case`), danh từ số nhiều (ví dụ `/api/v1/specialties`, `/api/v1/appointments`).
- **Xác thực:** 
  - Người dùng đăng nhập: Bearer Token (JWT) trong HTTP Header: `Authorization: Bearer <access_token>`.
  - Khách vãng lai thao tác nhạy cảm: `X-OTP-Challenge-Token: <challenge_token>` (cấp tạm thời sau khi verify OTP thành công theo đúng `purpose`).
- **Kiểm tra quyền hạn:** Toàn bộ authorization bắt buộc kiểm tra tại Backend API; không tin cậy việc ẩn/hiện nút bấm trên giao diện.

### 1.1 Cấu trúc phản hồi chuẩn (Response Envelope)

Phản hồi thành công (Success Response):
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Thao tác thành công.",
  "data": { ... }
}
```

Phản hồi có phân trang (Paginated Response):
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Truy vấn dữ liệu thành công.",
  "data": {
    "items": [ ... ],
    "pageIndex": 1,
    "pageSize": 20,
    "totalCount": 150,
    "totalPages": 8,
    "hasNextPage": true,
    "hasPreviousPage": false
  }
}
```

Phản hồi lỗi (Error Response):
```json
{
  "success": false,
  "statusCode": 400,
  "message": "Dữ liệu yêu cầu không hợp lệ.",
  "errors": [
    {
      "field": "startTime",
      "message": "Khung giờ đã có người đặt, vui lòng chọn khung giờ khác."
    }
  ]
}
```

---

## 2. Danh mục Endpoints chi tiết

### 2.1 Danh mục Công khai & Chuyên khoa & Dịch vụ (`/api/v1/public`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/specialties` | Public | Danh sách chuyên khoa đang hoạt động (FR-002) |
| `GET` | `/api/v1/specialties/{id}/services` | Public | Danh sách dịch vụ khám, thời lượng, giá niêm yết (FR-004) |
| `GET` | `/api/v1/doctors` | Public | Danh bạ bác sĩ (lọc theo chuyên khoa, từ khóa) (FR-003) |
| `GET` | `/api/v1/doctors/{id}` | Public | Hồ sơ chuyên môn công khai của bác sĩ |
| `GET` | `/api/v1/doctors/{id}/available-slots` | Public | Tính toán các khung giờ trống (`?date=YYYY-MM-DD&serviceId=...`) (FR-005) |

### 2.2 Xác thực OTP cho Guest (`/api/v1/otp`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/otp/send` | Public | Gửi mã OTP về số điện thoại theo `bookingCode` và `purpose` (`LOOKUP`, `RESCHEDULE`, `CANCEL`) (FR-026, FR-027) |
| `POST` | `/api/v1/otp/verify` | Public | Xác thực OTP một lần (FR-028). Thành công trả về `otpChallengeToken` có giới hạn phạm vi (FR-029) |

### 2.3 Đặt lịch & Quản lý Lịch hẹn (`/api/v1/appointments`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/appointments/guest` | Public | **Guest Booking:** Đặt lịch không cần tài khoản, bắt buộc `fullName` + (`dateOfBirth` hoặc `birthYear`), `phoneNumber`, `doctorId`, `serviceId`, `date`, `startTime`. Sinh `bookingCode` duy nhất và lưu snapshot dịch vụ (FR-018 - FR-025). Trạng thái khởi tạo: `PendingConfirmation`. |
| `POST` | `/api/v1/appointments` | Patient | Bệnh nhân đã đăng nhập đặt lịch khám (FR-013) |
| `POST` | `/api/v1/appointments/lookup` | Public / OTP | Tra cứu lịch hẹn bằng `bookingCode` + xác thực `otpChallengeToken` (`purpose = LOOKUP`). Ẩn bớt PII và không lộ bệnh án chi tiết (FR-030). |
| `GET` | `/api/v1/appointments/my-history` | Patient | Xem lịch sử và lịch sắp tới của chính mình (FR-033) |
| `GET` | `/api/v1/appointments/{id}` | Authenticated / OTP | Chi tiết thông tin lịch hẹn |
| `POST` | `/api/v1/appointments/{id}/cancel` | Patient / OTP | **Bệnh nhân chủ động hủy lịch:** Sau xác thực OTP (`purpose = CANCEL`) hoặc đăng nhập. **Không cần lễ tân duyệt**. Tính toán hoàn phí tự động theo mốc thời gian máy chủ (FR-043 - FR-050). |

### 2.4 Đổi lịch khám (Reschedule Workflow) (`/api/v1/reschedule-requests`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/appointments/{id}/reschedule-requests` | Patient / OTP | Bệnh nhân gửi yêu cầu đề xuất ngày/giờ mới sau khi xác thực OTP (`purpose = RESCHEDULE`). Tạo trạng thái `PendingReview`. **Slot cũ giữ nguyên** (FR-036 - FR-042). |
| `GET` | `/api/v1/reception/reschedule-requests` | Receptionist | Lễ tân xem danh sách các yêu cầu đổi lịch đang chờ duyệt (`PendingReview`) |
| `POST` | `/api/v1/reception/reschedule-requests/{id}/approve` | Receptionist | **Lễ tân duyệt đổi lịch:** Kiểm tra lại slot mới trong transaction. Nếu còn trống: chuyển sang `Approved`, giải phóng slot cũ, đổi lịch thành công (FR-039, FR-041). |
| `POST` | `/api/v1/reception/reschedule-requests/{id}/reject` | Receptionist | Lễ tân từ chối yêu cầu đổi lịch (ghi rõ lý do); slot cũ tiếp tục được giữ nguyên. |

### 2.5 Lễ tân & Tiếp đón (`/api/v1/reception`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/reception/daily-queue` | Receptionist | Xem hàng đợi khám trong ngày (`?date=...&status=...`) (FR-058) |
| `POST` | `/api/v1/reception/appointments/{id}/confirm` | Receptionist | Xác nhận lịch hẹn mới (`PendingConfirmation` -> `Confirmed`) (FR-032) |
| `POST` | `/api/v1/reception/appointments/{id}/check-in` | Receptionist | Check-in bệnh nhân có mặt tại quầy (`Confirmed` -> `CheckedIn`) (FR-057) |
| `GET` | `/api/v1/reception/duplicate-candidates` | Receptionist | Rà soát danh sách hồ sơ bệnh nhân nghi ngờ trùng lặp (FR-015, FR-016) |
| `POST` | `/api/v1/reception/patients/merge` | Receptionist/Admin | Gộp hồ sơ thủ công kèm ghi nhận lý do và audit trail (FR-017) |

### 2.6 Bác sĩ Thăm khám (`/api/v1/doctor-workspace`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/doctor-workspace/appointments` | Doctor | Danh sách lịch khám trong ca được phân công (FR-059, FR-066) |
| `POST` | `/api/v1/doctor-workspace/appointments/{id}/start` | Doctor | Bắt đầu thăm khám (`CheckedIn` -> `InProgress`) (FR-060) |
| `POST` | `/api/v1/doctor-workspace/appointments/{id}/complete`| Doctor | Lưu triệu chứng, khám lâm sàng, chẩn đoán, dặn dò và hoàn tất ca khám (`InProgress` -> `Completed`) (FR-061, FR-062) |

### 2.7 Quản trị & Báo cáo (`/api/v1/admin`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/admin/dashboard` | Admin | Bảng điều khiển tổng quan thống kê lịch, bác sĩ, bệnh nhân (FR-065) |
| `GET` | `/api/v1/admin/reports` | Admin | Báo cáo chi tiết lọc theo thời gian, chuyên khoa, trạng thái (FR-067) |
| `POST` | `/api/v1/admin/doctors/{id}/schedules` | Admin | Cấu hình lịch làm việc định kỳ (FR-052) |
| `POST` | `/api/v1/admin/doctors/{id}/unavailabilities`| Admin | Đăng ký ngày nghỉ / giờ bận đột xuất (FR-053) |
| `GET` | `/api/v1/admin/audit-logs` | Admin | Tra cứu nhật ký kiểm toán hệ thống |
