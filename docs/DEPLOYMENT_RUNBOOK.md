# DEPLOYMENT & OPERATION RUNBOOK — CLINIC APPOINTMENT MANAGEMENT SYSTEM

Tài liệu hướng dẫn vận hành, triển khai môi trường Production, cấu hình bảo mật HTTPS và quy trình sao lưu / phục hồi cơ sở dữ liệu cho hệ thống **Clinic Appointment Management System**.

---

## 1. Mục Tiêu & Nguyên Tắc Bảo Mật (Core Invariants)

Theo quy định bất biến của dự án tại `AGENTS.md` và `docs/ARCHITECTURE.md`:
1. **Không lưu trữ Secret trong Git (No Secrets in Repo):**
   - Tuyệt đối không commit tệp `.env`, `appsettings.Production.json`, mật khẩu database, khóa JWT bí mật hoặc dữ liệu bệnh nhân thật vào Git.
   - Luôn sử dụng các tệp mẫu `.env.example`, `appsettings.Production.json.example`, `frontend/.env.example`.
2. **Bắt buộc HTTPS ở môi trường mục tiêu (HTTPS in Target Env):**
   - Toàn bộ lưu lượng HTTP (Cổng 80) được tự động chuyển hướng sang HTTPS (Cổng 443) bằng mã chuyển hướng vĩnh viễn `301 Moved Permanently`.
   - Bắt buộc kích hoạt HTTP Strict Transport Security (HSTS) với thời hạn tối thiểu 1 năm (`max-age=31536000; includeSubDomains; preload`).
   - Cấu hình đầy đủ các tiêu đề an toàn: `X-Frame-Options: SAMEORIGIN`, `X-Content-Type-Options: nosniff`, `X-XSS-Protection: 1; mode=block`, `Referrer-Policy: strict-origin-when-cross-origin`.
3. **Múi giờ & Chuẩn hóa dữ liệu:**
   - Cơ sở dữ liệu PostgreSQL lưu trữ toàn bộ mốc thời gian theo chuẩn `UTC` (`timestamptz`).
   - Giao diện người dùng và thông báo hiển thị theo múi giờ phòng khám `Asia/Ho_Chi_Minh` (UTC+7).
4. **Truy vết & Che giấu thông tin nhạy cảm:**
   - Mọi request đều được gán mã Correlation ID (`X-Correlation-ID`) có độ dài $\le 80$ ký tự.
   - Hệ thống log tự động làm sạch (Redact) số điện thoại (`091***678`), số CCCD (`[REDACTED_CCCD]`), BHYT (`[REDACTED_BHYT]`), mã OTP (`[REDACTED_OTP]`) và mật khẩu (`[REDACTED]`).

---

## 2. Kiến Trúc Triển Khai Môi Trường Production

Hệ thống được đóng gói thành các dịch vụ độc lập chạy trên nền tảng Docker & Docker Compose:

```
[ Trình Duyệt / Bệnh Nhân / Nhân Viên ]
                  │
          (HTTPS Port 443 / HTTP Port 80 Redirect)
                  ▼
┌─────────────────────────────────────────────────────────────┐
│              NGINX REVERSE PROXY & SSL GATEWAY              │
│  - SSL/TLS Termination (clinic.crt / clinic.key)            │
│  - HTTP -> HTTPS 301 Redirect                               │
│  - HSTS & Security Headers                                  │
│  - X-Correlation-ID header forwarding                       │
└──────────────┬──────────────────────────────┬───────────────┘
               │                              │
         /api/* & /health                     /* (Static Assets)
               ▼                              ▼
┌──────────────────────────────┐ ┌────────────────────────────┐
│   ASP.NET CORE WEB API       │ │    REACT FRONTEND (SPA)    │
│  - Kestrel Port 8080         │ │   - Nginx Alpine Port 80   │
│  - Clean Architecture        │ │   - HTML5 SPA Router       │
│  - Non-root user appuser     │ │   - Gzip Compression       │
└──────────────┬───────────────┘ └────────────────────────────┘
               │
          Port 5432 (Nội bộ network, không expose ra Internet)
               ▼
┌─────────────────────────────────────────────────────────────┐
│                 POSTGRESQL 16/18 DATABASE                   │
│  - GiST Exclusion Constraint (Chống trùng ca khám)          │
│  - AES-256-GCM Encrypted Identifiers                        │
│  - Named Volume: postgres_prod_data                         │
└─────────────────────────────────────────────────────────────┘
```

---

## 3. Cấu Hình Biến Môi Trường & Quản Lý Bí Mật

### 3.1. Các biến môi trường bắt buộc (Environment Variables)

