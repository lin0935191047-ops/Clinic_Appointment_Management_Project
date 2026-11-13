---
name: clinic-business-logic
description: Analyze and implement clinic workflows: Guest Booking, Patient identity resolution, OTP, scheduling, reschedule approval, cancellation/refund, reception and consultation.
---
# Clinic Business Logic Skill
## Read first
`docs/SPEC.md`, `docs/BUSINESS_RULES.md`, `docs/USE_CASE_SPECIFICATION.md`, `docs/ACTIVITY_DIAGRAMS.md`, `docs/DATABASE_DESIGN.md`.
## Core invariants
1. Guest Booking requires no account/login.
2. Patient can exist without User.
3. FullName + full DOB or BirthYear required.
4. Additional identifiers: CCCD/national ID, passport, verified personal phone, BHYT, legacy PID.
5. Name + DOB/year alone never auto-merges.
6. Guest lookup/reschedule/cancel requires OTP tied to booking and purpose.
7. Reschedule requires Receptionist approval; old slot remains held until successful approval.
8. Patient can self-cancel before appointment start after OTP/session verification, no Receptionist approval.
9. >=60 minutes: 100% refund, 0% cancellation fee.
10. <60 and >0 minutes: 50% refund, 50% cancellation fee.
11. At/after start: patient self-cancel denied.
12. DB prevents overlap per doctor.
## Workflow
Model use case/state transition → identify actor/authority → identify transaction/race conditions → validate server time/state/slot/payment snapshot → write history/audit/outbox → add boundary/security tests.
## Identity
Exact verified strong identifiers are strongest signal. Name + DOB/year is candidate only. Shared phone/conflict requires review. Merge requires authorized review, reason and audit.
## Refund calculation
Use server clock and immutable fee snapshot. Fully paid amount P: `minutesBefore >= 60` => refund P; `0 < minutesBefore < 60` => refund P × 0.5. Deny self-cancel if StartAt <= now. Never claim provider refund completed until confirmed. Partial/unpaid-fee collection TBD.
