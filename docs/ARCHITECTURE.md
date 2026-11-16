# ARCHITECTURE — Clinic Appointment Management System

Version 1.0 | Proposed baseline — duyệt trước khi scaffold code

## 1. Goals
- Dễ hiểu/bảo trì cho developer mới ASP.NET Core.
- React Frontend và ASP.NET Core Backend tách biệt.
- Phân module theo nghiệp vụ, không nhồi logic trong Controller.
- Bảo vệ lịch khám bằng Application + PostgreSQL constraints.
- Guest Booking không cần login.
- Bảo vệ dữ liệu định danh và thông tin khám.

## 2. High-level architecture
```text
Browser / React.js SPA
  Public + Guest Booking (VietQR) | Patient | Reception | Doctor | Admin
                  | HTTPS / JSON
                  v
ASP.NET Core Web API — Modular Monolith
  Presentation: Controllers, DTOs, Middleware, Webhook PayOS
  Application: Use cases, Services, Validators, Policies
  Domain: Entities, Value Objects, State Transitions
  Infrastructure: EF Core, PostgreSQL, External Adapters
                  |
                  v
             PostgreSQL

External adapters:
  - SMS/OTP: SpeedSMS API (gửi số điện thoại thật) & DevStubSmsChannel
  - Payment: PayOS VietQR SDK (quét mã QR ngân hàng) & Quầy Lễ tân
  - Email: IEmailSender / DevStubEmailChannel
```

## 3. Style and modules
Đề xuất Modular Monolith + layered boundaries cho v1; không microservices. Modules: Identity & Access; Patient Records & Identity Resolution; Catalog; Scheduling; Appointments; Reschedule/Cancellation/Refund; Reception/Queue; Consultation; Notifications; Reporting/Audit.

## 4. Technology stack
| Layer | Tech | Decision |
|---|---|---|
| Frontend | React.js 18 + Vite 6 | Required |
| Language | TypeScript | Required |
| State | Redux Toolkit | Required per project direction |
| Router | React Router 6 | Recommended |
| UI | Tailwind CSS + Lucide Icons | Recommended |
| Backend | C# / ASP.NET Core Web API (.NET 8) | Required |
| ORM | Entity Framework Core + Npgsql | Required |
| DB | PostgreSQL 16/18 + `btree_gist` extension | Required |
| Payment Gateway | PayOS SDK & VietQR NAPAS 24/7 | Integrated |
| SMS Gateway | SpeedSMS API (CSKH Brandname / Verify OTP) | Integrated |
| API docs | OpenAPI/Swagger | Required |
| Backend tests | xUnit + integration tests (522 tests PASS) | Required |
| Frontend tests | Vitest + React Testing Library (16 tests PASS) | Required |
| Deployment | Docker Compose + Render Cloud Blueprint | Production & Cloud |

Chọn .NET LTS và package versions tương thích tại thời điểm scaffold, ghi vào README. Không pin phiên bản cũ theo ví dụ lỗi thời.

## 5. Frontend target structure
```text
frontend/
  public/
    images/doctors/    # 100% ảnh chân dung y tế JPG (dr-*.jpg, default-doctor.jpg)
  src/
    api/               # apiClient, Axios interceptors, JWT handling
    components/        # Reusable UI, Layout, Navbar, Modal, ProtectedRoute
    pages/             # Guest Booking (VietQR), Lookup, Reception, Doctor, Admin Dashboards
    utils/
      doctorAvatars.ts # Thuật toán phân giải bác sĩ đa tiêu chí (100% JPG, không dùng SVG)
    hooks/ layouts/ types/
    test/              # Vitest journey & doctor avatar tests
```
Rules: shared API client; Guest booking không redirect login; UI route guards chỉ phục vụ UX; Backend enforce permission; không lưu OTP/full identifier/health notes vào localStorage.

## 6. Backend target structure
```text
backend/
  ClinicAppointment.sln
  src/
    ClinicAppointment.Api/             # Controllers, Middleware, Program.cs
    ClinicAppointment.Application/     # DTOs, use cases, validators, abstractions
    ClinicAppointment.Domain/          # Entities, enums, value objects, domain rules
    ClinicAppointment.Infrastructure/  # EF Core, repositories, integrations
  tests/
    ClinicAppointment.UnitTests/
    ClinicAppointment.IntegrationTests/
```
Nếu multi-project làm người mới quá tải, có thể bắt đầu một ASP.NET Core project với folder boundaries; phải ghi ADR trước khi thay đổi kiến trúc. Không tạo abstraction vô ích.

### Dependency rules
- Api phụ thuộc Application và Infrastructure composition root.
- Application phụ thuộc Domain và abstractions.
- Infrastructure implement interfaces và dùng EF Core.
- Domain không phụ thuộc ASP.NET Core, EF Core hoặc external provider.
- Controller chỉ xử lý HTTP boundary và dispatch use case.

## 7. Request flow
`HTTP → Middleware → Controller/DTO validation → Application use case → Domain rules → Infrastructure/EF Core → PostgreSQL → response DTO`.

