---
name: dotnet-backend
description: Hướng dẫn chuyên sâu về phát triển ASP.NET Core 8 Web API theo kiến trúc Clean Architecture, CQRS với MediatR, FluentValidation và xử lý ngoại lệ chuẩn mực.
---

# .NET Backend Development Skill

Kỹ năng này hướng dẫn AI Agent cách xây dựng, mở rộng và bảo trì mã nguồn Backend bằng C# và ASP.NET Core 8.0+.

---

## 1. Cấu trúc Triển khai Tính năng mới (Feature Implementation Workflow)

Khi triển khai một Use Case mới (ví dụ: `BookAppointmentCommand`):

1. **Domain Layer (`ClinicAppointment.Domain`):**
   - Kiểm tra Entity tương ứng (`Appointment.cs`).
   - Bổ sung logic nghiệp vụ, phương thức thay đổi trạng thái, hoặc Domain Event nếu có.
   - Định nghĩa Domain Exception nếu có vi phạm quy tắc nghiệp vụ.

2. **Application Layer (`ClinicAppointment.Application`):**
   - Tạo thư mục feature: `Features/Appointments/Commands/BookAppointment/`.
   - Tạo Command: `BookAppointmentCommand.cs` kế thừa `IRequest<Result<AppointmentResponseDto>>`.
   - Tạo Validator: `BookAppointmentCommandValidator.cs` kế thừa `AbstractValidator<BookAppointmentCommand>`.
   - Tạo Handler: `BookAppointmentCommandHandler.cs` kế thừa `IRequestHandler<BookAppointmentCommand, Result<AppointmentResponseDto>>`.
   - Trong Handler: Inject `IApplicationDbContext` (hoặc Repository), xử lý transaction và gọi Domain method.

3. **API Layer (`ClinicAppointment.Api`):**
   - Trong `AppointmentsController.cs`:
     ```csharp
     [HttpPost]
     [ProducesResponseType(typeof(ApiResponse<AppointmentResponseDto>), StatusCodes.Status201Created)]
     [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
     [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
     public async Task<IActionResult> BookAppointment(
         [FromBody] BookAppointmentCommand command, 
         CancellationToken cancellationToken)
     {
         var result = await _mediator.Send(command, cancellationToken);
         return result.IsSuccess 
             ? CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result)
             : HandleFailure(result);
     }
     ```

---

## 2. Tiêu chuẩn Xử lý Ngoại lệ & Validation Pipeline

- Sử dụng MediatR Pipeline Behavior (`ValidationBehavior<TRequest, TResponse>`) để tự động quét và chạy FluentValidation trước khi request đi tới Handler.
- Khi validation thất bại, ném `ValidationException` chứa danh sách lỗi chi tiết theo từng field.
- Middleware toàn cục bắt `ValidationException` và trả về HTTP 400 kèm định dạng chuẩn:
  ```json
  {
    "success": false,
    "statusCode": 400,
    "message": "Dữ liệu không hợp lệ.",
    "errors": [
      { "field": "startTime", "message": "Giờ khám không được nằm trong quá khứ." }
    ]
  }
  ```

---

## 3. Checklist Kiểm tra Code Backend

Trước khi hoàn tất mã C#:
- [ ] Đã sử dụng file-scoped namespaces chưa?
- [ ] Đã truyền `CancellationToken` vào các phương thức async chưa?
- [ ] Đã kiểm tra null safety (không phát sinh warning nullability) chưa?
- [ ] Logic nghiệp vụ có bị rò rỉ ra ngoài Controller không?
- [ ] Dữ liệu trả về có được wrap trong DTO / Envelope không?

