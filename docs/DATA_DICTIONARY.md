# DATA DICTIONARY — Clinic Appointment Management System

- **Version:** 1.2
- **Source:** `docs/DATABASE_DESIGN.md` v1.2
- **Convention:** PostgreSQL `snake_case`; UUID PK/FK; timestamps stored as UTC `timestamptz`.
- **Important:** This dictionary describes the proposed logical schema. Review and approve before EF Core migrations.

## Legend
- **PK:** Primary Key; **FK:** Foreign Key; **UK:** Unique Key.
- `NULL` means optional. Required columns are `NOT NULL`.
- Sensitive values use application-layer encryption and keyed HMAC where described.

## `roles` — Vai trò hệ thống

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh vai trò |
| `code` | `varchar(40)` | No | UNIQUE | ADMIN, DOCTOR, RECEPTIONIST, PATIENT |
| `name` | `varchar(100)` | No |  | Tên hiển thị |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `users` — Tài khoản đăng nhập

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh tài khoản |
| `email` | `varchar(254)` | No |  | Email hiển thị |
| `email_normalized` | `varchar(254)` | No | UNIQUE | Email chuẩn hóa dùng đăng nhập |
| `password_hash` | `text` | No |  | Hash do password hasher tạo |
| `is_active` | `boolean` | No | DEFAULT true | Tài khoản hoạt động |
| `email_verified_at` | `timestamptz` | Yes |  | Thời điểm xác minh email |
| `last_login_at` | `timestamptz` | Yes |  | Lần đăng nhập gần nhất |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `user_roles` — Bảng nối User và Role

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `user_id` | `uuid` | No | PK, FK -> users.id | Tài khoản |
| `role_id` | `uuid` | No | PK, FK -> roles.id | Vai trò |
| `assigned_at` | `timestamptz` | No |  | Thời điểm gán vai trò |

