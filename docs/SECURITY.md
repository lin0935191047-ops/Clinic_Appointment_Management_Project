# Security & Data Protection Specification

## 1. Cơ chế Xác thực & Phân quyền (Authentication & Authorization)

### 1.1 Quản lý Phiên & Token
- **Access Token:** Định dạng JSON Web Token (JWT) có thời hạn sống ngắn (**15 - 30 phút**), mang thông tin cơ bản: `UserId`, `Role`, `Email`.
- **Refresh Token:** Mã ngẫu nhiên cryptographically secure, lưu trữ trong bảng CSDL hoặc Redis, thời hạn 7 - 30 ngày, cơ chế **Token Rotation** (mỗi lần refresh sẽ sinh mã mới và hủy mã cũ).
- **Lưu trữ phía Client:** Khuyến nghị lưu Refresh Token trong Cookie dạng `HttpOnly`, `Secure`, `SameSite=Strict` để phòng chống đánh cắp qua tấn công XSS.

### 1.2 Phân quyền theo vai trò (Role-Based Access Control - RBAC)
Hệ thống xác định 4 vai trò chính:
- `ADMIN`: Toàn quyền quản trị danh mục, bác sĩ, lịch trình, người dùng, xem báo cáo toàn hệ thống.
- `DOCTOR`: Truy cập lịch làm việc cá nhân, xem danh sách bệnh nhân khám của mình, cập nhật chẩn đoán/kết quả khám.
- `RECEPTIONIST`: Xem hàng đợi khám trong ngày, thực hiện check-in, tạo lịch hẹn tại quầy (walk-in), liên hệ bệnh nhân.
- `PATIENT`: Xem lịch sử khám của bản thân, quản lý thông tin cá nhân, đặt lịch và hủy lịch của chính mình.

---

## 2. Bảo vệ Dữ liệu Y tế & Thông tin Cá nhân (Data Privacy)

1. **Mã hóa khi lưu trữ và truyền tải (Encryption):**
   - **In-Transit:** Toàn bộ giao tiếp giữa Client và Server bắt buộc sử dụng HTTPS với TLS 1.3 / TLS 1.2.
   - **At-Rest:** Mật khẩu người dùng được băm một chiều bằng thuật toán an toàn (`Argon2id` hoặc `BCrypt` work factor >= 12).
2. **Quyền riêng tư Dữ liệu Bệnh nhân (Principle of Least Privilege):**
   - Bác sĩ chỉ được phép đọc hồ sơ y tế / lý do khám của những bệnh nhân có lịch hẹn với chính bác sĩ đó.
   - Lễ tân chỉ được xem thông tin hành chính (Họ tên, SĐT, Mã hẹn, Giờ khám), không có quyền can thiệp vào chẩn đoán chi tiết chuyên sâu của bác sĩ.
3. **Nhật ký Giám sát (Audit Logging):**
   - Ghi log vết kiểm toán đối với mọi hành vi sửa đổi trạng thái lịch hẹn, hủy lịch, xem thông tin nhạy cảm.
   - Bản ghi audit bao gồm: `UserId`, `Action`, `EntityName`, `EntityId`, `IPAddress`, `Timestamp`.

---

## 3. Phòng chống các lỗ hổng Web phổ biến (OWASP Top 10)

| Nguy cơ | Giải pháp phòng vệ trong hệ thống |
| :--- | :--- |
| **SQL Injection** | Sử dụng hoàn toàn ORM Entity Framework Core với Parameterized Queries và LINQ. Tuyệt đối không ghép chuỗi SQL thuần. |
| **Cross-Site Scripting (XSS)** | React tự động escape HTML khi render. Kết hợp thiết lập HTTP Header `Content-Security-Policy (CSP)`. |
| **Cross-Site Request Forgery (CSRF)** | Sử dụng cơ chế Cookie `SameSite=Strict`/`Lax` và Anti-Forgery Token cho các endpoint nhạy cảm. |
| **Brute Force & DoS** | Áp dụng ASP.NET Core Rate Limiting Middleware: giới hạn tối đa 5 lần thử đăng nhập/phút theo IP; giới hạn tạo lịch vãng lai 10 request/giờ. |
| **Broken Object Level Auth (BOLA / IDOR)** | Mọi câu truy vấn dữ liệu chi tiết của Patient hoặc Doctor đều phải kiểm tra quyền sở hữu (`patient.UserId == currentUserId`). |

---

## 4. Quản lý Bí mật & Biến môi trường (Secrets Management)

- **Không bao giờ commit file chứa thông tin nhạy cảm:** `.env`, `appsettings.Production.json`, connection strings chứa password vào Git.
- **Môi trường phát triển cục bộ:** Dùng .NET `dotnet user-secrets` và file `.env.local` của React.
- **Môi trường triển khai Production:** Nạp biến môi trường thông qua Docker Secrets, Azure Key Vault hoặc GitHub Repository Secrets.

