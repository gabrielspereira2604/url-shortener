using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;
using UrlShortener.Persistence;

namespace UrlShortener.Tests.Integration;

public class UrlShortenerWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private readonly RedisContainer _redis = new RedisBuilder()
        .WithImage("redis:7-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var dbDescriptor = services.Single(s => s.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(dbDescriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            var redisDescriptor = services.SingleOrDefault(s =>
                s.ImplementationType?.FullName?.Contains("RedisCache") == true);
            if (redisDescriptor is not null) services.Remove(redisDescriptor);
            services.AddStackExchangeRedisCache(options =>
                options.Configuration = _redis.GetConnectionString());
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await _redis.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await _redis.DisposeAsync();
    }
}