## `refresh_tokens` — Refresh token rotation/revocation

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh token |
| `user_id` | `uuid` | No | FK -> users.id | Chủ tài khoản |
| `token_hash` | `bytea` | No | UNIQUE | Chỉ lưu hash token |
| `expires_at` | `timestamptz` | No |  | Thời điểm hết hạn |
| `revoked_at` | `timestamptz` | Yes |  | Thời điểm thu hồi |
| `replaced_by_token_id` | `uuid` | Yes | FK -> refresh_tokens.id | Token thay thế nếu rotate |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `patients` — Hồ sơ bệnh nhân, độc lập với User

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh hồ sơ |
| `patient_code` | `varchar(32)` | No | UNIQUE | Mã bệnh nhân nội bộ |
| `user_id` | `uuid` | Yes | FK -> users.id; partial UNIQUE khi khác NULL | Tài khoản tùy chọn; Guest có thể NULL |
| `full_name` | `varchar(200)` | No |  | Họ tên đầy đủ |
| `normalized_full_name` | `varchar(200)` | No | INDEX tùy nhu cầu | Tên chuẩn hóa để tìm ứng viên; không unique |
| `date_of_birth` | `date` | Yes | CHECK cùng birth_year | Ngày/tháng/năm sinh nếu có |
| `birth_year` | `smallint` | Yes | CHECK cùng date_of_birth | Năm sinh nếu chưa có ngày đầy đủ |
| `gender` | `varchar(30)` | Yes |  | Theo danh mục cấu hình |
| `address` | `text` | Yes |  | Địa chỉ nếu được thu thập |
| `record_status` | `varchar(30)` | No | DEFAULT ACTIVE | ACTIVE, REVIEW_REQUIRED, INACTIVE, MERGED |
| `merged_into_patient_id` | `uuid` | Yes | FK -> patients.id | Hồ sơ đích nếu đã merge |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `patient_contacts` — Kênh liên hệ; số phone/email được mã hóa

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK; UNIQUE cùng patient_id | Định danh contact |
| `patient_id` | `uuid` | No | FK -> patients.id | Hồ sơ sở hữu contact |
| `contact_type` | `varchar(20)` | No | CHECK PHONE/EMAIL | Loại liên hệ |
| `value_ciphertext` | `bytea` | No |  | Giá trị liên hệ đã mã hóa |
| `value_hmac` | `bytea` | No | HMAC keyed | Dò khớp exact mà không lưu plaintext |
| `masked_value` | `varchar(100)` | No |  | Dạng che khi hiển thị |
| `is_primary` | `boolean` | No | DEFAULT false | Contact chính theo loại |
| `is_verified` | `boolean` | No | DEFAULT false | Đã xác minh khả năng truy cập |
| `verified_at` | `timestamptz` | Yes |  | Thời điểm xác minh |
| `is_personal_owner_verified` | `boolean` | No | DEFAULT false; CHECK chỉ PHONE + is_verified | Đã xác minh số điện thoại cá nhân chính chủ |
| `is_active` | `boolean` | No | DEFAULT true | Contact còn hiệu lực |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `patient_identifiers` — Định danh chính thức/định danh hồ sơ

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh record |
| `patient_id` | `uuid` | No | FK -> patients.id | Hồ sơ bệnh nhân |
| `identifier_type` | `varchar(30)` | No | CHECK NATIONAL_ID/PASSPORT/HEALTH_INSURANCE/LEGACY_PID | Loại giấy tờ/mã |
| `issuer_code` | `varchar(40)` | No |  | Quốc gia/cơ quan/hệ thống cấp |
| `value_ciphertext` | `bytea` | No |  | Giá trị gốc đã mã hóa |
| `normalized_value_hmac` | `bytea` | No | Keyed HMAC | Dò exact match |
| `masked_value` | `varchar(100)` | No |  | Giá trị che |
| `verification_status` | `varchar(20)` | No | DEFAULT UNVERIFIED | UNVERIFIED, PENDING, VERIFIED, REJECTED |
| `verified_at` | `timestamptz` | Yes |  | Thời điểm xác minh |
| `is_active` | `boolean` | No | DEFAULT true | Định danh còn hiệu lực |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `patient_identity_review_cases` — Hồ sơ cần nhân viên rà soát trùng/khác người

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh review case |
| `patient_id` | `uuid` | No | FK -> patients.id | Hồ sơ đang được kiểm tra |
| `candidate_patient_id` | `uuid` | No | FK -> patients.id | Hồ sơ ứng viên |
| `match_reason` | `varchar(40)` | No |  | Lý do: identifier/phone/name+DOB/name+year/conflict |
| `status` | `varchar(30)` | No |  | OPEN, CONFIRMED_SAME_PERSON, DIFFERENT_PERSON, NEEDS_MORE_INFO |
| `reviewed_by_user_id` | `uuid` | Yes | FK -> users.id | Nhân viên xử lý |
| `reviewed_at` | `timestamptz` | Yes |  | Thời điểm xử lý |
| `review_note` | `text` | Yes |  | Ghi chú đã kiểm soát dữ liệu |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `patient_merge_events` — Lịch sử gộp hồ sơ có kiểm soát

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh sự kiện |
| `source_patient_id` | `uuid` | No | FK -> patients.id | Hồ sơ nguồn |
| `target_patient_id` | `uuid` | No | FK -> patients.id | Hồ sơ chính/đích |
| `merged_by_user_id` | `uuid` | No | FK -> users.id | Nhân viên thực hiện |
| `reason` | `text` | No |  | Lý do bắt buộc |
| `before_snapshot` | `jsonb` | No | Redacted only | Ảnh chụp metadata trước khi merge |
| `merged_at` | `timestamptz` | No |  | Thời điểm merge |

## `specialties` — Danh mục chuyên khoa

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `code` | `varchar(40)` | No | UNIQUE | Mã chuyên khoa |
| `name` | `varchar(150)` | No |  | Tên chuyên khoa |
| `slug` | `varchar(180)` | No | UNIQUE | Slug URL |
| `description` | `text` | Yes |  | Mô tả |
| `is_active` | `boolean` | No | DEFAULT true | Đang hoạt động |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `doctors` — Hồ sơ bác sĩ

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh bác sĩ |
| `user_id` | `uuid` | No | UNIQUE FK -> users.id | Tài khoản bác sĩ |
| `doctor_code` | `varchar(32)` | No | UNIQUE | Mã bác sĩ nội bộ |
| `full_name` | `varchar(200)` | No |  | Họ tên |
| `license_number_ciphertext` | `bytea` | Yes |  | Số giấy phép đã mã hóa |
| `license_number_hmac` | `bytea` | Yes | HMAC | Tìm exact giấy phép nếu cần |
| `bio` | `text` | Yes |  | Giới thiệu chuyên môn |
| `years_experience` | `smallint` | Yes | CHECK >= 0 | Số năm kinh nghiệm |
| `is_active` | `boolean` | No | DEFAULT true | Trạng thái làm việc |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `doctor_specialties` — Quan hệ nhiều-nhiều Doctor và Specialty

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `doctor_id` | `uuid` | No | PK, FK -> doctors.id | Bác sĩ |
| `specialty_id` | `uuid` | No | PK, FK -> specialties.id | Chuyên khoa |
| `is_primary` | `boolean` | No | DEFAULT false | Chuyên khoa chính |
| `created_at` | `timestamptz` | No |  | Thời điểm gán |

