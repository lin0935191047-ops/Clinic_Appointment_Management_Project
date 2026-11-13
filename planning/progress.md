# Project Progress Tracker (Theo dõi Tiến độ Dự án)

> **Cập nhật lần cuối:** 2026-11-13  
> **Trạng thái tổng thể:** Giai đoạn 0 (Khởi tạo Khung & Đặc tả Kỹ thuật) - Hoàn thành 100%  
> **Tổng tiến độ toàn dự án:** ~15% (Đã hoàn thiện tài liệu nền tảng, thiết kế CSDL, quy tắc nghiệp vụ và bộ kỹ năng)

---

## 📊 Bảng tổng hợp theo giai đoạn (Phase Summary)

| Giai đoạn | Nội dung trọng tâm | Trạng thái | Tiến độ |
| :--- | :--- | :--- | :--- |
| **Phase 0** | Khung kiến trúc, Đặc tả SRS, CSDL, API, Quy tắc nghiệp vụ | ✅ COMPLETED | 100% |
| **Phase 1** | Database Schema, EF Core, PostgreSQL Docker, Seed Data | ⏳ PENDING | 0% |
| **Phase 2** | Auth JWT, Quản lý Chuyên khoa, Bác sĩ & Tính Slot | ⏳ PENDING | 0% |
| **Phase 3** | Động cơ Đặt lịch, Khách vãng lai & Chống trùng lặp (Concurrency) | ⏳ PENDING | 0% |
| **Phase 4** | Frontend React: Booking Wizard, Tra cứu, Patient Portal | ⏳ PENDING | 0% |
| **Phase 5** | Staff/Lễ tân Portal (Check-in) & Bác sĩ Workspace | ⏳ PENDING | 0% |
| **Phase 6** | Testing toàn diện (xUnit, Testcontainers, E2E) & Dockerization | ⏳ PENDING | 0% |

---

## ✅ Các hạng mục đã hoàn thành (Recent Achievements)

1. **Bộ khung thư mục chuẩn mực:**
   - Cấu trúc đầy đủ các thư mục `docs/`, `planning/`, `.agents/`, `backend/`, `frontend/`.
   - File `.gitignore` toàn diện cho cả hệ sinh thái .NET và React.
2. **Hệ thống tài liệu kỹ thuật chi tiết:**
   - [docs/SPEC.md](../docs/SPEC.md): Đặc tả yêu cầu chức năng & phi chức năng.
   - [docs/ARCHITECTURE.md](../docs/ARCHITECTURE.md): Kiến trúc Clean Architecture cho .NET và React.
   - [docs/DATABASE_DESIGN.md](../docs/DATABASE_DESIGN.md): Thiết kế bảng dữ liệu PostgreSQL, ERD và Index.
   - [docs/API_SPECIFICATION.md](../docs/API_SPECIFICATION.md): Đặc tả chuẩn RESTful API và các endpoints.
   - [docs/BUSINESS_RULES.md](../docs/BUSINESS_RULES.md): Quy tắc đặt lịch, chia slot và chống trùng lặp.
   - [docs/SECURITY.md](../docs/SECURITY.md): Chính sách bảo mật dữ liệu y tế và xác thực JWT.
   - [docs/TESTING_STRATEGY.md](../docs/TESTING_STRATEGY.md): Chiến lược kiểm thử tự động phân tầng.
   - [docs/DEVELOPMENT_PLAN.md](../docs/DEVELOPMENT_PLAN.md): Lộ trình từng bước triển khai.
3. **Quyết định kiến trúc (ADRs):**
   - [ADR 0001](../docs/adr/0001-frontend-backend-separation.md): Tách biệt Frontend React & Backend Web API.
   - [ADR 0002](../docs/adr/0002-database-postgresql.md): Sử dụng PostgreSQL với EF Core.
   - [ADR 0003](../docs/adr/0003-guest-booking.md): Hỗ trợ đặt lịch khám cho khách vãng lai.
4. **Bộ quy chuẩn & Kỹ năng AI (Agent Rules & Skills):**
   - Hoàn thiện 4 bộ quy chuẩn coding: `project-rules.md`, `backend-rules.md`, `frontend-rules.md`, `database-rules.md`.
   - Hoàn thiện 7 tài liệu kỹ năng chuyên biệt trong `.agents/skills/`.

---

## 🎯 Các bước tiếp theo (Immediate Next Steps - Bước vào Phase 1)

1. Tạo thư mục và cấu trúc Solution ASP.NET Core Clean Architecture trong `backend/`.
2. Tạo file `docker-compose.yml` cho PostgreSQL 16 cục bộ.
3. Cài đặt các gói NuGet EF Core & Npgsql, viết cấu hình DbContext.
4. Chạy lệnh Add-Migration đầu tiên để khởi tạo schema CSDL.

