using ClinicAppointment.Application.Common.Interfaces;
using ClinicAppointment.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicAppointment.Infrastructure.Services;

public class PatientSecurityService : IPatientSecurityService
{
    private readonly IPiiEncryptionService _encryptionService;
    private readonly IHmacService _hmacService;
    private readonly IIdentifierMaskingService _maskingService;
    private readonly IApplicationDbContext _context;
    private readonly ILogger<PatientSecurityService> _logger;

    public PatientSecurityService(
        IPiiEncryptionService encryptionService,
        IHmacService hmacService,
        IIdentifierMaskingService maskingService,
        IApplicationDbContext context,
        ILogger<PatientSecurityService> logger)
    {
        _encryptionService = encryptionService;
        _hmacService = hmacService;
        _maskingService = maskingService;
        _context = context;
        _logger = logger;
    }

    public void AttachNationalId(Patient patient, string nationalId)
    {
        if (string.IsNullOrWhiteSpace(nationalId)) return;

        patient.NationalIdHmac = _hmacService.ComputeHmac(nationalId);
        patient.NationalIdCiphertext = _encryptionService.Encrypt(nationalId.Trim());
    }

    public void AttachPassport(Patient patient, string passport)
    {
        if (string.IsNullOrWhiteSpace(passport)) return;

        patient.PassportHmac = _hmacService.ComputeHmac(passport);
        patient.PassportCiphertext = _encryptionService.Encrypt(passport.Trim());
    }

    public void AttachInsuranceCard(Patient patient, string insuranceCard)
    {
        if (string.IsNullOrWhiteSpace(insuranceCard)) return;

        patient.InsuranceCardHmac = _hmacService.ComputeHmac(insuranceCard);
        patient.InsuranceCardCiphertext = _encryptionService.Encrypt(insuranceCard.Trim());
    }

    public void AttachPid(Patient patient, string pid)
    {
        if (string.IsNullOrWhiteSpace(pid)) return;

        patient.PidHmac = _hmacService.ComputeHmac(pid);
        patient.PidCiphertext = _encryptionService.Encrypt(pid.Trim());
    }

    public string DecryptNationalId(Patient patient)
    {
        return patient.NationalIdCiphertext != null
            ? _encryptionService.Decrypt(patient.NationalIdCiphertext)
            : string.Empty;
    }

    public string DecryptPassport(Patient patient)
    {
        return patient.PassportCiphertext != null
            ? _encryptionService.Decrypt(patient.PassportCiphertext)
            : string.Empty;
    }

    public string DecryptInsuranceCard(Patient patient)
    {
        return patient.InsuranceCardCiphertext != null
            ? _encryptionService.Decrypt(patient.InsuranceCardCiphertext)
            : string.Empty;
    }

    public async Task<Patient?> FindByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nationalId)) return null;

        var hmac = _hmacService.ComputeHmac(nationalId);
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.NationalIdHmac == hmac && p.RecordStatus != "MERGED", cancellationToken);
    }

    public async Task<Patient?> FindByPassportAsync(string passport, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(passport)) return null;

        var hmac = _hmacService.ComputeHmac(passport);
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.PassportHmac == hmac && p.RecordStatus != "MERGED", cancellationToken);
    }

    public async Task<Patient?> FindByInsuranceCardAsync(string insuranceCard, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(insuranceCard)) return null;

        var hmac = _hmacService.ComputeHmac(insuranceCard);
        return await _context.Patients
            .FirstOrDefaultAsync(p => p.InsuranceCardHmac == hmac && p.RecordStatus != "MERGED", cancellationToken);
    }

    public async Task CheckAndFlagDuplicateIdentifierAsync(
        Patient patient, 
        string identifierType, 
        string rawValue, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawValue)) return;

        var hmac = _hmacService.ComputeHmac(rawValue);
        var maskedVal = _maskingService.MaskIdentifier(rawValue, identifierType);

        // Tìm hồ sơ khác có cùng HMAC định danh mạnh
        var existingCandidate = await _context.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => 
                p.Id != patient.Id &&
                p.RecordStatus != "MERGED" &&
                ((identifierType == "NATIONAL_ID" && p.NationalIdHmac == hmac) ||
                 (identifierType == "PASSPORT" && p.PassportHmac == hmac)),
                cancellationToken);

        if (existingCandidate != null)
        {
            patient.IsPossibleDuplicate = true;
            patient.RecordStatus = "REVIEW_REQUIRED";

            // Tuyệt đối không log raw identifier, chỉ log masked identifier
            _logger.LogWarning(
                "Phát hiện định danh mạnh {Type} đã tồn tại trong hệ thống (Giá trị che giấu: {MaskedValue}, Hồ sơ gốc: {OriginalPatientCode}). " +
                "Đánh dấu hồ sơ mới {NewPatientCode} trạng thái REVIEW_REQUIRED để nhân viên kiểm tra, KHÔNG tự ý gộp hồ sơ.",
                identifierType, maskedVal, existingCandidate.PatientCode, patient.PatientCode);
        }
    }
}
