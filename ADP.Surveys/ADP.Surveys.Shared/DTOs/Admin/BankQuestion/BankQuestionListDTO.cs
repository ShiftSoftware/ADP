using ShiftSoftware.ADP.Surveys.Shared.Enums;
using ShiftSoftware.ADP.Surveys.Shared.HashIds;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Surveys.Shared.DTOs.Admin.BankQuestion;

public class BankQuestionListDTO : ShiftEntityListDTO
{
    [BankQuestionHashId]
    public override string? ID { get; set; }

    /// <summary>Decision #11 stable anchor — also surfaced here so the UI can link to analytics joins.</summary>
    public Guid BankEntryID { get; set; }

    /// <summary>Human-readable key surveys reference via <c>bankRef</c>.</summary>
    public string Key { get; set; } = default!;

    /// <summary>Lifted out of the serialized question for grid display.</summary>
    public QuestionType Type { get; set; }

    public bool Locked { get; set; }
    public bool Retired { get; set; }
}
