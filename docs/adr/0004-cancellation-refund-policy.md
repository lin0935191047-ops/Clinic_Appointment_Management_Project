# ADR-0004: Cancellation refund boundary

- Status: Accepted
- Decision: Patient tự hủy trước giờ bắt đầu sau OTP/session. Từ 60 phút trở lên (bao gồm đúng 60) => hoàn 100% phí dịch vụ đã thanh toán, phí hủy 0%. Dưới 60 phút nhưng trước giờ bắt đầu => hoàn 50%, phí hủy 50%.
- Consequences: Server time authoritative; fee snapshot được lưu; refund processing tách khỏi calculation; partial payment/unpaid-fee collection cần xác nhận thêm.
