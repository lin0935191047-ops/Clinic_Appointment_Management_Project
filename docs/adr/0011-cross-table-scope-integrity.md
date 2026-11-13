# ADR-0011: Enforce cross-table booking scope integrity

- **Status:** Proposed — pending database review.
- **Context:** Foreign keys on individual IDs do not prove that a Patient contact belongs to the Patient on the Appointment, that an OTP challenge targets the same booking/contact, or that a Consultation Doctor is assigned to the Appointment.
- **Decision:** Add composite candidate keys and composite foreign keys for Patient/Appointment, Doctor/Appointment, Doctor/Service, OTP challenge/booking/contact/purpose, and Patient actor/Appointment relationships where they materially protect business invariants.
- **Consequences:** EF Core model configuration and migrations are more explicit; PostgreSQL integration tests must cover invalid cross-link attempts. Application authorization is still required; composite FKs do not replace authorization.
- **Scope:** These are proposed physical constraints, not an already-created schema.
