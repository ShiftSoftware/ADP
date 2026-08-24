using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Text;

namespace ShiftSoftware.ADP.Hawta;

/// <summary>One entry in a published location. <paramref name="RelativePath"/> is always forward-slashed.</summary>
public sealed record PublishEntry(string Location, string RelativePath, long Length, DateTime LastWriteUtc);

/// <summary>
/// A payload prepared for commit but not yet visible under its final name.
/// <paramref name="StagedLocation"/> is non-null only where the store materialises the staged
/// artifact — a filesystem does, blob does not.
/// </summary>
public sealed record PendingCommit(string FinalLocation, string? StagedLocation, string Content);

/// <summary>
/// The publish tier's storage surface — a local directory, an SMB share, or a blob container prefix.
///
/// <para>This exists because the tier's correctness rests on a handful of storage behaviours that
/// <see cref="File"/> and <see cref="Directory"/> express one way and blob expresses another, and
/// because the two disagree <b>silently</b> in the cases that matter. Measured against a real
/// container on 2026-08-15:</para>
///
/// <list type="bullet">
///   <item><b>Absence and unreachability are the same answer.</b> <c>Directory.Exists("az://…")</c>
///     returns <c>false</c> rather than throwing, and DuckDB's <c>glob</c> over a missing prefix
///     returns zero rows rather than raising. "Nothing has been published yet" and "the store is
///     unreachable" demand opposite responses — seed from source versus stop — so <see cref="EnsureReady"/>
///     is a separate, mandatory question that a listing result can never answer.</item>
///   <item><b>Existence is not completeness.</b> A parquet under construction is visible at zero
///     bytes for the whole write and then jumps to full size. Content is never torn, but no caller
///     may treat presence as validity.</item>
///   <item><b>There is no atomic rename.</b> The commit primitive is <see cref="TryCommitNew"/>,
///     which is <c>File.Move(…, overwrite: false)</c> locally and a conditional PUT
///     (<c>If-None-Match: *</c>) on blob. Both refuse to replace an existing name, which is what
///     makes a manifest appear under its final name in exactly one operation.</item>
/// </list>
///
/// <para><b>Not for the write database.</b> That is instance-local disk by design — DuckDB on a
/// network filesystem is a correctness risk, not a latency one — so it stays on plain
/// <see cref="File"/>/<see cref="Directory"/> calls and must never be routed through here.</para>
/// </summary>
public abstract class PublishStore
{
    /// <summary>The location, safe to log. Implementations must never expose a credential here.</summary>
    public abstract string Root { get; }

    /// <summary>
    /// Proves the store is reachable and usable, creating it where that is meaningful. <b>Throws</b>
    /// when it is not — which is the entire point, because every other operation reports an
    /// unreachable store as an empty one.
    /// </summary>
    public abstract void EnsureReady();

    /// <summary>
    /// Every entry under the root, recursively, newest-agnostic. <paramref name="relativePrefix"/>
    /// narrows to a subtree. Returns an empty list for a subtree that holds nothing — callers that
    /// need to distinguish that from an unreachable store must have called <see cref="EnsureReady"/>.
    /// </summary>
    public abstract IReadOnlyList<PublishEntry> List(string? relativePrefix = null);

    /// <summary>Whether a location exists. Says nothing about whether its contents are complete.</summary>
    public abstract bool Exists(string location);

    public abstract string ReadAllText(string location);

    /// <summary>Writes, replacing any existing content. Never the commit — see <see cref="TryCommitNew"/>.</summary>
    public abstract void WriteAllText(string location, string content);

    /// <summary>
    /// First half of the atomic commit: gets the payload as close to committed as the store allows
    /// <b>without</b> making it visible under its final name.
    ///
    /// <para>The split is not ceremony. On a filesystem this materialises a complete, readable
    /// artifact under a staging name — so at the moment of commit the whole set genuinely exists
    /// and only the name is missing, which is what makes the rename atomic rather than merely
    /// quick. Publish drills inject their fault between the two halves and assert exactly that.
    /// On blob nothing is written until <see cref="Commit"/>, because the conditional PUT is
    /// already a single operation and a staging blob would add a name retention cannot recognise.</para>
    /// </summary>
    public abstract PendingCommit PrepareCommit(string location, string content);

    /// <summary>
    /// Second half: publishes the prepared payload under its final name, or returns false because
    /// the name is already taken. A caller must not "helpfully" fall back to
    /// <see cref="WriteAllText"/> on false — the refusal is the guarantee.
    /// </summary>
    public abstract bool Commit(PendingCommit pending);

