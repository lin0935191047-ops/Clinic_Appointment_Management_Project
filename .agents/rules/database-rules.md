# Database Rules (PostgreSQL & EF Core)

> Quy chuẩn thiết kế cơ sở dữ liệu và thao tác với Entity Framework Core trong dự án **Clinic-Appointment-Management**.

---

## 1. Quy chuẩn Cơ sở Dữ liệu PostgreSQL

- **Quy chuẩn đặt tên (Naming Convention):**
  - Tên bảng: Chữ thường số nhiều, nối gạch dưới (`users`, `doctors`, `doctor_schedules`, `appointments`).
  - Tên cột: Chữ thường, nối gạch dưới (`appointment_date`, `start_time`, `is_guest`).
  - Tên khóa chính: `id` hoặc `pk_<table_name>`.
  - Tên khóa ngoại: `<singular_table>_id` (ví dụ `doctor_id`, `specialty_id`).
  - Tên Index: `idx_<table_name>_<column_names>`.
  - Tên Unique Constraint: `uq_<table_name>_<column_names>`.
- **Kiểu dữ liệu:**
  - Khóa chính định danh: `uuid` (sử dụng hàm `gen_random_uuid()` của PostgreSQL).
  - Chuỗi ký tự có giới hạn độ dài: `varchar(n)` (không dùng `varchar` không giới hạn trừ khi cần thiết).
  - Văn bản dài: `text`.
  - Tiền tệ: `numeric(12, 2)`.
  - Ngày giờ: `timestamptz` (Timestamp with time zone - luôn lưu trữ ở chuẩn UTC).

---

## 2. Quy chuẩn Entity Framework Core

1. **Fluent API Configuration:**
   - Không đặt các thuộc tính ràng buộc (`[Required]`, `[MaxLength]`) trực tiếp trên Domain Entities.
   - 100% cấu hình bảng, khóa, quan hệ và index được viết trong các class riêng biệt kế thừa `IEntityTypeConfiguration<T>` đặt tại `ClinicAppointment.Infrastructure.Persistence.Configurations`.
   - Sử dụng package `EFCore.NamingConventions` để tự động chuyển đổi PascalCase trong C# sang `snake_case` trong PostgreSQL:
     ```csharp
     optionsBuilder.UseNpgsql(connectionString).UseSnakeCaseNamingConvention();
     ```
2. **Quản lý Migrations:**
   - Đặt tên Migration có ý nghĩa mô tả rõ ràng: `AddAppointmentEntity`, `AddDoctorScheduleUniqueIndex` (tránh đặt tên vô nghĩa như `Update1`, `Migration2`).
   - Tuyệt đối không tự ý chỉnh sửa file Migration đã được merge vào nhánh chính (`develop`/`main`) và đã được áp dụng lên CSDL môi trường dùng chung. Muốn sửa, hãy tạo một Migration mới.
3. **Bộ lọc Toàn cục & Xóa mềm (Global Query Filters):**
   - Với các entity có áp dụng xóa mềm (`is_deleted`), cấu hình Global Query Filter trong DbContext:
     ```csharp
     builder.Entity<Appointment>().HasQueryFilter(a => !a.IsDeleted);
     ```
   - Khi cần truy vấn cả dữ liệu đã xóa (ví dụ cho mục đích khôi phục hoặc báo cáo kiểm toán), sử dụng `.IgnoreQueryFilters()`.

---

## 3. Tối ưu hóa Truy vấn & Hiệu năng (Performance Guidelines)

- **AsNoTracking:** Luôn áp dụng `.AsNoTracking()` cho toàn bộ các câu truy vấn Read-only (Queries) trong CQRS để giảm overhead bộ nhớ và tăng tốc độ xử lý.
- **Select Projection:** Tránh `SELECT *` bằng cách sử dụng `.Select(x => new MyDto { ... })` để chỉ lấy đúng các cột cần thiết phục vụ giao diện.
- **Phân trang bắt buộc (Mandatory Pagination):** Mọi API trả về danh sách có nguy cơ tăng trưởng theo thời gian (Lịch hẹn, Bệnh nhân, Logs) bắt buộc phải sử dụng `.Skip((page - 1) * pageSize).Take(pageSize)`.
- **N+1 Problem:** Luôn chú ý load quan hệ bằng `.Include()` hoặc Projection hợp lý để tránh phát sinh hàng trăm câu query con lặp lại.

