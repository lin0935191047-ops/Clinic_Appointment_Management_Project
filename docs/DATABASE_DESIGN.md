# DATABASE DESIGN — Clinic Appointment Management System

- **Phiên bản:** 1.2 — Logical design with scoped cross-table constraints
- **Cơ sở:** `docs/SPEC.md` v1.2, `docs/BUSINESS_RULES.md` v1.2 và quyết định nghiệp vụ đã xác nhận
- **Trạng thái:** Draft — cần chủ dự án và Harness Audit duyệt trước khi viết EF Core Migration
- **DBMS:** PostgreSQL
- **ORM mục tiêu:** Entity Framework Core + Npgsql
- **Múi giờ nghiệp vụ:** `Asia/Ho_Chi_Minh`; lưu timestamp dưới dạng `timestamptz` theo UTC

> Tài liệu này là thiết kế logic và hướng dẫn ràng buộc. Chưa phải migration, chưa tạo Database và không được chạy các đoạn SQL tham khảo như một script khởi tạo hoàn chỉnh.

## 1. Mục tiêu thiết kế

1. Quản lý Patient độc lập với User; Guest đặt lịch không cần tài khoản.
2. Lưu và bảo vệ thông tin định danh nhạy cảm; tìm ứng viên trùng nhưng không tự động gộp sai hồ sơ.
3. Quản lý lịch bác sĩ và ngăn hai lịch cùng bác sĩ chồng lấn ngay tại PostgreSQL.
4. Lưu lịch sử đổi/hủy, phê duyệt đổi lịch và kết quả tính hoàn phí có thể kiểm toán.
5. OTP có scope theo booking và mục đích; không dùng OTP thay cho tài khoản đăng nhập.
6. Hỗ trợ thanh toán/hoàn tiền ở mức ghi nhận giao dịch, nhưng không giả định đã chọn cổng thanh toán.
7. Dùng FK, CHECK, UNIQUE, partial index, transaction và audit để bảo vệ tính toàn vẹn.

## 2. Quyết định thiết kế chính

| ID | Quyết định | Lý do / hệ quả |
|---|---|---|
| DB-DEC-01 | PK dùng UUID | Dễ tích hợp API và giảm khả năng đoán ID nội bộ |
| DB-DEC-02 | `patients.user_id` nullable, unique | Patient có thể không có User; một User Patient chỉ liên kết một hồ sơ |
| DB-DEC-03 | Patient identity tách `patient_contacts` và `patient_identifiers` | Phân biệt kênh liên hệ/OTP với CCCD, hộ chiếu, BHYT, PID |
| DB-DEC-04 | Họ tên + DOB/BirthYear chỉ là tín hiệu tìm candidate | Không unique và không auto-merge |
| DB-DEC-05 | Định danh nhạy cảm lưu ciphertext + keyed HMAC | Có thể giải mã khi được phép; exact-match mà không lưu hash thường |
| DB-DEC-06 | Appointment lưu snapshot dịch vụ/giá/thời lượng | Lịch cũ không đổi khi danh mục được cập nhật |
| DB-DEC-07 | `appointments` là nguồn giữ slot chính | Exclusion constraint trên bảng này bảo vệ booking và reschedule approval đồng thời |
| DB-DEC-08 | Reschedule request không giữ slot mới trước khi duyệt | Đúng quy trình: lịch cũ được giữ nguyên; khi duyệt phải cập nhật trong transaction và DB constraint quyết định thắng/thua |
| DB-DEC-09 | Cancellation lưu snapshot policy và kết quả tài chính | Có thể giải thích mức hoàn tại thời điểm hủy kể cả khi policy thay đổi sau này |
| DB-DEC-10 | OTP challenge và OTP grant là hai bản ghi riêng | Challenge theo dõi xác minh; grant giới hạn booking + purpose + thời hạn |
| DB-DEC-11 | Idempotency cho các POST có tác động tài chính/đặt lịch | Hạn chế tạo booking hoặc cancel trùng do retry mạng |
| DB-DEC-12 | Không hard-delete hồ sơ có lịch sử y tế/giao dịch | Bảo toàn audit và quan hệ nghiệp vụ; retention là TBD |

## 3. Phạm vi và giả định

- V1 thiết kế cho **một phòng khám**, chưa có bảng Branch/Room. Mở rộng nhiều chi nhánh là TBD.
- Một Doctor cần có User để đăng nhập. Patient có User là tùy chọn.
- Một Appointment gắn một Patient, một Doctor và một Service.
- Reschedule hiện chỉ đổi thời gian của cùng bác sĩ; đổi bác sĩ là TBD.
- `PatientContacts` lưu nhiều số điện thoại/email. Số điện thoại dùng chung không mặc nhiên là định danh duy nhất.
- Thanh toán có thể được ghi nhận thủ công; payment gateway cụ thể chưa được chọn.
- Mức hoàn áp dụng trên **phí dịch vụ đã thanh toán**. Xử lý partial payment và thu khoản phí còn thiếu khi chưa thanh toán là TBD-04 trong SPEC.

## 4. Logical entities / bảng

### 4.1 Identity & access

#### `roles`
- `id uuid PK`
- `code varchar(40) NOT NULL UNIQUE` — ADMIN, DOCTOR, RECEPTIONIST, PATIENT
- `name varchar(100) NOT NULL`
- `created_at timestamptz NOT NULL`

#### `users`
- `id uuid PK`
- `email varchar(254) NOT NULL`
- `email_normalized varchar(254) NOT NULL UNIQUE`
- `password_hash text NOT NULL`
- `is_active boolean NOT NULL DEFAULT true`
- `email_verified_at timestamptz NULL`
- `last_login_at timestamptz NULL`
- `created_at`, `updated_at timestamptz NOT NULL`

