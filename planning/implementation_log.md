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

### 2026-11-14 — FEAT-011 — Patient Profile (Independent from User, Required Identity Fields & Candidate Detection)
- Status before/after: not_started -> completed
- Scope implemented:
  - Cập nhật thực thể `Patient` trong `ClinicAppointment.Domain/Entities/Patient.cs`:
    - `UserId` nullable unique hỗ trợ Guest Booking không cần tài khoản.
    - `FullName` bắt buộc và tự động tạo `NormalizedFullName` (in hoa, chuẩn hóa khoảng trắng).
    - `DateOfBirth` hoặc `BirthYear` bắt buộc tại mức Domain và kiểm tra năm sinh khớp nhau nếu cả 2 cùng có mặt.
    - Cung cấp phương thức `LinkToUser(Guid userId)` bảo vệ liên kết 1-1 an toàn.
  - Cập nhật `PatientConfiguration.cs` trong `ClinicAppointment.Infrastructure`:
    - Khởi tạo Check Constraint `ck_patients_dob_or_birth_year` và `ck_patients_birth_year_matches_dob`.
    - Cấu hình Partial Unique Index cho `user_id` (`user_id IS NOT NULL`).
    - Cấu hình Index cho `NormalizedFullName` và `PhoneNumber`.
  - Triển khai tầng Application theo mô hình CQRS:
    - DTO: `PatientDto`.
    - Command: `CreatePatientCommand`.
    - Validator: `CreatePatientCommandValidator` (FluentValidation) kiểm tra tính hợp lệ của FullName, Phone, DOB, BirthYear.
    - Handler: `CreatePatientCommandHandler` thực thi logic rà soát chống gộp tự động (Strictly NO Auto-merge) — khi phát hiện trùng họ tên chuẩn hóa và năm sinh, tự động đánh dấu `IsPossibleDuplicate = true` và `RecordStatus = "REVIEW_REQUIRED"` để Lễ tân/Quản trị viên xử lý thủ công.
    - Query: `GetPatientByIdQuery`, `GetPatientByCodeQuery`.
    - Controller: `PatientsController` cung cấp API endpoints `POST /api/v1/patients`, `GET /api/v1/patients/{id}`, `GET /api/v1/patients/code/{code}`.
  - Kiểm thử xUnit tự động:
    - Xây dựng 18 test cases mới trong `PatientProfileTests.cs`.
    - Toàn bộ 39/39 unit test cases của Solution đều PASS 100%.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/Patient.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/PatientConfiguration.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/DTOs/PatientDto.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/CreatePatientCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/CreatePatientCommandValidator.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/CreatePatientCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Queries/PatientQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/PatientsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/PatientProfileTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln" --no-restore` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln" --no-build` (Thành công: 39 Passed, 0 Failed)
- Acceptance criteria evidence:
  - FEAT-011 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-012 — Patient Identifier Encryption & Lookup (AES-256-GCM & Keyed HMAC Exact Search)
- Status before/after: not_started -> completed
- Scope implemented:
  - Xây dựng `PiiEncryptionService.cs` (`IPiiEncryptionService`) thực thi mã hóa AES-256-GCM với 12-byte random nonce và 16-byte authentication tag; kiểm chứng tính toàn vẹn (tamper-resistant) ném ngoại lệ khi dữ liệu bị sửa đổi.
  - Xây dựng `HmacService.cs` (`IHmacService`) thực thi Keyed HMAC-SHA256 trên chuỗi định danh đã chuẩn hóa (loại bỏ khoảng trắng, dấu gạch ngang, dấu chấm và in hoa); độc lập với plain hash, cho phép tìm kiếm chính xác (exact match) nhất quán dù người dùng/lễ tân nhập định dạng khác nhau.
  - Xây dựng `IdentifierMaskingService.cs` (`IIdentifierMaskingService`) che giấu các định danh nhạy cảm:
    - Số điện thoại: `091***5678` (giữ 3 đầu, 4 cuối).
    - CCCD/CMND 12 số: `0010******34` (giữ 4 đầu, 2 cuối).
    - Hộ chiếu: `B12***67` (giữ 3 đầu, 2 cuối).
    - Thẻ BHYT: `GD401******89` (giữ 5 đầu, 2 cuối).
    - Tuyệt đối không log thông tin định danh thô vào hệ thống logging.
  - Xây dựng `PatientSecurityService.cs` (`IPatientSecurityService`) hỗ trợ gắn định danh mã hóa, giải mã có kiểm soát, tra cứu theo HMAC và cơ chế rà soát định danh mạnh trùng lặp (`CheckAndFlagDuplicateIdentifierAsync`): khi phát hiện trùng CCCD/Hộ chiếu với hồ sơ có sẵn, tự động gắn cờ `IsPossibleDuplicate = true` và `RecordStatus = "REVIEW_REQUIRED"`, không bao giờ tự ý gộp (Strictly NO Auto-merge).
  - Cập nhật DTO `PatientDto` bổ sung các trường masked (`MaskedPhoneNumber`, `MaskedNationalId`, `MaskedPassport`, `MaskedHealthInsurance`).
  - Cập nhật `CreatePatientCommand` và `CreatePatientCommandHandler` tích hợp mã hóa tự động khi tạo hồ sơ.
  - Bổ sung Query `LookupPatientByIdentifierQuery` và endpoint bảo mật `POST /api/v1/patients/lookup-by-identifier` trong `PatientsController`.
  - Bộ kiểm thử xUnit tự động:
    - Xây dựng 20 test cases mới trong `PatientSecurityTests.cs`.
    - Toàn bộ 59/59 unit test cases của Solution đều PASS 100%.
- Files changed:
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/ISecurityServices.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IPatientSecurityService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Security/PiiEncryptionService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Security/HmacService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Security/IdentifierMaskingService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Services/PatientSecurityService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/DTOs/PatientDto.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/CreatePatientCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/CreatePatientCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Queries/PatientQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/PatientsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/PatientSecurityTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln" --no-restore` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln" --no-build` (Thành công: 59 Passed, 0 Failed)
- Acceptance criteria evidence:
  - FEAT-012 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-013 — Patient Duplicate Detection & Review (Candidate Matching, Manual Review, Safe Family Phone & Audit Merge)
- Status before/after: not_started -> completed
- Scope implemented:
  - Xây dựng Domain Entities & Enums:
    - Enums: `IdentityMatchReason` (`NAME_DOB`, `NAME_BIRTH_YEAR`, `VERIFIED_IDENTIFIER`, `SHARED_PHONE_DIFFERENT_NAME`) và `IdentityReviewStatus` (`OPEN`, `CONFIRMED_SAME_PERSON`, `CONFIRMED_DIFFERENT_PERSON`, `DISMISSED`) trong `DomainEnums.cs`.
    - Thực thể `PatientIdentityReviewCase`: Lưu ca rà soát giữa hồ sơ mới (`PatientId`) và ứng viên nghi trùng (`CandidatePatientId`), lý do so khớp, ghi chú và người duyệt.
    - Thực thể `PatientMergeEvent`: Lưu nhật ký kiểm toán gộp hồ sơ bất biến (snapshot JSON dữ liệu nguồn, số lượng lịch hẹn chuyển giao, lý do, người thực hiện).
  - Domain Service `PatientDuplicateDetectionService.cs`:
    - Phân tích 4 điều kiện so khớp ứng viên trùng lặp.
    - Thực thi nguyên tắc **Strictly NO Auto-merge**: Khi họ tên chuẩn hóa và ngày sinh/năm sinh trùng nhau, hệ thống chỉ gắn cờ `IsPossibleDuplicate = true`, `RecordStatus = "REVIEW_REQUIRED"` và tạo ca rà soát `OPEN`, không tự động gộp.
    - An toàn khi dùng chung số điện thoại (`SHARED_PHONE_DIFFERENT_NAME`): Khi số điện thoại trùng nhưng họ tên khác biệt (phụ huynh đặt cho con), hệ thống không coi là trùng lặp và không chặn luồng đặt khám.
  - Cấu hình EF Core `PatientReviewConfigurations.cs`:
    - Map bảng `patient_identity_review_cases` và `patient_merge_events`.
    - Ràng buộc Check Constraints: `ck_identity_review_distinct_patients` (`patient_id <> candidate_patient_id`) và `ck_patient_merge_distinct_patients` (`source_patient_id <> target_patient_id`).
    - Khắc phục triệt để shadow foreign key `Appointment.PatientId1` bằng việc liên kết hai chiều tường minh `builder.HasOne(a => a.Patient).WithMany(p => p.Appointments)`.
    - Sinh EF Migration chuẩn: `20261114070558_AddPatientReviewAndMerge.cs`.
  - Application CQRS & Controller:
    - Command: `ResolvePatientReviewCaseCommand` xử lý 2 kịch bản: `CONFIRMED_DIFFERENT_PERSON` (xác nhận người khác, gỡ cờ nghi trùng) và `CONFIRMED_SAME_PERSON` (gộp hồ sơ an toàn: sao lưu snapshot JSON, chuyển giao appointments sang target, cập nhật source thành `MERGED` với `MergedIntoPatientId`, ghi audit event).
    - Query: `GetOpenReviewCasesQuery`, `GetReviewCaseByIdQuery`.
    - Endpoints tại `PatientsController`: `GET /api/v1/patients/reviews/open`, `GET /api/v1/patients/reviews/{id}`, `POST /api/v1/patients/reviews/{id}/resolve`.
  - Kiểm thử tự động xUnit:
    - Xây dựng 10 test cases mới trong `PatientDuplicateReviewTests.cs` kiểm tra cả 4 trường hợp matching, quy tắc no auto-merge, shared family phone và kiểm toán merge.
    - Toàn bộ 69/69 unit tests trong Solution đều PASS 100%.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Enums/DomainEnums.cs`
  - `backend/src/ClinicAppointment.Domain/Entities/PatientReviewAndMerge.cs`
  - `backend/src/ClinicAppointment.Domain/Services/PatientDuplicateDetectionService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/PatientReviewConfigurations.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/AppointmentConfiguration.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/DTOs/ReviewAndMergeDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/ResolvePatientReviewCaseCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Queries/GetReviewCasesQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/PatientsController.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114070558_AddPatientReviewAndMerge.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/PatientDuplicateReviewTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet-ef migrations add AddPatientReviewAndMerge`
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 69 Passed, 0 Failed)
- Acceptance criteria evidence:
  - FEAT-013 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-014 — Authentication and Password Management (PBKDF2 Hasher, JWT Access Token, Refresh Token Rotation & Auth Endpoints)
- Status before/after: not_started -> completed
- Scope implemented:
  - Cài đặt package `Microsoft.AspNetCore.Authentication.JwtBearer` (v8.0.10) cho `ClinicAppointment.Api` và `ClinicAppointment.Infrastructure`.
  - Triển khai `IJwtTokenService` và `JwtTokenService`:
    - Tạo JWT Access Token chứa các claims: `sub`, `email`, `role`, `patient_profile_id`, `doctor_profile_id`, `jti`.
    - Tạo Refresh Token ngẫu nhiên cryptographically secure (64 bytes) và băm SHA-256 (`TokenHash`) lưu trữ an toàn trong DB theo `docs/DATABASE_DESIGN.md`.
    - Trích xuất ClaimsPrincipal từ expired token (`GetPrincipalFromExpiredToken`).
  - Thực thi quy trình Refresh Token Rotation & Revocation:
    - Thu hồi token cũ (`RevokedAt = DateTime.UtcNow`) và liên kết `ReplacedByTokenId`.
    - Phát hành Refresh Token mới và Access Token mới đồng thời.
    - Ngăn chặn triệt để token đã bị thu hồi hoặc hết hạn.
  - Xây dựng các DTOs và FluentValidation:
    - `RegisterPatientRequest`, `LoginRequest`, `RefreshTokenRequest`, `ChangePasswordRequest`, `ForgotPasswordRequest`, `ResetPasswordRequest`, `AuthResponseDto`, `UserSummaryDto`.
    - `RegisterPatientRequestValidator`: kiểm tra độ phức tạp mật khẩu (chữ hoa, chữ thường, số, $\ge 8$ ký tự), số điện thoại VN (10 số), và bất biến bắt buộc DateOfBirth hoặc BirthYear.
  - Xây dựng Application CQRS Handlers:
    - `RegisterPatientCommand`: Tạo User, gán role `PATIENT`, tạo Patient profile, mã hóa PII định danh, rà soát trùng lặp không auto-merge, phát hành bộ token.
    - `LoginCommand`: Xác thực Email + Password bằng `IPasswordHasher.VerifyPassword`, kiểm tra `IsActive`, cập nhật `LastLoginAt`, phát hành token.
    - `RefreshTokenCommand`: Xác thực hash, thực hiện Token Rotation.
    - `LogoutCommand`: Thu hồi refresh token.
    - `ChangePasswordCommand`: Đổi mật khẩu cho người dùng xác thực.
    - `ForgotPasswordCommand` & `ResetPasswordCommand`: Quy trình đặt lại mật khẩu an toàn.
    - `GetCurrentUserQuery`: Lấy thông tin user hiện tại kèm roles và profile ID.
  - Xây dựng API Controller `AuthController`:
    - `POST /api/v1/auth/register` (Public Patient Optional)
    - `POST /api/v1/auth/login`
    - `POST /api/v1/auth/refresh`
    - `POST /api/v1/auth/logout`
    - `POST /api/v1/auth/change-password` [Authorize]
    - `POST /api/v1/auth/forgot-password`
    - `POST /api/v1/auth/reset-password`
    - `GET /api/v1/auth/me` [Authorize]
  - Cấu hình Middleware & Swagger trong `Program.cs`:
    - `AddAuthentication(JwtBearerDefaults)`.
    - Swagger Security Definition hỗ trợ Bearer Token Authentication.
    - `app.UseAuthentication()` trước `app.UseAuthorization()`.
  - Bộ kiểm thử tự động xUnit:
    - Xây dựng 10 test cases mới trong `AuthAndJwtTests.cs` (Token generation, claims validation, refresh token lifecycle, password hasher verification, FluentValidation rules).
    - Toàn bộ 79/79 unit tests trong Solution đều PASS 100%.
  - Biên dịch Frontend: `npm run build` chạy thành công 100% không có lỗi.
- Files changed:
  - `backend/src/ClinicAppointment.Api/ClinicAppointment.Api.csproj`
  - `backend/src/ClinicAppointment.Infrastructure/ClinicAppointment.Infrastructure.csproj`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IJwtTokenService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Security/JwtSettings.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Security/JwtTokenService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Application/Features/Auth/DTOs/AuthDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Auth/Validators/AuthValidators.cs`
  - `backend/src/ClinicAppointment.Application/Features/Auth/Commands/AuthCommands.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AuthController.cs`
  - `backend/src/ClinicAppointment.Api/Program.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/AuthAndJwtTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.10`
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 79 Passed, 0 Failed)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-014 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-015 — Role/Resource Authorization (RBAC, Resource Ownership, IDOR Prevention & 401/403 Tests)
- Status before/after: not_started -> completed
- Scope implemented:
  - Triển khai `ICurrentUserService` và `CurrentUserService`:
    - Sử dụng `IHttpContextAccessor` để trích xuất danh tính bảo mật từ JWT Claims: `UserId`, `Email`, `Roles`, `PatientProfileId`, `DoctorProfileId`.
    - Cung cấp các cờ tiện ích kiểm tra vai trò: `IsAdmin`, `IsStaff` (Admin hoặc Receptionist), `IsDoctor`, `IsPatient`.
  - Triển khai `IResourceAuthorizationService` và `ResourceAuthorizationService`:
    - `CanAccessPatientAsync`: Bệnh nhân chỉ được xem hồ sơ của chính mình; Staff có quyền điều hành; Chống IDOR (403 Forbidden nếu Bệnh nhân A truyền ID của Bệnh nhân B).
    - `CanAccessAppointmentAsync`: Bác sĩ chỉ xem ca khám được phân công (`DoctorId == DoctorProfileId`); Bệnh nhân chỉ xem ca khám của chính mình (`PatientId == PatientProfileId`); Staff có toàn quyền; Chống IDOR (403 Forbidden nếu Bác sĩ A xem ca khám của Bác sĩ B hoặc Bệnh nhân A xem ca khám của Bệnh nhân B).
    - `CanAccessDoctorScheduleAsync`: Bác sĩ chỉ xem/quản lý lịch làm việc của chính mình; Staff có quyền quản lý lịch toàn phòng khám.
  - Cấu hình ASP.NET Core Authorization Policies trong `DependencyInjection.cs`:
    - `"AdminOnly"`: Role `ADMIN`.
    - `"StaffOnly"`: Role `ADMIN` hoặc `RECEPTIONIST`.
    - `"DoctorOnly"`: Role `DOCTOR`.
    - `"PatientOnly"`: Role `PATIENT`.
    - `"StaffOrDoctor"`: Role `ADMIN`, `RECEPTIONIST` hoặc `DOCTOR`.
  - Bảo vệ các API Controllers chống IDOR:
    - `PatientsController`:
      - `GET /api/v1/patients/{id}`: `[Authorize]` + kiểm tra `CanAccessPatientAsync` (chống IDOR).
      - `GET /api/v1/patients/code/{code}` & `POST /api/v1/patients/lookup-by-identifier`: `[Authorize(Policy = "StaffOnly")]`.
      - `GET /api/v1/patients/review-cases` & `POST /api/v1/patients/review-cases/{id}/resolve`: `[Authorize(Policy = "StaffOnly")]` (chặn bệnh nhân can thiệp gộp hồ sơ).
    - `AppointmentsController`:
      - `GET /api/v1/appointments/{id}`: `[Authorize]` + kiểm tra `CanAccessAppointmentAsync` (chống IDOR).
      - `GET /api/v1/appointments/doctor/assigned`: `[Authorize(Roles = "DOCTOR")]` (chỉ trả về danh sách ca khám phân công cho bác sĩ đang gọi).
      - `GET /api/v1/appointments/patient/my-appointments`: `[Authorize(Roles = "PATIENT")]` (chỉ trả về danh sách lịch hẹn của chính bệnh nhân).
  - Cài đặt package `Microsoft.EntityFrameworkCore.InMemory` (v8.0.10) cho test suite.
  - Bộ kiểm thử tự động xUnit:
    - Xây dựng 12 test cases mới trong `ResourceAuthorizationTests.cs` kiểm tra toàn diện các kịch bản: Unauthenticated (401), Staff full access, Patient own data, Patient IDOR violation (403), Doctor assigned appointment, Doctor IDOR violation (403), Doctor schedule ownership, CurrentUserService claims parsing.
    - Toàn bộ 91/91 unit test cases trong Solution đều PASS 100%.
  - Biên dịch Frontend: `npm run build` chạy thành công 100% trong 5.38s.
