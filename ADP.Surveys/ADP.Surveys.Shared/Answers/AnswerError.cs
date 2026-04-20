namespace ShiftSoftware.ADP.Surveys.Shared.Answers;

/// <summary>
/// One answer-shape failure from <see cref="AnswerValidator"/>. <see cref="QuestionId"/>
/// maps back to the question whose submitted value was rejected (or is missing when required).
/// </summary>
public record AnswerError(string QuestionId, string Message);
