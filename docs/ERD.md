# ENTITY RELATIONSHIP DIAGRAM (ERD)

- **Version:** 1.2 — Logical ERD draft with cross-table scope constraints
- **Nguồn:** `docs/DATABASE_DESIGN.md`
- **Lưu ý:** Các sơ đồ tách theo domain để dễ đọc. Sơ đồ Mermaid hiển thị được trên GitHub; file PlantUML trong `diagrams/plantuml/` có thể chỉnh sửa.

## 0. ERD images

- [ERD 01 — Identity & Access](../diagrams/ERD_01_IDENTITY.png)
- [ERD 02 — Catalog & Scheduling](../diagrams/ERD_02_CATALOG_SCHEDULING.png)
- [ERD 03 — Appointments & Finance](../diagrams/ERD_03_APPOINTMENTS_FINANCE.png)
- [ERD 04 — OTP & Operations](../diagrams/ERD_04_OTP_OPERATIONS.png)
- [ERD Overview](../diagrams/ERD_OVERVIEW.png)

## 1. Identity & Access

```mermaid
erDiagram
    ROLES ||--o{ USER_ROLES : assigns
    USERS ||--o{ USER_ROLES : has
    USERS ||--o| PATIENTS : optional_profile
    USERS ||--o| DOCTORS : doctor_account
    USERS ||--o{ REFRESH_TOKENS : owns
    PATIENTS ||--o{ PATIENT_CONTACTS : has
    PATIENTS ||--o{ PATIENT_IDENTIFIERS : has
    PATIENTS ||--o{ PATIENT_IDENTITY_REVIEW_CASES : reviewed_patient
    PATIENTS ||--o{ PATIENT_MERGE_EVENTS : source
    PATIENTS ||--o{ PATIENT_MERGE_EVENTS : target
    USERS ||--o{ PATIENT_IDENTITY_REVIEW_CASES : reviews

    ROLES {
      uuid id PK
      varchar code UK
      varchar name
    }
    USERS {
      uuid id PK
      varchar email_normalized UK
      text password_hash
      boolean is_active
    }
    USER_ROLES {
      uuid user_id PK,FK
      uuid role_id PK,FK
    }
    PATIENTS {
      uuid id PK
      uuid user_id FK "nullable unique"
      varchar patient_code UK
      varchar full_name
      date date_of_birth "nullable"
      smallint birth_year "nullable"
      varchar record_status
    }
    PATIENT_CONTACTS {
      uuid id PK
      uuid patient_id FK
      varchar contact_type
      bytea value_ciphertext
      bytea value_hmac
      boolean is_personal_owner_verified
    }
    PATIENT_IDENTIFIERS {
      uuid id PK
      uuid patient_id FK
      varchar identifier_type
      varchar issuer_code
      bytea normalized_value_hmac
      varchar verification_status
    }
    DOCTORS {
      uuid id PK
      uuid user_id FK
      varchar doctor_code UK
    }
    REFRESH_TOKENS {
      uuid id PK
      uuid user_id FK
      bytea token_hash UK
    }
    PATIENT_IDENTITY_REVIEW_CASES {
      uuid id PK
      uuid patient_id FK
      uuid candidate_patient_id FK
      varchar match_reason
      varchar status
    }
    PATIENT_MERGE_EVENTS {
      uuid id PK
      uuid source_patient_id FK
      uuid target_patient_id FK
      uuid merged_by_user_id FK
    }
```

## 2. Catalog & Scheduling

```mermaid
erDiagram
    SPECIALTIES ||--o{ DOCTOR_SPECIALTIES : has
    DOCTORS ||--o{ DOCTOR_SPECIALTIES : practices
    SPECIALTIES ||--o{ SERVICES : offers
    DOCTORS ||--o{ DOCTOR_SERVICES : provides
    SERVICES ||--o{ DOCTOR_SERVICES : assigned
    DOCTORS ||--o{ DOCTOR_WORKING_RULES : works
    DOCTORS ||--o{ DOCTOR_TIME_OFFS : unavailable
    DOCTORS ||--o{ DOCTOR_SCHEDULE_BLOCKS : blocked

    SPECIALTIES {
      uuid id PK
      varchar code UK
      varchar name
      varchar slug UK
    }
    DOCTOR_SPECIALTIES {
      uuid doctor_id PK,FK
      uuid specialty_id PK,FK
      boolean is_primary
    }
    DOCTOR_SERVICES {
      uuid doctor_id PK,FK
      uuid service_id PK,FK
      integer duration_minutes_override
      numeric price_override
    }
    SERVICES {
      uuid id PK
      uuid specialty_id FK
      varchar code UK
      integer duration_minutes
      numeric price
      char currency
    }
    DOCTOR_WORKING_RULES {
      uuid id PK
      uuid doctor_id FK
      smallint day_of_week
      time start_local_time
      time end_local_time
      date effective_from
      date effective_to
    }
    DOCTOR_TIME_OFFS {
      uuid id PK
      uuid doctor_id FK
      timestamptz start_at
      timestamptz end_at
    }
    DOCTOR_SCHEDULE_BLOCKS {
      uuid id PK
      uuid doctor_id FK
      timestamptz start_at
      timestamptz end_at
      varchar block_type
    }
```

## 3. Appointment, Reschedule, Cancellation & Finance

