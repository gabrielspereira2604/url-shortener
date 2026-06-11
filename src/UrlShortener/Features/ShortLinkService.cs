using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using UrlShortener.Persistence;
using UrlShortener.ShortCode;

namespace UrlShortener.Features;

public class ShortLinkService(AppDbContext db, IMemoryCache l1, IDistributedCache l2, ILogger<ShortLinkService> logger)
{
    private const int MaxCollisionRetries = 5;
    private const string CacheKeyPrefix = "shortlink:";
    private static readonly TimeSpan L1Ttl = TimeSpan.FromSeconds(30);

    public async Task<ShortLink> CreateAsync(string originalUrl, DateTimeOffset? expiresAt, CancellationToken ct)
    {
        for (var attempt = 0; attempt < MaxCollisionRetries; attempt++)
        {
            var code = ShortCodeGenerator.Generate();

            var link = new ShortLink
            {
                Code = code,
                OriginalUrl = originalUrl,
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = expiresAt
            };

            try
            {
                db.ShortLinks.Add(link);
                await db.SaveChangesAsync(ct);
                await SetCacheAsync(link, ct);
                return link;
            }
            catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
            {
                db.ChangeTracker.Clear();
            }
        }

        throw new InvalidOperationException("Failed to generate a unique short code after multiple attempts.");
    }

    public async Task<ShortLink?> ResolveAsync(string code, CancellationToken ct)
    {
        var key = CacheKeyPrefix + code;

        if (l1.TryGetValue(key, out string? l1Url))
            return new ShortLink { Code = code, OriginalUrl = l1Url! };

        try
        {
            var l2Url = await l2.GetStringAsync(key, ct);
            if (l2Url is not null)
            {
                SetL1(key, l2Url, expiresAt: null);
                return new ShortLink { Code = code, OriginalUrl = l2Url };
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable, falling back to database for code {Code}.", code);
        }

        var link = await db.ShortLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Code == code, ct);

        if (link is null || link.IsExpired())
            return null;

        await SetCacheAsync(link, ct);
        return link;
    }

    private async Task SetCacheAsync(ShortLink link, CancellationToken ct)
    {
        var key = CacheKeyPrefix + link.Code;
        var options = new DistributedCacheEntryOptions();

        if (link.ExpiresAt.HasValue)
            options.SetAbsoluteExpiration(link.ExpiresAt.Value);

        // No TTL for permanent links — Redis manages eviction via allkeys-lru policy.
        try
        {
            await l2.SetStringAsync(key, link.OriginalUrl, options, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Redis unavailable, skipping cache write for code {Code}.", link.Code);
        }

        SetL1(key, link.OriginalUrl, link.ExpiresAt);
    }

    private void SetL1(string key, string url, DateTimeOffset? expiresAt)
    {
        // Respect the link's expiration so L1 doesn't outlive an expired link.
        var ttl = L1Ttl;
        if (expiresAt.HasValue)
        {
            var remaining = expiresAt.Value - DateTimeOffset.UtcNow;
            if (remaining <= TimeSpan.Zero) return;
            ttl = remaining < L1Ttl ? remaining : L1Ttl;
        }

        l1.Set(key, url, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ttl
        });
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ?? false;
}
