# Testing Strategy (Chiến lược Kiểm thử)

## 1. Kim tự tháp Kiểm thử (Testing Pyramid)

Hệ thống áp dụng chiến lược kiểm thử phân tầng nhằm tối ưu tốc độ phản hồi và độ tin cậy:

```text
       /\
      /  \      E2E Tests (10%) - Playwright (Luồng khám then chốt)
     /----\
    /      \    Integration Tests (20%) - WebApplicationFactory + Testcontainers PostgreSQL
   /--------\
  /          \  Unit Tests (70%) - xUnit, FluentAssertions, Moq, Vitest
 /------------\
```

---

## 2. Kiểm thử Backend (.NET / C#)

### 2.1 Unit Tests (`ClinicAppointment.Domain.Tests` & `Application.Tests`)
- **Mục tiêu:** Kiểm tra các hàm logic độc lập, không phụ thuộc I/O hay CSDL.
- **Thư viện:** `xUnit`, `FluentAssertions`, `Moq` (hoặc `NSubstitute`).
- **Các ca kiểm thử trọng tâm:**
  - Thuật toán tạo slot khám từ lịch định kỳ (`DoctorSchedule.GenerateSlots`).
  - Logic xác định xung đột lịch khám (Overlapping time range).
  - Quy tắc xác thực dữ liệu đầu vào bằng `FluentValidation`.
  - Quy tắc chuyển đổi trạng thái (State machine transition rules).

### 2.2 Integration Tests (`ClinicAppointment.Api.IntegrationTests`)
- **Mục tiêu:** Kiểm thử trọn vẹn luồng từ HTTP Controller qua Application Handler đến CSDL PostgreSQL thật.
- **Công nghệ:**
  - `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`).
  - `Testcontainers.PostgreSql`: Tự động khởi chạy container PostgreSQL cô lập trên Docker cho mỗi đợt chạy test, loại bỏ nguy cơ phụ thuộc dữ liệu tĩnh.
- **Ca kiểm thử đồng thời (Concurrency Test - Bắt buộc):**
  - Giả lập 10 requests gửi đồng thời đến endpoint `POST /api/v1/appointments` để đặt cùng một bác sĩ trong cùng một slot `09:00 - 09:30`.
  - **Kỳ vọng:** Đúng 1 request thành công (`201 Created` hoặc `200 OK`), 9 requests còn lại trả về lỗi xung đột (`409 Conflict` hoặc validation error).

---

## 3. Kiểm thử Frontend (React.js)

### 3.1 Component & Hook Tests
- **Thư viện:** `Vitest` (hoặc `Jest`), `React Testing Library`, `MSW (Mock Service Worker)`.
- **Trọng tâm:**
  - Quy trình từng bước của wizard đặt lịch (Chọn Chuyên khoa -> Chọn Bác sĩ -> Chọn Ngày giờ -> Nhập thông tin).
  - Validation form đặt lịch (kiểm tra định dạng SĐT, thông báo khi để trống trường bắt buộc).
  - Render trạng thái Loading, Empty state và Error boundary.

### 3.2 End-to-End (E2E) Tests
- **Thư viện:** `Playwright`.
- **Kịch bản E2E chính:**
  1. **Luồng Khách vãng lai (Guest Booking Flow):** Truy cập trang chủ -> Tìm bác sĩ Tim mạch -> Chọn slot trống -> Nhập form khách -> Nhận mã đặt lịch thành công.
  2. **Luồng Tiếp đón (Reception Check-in):** Lễ tân đăng nhập -> Mở danh sách hàng đợi -> Tìm kiếm theo mã hẹn -> Nhấn nút Check-in -> Trạng thái đổi thành `CHECKED_IN`.

---

## 4. Chuẩn mực & Tự động hóa (Automation & Standards)

- **Nguyên tắc AAA (Arrange - Act - Assert):** Mọi test case phải tuân thủ rõ ràng 3 phân đoạn.
- **Chạy tự động (CI Pipeline):** Toàn bộ Unit & Integration test phải pass 100% trước khi merge Pull Request vào nhánh `main` hoặc `develop`.
- **Mục tiêu độ bao phủ (Code Coverage):** Tối thiểu 80% coverage đối với Domain Layer và Business Logic Handlers.

