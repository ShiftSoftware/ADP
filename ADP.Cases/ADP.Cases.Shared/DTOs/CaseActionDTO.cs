namespace ShiftSoftware.ADP.Cases.Shared.DTOs;

/// <summary>
/// What an action button sends with its action. In the body, so the comment can be long and stays out of request logs.
/// </summary>
public class CaseActionDTO
{
    /// <summary>The comment for the history line the action writes. Optional.</summary>
    public string? Comment { get; set; }
}
