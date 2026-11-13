---
name: efcore-postgresql
description: Hướng dẫn quản trị Entity Framework Core với PostgreSQL, cấu hình Fluent API, đánh chỉ mục tối ưu, quản lý Migrations và xử lý concurrency.
---

# EF Core & PostgreSQL Skill

Kỹ năng này hướng dẫn AI Agent cách thao tác an toàn với CSDL PostgreSQL thông qua Entity Framework Core trong dự án phòng khám.

---

## 1. Thiết lập Cấu hình Thực thể (Fluent API Best Practices)

- Không đặt attribute `[Table]`, `[Column]`, `[Key]` trực tiếp trong Domain Entities.
- Sử dụng class cấu hình riêng biệt kế thừa `IEntityTypeConfiguration<T>`:

```csharp
public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("appointments");
        
        builder.HasKey(a => a.Id);

        builder.Property(a => a.AppointmentCode)
            .HasMaxLength(30)
            .IsRequired();

        builder.HasIndex(a => a.AppointmentCode)
            .IsUnique();

        // Unique Partial Index: Chống Double-booking
        builder.HasIndex(a => new { a.DoctorId, a.AppointmentDate, a.StartTime })
            .HasFilter("status NOT IN ('CANCELLED', 'NO_SHOW')")
            .IsUnique();

        builder.HasOne(a => a.Doctor)
            .WithMany()
            .HasForeignKey(a => a.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Patient)
            .WithMany()
            .HasForeignKey(a => a.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

---

## 2. Quy trình Quản lý Migrations

Mỗi khi thay đổi Domain Entity hoặc bổ sung Index:
1. Mở terminal tại thư mục gốc backend:
   ```bash
   dotnet ef migrations add <DescriptiveMigrationName> --project src/ClinicAppointment.Infrastructure --startup-project src/ClinicAppointment.Api
   ```
2. Mở file migration vừa sinh ra trong `src/ClinicAppointment.Infrastructure/Migrations/` để kiểm tra:
   - Các cột `Up()` và `Down()` có khớp với thiết kế trong `docs/DATABASE_DESIGN.md` không?
   - Có vô tình xóa hoặc rename cột dữ liệu quan trọng không?
3. Cập nhật cơ sở dữ liệu:
   ```bash
   dotnet ef database update --project src/ClinicAppointment.Infrastructure --startup-project src/ClinicAppointment.Api
   ```
4. Cập nhật nhật ký kỹ thuật vào `planning/implementation_log.md`.

---

## 3. Chiến lược Khóa & Giao dịch (Concurrency & Transactions)

Khi thực hiện đặt lịch (Book Appointment):
1. Mở một Database Transaction:
   ```csharp
   await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
   ```
2. Kiểm tra lại slot trống trong Transaction.
3. Thêm bản ghi `Appointment` và gọi `SaveChangesAsync()`.
4. Bắt lỗi `DbUpdateException`:
   - Nếu lỗi do vi phạm Unique Index `idx_unique_doctor_active_slot` -> Bắn ra ngoại lệ nghiệp vụ `SlotAlreadyBookedException`.
5. Commit Transaction:
   ```csharp
   await transaction.CommitAsync(cancellationToken);
   ```

