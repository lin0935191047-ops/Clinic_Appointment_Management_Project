# SOFTWARE REQUIREMENTS SPECIFICATION (SRS)
## Clinic Appointment Management System

- **Tên tiếng Việt:** Hệ thống quản lý và đặt lịch khám bệnh trực tuyến
- **Phiên bản:** 1.2
- **Trạng thái:** Baseline nghiệp vụ theo các quyết định đã xác nhận
- **Ngày cập nhật:** 13/11/2026
- **Đối tượng đọc:** Chủ dự án, BA, Developer, Tester, AI Agent

> **Lưu ý quan trọng:** Đây là nguồn yêu cầu sản phẩm chính. Nội dung TBD chưa được chốt; AI Agent tuyệt đối không tự biến TBD thành yêu cầu bắt buộc.

---

## 1. Mục đích (Purpose)

Tài liệu này mô tả chi tiết các yêu cầu nghiệp vụ, yêu cầu chức năng, yêu cầu phi chức năng và tiêu chí nghiệm thu cho hệ thống quản lý lịch khám của phòng khám. Tài liệu đóng vai trò làm cơ sở căn bản cho thiết kế trải nghiệm người dùng (UX), kiến trúc kỹ thuật (Architecture), thiết kế cơ sở dữ liệu (Database Design), đặc tả giao diện lập trình ứng dụng (API Specification), quá trình lập trình, kiểm thử tự động và nghiệm thu sản phẩm.

---

## 2. Tổng quan hệ thống (System Overview)

Hệ thống là một ứng dụng Web Application hiện đại gồm 3 thành phần chính:
- **Frontend:** Xây dựng bằng React.js (Single Page Application).
- **Backend:** Xây dựng bằng ASP.NET Core Web API theo kiến trúc Clean Architecture.
- **Cơ sở dữ liệu:** Hệ quản trị CSDL quan hệ PostgreSQL.

**Luồng hoạt động tổng quan:**
- **Khách vãng lai (Guest):** Có thể tra cứu thông tin giới thiệu phòng khám, danh bạ bác sĩ, danh mục dịch vụ, xem các khung giờ khám còn trống theo thời gian thực và đặt lịch khám mà không cần đăng nhập hay tạo tài khoản.
- **Tra cứu, đổi và hủy lịch của Guest:** Được bảo vệ an toàn thông qua mã xác thực một lần (OTP) gửi tới số điện thoại đã ghi nhận trên lịch hẹn.
- **Nhân viên lễ tân (Receptionist):** Xác nhận lịch hẹn mới (`PendingConfirmation`), duyệt yêu cầu đổi lịch, tiếp nhận và check-in bệnh nhân có mặt tại quầy, quản lý hàng đợi khám bệnh.
- **Bác sĩ (Doctor):** Quản lý ca khám được phân công, bắt đầu thăm khám và ghi nhận kết luận khám bệnh.
- **Quản trị viên (Admin):** Cấu hình hệ thống, quản lý danh mục chuyên khoa/dịch vụ, quản lý tài khoản người dùng, phân quyền và xem báo cáo thống kê.

---

## 3. Mục tiêu nghiệp vụ (Business Objectives)

- Số hóa toàn diện quy trình đặt lịch và quản lý lịch khám bệnh tại phòng khám.
- Giảm thiểu thao tác ghi sổ thủ công, gọi điện thoại xác nhận và loại bỏ hoàn toàn tình trạng lịch trùng (Zero Double Booking).
- Cho phép Guest đặt lịch nhanh chóng mà không cần qua các bước tạo tài khoản rườm rà.
- Quản lý hồ sơ bệnh nhân (`Patient`) độc lập hoàn toàn với tài khoản người dùng (`User`).
- Quy trình đổi lịch minh bạch có sự phê duyệt của lễ tân; chính sách hủy lịch và tính toán hoàn phí rõ ràng theo quy chuẩn mốc thời gian.
- Bảo vệ định danh cá nhân (PII) và dữ liệu y tế nhạy cảm của người bệnh.
- Cung cấp bộ công cụ làm việc hiệu quả, chuyên biệt cho Lễ tân, Bác sĩ và Ban quản trị.

---

## 4. Phạm vi dự án (Scope)

