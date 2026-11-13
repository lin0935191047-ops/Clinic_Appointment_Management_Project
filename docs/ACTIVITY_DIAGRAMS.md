Clinic Appointment Management System
- Version: 1.0
- Based on: SRS Version 1.1 and Use Case Specification Version 1.0
- Notation: PlantUML Activity Diagram
- Source files: diagrams/plantuml/
1. Activity Diagram – Đặt lịch khám không cần đăng nhập
Mục đích: Mô tả luồng đặt lịch công khai từ lúc Guest chọn dịch vụ đến khi nhận mã đặt lịch.
@startuml
|Guest|
start
:Mở trang đặt lịch;
:Xem chuyên khoa và bác sĩ;
:Chọn bác sĩ và dịch vụ;
:Chọn ngày khám;

|Frontend - React.js|
:Gửi yêu cầu lấy availability;

|Backend - ASP.NET Core|
:Kiểm tra bác sĩ/dịch vụ đang hoạt động;
:Đọc lịch làm việc, ngày nghỉ và lịch hẹn;
:Tạo danh sách khung giờ dự kiến;

|Frontend - React.js|
:Hiển thị khung giờ;

|Guest|
if (Có khung giờ phù hợp?) then (Có)
  :Chọn khung giờ;
  :Nhập họ tên và số điện thoại;
  :Nhập email/ngày sinh/giới tính/lý do khám nếu cần;
  :Kiểm tra và xác nhận thông tin;
else (Không)
  :Chọn ngày hoặc bác sĩ khác;
  stop
endif

|Frontend - React.js|
:Gửi POST booking request;

|Backend - ASP.NET Core|
:Validate dữ liệu đầu vào;
if (Dữ liệu hợp lệ?) then (Có)
  :Kiểm tra lại bác sĩ, dịch vụ,
  lịch làm việc và thời gian;
  if (Khung giờ vẫn khả dụng?) then (Có)
    :Bắt đầu transaction;
    :Khóa/kiểm soát xung đột khung giờ;
    :Tìm hồ sơ Patient theo quy tắc
    nhận diện an toàn;
    if (Có hồ sơ khớp đã xác minh?) then (Có)
      :Liên kết Patient hiện có;
    else (Không)
      :Tạo hồ sơ Patient mới;
    endif
    :Tạo BookingCode duy nhất;
    :Tạo Appointment - PendingConfirmation;
    :Ghi Audit/Appointment History;
    if (Lưu tất cả thành công?) then (Có)
      :Commit transaction;
      |Frontend - React.js|
      :Hiển thị xác nhận và BookingCode;
      |Guest|
      :Lưu mã đặt lịch;
      stop
    else (Không)
      |Backend - ASP.NET Core|
      :Rollback transaction;
      :Trả lỗi hệ thống;
      |Frontend - React.js|
      :Hiển thị thông báo thất bại;
      stop
    endif
  else (Không)
    :Trả lỗi 409 - Slot no longer available;
    |Frontend - React.js|
    :Thông báo chọn khung giờ khác;
    stop
  endif
else (Không)
  :Trả lỗi validation;
  |Frontend - React.js|
  :Hiển thị lỗi tại trường dữ liệu;
  stop
endif
@enduml
Điểm kiểm soát kỹ thuật: availability hiển thị ở Frontend chỉ là dữ liệu tham khảo tại thời điểm truy vấn. Backend phải kiểm tra lại khi tạo booking. Transaction/constraint ở PostgreSQL phải ngăn hai request cùng chiếm một khoảng thời gian của bác sĩ.
2. Activity Diagram – Tra cứu lịch bằng mã đặt lịch
Mục đích: Guest tra cứu lịch mà không cần tài khoản nhưng phải chứng minh quyền truy cập.
@startuml
|Guest|
start
:Mở trang Tra cứu lịch;
:Nhập BookingCode;
:Nhập số điện thoại đã cung cấp;
:Gửi yêu cầu tra cứu;

|Frontend - React.js|
:Kiểm tra định dạng cơ bản;
:Gửi request tra cứu;

