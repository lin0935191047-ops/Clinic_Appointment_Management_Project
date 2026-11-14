# Project Progress

- Baseline: 13/11/2026
- Last updated: 14/11/2026
- Overall status: In progress (Solution & Foundation initialized)
- Current phase: Foundation & Backend Bootstrap (FEAT-005, FEAT-006)

## Summary
Nguồn trạng thái Feature là `feature_list.json`. Không suy ra tiến độ lập trình từ việc tạo tài liệu.

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

## Next steps
1. Cài đặt .NET 8 SDK trên Windows để chạy build và `dotnet test`.
2. Khởi chạy PostgreSQL (qua `docker compose up -d` hoặc sử dụng dịch vụ local PostgreSQL 18 đang chạy sẵn).
3. Chạy kiểm thử tự động `dotnet test` và ghi nhận test evidence để hoàn thành FEAT-005 / FEAT-006.
4. Triển khai DbContext connection và migration đầu tiên (FEAT-008, FEAT-009).

## Rules
Không tạo Database/Migration hoặc đánh dấu Feature completed chỉ vì đã sinh tài liệu. `completed` yêu cầu acceptance criteria và test evidence thực tế.
