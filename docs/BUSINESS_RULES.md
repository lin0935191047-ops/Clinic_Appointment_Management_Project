# BUSINESS RULES — Clinic Appointment Management

Version 1.2 | Baseline theo quyết định đã xác nhận

## BR-001 — Guest booking
Guest được xem danh mục công khai và tạo Appointment không cần đăng nhập hoặc tạo User. Public API phải rate-limit và validate input.

## BR-002 — Patient độc lập User
`Patients.UserId` nullable. Một Patient có thể có hoặc không có tài khoản. Không tạo User tự động cho Guest.

## BR-003 — Hồ sơ tối thiểu
FullName bắt buộc; phải có DateOfBirth hoặc BirthYear. Số điện thoại liên hệ cần có để dùng OTP; phone phải được xác minh trước lookup/change/cancel.

## BR-004 — Identity matching
- Exact match định danh mạnh đã xác minh (CCCD, Passport, BHYT, PID) là tín hiệu mạnh để xác định candidate.
- Họ tên + DOB/BirthYear chỉ tạo candidate; không tự động gộp.
- Điện thoại xác minh là tín hiệu đối sánh; nếu số dùng chung hoặc có xung đột thì manual review.
- Định danh mạnh xung đột: không tự động gộp.
- Mọi merge/link cần actor, timestamp, reason và audit.

## BR-005 — OTP
OTP purpose riêng: LOOKUP, RESCHEDULE, CANCEL. OTP gắn challenge, phone, booking và purpose; có expiry, max attempts, resend cooldown. OTP one-time, lưu hash, không log. Verify thành công cấp grant ngắn hạn đúng booking/purpose, không phải User JWT.

## BR-006 — Appointment slot
Khoảng thời gian `[StartAt, EndAt)`. Overlap khi `existing.StartAt < new.EndAt AND existing.EndAt > new.StartAt`. Cùng bác sĩ không có hai Appointment đang giữ slot overlap. PendingConfirmation/Confirmed/CheckedIn/InProgress giữ slot; Cancelled giải phóng.

## BR-007 — Booking transaction
Availability UI chỉ hỗ trợ UX. Backend kiểm tra lại trong transaction; Database là lớp cuối chống concurrent overlap. Chỉ trả success khi transaction commit.

## BR-008 — Reschedule requires Receptionist approval
Bệnh nhân xác minh và tạo RescheduleRequest PendingReview. Appointment hiện tại tiếp tục giữ slot cũ. Slot đề xuất không được coi là reserved. Receptionist approve/reject. Khi approve, recheck slot mới và update Appointment trong transaction. Nếu slot không còn trống, không đổi lịch cũ. Mỗi Appointment tối đa một request PendingReview.

## BR-009 — Patient-initiated cancellation
Guest/Patient chủ động hủy trước StartAt sau OTP/session hợp lệ; không yêu cầu Receptionist duyệt. Hủy phải ghi reason, actor, timestamp và financial result.

## BR-010 — Cancellation/refund boundary
Tính theo thời điểm Server chấp nhận hủy so với StartAt:
- `minutesBefore >= 60`: refund 100%, cancellation fee 0%.
- `0 < minutesBefore < 60`: refund 50%, cancellation fee 50%.
- `minutesBefore <= 0`: patient self-cancel bị từ chối.
Đúng 60 phút thuộc nhóm hoàn 100%. Dùng giá snapshot và số tiền đã thanh toán. Nếu chưa thanh toán, refund = 0; không tự thu phí chưa có policy. Partial payment handling là TBD.

## BR-011 — Service price snapshot
Appointment lưu ServiceNameSnapshot, DurationMinutesSnapshot, ServiceFeeSnapshot, CurrencySnapshot khi tạo. Sửa giá danh mục không đổi booking cũ.

## BR-012 — State transition
Chuyển trạng thái Appointment phải kiểm tra current state + actor + quyền ở Backend; ghi AppointmentHistory.

## BR-013 — Check-in and consultation
Receptionist check-in lịch phù hợp. Doctor chỉ thao tác lịch được phân công. Consultation có quyền truy cập chặt hơn dữ liệu lịch thông thường.

## BR-014 — Time
Lưu UTC, API ISO-8601, UI Asia/Ho_Chi_Minh. Không dùng client clock cho refund/audit.

## BR-015 — Delete and audit
Không hard-delete hồ sơ có lịch sử booking/consultation; dùng deactivate/soft-delete theo policy. Audit trail không bị xóa. Retention period TBD.