## `services` — Danh mục dịch vụ khám

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh dịch vụ |
| `specialty_id` | `uuid` | No | FK -> specialties.id | Chuyên khoa |
| `code` | `varchar(40)` | No | UNIQUE | Mã dịch vụ |
| `name` | `varchar(180)` | No |  | Tên dịch vụ |
| `description` | `text` | Yes |  | Mô tả |
| `duration_minutes` | `integer` | No | CHECK > 0 | Thời lượng mặc định |
| `price` | `numeric(12,2)` | No | CHECK >= 0 | Giá niêm yết |
| `currency` | `char(3)` | No | DEFAULT VND | Đơn vị tiền tệ |
| `is_active` | `boolean` | No | DEFAULT true | Trạng thái |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `doctor_services` — Dịch vụ bác sĩ được phép thực hiện

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `doctor_id` | `uuid` | No | PK, FK -> doctors.id | Bác sĩ |
| `service_id` | `uuid` | No | PK, FK -> services.id | Dịch vụ |
| `duration_minutes_override` | `integer` | Yes | CHECK > 0 nếu có | Thời lượng riêng nếu khác mặc định |
| `price_override` | `numeric(12,2)` | Yes | CHECK >= 0 nếu có | Giá riêng nếu có |
| `is_active` | `boolean` | No | DEFAULT true | Có nhận dịch vụ này không |

