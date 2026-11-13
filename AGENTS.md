# AGENTS.md — AI Agent Guidelines & Operating Manual

> **Purpose:** File này chứa các nguyên tắc làm việc cốt lõi, vai trò và quy trình bắt buộc dành cho mọi AI Agent (Antigravity, Claude, Cursor, Copilot, ChatGPT...) khi tham gia phát triển dự án **Clinic-Appointment-Management**.

---

## 1. Tổng quan hệ thống (System Context)

Dự án là hệ thống quản lý đặt lịch khám bệnh trực tuyến cho phòng khám đa khoa / chuyên khoa:
- **Backend:** ASP.NET Core 8+ Web API theo cấu trúc Clean Architecture.
- **ORM & CSDL:** Entity Framework Core với PostgreSQL.
- **Frontend:** React 18+ (hoặc Next.js / Vite SPA) với TypeScript, Tailwind CSS, TanStack Query.
- **Nghiệp vụ trọng tâm:** Quản lý lịch hẹn khám, ngăn chặn đặt lịch trùng lặp, hỗ trợ đặt lịch cho khách vãng lai (Guest Booking) và tài khoản đã đăng ký, hỗ trợ bác sĩ và lễ tân quản lý luồng khám.

---

## 2. Các nguyên tắc bắt buộc cho AI Agents (Non-Negotiable Rules)

1. **Tuân thủ Clean Architecture & Separation of Concerns:**
   - Không đặt logic nghiệp vụ trong Controller hoặc UI Components.
   - Domain Model và Business Rules là trung tâm, độc lập với cơ chế lưu trữ hay framework hiển thị.
2. **Quy tắc Nghiệp vụ (Business Rules) là tối thượng:**
   - Trước khi tạo/sửa đổi API hoặc logic liên quan đến đặt lịch, bác sĩ, khung giờ khám, BẮT BUỘC đọc và tuân thủ [docs/BUSINESS_RULES.md](docs/BUSINESS_RULES.md).
   - Tuyệt đối không để xảy ra tình trạng Double-Booking (2 bệnh nhân cùng đặt 1 bác sĩ trong cùng 1 khung giờ).
3. **Đồng bộ hóa tài liệu (Documentation Sync):**
   - Khi có thay đổi về schema DB -> Cập nhật [docs/DATABASE_DESIGN.md](docs/DATABASE_DESIGN.md) và [planning/implementation_log.md](planning/implementation_log.md).
   - Khi có thay đổi về Endpoint API -> Cập nhật [docs/API_SPECIFICATION.md](docs/API_SPECIFICATION.md).
   - Khi hoàn thành hoặc thay đổi phạm vi tính năng -> Cập nhật [planning/progress.md](planning/progress.md) và [planning/feature_list.json](planning/feature_list.json).
4. **Kiểm thử tự động (Testing First Mindset):**
   - Mọi use case nghiệp vụ (đặt lịch, hủy lịch, chuyển trạng thái, tính slot) đều phải có Unit Test và Integration Test tương ứng.
5. **Tham chiếu Rules & Skills:**
   - Khi viết mã nguồn C#/.NET: đọc [.agents/rules/backend-rules.md](.agents/rules/backend-rules.md).
   - Khi viết mã nguồn React/TS: đọc [.agents/rules/frontend-rules.md](.agents/rules/frontend-rules.md).
   - Khi viết Migration / EF Core: đọc [.agents/rules/database-rules.md](.agents/rules/database-rules.md).
   - Khi thực hiện các tác vụ chuyên biệt: đọc kỹ SKILL tương ứng trong thư mục [.agents/skills/](.agents/skills/).

---

## 3. Bản đồ vai trò & Kỹ năng (Role & Skill Mapping)

| Tác vụ cần thực hiện | Tài liệu & Kỹ năng cần nạp | File quy chuẩn liên quan |
| :--- | :--- | :--- |
| Lập kế hoạch, cập nhật tiến độ | `.agents/skills/project-planning/SKILL.md` | `planning/progress.md`, `planning/feature_list.json` |
| Phát triển API, Service, Middleware | `.agents/skills/dotnet-backend/SKILL.md` | `.agents/rules/backend-rules.md`, `docs/ARCHITECTURE.md` |
| Xây dựng UI, Component, Client Hook | `.agents/skills/react-frontend/SKILL.md` | `.agents/rules/frontend-rules.md` |
| Tạo Migration, Tối ưu Query, DB Index | `.agents/skills/efcore-postgresql/SKILL.md` | `.agents/rules/database-rules.md`, `docs/DATABASE_DESIGN.md` |
| Xử lý thuật toán chia ca khám, chống trùng | `.agents/skills/clinic-business-logic/SKILL.md` | `docs/BUSINESS_RULES.md` |
| Viết Unit/Integration Test | `.agents/skills/testing-qa/SKILL.md` | `docs/TESTING_STRATEGY.md` |
| Cập nhật log triển khai & ADR | `.agents/skills/documentation-sync/SKILL.md` | `docs/adr/`, `planning/implementation_log.md` |

---

## 4. Quy trình xử lý yêu cầu tiêu chuẩn (Standard Workflow)

Khi nhận một User Request:
1. **Phân tích (Understand & Trace):** Đọc các tài liệu thiết kế liên quan (`docs/SPEC.md`, `docs/BUSINESS_RULES.md`, v.v.).
2. **Kế hoạch (Plan):** Xác định các file cần thay đổi, kiểm tra tác động chéo (breaking changes).
3. **Thực hiện (Execute):**
   - Viết code sạch, kèm type safety và logging/exception handling chuẩn.
   - Tuân thủ coding standard đã đề ra trong `.agents/rules/`.
4. **Kiểm tra & Ghi nhận (Verify & Record):**
   - Chạy test / build kiểm tra cú pháp.
   - Cập nhật nhật ký triển khai vào `planning/implementation_log.md`.

