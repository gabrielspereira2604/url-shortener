using UrlShortener.ShortCode;

namespace UrlShortener.Tests.ShortCode;

public class ShortCodeGeneratorTests
{
    private const string ValidCharacters = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

    [Fact]
    public void Generate_ReturnsCodeWithLengthSix()
    {
        var code = ShortCodeGenerator.Generate();
        Assert.Equal(6, code.Length);
    }

    [Fact]
    public void Generate_ReturnsOnlyValidBase62Characters()
    {
        var code = ShortCodeGenerator.Generate();
        Assert.All(code, ch => Assert.Contains(ch, ValidCharacters));
    }

    [Fact]
    public void Generate_CalledMultipleTimes_ReturnsDifferentCodes()
    {
        var codes = Enumerable.Range(0, 100).Select(_ => ShortCodeGenerator.Generate()).ToHashSet();
        // With 56 billion combinations, 100 calls should always be unique
        Assert.Equal(100, codes.Count);
    }
}
