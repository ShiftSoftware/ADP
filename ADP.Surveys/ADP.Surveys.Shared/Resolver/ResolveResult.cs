using ShiftSoftware.ADP.Surveys.Shared.DTOs;

namespace ShiftSoftware.ADP.Surveys.Shared.Resolver;

/// <summary>
/// Outcome of a <see cref="SchemaResolver"/> pass. On success, <see cref="Survey"/>
/// holds the fully-inlined schema (no refs left) and <see cref="Errors"/> is empty.
/// On failure, <see cref="Survey"/> is <c>null</c> and <see cref="Errors"/> lists
/// what couldn't be resolved (unknown bankRef, unknown templateRef, etc.).
/// </summary>
public record ResolveResult(SurveyDto? Survey, IReadOnlyList<ResolveError> Errors)
{
    public bool Success => Errors.Count == 0 && Survey is not null;

    public static ResolveResult Ok(SurveyDto survey) => new(survey, []);

    public static ResolveResult Fail(IReadOnlyList<ResolveError> errors) => new(null, errors);
}

/// <summary>
/// Path is a dotted location inside the draft survey (e.g. <c>screens[2].questions[1].bankRef</c>)
/// to help the builder point the user at the offending reference.
/// </summary>
public record ResolveError(string Path, string Message);
