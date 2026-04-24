using System.Security.Cryptography;

namespace Intellect.Erp.Observability.AspNetCore.Internal;

/// <summary>
/// Generates ULID-compatible 26-character Crockford Base32 identifiers
/// from a millisecond timestamp (48 bits) + 80 random bits.
/// </summary>
internal static class UlidGenerator
{
    // Crockford Base32 alphabet (excludes I, L, O, U)
    private const string CrockfordBase32 = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";

    /// <summary>
    /// Generates a new ULID-26 string: 10 chars timestamp + 16 chars randomness.
    /// </summary>
    public static string NewUlid()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        Span<char> result = stackalloc char[26];

        // Encode 48-bit timestamp into 10 Crockford Base32 characters (most significant first)
        for (var i = 9; i >= 0; i--)
        {
            result[i] = CrockfordBase32[(int)(timestamp & 0x1F)];
            timestamp >>= 5;
        }

        // Encode 80 random bits into 16 Crockford Base32 characters
        Span<byte> randomBytes = stackalloc byte[10];
        RandomNumberGenerator.Fill(randomBytes);

        // Process 10 random bytes → 16 base32 chars
        // We need 80 bits → 16 * 5 = 80 bits, perfect fit
        var bitBuffer = 0UL;
        var bitsInBuffer = 0;
        var charIndex = 10;

        for (var i = 0; i < randomBytes.Length && charIndex < 26; i++)
        {
            bitBuffer = (bitBuffer << 8) | randomBytes[i];
            bitsInBuffer += 8;

            while (bitsInBuffer >= 5 && charIndex < 26)
            {
                bitsInBuffer -= 5;
                result[charIndex++] = CrockfordBase32[(int)((bitBuffer >> bitsInBuffer) & 0x1F)];
            }
        }

        return new string(result);
    }
}
