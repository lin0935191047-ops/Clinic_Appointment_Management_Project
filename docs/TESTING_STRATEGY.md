# TESTING STRATEGY

## 1. Test levels
- Unit: refund calculation, identity matching, appointment state transitions, validators.
- Integration: ASP.NET Core + EF Core + PostgreSQL migration/transaction/constraint.
- API contract: Swagger/OpenAPI, Postman.
- Frontend component: form, validation, loading/error, navigation.
- E2E: critical user journeys.
- Security: OTP, role/resource access, IDOR, rate limit, redaction.
- Concurrency: simultaneous booking/reschedule.

## 2. Mandatory test matrix

### Guest booking
1. Guest booking succeeds without JWT.
2. Missing FullName rejected.
3. Missing both DateOfBirth and BirthYear rejected.
4. Missing/invalid phone rejected.
5. Inactive doctor/service rejected.
6. Past or outside schedule slot rejected.
7. Two simultaneous overlapping requests: only one succeeds.
8. Same idempotency key retry does not duplicate booking.

### Patient identity
1. Exact verified national ID/passport/BHYT/PID match gives strong candidate.
2. Name + DOB only flags candidate, no auto-merge.
3. Same household phone with different patient profiles does not auto-merge.
4. Conflicting strong identifiers require manual review.
5. BirthYear alone with FullName is accepted.
6. Missing both DOB and BirthYear is rejected.

### OTP
1. Correct OTP/purpose/booking/phone grants access.
2. Wrong, expired, reused OTP fails.
3. Purpose mismatch fails.
4. Generic response avoids booking enumeration.
5. Rate limit returns 429.
6. Grant cannot be reused for another booking/purpose.

### Reschedule
1. Valid RESCHEDULE grant creates PendingReview.
2. Old appointment remains on old slot while pending.
3. Receptionist reject leaves old appointment unchanged.
4. Receptionist approve with free slot updates transactionally.
5. Proposed slot occupied before approval: approval fails and old appointment unchanged.
6. Only one pending request per appointment.
7. Non-Receptionist cannot approve/reject.

### Cancellation/refund boundaries
Dùng controllable server clock/fixed test time:
- 61 phút: 100% refund, 0% cancellation fee.
- 60 phút: 100% refund, 0% cancellation fee.
- 59 phút: 50% refund, 50% cancellation fee.
- 1 giây trước: 50% refund, 50% cancellation fee.
- At/after StartAt: patient self-cancel denied.
- Unpaid service: refund amount = 0; không giả lập payment/refund.
- Full payment: refund đúng 100% hoặc 50% snapshot fee.
- Provider/manual refund Pending không được thể hiện là Succeeded.

### Consultation/security
- Receptionist check-in valid appointment.
- Assigned Doctor only starts/completes appointment.
- Patient cannot access another Patient's Consultation.
- PII/OTP/password not present in logs.

## 3. Evidence
Feature completed phải cite test file + test name + command/output summary hoặc manual test record có thể tái hiện. Không bịa pass evidence.

## 4. Suggested tooling
Backend: xUnit + ASP.NET Core integration testing + PostgreSQL real/test container. Frontend: Vitest + React Testing Library; E2E runner TBD. API: Swagger/Postman.

## 5. Exit criteria
- Critical tests pass; no unresolved critical/high security defect.
- Migration tested from clean DB and upgrade path.
- Concurrency proves no double booking.
- Refund 61/60/59-minute tests pass.
- Evidence linked to FEAT IDs.