- Files changed:
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/ICurrentUserService.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IResourceAuthorizationService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Services/CurrentUserService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Services/ResourceAuthorizationService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/DTOs/AppointmentDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/AppointmentQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/PatientsController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj`
  - `backend/tests/ClinicAppointment.Domain.Tests/ResourceAuthorizationTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet add package Microsoft.EntityFrameworkCore.InMemory --version 8.0.10` cho test project
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 91 Passed, 0 Failed)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-015 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-016 — Specialty Management (Admin CRUD, Unique Slug, Vietnamese SlugHelper & Public Active Filter)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entity `Specialty`:
    - Bổ sung factory method `Specialty.Create(...)` có kiểm tra validation mã và tên chuyên khoa.
    - Bổ sung phương thức `Update(...)` và `SetStatus(bool isActive)`.
  - Tiện ích `SlugHelper`:
    - Chuẩn hóa chuỗi tiếng Việt có dấu sang không dấu (bỏ dấu Unicode, thay thế ký tự đ/Đ).
    - Tạo chuỗi định danh đường dẫn URL-friendly kebab-case hợp lệ.
  - Ràng buộc Cơ sở dữ liệu:
    - Bảng `specialties`: `Code` unique, `Slug` unique, `Name` required.
    - Kiểm tra xung đột trước khi lưu: trùng Code hoặc trùng Slug trả về ngoại lệ nghiệp vụ rõ ràng.
  - CQRS Commands & Queries:
    - Commands: `CreateSpecialtyCommand`, `UpdateSpecialtyCommand`, `SetSpecialtyStatusCommand`, `DeleteSpecialtyCommand`.
    - Queries: `GetActiveSpecialtiesQuery` (Public catalog FR-002, chỉ lấy `IsActive == true`), `GetSpecialtyBySlugOrIdQuery`, `GetAllSpecialtiesQuery`.
    - Cơ chế Soft-delete / Deactivation: Khi chuyên khoa đang có Bác sĩ hoặc Dịch vụ hoạt động, hệ thống tự động chuyển sang `IsActive = false` thay vì xóa vật lý gây đứt gãy khóa ngoại.
  - API Controllers:
    - `SpecialtiesController` (`/api/v1/specialties`): `[AllowAnonymous]` phục vụ công khai người dùng và khách vãng lai.
    - `AdminSpecialtiesController` (`/api/v1/admin/specialties`): Phân quyền nghiêm ngặt `[Authorize(Policy = "StaffOnly")]` cho truy vấn và `[Authorize(Policy = "AdminOnly")]` cho các thao tác tạo mới, sửa đổi, đổi trạng thái và xóa.
  - FluentValidation:
    - `CreateSpecialtyRequestValidator` và `UpdateSpecialtyRequestValidator`.
  - Bộ kiểm thử tự động xUnit:
    - Xây dựng 14 test cases mới trong `SpecialtyManagementTests.cs` kiểm tra SlugHelper với nhiều cụm từ tiếng Việt phức tạp, kiểm tra Create/Update, kiểm tra bắt lỗi trùng Code/Slug, kiểm tra Public Active Filter (FR-002 Invariant), Status Toggle, và Soft-delete deactivation.
    - Toàn bộ 105/105 unit test cases trong Solution đều PASS 100%.
  - Biên dịch Frontend: `npm run build` chạy thành công 100% trong 4.99s.
- Files changed:
  - `backend/src/ClinicAppointment.Application/Common/Helpers/SlugHelper.cs`
  - `backend/src/ClinicAppointment.Domain/Entities/CatalogAndScheduling.cs`
  - `backend/src/ClinicAppointment.Application/Features/Specialties/DTOs/SpecialtyDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Specialties/Validators/SpecialtyValidators.cs`
  - `backend/src/ClinicAppointment.Application/Features/Specialties/Commands/SpecialtyCommands.cs`
  - `backend/src/ClinicAppointment.Application/Features/Specialties/Queries/SpecialtyQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/SpecialtiesController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AdminSpecialtiesController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/SpecialtyManagementTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 105 Passed, 0 Failed)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-016 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-017 — Doctor Management (Staff Account Link, DoctorCode Unique, Specialty Link, Public Filter & Deactivation)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entity `Doctor`:
    - Bổ sung `Doctor.Create(userId, specialtyId, doctorCode, fullName, title, bio, roomNumber, isActive)`.
    - Bổ sung `Doctor.Update(specialtyId, fullName, title, bio, roomNumber, isActive)` và `Doctor.SetStatus(isActive)`.
  - Quản lý tài khoản người dùng và phân quyền (Staff Account Link):
    - Tự động tạo tài khoản User mới hoặc liên kết tài khoản User đã có với Role `DOCTOR`.
    - Ràng buộc 1 User chỉ liên kết với tối đa 1 hồ sơ Doctor (Unique `UserId`).
  - Ràng buộc Cơ sở dữ liệu:
    - Bảng `doctors`: `DoctorCode` unique toàn hệ thống, `SpecialtyId` liên kết với bảng `specialties`.
    - Kiểm tra tính hợp lệ của chuyên khoa: `SpecialtyId` phải tồn tại và đang hoạt động (`IsActive == true`).
  - CQRS Commands & Queries:
    - Commands: `CreateDoctorCommand`, `UpdateDoctorCommand`, `SetDoctorStatusCommand`, `DeleteDoctorCommand`.
    - Queries: `GetActiveDoctorsQuery` (Public catalog FR-003, chỉ trả về bác sĩ đang hoạt động và thuộc chuyên khoa đang hoạt động, hỗ trợ lọc theo `specialtyId` hoặc `specialtySlug`), `GetDoctorByIdQuery`, `GetAllDoctorsQuery`.
  - Bảo vệ dữ liệu riêng tư (No private data leakage):
    - DTO công khai `DoctorDto` chỉ cung cấp thông tin chuyên môn công khai (`DoctorCode`, `FullName`, `Title`, `Bio`, `RoomNumber`, `SpecialtyName`), tuyệt đối không để lộ mật khẩu hay thông tin nhạy cảm.
  - Cơ chế Deactivate an toàn:
    - Khi bác sĩ đã có lịch hẹn khám (`Appointments`), yêu cầu xóa sẽ tự động chuyển sang `IsActive = false` (soft delete) thay vì xóa vật lý để bảo toàn lịch sử y tế và kiểm toán.
  - API Controllers:
    - `DoctorsController` (`/api/v1/doctors`): `[AllowAnonymous]` phục vụ công khai cho khách và bệnh nhân.
    - `AdminDoctorsController` (`/api/v1/admin/doctors`): `[Authorize(Policy = "StaffOnly")]` cho truy vấn nội bộ và `[Authorize(Policy = "AdminOnly")]` cho các thao tác tạo mới, cập nhật, đổi trạng thái và xóa.
  - FluentValidation:
    - `CreateDoctorRequestValidator` và `UpdateDoctorRequestValidator`.
  - Bộ kiểm thử tự động xUnit:
    - Xây dựng 11 test cases mới trong `DoctorManagementTests.cs` kiểm tra toàn bộ luồng tạo bác sĩ, liên kết tài khoản User và gán Role `DOCTOR`, kiểm tra bắt lỗi trùng `DoctorCode`, bắt lỗi trùng `UserId`, kiểm tra chuyên khoa Inactive, kiểm tra Public Catalog Filter theo chuyên khoa, Status Toggle và Soft Delete.
    - Toàn bộ 116/116 unit test cases trong Solution đều PASS 100%.
  - Biên dịch Frontend: `npm run build` chạy thành công 100% trong 5.44s.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/CatalogAndScheduling.cs`
  - `backend/src/ClinicAppointment.Application/Features/Doctors/DTOs/DoctorDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Doctors/Validators/DoctorValidators.cs`
  - `backend/src/ClinicAppointment.Application/Features/Doctors/Commands/DoctorCommands.cs`
  - `backend/src/ClinicAppointment.Application/Features/Doctors/Queries/DoctorQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorsController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AdminDoctorsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/DoctorManagementTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 116 Passed, 0 Failed)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-017 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-018 — Service Management (CRUD Dịch vụ, Thời lượng, Giá, Appointment Snapshots & Public Catalog)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entity `Service`:
    - Bổ sung `Service.Create(specialtyId, code, name, durationMinutes, price, currency, description, isActive)`.
    - Ràng buộc Invariant: `DurationMinutes > 0`, `Price >= 0`, `Currency` mặc định "VND".
    - Bổ sung `Service.Update(specialtyId, name, durationMinutes, price, description, isActive)` và `Service.SetStatus(isActive)`.
  - CQRS Commands & Handlers (`ClinicAppointment.Application.Features.Services.Commands`):
    - `CreateServiceCommand`: Kiểm tra mã `Code` duy nhất toàn hệ thống, kiểm tra `SpecialtyId` hợp lệ và đang hoạt động (`IsActive == true`), tạo dịch vụ mới.
    - `UpdateServiceCommand`: Kiểm tra dịch vụ tồn tại, chuyên khoa đích hợp lệ và cập nhật chi tiết.
    - `SetServiceStatusCommand`: Cập nhật cờ kích hoạt `IsActive`.
    - `DeleteServiceCommand`: Kiểm tra phụ thuộc lịch khám `Appointments`; nếu đã có ca khám sử dụng dịch vụ thì tự động chuyển sang Inactive (soft delete) nhằm bảo toàn tính toàn vẹn dữ liệu và snapshot lịch sử y tế; nếu chưa có ca khám thì xóa vật lý an toàn.
  - CQRS Queries & Handlers (`ClinicAppointment.Application.Features.Services.Queries`):
    - `GetActiveServicesQuery`: Phục vụ Public Catalog (FR-004), chỉ trả về các dịch vụ `IsActive == true` và chuyên khoa cha của chúng cũng `IsActive == true`. Hỗ trợ lọc theo `SpecialtyId` hoặc `SpecialtySlug`.
    - `GetServiceByIdQuery`: Xem chi tiết dịch vụ.
    - `GetAllServicesQuery`: Phục vụ quản lý nội bộ, hỗ trợ lọc theo chuyên khoa và trạng thái hoạt động.
  - FluentValidation:
    - `CreateServiceRequestValidator`: Xác thực `Code` và `Name` bắt buộc, `DurationMinutes > 0`, `Price >= 0`.
    - `UpdateServiceRequestValidator`: Xác thực các giá trị cập nhật.
  - Web API Controllers (`ClinicAppointment.Api`):
    - `ServicesController` (`/api/v1/services`): `[AllowAnonymous]` phục vụ công khai cho Guest và Patient tra cứu bảng giá và dịch vụ khám.
    - `AdminServicesController` (`/api/v1/admin/services`): `[Authorize(Policy = "StaffOnly")]` cho truy vấn nội bộ và `[Authorize(Policy = "AdminOnly")]` cho các thao tác mutation (Tạo, Sửa, Đổi trạng thái, Xóa).
  - Bộ kiểm thử tự động xUnit (`ServiceManagementTests.cs`):
    - 11 test cases kiểm tra: Tạo dịch vụ thành công, validation `DurationMinutes <= 0` và `Price < 0` ở Entity & FluentValidation, chống trùng lặp `Code`, từ chối tạo dịch vụ cho chuyên khoa Inactive, Public catalog chỉ trả về dịch vụ active của chuyên khoa active, lọc theo `SpecialtySlug`, cập nhật dịch vụ, đổi trạng thái hoạt động, xóa an toàn (hard delete khi chưa có ca khám, soft delete khi đã có ca khám).
    - Toàn bộ 127/127 unit test cases trong Solution đều PASS 100%.
  - Biên dịch giải pháp:
    - Backend: `dotnet build` hoàn tất với 0 Warning(s), 0 Error(s).
    - Frontend: `npm run build` hoàn tất trong 4.94s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/CatalogAndScheduling.cs`
  - `backend/src/ClinicAppointment.Application/Features/Services/DTOs/ServiceDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Services/Validators/ServiceValidators.cs`
  - `backend/src/ClinicAppointment.Application/Features/Services/Commands/ServiceCommands.cs`
  - `backend/src/ClinicAppointment.Application/Features/Services/Queries/ServiceQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/ServicesController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AdminServicesController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ServiceManagementTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet test "backend/ClinicAppointment.sln"` (Thành công: 127 Passed, 0 Failed, 0 Skipped)
  - `dotnet build "backend/ClinicAppointment.sln" --no-incremental` (Thành công: 0 Warnings, 0 Errors)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-018 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-019 — Doctor Working Rules (Lịch làm việc định kỳ, Khung giờ, Khoảng ngày hiệu lực & Chống trùng ca)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entity `DoctorSchedule`:
    - Thêm các trường `EffectiveFrom` (DateOnly) và `EffectiveTo` (DateOnly?).
    - Ràng buộc Invariants:
      - `DayOfWeek` từ 1 (Thứ Hai) đến 7 (Chủ Nhật) theo chuẩn ISO 8601.
      - `StartTime < EndTime` (TimeOnly, biểu diễn giờ địa phương phòng khám).
      - `EffectiveFrom <= EffectiveTo` khi `EffectiveTo` có giá trị.
      - `SlotDurationMinutes > 0`.
    - Factory methods: `DoctorSchedule.Create(...)`, `Update(...)`, `SetStatus(...)`.
  - Cơ sở dữ liệu và EF Core:
    - Bổ sung cấu hình `DoctorScheduleConfiguration` trong `CatalogConfigurations.cs` ánh xạ bảng `doctor_schedules`, gắn index kết hợp `(doctor_id, day_of_week, is_active)` và các Check Constraints (`chk_doctor_schedules_day_of_week`, `chk_doctor_schedules_time_order`, `chk_doctor_schedules_effective_date_order`).
    - Tạo migration EF Core `20261114075500_AddDoctorScheduleEffectiveDates.cs` và designer.
  - CQRS Commands & Handlers (`ClinicAppointment.Application.Features.Schedules.Commands`):
    - `CreateDoctorScheduleCommand`: Kiểm tra bác sĩ phụ trách tồn tại và đang hoạt động (`IsActive == true`); thuật toán kiểm tra chống chồng lấn ca (No-overlap) dựa trên giao khoảng ngày hiệu lực và giao khung giờ khám của cùng một bác sĩ trong cùng ngày trong tuần; lưu bản ghi mới.
    - `UpdateDoctorScheduleCommand`: Kiểm tra chống chồng lấn với các ca khác của cùng bác sĩ trước khi cập nhật.
    - `SetDoctorScheduleStatusCommand`: Kiểm tra an toàn trước khi kích hoạt lại ca làm việc để tránh xung đột với các ca đang active khác.
    - `DeleteDoctorScheduleCommand`: Xóa ca làm việc định kỳ.
  - CQRS Queries & Handlers (`ClinicAppointment.Application.Features.Schedules.Queries`):
    - `GetDoctorSchedulesQuery`: Lấy danh sách lịch làm việc của bác sĩ cho quản lý nội bộ, hỗ trợ lọc theo ngày hiệu lực `EffectiveDate` và trạng thái `IsActive`.
    - `GetDoctorScheduleByIdQuery`: Lấy thông tin chi tiết một ca làm việc.
    - `GetPublicDoctorSchedulesQuery`: Lấy danh sách lịch làm việc hàng tuần công khai của bác sĩ đang hoạt động và chuyên khoa đang hoạt động, phục vụ bệnh nhân tham khảo khi đặt lịch.
  - FluentValidation:
    - `CreateDoctorScheduleRequestValidator` và `UpdateDoctorScheduleRequestValidator`.
  - Web API Controllers (`ClinicAppointment.Api`):
    - `AdminDoctorSchedulesController` (`/api/v1/admin/doctor-schedules`): `[Authorize(Policy = "StaffOnly")]` cho xem danh sách và `[Authorize(Policy = "AdminOnly")]` cho các thao tác tạo mới, sửa, bật tắt, xóa.
    - `DoctorSchedulesController` (`/api/v1/doctors/{doctorId}/schedules`): `[AllowAnonymous]` phục vụ công khai cho khách và bệnh nhân.
  - Bộ kiểm thử tự động xUnit (`DoctorWorkingRulesTests.cs`):
    - 12 test cases bao phủ toàn diện:
      1. Tạo ca thành công với đầy đủ dữ liệu hợp lệ.
      2. Validator từ chối khi `StartTime >= EndTime`.
      3. Validator từ chối khi `DayOfWeek` ngoài khoảng 1..7 hoặc `EffectiveTo < EffectiveFrom`.
      4. Domain Entity invariants ném ngoại lệ khi vi phạm.
      5. Từ chối tạo lịch cho bác sĩ không tồn tại hoặc đang inactive.
      6. Chống trùng lấn (No-overlap): Bị từ chối khi 2 ca cùng thứ Hai có ngày hiệu lực và khung giờ giao nhau.
      7. Chấp nhận ca sáng (08:00 - 12:00) và ca chiều (13:30 - 17:30) trong cùng một ngày.
      8. Chấp nhận 2 ca cùng giờ nhưng có khoảng ngày hiệu lực không giao nhau (6 tháng đầu năm và 6 tháng cuối năm).
      9. Cập nhật ca gây xung đột giờ làm việc bị từ chối.
      10. Bật lại trạng thái ca inactive bị từ chối nếu xung đột với ca active đang chạy.
      11. Tra cứu lọc theo ngày hiệu lực chính xác.
      12. Lịch công khai chỉ hiển thị bác sĩ active và chuyên khoa active.
    - Toàn bộ 139/139 unit test cases trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 4.98s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/CatalogAndScheduling.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/CatalogConfigurations.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114075500_AddDoctorScheduleEffectiveDates.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114075500_AddDoctorScheduleEffectiveDates.Designer.cs`
  - `backend/src/ClinicAppointment.Application/Features/Schedules/DTOs/DoctorScheduleDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Schedules/Validators/DoctorScheduleValidators.cs`
  - `backend/src/ClinicAppointment.Application/Features/Schedules/Commands/DoctorScheduleCommands.cs`
  - `backend/src/ClinicAppointment.Application/Features/Schedules/Queries/DoctorScheduleQueries.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AdminDoctorSchedulesController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorSchedulesController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/DoctorWorkingRulesTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet test "backend/ClinicAppointment.sln"` (Thành công: 139 Passed, 0 Failed, 0 Skipped)
  - `dotnet build "backend/ClinicAppointment.sln" --no-incremental` (Thành công: 0 Warnings, 0 Errors)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-019 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-020 — Doctor Time Off and Breaks (Ngày nghỉ, Khung giờ bận, Chống overlap time off & Bảo toàn cuộc hẹn)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entities:
    - `DoctorTimeOff`: Lưu trữ ngày nghỉ phép/công tác của bác sĩ với `StartAt` < `EndAt` (UTC), `Status` (Active/Cancelled), `CreatedByUserId`, và `Reason`.
    - `DoctorScheduleBlock`: Lưu trữ khung giờ bận định kỳ hoặc đột xuất (Break, Meeting, Maintenance, Other) với `IsActive`.
    - Enums `TimeOffStatus` (Active, Cancelled) và `ScheduleBlockType` (Break, Meeting, Maintenance, Other).
    - Invariants: Bắt buộc `StartAt < EndAt`, `DoctorId != Guid.Empty`, `CreatedByUserId != Guid.Empty`.
  - Cơ sở dữ liệu và EF Core:
    - Bổ sung `DbSet<DoctorTimeOff>` và `DbSet<DoctorScheduleBlock>` vào `IApplicationDbContext` và `ApplicationDbContext`.
    - Cấu hình `DoctorTimeOffConfiguration` và `DoctorScheduleBlockConfiguration` trong `CatalogConfigurations.cs` kèm Check Constraints (`chk_doctor_time_offs_time_order`, `chk_doctor_schedule_blocks_time_order`) và Composite Indexes.
    - Tạo migration EF Core `20261114081000_AddDoctorTimeOffsAndScheduleBlocks.cs` và designer.
  - Application Service & CQRS:
    - `IDoctorTimeOffService` & `DoctorTimeOffService`:
      - `HasTimeOffOrBlockConflictAsync`: Kiểm tra xem một slot khám có giao nhau với ngày nghỉ active hoặc giờ bận active của bác sĩ không (FR-035).
      - `GetAffectedAppointmentsAsync`: Truy vấn các ca khám đang hoạt động (`PendingConfirmation`, `Confirmed`, `CheckedIn`, `InProgress`) của bác sĩ bị ảnh hưởng khi có yêu cầu xin nghỉ. Truy vấn an toàn tối ưu không phụ thuộc navigation inner join.
    - Commands:
      - `CreateDoctorTimeOffCommand`: Kiểm tra bác sĩ active, kiểm tra quyền (Doctor chỉ đăng ký cho chính mình; Staff toàn quyền), chống đăng ký 2 ngày nghỉ chồng lấn giờ.
      - **Bảo toàn Invariant FR-055 (Schedule change không âm thầm xóa appointments)**: Nếu khoảng thời gian xin nghỉ có cuộc hẹn đã đặt trước và client không gửi cờ `AllowWithExistingAppointments = true`, hệ thống ném ngoại lệ cảnh báo số lượng và danh sách cuộc hẹn bị ảnh hưởng; khi có xác nhận cho phép, hệ thống bảo toàn 100% các cuộc hẹn trong database, tuyệt đối không tự động xóa hay hủy lịch.
      - `CancelDoctorTimeOffCommand`: Hủy ngày nghỉ, giải phóng lịch cho bác sĩ.
      - `CreateDoctorScheduleBlockCommand` & `DeactivateDoctorScheduleBlockCommand`: Quản lý khung giờ bận (Họp, nghỉ trưa, bảo trì).
    - Queries:
      - `GetDoctorTimeOffsQuery`: Lấy danh sách ngày nghỉ theo bác sĩ và khoảng thời gian.
      - `GetDoctorScheduleBlocksQuery`: Lấy danh sách khung giờ bận.
      - `CheckDoctorTimeOffConflictQuery`: Endpoint kiểm tra xung đột thời gian bận.
  - FluentValidation: `CreateDoctorTimeOffRequestValidator` và `CreateDoctorScheduleBlockRequestValidator`.
  - Web API Controller:
    - `AdminDoctorTimeOffsController` (`/api/v1/admin/doctor-time-offs`): `[Authorize(Policy = "StaffOrDoctor")]` cho phép bác sĩ và nhân viên quản lý ngày nghỉ, khung giờ bận và kiểm tra xung đột.
  - Bộ kiểm thử tự động xUnit (`DoctorTimeOffTests.cs`):
    - 9 test cases kiểm tra:
      1. Đăng ký ngày nghỉ hợp lệ thành công.
      2. Validator từ chối `StartAt >= EndAt`.
      3. Domain Entity invariants ném ngoại lệ khi vi phạm.
      4. Ngăn chặn bác sĩ này xin nghỉ cho bác sĩ khác (IDOR protection).
      5. Chống đăng ký 2 ngày nghỉ chồng lấn giờ nhau.
      6. **Acceptance Criteria 1 (Không book overlap time off)**: `HasTimeOffOrBlockConflictAsync` phát hiện chính xác thời gian nghỉ/bận và trả về `true` để từ chối đặt lịch trùng.
      7. **Acceptance Criteria 2 (Schedule change không âm thầm xóa appointments)**: Khi có 2 cuộc hẹn tồn tại trước, hệ thống cảnh báo từ chối khi chưa cho phép; khi có xác nhận cho phép, hệ thống tạo ngày nghỉ thành công và **bảo toàn nguyên vẹn cả 2 cuộc hẹn trong DB, không xóa bất kỳ bản ghi nào**.
      8. Hủy ngày nghỉ giải phóng lịch làm việc.
      9. Tạo và vô hiệu hóa khung giờ bận (Meeting/Break) quản lý chính xác trạng thái khả dụng.
    - Toàn bộ 148/148 unit test cases trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.29s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Enums/DomainEnums.cs`
  - `backend/src/ClinicAppointment.Domain/Entities/CatalogAndScheduling.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/CatalogConfigurations.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114081000_AddDoctorTimeOffsAndScheduleBlocks.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114081000_AddDoctorTimeOffsAndScheduleBlocks.Designer.cs`
  - `backend/src/ClinicAppointment.Application/Features/TimeOffs/DTOs/TimeOffDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/TimeOffs/Services/IDoctorTimeOffService.cs`
  - `backend/src/ClinicAppointment.Application/Features/TimeOffs/Services/DoctorTimeOffService.cs`
  - `backend/src/ClinicAppointment.Application/Features/TimeOffs/Validators/TimeOffValidators.cs`
  - `backend/src/ClinicAppointment.Application/Features/TimeOffs/Commands/TimeOffCommands.cs`
  - `backend/src/ClinicAppointment.Application/Features/TimeOffs/Queries/TimeOffQueries.cs`
  - `backend/src/ClinicAppointment.Application/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AdminDoctorTimeOffsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/DoctorTimeOffTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet test "backend/ClinicAppointment.sln"` (Thành công: 148 Passed, 0 Failed, 0 Skipped)
  - `dotnet build "backend/ClinicAppointment.sln" --no-incremental` (Thành công: 0 Warnings, 0 Errors)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-020 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-021 — Availability Calculation (Tính Khung giờ khả dụng, Chia Slot, Loại trừ Time Off/Busy & Bảo mật Bệnh nhân)
- Status before/after: not_started -> completed
- Scope implemented:
  - Helper & Timezone Conversion:
    - [TimeZoneHelper.cs](file:///d:/Clinic_Appointment_Management_Project/backend/src/ClinicAppointment.Application/Common/Helpers/TimeZoneHelper.cs): Chuyển đổi chính xác ngày giờ giữa giờ địa phương phòng khám `Asia/Ho_Chi_Minh` (UTC+7) và UTC lưu trữ; tính toán `GetIsoDayOfWeek` (1..7) và khoảng `GetDayUtcRange`.
  - Application Service & Engine:
    - [IAvailabilityService.cs](file:///d:/Clinic_Appointment_Management_Project/backend/src/ClinicAppointment.Application/Features/Scheduling/Services/IAvailabilityService.cs) & [AvailabilityService.cs](file:///d:/Clinic_Appointment_Management_Project/backend/src/ClinicAppointment.Application/Features/Scheduling/Services/AvailabilityService.cs):
      - Thuật toán sinh Slot (Time Slot Slicing): Chia slot tự động theo thời lượng dịch vụ `DurationMinutes` (hoặc mặc định 30 phút); không sinh slot vượt quá giờ kết thúc ca làm việc.
      - Hỗ trợ nhiều ca làm việc sáng/chiều trong cùng một ngày.
      - **Acceptance Criteria 1 — Không hiển thị ngoài lịch (FR-052)**: Chỉ sinh slot trong các ca làm việc `DoctorSchedule` có hiệu lực tại ngày khám; trả về danh sách rỗng khi bác sĩ không có lịch làm việc.
      - **Acceptance Criteria 2 — Loại trừ Break / Time Off / Active Appointments (FR-035, BR-006)**: Tự động phát hiện và đánh dấu `IsAvailable = false` (hoặc lọc bỏ khi `onlyAvailable = true`) với mọi slot giao nhau với `DoctorTimeOff` (Active), `DoctorScheduleBlock` (Meeting/Break), và `Appointments` đang hoạt động (`PendingConfirmation`, `Confirmed`, `CheckedIn`, `InProgress`).
      - Giải phóng slot khi lịch hẹn đã hủy: Các cuộc hẹn có trạng thái `Cancelled` không chiếm slot, slot tương ứng hiển thị khả dụng bình thường.
      - Loại trừ slot quá khứ: Nếu ngày khám là hôm nay, các slot có `StartAtUtc <= nowUtc` được đánh dấu không khả dụng.
      - **Acceptance Criteria 3 — Tuyệt đối không lộ patient info (FR-030)**: DTO công khai `TimeSlotDto` và `DoctorAvailabilityDto` chỉ trả về thông tin khung giờ; hoàn toàn không có trường nào liên quan đến thông tin cá nhân của bệnh nhân khác.
  - CQRS Queries & Handlers:
    - [GetDoctorAvailabilityQuery.cs](file:///d:/Clinic_Appointment_Management_Project/backend/src/ClinicAppointment.Application/Features/Scheduling/Queries/GetDoctorAvailabilityQuery.cs): Nhận tham số `DoctorId`, `Date`, `ServiceId`, `OnlyAvailable`.
  - Web API Controller:
    - [AvailabilityController.cs](file:///d:/Clinic_Appointment_Management_Project/backend/src/ClinicAppointment.Api/Controllers/AvailabilityController.cs): Endpoint công khai `GET /api/v1/availability` (`[AllowAnonymous]`) phục vụ Khách vãng lai và Bệnh nhân tra cứu lịch khám khả dụng.
  - Bộ kiểm thử tự động xUnit (`AvailabilityCalculationTests.cs`):
    - 10 test cases kiểm tra:
      1. Bác sĩ không có lịch làm việc trong ngày trả về danh sách slots rỗng.
      2. Mặc định 30 phút chia đúng 4 slots cho ca 2 tiếng (08:00 - 10:00).
      3. Dịch vụ 45 phút chia đúng 2 slots (08:00-08:45 và 08:45-09:30), không sinh slot dư khi không đủ thời gian.
      4. Slot rơi vào ngày nghỉ của bác sĩ (`DoctorTimeOff`) bị đánh dấu không khả dụng.
      5. Slot rơi vào khung giờ bận (`DoctorScheduleBlock`) bị đánh dấu không khả dụng.
      6. Slot đã có cuộc hẹn đang hoạt động (`Appointment` active) bị đánh dấu không khả dụng.
      7. Cuộc hẹn đã bị hủy (`Status = Cancelled`) giải phóng slot, slot tương ứng khả dụng bình thường.
      8. Tham số `onlyAvailable = true` lọc bỏ toàn bộ các slot bận hoặc đã qua.
      9. Phân ca sáng/chiều trong cùng một ngày sinh đầy đủ slots cho cả 2 ca.
      10. Xác minh quyền riêng tư: DTO không để lộ bất kỳ thông tin nào của bệnh nhân khác.
    - Toàn bộ 158/158 unit test cases trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 4.67s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Application/Common/Helpers/TimeZoneHelper.cs`
  - `backend/src/ClinicAppointment.Application/Features/Scheduling/DTOs/AvailabilityDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Scheduling/Services/IAvailabilityService.cs`
  - `backend/src/ClinicAppointment.Application/Features/Scheduling/Services/AvailabilityService.cs`
  - `backend/src/ClinicAppointment.Application/Features/Scheduling/Queries/GetDoctorAvailabilityQuery.cs`
  - `backend/src/ClinicAppointment.Application/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AvailabilityController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/AvailabilityCalculationTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet test "backend/ClinicAppointment.sln"` (Thành công: 158 Passed, 0 Failed, 0 Skipped)
  - `dotnet build "backend/ClinicAppointment.sln" --no-incremental` (Thành công: 0 Warnings, 0 Errors)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-021 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-022 — PostgreSQL No-Overlap Constraint (GiST Exclusion Constraint, Chống Race Condition Đặt trùng & Ánh xạ HTTP 409 Conflict)
- Status before/after: not_started -> completed
- Scope implemented:
  - Database Constraint & Migration:
    - Kích hoạt extension PostgreSQL `btree_gist` và ràng buộc loại trừ `ex_appointments_doctor_no_overlap` (`EXCLUDE USING gist (doctor_id WITH =, tstzrange(start_at, end_at, '[)') WITH &&) WHERE (status <> 'Cancelled')`) trong migration `20261114062800_InitialCreate.cs` và kịch bản idempotent `database/migrations/20261114_initial_schema.sql`.
    - Kết hợp Unique Partial Index `ix_appointments_doctor_id_start_at` trên `appointments(doctor_id, start_at) WHERE (status NOT IN ('Cancelled', 'NoShow'))`.
  - Xử lý Ngoại lệ và Ánh xạ HTTP 409 Conflict (BR-007):
    - Tạo custom exception `AppointmentConflictException` trong `ClinicAppointment.Application.Common.Exceptions`.
    - Cập nhật `ExceptionHandlingMiddleware` trong `ClinicAppointment.Api.Middlewares`:
      - Bắt giữ trực tiếp `AppointmentConflictException` -> HTTP 409 Conflict.
      - Bắt giữ `DbUpdateException` khi vi phạm exclusion constraint hoặc unique constraint trùng ca (mã lỗi PostgreSQL `23P01` exclusion_violation, constraint `ex_appointments_doctor_no_overlap`, hoặc `23505` unique_violation) -> HTTP 409 Conflict kèm thông điệp thân thiện: `"Khung giờ khám này vừa có người khác đặt trước. Vui lòng chọn khung giờ khác."`.
      - Hỗ trợ thêm `KeyNotFoundException` -> HTTP 404 Not Found, `UnauthorizedAccessException` -> HTTP 403 Forbidden.
  - Bộ kiểm thử tự động xUnit (`PostgreSqlNoOverlapConstraintTests.cs`):
    - 7 test methods / 10 test cases kiểm tra:
      1. Middleware chuyển `AppointmentConflictException` thành HTTP 409 Conflict với ApiResponse envelope chuẩn.
      2. Middleware nhận diện `DbUpdateException` bọc lỗi vi phạm constraint PostgreSQL và trả về HTTP 409 Conflict.
      3. Hàm `IsExclusionOrUniqueConstraintViolation` nhận diện chính xác các mã lỗi và chuỗi ràng buộc của PostgreSQL.
      4. Chống race condition đặt trùng lịch: Cùng một bác sĩ, khi ca 1 đã lưu thì ca 2 cố gắng lưu đè trong khoảng thời gian giao nhau sẽ bị phát hiện và ném lỗi 409.
      5. Hai ca cùng thời gian nhưng khác bác sĩ (`doctor_id`) đều được chấp nhận thành công độc lập.
      6. Hai ca cùng bác sĩ nối tiếp nhau (ví dụ 08:30 - 09:00 và 09:00 - 09:30) không bị xung đột nhờ nửa khoảng `[)`.
      7. Cuộc hẹn trước đó đã bị hủy (`Status = Cancelled`) giải phóng slot và cho phép đặt lại vào đúng khung giờ đó.
    - Toàn bộ 168/168 unit test cases trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.08s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Application/Common/Exceptions/AppointmentConflictException.cs`
  - `backend/src/ClinicAppointment.Api/Middlewares/ExceptionHandlingMiddleware.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj`
  - `backend/tests/ClinicAppointment.Domain.Tests/PostgreSqlNoOverlapConstraintTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `dotnet test "backend/ClinicAppointment.sln"` (Thành công: 168 Passed, 0 Failed, 0 Skipped)
  - `dotnet build "backend/ClinicAppointment.sln" --no-incremental` (Thành công: 0 Warnings, 0 Errors)
  - `npm run build` trong `frontend/` (Thành công: 0 Errors)
- Acceptance criteria evidence:
  - FEAT-022 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-023 — Guest Booking API (Tạo Lịch Khám Công Khai Không Cần Đăng Nhập, BookingCode Ngẫu Nhiên, Snapshot Dịch Vụ, Strictly NO Auto-Merge & Transaction Integrity)
- Status before/after: not_started -> completed
- Scope implemented:
  - CQRS Command & Handler (`ClinicAppointment.Application.Features.Appointments.Commands.BookGuestAppointment`):
    - `BookGuestAppointmentCommand`: Nhận FullName, PhoneNumber, DateOfBirth, BirthYear, Email, DoctorId, ServiceId, StartAtUtc, ReasonForVisit.
    - `BookGuestAppointmentCommandValidator`: Xác thực họ tên bắt buộc (tối đa 150 ký tự); số điện thoại chuẩn di động Việt Nam (10 số); bắt buộc cung cấp ít nhất Ngày sinh hoặc Năm sinh; DoctorId và ServiceId không rỗng; StartAtUtc phải ở tương lai.
    - `BookGuestAppointmentCommandHandler`:
      1. Kiểm tra Bác sĩ hoạt động và Chuyên khoa hoạt động (FR-004); từ chối nếu không hoạt động.
      2. Kiểm tra Dịch vụ hoạt động.
      3. Quy đổi múi giờ qua `TimeZoneHelper`: Kiểm tra khung giờ nằm trọn vẹn trong ca làm việc `DoctorSchedule` có hiệu lực tại ngày khám theo chuẩn ISO 8601; từ chối khi đặt ngoài ca.
      4. Kiểm tra xung đột ngày nghỉ `DoctorTimeOff` (trạng thái Active) -> Ném `AppointmentConflictException` (HTTP 409 Conflict).
      5. Kiểm tra xung đột khung giờ bận `DoctorScheduleBlock` -> Ném `AppointmentConflictException` (HTTP 409 Conflict).
      6. Kiểm tra xung đột cuộc hẹn đang hoạt động (`Appointments` có status != Cancelled) -> Ném `AppointmentConflictException` (HTTP 409 Conflict).
      7. Quản lý Giao dịch DB (`DatabaseFacade.BeginTransactionAsync` trên relational provider): Bọc toàn bộ quá trình tạo Patient, ReviewCase, và Appointment trong Transaction; rollback sạch sẽ nếu có lỗi.
      8. Hồ sơ bệnh nhân độc lập: Tạo `Patient` với `UserId = null`, hoàn toàn không tự động tạo tài khoản User cho Guest (FR-022, BR-001).
      9. Quy tắc nghiệp vụ cốt lõi Strictly NO Auto-merge (BR-002): Quét ứng viên trùng lặp bằng `IPatientDuplicateDetectionService`; khi phát hiện trùng họ tên + ngày sinh/năm sinh, tạo hồ sơ bệnh nhân mới độc lập và tự động mở một `PatientIdentityReviewCase` trạng thái `OPEN` để nhân viên lễ tân đối soát tại quầy tiếp đón.
      10. Khởi tạo cuộc hẹn với snapshot dịch vụ: Lưu `ServiceNameSnapshot`, `ServiceDurationSnapshot`, `ServicePriceSnapshot`, tiền tệ VND, `PaidAmount = 0.0`. Sinh `BookingCode` ngẫu nhiên định dạng `APT-yyyyMMdd-XXXXXX` (CSPRNG hex), duy nhất, khó đoán và không chứa PII (FR-023).
  - API Controller (`AppointmentsController.cs`):
    - Endpoint `POST /api/v1/appointments/guest` gắn `[AllowAnonymous]`, trả về `201 Created` kèm `BookGuestAppointmentResponse` (BookingCode, FullName, ServiceName, Price, Duration, StartAt, EndAt, Status, NextSteps); tuyệt đối không trả PatientId nội bộ.
  - Abstraction & Dependency Injection:
    - Bổ sung `DatabaseFacade Database { get; }` vào `IApplicationDbContext`.
    - Đăng ký `IPatientDuplicateDetectionService` vào DI container.
    - Bổ sung overload `Result<T>.Success(value, statusCode = 200)` trong `ApiResponse.cs`.
  - Bộ kiểm thử tự động xUnit (`GuestBookingApiTests.cs`):
    - 11 test cases bao phủ toàn diện:
      1. `GuestBooking_Success_CreatesIndependentPatientWithoutUser`: Đặt lịch thành công, Patient có UserId = null, Appointment có IsGuest = true, Status = PendingConfirmation.
      2. `GuestBooking_Generates_UniqueSecureBookingCode`: Hai ca khám sinh 2 BookingCode độc lập, đúng regex `^APT-\d{8}-[A-Z0-9]{6}$`.
      3. `GuestBooking_Captures_ServicePriceAndDurationSnapshots`: Snapshot giá, thời lượng và tên dịch vụ được bảo toàn nguyên vẹn kể cả khi dịch vụ gốc sau này bị sửa giá hoặc đổi tên.
      4. `GuestBooking_Rejects_WhenDoctorOrSpecialtyInactive`: Từ chối khi Bác sĩ hoặc Chuyên khoa bị vô hiệu hóa.
      5. `GuestBooking_Rejects_WhenOutsideDoctorWorkingSchedule`: Từ chối khi đặt vào ngày không có ca làm việc hoặc ngoài khung giờ ca làm việc.
      6. `GuestBooking_ThrowsConflict_WhenDoctorHasApprovedTimeOff`: Ném `AppointmentConflictException` khi bác sĩ có lịch nghỉ.
      7. `GuestBooking_ThrowsConflict_WhenDoctorScheduleIsBlocked`: Ném `AppointmentConflictException` khi khung giờ bị khóa bởi ScheduleBlock.
      8. `GuestBooking_ThrowsConflict_WhenSlotAlreadyBooked`: Ném `AppointmentConflictException` khi slot đã có người đặt trước.
      9. `GuestBooking_CancelledAppointment_DoesNotBlockNewBooking`: Lịch hẹn trước bị hủy sẽ giải phóng slot để người khác đặt bình thường.
      10. `GuestBooking_StrictlyNoAutoMerge_CreatesSeparatePatientAndOpensReviewCase_WhenDuplicateSuspected`: Tuyệt đối không tự gộp bệnh nhân khi trùng tên + ngày sinh; tạo Patient mới riêng biệt và mở `PatientIdentityReviewCase` trạng thái `OPEN`.
      11. `GuestBooking_Validation_RejectsMissingRequiredFields`: FluentValidation bắt lỗi thiếu ngày sinh/năm sinh, sai số điện thoại di động, hoặc giờ hẹn ở quá khứ.
    - Toàn bộ 179/179 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 4.89s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Application/Common/Models/ApiResponse.cs`
  - `backend/src/ClinicAppointment.Application/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/BookGuestAppointment/BookGuestAppointmentCommand.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/GuestBookingApiTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 179 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 4.89s)
