# USE CASE SPECIFICATION

## Actors
- Guest: chưa đăng nhập; đặt lịch và dùng OTP truy cập booking.
- Patient: có tài khoản tùy chọn.
- Receptionist: quản lý tiếp nhận và duyệt đổi lịch.
- Doctor: thực hiện khám.
- Admin: quản trị.

## Use case inventory
| ID | Use Case | Actor | Auth |
|---|---|---|---|
| UC-01 | Browse specialties/doctors/services | Guest, Patient | Public |
| UC-02 | Check availability | Guest, Patient | Public |
| UC-03 | Book appointment | Guest, Patient | Guest không cần login |
| UC-04 | Request OTP | Guest | Public + rate-limit |
| UC-05 | Verify OTP | Guest | OTP challenge |
| UC-06 | Lookup appointment | Guest, Patient | OTP grant/session |
| UC-07 | Request reschedule | Guest, Patient | OTP grant/session |
| UC-08 | Review reschedule request | Receptionist | Role |
| UC-09 | Cancel appointment | Guest, Patient | OTP grant/session |
| UC-10 | Manage patient profile | Patient, Receptionist | Role/ownership |
| UC-11 | Resolve possible duplicate Patient | Receptionist/Admin | Role + audit |
| UC-12 | Manage doctors | Admin | Admin |
| UC-13 | Manage specialties/services | Admin | Admin |
| UC-14 | Manage doctor schedules/time off | Admin/authorized Receptionist | Role |
| UC-15 | Confirm appointment | Receptionist | Receptionist |
| UC-16 | Check-in patient | Receptionist | Receptionist |
| UC-17 | Manage queue | Receptionist/Doctor | Role |
| UC-18 | Start consultation | Assigned Doctor | Resource check |
| UC-19 | Record consultation | Assigned Doctor | Resource check |
| UC-20 | Complete consultation | Assigned Doctor | Resource check |
| UC-21 | Manage accounts/roles | Admin | Admin |
| UC-22 | View reports | Admin | Admin |

## UC-03 — Guest Book Appointment
**Primary actor:** Guest. **Precondition:** Public catalog/availability service available; doctor/service active. Login is not required.

Main flow:
1. Guest chooses specialty, doctor and service.
2. Select date and available slot.
3. Enter FullName, DateOfBirth or BirthYear, contact phone, optional email/reason/identifiers.
4. System validates required fields and searches possible Patient candidates.
5. Exact verified strong match is evaluated; uncertain candidates are flagged, not auto-merged.
6. Backend rechecks schedule and overlap in transaction.
7. Create/link Patient and create Appointment without creating User.
8. Return unique BookingCode and booking summary.

Exceptions: missing fields; identity ambiguous; slot conflict (409); inactive doctor/service; outside schedule; DB failure rolls back.

Postcondition: Appointment has PatientId, BookingCode, status PendingConfirmation (unless later-approved configuration says otherwise), and service/fee snapshots.

## UC-06 — Lookup Appointment
1. Guest enters BookingCode and phone.
2. System requests OTP for LOOKUP.
3. Guest submits OTP.
4. Verify challenge, issue short-lived LOOKUP grant.
5. Guest submits lookup request.
6. Return minimal appointment summary, not consultation note/full identifiers.

## UC-07 — Request Reschedule
1. Guest verifies RESCHEDULE OTP.
2. Proposes a new slot.
3. System creates PendingReview; current appointment keeps old slot.
4. Receptionist reviews.
5. Reject → old appointment unchanged.
6. Approve → re-check slot and update atomically.
7. Slot taken → approval fails; old slot unchanged.

## UC-09 — Cancel Appointment
1. Guest verifies CANCEL OTP.
2. Submits cancellation reason.
3. Server checks appointment has not started.
4. Server captures timestamp and computes policy.
5. Records cancellation + refund expectation + Cancelled state in transaction.
6. No Receptionist approval required.
7. Refund remains Pending/Processing until actual provider/manual process completes.

## UC-11 — Resolve possible duplicate Patient
1. Authorized staff sees candidate records.
2. Compare permitted identifiers/evidence.
3. Link/merge/reject candidate.
4. Record actor, time, reason and source/target IDs.
5. Name + birth date alone is not sufficient for automatic merge.
