namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// A blob surface was pointed at a container that does not exist.
///
/// <para><b>The standing rule this enforces: the engine never creates or deletes a container.</b>
/// It is handed a container name and manages only the blobs inside it. Creating the container is a
/// one-time provisioning act by whoever owns the storage account — a developer locally, an operator
/// in production — and it stays that way whatever the credential happens to allow. A connection
/// string that COULD create a container still must not be used to: the container is the unit a
/// credential is scoped to, so creating one here would force every deployment to hand this engine a
/// wider credential than its work needs, and it would turn a mistyped container name into a
/// brand-new empty container that looks like a healthy, empty estate.</para>
///
/// <para>This is its own exception, rather than the SDK's raw 404, because the two failures an
/// operator confuses are "the container is missing" and "the credential cannot see it", and only
/// the first has a one-line fix.</para>
/// </summary>
public sealed class SnapshotBlobContainerMissingException : InvalidOperationException
{
    public string ContainerName { get; }

    private SnapshotBlobContainerMissingException(string containerName, string message, Exception? innerException)
        : base(message, innerException) => ContainerName = containerName;

    /// <param name="purpose">What the container is for, in the operator's words — e.g. "write-gate".</param>
    internal static SnapshotBlobContainerMissingException For(
        string containerName,
        string purpose,
        Exception? innerException = null) => new(
            containerName,
            $"The {purpose} container '{containerName}' does not exist on the configured storage account. " +
            "An operator must create it once — by hand, by script, or in the infrastructure definition — " +
            "before this can start. This engine deliberately never creates or deletes containers, whatever " +
            "the credential allows, so that it runs under a credential scoped to exactly the container it " +
            "was given and a wrong container name fails here instead of quietly becoming a new empty one. " +
            "No work was admitted.",
            innerException);
}
