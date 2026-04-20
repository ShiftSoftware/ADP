namespace ShiftSoftware.ADP.Surveys.Shared.Integrity;

/// <summary>
/// One integrity failure from <see cref="SurveyIntegrityValidator"/>. <see cref="Path"/>
/// is a dotted location inside the resolved schema the builder can use to point the
/// user at the offending field.
/// </summary>
public record IntegrityError(string Path, string Message);
