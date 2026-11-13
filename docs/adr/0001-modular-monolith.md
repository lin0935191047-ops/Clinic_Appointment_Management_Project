# ADR-0001: Modular monolith for version 1

- Status: Accepted baseline
- Decision: ASP.NET Core modular monolith với layer/feature boundaries; không chia microservices ở v1.
- Context: Dự án do một developer phát triển; ưu tiên dễ học, debug và deploy.
- Consequences: Giảm độ phức tạp vận hành; module boundary cần rõ để có thể tách sau này.
