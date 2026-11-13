# Project diagrams

## ERD images
- `ERD_01_IDENTITY.png` — Users, Roles, Patients and identifiers.
- `ERD_02_CATALOG_SCHEDULING.png` — Doctors, specialties, services and schedules.
- `ERD_03_APPOINTMENTS_FINANCE.png` — Appointments, reschedule, cancellation, payments, refunds and consultation.
- `ERD_04_OTP_OPERATIONS.png` / `.svg` — OTP challenges/grants (same Patient + booking scope), notifications, audit and idempotency.
- `ERD_OVERVIEW.png` / `.svg` — high-level system relationship overview.

## Editable source
- `mermaid/*.mmd` — Mermaid ERD source by domain.
- `plantuml/*.puml` — PlantUML use case, activity and ERD source.

ERD images are visual aids. `docs/DATABASE_DESIGN.md` and `docs/DATA_DICTIONARY.md` remain the detailed schema source of truth.
