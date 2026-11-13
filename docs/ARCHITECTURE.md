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
  Public + Guest Booking | Patient | Reception | Doctor | Admin
                  | HTTPS / JSON
                  v
ASP.NET Core Web API — Modular Monolith
  Presentation: Controllers, DTOs, Middleware
  Application: Use cases, Services, Validators, Policies
  Domain: Entities, Value Objects, State Transitions
  Infrastructure: EF Core, PostgreSQL, external adapters
                  |
                  v
             PostgreSQL

External adapters (TBD): SMS/OTP, Email, Payment/Refund
```

## 3. Style and modules
Đề xuất Modular Monolith + layered boundaries cho v1; không microservices. Modules: Identity & Access; Patient Records & Identity Resolution; Catalog; Scheduling; Appointments; Reschedule/Cancellation/Refund; Reception/Queue; Consultation; Notifications; Reporting/Audit.

## 4. Technology stack
| Layer | Tech | Decision |
|---|---|---|
| Frontend | React.js | Required |
| Language | TypeScript | Recommended; confirm before scaffold |
| State | Redux Toolkit | Required per project direction |
| Router | React Router | Recommended |
| UI | Tailwind CSS | Recommended |
| Backend | C# / ASP.NET Core Web API | Required |
| ORM | Entity Framework Core + Npgsql | Required |
| DB | PostgreSQL | Required; TablePlus is GUI client |
| API docs | OpenAPI/Swagger | Required |
| Backend tests | xUnit + integration tests | Proposed |
| Frontend tests | Vitest + React Testing Library | Proposed |

Chọn .NET LTS và package versions tương thích tại thời điểm scaffold, ghi vào README. Không pin phiên bản cũ theo ví dụ lỗi thời.

## 5. Frontend target structure
```text
frontend/src/
  app/                 # bootstrap, router, store
  components/          # reusable UI
  features/
    auth/
    public-catalog/
    guest-booking/
    appointment-lookup/
    patient/
    reception/
    doctor/
    admin/
  services/            # API client
  hooks/ layouts/ pages/ utils/ types/
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
- SMS provider TBD; dùng interface `ISmsSender`.

## 11. Cancellation/refund architecture
- Lưu service fee snapshot, payment record và cancellation calculation snapshot.
- Server time determines policy: >=60 phút => 100%; <60 và >0 => 50%; <=0 => self-cancel denied.
- Store minutes-before, rate, fee retained, expected refund, policy version và timestamp.
- Refund record tách khỏi Appointment status.
- Gateway chưa chọn; có thể manual processing adapter. Không báo đã hoàn khi status Pending.
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
Local: React dev server, ASP.NET Core API, PostgreSQL local. Production: frontend/API/DB separated, HTTPS, secrets manager, backups, monitoring. Hosting/provider/domain TBD.

## 15. Architecture Decision Records
Quyết định kiến trúc đáng kể phải ghi trong `docs/adr/` và cập nhật docs/Feature dependencies.
