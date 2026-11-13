# Harness Audit Report

- **Date:** 2026-11-13
- **Reviewer:** Antigravity AI Assistant
- **Harness Version:** 2.1-db-design-erd-v1.2
- **Result:** **READY**

---

## 1. Bảng Đánh giá Hạng mục Kiểm tra (Audit Checklist)

| Check | PASS/FAIL | Evidence |
|---|---|---|
| Root `AGENTS.md` and invariants | **PASS** | `AGENTS.md` và `.agents/rules/00-project-invariants.md` bảo đảm các nguyên tắc bất biến (invariants) về Clean Architecture, bảo mật, và quy tắc nghiệp vụ. |
| Skills use `.agents/skills/<name>/SKILL.md` | **PASS** | Cả 7 skills (`clinic-business-logic`, `dotnet-backend`, `react-frontend`, `efcore-postgresql`, `project-planning`, `testing-qa`, `documentation-sync`) đều có tệp `SKILL.md` chuẩn YAML frontmatter. |
| Rules have valid trigger frontmatter | **PASS** | Tất cả các tệp rules trong `.agents/rules/` (`00-project-invariants.md`, `backend-csharp.md`, `database-postgresql.md`, `frontend-react.md`, `docs-and-feature-tracking.md`) đều có `trigger` và `description` hợp lệ. |
| SPEC has confirmed policies | **PASS** | [docs/SPEC.md](SPEC.md) v1.2 chốt rõ các chính sách cốt lõi và 12 điểm TBD; Agent không tự biến TBD thành yêu cầu bắt buộc. |
| Patient may exist without User | **PASS** | [docs/DATA_DICTIONARY.md](DATA_DICTIONARY.md) và `constraints-reference.sql` hỗ trợ `patients.user_id = NULL` kèm partial unique index `ux_patients_user_id_not_null`. |
| OTP purpose/scoping defined | **PASS** | Phân định rõ 3 mục đích (`LOOKUP`, `RESCHEDULE`, `CANCEL`) qua bảng `otp_challenges` và `otp_verification_grants`. |
| Reschedule approval + old slot retention defined | **PASS** | [docs/adr/0003-reschedule-approval.md](adr/0003-reschedule-approval.md) và [docs/adr/0009-reschedule-slot-transaction.md](adr/0009-reschedule-slot-transaction.md): Yêu cầu đổi lịch ở trạng thái `PendingReview` giữ nguyên slot cũ; Lễ tân duyệt trong CSDL transaction. |
| Cancellation 60/59 boundary defined | **PASS** | [docs/adr/0004-cancellation-refund-policy.md](adr/0004-cancellation-refund-policy.md) và [docs/BUSINESS_RULES.md](BUSINESS_RULES.md): $\ge 60$ phút hoàn 100%, $< 60$ phút hoàn 50%, không cần Lễ tân duyệt. |
| Refund integration separated from calculation | **PASS** | Logic tính toán số tiền hoàn dựa trên snapshot phí dịch vụ và số tiền đã thanh toán; trạng thái hoàn tiền `Pending`/`Processing`/`Succeeded`/`Failed` tách biệt với cổng thanh toán (TBD). |
| Patient matching avoids unsafe auto-merge | **PASS** | [docs/adr/0005-patient-identity-resolution.md](adr/0005-patient-identity-resolution.md): `FullName + DOB/BirthYear` chỉ tạo hồ sơ nghi trùng (`is_possible_duplicate = true`); tuyệt đối không auto-merge. Mã hóa PII và dùng `Keyed HMAC`. |
| DB overlap protection/test planned | **PASS** | [docs/adr/0006-postgresql-overlap-constraint.md](adr/0006-postgresql-overlap-constraint.md) và `constraints-reference.sql`: GiST Exclusion Constraint `ex_appointments_doctor_no_overlap` dùng `tstzrange` trên PostgreSQL thật. |
| API covers Guest and staff workflows | **PASS** | [docs/API_SPECIFICATION.md](API_SPECIFICATION.md) đặc tả chi tiết các endpoints cho Guest, Patient, Receptionist, Doctor và Admin. |
| Feature IDs/dependencies/schema valid | **PASS** | [planning/feature_list.json](../planning/feature_list.json) gồm 55 features, đã được kiểm chứng parse hợp lệ 100% theo [planning/feature_schema.json](../planning/feature_schema.json). |
| No Feature falsely completed | **PASS** | 55/55 features đều ở trạng thái `not_started`; không có tính năng nào bị đánh dấu hoàn thành khi chưa có bằng chứng kiểm thử thực tế. |

---

## 2. Các phát hiện & Hoàn thiện (Findings)

1. **Kiến trúc CSDL Logic 30 Bảng (Enterprise Level):**
   - Đã được chuẩn hóa qua [docs/DATA_DICTIONARY.md](DATA_DICTIONARY.md), [docs/ERD.md](ERD.md), và các sơ đồ trực quan trong `diagrams/` (`ERD_01_IDENTITY.png`, `ERD_02_CATALOG_SCHEDULING.png`, `ERD_03_APPOINTMENTS_FINANCE.png`, `ERD_04_OTP_OPERATIONS.png`, `ERD_OVERVIEW.png`).
2. **Toàn vẹn Phạm vi Đặt lịch Đa bảng (Cross-Table Scope Integrity):**
   - Đã được định nghĩa trong [docs/adr/0011-cross-table-scope-integrity.md](adr/0011-cross-table-scope-integrity.md) và `database/reference/constraints-reference.sql` nhằm bảo đảm Contact, OTP challenge, OTP grant, History, Cancellation và Consultation luôn thuộc về cùng một Patient/Doctor trên Appointment.
3. **Ma trận Truy xuất Yêu cầu (Requirements Traceability Matrix):**
   - [docs/REQUIREMENTS_TRACEABILITY.md](REQUIREMENTS_TRACEABILITY.md) kết nối hoàn chỉnh 67 yêu cầu chức năng (`FR-001` đến `FR-067`) tới 55 mã Feature và các bộ kiểm thử tương ứng.

---

## 3. Không có Blocker kỹ thuật (Blockers: None)

Mọi yêu cầu nghiệp vụ, luồng xử lý và tài liệu thiết kế đều khớp nhau hoàn hảo giữa SRS, Business Rules, Use Cases, Activity Diagrams, ERD, Data Dictionary, API Spec và Feature List.

---

## 4. Quyết định Nghiệm thu Cuối cùng (Final Decision)

**READY** — Toàn bộ Harness, tài liệu nghiệp vụ và thiết kế cơ sở dữ liệu đã vượt qua 14/14 tiêu chí kiểm tra bắt buộc, sẵn sàng 100% để bước vào triển khai mã nguồn Backend (.NET 8 Clean Architecture) và Frontend (React.js).

