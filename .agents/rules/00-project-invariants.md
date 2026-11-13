---
trigger: always_on
description: Project-wide invariant rules for Clinic Appointment Management System.
---
# Project invariants
- Đọc root `AGENTS.md`.
- Không đổi nghiệp vụ trong `docs/SPEC.md` và `docs/BUSINESS_RULES.md` nếu chưa được duyệt.
- Guest booking không yêu cầu đăng nhập; Patient có thể tồn tại không có User.
- Không auto-merge Patient chỉ dựa FullName + DOB/BirthYear.
- OTP bắt buộc cho Guest lookup/reschedule/cancel.
- Reschedule phải được Receptionist duyệt; old appointment giữ nguyên trước approve.
- Hủy >=60 phút (kể cả đúng 60) hoàn 100%; dưới 60 phút hoàn 50%.
- Không mark Feature completed khi thiếu test evidence.