### 4.1 In Scope (Trong phạm vi triển khai)
1. **Trang công khai:** Giới thiệu phòng khám, danh mục chuyên khoa, danh bạ bác sĩ, bảng giá dịch vụ khám.
2. **Guest Booking:** Cho phép chọn chuyên khoa, bác sĩ, dịch vụ, ngày và khung giờ khám; nhập thông tin bệnh nhân; tạo lịch hẹn thành công mà không cần tài khoản.
3. **Quản lý hồ sơ Patient độc lập User:** Quản lý định danh y tế, phát hiện hồ sơ nghi trùng (duplicate candidates).
4. **Cơ chế OTP:** Phục vụ tra cứu lịch hẹn, gửi yêu cầu đổi lịch và thực hiện hủy lịch cho Guest.
5. **Vòng đời Lịch hẹn:** Trạng thái lịch khám rõ ràng, sinh mã đặt lịch duy nhất (`BookingCode`), lưu vết lịch sử chuyển đổi.
6. **Quy trình Đổi lịch (Reschedule):** Bệnh nhân gửi yêu cầu đề xuất ngày/giờ mới; Receptionist duyệt hoặc từ chối; lịch cũ và slot cũ giữ nguyên cho đến khi yêu cầu được phê duyệt thành công.
7. **Quy trình Hủy lịch & Hoàn phí (Cancellation & Refund):** Bệnh nhân có thể chủ động hủy lịch trước giờ hẹn; hệ thống tự động tính tỷ lệ hoàn phí minh bạch theo mốc 60 phút.
8. **Phân hệ Admin:** Quản trị tài khoản, vai trò (roles), hồ sơ bác sĩ, danh mục chuyên khoa, dịch vụ y tế, lịch làm việc định kỳ và ngày nghỉ/thời gian bận.
9. **Phân hệ Receptionist:** Quản lý hồ sơ bệnh nhân, xác nhận lịch mới, duyệt đổi lịch, check-in bệnh nhân tại quầy, điều phối hàng đợi.
10. **Phân hệ Doctor:** Xem danh sách lịch khám được phân công, bắt đầu ca khám, ghi nhận triệu chứng/chẩn đoán/dặn dò, hoàn tất ca khám.
11. **Hạ tầng thông báo:** Thiết kế theo mô hình trừu tượng (Abstraction); nhà cung cấp SMS/Email cụ thể là **TBD**.
12. **Báo cáo & Dashboard:** Bảng điều khiển thống kê cơ bản, nhật ký kiểm toán (Audit Log), bảo mật hệ thống, kiểm thử tự động và đóng gói triển khai.

### 4.2 Out of Scope mặc định (Ngoài phạm vi triển khai)
- Tư vấn khám chữa bệnh từ xa qua video (Video consultation / Telemedicine).
- Kết nối liên thông dữ liệu bệnh viện hoặc hệ thống y tế quốc gia.
- Kê đơn thuốc điện tử liên thông hệ thống dược quốc gia.
- Quản lý xét nghiệm cận lâm sàng hoặc hệ thống chẩn đoán hình ảnh chuyên sâu (PACS/LIS).
- Tích hợp cổng giám định Bảo hiểm y tế (BHYT) trực tiếp.
- Ứng dụng di động Native (iOS/Android Native App).
- Tích hợp cổng thanh toán trực tuyến (Payment Gateway) hoặc nhà cung cấp SMS cụ thể (chưa được chọn).
  > **Ghi chú về Thanh toán:** Hệ thống vẫn lưu trữ thông tin khoản phí dịch vụ đã thu, kết quả tính toán hoàn tiền và trạng thái hoàn tiền. Việc tích hợp tự động với cổng thanh toán điện tử là **TBD**; không tự ý giả định đã có nhà cung cấp.

---

## 5. Đối tượng người dùng & Quyền hạn (Actors & Permissions)

| Actor | Mô tả | Quyền hạn chính |
| :--- | :--- | :--- |
| **Guest** | Người dùng chưa đăng nhập hệ thống | Xem thông tin công khai, kiểm tra khung giờ trống, đặt lịch khám nhanh, tra cứu / yêu cầu đổi lịch / hủy lịch thông qua xác thực OTP. |
| **Patient** | Bệnh nhân có tài khoản đăng nhập (tùy chọn) | Đầy đủ quyền của Patient trong phạm vi dữ liệu và lịch hẹn của chính bản thân mình. |
| **Receptionist** | Nhân viên lễ tân phòng khám | Quản lý và rà soát hồ sơ bệnh nhân, xác nhận lịch mới (`PendingConfirmation`), phê duyệt yêu cầu đổi lịch, check-in bệnh nhân đến khám, điều phối hàng đợi. |
| **Doctor** | Bác sĩ khám bệnh | Xem danh sách lịch hẹn trong phạm vi được phân công, xem dữ liệu y tế cần thiết, bắt đầu khám và ghi nhận kết quả khám bệnh. |
| **Admin** | Quản trị viên hệ thống | Quản trị tài khoản người dùng, phân quyền vai trò, quản lý danh mục chuyên khoa, dịch vụ, cấu hình lịch làm việc/ngày nghỉ, xem báo cáo thống kê. |

> **Quy tắc bắt buộc:** Việc kiểm tra quyền hạn (Authorization) **luôn luôn phải được thực hiện ở Backend API**. Việc ẩn/hiện nút bấm trên giao diện Frontend chỉ nhằm tối ưu trải nghiệm và tuyệt đối không thay thế được cơ chế kiểm soát quyền ở Backend.

---

## 6. Yêu cầu chức năng chi tiết (Functional Requirements)

### 6.1 Danh mục công khai (Public Catalog)
- **FR-001:** Guest có thể xem thông tin giới thiệu chung, địa chỉ và thông tin liên hệ của phòng khám.
- **FR-002:** Guest có thể xem, tìm kiếm và lọc danh sách các chuyên khoa khám bệnh.
- **FR-003:** Guest có thể xem, tìm kiếm danh sách bác sĩ và hồ sơ chuyên môn công khai của từng bác sĩ.
- **FR-004:** Guest có thể xem danh mục dịch vụ khám, mô tả chi tiết, thời lượng khám dự kiến và giá dịch vụ niêm yết công khai.
- **FR-005:** Guest có thể tra cứu các khung giờ khám còn khả dụng theo ngày; hệ thống không được để lộ lịch hẹn hoặc thông tin định danh cá nhân (PII) của bệnh nhân khác.

