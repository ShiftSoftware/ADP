namespace ShiftSoftware.ADP.ClaimableItems.API;

/// <summary>
/// Consumer seam for recording failed claim submissions (the catch-all around the claim hot path).
/// The original host application logs the raw payload + exception to its Cosmos <c>Logs/FailedClaims</c> container so a
/// technician can replay the claim. Optional — when not registered, failures still return the
/// generic 400 response, they just aren't persisted anywhere.
/// </summary>
public interface IItemClaimFailureLogger
{
    Task LogAsync(string payload, IEnumerable<object> documentsMetadata, Exception exception);
}
