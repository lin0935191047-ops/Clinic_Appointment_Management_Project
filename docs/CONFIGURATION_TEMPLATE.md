# Configuration template — không commit secrets

Tên biến cấu hình đề xuất:
- `ConnectionStrings__ClinicDatabase`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__SigningKey`
- `Otp__TtlSeconds`
- `Otp__MaxAttempts`
- `Otp__ResendCooldownSeconds`
- `Otp__HmacKey`
- `PatientIdentifiers__EncryptionKeyReference`
- `Sms__Provider`
- `Sms__ApiKey`
- `Email__Provider`
- `Payment__Provider`

Dùng .NET User Secrets cho local hoặc environment/secret manager; không commit giá trị thực.
