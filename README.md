# Clinic Appointment Management System

> **Hệ thống Quản lý và Đặt lịch khám bệnh phòng khám**  
> Kiến trúc phân tầng **Clean Architecture** trên nền tảng **ASP.NET Core Web API (.NET 8)**, giao diện **React 18 SPA (Vite + TypeScript + Tailwind CSS)** và cơ sở dữ liệu **PostgreSQL 18** bảo vệ chống trùng ca bằng **GiST Exclusion Constraint**.

[![Backend Tests](https://img.shields.io/badge/Backend%20Tests-514%2F514%20PASS-brightgreen.svg)](#hướng-dẫn-chạy-kiểm-thử-tự-động)
[![Frontend Tests](https://img.shields.io/badge/Frontend%20Tests-9%2F9%20PASS-brightgreen.svg)](#hướng-dẫn-chạy-kiểm-thử-tự-động)
[![Status](https://img.shields.io/badge/Project%20Status-55%2F55%20Features%20Completed-success.svg)](#danh-sách-và-trạng-thái-tính-năng)
[![Security](https://img.shields.io/badge/Security-PII%20Redaction%20%7C%20HTTPS%20HSTS-blue.svg)](#bảo-mật-và-quyền-riêng-tư)

---

## 1. Tổng quan hệ thống

Hệ thống phục vụ việc quản lý phòng khám đa khoa hiện đại, tối ưu hóa trải nghiệm của bệnh nhân từ khâu đặt lịch trực tuyến đến tiếp đón, khám bệnh và quản trị đối soát tài chính:

- **Khách vãng lai (Guest Booking):** Tìm kiếm và đặt lịch khám trong 4 bước đơn giản **hoàn toàn không bắt buộc đăng nhập hoặc tạo tài khoản**.
- **Xác thực bảo mật OTP (Purpose-bound & Scoped Grant):** Tra cứu, đổi lịch và hủy lịch được bảo vệ bằng mã OTP 6 chữ số gửi qua số điện thoại; cấp Scoped Grant Token dùng 1 lần (One-time use) với TTL 15 phút.
- **Bảo toàn hồ sơ bệnh nhân (Strictly NO Auto-merge):** Hồ sơ bệnh nhân độc lập với tài khoản người dùng; không bao giờ tự động gộp hồ sơ khi trùng Tên + Ngày sinh/Năm sinh; mở ca đối soát tại quầy Lễ tân.
- **Chính sách hoàn phí minh bạch:**
  - Hủy $\ge 60$ phút trước giờ hẹn: Hoàn **100%** phí dịch vụ đã thanh toán (phí hủy 0%).
  - Hủy $< 60$ phút trước giờ hẹn: Hoàn **50%** phí dịch vụ (phí hủy 50%).
  - Tại hoặc sau giờ hẹn: Chặn tự hủy.
- **Chống trùng ca tuyệt đối (Zero Double Booking):** Bảo vệ tầng ứng dụng kết hợp ràng buộc vật lý **PostgreSQL GiST Exclusion Constraint** (`tstzrange` nửa khoảng `[)`) ngăn chặn triệt để race condition khi có nhiều giao dịch đặt cùng một bác sĩ trong cùng khung giờ.
- **Bảo mật và Quyền riêng tư y tế:** Che giấu PII (số điện thoại `091***678`, CCCD `[REDACTED_CCCD]`, BHYT `[REDACTED_BHYT]`, OTP `[REDACTED_OTP]`, Password `[REDACTED]`), không bao giờ log thông tin nhạy cảm; phân quyền nghiêm ngặt RBAC và Anti-IDOR.
- **Triển khai Production Ready:** Multi-stage Docker Compose, Nginx Reverse Proxy tự động chuyển hướng 301 sang HTTPS, HSTS 1 năm, Health Checks theo dõi trạng thái cơ sở dữ liệu.

---

## 2. Công nghệ sử dụng

| Tầng hệ thống | Công nghệ | Chi tiết phiên bản & vai trò |
| :--- | :--- | :--- |
| **Backend API** | C#, ASP.NET Core 8 | Clean Architecture (Domain, Application, Infrastructure, Api), MediatR CQRS, FluentValidation |
| **Data Access & ORM** | Entity Framework Core 8 | Npgsql, Npgsql.EntityFrameworkCore.PostgreSQL, Migrations, Idempotent Seeder |
| **Database** | PostgreSQL 18 / 16 | PostgreSQL `btree_gist` extension, GiST exclusion constraints, B-Tree indexes, JSONB audit snapshots |
| **Frontend SPA** | React 18, TypeScript, Vite 6 | Tailwind CSS, Lucide React, Axios API Client, Responsive Design |
| **Testing** | xUnit, Vitest, JSDOM, RTL | 514 Backend Tests (Unit, State Machine, PII Redaction, Real Postgres GiST Concurrency), 9 Frontend Journey Tests |
| **Deployment** | Docker, Nginx 1.27 Alpine | Multi-stage Dockerfiles, Docker Compose, Nginx HTTPS Reverse Proxy, HSTS, Security Headers |

---

## 3. Cấu trúc thư mục dự án

```text
Clinic_Appointment_Management_Project/
├── backend/                              # Mã nguồn Backend ASP.NET Core (.NET 8)
│   ├── src/
│   │   ├── ClinicAppointment.Domain/         # Thực thể cốt lõi, Enums, State Machine, Policies
│   │   ├── ClinicAppointment.Application/    # Commands/Queries MediatR, DTOs, Validators, Interfaces
│   │   ├── ClinicAppointment.Infrastructure/ # EF Core DbContext, Npgsql, PII Encryption, Logging, Health
│   │   └── ClinicAppointment.Api/            # Controllers RESTful v1, Middlewares, Program.cs
│   ├── tests/
│   │   ├── ClinicAppointment.Domain.Tests/   # 501 Unit & Real DB Concurrency Tests
│   │   └── ClinicAppointment.Integration.Tests/ # 13 API Integration Tests (WebApplicationFactory)
│   ├── Dockerfile                        # Multi-stage SDK build -> non-root appuser runtime
│   └── ClinicAppointment.sln             # Visual Studio Solution
├── frontend/                             # Mã nguồn Frontend React SPA (Vite + TS)
│   ├── src/
│   │   ├── api/                              # apiClient, Axios interceptors, JWT handling
│   │   ├── components/layout/                # Navbar, Footer, Modal, ProtectedRoute
│   │   ├── pages/                            # Guest Booking, Lookup, Reception, Doctor, Admin Dashboards
│   │   ├── types/                            # TypeScript interfaces & enums
│   │   └── tests/                            # Vitest + React Testing Library User Journeys
│   ├── Dockerfile                        # Multi-stage Node build -> Nginx alpine static serving
│   ├── nginx.conf                        # Frontend SPA routing fallback (try_files)
│   └── package.json
├── deployment/                           # Cấu hình Gateway Reverse Proxy
│   └── nginx/
│       ├── nginx.conf                        # Nginx main config
│       └── conf.d/clinic.conf                # Port 80 -> 443 SSL 301 Redirect, HSTS, Upstream
├── database/                             # Tài liệu DB, Migrations SQL & Thư mục sao lưu
│   ├── migrations/20261114_initial_schema.sql
│   └── backups/
├── scripts/                              # Kịch bản tự động hóa sao lưu và phục hồi DB
│   ├── backup-database.ps1 / .sh         # Kịch bản sao lưu nén nhị phân pg_dump
│   └── restore-database.ps1 / .sh        # Kịch bản phục hồi pg_restore kèm kiểm tra tính toàn vẹn
├── docs/                                 # Tài liệu kỹ thuật, kiến trúc, đặc tả, runbook & handover
│   ├── HANDOVER.md                       # Báo cáo bàn giao và nghiệm thu 55/55 tính năng
│   ├── DEPLOYMENT_RUNBOOK.md             # Tài liệu vận hành triển khai, SSL, giám sát, khắc phục sự cố
│   ├── SPEC.md                           # Đặc tả yêu cầu phần mềm (SRS v1.2)
│   ├── BUSINESS_RULES.md                 # Quy tắc nghiệp vụ bất biến
│   ├── ARCHITECTURE.md                   # Tài liệu thiết kế kiến trúc hệ thống
│   ├── DATABASE_DESIGN.md                # Thiết kế cơ sở dữ liệu logic v1.2
│   └── API_SPECIFICATION.md              # Đặc tả giao diện lập trình ứng dụng RESTful
├── planning/                             # Kế hoạch và nhật ký tiến độ dự án
│   ├── feature_list.json                 # Danh sách 55 features, tiêu chí nghiệm thu và bằng chứng
│   ├── progress.md                       # Báo cáo tiến độ tổng thể
│   └── implementation_log.md             # Nhật ký chi tiết triển khai từng feature
├── docker-compose.prod.yml               # Cấu hình khởi chạy Production an toàn (Zero Trust DB)
├── .env.example                          # Tệp cấu hình biến môi trường mẫu
└── README.md                             # Tài liệu giới thiệu và hướng dẫn tổng quan
```

---

## 4. Hướng dẫn cài đặt và khởi chạy (Local Development)

### Yêu cầu tiên quyết (Prerequisites)
- **.NET SDK:** Phiên bản 8.0 trở lên (hoặc .NET 10 SDK tương thích ngược).
- **Node.js:** Phiên bản 20.x hoặc 24.x; **npm** 10.x trở lên.
- **PostgreSQL:** Phiên bản 16 hoặc 18 (lắng nghe cổng mặc định 5432).

---

### Bước 1: Khởi động Cơ sở dữ liệu và Chạy Migration

1. Đảm bảo PostgreSQL đang chạy trên cổng 5432 và đã tạo database `clinicappointmentdb` (hoặc cấu hình tên DB tùy chọn trong chuỗi kết nối).
2. Di chuyển vào thư mục backend và cập nhật schema cơ sở dữ liệu qua EF Core:
```powershell
cd d:\Clinic_Appointment_Management_Project\backend
# Lệnh tiêu chuẩn:
dotnet ef database update --project src/ClinicAppointment.Infrastructure --startup-project src/ClinicAppointment.Api

# Hoặc dùng đường dẫn đầy đủ trên Windows:
& "C:\Program Files\dotnet\dotnet.exe" ef database update --project src/ClinicAppointment.Infrastructure --startup-project src/ClinicAppointment.Api
```
*(Cơ sở dữ liệu sẽ tự động kích hoạt extension `btree_gist`, tạo 15 bảng nghiệp vụ, thiết lập các ràng buộc GiST và khởi tạo dữ liệu mẫu seeder).*

---

### Bước 2: Khởi chạy Backend Web API

```powershell
cd d:\Clinic_Appointment_Management_Project\backend
# Lệnh tiêu chuẩn:
dotnet run --project src/ClinicAppointment.Api

# Hoặc dùng đường dẫn đầy đủ trên Windows:
& "C:\Program Files\dotnet\dotnet.exe" run --project src/ClinicAppointment.Api
```
- **Web API Endpoint:** `http://localhost:5000` (hoặc `https://localhost:5001`)
- **Tài liệu Swagger OpenAPI:** `http://localhost:5000/swagger`
- **Kiểm tra trạng thái sức khỏe (Health Check):** `http://localhost:5000/health` (trả về HTTP 200 OK trạng thái Healthy khi DB online).

---

### Bước 3: Khởi chạy Frontend React SPA

Mở một cửa sổ PowerShell mới:
```powershell
cd d:\Clinic_Appointment_Management_Project\frontend
npm install
npm run dev
```
- **Ứng dụng Web mở tại:** `http://localhost:5173`
- Giao diện người dùng sẽ tự động kết nối tới Backend API tại `http://localhost:5000/api/v1`.

---

## 5. Bảng tài khoản mẫu Demo (Seeded Accounts)

Hệ thống cung cấp sẵn các tài khoản demo đại diện cho mọi vai trò trong phòng khám. Bạn có thể sử dụng các nút **"Đăng nhập nhanh (Demo)"** trên giao diện hoặc đăng nhập thủ công:

| Vai trò | Email đăng nhập | Mật khẩu mặc định | Chức năng truy cập |
| :--- | :--- | :--- | :--- |
| **ADMIN** | `admin@clinic.com` | `Admin@123456` | Dashboard tổng quan, Đối soát tài chính, Quản lý Chuyên khoa, Bác sĩ, Dịch vụ & Lịch làm việc. |
| **RECEPTIONIST** | `receptionist@clinic.com` | `Reception@123456` | Bàn Lễ tân, Xác nhận ca khám mới, Duyệt/Từ chối đổi lịch, Check-in, Xử lý hồ sơ nghi trùng. |
| **DOCTOR** (Nội khoa) | `doctor.an@clinic.com` | `Doctor@123456` | Bác sĩ Nguyễn Văn An: Lịch khám hôm nay, Tiếp nhận khám, Ghi bệnh án lâm sàng, Khóa bệnh án. |
| **DOCTOR** (Tim mạch) | `doctor.binh@clinic.com` | `Doctor@123456` | Bác sĩ Trần Thị Bình: Lịch khám được phân công, Quy trình khám bệnh. |
| **PATIENT** | `patient.hoa@gmail.com` | `Patient@123456` | Bệnh nhân có tài khoản: Tra cứu và quản lý lịch khám cá nhân. |
| **GUEST** | *(Không cần đăng nhập)* | *(Không cần mật khẩu)* | Khách vãng lai: Đặt lịch 4 bước, tra cứu/đổi/hủy lịch qua mã xác thực OTP gửi về điện thoại. |

---

## 6. Hướng dẫn chạy kiểm thử tự động

Hệ thống được trang bị bộ kiểm thử tự động toàn diện, xác nhận mọi quy tắc nghiệp vụ và ràng buộc kỹ thuật:

### Kiểm thử Backend (xUnit)
```powershell
# Chạy toàn bộ 514 backend tests (100% PASS)
dotnet test backend/ClinicAppointment.sln

# Hoặc dùng đường dẫn đầy đủ:
& "C:\Program Files\dotnet\dotnet.exe" test d:\Clinic_Appointment_Management_Project\backend\ClinicAppointment.sln

# Chạy riêng bộ kiểm thử ranh giới hoàn tiền 60 phút
& "C:\Program Files\dotnet\dotnet.exe" test d:\Clinic_Appointment_Management_Project\backend\ClinicAppointment.sln --filter "FullyQualifiedName~RefundBoundaries"

# Chạy riêng kiểm thử Concurrency trên PostgreSQL thực tế
& "C:\Program Files\dotnet\dotnet.exe" test d:\Clinic_Appointment_Management_Project\backend\ClinicAppointment.sln --filter "FullyQualifiedName~PostgresGistConcurrency"
```

### Kiểm thử Frontend (Vitest & RTL)
```powershell
# Chạy toàn bộ 9 frontend journey tests (100% PASS)
npm --prefix d:\Clinic_Appointment_Management_Project\frontend test

# Kiểm tra build bundle tĩnh của Frontend (0 errors)
npm --prefix d:\Clinic_Appointment_Management_Project\frontend run build
```

---

## 7. Triển khai môi trường Production (Docker Compose)

Hệ thống hỗ trợ triển khai sẵn sàng bằng Docker Compose với mô hình bảo mật cô lập:
- PostgreSQL không mở cổng ra ngoài máy chủ (Zero Trust).
- Backend Kestrel chạy dưới tài khoản không đặc quyền `appuser` (non-root UID 1000).
- Nginx Reverse Proxy Gateway chuyển hướng 301 toàn bộ HTTP cổng 80 sang HTTPS cổng 443, áp dụng HSTS 1 năm và bộ tiêu đề bảo mật.

```powershell
cd d:\Clinic_Appointment_Management_Project

# 1. Khởi tạo tệp môi trường từ mẫu
Copy-Item .env.example .env

# 2. Sinh chứng chỉ TLS tạm thời (hoặc gắn chứng chỉ hợp lệ vào deployment/nginx/ssl/)
New-Item -ItemType Directory -Force -Path deployment/nginx/ssl
# Xem chi tiết cách sinh certs trong docs/DEPLOYMENT_RUNBOOK.md

# 3. Khởi chạy 4 container production
docker compose -f docker-compose.prod.yml up -d --build

# 4. Kiểm tra trạng thái hoạt động của các dịch vụ
docker compose -f docker-compose.prod.yml ps
```

---

## 8. Kịch bản sao lưu và phục hồi cơ sở dữ liệu

Dự án cung cấp sẵn kịch bản PowerShell (`.ps1`) và Bash (`.sh`) tự động hóa quy trình quản trị dữ liệu:

### Sao lưu dữ liệu (Backup)
```powershell
# Thực hiện sao lưu nén nhị phân chuẩn pg_dump
.\scripts\backup-database.ps1
# Tệp dump lưu tại: database/backups/clinic_backup_YYYYMMDD_HHMMSS.dump
```

### Phục hồi dữ liệu (Restore & Integrity Verification Drill)
```powershell
# Phục hồi dữ liệu từ tệp dump kèm quy trình kiểm tra tính toàn vẹn 3 bước
.\scripts\restore-database.ps1 -BackupFilePath .\database\backups\clinic_backup_YYYYMMDD_HHMMSS.dump
```
*Kịch bản tự động kiểm tra số lượng bảng cơ sở dữ liệu, xác thực sự tồn tại của ràng buộc GiST chống trùng lịch `ex_appointments_doctor_no_overlap` và kiểm tra nhật ký kiểm toán `audit_logs`.*

---

## 9. Các hạn chế đã biết và thành phần TBD (Known Limitations & TBD)

Theo quy định kiến trúc trong `AGENTS.md`, các dịch vụ phụ thuộc vào đối tác bên thứ ba được trừu tượng hóa và có sẵn Dev Stubs:

1. **SMS Gateway Provider (TBD):**
   - *Hiện trạng:* Đang sử dụng abstraction `ISmsSender` và `DevStubSmsChannel`. Mã OTP và tin nhắn gửi lịch khám được in ra log môi trường phát triển (được mask an toàn) và trả về trong phản hồi API ở môi trường dev để dễ dàng thử nghiệm.
   - *Khi go-live:* Thay thế bằng adapter của nhà mạng viễn thông (eSMS, VNPT, Viettel, Twilio) trong `DependencyInjection.cs`.
2. **Cổng thanh toán trực tuyến (TBD):**
   - *Hiện trạng:* Hệ thống quản lý snapshot chi phí `ServicePriceSnapshot` và đối soát hoàn tiền độc lập tại quầy thông qua bảng `refunds` (`/api/v1/reception/refunds/{id}/process`).
   - *Khi go-live:* Bổ sung webhook IPN cho các cổng thanh toán trực tuyến như VNPay, MoMo, VietQR.
3. **Email SMTP Provider (TBD):**
   - *Hiện trạng:* Đang sử dụng `IEmailSender` và `DevStubEmailChannel`.
   - *Khi go-live:* Cấu hình thông số máy chủ SMTP (SendGrid, Amazon SES, Google Workspace) trong `appsettings.Production.json`.

---

## 10. Tài liệu kỹ thuật chi tiết

- [Báo cáo bàn giao và nghiệm thu dự án (docs/HANDOVER.md)](file:///d:/Clinic_Appointment_Management_Project/docs/HANDOVER.md)
- [Tài liệu vận hành triển khai và xử lý sự cố (docs/DEPLOYMENT_RUNBOOK.md)](file:///d:/Clinic_Appointment_Management_Project/docs/DEPLOYMENT_RUNBOOK.md)
- [Đặc tả yêu cầu phần mềm SRS v1.2 (docs/SPEC.md)](file:///d:/Clinic_Appointment_Management_Project/docs/SPEC.md)
- [Quy tắc nghiệp vụ phòng khám (docs/BUSINESS_RULES.md)](file:///d:/Clinic_Appointment_Management_Project/docs/BUSINESS_RULES.md)
- [Thiết kế kiến trúc hệ thống (docs/ARCHITECTURE.md)](file:///d:/Clinic_Appointment_Management_Project/docs/ARCHITECTURE.md)
- [Thiết kế cơ sở dữ liệu PostgreSQL v1.2 (docs/DATABASE_DESIGN.md)](file:///d:/Clinic_Appointment_Management_Project/docs/DATABASE_DESIGN.md)
- [Đặc tả RESTful Web API v1 (docs/API_SPECIFICATION.md)](file:///d:/Clinic_Appointment_Management_Project/docs/API_SPECIFICATION.md)
- [Nhật ký triển khai chi tiết 55 tính năng (planning/implementation_log.md)](file:///d:/Clinic_Appointment_Management_Project/planning/implementation_log.md)

---
*© 2026 Clinic Appointment Management System — Bàn giao toàn diện và hoàn tất nghiệm thu.*
