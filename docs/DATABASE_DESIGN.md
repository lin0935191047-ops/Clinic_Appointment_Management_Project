# Database Design Specification

> **Đồng bộ với:** [docs/SPEC.md](SPEC.md) (Phiên bản SRS 1.2 Baseline)

---

## 1. Tổng quan thiết kế CSDL (Overview)

Hệ thống sử dụng hệ quản trị cơ sở dữ liệu quan hệ **PostgreSQL 16+**.
- **Quy chuẩn đặt tên:** Chữ thường nối gạch dưới (`snake_case`) cho bảng và cột.
- **Khóa chính:** `UUID` (sử dụng `gen_random_uuid()`) cho toàn bộ thực thể nghiệp vụ.
- **Thời gian:** Toàn bộ cột thời gian sử dụng kiểu `timestamptz` (Timestamp with Time Zone - luôn lưu trữ chuẩn UTC).
- **Xóa mềm (Soft Delete):** Áp dụng cột `is_deleted (boolean)` và `deleted_at (timestamptz)` trên các bảng nghiệp vụ.
- **Bảo mật PII:** Các định danh y tế nhạy cảm (CCCD, Hộ chiếu, Thẻ BHYT, Mã bệnh án cũ) được mã hóa tại tầng lưu trữ và lưu kèm mã băm có khóa bí mật (`Keyed HMAC`) để tra cứu chính xác (`Exact Lookup`).

---

## 2. Sơ đồ thực thể quan hệ (ERD - Entity Relationship Diagram)

```mermaid
erDiagram
    USERS ||--o| DOCTORS : "has profile"
    USERS ||--o| PATIENTS : "owns account (nullable)"
    SPECIALTIES ||--o{ DOCTORS : "belongs to"
    SPECIALTIES ||--o{ SERVICES : "offers"
    DOCTORS ||--o{ DOCTOR_SCHEDULES : "has shifts"
    DOCTORS ||--o{ DOCTOR_UNAVAILABILITIES : "has leaves/busy times"
    DOCTORS ||--o{ APPOINTMENTS : "assigned to"
    PATIENTS ||--o{ APPOINTMENTS : "books"
    SERVICES ||--o{ APPOINTMENTS : "requested service"
    APPOINTMENTS ||--o| RESCHEDULE_REQUESTS : "may have request"
    APPOINTMENTS ||--o| REFUNDS : "may have refund"
    APPOINTMENTS ||--o| MEDICAL_NOTES : "clinical notes"
    PATIENTS ||--o{ OTP_CHALLENGES : "verifies via"

    USERS {
        uuid id PK
        varchar email UK
        varchar phone_number UK
        varchar password_hash
        varchar full_name
        varchar role "Admin, Doctor, Receptionist, Patient"
        boolean is_active
        timestamptz created_at
    }

    SPECIALTIES {
        uuid id PK
        varchar name UK
        varchar slug UK
        text description
        varchar icon_url
        boolean is_active
    }

    SERVICES {
        uuid id PK
        uuid specialty_id FK
        varchar name
        text description
        int duration_minutes "Default 30"
        numeric base_price
        boolean is_active
    }

    DOCTORS {
        uuid id PK
        uuid user_id FK,UK
        uuid specialty_id FK
        varchar title "BS. CKI, ThS.BS, PGS.TS..."
        text bio
        decimal consultation_fee
        varchar room_number
        boolean is_active
    }

    PATIENTS {
        uuid id PK
        uuid user_id FK "nullable (Guest)"
        varchar full_name NOT_NULL
        date date_of_birth "nullable if birth_year present"
        smallint birth_year "nullable if date_of_birth present"
        varchar phone_number NOT_NULL
        boolean phone_verified
        varchar email
        varchar gender "Male, Female, Other"
        text address
        text national_id_encrypted
        varchar national_id_hmac
        text passport_encrypted
        varchar passport_hmac
        text insurance_card_encrypted
        varchar insurance_card_hmac
        text pid_encrypted
        varchar pid_hmac
        boolean is_possible_duplicate
        uuid merged_into_patient_id FK "self reference"
        timestamptz created_at
    }

    DOCTOR_SCHEDULES {
        uuid id PK
        uuid doctor_id FK
        smallint day_of_week "0=Sun..6=Sat"
        time start_time
        time end_time
        int slot_duration_minutes "Default 30"
        boolean is_active
    }

    DOCTOR_UNAVAILABILITIES {
        uuid id PK
        uuid doctor_id FK
        timestamptz start_time
        timestamptz end_time
        varchar reason
    }

    APPOINTMENTS {
        uuid id PK
        varchar booking_code UK "Format: APT-YYYYMMDD-XXXX"
        uuid doctor_id FK
        uuid patient_id FK
        uuid service_id FK
        varchar service_name_snapshot
        int service_duration_snapshot
        numeric service_price_snapshot
        date appointment_date
        time start_time
        time end_time
        varchar status "PendingConfirmation, Confirmed, CheckedIn, InProgress, Completed, Cancelled, NoShow"
        numeric paid_amount "Default 0"
        text reason_for_visit
        timestamptz cancellation_accepted_at "Server timestamp"
        text cancellation_reason
        boolean is_guest
        timestamptz created_at
        timestamptz updated_at
    }

    RESCHEDULE_REQUESTS {
        uuid id PK
        uuid appointment_id FK
        uuid requested_doctor_id FK
        date requested_date
        time requested_start_time
        time requested_end_time
        text reason
        varchar status "PendingReview, Approved, Rejected, Cancelled, Expired"
        uuid reviewed_by_user_id FK
        timestamptz reviewed_at
        text review_notes
        timestamptz created_at
    }

    REFUNDS {
        uuid id PK
        uuid appointment_id FK,UK
        numeric paid_service_amount
        numeric cancellation_fee_rate "0 or 0.50"
        numeric cancellation_fee_amount
        numeric expected_refund_amount
        varchar refund_status "NotApplicable, Pending, Processing, Succeeded, Failed"
        text reason
        timestamptz processed_at
        timestamptz created_at
    }

    OTP_CHALLENGES {
        uuid id PK
        varchar phone_number
        varchar booking_code
        varchar purpose "LOOKUP, RESCHEDULE, CANCEL"
        varchar otp_hash
        smallint attempts_count
        smallint max_attempts "Default 5"
        timestamptz expires_at
        boolean is_used
        timestamptz created_at
    }

    MEDICAL_NOTES {
        uuid id PK
        uuid appointment_id FK,UK
        uuid doctor_id FK
        text initial_symptoms
        text clinical_examination
        text diagnosis
        text treatment_instructions
        timestamptz created_at
    }
```