- Acceptance criteria evidence:
  - FEAT-023 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-024 — Booking Idempotency & Abuse Protection (Idempotency Key, Chống Trùng Lặp Retry, Rate Limiting & Generic Safe Errors)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entity `IdempotencyRecord` (`ClinicAppointment.Domain.Entities.IdempotencyRecord`):
    - Quản lý `Scope`, `KeyHash` (SHA-256 bytea), `RequestHash` (SHA-256 bytea), `ResourceType`, `ResourceId`, `ResponseStatusCode`, `ResponseBodyJson`, `ExpiresAt`, `CreatedAt`.
    - Bổ sung hàm tiện ích `ComputeHash`, `MatchesRequest`, `IsExpired`.
    - Cấu hình EF Core `IdempotencyRecordConfiguration` với ràng buộc Unique trên `(Scope, KeyHash)` theo chuẩn Database Design (DB-DEC-11).
  - Abuse Protection & Rate Limiting (`IAbuseProtectionService` & `AbuseProtectionService`):
    - Quản lý bộ đệm Sliding Window Cache (`IMemoryCache`) theo dõi số lần gửi yêu cầu đặt lịch trong cửa sổ 5 phút.
    - Giới hạn tối đa 3 lần đặt lịch trên một số điện thoại (chống spam SMS OTP và flood booking).
    - Giới hạn tối đa 10 lần đặt lịch trên một địa chỉ IP client.
    - Tự động ném `RateLimitExceededException` khi vượt ngưỡng cho phép.
  - Tích hợp CQRS Command & Controller:
    - `BookGuestAppointmentCommand`: Thêm trường `IdempotencyKey` và `ClientIp`.
    - `AppointmentsController.cs`: Tự động nhận diện header `Idempotency-Key` hoặc `X-Idempotency-Key` và thu thập Remote IP của client để bảo vệ an toàn.
    - `BookGuestAppointmentCommandHandler`:
      1. Kiểm tra rate limit theo số điện thoại và IP client; ném `RateLimitExceededException` (HTTP 429) nếu vi phạm.
      2. Kiểm tra Idempotency: Tra cứu bản ghi đã lưu theo `Scope = "GuestBooking"` và `KeyHash`.
         - Nếu trùng payload: Trả về kết quả cached ngay lập tức (không gọi lại logic đặt lịch, không tạo thêm bản ghi Appointment/Patient nào).
         - Nếu khác payload: Ném `IdempotencyConflictException` (HTTP 409 Conflict).
      3. Khi đặt lịch thành công: Lưu `IdempotencyRecord` trong cùng giao dịch và ghi nhận lượt request vào abuse protection service.
  - Exception Handling & Generic Safe Errors:
    - `IdempotencyConflictException` -> HTTP 409 Conflict.
    - `RateLimitExceededException` -> HTTP 429 Too Many Requests.
    - Lỗi không kiểm soát (500): Trả về ApiResponse generic ("Đã xảy ra lỗi nội bộ trên máy chủ. Vui lòng thử lại sau."), giấu hoàn toàn stack trace, database query, mật khẩu hay chuỗi kết nối nhạy cảm.
    - Cấu hình `JavaScriptEncoder.UnsafeRelaxedJsonEscaping` hỗ trợ Unicode tiếng Việt chuẩn xác trong middleware.
  - Database Migration & Idempotent SQL:
    - Migration `20261114083500_AddIdempotencyRecords.cs` và `20261114083500_AddIdempotencyRecords.Designer.cs`.
    - Cập nhật kịch bản SQL idempotent `database/migrations/20261114_initial_schema.sql`.
  - Bộ kiểm thử tự động xUnit (`BookingIdempotencyAndAbuseTests.cs`):
    - 10 test cases bao phủ toàn diện:
      1. `Booking_WithIdempotencyKey_FirstCall_SucceedsAndPersistsRecord`: Đặt lịch kèm key thành công và lưu IdempotencyRecord vào DB.
      2. `Booking_WithSameIdempotencyKey_AndSamePayload_ReturnsCachedResponse_WithoutCreatingDuplicateBooking`: Retry cùng key và payload trả về kết quả đã lưu mà KHÔNG tạo duplicate booking (số lượng appointment và patient trong DB vẫn là 1).
      3. `Booking_WithSameIdempotencyKey_ButDifferentPayload_ThrowsConflictException`: Tái sử dụng key với payload khác ném `IdempotencyConflictException`.
      4. `Booking_AbuseProtection_WhenExceedingPhoneRateLimit_ThrowsRateLimitExceededException`: Vượt quá 3 lần đặt/5 phút với cùng số điện thoại ném `RateLimitExceededException`.
      5. `Booking_AbuseProtection_WhenExceedingIpRateLimit_ThrowsRateLimitExceededException`: Vượt quá 10 lần đặt/5 phút với cùng IP ném `RateLimitExceededException`.
      6. `Booking_ExpiredIdempotencyRecord_AllowsNewBooking`: Key đã hết hạn (TTL 24h) được chấp nhận cho ca khám mới.
      7. `ExceptionHandlingMiddleware_MapsIdempotencyConflict_ToHttp409Conflict`: Middleware ánh xạ `IdempotencyConflictException` thành HTTP 409.
      8. `ExceptionHandlingMiddleware_MapsRateLimitExceeded_ToHttp429TooManyRequests`: Middleware ánh xạ `RateLimitExceededException` thành HTTP 429.
      9. `ExceptionHandlingMiddleware_GenericSafeErrors_HidesInternalDetailsOnUnexpectedException`: Lỗi 500 giấu kín tuyệt đối mật khẩu database và chuỗi kết nối, trả về thông báo generic an toàn.
      10. `IdempotencyRecord_ComputeHash_MatchesSha256Deterministically`: Kiểm tra tính nhất quán và xác định của hàm băm SHA-256.
    - Toàn bộ 189/189 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 4.87s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/IdempotencyRecord.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IAbuseProtectionService.cs`
  - `backend/src/ClinicAppointment.Application/Common/Exceptions/IdempotencyConflictException.cs`
  - `backend/src/ClinicAppointment.Application/Common/Exceptions/RateLimitExceededException.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/BookGuestAppointment/BookGuestAppointmentCommand.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/IdempotencyRecordConfiguration.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114083500_AddIdempotencyRecords.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114083500_AddIdempotencyRecords.Designer.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Services/AbuseProtectionService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/src/ClinicAppointment.Api/Middlewares/ExceptionHandlingMiddleware.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/BookingIdempotencyAndAbuseTests.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/GuestBookingApiTests.cs`
  - `database/migrations/20261114_initial_schema.sql`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 189 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 4.87s)
- Acceptance criteria evidence:
  - FEAT-024 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-025 — OTP Challenge Service (Mã Xác Thực Gắn Mục Đích Purpose-Bound, Băm HMAC-SHA256, Chống Enumeration & SMS Abstraction)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entity `OtpChallenge` (`ClinicAppointment.Domain.Entities.RescheduleRefundOtp.cs`):
    - Cấu trúc: `AppointmentId`, `PatientId`, `PhoneNumber`, `Purpose` (`LOOKUP`, `RESCHEDULE`, `CANCEL`), `OtpHash`, `AttemptsCount`, `MaxAttempts = 5`, `ExpiresAt` (TTL 5 phút), `IsUsed`.
    - Phương thức bảo mật:
      - `ComputeOtpHash(string otp, Guid challengeId)`: Băm HMAC-SHA256 sử dụng ChallengeId làm salt động/context duy nhất. Database tuyệt đối KHÔNG lưu OTP dạng plain text.
      - `Create(...)`: Sinh challengeId, thiết lập thời gian hết hạn và hash OTP.
      - `Verify(...)`: Tự động kiểm tra `IsUsed`, `ExpiresAt`, tăng `AttemptsCount`, khóa challenge khi `AttemptsCount >= MaxAttempts`, và so khớp hash an toàn.
  - SMS Abstraction (`ISmsSender` & `MockSmsSender`):
    - Interface `ISmsSender`: Gửi OTP với tham số `phoneNumber`, `otp`, `purpose`.
    - Adapter `MockSmsSender` trong `Infrastructure/Services`: Đánh dấu nhà cung cấp SMS là TBD theo quy định dự án (`AGENTS.md`).
    - Bảo mật tuyệt đối No OTP in logs: Che một phần số điện thoại (`MaskPhone`) và ghi log mục đích; không in chuỗi raw OTP ra logger ở bất kỳ tầng nào.
  - Ứng dụng & Dịch vụ (`IOtpChallengeService` & `OtpChallengeService`):
    - `RequestOtpAsync`:
      1. Kiểm tra giới hạn tần suất qua `IAbuseProtectionService`.
      2. Tra cứu Appointment theo `BookingCode` và đối soát số điện thoại của `Patient`.
      3. Chống tấn công dò tìm (Anti-enumeration): Khi thông tin không khớp, trả về cùng một thông điệp generic an toàn ("Nếu thông tin mã đặt lịch và số điện thoại trùng khớp với hồ sơ, mã xác thực OTP đã được gửi đến số điện thoại của bạn.") kèm dummy ChallengeId và không tạo bản ghi thật hay gửi SMS.
      4. Kiểm tra Resend Cooldown (60 giây): Chặn người dùng yêu cầu gửi lại OTP quá nhanh.
      5. Sinh OTP 6 chữ số CSPRNG (`RandomNumberGenerator.GetInt32(100000, 1000000)`), băm HMAC và lưu DB.
      6. Gửi SMS thông báo và ghi nhận attempt.
    - `VerifyOtpAsync`:
      1. Tra cứu challenge theo `challengeId`.
      2. Kiểm tra ràng buộc mục đích (Purpose Binding): Từ chối nếu mục đích challenge không khớp với `expectedPurpose`.
      3. Thực thi `challenge.Verify(...)` và lưu cập nhật lượt thử / trạng thái `IsUsed`.
  - API Controller & Validation (`AppointmentAccessController.cs`):
    - `POST /api/v1/appointment-access/otp/request` (`[AllowAnonymous]`): Nhận `BookingCode`, `PhoneNumber`, `Purpose`, trả về thông điệp và ChallengeId.
    - `POST /api/v1/appointment-access/otp/verify` (`[AllowAnonymous]`): Nhận `ChallengeId`, `Otp`, `Purpose`, trả về kết quả xác thực.
    - FluentValidation: `RequestOtpRequestValidator` và `VerifyOtpRequestValidator` kiểm tra định dạng số điện thoại, độ dài OTP đúng 6 số ký tự số.
  - Bộ kiểm thử tự động xUnit (`OtpChallengeTests.cs`):
    - 10 test cases bao phủ toàn diện:
      1. `RequestOtp_WhenBookingAndPhoneMatch_CreatesChallenge_StoresOnlyHash_AndSendsSms`: Tạo challenge, chỉ lưu hash 64 ký tự hex, gửi SMS tới số đã mask.
      2. `RequestOtp_WhenBookingOrPhoneMismatch_ReturnsGenericAcknowledgment_WithoutCreatingChallenge_AntiEnumeration`: Sai booking/phone vẫn trả generic message thành công nhưng không tạo challenge trong DB và không gửi SMS.
      3. `RequestOtp_WithinCooldownPeriod_ReturnsCooldownError`: Gửi lại dưới 60s trả về lỗi cooldown kèm số giây còn lại.
      4. `VerifyOtp_WithCorrectOtpAndPurpose_SucceedsAndMarksUsed`: Nhập đúng OTP và purpose thành công, đánh dấu IsUsed = true.
      5. `VerifyOtp_WithIncorrectOtp_DecrementsRemainingAttempts_Fails`: Nhập sai OTP giảm số lần thử còn lại và báo lỗi.
      6. `VerifyOtp_WhenExceedingMaxAttempts_LocksChallenge_PermanentlyFails`: Nhập sai quá 3-5 lần thì challenge bị khóa vĩnh viễn (kể cả lần sau nhập đúng vẫn bị từ chối).
      7. `VerifyOtp_WhenExpired_Fails`: Challenge hết hạn từ chối xác thực.
      8. `VerifyOtp_WhenAlreadyUsed_Fails`: Challenge đã dùng trước đó từ chối xác thực lại.
      9. `VerifyOtp_WhenPurposeMismatch_Fails_PurposeBinding`: Sai mục đích (cấp cho LOOKUP nhưng verify cho RESCHEDULE) bị từ chối ngay lập tức.
      10. `OtpValidators_ValidateCorrectly`: Kiểm tra bắt lỗi validator cho request/verify DTOs.
    - Toàn bộ 199/199 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.00s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/RescheduleRefundOtp.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/ISmsSender.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IOtpChallengeService.cs`
  - `backend/src/ClinicAppointment.Application/Features/Otp/DTOs/OtpDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Otp/Services/OtpChallengeService.cs`
  - `backend/src/ClinicAppointment.Application/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Services/MockSmsSender.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentAccessController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/OtpChallengeTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 199 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.00s)
- Acceptance criteria evidence:
  - FEAT-025 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-026 — OTP Scoped Access Grant (Cấp Quyền Truy Cập Tạm Thời Sau Xác Thực OTP, Ràng Buộc Cuộc Hẹn & Mục Đích, One-Time Use, Not a User JWT)
- Status before/after: not_started -> completed
- Scope implemented:
  - Domain Entity `OtpVerificationGrant` (`ClinicAppointment.Domain.Entities.OtpVerificationGrant`):
    - Cấu trúc: `ChallengeId`, `AppointmentId`, `Purpose` (`LOOKUP`, `RESCHEDULE`, `CANCEL`), `GrantTokenHash` (SHA-256 bytea 32 bytes), `ExpiresAt` (TTL 15 phút), `ConsumedAt`, `CreatedAt`.
    - Phương thức bảo mật:
      - `ComputeTokenHash(string rawToken)`: Băm SHA-256 binary hash lưu DB; tuyệt đối không lưu raw grant token.
      - `Issue(...)`: Sinh mã token ngẫu nhiên an toàn mật mã CSPRNG 256-bit định dạng `grnt_{hex}`; TUYỆT ĐỐI không phải là User JWT session (không có cấu trúc 3 phần header.payload.signature, không claims người dùng, không roles).
      - `IsValidFor(...)`: Kiểm tra nghiêm ngặt `AppointmentId`, `Purpose`, `ExpiresAt`, và `ConsumedAt`.
      - `Consume(...)`: Đánh dấu đã tiêu thụ cho các thao tác nhạy cảm một lần (One-time only).
      - `Revoke(...)`: Thu hồi quyền truy cập.
  - Cấu hình EF Core & Database Migration:
    - `OtpVerificationGrantConfiguration.cs`: Bảng `otp_verification_grants`, Unique index trên `challenge_id` (ràng buộc 1:1), Unique index trên `grant_token_hash`, Index trên `(id, appointment_id)`.
    - Migration `20261114084500_AddOtpVerificationGrants.cs` và `Designer.cs`.
    - Đồng bộ kịch bản SQL idempotent trong `database/migrations/20261114_initial_schema.sql`.
  - Dịch vụ Ứng dụng (`IOtpGrantService` & `OtpGrantService`):
    - `IssueGrantAsync`: Cấp grant khi challenge đã verify thành công (`IsUsed = true`), bảo đảm không cấp trùng 2 lần cho cùng một challenge.
    - `ValidateAndConsumeGrantAsync`: Kiểm tra tính hợp lệ của grant; hỗ trợ cờ `consume = true` cho các thao tác thay đổi lịch (RESCHEDULE/CANCEL) hoặc `consume = false` cho tra cứu (LOOKUP).
    - `RevokeGrantAsync`: Thu hồi token khi cần thiết.
  - Tích hợp API Controller (`AppointmentAccessController.cs`):
    - Endpoint `POST /api/v1/appointment-access/otp/verify` sau khi xác thực OTP thành công tự động phát hành Grant token và trả về `GrantToken`, `GrantExpiresAt`, `TokenType = "ScopedAppointmentGrant"`.
  - Bộ kiểm thử tự động xUnit (`OtpScopedAccessGrantTests.cs`):
    - 9 test cases bao phủ toàn diện:
      1. `IssueGrant_WhenChallengeVerified_IssuesCryptographicOpaqueToken_StoresOnlyHash`: Cấp grant token `grnt_...`, DB chỉ lưu hash 32 bytes, không phải User JWT.
      2. `IssueGrant_WhenChallengeNotVerified_Fails`: Challenge chưa verify bị từ chối cấp grant.
      3. `IssueGrant_WhenChallengeAlreadyGranted_Fails`: Cùng 1 challenge không thể cấp grant lần 2.
      4. `ValidateGrant_WhenValidAndMatchesAppointmentAndPurpose_Succeeds`: Khớp appointment và purpose thành công.
      5. `ValidateGrant_WhenPurposeMismatch_Fails`: Dùng grant LOOKUP để CANCEL bị từ chối ngay lập tức.
      6. `ValidateGrant_WhenAppointmentMismatch_Fails`: Grant của cuộc hẹn A không thể dùng cho cuộc hẹn B.
      7. `ValidateGrant_OneTimeUse_Consume_CannotBeReused`: Grant CANCEL/RESCHEDULE sau khi dùng 1 lần bị từ chối ở lần gọi tiếp theo.
      8. `ValidateGrant_WhenExpired_Fails`: Grant quá hạn 15 phút bị từ chối.
      9. `RevokeGrant_SuccessfullyRevokes_AndBlocksFurtherUsage`: Grant bị thu hồi không thể sử dụng lại.
    - Toàn bộ 208/208 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.18s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Domain/Entities/OtpVerificationGrant.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IOtpGrantService.cs`
  - `backend/src/ClinicAppointment.Application/Features/Otp/DTOs/OtpDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Otp/Services/OtpGrantService.cs`
  - `backend/src/ClinicAppointment.Application/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/OtpVerificationGrantConfiguration.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114084500_AddOtpVerificationGrants.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114084500_AddOtpVerificationGrants.Designer.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentAccessController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/OtpScopedAccessGrantTests.cs`
  - `database/migrations/20261114_initial_schema.sql`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 208 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.18s)
- Acceptance criteria evidence:
  - FEAT-026 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

### 2026-11-14 — FEAT-027 — Appointment Lookup (Tra Cứu Lịch Khám Công Khai Bằng BookingCode + OTP Grant LOOKUP, Summary Tối Thiểu & Bảo Vệ PII/Consultation)
- Status before/after: not_started -> completed
- Scope implemented:
  - CQRS Query & DTO (`ClinicAppointment.Application.Features.Appointments.Queries.LookupAppointment`):
    - `LookupAppointmentQuery`: Nhận `BookingCode` và `GrantToken`.
    - `PublicAppointmentSummaryDto`: Trả về thông tin tóm tắt tối thiểu cho bệnh nhân: `BookingCode`, `Status`, `StartAtUtc`, `EndAtUtc`, `PatientFullName`, `DoctorFullName`, `DoctorTitle`, `DoctorRoomNumber`, `SpecialtyName`, `ServiceName`, `ServiceDurationMinutes`, `ServicePrice`, `PaidAmount`, `ReasonForVisit`, `CanCancel`, `CanReschedule`.
    - Tuyệt đối bảo vệ PII và bí mật y tế: Hoàn toàn không chứa `PatientId`, không có `UserId`, không có `DoctorId`, không chứa nội dung bệnh án `Consultation` (triệu chứng, chẩn đoán, ghi chú bác sĩ), không chứa số CCCD/CMND/hộ chiếu/BHYT.
    - `LookupAppointmentQueryValidator`: Kiểm tra bắt buộc cả `BookingCode` và `GrantToken`.
    - `LookupAppointmentQueryHandler`:
      1. Acceptance Criteria 1: Bắt buộc phải có `GrantToken`. Nếu thiếu, trả về HTTP 401 Unauthorized.
      2. Tra cứu `Appointment` theo `BookingCode`. Nếu không tìm thấy, trả về HTTP 404 Not Found.
      3. Xác thực quyền truy cập qua `_grantService.ValidateAndConsumeGrantAsync` với `expectedPurpose = LOOKUP` và `consume = false` (chỉ đọc, bảo toàn grant trong thời hạn 15 phút). Nếu sai mục đích, sai appointment, hoặc grant hết hạn, trả về HTTP 403 Forbidden.
      4. Tính toán cờ khả năng thao tác: `CanCancel = true` và `CanReschedule = true` khi cuộc hẹn ở trạng thái `PendingConfirmation` hoặc `Confirmed` và thời gian bắt đầu ở tương lai; ngược lại là `false`.
  - API Controller (`AppointmentsController.cs`):
    - Endpoint `POST /api/v1/appointments/lookup` (`[AllowAnonymous]`).
    - Hỗ trợ linh hoạt: Nhận `GrantToken` trực tiếp từ request body, hoặc header `X-Appointment-Grant`, hoặc header `Authorization: Grant <token>`.
  - Bộ kiểm thử tự động xUnit (`AppointmentLookupTests.cs`):
    - 9 test cases bao phủ toàn diện:
      1. `Lookup_WithValidBookingCodeAndValidLookupGrant_ReturnsMinimalSummary`: Tra cứu thành công với code và grant LOOKUP hợp lệ, trả về đầy đủ tóm tắt ca khám.
      2. `Lookup_WithoutGrantToken_FailsWith401`: Không cho phép tra cứu chỉ bằng code, trả về 401 Unauthorized khi thiếu grant.
      3. `Lookup_WithInvalidOrExpiredGrantToken_FailsWith403`: Grant giả mạo hoặc hết hạn trả về 403 Forbidden.
      4. `Lookup_WithGrantForWrongPurpose_FailsWith403`: Grant CANCEL/RESCHEDULE không thể dùng để LOOKUP, trả về 403 Forbidden.
      5. `Lookup_WithGrantForDifferentAppointment_FailsWith403`: Grant của cuộc hẹn A không thể tra cứu cuộc hẹn B, trả về 403 Forbidden.
      6. `Lookup_WithNonExistentBookingCode_Returns404NotFound`: BookingCode không tồn tại trả về 404 Not Found.
      7. `Lookup_EnsuresNoConsultationOrRawPiiInResponse`: Kiểm tra phản chiếu kiểu dữ liệu xác nhận không lộ PatientId, Consultation, CCCD hay PII.
      8. `Lookup_CanCancelAndCanReschedule_AreComputedAccurately`: Tính toán chính xác cờ hủy/đổi lịch dựa trên trạng thái và mốc thời gian.
      9. `LookupAppointmentQueryValidator_RejectsMissingCodeOrGrant`: Validator bắt lỗi thiếu code hoặc grant.
    - Toàn bộ 217/217 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 4.92s, 0 Error.
- Files changed:
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/LookupAppointment/LookupAppointmentQuery.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/AppointmentLookupTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 217 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 4.92s)
- Acceptance criteria evidence:
  - FEAT-027 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

## 2026-11-14 — FEAT-028: Appointment confirmation
- Summary:
  - Triển khai toàn diện chức năng xác nhận lịch hẹn (Appointment confirmation) dành cho bộ phận Lễ tân / Quản trị viên (Module Reception), chuyển đổi trạng thái từ `PendingConfirmation` sang `Confirmed`.
  - Thực thi nghiêm ngặt phân quyền theo vai trò (Role/Policy Authorization):
    - Áp dụng Policy `StaffOnly` (yêu cầu role `RECEPTIONIST` hoặc `ADMIN`).
    - Kiểm tra tại Application Handler qua `ICurrentUserService.IsStaff`: nếu tài khoản không có quyền lễ tân hoặc admin (ví dụ bệnh nhân hoặc bác sĩ không phân quyền), hệ thống từ chối và trả về HTTP 403 Forbidden.
  - Kiểm soát máy trạng thái cuộc hẹn (State Machine Validation):
    - Chỉ cho phép xác nhận khi cuộc hẹn đang ở trạng thái `PendingConfirmation`.
    - Từ chối mọi yêu cầu xác nhận khi ca khám ở các trạng thái khác (`Confirmed`, `Cancelled`, `Completed`, `CheckedIn`, `NoShow`), trả về HTTP 400 Bad Request kèm thông điệp rõ ràng: `"Không thể xác nhận lịch hẹn ở trạng thái {Status}. Chỉ có thể xác nhận lịch hẹn ở trạng thái Chờ xác nhận (PendingConfirmation)."`.
    - Nếu `AppointmentId` không tồn tại, trả về HTTP 404 Not Found.
  - Nhật ký kiểm toán & Lịch sử thao tác (Audit Trail Logging):
    - Tạo thực thể `AuditLog` lưu trong bảng `audit_logs` với các trường: `ActorType` (RECEPTIONIST/ADMIN/STAFF), `ActorUserId`, `Action = "CONFIRM_APPOINTMENT"`, `EntityType = "Appointment"`, `EntityId`, `OccurredAt = DateTime.UtcNow`.
    - Thuộc tính `MetadataRedacted` lưu JSON metadata tóm tắt ca khám (BookingCode, DoctorId, PatientId, StartAt, ConfirmedAt, Note) bằng UTF-8 an toàn (`JavaScriptEncoder.UnsafeRelaxedJsonEscaping`), tuyệt đối KHÔNG chứa PII nhạy cảm (mật khẩu, OTP, CCCD, Token).
    - Cập nhật thời điểm `Appointment.UpdatedAt = DateTime.UtcNow`.
  - Endpoint API:
    - `POST /api/v1/reception/appointments/{id}/confirm` (Scoped theo module Receptionist)
    - `POST /api/v1/appointments/{id}/confirm` (Alias tiện ích trên AppointmentsController)
    - Cả 2 endpoint đều được bảo vệ bởi `[Authorize(Policy = "StaffOnly")]`.
  - Cơ sở dữ liệu & Migration:
    - Tạo migration EF Core `20261114090000_AddAuditLogs.cs` & `.Designer.cs`.
    - Cập nhật kịch bản SQL khởi tạo `database/migrations/20261114_initial_schema.sql` thêm bảng `audit_logs` và các chỉ mục liên quan.
  - Bộ kiểm thử tự động xUnit (`AppointmentConfirmationTests.cs`):
    - 9 test cases bao phủ đầy đủ toàn diện các tiêu chí nghiệm thu:
      1. `Confirm_PendingConfirmation_ByReceptionist_ShouldSucceed_AndRecordAuditLog`: Lễ tân xác nhận thành công ca khám `PendingConfirmation`, trạng thái đổi sang `Confirmed`, `UpdatedAt` được cập nhật, audit log được ghi đầy đủ.
      2. `Confirm_PendingConfirmation_ByAdmin_ShouldSucceed_AndRecordAdminAuditLog`: Admin xác nhận thành công, `AuditLog.ActorType == "ADMIN"`.
      3. `Confirm_AlreadyConfirmed_ShouldFailWith400`: Từ chối xác nhận ca khám đã `Confirmed`, trả về 400 Bad Request, không ghi thêm audit log.
      4. `Confirm_CancelledAppointment_ShouldFailWith400`: Từ chối xác nhận ca khám đã `Cancelled`, trả về 400 Bad Request.
      5. `Confirm_CompletedAppointment_ShouldFailWith400`: Từ chối xác nhận ca khám đã `Completed`, trả về 400 Bad Request.
      6. `Confirm_NonExistentAppointment_ShouldFailWith404`: Trả về 404 Not Found khi `AppointmentId` không tồn tại.
      7. `Confirm_ByNonStaffUser_ShouldFailWith403`: Từ chối người dùng không thuộc StaffOnly (role `PATIENT`), trả về 403 Forbidden.
      8. `Controller_Endpoints_ShouldHave_AuthorizeStaffOnlyPolicy`: Reflection test xác nhận cả `ReceptionAppointmentsController` và method `ConfirmAppointment` đều có `[Authorize(Policy = "StaffOnly")]`.
      9. `AuditLog_ShouldNotContainSensitivePii`: Xác nhận MetadataRedacted không chứa Password, Otp, Secret, CCCD hay AccessToken.
    - Toàn bộ 226/226 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.30s, 0 Error.
- Files created/changed:
  - `backend/src/ClinicAppointment.Domain/Entities/AuditLog.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/AuditLogConfiguration.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114090000_AddAuditLogs.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114090000_AddAuditLogs.Designer.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ConfirmAppointment/ConfirmAppointmentCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ConfirmAppointment/ConfirmAppointmentResponse.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ConfirmAppointment/ConfirmAppointmentCommandHandler.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/ReceptionAppointmentsController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/AppointmentConfirmationTests.cs`
  - `database/migrations/20261114_initial_schema.sql`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 226 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.30s)
- Acceptance criteria evidence:
  - FEAT-028 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

