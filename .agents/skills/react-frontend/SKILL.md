---
name: react-frontend
description: Implement React pages, feature components, Redux Toolkit state, API clients, forms, navigation, accessibility and role-aware user experience.
---
# React Frontend Skill
## Read first
`AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/API_SPECIFICATION.md`, `docs/SPEC.md`, target Feature criteria.
## Workflow
1. Inspect existing React setup and package versions.
2. Organize by features; reusable UI in common components.
3. Use one configured API client with base URL, timeout and normalized errors.
4. Redux Toolkit for shared app/session/server state; local form state stays local when practical.
5. Client validation for UX; render Backend validation consistently.
6. Include loading/empty/success/error states.
7. Route guard supports UX only; API enforces actual permission.
8. Guest booking must work without auth and never redirect to login.
9. Do not store OTP, full identifiers or health notes in localStorage/sessionStorage.
10. Add component/API integration tests.
## Clinic UX
- Form: FullName + DOB or BirthYear + contact phone.
- Explain no account is needed to book.
- Lookup/reschedule/cancel require BookingCode and OTP.
- Reschedule shows PendingReview; old booking remains.
- Cancellation shows expected refund separately from refund processed, with exact 60-minute boundary.
- Never expose another patient's record.
## Accessibility
Labels, keyboard controls, visible focus, semantic headings and responsive behavior.
