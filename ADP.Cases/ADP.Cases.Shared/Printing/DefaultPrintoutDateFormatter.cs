namespace ShiftSoftware.ADP.Cases.Shared.Printing;

/// <summary>
/// The module default <see cref="IPrintoutDateFormatter"/>: two plain format strings, defaulting to
/// "yyyy-MM-dd" for dates and "yyyy-MM-dd hh:mm tt" for date-times. Formatting uses the current
/// culture.
/// </summary>
public class DefaultPrintoutDateFormatter : IPrintoutDateFormatter
{
    private readonly string dateFormat;
    private readonly string dateTimeFormat;

    public DefaultPrintoutDateFormatter(string dateFormat = "yyyy-MM-dd", string dateTimeFormat = "yyyy-MM-dd hh:mm tt")
    {
        this.dateFormat = dateFormat;
        this.dateTimeFormat = dateTimeFormat;
    }

    public string GetFormattedDate(DateTime date)
    {
        return date.ToString(this.dateFormat);
    }

    public string? GetFormattedDate(DateTime? date)
    {
        if (date is null)
            return null;

        return GetFormattedDate(date.Value);
    }

    public string GetFormattedDateTime(DateTimeOffset date)
    {
        return date.ToString(this.dateTimeFormat);
    }

    public string? GetFormattedDateTime(DateTimeOffset? date)
    {
        if (date is null)
            return null;

        return GetFormattedDateTime(date.Value);
    }
}
