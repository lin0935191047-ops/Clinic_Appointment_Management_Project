# SOFTWARE REQUIREMENTS SPECIFICATION (SRS)
# Clinic Appointment Management System

- **Tên tiếng Việt:** Hệ thống quản lý và đặt lịch khám bệnh trực tuyến
- **Phiên bản:** 1.2
- **Trạng thái:** Baseline nghiệp vụ theo các quyết định đã xác nhận
- **Ngày cập nhật:** 13/11/2026
- **Đối tượng đọc:** Chủ dự án, BA, Developer, Tester, AI Agent

> Đây là nguồn yêu cầu sản phẩm chính. Nội dung `TBD` chưa được chốt; Agent không tự biến TBD thành yêu cầu bắt buộc.

## 1. Mục đích
Mô tả yêu cầu nghiệp vụ, chức năng, phi chức năng và tiêu chí nghiệm thu cho hệ thống quản lý lịch khám của phòng khám. Làm cơ sở cho UX, kiến trúc, Database, API, phát triển, kiểm thử và nghiệm thu.

## 2. Tổng quan
Web Application gồm React.js Frontend, ASP.NET Core Web API Backend và PostgreSQL. Guest có thể xem bác sĩ, dịch vụ, lịch trống và đặt lịch mà không cần đăng nhập. Tra cứu, đổi và hủy lịch của Guest cần OTP gửi tới số điện thoại đã ghi nhận. Lễ tân xác nhận lịch, duyệt đổi lịch và tiếp nhận bệnh nhân; bác sĩ quản lý ca khám và ghi nhận nội dung khám; Admin quản lý cấu hình, danh mục, tài khoản và báo cáo.

## 3. Mục tiêu nghiệp vụ
- Số hóa quy trình đặt và quản lý lịch khám.
- Giảm thao tác ghi sổ/gọi điện và hạn chế lịch trùng.
- Cho phép Guest đặt lịch nhanh không cần tạo tài khoản.
- Quản lý hồ sơ Patient độc lập với User.
- Đổi lịch có duyệt; hủy/hoàn phí minh bạch.
- Bảo vệ định danh và dữ liệu khám bệnh.
- Cung cấp công cụ cho lễ tân, bác sĩ và Admin.

## 4. Phạm vi
### 4.1 In scope
1. Trang công khai: giới thiệu, chuyên khoa, bác sĩ, dịch vụ.
2. Guest Booking: chọn chuyên khoa/bác sĩ/dịch vụ/ngày/giờ; nhập thông tin bệnh nhân; tạo lịch không cần tài khoản.
3. Patient profile độc lập User; quản lý định danh và phát hiện hồ sơ nghi trùng.
4. OTP cho tra cứu, yêu cầu đổi lịch, hủy lịch.
5. Appointment status, mã đặt lịch, lịch sử.
6. Reschedule: bệnh nhân gửi yêu cầu; Receptionist duyệt/từ chối; lịch cũ giữ nguyên đến khi duyệt.
7. Cancellation/refund: bệnh nhân chủ động hủy trước giờ hẹn; tự tính mức hoàn theo mốc 60 phút.
8. Admin: tài khoản/role, bác sĩ, chuyên khoa, dịch vụ, lịch làm việc/ngày nghỉ.
9. Receptionist: quản lý hồ sơ, xác nhận lịch, duyệt đổi lịch, check-in, hàng đợi.
10. Doctor: lịch được phân công, bắt đầu khám, ghi kết quả, hoàn thành.
11. Notification qua abstraction; provider SMS/email là TBD.
12. Dashboard/báo cáo cơ bản; audit log; bảo mật; kiểm thử; triển khai.

### 4.2 Out of scope mặc định
- Video consultation/telemedicine.
- Kết nối liên thông bệnh viện/hệ thống y tế quốc gia.
- Kê đơn thuốc điện tử liên thông.
- Quản lý xét nghiệm/chẩn đoán hình ảnh chuyên sâu.
- Kết nối cổng BHYT trực tiếp.
- Mobile native app.
- Payment gateway/SMS provider cụ thể chưa được chọn.

