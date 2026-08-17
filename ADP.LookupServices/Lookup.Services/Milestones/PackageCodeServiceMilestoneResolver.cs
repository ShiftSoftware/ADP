using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ShiftSoftware.ADP.Lookup.Services.Milestones;

/// <summary>
/// Reads a milestone out of the shape of a labour line's package code, on the convention that such a
/// code reads <c>&lt;programme&gt; &lt;model&gt; &lt;milestone&gt;K [&lt;qualifier&gt;]</c>.
/// <para>
/// <b>A heuristic, and meant to be temporary.</b> Nothing in the source states that a code names a
/// milestone; this infers it from how the code is written. It earns its place only because the
/// alternative today is no milestone at all — see <see cref="IServiceMilestoneResolver"/> for the
/// source that states the interval outright and retires this.
/// </para>
/// </summary>
public sealed class PackageCodeServiceMilestoneResolver : IServiceMilestoneResolver
{
    // The pattern is deployment configuration rather than authored catalog data, so the exposure is
    // small — but a timeout costs one argument and turns a pathological pattern into a code that
    // reads as unscheduled instead of a lookup that never returns.
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(250);

    private static readonly char[] TokenSeparators = { ' ', '\t', '\r', '\n' };

    // Null when the configuration cannot produce readings at all — an unparsable pattern or bounds
    // that admit nothing. Every code then reads as carrying no milestone, which withholds items
    // rather than offering them.
    private readonly Regex pattern;
    private readonly long minimum;
    private readonly long maximum;
    private readonly long step;

    public PackageCodeServiceMilestoneResolver(ServiceMilestoneOptions options)
    {
        if (options is null)
            throw new ArgumentNullException(nameof(options));

        minimum = options.MinimumInKilometres;
        maximum = options.MaximumInKilometres;
        step = options.StepInKilometres;

        if (string.IsNullOrWhiteSpace(options.PackageCodePattern) ||
            minimum <= 0 ||
            maximum < minimum ||
            step < 0)
            return;

        try
        {
            pattern = new Regex(
                options.PackageCodePattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                MatchTimeout);
        }
        catch (ArgumentException)
        {
            // A pattern that does not compile is a misconfiguration, and one that must not take the
            // whole lookup down with it.
            pattern = null;
        }
    }

    public ServiceMilestoneReading Resolve(string packageCode)
    {
        if (pattern is null || string.IsNullOrWhiteSpace(packageCode))
            return null;

        Match match;
        try
        {
            match = pattern.Match(packageCode);

            if (!match.Success)
                return null;

            // Exactly one milestone token, or none. A code carrying two says something this reader
            // cannot resolve, and picking one of them would be a guess that quietly decides whether
            // a reward is earned.
            if (match.NextMatch().Success)
                return null;
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }

        if (match.Groups.Count < 2 || !match.Groups[1].Success)
            return null;

        // NumberStyles.None: no sign, no separators, no leading whitespace. An overlong run of
        // digits simply fails to parse, which is the answer we want anyway.
        if (!long.TryParse(
                match.Groups[1].Value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var thousands) ||
            thousands > long.MaxValue / 1000)
            return null;

        var milestone = thousands * 1000;

        if (milestone < minimum || milestone > maximum || (step > 0 && milestone % step != 0))
            return null;

        var prefix = packageCode.Substring(0, match.Index);
        var suffix = packageCode.Substring(match.Index + match.Length);

        return new ServiceMilestoneReading(milestone, FirstToken(prefix), Trimmed(suffix));
    }

    /// <summary>
    /// The programme a code is booked under is its leading token. A deployment whose codes carry no
    /// programme token simply does not configure a programme filter, and the model token this then
    /// reports is never compared with anything.
    /// </summary>
    private static string FirstToken(string prefix)
    {
        var tokens = prefix.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        return tokens.Length == 0 ? null : tokens[0];
    }

    /// <summary>
    /// Everything after the milestone token is the qualifier, kept whole rather than split: a
    /// deny-list or allow-list names what a deployment writes there, and this reader has no basis
    /// for deciding that two trailing tokens are two qualifiers rather than one written with a space.
    /// </summary>
    private static string Trimmed(string suffix)
    {
        var trimmed = suffix.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}
