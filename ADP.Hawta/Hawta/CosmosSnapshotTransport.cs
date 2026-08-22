using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Cosmos;

namespace ShiftSoftware.ADP.Hawta;

internal readonly record struct CosmosContainerKey(string Database, string Container);

internal sealed record CosmosTransportResponse(
    HttpStatusCode StatusCode,
    bool IsSuccessStatusCode,
    double RequestCharge,
    TimeSpan? RetryAfter,
    string? ErrorMessage);

internal sealed class CosmosRequestPreparationException(string message, Exception innerException)
    : Exception(message, innerException);

/// <summary>
/// A resolved container handle. The replicator resolves every handle before it starts row
/// workers, then workers only read the immutable handle lookup.
/// </summary>
internal interface ICosmosSnapshotContainer
{
    Task<CosmosTransportResponse> UpsertAsync(
        CosmosDocument document,
        CancellationToken cancellationToken);

    Task<CosmosTransportResponse> DeleteAsync(
        string id,
        IReadOnlyList<object?> partitionKey,
        CancellationToken cancellationToken);
}

internal interface ICosmosSnapshotTransport
{
    ICosmosSnapshotContainer GetContainer(string database, string container);
}

internal sealed class CosmosClientSnapshotTransport(CosmosClient client) : ICosmosSnapshotTransport
{
    public ICosmosSnapshotContainer GetContainer(string database, string container) =>
        new CosmosClientSnapshotContainer(client.GetContainer(database, container));
}

internal sealed class CosmosClientSnapshotContainer(Container container) : ICosmosSnapshotContainer
{
    public async Task<CosmosTransportResponse> UpsertAsync(
        CosmosDocument document,
        CancellationToken cancellationToken)
    {
        byte[] bodyBytes;
        PartitionKey partitionKey;
        try
        {
            var body = new Dictionary<string, object?>(document.Body) { ["id"] = document.Id };
            bodyBytes = JsonSerializer.SerializeToUtf8Bytes(body);
            partitionKey = BuildPartitionKey(document.PartitionKey);
        }
        catch (Exception exception)
        {
            throw new CosmosRequestPreparationException(
                "The mapped Cosmos document could not be serialized or addressed.",
                exception);
        }

        using var stream = new MemoryStream(bodyBytes);
        using var response = await container.UpsertItemStreamAsync(
            stream,
            partitionKey,
            cancellationToken: cancellationToken);

        return ToResult(response);
    }

    public async Task<CosmosTransportResponse> DeleteAsync(
        string id,
        IReadOnlyList<object?> partitionKey,
        CancellationToken cancellationToken)
    {
        PartitionKey resolvedPartitionKey;
        try
        {
            resolvedPartitionKey = BuildPartitionKey(partitionKey);
        }
        catch (Exception exception)
        {
            throw new CosmosRequestPreparationException(
                "The stamped Cosmos partition key could not be addressed.",
                exception);
        }

        using var response = await container.DeleteItemStreamAsync(
            id,
            resolvedPartitionKey,
            cancellationToken: cancellationToken);

        return ToResult(response);
    }

    private static CosmosTransportResponse ToResult(ResponseMessage response)
    {
        var retryAfter = double.TryParse(
            response.Headers["x-ms-retry-after-ms"],
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out var retryAfterMilliseconds)
            ? TimeSpan.FromMilliseconds(retryAfterMilliseconds)
            : (TimeSpan?)null;

        return new CosmosTransportResponse(
            response.StatusCode,
            response.IsSuccessStatusCode,
            response.Headers.RequestCharge,
            retryAfter,
            response.ErrorMessage);
    }

    private static PartitionKey BuildPartitionKey(IReadOnlyList<object?> levels)
    {
        var builder = new PartitionKeyBuilder();
        foreach (var level in levels)
        {
            switch (level)
            {
                case string value: builder.Add(value); break;
                case bool value: builder.Add(value); break;
                // This is an explicit null partition-key value. It is deliberately not
                // PartitionKey.None, which addresses a different Cosmos partition.
                case null: builder.Add((string?)null); break;
                default: builder.Add(Convert.ToDouble(level)); break;
            }
        }

        return builder.Build();
    }
}

