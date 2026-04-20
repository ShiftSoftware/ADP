using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Surveys.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<LogicOperator>))]
public enum LogicOperator
{
    [JsonStringEnumMemberName("==")]
    Equals,

    [JsonStringEnumMemberName("!=")]
    NotEquals,

    [JsonStringEnumMemberName(">")]
    GreaterThan,

    [JsonStringEnumMemberName(">=")]
    GreaterThanOrEqual,

    [JsonStringEnumMemberName("<")]
    LessThan,

    [JsonStringEnumMemberName("<=")]
    LessThanOrEqual,

    [JsonStringEnumMemberName("in")]
    In,

    [JsonStringEnumMemberName("notIn")]
    NotIn,

    [JsonStringEnumMemberName("isSet")]
    IsSet,

    [JsonStringEnumMemberName("isNotSet")]
    IsNotSet,
}
