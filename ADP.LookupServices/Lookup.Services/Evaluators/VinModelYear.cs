namespace ShiftSoftware.ADP.Lookup.Services.Evaluators;

/// <summary>
/// Reads the model year from VIN character 10, the year code of ISO 3779 and US 49 CFR 565.
/// </summary>
/// <remarks>
/// The code repeats every 30 years, so a letter or digit stands for one year in each cycle. This picks the most
/// recent of them that is not later than the year after <c>referenceYear</c>: with a reference year of 2026,
/// codes read as 1998 to 2027. A vehicle built before that window reads 30 years too late.
/// <para>
/// Not every manufacturer fills character 10 with a year, and some fill it with <c>0</c> on some markets.
/// Callers decide which VINs the rule applies to.
/// </para>
/// </remarks>
public static class VinModelYear
{
    /// <summary>The year codes in order, starting from 1980. I, O, Q, U, Z and 0 are never year codes.</summary>
    private const string Codes = "ABCDEFGHJKLMNPRSTVWXY123456789";
    private const int FirstYear = 1980;
    private const int Cycle = 30;
    private const int YearCodePosition = 9;

    /// <summary>
    /// The model year VIN character 10 stands for, or null when the VIN is not 17 characters long or the
    /// character is not a year code (including <c>0</c>, "not encoded").
    /// </summary>
    public static int? Decode(string vin, int referenceYear)
    {
        vin = vin?.Trim().ToUpperInvariant();

        if (vin?.Length != 17)
            return null;

        var index = Codes.IndexOf(vin[YearCodePosition]);

        if (index < 0)
            return null;

        var year = FirstYear + index;
        var latest = referenceYear + 1;

        return year + Cycle * ((latest - year) / Cycle);
    }
}
