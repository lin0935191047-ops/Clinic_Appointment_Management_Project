# Implementation Log

Chỉ ghi công việc thực sự đã thực hiện; tài liệu Harness không phải implementation evidence.

## Template
```md
### YYYY-MM-DD — FEAT-XXX — Feature name
- Status before/after:
- Scope implemented:
- Files changed:
- Commands executed:
- Tests executed/results:
- Acceptance criteria evidence:
- Known issues:
- Documentation updated:
```

## Entries

### 2026-11-14 — FEAT-005 & FEAT-006 — Solution Clean Architecture & Docker PostgreSQL Setup
- Status before/after: not_started -> in_progress
- Scope implemented:
  - Khởi tạo tệp cấu hình Docker Compose `docker-compose.yml` cho PostgreSQL 16 Alpine và Mailpit dev server.
  - Khởi tạo cấu trúc Solution `ClinicAppointment.sln` phân tầng Clean Architecture (.NET 8):
    - `ClinicAppointment.Domain`: BaseEntity, Enums, Patient, Appointment, Doctor, Specialty, Service, DoctorSchedule, RescheduleRequest, Refund, OtpChallenge, RefundCalculator.
    - `ClinicAppointment.Application`: IApplicationDbContext, ApiResponse envelope, Result model, ValidationBehavior, MediatR handler & FluentValidation validator cho `BookGuestAppointmentCommand`.
    - `ClinicAppointment.Infrastructure`: ApplicationDbContext, Fluent API configurations (Patient check constraint, Appointment partial unique index, RescheduleRequest index), DependencyInjection cấu hình Npgsql snake_case.
    - `ClinicAppointment.Api`: Program.cs, Swagger OpenAPI, ExceptionHandlingMiddleware, BaseApiController, HealthController (`/health` & `/api/v1/health`), AppointmentsController (`/api/v1/appointments/guest`), appsettings.json.
    - `tests/ClinicAppointment.Domain.Tests`: RefundCalculationTests (ranh giới 61/60/59 phút và <= 0 phút), PatientValidationTests (FullName + DOB / BirthYear).
- Files changed:
  - `docker-compose.yml`
  - `backend/Directory.Build.props`
  - `backend/ClinicAppointment.sln`
  - `backend/src/ClinicAppointment.Domain/*`
  - `backend/src/ClinicAppointment.Application/*`
  - `backend/src/ClinicAppointment.Infrastructure/*`
  - `backend/src/ClinicAppointment.Api/*`
  - `backend/tests/ClinicAppointment.Domain.Tests/*`
- Commands executed:
  - Verification of local environment: Windows Service `postgresql-x64-18` đang chạy trên cổng 5432.
  - Setup of docker-compose file with configurable port.
- Known issues:
  - Máy tính chưa cài sẵn `dotnet` SDK CLI trong PATH môi trường. Cần cài đặt .NET 8 SDK để biên dịch và chạy `dotnet test`.
- Documentation updated:
  - `planning/implementation_log.md`