---

## 3. Chi tiết các bảng dữ liệu (Data Dictionary)

### 3.1 Bảng `patients` (Hồ sơ bệnh nhân độc lập)
| Cột | Kiểu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | PK, Default `gen_random_uuid()` | Định danh bệnh nhân |
| `user_id` | `uuid` | FK -> `users.id`, Nullable | Null đối với khách vãng lai (Guest) |
| `full_name` | `varchar(150)` | NOT NULL | Họ và tên đầy đủ |
| `date_of_birth` | `date` | Nullable | Ngày sinh đầy đủ (Check constraint cùng birth_year) |
| `birth_year` | `smallint` | Nullable | Năm sinh (nếu không rõ ngày tháng) |
| `phone_number` | `varchar(20)` | NOT NULL | Số điện thoại liên hệ |
| `phone_verified` | `boolean` | Default `false` | Đã xác minh qua OTP hay chưa |
| `email` | `varchar(150)` | Nullable | Email liên hệ nhận thông báo |
| `gender` | `varchar(10)` | Nullable | `Male`, `Female`, `Other` |
| `address` | `text` | Nullable | Địa chỉ cư trú |
| `national_id_encrypted`| `text` | Nullable | CCCD mã hóa (AES-256-GCM) |
| `national_id_hmac` | `varchar(64)` | Nullable | Hash HMAC phục vụ exact search CCCD |
| `passport_encrypted` | `text` | Nullable | Hộ chiếu mã hóa |
| `passport_hmac` | `varchar(64)` | Nullable | Hash HMAC exact search Hộ chiếu |
| `insurance_card_encrypted`| `text`| Nullable | Mã thẻ BHYT mã hóa |
| `insurance_card_hmac`| `varchar(64)`| Nullable | Hash HMAC exact search BHYT |
| `pid_encrypted` | `text` | Nullable | Mã hồ sơ y tế cũ (PID) mã hóa |
| `pid_hmac` | `varchar(64)` | Nullable | Hash HMAC exact search PID |
| `is_possible_duplicate`| `boolean`| Default `false` | Đánh dấu hồ sơ nghi trùng lặp cần rà soát |
| `merged_into_patient_id`| `uuid` | FK -> `patients.id`, Nullable | Hồ sơ đích sau khi được duyệt gộp thủ công |
| `created_at` | `timestamptz` | NOT NULL, Default `now()` | Thời điểm tạo |