Không lưu password plaintext. Nên dùng ASP.NET Core Identity password hasher hoặc PasswordHasher chuyên dụng; không tự viết thuật toán hash.

#### `user_roles`
- `user_id uuid FK -> users.id`
- `role_id uuid FK -> roles.id`
- `assigned_at timestamptz NOT NULL`
- PK (`user_id`, `role_id`)

Dùng bảng nối để không khóa cứng giả định một User chỉ có một Role. Backend vẫn phải áp dụng role/resource authorization.

#### `refresh_tokens`
- `id uuid PK`, `user_id uuid FK`
- `token_hash bytea NOT NULL UNIQUE`
- `expires_at timestamptz NOT NULL`, `revoked_at timestamptz NULL`
- `replaced_by_token_id uuid NULL FK -> refresh_tokens.id`
- `created_at timestamptz NOT NULL`

Lưu hash token, không lưu refresh token nguyên bản.

### 4.2 Patient profile & identity

#### `patients`
- `id uuid PK`
- `patient_code varchar(32) NOT NULL UNIQUE` — mã nội bộ, không thay thế định danh pháp lý
- `user_id uuid NULL FK -> users.id` — partial unique index khi khác NULL
- `full_name varchar(200) NOT NULL`
- `normalized_full_name varchar(200) NOT NULL`
- `address text NULL`
- `date_of_birth date NULL`
- `birth_year smallint NULL`
- `gender varchar(30) NULL`
- `record_status varchar(30) NOT NULL DEFAULT 'ACTIVE'` — ACTIVE, REVIEW_REQUIRED, INACTIVE, MERGED
- `merged_into_patient_id uuid NULL FK -> patients.id`
- `created_at`, `updated_at timestamptz NOT NULL`

CHECK: phải có `date_of_birth` hoặc `birth_year`; nếu cả hai có giá trị thì năm của `date_of_birth` phải bằng `birth_year`. `full_name + DOB/BirthYear` **không unique**.

#### `patient_contacts`
- `id uuid PK`, `patient_id uuid FK -> patients.id`
- UNIQUE (`id`, `patient_id`) để có thể tạo composite FK từ Appointment và bảo đảm booking contact thuộc đúng Patient.
- `contact_type varchar(20) NOT NULL` — PHONE, EMAIL
- `value_ciphertext bytea NOT NULL` — giá trị đã mã hóa
- `value_hmac bytea NOT NULL` — keyed HMAC trên giá trị chuẩn hóa
- `masked_value varchar(100) NOT NULL`
- `is_primary boolean NOT NULL DEFAULT false`
- `is_verified boolean NOT NULL DEFAULT false`
- `verified_at timestamptz NULL`
- `is_personal_owner_verified boolean NOT NULL DEFAULT false` — CHECK chỉ được true khi contact_type=PHONE và is_verified=true
- `is_active boolean NOT NULL DEFAULT true`
- `created_at`, `updated_at timestamptz NOT NULL`

Số dùng chung (ví dụ số của người giám hộ) được lưu như contact nhưng không đánh dấu `is_personal_owner_verified`. Chỉ số điện thoại cá nhân chính chủ đã xác minh mới được xem là định danh mạnh. Có thể dùng contact gắn với Appointment để gửi OTP; OTP không chứng minh quyền xem bệnh án khác. Khi contact đã được Appointment tham chiếu, không sửa đè giá trị mã hóa; thay đổi số/email nên tạo contact record mới để bảo toàn bằng chứng liên hệ tại thời điểm đặt.

#### `patient_identifiers`
- `id uuid PK`, `patient_id uuid FK -> patients.id`
- `identifier_type varchar(30) NOT NULL` — NATIONAL_ID, PASSPORT, HEALTH_INSURANCE, LEGACY_PID
- `issuer_code varchar(40) NOT NULL` — quốc gia/cơ quan/hệ thống cấp
- `value_ciphertext bytea NOT NULL`
- `normalized_value_hmac bytea NOT NULL`
- `masked_value varchar(100) NOT NULL`
- `verification_status varchar(20) NOT NULL` — UNVERIFIED, PENDING, VERIFIED, REJECTED
- `verified_at timestamptz NULL`
- `is_active boolean NOT NULL DEFAULT true`
- `created_at`, `updated_at timestamptz NOT NULL`

Unique partial index theo `(identifier_type, issuer_code, normalized_value_hmac)` chỉ với định danh `VERIFIED` và `is_active`. Không bật unique cho dữ liệu chưa xác minh.

#### `patient_identity_review_cases`
- `id uuid PK`
- `patient_id uuid FK -> patients.id` — hồ sơ đang được xem xét
- `candidate_patient_id uuid FK -> patients.id` — hồ sơ ứng viên
- `match_reason varchar(40) NOT NULL` — VERIFIED_IDENTIFIER, VERIFIED_PERSONAL_PHONE, NAME_DOB, NAME_BIRTH_YEAR, CONFLICT
- `status varchar(30) NOT NULL` — OPEN, CONFIRMED_SAME_PERSON, DIFFERENT_PERSON, NEEDS_MORE_INFO
- `reviewed_by_user_id uuid NULL FK -> users.id`
- `reviewed_at timestamptz NULL`, `review_note text NULL`
- `created_at timestamptz NOT NULL`

UNIQUE (`patient_id`, `candidate_patient_id`, `match_reason`). Không tự gộp khi chỉ trùng tên/ngày sinh.

