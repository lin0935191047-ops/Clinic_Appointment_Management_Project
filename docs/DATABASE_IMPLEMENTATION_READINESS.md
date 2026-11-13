# DATABASE IMPLEMENTATION READINESS

## Current state
- Logical Database Design v1.2: drafted.
- ERD v1.1: drafted in Mermaid and PlantUML source, with domain PNG previews.
- Data Dictionary: drafted.
- Constraint SQL patterns: reference only.
- Cross-table composite FK rules: proposed and documented; migration implementation still requires review.
- Physical schema: not implemented.
- PostgreSQL database: not created by this documentation step.
- EF Core entities/DbContext/migrations: not generated.

## Required approval before coding
1. Review `DATABASE_DESIGN.md` and `DATA_DICTIONARY.md`.
2. Render/review all ERD diagrams.
3. Decide the DB-TBD items affecting physical schema.
4. Review encryption, HMAC key rotation and identifier verification process.
5. Review PostgreSQL extension availability and permissions for `btree_gist`.
6. Approve exclusion constraint strategy and reschedule race behavior.
7. Approve migration sequence and seed-data plan.

## Next technical task after approval
Implement FEAT-008/009 only after environment foundation is ready: configure Npgsql + EF Core, create initial entities/DbContext and seed roles + cancellation policy v1. Do not combine all application features into the initial migration.