> **Ràng buộc kiểm tra (Check Constraint):**  
> `CONSTRAINT chk_patients_dob_or_year CHECK (date_of_birth IS NOT NULL OR birth_year IS NOT NULL)`

---

### 3.2 Bảng `appointments` (Lịch hẹn khám)
| Cột | Kiểu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | PK | Định danh lịch hẹn |
| `booking_code` | `varchar(32)` | UNIQUE, NOT NULL | Mã tra cứu ngẫu nhiên (Format `APT-YYYYMMDD-XXXX`) |
| `doctor_id` | `uuid` | FK -> `doctors.id`, NOT NULL | Bác sĩ phụ trách |
| `patient_id` | `uuid` | FK -> `patients.id`, NOT NULL | Bệnh nhân đặt lịch |
| `service_id` | `uuid` | FK -> `services.id`, NOT NULL | Dịch vụ khám lựa chọn |
| `service_name_snapshot`| `varchar(150)`| NOT NULL | Tên dịch vụ tại thời điểm đặt |
| `service_duration_snapshot`| `int` | NOT NULL | Thời lượng dịch vụ (phút) lúc đặt |
| `service_price_snapshot`| `numeric(12,2)`| NOT NULL | Giá dịch vụ niêm yết lúc đặt |
| `appointment_date`| `date` | NOT NULL | Ngày hẹn khám |
| `start_time` | `time` | NOT NULL | Giờ bắt đầu slot khám |
| `end_time` | `time` | NOT NULL | Giờ kết thúc slot khám |
| `status` | `varchar(25)` | NOT NULL | `PendingConfirmation`, `Confirmed`, `CheckedIn`, `InProgress`, `Completed`, `Cancelled`, `NoShow` |
| `paid_amount` | `numeric(12,2)`| Default 0 | Số tiền bệnh nhân đã thanh toán |
| `reason_for_visit`| `text` | Nullable | Triệu chứng / Lý do khám bệnh |
| `cancellation_accepted_at`| `timestamptz`| Nullable | Dấu thời gian máy chủ (UTC) khi tiếp nhận hủy |
| `cancellation_reason`| `text` | Nullable | Lý do hủy lịch |
| `is_guest` | `boolean` | Default `false` | Đặt lịch diện khách vãng lai |
| `created_at` | `timestamptz` | NOT NULL, Default `now()` | Thời điểm tạo |
| `updated_at` | `timestamptz` | Nullable | Thời điểm cập nhật cuối |

---

### 3.3 Bảng `reschedule_requests` (Yêu cầu Đổi lịch)
| Cột | Kiểu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | PK | Định danh yêu cầu đổi lịch |
| `appointment_id` | `uuid` | FK -> `appointments.id`, NOT NULL | Lịch hẹn cần đổi |
| `requested_doctor_id`| `uuid` | FK -> `doctors.id`, NOT NULL | Bác sĩ đề xuất |
| `requested_date`| `date` | NOT NULL | Ngày khám mới đề xuất |
| `requested_start_time`| `time`| NOT NULL | Giờ bắt đầu mới |
| `requested_end_time`| `time` | NOT NULL | Giờ kết thúc mới |
| `reason` | `text` | Nullable | Lý do bệnh nhân muốn đổi lịch |
| `status` | `varchar(20)` | NOT NULL | `PendingReview`, `Approved`, `Rejected`, `Cancelled`, `Expired` |
| `reviewed_by_user_id`| `uuid`| FK -> `users.id`, Nullable | Lễ tân tiếp nhận xử lý |
| `reviewed_at` | `timestamptz`| Nullable | Thời điểm duyệt / từ chối |
| `review_notes` | `text` | Nullable | Ghi chú phản hồi của lễ tân |
| `created_at` | `timestamptz`| NOT NULL, Default `now()` | Thời điểm gửi yêu cầu |