internal interface IReplicationStateStore
{
    IReadOnlyList<DirtyRow> ReadDirtyRows(
        SnapshotTableDefinition table,
        string? afterPrimaryKey,
        int limit);

    ReplicationGroupPage ReadReplicationGroups(
        SnapshotTableDefinition table,
        CosmosGroupProjection grouping,
        string? afterGroupKey,
        int limit,
        bool dirtyGroupsOnly) =>
        throw new NotSupportedException("This replication-state test double does not support grouped projections.");

    /// <summary>
    /// Unreplicated rows past the attempt limit — the drain report's settled-vs-drained
    /// divergence. Defaulted to zero so replication-state test doubles that model no failure
    /// ledger keep compiling; the real store counts.
    /// </summary>
    long CountDeadLetteredRows(SnapshotTableDefinition table) => 0;

    void PruneReconOps(SnapshotTableDefinition table);
    void AppendReconOp(ReplicationReconOperation operation);

    void CommitReplicationOutcomes(
        SnapshotTableDefinition table,
        IReadOnlyList<ReplicationStateOutcome> outcomes,
        Action ensureCommitAllowed);
}

internal enum ReplicationStateOutcomeKind
{
    Replicated,
    Failed,
}

/// <summary>
/// A terminal row outcome produced by the Cosmos side of the pump. Successful outcomes
/// carry the exact captured watermark and stamp that landed remotely; failed outcomes carry
/// the error to add once to that captured version's retry ledger.
/// </summary>
internal sealed record ReplicationStateOutcome(
    string PrimaryKey,
    DateTime CapturedLastModified,
    ReplicationStateOutcomeKind Kind,
    string? ReplicationStamp,
    string? Error)
{
    public static ReplicationStateOutcome Replicated(
        string primaryKey,
        DateTime capturedLastModified,
        string? replicationStamp) =>
        new(
            primaryKey,
            capturedLastModified,
            ReplicationStateOutcomeKind.Replicated,
            replicationStamp,
            Error: null);

    public static ReplicationStateOutcome Failed(
        string primaryKey,
        DateTime capturedLastModified,
        string error) =>
        new(
            primaryKey,
            capturedLastModified,
            ReplicationStateOutcomeKind.Failed,
            ReplicationStamp: null,
            error);
}

internal sealed record ReplicationReconOperation(
    string RunId,
    string TargetTable,
    string Family,
    string PrimaryKey,
    string Operation,
    string? CosmosId,
    string? PartitionKey,
    string? DocumentHash,
    DateTime CapturedLastModified,
    DateTime EmittedAt);

internal sealed class SnapshotReplicationStateStore(SnapshotStore store) : IReplicationStateStore
{
    public IReadOnlyList<DirtyRow> ReadDirtyRows(
        SnapshotTableDefinition table,
        string? afterPrimaryKey,
        int limit) =>
        store.ReadDirtyRows(table, afterPrimaryKey, limit);

    public ReplicationGroupPage ReadReplicationGroups(
        SnapshotTableDefinition table,
        CosmosGroupProjection grouping,
        string? afterGroupKey,
        int limit,
        bool dirtyGroupsOnly) =>
        store.ReadReplicationGroups(table, grouping, afterGroupKey, limit, dirtyGroupsOnly);

    public long CountDeadLetteredRows(SnapshotTableDefinition table) => store.CountDeadLetteredRows(table);

    public void PruneReconOps(SnapshotTableDefinition table) => store.PruneReconOps(table);

    public void AppendReconOp(ReplicationReconOperation operation) =>
        store.Execute(
            "INSERT INTO meta.ReconOps VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            operation.RunId,
            operation.TargetTable,
            operation.Family,
            operation.PrimaryKey,
            operation.Operation,
            operation.CosmosId,
            operation.PartitionKey,
            operation.DocumentHash,
            operation.CapturedLastModified,
            operation.EmittedAt);

    public void CommitReplicationOutcomes(
        SnapshotTableDefinition table,
        IReadOnlyList<ReplicationStateOutcome> outcomes,
        Action ensureCommitAllowed) =>
        store.CommitReplicationOutcomes(table, outcomes, ensureCommitAllowed);
}
