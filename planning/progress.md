# Project Progress

- Baseline: 13/11/2026
- Last updated: 14/11/2026
- Overall status: In progress (FEAT-001 through FEAT-010 Completed)
- Current phase: Patient Profile & Identity Resolution (FEAT-011, FEAT-012)

## Summary
Nguồn trạng thái Feature là `feature_list.json`. Không suy ra tiến độ lập trình từ việc tạo tài liệu.

## Features Completed (FEAT-001 -> FEAT-010)
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

## Next steps
1. Triển khai FEAT-011 (Patient profile độc lập User và các trường định danh bắt buộc).
2. Triển khai FEAT-012 (Patient contacts và mã hóa số điện thoại / PII).

## Rules
Không tạo Database/Migration hoặc đánh dấu Feature completed chỉ vì đã sinh tài liệu. `completed` yêu cầu acceptance criteria và test evidence thực tế.
