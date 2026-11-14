using System.Security.Cryptography;
using System.Text;
using ClinicAppointment.Domain.Entities;
using ClinicAppointment.Infrastructure.Security;
using FluentAssertions;
using Xunit;

namespace ClinicAppointment.Domain.Tests;

public class PatientSecurityTests
{
    private static readonly byte[] TestEncryptionKey = SHA256.HashData(Encoding.UTF8.GetBytes("Test_Encryption_Secret_Key_32B!"));
    private static readonly byte[] TestHmacKey1 = SHA256.HashData(Encoding.UTF8.GetBytes("Test_HMAC_Secret_Key_Number_1!"));
    private static readonly byte[] TestHmacKey2 = SHA256.HashData(Encoding.UTF8.GetBytes("Test_HMAC_Secret_Key_Number_2!"));

    private readonly PiiEncryptionService _encryptionService = new(TestEncryptionKey);
    private readonly HmacService _hmacService1 = new(TestHmacKey1);
    private readonly HmacService _hmacService2 = new(TestHmacKey2);
    private readonly IdentifierMaskingService _maskingService = new();

    #region 1. Sensitive Identifier Encryption (AES-256-GCM)

    [Theory]
    [InlineData("001092001234")]               // CCCD 12 số
    [InlineData("B1234567")]                   // Hộ chiếu
    [InlineData("GD4010123456789")]            // Thẻ BHYT 15 ký tự
    [InlineData("0912345678")]                 // Số điện thoại
    public void AesGcm_EncryptAndDecrypt_ShouldRestoreOriginalPlainText(string plainText)
    {
        // Act
        byte[] cipherBytes = _encryptionService.Encrypt(plainText);
        string decrypted = _encryptionService.Decrypt(cipherBytes);

        // Assert
        cipherBytes.Should().NotBeNullOrEmpty();
        cipherBytes.Length.Should().BeGreaterThan(28); // 12B nonce + 16B tag + cipher
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void AesGcm_Encrypt_TwiceWithSamePlainText_ShouldProduceDifferentCipherBytes_DueToRandomNonce()
    {
        var rawCccd = "001092001234";

        byte[] cipher1 = _encryptionService.Encrypt(rawCccd);
        byte[] cipher2 = _encryptionService.Encrypt(rawCccd);

        cipher1.Should().NotEqual(cipher2); // Nonce ngẫu nhiên mỗi lần mã hóa
        _encryptionService.Decrypt(cipher1).Should().Be(rawCccd);
        _encryptionService.Decrypt(cipher2).Should().Be(rawCccd);
    }

    [Fact]
    public void AesGcm_Decrypt_WithTamperedCipherBytes_ShouldThrowCryptographicException()
    {
        var rawCccd = "001092001234";
        byte[] cipher = _encryptionService.Encrypt(rawCccd);

        // Làm giả/thay đổi 1 byte trong ciphertext
        cipher[^1] ^= 0xFF;

        var act = () => _encryptionService.Decrypt(cipher);
        act.Should().Throw<CryptographicException>(); // Xác thực tính toàn vẹn (Auth Tag) thất bại
    }

    #endregion

    #region 2. Keyed HMAC Indexing & Normalization (Not Plain Hash)

    [Fact]
    public void Hmac_DifferentKeys_ShouldProduceDifferentHashes_ForSameIdentifier()
    {
        var cccd = "001092001234";

        var hmac1 = _hmacService1.ComputeHmac(cccd);
        var hmac2 = _hmacService2.ComputeHmac(cccd);

        hmac1.Should().NotBeEmpty();
        hmac2.Should().NotBeEmpty();
        hmac1.Should().NotBe(hmac2); // Keyed HMAC độc lập với plain SHA256
    }

    [Theory]
    [InlineData("001092001234", "001 092 001 234")]
    [InlineData("001092001234", "001-092-001234")]
    [InlineData("001092001234", "  001.092.001234  ")]
    [InlineData("B1234567", "b1234567")]
    [InlineData("GD401-0123456789", "gd401 0123456789")]
    public void Hmac_NormalizedValues_ShouldProduceIdenticalHmac_ForExactSearch(string val1, string val2)
    {
        var hmac1 = _hmacService1.ComputeHmac(val1);
        var hmac2 = _hmacService1.ComputeHmac(val2);

        hmac1.Should().Be(hmac2);
        _hmacService1.VerifyHmac(val2, hmac1).Should().BeTrue();
    }

    #endregion

    #region 3. Identifier Masking (Không log raw identifier)

    [Theory]
    [InlineData("0912345678", "091***5678")]
    [InlineData("0987654321", "098***4321")]
    public void MaskPhone_ShouldMaskMiddleDigits_AndKeepPrefixSuffix(string raw, string expected)
    {
        var masked = _maskingService.MaskPhone(raw);
        masked.Should().Be(expected);
        masked.Should().NotContain(raw); // Không để lộ raw phone đầy đủ
    }

    [Theory]
    [InlineData("001092001234", "0010******34")]
    [InlineData("079085004321", "0790******21")]
    public void MaskNationalId_ShouldMaskMiddleDigits_AndKeep4Prefix2Suffix(string raw, string expected)
    {
        var masked = _maskingService.MaskNationalId(raw);
        masked.Should().Be(expected);
        masked.Should().NotContain(raw);
    }

    [Theory]
    [InlineData("B1234567", "B12***67")]
    [InlineData("C9876543", "C98***43")]
    public void MaskPassport_ShouldMaskMiddleCharacters(string raw, string expected)
    {
        var masked = _maskingService.MaskPassport(raw);
        masked.Should().Be(expected);
    }

    [Theory]
    [InlineData("GD4010123456789", "GD401******89")]
    public void MaskHealthInsurance_ShouldMaskMiddleCode(string raw, string expected)
    {
        var masked = _maskingService.MaskHealthInsurance(raw);
        masked.Should().Be(expected);
    }

    #endregion

    #region 4. Patient Entity Integration with Ciphertext & HMAC

    [Fact]
    public void Patient_AttachIdentifiers_ShouldStoreCiphertextAndHmac_WithoutPlainText()
    {
        var patient = Patient.Create("Nguyen Van A", "0912345678", new DateOnly(1985, 3, 10));
        var rawCccd = "001085009988";

        // Gán CCCD mã hóa
        patient.NationalIdHmac = _hmacService1.ComputeHmac(rawCccd);
        patient.NationalIdCiphertext = _encryptionService.Encrypt(rawCccd);

        // Kiểm tra
        patient.NationalIdHmac.Should().NotBeNullOrEmpty();
        patient.NationalIdCiphertext.Should().NotBeNullOrEmpty();

        // Không chứa raw string trong byte[] hay HMAC
        Encoding.UTF8.GetString(patient.NationalIdCiphertext).Should().NotContain(rawCccd);

        // Giải mã khôi phục chính xác
        var decrypted = _encryptionService.Decrypt(patient.NationalIdCiphertext);
        decrypted.Should().Be(rawCccd);
    }

    #endregion
}