|Backend - ASP.NET Core|
:Áp dụng rate limiting;
:Validate dữ liệu request;
if (Request hợp lệ và chưa vượt giới hạn?) then (Có)
  :Tìm lịch theo BookingCode;
  :Đối chiếu số điện thoại đã chuẩn hóa;
  if (Thông tin khớp?) then (Có)
    if (Chính sách yêu cầu OTP?) then (Có)
      :Tạo và gửi OTP;
      |Guest|
      :Nhập OTP;
      |Frontend - React.js|
      :Gửi OTP xác minh;
      |Backend - ASP.NET Core|
      :Kiểm tra OTP, thời hạn và số lần thử;
      if (OTP hợp lệ?) then (Có)
        :Cho phép xem thông tin lịch cơ bản;
      else (Không)
        :Từ chối xác minh;
        |Frontend - React.js|
        :Hiển thị thông báo chung;
        stop
      endif
    else (Không)
      :Cho phép xem thông tin lịch cơ bản
      theo chính sách xác minh;
    endif
    |Frontend - React.js|
    :Hiển thị thông tin tối thiểu:
    bác sĩ, dịch vụ, ngày giờ, trạng thái;
    stop
  else (Không)
    :Ghi nhận lần tra cứu thất bại;
    :Trả thông báo chung;
    |Frontend - React.js|
    :Hiển thị không thể xác minh;
    stop
  endif
else (Không)
  :Từ chối request hoặc yêu cầu thử lại sau;
  |Frontend - React.js|
  :Hiển thị thông báo phù hợp;
  stop
endif
@enduml
Điểm kiểm soát bảo mật: Không tiết lộ riêng rằng BookingCode có tồn tại nhưng số điện thoại sai. Không hiển thị bệnh án hoặc kết quả khám qua chức năng tra cứu công khai. Lookup và thao tác đổi/hủy có thể dùng mức xác minh khác nhau; cần chốt trong Security Policy.
3. Activity Diagram – Đổi lịch hoặc hủy lịch
Mục đích: Mô tả hai nhánh xử lý sau khi xác minh Guest/Patient có quyền yêu cầu thay đổi lịch.
@startuml
|Guest / Patient|
start
:Mở chức năng quản lý lịch;
:Nhập BookingCode và thông tin xác minh;
:Gửi yêu cầu xác minh;

|Backend - ASP.NET Core|
:Xác minh quyền quản lý lịch;
if (Xác minh thành công?) then (Có)
  :Tải lịch hẹn và trạng thái hiện tại;
  if (Lịch còn được phép thay đổi?) then (Có)
    |Guest / Patient|
    if (Chọn hủy lịch?) then (Hủy)
      :Nhập lý do hủy nếu được yêu cầu;
      :Gửi yêu cầu hủy;
      |Backend - ASP.NET Core|
      :Kiểm tra thời hạn và chính sách hủy;
      if (Đủ điều kiện hủy?) then (Có)
        :Bắt đầu transaction;
        :Cập nhật trạng thái Cancelled;
        :Ghi AppointmentHistory và AuditLog;
        :Giải phóng khung giờ theo cơ chế dữ liệu;
        :Commit transaction;
        :Tạo thông báo hủy;
        |Frontend - React.js|
        :Hiển thị hủy lịch thành công;
        stop
      else (Không)
        :Từ chối hủy;
        |Frontend - React.js|
        :Hiển thị lý do không đủ điều kiện;
        stop
      endif
    else (Đổi lịch)
      :Chọn ngày/giờ khám mới;
      :Gửi yêu cầu đổi lịch;
      |Backend - ASP.NET Core|
      :Kiểm tra thời hạn đổi lịch;
      :Kiểm tra lịch bác sĩ và slot mới;
      if (Slot mới khả dụng?) then (Có)
        :Bắt đầu transaction;
        :Giữ/khóa slot mới;
        :Cập nhật StartAt và EndAt;
        :Ghi lịch cũ, lịch mới và người yêu cầu;
        :Áp dụng trạng thái sau đổi theo chính sách;
        if (Cập nhật thành công?) then (Có)
          :Commit transaction;
          :Tạo thông báo đổi lịch;
          |Frontend - React.js|
          :Hiển thị lịch mới;
          stop
        else (Không)
          :Rollback transaction;
          |Frontend - React.js|
          :Thông báo đổi lịch thất bại;
          note right
            Lịch cũ phải được giữ nguyên
            khi đổi lịch không thành công.
          end note
          stop
        endif
      else (Không)
        :Từ chối slot mới;
        |Frontend - React.js|
        :Yêu cầu chọn khung giờ khác;
        stop
      endif
    endif
  else (Không)
    :Từ chối thay đổi theo trạng thái/policy;
    |Frontend - React.js|
    :Hiển thị thông báo;
    stop
  endif
