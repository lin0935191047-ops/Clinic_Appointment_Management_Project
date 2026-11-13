# Testing Strategy (Chiến lược Kiểm thử)

> **Đồng bộ với:** [docs/SPEC.md](SPEC.md) (Phiên bản SRS 1.2 Baseline & Tiêu chí nghiệm thu)

---

## 1. Kim tự tháp Kiểm thử (Testing Pyramid)

Hệ thống áp dụng chiến lược kiểm thử phân tầng với tiêu chí kiểm thử dựa trên bằng chứng thực tế (Test Evidence First):

```text
       /\
      /  \      E2E Tests (10%) - Playwright (Luồng Guest Booking, OTP, Reception Check-in)
     /----\
    /      \    Integration Tests (20%) - WebApplicationFactory + Testcontainers PostgreSQL
   /--------\
  /          \  Unit Tests (70%) - xUnit, FluentAssertions, Moq, Vitest
 /------------\
```

---

## 2. Kiểm thử Backend (.NET / C#)

### 2.1 Unit Tests (`ClinicAppointment.Domain.Tests` & `Application.Tests`)
- **Bộ kiểm thử Hoàn tiền theo Mốc thời gian (Refund Calculation Boundary Tests):**
  - Mốc 61 phút trước ca khám: Hoàn tiền 100%, phí hủy 0%.
  - Đúng mốc 60 phút: Hoàn tiền 100%, phí hủy 0%.
  - Mốc 59 phút: Hoàn tiền 50%, phí hủy 50%.
  - Mốc 0 phút hoặc đã qua giờ khám: Từ chối luồng tự hủy của bệnh nhân.
- **Bộ kiểm thử Xác thực Hồ sơ Bệnh nhân (Patient Identity Validation):**
  - Có `FullName` + `DateOfBirth` hợp lệ -> Pass.
  - Có `FullName` + `BirthYear` hợp lệ -> Pass.
  - Thiếu cả `DateOfBirth` và `BirthYear` -> Bắn lỗi validation.
  - Đối sánh `FullName + DOB` chỉ gắn cờ nghi trùng (`is_possible_duplicate = true`), **tuyệt đối không tự động gộp (No Auto-merge)**.
- **Bộ kiểm thử Máy trạng thái & Bảo toàn Slot khi Đổi lịch (Reschedule State Machine):**
  - Trạng thái yêu cầu đổi lịch: `PendingReview` -> `Approved` / `Rejected`.
  - Trong lúc `PendingReview`, lịch cũ và slot cũ không bị thay đổi hoặc giải phóng.
- **Bộ kiểm thử Cơ chế OTP (OTP Lifecycle Tests):**
  - Kiểm tra đúng `purpose` (`LOOKUP`, `RESCHEDULE`, `CANCEL`). Mã sinh cho `LOOKUP` không được dùng để `CANCEL`.
  - Kiểm tra giới hạn 5 lần nhập sai và mã hết hạn sau 5 phút.

### 2.2 Integration Tests (`ClinicAppointment.Api.IntegrationTests`)
- **Môi trường:** `WebApplicationFactory` kết hợp `Testcontainers.PostgreSql` trên Docker.
- **Ca kiểm thử Chống trùng lịch (Zero Double-Booking Concurrency Test - Bắt buộc):**
  - Giả lập 10 requests gửi đồng thời đến endpoint `POST /api/v1/appointments/guest` để đặt cùng một bác sĩ trong cùng một slot `09:00 - 09:30`.
  - **Kỳ vọng:** Đúng 1 request thành công (`201 Created`), 9 requests còn lại nhận `409 Conflict`.
- **Ca kiểm thử Phê duyệt Đổi lịch trong Transaction (Reschedule Atomic Approval):**
  - Lễ tân duyệt yêu cầu đổi lịch sang slot mới: slot mới được gán cho lịch hẹn, slot cũ được giải phóng thành trạng thái trống.
  - Giả lập trường hợp slot mới đã bị một ca khám khác chiếm trong lúc chờ duyệt -> Lễ tân duyệt thất bại và slot cũ của lịch hẹn vẫn được bảo toàn nguyên vẹn.
- **Ca kiểm thử Cách ly Dữ liệu (Patient Data Isolation):**
  - Bệnh nhân A đăng nhập không thể truy vấn hoặc can thiệp vào hồ sơ, lịch hẹn của Bệnh nhân B (trả về `403 Forbidden` hoặc `404 Not Found`).

---

## 3. Kiểm thử Frontend (React.js)

### 3.1 Component & Form Tests (Vitest + React Testing Library)
- **Guest Booking Wizard:**
  - Kiểm tra validate form bệnh nhân: Họ tên, số điện thoại đúng chuẩn Việt Nam, ngày sinh hoặc năm sinh.
  - Hiển thị thông tin snapshot giá dịch vụ và thời lượng trước khi xác nhận đặt lịch.
- **OTP Dialog & Lookup Flow:**
  - Nhập mã OTP, đếm ngược thời gian resend (60s), thông báo lỗi khi nhập sai hoặc hết hạn.

### 3.2 End-to-End Tests (Playwright)
1. **E2E Luồng Khách vãng lai & Tra cứu OTP:**
   - Guest đặt lịch thành công -> Nhận `BookingCode` -> Mở trang tra cứu -> Yêu cầu OTP -> Nhập OTP -> Xem thông tin tóm tắt lịch hẹn.
2. **E2E Luồng Hủy lịch Tự động:**
   - Lịch hẹn trước 2 tiếng -> Guest bấm Hủy qua OTP -> Hệ thống hiển thị số tiền hoàn 100% -> Trạng thái đổi thành `Cancelled` mà không cần lễ tân duyệt.
3. **E2E Luồng Lễ tân Check-in:**
   - Lễ tân đăng nhập -> Vào hàng đợi ngày -> Tìm kiếm theo `BookingCode` -> Bấm Check-in -> Trạng thái đổi thành `CheckedIn`.

---

## 4. Bằng chứng Kiểm thử & Tiêu chuẩn Bàn giao (Test Evidence Delivery)

- Toàn bộ tính năng chỉ được đánh dấu `COMPLETED` trong `planning/feature_list.json` khi có kết quả test thực tế pass 100%.
- Không cho phép bỏ qua test hoặc dùng dummy test không có assert.
- Báo cáo coverage đạt tối thiểu 80% cho Domain Models, Business Rules và Use Case Handlers.
