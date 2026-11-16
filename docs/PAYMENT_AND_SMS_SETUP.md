# Hướng Dẫn Tích Hợp Thanh Toán VietQR (PayOS) & Gửi SMS OTP Thật (Twilio)

Tài liệu này hướng dẫn chi tiết từng bước dành cho người mới để cấu hình và kích hoạt:
1. **Thanh toán thật tự động qua VietQR (PayOS)**: Quét mã QR bằng App ngân hàng bất kỳ, tiền chuyển thẳng vào tài khoản của bạn và hệ thống tự động xác nhận lịch hẹn.
2. **Gửi mã OTP thật về điện thoại (Twilio)**: Khi khách tra cứu, đổi lịch hoặc hủy lịch, tin nhắn SMS chứa mã OTP 6 số sẽ được gửi trực tiếp vào số điện thoại của khách.

---

## 1. Cấu Hình Thanh Toán VietQR Tự Động (PayOS)

[PayOS (payos.vn)](https://payos.vn) là giải pháp cổng thanh toán VietQR miễn phí và nhanh chóng nhất tại Việt Nam dành cho nhà phát triển và doanh nghiệp.

### Bước 1: Tạo tài khoản PayOS
1. Truy cập [https://payos.vn](https://payos.vn) và đăng ký tài khoản miễn phí.
2. Trong Dashboard, vào mục **Kênh thanh toán** -> **Tạo kênh thanh toán mới**.
3. Điền thông tin tài khoản ngân hàng của bạn (Số tài khoản, Tên chủ tài khoản, Ngân hàng). Khi khách hàng quét mã VietQR, tiền sẽ chuyển thẳng vào tài khoản này.

### Bước 2: Lấy thông tin kết nối API
Trong trang chi tiết Kênh thanh toán vừa tạo, bạn sẽ thấy 3 thông số:
- **Client ID**
- **API Key**
- **Checksum Key**

### Bước 3: Cấu hình vào dự án
Mở file `backend/src/ClinicAppointment.Api/appsettings.json` và thay thế vào mục `PayOS`:

```json
"PayOS": {
  "ClientId": "điền_client_id_của_bạn",
  "ApiKey": "điền_api_key_của_bạn",
  "ChecksumKey": "điền_checksum_key_của_bạn",
  "ReturnUrl": "http://localhost:5173/lookup",
  "CancelUrl": "http://localhost:5173/lookup"
}
```

*(Hoặc trên Render / Production, bạn thêm 3 biến môi trường tương ứng: `PayOS__ClientId`, `PayOS__ApiKey`, `PayOS__ChecksumKey`)*.

### Bước 4: Cấu hình Webhook nhận tiền tự động
1. Trong Dashboard PayOS, mục **Webhook**, nhập URL:
   - Với Demo Online (Render/Cloudflare): `https://<tên-miền-của-bạn>/api/v1/payments/payos/webhook`
2. Bấm **Lưu Webhook**. Hệ thống của bạn đã tích hợp sẵn cơ chế phản hồi tự động xác thực URL của PayOS.
3. Khi khách quét mã chuyển tiền thành công, PayOS sẽ tự động bắn Webhook về Backend, cập nhật cuộc hẹn sang trạng thái `Confirmed` và ghi nhận `PaidAmount`!

---

## 2. Cấu Hình Gửi SMS OTP Thật (Twilio)

[Twilio (twilio.com)](https://twilio.com) là nhà cung cấp viễn thông toàn cầu hỗ trợ gửi tin nhắn SMS tới số điện thoại thật tại Việt Nam (+84).

### Bước 1: Tạo tài khoản Twilio
1. Truy cập [https://twilio.com](https://twilio.com) và đăng ký tài khoản miễn phí (Twilio tặng sẵn ~$15 credit dùng thử).
2. Xác minh số điện thoại cá nhân của bạn khi đăng ký.

### Bước 2: Lấy số điện thoại gửi SMS và API Credentials
1. Tại trang **Twilio Console Dashboard**:
   - Nhấn **Get a phone number** để nhận 1 số điện thoại gửi SMS miễn phí (`Twilio Phone Number`).
   - Copy 2 thông số:
     - **Account SID**
     - **Auth Token**

### Bước 3: Cấu hình vào dự án
Mở file `backend/src/ClinicAppointment.Api/appsettings.json` và thay thế vào mục `Twilio`:

```json
"Twilio": {
  "AccountSid": "điền_account_sid_của_bạn",
  "AuthToken": "điền_auth_token_của_bạn",
  "FromPhoneNumber": "điền_số_twilio_của_bạn_ví_dụ_+1234567890"
}
```

*(Hoặc trên Render / Production, bạn cấu hình 3 biến môi trường: `Twilio__AccountSid`, `Twilio__AuthToken`, `Twilio__FromPhoneNumber`)*.

> [!NOTE]
> Với tài khoản Twilio dùng thử (Trial), Twilio cho phép gửi tin nhắn tới các số điện thoại đã được thêm vào mục **Phone Numbers** -> **Manage** -> **Verified Caller IDs** trong Twilio Console. Khi nâng cấp tài khoản, bạn có thể gửi tới bất kỳ số điện thoại nào tại Việt Nam.

---

## 3. Chế Độ Thử Nghiệm An Toàn (Built-in Demo Mode)

Hệ thống được thiết kế thông minh:
- Nếu bạn **chưa nhập keys** hoặc đang dùng key mẫu `YOUR_...`, hệ thống **vẫn hoạt động trơn tru 100%**:
  - Tự động sinh mã VietQR mẫu chuẩn với số tài khoản và nội dung chuyển khoản tương ứng số tiền dịch vụ khám.
  - Có sẵn nút **"[Chế độ Demo: Nhấn vào đây để mô phỏng quét mã thành công]"** để kiểm thử luồng xác nhận thanh toán ngay lập tức.
  - Giả lập gửi OTP an toàn, ghi log thông tin rõ ràng và cho phép nhập mã `123456` để thử nghiệm.
- Ngay khi bạn **nhập thông tin thật** vào `appsettings.json`, hệ thống sẽ tự động chuyển sang gọi trực tiếp API thật của PayOS và Twilio mà không cần phải viết lại bất kỳ dòng code nào.
