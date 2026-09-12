using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// Every synthetic value the anonymiser writes is a keyed derivation of the real one: HMAC-SHA256 under a
/// private seed, so the same seed re-yields the same synthetic identifiers after a re-extraction (the
/// fixtures stay byte-stable) while nothing about a real value can be recovered from its synthetic form
/// without the seed. The seed never leaves the private repository that holds it; this class only ever
/// sees it as the HMAC key.
/// </summary>
public sealed class Keyed
{
    private readonly byte[] key;

    public Keyed(string seed)
    {
        if (string.IsNullOrWhiteSpace(seed) || seed.Trim().Length < 16)
            throw new ArgumentException("The seed must be at least 16 characters.", nameof(seed));

        key = Encoding.UTF8.GetBytes(seed.Trim());
    }

    /// <summary>A short, non-reversible identity of the seed, so a key map can say which seed produced it.</summary>
    public string Fingerprint => Convert.ToHexString(SHA256.HashData(key))[..12].ToLowerInvariant();

    public byte[] Hash(params string[] parts) =>
        HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(string.Join('\0', parts)));

    public uint Uint(params string[] parts) => BinaryPrimitives.ReadUInt32BigEndian(Hash(parts));

    /// <summary>A keyed choice among <paramref name="count"/> alternatives.</summary>
    public int Pick(int count, params string[] parts) => (int)(Uint(parts) % (uint)count);

    /// <summary>An endless keyed byte stream for one derivation (blocks of HMAC output, counter-chained).</summary>
    public IEnumerable<byte> Stream(params string[] parts)
    {
        for (var block = 0; ; block++)
        {
            var blockParts = new string[parts.Length + 1];
            parts.CopyTo(blockParts, 0);
            blockParts[^1] = "#" + block;

            foreach (var b in Hash(blockParts))
                yield return b;
        }
    }

    /// <summary>
    /// Format-preserving substitution: every letter becomes a letter of the same case, every digit a digit,
    /// everything else (dashes, spaces, punctuation) stays where it is, so the synthetic value has exactly
    /// the shape of the real one. The substitution is keyed on the whole value, so two real values that
    /// share a prefix share nothing in their synthetic forms. <paramref name="alphabet"/> narrows the
    /// letter set when a format forbids some letters (VINs have no I, O or Q).
    /// </summary>
    public string Substitute(string family, string value, string? attempt = null, CharSet alphabet = CharSet.Default)
    {
        var stream = Stream(family, value, attempt ?? string.Empty).GetEnumerator();
        var result = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            stream.MoveNext();
            var k = stream.Current;

            result.Append(SubstituteChar(c, k, alphabet, opensNumber: i == 0 || !char.IsAsciiDigit(value[i - 1])));
        }

        return result.ToString();
    }

    /// <summary>
    /// Prefix-preserving substitution: the character at each position is re-keyed under the real characters
    /// before it, so <c>a</c> is a prefix of <c>b</c> exactly when the synthetic <c>a</c> is a prefix of the
    /// synthetic <c>b</c>. Used for the family of codes that evaluators prefix-match (a service item's model
    /// cost against the vehicle's katashiki or variant code) and for programme names inside the milestone
    /// convention's alternation, whose ordering the same property keeps valid.
    /// </summary>
    public string SubstitutePrefixPreserving(string family, string value)
    {
        var result = new StringBuilder(value.Length);

        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            var k = (byte)(Uint(family, value[..i], i.ToString()) & 0xFF);

            result.Append(SubstituteChar(c, k, CharSet.Default, opensNumber: false));
        }

        return result.ToString();
    }

    /// <summary>Keyed hexadecimal of a given length, for opaque tokens (GUIDs, hash digests) that must stay hex.</summary>
    public string Hex(string family, string value, int length, bool upper = false)
    {
        var stream = Stream(family, value).GetEnumerator();
        var result = new StringBuilder(length);

        while (result.Length < length)
        {
            stream.MoveNext();
            var b = stream.Current;
            result.Append((b & 0xF).ToString(upper ? "X" : "x"));
        }

        return result.ToString();
    }

    /// <summary>Keyed digits of a given length; the first is never zero when <paramref name="nonZeroLead"/> asks so.</summary>
    public string Digits(string family, string value, int length, bool nonZeroLead)
    {
        var stream = Stream(family, value).GetEnumerator();
        var result = new StringBuilder(length);

        while (result.Length < length)
        {
            stream.MoveNext();
            var b = stream.Current;
            result.Append(result.Length == 0 && nonZeroLead ? (char)('1' + b % 9) : (char)('0' + b % 10));
        }

        return result.ToString();
    }

    private static char SubstituteChar(char c, byte k, CharSet alphabet, bool opensNumber)
    {
        if (char.IsAsciiDigit(c))
        {
            // A non-zero digit that opens a digit run stays non-zero, so a value stored as a number and the
            // same value stored as text come out as the same string (a number cannot carry a leading zero).
            if (opensNumber && c != '0')
                return (char)('1' + (c - '1' + k) % 9);

            return (char)('0' + (c - '0' + k) % 10);
        }

        if (c is >= 'A' and <= 'Z')
        {
            var letters = alphabet == CharSet.Vin ? VinLetters : Letters;
            var index = letters.IndexOf(c);

            return index < 0 ? c : letters[(index + k) % letters.Length];
        }

        if (c is >= 'a' and <= 'z')
            return (char)('a' + (c - 'a' + k) % 26);

        return c;
    }

    private const string Letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string VinLetters = "ABCDEFGHJKLMNPRSTUVWXYZ";
}

public enum CharSet
{
    Default,
    /// <summary>The ISO 3779 alphabet: no I, O or Q.</summary>
    Vin,
}
