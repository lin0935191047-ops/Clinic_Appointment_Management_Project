# ADR-0003: Reschedule requires receptionist approval

- Status: Accepted
- Decision: RescheduleRequest PendingReview; Receptionist approve/reject; old appointment/slot được giữ đến khi approve thành công.
- Consequences: Approve phải recheck slot mới trong transaction; slot đề xuất không reserved khi pending.
