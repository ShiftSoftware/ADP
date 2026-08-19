namespace ShiftSoftware.ADP.Hawta;

/// <summary>
/// The bookkeeping columns Hawta adds to every snapshot table. Ingestors populate
/// <c>_PrimaryKey</c>/<c>_RowHash</c>/<c>_ReplicationHash</c> (and optionally a source-modified timestamp) in staging;
/// <see cref="SnapshotMerge"/> owns every other column. Replication columns carry the same
/// semantics as ShiftEntity's <c>IShiftEntityReplication</c>: <c>_LastReplicationDate</c> is
/// the <c>_ReplicationModified</c> of the document-affecting row version that was pushed —
/// never wall clock. <c>_LastModified</c> remains the truthful stamp for any stored source change.
///
/// <para>The downstream incremental-ingestion contract adds only <c>_ChangeSequence</c>, a
/// store-wide durable increasing version number, and <c>_ChangeRecordedAt</c>, when Hawta accepted
/// that version. Source ownership and key semantics are published once in the manifest catalog,
/// not denormalized onto every row. The sequence/timestamp advance only for an insert, content
/// change, resurrection, source/scope adoption, or tombstone — never an unchanged scan or forced
/// republish.</para>
/// </summary>
public static class BookkeepingColumns
{
    public const string PrimaryKey = "_PrimaryKey";
    public const string RowHash = "_RowHash";
    public const string ReplicationHash = "_ReplicationHash";
    public const string SourceScope = "_SourceScope";
    public const string LastModified = "_LastModified";
    public const string ReplicationModified = "_ReplicationModified";
    public const string Deleted = "_Deleted";
    public const string DeletedAt = "_DeletedAt";
    public const string LastReplicationDate = "_LastReplicationDate";
    public const string ReplicationStamp = "_ReplicationStamp";
    public const string ReplicationAttempts = "_ReplicationAttempts";
    public const string ReplicationError = "_ReplicationError";
    public const string ReplicatedAt = "_ReplicatedAt";
    public const string ChangeSequence = "_ChangeSequence";
    public const string ChangeRecordedAt = "_ChangeRecordedAt";

    /// <summary>All bookkeeping column names, in table-DDL order.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        PrimaryKey, RowHash, ReplicationHash, SourceScope, LastModified, ReplicationModified, Deleted, DeletedAt,
        LastReplicationDate, ReplicationStamp, ReplicationAttempts, ReplicationError, ReplicatedAt,
        ChangeSequence, ChangeRecordedAt,
    ];

    internal const string TableDdl =
        $"""
        "{PrimaryKey}" VARCHAR NOT NULL,
        "{RowHash}" VARCHAR NOT NULL,
        "{ReplicationHash}" VARCHAR NOT NULL,
        "{SourceScope}" VARCHAR,
        "{LastModified}" TIMESTAMP NOT NULL,
        "{ReplicationModified}" TIMESTAMP NOT NULL,
        "{Deleted}" BOOLEAN NOT NULL DEFAULT false,
        "{DeletedAt}" TIMESTAMP,
        "{LastReplicationDate}" TIMESTAMP,
        "{ReplicationStamp}" VARCHAR,
        "{ReplicationAttempts}" INTEGER NOT NULL DEFAULT 0,
        "{ReplicationError}" VARCHAR,
        "{ReplicatedAt}" TIMESTAMP,
        "{ChangeSequence}" BIGINT NOT NULL,
        "{ChangeRecordedAt}" TIMESTAMP NOT NULL
        """;
}