Hệ thống vẫn lưu khoản phí đã thu, kết quả tính hoàn và trạng thái hoàn. Tích hợp tự động với payment gateway là TBD; không giả định có provider.

## 5. Actors
| Actor | Mô tả | Quyền chính |
|---|---|---|
| Guest | Người chưa đăng nhập | Xem công khai, kiểm tra lịch trống, đặt lịch, tra cứu/đổi/hủy qua OTP |
| Patient | Bệnh nhân có tài khoản tùy chọn | Quyền Patient trong phạm vi dữ liệu bản thân |
| Receptionist | Nhân viên lễ tân | Quản lý hồ sơ, xác nhận lịch, duyệt đổi lịch, check-in, hàng đợi |
| Doctor | Bác sĩ | Xem lịch được phân công, xem dữ liệu cần thiết, ghi kết quả khám |
| Admin | Quản trị viên | Quản lý người dùng, vai trò, danh mục, lịch, cấu hình, báo cáo |

Authorization luôn phải được kiểm tra ở Backend; ẩn nút trên Frontend không thay thế authorization.

## 6. Functional requirements

### 6.1 Public catalog
- **FR-001:** Guest xem thông tin phòng khám.
- **FR-002:** Guest xem/tìm/lọc chuyên khoa.
- **FR-003:** Guest xem/tìm bác sĩ và hồ sơ chuyên môn công khai.
- **FR-004:** Guest xem dịch vụ, mô tả, thời lượng, giá công khai.
- **FR-005:** Guest xem khung giờ khả dụng; không lộ lịch/PII của người khác.

### 6.2 Accounts
- **FR-006:** Patient có thể đăng ký tài khoản tùy chọn.
- **FR-007:** Patient, Doctor, Receptionist, Admin đăng nhập/đăng xuất.
- **FR-008:** Xác thực access token và refresh token nếu được triển khai.
- **FR-009:** Admin quản lý tài khoản và vai trò.
- **FR-010:** Đặt lại mật khẩu qua phương thức xác minh được cấu hình.

### 6.3 Patient profile and identity resolution
- **FR-011:** Tạo Patient mà không cần tạo User.
- **FR-012:** Hồ sơ phải có FullName và ít nhất một trong DateOfBirth hoặc BirthYear.
- **FR-013:** Lưu số điện thoại liên hệ; số dùng OTP phải được xác minh.
- **FR-014:** Có thể lưu CCCD/số định danh, hộ chiếu, BHYT, PID/mã hồ sơ cũ.
- **FR-015:** Tìm hồ sơ ứng viên trùng theo định danh chuẩn hóa và nhân khẩu học.
- **FR-016:** Không tự động hợp nhất chỉ dựa họ tên + ngày sinh/năm sinh; hồ sơ nghi trùng cần xác minh/review.
- **FR-017:** Tạo/sửa/liên kết/hợp nhất hồ sơ có audit; chỉ vai trò được phép thực hiện.

### 6.4 Guest booking
- **FR-018:** Guest đặt lịch không cần đăng nhập/tạo tài khoản.
- **FR-019:** Form bắt buộc FullName, DOB hoặc BirthYear, số điện thoại liên hệ; trường khác theo cấu hình.
- **FR-020:** Guest chọn chuyên khoa, bác sĩ, dịch vụ, ngày và khung giờ.
- **FR-021:** Backend kiểm tra bác sĩ/dịch vụ/lịch làm việc/ngày nghỉ/slot ngay khi ghi.
- **FR-022:** Tạo Patient mới hoặc liên kết hồ sơ đã xác minh; không tự tạo User.
- **FR-023:** Sinh BookingCode duy nhất, khó đoán, không chứa PII.
- **FR-024:** Lưu snapshot dịch vụ, thời lượng và giá tại thời điểm đặt.
- **FR-025:** Trả xác nhận; thông báo qua provider nếu đã cấu hình.

