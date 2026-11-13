# SECURITY & PRIVACY BASELINE

## Data classification
- Public: specialties, public doctor profiles, service descriptions, clinic contact.
- Internal: schedule rules, queue, operational reports.
- Personal: name, birth date/year, phone, email, address, identifiers.
- Sensitive health: symptoms, consultation notes, diagnosis.
- Secrets: password hashes, signing keys, DB credentials, OTP material, provider tokens.

## Authentication and authorization
- Backend enforces authentication, role and resource ownership.
- Patient chỉ xem hồ sơ/lịch của mình; query/service phải kiểm tra ownership.
- Doctor chỉ xem lịch được phân công và dữ liệu cần cho chăm sóc.
- Receptionist chỉ có operational access; không mặc nhiên xem/chỉnh Consultation.
- Admin theo least privilege; audit truy cập dữ liệu nhạy cảm.
- Guest chỉ vào public endpoints hoặc dùng OTP-derived scoped grant.

## OTP
- Dùng cryptographically secure random generator.
- Lưu keyed hash, không lưu plaintext.
- Bind challenge với phone, booking, purpose, expiry.
- One-time; TTL ngắn; max attempts; resend cooldown; per-IP/per-phone throttle.
- Generic responses để tránh booking enumeration.
- OTP/grant không được log.
- Grant chỉ được phép thao tác theo purpose đã verify.

## Patient data
- Encrypt identity documents/identifiers at rest; key ngoài DB/repo.
- Mask identifier khi hiển thị/log.
- Dùng HMAC normalized identifier để lookup; có kế hoạch key rotation.
- Không auto-merge chỉ dựa name + birth date/year.
- Audit access, edits, merges.
- Không đưa diagnosis/symptoms/identifier vào SMS/email.

## API security
- HTTPS production; DTO allowlist; server validation.
- Dùng EF Core/parameterized SQL; tránh raw SQL nối chuỗi input.
- Rate limit public booking, OTP, lookup.
- CORS allowlist; no stack trace/secrets to client.
- Chống IDOR bằng resource-level checks.
- CSRF strategy phụ thuộc cách lưu token/cookie.

## Secrets and operations
- .NET User Secrets cho local hoặc environment/secret manager.
- Không commit `.env`, appsettings secret, password.
- Rotate keys nếu compromise; redact logs.
- Backup mã hóa và có restore drill.

## Security tests
- Booking code alone cannot lookup.
- OTP purpose mismatch/expired/reused/over-attempt fails.
- Patient A cannot access Patient B by changing UUID.
- Doctor cannot access unassigned appointment.
- Receptionist cannot edit consultation without permission.
- Public DTO cannot set status/refund/role/PatientId.
- Concurrent booking cannot double-book.
