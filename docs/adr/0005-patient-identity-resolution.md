# ADR-0005: Patient identity resolution

- Status: Accepted baseline
- Decision: FullName + DateOfBirth hoặc BirthYear là required. Định danh bổ sung: CCCD/national ID, passport, personal verified phone, BHYT, legacy PID.
- Consequences: Exact strong verified identifier là tín hiệu mạnh; name + birth date/year không auto-merge; phone shared/conflict cần manual review; sensitive identifiers encrypted + HMAC-indexed.
