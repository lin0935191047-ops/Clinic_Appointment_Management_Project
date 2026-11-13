# ADR-0009: Keep the old appointment slot until receptionist approval

- **Status:** Proposed — pending review
- **Context:** Patient requests a new time, but business policy requires Receptionist approval.
- **Decision:** Creating a RescheduleRequest does not update the Appointment or reserve the proposed slot. The current Appointment and slot remain unchanged. On approval, backend rechecks and updates the Appointment inside one transaction.
- **Concurrency:** The appointment overlap exclusion constraint is the final arbiter. If a concurrent booking wins the proposed time first, approval rolls back and the old appointment remains intact.
- **Consequence/TBD:** Decide whether a request that conflicts at approval stays PendingReview for staff follow-up or is automatically Rejected with a reason.