### 6.5 OTP and lookup
- **FR-026:** Guest yêu cầu OTP cho số điện thoại gắn với booking.
- **FR-027:** OTP có purpose LOOKUP/RESCHEDULE/CANCEL, expiry, attempt limit, resend limit.
- **FR-028:** OTP dùng một lần, gắn với booking/purpose/challenge.
- **FR-029:** Verify thành công cấp quyền tạm thời, giới hạn theo booking và purpose.
- **FR-030:** Guest tra cứu bằng BookingCode và OTP; không hiển thị bệnh án/định danh đầy đủ.

### 6.6 Appointment lifecycle
- **FR-031:** Trạng thái: PendingConfirmation, Confirmed, CheckedIn, InProgress, Completed, Cancelled, NoShow.
- **FR-032:** Receptionist xác nhận lịch PendingConfirmation.
- **FR-033:** Patient xem lịch của bản thân; Guest xem chi tiết sau OTP.
- **FR-034:** Mọi chuyển trạng thái kiểm tra ở Backend và ghi lịch sử.
- **FR-035:** Không đặt ngoài lịch, trong ngày nghỉ, khoảng bận hoặc ngoài thời hạn cấu hình.

### 6.7 Reschedule
- **FR-036:** Guest/Patient gửi yêu cầu đổi lịch sau OTP/session hợp lệ.
- **FR-037:** Bệnh nhân đề xuất ngày/giờ mới; hệ thống kiểm tra sơ bộ.
- **FR-038:** Yêu cầu có trạng thái PendingReview, Approved, Rejected, Cancelled hoặc Expired.
- **FR-039:** Receptionist duyệt/từ chối, ghi lý do/người xử lý/thời gian.
- **FR-040:** Lịch cũ vẫn giữ nguyên và giữ slot đến khi đổi thành công.
- **FR-041:** Khi duyệt, Backend kiểm tra lại slot trong transaction. Nếu slot không còn trống, không đổi lịch cũ.
- **FR-042:** Mỗi Appointment tối đa một request PendingReview.

### 6.8 Cancellation and refund
- **FR-043:** Guest/Patient có thể chủ động hủy lịch trước giờ bắt đầu sau OTP/session hợp lệ.
- **FR-044:** Không cần Receptionist duyệt yêu cầu hủy do bệnh nhân gửi.
- **FR-045:** Backend ghi thời điểm hủy theo server timestamp, không tin client clock.
- **FR-046:** Hủy từ 60 phút trở lên (bao gồm đúng 60 phút): hoàn 100% phí dịch vụ đã thanh toán, phí hủy 0%.
- **FR-047:** Hủy dưới 60 phút nhưng trước giờ hẹn: hoàn 50% phí dịch vụ đã thanh toán, phí hủy 50%.
- **FR-048:** Lưu giá snapshot, số đã thanh toán, tỷ lệ/giá trị phí hủy, số dự kiến hoàn, lý do, timestamp và refund status.
- **FR-049:** Chưa thanh toán thì refund amount = 0; không tự thu khoản phí chưa được policy phê duyệt.
- **FR-050:** Tại/sau giờ bắt đầu không thuộc luồng tự hủy bệnh nhân; xử lý qua Receptionist/NoShow.
- **FR-051:** Refund có trạng thái NotApplicable, Pending, Processing, Succeeded hoặc Failed.

### 6.9 Schedule management
- **FR-052:** Admin quản lý lịch làm việc định kỳ.
- **FR-053:** Admin/Receptionist được cấp quyền quản lý ngày nghỉ, giờ nghỉ, thời gian bận.
- **FR-054:** Sinh slot theo thời lượng dịch vụ/cấu hình.
- **FR-055:** Sửa lịch không âm thầm xóa hoặc thay đổi appointment đã xác nhận.