---

### 3.4 Bảng `refunds` (Hoàn tiền & Phí hủy)
| Cột | Kiểu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | PK | Định danh bản ghi hoàn tiền |
| `appointment_id` | `uuid` | FK -> `appointments.id`, UNIQUE | Lịch hẹn liên quan |
| `paid_service_amount`| `numeric(12,2)`| NOT NULL | Số tiền dịch vụ đã thanh toán thực tế |
| `cancellation_fee_rate`| `numeric(5,2)`| NOT NULL | Tỷ lệ phí hủy (0.00 hoặc 0.50) |
| `cancellation_fee_amount`| `numeric(12,2)`| NOT NULL | Số tiền phí hủy khấu trừ |
| `expected_refund_amount`| `numeric(12,2)`| NOT NULL | Số tiền dự kiến hoàn trả |
| `refund_status` | `varchar(20)` | NOT NULL | `NotApplicable`, `Pending`, `Processing`, `Succeeded`, `Failed` |
| `reason` | `text` | Nullable | Lý do hoàn tiền |
| `processed_at` | `timestamptz`| Nullable | Thời điểm xử lý hoàn tiền thành công |
| `created_at` | `timestamptz`| NOT NULL, Default `now()` | Thời điểm tạo bản ghi |

---

### 3.5 Bảng `otp_challenges` (Thử thách xác thực OTP)
| Cột | Kiểu | Ràng buộc | Mô tả |
| :--- | :--- | :--- | :--- |
| `id` | `uuid` | PK | Định danh phiên OTP |
| `phone_number` | `varchar(20)` | NOT NULL | Số điện thoại nhận OTP |
| `booking_code` | `varchar(32)` | NOT NULL | Mã booking gắn kèm |
| `purpose` | `varchar(20)` | NOT NULL | `LOOKUP`, `RESCHEDULE`, `CANCEL` |
| `otp_hash` | `varchar(128)` | NOT NULL | Hash mã OTP (không lưu OTP dạng plain text) |
| `attempts_count` | `smallint` | Default 0 | Số lần nhập sai hiện tại |
| `max_attempts` | `smallint` | Default 5 | Số lần nhập sai tối đa cho phép |
| `expires_at` | `timestamptz` | NOT NULL | Thời điểm hết hạn hiệu lực (5 phút) |
| `is_used` | `boolean` | Default `false` | Đã dùng để xác thực chưa |
| `created_at` | `timestamptz` | NOT NULL, Default `now()` | Thời điểm phát sinh mã |

---

## 4. Ràng buộc toàn vẹn & Chỉ mục hiệu năng (Indexes & Constraints)

1. **Khóa chống Double-booking trên lịch đang hiệu lực (Partial Unique Index):**
   ```sql
   -- Đảm bảo một bác sĩ chỉ có đúng 1 lịch hoạt động trong 1 khung giờ
   CREATE UNIQUE INDEX idx_unique_doctor_active_slot 
   ON appointments (doctor_id, appointment_date, start_time) 
   WHERE status NOT IN ('Cancelled', 'NoShow');
   ```

2. **Chỉ mục giới hạn mỗi lịch chỉ có 1 yêu cầu đổi lịch đang chờ duyệt:**
   ```sql
   CREATE UNIQUE INDEX idx_single_pending_reschedule
   ON reschedule_requests (appointment_id)
   WHERE status = 'PendingReview';
   ```

3. **Chỉ mục tìm kiếm nhanh:**
   ```sql
   -- Tra cứu theo BookingCode
   CREATE UNIQUE INDEX idx_appointments_booking_code ON appointments (booking_code);

   -- Tra cứu exact search định danh nhạy cảm qua HMAC
   CREATE INDEX idx_patients_national_id_hmac ON patients (national_id_hmac) WHERE national_id_hmac IS NOT NULL;
   CREATE INDEX idx_patients_phone ON patients (phone_number);

   -- Phục vụ Reception và Doctor Workspace
   CREATE INDEX idx_appointments_doctor_date ON appointments (doctor_id, appointment_date);
   CREATE INDEX idx_appointments_date_status ON appointments (appointment_date, status);
   ```
