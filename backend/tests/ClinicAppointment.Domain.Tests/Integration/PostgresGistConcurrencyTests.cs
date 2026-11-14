using System;
using System.Linq;
using System.Threading.Tasks;
using ClinicAppointment.Application.Common.Exceptions;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;
using ClinicAppointment.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace ClinicAppointment.Domain.Tests.Integration;

/// <summary>
/// Kiểm thử kiểm tra Exclusion Constraint GiST trên cơ sở dữ liệu PostgreSQL thực tế (FEAT-051)
/// Ràng buộc: ex_appointments_doctor_no_overlap EXCLUDE USING gist (doctor_id WITH =, tstzrange(start_at, end_at, '[)') WITH &&) WHERE (status <> 'Cancelled')
/// </summary>
public class PostgresGistConcurrencyTests
{
    private const string ConnectionString = "Host=localhost;Port=5432;Database=clinicappointmentdb;Username=postgres;Password=Postgres@123;";

    private ApplicationDbContext CreatePostgresDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString)
                      .UseSnakeCaseNamingConvention();
        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private async Task<(Guid doctorId, Guid patient1Id, Guid patient2Id, Guid serviceId)> EnsureTestDataAsync()
    {
        using var db = CreatePostgresDbContext();

        // 1. Kiểm tra / Tạo chuyên khoa
        var specialty = await db.Specialties.FirstOrDefaultAsync(s => s.Code == "CONCURRENCY_SPEC");
        if (specialty == null)
        {
            specialty = Specialty.Create("CONCURRENCY_SPEC", "Chuyên Khoa Kiểm Thử Đồng Thời", "chuyen-khoa-kiem-thu", "Mô tả kiểm thử");
            db.Specialties.Add(specialty);
            await db.SaveChangesAsync();
        }

        // 2. Kiểm tra / Tạo Bác sĩ
        var doctor = await db.Doctors.FirstOrDefaultAsync(d => d.DoctorCode == "DOC-GIST-01");
        if (doctor == null)
        {
            var user = User.Create($"doctor.gist.{Guid.NewGuid():N}@clinic.vn", "PasswordHash@123");
            db.Users.Add(user);
            await db.SaveChangesAsync();

            doctor = Doctor.Create(
                userId: user.Id,
                specialtyId: specialty.Id,
                doctorCode: "DOC-GIST-01",
                fullName: "BS. Concurrency Tester",
                title: "ThS.BS",
                roomNumber: "P.999");
            db.Doctors.Add(doctor);
            await db.SaveChangesAsync();
        }

        // 3. Kiểm tra / Tạo Dịch vụ
        var service = await db.Services.FirstOrDefaultAsync(s => s.Code == "SRV-GIST-01");
        if (service == null)
        {
            service = Service.Create(
                specialtyId: specialty.Id,
                code: "SRV-GIST-01",
                name: "Dịch Vụ Khám Đo Lường Đồng Thời",
                durationMinutes: 30,
                price: 200_000m,
                description: "Test service");
            db.Services.Add(service);
            await db.SaveChangesAsync();
        }

        // 4. Tạo 2 bệnh nhân độc lập
        var patient1 = Patient.Create(
            fullName: "Bệnh Nhân Concurrency Một",
            phoneNumber: "0901112233",
            dateOfBirth: new DateOnly(1990, 1, 1),
            birthYear: 1990);
        var patient2 = Patient.Create(
            fullName: "Bệnh Nhân Concurrency Hai",
            phoneNumber: "0904445566",
            dateOfBirth: new DateOnly(1992, 2, 2),
            birthYear: 1992);

        db.Patients.AddRange(patient1, patient2);
        await db.SaveChangesAsync();

        return (doctor.Id, patient1.Id, patient2.Id, service.Id);
    }

    [Fact]
    public async Task PostgresGist_ConcurrentBookingSameSlot_OneSucceedsAndOneFailsWithExclusionViolation()
    {
        // Arrange: Chuẩn bị 2 DbContext riêng biệt đại diện cho 2 client/connection đồng thời
        var (doctorId, patient1Id, patient2Id, serviceId) = await EnsureTestDataAsync();

        // Khung giờ khám: Ngày ngẫu nhiên ở tương lai để tránh trùng dữ liệu cũ
        var baseDate = DateTime.UtcNow.Date.AddDays(new Random().Next(30, 90));
        var startAtUtc = DateTime.SpecifyKind(baseDate.AddHours(9), DateTimeKind.Utc); // 09:00 UTC
        var endAtUtc = DateTime.SpecifyKind(baseDate.AddHours(9).AddMinutes(30), DateTimeKind.Utc); // 09:30 UTC

        var bookingCode1 = $"APT-RACE-{Guid.NewGuid():N}"[..20].ToUpperInvariant();
        var bookingCode2 = $"APT-RACE-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

        var apt1 = Appointment.CreateGuest(
            doctorId: doctorId,
            patientId: patient1Id,
            serviceId: serviceId,
            serviceName: "Dịch Vụ Khám Đo Lường Đồng Thời",
            serviceDuration: 30,
            servicePrice: 200_000m,
            startAtUtc: startAtUtc,
            reasonForVisit: "Client 1 đặt chỗ");
        apt1.BookingCode = bookingCode1;

        var apt2 = Appointment.CreateGuest(
            doctorId: doctorId,
            patientId: patient2Id,
            serviceId: serviceId,
            serviceName: "Dịch Vụ Khám Đo Lường Đồng Thời",
            serviceDuration: 30,
            servicePrice: 200_000m,
            startAtUtc: startAtUtc,
            reasonForVisit: "Client 2 đặt chỗ trùng khung giờ");
        apt2.BookingCode = bookingCode2;

        // Act: Thực hiện lưu đồng thời qua 2 DbContext chạy song song
        Exception? exception1 = null;
        Exception? exception2 = null;

        var task1 = Task.Run(async () =>
        {
            try
            {
                using var db1 = CreatePostgresDbContext();
                db1.Appointments.Add(apt1);
                await db1.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                exception1 = ex;
            }
        });

        var task2 = Task.Run(async () =>
        {
            try
            {
                using var db2 = CreatePostgresDbContext();
                db2.Appointments.Add(apt2);
                await db2.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                exception2 = ex;
            }
        });

        await Task.WhenAll(task1, task2);

        // Assert: Đúng 1 task thành công và đúng 1 task ném DbUpdateException do vi phạm GiST exclusion constraint
        var successCount = (exception1 == null ? 1 : 0) + (exception2 == null ? 1 : 0);
        var failedCount = (exception1 != null ? 1 : 0) + (exception2 != null ? 1 : 0);

        successCount.Should().Be(1, "Chỉ được phép có duy nhất 1 cuộc hẹn thành công khi tranh chấp đồng thời");
        failedCount.Should().Be(1, "Cuộc hẹn còn lại bắt buộc phải thất bại do ràng buộc PostgreSQL");

        var failedException = exception1 ?? exception2;
        failedException.Should().NotBeNull();
        failedException.Should().BeOfType<DbUpdateException>();

        var postgresEx = failedException!.InnerException as PostgresException;
        postgresEx.Should().NotBeNull();
        // Cả hai cơ chế bảo vệ của PostgreSQL: 23P01 (exclusion_violation từ GiST) hoặc 23505 (unique_violation từ B-tree)
        postgresEx!.SqlState.Should().BeOneOf(PostgresErrorCodes.ExclusionViolation, PostgresErrorCodes.UniqueViolation);

        // Kiểm tra trong PostgreSQL: Chỉ có đúng 1 bản ghi được lưu
        using var verifyDb = CreatePostgresDbContext();
        var appointmentsInDb = await verifyDb.Appointments
            .Where(a => a.DoctorId == doctorId && a.StartAt == startAtUtc && a.Status != AppointmentStatus.Cancelled)
            .ToListAsync();

        appointmentsInDb.Should().HaveCount(1);
    }

    [Fact]
    public async Task PostgresGist_OverlappingTimeRanges_RejectsSecondAppointment()
    {
        // Arrange
        var (doctorId, patient1Id, patient2Id, serviceId) = await EnsureTestDataAsync();

        var baseDate = DateTime.UtcNow.Date.AddDays(new Random().Next(100, 150));
        // Ca 1: 14:00 - 14:30
        var start1 = DateTime.SpecifyKind(baseDate.AddHours(14), DateTimeKind.Utc);
        // Ca 2: 14:15 - 14:45 (Bị overlap 15 phút với ca 1)
        var start2 = DateTime.SpecifyKind(baseDate.AddHours(14).AddMinutes(15), DateTimeKind.Utc);

        var apt1 = Appointment.CreateGuest(
            doctorId: doctorId,
            patientId: patient1Id,
            serviceId: serviceId,
            serviceName: "Khám Overlap 1",
            serviceDuration: 30,
            servicePrice: 200_000m,
            startAtUtc: start1,
            reasonForVisit: "Lý do overlap 1");

        var apt2 = Appointment.CreateGuest(
            doctorId: doctorId,
            patientId: patient2Id,
            serviceId: serviceId,
            serviceName: "Khám Overlap 2",
            serviceDuration: 30,
            servicePrice: 200_000m,
            startAtUtc: start2,
            reasonForVisit: "Lý do overlap 2");

        using (var db1 = CreatePostgresDbContext())
        {
            db1.Appointments.Add(apt1);
            await db1.SaveChangesAsync();
        }

        // Act & Assert: Ca 2 giao nhau 1 phần với ca 1 -> Bị PostgreSQL GiST reject
        using (var db2 = CreatePostgresDbContext())
        {
            db2.Appointments.Add(apt2);
            var act = async () => await db2.SaveChangesAsync();

            var thrownEx = await act.Should().ThrowAsync<DbUpdateException>();
            var pgEx = thrownEx.Which.InnerException as PostgresException;
            pgEx.Should().NotBeNull();
            pgEx!.SqlState.Should().Be(PostgresErrorCodes.ExclusionViolation);
            pgEx.ConstraintName.Should().Be("ex_appointments_doctor_no_overlap");
        }
    }

    [Fact]
    public async Task PostgresGist_CancelledAppointment_ReleasesSlotForNewBooking()
    {
        // Arrange: Tạo cuộc hẹn và sau đó hủy (Status = Cancelled)
        var (doctorId, patient1Id, patient2Id, serviceId) = await EnsureTestDataAsync();

        var baseDate = DateTime.UtcNow.Date.AddDays(new Random().Next(160, 200));
        var startUtc = DateTime.SpecifyKind(baseDate.AddHours(10), DateTimeKind.Utc); // 10:00

        using (var db1 = CreatePostgresDbContext())
        {
            var initialApt = Appointment.CreateGuest(
                doctorId: doctorId,
                patientId: patient1Id,
                serviceId: serviceId,
                serviceName: "Khám sắp hủy",
                serviceDuration: 30,
                servicePrice: 200_000m,
                startAtUtc: startUtc,
                reasonForVisit: "Khám ban đầu");

            db1.Appointments.Add(initialApt);
            await db1.SaveChangesAsync();

            // Thực hiện Hủy ca hẹn (reason trước, serverTimestampUtc sau)
            initialApt.Cancel("Bệnh nhân bận việc gia đình", DateTime.UtcNow);
            await db1.SaveChangesAsync();
        }

        // Act: Bệnh nhân khác đặt vào đúng khung giờ 10:00 của ca đã hủy
        using (var db2 = CreatePostgresDbContext())
        {
            var newApt = Appointment.CreateGuest(
                doctorId: doctorId,
                patientId: patient2Id,
                serviceId: serviceId,
                serviceName: "Khám thế chỗ ca hủy",
                serviceDuration: 30,
                servicePrice: 200_000m,
                startAtUtc: startUtc,
                reasonForVisit: "Đặt vào giờ đã hủy");

            db2.Appointments.Add(newApt);
            var saveAction = async () => await db2.SaveChangesAsync();

            // Assert: Phải thành công mỹ mãn vì GiST constraint có điều kiện WHERE (status <> 'Cancelled')
            await saveAction.Should().NotThrowAsync();
        }
    }

    [Fact]
    public async Task PostgresGist_TransactionRollbackOnConflict_MaintainsDatabaseConsistency()
    {
        // Arrange
        var (doctorId, patient1Id, patient2Id, serviceId) = await EnsureTestDataAsync();

        var baseDate = DateTime.UtcNow.Date.AddDays(new Random().Next(210, 250));
        var startUtc = DateTime.SpecifyKind(baseDate.AddHours(15), DateTimeKind.Utc);

        using (var setupDb = CreatePostgresDbContext())
        {
            var aptExisting = Appointment.CreateGuest(
                doctorId: doctorId,
                patientId: patient1Id,
                serviceId: serviceId,
                serviceName: "Khám hiện hữu",
                serviceDuration: 30,
                servicePrice: 200_000m,
                startAtUtc: startUtc,
                reasonForVisit: "Khám đã có sẵn");
            setupDb.Appointments.Add(aptExisting);
            await setupDb.SaveChangesAsync();
        }

        // Act: Bắt đầu một Transaction rõ ràng, cố gắng thêm ca trùng giờ và một notification
        using (var transDb = CreatePostgresDbContext())
        {
            await using var transaction = await transDb.Database.BeginTransactionAsync();

            var conflictingApt = Appointment.CreateGuest(
                doctorId: doctorId,
                patientId: patient2Id,
                serviceId: serviceId,
                serviceName: "Khám xung đột",
                serviceDuration: 30,
                servicePrice: 200_000m,
                startAtUtc: startUtc,
                reasonForVisit: "Cố tình đặt trùng");

            transDb.Appointments.Add(conflictingApt);

            // Giả lập lưu trong transaction
            Func<Task> action = async () =>
            {
                await transDb.SaveChangesAsync();
                await transaction.CommitAsync();
            };

            // Assert: Bị chặn và ném ngoại lệ
            await action.Should().ThrowAsync<DbUpdateException>();
            
            // Rollback an toàn
            await transaction.RollbackAsync();
        }

        // Kiểm tra sau khi rollback: Số lượng cuộc hẹn tại giờ đó vẫn là 1 (chỉ có ca ban đầu)
        using (var verifyDb = CreatePostgresDbContext())
        {
            var totalAtSlot = await verifyDb.Appointments
                .CountAsync(a => a.DoctorId == doctorId && a.StartAt == startUtc && a.Status != AppointmentStatus.Cancelled);
            totalAtSlot.Should().Be(1);
        }
    }
}
