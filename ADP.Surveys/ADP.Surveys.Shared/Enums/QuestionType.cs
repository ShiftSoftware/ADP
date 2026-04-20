using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Surveys.Shared.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<QuestionType>))]
public enum QuestionType
{
    [JsonStringEnumMemberName("text")]
    Text,

    [JsonStringEnumMemberName("paragraph")]
    Paragraph,

    [JsonStringEnumMemberName("number")]
    Number,

    [JsonStringEnumMemberName("rating")]
    Rating,

    [JsonStringEnumMemberName("nps")]
    Nps,

    [JsonStringEnumMemberName("singleChoice")]
    SingleChoice,

    [JsonStringEnumMemberName("multiChoice")]
    MultiChoice,

    [JsonStringEnumMemberName("dropdown")]
    Dropdown,

    [JsonStringEnumMemberName("date")]
    Date,

    [JsonStringEnumMemberName("dateTime")]
    DateTime,

    [JsonStringEnumMemberName("file")]
    File,

    [JsonStringEnumMemberName("signature")]
    Signature,

    [JsonStringEnumMemberName("yesNo")]
    YesNo,

    [JsonStringEnumMemberName("navigationList")]
    NavigationList,
}
