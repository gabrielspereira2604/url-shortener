using UrlShortener.ShortCode;

namespace UrlShortener.Tests.ShortCode;

public class Base62EncoderTests
{
    [Theory]
    [InlineData(1, "1")]
    [InlineData(62, "10")]
    [InlineData(3844, "100")]  // 62^2
    [InlineData(238328, "1000")]  // 62^3
    public void Encode_KnownValues_ReturnsExpectedCode(long value, string expected)
    {
        var result = Base62Encoder.Encode(value);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("10", 62)]
    [InlineData("100", 3844)]
    [InlineData("1000", 238328)]
    public void Decode_KnownCodes_ReturnsExpectedValue(string code, long expected)
    {
        var result = Base62Encoder.Decode(code);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(999999)]
    [InlineData(long.MaxValue / 2)]
    public void Encode_ThenDecode_ReturnsOriginalValue(long value)
    {
        var code = Base62Encoder.Encode(value);
        var decoded = Base62Encoder.Decode(code);
        Assert.Equal(value, decoded);
    }

    [Fact]
    public void Encode_Zero_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Base62Encoder.Encode(0));
    }

    [Fact]
    public void Encode_Negative_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Base62Encoder.Encode(-1));
    }

    [Fact]
    public void Decode_EmptyString_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Base62Encoder.Decode(""));
    }

    [Fact]
    public void Decode_InvalidCharacter_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Base62Encoder.Decode("ab!cd"));
    }
}