### 6.2 Quản lý Tài khoản (Accounts & Identity)
- **FR-006:** Patient có thể đăng ký tài khoản tùy chọn trên hệ thống.
- **FR-007:** Patient, Doctor, Receptionist, Admin có thể đăng nhập và đăng xuất hệ thống an toàn.
- **FR-008:** Hệ thống xác thực bằng Access Token và Refresh Token (nếu được triển khai).
- **FR-009:** Admin có quyền quản lý tài khoản người dùng và gán vai trò tương ứng.
- **FR-010:** Người dùng có thể đặt lại mật khẩu thông qua phương thức xác minh được cấu hình trong hệ thống.

### 6.3 Hồ sơ Bệnh nhân & Quản lý Định danh (Patient Profile & Identity Resolution)
- **FR-011:** Hệ thống hỗ trợ tạo hồ sơ bệnh nhân (`Patient`) mà không bắt buộc phải tạo tài khoản người dùng (`User`). Trường `UserId` trong bảng `Patient` có thể nhận giá trị `NULL`.
- **FR-012:** Mỗi hồ sơ bệnh nhân bắt buộc phải có `FullName` (Họ và tên) và ít nhất một trong hai thông tin: `DateOfBirth` (Ngày tháng năm sinh đầy đủ) hoặc `BirthYear` (Năm sinh).
- **FR-013:** Lưu trữ số điện thoại liên hệ; số điện thoại dùng để nhận mã OTP bắt buộc phải được xác minh thành công.
- **FR-014:** Hệ thống có khả năng lưu trữ các định danh mở rộng: CCCD / Số định danh cá nhân, Hộ chiếu, Số thẻ BHYT, Mã hồ sơ y tế cũ (PID).
- **FR-015:** Hệ thống hỗ trợ tìm kiếm các hồ sơ ứng viên nghi trùng lặp dựa trên các trường định danh đã chuẩn hóa và dữ liệu nhân khẩu học.
- **FR-016:** **Không tự động hợp nhất (auto-merge)** hồ sơ chỉ dựa trên Họ tên + Ngày sinh/Năm sinh trùng nhau; các hồ sơ nghi trùng lặp phải được gắn cờ để nhân viên có thẩm quyền kiểm tra và xác minh thủ công.
- **FR-017:** Mọi thao tác tạo mới, chỉnh sửa thông tin, liên kết tài khoản hoặc hợp nhất hồ sơ bệnh nhân đều phải được ghi vết kiểm toán (Audit Log); chỉ các vai trò được phân quyền mới được phép thực hiện.

### 6.4 Đặt lịch cho Khách vãng lai (Guest Booking)
- **FR-018:** Cho phép Guest đặt lịch khám thành công mà không cần đăng nhập hay tạo tài khoản người dùng.
- **FR-019:** Biểu mẫu đặt lịch bắt buộc phải có: `FullName`, `DateOfBirth` hoặc `BirthYear`, Số điện thoại liên hệ; các trường thông tin khác theo cấu hình phòng khám.
- **FR-020:** Guest được chọn Chuyên khoa, Bác sĩ, Dịch vụ khám, Ngày khám và Khung giờ khả dụng.
- **FR-021:** Backend bắt buộc kiểm tra tính hợp lệ của Bác sĩ, Dịch vụ, Lịch làm việc, Ngày nghỉ và Khung giờ trống ngay tại thời điểm ghi dữ liệu (Atomic Check).
- **FR-022:** Tạo hồ sơ `Patient` mới hoặc liên kết với hồ sơ bệnh nhân đã được xác minh trước đó; không tự ý tạo tài khoản `User`.
- **FR-023:** Hệ thống tự động sinh mã đặt lịch (`BookingCode`) có tính duy nhất trên toàn hệ thống, khó đoán và không chứa thông tin định danh cá nhân (PII).
- **FR-024:** Lưu trữ bản chụp (Snapshot) thông tin dịch vụ, thời lượng khám dự kiến và giá dịch vụ niêm yết tại chính thời điểm đặt lịch.
- **FR-025:** Trả về thông tin xác nhận đặt lịch cho người dùng; gửi thông báo qua kênh thông báo nếu đã được cấu hình.

### 6.5 Cơ chế OTP & Tra cứu Lịch hẹn (OTP & Lookup)
- **FR-026:** Guest có thể yêu cầu gửi mã xác thực OTP về số điện thoại gắn liền với lịch hẹn đã đặt.
- **FR-027:** Mỗi mã OTP phải có mục đích rõ ràng (`Purpose`: `LOOKUP`, `RESCHEDULE`, `CANCEL`), có thời hạn hiệu lực (Expiry), giới hạn số lần nhập sai (Attempt limit) và giới hạn tần suất gửi lại (Resend limit).
- **FR-028:** Mã OTP chỉ được sử dụng một lần duy nhất (Single-use), gắn chặt với mã booking, mục đích sử dụng và phiên thử thách (Challenge).
- **FR-029:** Khi xác thực OTP thành công, hệ thống cấp quyền tạm thời với phạm vi giới hạn theo đúng mã booking và mục đích đã chỉ định.
- **FR-030:** Guest có thể tra cứu thông tin lịch hẹn bằng `BookingCode` kết hợp xác thực OTP; hệ thống không hiển thị đầy đủ thông tin bệnh án chi tiết hay định danh đầy đủ ra ngoài màn hình tra cứu công khai.

