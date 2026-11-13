---
name: testing-qa
description: Create and execute unit, integration, API, security, concurrency and end-to-end tests for Clinic Appointment Management; required before completing Features.
---
# Testing & QA Skill
## Read first
`docs/TESTING_STRATEGY.md`, `docs/SPEC.md`, `docs/BUSINESS_RULES.md` and target Feature acceptance criteria.
## Workflow
1. Map every criterion to one or more tests.
2. Cover happy, validation, forbidden, invalid-state, failure and concurrency paths.
3. Unit-test domain calculations with controlled clock.
4. Integration-test migrations/constraints/transactions on PostgreSQL, not only EF InMemory.
5. API-test status/response/OTP grant/data leakage.
6. Frontend-test loading/error/empty/validation and Guest journeys.
7. Record actual command, test name, environment and result.
8. Report flaky/blocked tests; do not hide failures.
## Mandatory boundary tests
- Cancellation at 61, 60, 59 minutes and at/after appointment start.
- Two simultaneous bookings same doctor/overlap slot.
- Proposed reschedule slot becomes occupied before receptionist approval.
- OTP wrong/expired/reused/purpose mismatch/rate limited.
- Patient A attempts Patient B; Doctor attempts unassigned appointment.
- Identity matching strong identifier vs name+birthday candidate only.
## Definition of Done
Acceptance criteria pass; evidence reproducible; no critical/high defect; docs synced; Feature status updated only after review.
