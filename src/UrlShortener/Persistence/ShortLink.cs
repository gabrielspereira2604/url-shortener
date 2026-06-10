namespace UrlShortener.Persistence;

public class ShortLink
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public long HitCount { get; set; }

    public bool IsExpired() => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;
}