### 6.6 Vòng đời Lịch hẹn (Appointment Lifecycle)
- **FR-031:** Hệ thống quản lý lịch hẹn thông qua các trạng thái chuẩn:
  - `PendingConfirmation`: Chờ lễ tân xác nhận.
  - `Confirmed`: Đã xác nhận ca khám.
  - `CheckedIn`: Bệnh nhân đã có mặt tại phòng khám.
  - `InProgress`: Bác sĩ đang tiến hành thăm khám.
  - `Completed`: Ca khám đã hoàn thành.
  - `Cancelled`: Lịch hẹn đã bị hủy.
  - `NoShow`: Bệnh nhân không đến khám theo lịch đã hẹn.
- **FR-032:** Nhân viên lễ tân có quyền xác nhận lịch hẹn ở trạng thái `PendingConfirmation` chuyển sang `Confirmed`.
- **FR-033:** Bệnh nhân đã đăng nhập có thể xem danh sách lịch khám của chính mình; Guest chỉ có thể xem chi tiết sau khi xác thực OTP thành công.
- **FR-034:** Mọi thao tác chuyển đổi trạng thái lịch hẹn phải được kiểm duyệt chặt chẽ ở Backend và lưu vết lịch sử chuyển đổi trạng thái đầy đủ.
- **FR-035:** Tuyệt đối không cho phép tạo lịch hẹn ngoài giờ làm việc, trong ngày nghỉ của bác sĩ, trong khoảng thời gian đã bận hoặc vượt quá thời hạn đặt trước được cấu hình.

### 6.7 Quy trình Đổi lịch khám (Reschedule)
- **FR-036:** Guest hoặc Patient có thể gửi yêu cầu đổi lịch sau khi đã xác thực OTP hoặc có phiên đăng nhập hợp lệ.
- **FR-037:** Bệnh nhân đề xuất ngày và khung giờ khám mới; hệ thống kiểm tra sơ bộ tính khả dụng của khung giờ đề xuất.
- **FR-038:** Yêu cầu đổi lịch được quản lý bằng các trạng thái:
  - `PendingReview`: Chờ lễ tân phê duyệt.
  - `Approved`: Đã chấp thuận đổi lịch.
  - `Rejected`: Bị từ chối đổi lịch.
  - `Cancelled`: Người gửi chủ động hủy yêu cầu đổi lịch.
  - `Expired`: Yêu cầu hết hạn xử lý.
- **FR-039:** Nhân viên lễ tân có quyền duyệt hoặc từ chối yêu cầu đổi lịch, bắt buộc ghi nhận lý do, người xử lý và thời điểm xử lý.
- **FR-040:** **Lịch hẹn cũ và khung giờ cũ vẫn được giữ nguyên vẹn** và tiếp tục chiếm giữ slot cho đến khi yêu cầu đổi lịch được phê duyệt thành công.
- **FR-041:** Khi lễ tân bấm Duyệt, Backend bắt buộc phải kiểm tra lại tính khả dụng của slot mới trong một Transaction CSDL. Nếu slot mới không còn trống, hệ thống không được phép đổi và giữ nguyên lịch cũ.
- **FR-042:** Mỗi lịch hẹn (`Appointment`) tại một thời điểm chỉ được phép có tối đa một yêu cầu đổi lịch ở trạng thái `PendingReview`.

### 6.8 Quy trình Hủy lịch & Hoàn phí (Cancellation & Refund)
- **FR-043:** Guest hoặc Patient có thể chủ động hủy lịch hẹn trước giờ khám bắt đầu sau khi xác thực OTP hoặc qua phiên đăng nhập hợp lệ.
- **FR-044:** **Không cần nhân viên lễ tân duyệt** đối với các yêu cầu hủy lịch do chính bệnh nhân chủ động thực hiện.
- **FR-045:** Backend bắt buộc ghi nhận thời điểm hủy theo dấu thời gian thực tế của máy chủ (Server timestamp - UTC), tuyệt đối không tin tưởng đồng hồ của thiết bị Client.
- **FR-046:** Nếu thời điểm hủy cách giờ hẹn khám **từ 60 phút trở lên (bao gồm đúng mốc 60 phút)**: Hoàn 100% phí dịch vụ đã thanh toán, phí hủy lịch là 0%.
- **FR-047:** Nếu thời điểm hủy cách giờ hẹn khám **dưới 60 phút nhưng trước giờ hẹn**: Hoàn 50% phí dịch vụ đã thanh toán, phí hủy lịch là 50%.
- **FR-048:** Hệ thống lưu trữ giá dịch vụ snapshot, số tiền đã thanh toán thực tế, tỷ lệ phí hủy, số tiền phí hủy, số tiền dự kiến hoàn, lý do hủy, dấu thời gian và trạng thái hoàn tiền.
- **FR-049:** Nếu lịch hẹn chưa thanh toán khoản phí nào thì số tiền hoàn trả (`RefundAmount`) = 0; hệ thống không tự ý phát sinh việc thu các khoản phí chưa được chính sách phê duyệt.
- **FR-050:** Tại thời điểm hoặc sau khi giờ hẹn bắt đầu, lịch hẹn không còn thuộc luồng bệnh nhân tự hủy; việc xử lý sẽ do Lễ tân can thiệp hoặc hệ thống ghi nhận `NoShow`.
- **FR-051:** Trạng thái hoàn tiền (`RefundStatus`) được quản lý gồm:
  - `NotApplicable`: Không áp dụng hoàn phí (chưa thanh toán hoặc phí hủy 100%).
  - `Pending`: Đang chờ xử lý hoàn tiền.
  - `Processing`: Đang trong tiến trình chuyển hoàn.
  - `Succeeded`: Hoàn tiền thành công.
  - `Failed`: Hoàn tiền thất bại.

