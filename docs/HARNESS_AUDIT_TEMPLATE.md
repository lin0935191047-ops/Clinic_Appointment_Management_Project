# Harness Audit Report

- Date:
- Reviewer:
- Commit:
- Result: READY / NOT READY

| Check | PASS/FAIL | Evidence |
|---|---|---|
| Root AGENTS.md and invariants | | |
| Skills use `.agents/skills/<name>/SKILL.md` | | |
| Rules have valid trigger frontmatter | | |
| SPEC has confirmed policies | | |
| Patient may exist without User | | |
| OTP purpose/scoping defined | | |
| Reschedule approval + old slot retention defined | | |
| Cancellation 60/59 boundary defined | | |
| Refund integration separated from calculation | | |
| Patient matching avoids unsafe auto-merge | | |
| DB overlap protection/test planned | | |
| API covers Guest and staff workflows | | |
| Feature IDs/dependencies/schema valid | | |
| No Feature falsely completed | | |

## Findings
## Blockers
## Recommendations
## Final decision
READY only when all mandatory checks pass; otherwise NOT READY.
