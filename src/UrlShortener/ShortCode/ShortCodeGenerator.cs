using System.Security.Cryptography;

namespace UrlShortener.ShortCode;

public static class ShortCodeGenerator
{
    private const string Alphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const int CodeLength = 6;

    public static string Generate()
    {
        var code = new char[CodeLength];

        for (var i = 0; i < CodeLength; i++)
            code[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(code);
    }
}
