# ADR-0008: Separate Patient contacts from sensitive identity identifiers

- **Status:** Proposed — pending review
- **Context:** A Patient may have no User account. Contact phone/email and official identifiers (national ID, passport, health insurance, legacy PID) serve different purposes and have different verification/security requirements.
- **Decision:** Store contact channels in `patient_contacts`; store official identifiers in `patient_identifiers`. Keep encrypted values and keyed HMAC lookup values. Mark a phone as a unique personal identifier only when personal ownership is verified.
- **Matching:** Strong verified identifiers and verified personal phones produce high-confidence candidates. Full name + DOB/year produces a candidate only. Never auto-merge based solely on demographic match.
- **Consequences:** Application needs encryption/HMAC key management and an authorized staff review workflow. Keys must not be stored in PostgreSQL or Git.
