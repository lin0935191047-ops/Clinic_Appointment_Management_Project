using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Application.Common.Models;
using ClinicAppointment.Application.Features.Patients.DTOs;
using ClinicAppointment.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ClinicAppointment.Application.Features.Patients.Queries;

public record SearchReceptionPatientsQuery(
    string? Keyword = null,
    DateOnly? DateOfBirth = null,
    short? BirthYear = null,
    bool? IsPossibleDuplicate = null,
    string? RecordStatus = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<Result<PaginatedList<PatientDto>>>;

public class SearchReceptionPatientsQueryHandler : IRequestHandler<SearchReceptionPatientsQuery, Result<PaginatedList<PatientDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPatientSecurityService _securityService;
    private readonly IIdentifierMaskingService _maskingService;

    public SearchReceptionPatientsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IPatientSecurityService securityService,
        IIdentifierMaskingService maskingService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _securityService = securityService;
        _maskingService = maskingService;
    }

    public async Task<Result<PaginatedList<PatientDto>>> Handle(
        SearchReceptionPatientsQuery request, 
        CancellationToken cancellationToken)
    {
        // 1. Phân quyền: Chỉ Staff (Receptionist / Admin) mới được tìm kiếm tại quầy lễ tân
        if (!_currentUserService.IsStaff)
        {
            return Result<PaginatedList<PatientDto>>.Failure(
                "Bạn không có quyền truy cập danh sách hồ sơ tại quầy lễ tân. Yêu cầu quyền Receptionist hoặc Admin.", 
                403);
        }

        var query = _context.Patients.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Keyword))
        {
            var keyword = request.Keyword.Trim();
            var normalizedKeyword = Patient.NormalizeFullName(keyword);

            query = query.Where(p => 
                p.FullName.Contains(keyword) || 
                p.NormalizedFullName.Contains(normalizedKeyword) ||
                p.PatientCode.Contains(keyword) ||
                p.PhoneNumber.Contains(keyword));
        }

        if (request.DateOfBirth.HasValue)
        {
            query = query.Where(p => p.DateOfBirth == request.DateOfBirth.Value);
        }

        if (request.BirthYear.HasValue)
        {
            query = query.Where(p => p.BirthYear == request.BirthYear.Value);
        }

        if (request.IsPossibleDuplicate.HasValue)
        {
            query = query.Where(p => p.IsPossibleDuplicate == request.IsPossibleDuplicate.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.RecordStatus))
        {
            query = query.Where(p => p.RecordStatus == request.RecordStatus.Trim());
        }

        int totalCount = await query.CountAsync(cancellationToken);

        int page = request.Page <= 0 ? 1 : request.Page;
        int pageSize = request.PageSize <= 0 ? 20 : (request.PageSize > 100 ? 100 : request.PageSize);

        var patients = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var dtos = patients.Select(p =>
        {
            var decryptedCccd = _securityService.DecryptNationalId(p);
            var decryptedPassport = _securityService.DecryptPassport(p);
            var decryptedBhyt = _securityService.DecryptInsuranceCard(p);

            return new PatientDto
            {
                Id = p.Id,
                UserId = p.UserId,
                PatientCode = p.PatientCode,
                FullName = p.FullName,
                NormalizedFullName = p.NormalizedFullName,
                DateOfBirth = p.DateOfBirth,
                BirthYear = p.BirthYear,
                PhoneNumber = p.PhoneNumber,
                MaskedPhoneNumber = _maskingService.MaskPhone(p.PhoneNumber),
                Email = p.Email,
                Gender = p.Gender,
                Address = p.Address,
                MaskedNationalId = !string.IsNullOrWhiteSpace(decryptedCccd) ? _maskingService.MaskNationalId(decryptedCccd) : null,
                MaskedPassport = !string.IsNullOrWhiteSpace(decryptedPassport) ? _maskingService.MaskPassport(decryptedPassport) : null,
                MaskedHealthInsurance = !string.IsNullOrWhiteSpace(decryptedBhyt) ? _maskingService.MaskHealthInsurance(decryptedBhyt) : null,
                RecordStatus = p.RecordStatus,
                IsPossibleDuplicate = p.IsPossibleDuplicate,
                CreatedAt = p.CreatedAt
            };
        }).ToList();

        var paginated = new PaginatedList<PatientDto>(dtos, totalCount, page, pageSize);
        return Result<PaginatedList<PatientDto>>.Success(paginated, 200);
    }
}
