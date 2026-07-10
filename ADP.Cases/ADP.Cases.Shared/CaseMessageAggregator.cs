using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Cases.Shared;

/// <summary>
/// Shared error-aggregation helper for batch case operations: cap the per-item sub-errors, add a
/// "+N more" summary row, and throw one <c>ShiftEntityException</c> titled "Error" — the dialog
/// shape all consumer list pages already render.
/// </summary>
/// <remarks>
/// <see cref="Services.SharedClaimService"/> intentionally keeps its own inline VERBATIM copy of
/// this logic (D16 — byte-faithful move); new engine consumers use this helper instead.
/// </remarks>
public static class CaseMessageAggregator
{
    public const int DefaultErrorLimit = 10;

    /// <summary>Throws the aggregated <see cref="ShiftEntityException"/> when any errors exist; no-op otherwise.</summary>
    /// <param name="subErrors">Per-item error messages collected by the batch operation.</param>
    /// <param name="omittedNoun">Noun used in the "+N more {noun}" summary row (e.g. "claims", "cases").</param>
    /// <param name="errorLimit">Maximum sub-errors shown before summarizing.</param>
    public static void ThrowIfAny(List<Message> subErrors, string omittedNoun = "cases", int errorLimit = DefaultErrorLimit)
    {
        if (subErrors.Count == 0)
            return;

        var shown = subErrors;

        if (shown.Count > errorLimit)
        {
            var omitted = shown.Count - errorLimit;

            shown = shown.Take(errorLimit).ToList();
            shown.Add(new Message($"+{omitted} more {omittedNoun}", "omitted in this warning dialog"));
        }

        throw new ShiftEntityException(new Message("Error", "", shown));
    }
}
