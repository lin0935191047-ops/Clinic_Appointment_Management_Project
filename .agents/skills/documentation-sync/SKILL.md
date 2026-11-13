---
name: documentation-sync
description: Keep SRS, business rules, architecture, database, API, ADRs, feature tracking and instructions consistent when requirements or implementation change.
---
# Documentation Sync Skill
## Read first
Root `AGENTS.md`, modified source files, related docs and current Feature.
## Workflow
1. Classify change: business/API/schema/architecture/security/test/implementation.
2. Map impact across SPEC, BUSINESS_RULES, ARCHITECTURE, DATABASE_DESIGN, API_SPECIFICATION, SECURITY, TESTING_STRATEGY, ADR and feature_list.
3. Update canonical source first, then dependent docs.
4. Preserve version/date and Accepted/Proposed/TBD status.
5. Add/update ADR for meaningful architecture decisions.
6. Validate JSON, Feature IDs and dependencies.
7. Log only work actually performed.
8. Report updated files and remaining inconsistency.
## Rules
Do not silently change decisions; distinguish implemented/planned/TBD; never fabricate test evidence; use Vietnamese explanations with consistent English identifiers.
