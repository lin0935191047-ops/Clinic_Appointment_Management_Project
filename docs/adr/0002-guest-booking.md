# ADR-0002: Guest booking without account

- Status: Accepted
- Decision: Guest tạo Appointment không login; Patient record độc lập User.
- Consequences: Public API rate-limited; OTP grant cho lookup/change/cancel; Patient.UserId nullable; cần identity resolution an toàn.
