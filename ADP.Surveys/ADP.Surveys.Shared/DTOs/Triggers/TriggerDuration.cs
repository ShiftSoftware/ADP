using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Triggers;

public static class TriggerDuration
{
    private static readonly Regex Pattern = new(@"^\s*(\d+)\s*([smhdSMHD])\s*$", RegexOptions.Compiled);

    public static bool TryParse(string? input, out TimeSpan duration)
    {
        duration = TimeSpan.Zero;
        if (string.IsNullOrWhiteSpace(input)) return false;

        var match = Pattern.Match(input);
        if (!match.Success) return false;

        if (!long.TryParse(match.Groups[1].Value, out var n)) return false;

        try
        {
            duration = char.ToLowerInvariant(match.Groups[2].Value[0]) switch
            {
                's' => TimeSpan.FromSeconds(n),
                'm' => TimeSpan.FromMinutes(n),
                'h' => TimeSpan.FromHours(n),
                'd' => TimeSpan.FromDays(n),
                _ => TimeSpan.Zero
            };
            return true;
        }
        catch (OverflowException)
        {
            return false;
        }
    }
}
