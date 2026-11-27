using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ADP.Cases.Shared.Flow;

/// <summary>
/// What <see cref="CaseDefinition{TStatus, TTrigger}.ResolveSave"/> decided about one save of a case: it is refused,
/// it goes through without touching the status, or it goes through and applies a trigger.
/// </summary>
/// <typeparam name="TTrigger">The case type's trigger enum.</typeparam>
public sealed record CaseSaveResolution<TTrigger> where TTrigger : struct, Enum
{
    private CaseSaveResolution(TTrigger? trigger, Message? rejection)
    {
        Trigger = trigger;
        Rejection = rejection;
    }

    /// <summary>The trigger to apply, or null when the status must not change.</summary>
    public TTrigger? Trigger { get; }

    /// <summary>Why the save was refused, or null when it is allowed.</summary>
    public Message? Rejection { get; }

    /// <summary>True when the caller must refuse the save.</summary>
    public bool IsRejected => Rejection is not null;

    /// <summary>Save the case, leave the status alone.</summary>
    public static CaseSaveResolution<TTrigger> NoStatusChange { get; } = new(null, null);

    /// <summary>Save the case and apply <paramref name="trigger"/>.</summary>
    public static CaseSaveResolution<TTrigger> Apply(TTrigger trigger) => new(trigger, null);

    /// <summary>Refuse the save.</summary>
    public static CaseSaveResolution<TTrigger> Reject(string message) =>
        new(null, new Message("Not Allowed", message));
}
