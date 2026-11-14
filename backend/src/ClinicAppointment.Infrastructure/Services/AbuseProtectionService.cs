using ClinicAppointment.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace ClinicAppointment.Infrastructure.Services;

public class AbuseProtectionService : IAbuseProtectionService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(5);
    private const int MaxAttemptsPerPhone = 3;
    private const int MaxAttemptsPerIp = 10;

    public AbuseProtectionService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool IsBookingRateLimitExceeded(string clientIp, string phoneNumber)
    {
        var now = DateTime.UtcNow;

        // 1. Kiểm tra giới hạn theo Số điện thoại
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneKey = GetPhoneCacheKey(phoneNumber);
            if (_cache.TryGetValue<List<DateTime>>(phoneKey, out var phoneAttempts) && phoneAttempts != null)
            {
                var recentPhoneAttempts = phoneAttempts.Where(t => now - t < WindowDuration).Count();
                if (recentPhoneAttempts >= MaxAttemptsPerPhone)
                {
                    return true;
                }
            }
        }

        // 2. Kiểm tra giới hạn theo Địa chỉ IP
        if (!string.IsNullOrWhiteSpace(clientIp) && clientIp != "unknown")
        {
            var ipKey = GetIpCacheKey(clientIp);
            if (_cache.TryGetValue<List<DateTime>>(ipKey, out var ipAttempts) && ipAttempts != null)
            {
                var recentIpAttempts = ipAttempts.Where(t => now - t < WindowDuration).Count();
                if (recentIpAttempts >= MaxAttemptsPerIp)
                {
                    return true;
                }
            }
        }

        return false;
    }

    public void RecordBookingAttempt(string clientIp, string phoneNumber)
    {
        var now = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            var phoneKey = GetPhoneCacheKey(phoneNumber);
            var phoneAttempts = _cache.GetOrCreate(phoneKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return new List<DateTime>();
            }) ?? new List<DateTime>();

            lock (phoneAttempts)
            {
                phoneAttempts.RemoveAll(t => now - t >= WindowDuration);
                phoneAttempts.Add(now);
            }
        }

        if (!string.IsNullOrWhiteSpace(clientIp) && clientIp != "unknown")
        {
            var ipKey = GetIpCacheKey(clientIp);
            var ipAttempts = _cache.GetOrCreate(ipKey, entry =>
            {
                entry.SlidingExpiration = TimeSpan.FromMinutes(10);
                return new List<DateTime>();
            }) ?? new List<DateTime>();

            lock (ipAttempts)
            {
                ipAttempts.RemoveAll(t => now - t >= WindowDuration);
                ipAttempts.Add(now);
            }
        }
    }

    public void ResetRateLimit(string clientIp, string phoneNumber)
    {
        if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            _cache.Remove(GetPhoneCacheKey(phoneNumber));
        }

        if (!string.IsNullOrWhiteSpace(clientIp))
        {
            _cache.Remove(GetIpCacheKey(clientIp));
        }
    }

    private static string GetPhoneCacheKey(string phone) => $"ratelimit:booking:phone:{phone.Trim()}";
    private static string GetIpCacheKey(string ip) => $"ratelimit:booking:ip:{ip.Trim()}";
}