else (Không)
  :Từ chối truy cập;
  |Frontend - React.js|
  :Hiển thị không thể xác minh;
  stop
endif
@enduml
Điểm cần chốt trước khi code: thời hạn đổi/hủy; đổi lịch tự động hay chờ lễ tân duyệt; trạng thái sau đổi lịch; có yêu cầu OTP cho từng thao tác hay không.
4. Activity Diagram – Bác sĩ tiếp nhận và hoàn thành ca khám
Mục đích: Mô tả quy trình từ lúc bác sĩ mở danh sách lịch được phân công đến khi hoàn thành ca khám.
@startuml
|Doctor|
start
:Đăng nhập hệ thống;
:Mở lịch khám được phân công;
:Chọn một lịch hẹn;

|Backend - ASP.NET Core|
:Xác thực JWT và vai trò Doctor;
:Kiểm tra bác sĩ có được phân công lịch không;
:Kiểm tra quyền truy cập hồ sơ;
if (Có quyền và lịch hợp lệ?) then (Có)
  :Trả thông tin cần thiết của lịch;
  |Doctor|
  :Xem thông tin bệnh nhân;
  if (Lịch đã CheckedIn?) then (Có)
    :Chọn Bắt đầu khám;
    |Backend - ASP.NET Core|
    :Kiểm tra trạng thái hiện tại;
    :Cập nhật InProgress và ghi audit;
    |Doctor|
    :Thực hiện thăm khám;
    :Nhập triệu chứng, ghi nhận thăm khám,
    chẩn đoán và hướng dẫn;
    :Lưu kết quả khám;
    |Backend - ASP.NET Core|
    :Validate dữ liệu và quyền cập nhật;
    :Lưu Consultation;
    |Doctor|
    if (Đã đủ thông tin bắt buộc để hoàn tất?) then (Có)
      :Chọn Hoàn tất khám;
      |Backend - ASP.NET Core|
      :Kiểm tra lịch đang InProgress;
      :Kiểm tra dữ liệu Consultation;
      if (Điều kiện hoàn tất hợp lệ?) then (Có)
        :Cập nhật Appointment = Completed;
        :Ghi thời gian hoàn tất và AuditLog;
        :Lưu giao dịch;
        |Frontend - React.js|
        :Hiển thị hoàn thành ca khám;
        stop
      else (Không)
        :Từ chối hoàn tất;
        |Frontend - React.js|
        :Hiển thị các điều kiện còn thiếu;
        stop
      endif
    else (Không)
      :Tiếp tục bổ sung kết quả khám;
      stop
    endif
  else (Không)
    :Không cho bắt đầu khám;
    |Frontend - React.js|
    :Hiển thị lịch chưa được check-in;
    stop
  endif
else (Không)
  :Từ chối truy cập dữ liệu;
  |Frontend - React.js|
  :Hiển thị lỗi quyền hoặc lịch không hợp lệ;
  stop
endif
@enduml
Điểm kiểm soát: Không tin DoctorId do Frontend gửi lên. Backend lấy danh tính từ token và kiểm tra phân công ở Database. Doctor không được tự hoàn tất lịch chưa ở trạng thái InProgress.
5. Bảng liên kết Activity – Use Case – Feature
Activity	Use Case	Feature liên quan (dự kiến)
Đặt lịch Guest	UC-05, UC-06	Guest Booking, Availability, Conflict Prevention
Tra cứu lịch	UC-07	Booking Lookup and Verification
Đổi/hủy lịch	UC-08, UC-09	Reschedule, Cancellation, Appointment History
Quy trình khám	UC-14 đến UC-18	Doctor Schedule, Reception Check-in, Consultation


Khi feature_list.json được Agent phân rã thành Feature nhỏ, cập nhật bảng này bằng đúng Feature ID thực tế.
6. Quy ước dùng sơ đồ
- Các sơ đồ thể hiện luồng nghiệp vụ, không thay thế API contract hoặc thiết kế Database.
- Mọi quyết định ghi TBD phải được xác nhận trước khi triển khai Feature liên quan.
- Khi thay đổi quy tắc nghiệp vụ, cập nhật đồng thời SPEC.md, BUSINESS_RULES.md, Use Case, Activity Diagram, Feature và test case.
- Không coi nhánh thông báo/email là thành công nếu dịch vụ gửi thông báo chưa được cấu hình.