#### `patient_merge_events`
- `id uuid PK`
- `source_patient_id uuid FK -> patients.id`
- `target_patient_id uuid FK -> patients.id`
- `merged_by_user_id uuid FK -> users.id`
- `reason text NOT NULL`
- `merged_at timestamptz NOT NULL`
- `before_snapshot jsonb NOT NULL` — metadata tối thiểu, đã redaction

Chỉ nhân sự được cấp quyền mới được merge. Trong transaction phải chuyển các FK nghiệp vụ cần thiết, đánh dấu source `MERGED`, đặt `merged_into_patient_id`, giữ audit và không xóa cứng.

### 4.3 Catalog & clinical staff

#### `specialties`
- `id uuid PK`, `code varchar(40) UNIQUE`, `name varchar(150)`, `slug varchar(180) UNIQUE`
- `description text NULL`, `is_active boolean`, timestamps

#### `doctors`
- `id uuid PK`, `user_id uuid NOT NULL UNIQUE FK -> users.id`
- `doctor_code varchar(32) UNIQUE`, `full_name varchar(200) NOT NULL`
- `license_number_ciphertext bytea NULL`, `license_number_hmac bytea NULL`
- `bio text NULL`, `years_experience smallint NULL`, `is_active boolean`
- timestamps

#### `doctor_specialties`
- `doctor_id uuid FK -> doctors.id`, `specialty_id uuid FK -> specialties.id`
- `is_primary boolean NOT NULL DEFAULT false`, `created_at timestamptz`
- PK (`doctor_id`, `specialty_id`); partial unique index để một Doctor tối đa một primary specialty.

#### `services`
- `id uuid PK`, `specialty_id uuid FK -> specialties.id`
- `code varchar(40) UNIQUE`, `name varchar(180)`, `description text NULL`
- `duration_minutes integer NOT NULL`, `price numeric(12,2) NOT NULL`, `currency char(3) NOT NULL DEFAULT 'VND'`
- `is_active boolean NOT NULL`, timestamps
- CHECK duration > 0, price >= 0.

#### `doctor_services`
- `doctor_id uuid FK -> doctors.id`, `service_id uuid FK -> services.id`
- PK (`doctor_id`, `service_id`) là khóa ghép để kiểm tra bác sĩ được phép thực hiện dịch vụ.
- `duration_minutes_override integer NULL`, `price_override numeric(12,2) NULL`
- `is_active boolean NOT NULL DEFAULT true`
- CHECK override duration > 0 nếu có; price override >= 0 nếu có.

### 4.4 Scheduling

#### `doctor_working_rules`
- `id uuid PK`, `doctor_id uuid FK -> doctors.id`
- `day_of_week smallint NOT NULL` — ISO: Monday=1 ... Sunday=7
- `start_local_time time NOT NULL`, `end_local_time time NOT NULL`
- `effective_from date NOT NULL`, `effective_to date NULL`
- `is_active boolean NOT NULL DEFAULT true`, timestamps
- CHECK day 1..7; start < end; effective_to >= effective_from nếu có.

Cho phép nhiều dòng trong một ngày để mô tả ca sáng/chiều. Overlap giữa các rule cùng doctor phải được validate khi tạo/cập nhật.

#### `doctor_time_offs`
- `id uuid PK`, `doctor_id uuid FK -> doctors.id`
- `start_at timestamptz NOT NULL`, `end_at timestamptz NOT NULL`
- `reason text NULL`, `status varchar(20) NOT NULL` — ACTIVE/CANCELLED
- `created_by_user_id uuid FK -> users.id`, timestamps
- CHECK start < end.

#### `doctor_schedule_blocks`
- `id uuid PK`, `doctor_id uuid FK -> doctors.id`
- `start_at timestamptz NOT NULL`, `end_at timestamptz NOT NULL`
- `block_type varchar(30) NOT NULL` — BREAK, MEETING, MAINTENANCE, OTHER
- `reason text NULL`, `created_by_user_id uuid FK -> users.id`, `is_active boolean`
- CHECK start < end.

`doctor_time_offs` và `doctor_schedule_blocks` là các khoảng không nhận lịch; availability phải trừ chúng khỏi khung làm việc.

### 4.5 Appointment & workflow

#### `appointments`
- `id uuid PK`, `booking_code varchar(32) NOT NULL UNIQUE`
- `patient_id uuid NOT NULL FK -> patients.id`, `booking_contact_id uuid NOT NULL` — composite FK (`booking_contact_id`, `patient_id`) -> `patient_contacts(id, patient_id)`.
- Tạo unique key phụ trên `appointments(id, patient_id)` và `appointments(id, doctor_id)` để các bảng con có thể tham chiếu cùng cặp định danh, tránh gắn nhầm Patient/Doctor.
- `doctor_id uuid FK -> doctors.id`, `service_id uuid FK -> services.id`
- `patient_name_snapshot varchar(200) NOT NULL`
- `service_name_snapshot varchar(180) NOT NULL`
- `duration_minutes_snapshot integer NOT NULL`
- `service_fee_snapshot numeric(12,2) NOT NULL`, `currency_snapshot char(3) NOT NULL`
- `start_at timestamptz NOT NULL`, `end_at timestamptz NOT NULL`
- `status varchar(30) NOT NULL` — PendingConfirmation, Confirmed, CheckedIn, InProgress, Completed, Cancelled, NoShow
- `booking_channel varchar(30) NOT NULL` — PUBLIC_WEB, PATIENT_PORTAL, RECEPTION_DESK
- `reason text NULL`, `notes text NULL`
- `created_by_user_id uuid NULL FK -> users.id`
- `confirmed_by_user_id uuid NULL FK -> users.id`, `confirmed_at timestamptz NULL`
- `created_at`, `updated_at timestamptz NOT NULL`
- CHECK start < end, duration > 0, fee >= 0.

