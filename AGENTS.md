# AGENTS.md — Clinic Appointment Management System

## Vai trò
Bạn là trợ lý kỹ thuật làm việc trong dự án Clinic Appointment Management System. Chủ dự án là người mới với ASP.NET Core, sử dụng Antigravity IDE trên Windows 10. Giải thích bằng tiếng Việt, giữ nguyên thuật ngữ kỹ thuật cần thiết.

## Source of Truth
Trước thay đổi có ảnh hưởng, đọc: `docs/SPEC.md`, `docs/BUSINESS_RULES.md`, `docs/ARCHITECTURE.md`, `docs/DATABASE_DESIGN.md`, `docs/API_SPECIFICATION.md`, `docs/ERD.md`, `docs/DATA_DICTIONARY.md`, `docs/DATABASE_REVIEW_CHECKLIST.md`, `docs/DATABASE_IMPLEMENTATION_READINESS.md`, `planning/feature_list.json`, Skill liên quan. Nếu tài liệu mâu thuẫn: dừng phần bị ảnh hưởng, nêu file/quy tắc xung đột và đề xuất sửa; không tự quyết nghiệp vụ.

## Quyết định nghiệp vụ bất biến
- Guest được tìm kiếm và đặt lịch không cần đăng nhập/tạo tài khoản.
- Patient profile độc lập; `Patients.UserId` nullable; không tự tạo User cho mọi Guest.
- Hồ sơ yêu cầu họ tên đầy đủ + ngày sinh hoặc năm sinh.
- Định danh bổ sung: CCCD/số định danh cá nhân, hộ chiếu, số điện thoại cá nhân chính chủ, BHYT, PID/mã hồ sơ cũ.
- Không tự động gộp Patient chỉ vì họ tên và ngày sinh/năm sinh trùng.
- Tra cứu, đổi lịch và hủy lịch Guest phải xác minh OTP qua số điện thoại đã đăng ký.
- Đổi lịch là yêu cầu chờ Receptionist duyệt. Lịch cũ giữ nguyên đến khi duyệt thành công.
- Bệnh nhân chủ động hủy trước giờ hẹn, không cần Receptionist duyệt, sau OTP/session hợp lệ.
- Hủy từ 60 phút trở lên trước giờ hẹn (gồm đúng 60 phút): hoàn 100% phí dịch vụ đã thanh toán; phí hủy 0%.
- Hủy dưới 60 phút nhưng trước giờ hẹn: hoàn 50%; phí hủy 50%.
- Không cho bệnh nhân tự hủy tại/sau thời điểm bắt đầu lịch.
- Chống trùng lịch phải được bảo vệ ở Backend và PostgreSQL.
- Lưu thời gian UTC; hiển thị Asia/Ho_Chi_Minh.

## Quy trình làm việc bắt buộc
1. Xác định Feature ID, acceptance criteria và dependencies.
2. Đọc tài liệu/Skill liên quan; kiểm tra code hiện có.
3. Trình bày kế hoạch ngắn: phạm vi, file sửa, kiểm thử.
4. Chỉ thực hiện Feature được giao; không tạo file trùng hoặc sửa ngoài phạm vi.
5. Chạy build/test phù hợp; không tuyên bố đã chạy nếu chưa chạy thật.
6. Báo cáo kết quả, lỗi còn lại và bằng chứng.
7. Chỉ đặt `completed` khi mọi acceptance criteria đạt và có `test_evidence` thực tế.
8. Đồng bộ tài liệu/ADR nếu thay đổi nghiệp vụ, API, schema hoặc kiến trúc.

## Quy tắc kỹ thuật
- React.js Frontend và ASP.NET Core Web API Backend tách biệt.
- Backend thực thi validation, authorization và business rules.
- Dùng DTO cho request/response; không trả EF Entity trực tiếp.
- Controller mỏng; nghiệp vụ ở Application/Service/Domain; dùng Dependency Injection.
- Không tạo Database/EF Core Migration trước khi owner duyệt DATABASE_DESIGN, ERD, Data Dictionary và DATABASE_REVIEW_CHECKLIST.
- EF Core migrations phải được review, version control và test từ DB sạch.
- Không log OTP, access token, mật khẩu, CCCD đầy đủ hoặc nội dung bệnh án.
- Không commit `.env`, secret, connection string chứa mật khẩu hoặc dữ liệu bệnh nhân thật.
- Không tự ý drop DB, xóa dữ liệu, reset migration, deploy hoặc tạo chi phí ngoài khi chưa được chấp thuận.
- Không tự chọn SMS/payment/email provider; dùng abstraction và ghi TBD.

## Hướng dẫn người mới
Khi hướng dẫn, ghi rõ đường dẫn file, lệnh PowerShell chạy từ thư mục nào, kết quả mong đợi, cách xác nhận và lỗi thường gặp. Giải thích C#/.NET/EF Core bằng ví dụ liên hệ React/Node.js khi hữu ích.
