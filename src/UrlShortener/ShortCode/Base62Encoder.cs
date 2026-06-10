namespace UrlShortener.ShortCode;

public static class Base62Encoder
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int Base = 62;

    public static string Encode(long value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Value must be positive.");

        // 62^11 > long.MaxValue, so 11 chars is always enough
        var result = new char[11];
        var index = result.Length;

        while (value > 0)
        {
            result[--index] = Alphabet[(int)(value % Base)];
            value /= Base;
        }

        return new string(result, index, result.Length - index);
    }

    public static long Decode(string code)
    {
        if (string.IsNullOrEmpty(code))
            throw new ArgumentException("Code cannot be null or empty.", nameof(code));

        long result = 0;

        foreach (var ch in code)
        {
            var digit = Alphabet.IndexOf(ch);
            if (digit < 0)
                throw new ArgumentException($"Invalid character '{ch}' in code.", nameof(code));

            result = result * Base + digit;
        }

        return result;
    }
}