Composite FK bảo đảm contact đặt lịch thuộc đúng Patient. Thêm composite FK (`doctor_id`, `service_id`) -> `doctor_services(doctor_id, service_id)` để không tạo appointment với bác sĩ chưa được gán dịch vụ. Contact tại thời điểm đặt được giữ làm đích OTP; không hard-delete contact đã được booking tham chiếu. Nếu thông tin liên hệ thay đổi, cần quy trình xác minh/cập nhật có audit.

#### `appointment_histories`
- `id uuid PK`, `appointment_id uuid FK -> appointments.id`
- `from_status varchar(30) NULL`, `to_status varchar(30) NOT NULL`
- `actor_type varchar(30) NOT NULL`, `actor_user_id uuid NULL FK -> users.id`, `actor_patient_id uuid NULL FK -> patients.id`
- Nếu `actor_patient_id` khác NULL, composite FK (`appointment_id`, `actor_patient_id`) -> `appointments(id, patient_id)` bảo đảm Patient thực hiện là chủ booking.
- `reason text NULL`, `metadata_redacted jsonb NULL`, `created_at timestamptz NOT NULL`

#### `reschedule_requests`
- `id uuid PK`, `appointment_id uuid NOT NULL`
- `requested_by_patient_id uuid NOT NULL`; composite FK (`appointment_id`, `requested_by_patient_id`) -> `appointments(id, patient_id)` bảo đảm request thuộc đúng Patient.
- `otp_grant_id uuid NULL`; composite FK (`otp_grant_id`, `appointment_id`) -> `otp_verification_grants(id, appointment_id)` để grant không thể dùng cho booking khác. Backend còn phải xác minh purpose = RESCHEDULE.
- `old_start_at timestamptz NOT NULL`, `old_end_at timestamptz NOT NULL`
- `proposed_start_at timestamptz NOT NULL`, `proposed_end_at timestamptz NOT NULL`
- `status varchar(30) NOT NULL` — PendingReview, Approved, Rejected, Cancelled, Expired
- `patient_note text NULL`, `reviewed_by_user_id uuid NULL FK -> users.id`
- `reviewed_at timestamptz NULL`, `review_note text NULL`, timestamps
- CHECK proposed start < proposed end; proposed interval khác old interval.
- Partial unique index cho một request PendingReview/Appointment.

Slot đề xuất **không được giữ chỗ trước khi lễ tân duyệt**. Khi duyệt, cập nhật `appointments.start_at/end_at` trong transaction; PostgreSQL exclusion constraint là lớp chống xung đột cuối cùng. Nếu xung đột, transaction rollback, lịch cũ không đổi.

#### `cancellation_policy_versions`
- `id uuid PK`, `version varchar(30) NOT NULL UNIQUE`
- `threshold_minutes integer NOT NULL DEFAULT 60`
- `refund_rate_at_or_above numeric(5,4) NOT NULL DEFAULT 1.0000`
- `refund_rate_below numeric(5,4) NOT NULL DEFAULT 0.5000`
- `cancellation_fee_rate_at_or_above numeric(5,4) NOT NULL DEFAULT 0.0000`
- `cancellation_fee_rate_below numeric(5,4) NOT NULL DEFAULT 0.5000`
- `effective_from timestamptz NOT NULL`, `effective_to timestamptz NULL`
- `is_active boolean NOT NULL DEFAULT false`, `created_at timestamptz NOT NULL`

Policy v1: >=60 phút hoàn 100%, phí hủy 0%; 0 đến <60 phút và trước giờ hẹn hoàn 50%, phí hủy 50%. Đúng 60 phút thuộc nhóm hoàn 100%. Hai tỷ lệ hoàn + phí hủy phải cộng thành 100%. Không tự sửa policy đã được dùng; tạo version mới.

#### `appointment_cancellations`
- `id uuid PK`, `appointment_id uuid NOT NULL UNIQUE FK -> appointments.id`
- `otp_grant_id uuid NULL UNIQUE`; composite FK (`otp_grant_id`, `appointment_id`) -> `otp_verification_grants(id, appointment_id)`; lưu grant CANCEL đã dùng cho Guest. Backend phải xác minh purpose = CANCEL; Patient đã xác thực có thể dùng session hợp lệ nếu policy cho phép.
- `policy_version_id uuid FK -> cancellation_policy_versions.id`
- `requested_by_actor_type varchar(30) NOT NULL`, `requested_by_user_id uuid NULL FK -> users.id`, `requested_by_patient_id uuid NULL FK -> patients.id`
- `reason text NULL`, `cancelled_at timestamptz NOT NULL`
- Nếu `requested_by_patient_id` khác NULL, composite FK (`appointment_id`, `requested_by_patient_id`) -> `appointments(id, patient_id)` bảo đảm người hủy là Patient của booking.
- `seconds_before_start bigint NOT NULL`
- `refund_rate_snapshot numeric(5,4) NOT NULL`, `cancellation_fee_rate_snapshot numeric(5,4) NOT NULL`
- `service_fee_snapshot numeric(12,2) NOT NULL`, `paid_service_amount_snapshot numeric(12,2) NOT NULL`
- `expected_refund_amount numeric(12,2) NOT NULL`, `cancellation_fee_amount numeric(12,2) NOT NULL`
- `refund_status varchar(20) NOT NULL` — NotApplicable, Pending, Processing, Succeeded, Failed
- `created_at timestamptz NOT NULL`

