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

### 2026-11-14 — FEAT-005, FEAT-006, FEAT-007 — Build Verification, Domain Tests & React Frontend Bootstrap
- Status before/after:
  - FEAT-005: not_started -> completed
  - FEAT-006: not_started -> completed
  - FEAT-007: not_started -> completed
- Scope implemented:
  - Xác thực công cụ môi trường: .NET SDK 10.0.404 / runtime 10.0.15 tại `C:\Program Files\dotnet\dotnet.exe`, Node.js v24.18.0, npm 11.16.0, local PostgreSQL 18 trên cổng 5432.
  - Biên dịch thành công giải pháp Clean Architecture `backend/ClinicAppointment.sln` với 0 warnings, 0 errors.
  - Thực thi kiểm thử tự động xUnit: 10/10 Domain unit tests pass (kiểm tra ranh giới hoàn tiền 60 phút và xác thực bệnh nhân bắt buộc Họ tên + Ngày sinh/Năm sinh).
  - Khởi chạy và kiểm tra Web API endpoint: `http://localhost:5000/health` trả về HTTP 200 OK trạng thái Healthy với ApiResponse envelope chuẩn.
  - Khởi tạo và xây dựng toàn diện ứng dụng Frontend SPA (React 18 + TypeScript + Vite + Tailwind CSS):
    - `src/components/layout/Navbar.tsx` & `Footer.tsx`: Cung cấp điều hướng, logo nhận diện y tế, và ghi chú chính sách Guest Booking không cần đăng nhập.
    - `src/pages/HomePage.tsx`: Giới thiệu 4 bước đặt lịch, chuyên khoa nổi bật, cam kết hoàn phí minh bạch.
    - `src/pages/DoctorsPage.tsx`: Danh sách bác sĩ chuyên khoa, bộ lọc chuyên khoa, giá khám và thông tin chi tiết.
    - `src/pages/BookingPage.tsx`: Luồng đặt lịch 4 bước hoàn chỉnh không cần tài khoản, lựa chọn Ngày sinh/Năm sinh, tự động chống trùng slot, sinh mã `BookingCode` và hướng dẫn bảo lưu.
    - `src/pages/LookupPage.tsx`: Tra cứu lịch hẹn bằng mã đặt lịch + OTP xác thực qua số điện thoại; Modal yêu cầu Đổi lịch (trạng thái Chờ Lễ tân duyệt, lịch cũ giữ nguyên); Modal Hủy lịch với thuật toán tính hoàn phí chính xác theo mốc 60 phút ($\ge 60$ phút: hoàn 100%, $< 60$ phút: hoàn 50%).
    - `src/pages/ReceptionistDashboardPage.tsx`: Kênh bàn lễ tân tiếp nhận (xác nhận lịch pending, phê duyệt/từ chối đổi lịch, check-in bệnh nhân, cảnh báo hồ sơ nghi trùng).
    - `npm run build` (`tsc && vite build`) thực thi biên dịch thành công 100% không có lỗi.
- Files changed:
  - `frontend/package.json`
  - `frontend/tsconfig.json`
  - `frontend/vite.config.ts`
  - `frontend/tailwind.config.js`
  - `frontend/postcss.config.js`
  - `frontend/index.html`
  - `frontend/src/vite-env.d.ts`
  - `frontend/src/index.css`
  - `frontend/src/types/index.ts`
  - `frontend/src/api/client.ts`
  - `frontend/src/components/layout/Navbar.tsx`
  - `frontend/src/components/layout/Footer.tsx`
  - `frontend/src/pages/HomePage.tsx`
  - `frontend/src/pages/DoctorsPage.tsx`
  - `frontend/src/pages/BookingPage.tsx`
  - `frontend/src/pages/LookupPage.tsx`
  - `frontend/src/pages/ReceptionistDashboardPage.tsx`
  - `frontend/src/App.tsx`
  - `frontend/src/main.tsx`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" restore "backend/ClinicAppointment.sln"`
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln" --no-restore` (Thành công: 0 Warning, 0 Error)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln" --no-build` (Thành công: 10 Passed, 0 Failed)
  - `Invoke-RestMethod -Uri "http://localhost:5000/health"` (Thành công: HTTP 200 OK)
  - `npm install` trong `frontend/` (Thành công: 165 packages added)
  - `npm run build` trong `frontend/` (Thành công: tsc && vite build thành công)
- Acceptance criteria evidence:
  - FEAT-005, FEAT-006, FEAT-007 đều đạt đầy đủ tiêu chí nghiệm thu và có kết quả kiểm thử thực tế.
- Documentation updated:
  - `planning/feature_list.json`, `planning/progress.md`, `planning/implementation_log.md`