| Tên biến | Mục đích | Ví dụ giá trị | Ghi chú |
| :--- | :--- | :--- | :--- |
| `POSTGRES_DB` | Tên cơ sở dữ liệu | `ClinicAppointmentDb` | Không dấu, camelCase hoặc snake_case |
| `POSTGRES_USER` | Tên người dùng DB | `postgres` | Tài khoản có quyền DDL |
| `POSTGRES_PASSWORD` | Mật khẩu database | *`Mật khẩu mạnh ngẫu nhiên`* | **Bắt buộc bí mật, không commit** |
| `JWT_SECRET_KEY` | Khóa ký số JWT token | *`Chuỗi ngẫu nhiên >= 32 ký tự`* | **Bắt buộc bí mật** |
| `PII_ENCRYPTION_KEY`| Khóa mã hóa AES-256-GCM | *`Chuỗi bí mật 32 bytes`* | Dùng mã hóa CCCD/Hộ chiếu/BHYT |
| `HMAC_SECRET_KEY` | Khóa băm xác thực HMAC | *`Chuỗi bí mật 32 bytes`* | Dùng băm chính xác để tra cứu |
| `ASPNETCORE_ENVIRONMENT` | Môi trường chạy backend | `Production` | Tắt Swagger public, bật log an toàn |

### 3.2. Lệnh sinh khóa ngẫu nhiên an toàn (PowerShell / Linux)

```powershell
# Sinh chuỗi bí mật ngẫu nhiên 32 ký tự bằng PowerShell:
-join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | ForEach-Object {[char]$_})
```

```bash
# Sinh chuỗi bí mật ngẫu nhiên bằng OpenSSL trên Linux:
openssl rand -base64 32
```

---

## 4. Quy Trình Triển Khai Production Với Docker

### Bước 1: Chuẩn bị chứng chỉ SSL/TLS
Tạo thư mục `deployment/ssl` và copy chứng chỉ SSL vào:
- `deployment/ssl/clinic.crt`: Tệp chứng chỉ (Certificate hoặc Full Chain).
- `deployment/ssl/clinic.key`: Tệp khóa riêng tư (Private Key).

> [!TIP]
> Đối với môi trường staging/nội bộ hoặc thử nghiệm, bạn có thể tự sinh chứng chỉ Self-Signed:
> ```bash
> openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
>   -keyout deployment/ssl/clinic.key \
>   -out deployment/ssl/clinic.crt \
>   -subj "/C=VN/ST=HCM/L=HoChiMinh/O=Clinic/CN=clinic.local"
> ```

### Bước 2: Thiết lập tệp cấu hình `.env`
Sao chép từ tệp mẫu và cập nhật các mật khẩu mạnh:
```powershell
Copy-Item .env.example .env
```

### Bước 3: Khởi chạy cụm dịch vụ Production
```powershell
# Build images và khởi chạy toàn bộ 4 containers ở chế độ nền:
docker compose -f docker-compose.prod.yml up -d --build
```

### Bước 4: Kiểm tra trạng thái hoạt động (Health Checks)
```powershell
# Kiểm tra danh sách container đang chạy:
docker compose -f docker-compose.prod.yml ps

# Kiểm tra Liveness Probe của API:
curl -k https://localhost/health/live

# Kiểm tra Readiness Probe và kết nối PostgreSQL:
curl -k https://localhost/health/ready

# Kiểm tra API toàn diện:
curl -k https://localhost/api/v1/health
```

**Kết quả mong đợi:** Mã HTTP `200 OK` với JSON `{"status": "Healthy", ...}`.

---

## 5. Quy Trình Sao Lưu Cơ Sở Dữ Liệu (Backup Runbook)

### 5.1. Sao lưu thủ công tức thì (Ad-hoc Backup)
Sử dụng script tích hợp sẵn:
```powershell
# Trên Windows:
.\scripts\backup-database.ps1 -BackupDir "database/backups"

# Hoặc bằng lệnh pg_dump trực tiếp:
pg_dump -h localhost -p 5432 -U postgres -Fc -b -v -f "database/backups/clinic_manual_$(Get-Date -Format 'yyyyMMdd_HHmmss').dump" clinicappointmentdb
```

```bash
# Trên Linux / Server:
./scripts/backup-database.sh
```

### 5.2. Tự động hóa sao lưu định kỳ (Scheduled Backup)
Thiết lập Task Scheduler (Windows) hoặc Cron Job (Linux) chạy hàng ngày vào lúc **02:00 sáng**:
```cron
# Chạy sao lưu mỗi ngày lúc 02:00 sáng và tự động xóa bản sao lưu cũ quá 14 ngày:
0 2 * * * /opt/clinic/scripts/backup-database.sh >> /var/log/clinic_backup.log 2>&1
```