## `doctor_working_rules` — Lịch làm việc định kỳ theo thứ

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh quy tắc |
| `doctor_id` | `uuid` | No | FK -> doctors.id | Bác sĩ |
| `day_of_week` | `smallint` | No | CHECK 1..7 ISO | Thứ trong tuần, Monday=1 |
| `start_local_time` | `time` | No | CHECK < end | Giờ bắt đầu địa phương |
| `end_local_time` | `time` | No | CHECK > start | Giờ kết thúc địa phương |
| `effective_from` | `date` | No |  | Ngày bắt đầu áp dụng |
| `effective_to` | `date` | Yes | >= effective_from | Ngày kết thúc áp dụng |
| `is_active` | `boolean` | No | DEFAULT true | Còn áp dụng |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `doctor_time_offs` — Khoảng nghỉ/nghỉ phép bác sĩ

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `doctor_id` | `uuid` | No | FK -> doctors.id | Bác sĩ |
| `start_at` | `timestamptz` | No | CHECK < end | Bắt đầu nghỉ UTC |
| `end_at` | `timestamptz` | No | CHECK > start | Kết thúc nghỉ UTC |
| `reason` | `text` | Yes |  | Lý do |
| `status` | `varchar(20)` | No | ACTIVE/CANCELLED | Trạng thái |
| `created_by_user_id` | `uuid` | No | FK -> users.id | Người tạo |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `doctor_schedule_blocks` — Khoảng bận cụ thể như họp/break

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `doctor_id` | `uuid` | No | FK -> doctors.id | Bác sĩ |
| `start_at` | `timestamptz` | No | CHECK < end | Bắt đầu |
| `end_at` | `timestamptz` | No | CHECK > start | Kết thúc |
| `block_type` | `varchar(30)` | No | BREAK/MEETING/MAINTENANCE/OTHER | Loại block |
| `reason` | `text` | Yes |  | Ghi chú |
| `created_by_user_id` | `uuid` | No | FK -> users.id | Người tạo |
| `is_active` | `boolean` | No | DEFAULT true | Còn hiệu lực |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `appointments` — Lịch hẹn khám — nguồn dữ liệu giữ slot

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh lịch |
| `booking_code` | `varchar(32)` | No | UNIQUE | Mã tra cứu ngẫu nhiên, không chứa PII |
| `patient_id` | `uuid` | No | FK -> patients.id | Bệnh nhân |
| `booking_contact_id` | `uuid` | No | Composite FK cùng patient_id -> patient_contacts(id,patient_id) | Số liên hệ tại thời điểm đặt/đích OTP |
| `doctor_id` | `uuid` | No | FK -> doctors.id | Bác sĩ |
| `service_id` | `uuid` | No | FK -> services.id | Dịch vụ |
| `patient_name_snapshot` | `varchar(200)` | No |  | Tên tại thời điểm đặt |
| `service_name_snapshot` | `varchar(180)` | No |  | Tên dịch vụ tại thời điểm đặt |
| `duration_minutes_snapshot` | `integer` | No | CHECK > 0 | Thời lượng snapshot |
| `service_fee_snapshot` | `numeric(12,2)` | No | CHECK >= 0 | Giá snapshot |
| `currency_snapshot` | `char(3)` | No |  | Tiền tệ snapshot |
| `start_at` | `timestamptz` | No | CHECK < end | Giờ bắt đầu UTC |
| `end_at` | `timestamptz` | No | CHECK > start | Giờ kết thúc UTC |
| `status` | `varchar(30)` | No | State machine | PendingConfirmation/Confirmed/CheckedIn/InProgress/Completed/Cancelled/NoShow |
| `booking_channel` | `varchar(30)` | No | PUBLIC_WEB/PATIENT_PORTAL/RECEPTION_DESK | Kênh tạo |
| `reason` | `text` | Yes | Sensitive | Lý do khám ban đầu |
| `notes` | `text` | Yes | Sensitive | Ghi chú |
| `created_by_user_id` | `uuid` | Yes | FK -> users.id | NULL nếu Guest |
| `confirmed_by_user_id` | `uuid` | Yes | FK -> users.id | Lễ tân xác nhận |
| `confirmed_at` | `timestamptz` | Yes |  | Thời điểm xác nhận |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `appointment_histories` — Nhật ký thay đổi trạng thái lịch

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `appointment_id` | `uuid` | No | FK -> appointments.id | Lịch hẹn |
| `from_status` | `varchar(30)` | Yes |  | Trạng thái trước |
| `to_status` | `varchar(30)` | No |  | Trạng thái sau |
| `actor_type` | `varchar(30)` | No | GUEST/PATIENT/DOCTOR/RECEPTIONIST/ADMIN/SYSTEM | Loại người thực hiện |
| `actor_user_id` | `uuid` | Yes | FK -> users.id | User nếu có |
| `actor_patient_id` | `uuid` | Yes | Composite FK cùng appointment_id -> appointments(id, patient_id) | Patient nếu có; phải là chủ booking |
| `reason` | `text` | Yes |  | Lý do |
| `metadata_redacted` | `jsonb` | Yes | Redacted only | Metadata không chứa dữ liệu nhạy cảm thô |
| `created_at` | `timestamptz` | No |  | Thời điểm ghi |

