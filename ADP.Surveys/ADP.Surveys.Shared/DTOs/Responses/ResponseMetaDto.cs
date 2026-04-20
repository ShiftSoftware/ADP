using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Responses;

public class ResponseMetaDto
{
    [JsonPropertyName("startedAt")]
    public DateTimeOffset? StartedAt { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; set; }

    /// <summary>
    /// Non-null when the response was submitted through the agent-assist iframe.
    /// Spoofable — no privileged action is gated on this. Used for analytics only.
    /// </summary>
    [JsonPropertyName("agentId")]
    public string? AgentId { get; set; }

    [JsonPropertyName("locale")]
    public string? Locale { get; set; }

    [JsonPropertyName("userAgent")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Catch-all for channel-specific context (referrer URL, UTM tags, etc.).
    /// </summary>
    [JsonPropertyName("extra")]
    public Dictionary<string, JsonElement>? Extra { get; set; }
}

public class ResponseMetaDtoValidator : AbstractValidator<ResponseMetaDto>
{
    public ResponseMetaDtoValidator()
    {
        RuleFor(x => x).Must(x => x.StartedAt!.Value <= x.CompletedAt!.Value)
            .When(x => x.StartedAt.HasValue && x.CompletedAt.HasValue)
            .WithMessage("startedAt must be on or before completedAt.");
    }
}
