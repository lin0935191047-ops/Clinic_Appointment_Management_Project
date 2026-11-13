# DATABASE REVIEW CHECKLIST

Dùng checklist này trước khi phê duyệt `DATABASE_DESIGN.md` và trước khi tạo EF Core Migration.

## A. Business alignment
- [ ] Patient có thể tồn tại với `user_id = NULL`.
- [ ] Guest booking không yêu cầu User/JWT.
- [ ] Patient bắt buộc FullName và DateOfBirth hoặc BirthYear.
- [ ] Name + DOB/BirthYear không unique và không auto-merge.
- [ ] CCCD/passport/BHYT/PID lưu ciphertext + keyed HMAC.
- [ ] Phone contact dùng chung được; phone personal-owner verified được phân biệt.
- [ ] Tra cứu/đổi/hủy có OTP challenge và scoped grant.
- [ ] Reschedule request chờ Receptionist duyệt; appointment cũ giữ nguyên trước khi duyệt.
- [ ] Patient cancellation tự thực hiện trước giờ hẹn sau OTP/session hợp lệ.
- [ ] 60 phút hoặc hơn: hoàn 100%; dưới 60 phút nhưng trước giờ hẹn: hoàn 50%.
- [ ] Refund dựa vào phí snapshot và khoản đã thanh toán.

## B. Referential integrity
- [ ] Tất cả FK có delete behavior được xác định.
- [ ] Không cascade delete Patient/Appointment/Consultation/Payment/Audit.
- [ ] PK và unique identifiers được kiểm tra.
- [ ] Patient merge có event/audit và giữ lịch sử.
- [ ] BookingCode random và unique.
- [ ] Composite FK bảo đảm booking contact/OTP contact thuộc cùng Patient.
- [ ] Composite FK bảo đảm Consultation Doctor và Appointment Doctor nhất quán.
- [ ] Composite FK bảo đảm OTP grant đúng Booking; Application xác minh đúng Purpose LOOKUP/RESCHEDULE/CANCEL.
- [ ] Appointment chỉ tham chiếu Doctor-Service đã được gán.

## C. Time and money
- [ ] Timestamp sử dụng `timestamptz` UTC.
- [ ] Schedule weekly local time được diễn giải theo `Asia/Ho_Chi_Minh`.
- [ ] Slot interval dùng `[start_at, end_at)`.
- [ ] Money dùng `numeric`, C# `decimal`.
- [ ] Currency snapshot trên Appointment.
- [ ] Mỗi refund gắn với một payment nguồn cụ thể; refund không vượt số payment thành công.
- [ ] Ranh giới 61/60/59 phút có Unit Test.

## D. Concurrency
- [ ] PostgreSQL exclusion constraint cho cùng Doctor và slot overlap.
- [ ] Booking race test chạy trên PostgreSQL thật.
- [ ] Reschedule approval race test chạy trên PostgreSQL thật.
- [ ] Transaction rollback giữ lịch cũ khi approval xung đột.
- [ ] Idempotency xử lý retry và key reuse với payload khác.

## E. Security and privacy
- [ ] Không lưu OTP/grant/password plaintext.
- [ ] Raw identifier được mã hóa; HMAC key đặt ngoài Database/repository.
- [ ] Partial unique chỉ áp dụng identifier đã VERIFIED.
- [ ] Guest lookup không trả bệnh án hoặc identifier đầy đủ.
- [ ] Audit metadata đã redaction.
- [ ] Contact được booking sử dụng không bị hard-delete hoặc sửa đè giá trị; thay đổi tạo contact record mới.
- [ ] Role/resource access được kiểm tra ở Backend.

## F. EF Core implementation readiness
- [ ] Table/column names và max lengths rõ ràng.
- [ ] Precision/scale tiền tệ được cấu hình.
- [ ] DateOnly/TimeOnly/DateTimeOffset mapping được test với phiên bản .NET/Npgsql đã chọn.
- [ ] DeleteBehavior được cấu hình tường minh.
- [ ] Partial unique index được tạo qua migration và test.
- [ ] Exclusion constraint dùng migration SQL được review.
- [ ] Migration chạy từ database rỗng và rollback/recovery được kiểm thử.

## G. Decisions still required
- [ ] Multi-branch/room.
- [ ] Partial payment và phí hủy khi chưa thanh toán.
- [ ] Cách xử lý reschedule approval conflict.
- [ ] Quy trình xác minh CCCD/passport/BHYT/PID.
- [ ] Quy trình cập nhật số điện thoại booking.
- [ ] Retention và deletion policy.
- [ ] Provider SMS/email/payment.
- [ ] Clinic cancellation/NoShow policy.

**Không tạo migration cho đến khi** các mục A-E đạt và các TBD ảnh hưởng trực tiếp schema được chủ dự án chốt hoặc ghi rõ baseline.
