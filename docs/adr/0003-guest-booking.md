# ADR 0003: Hỗ trợ Tính năng Đặt lịch cho Khách vãng lai (Guest Booking)

- **Trạng thái:** Được chấp thuận (Accepted)
- **Ngày quyết định:** 2026-11-13
- **Người đề xuất:** Product & Business Team

---

## 1. Ngữ cảnh (Context)
Trong trải nghiệm y tế trực tuyến, một tỷ lệ lớn bệnh nhân (đặc biệt là người lớn tuổi, người bận rộn hoặc người khám lần đầu) thường bỏ dở quá trình đặt lịch nếu hệ thống bắt buộc phải đăng ký tài khoản với nhiều bước tạo mật khẩu và xác thực phức tạp trước khi chọn được giờ khám.

Chúng ta cần lựa chọn giữa:
1. **Bắt buộc đăng ký tài khoản (Mandatory Registration):** Yêu cầu người dùng phải có tài khoản đăng nhập trước khi được phép đặt lịch.
2. **Hỗ trợ Khách vãng lai (Guest Booking with Verification):** Cho phép người dùng đặt lịch nhanh chỉ với Họ tên, Số điện thoại và Email; sau đó xác thực danh tính qua OTP hoặc mã tra cứu.

---

## 2. Quyết định (Decision)
Chúng tôi quyết định **Hỗ trợ Tính năng Đặt lịch cho Khách vãng lai (Guest Booking)** song song với tính năng đặt lịch cho Bệnh nhân đã đăng nhập.

---

## 3. Lý do lựa chọn (Rationale)
1. **Tối đa hóa tỷ lệ chuyển đổi (Conversion Rate):**
   - Giảm thiểu rào cản thao tác (Frictionless Booking). Người bệnh chỉ mất dưới 60 giây để chọn bác sĩ và hoàn tất thông tin đặt hẹn.
2. **Phù hợp với đặc thù y tế tại Việt Nam:**
   - Nhiều người bệnh đặt lịch hộ cho bố mẹ, con cái hoặc người thân mà không nhất thiết phải tạo nhiều tài khoản riêng biệt.
3. **Cơ chế liên kết tài khoản linh hoạt:**
   - Khi khách vãng lai sau này quyết định tạo tài khoản bằng chính Số điện thoại hoặc Email đã từng đặt lịch, hệ thống có thể tự động liên kết toàn bộ lịch sử khám trước đó vào tài khoản mới.

---

## 4. Giải pháp kiểm soát rủi ro (Risk Mitigation)
- **Nguy cơ spam hoặc đặt lịch ảo:**
  - Áp dụng Rate Limiting trên IP và Số điện thoại.
  - Gửi mã xác nhận OTP / Email kích hoạt trong 10 phút. Nếu quá hạn không xác nhận, slot khám tự động được trả về trạng thái trống.
- **Bảo vệ quyền riêng tư:**
  - Khách vãng lai chỉ có thể tra cứu trạng thái lịch hẹn khi cung cấp chính xác đồng thời cả **Mã lịch hẹn (Appointment Code)** và **Số điện thoại đã đăng ký**.

