namespace ShiftSoftware.ADP.Darlastic.Data;

/// <summary>
/// SQL for the registry's serving views. EF's <c>ToView</c> mapping deliberately generates no
/// DDL, so a HOST migration creates the view by calling these in <c>migrationBuilder.Sql(...)</c>:
///
///   Up:   migrationBuilder.Sql(DarlasticViews.CreateGoldenCustomerSql());
///   Down: migrationBuilder.Sql(DarlasticViews.DropGoldenCustomerSql());
///
/// Versioning contract: this SQL is APPEND-ONLY in spirit — if a future package version changes
/// a view's shape, the host adds a NEW migration (drop + recreate via the new constant); an
/// already-applied migration keeps producing whatever it produced when it was written, which is
/// fine because migrations replay against fresh databases in order.
/// </summary>
public static class DarlasticViews
{
    /// <summary>
    /// One row per live golden identity, unpacked from the canonical payload the resolve engine
    /// stages in ProjectionState (ArtifactType 'golden', shape
    /// <c>{"goldenId":N,"attrs":[{"t","v"}...],"members":[...]}</c> — authoritative producer:
    /// ADP.Darlastic.Engine's <c>Registry.GoldenCanonical</c>). The extraction mirrors the Cosmos
    /// drain exactly — attrs are staged sorted by (type, value) and the drain last-wins per type,
    /// which is MAX(v) — so this view and the drained GoldenCustomerModel docs always agree.
    /// Tombstoned artifacts (redirected-away identities) and pre-backfill rows without payload
    /// are excluded, matching what exists downstream.
    ///
    /// Scale note: a query range-seeks the 'golden' slice of ProjectionState's clustered PK and
    /// OPENJSONs each payload (~tens of ms at a ~24K-golden corpus). If a tenant outgrows that,
    /// the upgrade path is a persisted projection table maintained by the drain — not indexes
    /// here (you can't index a view over OPENJSON).
    /// </summary>
    public static string CreateGoldenCustomerSql(string schema = "Darlastic") =>
        $"CREATE VIEW [{schema}].[GoldenCustomer] AS\n{GoldenCustomerSelect(schema)}";

    /// <summary>
    /// The persisted projection the scale note above names, for a tenant that has outgrown the OPENJSON view
    /// (at ~960K goldens the list's first page, sorted by source count, takes over ten seconds: every request
    /// unpacks every payload). Opt-in per host, by migration, in three steps:
    ///
    ///   Up:   migrationBuilder.Sql(DarlasticViews.CreateGoldenCustomerRowsSql());
    ///         migrationBuilder.Sql(DarlasticViews.RefreshGoldenCustomerRowsSql());
    ///         migrationBuilder.Sql(DarlasticViews.GoldenCustomerOverRowsSql());
    ///   Down: migrationBuilder.Sql(DarlasticViews.GoldenCustomerOverRegistrySql());
    ///         migrationBuilder.Sql(DarlasticViews.DropGoldenCustomerRowsSql());
    ///
    /// The view keeps its name and shape, so nothing that reads it changes; it then reads the table, which holds
    /// what the registry held at the last refresh. The host refreshes it after every committed resolve
    /// (<see cref="Services.GoldenCustomerRows.RefreshAsync"/>): a host that adopts the table and does not
    /// refresh it serves a stale list.
    /// </summary>
    public static string CreateGoldenCustomerRowsSql(string schema = "Darlastic") => $"""
        CREATE TABLE [{schema}].[GoldenCustomerRow] (
            ID bigint NOT NULL CONSTRAINT [PK_GoldenCustomerRow] PRIMARY KEY,
            FullName nvarchar(1024) NULL,
            Phone nvarchar(1024) NULL,
            City nvarchar(1024) NULL,
            IDNumber nvarchar(1024) NULL,
            Email nvarchar(1024) NULL,
            SourceCount int NOT NULL
        );
        CREATE INDEX [IX_GoldenCustomerRow_SourceCount] ON [{schema}].[GoldenCustomerRow] (SourceCount DESC, ID);
        """;

    /// <summary>
    /// Refill the rows table from the registry in one transaction (readers under row versioning keep seeing the
    /// previous rows until it commits). 15 to 40 s at ~960K goldens.
    /// </summary>
    public static string RefreshGoldenCustomerRowsSql(string schema = "Darlastic") => $"""
        SET XACT_ABORT ON;
        BEGIN TRANSACTION;
        DELETE FROM [{schema}].[GoldenCustomerRow];
        INSERT INTO [{schema}].[GoldenCustomerRow] (ID, FullName, Phone, City, IDNumber, Email, SourceCount)
        {GoldenCustomerSelect(schema)};
        COMMIT TRANSACTION;
        """;

    /// <summary>Point the view at the rows table (same name, same columns).</summary>
    public static string GoldenCustomerOverRowsSql(string schema = "Darlastic") => $"""
        CREATE OR ALTER VIEW [{schema}].[GoldenCustomer] AS
        SELECT ID, FullName, Phone, City, IDNumber, Email, SourceCount FROM [{schema}].[GoldenCustomerRow]
        """;

    /// <summary>Point the view back at the registry (what <see cref="CreateGoldenCustomerSql"/> creates).</summary>
    public static string GoldenCustomerOverRegistrySql(string schema = "Darlastic") =>
        $"CREATE OR ALTER VIEW [{schema}].[GoldenCustomer] AS\n{GoldenCustomerSelect(schema)}";

    public static string DropGoldenCustomerRowsSql(string schema = "Darlastic") =>
        $"DROP TABLE [{schema}].[GoldenCustomerRow]";

    private static string GoldenCustomerSelect(string schema) => $"""
        SELECT CAST(ps.ArtifactKey AS bigint) AS ID,
               a.FullName,
               a.Phone,
               a.City,
               a.IDNumber,
               a.Email,
               ISNULL(m.SourceCount, 0) AS SourceCount
        FROM [{schema}].[ProjectionState] ps
        OUTER APPLY (
            SELECT MAX(CASE WHEN j.t = 'full_name'   THEN j.v END) AS FullName,
                   MAX(CASE WHEN j.t = 'phone'       THEN j.v END) AS Phone,
                   MAX(CASE WHEN j.t = 'city'        THEN j.v END) AS City,
                   MAX(CASE WHEN j.t = 'national_id' THEN j.v END) AS IDNumber,
                   MAX(CASE WHEN j.t = 'email'       THEN j.v END) AS Email
            FROM OPENJSON(ps.Payload, '$.attrs')
                 WITH (t nvarchar(64) '$.t', v nvarchar(1024) '$.v') j
        ) a
        OUTER APPLY (
            SELECT COUNT(*) AS SourceCount FROM OPENJSON(ps.Payload, '$.members')
        ) m
        WHERE ps.ArtifactType = 'golden'
          AND ps.ContentHash <> 'TOMBSTONE'
          AND ps.Payload IS NOT NULL
        """;

    public static string DropGoldenCustomerSql(string schema = "Darlastic") =>
        $"DROP VIEW [{schema}].[GoldenCustomer]";
}