### 6.10 Reception and consultation
- **FR-056:** Receptionist tìm/tạo/cập nhật hồ sơ theo quyền.
- **FR-057:** Receptionist check-in lịch hợp lệ.
- **FR-058:** Receptionist quản lý hàng đợi.
- **FR-059:** Doctor xem lịch được phân công và dữ liệu cần thiết.
- **FR-060:** Doctor chuyển CheckedIn → InProgress.
- **FR-061:** Doctor ghi triệu chứng, thăm khám, chẩn đoán, kết luận, hướng dẫn.
- **FR-062:** Doctor hoàn tất khám, chuyển InProgress → Completed.

### 6.11 Notification and reporting
- **FR-063:** Thông báo đặt/xác nhận/đổi/hủy theo kênh cấu hình.
- **FR-064:** Nhắc lịch theo cấu hình, chống gửi trùng, log trạng thái gửi.
- **FR-065:** Dashboard Admin thống kê lịch, bác sĩ, bệnh nhân, trạng thái.
- **FR-066:** Dashboard Doctor hiển thị lịch trong phạm vi phân công.
- **FR-067:** Báo cáo lọc thời gian, bác sĩ, chuyên khoa, trạng thái.

## 7. Cancellation and refund policy — baseline

Tính theo `CancellationAcceptedAt` do Server ghi nhận và `Appointment.StartAt` (UTC):

`minutesBeforeAppointment = (StartAt - CancellationAcceptedAt).TotalMinutes`

| Điều kiện | Phí hủy trên phí dịch vụ | Hoàn trên phí dịch vụ đã thanh toán |
|---|---:|---:|
| `minutesBeforeAppointment >= 60` | 0% | 100% |
| `0 < minutesBeforeAppointment < 60` | 50% | 50% |
| `minutesBeforeAppointment <= 0` | Không tự hủy | Không tự hoàn |

- Đúng 60 phút thuộc nhóm hoàn 100%.
- Backend quyết định; không nhận refund rate/amount từ Frontend.
- Căn cứ giá dịch vụ snapshot lúc đặt và số tiền thực tế đã thanh toán.
- Với thanh toán toàn bộ: refund dự kiến = PaidServiceAmount × RefundRate.
- Partial payment và thu phí chưa thanh toán là TBD-04; không tự triển khai cách thu.
- Refund gateway là TBD; trạng thái Pending không được trình bày là tiền đã hoàn thành.
- Không áp dụng patient self-cancel sau khi lịch bắt đầu.

## 8. Patient identity and duplicate prevention

### 8.1 Required
- FullName đầy đủ.
- Ít nhất một trong DateOfBirth (đầy đủ) hoặc BirthYear.
- Số điện thoại liên hệ cần có cho OTP; phải xác minh trước khi lookup/change/cancel.

### 8.2 Additional identifiers
- CCCD/số định danh cá nhân.
- Hộ chiếu.
- Số điện thoại cá nhân chính chủ.
- Mã BHYT.
- PID/mã hồ sơ bệnh án cũ.

### 8.3 Matching policy
1. Chuẩn hóa định danh theo loại; giữ bản gốc được mã hóa.
2. Exact match theo định danh mạnh đã xác minh (CCCD, hộ chiếu, BHYT, PID) là tín hiệu mạnh; nhiều match/xung đột phải manual review.
3. Phone verified là tín hiệu đối sánh, nhưng số dùng chung hoặc xung đột phải review.
4. FullName + DOB/BirthYear chỉ tạo candidate; không đủ để auto-merge.
5. Nếu không có match chắc chắn, tạo Patient mới và có thể gắn cờ possible duplicate.
6. Chỉ người được phân quyền gộp; ghi actor, timestamp, reason và audit trail.