### 6.9 Quản lý Lịch làm việc & Ca trực (Schedule Management)
- **FR-052:** Quản trị viên quản lý cấu hình lịch làm việc định kỳ theo tuần của từng bác sĩ.
- **FR-053:** Quản trị viên hoặc Lễ tân được cấp quyền có thể quản lý ngày nghỉ lễ, ngày nghỉ phép, giờ nghỉ và các khoảng thời gian bận đột xuất của bác sĩ.
- **FR-054:** Hệ thống tự động sinh các khung giờ khám khả dụng theo thời lượng dịch vụ và cấu hình ca trực.
- **FR-055:** Mọi thao tác chỉnh sửa lịch làm việc không được phép âm thầm xóa bỏ hoặc tự ý làm sai lệch các lịch hẹn đã được xác nhận của bệnh nhân.

### 6.10 Tiếp đón & Khám bệnh (Reception & Consultation)
- **FR-056:** Nhân viên lễ tân có thể tìm kiếm, tạo mới hoặc cập nhật hồ sơ bệnh nhân theo quyền hạn.
- **FR-057:** Lễ tân thực hiện thao tác Check-in cho các lịch hẹn hợp lệ khi bệnh nhân có mặt tại phòng khám.
- **FR-058:** Lễ tân quản lý và theo dõi danh sách hàng đợi khám bệnh trong ngày.
- **FR-059:** Bác sĩ có thể xem danh sách lịch hẹn được phân công cho mình và truy cập các thông tin y tế cần thiết.
- **FR-060:** Bác sĩ thực hiện thao tác chuyển trạng thái ca khám từ `CheckedIn` sang `InProgress` khi bắt đầu khám.
- **FR-061:** Bác sĩ ghi nhận thông tin triệu chứng ban đầu, kết quả thăm khám lâm sàng, chẩn đoán sơ bộ, kết luận và hướng dẫn dặn dò bệnh nhân.
- **FR-062:** Bác sĩ hoàn tất ca khám, chuyển trạng thái lịch hẹn từ `InProgress` sang `Completed`.

### 6.11 Thông báo & Báo cáo thống kê (Notification & Reporting)
- **FR-063:** Gửi thông báo khi lịch hẹn được đặt, xác nhận, đổi lịch hoặc hủy lịch theo các kênh thông báo được cấu hình.
- **FR-064:** Tự động gửi thông báo nhắc lịch khám theo cấu hình thời gian; có cơ chế chống gửi trùng lặp và ghi nhận trạng thái gửi thông báo.
- **FR-065:** Bảng điều khiển (Dashboard) của Admin hiển thị thống kê tổng quan về số lượng lịch hẹn, bác sĩ, bệnh nhân và phân bố trạng thái.
- **FR-066:** Bảng điều khiển của Bác sĩ hiển thị danh sách các lịch hẹn trong phạm vi được phân công theo ca làm việc.
- **FR-067:** Báo cáo quản trị hỗ trợ lọc linh hoạt theo khoảng thời gian, bác sĩ, chuyên khoa và trạng thái lịch hẹn.

---

## 7. Chính sách Hủy lịch & Hoàn phí — Baseline (Cancellation and Refund Policy)

Mốc thời gian hủy được tính toán tự động dựa trên thời điểm máy chủ tiếp nhận hủy (`CancellationAcceptedAt`) và thời điểm bắt đầu ca khám (`Appointment.StartAt`) theo chuẩn UTC:

$$\text{minutesBeforeAppointment} = (\text{StartAt} - \text{CancellationAcceptedAt}).\text{TotalMinutes}$$

### Bảng quy tắc hoàn phí chuẩn:

| Điều kiện thời gian | Phí hủy (tính trên phí dịch vụ) | Tỷ lệ hoàn (trên số tiền dịch vụ đã thanh toán) |
| :--- | :--- | :--- |
| $\text{minutesBeforeAppointment} \ge 60$ | **0%** | **100%** |
| $0 < \text{minutesBeforeAppointment} < 60$ | **50%** | **50%** |
| $\text{minutesBeforeAppointment} \le 0$ | *Không áp dụng tự hủy* | *Không áp dụng tự hoàn tiền* |

