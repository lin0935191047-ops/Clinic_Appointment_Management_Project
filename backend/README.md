# Backend (ASP.NET Core Web API)

Thư mục này được dành riêng cho mã nguồn Backend xây dựng trên **ASP.NET Core 8.0+ Web API** theo mô hình **Clean Architecture**:

- `src/ClinicAppointment.Domain/`: Domain Entities, Enums, Value Objects, Domain Exceptions.
- `src/ClinicAppointment.Application/`: Commands, Queries, MediatR Handlers, DTOs, FluentValidation.
- `src/ClinicAppointment.Infrastructure/`: EF Core DbContext, PostgreSQL Configurations, External Services (Email, SMS).
- `src/ClinicAppointment.Api/`: REST Controllers, Middlewares, Dependency Injection Setup.
- `tests/`: Unit Tests và Integration Tests với Testcontainers PostgreSQL.

> Sẽ được khởi tạo chi tiết trong **Phase 1** theo kế hoạch tại [docs/DEVELOPMENT_PLAN.md](../docs/DEVELOPMENT_PLAN.md).

