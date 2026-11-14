using System.Security.Cryptography;
using System.Text;
using ClinicAppointment.Domain.Common;

namespace ClinicAppointment.Domain.Entities;

public class IdempotencyRecord : BaseEntity
{
    public string Scope { get; set; } = string.Empty;
    public byte[] KeyHash { get; set; } = Array.Empty<byte>();
    public byte[] RequestHash { get; set; } = Array.Empty<byte>();
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public short? ResponseStatusCode { get; set; }
    public string? ResponseBodyJson { get; set; }
    public DateTime ExpiresAt { get; set; }

    public static byte[] ComputeHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        return SHA256.HashData(bytes);
    }

    public static IdempotencyRecord Create(
        string scope,
        string idempotencyKey,
        string requestPayload,
        string? resourceType = null,
        Guid? resourceId = null,
        short responseStatusCode = 200,
        string? responseBodyJson = null,
        TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(scope))
            throw new ArgumentException("Scope không được để trống.", nameof(scope));

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("IdempotencyKey không được để trống.", nameof(idempotencyKey));

        return new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Scope = scope.Trim(),
            KeyHash = ComputeHash(idempotencyKey.Trim()),
            RequestHash = ComputeHash((requestPayload ?? string.Empty).Trim()),
            ResourceType = resourceType?.Trim(),
            ResourceId = resourceId,
            ResponseStatusCode = responseStatusCode,
            ResponseBodyJson = responseBodyJson,
            ExpiresAt = DateTime.UtcNow.Add(ttl ?? TimeSpan.FromHours(24)),
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool MatchesRequest(string requestPayload)
    {
        var currentHash = ComputeHash((requestPayload ?? string.Empty).Trim());
        return RequestHash.SequenceEqual(currentHash);
    }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
}
