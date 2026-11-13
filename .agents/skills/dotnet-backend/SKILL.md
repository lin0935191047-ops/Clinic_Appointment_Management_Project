---
name: dotnet-backend
description: Build and review C# ASP.NET Core Web API features with DTOs, dependency injection, application services, authorization, validation, logging and HTTP conventions.
---
# ASP.NET Core Backend Skill
## Read first
`AGENTS.md`, `docs/ARCHITECTURE.md`, `docs/API_SPECIFICATION.md`, `docs/BUSINESS_RULES.md`, `docs/SECURITY.md`, target Feature.
## Workflow
1. Confirm installed .NET SDK and compatible package versions; record them.
2. Inspect existing solution/code before creating files.
3. Create request/response DTOs and server-side validation.
4. Keep Controllers thin; delegate to Application use cases/services.
5. Place invariants in Domain/Application, not React.
6. Use DI, async I/O, cancellation tokens where suitable and structured logging.
7. Use consistent ProblemDetails error handling.
8. Enforce role and resource ownership at server.
9. Define transaction/idempotency/concurrency for data-changing operations.
10. Add unit/integration tests and API examples.
## Clinic constraints
- Public booking works without JWT.
- Never trust client PatientId/UserId/Role/Status/RefundRate/RefundAmount.
- Guest OTP grants are scoped and short-lived, not User JWT.
- Receptionist-only reschedule approval; old appointment unchanged until transaction succeeds.
- Server time policy: >=60 min = 100% refund; <60 and >0 = 50%; at/after start denied for patient self-cancel.
- Map PostgreSQL overlap conflict to HTTP 409.
- Prevent IDOR for Patient and Doctor resources.
## Reporting
Report exact commands/tests run; never claim build/test passed if not executed.
