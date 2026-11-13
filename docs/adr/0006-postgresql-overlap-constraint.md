# ADR-0006: PostgreSQL exclusion constraint for doctor appointment overlap

- **Status:** Proposed — pending review and migration test
- **Context:** Two concurrent Guest booking requests may both pass application availability checks before either transaction commits. Receptionist approval of a reschedule can also race with a new booking for the proposed slot.
- **Decision:** Keep `appointments` as the authoritative table for held slots and add a GiST exclusion constraint on `doctor_id` and `tstzrange(start_at,end_at,'[)')`, applying to rows whose status is not `Cancelled`.
- **Consequences:** Adjacent slots are allowed. Overlapping intervals for one Doctor are rejected by PostgreSQL. Requires `btree_gist`, reviewed migration SQL and PostgreSQL integration tests. Map SQLSTATE `23P01` to HTTP 409.
- **Reschedule:** A pending request does not reserve the proposed slot. On approval, update the existing appointment in a transaction. If a concurrent booking owns the slot first, the update fails and rollback preserves the old appointment.
- **Alternatives considered:** Application-only check (race-prone); a separate reservation table (more moving parts for the current single-clinic model).