Tính bằng server time và số tiền payment thực tế đã thành công. Chưa thanh toán thì expected refund = 0; không tự tạo khoản phải thu phí hủy cho tiền chưa thanh toán cho đến khi TBD-04 được chốt.

### 4.6 Payment & refund

#### `payments`
- `id uuid PK`, `appointment_id uuid FK -> appointments.id`
- `payment_purpose varchar(30) NOT NULL DEFAULT 'SERVICE_FEE'`
- `amount numeric(12,2) NOT NULL`, `currency char(3) NOT NULL`
- `method varchar(30) NOT NULL` — CASH, BANK_TRANSFER, POS, ONLINE_GATEWAY
- `status varchar(20) NOT NULL` — Pending, Succeeded, Failed, Voided
- `provider_reference varchar(150) NULL`, `paid_at timestamptz NULL`
- `created_at`, `updated_at timestamptz NOT NULL`
- CHECK amount > 0.

#### `refunds`
- `id uuid PK`, `appointment_cancellation_id uuid FK -> appointment_cancellations.id`
- `payment_id uuid NOT NULL FK -> payments.id` — mỗi refund record hoàn về một payment nguồn cụ thể; nếu có nhiều payment, tạo nhiều refund records.
- `amount numeric(12,2) NOT NULL CHECK > 0`, `status varchar(20) NOT NULL` — chỉ tạo Refund record khi số tiền hoàn dương.
- `provider_reference varchar(150) NULL`, `processed_by_user_id uuid NULL FK -> users.id`
- `processed_at timestamptz NULL`, `failure_reason text NULL`, timestamps
- CHECK amount >= 0.

Không báo tiền đã hoàn khi refund chỉ ở Pending/Processing. Mỗi refund record phải liên kết payment nguồn cụ thể. Tổng refund của một Payment không được vượt số tiền Payment đã Succeeded; kiểm tra trong Application transaction và PostgreSQL integration test.

### 4.7 OTP, notification & idempotency

#### `otp_challenges`
- `id uuid PK`, `appointment_id uuid NOT NULL`, `patient_id uuid NOT NULL`, `patient_contact_id uuid NOT NULL`.
- Composite FK (`appointment_id`, `patient_id`) -> `appointments(id, patient_id)` và (`patient_contact_id`, `patient_id`) -> `patient_contacts(id, patient_id)` bảo đảm OTP được gửi tới contact của đúng Patient trong booking.
- `purpose varchar(20) NOT NULL` — LOOKUP, RESCHEDULE, CANCEL; tạo unique key (`id`, `appointment_id`, `purpose`) để grant tham chiếu đúng challenge scope.
- `otp_verifier_hmac bytea NOT NULL`, `phone_hmac bytea NOT NULL`
- `expires_at timestamptz NOT NULL`, `attempt_count smallint NOT NULL DEFAULT 0`
- `max_attempts smallint NOT NULL`, `resend_after timestamptz NOT NULL`
- `consumed_at timestamptz NULL`, `created_at timestamptz NOT NULL`

#### `otp_verification_grants`
- `id uuid PK`, `challenge_id uuid NOT NULL UNIQUE`, `appointment_id uuid NOT NULL`, `purpose varchar(20) NOT NULL`.
- Composite FK (`challenge_id`, `appointment_id`, `purpose`) -> `otp_challenges(id, appointment_id, purpose)` để không thể ghép challenge của booking/purpose khác.
- Unique (`id`, `appointment_id`) để RescheduleRequest/Cancellation có thể FK theo đúng booking; Application phải kiểm tra purpose tương ứng trước khi ghi nhận hành động.
- `grant_token_hash bytea NOT NULL UNIQUE`, `expires_at timestamptz NOT NULL`
- `consumed_at timestamptz NULL`, `created_at timestamptz NOT NULL`

OTP là mã entropy thấp: không dùng plain SHA hash; lưu verifier HMAC với secret pepper ngoài Database, giới hạn attempt/resend và thời hạn ngắn. Grant không phải JWT đăng nhập; chỉ cho một booking và một purpose. Grant dùng một lần cho action đổi/hủy; lookup có thể dùng một lần hoặc giới hạn ngắn theo API contract. Khi tạo RescheduleRequest hoặc AppointmentCancellation, backend phải kiểm tra đúng purpose và booking, ghi nhận grant đã tiêu thụ nguyên tử; không chấp nhận grant đã hết hạn/đã dùng.

#### `notifications`
- `id uuid PK`, `patient_id uuid NULL FK -> patients.id`, `appointment_id uuid NULL FK -> appointments.id`
- `channel varchar(20) NOT NULL` — SMS, EMAIL, IN_APP
- `template_code varchar(60) NOT NULL`, `recipient_ciphertext bytea NULL`, `recipient_masked varchar(100) NULL`
- `status varchar(20) NOT NULL` — Pending, Sent, Failed
- `provider_message_id varchar(150) NULL`, `sent_at timestamptz NULL`, `failure_reason text NULL`, `created_at timestamptz NOT NULL`

#### `idempotency_records`
- `id uuid PK`, `scope varchar(100) NOT NULL`
- `key_hash bytea NOT NULL`, `request_hash bytea NOT NULL`
- `resource_type varchar(50) NULL`, `resource_id uuid NULL`
- `response_status_code smallint NULL`, `expires_at timestamptz NOT NULL`, `created_at timestamptz NOT NULL`
- UNIQUE (`scope`, `key_hash`). Reuse cùng key nhưng request hash khác phải trả conflict.

### 4.8 Consultation & audit

