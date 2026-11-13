---
trigger: glob
globs: "**/*Migration*.cs, **/*.sql, **/*DbContext*.cs, **/*Configuration*.cs"
description: PostgreSQL and EF Core schema, migration, privacy and integrity rules.
---
# Database rules
- Migration phải review được, chạy DB sạch và upgrade DB hiện có.
- Không destructive drop/rename nếu chưa có kế hoạch migrate/backup.
- Appointment time dùng timestamptz UTC.
- Chống overlap ở PostgreSQL, không chỉ Application.
- Patient.UserId nullable; DOB hoặc BirthYear bắt buộc.
- Identifier nhạy cảm encrypted; HMAC dùng exact lookup.
- Không lưu OTP plaintext.
- Tiền dùng numeric/decimal, không float/double.
- Test constraints trên PostgreSQL thật, không chỉ EF InMemory.