### 5.3. Chiến lược lưu trữ bản sao lưu (Retention Policy)
- **Hàng ngày (Daily):** Lưu giữ trong vòng 14 ngày gần nhất.
- **Hàng tuần (Weekly):** Bản sao lưu Chủ Nhật được lưu giữ trong 8 tuần.
- **Hàng tháng (Monthly):** Bản sao lưu ngày đầu tháng được lưu giữ trong 12 tháng.
- Toàn bộ bản sao lưu phải được đồng bộ sang vị trí lưu trữ thứ cấp (Offsite storage hoặc Cloud Object Storage có mã hóa KMS).

---

## 6. Quy Trình Phục Hồi Dữ Liệu Khi Gặp Sự Cố (Disaster Recovery Runbook)

Khi cơ sở dữ liệu gặp lỗi hoặc cần khôi phục về trạng thái trước đó, thực hiện nghiêm ngặt theo các bước sau:

### Bước 1: Tạm dừng dịch vụ Web API để cô lập dữ liệu
Ngăn chặn người dùng gửi thêm request mới trong khi phục hồi:
```powershell
docker compose -f docker-compose.prod.yml stop backend frontend
```

### Bước 2: Tạo bản sao lưu an toàn trước khi khôi phục (Pre-restore Safety Backup)
Ngay cả khi DB bị lỗi, luôn tạo một bản snapshot hiện trạng để phòng ngừa:
```powershell
pg_dump -h localhost -U postgres -Fc -f "database/backups/pre_restore_safety_$(Get-Date -Format 'yyyyMMdd_HHmmss').dump" clinicappointmentdb
```

### Bước 3: Thực hiện phục hồi dữ liệu từ bản sao lưu
Sử dụng script phục hồi kèm cờ xác nhận an toàn:
```powershell
# Trên Windows:
.\scripts\restore-database.ps1 -BackupFile "database/backups/clinic_backup_20261114_120000.dump" -ConfirmSafety

# Hoặc trên Linux:
./scripts/restore-database.sh "database/backups/clinic_backup_20261114_120000.dump" --confirm

# Hoặc sử dụng câu lệnh pg_restore trực tiếp:
pg_restore -h localhost -p 5432 -U postgres -d clinicappointmentdb --clean --if-exists -v "database/backups/clinic_backup_20261114_120000.dump"
```

### Bước 4: Kiểm tra tính toàn vẹn dữ liệu (Verification Drill)
Đăng nhập PostgreSQL và kiểm tra số liệu:
```sql
-- 1. Kiểm tra số lượng bảng:
SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public';

-- 2. Kiểm tra ràng buộc GiST chống trùng ca khám còn nguyên vẹn:
SELECT conname, contype FROM pg_constraint WHERE conname = 'ex_appointments_doctor_no_overlap';

-- 3. Kiểm tra số lượng bản ghi lịch hẹn gần nhất:
SELECT COUNT(*), status FROM appointments GROUP BY status;

-- 4. Kiểm tra audit logs:
SELECT COUNT(*) FROM audit_logs;
```

### Bước 5: Khởi động lại dịch vụ và xác nhận sức khỏe hệ thống
```powershell
docker compose -f docker-compose.prod.yml start backend frontend
curl -k https://localhost/health/ready
```

---

## 7. Bảng Kiểm Tra Sẵn Sàng Vận Hành (Operational Readiness Checklist)

- [x] Không có tệp `.env`, khóa mật khẩu hay connection string thật nào bị đưa vào Git.
- [x] Đã cấu hình Nginx chuyển hướng tự động 100% từ HTTP sang HTTPS (Mã chuyển hướng 301).
- [x] Đã kích hoạt tiêu đề bảo mật HSTS với thời hạn 1 năm.
- [x] Đã thiết lập Multi-stage Dockerfile tối ưu kích thước và chạy quyền non-root.
- [x] Kịch bản sao lưu và phục hồi cơ sở dữ liệu đã được thử nghiệm và tài liệu hóa đầy đủ.
- [x] Các probe `/health/live` và `/health/ready` hoạt động chính xác với Kubernetes / Docker.

---

## 8. Hướng Dẫn Triển Khai Hoàn Toàn Lên Cloud 24/7 (Vercel + Render / Azure + PostgreSQL Cloud)

Mô hình triển khai Cloud 3 tầng độc lập giúp khách hàng và phòng khám truy cập hệ thống 24/7 mà **hoàn toàn không phụ thuộc vào máy tính cá nhân**:

