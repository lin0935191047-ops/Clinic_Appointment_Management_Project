# Project Progress

- Baseline: 13/11/2026
- Last updated: 14/11/2026
- Overall status: In progress (Frontend & Backend Foundation Completed)
- Current phase: Database Connection & Migration Configuration (FEAT-008, FEAT-009)

## Summary
Nguồn trạng thái Feature là `feature_list.json`. Không suy ra tiến độ lập trình từ việc tạo tài liệu.

## Features Completed
- `FEAT-005` (Development environment): Node.js v24.18.0, npm 11.16.0, .NET SDK 10.0.404 tại `C:\Program Files\dotnet\dotnet.exe`, PostgreSQL 18 service listening on port 5432.
- `FEAT-006` (ASP.NET Core API bootstrap): Solution Clean Architecture biên dịch thành công 0 warning / 0 error; 10/10 Domain unit tests pass; `/health` endpoint HTTP 200 OK.
- `FEAT-007` (React app bootstrap): React 18 SPA + Vite + TypeScript + Tailwind CSS; luồng Guest Booking 4 bước không cần tài khoản; Tra cứu OTP và tính hoàn phí 60 phút; Kênh Lễ tân; `npm run build` pass 100%.

## Current deliverables drafted & initialized
- SRS baseline: `docs/SPEC.md`
- Business rules: `docs/BUSINESS_RULES.md`
- Architecture: `docs/ARCHITECTURE.md`
- Database logical design: `docs/DATABASE_DESIGN.md` v1.2 & `docs/DATA_DICTIONARY.md`
- ERD: `docs/ERD.md` + 4 domain diagrams
- Harness Audit: `docs/HARNESS_AUDIT.md` (READY)
- Docker Compose: `docker-compose.yml` (PostgreSQL 16 Alpine + Mailpit)
- Backend Clean Architecture Solution:
  - `backend/ClinicAppointment.sln`
  - `backend/src/ClinicAppointment.Domain` (Entities, Enums, RefundCalculator)
  - `backend/src/ClinicAppointment.Application` (MediatR, FluentValidation, BookGuestAppointment)
  - `backend/src/ClinicAppointment.Infrastructure` (DbContext, Npgsql, Fluent API configurations)
  - `backend/src/ClinicAppointment.Api` (Swagger, Middleware, HealthController, AppointmentsController)
  - `backend/tests/ClinicAppointment.Domain.Tests` (Refund boundary tests, Patient validation tests)
- Frontend React SPA:
  - `frontend/src/pages/HomePage.tsx`
  - `frontend/src/pages/DoctorsPage.tsx`
  - `frontend/src/pages/BookingPage.tsx`
  - `frontend/src/pages/LookupPage.tsx`
  - `frontend/src/pages/ReceptionistDashboardPage.tsx`
  - `frontend/dist` production build ready

## Next steps
1. Nhận mật khẩu PostgreSQL local từ người dùng hoặc cấu hình mật khẩu trong `.env`.
2. Tạo database `ClinicAppointmentDb` và cấu hình EF Core Migrations (FEAT-008, FEAT-009).
3. Triển khai API cho Catalog và hoàn thiện tích hợp end-to-end giữa Frontend và Backend.

## Rules
Không tạo Database/Migration hoặc đánh dấu Feature completed chỉ vì đã sinh tài liệu. `completed` yêu cầu acceptance criteria và test evidence thực tế.
