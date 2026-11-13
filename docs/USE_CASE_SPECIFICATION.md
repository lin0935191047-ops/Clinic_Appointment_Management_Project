Clinic Appointment Management System
- Version: 1.0
- Based on: SRS Version 1.1
- Status: Proposed design; policy decisions marked TBD remain subject to confirmation.
- Primary language: Vietnamese; identifiers retained in English for implementation consistency.
1. Mục đích
Tài liệu mô tả các tác nhân và Use Case chính của Clinic Appointment Management System. Đây là đầu vào cho thiết kế Activity Diagram, ERD, API và kiểm thử.

Quyết định nghiệp vụ đã chốt: Guest được xem thông tin công khai, kiểm tra khung giờ và đặt lịch khám mà không cần đăng nhập hoặc tạo tài khoản. Hồ sơ Patient có thể tồn tại độc lập với tài khoản User.
2. Tác nhân (Actors)
Actor	Mô tả	Quyền/Phạm vi chính
Guest	Người truy cập chưa đăng nhập	Xem thông tin công khai, kiểm tra lịch trống, đặt lịch, tra cứu/đề nghị đổi hoặc hủy lịch sau xác minh
Patient	Bệnh nhân có tài khoản (tùy chọn)	Có thể dùng chức năng công khai; ngoài ra quản lý hồ sơ và xem lịch sử thuộc tài khoản sau đăng nhập
Receptionist	Nhân viên lễ tân	Quản lý hồ sơ bệnh nhân, lịch hẹn, xác nhận, check-in và hàng đợi theo phân quyền
Doctor	Bác sĩ	Xem lịch được phân công, tiếp nhận ca khám, ghi nhận nội dung khám và hoàn tất ca khám
Admin	Quản trị viên	Quản lý người dùng, bác sĩ, chuyên khoa, dịch vụ, lịch làm việc, cấu hình và báo cáo


Lưu ý mô hình hóa: Guest và Patient cùng tương tác với các chức năng đặt lịch công khai. Patient là hồ sơ bệnh nhân; tài khoản User chỉ là phương thức đăng nhập tùy chọn, không phải điều kiện để có hồ sơ bệnh nhân.
3. Danh mục Use Case
Các mã UC-01 đến UC-20 được giữ tương thích với danh mục Use Case trong SRS phiên bản 1.1. UC-21 trở đi là các Use Case hỗ trợ được bổ sung để mô tả đầy đủ nghiệp vụ vận hành.
ID	Use Case	Actor chính	Phân loại
UC-01	Đăng ký tài khoản tùy chọn	Guest	Authentication
UC-02	Đăng nhập	Patient, Receptionist, Doctor, Admin	Authentication
UC-03	Xem chuyên khoa	Guest, Patient	Public
UC-04	Tìm kiếm/xem bác sĩ	Guest, Patient	Public
UC-05	Xem khung giờ còn trống	Guest, Patient	Public
UC-06	Đặt lịch khám không cần đăng nhập	Guest, Patient	Public booking
UC-07	Tra cứu lịch bằng mã đặt lịch	Guest, Patient	Public + verification
UC-08	Đề nghị đổi lịch	Guest, Patient	Public + verification
UC-09	Đề nghị hủy lịch	Guest, Patient	Public + verification
UC-10	Quản lý bác sĩ	Admin	Admin
UC-11	Quản lý chuyên khoa/dịch vụ	Admin	Admin
UC-12	Quản lý lịch làm việc/ngày nghỉ	Admin, Receptionist (được cấp quyền)	Admin/Staff
UC-13	Xác nhận lịch hẹn	Receptionist	Staff
UC-14	Check-in bệnh nhân	Receptionist	Staff
UC-15	Quản lý hàng đợi	Receptionist, Doctor	Staff
UC-16	Bắt đầu ca khám	Doctor	Staff
UC-17	Ghi nhận kết quả khám	Doctor	Staff
UC-18	Hoàn tất ca khám	Doctor	Staff
UC-19	Xem báo cáo thống kê	Admin	Admin
UC-20	Quản lý người dùng và vai trò	Admin	Admin
UC-21	Quản lý hồ sơ cá nhân	Patient	Authenticated
UC-22	Xem lịch/lịch sử của tài khoản	Patient	Authenticated
UC-23	Quản lý hồ sơ bệnh nhân	Receptionist	Staff
UC-24	Tạo lịch tại quầy	Receptionist	Staff
UC-25	Xem lịch được phân công	Doctor	Staff
UC-26	Quản lý tất cả lịch hẹn	Admin, Receptionist	Admin/Staff
UC-27	Quản lý cấu hình nghiệp vụ	Admin	Admin