```mermaid
erDiagram
    PATIENTS ||--o{ APPOINTMENTS : books
    PATIENT_CONTACTS ||--o{ APPOINTMENTS : booking_contact
    DOCTORS ||--o{ APPOINTMENTS : assigned
    SERVICES ||--o{ APPOINTMENTS : service
    APPOINTMENTS ||--o{ APPOINTMENT_HISTORIES : records
    APPOINTMENTS ||--o{ RESCHEDULE_REQUESTS : requests
    APPOINTMENTS ||--o| APPOINTMENT_CANCELLATIONS : cancellation
    APPOINTMENTS ||--o{ PAYMENTS : paid_by
    CANCELLATION_POLICY_VERSIONS ||--o{ APPOINTMENT_CANCELLATIONS : policy
    APPOINTMENT_CANCELLATIONS ||--o{ REFUNDS : generates
    PAYMENTS ||--o{ REFUNDS : refunded_from
    DOCTORS ||--o{ CONSULTATIONS : performs
    APPOINTMENTS ||--o| CONSULTATIONS : results

    APPOINTMENTS {
      uuid id PK
      varchar booking_code UK
      uuid patient_id FK
      uuid booking_contact_id FK
      uuid doctor_id FK
      uuid service_id FK
      timestamptz start_at
      timestamptz end_at
      varchar status
      numeric service_fee_snapshot
    }
    APPOINTMENT_HISTORIES {
      uuid id PK
      uuid appointment_id FK
      varchar from_status
      varchar to_status
      varchar actor_type
    }
    RESCHEDULE_REQUESTS {
      uuid id PK
      uuid appointment_id FK
      timestamptz proposed_start_at
      timestamptz proposed_end_at
      varchar status
      uuid reviewed_by_user_id FK
    }
    CANCELLATION_POLICY_VERSIONS {
      uuid id PK
      varchar version UK
      integer threshold_minutes
      numeric refund_rate_at_or_above
      numeric refund_rate_below
    }
    APPOINTMENT_CANCELLATIONS {
      uuid id PK
      uuid appointment_id FK,UK
      uuid otp_grant_id FK
      uuid policy_version_id FK
      bigint seconds_before_start
      numeric expected_refund_amount
      varchar refund_status
    }
    PAYMENTS {
      uuid id PK
      uuid appointment_id FK
      numeric amount
      varchar status
      varchar method
    }
    REFUNDS {
      uuid id PK
      uuid appointment_cancellation_id FK
      uuid payment_id FK
      numeric amount
      varchar status
    }
    CONSULTATIONS {
      uuid id PK
      uuid appointment_id FK,UK
      uuid doctor_id FK
      text diagnosis
    }
```

## 4. OTP, Notifications, Audit & Idempotency

Lưu ý: OTP challenge có `patient_id` để composite FK buộc `appointment_id` và `patient_contact_id` thuộc cùng một Patient. OTP grant có composite FK tới đúng challenge + booking + purpose. Đây là ràng buộc vật lý bổ sung cho các quan hệ logic bên dưới.

```mermaid
erDiagram
    APPOINTMENTS ||--o{ OTP_CHALLENGES : protected_by
    PATIENTS ||--o{ OTP_CHALLENGES : owner
    PATIENT_CONTACTS ||--o{ OTP_CHALLENGES : destination
    OTP_CHALLENGES ||--o| OTP_VERIFICATION_GRANTS : issues
    APPOINTMENTS ||--o{ NOTIFICATIONS : triggers
    PATIENTS ||--o{ NOTIFICATIONS : receives
    USERS ||--o{ AUDIT_LOGS : acts

    OTP_CHALLENGES {
      uuid id PK
      uuid appointment_id FK
      uuid patient_id FK
      uuid patient_contact_id FK
      varchar purpose
      bytea otp_verifier_hmac
      timestamptz expires_at
      timestamptz consumed_at
    }
    OTP_VERIFICATION_GRANTS {
      uuid id PK
      uuid challenge_id FK,UK
      uuid appointment_id FK
      varchar purpose
      bytea grant_token_hash UK
      timestamptz expires_at
      timestamptz consumed_at
    }
    NOTIFICATIONS {
      uuid id PK
      uuid patient_id FK
      uuid appointment_id FK
      varchar channel
      varchar status
    }
    AUDIT_LOGS {
      uuid id PK
      uuid actor_user_id FK
      varchar action
      varchar entity_type
      uuid entity_id
      jsonb metadata_redacted
    }
    IDEMPOTENCY_RECORDS {
      uuid id PK
      varchar scope
      bytea key_hash
      bytea request_hash
      uuid resource_id
    }
```

## 5. Relationship rules to verify

1. `Patients.UserId` nullable and UNIQUE: guest-created Patient không cần User.
2. `Appointments` luôn có Patient, Doctor, Service và booking contact.
3. `Appointments` không lưu raw CCCD/BHYT/PID/OTP.
4. Một Appointment tối đa một Consultation và một Cancellation.
5. Một Appointment có thể có nhiều RescheduleRequest nhưng tối đa một request PendingReview.
6. Reschedule request không giữ proposed slot trước khi Receptionist approve.
7. PostgreSQL exclusion constraint kiểm soát overlap giữa appointment đang giữ slot.
8. Một Appointment có thể có nhiều Payment và Refund; không hoàn vượt số đã thanh toán thành công.
9. OTP grant luôn scope theo Appointment và purpose, không thay thế User authentication.
10. Merge Patient phải giữ nguồn gốc, audit và liên kết lịch sử.
