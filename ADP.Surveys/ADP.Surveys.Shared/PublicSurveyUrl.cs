namespace ShiftSoftware.ADP.Surveys.Shared;

/// <summary>
/// Composition and sanity-checking for the recipient-facing survey link.
///
/// The link is the entire delivery mechanism — an instance id is worthless to a customer
/// without a URL that resolves. <c>SurveyApiOptions.PublicSurveyUrlTemplate</c> ships with
/// a localhost dev default so the sample app works out of the box, and that default is
/// exactly what a real deployment forgets to override. These helpers exist so the
/// forgetting is caught at startup and, failing that, before anything is sent.
///
/// Lives in Shared rather than API so both the send path and the dashboard reason about
/// the link the same way — and so it is testable without dragging ASP.NET Core into the
/// test project.
/// </summary>
public static class PublicSurveyUrl
{
    /// <summary>The out-of-the-box template. Points at the standalone Vite dev server.</summary>
    public const string DevDefault = "http://localhost:5190/s/{publicId}";

    /// <summary>Placeholder the instance's public id is substituted into.</summary>
    public const string Placeholder = "{publicId}";

    /// <summary>
    /// True when the template can produce a link a customer could actually open — i.e. it
    /// is set, carries the placeholder, and doesn't point back at the developer's machine.
    /// Loopback hosts are the tell: nothing a respondent opens on their phone can reach them.
    /// </summary>
    public static bool IsDeployable(string? template)
    {
        if (string.IsNullOrWhiteSpace(template)) return false;
        if (!template.Contains(Placeholder, StringComparison.Ordinal)) return false;
        return !PointsAtLoopback(template);
    }

    /// <summary>
    /// True when the template resolves to the local machine — the dev default, or any other
    /// loopback address someone pointed it at.
    /// </summary>
    public static bool PointsAtLoopback(string? template)
    {
        if (string.IsNullOrWhiteSpace(template)) return false;

        // Match on host, not substring: a legitimate host could contain "localhost" as a
        // label (e.g. surveys.localhost-labs.example) without being loopback.
        if (!Uri.TryCreate(template.Replace(Placeholder, "placeholder", StringComparison.Ordinal), UriKind.Absolute, out var uri))
            return false;

        return uri.IsLoopback;
    }

    /// <summary>
    /// Substitutes the instance's public id into the template. Returns null when the
    /// template is unset — callers surface that as "no link available" rather than
    /// handing out a half-formed URL.
    /// </summary>
    public static string? Compose(string? template, Guid publicId) =>
        string.IsNullOrWhiteSpace(template)
            ? null
            : template.Replace(Placeholder, publicId.ToString(), StringComparison.Ordinal);

    /// <summary>Operator-facing explanation of why a template isn't deployable.</summary>
    public static string DescribeProblem(string? template)
    {
        if (string.IsNullOrWhiteSpace(template))
            return "PublicSurveyUrlTemplate is not set.";
        if (!template.Contains(Placeholder, StringComparison.Ordinal))
            return $"PublicSurveyUrlTemplate ('{template}') has no {Placeholder} placeholder, so every recipient would get the same link.";
        if (PointsAtLoopback(template))
            return $"PublicSurveyUrlTemplate ('{template}') points at the local machine, so recipients cannot open it.";
        return "PublicSurveyUrlTemplate is valid.";
    }
}
