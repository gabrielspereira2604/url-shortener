using System.Net;
using System.Net.Http.Json;

namespace UrlShortener.Tests.Integration;

public class ShortLinkEndpointTests(UrlShortenerWebFactory factory)
    : IClassFixture<UrlShortenerWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    [Fact]
    public async Task PostShorten_ValidUrl_ReturnsCode()
    {
        var response = await _client.PostAsJsonAsync("/shorten", new
        {
            url = "https://google.com",
            expiresAt = (DateTimeOffset?)null
        });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<ShortenResponse>();
        Assert.NotNull(body);
        Assert.Equal(6, body!.Code.Length);
        Assert.Contains(body.Code, body.ShortUrl);
    }

    [Fact]
    public async Task PostShorten_InvalidUrl_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/shorten", new
        {
            url = "not-a-url",
            expiresAt = (DateTimeOffset?)null
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetCode_ExistingCode_ReturnsRedirect()
    {
        var createResponse = await _client.PostAsJsonAsync("/shorten", new
        {
            url = "https://github.com",
            expiresAt = (DateTimeOffset?)null
        });
        var body = await createResponse.Content.ReadFromJsonAsync<ShortenResponse>();

        var redirectResponse = await _client.GetAsync($"/{body!.Code}");

        Assert.Equal(HttpStatusCode.Redirect, redirectResponse.StatusCode);
        Assert.Equal("https://github.com", redirectResponse.Headers.Location?.ToString());
    }

    [Fact]
    public async Task GetCode_NonExistentCode_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/doesnotexist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetCode_ExpiredLink_ReturnsNotFound()
    {
        var createResponse = await _client.PostAsJsonAsync("/shorten", new
        {
            url = "https://expired.com",
            expiresAt = DateTimeOffset.UtcNow.AddSeconds(-1)
        });
        var body = await createResponse.Content.ReadFromJsonAsync<ShortenResponse>();

        var response = await _client.GetAsync($"/{body!.Code}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private record ShortenResponse(string Code, string ShortUrl, string OriginalUrl);
}
