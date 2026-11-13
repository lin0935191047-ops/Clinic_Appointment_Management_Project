---
name: testing-qa
description: Hướng dẫn viết Unit Test, Integration Test với Testcontainers PostgreSQL trong .NET và Component/E2E Test cho React.
---

# Testing & Quality Assurance Skill

Kỹ năng này hướng dẫn AI Agent cách thiết lập và viết các bộ kiểm thử tự động toàn diện, đảm bảo độ tin cậy cao nhất cho hệ thống y tế.

---

## 1. Viết Unit Test cho Business Logic (xUnit + FluentAssertions)

Khi kiểm thử thuật toán chia ca hoặc máy trạng thái:

```csharp
public class AppointmentStateMachineTests
{
    [Theory]
    [InlineData(AppointmentStatus.Pending, AppointmentStatus.Confirmed, true)]
    [InlineData(AppointmentStatus.Confirmed, AppointmentStatus.CheckedIn, true)]
    [InlineData(AppointmentStatus.CheckedIn, AppointmentStatus.InProgress, true)]
    [InlineData(AppointmentStatus.InProgress, AppointmentStatus.Completed, true)]
    [InlineData(AppointmentStatus.Completed, AppointmentStatus.Cancelled, false)] // Không thể hủy lịch đã khám xong
    public void CanTransition_ShouldValidateAllowedTransitions(
        AppointmentStatus from, 
        AppointmentStatus to, 
        bool expectedResult)
    {
        // Act
        var result = AppointmentStateMachine.CanTransition(from, to);

        // Assert
        result.Should().Be(expectedResult);
    }
}
```

---

## 2. Viết Integration Test với Testcontainers PostgreSQL

Kiểm thử đồng thời chống Double-Booking:

```csharp
[Fact]
public async Task ConcurrentBooking_SameDoctorAndSlot_OnlyOneShouldSucceed()
{
    // Arrange: Khởi tạo dữ liệu bác sĩ và 10 request đặt cùng khung giờ
    var doctor = await SeedDoctorAsync();
    var slotDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
    var startTime = new TimeOnly(9, 0);

    var tasks = Enumerable.Range(1, 10).Select(i => 
    {
        var command = new BookGuestAppointmentCommand
        {
            DoctorId = doctor.Id,
            Date = slotDate,
            StartTime = startTime,
            FullName = $"Patient {i}",
            PhoneNumber = $"090123456{i}"
        };
        return _client.PostAsJsonAsync("/api/v1/appointments/guest", command);
    });

    // Act: Gửi đồng thời 10 requests
    var responses = await Task.WhenAll(tasks);

    // Assert: Đúng 1 request thành công (201 Created), 9 requests còn lại nhận 409 Conflict
    var successCount = responses.Count(r => r.StatusCode == HttpStatusCode.Created);
    var conflictCount = responses.Count(r => r.StatusCode == HttpStatusCode.Conflict);

    successCount.Should().Be(1);
    conflictCount.Should().Be(9);
}
```

---

## 3. Checklist Kiểm thử trước khi Bàn giao (QA Checklist)

- [ ] Tất cả các ca Unit Test và Integration Test đều pass khi chạy `dotnet test`.
- [ ] Không có test nào phụ thuộc vào thứ tự chạy hoặc dữ liệu tĩnh còn sót lại từ lần chạy trước.
- [ ] Các trường hợp biên (Edge Cases) đã được bao phủ:
  - Giờ bắt đầu trùng với giờ kết thúc ca.
  - Đặt lịch qua nửa đêm hoặc đổi múi giờ.
  - Số điện thoại có định dạng quốc tế hoặc khoảng trắng.

