namespace ShiftSoftware.ADP.Cases.Shared.Printing;

/// <summary>
/// Consumer seam formatting the dates module printouts render (Phase 3 Slice 3.2). Mirrors the
/// original host's PrintOutDateFormatter surface. The module API extensions TryAdd
/// <see cref="DefaultPrintoutDateFormatter"/>; a consumer registration
/// (<c>services.AddScoped&lt;IPrintoutDateFormatter, YourPrintoutDateFormatter&gt;()</c>) wins.
/// </summary>
public interface IPrintoutDateFormatter
{
    string GetFormattedDate(DateTime date);

    string? GetFormattedDate(DateTime? date);

    string GetFormattedDateTime(DateTimeOffset date);

    string? GetFormattedDateTime(DateTimeOffset? date);
}
