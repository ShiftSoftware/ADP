using System.Text;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// CP1256-as-CP1252 mojibake detection and repair — Arabic names stored through a Latin codepage
/// arrive as dense accented garbage; the byte round-trip recovers the original script.
/// </summary>
public static class Mojibake
{
    private static readonly Encoding Cp1252, Cp1256;
    static Mojibake()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp1252 = Encoding.GetEncoding(1252);
        Cp1256 = Encoding.GetEncoding(1256);
    }

    /// <summary>
    /// Recover a CP1256 (Arabic) name that was stored as its CP1252 byte-misreading
    /// ('äÕÑ ÇáÏíä' → 'نصر الدين'): re-encode to the original bytes, decode as Arabic. Accept only
    /// if the result is genuinely Arabic script, so rare 3+-accent Latin names aren't mangled.
    /// </summary>
    public static string? TryRepair(string raw)
    {
        try
        {
            string arabic = Cp1256.GetString(Cp1252.GetBytes(raw));
            // Require real Arabic LETTERS (U+0621..U+064A), not diacritics / punctuation / digits
            // from the block — a decode yielding only marks must not pass as a recovered name.
            int arabicLetters = arabic.Count(c => c is >= 'ء' and <= 'ي');
            return arabicLetters >= 2 ? arabic : null;
        }
        catch { return null; }
    }

    /// <summary>
    /// CP1256-decoded-as-Latin mojibake detector (~879 garbage names across the 7 files).
    /// Fingerprint = dense non-ASCII (3+). Adversarial review 2026-06-07: symbol-counting
    /// clauses ('(' ')' apostrophes) wrongly blanked ~313 legitimate annotated names like
    /// "Haithem Al Janabi(Company)" / "Ra'afat Ra'ad" — punctuation is NOT mojibake signal.
    /// 2026-06-08: the byte transform is now verified, so these are REPAIRED (see TryRepairMojibake),
    /// not discarded — they are real Arabic names (~879 across the 7 files).
    /// </summary>
    public static bool Looks(string raw)
    {
        return raw.Count(c => c > 127) >= 3;
    }
}