### Các nguyên tắc áp dụng:
- Thời điểm hủy **đúng mốc 60 phút** được tính thuộc nhóm được **hoàn 100%**.
- Toàn bộ phép tính tỷ lệ và số tiền hoàn phí phải do **Backend quyết định độc lập**, tuyệt đối không nhận tham số tỷ lệ hoàn (`refundRate`) hay số tiền hoàn (`refundAmount`) do Frontend gửi lên.
- Căn cứ tính toán dựa trên giá dịch vụ snapshot tại thời điểm đặt lịch và số tiền thực tế bệnh nhân đã thanh toán.
- Trường hợp đã thanh toán đủ:
  $$\text{ExpectedRefundAmount} = \text{PaidServiceAmount} \times \text{RefundRate}$$
- Trường hợp thanh toán một phần (Partial payment) hoặc chính sách truy thu khoản phí chưa thanh toán thuộc phạm vi **TBD-04**; hệ thống hiện tại không tự tiện triển khai các phương thức truy thu này.
- Cơ chế cổng thanh toán chuyển hoàn tự động là **TBD**; khi trạng thái hoàn là `Pending`, giao diện không được thông báo sai lệch rằng tiền đã chuyển về tài khoản người dùng thành công.
- Không áp dụng luồng tự hủy của bệnh nhân sau khi ca khám đã đến giờ bắt đầu.

---

## 8. Quản lý Định danh Bệnh nhân & Chống Trùng lặp (Patient Identity & Duplicate Prevention)

### 8.1 Thông tin bắt buộc (Required Fields)
- `FullName`: Họ và tên đầy đủ của bệnh nhân.
- Tối thiểu một trong hai trường: `DateOfBirth` (ngày sinh đầy đủ) hoặc `BirthYear` (năm sinh).
- Số điện thoại liên hệ: Cần có để gửi mã OTP; bắt buộc phải xác minh OTP thành công trước khi cho phép tra cứu (Lookup), đổi lịch (Reschedule) hoặc hủy lịch (Cancel).

### 8.2 Định danh mở rộng (Additional Identifiers)
- Số Căn cước công dân (CCCD) hoặc Số định danh cá nhân.
- Số Hộ chiếu (Passport).
- Số điện thoại cá nhân chính chủ.
- Mã số thẻ Bảo hiểm y tế (BHYT).
- Mã hồ sơ bệnh án cũ (PID).

### 8.3 Chính sách đối sánh hồ sơ (Matching Policy)
1. Chuẩn hóa định danh theo từng loại dữ liệu; lưu trữ bản gốc có mã hóa an toàn.
2. Trùng khớp chính xác (Exact match) theo định danh mạnh đã được xác minh (CCCD, Hộ chiếu, BHYT, PID) là tín hiệu đối sánh mạnh nhất; trường hợp phát hiện nhiều bản ghi khớp hoặc có xung đột dữ liệu thì bắt buộc phải chuyển sang kiểm tra thủ công (Manual Review).
3. Số điện thoại đã xác minh (Phone verified) là tín hiệu đối sánh quan trọng, tuy nhiên các trường hợp số điện thoại dùng chung cho gia đình hoặc có tranh chấp thông tin phải được đưa vào danh sách kiểm tra.
4. Trùng khớp Họ tên + Ngày sinh/Năm sinh (`FullName` + `DOB`/`BirthYear`) **chỉ tạo ra hồ sơ ứng viên nghi ngờ (Candidate Duplicate)**, tuyệt đối không đủ điều kiện để tự động gộp hồ sơ (No Auto-merge).
5. Nếu không có kết quả đối sánh chắc chắn, hệ thống tạo một hồ sơ `Patient` mới và có thể gắn cờ nghi ngờ trùng lặp để nhân viên rà soát sau.
6. Chỉ những nhân viên được phân quyền rõ ràng mới được phép thực hiện thao tác gộp hồ sơ; quá trình gộp bắt buộc ghi nhận danh tính người thực hiện (`Actor`), thời điểm (`Timestamp`), lý do gộp (`Reason`) và nhật ký kiểm toán (`Audit Trail`).

### 8.4 Bảo vệ các định danh nhạy cảm (Sensitive Identifiers)
- Dữ liệu CCCD, Hộ chiếu, Thẻ BHYT, Mã bệnh án cũ (PID) bắt buộc phải được mã hóa khi lưu trữ trong cơ sở dữ liệu.
- Phải che một phần thông tin (Masking, ví dụ: `001******892`) khi hiển thị trên giao diện người dùng.
- Giới hạn nghiêm ngặt các vai trò được phép giải mã và xem dữ liệu gốc.
- Sử dụng mã băm có khóa bí mật (`Keyed HMAC`) của giá trị đã chuẩn hóa để phục vụ tra cứu chính xác (`Exact Lookup`); tuyệt đối không dùng mã băm đơn giản (`Plain SHA-256`) đối với các trường định danh có không gian giá trị dễ đoán.
- Tuyệt đối không ghi số định danh đầy đủ, mã OTP thuần hoặc nội dung bệnh án chi tiết vào hệ thống Log.

---

## 9. Bảng Trạng thái Hệ thống (System Statuses)

### 9.1 Trạng thái Lịch hẹn (Appointment Status)
- `PendingConfirmation`: Chờ nhân viên lễ tân xác nhận ca khám.
- `Confirmed`: Lịch hẹn đã được xác nhận thành công.
- `CheckedIn`: Bệnh nhân đã đến quầy và hoàn tất thủ tục check-in.
- `InProgress`: Bác sĩ đang tiến hành khám cho bệnh nhân.
- `Completed`: Ca khám bệnh đã hoàn tất.
- `Cancelled`: Lịch hẹn đã bị hủy (bởi bệnh nhân hoặc phòng khám).
- `NoShow`: Bệnh nhân không đến khám theo đúng lịch hẹn.

