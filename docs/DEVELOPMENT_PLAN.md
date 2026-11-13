# DEVELOPMENT PLAN

## Delivery phases
| Phase | Nội dung | Exit criteria |
|---|---|---|
| 0 | Harness baseline | Rules/Skills/docs/feature tracking audit pass |
| 1 | Business analysis | SRS, actors, use cases, activity diagrams, TBD review |
| 2 | System design | Architecture, ERD/DB, API contract, ADR reviewed |
| 3 | Foundation | .NET API, React app, config, health endpoint |
| 4 | Database | EF Core, PostgreSQL, migrations, seed |
| 5 | Identity/security | User, role, auth, authorization, OTP infrastructure |
| 6 | Catalog/schedule | Specialty, Doctor, Service, working hours, time off |
| 7 | Guest booking | Availability, patient matching, booking, no-overlap |
| 8 | Lookup and OTP | OTP purpose flows, scoped grants, lookup |
| 9 | Reschedule/cancel | Receptionist approval, refund policy and tracking |
| 10 | Reception/consultation | Confirm, check-in, queue, consultation, complete |
| 11 | Frontend integration | Guest + staff portals and API integration |
| 12 | QA/hardening | Security, concurrency, accessibility, regression |
| 13 | Deployment/docs | Environment, backup, README, operation guide |

## Feature protocol
1. Chọn một Feature có dependencies hoàn thành.
2. Đọc acceptance criteria và tài liệu liên quan.
3. Agent trình bày plan + file dự kiến sửa.
4. User review nếu có thay đổi schema/security/business.
5. Implement Feature và tests.
6. Chạy build/test thực tế.
7. Cập nhật status/evidence.
8. Cập nhật docs/ADR nếu API/schema/architecture/business thay đổi.

## Beginner-friendly delivery
Mỗi hướng dẫn cần có: mục tiêu, điều kiện trước, đường dẫn file, lệnh PowerShell, code, giải thích, cách chạy, expected output, cách kiểm tra, lỗi thường gặp và checklist.
