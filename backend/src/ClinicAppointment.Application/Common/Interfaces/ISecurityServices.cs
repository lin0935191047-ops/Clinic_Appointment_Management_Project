namespace ClinicAppointment.Application.Common.Interfaces;

public interface IPiiEncryptionService
{
    byte[] Encrypt(string plainText);
    string Decrypt(byte[] cipherBytes);
}

public interface IHmacService
{
    string ComputeHmac(string rawValue);
    bool VerifyHmac(string rawValue, string expectedHmac);
}

public interface IIdentifierMaskingService
{
    string MaskPhone(string phone);
    string MaskNationalId(string nationalId);
    string MaskPassport(string passport);
    string MaskHealthInsurance(string healthInsurance);
    string MaskIdentifier(string identifier, string type);
}