4. Use Case Diagram tổng thể
Mã nguồn PlantUML: diagrams/plantuml/use-case-overview.puml.
@startuml
left to right direction
skinparam packageStyle rectangle
skinparam actorStyle awesome

actor Guest
actor Patient
actor Receptionist
actor Doctor
actor Admin

rectangle "Clinic Appointment Management System" {
  usecase "UC-03\nXem chuyên khoa" as UC03
  usecase "UC-04\nTìm kiếm / xem bác sĩ" as UC04
  usecase "UC-05\nXem khung giờ trống" as UC05
  usecase "UC-06\nĐặt lịch khám không cần đăng nhập" as UC06
  usecase "UC-07\nTra cứu lịch hẹn" as UC07
  usecase "UC-08\nĐề nghị đổi lịch" as UC08
  usecase "UC-09\nĐề nghị hủy lịch" as UC09
  usecase "UC-01\nĐăng ký tài khoản tùy chọn" as UC01
  usecase "UC-02\nĐăng nhập" as UC02
  usecase "UC-21\nQuản lý hồ sơ cá nhân" as UC21
  usecase "UC-22\nXem lịch / lịch sử của tôi" as UC22
  usecase "Xác minh quyền quản lý lịch" as UCV
  usecase "Kiểm tra khung giờ khả dụng" as UCA
  usecase "UC-23\nQuản lý hồ sơ bệnh nhân" as UC23
  usecase "UC-24\nTạo lịch tại quầy" as UC24
  usecase "UC-13\nXác nhận lịch hẹn" as UC13
  usecase "UC-14\nCheck-in bệnh nhân" as UC14
  usecase "UC-15\nQuản lý hàng đợi" as UC15
  usecase "UC-25\nXem lịch được phân công" as UC25
  usecase "UC-16\nBắt đầu ca khám" as UC16
  usecase "UC-17\nGhi nhận kết quả khám" as UC17
  usecase "UC-18\nHoàn tất ca khám" as UC18
  usecase "UC-10\nQuản lý bác sĩ" as UC10
  usecase "UC-11\nQuản lý chuyên khoa / dịch vụ" as UC11
  usecase "UC-12\nQuản lý lịch làm việc / ngày nghỉ" as UC12
  usecase "UC-20\nQuản lý người dùng / vai trò" as UC20
  usecase "UC-26\nQuản lý tất cả lịch hẹn" as UC26
  usecase "UC-19\nXem báo cáo thống kê" as UC19
  usecase "UC-27\nQuản lý cấu hình nghiệp vụ" as UC27
}

Guest -- UC03
Guest -- UC04
Guest -- UC05
Guest -- UC06
Guest -- UC07
Guest -- UC08
Guest -- UC09
Guest -- UC01
Patient -- UC03
Patient -- UC04
Patient -- UC05
Patient -- UC06
Patient -- UC07
Patient -- UC08
Patient -- UC09
Patient -- UC02
Patient -- UC21
Patient -- UC22
Receptionist -- UC02
Receptionist -- UC23
Receptionist -- UC24
Receptionist -- UC13
Receptionist -- UC14
Receptionist -- UC15
Receptionist -- UC12
Receptionist -- UC26
Doctor -- UC02
Doctor -- UC15
Doctor -- UC25
Doctor -- UC16
Doctor -- UC17
Doctor -- UC18
Admin -- UC02
Admin -- UC10
Admin -- UC11
Admin -- UC12
Admin -- UC20
Admin -- UC26
Admin -- UC19
Admin -- UC27

