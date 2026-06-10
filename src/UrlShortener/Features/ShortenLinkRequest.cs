namespace UrlShortener.Features;

public record ShortenLinkRequest(string Url, DateTimeOffset? ExpiresAt);
