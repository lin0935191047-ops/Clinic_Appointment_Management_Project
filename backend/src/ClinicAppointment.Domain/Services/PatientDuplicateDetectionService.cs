using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Domain.Enums;

namespace ClinicAppointment.Domain.Services;

public class CandidateMatch
{
    public Patient ExistingPatient { get; set; } = null!;
    public IdentityMatchReason Reason { get; set; }
    public bool RequiresManualReview { get; set; }
    public string Description { get; set; } = string.Empty;
}

public interface IPatientDuplicateDetectionService
{
    List<CandidateMatch> EvaluateCandidates(Patient targetPatient, IEnumerable<Patient> candidates);
}

public class PatientDuplicateDetectionService : IPatientDuplicateDetectionService
{
    public List<CandidateMatch> EvaluateCandidates(Patient targetPatient, IEnumerable<Patient> candidates)
    {
        var matches = new List<CandidateMatch>();

        foreach (var existing in candidates)
        {
            if (existing.Id == targetPatient.Id || existing.RecordStatus == "MERGED")
                continue;

            // 1. Kiểm tra Định danh mạnh (CCCD hoặc Hộ chiếu đã băm HMAC)
            if (!string.IsNullOrEmpty(targetPatient.NationalIdHmac) &&
                !string.IsNullOrEmpty(existing.NationalIdHmac) &&
                targetPatient.NationalIdHmac == existing.NationalIdHmac)
            {
                matches.Add(new CandidateMatch
                {
                    ExistingPatient = existing,
                    Reason = IdentityMatchReason.VERIFIED_IDENTIFIER,
                    RequiresManualReview = true,
                    Description = "Trùng khớp định danh mạnh CCCD/CMND. Cần nhân sự xác minh, không tự động gộp."
                });
                continue;
            }

            if (!string.IsNullOrEmpty(targetPatient.PassportHmac) &&
                !string.IsNullOrEmpty(existing.PassportHmac) &&
                targetPatient.PassportHmac == existing.PassportHmac)
            {
                matches.Add(new CandidateMatch
                {
                    ExistingPatient = existing,
                    Reason = IdentityMatchReason.VERIFIED_IDENTIFIER,
                    RequiresManualReview = true,
                    Description = "Trùng khớp định danh Hộ chiếu. Cần nhân sự xác minh, không tự động gộp."
                });
                continue;
            }

            // 2. Kiểm tra Họ tên chuẩn hóa + Ngày sinh đầy đủ (NAME_DOB)
            if (targetPatient.NormalizedFullName == existing.NormalizedFullName &&
                targetPatient.DateOfBirth.HasValue && existing.DateOfBirth.HasValue &&
                targetPatient.DateOfBirth.Value == existing.DateOfBirth.Value)
            {
                matches.Add(new CandidateMatch
                {
                    ExistingPatient = existing,
                    Reason = IdentityMatchReason.NAME_DOB,
                    RequiresManualReview = true,
                    Description = "Trùng Họ tên chuẩn hóa và Ngày tháng năm sinh. Đánh dấu nghi trùng, tuyệt đối không tự động gộp."
                });
                continue;
            }

            // 3. Kiểm tra Họ tên chuẩn hóa + Năm sinh (NAME_BIRTH_YEAR)
            if (targetPatient.NormalizedFullName == existing.NormalizedFullName &&
                targetPatient.BirthYear.HasValue && existing.BirthYear.HasValue &&
                targetPatient.BirthYear.Value == existing.BirthYear.Value)
            {
                matches.Add(new CandidateMatch
                {
                    ExistingPatient = existing,
                    Reason = IdentityMatchReason.NAME_BIRTH_YEAR,
                    RequiresManualReview = true,
                    Description = "Trùng Họ tên chuẩn hóa và Năm sinh. Đánh dấu nghi trùng, tuyệt đối không tự động gộp."
                });
                continue;
            }

            // 4. Kiểm tra Số điện thoại dùng chung nhưng khác Họ tên (SHARED_PHONE_DIFFERENT_NAME)
            if (!string.IsNullOrEmpty(targetPatient.PhoneNumber) &&
                !string.IsNullOrEmpty(existing.PhoneNumber) &&
                targetPatient.PhoneNumber == existing.PhoneNumber &&
                targetPatient.NormalizedFullName != existing.NormalizedFullName)
            {
                matches.Add(new CandidateMatch
                {
                    ExistingPatient = existing,
                    Reason = IdentityMatchReason.SHARED_PHONE_DIFFERENT_NAME,
                    RequiresManualReview = false, // An toàn: Đây là số dùng chung gia đình/người giám hộ
                    Description = "Số điện thoại dùng chung giữa các cá nhân có họ tên khác nhau (người giám hộ/người thân). An toàn lưu trữ độc lập, không gộp."
                });
            }
        }

        return matches;
    }
}
