using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Patients.Queries;

public record GetOpenReviewCasesQuery(IdentityReviewStatus? Status = null) : IRequest<Result<List<PatientIdentityReviewCaseDto>>>;

public class GetOpenReviewCasesQueryHandler : IRequestHandler<GetOpenReviewCasesQuery, Result<List<PatientIdentityReviewCaseDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IIdentifierMaskingService _maskingService;

    public GetOpenReviewCasesQueryHandler(
        IApplicationDbContext context,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _maskingService = maskingService;
    }

    public async Task<Result<List<PatientIdentityReviewCaseDto>>> Handle(
        GetOpenReviewCasesQuery request, 
        CancellationToken cancellationToken)
    {
        var targetStatus = request.Status ?? IdentityReviewStatus.OPEN;

        var cases = await _context.PatientIdentityReviewCases
            .AsNoTracking()
            .Include(c => c.Patient)
            .Include(c => c.CandidatePatient)
            .Where(c => c.Status == targetStatus)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

        var dtoList = cases.Select(c => new PatientIdentityReviewCaseDto
        {
            Id = c.Id,
            PatientId = c.PatientId,
            PatientCode = c.Patient.PatientCode,
            PatientFullName = c.Patient.FullName,
            PatientMaskedPhone = _maskingService.MaskPhone(c.Patient.PhoneNumber),
            CandidatePatientId = c.CandidatePatientId,
            CandidatePatientCode = c.CandidatePatient.PatientCode,
            CandidatePatientFullName = c.CandidatePatient.FullName,
            CandidateMaskedPhone = _maskingService.MaskPhone(c.CandidatePatient.PhoneNumber),
            MatchReason = c.MatchReason.ToString(),
            Status = c.Status.ToString(),
            ReviewedByUserId = c.ReviewedByUserId,
            ReviewedAt = c.ReviewedAt,
            ReviewNote = c.ReviewNote,
            CreatedAt = c.CreatedAt
        }).ToList();

        return Result<List<PatientIdentityReviewCaseDto>>.Success(dtoList);
    }
}
