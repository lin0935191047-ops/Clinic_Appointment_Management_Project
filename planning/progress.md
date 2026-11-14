# Project Progress

- Baseline: 13/11/2026
- Last updated: 14/11/2026
- Overall status: In progress (FEAT-001 through FEAT-023 Completed)
- Current phase: Booking idempotency and abuse protection (FEAT-024)

## Summary
Nguồn trạng thái Feature là `feature_list.json`. Không suy ra tiến độ lập trình từ việc tạo tài liệu.

## Features Completed (FEAT-001 -> FEAT-023)
- `FEAT-001` (Repository & Harness baseline): AGENTS.md, rules, skills, HARNESS_AUDIT 14/14 PASS.
- `FEAT-002` (SRS & workflow baseline): docs/SPEC.md v1.2, docs/BUSINESS_RULES.md, use case / activity diagrams.
- `FEAT-003` (Architecture & API design): docs/ARCHITECTURE.md, docs/API_SPECIFICATION.md, 3 ADRs.
- `FEAT-004` (Database design): docs/DATABASE_DESIGN.md v1.2, docs/DATA_DICTIONARY.md, docs/ERD.md, GiST reference SQL.
- `FEAT-005` (Development environment): Node.js v24.18.0, npm 11.16.0, .NET SDK 10.0.404 tại `C:\Program Files\dotnet\dotnet.exe`, PostgreSQL 18 service listening on port 5432.
- `FEAT-006` (ASP.NET Core API bootstrap): Solution Clean Architecture biên dịch thành công 0 warning / 0 error; 10/10 Domain unit tests pass; `/health` endpoint HTTP 200 OK.
- `FEAT-007` (React app bootstrap): React 18 SPA + Vite + TypeScript + Tailwind CSS; luồng Guest Booking 4 bước không cần tài khoản; Tra cứu OTP và tính hoàn phí 60 phút; Kênh Lễ tân; `npm run build` pass 100%.
- `FEAT-008` (PostgreSQL and EF Core connection): Npgsql snake-case convention, DbContext DI, cấu hình chuỗi kết nối an toàn ngoài source.
- `FEAT-009` (Initial migrations and seed): EF Core migration `20261114062800_InitialCreate.cs` kèm btree_gist GiST exclusion constraint chống trùng ca; kịch bản SQL idempotent `database/migrations/20261114_initial_schema.sql`; `DatabaseSeeder.cs` idempotent.
- `FEAT-010` (Users and roles): Thực thể User, Role, UserRoleAssignment, RefreshToken; 4 vai trò ADMIN, DOCTOR, RECEPTIONIST, PATIENT; RoleAssignmentPolicy; PasswordHasher (PBKDF2 HMAC-SHA256); 21/21 unit tests PASS.
- `FEAT-011` (Patient profile): Patient độc lập User (UserId nullable unique); FullName bắt buộc + NormalizedFullName; Ràng buộc DateOfBirth hoặc BirthYear; Candidate Duplicate Detection (Strictly NO Auto-merge); CQRS CreatePatientCommand/Queries/PatientsController; 39/39 tests PASS.
- `FEAT-012` (Patient identifier encryption and lookup): Mã hóa xác thực AES-256-GCM; Keyed HMAC-SHA256 trên chuỗi chuẩn hóa cho exact search; Che giấu số điện thoại/CCCD/hộ chiếu/BHYT không log thô; Xử lý trùng định danh mạnh bảo đảm không auto-merge; 59/59 tests PASS.
- `FEAT-013` (Patient duplicate detection/review): Candidate matching (NAME_DOB, NAME_BIRTH_YEAR, VERIFIED_IDENTIFIER, SHARED_PHONE_DIFFERENT_NAME); Không auto-merge khi trùng Name+DOB; Manual review workflow bởi Receptionist/Admin; Quy trình Merge có snapshot JSON & audit log bất biến; EF migration 20261114070558_AddPatientReviewAndMerge; 69/69 unit tests PASS.
- `FEAT-014` (Authentication and password management): Băm mật khẩu PBKDF2 HMAC-SHA256; Cấp phát JWT Access Token & Refresh Token (Rotation, Revocation); Đăng ký Patient tùy chọn tự động gán role/profile/rà soát trùng; Login, Logout, ChangePassword, Forgot/Reset password, CurrentUser endpoints; Swagger Bearer auth; 79/79 unit tests PASS.
- `FEAT-015` (Role/resource authorization): Enforce RBAC & Resource-based authorization; CurrentUserService; ResourceAuthorizationService; Patient chỉ xem own data; Doctor chỉ xem assigned appointments; Ngăn chặn triệt để lỗ hổng IDOR (403 Forbidden); ASP.NET Core Policies: AdminOnly, StaffOnly, DoctorOnly, PatientOnly; 91/91 unit tests PASS.
- `FEAT-016` (Specialty management): SlugHelper sinh slug tiếng Việt chuẩn; Ràng buộc Code và Slug unique; Public catalog chỉ trả về IsActive == true (FR-002); Admin CRUD chuyên khoa, quản lý trạng thái IsActive; Soft-delete / Deactivation bảo vệ dữ liệu liên kết; 105/105 unit tests PASS.
- `FEAT-017` (Doctor management): Liên kết User tài khoản và Role DOCTOR; Ràng buộc DoctorCode unique; Kiểm tra SpecialtyId hợp lệ; Public catalog doctor view chỉ hiển thị bác sĩ và chuyên khoa đang hoạt động; Lọc bác sĩ theo chuyên khoa (SpecialtySlug); Deactivation bảo toàn lịch sử y tế; 116/116 unit tests PASS.
- `FEAT-018` (Service management): Quản lý danh mục dịch vụ khám bệnh; Ràng buộc DurationMinutes > 0 và Price >= 0 (Domain Entity + FluentValidation); Ràng buộc Code unique; Tiền tệ mặc định 'VND'; Public catalog chỉ trả về dịch vụ Active của chuyên khoa cha Active (FR-004); Lọc theo Chuyên khoa / Slug; Bảo toàn Appointment snapshots với cơ chế Deactivation khi đã có ca khám; 127/127 unit tests PASS.
- `FEAT-019` (Doctor working rules): Quản lý lịch làm việc định kỳ của Bác sĩ; Ràng buộc StartTime < EndTime (TimeOnly); Ràng buộc DayOfWeek 1..7 (ISO 8601); Ràng buộc EffectiveFrom <= EffectiveTo (DateOnly); Chống trùng lặp/chồng lấn ca (No-overlap) trên cùng bác sĩ; Hỗ trợ ca sáng/chiều độc lập; EF Core migration AddDoctorScheduleEffectiveDates; 139/139 unit tests PASS.
- `FEAT-020` (Doctor time off and breaks): Quản lý ngày nghỉ, giờ nghỉ, thời gian bận (DoctorTimeOff & DoctorScheduleBlock); Bất biến không book overlap time off (FR-035); Bất biến schedule change không âm thầm xóa appointments (FR-055); Cảnh báo và bảo toàn nguyên vẹn appointments của bệnh nhân; EF Core migration AddDoctorTimeOffsAndScheduleBlocks; 148/148 unit tests PASS.
- `FEAT-021` (Availability calculation): Thuật toán tính khung giờ khả dụng cho Bác sĩ; Chia slot theo thời lượng dịch vụ DurationMinutes; Không hiển thị ngoài lịch làm việc; Tự động loại trừ ngày nghỉ DoctorTimeOff, khung giờ bận DoctorScheduleBlock, và các ca khám đang hoạt động Appointments; Giải phóng slot cho lịch hẹn đã hủy Cancelled; Bảo mật tuyệt đối không lộ thông tin bệnh nhân; Endpoint công khai GET /api/v1/availability; 158/158 unit tests PASS.
- `FEAT-022` (PostgreSQL no-overlap constraint): GiST Exclusion constraint (ex_appointments_doctor_no_overlap) trên appointments(doctor_id, tstzrange(start_at, end_at, '[)')) WHERE (status <> 'Cancelled') bảo vệ tầng cuối ở PostgreSQL; Chống race condition đặt trùng ca đồng thời; ExceptionHandlingMiddleware ánh xạ chuẩn xác ngoại lệ vi phạm constraint và AppointmentConflictException thành HTTP 409 Conflict; 168/168 unit tests PASS.
- `FEAT-023` (Guest booking API): Tạo lịch khám công khai không yêu cầu tài khoản/JWT ([AllowAnonymous]); Hồ sơ bệnh nhân độc lập (UserId = null); Strictly NO Auto-merge: quét ứng viên trùng lặp mở PatientIdentityReviewCase trạng thái OPEN để lễ tân đối soát tại quầy; Sinh BookingCode ngẫu nhiên duy nhất khó đoán (format APT-yyyyMMdd-XXXXXX); Lưu snapshot bất biến của dịch vụ (tên, giá, thời lượng); Kiểm tra Availability và ca làm việc; Bọc trong Database Transaction với rollback khi lỗi; 179/179 unit tests PASS.

## Next steps
1. Triển khai FEAT-024 (Booking idempotency and abuse protection: Chống retry trùng lặp và lạm dụng đặt lịch).
2. Triển khai FEAT-025 (Guest OTP issuance and verification: Cấp phát và xác thực OTP tra cứu, đổi lịch, hủy lịch).

## Rules
Không tạo Database/Migration hoặc đánh dấu Feature completed chỉ vì đã sinh tài liệu. `completed` yêu cầu acceptance criteria và test evidence thực tế.
