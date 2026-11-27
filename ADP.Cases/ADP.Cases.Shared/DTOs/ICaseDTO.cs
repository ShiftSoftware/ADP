namespace ShiftSoftware.ADP.Cases.Shared.DTOs;

/// <summary>What the shared repository and endpoints need from a case type's DTO.</summary>
public interface ICaseDTO<TStatus> where TStatus : struct, Enum
{
    TStatus? Status { get; set; }
}
