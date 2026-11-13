# Backend Rules (.NET / ASP.NET Core)

> Quy chuẩn lập trình Backend áp dụng cho C# và ASP.NET Core 8.0+.

---

## 1. C# & .NET Code Style

- **C# Modern Features:**
  - Sử dụng **File-scoped namespaces** (`namespace ClinicAppointment.Domain.Entities;`).
  - Bật tính năng **Nullable Reference Types** (`<Nullable>enable</Nullable>`) trên toàn bộ các project C#.
  - Sử dụng `record` hoặc `record struct` cho DTOs, Commands, Queries, Value Objects.
  - Sử dụng pattern matching và expression-bodied members khi phù hợp để tăng tính trong sáng của mã.
- **Asynchronous Programming:**
  - Mọi thao tác truy xuất I/O (Database, Network, File) bắt buộc dùng `async/await`.
  - Luôn truyền `CancellationToken cancellationToken = default` vào các phương thức async và chuyển tiếp vào EF Core / HttpClient.
- **Naming Conventions:**
  - `PascalCase` cho tên Class, Interface, Method, Property, Enum.
  - `camelCase` cho parameters và local variables.
  - `_camelCase` cho private readonly fields.
  - Interface bắt đầu bằng tiền tố `I` (ví dụ `IAppointmentRepository`, `IDateTimeProvider`).

---

## 2. Quy tắc Phân tầng Clean Architecture

```text
Domain  <---  Application  <---  Infrastructure / Api
```

1. **Domain Layer:**
   - Hoàn toàn độc lập, không tham chiếu bất kỳ project hay external NuGet package nào (ngoài các package tiện ích lõi như MediatR.Contracts nếu cần).
   - Domain Entities quản lý trạng thái của chính nó (Encapsulation). Hạn chế `public set` tràn lan; ưu tiên dùng methods để thay đổi trạng thái (ví dụ `appointment.Cancel(reason)` thay vì gán trực tiếp `appointment.Status = ...`).
2. **Application Layer:**
   - Chứa các Use Cases được triển khai theo mô hình CQRS (Command / Query) thông qua `MediatR`.
   - Mỗi Command / Query nên nằm trong một thư mục riêng biệt kèm theo Handler, Validator và DTO phản hồi.
   - Sử dụng `FluentValidation` để tự động validate dữ liệu đầu vào thông qua MediatR Pipeline Behavior.
3. **Infrastructure Layer:**
   - Nơi duy nhất chứa mã liên quan đến EF Core DbContext, cấu hình Npgsql, gửi mail, sinh mã JWT token.
4. **API Layer:**
   - Controllers phải mỏng (Thin Controllers). Nhiệm vụ duy nhất của Controller là nhận request HTTP, gửi qua `IMediator.Send()`, và trả về `ActionResult` tương ứng.
   - Không đặt câu truy vấn LINQ hoặc logic nghiệp vụ bên trong Controller.

---

## 3. Xử lý Lỗi & Phản hồi (Error Handling & Responses)

- **Không bắt Exception nuốt im lặng:** Bắt buộc sử dụng Global Exception Handling Middleware (`UseExceptionHandler` hoặc `IExceptionHandler` trong .NET 8).
- **Kết quả trả về:**
  - Phản hồi HTTP tuân thủ RFC 7807 (`ProblemDetails`) hoặc mẫu Response Envelope đã quy định tại `docs/API_SPECIFICATION.md`.
  - Trả đúng HTTP Status Code:
    - `200 OK`: Truy vấn hoặc cập nhật thành công.
    - `201 Created`: Tạo mới tài nguyên thành công.
    - `400 Bad Request`: Dữ liệu đầu vào sai validation.
    - `401 Unauthorized`: Chưa đăng nhập hoặc token hết hạn.
    - `403 Forbidden`: Đã đăng nhập nhưng không đủ quyền truy cập.
    - `404 Not Found`: Không tìm thấy tài nguyên.
    - `409 Conflict`: Xung đột dữ liệu (ví dụ trùng lịch khám hoặc trùng email).
    - `500 Internal Server Error`: Lỗi hệ thống không lường trước (ẩn chi tiết stack trace trên môi trường Production).

---

## 4. Ghi log có cấu trúc (Structured Logging)

- Sử dụng `ILogger<T>` với định dạng message template tham số hóa (không dùng phép cộng chuỗi string interpolation `$""` để tránh mất tính năng structured logging).
- Ví dụ chuẩn:
  ```csharp
  _logger.LogInformation("Lịch hẹn {AppointmentCode} đã được tạo thành công cho bệnh nhân {PatientId}", appointment.Code, patient.Id);
  ```
- Tuyệt đối không log thông tin nhạy cảm: mật khẩu thuần, mã OTP, số thẻ tín dụng hoặc toàn bộ hồ sơ y bạ chi tiết.

