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
    public static string CreateGoldenCustomerSql(string schema = "Darlastic") => $"""
        CREATE VIEW [{schema}].[GoldenCustomer] AS
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
