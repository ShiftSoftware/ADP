namespace LookupServices.BDD.Support;

/// <summary>
/// A <see cref="TimeProvider"/> frozen at a fixed instant. Scenarios whose outcome depends on
/// "now" — service item expiry, signature expiry — set this via <c>LookupOptions.TimeProvider</c>
/// so they assert against a known clock instead of the machine's, and stay deterministic forever
/// rather than only until the dates they use go stale.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow) => this.utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => utcNow;
}
