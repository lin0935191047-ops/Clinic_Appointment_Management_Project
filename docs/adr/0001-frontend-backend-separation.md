# ADR 0001: Tách biệt Frontend (React SPA) và Backend (ASP.NET Core Web API)

- **Trạng thái:** Được chấp thuận (Accepted)
- **Ngày quyết định:** 2026-11-13
- **Người đề xuất:** Architecture Team

---

## 1. Ngữ cảnh (Context)
Hệ thống quản lý đặt lịch khám bệnh phục vụ nhiều nhóm đối tượng: Bệnh nhân (truy cập qua trình duyệt máy tính, điện thoại di động), Bác sĩ và Nhân viên Lễ tân (sử dụng tại quầy tiếp đón). Trong tương lai, hệ thống có thể cần mở rộng ứng dụng di động native (React Native / Flutter) hoặc tích hợp với kiosk tự phục vụ tại sảnh phòng khám.

Chúng ta cần lựa chọn giữa:
1. Mô hình Monolithic SSR (ASP.NET Core Razor Pages / Blazor / MVC).
2. Mô hình Tách biệt Frontend (Single Page Application - React) và Backend (RESTful Web API độc lập).

---

## 2. Quyết định (Decision)
Chúng tôi quyết định áp dụng **Mô hình Tách biệt hoàn toàn giữa Frontend (React SPA) và Backend (ASP.NET Core Web API)** giao tiếp qua giao thức chuẩn HTTPS / JSON REST API.

---

## 3. Lý do lựa chọn (Rationale)
1. **Trải nghiệm người dùng mượt mà (Rich User Experience):**
   - Đặt lịch khám yêu cầu tương tác cao: chọn lịch theo thời gian thực, chuyển đổi các bước (wizard) không tải lại trang, phản hồi giao diện tức thì.
   - React với hệ sinh thái phong phú (Tailwind, TanStack Query) mang lại trải nghiệm tối ưu cho bệnh nhân và nhân viên lễ tân.
2. **Khả năng tái sử dụng API:**
   - Backend đóng vai trò cung cấp API phi trạng thái (Stateless). Các endpoint này có thể phục vụ song song cho Web SPA, Mobile App sau này, hoặc thiết bị Kiosk check-in mà không cần viết lại logic nghiệp vụ.
3. **Phân chia công việc chuyên biệt:**
   - Đội ngũ phát triển Frontend và Backend có thể làm việc độc lập dựa trên tài liệu đặc tả API (`docs/API_SPECIFICATION.md`) mà không bị ràng buộc phụ thuộc lẫn nhau.
4. **Khả năng mở rộng và triển khai linh hoạt:**
   - Frontend tĩnh có thể được lưu trữ và phân phối qua CDN với chi phí cực thấp và tốc độ tải trang cao.
   - Backend có thể được scale theo chiều ngang tùy thuộc vào tải xử lý nghiệp vụ.

---

## 4. Hệ quả & Đánh đổi (Consequences)
- **Tích cực:** Tách biệt rõ ràng trách nhiệm, dễ bảo trì, dễ mở rộng, hiệu suất tương tác cao.
- **Tiêu cực / Cần khắc phục:** Cần quản lý cấu hình CORS chặt chẽ, xử lý SEO (nếu cần cho trang danh bạ bác sĩ - có thể dùng prerendering hoặc SSR sau này nếu cần thiết).

