# Implementation Log (Nhật ký Triển khai Kỹ thuật)

> Tài liệu ghi chép lại toàn bộ các bước triển khai, thay đổi kiến trúc, cấu hình hệ thống và quyết định kỹ thuật phát sinh trong quá trình xây dựng dự án.

---

## [2026-11-13] - Khởi tạo Bộ khung Dự án & Tài liệu Kỹ thuật Nền tảng (Phase 0)

### 1. Bối cảnh & Yêu cầu
Khởi tạo toàn bộ bộ khung chuẩn cho dự án **Clinic Appointment Management** bao gồm:
- Tài liệu quy chuẩn cho AI Agents (`AGENTS.md`).
- Tài liệu kỹ thuật chi tiết (`docs/SPEC.md`, `docs/ARCHITECTURE.md`, `docs/DATABASE_DESIGN.md`, `docs/API_SPECIFICATION.md`, `docs/BUSINESS_RULES.md`, `docs/SECURITY.md`, `docs/TESTING_STRATEGY.md`, `docs/DEVELOPMENT_PLAN.md`).
- Các bản ghi quyết định kiến trúc (`docs/adr/0001`, `0002`, `0003`).
- Bộ công cụ lập kế hoạch (`planning/feature_list.json`, `planning/progress.md`, `planning/backlog.md`, `planning/implementation_log.md`).
- Bộ quy chuẩn lập trình (`.agents/rules/*`) và các kỹ năng chuyên biệt (`.agents/skills/*`).

### 2. Các tệp đã được tạo & Cấu trúc hoàn thiện
- **Root:**
  - `.gitignore`: Cấu hình loại trừ file rác hệ điều hành, IDE, build artifacts của .NET (`bin`, `obj`) và Node.js (`node_modules`, `dist`, `.env`).
  - `README.md`: Tài liệu giới thiệu tổng quan, tech stack, kiến trúc và hướng dẫn cài đặt nhanh.
  - `AGENTS.md`: Sổ tay hướng dẫn bắt buộc cho AI Agents khi phát triển tính năng trong repository.
- **Tài liệu kỹ thuật (`docs/`):**
  - `SPEC.md`: Đặc tả yêu cầu phần mềm chi tiết cho 5 đối tượng người dùng (Khách vãng lai, Bệnh nhân, Bác sĩ, Lễ tân, Quản trị viên).
  - `ARCHITECTURE.md`: Sơ đồ kiến trúc Mermaid, phân tầng Clean Architecture cho ASP.NET Core và Feature-based structure cho React.
  - `DATABASE_DESIGN.md`: Sơ đồ ERD quan hệ thực thể, từ điển dữ liệu (Data Dictionary) cho PostgreSQL 16 và chiến lược Unique Index chống Double-booking.
  - `API_SPECIFICATION.md`: Quy chuẩn RESTful API, envelope response, mã lỗi và danh sách chi tiết các endpoints.
  - `BUSINESS_RULES.md`: Quy định thời hạn đặt lịch (tối thiểu 2h, tối đa 30 ngày), thuật toán tính slot, ma trận trạng thái vòng đời lịch hẹn.
  - `SECURITY.md`: Cơ chế xác thực JWT Token Rotation, RBAC, phòng vệ lỗ hổng OWASP và bảo vệ dữ liệu y tế.
  - `TESTING_STRATEGY.md`: Chiến lược kiểm thử tự động với xUnit, Testcontainers PostgreSQL và Playwright E2E.
  - `DEVELOPMENT_PLAN.md`: Lộ trình 6 giai đoạn phát triển từ hạ tầng đến đóng gói sản phẩm.
  - `adr/0001-frontend-backend-separation.md`: Quyết định tách biệt SPA React và Web API .NET.
  - `adr/0002-database-postgresql.md`: Quyết định sử dụng PostgreSQL với EF Core.
  - `adr/0003-guest-booking.md`: Quyết định hỗ trợ đặt lịch cho khách vãng lai không cần tài khoản.
- **Kế hoạch dự án (`planning/`):**
  - `feature_list.json`: 7 modules với 22 features được phân loại độ phức tạp và mức độ ưu tiên.
  - `progress.md`: Theo dõi trạng thái tiến độ theo từng giai đoạn.
  - `backlog.md`: Danh sách User Stories chi tiết kèm tiêu chí chấp nhận (Acceptance Criteria).
  - `implementation_log.md`: Nhật ký triển khai kỹ thuật này.

### 3. Đánh giá & Bước kế tiếp
- Toàn bộ tài liệu và khung dự án đã khớp 100% với cấu trúc được yêu cầu.
- Đã khởi tạo `.env` (cấu hình môi trường dev cục bộ) và `.env.example` (bản mẫu biến môi trường chuẩn).
- Sẵn sàng chuyển giao cho Giai đoạn 1: Khởi tạo Solution .NET 8 Web API và thiết lập Docker PostgreSQL.


---

## [2026-11-13] - Cập nhật Đặc tả Yêu cầu Phần mềm SRS v1.2 Baseline (docs/SPEC.md)

### 1. Bối cảnh & Yêu cầu
Cập nhật tệp [docs/SPEC.md](file:///d:/Clinic_Appointment_Management_Project/docs/SPEC.md) lên phiên bản **1.2 (Baseline nghiệp vụ theo các quyết định đã xác nhận)**:
- Nguồn yêu cầu sản phẩm chính thống cho toàn bộ dự án.
- Tôn trọng nguyên tắc TBD: nội dung TBD chưa chốt, không tự biến TBD thành yêu cầu bắt buộc.

### 2. Nội dung thay đổi cốt lõi
- **Phạm vi (Scope):** Xác định rõ 12 nhóm In-scope và danh mục Out-of-scope (không có Telemedicine, không liên thông BHYT/Bệnh viện quốc gia, không native app; cổng thanh toán tự động là TBD).
- **Actors:** 5 đối tượng (Guest, Patient, Receptionist, Doctor, Admin) với quy tắc bắt buộc: Kiểm tra Authorization luôn ở Backend.
- **Yêu cầu chức năng (FR-001 đến FR-067):**
  - Quản lý hồ sơ `Patient` độc lập `User` (cho phép `UserId = NULL`).
  - Xác thực OTP chỉ áp dụng cho Tra cứu (`LOOKUP`), Đổi lịch (`RESCHEDULE`), Hủy lịch (`CANCEL`).
  - Sinh mã `BookingCode` duy nhất, khó đoán, không lộ PII.
  - Quy trình Đổi lịch (Reschedule): Bệnh nhân gửi yêu cầu, Receptionist duyệt/từ chối; giữ nguyên slot cũ đến khi duyệt thành công và kiểm tra slot mới trong Transaction.
  - Chính sách Hủy lịch & Hoàn phí: Bệnh nhân tự hủy không cần duyệt; mốc $\ge$ 60 phút hoàn 100% (phí hủy 0%), mốc < 60 phút hoàn 50% (phí hủy 50%).
- **Quản lý Định danh & Chống trùng (Section 8):** Phân biệt định danh mạnh vs yếu; không auto-merge hồ sơ chỉ dựa trên Họ tên + Ngày sinh/Năm sinh; mã hóa định danh nhạy cảm (CCCD, BHYT, PID) và sử dụng Keyed HMAC cho exact lookup.
- **Trạng thái (Section 9):** Chuẩn hóa trạng thái của Appointment, RescheduleRequest và Refund.
- **Tiêu chí nghiệm thu (Section 11) & Danh mục 12 điểm TBD (Section 12).**
