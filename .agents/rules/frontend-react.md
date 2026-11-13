---
trigger: glob
globs: "frontend/**/*.js, frontend/**/*.jsx, frontend/**/*.ts, frontend/**/*.tsx"
description: React conventions, API integration, accessibility and privacy rules.
---
# Frontend rules
- Chia component theo feature; tránh component khổng lồ.
- Dùng shared API client; có loading/empty/error/success states.
- Guest booking không redirect đến login.
- Client validation hỗ trợ UX; Backend vẫn là nguồn quyết định.
- Không lưu OTP/full identifiers/health notes/secrets trong localStorage.
- Không hiển thị hồ sơ/bệnh án ngoài quyền.
- Form có label, lỗi rõ, keyboard accessibility và responsive layout.
