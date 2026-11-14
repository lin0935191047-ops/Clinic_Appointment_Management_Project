# Project Progress

- Baseline: 13/11/2026
- Last updated: 14/11/2026
- Overall status: In progress (FEAT-001 through FEAT-016 Completed)
- Current phase: Doctor Management (FEAT-017)

## Summary
Nguồn trạng thái Feature là `feature_list.json`. Không suy ra tiến độ lập trình từ việc tạo tài liệu.

## Features Completed (FEAT-001 -> FEAT-016)
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

## Next steps
1. Triển khai FEAT-017 (Doctor management: Quản lý hồ sơ bác sĩ và staff account).
2. Triển khai FEAT-018 (Service management: Quản lý dịch vụ khám và giá).

## Rules
Không tạo Database/Migration hoặc đánh dấu Feature completed chỉ vì đã sinh tài liệu. `completed` yêu cầu acceptance criteria và test evidence thực tế.