## `reschedule_requests` — Yêu cầu đổi lịch chờ lễ tân duyệt

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh yêu cầu |
| `appointment_id` | `uuid` | No | FK -> appointments.id | Lịch hiện tại |
| `requested_by_patient_id` | `uuid` | No | Composite FK cùng appointment_id -> appointments(id, patient_id) | Chủ booking gửi yêu cầu |
| `otp_grant_id` | `uuid` | Yes | Composite FK cùng appointment_id -> otp_verification_grants(id, appointment_id); UNIQUE khi khác NULL | Grant RESCHEDULE nếu Guest; backend kiểm tra purpose |
| `old_start_at` | `timestamptz` | No |  | Giờ cũ snapshot |
| `old_end_at` | `timestamptz` | No |  | Giờ cũ snapshot |
| `proposed_start_at` | `timestamptz` | No | CHECK < proposed_end | Giờ đề xuất mới |
| `proposed_end_at` | `timestamptz` | No | CHECK > proposed_start | Kết thúc đề xuất |
| `status` | `varchar(30)` | No | PendingReview/Approved/Rejected/Cancelled/Expired | Trạng thái duyệt |
| `patient_note` | `text` | Yes |  | Ghi chú bệnh nhân |
| `reviewed_by_user_id` | `uuid` | Yes | FK -> users.id | Lễ tân xử lý |
| `reviewed_at` | `timestamptz` | Yes |  | Thời điểm duyệt |
| `review_note` | `text` | Yes |  | Lý do duyệt/từ chối |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `cancellation_policy_versions` — Phiên bản chính sách hủy/hoàn phí

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh policy |
| `version` | `varchar(30)` | No | UNIQUE | Mã phiên bản policy |
| `threshold_minutes` | `integer` | No | DEFAULT 60 | Ngưỡng phút |
| `refund_rate_at_or_above` | `numeric(5,4)` | No | DEFAULT 1.0000; 0..1 | Tỷ lệ hoàn nếu >= ngưỡng |
| `refund_rate_below` | `numeric(5,4)` | No | DEFAULT 0.5000; 0..1 | Tỷ lệ hoàn nếu < ngưỡng và trước giờ hẹn |
| `cancellation_fee_rate_at_or_above` | `numeric(5,4)` | No | DEFAULT 0.0000; 0..1 | Phí hủy nếu >= ngưỡng |
| `cancellation_fee_rate_below` | `numeric(5,4)` | No | DEFAULT 0.5000; 0..1 | Phí hủy nếu < ngưỡng |
| `effective_from` | `timestamptz` | No |  | Bắt đầu hiệu lực |
| `effective_to` | `timestamptz` | Yes |  | Kết thúc hiệu lực |
| `is_active` | `boolean` | No | DEFAULT false | Policy hiện tại |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `appointment_cancellations` — Snapshot hủy lịch và kết quả tính tài chính

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `appointment_id` | `uuid` | No | UNIQUE FK -> appointments.id | Mỗi lịch tối đa một cancellation |
| `otp_grant_id` | `uuid` | Yes | UNIQUE; Composite FK cùng appointment_id -> otp_verification_grants(id, appointment_id) | Grant CANCEL nếu Guest; backend xác minh purpose |
| `policy_version_id` | `uuid` | No | FK -> cancellation_policy_versions.id | Policy áp dụng |
| `requested_by_actor_type` | `varchar(30)` | No |  | GUEST/PATIENT/RECEPTIONIST/ADMIN/SYSTEM |
| `requested_by_user_id` | `uuid` | Yes | FK -> users.id | User nếu có |
| `requested_by_patient_id` | `uuid` | Yes | Composite FK cùng appointment_id -> appointments(id, patient_id) | Patient hủy nếu có; phải là chủ booking |
| `reason` | `text` | Yes |  | Lý do hủy |
| `cancelled_at` | `timestamptz` | No |  | Server timestamp |
| `seconds_before_start` | `bigint` | No |  | Số giây trước giờ hẹn |
| `refund_rate_snapshot` | `numeric(5,4)` | No | 0..1 | Tỷ lệ hoàn snapshot |
| `cancellation_fee_rate_snapshot` | `numeric(5,4)` | No | 0..1 | Tỷ lệ phí hủy snapshot |
| `service_fee_snapshot` | `numeric(12,2)` | No |  | Giá dịch vụ lúc đặt |
| `paid_service_amount_snapshot` | `numeric(12,2)` | No |  | Tổng phí dịch vụ đã thanh toán thành công |
| `expected_refund_amount` | `numeric(12,2)` | No |  | Số tiền dự kiến hoàn |
| `cancellation_fee_amount` | `numeric(12,2)` | No |  | Số phí hủy được giữ lại trên số đã thanh toán |
| `refund_status` | `varchar(20)` | No |  | NotApplicable/Pending/Processing/Succeeded/Failed |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `payments` — Thanh toán gắn với Appointment

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh giao dịch |
| `appointment_id` | `uuid` | No | FK -> appointments.id | Lịch hẹn |
| `payment_purpose` | `varchar(30)` | No | DEFAULT SERVICE_FEE | Mục đích thu |
| `amount` | `numeric(12,2)` | No | CHECK > 0 | Số tiền |
| `currency` | `char(3)` | No |  | Đơn vị tiền tệ |
| `method` | `varchar(30)` | No | CASH/BANK_TRANSFER/POS/ONLINE_GATEWAY | Phương thức |
| `status` | `varchar(20)` | No | Pending/Succeeded/Failed/Voided | Trạng thái giao dịch |
| `provider_reference` | `varchar(150)` | Yes |  | Mã từ provider/biên lai |
| `paid_at` | `timestamptz` | Yes |  | Thời điểm thanh toán thành công |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `refunds` — Giao dịch hoàn tiền

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh hoàn |
| `appointment_cancellation_id` | `uuid` | No | FK -> appointment_cancellations.id | Cancellation liên quan |
| `payment_id` | `uuid` | No | FK -> payments.id | Payment nguồn cụ thể được hoàn |
| `amount` | `numeric(12,2)` | No | CHECK > 0 | Số tiền hoàn dương; không tạo record nếu không có khoản hoàn |
| `status` | `varchar(20)` | No | Pending/Processing/Succeeded/Failed | Trạng thái hoàn |
| `provider_reference` | `varchar(150)` | Yes |  | Mã provider |
| `processed_by_user_id` | `uuid` | Yes | FK -> users.id | Nhân viên xử lý nếu thủ công |
| `processed_at` | `timestamptz` | Yes |  | Thời điểm xác nhận hoàn |
| `failure_reason` | `text` | Yes |  | Lý do thất bại |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `otp_challenges` — OTP challenge cho một booking và mục đích

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK; UNIQUE cùng appointment_id và purpose | Định danh challenge |
| `appointment_id` | `uuid` | No | Composite FK cùng patient_id -> appointments(id, patient_id) | Booking được bảo vệ |
| `patient_id` | `uuid` | No | Composite FK -> appointments(id, patient_id) và patient_contacts(id, patient_id) | Patient sở hữu booking/contact |
| `patient_contact_id` | `uuid` | No | Composite FK cùng patient_id -> patient_contacts(id, patient_id) | Số điện thoại đích thuộc đúng Patient |
| `purpose` | `varchar(20)` | No | LOOKUP/RESCHEDULE/CANCEL | Mục đích OTP |
| `otp_verifier_hmac` | `bytea` | No |  | HMAC/pepper của OTP; không lưu mã gốc hoặc SHA trần |
| `phone_hmac` | `bytea` | No |  | HMAC của số đã chuẩn hóa |
| `expires_at` | `timestamptz` | No |  | Thời hạn |
| `attempt_count` | `smallint` | No | DEFAULT 0 | Số lần thử |
| `max_attempts` | `smallint` | No |  | Giới hạn lần thử |
| `resend_after` | `timestamptz` | No |  | Thời điểm cho phép gửi lại |
| `consumed_at` | `timestamptz` | Yes |  | Thời điểm dùng thành công |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `otp_verification_grants` — Grant ngắn hạn sau khi OTP hợp lệ

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK; UNIQUE cùng appointment_id | Định danh grant |
| `challenge_id` | `uuid` | No | UNIQUE; Composite FK cùng appointment_id + purpose | Challenge tạo grant |
| `appointment_id` | `uuid` | No | Composite FK -> otp_challenges(id, appointment_id, purpose); UNIQUE cùng id | Booking scope |
| `purpose` | `varchar(20)` | No | LOOKUP/RESCHEDULE/CANCEL | Mục đích được grant |
| `grant_token_hash` | `bytea` | No | UNIQUE | Chỉ lưu hash token grant |
| `expires_at` | `timestamptz` | No |  | Thời hạn |
| `consumed_at` | `timestamptz` | Yes |  | Thời điểm đã dùng |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `notifications` — Thông báo SMS/email/in-app

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `patient_id` | `uuid` | Yes | FK -> patients.id | Người nhận nếu đã có hồ sơ |
| `appointment_id` | `uuid` | Yes | FK -> appointments.id | Lịch liên quan |
| `channel` | `varchar(20)` | No | SMS/EMAIL/IN_APP | Kênh |
| `template_code` | `varchar(60)` | No |  | Mã mẫu |
| `recipient_ciphertext` | `bytea` | Yes |  | Địa chỉ nhận đã mã hóa |
| `recipient_masked` | `varchar(100)` | Yes |  | Địa chỉ nhận dạng che |
| `status` | `varchar(20)` | No | Pending/Sent/Failed | Trạng thái gửi |
| `provider_message_id` | `varchar(150)` | Yes |  | Mã provider |
| `sent_at` | `timestamptz` | Yes |  | Thời điểm gửi |
| `failure_reason` | `text` | Yes |  | Lỗi gửi |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## `consultations` — Nội dung khám bệnh nhạy cảm

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `appointment_id` | `uuid` | No | UNIQUE FK -> appointments.id | Lịch khám; tối đa một record v1 |
| `doctor_id` | `uuid` | No | Composite FK cùng appointment_id -> appointments(id, doctor_id) | Bác sĩ được phân công |
| `symptoms` | `text` | Yes | Sensitive | Triệu chứng |
| `examination_notes` | `text` | Yes | Sensitive | Nội dung thăm khám |
| `diagnosis` | `text` | Yes | Sensitive | Chẩn đoán |
| `conclusion` | `text` | Yes | Sensitive | Kết luận |
| `follow_up_instructions` | `text` | Yes | Sensitive | Hướng dẫn theo dõi |
| `consulted_at` | `timestamptz` | Yes |  | Thời điểm khám |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |
| `updated_at` | `timestamptz` | No |  | Thời điểm cập nhật |

