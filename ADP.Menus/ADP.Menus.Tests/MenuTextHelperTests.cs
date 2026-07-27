using System.Globalization;

using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Shared;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// PHASE 0 — pins the two text helpers that feed generated codes. Both must be ported verbatim into
/// the shared <c>MenuTextHelpers</c> in Phase 1, so their current behaviour (quirks included) is
/// captured here first.
/// </summary>
public class MenuTextHelperTests
{
    /// <summary>
    /// <see cref="Utility.GetAllowedTimeText"/> is concatenated straight into the labour code, so its
    /// exact output — including the padding rules and the decimal SCALE sensitivity — is contractual.
    /// </summary>
    [Theory]
    [InlineData(0, "0")]
    [InlineData(0.05, "005")]
    [InlineData(0.25, "025")]
    [InlineData(0.3, "03")]
    [InlineData(0.5, "05")]
    [InlineData(0.75, "075")]
    [InlineData(1.5, "15")]
    [InlineData(12.34, "1234")]
    public void GetAllowedTimeText_PinnedOutputs(double allowedTime, string expected)
    {
        Assert.Equal(expected, Utility.GetAllowedTimeText((decimal)allowedTime));
    }

    /// <summary>
    /// KNOWN QUIRK — the trailing-zero trim makes 1 and 10 (and 2 and 20) collide onto the same text,
    /// so two different allowed times can yield the SAME labour code. Pinned because the port must not
    /// "fix" it silently: doing so would change codes the DMS has already received.
    /// </summary>
    [Fact]
    public void GetAllowedTimeText_WholeHourCollision_IsPinned()
    {
        Assert.Equal("10", Utility.GetAllowedTimeText(1m));
        Assert.Equal("10", Utility.GetAllowedTimeText(10m));
        Assert.Equal("20", Utility.GetAllowedTimeText(2m));
        Assert.Equal("20", Utility.GetAllowedTimeText(20m));
    }

    /// <summary>Trailing decimal zeros do not change the text (1.5 and 1.50 agree).</summary>
    [Fact]
    public void GetAllowedTimeText_IsInsensitiveToTrailingDecimalZeros()
    {
        Assert.Equal(Utility.GetAllowedTimeText(1.5m), Utility.GetAllowedTimeText(1.50m));
        Assert.Equal(Utility.GetAllowedTimeText(0.5m), Utility.GetAllowedTimeText(0.50m));
    }

    [Fact]
    public void GetAllowedTimeText_NegativeThrows()
    {
        Assert.Throws<ArgumentException>(() => Utility.GetAllowedTimeText(-1m));
    }

    /// <summary>
    /// Open item O7 — <see cref="Utility.GetAllowedTimeText"/> formats the decimal with the AMBIENT
    /// culture, so the same allowed time produces a different labour code under a culture whose
    /// decimal separator is not '.'.
    ///
    /// DECIDED: leave as-is. Pinning the culture would change labour codes already issued to a DMS,
    /// for a case the deployments do not hit. The shared generator's ported helper reproduces this
    /// exactly. This test exists so the behaviour is visible rather than latent — if a future
    /// deployment DOES run under such a culture, this is the note that explains the symptom.
    /// </summary>
    [Fact]
    public void GetAllowedTimeText_IsCultureSensitive_O7()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var invariant = Utility.GetAllowedTimeText(0.5m);

            CultureInfo.CurrentCulture = new CultureInfo("de-DE");
            var german = Utility.GetAllowedTimeText(0.5m);

            Assert.Equal("05", invariant);
            Assert.Equal("0,5", german);
            Assert.NotEqual(invariant, german);   // ← the O7 defect, pinned so the fix is visible
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    /// <summary>
    /// <see cref="LocalizedText.Resolve"/> turns a possibly-multi-language stored value into one
    /// language. It drives the menu prefix/postfix, the standalone operation code and the standalone
    /// group menu code — i.e. it is part of the generated menu code.
    /// </summary>
    [Theory]
    [InlineData(null, "en", "")]
    [InlineData("", "en", "")]
    [InlineData("PLAIN", "ar", "PLAIN")]                                     // non-JSON passes through
    [InlineData("""{"en":"A","ar":"B"}""", "en", "A")]
    [InlineData("""{"en":"A","ar":"B"}""", "ar", "B")]
    [InlineData("""{"en":"A","ar":"B"}""", null, "A")]                       // null language → "en"
    [InlineData("""{"en":"A","ar":"B"}""", "", "A")]                         // empty language → "en"
    [InlineData("""{"en":"A","ar":"B"}""", "en-US", "A")]                    // culture name → 2-letter
    [InlineData("""{"en":"A","ar":"B"}""", "fr", "A")]                       // unknown → "en" fallback
    [InlineData("""{"fr":"C"}""", "de", "C")]                                // no "en" → first value
    [InlineData("{not valid json", "en", "{not valid json")]                 // unparseable → raw
    public void LocalizedText_Resolve_PinnedOutputs(string? raw, string? language, string expected)
    {
        Assert.Equal(expected, LocalizedText.Resolve(raw, language));
    }
}
