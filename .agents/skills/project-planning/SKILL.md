---
name: project-planning
description: Hướng dẫn quy trình lập kế hoạch tính năng, phân rã user stories, cập nhật tiến độ và quản lý backlog cho dự án phòng khám.
---

# Project Planning Skill

Kỹ năng này hướng dẫn AI Agent cách quản lý vòng đời tính năng, lập kế hoạch chi tiết và duy trì tính nhất quán của các tệp quản lý dự án trong thư mục `planning/`.

---

## 1. Khi nào kích hoạt kỹ năng này?
- Khi tiếp nhận một yêu cầu tính năng mới từ người dùng (ví dụ: "Thêm tính năng gửi SMS nhắc lịch khám").
- Khi hoàn thành một module/tính năng và cần cập nhật trạng thái tiến độ toàn bộ dự án.
- Khi cần xem xét lại mức độ ưu tiên hoặc phân rã công việc lớn thành các User Stories nhỏ hơn.

---

## 2. Quy trình phân rã tính năng (Feature Breakdown Process)

Khi có một tính năng mới cần đưa vào hệ thống:
1. **Kiểm tra tính trùng lặp:** Đối chiếu với `planning/feature_list.json` xem tính năng đã tồn tại hay chưa.
2. **Xác định Module & Độ ưu tiên:**
   - Thuộc module nào (`MOD-01` đến `MOD-07` hoặc tạo module mới).
   - Đánh giá mức độ ưu tiên (`P0` - Bắt buộc cho MVP, `P1` - Quan trọng, `P2` - Tiện ích mở rộng).
3. **Phân rã thành User Stories:**
   - Viết User Story theo cấu trúc: `Là một [Role], tôi muốn [Hành động] để [Lợi ích]`.
   - Xác định rõ ràng các Tiêu chí chấp nhận (Acceptance Criteria - AC).
   - Bổ sung vào `planning/backlog.md`.
4. **Cập nhật File JSON:**
   - Thêm bản ghi tương ứng vào `planning/feature_list.json` với trạng thái ban đầu `NOT_STARTED`.

---

## 3. Quy trình Cập nhật Tiến độ Dự án (Progress Update Workflow)

Sau khi một tính năng hoặc một nhóm tác vụ được triển khai và kiểm thử thành công:
1. Mở `planning/feature_list.json`: Chuyển trạng thái tính năng từ `IN_PROGRESS` sang `COMPLETED`.
2. Mở `planning/progress.md`:
   - Đánh dấu tick `[x]` vào các checklist tương ứng.
   - Tính toán lại phần trăm tiến độ giai đoạn và toàn dự án.
   - Thêm mục vào phần "Các hạng mục đã hoàn thành gần đây" (Recent Achievements).
   - Cập nhật các bước tiếp theo (Immediate Next Steps).
3. Mở `planning/implementation_log.md`:
   - Ghi lại bản ghi mới: Thời gian, tác vụ đã thực hiện, file sửa đổi, kết quả kiểm tra.