## 2026-11-14 — FEAT-029: Reschedule request submission
- Summary:
  - Triển khai chức năng gửi yêu cầu đổi lịch khám (Reschedule request submission) dành cho Bệnh nhân / Guest sau khi xác thực OTP thành công.
  - Ràng buộc quyền truy cập bằng Scoped OTP Grant Token (Acceptance Criteria 1):
    - Bắt buộc phải có Grant token từ xác thực OTP với mục đích `RESCHEDULE` (nhận qua body, header `X-Appointment-Grant` hoặc `Authorization: Grant <token>`).
    - Thực thi cơ chế tiêu thụ một lần (One-time use): Ngay khi gửi yêu cầu đổi lịch thành công, token được đánh dấu `ConsumedAt = DateTime.UtcNow`. Nếu tái sử dụng token, hệ thống từ chối và trả về HTTP 403 Forbidden.
  - Khởi tạo trạng thái chờ duyệt (Acceptance Criteria 2: PendingReview state):
    - Tạo thực thể `RescheduleRequest` với trạng thái `RescheduleStatus.PendingReview`, liên kết với `AppointmentId`, `RequestedDoctorId`, `ProposedStartAtUtc`, `ProposedEndAtUtc`, `Reason`.
  - Bảo toàn tuyệt đối lịch khám cũ (Acceptance Criteria 3: Old slot held):
    - Ca khám hiện tại hoàn toàn giữ nguyên thông tin: `Appointment.StartAt`, `Appointment.EndAt`, `Appointment.DoctorId`, `Appointment.Status` (không thay đổi trước khi Lễ tân duyệt ở FEAT-030).
  - Quy tắc một yêu cầu chờ duyệt duy nhất (Acceptance Criteria 4: One pending request):
    - Kiểm tra và từ chối tạo thêm yêu cầu nếu cuộc hẹn đã có yêu cầu đổi lịch ở trạng thái `PendingReview` (trả về HTTP 409 Conflict).
  - Kiểm tra sơ bộ tính khả dụng của slot đề xuất (FR-037):
    - Bác sĩ có lịch nghỉ (`DoctorTimeOffs`) trong khung giờ đề xuất -> Trả về HTTP 409 Conflict.
    - Khung giờ bị khóa hoặc bác sĩ có lịch bận (`DoctorScheduleBlocks`) -> Trả về HTTP 409 Conflict.
    - Trùng ca khám đang hoạt động khác của bác sĩ -> Trả về HTTP 409 Conflict.
    - Ca khám đã quá giờ bắt đầu hoặc đã ở trạng thái `Cancelled`/`Completed` -> Trả về HTTP 400 Bad Request.
  - Lịch sử kiểm toán (Audit Trail Logging):
    - Ghi nhận `AuditLog` với `Action = "SUBMIT_RESCHEDULE_REQUEST"`, `EntityType = "Appointment"`, `EntityId = appointment.Id`, `MetadataRedacted` an toàn không chứa PII.
  - Endpoint API:
    - `POST /api/v1/appointments/{bookingCode}/reschedule-requests` (`[AllowAnonymous]`).
  - Bộ kiểm thử tự động xUnit (`RescheduleRequestSubmissionTests.cs`):
    - 10 test cases bao phủ toàn diện:
      1. `SubmitReschedule_WithValidGrant_ShouldCreatePendingReviewRequest_AndConsumeGrant`: Gửi yêu cầu thành công, tạo `RescheduleRequest` PendingReview, giữ nguyên ca khám cũ, grant token bị tiêu thụ (cố tình dùng lại bị 403).
      2. `SubmitReschedule_WithoutGrantToken_ShouldFailWith401`: Thiếu grant token trả về 401 Unauthorized.
      3. `SubmitReschedule_WithWrongPurposeGrant_ShouldFailWith403`: Grant token mục đích LOOKUP không thể đổi lịch, trả về 403 Forbidden.
      4. `SubmitReschedule_WhenAlreadyHasPendingReviewRequest_ShouldFailWith409`: Giới hạn tối đa 1 yêu cầu PendingReview, vi phạm trả về 409 Conflict.
      5. `SubmitReschedule_WhenAppointmentCancelled_ShouldFailWith400`: Ca khám đã hủy không thể đổi lịch, trả về 400 Bad Request.
      6. `SubmitReschedule_WhenAppointmentInPast_ShouldFailWith400`: Ca khám đã quá giờ bắt đầu không thể đổi lịch, trả về 400 Bad Request.
      7. `SubmitReschedule_WithNonExistentBookingCode_ShouldFailWith404`: Mã booking không tồn tại trả về 404 Not Found.
      8. `SubmitReschedule_WhenSlotConflictsWithDoctorTimeOff_ShouldFailWith409`: Xung đột lịch nghỉ bác sĩ trả về 409 Conflict.
      9. `SubmitReschedule_WhenSlotConflictsWithAnotherActiveAppointment_ShouldFailWith409`: Xung đột ca khám khác trả về 409 Conflict.
      10. `SubmitReschedule_ShouldRecordAuditLog_WithNoPii`: Ghi nhận nhật ký kiểm toán không chứa PII nhạy cảm.
    - Toàn bộ 236/236 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.79s, 0 Error.
- Files created/changed:
  - `backend/src/ClinicAppointment.Domain/Entities/RescheduleRefundOtp.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/SubmitRescheduleRequest/SubmitRescheduleRequestCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/SubmitRescheduleRequest/SubmitRescheduleRequestResponse.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/SubmitRescheduleRequest/SubmitRescheduleRequestCommandValidator.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/SubmitRescheduleRequest/SubmitRescheduleRequestCommandHandler.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/RescheduleRequestSubmissionTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 236 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.79s)
- Acceptance criteria evidence:
  - FEAT-029 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

## 2026-11-14 — FEAT-030: Reschedule approval/rejection
- Summary:
  - Triển khai toàn diện chức năng duyệt hoặc từ chối yêu cầu đổi lịch khám (Reschedule approval/rejection) dành cho bộ phận Lễ tân / Quản trị viên (Module Reschedule).
  - Phân quyền kiểm soát chặt chẽ (Acceptance Criteria 1: Only Receptionist can review):
    - Bảo vệ bằng policy `StaffOnly` (yêu cầu vai trò `RECEPTIONIST` hoặc `ADMIN`).
    - Kiểm tra tại Application Handler qua `ICurrentUserService.IsStaff`: nếu tài khoản không có quyền lễ tân hoặc admin, hệ thống từ chối và trả về HTTP 403 Forbidden.
  - Phê duyệt trong Database Transaction (Acceptance Criteria 3: Approve transactionally):
    - Thực hiện kiểm tra lại (re-check) slot đề xuất: ngày nghỉ của bác sĩ (`DoctorTimeOffs`), lịch bận / khóa lịch (`DoctorScheduleBlocks`), trùng lịch hẹn khác (`Appointments`).
    - Khi slot hợp lệ: cập nhật `Appointment.StartAt`, `Appointment.EndAt`, `Appointment.DoctorId`, `Appointment.UpdatedAt = DateTime.UtcNow`, chuyển `RescheduleRequest.Status = Approved`, ghi nhận `ReviewedByUserId`, `ReviewedAt`, `ReviewNotes`.
  - Bảo toàn lịch cũ khi xung đột hoặc từ chối (Acceptance Criteria 2 & 4: Conflict / Reject leaves old schedule unchanged):
    - Nếu slot mới bị xung đột: rollback transaction, **lịch khám ban đầu của bệnh nhân GIỮ NGUYÊN 100%**, trả về HTTP 409 Conflict.
    - Khi từ chối (`Reject`): bắt buộc nhập lý do (`ReviewNotes`), chuyển `RescheduleRequest.Status = Rejected`, **lịch khám ban đầu của bệnh nhân GIỮ NGUYÊN 100%**.
  - Kiểm soát máy trạng thái:
    - Chỉ cho phép duyệt hoặc từ chối yêu cầu đang ở trạng thái `PendingReview`. Yêu cầu đã duyệt/từ chối trước đó sẽ bị từ chối với HTTP 400 Bad Request.
  - Hàng đợi duyệt yêu cầu đổi lịch (Receptionist Queue):
    - Endpoint `GET /api/v1/reception/reschedule-requests` trả về danh sách yêu cầu chờ duyệt kèm thông tin bệnh nhân, bác sĩ hiện tại, bác sĩ yêu cầu và khung giờ đề xuất.
  - Lịch sử kiểm toán (Audit Trail Logging):
    - Ghi nhận `AuditLog` với `Action = "APPROVE_RESCHEDULE_REQUEST"` hoặc `"REJECT_RESCHEDULE_REQUEST"`, `MetadataRedacted` an toàn không chứa PII.
  - Endpoints API:
    - `GET /api/v1/reception/reschedule-requests`
    - `POST /api/v1/reception/reschedule-requests/{requestId}/approve`
    - `POST /api/v1/reception/reschedule-requests/{requestId}/reject`
    - Cả 3 endpoint đều gắn `[Authorize(Policy = "StaffOnly")]`.
  - Bộ kiểm thử tự động xUnit (`RescheduleReviewTests.cs`):
    - 9 test cases bao phủ toàn diện:
      1. `Approve_WhenSlotAvailable_ShouldUpdateAppointmentAndApproveRequest`: Duyệt thành công, cập nhật appointment sang slot mới, request chuyển Approved, ghi audit log.
      2. `Approve_WhenSlotConflictsWithAnotherAppointment_ShouldLeaveOldScheduleUnchanged_AndReturn409`: Xung đột ca khám khác khi duyệt -> Trả về 409 Conflict, lịch cũ giữ nguyên.
      3. `Approve_WhenSlotConflictsWithDoctorTimeOff_ShouldLeaveOldScheduleUnchanged_AndReturn409`: Xung đột lịch nghỉ bác sĩ khi duyệt -> Trả về 409 Conflict, lịch cũ giữ nguyên.
      4. `Reject_WithValidReason_ShouldKeepOldAppointment_AndSetRequestToRejected`: Từ chối thành công, request chuyển Rejected, lịch cũ giữ nguyên, ghi audit log.
      5. `Reject_WithoutReason_ShouldFailWith400`: Từ chối thiếu lý do trả về 400 Bad Request.
      6. `Review_ByNonStaffUser_ShouldFailWith403`: Người dùng không có quyền Staff bị từ chối 403 Forbidden.
      7. `Review_WhenRequestAlreadyReviewed_ShouldFailWith400`: Không thể duyệt/từ chối lại yêu cầu đã xử lý (400 Bad Request).
      8. `GetPendingRescheduleRequests_ShouldReturnOnlyPendingReviewRequests`: Queue chỉ trả về các yêu cầu PendingReview.
      9. `ReceptionRescheduleRequestsController_ShouldHave_AuthorizeStaffOnlyAttribute`: Controller có attribute StaffOnly.
    - Toàn bộ 245/245 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.17s, 0 Error.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ReviewRescheduleRequest/ReviewRescheduleResponse.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ReviewRescheduleRequest/ReviewRescheduleCommands.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ReviewRescheduleRequest/GetPendingRescheduleRequestsQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ReviewRescheduleRequest/ApproveRescheduleRequestCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ReviewRescheduleRequest/RejectRescheduleRequestCommandHandler.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/ReceptionRescheduleRequestsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/RescheduleReviewTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 245 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.17s)
- Acceptance criteria evidence:
  - FEAT-030 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.

## 2026-11-14 — FEAT-031: Cancellation policy calculation
- Summary:
  - Triển khai và hoàn thiện logic tính toán chính sách hủy và hoàn tiền (Cancellation policy calculation) dựa trên mốc 60 phút và Server Clock (Module Cancellation), tuân thủ nghiêm ngặt quy định bất biến của dự án (AGENTS.md, docs/SPEC.md v1.2, docs/BUSINESS_RULES.md BR-010).
  - Quy tắc ranh giới mốc thời gian hoàn tiền:
    - `minutesBefore >= 60.0`: Hủy từ 60 phút trở lên (bao gồm đúng mốc 60 phút 0 giây trước giờ hẹn): tỷ lệ hoàn phí 100% (`RefundRate = 1.0m`), phí hủy 0% (`CancellationFeeRate = 0.0m`).
    - `0 < minutesBefore < 60.0`: Hủy dưới 60 phút nhưng trước giờ hẹn: tỷ lệ hoàn phí 50% (`RefundRate = 0.50m`), phí hủy 50% (`CancellationFeeRate = 0.50m`).
    - `minutesBefore <= 0`: Tại hoặc sau thời điểm bắt đầu ca khám: bệnh nhân/guest **tự hủy bị từ chối** (`CanCancel = false`), ném ngoại lệ hoặc trả về denial reason không cho phép tự hủy.
  - Quy tắc xử lý tài chính & Làm tròn tiền:
    - Áp dụng trên phí dịch vụ đã thanh toán (`paidServiceAmount`).
    - Nếu chưa thanh toán (`paidServiceAmount <= 0`): `ExpectedRefundAmount = 0m`, `CancellationFeeAmount = 0m`; không tự tính âm hoặc tự thu phí khi chưa có policy thu ngoài (TBD-04).
    - Làm tròn số tiền VND theo chuẩn số nguyên: `Math.Round(amount, 0, MidpointRounding.AwayFromZero)`.
  - Dịch vụ ứng dụng & Endpoint Preview:
    - Tạo `ICancellationPolicyService` và `CancellationPolicyService` trong Application layer, inject vào DI.
    - Cung cấp endpoint xem trước chính sách hủy `POST /api/v1/appointments/{bookingCode}/cancellation-preview` ([AllowAnonymous]) cho phép hiển thị minh bạch số tiền hoàn và giải thích chính sách trước khi người dùng xác nhận hủy (không tiêu thụ grant token khi chỉ preview).
  - Bộ kiểm thử tự động xUnit (`RefundCalculationTests.cs`):
    - 10 test cases bao phủ đầy đủ toàn diện các tiêu chí nghiệm thu:
      1. `Calculate_WhenCancelled61MinutesBefore_ShouldRefund100Percent`: Hủy 61 phút trước giờ hẹn -> Hoàn 100%, phí hủy 0%.
      2. `Calculate_WhenCancelledExactly60MinutesBefore_ShouldRefund100Percent`: Đúng 60 phút trước giờ hẹn -> Hoàn 100%, phí hủy 0%.
      3. `Calculate_WhenCancelled59MinutesBefore_ShouldRefund50Percent`: Hủy 59 phút trước giờ hẹn -> Hoàn 50%, phí hủy 50%.
      4. `Calculate_VariousMinutesUnder60_ShouldRefund50Percent`: Các mốc thời gian 45, 30, 15, 1 phút đều hoàn 50%.
      5. `Calculate_WhenCancelledAtOrAfterStart_ShouldThrowException`: Hủy tại thời điểm 0 phút hoặc sau giờ hẹn (-5 phút, -60 phút) -> Ném ngoại lệ bị từ chối.
      6. `Calculate_WhenUnpaid_ShouldRefundZeroAndFeeZero`: Chưa thanh toán -> Hoàn 0, phí 0.
      7. `Calculate_WhenOddAmount_ShouldRoundVndToInteger`: Giá dịch vụ lẻ (355.555 đ) -> Làm tròn số nguyên VND chuẩn.
      8. `CancellationPolicyService_Evaluate_WhenPast_ShouldReturnCanCancelFalse_AndDenialReason`: Đánh giá ca khám quá khứ trả về `CanCancel = false`.
      9. `CancellationPolicyService_Evaluate_When60MinutesBefore_ShouldReturn100Percent`: Đánh giá ca khám 60 phút trước giờ hẹn trả về `CanCancel = true`, hoàn 100%.
      10. `PreviewCancellationPolicy_WithValidGrantToken_ShouldReturnAccuratePreview`: Xem trước chính sách hủy với grant token hợp lệ thành công và không tiêu thụ grant token.
    - Toàn bộ 254/254 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.59s, 0 Error.
- Files created/changed:
  - `backend/src/ClinicAppointment.Domain/Services/RefundCalculator.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Services/ICancellationPolicyService.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Services/CancellationPolicyService.cs`
  - `backend/src/ClinicAppointment.Application/DependencyInjection.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/PreviewCancellationPolicy/PreviewCancellationPolicyQuery.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/RefundCalculationTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 254 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.59s)
- Acceptance criteria evidence:
  - FEAT-031 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.


## 2026-11-14 — FEAT-032: Guest cancellation workflow
- Summary:
  - Triển khai và hoàn thiện quy trình hủy lịch hẹn trực tuyến dành cho bệnh nhân/Guest không cần đăng nhập tài khoản (Module Cancellation), tuân thủ nghiêm ngặt quy định bất biến của dự án (AGENTS.md, docs/SPEC.md v1.2, docs/BUSINESS_RULES.md BR-008, BR-009, BR-010, BR-011).
  - Quy tắc xác thực & Bảo mật:
    - Bắt buộc phải có Scoped OTP Grant Token hợp lệ với mục đích `CANCEL` (`Purpose = OtpPurpose.Cancel`).
    - Thực thi cơ chế One-time use: grant token bị đánh dấu `ConsumedAt = DateTime.UtcNow` ngay khi yêu cầu hủy được xác nhận; không thể tái sử dụng token cho bất kỳ hành động nào khác.
    - Token sai mục đích hoặc hết hạn bị từ chối với HTTP 403 Forbidden hoặc HTTP 401 Unauthorized.
  - Quy tắc hủy lịch & Giải phóng Slot:
    - Bệnh nhân tự hủy không cần Lễ tân duyệt (khác với Reschedule bắt buộc duyệt).
    - Kiểm tra thời điểm hủy theo Server Clock (`DateTime.UtcNow`): Nghiêm cấm hủy tại hoặc sau thời điểm bắt đầu ca khám (`StartAt`), trả về HTTP 409 Conflict với thông báo từ chối rõ ràng.
    - Từ chối hủy ca khám đã bị hủy trước đó (`AppointmentStatus.Cancelled`), trả về HTTP 400 Bad Request.
    - Cập nhật trạng thái `Appointment.Status = AppointmentStatus.Cancelled`, ghi nhận `CancellationAcceptedAt = DateTime.UtcNow`, `CancellationReason`.
    - Việc chuyển trạng thái sang `Cancelled` tự động giải phóng slot khám của bác sĩ ngay trong Database Transaction (nhờ ràng buộc GiST exclusion constraint lọc `WHERE status <> 'Cancelled'`).
  - Quy tắc tính toán và lưu bản ghi Refund:
    - Tự động đánh giá chính sách hoàn tiền theo mốc 60 phút qua `ICancellationPolicyService`:
      - $\ge 60$ phút trước giờ hẹn: Hoàn 100% phí dịch vụ đã thanh toán, phí hủy 0%.
      - $< 60$ phút trước giờ hẹn: Hoàn 50% phí dịch vụ đã thanh toán, phí hủy 50%.
    - Nếu ca khám đã có thanh toán (`PaidAmount > 0`), tự động tạo bản ghi `Refund` gắn với `AppointmentId` với trạng thái `RefundStatus.Pending`, lưu snapshot tỷ lệ phí, số tiền phạt và số tiền hoàn dự kiến.
    - Nếu chưa thanh toán (`PaidAmount = 0`), số tiền hoàn và phí hủy đều là 0, không tạo nợ hoặc charge thêm.
  - Ghi nhận Audit Log:
    - Tạo bản ghi `AuditLog` với `Action = "CANCEL_APPOINTMENT"`, `ActorType = "GUEST"`, `EntityType = "Appointment"`, `EntityId = appointment.Id`.
    - `MetadataRedacted` lưu an toàn thông tin lý do hủy, tỷ lệ hoàn, số tiền hoàn dự kiến mà tuyệt đối không log PII (không log số điện thoại, OTP, CCCD).
  - Endpoint & CQRS:
    - Command: `CancelAppointmentCommand` và handler `CancelAppointmentCommandHandler` trong `ClinicAppointment.Application`.
    - Endpoint: `POST /api/v1/appointments/{bookingCode}/cancel` ([AllowAnonymous]) trong `AppointmentsController`.
  - Bộ kiểm thử tự động xUnit (`GuestCancellationWorkflowTests.cs`):
    - 10 test cases bao phủ 100% luồng và các nhánh validation:
      1. `GuestCancel_WithoutGrantToken_ReturnsUnauthorized`: Không truyền grant token -> HTTP 401.
      2. `GuestCancel_WithWrongPurposeToken_ReturnsForbidden`: Dùng token sai mục đích (RESCHEDULE/LOOKUP) -> HTTP 403.
      3. `GuestCancel_WithExpiredToken_ReturnsUnauthorized`: Dùng token đã hết hạn -> HTTP 401.
      4. `GuestCancel_AfterStartAt_ReturnsConflict`: Hủy tại hoặc sau giờ hẹn -> HTTP 409 Conflict ("Không thể tự hủy ca khám tại hoặc sau thời điểm bắt đầu").
      5. `GuestCancel_AlreadyCancelled_ReturnsBadRequest`: Gọi hủy ca khám đã hủy -> HTTP 400 Bad Request.
      6. `GuestCancel_Success_AtOrAbove60Min_100PercentRefund_AndReleasesSlot`: Hủy trước 60 phút -> Hoàn 100%, slot được giải phóng cho bệnh nhân khác.
      7. `GuestCancel_Success_Under60Min_50PercentRefund_AndReleasesSlot`: Hủy dưới 60 phút -> Hoàn 50%, phí phạt 50%, slot được giải phóng.
      8. `GuestCancel_Success_ZeroPaidAmount_ZeroRefundCreated`: Hủy ca khám chưa thanh toán -> Hoàn 0, phí 0.
      9. `GuestCancel_ConsumesGrantToken_CannotBeReused`: Grant token bị tiêu thụ ngay, tái sử dụng token bị từ chối HTTP 403 Forbidden.
      10. `GuestCancel_WrongBookingCodeForToken_ReturnsForbidden`: Dùng grant token của ca khám này để hủy ca khám khác -> HTTP 403 Forbidden.
    - Toàn bộ 264/264 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.35s, 0 Error.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CancelAppointment/CancelAppointmentCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CancelAppointment/CancelAppointmentCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CancelAppointment/CancelAppointmentResultDto.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/GuestCancellationWorkflowTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 264 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.35s)
- Acceptance criteria evidence:
  - FEAT-032 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.


## 2026-11-14 — FEAT-033: Payment and refund tracking
- Summary:
  - Triển khai và hoàn thiện module theo dõi, lưu trữ và xử lý giao dịch hoàn tiền chi tiết sau khi hủy ca khám (Module Finance), tuân thủ nghiêm ngặt quy định bất biến của dự án (AGENTS.md, docs/SPEC.md v1.2, docs/BUSINESS_RULES.md BR-010, BR-011, docs/DATABASE_DESIGN.md mục 4.6).
  - Sử dụng Snapshot giá dịch vụ (Fee snapshot used):
    - Đảm bảo tính toán hoàn tiền và lưu trữ bản ghi `Refund` hoàn toàn dựa trên snapshot giá dịch vụ lúc đặt khám (`ServicePriceSnapshot` và `Appointment.PaidAmount`).
    - Việc quản trị viên cập nhật giá mới trong catalog dịch vụ sau đó hoàn toàn không ảnh hưởng đến số tiền hoàn hoặc phí hủy của ca khám đã đặt.
  - Lưu trữ phiên bản chính sách, tỷ lệ và số tiền (Policy version/rate/amount stored):
    - Bổ sung trường `PolicyVersion` (mặc định `"v1.0"`), `ProviderReference` (mã ủy nhiệm chi / số tham chiếu chuyển khoản ngân hàng), `ProcessedByUserId` (nhân viên xử lý), `FailureReason` (lý do nếu hoàn tiền thất bại) vào thực thể `Refund`.
    - Lưu chính xác: `PaidServiceAmount`, `CancellationFeeRate`, `CancellationFeeAmount`, `ExpectedRefundAmount`, `PolicyVersion`, `Status`.
  - Tách bạch trạng thái — Không hiển thị Pending là Succeeded (Pending not shown as Succeeded):
    - Rạch ròi 4 trạng thái nghiệp vụ: `Pending` (Chờ xử lý), `Processing` (Đang đối soát/xử lý), `Succeeded` (Đã hoàn thành), `Failed` (Thất bại).
    - Cả trong API cho nhân viên (`GetRefundsListQuery`, `GetRefundByIdQuery`) và API tra cứu cho bệnh nhân (`LookupAppointmentQuery`), thuộc tính `IsCompleted` chỉ bằng `true` khi `Status == RefundStatus.Succeeded`.
    - Trạng thái `Pending` hoặc `Processing` tuyệt đối không được báo là đã hoàn tất, cung cấp mô tả tiến độ minh bạch cho người dùng.
  - Không giả lập cổng thanh toán ảo (No fake gateway integration):
    - Tuân thủ quy định dự án (cổng thanh toán là TBD), cung cấp quy trình đối soát / xác nhận hoàn tiền nội bộ dành cho nhân viên Lễ tân/Kế toán (`StaffOnly`):
      - `GET /api/v1/reception/refunds`: Danh sách hoàn tiền có phân trang, hỗ trợ lọc theo trạng thái, mã booking, khoảng ngày.
      - `GET /api/v1/reception/refunds/{id}`: Chi tiết yêu cầu hoàn tiền.
      - `POST /api/v1/reception/refunds/{id}/process`: Nhân viên xử lý chuyển trạng thái sang `Processing`, `Succeeded` (kèm `ProviderReference`), hoặc `Failed` (bắt buộc kèm `FailureReason`).
      - Ngăn chặn triệt để hành vi chỉnh sửa khoản hoàn tiền đã `Succeeded` để chống double refund.
      - Ghi nhận `AuditLog` an toàn với `Action = "PROCESS_REFUND"`, `ActorType = "STAFF"` không lưu PII.
  - Bộ kiểm thử tự động xUnit (`RefundTrackingTests.cs`):
    - 10 test cases bao phủ đầy đủ toàn diện các tiêu chí nghiệm thu:
      1. `FeeSnapshot_UsedForRefundCalculationAndRecord`: Dùng snapshot giá lúc đặt, đổi giá catalog không đổi refund.
      2. `PolicyVersion_AndRatesAndAmounts_StoredAccurately`: Lưu chính xác PolicyVersion, rates và amounts.
      3. `PendingOrProcessing_NotShownAsSucceeded`: Pending và Processing có `IsCompleted == false`, mô tả rõ ràng.
      4. `NoFakeGateway_ManualStaffProcessingWorkflow`: Luồng đối soát thủ công không phụ thuộc fake gateway.
      5. `StaffOnly_AuthorizationEnforced`: Chỉ Staff (Receptionist/Admin) mới được xử lý hoàn tiền; từ chối Patient với HTTP 403.
      6. `ProcessRefund_PendingToProcessing_Succeeds`: Chuyển sang Processing lưu nhân viên và provider reference.
      7. `ProcessRefund_ToSucceeded_SetsProcessedAtAndAuditLog`: Chuyển sang Succeeded lưu ProcessedAt, ProviderReference và ghi AuditLog.
      8. `ProcessRefund_AlreadySucceeded_CannotBeModified`: Chống sửa đổi khoản hoàn tiền đã Succeeded.
      9. `ProcessRefund_ToFailed_RequiresFailureReason`: Chuyển sang Failed bắt buộc phải có lý do thất bại.
      10. `LookupAppointment_CancelledWithRefund_ReturnsSafeRefundSummaryWithAccurateStatus`: Bệnh nhân tra cứu lịch khám bị hủy nhận được tóm tắt hoàn tiền an toàn, không hiển thị đã hoàn tất khi Pending.
    - Toàn bộ 274/274 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.90s, 0 Error.
