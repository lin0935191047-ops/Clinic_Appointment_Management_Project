# ADR 0002: Lựa chọn Cơ sở Dữ liệu PostgreSQL kết hợp Entity Framework Core

- **Trạng thái:** Được chấp thuận (Accepted)
- **Ngày quyết định:** 2026-11-13
- **Người đề xuất:** Data & Architecture Team

---

## 1. Ngữ cảnh (Context)
Hệ thống đặt lịch khám bệnh yêu cầu:
- Tính toàn vẹn dữ liệu cực kỳ khắt khe (ACID) để đảm bảo không xảy ra đặt trùng lịch hẹn (Zero Double Booking).
- Hỗ trợ quan hệ dữ liệu phức tạp giữa Người dùng, Bác sĩ, Chuyên khoa, Lịch làm việc định kỳ và Lịch sử khám bệnh.
- Chi phí bản quyền và khả năng triển khai linh hoạt trên đám mây (Cloud) hoặc môi trường On-Premises.

Các lựa chọn được cân nhắc:
- Microsoft SQL Server
- PostgreSQL
- MySQL / MariaDB
- MongoDB (NoSQL)

---

## 2. Quyết định (Decision)
Chúng tôi quyết định lựa chọn **PostgreSQL 16+** làm cơ sở dữ liệu chính, giao tiếp qua **Entity Framework Core** với nhà cung cấp `Npgsql.EntityFrameworkCore.PostgreSQL`.

---

## 3. Lý do lựa chọn (Rationale)
1. **Độ tin cậy và Tính toàn vẹn giao dịch (ACID & Concurrency Control):**
   - PostgreSQL hỗ trợ các mức cô lập giao dịch chuẩn mực (`Read Committed`, `Repeatable Read`, `Serializable`) và cơ chế MVCC (Multi-Version Concurrency Control) vượt trội.
   - Hỗ trợ Partial Unique Indexes (ví dụ: chỉ unique khi `status NOT IN ('CANCELLED')`), tính năng cực kỳ quan trọng cho bài toán đặt lịch khám.
2. **Khả năng lưu trữ dữ liệu bán cấu trúc (JSONB):**
   - Các trường dữ liệu y tế mở rộng, log thay đổi trạng thái hoặc payload cấu hình có thể lưu dưới dạng JSONB mà vẫn có thể đánh chỉ mục (GIN Index) và truy vấn tốc độ cao.
3. **Mã nguồn mở & Tối ưu chi phí:**
   - Hoàn toàn miễn phí, không phát sinh chi phí bản quyền thương mại đắt đỏ như MS SQL Server.
   - Được hỗ trợ mạnh mẽ trên tất cả các nền tảng đám mây (AWS RDS, Google Cloud SQL, Azure Database for PostgreSQL) và Docker.
4. **Hệ sinh thái Npgsql / EF Core hoàn thiện:**
   - Npgsql là một trong những provider ADO.NET / EF Core tốt nhất hiện nay, liên tục cập nhật các tính năng mới của .NET.

---

## 4. Hệ quả & Đánh đổi (Consequences)
- **Tích cực:** Chi phí thấp, độ tin cậy tuyệt đối, hỗ trợ đầy đủ các kiểu dữ liệu nâng cao (UUID, JSONB, Range types, Full-text search).
- **Tiêu cực / Cần khắc phục:** Cần lưu ý quy chuẩn đặt tên cột chữ thường `snake_case` trong PostgreSQL để tránh các vấn đề về phân biệt hoa/thường (case sensitivity) khi viết raw SQL nếu có.

