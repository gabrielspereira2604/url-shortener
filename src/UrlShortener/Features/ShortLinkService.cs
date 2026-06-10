using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using UrlShortener.Persistence;
using UrlShortener.ShortCode;

namespace UrlShortener.Features;

public class ShortLinkService(AppDbContext db, IDistributedCache cache)
{
    private const int MaxCollisionRetries = 5;
    private const string CacheKeyPrefix = "shortlink:";

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
        var cached = await cache.GetStringAsync(CacheKeyPrefix + code, ct);
        if (cached is not null)
            return new ShortLink { Code = code, OriginalUrl = cached };

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
        var options = new DistributedCacheEntryOptions();

        if (link.ExpiresAt.HasValue)
            options.SetAbsoluteExpiration(link.ExpiresAt.Value);

        // No TTL for permanent links — Redis manages eviction via allkeys-lfu policy.
        // Frequently accessed links stay in cache; unused ones are evicted when memory is full.
        await cache.SetStringAsync(CacheKeyPrefix + link.Code, link.OriginalUrl, options, ct);
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ?? false;
}
