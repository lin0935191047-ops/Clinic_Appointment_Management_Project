using ClinicAppointment.Domain.Entities;

namespace ClinicAppointment.Application.Common.Interfaces;

public interface IPatientSecurityService
{
    void AttachNationalId(Patient patient, string nationalId);
    void AttachPassport(Patient patient, string passport);
    void AttachInsuranceCard(Patient patient, string insuranceCard);
    void AttachPid(Patient patient, string pid);

    string DecryptNationalId(Patient patient);
    string DecryptPassport(Patient patient);
    string DecryptInsuranceCard(Patient patient);

    Task<Patient?> FindByNationalIdAsync(string nationalId, CancellationToken cancellationToken = default);
    Task<Patient?> FindByPassportAsync(string passport, CancellationToken cancellationToken = default);
    Task<Patient?> FindByInsuranceCardAsync(string insuranceCard, CancellationToken cancellationToken = default);

    Task CheckAndFlagDuplicateIdentifierAsync(Patient patient, string identifierType, string rawValue, CancellationToken cancellationToken = default);
}