## 8. Database/concurrency
- UUID PK; PostgreSQL `timestamptz` UTC; C# `DateTimeOffset`.
- Appointment lưu `StartAt`, `EndAt`, `DoctorId`, status và service/fee snapshots.
- Đề xuất PostgreSQL GiST exclusion constraint với `tstzrange(start_at,end_at,'[)')` theo doctor và trạng thái đang giữ slot. Cần `btree_gist` hoặc strategy tương đương, migration review và integration test.
- Application check availability cho UX; DB constraint là lớp cuối khi request song song.
- Transaction cho booking, reschedule approve, cancellation/refund record.
- PendingReview reschedule không reserve slot mới; old appointment giữ slot. Khi approve recheck trong transaction.
- Dùng idempotency key cho POST booking/cancel phù hợp để giảm duplicate retry.

## 9. Patient identity
- Patient.UserId nullable unique.
- `PatientIdentifiers` chứa loại định danh; giá trị được encrypted, tìm exact match qua keyed HMAC.
- Định danh mạnh verified có thể có unique constraint sau khi xác định duplicate conflict handling.
- Không unique phone toàn bảng nếu cho phép số dùng chung; xác minh phone cá nhân chính chủ và đánh dấu is_personal.
- Name + DOB/BirthYear là candidate match, không auto-merge.

## 10. OTP architecture
- OtpChallenges: challenge, purpose, booking reference, phone HMAC, OTP hash, expiry, attempts, resendAt, consumedAt.
- Đề xuất mặc định ban đầu TTL 5 phút, tối đa 5 lần thử, resend cooldown 60 giây; security review trước production.
- OTP cryptographically random, one-time, hash at rest, không log.
- Verify tạo scoped short-lived grant theo booking/purpose; không tạo User login session.
- SMS provider: Tích hợp dịch vụ SMS Gateway **SpeedSMS** (`SpeedSmsSender`) gửi mã OTP thật vào điện thoại; duy trì `DevStubSmsChannel` cho môi trường dev/test khi chưa cấu hình token.

## 11. Cancellation/refund architecture
- Lưu service fee snapshot, payment record và cancellation calculation snapshot.
- Server time determines policy: >=60 phút => 100%; <60 và >0 => 50%; <=0 => self-cancel denied.
- Store minutes-before, rate, fee retained, expected refund, policy version và timestamp.
- Refund record tách khỏi Appointment status.
- Payment Gateway: Đã tích hợp Cổng thanh toán quét mã **VietQR (PayOS)** tự động sinh mã VietQR chuẩn ngân hàng khi đặt lịch hoặc tra cứu, đồng thời duy trì đối soát hoàn tiền độc lập tại quầy thông qua bảng `refunds` (`/api/v1/reception/refunds/{id}/process`).
- Webhook PayOS xử lý xác thực chữ ký HMAC-SHA256 tự động cập nhật ca khám sang `Confirmed`.
- Partial/unpaid-fee collection TBD.

## 12. Security and observability
- Password hashing bằng ASP.NET Core Identity/hasher chuyên dụng.
- JWT access ngắn hạn; refresh rotation/revocation nếu triển khai.
- Role + resource ownership checks.
- Rate limit public/OTP/lookup; generic response chống booking enumeration.
- Secrets từ User Secrets/environment/secret manager; không commit.
- Encrypt sensitive identifiers; HTTPS production; redact logs.
- CORS allowlist, DTO validation, ProblemDetails, correlation ID, health checks.

## 13. Testing
- Unit: refund calculator (61/60/59), state transitions, identity matching.
- Integration: PostgreSQL migrations, transactions, exclusion constraints; không chỉ EF InMemory.
- API: OTP grant, role/resource authorization, 401/403/409/429.
- Concurrency: two parallel bookings same doctor/slot.
- Frontend: guest booking, forms, role routes, loading/error state.
- E2E: guest booking → OTP lookup → reschedule approval → cancellation.

## 14. Deployment
- **Local Development:** React dev server (port 5173), ASP.NET Core API (port 5000), PostgreSQL local (port 5432).
- **Production (Docker Compose):** Frontend/API/DB separated, Nginx Gateway Reverse Proxy chuyển hướng HTTPS 301, HSTS 1 năm, Zero Trust isolated database network, tự động hóa sao lưu và phục hồi qua scripts (`backup-database.ps1`, `restore-database.ps1`).
- **Cloud Deployment (Render Blueprint):** Tệp cấu hình `render.yaml` chuẩn hóa triển khai multi-service: Web Service Backend ASP.NET Core (.NET 8 SDK build), Static Site Frontend React SPA (Vite production build), và Managed PostgreSQL Database. Chi tiết hướng dẫn trong `docs/DEPLOYMENT_RUNBOOK.md`.

## 15. Architecture Decision Records
Quyết định kiến trúc đáng kể phải ghi trong `docs/adr/` và cập nhật docs/Feature dependencies.