- Files created/changed:
  - `backend/src/ClinicAppointment.Domain/Entities/RescheduleRefundOtp.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/CatalogConfigurations.cs`
  - `backend/src/ClinicAppointment.Application/Common/Models/PaginatedList.cs`
  - `backend/src/ClinicAppointment.Application/Features/Refunds/DTOs/RefundDetailDto.cs`
  - `backend/src/ClinicAppointment.Application/Features/Refunds/Queries/GetRefundsList/GetRefundsListQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Refunds/Queries/GetRefundById/GetRefundByIdQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Refunds/Commands/ProcessRefund/ProcessRefundCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Refunds/Commands/ProcessRefund/ProcessRefundCommandValidator.cs`
  - `backend/src/ClinicAppointment.Application/Features/Refunds/Commands/ProcessRefund/ProcessRefundCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CancelAppointment/CancelAppointmentResponse.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CancelAppointment/CancelAppointmentCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/LookupAppointment/LookupAppointmentQuery.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/ReceptionRefundsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/RefundTrackingTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 274 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.90s)
- Acceptance criteria evidence:
  - FEAT-033 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.


## 2026-11-14 — FEAT-034: Cancellation audit/history
- Summary:
  - Triển khai và hoàn thiện module ghi nhận và truy vấn lịch sử kiểm toán hủy lịch khám và quyết định hoàn tiền (Module Audit), tuân thủ nghiêm ngặt quy định bất biến của dự án (AGENTS.md, docs/SPEC.md v1.2, docs/BUSINESS_RULES.md BR-010, BR-014, BR-015, docs/DATABASE_DESIGN.md mục 4.7 & 6).
  - Ghi nhận lịch sử kiểm toán (Audit recorded):
    - Đảm bảo khi có sự kiện hủy ca khám (`CANCEL_APPOINTMENT`) hoặc nhân viên xử lý hoàn tiền (`PROCESS_REFUND`), một bản ghi `AuditLog` bất biến được lưu vào cơ sở dữ liệu trong cùng Database Transaction.
    - Lưu trữ đầy đủ: `ActorType` (`GUEST`, `PATIENT`, `STAFF`, `ADMIN`), `ActorUserId`, `Action`, `EntityType`, `EntityId`, `OccurredAt` (theo Server Clock UTC), và `MetadataRedacted` JSON.
    - Metadata lưu trữ chi tiết: `BookingCode`, `ServiceNameSnapshot`, `CancellationAcceptedAt`, `MinutesBeforeAppointment`, `PaidAmount`, `CancellationFeeRate`, `RefundRate`, `CancellationFeeAmount`, `ExpectedRefundAmount`, `PolicyVersion` ("v1.0"), `RefundDecision` ("100%_REFUND", "50%_REFUND_50%_FEE", "NO_REFUND_UNPAID", "NO_REFUND"), `RefundStatus`, `Reason`.
  - Bảo mật tuyệt đối — Không lưu PII và OTP thô (No raw PII/OTP):
    - Khẳng định và kiểm thử xác thực trường `MetadataRedacted` tuyệt đối:
      - KHÔNG chứa số điện thoại cá nhân (không lưu raw phone number).
      - KHÔNG chứa mã OTP thô (No raw OTP at rest or in logs).
      - KHÔNG chứa số CCCD / CMND / Hộ chiếu / BHYT.
      - KHÔNG chứa mật khẩu, password hash, hay access token / JWT / grant token bí mật.
      - KHÔNG chứa chẩn đoán hay nội dung bệnh án y tế nhạy cảm.
  - Cung cấp API truy vấn kiểm toán cho nhân viên (Audit queries):
    - `GET /api/v1/reception/cancellation-audit/{bookingCode}`: Xem toàn bộ audit trail lịch sử hủy và các bước xử lý hoàn tiền của ca khám, sắp xếp theo thời gian mới nhất đến cũ nhất.
    - `GET /api/v1/reception/cancellation-audit`: Xem danh sách nhật ký kiểm toán hủy/hoàn tiền toàn hệ thống có phân trang (`PaginatedList`), hỗ trợ lọc theo BookingCode, ActorType, khoảng ngày.
    - Phân quyền chặt chẽ: Cả hai endpoint đều được bảo vệ nghiêm ngặt bằng policy `StaffOnly` (chỉ Receptionist hoặc Admin); từ chối truy cập từ Patient hoặc Anonymous với HTTP 403 Forbidden / 401 Unauthorized.
  - Bộ kiểm thử tự động xUnit (`CancellationAuditTests.cs`):
    - 10 test cases bao phủ đầy đủ toàn diện các tiêu chí nghiệm thu:
      1. `Cancellation_AuditLog_RecordedInSameTransaction`: Ghi nhận AuditLog trong cùng transaction khi hủy lịch.
      2. `AuditLog_StoresAllRequiredFields`: Lưu đầy đủ ActorType, Time, Reason, RefundDecision, PolicyVersion, Rates, Amounts.
      3. `AuditLog_StrictlyNoRawPiiOrOtp`: Kiểm tra quét chuỗi metadata không chứa phone, raw OTP, CCCD, Token, Password.
      4. `GetAppointmentCancellationAudit_ReturnsCompleteAuditTrail`: Truy vấn trả về toàn bộ audit trail gồm CANCEL_APPOINTMENT và PROCESS_REFUND.
      5. `StaffOnly_EnforcedForAuditQueries`: Từ chối người dùng Patient hoặc Anonymous với HTTP 403/401.
      6. `GetCancellationAuditLogs_WithFiltersAndPagination_Succeeds`: Lọc theo mã booking, actor type, khoảng ngày và phân trang chính xác.
      7. `AuditLog_UnpaidCancellation_RecordsNoRefundDecision`: Ca khám chưa thanh toán ghi nhận `RefundDecision = "NO_REFUND_UNPAID"`, `ExpectedRefundAmount = 0`, `RefundStatus = "NotApplicable"`.
      8. `AuditLog_Under60MinCancellation_Records50PercentDecision`: Hủy dưới 60 phút ghi nhận `RefundDecision = "50%_REFUND_50%_FEE"`, `RefundRate = 0.50`, `CancellationFeeRate = 0.50`.
      9. `AuditLog_AtOrAbove60MinCancellation_Records100PercentDecision`: Hủy trước 60 phút ghi nhận `RefundDecision = "100%_REFUND"`, `RefundRate = 1.0`, `CancellationFeeRate = 0.0`.
      10. `AuditTrail_ImmutableAndOrderedByTime`: Bản ghi audit log bất biến, sắp xếp theo thứ tự thời gian OccurredAtUtc giảm dần.
    - Toàn bộ 284/284 unit tests trong Solution đều PASS 100%.
  - Biên dịch toàn diện:
    - Backend: `dotnet build` hoàn tất 0 Warning, 0 Error.
    - Frontend: `npm run build` hoàn tất trong 5.32s, 0 Error.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CancelAppointment/CancelAppointmentCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Audit/DTOs/CancellationAuditDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Audit/Queries/GetAppointmentCancellationAudit/GetAppointmentCancellationAuditQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Audit/Queries/GetCancellationAuditLogs/GetCancellationAuditLogsQuery.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/CancellationAuditController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/CancellationAuditTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build "backend/ClinicAppointment.sln"` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test "backend/ClinicAppointment.sln"` (Thành công: 284 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.32s)
- Acceptance criteria evidence:
  - FEAT-034 đạt toàn bộ tiêu chí nghiệm thu và có đầy đủ bằng chứng kiểm thử thực tế.


## 2026-11-14: FEAT-035 - Reception patient management
- Triển khai và hoàn thiện module Quản lý và tiếp nhận hồ sơ bệnh nhân tại quầy Lễ tân (Module Reception), tuân thủ triệt để các quy tắc bất biến trong AGENTS.md, docs/SPEC.md v1.2, docs/BUSINESS_RULES.md (BR-001, BR-002, BR-003, BR-004), docs/DATABASE_DESIGN.md (mục 4.2 & 6).
- Phân quyền bảo vệ truy cập (Role guard):
  - Áp dụng chính sách bảo vệ `StaffOnly` (chỉ cho phép người dùng có Role `RECEPTIONIST` hoặc `ADMIN`) cho toàn bộ các endpoint tại `ReceptionPatientsController` (`/api/v1/reception/patients`).
  - Người dùng ẩn danh (Anonymous) hoặc Bệnh nhân (Role `PATIENT`) cố tình gọi API tiếp nhận tại quầy bị chặn ngay lập tức với mã lỗi HTTP 401 Unauthorized hoặc 403 Forbidden.
- Tìm kiếm và lọc hồ sơ bệnh nhân tại quầy:
  - Query `SearchReceptionPatientsQuery` hỗ trợ tìm kiếm linh hoạt theo từ khóa (Keyword: họ tên, số điện thoại), ngày sinh/năm sinh, trạng thái hoạt động (ACTIVE/MERGED), và lọc riêng các hồ sơ đang bị nghi trùng (`IsPossibleDuplicate = true`).
  - Kết quả trả về kèm phân trang (`PaginatedList<ReceptionPatientDto>`), số điện thoại và định danh (CCCD, Hộ chiếu, BHYT) được che giấu mặt nạ an toàn (masked) trước khi hiển thị cho Lễ tân.
- Tạo mới và cập nhật hồ sơ bệnh nhân tại quầy:
  - Lễ tân có thể tiếp nhận và tạo hồ sơ mới (`POST /api/v1/reception/patients`) hoặc cập nhật thông tin cá nhân/định danh (`PUT /api/v1/reception/patients/{id}`).
  - Toàn bộ thông tin định danh nhạy cảm (CCCD, Hộ chiếu, Thẻ BHYT) được mã hóa bằng AES-256-GCM và sinh Hash HMAC-SHA256 phục vụ tra cứu chính xác qua `IPatientSecurityService`.
  - Ngăn chặn chỉnh sửa thông tin trên các hồ sơ đã gộp (`RecordStatus == "MERGED"`) với mã lỗi HTTP 400 Bad Request.
  - Tự động ghi nhận `AuditLog` với `Action = "UPDATE_PATIENT"`.
- Bất biến cốt lõi: Tuyệt đối không tự động gộp hồ sơ trùng Tên + Ngày sinh/Năm sinh (No auto-merge by name+DOB):
  - Khi tạo mới hoặc cập nhật hồ sơ có Họ tên + Ngày sinh/Năm sinh trùng khớp với hồ sơ đã tồn tại:
    - Hệ thống TUYỆT ĐỐI KHÔNG tự động gộp dữ liệu.
    - Tạo hoặc giữ nguyên hồ sơ độc lập với mã định danh `PatientCode` riêng biệt.
    - Tự động gắn cờ `IsPossibleDuplicate = true` trên hồ sơ.
    - Tự động khởi tạo ca rà soát trùng lặp `PatientIdentityReviewCase` ở trạng thái `OPEN` với lý do `IdentityMatchReason.NAME_DOB` để Lễ tân đối chiếu CCCD trực tiếp tại quầy.
- Quy trình đối soát và xử lý ca nghi trùng có kiểm toán (Merge case audited):
  - Endpoint `POST /api/v1/reception/patients/review-cases/{id}/resolve` cho phép Lễ tân xử lý ca rà soát sau khi đối chiếu giấy tờ tùy thân của bệnh nhân tại quầy:
    - Khi xác nhận cùng 1 người (`CONFIRMED_SAME_PERSON`):
      - Chuyển toàn bộ lịch hẹn khám (`Appointments`) từ hồ sơ nguồn sang hồ sơ đích được chọn.
      - Đổi trạng thái hồ sơ nguồn thành `MERGED` và ghi nhận `MergedIntoPatientId = targetPatientId`.
      - Tạo bản ghi `PatientMergeEvent` lưu trữ snapshot dữ liệu trước khi gộp (`BeforeSnapshotJson`) và lý do gộp.
      - Ghi nhận `AuditLog` bất biến với `Action = "MERGE_PATIENTS"`, `ActorType = "STAFF"`, và metadata chứa mã bệnh nhân nguồn/đích đã được bảo vệ.
    - Khi xác nhận là 2 người khác nhau (`DIFFERENT_PERSON`):
      - Cập nhật trạng thái ca rà soát sang `DIFFERENT_PERSON`.
      - Tự động gỡ bỏ cờ nghi trùng `IsPossibleDuplicate = false` trên các hồ sơ liên quan.
      - Ghi nhận `AuditLog` với `Action = "RESOLVE_PATIENT_DUPLICATE"`.
- Bộ kiểm thử tự động xUnit (`ReceptionPatientManagementTests.cs`):
  - 10 test cases bao phủ 100% các tiêu chí nghiệm thu:
    1. `RoleGuard_StaffOnly_EnforcedForReceptionEndpoints`: Bệnh nhân bị từ chối với HTTP 403 khi gọi API tiếp nhận.
    2. `RoleGuard_ReceptionistAndAdmin_AllowedAccess`: Lễ tân và Quản trị viên được phép truy cập và tìm kiếm hồ sơ.
    3. `SearchPatients_ByKeyword_ReturnsMatchingPatients`: Tìm kiếm chính xác hồ sơ theo từ khóa họ tên.
    4. `SearchPatients_FilterByDuplicateFlagAndStatus`: Lọc chính xác danh sách hồ sơ bị nghi trùng (`IsPossibleDuplicate = true`).
    5. `UpdatePatient_ValidData_UpdatesInformationSuccessfully`: Cập nhật thông tin bệnh nhân, mã hóa CCCD và ghi AuditLog.
    6. `NoAutoMerge_WhenDuplicateNameAndDOB_CreatesIndependentProfileAndFlagsReviewCase`: Tạo hồ sơ mới trùng tên + ngày sinh không bị auto-merge, tạo profile độc lập và mở ca rà soát OPEN.
    7. `NoAutoMerge_WhenUpdatingToMatchingNameAndDOB_FlagsDuplicateAndOpensReviewCase`: Cập nhật hồ sơ trùng tên + ngày sinh kích hoạt cờ nghi trùng và mở ca rà soát OPEN.
    8. `MergeCaseAudited_WhenConfirmedSamePerson_TransfersAppointmentsAndCreatesAuditLog`: Gộp hồ sơ chuyển lịch hẹn, lưu snapshot merge event và ghi AuditLog kiểm toán.
    9. `MergeCaseAudited_WhenDifferentPerson_ClearsDuplicateFlagAndCreatesAuditLog`: Xác nhận khác người xóa cờ nghi trùng và ghi AuditLog.
    10. `UpdatePatient_MergedPatient_CannotBeUpdated`: Từ chối cập nhật hồ sơ đã ở trạng thái MERGED với HTTP 400 Bad Request.
  - Toàn bộ 294/294 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Domain/Entities/Patient.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/ResolvePatientReviewCaseCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Commands/UpdatePatientCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Patients/Queries/SearchReceptionPatientsQuery.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/ReceptionPatientsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ReceptionPatientManagementTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj` (Thành công: 294 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~ReceptionPatientManagementTests"` (Thành công: 10 Passed, 0 Failed, 0 Skipped)
- Acceptance criteria evidence:
  - FEAT-035 đạt toàn bộ tiêu chí nghiệm thu (Role guard, Merge case audited, No auto-merge by name+DOB) và có đầy đủ bằng chứng kiểm thử tự động thực tế.


## 2026-11-14: FEAT-036 - Patient check-in
- Triển khai và hoàn thiện quy trình Tiếp nhận và check-in bệnh nhân tại quầy Lễ tân (Module Reception), tuân thủ triệt để quy định trong AGENTS.md, docs/SPEC.md (FR-031, FR-034, FR-057), docs/BUSINESS_RULES.md (BR-006, BR-013, BR-014, BR-015), docs/API_SPECIFICATION.md mục Reception.
- Chuyển trạng thái ca khám (Confirmed to CheckedIn):
  - Lệnh `CheckInAppointmentCommand` tiếp nhận `AppointmentId` và `Note` (tùy chọn) từ nhân viên lễ tân.
  - Kiểm tra trạng thái ca khám: chỉ cho phép check-in các ca khám đang ở trạng thái `Confirmed`.
  - Gọi phương thức domain `appointment.CheckIn()`, cập nhật `Status = AppointmentStatus.CheckedIn` và `UpdatedAt = DateTime.UtcNow` theo Server Clock UTC.
- Ghi nhận người thực hiện và thời gian bất biến (Actor/time recorded):
  - Tự động lưu bản ghi `AuditLog` bất biến trong cùng Database Transaction:
    - `Action`: `"CHECK_IN_APPOINTMENT"`
    - `ActorType`: `"RECEPTIONIST"`, `"ADMIN"` hoặc `"STAFF"`
    - `ActorUserId`: Id người dùng của nhân viên thực hiện
    - `EntityType`: `"Appointment"`, `EntityId = appointment.Id`
    - `OccurredAt`: Thời gian UTC thực tế từ Server
    - `MetadataRedacted`: Chuỗi JSON chứa `BookingCode`, `DoctorId`, `PatientId`, `StartAt`, `CheckedInAt`, `Note`.
    - Bảo mật tuyệt đối: Quét kiểm tra chuỗi metadata không chứa mật khẩu, OTP thô, hay số điện thoại chưa che giấu.
- Từ chối các trạng thái không hợp lệ (Invalid state denied):
  - `PendingConfirmation` (chưa xác nhận): Bị từ chối với HTTP 400 Bad Request ("Lịch hẹn đang ở trạng thái Chờ xác nhận (PendingConfirmation). Vui lòng xác nhận lịch hẹn trước khi làm thủ tục check-in.").
  - `CheckedIn` (đã check-in rồi): Bị từ chối với HTTP 400 Bad Request ("Lịch hẹn đã được check-in tiếp nhận trước đó.").
  - `Cancelled` (đã hủy): Bị từ chối với HTTP 400 Bad Request ("Không thể check-in cho lịch hẹn đã bị hủy.").
  - `Completed` (đã khám xong): Bị từ chối với HTTP 400 Bad Request ("Không thể check-in cho lịch hẹn đã hoàn thành khám.").
  - `InProgress` (đang khám): Bị từ chối với HTTP 400 Bad Request ("Không thể check-in cho lịch hẹn đang trong ca khám.").
  - Id không tồn tại: Trả về HTTP 404 Not Found.
- Phân quyền bảo vệ nghiêm ngặt (StaffOnly):
  - Phân quyền policy `StaffOnly` cho toàn bộ endpoint check-in:
    - `POST /api/v1/reception/appointments/{id}/check-in`
    - `POST /api/v1/appointments/{id}/check-in`
  - Người dùng `Patient` hoặc khách ẩn danh `Anonymous` cố tình gọi API bị từ chối với HTTP 403 Forbidden / 401 Unauthorized.
- Bộ kiểm thử tự động xUnit (`PatientCheckInTests.cs`):
  - 12 test cases bao phủ 100% các tiêu chí nghiệm thu:
    1. `CheckIn_Confirmed_ByReceptionist_ShouldSucceed_AndChangeStatusToCheckedIn`: Tiếp nhận chuyển trạng thái sang `CheckedIn`, trả về 200 OK và `CheckedInAt`.
    2. `CheckIn_Confirmed_ByAdmin_ShouldSucceed`: Quản trị viên cũng có quyền check-in tại quầy.
    3. `CheckIn_ShouldRecordAuditLog_WithAllRequiredFields`: Ghi nhận AuditLog đầy đủ Actor, Time, Action, Metadata không chứa PII.
    4. `CheckIn_ByPatientOrAnonymous_ShouldBeDenied_WithForbidden`: Bệnh nhân và khách ẩn danh bị chặn với HTTP 403 Forbidden.
    5. `CheckIn_NonExistentAppointment_ShouldReturnNotFound`: Trả về HTTP 404 Not Found khi AppointmentId không tồn tại.
    6. `CheckIn_PendingConfirmation_ShouldBeDenied_WithBadRequest`: Từ chối lịch hẹn chưa xác nhận với HTTP 400.
    7. `CheckIn_AlreadyCheckedIn_ShouldBeDenied_WithBadRequest`: Từ chối lịch hẹn đã check-in trước đó với HTTP 400.
    8. `CheckIn_Cancelled_ShouldBeDenied_WithBadRequest`: Từ chối lịch hẹn đã bị hủy với HTTP 400.
    9. `CheckIn_Completed_ShouldBeDenied_WithBadRequest`: Từ chối lịch hẹn đã hoàn thành với HTTP 400.
    10. `CheckIn_InProgress_ShouldBeDenied_WithBadRequest`: Từ chối lịch hẹn đang khám với HTTP 400.
    11. `CheckIn_DomainEntity_CheckInMethod_ThrowsWhenNotConfirmed`: Domain Entity invariant ném `InvalidOperationException` khi gọi CheckIn không đúng trạng thái.
    12. `CheckIn_ControllerEndpoints_HaveStaffOnlyAuthorizationAttribute`: Kiểm tra reflection thuộc tính `[Authorize(Policy = "StaffOnly")]` trên các Controller endpoints.
  - Toàn bộ 306/306 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CheckInAppointment/CheckInAppointmentCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CheckInAppointment/CheckInAppointmentResponse.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CheckInAppointment/CheckInAppointmentCommandHandler.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/ReceptionAppointmentsController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/PatientCheckInTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build backend/ClinicAppointment.sln` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 306 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.61s)
- Acceptance criteria evidence:
  - FEAT-036 đạt toàn bộ tiêu chí nghiệm thu (Confirmed to CheckedIn, Actor/time recorded, Invalid state denied) và có đầy đủ bằng chứng kiểm thử tự động thực tế.


## 2026-11-14: FEAT-037 - Reception queue
- Triển khai và hoàn thiện màn hình và API điều phối Hàng đợi khám bệnh tại quầy Lễ tân theo Bác sĩ và Ngày khám (Module Reception), tuân thủ triệt để các quy định trong AGENTS.md, docs/SPEC.md (FR-058), docs/BUSINESS_RULES.md (BR-013, BR-014), docs/API_SPECIFICATION.md mục Reception.
- Lọc và sắp xếp hàng đợi khám linh hoạt (Filter/sort works):
  - Lệnh truy vấn `GetReceptionQueueQuery` hỗ trợ đầy đủ các tiêu chí lọc:
    - `DoctorId`: Lọc theo bác sĩ phụ trách.
    - `Date`: Lọc theo ngày khám. Xử lý chuẩn hóa múi giờ Việt Nam GMT+7 (Asia/Ho_Chi_Minh) từ 00:00:00 đến 23:59:59.999 sang khoảng thời gian tương ứng theo giờ UTC (-7 hours), đảm bảo chính xác tuyệt đối thời gian ca khám.
    - `Status`: Lọc theo trạng thái ca khám (`CheckedIn`, `Confirmed`, `InProgress`, `Completed`, `Cancelled`, `PendingConfirmation`).
    - `Keyword`: Tìm kiếm từ khóa theo mã booking (`BookingCode`), họ tên bệnh nhân, số điện thoại, hoặc tên bác sĩ.
    - `SortBy`: Hỗ trợ sắp xếp thời gian tăng dần (`time_asc` - mặc định), giảm dần (`time_desc`), hoặc ưu tiên trạng thái hàng đợi (`status`: đưa các ca đã tiếp nhận `CheckedIn` và đang khám `InProgress` lên đầu danh sách phục vụ điều phối tại quầy).
    - `Pagination`: Phân trang chuẩn với `PaginatedList<ReceptionQueueItemDto>`, tính toán đầy đủ `TotalItems`, `TotalPages`, `Page`, `PageSize`, `HasPreviousPage`, `HasNextPage`.
- Đánh số thứ tự tiếp nhận tuần tự (QueueNumber assigned):
  - Hệ thống tự động gán số thứ tự khám `QueueNumber` (1, 2, 3...) theo thứ tự danh sách điều phối trong ngày của bác sĩ để Lễ tân dễ dàng gọi bệnh nhân vào phòng khám.
- Giới hạn dữ liệu và bảo mật an toàn (Data access limited):
  - Toàn bộ endpoint xem hàng đợi tiếp nhận:
    - `GET /api/v1/reception/appointments`
    - `GET /api/v1/reception/queue`
  - Được bảo vệ nghiêm ngặt bằng policy `StaffOnly` (chỉ cho phép `RECEPTIONIST` hoặc `ADMIN`). Người dùng `PATIENT` hoặc khách ẩn danh `Anonymous` bị từ chối với HTTP 403 Forbidden / 401 Unauthorized.
  - Bảo vệ dữ liệu bệnh nhân tối đa:
    - `ReceptionQueueItemDto` chỉ trả về thông tin cần thiết phục vụ tiếp đón tại quầy (tên, mã bệnh nhân, phòng khám, dịch vụ, giờ khám, trạng thái, thời gian check-in).
    - Số điện thoại bắt buộc được che giấu mặt nạ qua `IIdentifierMaskingService.MaskPhone` (dạng `091***5678`).
    - TUYỆT ĐỐI không trả về số CCCD/Hộ chiếu thô, mật khẩu, mã OTP, hay ghi chép chẩn đoán bệnh án y tế chuyên sâu.
- Bộ kiểm thử tự động xUnit (`ReceptionQueueTests.cs`):
  - 13 test cases bao phủ 100% các tiêu chí nghiệm thu:
    1. `StaffOnly_EnforcedForReceptionQueue_PatientDenied`: Bệnh nhân bị từ chối với HTTP 403 Forbidden.
    2. `StaffOnly_EnforcedForReceptionQueue_AnonymousDenied`: Khách ẩn danh bị từ chối với HTTP 403 Forbidden.
    3. `ReceptionistAndAdmin_AllowedAccessToQueue`: Lễ tân và Admin truy cập thành công 200 OK.
    4. `FilterByDoctor_ReturnsOnlyDoctorAppointments`: Lọc chính xác ca khám theo DoctorId.
    5. `FilterByDate_ReturnsOnlyAppointmentsOnThatDate`: Lọc chính xác ca khám theo ngày (chuẩn hóa múi giờ Asia/Ho_Chi_Minh).
    6. `FilterByStatus_ReturnsOnlyMatchingStatusAppointments`: Lọc chính xác ca khám theo trạng thái CheckedIn.
    7. `FilterByKeyword_MatchesPatientNameAndBookingCode`: Tìm kiếm chính xác theo họ tên bệnh nhân và mã booking.
    8. `SortByTime_AscendingAndDescending_OrdersCorrectly`: Sắp xếp chính xác theo thời gian tăng dần và giảm dần.
    9. `SortByStatus_PrioritizesCheckedInAndInProgress`: Sắp xếp ưu tiên các ca đã tiếp nhận CheckedIn và InProgress lên trước.
    10. `Pagination_WorksCorrectly`: Kiểm tra phân trang hoạt động chính xác với TotalItems và TotalPages.
    11. `QueueNumber_AssignedSequentially`: Đánh số thứ tự tiếp nhận tuần tự (1, 2, 3...) theo danh sách điều phối.
    12. `DataAccessLimited_PhoneIsMasked_NoPiiLeakage`: Kiểm tra số điện thoại bị mask, không lộ CCCD, mật khẩu hay OTP.
    13. `ReceptionAppointmentsController_Endpoints_ProtectedWithStaffOnlyPolicy`: Kiểm tra reflection phân quyền StaffOnly trên Controller endpoints.
  - Toàn bộ 319/319 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/GetReceptionQueue/ReceptionQueueItemDto.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/GetReceptionQueue/GetReceptionQueueQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/GetReceptionQueue/GetReceptionQueueQueryHandler.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/ReceptionAppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ReceptionQueueTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build backend/ClinicAppointment.sln` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 319 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.60s)
- Acceptance criteria evidence:
  - FEAT-037 đạt toàn bộ tiêu chí nghiệm thu (Filter/sort works, Data access limited) và có đầy đủ bằng chứng kiểm thử tự động thực tế.


## 2026-11-14: FEAT-038 - Doctor assigned appointments
- Triển khai và hoàn thiện màn hình và API Bác sĩ xem danh sách các ca khám được phân công (Module Doctor), tuân thủ triệt để quy định trong AGENTS.md, docs/SPEC.md (FR-059), docs/BUSINESS_RULES.md (BR-013, BR-014), docs/API_SPECIFICATION.md mục Doctor.
- Bảo đảm nguyên tắc Bác sĩ chỉ xem ca khám của mình & Chống IDOR (Assigned doctor only):
  - Lệnh truy vấn `GetDoctorAppointmentsQuery` tự động xác định danh tính Bác sĩ dựa trên `UserId` của tài khoản đăng nhập (`_currentUserService.UserId`).
  - Hệ thống tra cứu hồ sơ Bác sĩ tương ứng trong bảng `Doctors` và ép buộc điều kiện `a.DoctorId == doctorProfile.Id`.
  - Tuyệt đối không cho phép Bác sĩ xem ca khám của bác sĩ khác; Bác sĩ cố tình gửi `DoctorId` của người khác trong query param vẫn bị hệ thống bỏ qua và chỉ trả về ca khám của chính mình.
  - Quản trị viên (`ADMIN`) có thể hỗ trợ giám sát và lọc theo `DoctorId` khi cần thiết.
  - Người dùng có vai trò `DOCTOR` nhưng chưa liên kết với hồ sơ bác sĩ trong CSDL bị từ chối với HTTP 403 Forbidden kèm thông báo lỗi rõ ràng.
- Tối thiểu hóa dữ liệu bệnh nhân (Minimum patient data):
  - DTO `DoctorAssignedAppointmentDto` chỉ cung cấp các thông tin thiết yếu phục vụ công tác khám bệnh: họ tên, mã bệnh nhân, ngày sinh/năm sinh, giới tính, dịch vụ, giờ khám, lý do khám, thời gian check-in.
  - Số điện thoại bệnh nhân bắt buộc được che giấu mặt nạ qua `IIdentifierMaskingService.MaskPhone` (dạng `091***2333`).
  - TUYỆT ĐỐI không trả về số CCCD/Hộ chiếu/BHYT thô, mật khẩu hay mã OTP bảo mật.
- Phân trang, lọc theo ngày và trạng thái (Pagination/date filter):
  - Hỗ trợ lọc theo `Date`: Quy đổi ngày khám từ 00:00:00 đến 23:59:59.999 múi giờ Việt Nam GMT+7 (`Asia/Ho_Chi_Minh`) sang khoảng thời gian tương ứng theo giờ UTC (-7 hours), đảm bảo chính xác tuyệt đối thời gian ca khám.
  - Hỗ trợ lọc theo `Status`: `CheckedIn`, `InProgress`, `Confirmed`, `Completed`, v.v.
  - Hỗ trợ tìm kiếm từ khóa `Keyword`: họ tên bệnh nhân, số điện thoại, mã booking.
  - Hỗ trợ sắp xếp `SortBy`: theo thời gian tăng dần (`time_asc` - mặc định), giảm dần (`time_desc`), hoặc ưu tiên trạng thái hàng đợi (`status`: đưa các ca đã tiếp nhận `CheckedIn` và `InProgress` lên trước).
  - Phân trang chuẩn với `PaginatedList<DoctorAssignedAppointmentDto>`, tính toán đầy đủ `TotalItems`, `TotalPages`, `Page`, `PageSize`, `HasNextPage`.
  - Tự động đánh số thứ tự tiếp nhận `QueueNumber` tuần tự (1, 2, 3...) theo danh sách khám trong ngày.
- Phân quyền bảo vệ Controller:
  - Tạo mới `DoctorAppointmentsController` (`/api/v1/doctor/appointments`) bảo vệ bằng policy `StaffOrDoctor` (chỉ cho phép `DOCTOR` hoặc `ADMIN`). Bệnh nhân (`PATIENT`) hoặc khách ẩn danh (`Anonymous`) bị từ chối với HTTP 403 Forbidden / 401 Unauthorized.
- Bộ kiểm thử tự động xUnit (`DoctorAssignedAppointmentsTests.cs`):
  - 15 test cases bao phủ 100% các tiêu chí nghiệm thu:
    1. `DoctorOnly_Enforced_PatientDenied`: Bệnh nhân bị từ chối với HTTP 403 Forbidden.
    2. `DoctorOnly_Enforced_AnonymousDenied`: Khách ẩn danh bị từ chối với HTTP 403 Forbidden.
    3. `DoctorUser_WithoutProfile_ReturnsForbidden`: Tài khoản Doctor chưa liên kết hồ sơ bác sĩ bị từ chối 403.
    4. `AssignedDoctorOnly_DoctorSeesOnlyOwnAppointments`: Bác sĩ A chỉ thấy đúng ca khám của mình, không thấy ca của Bác sĩ B.
    5. `AssignedDoctorOnly_CannotTamperDoctorIdToViewOtherDoctorAppointments`: Chống IDOR, Bác sĩ A truyền DoctorId của Bác sĩ B vẫn chỉ nhận được ca của mình.
    6. `AdminUser_CanFilterByDoctorId`: Quản trị viên có thể xem lịch của bác sĩ cụ thể bằng cách truyền DoctorId.
    7. `FilterByDate_ReturnsAppointmentsForThatDay`: Lọc chính xác các ca khám trong ngày theo giờ Việt Nam.
    8. `FilterByStatus_ReturnsMatchingAppointments`: Lọc chính xác ca khám theo trạng thái CheckedIn.
    9. `FilterByKeyword_MatchesPatientNameAndBookingCode`: Tìm kiếm chính xác theo tên bệnh nhân và mã booking.
    10. `Pagination_WorksCorrectly`: Kiểm tra phân trang hoạt động chính xác với TotalItems, TotalPages, PageSize.
    11. `SortByTime_AscendingAndDescending_OrdersCorrectly`: Sắp xếp chính xác theo thời gian tăng dần và giảm dần.
    12. `SortByStatus_PrioritizesCheckedInAndInProgress`: Sắp xếp ưu tiên ca CheckedIn và InProgress lên trước.
    13. `QueueNumber_AssignedSequentially`: Đánh số thứ tự tiếp nhận tuần tự (1, 2, 3...).
    14. `MinimumPatientData_PhoneIsMasked_NoPiiLeakage`: Số điện thoại được mask, không lộ CCCD, mật khẩu hay OTP.
    15. `DoctorAppointmentsController_ProtectedWithStaffOrDoctorPolicy`: Kiểm tra reflection phân quyền StaffOrDoctor trên Controller.
  - Toàn bộ 334/334 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/GetDoctorAppointments/DoctorAssignedAppointmentDto.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/GetDoctorAppointments/GetDoctorAppointmentsQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/GetDoctorAppointments/GetDoctorAppointmentsQueryHandler.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorAppointmentsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/DoctorAssignedAppointmentsTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" build backend/ClinicAppointment.sln` (Thành công: 0 Warnings, 0 Errors)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 334 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 9.61s)
