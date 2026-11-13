# ACTIVITY DIAGRAMS

PlantUML source files are in `diagrams/plantuml/`.

- **AD-01 Guest Booking:** Guest selects specialty/doctor/service/date/slot → enters FullName + DOB or BirthYear + phone → identity candidate matching → Backend rechecks availability in transaction → Patient link/create + Appointment + BookingCode. No login. Invalid data/slot conflict/inactive doctor/DB failure are alternate paths.
- **AD-02 OTP Lookup:** BookingCode + phone → request LOOKUP OTP → SMS adapter → submit OTP → verify purpose/expiry/attempts → issue scoped grant → return minimal appointment summary.
- **AD-03 Reschedule Approval:** RESCHEDULE OTP → propose slot → PendingReview; old slot remains → Receptionist review → reject or approve → approve rechecks slot transactionally → move appointment only if available.
- **AD-04 Cancellation/Refund:** CANCEL OTP → submit cancellation → server timestamp → >=60 min: 100% refund/0% fee; <60 and >0: 50% refund/50% fee → cancellation/refund ledger + Cancelled transaction → provider/manual refund processing. At/after StartAt: self-cancel denied.
- **AD-05 Consultation:** Receptionist check-in → Doctor assigned starts → InProgress → records consultation → completes → Completed + history/audit.
