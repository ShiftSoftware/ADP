using System.Text.Json.Serialization;
using ShiftSoftware.ADP.Surveys.Shared.Json;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Screens;

/// <summary>
/// A screen in a survey's flow. Two concrete shapes:
/// <see cref="InlineScreenDto"/> (self-contained) or <see cref="ScreenTemplateRefDto"/>
/// (reference to a <c>ScreenTemplate</c>, resolved at publish time).
///
/// Discriminated by presence of <c>templateRef</c> in the JSON — see <see cref="ScreenDtoConverter"/>.
/// </summary>
[JsonConverter(typeof(ScreenDtoConverter))]
public abstract class ScreenDto
{
}
