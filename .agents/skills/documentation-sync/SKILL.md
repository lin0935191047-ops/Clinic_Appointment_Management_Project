---
name: documentation-sync
description: Hướng dẫn duy trì và đồng bộ hóa tài liệu kỹ thuật, ADRs, nhật ký triển khai và tiến độ dự án khi mã nguồn thay đổi.
---

# Documentation Sync Skill

Kỹ năng này hướng dẫn AI Agent cách duy trì sự đồng bộ hoàn hảo giữa mã nguồn (Codebase) và hệ thống tài liệu kỹ thuật của dự án.

---

## 1. Nguyên tắc "Code Thay đổi - Tài liệu Đồng hành"

Bất kỳ thay đổi nào trong mã nguồn làm ảnh hưởng đến cấu trúc hoặc quy ước phải được phản ánh ngay lập tức vào tài liệu liên quan:

| Thay đổi trong Codebase | Tài liệu bắt buộc phải cập nhật đồng thời |
| :--- | :--- |
| Thêm/sửa/xóa bảng hoặc cột trong PostgreSQL | `docs/DATABASE_DESIGN.md` |
| Thêm/thay đổi tham số, endpoint REST API | `docs/API_SPECIFICATION.md` |
| Thay đổi quy tắc nghiệp vụ (thời gian, slot, quyền) | `docs/BUSINESS_RULES.md` |
| Đưa ra một quyết định kiến trúc quan trọng mới | Tạo file mới trong `docs/adr/000X-...md` |
| Hoàn thành một tính năng hoặc chuyển giai đoạn | `planning/progress.md` & `planning/feature_list.json` |
| Mọi phiên can thiệp kỹ thuật hoặc migration | `planning/implementation_log.md` |

---

## 2. Quy trình Viết Quyết định Kiến trúc (ADR Workflow)

Khi gặp một vấn đề kỹ thuật lớn cần đưa ra lựa chọn (ví dụ: Sử dụng Redis để cache hay không, chọn thư viện UI component nào):
1. Tạo file mới: `docs/adr/000X-<tên-ngắn-gọn-kebab-case>.md`.
2. Tuân thủ định dạng chuẩn gồm 4 phần:
   - **Trạng thái & Ngày tháng:** `Proposed` / `Accepted` / `Superseded`.
   - **1. Ngữ cảnh (Context):** Vấn đề là gì? Có những lựa chọn nào?
   - **2. Quyết định (Decision):** Chúng ta chọn phương án nào?
   - **3. Lý do lựa chọn (Rationale):** Tại sao phương án này vượt trội hơn?
   - **4. Đánh đổi & Hệ quả (Consequences):** Lợi ích mang lại và những điểm cần lưu ý/khắc phục.
3. Thông báo cho nhóm hoặc ghi nhận vào `planning/implementation_log.md`.

---

## 3. Duy trì Nhật ký Triển khai (Implementation Log Guidelines)

- Luôn thêm bản ghi mới ở đầu hoặc cuối theo trật tự thời gian rõ ràng trong `planning/implementation_log.md`.
- Ghi rõ ngày giờ, nội dung đã làm, lý do kỹ thuật và các bước tiếp theo cần triển khai.

