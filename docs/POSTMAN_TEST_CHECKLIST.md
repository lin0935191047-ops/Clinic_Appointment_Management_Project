# API Test Checklist

- [ ] Public GET specialties/doctors/services không cần JWT.
- [ ] POST public appointment thành công không JWT.
- [ ] Thiếu FullName bị reject.
- [ ] Thiếu cả DOB và BirthYear bị reject.
- [ ] Lookup không OTP grant bị reject.
- [ ] OTP purpose mismatch/expired/reused bị reject.
- [ ] Lookup grant không dùng được để cancel/reschedule.
- [ ] Reschedule request tạo PendingReview; old slot vẫn giữ.
- [ ] Patient không tự approve reschedule.
- [ ] Reception approve recheck slot; conflict giữ old appointment.
- [ ] Cancel không CANCEL grant bị reject.
- [ ] Cancel đúng 60 phút trả expected refund 100%.
- [ ] Cancel 59 phút trả expected refund 50%.
- [ ] Cancel tại/sau StartAt bị từ chối cho self-service.
- [ ] Refund Pending không hiển thị Succeeded.
- [ ] Patient A không truy cập Patient B.
- [ ] Doctor không truy cập appointment không phân công.
- [ ] Public API không trả consultation hoặc raw identifier.
