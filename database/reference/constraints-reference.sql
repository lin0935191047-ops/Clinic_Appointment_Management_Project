-- REFERENCE ONLY — NOT A COMPLETE SCHEMA AND NOT READY TO RUN.
-- Review table/column names, PostgreSQL privileges and EF Core migrations first.

-- 1. Patient must have full date of birth OR birth year.
ALTER TABLE patients
ADD CONSTRAINT ck_patients_dob_or_birth_year
CHECK (
    date_of_birth IS NOT NULL
    OR birth_year IS NOT NULL
);

-- If both values exist, they must describe the same year.
ALTER TABLE patients
ADD CONSTRAINT ck_patients_birth_year_matches_dob
CHECK (
    date_of_birth IS NULL
    OR birth_year IS NULL
    OR birth_year = EXTRACT(YEAR FROM date_of_birth)::smallint
);

-- 2. A patient can have at most one linked account.
CREATE UNIQUE INDEX ux_patients_user_id_not_null
ON patients(user_id)
WHERE user_id IS NOT NULL;


-- 2b. Ensure the booking contact belongs to the same Patient as the appointment.
ALTER TABLE patient_contacts
ADD CONSTRAINT uq_patient_contacts_id_patient UNIQUE (id, patient_id);

ALTER TABLE appointments
ADD CONSTRAINT fk_appointments_booking_contact_patient
FOREIGN KEY (booking_contact_id, patient_id)
REFERENCES patient_contacts(id, patient_id);


-- 2c. Keep appointment-child relationships scoped to the same Patient/Doctor.
ALTER TABLE appointments
ADD CONSTRAINT uq_appointments_id_patient UNIQUE (id, patient_id);

ALTER TABLE appointments
ADD CONSTRAINT uq_appointments_id_doctor UNIQUE (id, doctor_id);

ALTER TABLE appointments
ADD CONSTRAINT fk_appointments_assigned_doctor_service
FOREIGN KEY (doctor_id, service_id)
REFERENCES doctor_services(doctor_id, service_id);

ALTER TABLE appointment_histories
ADD CONSTRAINT fk_appointment_histories_patient_actor
FOREIGN KEY (appointment_id, actor_patient_id)
REFERENCES appointments(id, patient_id);

ALTER TABLE reschedule_requests
ADD CONSTRAINT fk_reschedule_requests_patient_booking
FOREIGN KEY (appointment_id, requested_by_patient_id)
REFERENCES appointments(id, patient_id);

ALTER TABLE appointment_cancellations
ADD CONSTRAINT fk_appointment_cancellations_patient_actor
FOREIGN KEY (appointment_id, requested_by_patient_id)
REFERENCES appointments(id, patient_id);

ALTER TABLE consultations
ADD CONSTRAINT fk_consultations_assigned_doctor
FOREIGN KEY (appointment_id, doctor_id)
REFERENCES appointments(id, doctor_id);

-- 2d. OTP challenge/grant references must not cross Patient, Booking or Purpose.
ALTER TABLE otp_challenges
ADD CONSTRAINT uq_otp_challenges_scope UNIQUE (id, appointment_id, purpose);

ALTER TABLE otp_challenges
ADD CONSTRAINT fk_otp_challenges_patient_booking
FOREIGN KEY (appointment_id, patient_id)
REFERENCES appointments(id, patient_id);

ALTER TABLE otp_challenges
ADD CONSTRAINT fk_otp_challenges_patient_contact
FOREIGN KEY (patient_contact_id, patient_id)
REFERENCES patient_contacts(id, patient_id);

ALTER TABLE otp_verification_grants
ADD CONSTRAINT fk_otp_grants_matching_challenge_scope
FOREIGN KEY (challenge_id, appointment_id, purpose)
REFERENCES otp_challenges(id, appointment_id, purpose);

ALTER TABLE otp_verification_grants
ADD CONSTRAINT uq_otp_grants_id_appointment UNIQUE (id, appointment_id);

ALTER TABLE reschedule_requests
ADD CONSTRAINT fk_reschedule_otp_grant_booking_scope
FOREIGN KEY (otp_grant_id, appointment_id)
REFERENCES otp_verification_grants(id, appointment_id);

