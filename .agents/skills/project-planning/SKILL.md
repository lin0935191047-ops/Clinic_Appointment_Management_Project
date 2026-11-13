---
name: project-planning
description: Analyze SRS, break down requirements into dependency-aware features, manage milestones and keep planning artifacts consistent. Use for scope changes, feature planning, progress audit and selecting next work.
---
# Project Planning Skill
## Read first
`AGENTS.md`, `docs/SPEC.md`, `docs/BUSINESS_RULES.md`, `docs/ARCHITECTURE.md`, `planning/feature_list.json`, `planning/backlog.md`.
## Workflow
1. Restate the outcome and map it to FR/BR/UC/FEAT IDs.
2. Identify TBDs/dependencies; do not silently decide them.
3. Decompose into small, independently testable Features with stable IDs, module, description, priority, dependencies and acceptance criteria.
4. Validate dependency graph (all IDs exist; no cycles).
5. Select only Features whose dependencies are completed or explicitly waived.
6. Update `feature_list.json`, `progress.md`, `backlog.md` consistently.
7. Validate JSON and report changes.
## Acceptance checklist
- No duplicate IDs or missing dependencies.
- No dependency cycles.
- Every Feature has observable criteria.
- New Features start `not_started`.
- Completed requires real test evidence.
- Guest Booking, OTP, reschedule approval, refund boundary and Patient matching remain intact.
## Do not
Do not mark completed because files were created; do not change scope to simplify implementation; do not override user decisions.
