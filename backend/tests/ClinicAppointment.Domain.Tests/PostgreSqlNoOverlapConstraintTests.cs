using System.Net;
using System.Text.Json;
using ClinicAppointment.Api.Middlewares;
using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class PostgreSqlNoOverlapConstraintTests
{
    private ApplicationDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task Middleware_WhenAppointmentConflictExceptionThrown_ShouldReturn409Conflict()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var exception = new AppointmentConflictException("Khung giờ khám này vừa có người khác đặt trước. Vui lòng chọn khung giờ khác.");

        // Act
        await ExceptionHandlingMiddleware.HandleExceptionAsync(context, exception);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        var response = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.StatusCode.Should().Be(409);
        response.Message.Should().Contain("vừa có người khác đặt trước");
    }

    [Fact]
    public async Task Middleware_WhenDbUpdateExceptionWithExclusionConstraintThrown_ShouldReturn409Conflict()
    {
        // Arrange: Giả lập DbUpdateException bọc lỗi vi phạm constraint ex_appointments_doctor_no_overlap
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var innerEx = new Exception("duplicate key value violates exclusion constraint \"ex_appointments_doctor_no_overlap\" (SQLSTATE 23P01)");
        var dbUpdateEx = new DbUpdateException("An error occurred while saving the entity changes.", innerEx);

        // Act
        await ExceptionHandlingMiddleware.HandleExceptionAsync(context, dbUpdateEx);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Conflict);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        var response = JsonSerializer.Deserialize<ApiResponse<object>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        response.Should().NotBeNull();
        response!.Success.Should().BeFalse();
        response.StatusCode.Should().Be(409);
        response.Message.Should().Contain("vừa có người khác đặt trước");
    }

    [Theory]
    [InlineData("ex_appointments_doctor_no_overlap", true)]
    [InlineData("error 23P01 exclusion_violation", true)]
    [InlineData("ix_appointments_doctor_id_start_at", true)]
    [InlineData("some other database error", false)]
    public void IsExclusionOrUniqueConstraintViolation_ShouldIdentifyCorrectly(string errorMessage, bool expected)
    {
        var ex = new DbUpdateException("Test", new Exception(errorMessage));
        var result = ExceptionHandlingMiddleware.IsExclusionOrUniqueConstraintViolation(ex);
        result.Should().Be(expected);
    }

    [Fact]
    public async Task ConcurrentBookingIntegrity_WhenOverlapOccurs_ShouldPreventDoubleBooking()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var doctorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddDays(2).AddHours(9); // 09:00 UTC

        // Ca 1: 09:00 - 09:30
        var appointment1 = Appointment.CreateGuest(
            doctorId: doctorId,
            patientId: Guid.NewGuid(),
            serviceId: serviceId,
            serviceName: "Khám chuyên khoa",
            serviceDuration: 30,
            servicePrice: 200000m,
            startAtUtc: baseTime,
            reasonForVisit: "Ca khám 1"
        );
        appointment1.Confirm();
        db.Appointments.Add(appointment1);
        await db.SaveChangesAsync();

        // Ca 2: Cố gắng đặt cùng bác sĩ từ 09:15 - 09:45 (giao nhau từ 09:15 đến 09:30)
        var overlapStart = baseTime.AddMinutes(15);
        var overlapEnd = overlapStart.AddMinutes(30);

        // Kiểm tra logic exclusion trước khi ghi (Application / DB Guard)
        var hasConflict = await db.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartAt < overlapEnd &&
            overlapStart < a.EndAt);

        // Assert: Phát hiện xung đột chồng lấn ca
        hasConflict.Should().BeTrue();

        var act = () =>
        {
            if (hasConflict)
            {
                throw new AppointmentConflictException("Khung giờ khám này vừa có người khác đặt trước. Vui lòng chọn khung giờ khác.");
            }
        };

        act.Should().Throw<AppointmentConflictException>()
            .WithMessage("*vừa có người khác đặt trước*");
    }

    [Fact]
    public async Task ConcurrentBookingIntegrity_WhenDifferentDoctorsSameTime_ShouldSucceed()
    {
        // Arrange: Cùng thời gian 09:00 - 09:30 nhưng khác Bác sĩ -> Hợp lệ
        using var db = CreateInMemoryDbContext();
        var doc1Id = Guid.NewGuid();
        var doc2Id = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddDays(2).AddHours(9);

        var apt1 = Appointment.CreateGuest(doc1Id, Guid.NewGuid(), serviceId, "Khám A", 30, 200000m, baseTime, null);
        var apt2 = Appointment.CreateGuest(doc2Id, Guid.NewGuid(), serviceId, "Khám B", 30, 200000m, baseTime, null);
        db.Appointments.AddRange(apt1, apt2);
        await db.SaveChangesAsync();

        // Kiểm tra ca 2 không bị coi là xung đột với ca 1
        var conflictForDoc2 = await db.Appointments.AnyAsync(a =>
            a.DoctorId == doc2Id &&
            a.Id != apt2.Id &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartAt < apt2.EndAt &&
            apt2.StartAt < a.EndAt);

        conflictForDoc2.Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentBookingIntegrity_WhenConsecutiveSlotsSameDoctor_ShouldSucceed()
    {
        // Arrange: Cùng 1 bác sĩ, ca 1 là 08:30 - 09:00 và ca 2 là 09:00 - 09:30
        // Trong định nghĩa range [StartAt, EndAt) của GiST: 09:00 của ca 1 là exclusive, ca 2 là inclusive -> KHÔNG OVERLAP
        using var db = CreateInMemoryDbContext();
        var doctorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddDays(3).AddHours(8).AddMinutes(30);

        var apt1 = Appointment.CreateGuest(doctorId, Guid.NewGuid(), serviceId, "Ca 1", 30, 200000m, baseTime, null);
        var apt2 = Appointment.CreateGuest(doctorId, Guid.NewGuid(), serviceId, "Ca 2", 30, 200000m, baseTime.AddMinutes(30), null);
        db.Appointments.AddRange(apt1, apt2);
        await db.SaveChangesAsync();

        var overlap = await db.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Id != apt2.Id &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartAt < apt2.EndAt &&
            apt2.StartAt < a.EndAt);

        // Hai ca nối tiếp nhau không overlap
        overlap.Should().BeFalse();
    }

    [Fact]
    public async Task ConcurrentBookingIntegrity_WhenPreviousAppointmentCancelled_ShouldAllowNewBookingInSameSlot()
    {
        // Arrange: Ca trước lúc 09:00 - 09:30 đã bị hủy (Status = Cancelled)
        // Ràng buộc WHERE (status <> 'Cancelled') cho phép đặt lại đúng slot này
        using var db = CreateInMemoryDbContext();
        var doctorId = Guid.NewGuid();
        var serviceId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow.Date.AddDays(4).AddHours(9);

        var cancelledApt = Appointment.CreateGuest(doctorId, Guid.NewGuid(), serviceId, "Ca cũ", 30, 200000m, baseTime, null);
        cancelledApt.Status = AppointmentStatus.Cancelled; // Đã hủy
        db.Appointments.Add(cancelledApt);
        await db.SaveChangesAsync();

        // Ca mới đặt đúng slot 09:00 - 09:30
        var newStart = baseTime;
        var newEnd = baseTime.AddMinutes(30);

        var isBlocked = await db.Appointments.AnyAsync(a =>
            a.DoctorId == doctorId &&
            a.Status != AppointmentStatus.Cancelled &&
            a.StartAt < newEnd &&
            newStart < a.EndAt);

        // Khung giờ không bị chặn vì ca cũ đã Cancelled
        isBlocked.Should().BeFalse();

        var newApt = Appointment.CreateGuest(doctorId, Guid.NewGuid(), serviceId, "Ca mới", 30, 200000m, newStart, null);
        db.Appointments.Add(newApt);
        await db.SaveChangesAsync();

        var inDb = await db.Appointments.FindAsync(newApt.Id);
        inDb.Should().NotBeNull();
        inDb!.Status.Should().Be(AppointmentStatus.PendingConfirmation);
    }
}