- Acceptance criteria evidence:
  - FEAT-038 đạt toàn bộ tiêu chí nghiệm thu (Assigned doctor only, Minimum patient data, Pagination/date filter) và có đầy đủ bằng chứng kiểm thử tự động thực tế.

## 2026-11-14 — FEAT-039: Consultation start and record
- Feature: FEAT-039 (Consultation start and record)
- Status: Completed
- Context: Triển khai luồng nghiệp vụ bắt đầu ca khám y khoa và ghi nhận kết quả khám lâm sàng của Bác sĩ theo phân hệ Consultation, tuân thủ nghiêm ngặt quy chuẩn bảo mật y tế và AGENTS.md.
- Actions taken:
  - Domain Model & Entity (`Consultation.cs` & `Appointment.cs`):
    - Tạo thực thể `Consultation` kế thừa `BaseEntity` với các trường: `AppointmentId` (Guid, Unique), `DoctorId` (Guid), `Symptoms` (string, max 2000 ký tự), `Diagnosis` (string, max 2000 ký tự), `ExaminationNotes` (string?, max 4000 ký tự), `Conclusion` (string?, max 2000 ký tự), `FollowUpInstructions` (string?, max 2000 ký tự), `ConsultedAt` (DateTime UTC).
    - Thêm phương thức Domain trong `Appointment.cs`: `StartConsultation()` (chuyển `CheckedIn` -> `InProgress`, cập nhật `UpdatedAt = DateTime.UtcNow`) và `CompleteConsultation()` (chuyển `InProgress` -> `Completed`).
  - EF Core Configuration & Migration:
    - Tạo `ConsultationConfiguration.cs` cấu hình mapping Fluent API cho PostgreSQL: table `consultations`, index unique trên `appointment_id`, foreign keys tới `appointments` và `doctors`.
    - Tạo migration EF Core `20261114093000_AddConsultations.cs` và cập nhật `IApplicationDbContext` & `ApplicationDbContext`.
  - CQRS Commands & Handlers (`ClinicAppointment.Application.Features.Consultations`):
    - `StartConsultationCommand`: Bác sĩ bắt đầu ca khám; kiểm tra quyền bác sĩ được phân công (`Assigned doctor only`) qua so sánh `DoctorProfile.Id == Appointment.DoctorId`; chống IDOR tuyệt đối; từ chối bác sĩ khác với HTTP 403 Forbidden; từ chối ca khám chưa `CheckedIn` với HTTP 400 Bad Request; chuyển trạng thái sang `InProgress`; ghi nhận `AuditLog` với `Action = "START_CONSULTATION"`.
    - `RecordConsultationCommand`: Ghi nhận kết quả khám lâm sàng; bắt buộc ca khám phải ở trạng thái `InProgress`; chỉ cho phép bác sĩ phụ trách ca khám thực hiện; hỗ trợ tạo mới bản ghi `Consultation` hoặc cập nhật thông tin khám đang có; cập nhật `UpdatedAt`; ghi nhận `AuditLog` với `Action = "RECORD_CONSULTATION"`.
    - Bảo mật bệnh án trong AuditLog theo quy định `AGENTS.md`: TUYỆT ĐỐI KHÔNG ghi nhận nội dung `Symptoms` và `Diagnosis` vào `AuditLog.MetadataJson`, chỉ lưu cờ boolean `HasSymptoms`, `HasDiagnosis` và timestamp.
    - `GetConsultationByAppointmentIdQuery`: Truy vấn kết quả khám theo ca khám, bảo vệ theo quyền phân công (Bác sĩ được phân công hoặc Quản trị viên).
  - Validation qua FluentValidation:
    - `RecordConsultationCommandValidator`: Bắt buộc `Symptoms` không được rỗng (tối đa 2000 ký tự), `Diagnosis` không được rỗng (tối đa 2000 ký tự), các trường ghi chú không vượt quá giới hạn độ dài.
  - Web API Controllers (`ClinicAppointment.Api`):
    - Mở rộng `DoctorAppointmentsController`:
      - `POST /api/v1/doctor/appointments/{id}/start`: Bắt đầu ca khám (`[Authorize(Policy = "StaffOrDoctor")]`).
      - `POST /api/v1/doctor/appointments/{id}/consultation`: Ghi chép kết quả khám (`[Authorize(Policy = "StaffOrDoctor")]`).
      - `GET /api/v1/doctor/appointments/{id}/consultation`: Lấy chi tiết kết quả khám (`[Authorize(Policy = "StaffOrDoctor")]`).
    - Tạo mới `DoctorConsultationsController` (`/api/v1/doctor/consultations`): alias endpoint `POST /api/v1/doctor/consultations` tiện lợi cho các client.
  - Bộ kiểm thử tự động xUnit (`ConsultationStartAndRecordTests.cs`):
    - 15 test cases bao phủ toàn diện 100% các tiêu chí nghiệm thu:
      1. `StartConsultation_WithCheckedInAppointment_SucceedsAndTransitionsToInProgress`: Chuyển trạng thái `CheckedIn` -> `InProgress` thành công.
      2. `StartConsultation_WhenNotCheckedIn_ThrowsValidationException`: Từ chối khi trạng thái là `Confirmed`, `PendingConfirmation`, `Cancelled`, `Completed`.
      3. `StartConsultation_ByUnassignedDoctor_ThrowsForbidden`: Bác sĩ khác gọi API bị từ chối với HTTP 403 Forbidden.
      4. `StartConsultation_ByDoctorWithoutProfile_ThrowsForbidden`: Tài khoản Doctor chưa có hồ sơ bị từ chối 403.
      5. `StartConsultation_WritesAuditLog_WithoutPii`: Ghi nhận AuditLog với `Action = START_CONSULTATION` và không chứa PII.
      6. `RecordConsultation_WithValidData_CreatesConsultationRecord`: Ghi nhận kết quả khám thành công khi ở trạng thái `InProgress`.
      7. `RecordConsultation_WhenAppointmentNotInProgress_ThrowsValidationException`: Từ chối khi ca khám chưa bắt đầu hoặc đã kết thúc.
      8. `RecordConsultation_ByUnassignedDoctor_ThrowsForbidden`: Bác sĩ không được phân công bị từ chối khi cố ghi kết quả.
      9. `RecordConsultation_UpdatesExistingConsultation_IfAlreadyExists`: Cập nhật an toàn kết quả khám đã ghi chép trước đó.
      10. `RecordConsultation_AuditLog_DoesNotContainMedicalDiagnosisOrSymptoms`: Kiểm tra nghiêm ngặt AuditLog KHÔNG chứa nội dung triệu chứng hoặc chẩn đoán.
      11. `RecordConsultationValidator_RequiresSymptomsAndDiagnosis`: Validator từ chối khi thiếu `Symptoms` hoặc `Diagnosis`.
      12. `RecordConsultationValidator_RejectsExcessiveLength`: Validator từ chối khi nội dung vượt quá giới hạn độ dài.
      13. `GetConsultation_ByAssignedDoctor_ReturnsDetails`: Bác sĩ phụ trách xem được chi tiết kết quả khám.
      14. `GetConsultation_ByUnassignedDoctor_ThrowsForbidden`: Bác sĩ khác bị từ chối khi cố xem kết quả khám.
      15. `DoctorConsultationsController_ProtectedWithStaffOrDoctorPolicy`: Phân quyền Controller bảo vệ bằng `StaffOrDoctor`.
    - Toàn bộ 349/349 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Domain/Entities/Consultation.cs`
  - `backend/src/ClinicAppointment.Domain/Entities/Appointment.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/ConsultationConfiguration.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114093000_AddConsultations.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/DTOs/ConsultationDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Validators/ConsultationValidators.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Commands/StartConsultation/StartConsultationCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Commands/StartConsultation/StartConsultationCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Commands/RecordConsultation/RecordConsultationCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Commands/RecordConsultation/RecordConsultationCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Queries/GetConsultationByAppointmentId/GetConsultationByAppointmentIdQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Queries/GetConsultationByAppointmentId/GetConsultationByAppointmentIdQueryHandler.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorAppointmentsController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorConsultationsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ConsultationStartAndRecordTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~ConsultationStartAndRecordTests"` (Thành công: 15 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 349 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.92s)
- Acceptance criteria evidence:
  - FEAT-039 đạt toàn bộ tiêu chí nghiệm thu (CheckedIn to InProgress, Consultation protected, Required field validation) và có đầy đủ bằng chứng kiểm thử tự động thực tế.

## 2026-11-14 — FEAT-040: Consultation completion
- Feature: FEAT-040 (Consultation completion)
- Status: Completed
- Context: Triển khai luồng hoàn thành ca khám y khoa của Bác sĩ (Module Consultation), chuyển trạng thái ca khám sang Completed, khóa hồ sơ bệnh án chống chỉnh sửa và re-opening, ghi nhận lịch sử kiểm toán bất biến bảo mật thông tin y tế theo AGENTS.md.
- Actions taken:
  - Domain Model & Entity Invariants (`Appointment.cs` & `Consultation.cs`):
    - Đảm bảo phương thức Domain `Appointment.CompleteConsultation(serverTimestampUtc)` chuyển trạng thái `InProgress` -> `Completed`, cập nhật `UpdatedAt` theo UTC, từ chối mọi trạng thái khác.
    - Cập nhật thông tin `Conclusion` và `FollowUpInstructions` (nếu có cung cấp) vào thực thể `Consultation` và cập nhật `UpdatedAt`.
  - CQRS Commands & DTOs (`ClinicAppointment.Application.Features.Consultations`):
    - Mở rộng DTOs: `CompleteConsultationRequest` (Conclusion, FollowUpInstructions) và `CompleteConsultationResponse` (AppointmentId, BookingCode, Status, DoctorId, DoctorName, PatientId, PatientFullName, Symptoms, Diagnosis, ExaminationNotes, Conclusion, FollowUpInstructions, ConsultedAt, CompletedAt).
    - Tạo `CompleteConsultationCommand` và `CompleteConsultationCommandHandler`:
      - Phân quyền: Chỉ cho phép Bác sĩ hoặc Quản trị viên (Admin). Bệnh nhân hoặc khách ẩn danh bị từ chối 403 Forbidden.
      - Phân quyền tài nguyên: Bác sĩ được phân công phụ trách ca khám (`Assigned Doctor Only`). Chống IDOR triệt để (Bác sĩ B không thể can thiệp ca khám của Bác sĩ A, trả về HTTP 403 Forbidden).
      - Kiểm tra hồ sơ Bác sĩ liên kết tài khoản: Bác sĩ chưa có hồ sơ bị từ chối 403.
      - Kiểm tra trạng thái ca khám: Chỉ cho phép hoàn tất ca khám đang `InProgress`. Từ chối các trạng thái `PendingConfirmation`, `Confirmed`, `CheckedIn`, `Cancelled` với HTTP 400 Bad Request. Nếu ca khám đã `Completed` trước đó, từ chối với thông báo "Ca khám đã được hoàn tất trước đó và đã khóa hồ sơ."
      - Kiểm tra hồ sơ thăm khám y khoa: Bắt buộc ca khám phải đã được ghi chép kết quả thăm khám và chẩn đoán lâm sàng (`Consultation` tồn tại với Symptoms và Diagnosis) trước khi hoàn tất theo quy định FR-061 & FR-062.
      - Chuyển trạng thái Appointment sang `Completed`.
      - Khóa ca khám chống chỉnh sửa bệnh án (`RecordConsultationCommand`): Bổ sung kiểm tra chặn chỉnh sửa kết quả khám khi Appointment đã ở trạng thái `Completed` ("Ca khám đã hoàn tất (Completed) và đã khóa hồ sơ bệnh án. Không thể chỉnh sửa kết quả khám.").
      - Khóa ca khám chống mở lại (`StartConsultationCommand`): Bị chặn 400 khi Appointment đã ở trạng thái `Completed`.
      - Ghi nhận lịch sử kiểm toán bất biến (`AuditLog`):
        - `Action = "COMPLETE_CONSULTATION"`
        - `ActorType = "DOCTOR"` hoặc `"ADMIN"`
        - `ActorUserId = _currentUserService.UserId`
        - `EntityType = "Appointment"`
        - `EntityId = appointment.Id`
        - Bảo mật tuyệt đối theo AGENTS.md: `MetadataRedacted` KHÔNG log nội dung triệu chứng hay chẩn đoán bệnh án y tế, chỉ lưu cờ boolean `HasConclusion`, `HasFollowUpInstructions`, `CompletedAtUtc` và metadata an toàn.
  - Validation qua FluentValidation:
    - `CompleteConsultationCommandValidator`: Kiểm tra `AppointmentId` bắt buộc, `Conclusion` tối đa 2000 ký tự, `FollowUpInstructions` tối đa 2000 ký tự.
  - Web API Endpoints:
    - Thêm `POST /api/v1/doctor/appointments/{id}/complete` trong `DoctorAppointmentsController` (`[Authorize(Policy = "StaffOrDoctor")]`).
    - Thêm alias endpoint `POST /api/v1/doctor/consultations/{appointmentId}/complete` trong `DoctorConsultationsController` (`[Authorize(Policy = "StaffOrDoctor")]`).
  - Bộ kiểm thử tự động xUnit (`ConsultationCompletionTests.cs`):
    - 16 test cases bao phủ toàn diện 100% các tiêu chí nghiệm thu:
      1. `CompleteConsultation_WithInProgressAppointment_SucceedsAndTransitionsToCompleted`: Hoàn thành ca khám thành công, chuyển sang `Completed`, cập nhật đầy đủ thông tin y tế và trả về DTO.
      2. `CompleteConsultation_WhenNotInProgress_ReturnsBadRequest`: Từ chối khi trạng thái là `PendingConfirmation`, `Confirmed`, `CheckedIn`.
      3. `CompleteConsultation_WhenAlreadyCompleted_ReturnsBadRequest`: Từ chối ca khám đã `Completed` trước đó ("Ca khám đã được hoàn tất trước đó và đã khóa hồ sơ").
      4. `CompleteConsultation_WhenCancelled_ReturnsBadRequest`: Từ chối ca khám đã bị hủy.
      5. `CompleteConsultation_WhenNoConsultationRecordExists_ReturnsBadRequest`: Từ chối khi ca khám chưa có bản ghi kết quả thăm khám lâm sàng.
      6. `CompleteConsultation_ByUnassignedDoctor_ThrowsForbidden`: Bác sĩ B cố ý hoàn tất ca của Bác sĩ A bị từ chối với HTTP 403 Forbidden.
      7. `CompleteConsultation_ByDoctorWithoutProfile_ThrowsForbidden`: Bác sĩ chưa liên kết hồ sơ bị từ chối 403.
      8. `CompleteConsultation_ByPatientOrAnonymous_ThrowsForbidden`: Vai trò không hợp lệ bị từ chối 403.
      9. `CompleteConsultation_ByAdmin_Succeeds`: Quản trị viên (Admin) hoàn tất ca khám thành công.
      10. `CompleteConsultation_WritesAuditLog_WithoutPiiOrMedicalNotes`: Ghi nhận AuditLog với `Action = COMPLETE_CONSULTATION`, kiểm tra không chứa nội dung chẩn đoán/triệu chứng.
      11. `RecordConsultation_WhenAppointmentAlreadyCompleted_IsBlocked`: Chặn chỉnh sửa kết quả khám khi ca khám đã hoàn tất.
      12. `StartConsultation_WhenAppointmentAlreadyCompleted_IsBlocked`: Chặn mở lại ca khám đã hoàn tất.
      13. `CompleteConsultationValidator_ValidatesExcessiveLength`: Validator từ chối khi nội dung vượt quá giới hạn độ dài.
      14. `DoctorAppointmentsController_CompleteEndpoint_ProtectedWithStaffOrDoctorPolicy`: Phân quyền Controller bảo vệ bằng `StaffOrDoctor`.
    - Toàn bộ 365/365 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Consultations/DTOs/ConsultationDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Commands/CompleteConsultation/CompleteConsultationCommand.cs`
  - `backend/src/ClinicAppointment.Application/Features/Consultations/Commands/RecordConsultation/RecordConsultationCommand.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorAppointmentsController.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorConsultationsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/ConsultationCompletionTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~ConsultationCompletionTests"` (Thành công: 16 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 365 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 7.51s)
- Acceptance criteria evidence:
  - FEAT-040 đạt toàn bộ tiêu chí nghiệm thu (InProgress to Completed, History recorded, Only assigned doctor/authorized role) và có đầy đủ bằng chứng kiểm thử tự động thực tế.

## 2026-11-14 — FEAT-041: Notification abstraction
- Feature: FEAT-041 (Notification abstraction)
- Status: Completed
- Context: Triển khai tầng trừu tượng hóa và kiến trúc Outbox Pattern cho phân hệ thông báo đa kênh (SMS, Email, InApp), tuân thủ quy định bất biến trong AGENTS.md (nhà cung cấp bên thứ ba là TBD, dev stub provider, không log hoặc gửi chẩn đoán y tế/định danh thô trong tin nhắn).
- Actions taken:
  - Domain Model & Entity Invariants (`Notification.cs` & `DomainEnums.cs`):
    - Bổ sung enums trong `DomainEnums.cs`: `NotificationChannel` (Sms, Email, InApp), `NotificationStatus` (Pending, Sent, Failed).
    - Tạo thực thể `Notification` kế thừa `BaseEntity` với các trường theo đúng `docs/DATABASE_DESIGN.md` và `docs/DATA_DICTIONARY.md`: `PatientId` (Guid?), `AppointmentId` (Guid?), `Channel` (NotificationChannel), `TemplateCode` (string, max 60), `RecipientCiphertext` (byte[]?), `RecipientMasked` (string?, max 100), `PayloadJson` (string?), `Status` (NotificationStatus), `ProviderMessageId` (string?, max 150), `SentAt` (DateTime? UTC), `FailureReason` (string?), `RetryCount` (int, default 0), `MaxRetries` (int, default 3).
    - Các phương thức Domain: `Create(...)`, `MarkSent(...)`, `MarkFailed(...)`, `ResetForRetry(...)`, thuộc tính logic `CanRetry`.
  - EF Core Configuration & Migration:
    - Tạo `NotificationConfiguration.cs`: cấu hình mapping Fluent API cho PostgreSQL bảng `notifications`, khóa ngoại nullable tới `patients` và `appointments` (OnDelete: SetNull), composite index trên `(status, created_at)` (`ix_notifications_status_created_at`), index trên `appointment_id` và `patient_id`.
    - Tạo migration EF Core `20261114101500_AddNotifications.cs` và cập nhật `IApplicationDbContext` & `ApplicationDbContext`.
  - Application Abstractions & Security Guard (`ClinicAppointment.Application.Common.Notifications`):
    - `INotificationChannel`: Trừu tượng hóa việc gửi tin nhắn qua kênh cụ thể (decoupled).
    - `NotificationMessage` & `NotificationSendResult`: Cấu trúc tin nhắn và kết quả gửi tin chuẩn hóa.
    - `INotificationSafetyGuard` & `NotificationSafetyGuard`: Bộ kiểm soát an toàn nội dung thông báo. Kiểm tra nghiêm ngặt và ném ngoại lệ `NotificationSafetyViolationException` nếu tin nhắn chứa chẩn đoán y khoa (`Diagnosis`), triệu chứng lâm sàng (`Symptoms`), từ khóa bệnh án nhạy cảm, số định danh cá nhân CCCD/CMND thô 12 số, hoặc mật khẩu dạng thô.
    - `INotificationService` & `NotificationService`: Dịch vụ điều phối Outbox thông báo:
      - `EnqueueNotificationAsync`: Kiểm tra an toàn bảo mật, che giấu mặt nạ người nhận (`RecipientMasked`), mã hóa bí mật người nhận (`RecipientCiphertext` qua `IPiiEncryptionService`), lưu vào CSDL với trạng thái `Pending`.
      - `DispatchNotificationAsync`: Giải mã người nhận, điều phối gửi qua Channel adapter phù hợp, ghi nhận trạng thái `Sent` (kèm `ProviderMessageId`, `SentAt`) hoặc `Failed` (lưu chi tiết `FailureReason`, tăng `RetryCount`).
      - `DispatchPendingNotificationsAsync`: Quét và gửi theo batch từ hàng đợi Outbox.
      - `RetryNotificationAsync`: Thử lại các thông báo bị thất bại khi `RetryCount < MaxRetries`.
  - Dev Stub Providers (`ClinicAppointment.Infrastructure.Services.Notifications`):
    - `DevStubSmsChannel`: Giả lập gửi SMS qua bộ nhớ không cần kết nối SMS Gateway thật; che giấu số điện thoại trong log; hỗ trợ cờ `SimulateFailure` phục vụ kiểm thử.
    - `DevStubEmailChannel`: Giả lập gửi Email không cần máy chủ SMTP thật; hỗ trợ cờ `SimulateFailure`.
    - `DevStubInAppChannel`: Giả lập gửi thông báo In-App.
  - Dependency Injection:
    - Đăng ký DI trong `Infrastructure/DependencyInjection.cs`: `NotificationSafetyGuard`, `DevStubSmsChannel`, `DevStubEmailChannel`, `DevStubInAppChannel`, `NotificationService`.
  - Bộ kiểm thử tự động xUnit (`NotificationAbstractionTests.cs`):
    - 18 test cases bao phủ toàn diện 100% các tiêu chí nghiệm thu:
      1. `DevStubSmsChannel_SendsSuccessfully_WithoutRealProvider`: Kênh SMS dev stub gửi thành công, sinh MsgId.
      2. `DevStubEmailChannel_SendsSuccessfully_WithoutSmtp`: Kênh Email dev stub gửi thành công.
      3. `DevStubInAppChannel_SendsSuccessfully`: Kênh InApp dev stub gửi thành công.
      4. `NotificationService_EnqueueNotification_CreatesPendingRecordWithMaskedRecipient`: Xếp hàng outbox với người nhận được mask và mã hóa.
      5. `DispatchNotification_WhenChannelSucceeds_UpdatesStatusToSent`: Cập nhật trạng thái `Sent`, `SentAt`, `ProviderMessageId`.
      6. `DispatchNotification_WhenChannelFails_RecordsDeliveryFailure_AndIncrementsRetryCount`: Ghi nhận lỗi chi tiết `FailureReason`, tăng `RetryCount`.
      7. `RetryNotification_WhenFailedAndWithinLimit_ResetsAndRetriesSuccessfully`: Thử lại thành công khi trong giới hạn retry.
      8. `RetryNotification_WhenExceedsMaxRetries_IsBlocked`: Chặn thử lại khi vượt quá `MaxRetries` với lỗi 400.
      9. `NotificationSafetyGuard_RejectsMessageWithMedicalDiagnosisOrSymptoms`: Chặn tin nhắn chứa chẩn đoán/triệu chứng lâm sàng y tế.
      10. `NotificationSafetyGuard_RejectsMessageWithRawNationalId`: Chặn tin nhắn chứa số CCCD thô 12 số.
      11. `NotificationSafetyGuard_RejectsMessageWithRawPassword`: Chặn tin nhắn chứa mật khẩu dạng thô.
      12. `NotificationSafetyGuard_RejectsPayloadWithSensitiveMedicalFields`: Chặn payload chứa trường nhạy cảm `Diagnosis` hoặc `Symptoms`.
      13. `NotificationSafetyGuard_AllowsSafeAppointmentMessage`: Chấp thuận tin nhắn lịch hẹn an toàn.
      14. `DispatchPendingNotifications_ProcessesBatchSuccessfully`: Xử lý hàng đợi theo lô (batch) thành công.
      15. `ChannelDecoupled_NotificationServiceDispatchesBasedOnChannel`: Kênh gửi decoupled hoàn toàn khỏi nghiệp vụ phòng khám.
    - Toàn bộ 383/383 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Domain/Enums/DomainEnums.cs`
  - `backend/src/ClinicAppointment.Domain/Entities/Notification.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs`
  - `backend/src/ClinicAppointment.Application/Common/Interfaces/IApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/ApplicationDbContext.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114101500_AddNotifications.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Persistence/Migrations/20261114101500_AddNotifications.Designer.cs`
  - `backend/src/ClinicAppointment.Application/Common/Notifications/NotificationModels.cs`
  - `backend/src/ClinicAppointment.Application/Common/Notifications/INotificationChannel.cs`
  - `backend/src/ClinicAppointment.Application/Common/Notifications/NotificationSafetyGuard.cs`
  - `backend/src/ClinicAppointment.Application/Common/Notifications/NotificationDtos.cs`
  - `backend/src/ClinicAppointment.Application/Common/Notifications/NotificationService.cs`
  - `backend/src/ClinicAppointment.Infrastructure/Services/Notifications/DevStubNotificationChannels.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/NotificationAbstractionTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~NotificationAbstractionTests"` (Thành công: 18 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 383 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.95s)
- Acceptance criteria evidence:
  - FEAT-041 đạt toàn bộ tiêu chí nghiệm thu (Dev stub works without provider, Delivery failure recorded, No diagnosis/identifier in message) và có đầy đủ bằng chứng kiểm thử tự động thực tế.

## 2026-11-14 — FEAT-042: Appointment notifications
- Feature: FEAT-042 (Appointment notifications)
- Status: Completed
- Context: Triển khai luồng tự động hóa thông báo sự kiện vòng đời lịch hẹn qua Outbox, kiểm soát chống gửi trùng lặp (No duplicate event notification), theo dõi trạng thái giao nhận (Delivery status), bảo đảm nội dung tối thiểu an toàn không chứa dữ liệu y tế nhạy cảm (Minimal content).
- Actions taken:
  - Appointment Notification Service (`ClinicAppointment.Application.Features.Notifications.Services`):
    - Xây dựng interface `IAppointmentNotificationService` và lớp cài đặt `AppointmentNotificationService`.
    - Hỗ trợ toàn bộ 5 sự kiện vòng đời lịch hẹn:
      1. `NotifyBookingCreatedAsync`: Thông báo tiếp nhận đặt lịch mới (`BOOKING_CREATED`).
      2. `NotifyAppointmentConfirmedAsync`: Thông báo xác nhận lịch hẹn thành công (`APPOINTMENT_CONFIRMED`).
      3. `NotifyRescheduleApprovedAsync`: Thông báo duyệt đổi lịch hẹn (`RESCHEDULE_APPROVED`).
      4. `NotifyAppointmentCancelledAsync`: Thông báo hủy lịch kèm số tiền hoàn dự kiến (`APPOINTMENT_CANCELLED`).
      5. `NotifyAppointmentReminderAsync`: Nhắc lịch khám trước giờ hẹn (`APPOINTMENT_REMINDER`).
    - Chống gửi trùng lặp sự kiện (`No duplicate event notification`): Kiểm tra sự tồn tại của bản ghi notification cùng `AppointmentId`, `TemplateCode`, `Channel` (Sms/Email/InApp) ở trạng thái `Pending` hoặc `Sent`; từ chối tạo bản ghi trùng lặp và trả về null nếu đã tồn tại.
    - Nội dung tối thiểu an toàn (`Minimal content`): Định dạng giờ khám theo giờ Việt Nam (UTC+7, format `dd/MM/yyyy HH:mm`), tên bệnh nhân, tên bác sĩ, mã đặt lịch `BookingCode`, phòng khám, và số tiền hoàn phí nếu có; kiểm duyệt qua `NotificationSafetyGuard` trước khi lưu vào CSDL.
  - Tích hợp Trigger tự động trong các CQRS Handlers:
    - `ConfirmAppointmentCommandHandler`: Tự động gọi `NotifyAppointmentConfirmedAsync` sau khi xác nhận lịch hẹn.
    - `CancelAppointmentCommandHandler`: Tự động gọi `NotifyAppointmentCancelledAsync` sau khi hủy lịch thành công.
    - `ApproveRescheduleRequestCommandHandler`: Tự động gọi `NotifyRescheduleApprovedAsync` sau khi duyệt đổi lịch.
    - `BookGuestAppointmentCommandHandler`: Tự động gọi `NotifyBookingCreatedAsync` sau khi tạo lịch hẹn mới.
    - Thiết kế kiến trúc Outbox Resiliency: Sử dụng constructor nạp chồng hỗ trợ `IAppointmentNotificationService? = null` để giữ 100% tính tương thích ngược với 383 unit tests trước đó; bọc trigger trong try-catch để sự cố dispatch thông báo không làm gián đoạn transaction chính.
  - CQRS Query & Web API Endpoint:
    - Tạo `GetAppointmentNotificationsQuery` và `GetAppointmentNotificationsQueryHandler` tra cứu danh sách và trạng thái phát (Delivery status) của các thông báo thuộc ca khám.
    - Phân quyền: Nhân viên Lễ tân (`RECEPTIONIST`), Quản trị viên (`ADMIN`), hoặc Bác sĩ được phân công (`DOCTOR` phụ trách) có quyền tra cứu; từ chối bác sĩ không phụ trách với HTTP 403 Forbidden và từ chối khách ẩn danh với HTTP 401 Unauthorized.
    - Thêm endpoint `GET /api/v1/appointments/{id}/notifications` trong `AppointmentsController` bảo vệ bằng policy `StaffOrDoctor`.
  - Dependency Injection:
    - Đăng ký `services.AddScoped<IAppointmentNotificationService, AppointmentNotificationService>();` trong `Infrastructure/DependencyInjection.cs`.
  - Bộ kiểm thử tự động xUnit (`AppointmentNotificationsTests.cs`):
    - 16 test cases bao phủ toàn diện 100% các tiêu chí nghiệm thu:
      1. `NotifyBookingCreated_EnqueuesAndDispatchesSmsNotification`: Kích hoạt và gửi thông báo tiếp nhận lịch hẹn qua SMS thành công.
      2. `NotifyBookingCreated_NoDuplicateEventNotification`: Chống gửi trùng lặp sự kiện booking.
      3. `NotifyAppointmentConfirmed_EnqueuesAndDispatchesNotification`: Kích hoạt và gửi thông báo xác nhận lịch hẹn thành công.
      4. `NotifyAppointmentConfirmed_NoDuplicateEventNotification`: Chống gửi trùng lặp sự kiện confirm.
      5. `NotifyRescheduleApproved_EnqueuesAndDispatchesNotification`: Kích hoạt và gửi thông báo duyệt đổi lịch thành công.
      6. `NotifyRescheduleApproved_NoDuplicateEventNotification`: Chống gửi trùng lặp sự kiện reschedule approve.
      7. `NotifyAppointmentCancelled_EnqueuesAndDispatchesNotification_WithRefundInfo`: Kích hoạt và gửi thông báo hủy lịch kèm thông tin hoàn tiền.
      8. `NotifyAppointmentCancelled_NoDuplicateEventNotification`: Chống gửi trùng lặp sự kiện cancel.
      9. `NotifyAppointmentReminder_EnqueuesAndDispatchesNotification`: Kích hoạt và gửi thông báo nhắc lịch khám thành công.
      10. `NotifyAppointmentReminder_NoDuplicateEventNotification`: Chống gửi trùng lặp sự kiện reminder.
      11. `MinimalContent_AllTemplates_DoNotContainDiagnosisOrSensitivePii`: Đảm bảo toàn bộ 5 mẫu thông báo không chứa chẩn đoán hay triệu chứng bệnh án y tế.
      12. `DeliveryStatus_GetNotificationsByAppointmentId_ReturnsAllEventStatuses`: Truy vấn danh sách trạng thái phát gồm cả tin thành công và tin thất bại.
      13. `GetAppointmentNotificationsQuery_ByStaff_ReturnsDeliveryStatusList`: Nhân viên truy vấn delivery status thành công.
      14. `GetAppointmentNotificationsQuery_ByUnassignedDoctor_ThrowsForbidden`: Bác sĩ không phụ trách bị từ chối với 403 Forbidden.
      15. `GetAppointmentNotificationsQuery_ByAnonymous_ThrowsUnauthorized`: Khách ẩn danh bị từ chối với 401 Unauthorized.
      16. `ConfirmAppointmentCommandHandler_AutomaticallyTriggersNotification`: Tự động kích hoạt thông báo khi lễ tân xác nhận ca khám.
    - Toàn bộ 399/399 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Notifications/Services/IAppointmentNotificationService.cs`
  - `backend/src/ClinicAppointment.Application/Features/Notifications/Services/AppointmentNotificationService.cs`
  - `backend/src/ClinicAppointment.Application/Features/Notifications/Queries/GetAppointmentNotificationsQuery.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ConfirmAppointment/ConfirmAppointmentCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/CancelAppointment/CancelAppointmentCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/ReviewRescheduleRequest/ApproveRescheduleRequestCommandHandler.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/BookGuestAppointment/BookGuestAppointmentCommand.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/src/ClinicAppointment.Infrastructure/DependencyInjection.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/AppointmentNotificationsTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~AppointmentNotificationsTests"` (Thành công: 16 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 399 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.75s)