#### `consultations`
- `id uuid PK`, `appointment_id uuid NOT NULL UNIQUE`
- `doctor_id uuid NOT NULL`; composite FK (`appointment_id`, `doctor_id`) -> `appointments(id, doctor_id)` bảo đảm bác sĩ ghi nhận khám đúng lịch được phân công.
- `symptoms text NULL`, `examination_notes text NULL`, `diagnosis text NULL`
- `conclusion text NULL`, `follow_up_instructions text NULL`
- `consulted_at timestamptz NULL`, `created_at`, `updated_at timestamptz NOT NULL`

Dữ liệu này là dữ liệu sức khỏe nhạy cảm; quyền đọc/ghi phải kiểm tra theo Doctor được phân công và vai trò được phê duyệt. Không trả nội dung Consultation trong Guest lookup.

#### `audit_logs`
- `id uuid PK`, `actor_type varchar(30) NOT NULL`, `actor_user_id uuid NULL FK -> users.id`
- `action varchar(100) NOT NULL`, `entity_type varchar(80) NOT NULL`, `entity_id uuid NULL`
- `correlation_id varchar(80) NULL`, `metadata_redacted jsonb NULL`, `occurred_at timestamptz NOT NULL`

Không log OTP, token, password, CCCD/hộ chiếu/BHYT/PID nguyên bản hoặc nội dung bệnh án đầy đủ.

## 5. Cardinality / quan hệ

- `roles` M:N `users` qua `user_roles`.
- `users` 0..1:0..1 `patients` qua `patients.user_id` nullable unique.
- `users` 1:0..1 `doctors` qua `doctors.user_id` unique.
- `patients` 1:N `patient_contacts`, `patient_identifiers`, `appointments`, `otp_challenges` (qua scope Patient).
- `patients` self-reference qua `merged_into_patient_id`; các identity review cases liên kết hai Patient.
- `doctors` M:N `specialties` qua `doctor_specialties`.
- `specialties` 1:N `services`; `doctors` M:N `services` qua `doctor_services`.
- `doctors` 1:N `doctor_working_rules`, `doctor_time_offs`, `doctor_schedule_blocks`, `appointments`.
- `services` 1:N `appointments`; composite FK `appointments(doctor_id, service_id)` bảo đảm bác sĩ được gán dịch vụ.
- `appointments` 1:N `appointment_histories`, `reschedule_requests`, `payments`, `otp_challenges`, `notifications`; 1:0..1 `appointment_cancellations`, `consultations`.
- `appointment_cancellations` 1:N `refunds`; `payments` 1:N `refunds`.
- `otp_challenges` 1:0..1 `otp_verification_grants`; composite FK bảo đảm đúng booking + purpose.
- `appointments(id, patient_id)` và `appointments(id, doctor_id)` là unique candidate keys phục vụ các composite FK ở bảng con.

## 6. Index, unique, check constraints

Các index tối thiểu:
- `users(email_normalized)` UNIQUE.
- `patients(patient_code)` UNIQUE; `patients(user_id)` UNIQUE WHERE NOT NULL.
- `patient_contacts(id, patient_id)` UNIQUE để enforce contact thuộc đúng Patient; `patient_contacts(patient_id, contact_type)` partial unique cho một primary PHONE/EMAIL mỗi Patient.
- `patient_contacts(value_hmac)` partial unique chỉ khi PHONE + `is_verified=true` + `is_personal_owner_verified=true` + active.
- CHECK trên `patient_contacts`: personal-owner verified chỉ áp dụng cho PHONE đã verified.
- `cancellation_policy_versions` chỉ có tối đa một version active; cặp refund/fee rates ở từng nhánh phải nằm [0,1] và tổng bằng 1.
- `patient_identifiers(identifier_type, issuer_code, normalized_value_hmac)` partial unique khi VERIFIED + active.
- `appointments(booking_code)` UNIQUE; `(patient_id, start_at DESC)`, `(doctor_id, start_at)`, `(status, start_at)`.
- `reschedule_requests(appointment_id)` partial unique WHERE status='PendingReview'; `otp_grant_id` unique khi khác NULL để grant không bị tái sử dụng.
- `otp_verification_grants(id, appointment_id)` UNIQUE; composite FK từ RescheduleRequest/Cancellation giới hạn grant vào đúng booking.
- `appointment_cancellations(otp_grant_id)` partial unique khi khác NULL; cancellation có composite FK liên kết Patient actor với đúng Appointment.
- `appointments(id, patient_id)`, `appointments(id, doctor_id)`, `doctor_services(doctor_id, service_id)` có UNIQUE/PK phù hợp cho composite FK.
- `otp_challenges(id, appointment_id, purpose)` UNIQUE cho composite FK của grant; `otp_grant_id` chỉ được sử dụng một lần trong reschedule/cancellation.
- `payments(appointment_id, status)`; `refunds(appointment_cancellation_id, status)`.
- `otp_challenges(appointment_id, purpose, created_at DESC)`; `expires_at`.
- `notifications(status, created_at)`.
- `audit_logs(entity_type, entity_id, occurred_at DESC)` và `actor_user_id, occurred_at DESC`.
- `idempotency_records(scope, key_hash)` UNIQUE.

CHECK constraints cần có:
- Patient có DOB hoặc BirthYear; nếu cả hai thì khớp năm.
- Date of birth không ở tương lai (validate tại Application; CHECK với `CURRENT_DATE` không nên dùng do tính ổn định của CHECK).
- Schedule/Appointment/TimeOff/Block có start < end.
- Duration > 0; giá/amount >= 0.
- Tỷ lệ refund/fee nằm trong [0,1]; refund rate + cancellation fee rate = 1 cho từng nhánh policy.
- Các enum/status chỉ nhận giá trị hợp lệ (CHECK hoặc PostgreSQL enum; thống nhất một cách trước Migration).

