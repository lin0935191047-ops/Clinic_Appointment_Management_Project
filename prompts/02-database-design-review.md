# Prompt — Database Design Review

Dùng `/efcore-postgresql` và đọc SPEC, BUSINESS_RULES, ARCHITECTURE, DATABASE_DESIGN, ADR.
Read `DATA_DICTIONARY.md`, `ERD.md`, `DATABASE_REVIEW_CHECKLIST.md` and `DATABASE_IMPLEMENTATION_READINESS.md` too. Review logical schema, ERD, key/FK/cardinality, indexes, check/unique constraints, patient identifier encryption/HMAC, cancellation/refund ledger, OTP verifier HMAC/pepper and scoped grants, reschedule approval, cross-table composite FK integrity, audit and PostgreSQL no-overlap exclusion constraint.
Tạo bản review/ERD đề xuất, ghi mọi assumption/TBD, không tạo migration hoặc kết nối DB. Không thay đổi policy nghiệp vụ.
