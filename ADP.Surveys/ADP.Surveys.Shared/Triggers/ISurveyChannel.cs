namespace ShiftSoftware.ADP.Surveys.Shared.Triggers;

/// <summary>
/// Pluggable delivery transport for trigger sends. Each implementation declares a
/// stable <see cref="Key"/> string that the trigger config's <c>channel</c> field
/// references; the scheduler resolves this key against registered channels.
///
/// Implementations are typically thin adapters around a vendor SDK (Twilio for SMS,
/// Infobip for WhatsApp, an SMTP relay for email, etc.). The mock channel shipped
/// with this module records sends for testing.
/// </summary>
public interface ISurveyChannel
{
    /// <summary>e.g. <c>"whatsapp:default"</c>, <c>"sms:twilio"</c>, <c>"email:smtp"</c>, <c>"memory:default"</c>.</summary>
    string Key { get; }

    Task<ChannelSendResult> SendAsync(ChannelSendRequest request, CancellationToken ct);
}

public class ChannelSendRequest
{
    public Guid PublicId { get; set; }
    public required string Address { get; set; }
    public string? Locale { get; set; }
    public required string SurveyUrl { get; set; }
    /// <summary>1 = initial send; 2..N = reminder sends.</summary>
    public int AttemptNumber { get; set; }
    /// <summary>The instance's <c>MetaDataJson</c> blob — channels can use it for templating placeholders.</summary>
    public string? CandidateMetadataJson { get; set; }
}

public class ChannelSendResult
{
    public bool Delivered { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? Error { get; set; }

    public static ChannelSendResult Success(string? providerMessageId = null)
        => new() { Delivered = true, ProviderMessageId = providerMessageId };

    public static ChannelSendResult Failure(string error)
        => new() { Delivered = false, Error = error };
}
