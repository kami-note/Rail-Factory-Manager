using System.Security.Cryptography;

namespace RailFactory.Iam.Api.Application.Mfa;

/// <summary>
/// RFC 6238 TOTP implementation. No external dependencies required.
/// </summary>
public static class TotpService
{
    private const int Digits = 6;
    private const int Period = 30;
    private const int WindowSize = 1; // accept ±1 period to tolerate clock skew

    public static string GenerateSecretBase32()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        return Base32Encode(bytes);
    }

    public static bool Verify(string secretBase32, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits)
            return false;

        if (!int.TryParse(code, out var inputCode))
            return false;

        var secret = Base32Decode(secretBase32);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / Period;

        for (var w = -WindowSize; w <= WindowSize; w++)
        {
            if (GenerateCode(secret, counter + w) == inputCode)
                return true;
        }

        return false;
    }

    /// <summary>Returns an otpauth:// URI for QR code display.</summary>
    public static string GetOtpAuthUri(string secretBase32, string email, string issuer = "RailFactory")
    {
        Func<string, string> enc = Uri.EscapeDataString;
        return $"otpauth://totp/{enc(issuer)}:{enc(email)}?secret={secretBase32}&issuer={enc(issuer)}&digits={Digits}&period={Period}";
    }

    private static int GenerateCode(byte[] secret, long counter)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0f;
        var truncated = ((hash[offset] & 0x7f) << 24)
                      | ((hash[offset + 1] & 0xff) << 16)
                      | ((hash[offset + 2] & 0xff) << 8)
                      |  (hash[offset + 3] & 0xff);

        return truncated % (int)Math.Pow(10, Digits);
    }

    private static string Base32Encode(byte[] data)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var sb = new System.Text.StringBuilder();
        int buffer = data[0], next = 1, bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xff;
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }
            int index = 0x1f & (buffer >> (bitsLeft - 5));
            bitsLeft -= 5;
            sb.Append(alphabet[index]);
        }
        return sb.ToString();
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var input = base32.TrimEnd('=').ToUpperInvariant();
        var output = new byte[input.Length * 5 / 8];
        int buffer = 0, bitsLeft = 0, index = 0;

        foreach (var c in input)
        {
            int value = alphabet.IndexOf(c);
            if (value < 0) continue;
            buffer = (buffer << 5) | value;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                output[index++] = (byte)(buffer >> (bitsLeft - 8));
                bitsLeft -= 8;
            }
        }
        return output;
    }
}