ALTER TABLE appointment_cancellations
ADD CONSTRAINT fk_cancellation_otp_grant_booking_scope
FOREIGN KEY (otp_grant_id, appointment_id)
REFERENCES otp_verification_grants(id, appointment_id);
-- The Application layer must still enforce grant purpose = RESCHEDULE/CANCEL respectively.

-- 2e. Prevent reuse of one OTP grant for multiple reschedule/cancellation records.
CREATE UNIQUE INDEX ux_reschedule_requests_otp_grant
ON reschedule_requests(otp_grant_id) WHERE otp_grant_id IS NOT NULL;

CREATE UNIQUE INDEX ux_appointment_cancellations_otp_grant
ON appointment_cancellations(otp_grant_id) WHERE otp_grant_id IS NOT NULL;

-- 3. Only verified personal-owner phone numbers are globally unique.
-- Shared/guardian phone contacts are allowed when is_personal_owner_verified = false.
ALTER TABLE patient_contacts
ADD CONSTRAINT ck_patient_contacts_personal_owner_verified
CHECK (
    is_personal_owner_verified = false
    OR (contact_type = 'PHONE' AND is_verified = true)
);

CREATE UNIQUE INDEX ux_patient_contacts_verified_personal_phone
ON patient_contacts(value_hmac)
WHERE contact_type = 'PHONE'
  AND is_verified = true
  AND is_personal_owner_verified = true
  AND is_active = true;

-- 4. Verified legal/health identifiers are unique within their issuer.
CREATE UNIQUE INDEX ux_patient_identifiers_verified
ON patient_identifiers(identifier_type, issuer_code, normalized_value_hmac)
WHERE verification_status = 'VERIFIED'
  AND is_active = true;

-- 5. Only one primary phone or email contact per Patient and contact type.
CREATE UNIQUE INDEX ux_patient_contacts_one_primary_per_type
ON patient_contacts(patient_id, contact_type)
WHERE is_primary = true AND is_active = true;

-- 6. Only one pending reschedule request per appointment.
CREATE UNIQUE INDEX ux_reschedule_one_pending_per_appointment
ON reschedule_requests(appointment_id)
WHERE status = 'PendingReview';


-- 6b. Cancellation policy values must be internally consistent.
ALTER TABLE cancellation_policy_versions
ADD CONSTRAINT ck_cancellation_policy_rates_sum_to_one
CHECK (
    refund_rate_at_or_above BETWEEN 0 AND 1
    AND cancellation_fee_rate_at_or_above BETWEEN 0 AND 1
    AND refund_rate_below BETWEEN 0 AND 1
    AND cancellation_fee_rate_below BETWEEN 0 AND 1
    AND refund_rate_at_or_above + cancellation_fee_rate_at_or_above = 1
    AND refund_rate_below + cancellation_fee_rate_below = 1
);

CREATE UNIQUE INDEX ux_cancellation_policy_one_active
ON cancellation_policy_versions(is_active)
WHERE is_active = true;

ALTER TABLE refunds
ADD CONSTRAINT ck_refunds_amount_positive CHECK (amount > 0);

ALTER TABLE patient_identity_review_cases
ADD CONSTRAINT ck_identity_review_distinct_patients
CHECK (patient_id <> candidate_patient_id);

ALTER TABLE patient_merge_events
ADD CONSTRAINT ck_patient_merge_distinct_patients
CHECK (source_patient_id <> target_patient_id);

-- 7. Doctor overlap protection. Requires btree_gist extension permission.
CREATE EXTENSION IF NOT EXISTS btree_gist;

ALTER TABLE appointments
ADD CONSTRAINT ex_appointments_doctor_no_overlap
EXCLUDE USING gist (
    doctor_id WITH =,
    tstzrange(start_at, end_at, '[)') WITH &&
)
WHERE (status <> 'Cancelled');

-- PostgreSQL SQLSTATE 23P01 = exclusion_violation.
-- Map this exception to an API 409 Conflict response.

-- 8. Additional row checks should be added with reviewed migrations:
-- CHECK (end_at > start_at)
-- CHECK (duration_minutes_snapshot > 0)
-- CHECK (service_fee_snapshot >= 0)
-- CHECK (refund_rate_snapshot BETWEEN 0 AND 1)
-- CHECK (cancellation_fee_rate_snapshot BETWEEN 0 AND 1)
