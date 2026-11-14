CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;


DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE otp_challenges (
        id uuid NOT NULL,
        appointment_id uuid NOT NULL,
        patient_id uuid NOT NULL,
        phone_number text NOT NULL,
        purpose integer NOT NULL,
        otp_hash text NOT NULL,
        attempts_count smallint NOT NULL,
        max_attempts smallint NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        is_used boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_otp_challenges PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE roles (
        id uuid NOT NULL,
        code character varying(40) NOT NULL,
        name character varying(100) NOT NULL,
        created_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_roles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE specialties (
        id uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(100) NOT NULL,
        slug character varying(120) NOT NULL,
        description text,
        icon_url text,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_specialties PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        email character varying(254) NOT NULL,
        normalized_email character varying(254) NOT NULL,
        password_hash text NOT NULL,
        is_active boolean NOT NULL DEFAULT TRUE,
        email_verified_at timestamp with time zone,
        last_login_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE services (
        id uuid NOT NULL,
        specialty_id uuid NOT NULL,
        code character varying(50) NOT NULL,
        name character varying(150) NOT NULL,
        description text,
        duration_minutes integer NOT NULL,
        price numeric(12,2) NOT NULL,
        currency text NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_services PRIMARY KEY (id),
        CONSTRAINT fk_services_specialties_specialty_id FOREIGN KEY (specialty_id) REFERENCES specialties (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE doctors (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        specialty_id uuid NOT NULL,
        doctor_code character varying(32) NOT NULL,
        full_name character varying(150) NOT NULL,
        title text,
        bio text,
        room_number text,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_doctors PRIMARY KEY (id),
        CONSTRAINT fk_doctors_specialties_specialty_id FOREIGN KEY (specialty_id) REFERENCES specialties (id) ON DELETE RESTRICT,
        CONSTRAINT fk_doctors_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE patients (
        id uuid NOT NULL,
        user_id uuid,
        patient_code character varying(32) NOT NULL,
        full_name character varying(200) NOT NULL,
        date_of_birth date,
        birth_year smallint,
        phone_number character varying(20) NOT NULL,
        phone_verified boolean NOT NULL,
        email text,
        gender text,
        address text,
        national_id_ciphertext bytea,
        national_id_hmac text,
        passport_ciphertext bytea,
        passport_hmac text,
        insurance_card_ciphertext bytea,
        insurance_card_hmac text,
        pid_ciphertext bytea,
        pid_hmac text,
        is_possible_duplicate boolean NOT NULL,
        merged_into_patient_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_patients PRIMARY KEY (id),
        CONSTRAINT ck_patients_dob_or_birth_year CHECK (date_of_birth IS NOT NULL OR birth_year IS NOT NULL),
        CONSTRAINT fk_patients_users_user_id FOREIGN KEY (user_id) REFERENCES users (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE refresh_tokens (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        token_hash bytea NOT NULL,
        expires_at timestamp with time zone NOT NULL,
        revoked_at timestamp with time zone,
        replaced_by_token_id uuid,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_refresh_tokens PRIMARY KEY (id),
        CONSTRAINT fk_refresh_tokens_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        assigned_at timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id),
        CONSTRAINT fk_user_roles_roles_role_id FOREIGN KEY (role_id) REFERENCES roles (id) ON DELETE CASCADE,
        CONSTRAINT fk_user_roles_users_user_id FOREIGN KEY (user_id) REFERENCES users (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE doctor_schedules (
        id uuid NOT NULL,
        doctor_id uuid NOT NULL,
        day_of_week smallint NOT NULL,
        start_time time without time zone NOT NULL,
        end_time time without time zone NOT NULL,
        slot_duration_minutes integer NOT NULL,
        is_active boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_doctor_schedules PRIMARY KEY (id),
        CONSTRAINT fk_doctor_schedules_doctors_doctor_id FOREIGN KEY (doctor_id) REFERENCES doctors (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE appointments (
        id uuid NOT NULL,
        booking_code character varying(32) NOT NULL,
        doctor_id uuid NOT NULL,
        patient_id uuid NOT NULL,
        service_id uuid NOT NULL,
        start_at timestamp with time zone NOT NULL,
        end_at timestamp with time zone NOT NULL,
        status character varying(30) NOT NULL,
        service_name_snapshot character varying(150) NOT NULL,
        service_duration_snapshot integer NOT NULL,
        service_price_snapshot numeric(12,2) NOT NULL,
        paid_amount numeric(12,2) NOT NULL DEFAULT 0.0,
        reason_for_visit text,
        cancellation_accepted_at timestamp with time zone,
        cancellation_reason text,
        is_guest boolean NOT NULL,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_appointments PRIMARY KEY (id),
        CONSTRAINT fk_appointments_doctors_doctor_id FOREIGN KEY (doctor_id) REFERENCES doctors (id) ON DELETE RESTRICT,
        CONSTRAINT fk_appointments_patients_patient_id FOREIGN KEY (patient_id) REFERENCES patients (id) ON DELETE RESTRICT,
        CONSTRAINT fk_appointments_services_service_id FOREIGN KEY (service_id) REFERENCES services (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE refunds (
        id uuid NOT NULL,
        appointment_id uuid NOT NULL,
        paid_service_amount numeric(12,2) NOT NULL,
        cancellation_fee_rate numeric(5,2) NOT NULL,
        cancellation_fee_amount numeric(12,2) NOT NULL,
        expected_refund_amount numeric(12,2) NOT NULL,
        status character varying(30) NOT NULL,
        reason text,
        processed_at timestamp with time zone,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_refunds PRIMARY KEY (id),
        CONSTRAINT fk_refunds_appointments_appointment_id FOREIGN KEY (appointment_id) REFERENCES appointments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE TABLE reschedule_requests (
        id uuid NOT NULL,
        appointment_id uuid NOT NULL,
        requested_doctor_id uuid NOT NULL,
        proposed_start_at timestamp with time zone NOT NULL,
        proposed_end_at timestamp with time zone NOT NULL,
        reason text,
        status character varying(30) NOT NULL,
        reviewed_by_user_id uuid,
        reviewed_at timestamp with time zone,
        review_notes text,
        created_at timestamp with time zone NOT NULL,
        updated_at timestamp with time zone,
        CONSTRAINT pk_reschedule_requests PRIMARY KEY (id),
        CONSTRAINT fk_reschedule_requests_appointments_appointment_id FOREIGN KEY (appointment_id) REFERENCES appointments (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_appointments_booking_code ON appointments (booking_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_appointments_doctor_id_start_at ON appointments (doctor_id, start_at) WHERE status NOT IN ('Cancelled', 'NoShow');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_appointments_patient_id ON appointments (patient_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_appointments_service_id ON appointments (service_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_doctor_schedules_doctor_id ON doctor_schedules (doctor_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_doctors_doctor_code ON doctors (doctor_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_doctors_specialty_id ON doctors (specialty_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_doctors_user_id ON doctors (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_patients_national_id_hmac ON patients (national_id_hmac) WHERE national_id_hmac IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_patients_patient_code ON patients (patient_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_patients_phone_number ON patients (phone_number);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_patients_user_id ON patients (user_id) WHERE user_id IS NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_refresh_tokens_token_hash ON refresh_tokens (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_refresh_tokens_user_id ON refresh_tokens (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_refunds_appointment_id ON refunds (appointment_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_reschedule_requests_appointment_id ON reschedule_requests (appointment_id) WHERE status = 'PendingReview';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_roles_code ON roles (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_services_code ON services (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_services_specialty_id ON services (specialty_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_specialties_code ON specialties (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_specialties_slug ON specialties (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE INDEX ix_user_roles_role_id ON user_roles (role_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE UNIQUE INDEX ix_users_normalized_email ON users (normalized_email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    CREATE EXTENSION IF NOT EXISTS btree_gist;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN

                    ALTER TABLE appointments
                    ADD CONSTRAINT ex_appointments_doctor_no_overlap
                    EXCLUDE USING gist (
                        doctor_id WITH =,
                        tstzrange(start_at, end_at, '[)') WITH &&
                    )
                    WHERE (status <> 'Cancelled');
                
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114062800_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261114062800_InitialCreate', '8.0.10');
    END IF;
END $EF$;
COMMIT;



-- Migration: 20261114083500_AddIdempotencyRecords
START TRANSACTION;
CREATE TABLE IF NOT EXISTS idempotency_records (
    id uuid NOT NULL,
    scope character varying(100) NOT NULL,
    key_hash bytea NOT NULL,
    request_hash bytea NOT NULL,
    resource_type character varying(50) NULL,
    resource_id uuid NULL,
    response_status_code smallint NULL,
    response_body_json text NULL,
    expires_at timestamp with time zone NOT NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    CONSTRAINT pk_idempotency_records PRIMARY KEY (id)
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_idempotency_records_scope_key_hash 
ON idempotency_records (scope, key_hash);

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114083500_AddIdempotencyRecords') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261114083500_AddIdempotencyRecords', '8.0.10');
    END IF;
END $EF$;
COMMIT;


-- Migration: 20261114084500_AddOtpVerificationGrants
START TRANSACTION;
CREATE TABLE IF NOT EXISTS otp_verification_grants (
    id uuid NOT NULL,
    challenge_id uuid NOT NULL,
    appointment_id uuid NOT NULL,
    purpose character varying(20) NOT NULL,
    grant_token_hash bytea NOT NULL,
    expires_at timestamp with time zone NOT NULL,
    consumed_at timestamp with time zone NULL,
    created_at timestamp with time zone NOT NULL,
    updated_at timestamp with time zone NULL,
    CONSTRAINT pk_otp_verification_grants PRIMARY KEY (id),
    CONSTRAINT fk_otp_verification_grants_otp_challenges_challenge_id FOREIGN KEY (challenge_id) REFERENCES otp_challenges (id) ON DELETE CASCADE,
    CONSTRAINT fk_otp_verification_grants_appointments_appointment_id FOREIGN KEY (appointment_id) REFERENCES appointments (id) ON DELETE CASCADE
);

CREATE UNIQUE INDEX IF NOT EXISTS ix_otp_verification_grants_challenge_id 
ON otp_verification_grants (challenge_id);

CREATE UNIQUE INDEX IF NOT EXISTS ix_otp_verification_grants_grant_token_hash 
ON otp_verification_grants (grant_token_hash);

CREATE INDEX IF NOT EXISTS ix_otp_verification_grants_id_appointment_id 
ON otp_verification_grants (id, appointment_id);

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "migration_id" = '20261114084500_AddOtpVerificationGrants') THEN
    INSERT INTO "__EFMigrationsHistory" (migration_id, product_version)
    VALUES ('20261114084500_AddOtpVerificationGrants', '8.0.10');
    END IF;
END $EF$;
COMMIT;