    /// <summary>Both halves at once, for callers with nothing to do in between.</summary>
    public bool TryCommitNew(string location, string content) => Commit(PrepareCommit(location, content));

    /// <summary>Deletes if present. Returns whether anything was removed. Must not throw for absence.</summary>
    public abstract bool Delete(string location);

    /// <summary>
    /// Last-write time, or null when it cannot be established. Retention's age floor is measured
    /// from this and <b>never</b> from the timestamp in the name: the name records when a publish
    /// STARTED, and a large export finishes minutes later — it is the finish that answers "could
    /// something still be writing this".
    /// </summary>
    public abstract DateTime? LastWriteUtc(string location);

    /// <summary>
    /// Whether a <b>bulk</b> write — DuckDB's <c>COPY … TO</c>, which this type never carries itself —
    /// has to land on a staging name and be renamed, or may target its final name directly.
    ///
    /// <para>True on a filesystem, where a reader can observe a half-written file. False on blob,
    /// where the block-blob commit already makes content appear all at once: measured, a parquet
    /// under construction is visible at zero bytes and then jumps to full size, so a staging name
    /// would buy nothing and cost a second name that retention does not recognise.</para>
    /// </summary>
    public abstract bool BulkWriteNeedsStaging { get; }

    /// <summary>Moves a staged bulk write onto its final name. Only called when <see cref="BulkWriteNeedsStaging"/>.</summary>
    public abstract void PromoteStaged(string stagingLocation, string finalLocation);

    /// <summary>Prepares the containing folder for a write. A no-op where the namespace is flat.</summary>
    public abstract void EnsureFolderFor(string location);

    /// <summary>Joins a forward-slashed relative path onto the root.</summary>
    public string Resolve(string relativePath) => PublishPath.Combine(Root, relativePath);
}

/// <summary>A local directory or an SMB share. The incumbent behaviour, unchanged.</summary>
public sealed class LocalPublishStore : PublishStore
{
    public LocalPublishStore(string directory)
    {
        // A URI reaching a System.IO store is the silent-failure case this whole abstraction
        // exists to prevent, so it is rejected at construction rather than at first use.
        // Directory.Exists("az://…") answers FALSE without throwing, and every caller reads that
        // as "nothing is published yet" — a full re-export against an empty baseline, a skipped
        // stamp-monotonicity clamp, and a cold start concluding the published set is gone. The
        // guard used to sit in SnapshotPublisher; here it covers every caller instead of one.
        PublishPath.RequireLocal(directory, $"Opening a local publish store at '{directory}'");
        Root = directory;
    }

    public override string Root { get; }

    public override void EnsureReady() => Directory.CreateDirectory(Root);

    public override IReadOnlyList<PublishEntry> List(string? relativePrefix = null)
    {
        var start = relativePrefix is null ? Root : PublishPath.Combine(Root, relativePrefix);
        if (!Directory.Exists(start))
            return [];

        var entries = new List<PublishEntry>();
        foreach (var path in Directory.EnumerateFiles(start, "*", SearchOption.AllDirectories))
        {
            FileInfo info;
            try
            {
                info = new FileInfo(path);
                _ = info.Length;
            }
            catch (IOException) { continue; }
            catch (UnauthorizedAccessException) { continue; }

            entries.Add(new PublishEntry(
                path,
                Path.GetRelativePath(Root, path).Replace(Path.DirectorySeparatorChar, '/'),
                info.Length,
                info.LastWriteTimeUtc));
        }
        return entries;
    }

    public override bool BulkWriteNeedsStaging => true;

    /// <summary>
    /// Overwrite is deliberate: a colliding final name can only be an unreferenced orphan from a
    /// crashed run, because every referenced name carries a stamp older than the current publish's.
    /// </summary>
    public override void PromoteStaged(string stagingLocation, string finalLocation) =>
        File.Move(stagingLocation, finalLocation, overwrite: true);

    public override void EnsureFolderFor(string location)
    {
        var parent = Path.GetDirectoryName(location);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
    }

    public override bool Exists(string location) => File.Exists(location);

    public override string ReadAllText(string location) => File.ReadAllText(location);

    public override void WriteAllText(string location, string content)
    {
        var parent = Path.GetDirectoryName(location);
        if (!string.IsNullOrEmpty(parent))
            Directory.CreateDirectory(parent);
        File.WriteAllText(location, content);
    }