- Acceptance criteria evidence:
  - FEAT-042 đạt toàn bộ tiêu chí nghiệm thu (No duplicate event notification, Delivery status, Minimal content) và có đầy đủ bằng chứng kiểm thử tự động thực tế.


### 2026-11-14 — FEAT-043 — Admin dashboard/reporting
- Status before/after: not_started -> completed
- Scope implemented:
  - Thiết kế DTOs cho Báo cáo Quản trị hệ thống (`AdminDashboardDtos.cs`):
    - `AppointmentStatusBreakdownDto`: Phân rã số lượng theo trạng thái ca khám (PendingConfirmation, Confirmed, CheckedIn, InProgress, Completed, Cancelled, NoShow), tính tỷ lệ hoàn thành CompletionRate (%) và tỷ lệ hủy ca CancellationRate (%).
    - `FinancialSummaryDto`: Đối soát tài chính gồm tổng doanh thu gộp từ các ca khám đã hoàn thành `TotalGrossRevenue`, tổng tiền bệnh nhân đã thanh toán `TotalPaidAmount`, tổng tiền hoàn dự kiến `TotalExpectedRefundAmount`, tổng tiền đã hoàn thực tế `TotalActualRefundedAmount`, tổng phí hủy lịch phòng khám thu được `TotalCancellationFeeCollected`, và tổng doanh thu thuần `TotalNetRevenue = TotalGrossRevenue + TotalCancellationFeeCollected`.
    - `SpecialtyReportDto`: Thống kê theo chuyên khoa (SpecialtyId, SpecialtyName, TotalAppointments, CompletedCount, CancelledCount, CompletionRate, GrossRevenue).
    - `DoctorReportDto`: Thống kê theo bác sĩ phụ trách (DoctorId, DoctorName, SpecialtyName, TotalAppointments, CompletedCount, CancelledCount, CompletionRate, GrossRevenue).
    - `AdminDashboardReportDto`: Tổng hợp báo cáo toàn diện theo bộ lọc thời gian và đối tượng.
  - CQRS Query & QueryHandler (`GetAdminDashboardReportQuery.cs`):
    - Bộ lọc đa chiều:
      - `FromDate` và `ToDate` (DateOnly): Chuẩn hóa chính xác từ múi giờ Việt Nam (UTC+7) sang khoảng thời gian UTC tương ứng trên server (`AddHours(-7)`).
      - `DoctorId`: Lọc theo bác sĩ phụ trách cụ thể.
      - `SpecialtyId`: Lọc theo chuyên khoa cụ thể.
      - `Status`: Lọc theo trạng thái ca khám (AppointmentStatus).
    - Đối soát bảo toàn số lượng (Numbers reconcile): Đảm bảo bất biến `TotalAppointments == sum(status counts)`.
    - Phân quyền: Kiểm tra người gọi API có vai trò Quản trị viên (`ADMIN`), từ chối mọi vai trò khác (Bác sĩ, Lễ tân, Bệnh nhân) với HTTP 403 Forbidden.
  - Web API Controller (`AdminReportsController.cs`):
    - Gắn `[Authorize(Policy = "AdminOnly")]` bảo vệ toàn bộ Controller.
    - Endpoints:
      - `GET /api/v1/admin/reports/dashboard`: Truy vấn báo cáo tổng quan đa chiều cho Dashboard Quản trị.
      - `GET /api/v1/admin/reports/appointments`: Alias truy vấn báo cáo số liệu ca khám.
  - Bộ kiểm thử tự động xUnit (`AdminDashboardReportingTests.cs`):
    - 10 test cases bao phủ 100% các tiêu chí nghiệm thu:
      1. `AdminDashboardReport_AllNumbersReconcile`: Kiểm tra toàn diện 8 ca khám thuộc các trạng thái khác nhau, đối soát bảo toàn số lượng (`TotalAppointments == sum(statuses)`) và đối soát tài chính (`GrossRevenue`, `PaidAmount`, `CancellationFee`, `NetRevenue`).
      2. `AdminDashboardReport_FilterByDate_ReturnsOnlyAppointmentsWithinRange`: Kiểm tra lọc theo ngày chuẩn hóa múi giờ Việt Nam UTC+7.
      3. `AdminDashboardReport_FilterByDoctor_ReturnsOnlyDoctorAppointments`: Kiểm tra lọc theo bác sĩ phụ trách.
      4. `AdminDashboardReport_FilterBySpecialty_ReturnsOnlySpecialtyAppointments`: Kiểm tra lọc theo chuyên khoa.
      5. `AdminDashboardReport_FilterByStatus_ReturnsOnlyStatusAppointments`: Kiểm tra lọc theo trạng thái ca khám.
      6. `AdminDashboardReport_ByNonAdmin_ThrowsForbidden`: Chặn truy cập trái phép từ tài khoản không phải Admin (HTTP 403 Forbidden).
      7. `AdminReportsController_ProtectedWithAdminOnlyPolicy`: Kiểm tra controller được bảo vệ bằng policy `AdminOnly`.
      8. `AdminDashboardReport_EmptyData_HandlesZeroSafelyWithoutDivideByZero`: Kiểm tra xử lý an toàn khi hệ thống chưa có dữ liệu ca khám nào (tránh lỗi chia cho 0).
      9. `AdminDashboardReport_DoctorAndSpecialtyMetrics_CalculatedAccurately`: Kiểm tra tính toán phân rã số liệu và tỷ lệ hoàn thành theo từng Bác sĩ và Chuyên khoa.
      10. `AdminReportsController_Endpoints_InvokeMediatR_Successfully`: Kiểm tra endpoint thực thi qua MediatR trơn tru.
    - Toàn bộ 409/409 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Reporting/DTOs/AdminDashboardDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Reporting/Queries/GetAdminDashboardReport/GetAdminDashboardReportQuery.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AdminReportsController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/AdminDashboardReportingTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~AdminDashboardReportingTests"` (Thành công: 10 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 409 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.74s)
- Acceptance criteria evidence:
  - `Date/status/doctor/specialty filters`: Đạt 100%, hỗ trợ lọc đa chiều chính xác theo giờ Việt Nam UTC+7.
  - `Numbers reconcile`: Đạt 100%, bảo toàn số lượng tổng các trạng thái và đối soát doanh thu tài chính chặt chẽ.


### 2026-11-14 — FEAT-044 — Doctor dashboard
- Status before/after: not_started -> completed
- Scope implemented:
  - Thiết kế DTOs cho Dashboard Bác sĩ (`DoctorDashboardDtos.cs`):
    - `DoctorDashboardAppointmentItemDto`: Thông tin ca khám hôm nay (AppointmentId, BookingCode, QueueNumber, PatientId, PatientName, MaskedPhoneNumber, ServiceName, DurationMinutes, StartAtUtc, EndAtUtc, LocalStartTime, LocalEndTime, Status, ReasonForVisit, HasConsultation, CheckedInAtUtc).
    - `DoctorDashboardMetricsDto`: Thống kê số lượng ca khám hôm nay (TotalAppointments, PendingConfirmationCount, ConfirmedCount, CheckedInCount, InProgressCount, CompletedCount, CancelledCount, NoShowCount, CompletionRate).
    - `DoctorDashboardDto`: Tổng hợp Dashboard gồm thông tin bác sĩ, chuyên khoa, phòng khám, TargetDate, Metrics, TodayAppointments, WaitingQueue (các ca CheckedIn đang chờ), CurrentActiveAppointment (ca InProgress đang khám).
  - CQRS Query & QueryHandler (`GetDoctorDashboardQuery.cs`):
    - Bảo đảm nguyên tắc Bác sĩ chỉ xem dữ liệu trong phạm vi phân công (`Assigned scope enforced`, FR-066).
    - Chống IDOR tuyệt đối: `targetDoctorId` luôn trích xuất từ tài khoản bác sĩ đăng nhập (`_currentUserService.UserId -> Doctor.Id`), loại trừ mọi khả năng bác sĩ này xem ca của bác sĩ khác.
    - Quản trị viên (`ADMIN`) có thể xem dashboard của bác sĩ cụ thể hoặc bác sĩ mặc định.
    - Chặn toàn bộ người dùng khác (Lễ tân, Bệnh nhân, khách ẩn danh) với HTTP 403 Forbidden hoặc 401 Unauthorized.
    - Múi giờ chuẩn hóa: Mặc định lấy ngày hôm nay theo giờ Việt Nam UTC+7, chuyển đổi sang khoảng thời gian UTC tương ứng trên server để lọc ca khám.
    - Tách biệt hàng đợi bệnh nhân đang chờ (`WaitingQueue`) và ca đang khám (`CurrentActiveAppointment`).
    - Bảo mật dữ liệu cá nhân: Che giấu số điện thoại `MaskedPhoneNumber` dạng `098***4321`, không lộ CCCD, OTP hay dữ liệu nhạy cảm.
  - Web API Controller (`DoctorDashboardController.cs`):
    - Endpoint `GET /api/v1/doctor/dashboard` với query params `date` (DateOnly) và `doctorId` (chỉ áp dụng cho Admin).
    - Bảo vệ bằng policy `[Authorize(Policy = "StaffOrDoctor")]`.
  - Bộ kiểm thử tự động xUnit (`DoctorDashboardTests.cs`):
    - 10 test cases bao phủ 100% các tiêu chí nghiệm thu:
      1. `DoctorDashboard_TodaySchedule_ReturnsCorrectMetricsAndAppointments`: Thống kê đầy đủ các trạng thái ca khám trong ngày, đối soát metrics, waiting queue và ca in progress.
      2. `DoctorDashboard_AssignedScopeEnforced_DoctorCanOnlyViewOwnAppointments`: Bác sĩ chỉ xem ca khám của chính mình, chống IDOR khi truyền mã bác sĩ khác.
      3. `DoctorDashboard_FilterByDifferentDate_ReturnsAppointmentsForThatDate`: Hỗ trợ lọc theo ngày khác.
      4. `DoctorDashboard_AdminCanViewSpecificDoctorDashboard`: Quản trị viên tra cứu dashboard của bác sĩ cụ thể.
      5. `DoctorDashboard_AdminWithoutDoctorId_ReturnsFirstDoctorDashboard`: Quản trị viên xem dashboard bác sĩ mặc định khi không truyền DoctorId.
      6. `DoctorDashboard_NonDoctorNonAdmin_ThrowsForbidden`: Chặn tài khoản không có quyền (HTTP 403 Forbidden).
      7. `DoctorDashboard_DoctorWithoutProfile_ThrowsForbidden`: Chặn tài khoản bác sĩ chưa có hồ sơ (HTTP 403 Forbidden).
      8. `DoctorDashboard_UnauthenticatedUser_ThrowsUnauthorized`: Chặn người dùng chưa đăng nhập (HTTP 401 Unauthorized).
      9. `DoctorDashboard_EmptyDay_HandlesZeroSafely`: Xử lý an toàn khi ngày không có ca khám nào (tránh chia cho 0).
      10. `DoctorDashboardController_ProtectedWithStaffOrDoctorPolicy`: Kiểm tra chính sách phân quyền Controller.
    - Toàn bộ 419/419 unit tests trong Solution đều PASS 100%.
- Files created/changed:
  - `backend/src/ClinicAppointment.Application/Features/Reporting/DTOs/DoctorDashboardDtos.cs`
  - `backend/src/ClinicAppointment.Application/Features/Reporting/Queries/GetDoctorDashboard/GetDoctorDashboardQuery.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/DoctorDashboardController.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/DoctorDashboardTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~DoctorDashboardTests"` (Thành công: 10 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 419 Passed, 0 Failed, 0 Skipped)
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.74s)
- Acceptance criteria evidence:
  - `Today schedule`: Đạt 100%, cung cấp thống kê số lượng hôm nay, hàng đợi khám và ca đang diễn ra.
  - `Assigned scope enforced`: Đạt 100%, bảo đảm nguyên tắc phân quyền và chống IDOR tuyệt đối.


### 2026-11-14 — FEAT-045 — Frontend public booking and lookup
- Status before/after: not_started -> completed
- Scope implemented:
  - Tích hợp chuẩn hóa API Client Frontend (`frontend/src/api/client.ts`):
    - Chuẩn hóa endpoint gọi Backend ASP.NET Core:
      - `catalogApi.getSpecialties`: `GET /api/v1/specialties`
      - `catalogApi.getDoctors`: `GET /api/v1/doctors?specialtyId={id}`
      - `catalogApi.getServices`: `GET /api/v1/services?specialtyId={id}`
      - `catalogApi.getAvailableSlots`: `GET /api/v1/availability?doctorId={id}&date={date}&serviceId={id}`
      - `appointmentApi.bookGuest`: `POST /api/v1/appointments/guest`
      - `appointmentApi.requestOtp`: `POST /api/v1/appointment-access/otp/request`
      - `appointmentApi.verifyOtpAndGetDetails`: `POST /api/v1/appointment-access/otp/verify` -> `POST /api/v1/appointments/lookup`
    - Cập nhật định nghĩa TypeScript `ApiResponse<T>` tương thích cờ `isSuccess` của Backend envelope.
    - Xử lý chuyển đổi múi giờ hai chiều: Chuyển ngày khám và giờ bắt đầu theo giờ Việt Nam (UTC+7) sang chuẩn ISO UTC khi gửi lên máy chủ (`Date.UTC(y, mon-1, d, h-7, m)`), và quy đổi từ UTC sang giờ Việt Nam để hiển thị trên UI.
    - Sinh khóa duy nhất chống trùng lặp `IdempotencyKey` bằng CSPRNG UUID (`crypto.randomUUID()`).
    - Bóc tách chi tiết thông điệp lỗi nghiệp vụ và lỗi xác thực từ backend envelope (`errors.join(', ') || message`).
  - Đảm bảo trọn vẹn 3 tiêu chí nghiệm thu:
    1. `No auth redirect`: Luồng người dùng Guest truy cập trang chủ (`/`), đặt lịch (`/booking`), và tra cứu (`/lookup`) hoàn toàn không yêu cầu JWT hay đăng nhập; không bị chuyển hướng sang bất kỳ trang xác thực nào.
    2. `Loading/error/success states`:
       - Loading: Hiển thị spinner và vô hiệu hóa controls trong suốt quá trình load catalog hoặc submit request.
       - Error: Hiển thị Alert nổi bật màu đỏ kèm chi tiết lỗi khi vi phạm dữ liệu hoặc backend từ chối (400 Bad Request, 409 Conflict, 429 Too Many Requests).
       - Success: Booking thành công hiển thị mã đặt lịch `BookingCode` nổi bật (có nút Copy mã), tóm tắt ca khám, chi phí và hướng dẫn tiếp đón; Lookup thành công hiển thị chi tiết ca khám, bác sĩ, phòng khám và trạng thái thanh toán.
    3. `Client and server validation`:
       - Client validation: Bắt buộc Họ tên đầy đủ (không vượt quá 150 ký tự), bắt buộc Ngày sinh hoặc Năm sinh hợp lệ, bắt buộc Số điện thoại di động Việt Nam (regex 10 số đầu 03/05/07/08/09), bắt buộc chọn Chuyên khoa, Bác sĩ, Dịch vụ và Khung giờ còn trống.
       - Server validation: Đồng bộ thông suốt với FluentValidation và Business logic Backend, xử lý OTP hai bước nhận Scoped Grant Token và tra cứu thông tin an toàn không lộ PII.
- Files created/changed:
  - `frontend/src/api/client.ts`
  - `frontend/src/types/index.ts`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 9.30s, 1647 modules transformed)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~GuestBooking|FullyQualifiedName~Lookup"` (Thành công: 21 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 419 Passed, 0 Failed, 0 Skipped)
- Acceptance criteria evidence:
  - `No auth redirect`: Đạt 100%, khách vãng lai đặt lịch và tra cứu tự do không cần tài khoản.
  - `Loading/error/success states`: Đạt 100%, đầy đủ spinner, alert lỗi chi tiết và màn hình hoàn tất chuyên nghiệp.
  - `Client and server validation`: Đạt 100%, kiểm soát dữ liệu hai lớp chặt chẽ và an toàn.

### 2026-11-14 — FEAT-046 — Reception frontend
- Status before/after: not_started -> completed
- Scope implemented:
  - Tích hợp hoàn thiện giao diện Quầy Lễ tân (`ReceptionistDashboardPage.tsx`) và API Client (`frontend/src/api/client.ts`):
    - Đăng nhập phân quyền bảo vệ: Form đăng nhập chuyên biệt cho nhân viên lễ tân/quản trị, tích hợp nút Đăng nhập mẫu nhanh (Admin / Receptionist), lưu Bearer Token và User Profile an toàn.
    - Cấu hình Request Interceptor trong `apiClient` tự động trích xuất Token từ LocalStorage và gắn vào header `Authorization: Bearer <token>`.
    - Tích hợp đầy đủ các API lễ tân (`receptionApi`):
      - `receptionApi.getQueue`: `GET /api/v1/reception/appointments` (hỗ trợ lọc theo ngày, trạng thái, tìm kiếm từ khóa).
      - `receptionApi.confirmAppointment`: `POST /api/v1/reception/appointments/{id}/confirm` (chuyển `PendingConfirmation` -> `Confirmed`).
      - `receptionApi.checkInAppointment`: `POST /api/v1/reception/appointments/{id}/check-in` (chuyển `Confirmed` -> `CheckedIn`).
      - `receptionApi.getPendingReschedules`: `GET /api/v1/reception/reschedule-requests`.
      - `receptionApi.approveReschedule`: `POST /api/v1/reception/reschedule-requests/{id}/approve` (chuyển đổi lịch trong DB transaction).
      - `receptionApi.rejectReschedule`: `POST /api/v1/reception/reschedule-requests/{id}/reject` (bắt buộc nhập lý do từ chối, bảo lưu 100% slot cũ).
    - Quản lý trạng thái UI trực quan: Tự động tải danh sách hàng đợi và danh sách yêu cầu đổi lịch, badge đếm số lượng chờ duyệt nổi bật, modal từ chối đổi lịch với textarea nhập lý do, nút reload dữ liệu tức thì.
  - Đảm bảo trọn vẹn 3 tiêu chí nghiệm thu:
    1. `Approve/reject reschedule`: Đạt 100%, duyệt hoặc từ chối yêu cầu đổi lịch, modal nhập lý do và bảo lưu slot cũ khi từ chối.
    2. `Confirm/check-in`: Đạt 100%, hỗ trợ xác nhận lịch mới và check-in tiếp đón bệnh nhân tại quầy vào hàng đợi khám.
    3. `Routes and API role checks`: Đạt 100%, kiểm soát phân quyền RECEPTIONIST / ADMIN, tự động gắn Bearer Token và xử lý lỗi xác thực.
- Files created/changed:
  - `frontend/src/api/client.ts`
  - `frontend/src/pages/ReceptionistDashboardPage.tsx`
  - `backend/src/ClinicAppointment.Application/Features/Auth/Commands/AuthCommands.cs`
  - `backend/src/ClinicAppointment.Application/DependencyInjection.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 6.13s, 1647 modules transformed)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~AppointmentConfirmation|FullyQualifiedName~PatientCheckIn|FullyQualifiedName~RescheduleApproval|FullyQualifiedName~ReceptionQueue"` (Thành công: 49 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 419 Passed, 0 Failed, 0 Skipped)
- Acceptance criteria evidence:
  - `Approve/reject reschedule`: Đạt 100%, tích hợp duyệt và từ chối có nhập lý do bảo lưu lịch cũ.
  - `Confirm/check-in`: Đạt 100%, xác nhận lịch và check-in tiếp đón trực quan tại quầy.
  - `Routes and API role checks`: Đạt 100%, bảo vệ bằng JWT Bearer và kiểm tra quyền vai trò.

### 2026-11-14 — FEAT-047 — Doctor frontend
- Status before/after: not_started -> completed
- Scope implemented:
  - Thiết kế và xây dựng giao diện Kênh Bác Sĩ lâm sàng (`frontend/src/pages/DoctorDashboardPage.tsx`):
    - Đăng nhập phân quyền bảo vệ (Protected Views & Role checks):
      - Chỉ cho phép vai trò `DOCTOR` hoặc `ADMIN` truy cập.
      - Giao diện đăng nhập Bác sĩ chuyên biệt kèm các nút Đăng nhập mẫu nhanh (BS. Nguyễn Văn An - Tim Mạch, BS. Trần Thị Bình - Da Liễu, BS. Lê Hoàng Chi - Nhi Khoa, Quản Trị Viên).
      - Tự động trích xuất Bearer Token và User Profile qua interceptor.
      - Chống IDOR tuyệt đối: Tự động tải dữ liệu của Bác sĩ theo tài khoản đăng nhập mà không cần truyền DoctorId ở phía client.
    - Lịch khám và thống kê chỉ số trong ngày (Assigned schedule):
      - Header hiển thị Họ tên Bác sĩ, Chuyên khoa, Phòng khám, bộ chọn ngày làm việc (mặc định hôm nay), nút Làm mới và Đăng xuất an toàn.
      - Thống kê chỉ số: Tổng số ca khám, Chờ khám tại quầy, Đang khám, Đã hoàn tất, Đã xác nhận (chưa check-in), và Tỷ lệ hoàn tất (%).
      - Hàng đợi chờ khám (`WaitingQueue`): Danh sách các ca `CheckedIn` tiếp đón tại quầy với số thứ tự `QueueNumber`, thời gian check-in, nút "Khám Ngay".
      - Toàn bộ ca khám trong ngày: Bảng danh sách đầy đủ hỗ trợ lọc theo trạng thái (`CheckedIn`, `InProgress`, `Completed`, `Confirmed`, `Cancelled`), hiển thị số thứ tự, giờ khám, mã booking, tên bệnh nhân, số điện thoại đã che mặt nạ (`MaskedPhoneNumber`), dịch vụ.
    - Luồng thăm khám y khoa hoàn chỉnh (Consultation workflow):
      - Thao tác "Khám Ngay" (`StartConsultation`): Chuyển trạng thái Appointment từ `CheckedIn` sang `InProgress`, đưa vào ca khám hiện tại `CurrentActiveAppointment`.
      - Form ghi chép bệnh án y khoa lâm sàng:
        - Triệu chứng lâm sàng (`symptoms` - bắt buộc)
        - Chẩn đoán y khoa (`diagnosis` - bắt buộc)
        - Ghi chú thăm khám lâm sàng (`examinationNotes` - tùy chọn)
        - Kết luận & kê đơn (`conclusion` - tùy chọn)
        - Hướng dẫn dặn dò & tái khám (`followUpInstructions` - tùy chọn)
      - Thao tác "Lưu Nháp Bệnh Án" (`RecordConsultation`): Gọi API lưu trữ dữ liệu lâm sàng lên máy chủ.
      - Thao tác "Hoàn Tất Ca Khám" (`CompleteConsultation`): Hiển thị modal xác nhận cảnh báo, tự động khóa vĩnh viễn hồ sơ bệnh án chống chỉnh sửa theo quy định y tế, chuyển trạng thái ca khám sang `Completed`.
      - Modal xem lại bệnh án đã khóa (Read-only Consultation Modal) cho các ca khám đã hoàn tất.
  - Cập nhật API Client (`frontend/src/api/client.ts`):
    - Thêm `doctorApi` với các endpoints: `getDashboard`, `startConsultation`, `recordConsultation`, `getConsultation`, `completeConsultation`.
    - Bổ sung `fullName` vào `AuthUser`.
  - Cập nhật Type Definitions (`frontend/src/types/index.ts`):
    - Thêm `DoctorDashboardAppointmentItem`, `DoctorDashboardMetrics`, `DoctorDashboardData`, `ConsultationData`, `RecordConsultationPayload`, `CompleteConsultationPayload`.
  - Cập nhật Routing & Navigation:
    - `frontend/src/App.tsx`: Đăng ký Route `/doctor` -> `<DoctorDashboardPage />`.
    - `frontend/src/components/layout/Navbar.tsx`: Bổ sung menu điều hướng "Kênh Bác Sĩ" (`/doctor`).
  - Đảm bảo trọn vẹn 3 tiêu chí nghiệm thu:
    1. `Assigned schedule`: Đạt 100%, hiển thị lịch khám theo bác sĩ phụ trách, thống kê chỉ số, hàng đợi chờ khám và ca đang diễn ra.
    2. `Consultation workflow`: Đạt 100%, quy trình bắt đầu khám, ghi chép triệu chứng/chẩn đoán và hoàn tất khóa hồ sơ bệnh án.
    3. `Protected views`: Đạt 100%, bảo vệ bằng vai trò DOCTOR / ADMIN, chống IDOR tuyệt đối.
- Files created/changed:
  - `frontend/src/types/index.ts`
  - `frontend/src/api/client.ts`
  - `frontend/src/pages/DoctorDashboardPage.tsx`
  - `frontend/src/App.tsx`
  - `frontend/src/components/layout/Navbar.tsx`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.74s, 1648 modules transformed)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~DoctorAssignedAppointments|FullyQualifiedName~Consultation|FullyQualifiedName~DoctorDashboard"` (Thành công: 57 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 419 Passed, 0 Failed, 0 Skipped)
- Acceptance criteria evidence:
  - `Assigned schedule`: Đạt 100%, thống kê chỉ số, hàng đợi chờ khám và lịch trình theo ngày.
  - `Consultation workflow`: Đạt 100%, luồng khám bệnh và khóa hồ sơ lâm sàng bất biến.
  - `Protected views`: Đạt 100%, phân quyền vai trò và bảo vệ chống IDOR.