### 9.2 Trạng thái Yêu cầu Đổi lịch (Reschedule Request Status)
- `PendingReview`: Đang chờ nhân viên lễ tân xem xét và phê duyệt.
- `Approved`: Yêu cầu đổi lịch đã được chấp thuận, slot mới được áp dụng.
- `Rejected`: Yêu cầu đổi lịch bị từ chối, lịch cũ tiếp tục được giữ nguyên.
- `Cancelled`: Người gửi đã chủ động hủy bỏ yêu cầu đổi lịch này.
- `Expired`: Yêu cầu đổi lịch bị quá hạn xử lý và tự động đóng.

### 9.3 Trạng thái Hoàn tiền (Refund Status)
- `NotApplicable`: Không phát sinh hoàn phí (chưa đóng phí hoặc phí hủy 100%).
- `Pending`: Đã ghi nhận yêu cầu hoàn tiền, chờ lệnh xử lý.
- `Processing`: Đang trong tiến trình chuyển khoản hoàn tiền cho bệnh nhân.
- `Succeeded`: Giao dịch hoàn tiền đã thực hiện thành công.
- `Failed`: Quá trình hoàn tiền phát sinh lỗi kỹ thuật cần xử lý.

> **Quy định chuyển trạng thái:** Mọi thao tác chuyển đổi trạng thái (State Transition) bắt buộc phải được kiểm tra tính hợp lệ về vai trò (`Actor`) và trạng thái nguồn (`Source State`) tại Backend; đồng thời ghi nhận lịch sử thay đổi trạng thái đầy đủ.

---

## 10. Yêu cầu phi chức năng (Non-Functional Requirements)

- **Bảo mật (Security):** Toàn bộ cơ chế phân quyền thực thi tại Server-side; sử dụng thư viện băm mật khẩu chuyên dụng (Argon2id/BCrypt); cơ chế bảo vệ mã OTP nghiêm ngặt (chống Brute-force/Rate limit); mã hóa dữ liệu nhạy cảm PII; bắt buộc HTTPS trên môi trường Production; tuân thủ nguyên tắc đặc quyền tối thiểu (Least Privilege).
- **Quyền riêng tư Dữ liệu (Privacy):** Chỉ thu thập những trường thông tin thực sự cần thiết cho hoạt động khám bệnh; phân quyền truy cập hồ sơ bệnh án theo ca trực của bác sĩ; ghi vết kiểm toán (Audit Trail) cho mọi thao tác đọc/ghi dữ liệu nhạy cảm; chính sách lưu trữ dữ liệu y tế tuân thủ mục **TBD-09**.
- **Tính toàn vẹn Dữ liệu (Data Integrity):** Sử dụng CSDL Transaction cho các tác vụ Đặt lịch, Đổi lịch và Hủy lịch; tạo Unique / Exclusion Constraint ở tầng CSDL để ngăn chặn triệt để đặt trùng khung giờ (Zero Double Booking); đảm bảo tính duy nhất của mã `BookingCode` và các định danh mạnh.
- **Hiệu năng & Tải (Performance):** Bắt buộc phân trang và đánh chỉ mục tối ưu trên các bảng nghiệp vụ lớn; mục tiêu đề xuất là thời gian phản hồi ở phân vị P95 $\le$ 2 giây cho các API đọc thông thường trong điều kiện tải được định nghĩa và kiểm thử.
- **Sao lưu & Khôi phục (Recovery):** Quy trình sao lưu và phục hồi dữ liệu định kỳ phải được tài liệu hóa rõ ràng; chỉ số RPO và RTO chi tiết là **TBD**.
- **Tính khả dụng & Trải nghiệm (Usability):** Giao diện tương thích đa thiết bị (Responsive Mobile, Tablet, Desktop); thông báo lỗi rõ ràng, dễ hiểu cho người bệnh; hỗ trợ điều hướng bàn phím và nhãn đầy đủ cho các trường nhập liệu.
- **Khả năng bảo trì & Mở rộng (Maintainability):** Phân chia module rõ ràng theo Clean Architecture; áp dụng Dependency Injection (DI); ghi log có cấu trúc (Structured Logging); xây dựng hệ thống kiểm thử tự động (Unit/Integration Tests); quản lý phiên bản cơ sở dữ liệu bằng EF Core Migrations.
- **Tính tương thích (Compatibility):** Tương thích tốt với các trình duyệt web hiện đại phổ biến (Chrome, Edge, Firefox, Safari) trên cả máy tính để bàn, máy tính bảng và điện thoại thông minh.

---

## 11. Tiêu chí Nghiệm thu Hệ thống (System Acceptance Criteria)