## 7. Chống trùng lịch ở PostgreSQL

Đề xuất `btree_gist` và exclusion constraint trên `appointments`. Khoảng dùng `[start_at, end_at)` nên hai lịch liền kề 09:00–09:30 và 09:30–10:00 không bị coi là chồng lấn.

```sql
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE appointments
ADD CONSTRAINT ex_appointments_doctor_no_overlap
EXCLUDE USING gist (
    doctor_id WITH =,
    tstzrange(start_at, end_at, '[)') WITH &&
)
WHERE (status <> 'Cancelled');
```

- Ứng dụng vẫn phải kiểm tra availability để hiển thị UX tốt.
- Exclusion constraint mới là hàng rào cuối khi hai request đồng thời.
- Booking: insert appointment trong transaction; chỉ trả success sau commit.
- Reschedule approval: lock appointment row, verify request/state, cập nhật `start_at/end_at` trong transaction. Nếu slot đã bị booking khác chiếm, constraint vi phạm; rollback giữ lịch cũ.
- Map PostgreSQL SQLSTATE `23P01` (exclusion_violation) thành HTTP 409 Conflict.
- Cần migration SQL thủ công/`migrationBuilder.Sql(...)` và Integration Test trên PostgreSQL thật; EF Core InMemory không kiểm thử được constraint này.

## 8. Transaction workflows

### 8.1 Guest booking
1. Kiểm tra idempotency key + request hash.
2. Validate bác sĩ, service, rule/exception và thời điểm.
3. Tìm candidate Patient theo định danh HMAC/phone/name+DOB; không auto-merge.
4. Tạo Patient mới hoặc tiếp tục hồ sơ đã được staff xác minh; nếu nghi trùng, tạo identity review case.
5. Tạo patient contact được mã hóa và booking contact.
6. Tạo Appointment + service snapshots + initial history.
7. Ghi idempotency resource mapping.
8. Commit transaction; sau commit mới enqueue notification.

Nếu có định danh trùng/xung đột, không tiết lộ hồ sơ cũ cho Guest và không tự liên kết hồ sơ có sẵn khi chưa xác minh quyền. Cách xử lý booking trong lúc chờ manual review cần được xác định trong UI/Reception process.

### 8.2 OTP lookup/change/cancel
1. Nhận BookingCode + phone + purpose.
2. Rate limit và trả response chung, không tiết lộ booking/phone tồn tại.
3. Khi match hợp lệ, tạo OtpChallenge, lưu HMAC/pepper của OTP và expiry/attempts.
4. Gửi OTP qua `ISmsSender` sau khi tạo challenge.
5. Verify đúng challenge + purpose + booking + phone; cấp grant có expiry.
6. Grant không có quyền truy cập toàn hệ thống và không thay User JWT.

### 8.3 Reschedule
1. Guest/Patient xác minh OTP purpose RESCHEDULE.
2. Tạo RescheduleRequest PendingReview; Appointment giữ nguyên slot cũ.
3. Receptionist duyệt/từ chối và ghi actor/time/reason.
4. Khi duyệt, lock Appointment và Request, validate state/time.
5. Update start/end trong transaction; exclusion constraint kiểm tra slot.
6. Thành công: Appointment giữ lịch mới, request Approved, history + audit.
7. Conflict: rollback, lịch cũ không đổi. Xử lý request conflict (giữ PendingReview hay tự chuyển Rejected) là policy vận hành cần chốt.

### 8.4 Patient cancellation and refund
1. Verify OTP purpose CANCEL (hoặc session Patient có quyền tương ứng).
2. Lock Appointment; xác nhận trạng thái còn được hủy và `now < StartAt`.
3. Ghi `cancelled_at` bằng server UTC, tính `seconds_before_start`.
4. Nếu `seconds_before_start >= 3600`: refund rate 100%, fee rate 0%.
5. Nếu `0 < seconds_before_start < 3600`: refund rate 50%, fee rate 50%.
6. Tổng hợp payment đã Succeeded cho service fee; tính refund dự kiến trên số đã trả.
7. Tạo AppointmentCancellation + Refund record nếu cần; chuyển Appointment Cancelled; ghi history/audit trong cùng transaction.
8. Thực hiện refund bên ngoài transaction; chỉ chuyển Succeeded khi provider/quy trình thủ công xác nhận hoàn tất.
9. Chưa thanh toán: refund = 0; không tự thu phí hủy chưa thanh toán (TBD-04).

## 9. Patient identity matching

### 9.1 Chuẩn hóa
- FullName: Unicode normalization, trim, gộp whitespace, case-fold theo quy tắc nhất quán; không loại bỏ dấu tiếng Việt làm mất thông tin.
- Phone: chuẩn E.164 trước HMAC.
- CCCD/passport/BHYT/PID: chuẩn theo từng loại và `issuer_code`; không dùng chung một cách normalize cho tất cả.

### 9.2 Matching priority
1. Exact HMAC match với identifier mạnh đã VERIFIED.
2. Exact match với số điện thoại cá nhân đã xác minh quyền sở hữu.
3. FullName + DateOfBirth.
4. FullName + BirthYear.