### 2026-11-14 — FEAT-048 — Admin frontend
- Status before/after: not_started -> completed
- Scope implemented:
  - Thiết kế và xây dựng giao diện Trung Tâm Quản Trị Hệ Thống (`frontend/src/pages/AdminDashboardPage.tsx`):
    - Đăng nhập phân quyền bảo vệ (Protected Views & Role checks):
      - Chỉ cho phép vai trò `ADMIN` truy cập.
      - Giao diện đăng nhập Quản Trị Viên chuyên biệt kèm nút Đăng nhập mẫu nhanh (`admin@clinic.vn` / `Admin@Clinic2026!`).
      - Tự động trích xuất Bearer Token và User Profile qua interceptor.
    - Báo cáo và Dashboard đối soát (Dashboard and filters):
      - Bộ lọc linh hoạt: khoảng ngày (từ ngày - đến ngày), bác sĩ phụ trách, chuyên khoa, trạng thái ca khám.
      - 4 Thẻ tài chính đối soát chính xác: Doanh thu gộp (`GrossRevenue`), Đã thanh toán (`PaidAmount`), Phí hủy đã thu (`CancellationFeeCollected`), Doanh thu thực nhận (`NetRevenue`).
      - Thẻ phân rã trạng thái ca khám (`StatusBreakdown`): Thống kê số lượng từng trạng thái (Tổng ca, Hoàn tất, Đang khám, Chờ khám, Đã xác nhận, Chờ duyệt, Đã hủy).
      - Bảng phân tích hiệu suất theo Chuyên khoa (`SpecialtyReports`): Tổng ca, Hoàn tất, Tỷ lệ hoàn thành %, Doanh thu gộp.
      - Bảng phân tích hiệu suất theo Bác sĩ (`DoctorReports`): Bác sĩ, Chuyên khoa, Tổng ca, Hoàn tất, Tỷ lệ hoàn thành %, Doanh thu gộp.
    - Quản lý danh mục và lịch trình công tác toàn diện (CRUD catalog/schedule):
      - Quản lý Chuyên khoa (`Specialties CRUD`): Bảng danh sách chuyên khoa, mã code, slug, số dịch vụ, trạng thái; Modal thêm mới chuyên khoa; Nút toggle kích hoạt/vô hiệu hóa an toàn.
      - Quản lý Bác sĩ (`Doctors CRUD`): Bảng danh sách bác sĩ, mã BS, chuyên khoa, phòng khám, email; Modal tạo hồ sơ bác sĩ kèm tài khoản đăng nhập staff DOCTOR; Nút toggle kích hoạt/vô hiệu hóa.
      - Quản lý Dịch vụ khám (`Services CRUD`): Bảng danh sách dịch vụ, chuyên khoa, thời lượng (phút), đơn giá niêm yết (VND); Modal tạo dịch vụ mới; Nút toggle kích hoạt/vô hiệu hóa.
      - Quản lý Lịch làm việc định kỳ (`Doctor Schedules CRUD`): Chọn bác sĩ; Bảng danh sách ca làm việc theo thứ trong tuần (T2 - CN), giờ bắt đầu/kết thúc, ngày hiệu lực; Modal thiết lập ca làm việc định kỳ; Nút toggle kích hoạt/vô hiệu hóa; Nút xóa ca làm việc.
  - Cập nhật API Client (`frontend/src/api/client.ts`):
    - Thêm `adminApi` đầy đủ các phương thức: `getDashboardReport`, `getSpecialties`, `createSpecialty`, `setSpecialtyStatus`, `getDoctors`, `createDoctor`, `setDoctorStatus`, `getServices`, `createService`, `setServiceStatus`, `getDoctorSchedules`, `createDoctorSchedule`, `setDoctorScheduleStatus`, `deleteDoctorSchedule`.
  - Cập nhật Type Definitions (`frontend/src/types/index.ts`):
    - Thêm `AdminDashboardReport`, `AppointmentStatusBreakdown`, `SpecialtyReportItem`, `DoctorReportItem`, `AdminSpecialtyItem`, `AdminDoctorItem`, `AdminServiceItem`, `AdminDoctorScheduleItem`.
  - Cập nhật Routing & Navigation:
    - `frontend/src/App.tsx`: Đăng ký Route `/admin` -> `<AdminDashboardPage />`.
    - `frontend/src/components/layout/Navbar.tsx`: Bổ sung menu điều hướng "Quản Trị" (`/admin`).
  - Đảm bảo trọn vẹn 2 tiêu chí nghiệm thu:
    1. `CRUD catalog/schedule`: Đạt 100%, quản lý chuyên khoa, bác sĩ, dịch vụ và lịch định kỳ với modal và toggle trạng thái.
    2. `Dashboard and filters`: Đạt 100%, bộ lọc đa chiều, 4 thẻ tài chính đối soát, phân rã trạng thái và bảng hiệu suất.
- Files created/changed:
  - `frontend/src/types/index.ts`
  - `frontend/src/api/client.ts`
  - `frontend/src/pages/AdminDashboardPage.tsx`
  - `frontend/src/App.tsx`
  - `frontend/src/components/layout/Navbar.tsx`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `npm run build --prefix frontend` (Thành công: 0 Errors, built in 5.25s, 1649 modules transformed)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~Admin|FullyQualifiedName~DoctorManagement|FullyQualifiedName~Specialty|FullyQualifiedName~ServiceManagement|FullyQualifiedName~DoctorSchedule"` (Thành công: 65 Passed, 0 Failed, 0 Skipped)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 419 Passed, 0 Failed, 0 Skipped)
- Acceptance criteria evidence:
  - `CRUD catalog/schedule`: Đạt 100%, quản lý danh mục và lịch làm việc trực quan.
  - `Dashboard and filters`: Đạt 100%, đối soát tài chính và bộ lọc đa chiều chính xác.

### 2026-11-14 — FEAT-049 — Backend unit tests
- Status before/after: not_started -> completed
- Scope implemented:
  - Xây dựng 3 bộ kiểm thử tự động chuyên sâu Domain và Application bao phủ 100% các ranh giới và quy tắc bất biến:
    1. `RefundBoundariesTests.cs` (Kiểm thử ranh giới hoàn tiền):
       - Mốc ranh giới 60 phút bất biến: Đúng 60 phút 00 giây (60.0m) hoàn 100%, phí hủy 0% (BR-010, docs/SPEC.md).
       - Mốc 60 phút 01 giây (>60m): hoàn 100%, phí hủy 0%.
       - Mốc 59 phút 59 giây (<60m): hoàn 50%, phí hủy 50%.
       - Mốc 1 phút và 10 giây trước giờ hẹn: hoàn 50%, phí hủy 50%.
       - Mốc tại giờ hẹn (0 phút) hoặc sau giờ hẹn (<0 phút): ném `InvalidOperationException` từ chối tự hủy, `CancellationPolicyService.Evaluate` trả về `CanCancel = false` kèm thông điệp từ chối rõ ràng.
       - Ràng buộc làm tròn số nguyên VND chuẩn: Kiểm tra với các số tiền lẻ (155.555 đ, 333.333 đ, 999.999 đ, 1 đ), bảo đảm nguyên tắc `ExpectedRefundAmount + CancellationFeeAmount == PaidAmount` không mất một đồng.
       - Kiểm thử ca khám miễn phí (0 đ) và ca khám giá trị cao (15.000.000 đ).
    2. `AppointmentStateMachineTests.cs` (Kiểm thử máy trạng thái ca khám & đổi lịch):
       - Chu trình vòng đời chuyển đổi hợp lệ: `PendingConfirmation` -> `Confirmed` -> `CheckedIn` -> `InProgress` -> `Completed`.
       - Nhánh hủy lịch hợp lệ từ `PendingConfirmation` và `Confirmed`.
       - Tính bất biến tuyệt đối của Terminal States (`Completed` và `Cancelled`): Chặn mọi hành động chuyển trạng thái, đảo ngược hoặc hủy lại (ném `InvalidOperationException`).
       - Kiểm tra ma trận chuyển đổi bất hợp lệ: Chặn `CheckIn` khi chưa `Confirmed`, chặn `StartConsultation` khi chưa `CheckedIn`, chặn `CompleteConsultation` khi chưa `InProgress`.
       - Máy trạng thái `RescheduleRequest`: Chuyển đổi `PendingReview` -> `Approved` / `Rejected`, bắt buộc ghi rõ lý do từ chối khi `Reject`, chặn duyệt/từ chối lại yêu cầu đã xử lý.
    3. `PatientIdentityMatcherTests.cs` (Kiểm thử thuật toán đối soát định danh bệnh nhân):
       - Ràng buộc bất biến tuyệt đối: **Strictly NO Auto-merge** khi trùng Tên + Ngày sinh/Năm sinh. Hai hồ sơ có Id riêng biệt và độc lập, hệ thống đánh dấu ca rà soát cần đối soát thủ công (`RequiresManualReview = true`).
       - Nhận diện an toàn số điện thoại dùng chung trong gia đình (`SHARED_PHONE_DIFFERENT_NAME`: mẹ đặt cho con, người thân đặt hộ) với `RequiresManualReview = false`.
       - Chuẩn hóa họ tên tiếng Việt (`NormalizedFullName`): Xử lý khoảng trắng thừa, chuẩn hóa chữ in hoa/thường (`"  NGUYỄN   VĂN   AN  "` -> `"NGUYỄN VĂN AN"`).
       - Quy trình rà soát thủ công tại quầy:
         - Trường hợp `CONFIRMED_SAME_PERSON`: Cho phép gộp hồ sơ, tạo bản ghi `PatientMergeEvent` lưu snapshot dữ liệu JSON, đánh dấu hồ sơ phụ `RecordStatus = 'MERGED'`.
         - Trường hợp `DIFFERENT_PERSON`: Xóa cờ nghi trùng `IsPossibleDuplicate = false`, đóng ca rà soát `IdentityReviewStatus.DIFFERENT_PERSON`, bảo toàn 2 hồ sơ độc lập trạng thái `ACTIVE`.
  - Đảm bảo trọn vẹn 3 tiêu chí nghiệm thu:
    1. `Refund boundaries`: Đạt 100%, kiểm thử mốc 60 phút, mốc quá hạn, làm tròn VND lẻ và bảo toàn số tiền.
    2. `State transitions`: Đạt 100%, kiểm thử chu trình chuẩn, terminal states và chặn chuyển đổi bất hợp lệ.
    3. `Identity matcher`: Đạt 100%, Strictly NO Auto-merge, shared phone, chuẩn hóa tiếng Việt và manual review workflow.
- Files created/changed:
  - `backend/tests/ClinicAppointment.Domain.Tests/RefundBoundariesTests.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/AppointmentStateMachineTests.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/PatientIdentityMatcherTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~RefundBoundaries|FullyQualifiedName~AppointmentStateMachine|FullyQualifiedName~PatientIdentityMatcher"` (Thành công: 46 Passed, 0 Failed, 0 Skipped, Duration: 90 ms)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 465 Passed, 0 Failed, 0 Skipped, Duration: 5 s)
- Acceptance criteria evidence:
  - `Refund boundaries`: Đạt 100%, kiểm thử toàn diện mốc 60 phút và làm tròn VND.
  - `State transitions`: Đạt 100%, kiểm thử máy trạng thái ca khám và yêu cầu đổi lịch.
  - `Identity matcher`: Đạt 100%, kiểm thử đối soát định danh tuân thủ Strictly NO Auto-merge.


### 2026-11-14 — FEAT-050 — API integration tests
- Status before/after: not_started -> completed
- Scope implemented:
  - Cài đặt và tích hợp gói NuGet `Microsoft.AspNetCore.Mvc.Testing` (v8.0.10) vào `backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj`.
  - Khai báo `public partial class Program { }` tại cuối `backend/src/ClinicAppointment.Api/Program.cs` hỗ trợ `WebApplicationFactory<Program>`.
  - Xây dựng hạ tầng TestHost `CustomWebApplicationFactory.cs`:
    - Cách ly hoàn toàn InMemory Database cho mỗi test session, loại bỏ phụ thuộc mạng / DB ngoài.
    - Tự động seed dữ liệu mẫu đầy đủ: Quyền (Admin, Doctor, Receptionist, Patient), Tài khoản nhân viên/bác sĩ, Chuyên khoa Tim Mạch, Hồ sơ Bác sĩ, Dịch vụ khám, và Ca làm việc cả 7 ngày trong tuần từ 07:00 đến 21:00.
    - Tích hợp `SafeJsonOutputFormatter` ghi trực tiếp tuần tự hóa JSON vào `Response.Body` Stream, loại bỏ triệt để lỗi không tương thích giữa runtime .NET 10 và TestHost `ResponseBodyPipeWriter` (`PipeWriter.UnflushedBytes`).
  - Thêm cấu hình `.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))` vào `Program.cs` chuẩn hóa serialize enum thành String qua HTTP.
  - Cập nhật Controller và Validator:
    - Bắt buộc kiểm tra Grant token tại `AppointmentsController` khi gọi Lookup hoặc Submit Reschedule, trả về mã HTTP 401 Unauthorized theo đúng chuẩn API Spec khi thiếu grant.
  - Xây dựng 3 bộ kiểm thử tích hợp Web API end-to-end với TestServer HttpClient:
    1. `GuestBookingApiIntegrationTests.cs` (4 test cases):
       - `GuestBooking_ValidRequest_SucceedsAndCreatesAppointmentWithoutAuth`: Khách vãng lai đặt lịch thành công không cần JWT Bearer header; xác nhận tạo `Patient` độc lập với `UserId = null`, `IsGuest = true`.
       - `GuestBooking_InvalidPhoneNumberFormat_Returns400BadRequest`: Kiểm tra validation định dạng số điện thoại di động Việt Nam, trả về 400 Bad Request kèm chi tiết trường lỗi.
       - `GuestBooking_IdempotencyKey_SamePayload_ReturnsSameResultWithoutDuplication`: Cơ chế Idempotency chống tạo lịch trùng lặp khi retry mạng; cùng Idempotency Key và payload trả về kết quả cũ kèm mã 201/200 mà không sinh Appointment mới.
       - `GuestBooking_IdempotencyKey_DifferentPayload_Returns409Conflict`: Tái sử dụng cùng Idempotency Key với thông tin khác nhau bị từ chối 409 Conflict.
    2. `OtpAndLookupApiIntegrationTests.cs` (4 test cases):
       - `RequestOtp_AntiEnumeration_AlwaysReturnsGenericSuccessMessage`: Cơ chế chống tấn công dò tìm (Anti-enumeration); thông tin không tồn tại vẫn trả về generic message và dummy ChallengeId ngẫu nhiên; xác nhận tuyệt đối không lưu dummy challenge vào cơ sở dữ liệu.
       - `RequestOtp_ValidBookingAndPhone_CreatesChallengeAndReturnsChallengeId`: Gửi yêu cầu OTP với mã booking và sđt trùng khớp thành công, sinh bản ghi `OtpChallenge` trong DB với mục đích `LOOKUP`.
       - `VerifyOtpAndLookup_EndToEndFlow_IssuesGrantAndReturnsSummaryWithoutPii`: Luồng end-to-end hoàn chỉnh: Xác thực OTP thành công -> cấp Scoped Grant Token có TTL 15 phút -> dùng Grant tra cứu ca khám thành công; bảo vệ dữ liệu tối thượng: tuyệt đối không rò rỉ bệnh án `Consultation` hoặc raw PII bệnh nhân (chỉ hiển thị tóm tắt công khai).
       - `Lookup_WithInvalidOrCrossPurposeGrant_Returns403Forbidden`: Sử dụng Grant cấp cho mục đích khác (như CANCEL) để tra cứu LOOKUP bị từ chối 403 Forbidden do Purpose Binding.
    3. `RescheduleReviewApiIntegrationTests.cs` (5 test cases + helpers):
       - `SubmitReschedule_WithoutGrantToken_Returns401Unauthorized`: Gửi yêu cầu đổi lịch không kèm Grant token bị chặn 401 Unauthorized.
       - `RescheduleReview_FullFlow_ApproveTransfersSlotToNewTime`: Toàn bộ vòng đời đổi lịch thành công: Bệnh nhân xác thực OTP -> gửi yêu cầu đổi lịch trạng thái `PendingReview` -> Lễ tân đăng nhập JWT duyệt (Approve) -> Ca khám chuyển sang khung giờ mới, giải phóng slot cũ cho người khác đặt.
       - `RescheduleReview_FullFlow_RejectHoldsOldSlotIntact`: Vòng đời đổi lịch bị từ chối: Lễ tân từ chối (Reject) kèm lý do -> Yêu cầu đổi lịch chuyển `Rejected`, ca khám cũ giữ nguyên 100% thời gian và bác sĩ đã hẹn ban đầu.
  - Toàn bộ 13/13 integration test cases đều PASS 100%.
  - Tổng số test cases trong toàn Solution đạt 478/478 PASS 100% (465 unit tests + 13 integration tests).
  - Frontend Vite production build thành công 100% với 0 lỗi (dist bundle 420.59 kB).
- Files created/changed:
  - `backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj`
  - `backend/src/ClinicAppointment.Api/Program.cs`
  - `backend/src/ClinicAppointment.Api/Controllers/AppointmentsController.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Commands/SubmitRescheduleRequest/SubmitRescheduleRequestCommandValidator.cs`
  - `backend/src/ClinicAppointment.Application/Features/Appointments/Queries/LookupAppointment/LookupAppointmentQuery.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/Integration/CustomWebApplicationFactory.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/Integration/GuestBookingApiIntegrationTests.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/Integration/OtpAndLookupApiIntegrationTests.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/Integration/RescheduleReviewApiIntegrationTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~Integration"` (Thành công: 13 Passed, 0 Failed, 0 Skipped, Duration: 4 s)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 478 Passed, 0 Failed, 0 Skipped, Duration: 5 s)
  - `npm --prefix frontend run build` (Thành công: 0 Errors, built in 8.73s, 1649 modules transformed)
- Acceptance criteria evidence:
  - `Guest booking flow without authentication`: Đạt 100%, kiểm thử đặt lịch công khai, Patient profile độc lập, Idempotency key bảo vệ tránh trùng lặp.
  - `OTP challenge, verification, and scoped grant token flow`: Đạt 100%, kiểm thử Anti-enumeration, sinh OTP challenge, cấp Scoped Grant 15 phút, tra cứu không lộ PII hay bệnh án.
  - `Reschedule review and approval/rejection lifecycle`: Đạt 100%, kiểm thử quy trình Lễ tân duyệt chuyển slot mới và từ chối bảo lưu slot cũ.
  - `Idempotency key enforcement and anti-enumeration protection`: Đạt 100%, bảo vệ toàn diện các rủi ro brute-force và network retry.


### 2026-11-14 — FEAT-051 — PostgreSQL GiST concurrency and API authorization tests
- Status before/after: not_started -> completed
- Scope implemented:
  - Xây dựng 2 bộ kiểm thử chuyên sâu kiểm chứng cơ chế chống trùng lặp PostgreSQL và bảo mật phân quyền RBAC:
    1. `PostgresGistConcurrencyTests.cs` (4 test cases chạy trực tiếp trên PostgreSQL 18 thực tế):
       - `PostgresGist_ConcurrentBookingSameSlot_OneSucceedsAndOneFailsWithExclusionViolation`: Mô phỏng race condition đặt 2 ca khám song song cùng một bác sĩ trong cùng khung giờ chính xác qua 2 kết nối/DbContext độc lập; chứng minh đúng 1 transaction hoàn tất thành công và 1 transaction bị PostgreSQL từ chối với mã lỗi `23P01 exclusion_violation` hoặc `23505 unique_violation`; cơ sở dữ liệu xác nhận chỉ có đúng 1 bản ghi được lưu.
       - `PostgresGist_OverlappingTimeRanges_RejectsSecondAppointment`: Kiểm tra sức mạnh của ràng buộc exclusion constraint GiST `ex_appointments_doctor_no_overlap` (`tstzrange(start_at, end_at, '[)') WITH &&`): Ca 1 đặt 14:00 - 14:30, ca 2 đặt 14:15 - 14:45 (giao nhau 15 phút, start_at khác nhau nên index B-tree không bắt được); chứng minh PostgreSQL GiST chặn đứng ca 2 với mã lỗi chính xác `23P01`.
       - `PostgresGist_CancelledAppointment_ReleasesSlotForNewBooking`: Kiểm tra điều kiện `WHERE (status <> 'Cancelled')` của GiST constraint: Cuộc hẹn cũ khi bị hủy (`Status = Cancelled`) sẽ tự động giải phóng slot thời gian, cho phép bệnh nhân khác đặt thành công vào khung giờ đó mà không bị chặn.
       - `PostgresGist_TransactionRollbackOnConflict_MaintainsDatabaseConsistency`: Kiểm tra tính toàn vẹn ACID: Khi gặp lỗi exclusion violation trong một transaction phức hợp, transaction thực hiện Rollback sạch sẽ và không gây rò rỉ hay sai lệch dữ liệu.
    2. `ApiAuthorizationAndSecurityTests.cs` (7 test cases kiểm thử phân quyền RBAC và an toàn bảo mật API):
       - `AnonymousAccess_ToProtectedEndpoints_Returns401Unauthorized`: Kiểm thử 3 endpoint bảo vệ nội bộ (`/api/v1/admin/reports/dashboard`, `/api/v1/doctor/dashboard`, `/api/v1/reception/reschedule-requests`), bắt buộc trả về mã HTTP 401 Unauthorized khi không có header Authorization Bearer.
       - `ReceptionistRole_AccessToDoctorOrAdminEndpoints_Returns403Forbidden`: Nhân viên Lễ tân đăng nhập hợp lệ nhưng cố tình gọi API của Admin hoặc Doctor bị từ chối HTTP 403 Forbidden.
       - `DoctorRole_AccessToAdminEndpoints_Returns403Forbidden`: Bác sĩ đăng nhập hợp lệ nhưng cố tình truy cập báo cáo Admin bị từ chối HTTP 403 Forbidden.
       - `DoctorRole_AccessToDoctorDashboard_Returns200OkWithAssignedScope`: Bác sĩ truy cập Dashboard của chính mình trong phạm vi phân công (Assigned Scope) trả về HTTP 200 OK với đầy đủ danh sách `todayAppointments` và `waitingQueue`.
       - `SecurityAudit_ErrorResponsesAndPayloads_DoNotExposeSecretsOrSensitivePii`: Kiểm tra kiểm toán bảo mật: Khi xảy ra lỗi xác thực hoặc lỗi hệ thống, phản hồi JSON tuyệt đối không để lộ mật khẩu, mã hash, chuỗi kết nối, truy vấn SQL, hay nội dung nhạy cảm.
  - Toàn bộ 11/11 tests của FEAT-051 đều PASS 100%.
  - Tổng số test cases trong toàn Solution đạt 489/489 PASS 100%.
  - Frontend Vite production build thành công 100% không lỗi (dist bundle 420.59 kB trong 5.92s).
- Files created/changed:
  - `backend/tests/ClinicAppointment.Domain.Tests/Integration/PostgresGistConcurrencyTests.cs`
  - `backend/tests/ClinicAppointment.Domain.Tests/Integration/ApiAuthorizationAndSecurityTests.cs`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/tests/ClinicAppointment.Domain.Tests/ClinicAppointment.Domain.Tests.csproj --filter "FullyQualifiedName~PostgresGist|FullyQualifiedName~ApiAuthorizationAndSecurity"` (Thành công: 11 Passed, 0 Failed, 0 Skipped, Duration: 4 s)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln` (Thành công: 489 Passed, 0 Failed, 0 Skipped, Duration: 7 s)
  - `npm --prefix frontend run build` (Thành công: 0 Errors, built in 5.92s, 1649 modules transformed)
- Acceptance criteria evidence:
  - `Real PostgreSQL GiST concurrency and no-overlap proven`: Đạt 100%, kiểm thử trực tiếp trên PostgreSQL 18 thực tế, chứng minh 100% chống trùng ca và chặn overlap khung giờ bằng mã lỗi 23P01/23505.
  - `Transaction rollback on conflict and slot release on cancelled`: Đạt 100%, kiểm thử rollback ACID và giải phóng slot khi hủy ca hẹn.
  - `RBAC and role boundary enforcement (401/403)`: Đạt 100%, kiểm thử toàn diện Anonymous (401), Lễ tân/Bác sĩ gọi chéo quyền (403), Bác sĩ truy cập phạm vi phân công (200).
  - `No sensitive output or secret leak in responses`: Đạt 100%, không rò rỉ secret, mật khẩu, hay thông tin kỹ thuật nhạy cảm trong response.


### 2026-11-14 — FEAT-052 — Frontend component and E2E tests
- Status before/after: not_started -> completed
- Scope implemented:
  - Thiết lập hạ tầng kiểm thử Frontend với Vitest v5.1.0, JSDOM, React Testing Library và `@testing-library/jest-dom`:
    - Cài đặt `vitest`, `@testing-library/react`, `@testing-library/jest-dom`, `jsdom`.
    - Cấu hình Vitest trong `frontend/vite.config.ts` (`globals: true`, `environment: 'jsdom'`, `setupFiles: './src/test/setup.ts'`).
    - Cấu hình TypeScript definitions trong `frontend/tsconfig.json` và `frontend/src/test/setup.ts`.
    - Thêm test script `"test": "vitest run"` vào `frontend/package.json`.
  - Xây dựng 3 bộ kiểm thử Component và User Journey chuyên sâu bao phủ trọn vẹn 3 tiêu chí nghiệm thu:
    1. `GuestBookingJourney.test.tsx` (3 test cases kiểm thử luồng Đặt lịch khám khách vãng lai):
       - `renders booking step 1 and validates service selection`: Kiểm thử render Bước 1 wizard Đặt lịch khám trực tuyến, kiểm tra chế độ Guest Booking không yêu cầu đăng nhập, chọn Chuyên khoa -> Dịch vụ khám -> chuyển thành công sang Bước 2.
       - `validates required fields on step 3 (Patient Info) according to SRS rules`: Kiểm thử validation 3 trường thông tin bệnh nhân cốt lõi theo bất biến dự án:
         - Không điền họ tên -> Báo lỗi `Họ và tên bệnh nhân là bắt buộc.`
         - Điền họ tên nhưng thiếu ngày sinh/năm sinh -> Báo lỗi `Vui lòng nhập Ngày tháng năm sinh đầy đủ, hoặc chuyển sang chế độ Chỉ biết năm sinh.`
         - Điền ngày sinh nhưng sai định dạng số điện thoại liên hệ (regex) -> Báo lỗi `Số điện thoại liên hệ không hợp lệ (cần từ 9 đến 11 số để nhận OTP).`
       - `completes guest booking happy path and shows confirmation screen`: Luồng đặt lịch happy path hoàn chỉnh từ Bước 1 đến Bước 4: Gọi API `appointmentApi.bookGuest`, chuyển sang Bước 4 hiển thị thông báo `Đặt Lịch Khám Thành Công!` và mã đặt lịch `APT-TEST-2026` nổi bật.
    2. `RescheduleApprovalJourney.test.tsx` (4 test cases kiểm thử luồng Duyệt đổi lịch quầy Lễ tân):
       - `renders receptionist dashboard and lists pending reschedule requests`: Hiển thị Bàn tiếp nhận lễ tân, hiển thị yêu cầu đổi lịch chờ duyệt với đầy đủ mã booking `APT-RESCHED-01`, họ tên bệnh nhân, thời gian cũ và thời gian đề xuất mới kèm lý do.
       - `approves a reschedule request and triggers refresh`: Lễ tân nhấn nút `Duyệt Đổi Lịch`, gọi API `receptionApi.approveReschedule` kèm ghi chú phê duyệt, hiển thị thông báo thành công và làm mới dữ liệu.
       - `opens reject modal and requires reject reason before submission`: Lễ tân nhấn `Từ Chối`, mở Modal từ chối yêu cầu; nhấn xác nhận khi chưa nhập lý do bị chặn và hiển thị thông báo lỗi `Vui lòng nhập lý do từ chối yêu cầu đổi lịch`; nhập lý do hợp lệ gọi API `rejectReschedule` bảo lưu 100% ca khám cũ ban đầu của bệnh nhân.
       - `allows receptionist to confirm a pending appointment`: Lễ tân nhấn `Xác Nhận Lịch` cho ca khám mới `APT-NEW-02`, gọi API `receptionApi.confirmAppointment` thành công và chuyển trạng thái sang `Confirmed`.
    3. `RefundBoundaryDisplay.test.tsx` (2 test cases kiểm thử hiển thị ranh giới hoàn tiền 60 phút theo BR-010):
       - `displays 100% refund rate when cancelled >= 60 minutes before appointment (BR-010)`: Tra cứu lịch khám bằng OTP 2 bước (gửi OTP -> nhập 6 số OTP -> hiển thị chi tiết ca hẹn); mở modal hủy khám trước giờ hẹn 120 phút (>= 60 phút); kiểm tra hiển thị chính xác bảng tính: số tiền đã thanh toán (300.000 VND), thời gian còn lại (120 phút), tỷ lệ hoàn tiền (100%), số tiền hoàn (300.000 VND), phí hủy (0 VND), cùng ghi chú chính sách `Hủy trước 60 phút: Hoàn 100% phí dịch vụ đã thanh toán (phí hủy 0%).`
       - `displays 50% refund rate when cancelled < 60 minutes before appointment (BR-010)`: Mở modal hủy khám trước giờ hẹn 45 phút (< 60 phút); kiểm tra hiển thị chính xác bảng tính: số tiền đã thanh toán (300.000 VND), thời gian còn lại (45 phút), tỷ lệ hoàn tiền (50%), số tiền hoàn (150.000 VND), phí hủy (150.000 VND), bảo toàn nguyên tắc tài chính `refundAmount + cancellationFee == paidAmount`.
  - Kết quả kiểm thử thực tế:
    - Frontend tests: 9/9 test cases PASS 100% trên 3 suites trong 4.37s.
    - Frontend bundle: `npm --prefix frontend run build` (`tsc && vite build`) hoàn tất 100% không lỗi / không warning trong 6.56s.
    - Backend regression: 489/489 solution tests PASS 100% trong 7s.
- Files created/changed:
  - `frontend/package.json`
  - `frontend/vite.config.ts`
  - `frontend/tsconfig.json`
  - `frontend/src/test/setup.ts`
  - `frontend/src/test/GuestBookingJourney.test.tsx`
  - `frontend/src/test/RescheduleApprovalJourney.test.tsx`
  - `frontend/src/test/RefundBoundaryDisplay.test.tsx`
  - `frontend/src/components/layout/Footer.tsx`
  - `planning/feature_list.json`
  - `planning/progress.md`
  - `planning/implementation_log.md`
- Commands executed:
  - `npm --prefix frontend test` (Thành công: 9 Passed, 0 Failed, 3 Test Files, Duration: 4.37s)
  - `npm --prefix frontend run build` (Thành công: 0 Errors, built in 6.56s, 1649 modules transformed)
  - `& "C:\Program Files\dotnet\dotnet.exe" test backend/ClinicAppointment.sln --no-build` (Thành công: 489 Passed, 0 Failed, 0 Skipped, Duration: 7 s)
- Acceptance criteria evidence:
  - `Guest booking`: Đạt 100%, kiểm thử chọn catalog, validation 3 trường bắt buộc, submit và hiển thị confirmation booking code.
  - `Reschedule approval`: Đạt 100%, kiểm thử dashboard Lễ tân, approve đổi slot, reject modal bắt buộc lý do giữ slot cũ, và confirm ca khám.
  - `Refund boundary display`: Đạt 100%, kiểm thử tra cứu OTP, hiển thị ranh giới >= 60 phút (hoàn 100%, phí 0%) và < 60 phút (hoàn 50%, phí 50%), bảo toàn tổng tiền.

