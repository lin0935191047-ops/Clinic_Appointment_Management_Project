using System.Security.Cryptography;
using System.Text;
using ClinicAppointment.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClinicAppointment.Infrastructure.Security;

public class PiiEncryptionService : IPiiEncryptionService
{
    private readonly byte[] _key;
    private const int NonceSize = 12; // 96 bits for AES-GCM
    private const int TagSize = 16;   // 128 bits auth tag

    public PiiEncryptionService(IConfiguration configuration)
    {
        var keyString = configuration["PII_ENCRYPTION_KEY"] 
            ?? configuration["PiiSettings:EncryptionKey"]
            ?? "ClinicAppointment_PII_EncryptionKey_32Bytes!";

        // Ensure key is exactly 32 bytes (256-bit)
        var rawBytes = Encoding.UTF8.GetBytes(keyString);
        if (rawBytes.Length == 32)
        {
            _key = rawBytes;
        }
        else
        {
            // Derive a deterministic 32-byte key using SHA-256
            _key = SHA256.HashData(rawBytes);
        }
    }

    public PiiEncryptionService(byte[] key)
    {
        if (key.Length != 32)
            throw new ArgumentException("Khóa mã hóa AES-256 bắt buộc phải có độ dài 32 bytes (256 bits).", nameof(key));

        _key = key;
    }

    public byte[] Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return Array.Empty<byte>();

        byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] tag = new byte[TagSize];
        byte[] cipherBytes = new byte[plainBytes.Length];

        using (var aesGcm = new AesGcm(_key, TagSize))
        {
            aesGcm.Encrypt(nonce, plainBytes, cipherBytes, tag);
        }

        // Layout: [Nonce 12B][Tag 16B][CipherBytes]
        byte[] result = new byte[NonceSize + TagSize + cipherBytes.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(tag, 0, result, NonceSize, TagSize);
        Buffer.BlockCopy(cipherBytes, 0, result, NonceSize + TagSize, cipherBytes.Length);

        return result;
    }

    public string Decrypt(byte[] cipherData)
    {
        if (cipherData == null || cipherData.Length < (NonceSize + TagSize))
            return string.Empty;

        byte[] nonce = new byte[NonceSize];
        byte[] tag = new byte[TagSize];
        int cipherLength = cipherData.Length - NonceSize - TagSize;
        byte[] cipherBytes = new byte[cipherLength];
        byte[] plainBytes = new byte[cipherLength];

        Buffer.BlockCopy(cipherData, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(cipherData, NonceSize, tag, 0, TagSize);
        Buffer.BlockCopy(cipherData, NonceSize + TagSize, cipherBytes, 0, cipherLength);

        using (var aesGcm = new AesGcm(_key, TagSize))
        {
            aesGcm.Decrypt(nonce, cipherBytes, tag, plainBytes);
        }

        return Encoding.UTF8.GetString(plainBytes);
    }
}
