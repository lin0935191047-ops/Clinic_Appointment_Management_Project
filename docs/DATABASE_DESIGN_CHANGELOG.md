# Database Design Changelog

## Version 1.2 — 13/11/2026

- Hoàn thiện thiết kế logic 30 bảng PostgreSQL và từ điển cột.
- Bổ sung ERD theo 4 domain và ảnh PNG preview.
- Bổ sung composite FK scope để Patient, booking contact, OTP challenge/grant, Doctor-Service và Consultation không thể liên kết chéo sai hồ sơ.
- OTP verifier chuyển sang HMAC/pepper vì OTP có entropy thấp; grant token có entropy cao được lưu dạng hash.
- Mỗi refund record gắn một payment nguồn; policy refund + cancellation fee phải bằng 100%.
- Bổ sung constraint cho personal-owner verified phone, identity review và merge event.
- Bổ sung review checklist và readiness gate trước EF Core Migration.

**Lưu ý:** Đây vẫn là Logical Database Design. Chưa tạo PostgreSQL Database, EF Core entity, migration hoặc dữ liệu seed.
