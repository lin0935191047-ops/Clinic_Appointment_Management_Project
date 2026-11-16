# Hướng Dẫn Tích Hợp Cổng Thanh Toán Quét Mã QR Ngân Hàng & SMS Gateway SpeedSMS

Tài liệu này hướng dẫn chi tiết từng bước dành cho người mới để cấu hình và kích hoạt:
1. **Thanh toán quét mã QR ngân hàng (VietQR / PayOS)**: Tạo mã QR ngân hàng chuẩn NAPAS 247, khách hàng mở App ngân hàng bất kỳ (Vietcombank, MB, Techcombank, VPBank, Agribank, MoMo...) quét mã chuyển tiền thẳng vào tài khoản của phòng khám.
2. **Gửi mã OTP vào số điện thoại thật qua SpeedSMS Gateway (speedsms.vn)**: Khi khách tra cứu, đổi lịch hoặc hủy lịch, tin nhắn SMS OTP 6 chữ số sẽ được gửi trực tiếp tới điện thoại thật của khách.

---

## 1. Cấu Hình SMS Gateway SpeedSMS (Gửi SMS OTP Thật)

[SpeedSMS (speedsms.vn)](https://speedsms.vn) là dịch vụ SMS Gateway hàng đầu tại Việt Nam, tối ưu hóa cho tin nhắn OTP và chăm sóc khách hàng.

### Bước 1: Đăng ký tài khoản SpeedSMS
1. Truy cập [https://connect.speedsms.vn](https://connect.speedsms.vn) và đăng ký tài khoản.
2. Xác thực số điện thoại và email theo hướng dẫn.

### Bước 2: Lấy API Access Token
1. Đăng nhập vào trang quản trị SpeedSMS Connect.
2. Nhấn vào menu **Cài đặt (Settings)** -> **Thông tin tài khoản (Profile)** hoặc mục **API Access**.
3. Copy chuỗi **API Access Token** của bạn.

### Bước 3: Cấu hình vào dự án
Mở file `backend/src/ClinicAppointment.Api/appsettings.json` và cập nhật mục `"SpeedSMS"`:

```json
"SpeedSMS": {
  "AccessToken": "điền_access_token_của_bạn_ở_đây",
  "SmsType": 2,
  "Sender": ""
}
```

- `SmsType`: Mặc định là `2` (SMS Chăm sóc khách hàng / OTP đầu số cố định).
- `Sender`: Brandname (nếu bạn đã đăng ký Brandname riêng trên SpeedSMS; nếu chưa có thì để trống `""`).

*(Hoặc trên Render / biến môi trường production: `SpeedSMS__AccessToken`)*.

---

## 2. Cấu Hình Cổng Thanh Toán Quét Mã QR Ngân Hàng

Hệ thống hỗ trợ 2 chế độ quét mã QR ngân hàng linh hoạt:

### Chế độ A: Quét mã QR trực tiếp về Tài Khoản Ngân Hàng của bạn (Direct VietQR)
Bạn có thể nhận tiền trực tiếp vào bất kỳ tài khoản ngân hàng nào của bạn tại Việt Nam. Chỉ cần cấu hình trong `backend/src/ClinicAppointment.Api/appsettings.json` mục `"BankQR"`:

```json
"BankQR": {
  "BankId": "MB",
  "AccountNumber": "0935191047",
  "AccountName": "PHONG KHAM DA KHOA",
  "Template": "compact2"
}
```

- `BankId`: Mã ngân hàng nhận tiền của bạn. Ví dụ:
  - `MB`: Ngân hàng Quân Đội
  - `VCB`: Vietcombank
  - `TCB`: Techcombank
  - `ICB`: VietinBank
  - `BIDV`: BIDV
  - `ACB`: Á Châu
  - `VPB`: VPBank
  - `TPB`: TPBank
- `AccountNumber`: Số tài khoản ngân hàng của bạn.
- `AccountName`: Tên chủ tài khoản (viết hoa không dấu).

Khi khách đặt lịch, hệ thống sẽ tự sinh mã QR chuẩn ngân hàng NAPAS 247. Khách quét mã thì App ngân hàng sẽ tự động điền:
- Ngân hàng thụ hưởng & Số tài khoản của bạn
- Số tiền khám chính xác (`ServicePriceSnapshot`)
- Nội dung chuyển khoản chuẩn: `TT APT-YYYYMMDD-XXXXXX`

### Chế độ B: Tự động xác nhận tiền về qua Cổng PayOS (Automated Webhook IPN)
Nếu muốn hệ thống **tự động phát hiện khi khách vừa chuyển khoản xong** và tự động đổi trạng thái lịch hẹn sang `Confirmed` mà không cần kiểm tra thủ công:
1. Đăng ký tại [https://payos.vn](https://payos.vn) và liên kết tài khoản ngân hàng.
2. Lấy `ClientId`, `ApiKey`, `ChecksumKey` và điền vào mục `"PayOS"` trong `appsettings.json`:
   ```json
   "PayOS": {
     "ClientId": "điền_client_id_của_bạn",
     "ApiKey": "điền_api_key_của_bạn",
     "ChecksumKey": "điền_checksum_key_của_bạn",
     "ReturnUrl": "http://localhost:5173/lookup",
     "CancelUrl": "http://localhost:5173/lookup"
   }
   ```
3. Cài đặt Webhook URL trên PayOS: `https://<tên-miền-của-bạn>/api/v1/payments/payos/webhook`.

---

## 3. Chế Độ Thử Nghiệm An Toàn (Built-in Fallback)

Nếu bạn chưa điền Access Token của SpeedSMS:
- Hệ thống **tuyệt đối không bị lỗi hay dừng hoạt động**.
- Backend tự động ghi log cảnh báo thân thiện và giả lập gửi OTP thành công.
- Mã OTP thử nghiệm mặc định là `123456` để bạn có thể kiểm thử toàn bộ luồng đổi/hủy lịch trên Web ngay lập tức.
- Ngay khi bạn nhập Access Token thật của SpeedSMS vào `appsettings.json`, tin nhắn SMS thật sẽ được gửi lập tức đến số điện thoại người dùng.