```
[ Trình Duyệt Bệnh Nhân / Lễ Tân / Bác Sĩ Toàn Cầu ]
                       │
             (HTTPS / CDN Toàn Cầu)
                       ▼
 ┌───────────────────────────────────────────────┐
 │        1. FRONTEND: VERCEL CLOUD (SPA)        │
 │  - Domain: clinic-appointment-management-proje│
 │    .vercel.app                                │
 │  - Tự động build từ nhánh main GitHub        │
 │  - Gọi API qua biến: VITE_API_BASE_URL        │
 └───────────────────────┬───────────────────────┘
                         │
             (HTTPS RESTful API & CORS)
                         ▼
 ┌───────────────────────────────────────────────┐
 │        2. BACKEND: RENDER (Hoặc AZURE)        │
 │  - ASP.NET Core 8 Web API (Docker Linux)      │
 │  - Tự động Migrate schema & Seed 6 Bác sĩ     │
 │  - Cổng Kestrel Port 8080 (hoặc PORT)         │
 │  - URL: https://clinic-backend-api.onrender   │
 │    .com                                       │
 └───────────────────────┬───────────────────────┘
                         │
             (Mã hóa SSL Port 5432)
                         ▼
 ┌───────────────────────────────────────────────┐
 │    3. DATABASE: POSTGRESQL CLOUD (16/18)      │
 │  - Render Postgres / Supabase / Neon          │
 │  - Ràng buộc GiST (btree_gist) chống trùng ca │
 │  - 15 bảng nghiệp vụ & Audit Logs             │
 └───────────────────────────────────────────────┘
```

### Bước 1: Triển khai Database & Backend trên Render (Cách 1-Click nhanh nhất)
1. Đăng nhập [dashboard.render.com](https://dashboard.render.com).
2. Nhấn nút **New +** -> Chọn **Blueprint**.
3. Chọn kho lưu trữ GitHub của dự án (`lin0935191047-ops/Clinic_Appointment_Management_Project`).
4. Nhánh: `main`, Blueprint file: `render.yaml`.
5. Nhấn **Apply**:
   - Render sẽ tự động khởi tạo cơ sở dữ liệu `clinic-postgres` (PostgreSQL 16) tại Singapore.
   - Render tự động build Dockerfile của Backend `.NET 8` và cấp phát một URL HTTPS công khai (ví dụ: `https://clinic-backend-api-xxxx.onrender.com`).
   - Backend khi khởi động sẽ tự động kết nối cơ sở dữ liệu, chạy migration tạo 15 bảng và seed đầy đủ tài khoản & 6 bác sĩ chuyên khoa thực tế.

*(Tùy chọn Miễn phí vĩnh viễn: Bạn có thể tạo database PostgreSQL miễn phí vĩnh viễn tại [Supabase.com](https://supabase.com) hoặc [Neon.tech](https://neon.tech), sau đó trong Render Web Service thêm biến môi trường `ConnectionStrings__DefaultConnection` trỏ về chuỗi kết nối của Supabase/Neon).*

### Bước 2: Liên kết Frontend trên Vercel với Backend Cloud
1. Đăng nhập [vercel.com](https://vercel.com) và vào Project `clinic-appointment-management-project`.
2. Vào **Settings** -> mục **Environment Variables** (cột bên trái).
3. Thêm biến môi trường mới:
   - **Key:** `VITE_API_BASE_URL`
   - **Value:** `https://clinic-backend-api-xxxx.onrender.com` *(Thay bằng URL Backend Render vừa tạo)*
   - Đánh dấu chọn cả 3 môi trường: `Production`, `Preview`, `Development`.
   - Nhấn **Save**.
4. Vào tab **Deployments** trên Vercel -> Nhấn vào dấu 3 chấm `...` của lần deploy mới nhất -> Chọn **Redeploy**.
5. Sau 20 giây, trang web Vercel `https://clinic-appointment-management-proje.vercel.app` sẽ kết nối trực tiếp 100% với Cloud Backend và Cloud Database.

### Bước 3: Nghiệm thu truy cập độc lập
- Mở điện thoại di động hoặc một trình duyệt khác (không bật localhost trên máy tính).
- Truy cập `https://clinic-appointment-management-proje.vercel.app`.
- Thử đặt lịch khám mới (Guest Booking), quét mã VietQR Techcombank chuyển khoản, hoặc đăng nhập vào Kênh Lễ tân / Bác sĩ. Mọi dữ liệu đều được lưu trữ trực tiếp trên Cloud.