UC06 ..> UCA : <<include>>
UC07 ..> UCV : <<include>>
UC08 ..> UCV : <<include>>
UC09 ..> UCV : <<include>>
UC24 ..> UCA : <<include>>
@enduml
5. Đặc tả Use Case quan trọng
UC-06 – Đặt lịch khám không cần đăng nhập
- Actor chính: Guest.
- Actor phụ: Patient (nếu đã có tài khoản), hệ thống thông báo (nếu được cấu hình).
- Mục tiêu: Tạo một lịch hẹn khám mà không yêu cầu đăng nhập.
- Tiền điều kiện: Dịch vụ và bác sĩ đang hoạt động; khung giờ có thể được đặt; hệ thống nhận request công khai.
- Kích hoạt: Guest chọn chức năng Đặt lịch.
- Dữ liệu bắt buộc tối thiểu: Họ tên và số điện thoại; dữ liệu khác theo cấu hình nghiệp vụ.
Luồng chính:
1. Guest chọn chuyên khoa, bác sĩ và dịch vụ.
2. Guest chọn ngày khám.
3. Hệ thống hiển thị các khung giờ dự kiến còn trống.
4. Guest chọn một khung giờ.
5. Guest nhập họ tên, số điện thoại và các trường tùy chọn.
6. Guest xem lại và xác nhận thông tin.
7. Backend xác thực dữ liệu, kiểm tra bác sĩ/dịch vụ/lịch làm việc/ngày nghỉ và kiểm tra xung đột lần cuối.
8. Backend tạo hoặc liên kết hồ sơ Patient theo quy tắc nhận diện hồ sơ an toàn.
9. Backend tạo Appointment và mã đặt lịch duy nhất trong giao dịch dữ liệu.
10. Hệ thống trả kết quả và mã đặt lịch cho Guest.
11. Hệ thống gửi thông báo nếu kênh thông báo được cấu hình.
Luồng thay thế/ngoại lệ:
- A1: Thông tin bắt buộc thiếu hoặc sai định dạng → trả lỗi validation, không tạo Appointment.
- A2: Khung giờ không còn trống → trả lỗi xung đột, yêu cầu chọn giờ khác.
- A3: Bác sĩ/dịch vụ ngừng hoạt động → từ chối đặt lịch.
- A4: Request đồng thời tranh cùng khung giờ → chỉ một giao dịch được thành công.
- A5: Lưu dữ liệu thất bại → rollback toàn bộ thay đổi liên quan.
Hậu điều kiện thành công: Có PatientId hợp lệ, Appointment được tạo, BookingCode duy nhất được cấp, lịch sử thao tác được ghi nhận.

