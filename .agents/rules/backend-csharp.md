---
trigger: glob
globs: "**/*.cs, **/*.csproj, **/*.sln"
description: C# and ASP.NET Core backend architecture, validation, security and maintainability rules.
---
# Backend rules
- Controller mỏng; business logic ở Application/Service/Domain.
- Dùng DTO; không trả EF Entity trực tiếp.
- Dùng DI, async I/O và server validation.
- Authorization kiểm tra role và resource ownership.
- Không log PII, OTP, token, password hoặc Consultation content.
- Operation quan trọng có transaction và concurrency handling.
- Dùng DateTimeOffset/UTC; không tính refund bằng client clock.
- ProblemDetails nhất quán; không lộ stack trace.
