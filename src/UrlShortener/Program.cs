using Microsoft.EntityFrameworkCore;
using UrlShortener.Features;
using UrlShortener.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Database")));

builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis"));

builder.Services.AddScoped<ShortLinkService>();

var app = builder.Build();

app.UseHttpsRedirection();

app.MapPost("/shorten", async (ShortenLinkRequest request, ShortLinkService service, CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request.Url) || !Uri.TryCreate(request.Url, UriKind.Absolute, out _))
        return Results.BadRequest("Invalid URL.");

    var link = await service.CreateAsync(request.Url, request.ExpiresAt, ct);

    var shortUrl = $"{app.Urls.FirstOrDefault() ?? "https://localhost"}/{link.Code}";
    return Results.Ok(new { code = link.Code, shortUrl, originalUrl = link.OriginalUrl });
});

app.MapGet("/{code}", async (string code, ShortLinkService service, CancellationToken ct) =>
{
    var link = await service.ResolveAsync(code, ct);

    if (link is null)
        return Results.NotFound();

    return Results.Redirect(link.OriginalUrl, permanent: false);
});

app.Run();