Hậu điều kiện thất bại: Không tồn tại Appointment được tạo dở dang và khung giờ không bị giữ sai.
UC-07 – Tra cứu lịch hẹn
- Actor: Guest hoặc Patient.
- Mục tiêu: Xem thông tin cơ bản của một lịch đã đặt.
- Tiền điều kiện: Người dùng có mã đặt lịch và thông tin xác minh theo chính sách.
- Luồng chính: Nhập BookingCode và số điện thoại → hệ thống kiểm tra giới hạn request → xác minh cặp thông tin → (nếu cấu hình) xác minh OTP → hiển thị thông tin tối thiểu cần thiết.
- Ngoại lệ: Không khớp/không tồn tại/hết hạn xác minh → trả thông báo chung, không tiết lộ lịch có tồn tại hay không.
- Bảo mật: Không hiển thị bệnh án hoặc dữ liệu sức khỏe chi tiết trong chức năng tra cứu công khai.
UC-08 – Đề nghị đổi lịch
- Actor: Guest hoặc Patient.
- Tiền điều kiện: Xác minh được quyền quản lý lịch; lịch đang ở trạng thái cho phép đổi; còn trong thời hạn đổi.
- Luồng chính: Xác minh → chọn ngày/giờ mới → kiểm tra tính khả dụng → cập nhật lịch trong một giao dịch an toàn → ghi AppointmentHistory → thông báo kết quả.
- Ngoại lệ: Khung giờ mới hết chỗ hoặc lỗi lưu → giữ nguyên lịch cũ.
- TBD: Xác nhận đổi lịch tự động hay cần Receptionist duyệt; trạng thái sau đổi lịch.
UC-09 – Đề nghị hủy lịch
- Actor: Guest hoặc Patient.
- Tiền điều kiện: Xác minh quyền quản lý lịch; lịch chưa ở trạng thái kết thúc; còn trong thời hạn hủy.
- Luồng chính: Xác minh → nhập lý do (nếu bắt buộc) → kiểm tra chính sách hủy → cập nhật Cancelled → ghi lịch sử → giải phóng khung giờ → gửi thông báo.
- Ngoại lệ: Không đủ điều kiện hủy → không thay đổi lịch.
- TBD: Thời hạn hủy và có cần lễ tân phê duyệt hay không.
UC-13 – Xác nhận lịch hẹn
- Actor: Receptionist.
- Tiền điều kiện: Đã đăng nhập và có quyền; lịch đang ở PendingConfirmation.
- Luồng chính: Mở danh sách chờ → xem chi tiết → xác nhận → backend kiểm tra trạng thái hiện tại → cập nhật Confirmed và thông tin người xác nhận → gửi thông báo.
- Ngoại lệ: Lịch đã hủy/được xử lý bởi request khác → từ chối cập nhật và tải lại trạng thái.
UC-16/17/18 – Quy trình khám bệnh
- Actor: Doctor.
- Tiền điều kiện: Bác sĩ đã đăng nhập, được phân công lịch; lịch ở trạng thái CheckedIn.
- Luồng chính: Mở lịch được phân công → kiểm tra quyền và trạng thái → bắt đầu khám (InProgress) → nhập nội dung/chẩn đoán/hướng dẫn → lưu → hoàn tất (Completed).
- Ngoại lệ: Bác sĩ không được phân công hoặc lịch không ở trạng thái hợp lệ → từ chối.
- Bảo mật: Chỉ người có quyền nghiệp vụ phù hợp được đọc/ghi Consultation.
6. Quy tắc bảo mật liên quan đến Guest
1. API xem danh sách chuyên khoa, bác sĩ và availability chỉ trả dữ liệu công khai.
2. API tạo booking công khai phải validation toàn bộ dữ liệu ở Backend; không tin dữ liệu kiểm tra từ Frontend.
3. BookingCode phải không dễ đoán và có unique constraint.
4. Lookup/reschedule/cancel phải xác minh quyền sở hữu; không dùng BookingCode đơn lẻ làm bằng chứng.
5. Rate limit các endpoint công khai để giảm dò mã và spam booking.
6. Trả thông báo lỗi chung khi cặp BookingCode/Phone không khớp.
7. Không đưa thông tin sức khỏe nhạy cảm vào URL, log không bảo vệ hoặc nội dung thông báo công khai.
8. Không tự động gộp hai hồ sơ bệnh nhân chỉ dựa trên số điện thoại nếu chưa có quy tắc xác minh an toàn.
7. Các quyết định còn TBD
Mã	Nội dung cần chốt	Gợi ý mặc định để thảo luận
TBD-UC-03	Xác minh tra cứu	BookingCode + số điện thoại; OTP cho thao tác thay đổi
TBD-UC-04	Cách gửi OTP	SMS hoặc email; phụ thuộc nhà cung cấp và chi phí
TBD-UC-05	Đổi lịch	Cho phép đổi tự động khi đủ điều kiện hay tạo yêu cầu chờ lễ tân
TBD-UC-06	Hủy lịch	Khách tự hủy trong thời hạn cấu hình hay phải lễ tân duyệt
TBD-UC-07	Ghép hồ sơ Patient	Cần quy tắc chống trùng và quy trình xác minh khi tìm thấy hồ sơ tương tự


8. Traceability
Mỗi Use Case phải liên kết đến ít nhất một yêu cầu FR trong docs/SPEC.md, một hoặc nhiều Feature trong planning/feature_list.json, API tương ứng và test case. Khi thêm/sửa Use Case, phải rà soát đồng bộ các tài liệu liên quan.