### 9.3 Resolution policy
- Strong identifier exact match tạo candidate mạnh nhưng không tiết lộ dữ liệu và không tự động merge chỉ từ dữ liệu Guest nhập.
- Name + DOB/BirthYear chỉ tạo candidate; không unique.
- Số điện thoại dùng chung không được coi là định danh duy nhất.
- Nếu có xung đột/đa candidate, tạo PatientIdentityReviewCase cho Receptionist/nhân sự được phân quyền.
- Merge phải có người thực hiện, lý do, thời điểm, snapshot redacted và audit.

### 9.4 Security
- Lưu `value_ciphertext` bằng mã hóa ứng dụng; khóa ở secret store, không ở DB/repository.
- Lưu `value_hmac` bằng HMAC-SHA-256 với key riêng để exact lookup; key không đặt trong DB.
- Mask định danh khi hiển thị; chỉ role được phép xem full value sau khi có business need.
- Không log raw identifier, OTP, grant token hoặc clinical note.

## 10. EF Core / Npgsql mapping guidance

- UUID ↔ `Guid`.
- `timestamptz` ↔ `DateTimeOffset` (chuẩn hóa UTC); không trộn `DateTimeKind.Local` và UTC.
- `date` ↔ `DateOnly` nếu phiên bản .NET/Npgsql đang dùng hỗ trợ; nếu chưa, dùng mapping tương thích đã test.
- `time` ↔ `TimeOnly` nếu hỗ trợ.
- `numeric(12,2)` ↔ `decimal`.
- `jsonb` ↔ JSON document/owned mapping tùy cách tổ chức; không lưu PII raw trong metadata.
- Explicit table/column names, precision, max length, delete behavior, unique/partial indexes.
- Exclusion constraint và partial indexes phức tạp cần SQL trong Migration và integration test.
- Không dùng EF Core InMemory để kết luận PostgreSQL constraint/transaction hoạt động.

## 11. Migration order đề xuất

1. Extensions / roles / users / user_roles / refresh_tokens.
2. Patients / patient_contacts / patient_identifiers.
3. Patient identity review / merge event tables.
4. Specialties / doctors / doctor_specialties / services / doctor_services.
5. Doctor working rules / time offs / schedule blocks.
6. Cancellation policy versions.
7. Appointments / appointment histories.
8. OTP challenges / verification grants (both reference Appointments/PatientContacts).
9. Reschedule requests (may reference OTP verification grants).
10. Appointment cancellations / payments / refunds.
11. Notifications / consultations / audit logs / idempotency records.
12. Indexes, partial unique indexes, CHECK constraints, exclusion constraint.
13. Seed roles and initial cancellation policy v1.

Tạo migration từng phần nhỏ, review SQL do EF sinh ra, apply trên DB local thử nghiệm và test từ DB sạch trước khi đưa vào nhánh chính.

## 12. TBD trước khi chốt physical schema

- **DB-TBD-01:** Có cần nhiều chi nhánh/phòng khám hoặc phòng khám/phòng khám riêng theo Doctor không?
- **DB-TBD-02:** Payment có cho phép thanh toán một phần không?
- **DB-TBD-03:** Nếu chưa thanh toán, có phát sinh khoản phí hủy phải thu không? Baseline hiện tại: không tự thu.
- **DB-TBD-04:** Khi slot reschedule bị người khác đặt trước lúc lễ tân duyệt, request chuyển Rejected tự động hay giữ PendingReview để lễ tân chọn slot khác? Baseline an toàn: không đổi lịch cũ; trả conflict và yêu cầu lễ tân xử lý.
- **DB-TBD-05:** Quy trình xác minh CCCD/passport/BHYT/PID và vai trò có quyền VERIFIED.
- **DB-TBD-06:** Có cho phép Patient đổi số điện thoại booking sau khi đã đặt không? Nếu có, cần OTP số cũ hoặc manual verification.
- **DB-TBD-07:** Thời hạn lưu hồ sơ bệnh án, audit log, OTP và dữ liệu định danh.
- **DB-TBD-08:** Nhà cung cấp SMS/email/payment gateway.
- **DB-TBD-13:** Quy tắc làm tròn tiền theo currency/provider; baseline VND lưu `numeric(12,2)` nhưng nghiệp vụ VND có thể yêu cầu số nguyên, cần xác nhận trước tính phí/hoàn.
- **DB-TBD-09:** Quy tắc hủy bởi phòng khám/bác sĩ và NoShow.
- **DB-TBD-10:** Thời hạn đặt lịch trước, thời gian giữ slot và giới hạn lịch mỗi bệnh nhân.
- **DB-TBD-11:** Một Patient có được dùng nhiều tài khoản User không? Baseline: tối đa một User liên kết với một Patient.
- **DB-TBD-12:** Lịch khám có thể đổi sang bác sĩ khác hay chỉ đổi thời gian với cùng bác sĩ? Baseline: cùng bác sĩ.

## 13. Deliverables and review gate

- `docs/DATABASE_DESIGN.md` — logical model and constraints.
- `docs/DATA_DICTIONARY.md` — field-level dictionary.
- `docs/ERD.md` — Mermaid ERD master and relationship summary.
- `diagrams/mermaid/erd-*.mmd` — ERD split by domain.
- `diagrams/plantuml/erd-overview.puml` — editable PlantUML overview.
- `database/reference/constraints-reference.sql` — SQL pattern reference, not a full schema/migration.
- `docs/DATABASE_REVIEW_CHECKLIST.md` — manual review before implementation.

**Database Feature chỉ được xem là READY FOR IMPLEMENTATION sau khi** owner xác nhận TBD có ảnh hưởng, ERD và data dictionary được duyệt, constraints được review và Agent Audit không còn blocker. Chưa có Database hoặc Migration nào được tạo trong bước này.
