# RESTful API Specification

## 1. Tiêu chuẩn thiết kế API (API Standards)

- **Base URL:** `/api/v1`
- **Giao thức:** HTTPS, dữ liệu trao đổi định dạng JSON (UTF-8).
- **Quy chuẩn URL:** Chữ thường, phân cách bằng dấu gạch ngang (`kebab-case`), danh từ số nhiều (ví dụ `/api/v1/specialties`, `/api/v1/appointments`).
- **Xác thực:** Bearer Token (JWT) trong HTTP Header: `Authorization: Bearer <access_token>`.

### 1.1 Cấu trúc phản hồi chuẩn (Response Envelope)

Phản hồi thành công (Success Response):
```json
{
  "success": true,
  "statusCode": 200,
  "message": "Lấy danh sách thành công.",
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

Phản hồi lỗi (Error Response - RFC 7807 Problem Details hoặc Envelope):
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

## 2. Danh sách Endpoints chính (Core Endpoints)

### 2.1 Authentication & Profile (`/api/v1/auth`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/auth/login` | Public | Đăng nhập tài khoản bằng Email/Phone + Mật khẩu |
| `POST` | `/api/v1/auth/register` | Public | Đăng ký tài khoản bệnh nhân mới |
| `POST` | `/api/v1/auth/refresh-token` | Public | Làm mới Access Token bằng Refresh Token |
| `POST` | `/api/v1/auth/logout` | Authenticated | Đăng xuất và thu hồi Refresh Token |
| `GET` | `/api/v1/auth/me` | Authenticated | Lấy thông tin tài khoản đang đăng nhập |

### 2.2 Chuyên khoa (`/api/v1/specialties`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/specialties` | Public | Lấy danh sách các chuyên khoa khám đang hoạt động |
| `GET` | `/api/v1/specialties/{id}` | Public | Lấy thông tin chi tiết một chuyên khoa |
| `POST` | `/api/v1/specialties` | Admin | Thêm mới chuyên khoa |
| `PUT` | `/api/v1/specialties/{id}` | Admin | Cập nhật thông tin chuyên khoa |

### 2.3 Bác sĩ & Lịch làm việc (`/api/v1/doctors`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/doctors` | Public | Tìm kiếm bác sĩ (hỗ trợ filter: `specialtyId`, `search`, `pageIndex`, `pageSize`) |
| `GET` | `/api/v1/doctors/{id}` | Public | Chi tiết hồ sơ bác sĩ và chuyên môn |
| `GET` | `/api/v1/doctors/{id}/available-slots` | Public | **Tính toán các slot khả dụng trong ngày** (`?date=YYYY-MM-DD`) |
| `POST` | `/api/v1/doctors/{id}/schedules` | Admin/Doctor | Cấu hình ca làm việc định kỳ trong tuần |
| `POST` | `/api/v1/doctors/{id}/unavailabilities`| Admin/Doctor | Đăng ký thời gian nghỉ phép/không khả dụng |

### 2.4 Đặt lịch & Quản lý Lịch hẹn (`/api/v1/appointments`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `POST` | `/api/v1/appointments/guest` | Public | Đặt lịch khám cho khách vãng lai (không đăng nhập) |
| `POST` | `/api/v1/appointments` | Patient | Bệnh nhân đã đăng nhập đặt lịch khám |
| `GET` | `/api/v1/appointments/lookup` | Public | Tra cứu lịch bằng `code` (Mã lịch hẹn) + `phone` |
| `GET` | `/api/v1/appointments/my-history` | Patient | Lấy lịch sử khám và lịch hẹn sắp tới của tôi |
| `GET` | `/api/v1/appointments/{id}` | Authenticated | Chi tiết thông tin lịch hẹn |
| `PATCH`| `/api/v1/appointments/{id}/cancel` | Patient/Admin | Hủy lịch hẹn (kèm lý do) theo quy tắc hủy |

### 2.5 Lễ tân & Hàng đợi (`/api/v1/reception`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/reception/daily-queue` | Receptionist/Admin | Danh sách bệnh nhân hẹn trong ngày (`?date=...&status=...`) |
| `PATCH`| `/api/v1/reception/appointments/{id}/check-in` | Receptionist | Check-in bệnh nhân khi có mặt tại phòng khám |
| `POST` | `/api/v1/reception/walk-in` | Receptionist | Tiếp nhận và đặt lịch trực tiếp cho khách vãng lai tại quầy |

### 2.6 Không gian làm việc Bác sĩ (`/api/v1/doctor-workspace`)

| Method | Endpoint | Quyền hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/doctor-workspace/appointments` | Doctor | Danh sách bệnh nhân của bác sĩ trong ngày |
| `PATCH`| `/api/v1/doctor-workspace/appointments/{id}/start` | Doctor | Bắt đầu thăm khám (`status` -> `IN_PROGRESS`) |
| `POST` | `/api/v1/doctor-workspace/appointments/{id}/complete` | Doctor | Hoàn tất khám bệnh, lưu chẩn đoán sơ bộ & dặn dò |

