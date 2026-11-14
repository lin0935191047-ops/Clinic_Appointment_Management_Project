namespace ClinicAppointment.Application.Features.Reporting.DTOs;

public record AppointmentStatusBreakdownDto(
    int TotalAppointments,
    int PendingConfirmationCount,
    int ConfirmedCount,
    int CheckedInCount,
    int InProgressCount,
    int CompletedCount,
    int CancelledCount,
    int NoShowCount,
    double CompletionRate,
    double CancellationRate
);

public record FinancialSummaryDto(
    decimal TotalGrossRevenue,
    decimal TotalPaidAmount,
    decimal TotalExpectedRefundAmount,
    decimal TotalActualRefundedAmount,
    decimal TotalCancellationFeeCollected,
    decimal TotalNetRevenue
);

public record SpecialtyReportDto(
    Guid SpecialtyId,
    string SpecialtyName,
    int TotalAppointments,
    int CompletedCount,
    int CancelledCount,
    double CompletionRate,
    decimal GrossRevenue
);

public record DoctorReportDto(
    Guid DoctorId,
    string DoctorName,
    string SpecialtyName,
    int TotalAppointments,
    int CompletedCount,
    int CancelledCount,
    double CompletionRate,
    decimal GrossRevenue
);

public record AdminDashboardReportDto(
    DateOnly? FromDate,
    DateOnly? ToDate,
    Guid? DoctorId,
    Guid? SpecialtyId,
    AppointmentStatusBreakdownDto StatusBreakdown,
    FinancialSummaryDto FinancialSummary,
    IReadOnlyList<SpecialtyReportDto> SpecialtyReports,
    IReadOnlyList<DoctorReportDto> DoctorReports,
    DateTime GeneratedAtUtc
);