    /// <summary>
    /// Materialises the payload under a <c>.staging</c> name. After this returns, a complete and
    /// readable artifact exists on disk — only its name keeps it invisible to resolution.
    /// </summary>
    public override PendingCommit PrepareCommit(string location, string content)
    {
        EnsureFolderFor(location);
        var staging = location + ".staging";
        File.WriteAllText(staging, content);
        return new PendingCommit(location, staging, content);
    }

    /// <summary>
    /// <c>File.Move(overwrite: false)</c> — the rename is the single operation in which the name
    /// appears, and the <c>false</c> is what makes a second writer lose rather than clobber.
    /// </summary>
    public override bool Commit(PendingCommit pending)
    {
        try
        {
            File.Move(pending.StagedLocation!, pending.FinalLocation, overwrite: false);
            return true;
        }
        catch (IOException)
        {
            // The name was taken between the write and the move. Leave the destination alone.
            TryRemove(pending.StagedLocation!);
            return false;
        }
    }

    public override bool Delete(string location)
    {
        try
        {
            if (!File.Exists(location))
                return false;
            File.Delete(location);
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }

    public override DateTime? LastWriteUtc(string location)
    {
        try
        {
            return File.Exists(location) ? File.GetLastWriteTimeUtc(location) : null;
        }
        catch (IOException) { return null; }
        catch (UnauthorizedAccessException) { return null; }
    }

    private static void TryRemove(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}

/// <summary>
/// An Azure blob container, optionally under a prefix.
///
/// <para>Parquet still moves through DuckDB (<c>COPY … TO 'az://…'</c> / <c>read_parquet</c>) — this
/// type deliberately does <b>not</b> carry bulk data. It exists for the operations DuckDB either
/// cannot do or does without the metadata the tier needs: listing with <c>LastModified</c> (which
/// <c>glob</c> does not return, and which retention's age floor is measured from), the conditional
/// commit, and delete.</para>
/// </summary>
public sealed class BlobPublishStore : PublishStore
{
    private readonly BlobContainerClient container;
    private readonly string prefix;

    /// <param name="prefix">Optional forward-slashed prefix inside the container. No leading or trailing slash.</param>
    public BlobPublishStore(string connectionString, string containerName, string? prefix = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentException.ThrowIfNullOrWhiteSpace(containerName);

        container = new BlobContainerClient(connectionString, containerName);
        this.prefix = prefix?.Trim('/') ?? string.Empty;

        // Root is used in manifest paths and log lines, so it is the SCHEME form and never the
        // credentialed URL — a connection string carrying a SAS must not reach a log or a manifest.
        Root = this.prefix.Length == 0
            ? $"az://{containerName}"
            : $"az://{containerName}/{this.prefix}";
    }

    public override string Root { get; }

    /// <summary>
    /// Reaches the service. A container-scoped SAS cannot create a container, so this deliberately
    /// does not try: it asks whether the container is there and turns "no" into a throw. That is the
    /// distinction the rest of the tier cannot make for itself — every listing reports an
    /// unreachable store as an empty one.
    ///
    /// <para>Not creating it is the standing rule rather than a concession to the narrow credential:
    /// the engine never creates or deletes a container, whatever the credential would allow. See
    /// <see cref="SnapshotBlobContainerMissingException"/>. It is the one place this store parts
    /// company with <see cref="LocalPublishStore"/>, which does create its directory — a directory
    /// is not a permission boundary, and nobody scopes a credential to one.</para>
    /// </summary>
    public override void EnsureReady()
    {
        try
        {
            // Deliberately a LISTING, not container.Exists(). Exists() issues a container-level
            // GetProperties — a permission this tier never otherwise needs — so probing with it is
            // stricter than the real requirement and fails under credentials everything else works
            // under. Listing is also the more honest probe: it is the operation whose SILENCE is
            // the hazard. An empty container enumerates to nothing and is a legitimate "nothing
            // published yet"; an unreachable one throws here instead of enumerating to that same
            // nothing and being believed.
            foreach (var _ in container.GetBlobs(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None).Take(1))
                break;
        }
        catch (RequestFailedException exception) when (exception.ErrorCode == BlobErrorCode.ContainerNotFound)
        {
            // Separated from the reachability failure below because it is the one with a one-line
            // fix, and because "not there" and "cannot see it" are the two an operator confuses.
            throw SnapshotBlobContainerMissingException.For(container.Name, "publish", exception);
        }
        catch (RequestFailedException exception)
        {
            throw new InvalidOperationException(
                $"The publish container '{container.Name}' could not be reached ({exception.Status} " +
                $"{exception.ErrorCode}). Refusing to continue rather than reading the failure as an " +
                "empty published set — that would look like 'nothing has ever been published' and " +
                "re-seed the estate from source.", exception);
        }
    }

    public override IReadOnlyList<PublishEntry> List(string? relativePrefix = null)
    {
        var search = Combine(relativePrefix);
        var entries = new List<PublishEntry>();

        // Flat listing: blob has no directories, so this is recursive by nature and there is no
        // SearchOption.TopDirectoryOnly analogue. Callers needing top-level-only semantics filter on
        // RelativePath containing no '/'. The listing carries Length and LastModified, so the age
        // floor and the size checks cost no extra round trip.
        foreach (var blob in container.GetBlobs(BlobTraits.None, BlobStates.None, search, CancellationToken.None))
        {
            var relative = prefix.Length == 0 ? blob.Name : blob.Name[(prefix.Length + 1)..];
            entries.Add(new PublishEntry(
                $"az://{container.Name}/{blob.Name}",
                relative,
                blob.Properties.ContentLength.GetValueOrDefault(),
                blob.Properties.LastModified?.UtcDateTime ?? DateTime.MinValue));
        }
        return entries;
    }

    public override bool BulkWriteNeedsStaging => false;

    public override void PromoteStaged(string stagingLocation, string finalLocation) =>
        throw new NotSupportedException(
            "Blob writes are committed by the service, so a staged bulk write is never produced and " +
            "never needs promoting. Reaching here means a caller ignored BulkWriteNeedsStaging.");

    /// <summary>Blob namespaces are flat — the '/' in a name is a convention, not a directory.</summary>
    public override void EnsureFolderFor(string location) { }

    public override bool Exists(string location) => container.GetBlobClient(NameOf(location)).Exists();

    public override string ReadAllText(string location) =>
        container.GetBlobClient(NameOf(location)).DownloadContent().Value.Content.ToString();

    public override void WriteAllText(string location, string content) =>
        container.GetBlobClient(NameOf(location))
            .Upload(new BinaryData(Encoding.UTF8.GetBytes(content)), overwrite: true);

    /// <summary>
    /// Writes nothing. A block-blob commit already makes content appear all at once, so a staging
    /// blob would add a second name that retention does not recognise — which is how orphans become
    /// permanent — in exchange for a guarantee the service already provides.
    /// </summary>
    public override PendingCommit PrepareCommit(string location, string content) =>
        new(location, StagedLocation: null, content);

    /// <summary>
    /// A conditional PUT. <c>overwrite: false</c> sends <c>If-None-Match: *</c>, and the service
    /// answers 409 <c>BlobAlreadyExists</c> with the incumbent content untouched — measured against
    /// a real container through a container-scoped SAS, not assumed.
    /// </summary>
    public override bool Commit(PendingCommit pending)
    {
        try
        {
            container.GetBlobClient(NameOf(pending.FinalLocation))
                .Upload(new BinaryData(Encoding.UTF8.GetBytes(pending.Content)), overwrite: false);
            return true;
        }
        catch (RequestFailedException exception) when (exception.Status == 409)
        {
            return false;
        }
    }

    public override bool Delete(string location) =>
        container.GetBlobClient(NameOf(location)).DeleteIfExists();

    public override DateTime? LastWriteUtc(string location)
    {
        try
        {
            return container.GetBlobClient(NameOf(location)).GetProperties().Value.LastModified.UtcDateTime;
        }
        catch (RequestFailedException)
        {
            return null;
        }
    }

    private string Combine(string? relativePrefix)
    {
        if (string.IsNullOrEmpty(relativePrefix))
            return prefix;
        return prefix.Length == 0 ? relativePrefix.Trim('/') : $"{prefix}/{relativePrefix.Trim('/')}";
    }

    /// <summary>Turns a full <c>az://container/name</c> location back into the blob name the SDK wants.</summary>
    private string NameOf(string location)
    {
        var expected = $"az://{container.Name}/";
        if (location.StartsWith(expected, StringComparison.OrdinalIgnoreCase))
            return location[expected.Length..];

        // A caller passed something relative. Accept it rather than producing a nonsense blob name.
        return Combine(location);
    }
}