### 8.4 Sensitive identifiers
CCCD/hộ chiếu/BHYT/PID phải mã hóa khi lưu, che một phần khi hiển thị, giới hạn người truy cập. Dùng keyed HMAC của giá trị chuẩn hóa để exact lookup; không dùng plain SHA hash cho định danh có không gian giá trị dễ đoán. Không ghi định danh đầy đủ/OTP/bệnh án vào log.

## 9. Statuses
- Appointment: PendingConfirmation, Confirmed, CheckedIn, InProgress, Completed, Cancelled, NoShow.
- RescheduleRequest: PendingReview, Approved, Rejected, Cancelled, Expired.
- Refund: NotApplicable, Pending, Processing, Succeeded, Failed.

Mọi state transition phải kiểm tra actor/state ở Backend và ghi history.

## 10. Non-functional requirements
- Security: server-side authorization, password hasher chuyên dụng, OTP protection, mã hóa PII, HTTPS production, least privilege.
- Privacy: chỉ thu thập dữ liệu cần thiết; phân quyền hồ sơ/bệnh án; audit truy cập/thay đổi; retention policy là TBD theo quy định áp dụng.
- Integrity: transaction đặt/đổi/hủy; DB constraint chống overlap; unique BookingCode và định danh mạnh.
- Performance: pagination/index; mục tiêu đề xuất P95 ≤ 2 giây cho API đọc thông thường trong tải được định nghĩa và đo kiểm.
- Recovery: backup/restore có tài liệu; RPO/RTO là TBD.
- Usability: responsive, thông báo lỗi rõ ràng, keyboard access và label form.
- Maintainability: module rõ, DI, structured logging, test automation, migrations version-controlled.
- Compatibility: browser phổ biến hiện hành; desktop/tablet/mobile.

## 11. System acceptance criteria
- Guest đặt lịch thành công không có JWT/session.
- Patient có thể có `UserId = NULL`.
- FullName + DOB hoặc BirthYear được validate.
- Không auto-merge chỉ vì tên + ngày sinh trùng.
- Lookup/reschedule/cancel Guest yêu cầu OTP đúng purpose, chưa hết hạn, chưa dùng, đúng booking/phone.
- Reschedule chỉ cập nhật lịch sau Receptionist approve và slot còn trống; nếu lỗi, old slot giữ nguyên.
- Hủy 61 phút/đúng 60 phút: 100%; hủy 59 phút: 50%.
- Patient cancellation không cần Receptionist approve.
- Hai request đặt trùng cùng doctor/time không thể cùng thành công.
- Patient A không xem được hồ sơ/lịch của Patient B.
- Feature completed phải có test evidence thực tế.

## 12. TBD register
- **TBD-01:** Một phòng khám hay nhiều chi nhánh.
- **TBD-02:** OTP ngay lúc booking hay chỉ lookup/change/cancel (baseline hiện tại: chỉ lookup/change/cancel).
- **TBD-03:** Nhà cung cấp SMS/OTP, email, payment gateway.
- **TBD-04:** Thanh toán một phần và thu phí còn thiếu khi hủy.
- **TBD-05:** Cách xử lý khi phòng khám/bác sĩ hủy lịch đã thanh toán.
- **TBD-06:** Policy NoShow và phí NoShow.
- **TBD-07:** Thời hạn đặt trước, slot hold duration, giới hạn số lịch mỗi Patient.
- **TBD-08:** Thời lượng khám, buffer và capacity theo dịch vụ.
- **TBD-09:** Lưu trữ/xóa dữ liệu y tế, thời hạn audit.
- **TBD-10:** Bằng chứng và vai trò được phép xử lý hồ sơ nghi trùng.
- **TBD-11:** Phạm vi Guest/Patient xem lịch sử khám và nội dung y tế.
- **TBD-12:** Nhiều tiền tệ hay chỉ VND.

## 13. Out of scope
Không tự thêm chức năng chưa được liệt kê trong mục 4.1 vào Feature list nếu chưa được phê duyệt.
