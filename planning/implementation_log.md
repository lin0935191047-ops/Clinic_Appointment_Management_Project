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

### 2026-11-14 — FEAT-008, FEAT-009, FEAT-010 — EF Core Migrations, GiST Exclusion, Idempotent Seeder & Users/Roles
- Status before/after:
  - FEAT-001 -> FEAT-004: not_started -> completed (Tất cả tiêu chuẩn nghiệm thu & tài liệu đã sẵn sàng)
  - FEAT-008: not_started -> completed
  - FEAT-009: not_started -> completed
  - FEAT-010: not_started -> completed
- Scope implemented:
  - Cấu hình EF Core DbContext với Npgsql snake-case naming convention, chuỗi kết nối an toàn qua configuration/env.
  - Cài đặt công cụ CLI `dotnet-ef` phiên bản 11.0.0.
  - Khởi tạo migration `20261114062800_InitialCreate.cs` chứa đầy đủ 14 bảng dữ liệu cốt lõi, bổ sung câu lệnh kích hoạt extension `btree_gist` và ràng buộc chống trùng slot lịch khám của bác sĩ `ex_appointments_doctor_no_overlap` (EXCLUDE USING gist).
  - Xuất kịch bản SQL idempotent `database/migrations/20261114_initial_schema.sql` (17.4 KB) chạy an toàn trên mọi môi trường PostgreSQL.
  - Xây dựng `DatabaseSeeder.cs` idempotent để tự động khởi tạo 4 Roles hệ thống (ADMIN, DOCTOR, RECEPTIONIST, PATIENT), tài khoản Quản trị viên mặc định `admin@clinic.vn`, 5 chuyên khoa chuẩn và 5 dịch vụ khám cơ bản.
  - Xây dựng hoàn chỉnh mô hình Identity trong `ClinicAppointment.Domain/Entities/Identity.cs` (`User`, `Role`, `UserRoleAssignment`, `RefreshToken`).
  - Xây dựng `PasswordHasher.cs` mã hóa mật khẩu theo chuẩn PBKDF2 HMAC-SHA256 với salt 128-bit ngẫu nhiên và constant-time verification.
  - Xây dựng `RoleAssignmentPolicy.cs` thực thi kiểm soát phân quyền: vai trò Bệnh nhân (PATIENT) cho phép tự gán khi đăng ký; các vai trò quản trị/chuyên môn (ADMIN, DOCTOR, RECEPTIONIST) bắt buộc phải do người dùng có vai trò ADMIN gán; chống gán trùng vai trò.
  - Xây dựng bộ kiểm thử xUnit tự động `IdentityAndRoleTests.cs`: 11/11 test cases ĐẠT 100%, nâng tổng số test case xUnit của toàn solution lên 21/21 tests PASS.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/Identity.cs`
  - `backend/src/ClinicAppointment.Domain/Services/RoleAssignmentPolicy.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IPasswordHasher.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Security/PasswordHasher.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/IdentityConfigurations.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/DatabaseSeeder.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114062800_InitialCreate.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/ApplicationDbContextModelSnapshot.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Api/Program.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/IdentityAndRoleTests.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj`
  - `database/migrations/20261114_initial_schema.sql`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations add InitialCreate ...` (Thành công)
  - `& "$env:USERPROFILE\.dotnet\tools\dotnet-ef.exe" migrations script --idempotent ...` (Thành công sinh SQL 17.4 KB)
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln" --no-restore` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln" --no-build` (Thành công: 21 Passed, 0 Failed)
- Acceptance criteria evidence:
  - Toàn bộ tiêu chí từ FEAT-001 đến FEAT-010 đã được kiểm tra, đáp ứng và có bằng chứng xác thực thực tế.
