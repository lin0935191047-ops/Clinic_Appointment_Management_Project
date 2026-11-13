# Project Progress Tracker (Theo dõi Tiến độ Dự án)

> **Cập nhật lần cuối:** 2026-11-13  
> **Trạng thái tổng thể:** Giai đoạn 0 (Khởi tạo Khung & Đồng bộ Đặc tả SRS v1.2 Baseline) - Hoàn thành 100%  
> **Tổng tiến độ toàn dự án:** ~15% (Đã hoàn thiện tài liệu nền tảng, thiết kế CSDL, quy tắc nghiệp vụ, API, Backlog và bộ kỹ năng)

---

## 📊 Bảng tổng hợp theo giai đoạn (Phase Summary)

| Giai đoạn | Nội dung trọng tâm | Trạng thái | Tiến độ |
| :--- | :--- | :--- | :--- |
| **Phase 0** | Khung kiến trúc, Đặc tả SRS v1.2, CSDL, API, Quy tắc nghiệp vụ & Backlog | ✅ COMPLETED | 100% |
| **Phase 1** | Database Schema, EF Core, PostgreSQL Docker, Seed Data | ⏳ PENDING | 0% |
| **Phase 2** | Auth JWT, Quản lý Chuyên khoa, Bác sĩ & Tính Slot | ⏳ PENDING | 0% |
| **Phase 3** | Động cơ Đặt lịch, Khách vãng lai, OTP & Chống trùng lặp (Concurrency) | ⏳ PENDING | 0% |
| **Phase 4** | Frontend React: Booking Wizard, Tra cứu OTP, Reschedule, Patient Portal | ⏳ PENDING | 0% |
| **Phase 5** | Staff/Lễ tân Portal (Duyệt Đổi lịch, Check-in) & Bác sĩ Workspace | ⏳ PENDING | 0% |
| **Phase 6** | Testing toàn diện (xUnit, Testcontainers, Playwright E2E) & Dockerization | ⏳ PENDING | 0% |

---

## ✅ Các hạng mục đã hoàn thành (Recent Achievements)

1. **Bộ khung thư mục & Tệp cấu hình:**
   - Hoàn chỉnh `.gitignore`, `.env`, `.env.example`, `README.md`, `AGENTS.md`.
2. **Đồng bộ hóa toàn diện tài liệu theo SRS v1.2 Baseline:**
   - [docs/SPEC.md](../docs/SPEC.md): Baseline nghiệp vụ chuẩn phiên bản 1.2 (13/11/2026).
   - [docs/BUSINESS_RULES.md](../docs/BUSINESS_RULES.md): Cập nhật công thức hoàn tiền mốc 60 phút (100% / 50%), quy trình đổi lịch phê duyệt bởi lễ tân (bảo toàn slot cũ đến khi duyệt), quy tắc định danh không auto-merge.
   - [docs/DATABASE_DESIGN.md](../docs/DATABASE_DESIGN.md): Bổ sung thực thể `services`, `reschedule_requests`, `refunds`, `otp_challenges`, bảo mật PII bằng mã hóa và HMAC hash.
   - [docs/API_SPECIFICATION.md](../docs/API_SPECIFICATION.md): Bổ sung endpoints OTP (`LOOKUP`, `RESCHEDULE`, `CANCEL`), quy trình duyệt đổi lịch, hủy lịch tự động không cần lễ tân duyệt.
   - [docs/TESTING_STRATEGY.md](../docs/TESTING_STRATEGY.md): Bổ sung test boundaries tính hoàn phí, test transaction đổi lịch, test concurrency zero double-booking.
   - [planning/feature_list.json](../planning/feature_list.json) & [planning/backlog.md](../planning/backlog.md): Tái cấu trúc 10 modules và 13 user stories chi tiết kèm Acceptance Criteria.
   - [.agents/skills/clinic-business-logic/SKILL.md](../.agents/skills/clinic-business-logic/SKILL.md): Cập nhật code mẫu `RefundCalculator`, `AppointmentStateMachine`, và logic đối sánh hồ sơ bệnh nhân.

---

## 🎯 Các bước tiếp theo (Immediate Next Steps - Bước vào Phase 1)

1. Tạo thư mục và cấu trúc Solution ASP.NET Core Clean Architecture trong `backend/`.
2. Tạo file `docker-compose.yml` cho PostgreSQL 16 cục bộ.
3. Cài đặt các gói NuGet EF Core & Npgsql, cấu hình DbContext và Fluent API configurations.
4. Chạy lệnh Add-Migration đầu tiên để khởi tạo schema CSDL chuẩn xác.
