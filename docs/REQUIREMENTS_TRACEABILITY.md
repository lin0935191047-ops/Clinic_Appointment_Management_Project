# REQUIREMENTS TRACEABILITY MATRIX

| Requirement group | FR range | Feature IDs | Primary tests |
|---|---|---|---|
| Public catalog | FR-001–005 | FEAT-016–021 | Public catalog/API tests |
| Authentication | FR-006–010 | FEAT-010, 014, 015 | Auth/role/refresh tests |
| Patient profile/identity | FR-011–017 | FEAT-011–013, 035 | Identity match/merge security tests |
| Guest Booking | FR-018–025 | FEAT-021–024, 045 | Guest no-token, validation, concurrency tests |
| OTP and lookup | FR-026–030 | FEAT-025–027 | OTP purpose/expiry/reuse/rate-limit tests |
| Appointment lifecycle | FR-031–035 | FEAT-028, 036–040 | State transition tests |
| Reschedule | FR-036–042 | FEAT-029–030, 046 | Approval/reject/slot-conflict tests |
| Cancellation/refund | FR-043–051 | FEAT-031–034 | 61/60/59-minute tests, refund status tests |
| Schedule management | FR-052–055 | FEAT-019–022, 048 | Working-hour/time-off tests |
| Reception/consultation | FR-056–062 | FEAT-035–040, 046–047 | Check-in/doctor assignment/consultation tests |
| Notification/reporting | FR-063–067 | FEAT-041–044 | Outbox, delivery, dashboard tests |

Feature ID mappings may be expanded as features are split. Do not remove a requirement mapping without reviewing its downstream acceptance criteria.
