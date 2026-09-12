using System.Text;

namespace ADP.TestData.Generator.Anonymisation;

/// <summary>
/// Synthetic VINs: a fictional world manufacturer identifier, the real vehicle's model-year character (the
/// one position a demo may read as a year), everything else re-keyed position by position within the
/// ISO 3779 alphabet, and the check digit recomputed — the components validate it, and a demo must not
/// need <c>disableVinValidation</c>.
/// </summary>
public static class Vin
{
    private const string Alphabet = "ABCDEFGHJKLMNPRSTUVWXYZ0123456789";
    private const string YearCharacters = "ABCDEFGHJKLMNPRSTVWXY123456789";
    private static readonly int[] Weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

    public static bool LooksLikeVin(string? value) =>
        value is { Length: 17 } && value.All(c => Alphabet.Contains(char.ToUpperInvariant(c)));

    /// <summary>A real VIN → its synthetic twin. <paramref name="attempt"/> varies the derivation on a clash.</summary>
    public static string Derive(Keyed keyed, string real, int attempt)
    {
        var upper = real.ToUpperInvariant();
        var salt = attempt == 0 ? string.Empty : "#" + attempt;

        var wmi = FictionalNames.WorldManufacturerIdentifiers[
            keyed.Pick(FictionalNames.WorldManufacturerIdentifiers.Length, "vin-wmi", upper[..3])];
        var descriptor = keyed.Substitute("vin-vds", upper[3..8], salt, CharSet.Vin);
        var year = upper[9];
        var plant = keyed.Substitute("vin-plant", upper[10..11], salt, CharSet.Vin);
        var serial = keyed.Substitute("vin-serial", upper[11..], salt, CharSet.Vin);

        return WithCheckDigit(wmi + descriptor + "0" + year + plant + serial);
    }

    /// <summary>A VIN that exists in no record: the unauthorized-vehicle key, derived from the environment name alone.</summary>
    public static string Mint(Keyed keyed, string environment, int attempt)
    {
        var context = "minted:" + environment + (attempt == 0 ? string.Empty : "#" + attempt);
        var stream = keyed.Stream(context).GetEnumerator();
        var body = new StringBuilder();

        string Next(string alphabet)
        {
            stream.MoveNext();
            return alphabet[stream.Current % alphabet.Length].ToString();
        }

        body.Append(FictionalNames.WorldManufacturerIdentifiers[keyed.Pick(FictionalNames.WorldManufacturerIdentifiers.Length, context)]);
        for (var i = 0; i < 5; i++)
            body.Append(Next(Alphabet));
        body.Append('0');
        body.Append(Next(YearCharacters));
        body.Append(Next("ABCDEFGHJKLMNPRSTUVWXYZ"));
        for (var i = 0; i < 6; i++)
            body.Append(Next("0123456789"));

        return WithCheckDigit(body.ToString());
    }

    public static string WithCheckDigit(string vin)
    {
        if (vin.Length != 17)
            throw new ArgumentException("A VIN has 17 characters.", nameof(vin));

        var sum = 0;
        for (var i = 0; i < 17; i++)
            sum += Transliterate(vin[i]) * Weights[i];

        var remainder = sum % 11;
        var check = remainder == 10 ? 'X' : (char)('0' + remainder);

        return vin[..8] + check + vin[9..];
    }

    private static int Transliterate(char c) => char.ToUpperInvariant(c) switch
    {
        >= '0' and <= '9' => c - '0',
        'A' or 'J' => 1,
        'B' or 'K' or 'S' => 2,
        'C' or 'L' or 'T' => 3,
        'D' or 'M' or 'U' => 4,
        'E' or 'N' or 'V' => 5,
        'F' or 'W' => 6,
        'G' or 'P' or 'X' => 7,
        'H' or 'Y' => 8,
        'R' or 'Z' => 9,
        _ => throw new ArgumentException($"'{c}' is not a VIN character."),
    };
}