Hệ thống được coi là đạt yêu cầu nghiệm thu khi thỏa mãn toàn bộ các tiêu chí cốt lõi sau:
1. **Đặt lịch vãng lai không cần tài khoản:** Guest có thể hoàn tất quy trình đặt lịch khám thành công mà không có JWT token hay phiên đăng nhập trước đó.
2. **Hồ sơ bệnh nhân độc lập:** Thực thể `Patient` có thể tồn tại hợp lệ với giá trị `UserId = NULL`.
3. **Kiểm tra thông tin bắt buộc:** Hệ thống kiểm tra và xác thực hợp lệ trường `FullName` cùng ít nhất một trong hai trường `DateOfBirth` hoặc `BirthYear`.
4. **Không tự động gộp hồ sơ:** Tuyệt đối không tự ý sáp nhập hai hồ sơ bệnh nhân chỉ vì trùng khớp Họ tên và Ngày sinh/Năm sinh.
5. **Bảo vệ xác thực OTP:** Mọi thao tác Tra cứu, Đổi lịch, Hủy lịch của Guest bắt buộc phải có mã OTP hợp lệ: đúng mục đích (`Purpose`), còn thời hạn, chưa từng sử dụng, đúng số điện thoại và đúng mã booking.
6. **Bảo toàn slot khi Đổi lịch:** Quy trình đổi lịch chỉ cập nhật lịch mới sau khi Receptionist bấm Duyệt VÀ khung giờ mới còn trống; nếu phát sinh lỗi, khung giờ cũ của lịch hẹn cũ vẫn được giữ nguyên vẹn.
7. **Độ chính xác phép tính Hoàn tiền:**
   - Hủy trước giờ hẹn từ 60 phút trở lên (bao gồm đúng mốc 60 phút): Hoàn tiền 100%, phí hủy 0%.
   - Hủy trước giờ hẹn dưới 60 phút: Hoàn tiền 50%, phí hủy 50%.
8. **Quyền tự hủy của người bệnh:** Thao tác hủy lịch do bệnh nhân chủ động thực hiện không yêu cầu sự phê duyệt từ phía Lễ tân.
9. **Chống trùng lịch tuyệt đối:** Hai request đặt lịch gửi đồng thời cho cùng một bác sĩ trong cùng một khung giờ không bao giờ được phép cùng thành công.
10. **Phân quyền dữ liệu nghiêm ngặt:** Bệnh nhân A tuyệt đối không thể xem được hồ sơ bệnh án hoặc lịch hẹn của Bệnh nhân B.
11. **Bằng chứng kiểm thử:** Mọi tính năng được đánh dấu hoàn thành bắt buộc phải có bằng chứng kiểm thử thực tế (Test Evidence / Passing Tests).

---

## 12. Danh mục các điểm chưa chốt (TBD Register)

Các điểm sau đây hiện tại ở trạng thái **TBD (To Be Determined)** và chưa được chốt quyết định chính thức; AI Agent và đội ngũ phát triển không được tự ý biến các điểm này thành yêu cầu bắt buộc:

- **TBD-01:** Mô hình phòng khám hoạt động tại một cơ sở duy nhất hay hệ thống chuỗi nhiều chi nhánh.
- **TBD-02:** Yêu cầu xác thực OTP ngay tại thời điểm tạo booking ban đầu hay chỉ yêu cầu khi tra cứu, đổi và hủy lịch (Quy chuẩn baseline hiện tại: chỉ bắt buộc OTP cho Tra cứu / Đổi / Hủy).
- **TBD-03:** Nhà cung cấp dịch vụ SMS OTP, dịch vụ gửi Email thông báo và Cổng thanh toán điện tử cụ thể.
- **TBD-04:** Cơ chế thanh toán một phần (Partial payment) và chính sách thu khoản phí còn thiếu khi phát sinh hủy lịch.
- **TBD-05:** Quy trình xử lý và bồi hoàn khi phòng khám hoặc bác sĩ chủ động hủy ca khám đã được thanh toán.
- **TBD-06:** Chính sách chế tài đối với trường hợp bỏ hẹn (`NoShow`) và phí phạt No-Show.
- **TBD-07:** Cấu hình cụ thể về số ngày được đặt trước tối đa, thời gian tạm giữ slot (`Slot hold duration`), và giới hạn số lượng lịch hẹn tối đa của mỗi bệnh nhân.
- **TBD-08:** Thời lượng khám chi tiết, thời gian đệm giữa các ca (`Buffer time`) và công suất phục vụ (`Capacity`) theo từng loại dịch vụ.
- **TBD-09:** Quy định pháp lý chi tiết về thời hạn lưu trữ/xóa dữ liệu y tế và thời hạn lưu giữ nhật ký kiểm toán (Audit Log).
- **TBD-10:** Tiêu chuẩn bằng chứng xác minh và danh sách vai trò nhân viên cụ thể được phép xử lý các hồ sơ nghi ngờ trùng lặp.
- **TBD-11:** Phạm vi chi tiết của việc Guest và Patient được phép truy cập xem lại lịch sử khám và nội dung y tế tóm tắt.
- **TBD-12:** Hệ thống hỗ trợ đa tiền tệ hay chỉ sử dụng duy nhất Việt Nam Đồng (VND).

---

## 13. Ngoài phạm vi triển khai (Out of Scope)

Tuyệt đối **không tự ý bổ sung** các tính năng chưa được liệt kê trong mục 4.1 vào danh sách tính năng triển khai (`feature_list.json` hoặc kế hoạch phát triển) nếu chưa có sự phê duyệt chính thức từ Chủ dự án.