## `audit_logs` — Nhật ký bảo mật/nghiệp vụ

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `actor_type` | `varchar(30)` | No |  | GUEST/PATIENT/DOCTOR/RECEPTIONIST/ADMIN/SYSTEM |
| `actor_user_id` | `uuid` | Yes | FK -> users.id | User thực hiện nếu có |
| `action` | `varchar(100)` | No |  | Mã thao tác |
| `entity_type` | `varchar(80)` | No |  | Loại đối tượng |
| `entity_id` | `uuid` | Yes |  | ID đối tượng |
| `correlation_id` | `varchar(80)` | Yes |  | Trace/request correlation |
| `metadata_redacted` | `jsonb` | Yes | Redacted only | Metadata đã loại dữ liệu nhạy cảm |
| `occurred_at` | `timestamptz` | No |  | Thời điểm sự kiện |

## `idempotency_records` — Chống request trùng khi client retry

| Cột | Kiểu PostgreSQL | Null? | Key / Rule | Ý nghĩa |
|---|---|---|---|---|
| `id` | `uuid` | No | PK | Định danh |
| `scope` | `varchar(100)` | No |  | Endpoint/operation scope |
| `key_hash` | `bytea` | No | UNIQUE cùng scope | Hash idempotency key |
| `request_hash` | `bytea` | No |  | Hash payload để phát hiện reuse sai |
| `resource_type` | `varchar(50)` | Yes |  | Loại kết quả tạo ra |
| `resource_id` | `uuid` | Yes |  | ID resource kết quả |
| `response_status_code` | `smallint` | Yes |  | HTTP status đã ghi nhận |
| `expires_at` | `timestamptz` | No |  | Thời điểm hết hiệu lực |
| `created_at` | `timestamptz` | No |  | Thời điểm tạo |

