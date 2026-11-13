---
trigger: model_decision
description: Use when editing specifications, architecture, API, database docs, ADRs or feature tracking.
---
# Documentation and feature tracking rules
- Giữ nhất quán SPEC, BUSINESS_RULES, ARCHITECTURE, DATABASE_DESIGN, API_SPECIFICATION.
- Đánh TBD rõ; không tự quyết vấn đề người dùng chưa chốt.
- Feature ID không tái sử dụng; dependencies phải tồn tại.
- Chỉ completed khi acceptance criteria đạt và test_evidence có tham chiếu thật.
- Thay đổi API/schema/business rule phải cập nhật tài liệu phụ thuộc và ADR nếu cần.
