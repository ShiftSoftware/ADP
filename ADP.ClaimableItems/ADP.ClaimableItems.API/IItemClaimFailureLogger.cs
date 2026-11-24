namespace ShiftSoftware.ADP.ClaimableItems.API;

/// <summary>
/// Consumer seam for recording failed claim submissions (the catch-all around the claim hot path).
/// A host can log the raw payload + exception (e.g. to a Cosmos container) so a technician can
/// replay the claim. Optional — when not registered, failures still return the
/// generic 400 response, they just aren't persisted anywhere.
/// </summary>
public interface IItemClaimFailureLogger
{
    Task LogAsync(string payload, IEnumerable<object> documentsMetadata, Exception exception);
}
