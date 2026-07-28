namespace ShiftSoftware.ADP.Surveys.API.Services;

/// <summary>
/// Config for pull-based trigger ingestion (<see cref="TriggerPullWorker"/> /
/// <see cref="TriggerPullService"/>). Bound from the <c>SurveysIngest</c> section by
/// <c>AddSurveysTriggerPull</c>. Shared by all registered
/// <see cref="ITriggerPullSource"/>s in the host.
/// </summary>
public class TriggerPullOptions
{
    public const string SectionName = "SurveysIngest";

    /// <summary>Master switch. Off by default in every environment.</summary>
    public bool Enabled { get; set; } = false;

    /// <summary>How often the worker scans the upstream source(s).</summary>
    public int ScanIntervalMinutes { get; set; } = 30;

    /// <summary>
    /// How old an upstream row (by its creation date — row creation, NOT the event's
    /// effective date) may be when FIRST seen by the puller. Only matters at initial
    /// enablement, after a manual cursor reset, or after an outage: forward progress is
    /// carried by the durable <c>TriggerPullCursor</c> row, not by this window.
    /// </summary>
    /// <remarks>
    /// This doubles as a staleness bound after downtime: if the puller is off for LONGER
    /// than this window, rows recorded in the un-scanned gap beyond it are permanently
    /// passed over (deliberate — a survey about a weeks-old event is worse than no
    /// survey). Raise it BEFORE re-enabling if that catch-up is wanted.
    /// </remarks>
    public int LookbackDays { get; set; } = 7;

    /// <summary>
    /// Row cap per scan per source. The cursor advances past each processed batch, so a
    /// backlog larger than this drains across consecutive scans.
    /// </summary>
    public int MaxRowsPerScan { get; set; } = 500;

    /// <summary>Delay before the first scan so app startup (migrations, seeders) settles.</summary>
    public int StartupDelaySeconds { get; set; } = 60;

    /// <summary>
    /// How many consecutive scans a persistently failing row may block the drain before
    /// it is skipped with an ERROR log (never silently). Scans where EVERY item fails are
    /// treated as systemic (config/connectivity) and hold the cursor without consuming
    /// this budget.
    /// </summary>
    public int MaxRowRetries { get; set; } = 5;
}