## Cross-table invariants

1. `patients.user_id` has a partial unique index where not null; Patient may exist without User.
2. `patient_contacts` exposes a unique `(id, patient_id)` pair so `appointments` and `otp_challenges` can reference a contact belonging to the same Patient. A personal-owner verified phone must also be a verified PHONE contact.
3. `appointments` exposes unique `(id, patient_id)` and `(id, doctor_id)` candidate keys. Child composite FKs bind Patient actors, OTP challenges and Consultation Doctor to the correct appointment.
4. At least one of `patients.date_of_birth` or `patients.birth_year` is required; if both exist, years must match.
5. `patient_identifiers` unique index applies only to verified active identifiers; unverified data is not globally unique.
6. Name + DOB/year is a duplicate-candidate signal, not a unique key and never an auto-merge trigger.
7. Doctor slot overlap is guarded by an exclusion constraint on `appointments`; Cancelled appointments no longer hold a slot.
8. `reschedule_requests` has a partial unique index for one PendingReview request per Appointment.
9. Cancellation rates and amounts are server-computed and saved as snapshots; Client cannot submit these values.
10. Each `refunds` record links to one source Payment; per-Payment total refund cannot exceed successful payment amount.
11. OTP challenge must reference the same Patient as both Appointment and PatientContact; grant must reference the exact challenge and booking. Application verifies LOOKUP/RESCHEDULE/CANCEL purpose and consumes grants atomically.
12. Appointment Doctor/Service pair must exist in `doctor_services`; Consultation Doctor must match the Appointment Doctor.
13. OTP verifier uses server-side HMAC/pepper; grant token is high entropy and stored hashed. Never log plaintext OTP, tokens, identifiers or clinical notes.
