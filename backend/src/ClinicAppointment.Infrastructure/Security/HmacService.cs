using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ClinicAppointment.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ClinicAppointment.Infrastructure.Security;

public class HmacService : IHmacService
{
    private readonly byte[] _key;

    public HmacService(IConfiguration configuration)
    {
        var keyString = configuration["HMAC_SECRET_KEY"] 
            ?? configuration["PiiSettings:HmacKey"]
            ?? "ClinicAppointment_HMAC_SecretKey_ExactLookup32B!";

        var rawBytes = Encoding.UTF8.GetBytes(keyString);
        _key = rawBytes.Length == 32 ? rawBytes : SHA256.HashData(rawBytes);
    }

    public HmacService(byte[] key)
    {
        if (key.Length < 16)
            throw new ArgumentException("HMAC key cần tối thiểu 16 bytes.", nameof(key));

        _key = key;
    }

    public string ComputeHmac(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        // Chuẩn hóa: loại bỏ khoảng trắng, dấu gạch nối, dấu chấm, dấu xuyệt và chuyển thành chữ in hoa
        var normalized = NormalizeIdentifier(rawValue);
        byte[] dataBytes = Encoding.UTF8.GetBytes(normalized);

        using var hmac = new HMACSHA256(_key);
        byte[] hash = hmac.ComputeHash(dataBytes);

        // Trả về dạng hex chuỗi chữ thường để đánh index nhất quán trong database
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool VerifyHmac(string rawValue, string expectedHmac)
    {
        if (string.IsNullOrWhiteSpace(rawValue) || string.IsNullOrWhiteSpace(expectedHmac))
            return false;

        var computed = ComputeHmac(rawValue);
        byte[] a = Encoding.UTF8.GetBytes(computed);
        byte[] b = Encoding.UTF8.GetBytes(expectedHmac.Trim().ToLowerInvariant());

        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    private static string NormalizeIdentifier(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // Xóa sạch khoảng trắng, dấu gạch ngang, dấu chấm, dấu gạch chéo
        return Regex.Replace(input.Trim(), @"[\s\-\./\\]", "").ToUpperInvariant();
    }
}
