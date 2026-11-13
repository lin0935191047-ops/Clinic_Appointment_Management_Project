---
name: efcore-postgresql
description: Design PostgreSQL schema and implement EF Core mapping, migrations, indexes, transactions, constraints, encrypted identifiers and concurrency safeguards.
---
# EF Core + PostgreSQL Skill
## Read first
`docs/DATABASE_DESIGN.md`, `docs/DATA_DICTIONARY.md`, `docs/ERD.md`, `docs/DATABASE_REVIEW_CHECKLIST.md`, `docs/DATABASE_IMPLEMENTATION_READINESS.md`, `docs/ARCHITECTURE.md`, `docs/BUSINESS_RULES.md`, relevant ADR and Feature.

Before creating physical schema or migration, verify that the owner approved the logical design and all schema-blocking TBDs. If approval is absent, do documentation/review only; do not generate migrations.
## Workflow
1. Confirm PostgreSQL/.NET/EF Core/Npgsql compatibility.
2. Model PK/FK/nullability/precision/constraints/indexes explicitly.
3. Keep DTO separate from persistence entities.
4. Use Fluent API for complex constraints/indexes/precision.
5. Generate migration and inspect generated code/SQL before applying.
6. Test migration against clean DB and upgrade path where available.
7. Use explicit transactions for booking/reschedule/cancellation.
8. Implement DB-level no-overlap constraint (e.g. GiST exclusion + tstzrange) and integration tests.
9. Translate unique/exclusion violations to domain conflict/HTTP 409.
10. Keep secrets/encryption keys outside DB and repo.
## Clinic constraints
- Patient.UserId nullable; DOB OR BirthYear required.
- Identifiers encrypted; keyed HMAC for exact matching.
- Do not globally unique phone if shared-family numbers are allowed; distinguish verified personal phone.
- Appointment stores service/duration/price/currency snapshots.
- Store cancellation time, minutes-before, policy version/rates/refund expectation.
- Reschedule proposed slot is not reserved before approval; recheck on approve.
- timestamptz UTC; DateTimeOffset at API/domain boundary.
## Safety
Do not run destructive migration/reset without explicit approval. EF InMemory alone cannot prove PostgreSQL constraints.
