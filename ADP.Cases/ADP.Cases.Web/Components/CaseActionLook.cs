using MudBlazor;

namespace ShiftSoftware.ADP.Cases.Web.Components;

/// <summary>
/// How a case's actions and statuses look, on the form, on the list and in the history, by their names. One this does
/// not know still gets a plain button or chip.
/// </summary>
public sealed record CaseActionLook(int Order, string Icon, Color Color)
{
    public static CaseActionLook Of(string action) => action switch
    {
        "Approve" => new(0, Icons.Material.Filled.Check, Color.Success),
        "RequestAmendment" => new(1, Icons.Material.Filled.Undo, Color.Warning),
        "Deny" => new(2, Icons.Material.Filled.Block, Color.Error),
        "Close" => new(3, Icons.Material.Filled.Lock, Color.Default),
        "DealerSave" => new(4, Icons.Material.Filled.Send, Color.Primary),
        "Note" => new(5, Icons.Material.Filled.AddComment, Color.Default),

        // The claims' (UpdateStatusActionTypes).
        "SubmitToDistributor" => new(0, Icons.Material.Filled.Check, Color.Info),
        "DistributorAccepted" => new(1, Icons.Material.Filled.Check, Color.Success),
        "DistributorError" => new(2, Icons.Material.Filled.Warning, Color.Warning),
        "DistributorRejected" => new(3, Icons.Material.Filled.Error, Color.Error),

        _ => new(9, Icons.Material.Filled.PlayArrow, Color.Primary),
    };

    public static Color StatusColor(string? status) => status switch
    {
        "Approved" => Color.Success,
        "Denied" => Color.Error,
        "AmendRequest" => Color.Warning,
        "DealerResponse" => Color.Info,
        "Closed" => Color.Dark,

        // ClaimStatus.
        "PendingProcess" => Color.Info,
        "Accepted" => Color.Success,
        "RejectedWithError" => Color.Warning,
        "RejectedPermanently" => Color.Error,
        "Certified" => Color.Primary,
        "Invoiced" => Color.Tertiary,

        _ => Color.Default,
    };
}
