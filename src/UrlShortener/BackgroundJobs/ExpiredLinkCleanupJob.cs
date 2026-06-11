using Microsoft.EntityFrameworkCore;
using UrlShortener.Persistence;

namespace UrlShortener.BackgroundJobs;

public class ExpiredLinkCleanupJob(IServiceScopeFactory scopeFactory, ILogger<ExpiredLinkCleanupJob> logger)
    : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await CleanupAsync(ct);
            await Task.Delay(Interval, ct);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var deleted = await db.ShortLinks
            .Where(l => l.ExpiresAt != null && l.ExpiresAt < DateTimeOffset.UtcNow)
            .ExecuteDeleteAsync(ct);

        if (deleted > 0)
            logger.LogInformation("Deleted {Count} expired links.", deleted);
    }
}
