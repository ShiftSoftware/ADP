using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;

namespace ShiftSoftware.ADP.Darlastic.Engine;

/// <summary>
/// Darlastic V0 — the SQL registry that turns the spike's stateless clustering into STABLE golden
/// identities, plus the hash-guarded projection staging (the delta-out discipline).
///
/// Contract (deployment-architecture.md "Golden-ID stability"):
///   - Golden IDs are append-only and never reused. A merge writes a redirect (old -> surviving),
///     never a delete; a split keeps the ID with the max-overlap fragment and mints for the rest.
///     Redirect rows are chain-compressed on write, so a single hop always lands on a live identity.
///   - Reconciliation is MAX-OVERLAP: each prior identity elects the current cluster holding most of
///     its members; each cluster inherits the strongest claimant that elected it (ties broken
///     deterministically), else mints. Minting order is deterministic (clusters sorted by their
///     smallest member key), so identical input state yields identical IDs.
///   - Projection is DELTA-ONLY: every would-be Cosmos artifact is canonically serialized and
///     content-hashed into ProjectionState; only hash changes mark Pending. Two consecutive runs on
///     unchanged sources must mark ZERO deltas of any kind (the D3 acceptance test).
///
/// Resilience (2026-07-18 adversarial review):
///   - Profiles are SOFT-removed (Removed flag), never deleted — a record that reappears after a
///     transient source glitch votes for (and reactivates) its old identity instead of minting.
///   - A source system with prior rows but ZERO current records is treated as ABSENT (skipped for
///     removal/deactivation/tombstoning), not as "every customer left" — a missing dealer file must
///     freeze that dealer's state, not destroy it.
///   - Empty input against a non-empty registry aborts; so does a removal/deactivation wave above
///     MassChangeThreshold, unless DARLASTIC_FORCE=1.
///   - Resolve runs are serialized via sp_getapplock — concurrent runs would corrupt the
///     read-compute-write cycle.
///
/// Storage: SQL Server, schema [Darlastic], created idempotently on first run. Connection via the
/// DARLASTIC_SQL env var; defaults to localhost\SQLEXPRESS, database Darlastic (created if absent).
/// Key columns use a BIN2 collation to match the code's Ordinal string semantics. All writes are
/// set-based: bulk-copy into #temp tables, then MERGE/INSERT deltas only.
/// </summary>
public static class Registry
{
    private const string DefaultServer = @"Server=localhost\SQLEXPRESS;Integrated Security=true;TrustServerCertificate=true";
    // One registry per tenant. DARLASTIC_DB names the tenant's registry database (auto-created);
    // DARLASTIC_SQL still overrides the whole connection string for managed deployments.
    private static string DbName => Environment.GetEnvironmentVariable("DARLASTIC_DB") ?? "Darlastic";
    private const string KeyCollation = "Latin1_General_100_BIN2";
    private const double MassChangeThreshold = 0.05;   // removals+deactivations beyond this fraction of prior profiles abort
    private const string Tombstone = "TOMBSTONE";

    // Projected artifact families (ProjectionState.ArtifactType). One staging discipline, four shapes:
    //   golden   — key = goldenId; the GoldenCustomerModel doc (Customers/Customers)
    //   vinlinks — key = goldenId; the by-customer ownership-link doc (Customers/Customers, golden's partition)
    //   vinowner — key = VIN;      the by-VIN ownership-timeline doc (CompanyData/Vehicles)
    //   stamp    — key = "src|recId"; the GoldenCustomerID forward-link patch for the landing CustomerModel doc.
    //              ContentHash IS the golden id (readable ledger; the guard only needs equality). Never tombstoned:
    //              a soft-removed profile keeps its last assignment, so its landing doc keeps its last stamp —
    //              but the stamp FOLLOWS REDIRECTS: when that assignment's identity is merged away, the stamp
    //              re-stages onto the surviving id (the retired golden doc gets deleted; a frozen forward link
    //              to it would dangle on a 404 forever).
    internal const string ArtGolden = "golden";
    internal const string ArtVinLinks = "vinlinks";
    internal const string ArtVinOwner = "vinowner";
    internal const string ArtStamp = "stamp";

    public static string ConnectionString =>
        Environment.GetEnvironmentVariable("DARLASTIC_SQL")
        ?? $"{DefaultServer};Database={DbName}";

    private static bool Forced => Environment.GetEnvironmentVariable("DARLASTIC_FORCE") == "1";

    /// <summary>
    /// The host owns the schema (DARLASTIC_SCHEMA_MANAGED=1): its migrations create and version the
    /// [Darlastic] tables, and this engine must not also issue DDL. Two schema authorities against
    /// one database is how a deploy and a bootstrap silently disagree — the bootstrap's
    /// IF-OBJECT_ID-IS-NULL guards mean the disagreement shows up as a missing column mid-resolve,
    /// not as a failed migration. Deliberately NOT inferred from DARLASTIC_SQL: pointing at a
    /// managed server and wanting the dev bootstrap is a legitimate combination.
    /// </summary>
    private static bool HostManagedSchema => Environment.GetEnvironmentVariable("DARLASTIC_SCHEMA_MANAGED") == "1";

    /// <summary>Every table a resolve touches — the contract a host migration has to satisfy.
    /// Kept in sync with <c>ADP.Darlastic.Data</c>'s EF model, which is what hosts migrate from.</summary>
    private static readonly string[] RequiredTables =
    [
        "Identity", "IdentityRedirect", "SourceProfile", "ProjectionState",
        "AuditEntry", "StewardDecision", "StewardQueue", "StewardRecord",
        "ResolveRun", "TenantMarker",
    ];

    // ------------------------------------------------------------------ bootstrap

    public static SqlConnection Open()
    {
        EnsureDatabase();
        var conn = new SqlConnection(ConnectionString);
        conn.Open();
        AcquireResolveLock(conn);
        EnsureSchema(conn);
        AssertTenant(conn, stampIfMissing: true);
        return conn;
    }

    /// <summary>The tenant this process believes it is (DARLASTIC_TENANT; "default" when unset).</summary>
    private static string ExpectedTenant =>
        (Environment.GetEnvironmentVariable("DARLASTIC_TENANT") is { Length: > 0 } t ? t : "default").ToLowerInvariant();

    /// <summary>
    /// Cross-tenant guard (adversarial review 2026-07-19): the ONLY thing pairing a feed set with
    /// a registry is ambient environment variables, and a stale DARLASTIC_SQL/DARLASTIC_DB would
    /// let a resolve run one tenant's feeds against ANOTHER tenant's registry — every outage guard
    /// is structurally neutralized (disjoint source sets mean every victim source reads as ABSENT
    /// and frozen), the steward queue is destructively replaced, and ProjectionState is poisoned
    /// with the foreign corpus. The registry therefore carries its tenant name in a marker row,
    /// stamped on first resolve and asserted on every open/read thereafter.
    /// </summary>
    private static void AssertTenant(SqlConnection conn, bool stampIfMissing)
    {
        if (stampIfMissing && !HostManagedSchema)
            Exec(conn, "IF OBJECT_ID('Darlastic.TenantMarker') IS NULL CREATE TABLE Darlastic.TenantMarker (Tenant NVARCHAR(64) NOT NULL)");
        else if (Convert.ToInt32(Scalar(conn, "SELECT CASE WHEN OBJECT_ID('Darlastic.TenantMarker') IS NULL THEN 0 ELSE 1 END")) == 0)
            return;   // pre-marker registry read by a display surface — tolerate; the next resolve stamps it
        var current = Scalar(conn, "SELECT TOP 1 Tenant FROM Darlastic.TenantMarker") as string;
        if (current is null)
        {
            if (!stampIfMissing) return;
            using var cmd = new SqlCommand("INSERT INTO Darlastic.TenantMarker (Tenant) VALUES (@t)", conn);
            cmd.Parameters.AddWithValue("@t", ExpectedTenant);
            cmd.ExecuteNonQuery();
        }
        else if (!string.Equals(current, ExpectedTenant, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"This registry belongs to tenant '{current}' but the process is tenant '{ExpectedTenant}' — " +
                "DARLASTIC_TENANT / DARLASTIC_SQL / DARLASTIC_DB point at mismatched tenants. Refusing to touch it.");
    }

    /// <summary>Tenant assert for connections the Projector (or other surfaces) open directly —
    /// read-only against the marker, never stamps.</summary>
    public static void AssertTenantReadOnly(SqlConnection conn) => AssertTenant(conn, stampIfMissing: false);

    /// <summary>The registry database this process resolves to (display / reset surfaces).</summary>
    public static string DatabaseName => DbName;

    /// <summary>
    /// Dev-only re-mint: DROP this tenant's entire registry database, so the next resolve starts from
    /// nothing (fresh golden IDs, empty ProjectionState, empty steward queue). Four guards, because the
    /// name of the database to drop comes from ambient environment:
    ///   1. refuses when DARLASTIC_SCHEMA_MANAGED is set — a host's migrations own that schema;
    ///   2. refuses when DARLASTIC_SQL is set — a managed/hosted registry is never ours to drop;
    ///   3. asserts the tenant marker FIRST, so a stale DARLASTIC_DB cannot drop another tenant's registry;
    ///   4. refuses any database containing non-Darlastic tables — guards 1–3 trust configuration,
    ///      this one inspects the database itself, so a host database survives even a fully stale env.
    /// Returns the dropped database name, or null when it did not exist.
    /// </summary>
    public static string? DropDatabase()
    {
        if (HostManagedSchema)
            throw new InvalidOperationException(
                "Refusing to drop: DARLASTIC_SCHEMA_MANAGED=1, so the schema belongs to the host's migrations. Dropping it would leave the host's migration history claiming tables that no longer exist.");

        if (Environment.GetEnvironmentVariable("DARLASTIC_SQL") is not null)
            throw new InvalidOperationException(
                "Refusing to drop: DARLASTIC_SQL is set, so the registry is an externally-managed database. Drop it yourself if that is really the intent.");

        using (var probe = new SqlConnection(ConnectionString))
        {
            try
            {
                probe.Open();
                AssertTenant(probe, stampIfMissing: false);   // throws on a cross-tenant mismatch
                // Structural guard, deliberately independent of configuration: every flag above
                // reflects what the environment CLAIMS this database is, but a spike-owned registry
                // is recognizable by what it CONTAINS — Darlastic tables and nothing else. Any table
                // outside the schema means this is some application's own database (a host that
                // mounted the registry), and no combination of stale env vars may drop that.
                var foreign = Scalar(probe,
                    "SELECT TOP 1 SCHEMA_NAME(schema_id) + '.' + name FROM sys.tables WHERE SCHEMA_NAME(schema_id) <> 'Darlastic'") as string;
                if (foreign is not null)
                    throw new InvalidOperationException(
                        $"Refusing to drop {DbName}: it contains non-Darlastic objects (e.g. {foreign}) — this is an application's database that the registry is mounted IN, not a registry database.");
            }
            catch (SqlException) { return null; }             // database absent — nothing to drop
        }
        SqlConnection.ClearAllPools();                        // pooled connections would block the drop

        using var master = new SqlConnection($"{DefaultServer};Database=master");
        master.Open();
        using var cmd = master.CreateCommand();
        cmd.CommandText = $"""
            IF DB_ID('{DbName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{DbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{DbName}];
            END
            """;
        cmd.ExecuteNonQuery();
        return DbName;
    }

    private static void EnsureDatabase()
    {
        if (HostManagedSchema) return;                                              // host owns the database too
        if (Environment.GetEnvironmentVariable("DARLASTIC_SQL") is not null) return; // custom string: DB assumed managed
        using var master = new SqlConnection($"{DefaultServer};Database=master");
        master.Open();
        using var cmd = master.CreateCommand();
        cmd.CommandText = $"IF DB_ID('{DbName}') IS NULL CREATE DATABASE [{DbName}]";
        cmd.ExecuteNonQuery();
    }

    /// <summary>Session-scoped exclusive lock: two concurrent resolves would each read a stale
    /// snapshot and MERGE it over the other's fresher writes. Fail fast instead.</summary>
    private static void AcquireResolveLock(SqlConnection conn)
    {
        using var cmd = new SqlCommand("sp_getapplock", conn) { CommandType = System.Data.CommandType.StoredProcedure };
        cmd.Parameters.AddWithValue("@Resource", "Darlastic.Resolve");
        cmd.Parameters.AddWithValue("@LockMode", "Exclusive");
        cmd.Parameters.AddWithValue("@LockOwner", "Session");
        cmd.Parameters.AddWithValue("@LockTimeout", 0);
        var ret = cmd.Parameters.Add("@rv", System.Data.SqlDbType.Int);
        ret.Direction = System.Data.ParameterDirection.ReturnValue;
        cmd.ExecuteNonQuery();
        if ((int)ret.Value! < 0)
            throw new InvalidOperationException("Another Darlastic resolve is already running (sp_getapplock 'Darlastic.Resolve' denied).");
    }

    private static void EnsureSchema(SqlConnection conn)
    {
        if (HostManagedSchema) { VerifySchema(conn); return; }
        Exec(conn, "IF SCHEMA_ID('Darlastic') IS NULL EXEC('CREATE SCHEMA Darlastic')");
        Exec(conn, """
            IF OBJECT_ID('Darlastic.Identity') IS NULL
            CREATE TABLE Darlastic.[Identity] (
                IdentityID      BIGINT       NOT NULL PRIMARY KEY,
                Status          TINYINT      NOT NULL,   -- 1 active, 2 redirected, 3 inactive (no members)
                CreatedRunID    INT          NOT NULL,
                LastChangedRunID INT         NOT NULL)
            """);
        Exec(conn, """
            IF OBJECT_ID('Darlastic.IdentityRedirect') IS NULL
            CREATE TABLE Darlastic.IdentityRedirect (
                OldIdentityID   BIGINT NOT NULL PRIMARY KEY,
                NewIdentityID   BIGINT NOT NULL,
                RunID           INT    NOT NULL)
            """);
        Exec(conn, $"""
            IF OBJECT_ID('Darlastic.SourceProfile') IS NULL
            BEGIN
            CREATE TABLE Darlastic.SourceProfile (
                SourceSystem    NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                SourceRecordId  NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                IdentityID      BIGINT        NOT NULL,
                ContentHash     VARCHAR(64)   NOT NULL,
                Removed         BIT           NOT NULL,
                RemovedRunID    INT           NULL,
                FirstRunID      INT           NOT NULL,
                LastChangedRunID INT          NOT NULL,
                CONSTRAINT PK_SourceProfile PRIMARY KEY (SourceSystem, SourceRecordId));
            CREATE INDEX IX_SourceProfile_Identity ON Darlastic.SourceProfile(IdentityID);
            END
            """);
        Exec(conn, $"""
            IF OBJECT_ID('Darlastic.ProjectionState') IS NULL
            CREATE TABLE Darlastic.ProjectionState (
                ArtifactType    VARCHAR(32)   NOT NULL,
                ArtifactKey     NVARCHAR(140) COLLATE {KeyCollation} NOT NULL,   -- fits "src|recId" stamp keys (64+1+64)
                ContentHash     VARCHAR(64)   NOT NULL,   -- 'TOMBSTONE' when the artifact must be deleted downstream
                Pending         BIT           NOT NULL,
                UpdatedRunID    INT           NOT NULL,
                ProjectedRunID  INT           NULL,
                Payload         NVARCHAR(MAX) NULL,       -- the canonical artifact body; the drain is a dumb pump
                CONSTRAINT PK_ProjectionState PRIMARY KEY (ArtifactType, ArtifactKey))
            """);
        Exec(conn, "IF COL_LENGTH('Darlastic.ProjectionState', 'Payload') IS NULL ALTER TABLE Darlastic.ProjectionState ADD Payload NVARCHAR(MAX) NULL");
        // Widen ArtifactKey on databases created before the stamp family existed (COL_LENGTH is bytes: NVARCHAR(64) = 128).
        Exec(conn, $"""
            IF COL_LENGTH('Darlastic.ProjectionState', 'ArtifactKey') = 128
            BEGIN
                ALTER TABLE Darlastic.ProjectionState DROP CONSTRAINT PK_ProjectionState;
                ALTER TABLE Darlastic.ProjectionState ALTER COLUMN ArtifactKey NVARCHAR(140) COLLATE {KeyCollation} NOT NULL;
                ALTER TABLE Darlastic.ProjectionState ADD CONSTRAINT PK_ProjectionState PRIMARY KEY (ArtifactType, ArtifactKey);
            END
            """);
        Exec(conn, StewardDdl);
        Exec(conn, $"""
            IF OBJECT_ID('Darlastic.StewardQueue') IS NULL
            CREATE TABLE Darlastic.StewardQueue (
                PairKey         NVARCHAR(300) COLLATE {KeyCollation} NOT NULL PRIMARY KEY,  -- "src:id~src:id", canonical order — matches the case browser's audit keys
                RunID           INT  NOT NULL,
                Score           REAL NOT NULL,
                SourceSystemA   NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                SourceRecordIdA NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                SourceSystemB   NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                SourceRecordIdB NVARCHAR(64) COLLATE {KeyCollation} NOT NULL)
            """);
        Exec(conn, $"""
            IF OBJECT_ID('Darlastic.StewardRecord') IS NULL
            CREATE TABLE Darlastic.StewardRecord (
                SourceSystem    NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                SourceRecordId  NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                RunID           INT NOT NULL,
                Payload         NVARCHAR(MAX) NOT NULL,   -- the normalized RealRecord, JSON
                CONSTRAINT PK_StewardRecord PRIMARY KEY (SourceSystem, SourceRecordId))
            """);
        Exec(conn, """
            IF OBJECT_ID('Darlastic.ResolveRun') IS NULL
            CREATE TABLE Darlastic.ResolveRun (
                RunID           INT IDENTITY(1,1) PRIMARY KEY,
                StartedUtc      DATETIME2 NOT NULL,
                FinishedUtc     DATETIME2 NULL,
                Records         INT NULL, Identities INT NULL,
                Minted          INT NULL, Inherited  INT NULL, Redirected INT NULL, Deactivated INT NULL, Reactivated INT NULL,
                ProfilesNew     INT NULL, ProfilesReassigned INT NULL, ProfilesRehashed INT NULL, ProfilesRemoved INT NULL,
                ArtifactsPending INT NULL,
                Notes           NVARCHAR(400) NULL)
            """);
    }

    /// <summary>
    /// Host-managed counterpart to <see cref="EnsureSchema"/>: assert the migration actually ran
    /// rather than create anything. Checked up front, as one message naming every missing table —
    /// the alternative is discovering an unapplied migration as "Invalid object name
    /// 'Darlastic.StewardQueue'" thrown from inside a transaction two minutes into a resolve.
    /// </summary>
    private static void VerifySchema(SqlConnection conn)
    {
        var missing = RequiredTables
            .Where(t => Convert.ToInt32(Scalar(conn, $"SELECT CASE WHEN OBJECT_ID('Darlastic.{t}') IS NULL THEN 0 ELSE 1 END")) == 0)
            .ToArray();

        if (missing.Length > 0)
            throw new InvalidOperationException(
                $"DARLASTIC_SCHEMA_MANAGED=1 says the host owns the [Darlastic] schema, but {missing.Length} " +
                $"table(s) are missing: {string.Join(", ", missing)}. Apply the host's migrations before resolving " +
                "(or unset DARLASTIC_SCHEMA_MANAGED to let the engine bootstrap its own schema for local dev).");
    }

    /// <summary>Steward-surface tables (Phase 5). AuditEntry records EVERY steward action
    /// (verdicts, flags, future merges/splits/overrides) — immutable, append-only. StewardDecision
    /// holds the engine-honored constraints (merge / split / sticky override); empty until the
    /// real steward mutations ship — the engine will replay Active rows as hard constraints.</summary>
    private const string StewardDdl = $"""
        IF OBJECT_ID('Darlastic.AuditEntry') IS NULL
        CREATE TABLE Darlastic.AuditEntry (
            AuditID     BIGINT IDENTITY(1,1) PRIMARY KEY,
            AtUtc       DATETIME2 NOT NULL,
            Actor       NVARCHAR(128) NOT NULL,
            Action      VARCHAR(32)  NOT NULL,
            TargetKey   NVARCHAR(256) NOT NULL,
            Payload     NVARCHAR(MAX) NULL);
        IF OBJECT_ID('Darlastic.StewardDecision') IS NULL
        CREATE TABLE Darlastic.StewardDecision (
            DecisionID  BIGINT IDENTITY(1,1) PRIMARY KEY,
            AtUtc       DATETIME2 NOT NULL,
            Actor       NVARCHAR(128) NOT NULL,
            Kind        VARCHAR(16) NOT NULL,
            IdentityID  BIGINT NULL,
            SourceSystem NVARCHAR(64) COLLATE {KeyCollation} NULL,
            SourceRecordId NVARCHAR(64) COLLATE {KeyCollation} NULL,
            AttrType    NVARCHAR(64) NULL,
            Value       NVARCHAR(1024) NULL,
            Active      BIT NOT NULL,
            Payload     NVARCHAR(MAX) NULL)
        """;

    // ------------------------------------------------------------------ read-only surface (case browser / API)

    public sealed class Snapshot
    {
        public required Dictionary<(string Src, string RecId), long> ProfileToIdentity { get; init; }   // redirect-chased: always live IDs
        public required Dictionary<long, byte> IdentityStatus { get; init; }
        public int ActiveIdentities { get; init; }
        public int LastRunId { get; init; }
    }

    /// <summary>
    /// Read-only registry snapshot for display surfaces. Deliberately does NOT take the resolve
    /// applock and does NOT create the database/schema — if the registry isn't there, callers
    /// degrade to memory-only mode. Assignments are redirect-chased, so every returned ID is live.
    /// </summary>
    public static Snapshot LoadSnapshot()
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        AssertTenant(conn, stampIfMissing: false);   // mismatch throws -> caller degrades to memory-only with the message shown
        var redirect = new Dictionary<long, long>();
        using (var cmd = new SqlCommand("SELECT OldIdentityID, NewIdentityID FROM Darlastic.IdentityRedirect", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) redirect[r.GetInt64(0)] = r.GetInt64(1);
        long Chase(long id) { int hops = 0; while (redirect.TryGetValue(id, out var nxt)) { id = nxt; if (++hops > 64) break; } return id; }

        var status = new Dictionary<long, byte>();
        using (var cmd = new SqlCommand("SELECT IdentityID, Status FROM Darlastic.[Identity]", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) status[r.GetInt64(0)] = r.GetByte(1);

        var map = new Dictionary<(string, string), long>();
        using (var cmd = new SqlCommand("SELECT SourceSystem, SourceRecordId, IdentityID FROM Darlastic.SourceProfile WHERE Removed = 0", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) map[(r.GetString(0), r.GetString(1))] = Chase(r.GetInt64(2));

        int lastRun = Convert.ToInt32(Scalar(conn, "SELECT ISNULL(MAX(RunID), 0) FROM Darlastic.ResolveRun WHERE FinishedUtc IS NOT NULL"));
        return new Snapshot
        {
            ProfileToIdentity = map,
            IdentityStatus = status,
            ActiveIdentities = status.Count(kv => kv.Value == 1),
            LastRunId = lastRun,
        };
    }

    /// <summary>Append one immutable audit row (steward verdict / flag / unflag / …). Ensures the
    /// steward tables exist so audit capture works even before the first resolve on this database —
    /// except under a host-managed schema, where issuing DDL here would pre-create tables in the
    /// engine's shape and make the host's own migration fail on "object already exists"; there the
    /// tables must come from the migration, and their absence is an error worth surfacing.</summary>
    public static void AppendAudit(string actor, string action, string targetKey, string? payloadJson)
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        if (!HostManagedSchema)
        {
            Exec(conn, "IF SCHEMA_ID('Darlastic') IS NULL EXEC('CREATE SCHEMA Darlastic')");
            Exec(conn, StewardDdl);
        }
        else if (Convert.ToInt32(Scalar(conn, "SELECT CASE WHEN OBJECT_ID('Darlastic.AuditEntry') IS NULL THEN 0 ELSE 1 END")) == 0)
            throw new InvalidOperationException(
                "DARLASTIC_SCHEMA_MANAGED=1 but Darlastic.AuditEntry does not exist — apply the host's migrations before steward actions.");
        using var cmd = new SqlCommand(
            "INSERT INTO Darlastic.AuditEntry (AtUtc, Actor, Action, TargetKey, Payload) VALUES (SYSUTCDATETIME(), @a, @ac, @t, @p)", conn);
        cmd.Parameters.AddWithValue("@a", actor);
        cmd.Parameters.AddWithValue("@ac", action);
        cmd.Parameters.AddWithValue("@t", targetKey);
        cmd.Parameters.AddWithValue("@p", (object?)payloadJson ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    // ------------------------------------------------------------------ the resolve run

    public sealed class RunMetrics
    {
        public int RunId;
        public int Records, Identities;
        public int Minted, Inherited, Redirected, Deactivated, Reactivated;
        public int ProfilesNew, ProfilesReassigned, ProfilesRehashed, ProfilesRemoved;
        public int ArtifactsPending;
        public int StewardQueued;   // derived queue size — not a delta (excluded from IsZeroDelta)
        public Dictionary<string, int> PendingByType = new(StringComparer.Ordinal);
        public List<string> AbsentSources = [];

        /// <summary>The D3 acceptance: unchanged sources must produce zero deltas of EVERY kind —
        /// not just zero pending artifacts (a silent mass-rehash must fail the banner too).</summary>
        public bool IsZeroDelta =>
            Minted == 0 && Redirected == 0 && Deactivated == 0 && Reactivated == 0
            && ProfilesNew == 0 && ProfilesReassigned == 0 && ProfilesRehashed == 0 && ProfilesRemoved == 0
            && ArtifactsPending == 0;

        public override string ToString() =>
            $"run {RunId}: {Records:N0} profiles -> {Identities:N0} identities " +
            $"(minted {Minted:N0} / inherited {Inherited:N0} / redirected {Redirected:N0} / deactivated {Deactivated:N0} / reactivated {Reactivated:N0}); " +
            $"profiles new {ProfilesNew:N0} / reassigned {ProfilesReassigned:N0} / rehashed {ProfilesRehashed:N0} / removed {ProfilesRemoved:N0}; " +
            $"artifacts pending {ArtifactsPending:N0}" +
            (PendingByType.Count > 0 ? " (" + string.Join(" / ", PendingByType.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key} {kv.Value:N0}")) + ")" : "") +
            $"; steward queue {StewardQueued:N0}" +
            (AbsentSources.Count > 0 ? $"; ABSENT sources frozen: {string.Join(", ", AbsentSources)}" : "");
    }

    public static RunMetrics RunResolve(IReadOnlyList<RealRecord> records, Merge.Result merge)
    {
        using var conn = Open();
        var m = new RunMetrics { Records = records.Count };

        // ---- prior state (reads are safe: resolve runs are serialized by the applock)
        var priorProfile = new Dictionary<string, (long IdentityId, string Hash, bool Removed)>(StringComparer.Ordinal);
        using (var cmd = new SqlCommand("SELECT SourceSystem, SourceRecordId, IdentityID, ContentHash, Removed FROM Darlastic.SourceProfile", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                priorProfile[r.GetString(0) + "|" + r.GetString(1)] = (r.GetInt64(2), r.GetString(3), r.GetBoolean(4));
        var identityStatus = new Dictionary<long, byte>();
        using (var cmd = new SqlCommand("SELECT IdentityID, Status FROM Darlastic.[Identity]", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) identityStatus[r.GetInt64(0)] = r.GetByte(1);
        var redirect = new Dictionary<long, long>();
        using (var cmd = new SqlCommand("SELECT OldIdentityID, NewIdentityID FROM Darlastic.IdentityRedirect", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read()) redirect[r.GetInt64(0)] = r.GetInt64(1);
        long nextId = 1 + Convert.ToInt64(Scalar(conn, "SELECT ISNULL(MAX(IdentityID), 0) FROM Darlastic.[Identity]"));

        // A soft-removed profile keeps its last assignment; if that identity was later merged away,
        // the stored ID is stale — chase redirects so a revival votes for the LIVE identity.
        // Chains are compressed on write, but chase defensively anyway.
        long Chase(long id) { int hops = 0; while (redirect.TryGetValue(id, out var nxt)) { id = nxt; if (++hops > 64) throw new InvalidOperationException($"Redirect chain too long at {id}"); } return id; }

        // ---- guard: empty input against a non-empty registry is a source outage, not a data state
        if (records.Count == 0 && priorProfile.Count > 0 && !Forced)
            throw new InvalidOperationException(
                $"Refusing to resolve: 0 input records against {priorProfile.Count:N0} registered profiles. " +
                "This is almost certainly a missing/empty source directory. Set DARLASTIC_FORCE=1 to override.");

        // ---- current state: clusters (root -> members), deterministic order by smallest member key
        // merge.Parent is path-compressed (ClusterFromBlocks runs Find on every index), but chase
        // to the root anyway — correctness must not depend on a compression side effect.
        var clusters = new Dictionary<int, List<int>>();
        int Root(int i) { while (merge.Parent[i] != i) i = merge.Parent[i]; return i; }
        for (int i = 0; i < records.Count; i++)
        {
            int root = Root(i);
            (clusters.TryGetValue(root, out var l) ? l : clusters[root] = new List<int>()).Add(i);
        }
        string Key(int idx) => records[idx].SourceSystem + "|" + records[idx].SourceRecordId;
        var clusterMinKey = clusters.ToDictionary(kv => kv.Key, kv => kv.Value.Select(i => Key(i)).Min(StringComparer.Ordinal)!);
        var orderedRoots = clusters.Keys.OrderBy(r => clusterMinKey[r], StringComparer.Ordinal).ToList();
        m.Identities = orderedRoots.Count;

        // ---- absent sources: prior rows exist, zero current records -> freeze, don't destroy
        var presentSources = new HashSet<string>(records.Select(r => r.SourceSystem), StringComparer.Ordinal);
        var priorSources = new HashSet<string>(StringComparer.Ordinal);
        foreach (var k in priorProfile.Keys) priorSources.Add(k[..k.IndexOf('|')]);
        m.AbsentSources = priorSources.Where(s => !presentSources.Contains(s)).OrderBy(s => s, StringComparer.Ordinal).ToList();
        // identities frozen because they still have live profiles in an absent source
        var frozenIdentities = new HashSet<long>();
        if (m.AbsentSources.Count > 0)
        {
            var absent = new HashSet<string>(m.AbsentSources, StringComparer.Ordinal);
            foreach (var (k, p) in priorProfile)
                if (!p.Removed && absent.Contains(k[..k.IndexOf('|')]))
                    frozenIdentities.Add(Chase(p.IdentityId));
        }

        // ---- max-overlap reconciliation (votes chase redirects to live identities)
        var clusterVotes = new Dictionary<int, Dictionary<long, int>>();          // root -> (identity -> members here)
        var identityTotal = new Dictionary<long, int>();                          // identity -> surviving member count
        foreach (var (root, members) in clusters)
        {
            Dictionary<long, int>? votes = null;
            foreach (var idx in members)
                if (priorProfile.TryGetValue(Key(idx), out var p))
                {
                    long live = Chase(p.IdentityId);
                    votes ??= clusterVotes[root] = new Dictionary<long, int>();
                    votes[live] = votes.GetValueOrDefault(live) + 1;
                    identityTotal[live] = identityTotal.GetValueOrDefault(live) + 1;
                }
        }
        var bestCluster = new Dictionary<long, int>();                            // identity -> elected root
        foreach (var (root, votes) in clusterVotes)
            foreach (var (id, count) in votes)
                if (!bestCluster.TryGetValue(id, out var cur)
                    || count > clusterVotes[cur][id]
                    || (count == clusterVotes[cur][id] && string.CompareOrdinal(clusterMinKey[root], clusterMinKey[cur]) < 0))
                    bestCluster[id] = root;

        var finalId = new Dictionary<int, long>();                                // root -> final identity
        var newIdentities = new List<long>();
        var reactivated = new List<long>();
        foreach (var root in orderedRoots)
        {
            long winner = 0; int winnerCount = -1;
            if (clusterVotes.TryGetValue(root, out var votes))
                foreach (var (id, count) in votes)
                {
                    if (bestCluster[id] != root) continue;                        // identity elected a different cluster
                    if (count > winnerCount || (count == winnerCount && id < winner)) { winner = id; winnerCount = count; }
                }
            if (winnerCount > 0)
            {
                finalId[root] = winner; m.Inherited++;
                if (identityStatus.TryGetValue(winner, out var st) && st != 1) reactivated.Add(winner);   // revival
            }
            else { finalId[root] = nextId++; newIdentities.Add(finalId[root]); m.Minted++; }
        }
        m.Reactivated = reactivated.Count;

        // redirects: a surviving prior identity whose elected cluster ended up under a different ID
        var redirects = new List<(long Old, long New)>();
        foreach (var (id, root) in bestCluster)
            if (finalId[root] != id) redirects.Add((id, finalId[root]));
        m.Redirected = redirects.Count;
        // deactivations: prior ACTIVE identities with zero surviving members — unless frozen (absent source)
        var deactivated = identityStatus.Where(kv => kv.Value == 1)
            .Select(kv => kv.Key)
            .Where(id => !identityTotal.ContainsKey(id) && !frozenIdentities.Contains(id))
            .OrderBy(id => id).ToList();
        m.Deactivated = deactivated.Count;

        // ---- profile deltas (a revived soft-removed row counts as changed: Removed flips back)
        var profileRows = new List<(string Src, string RecId, long IdentityId, string Hash)>();
        var currentKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var root in orderedRoots)
            foreach (var idx in clusters[root])
            {
                string key = Key(idx);
                currentKeys.Add(key);
                string hash = ProfileHash(records[idx]);
                long id = finalId[root];
                if (!priorProfile.TryGetValue(key, out var p)) { profileRows.Add((records[idx].SourceSystem, records[idx].SourceRecordId, id, hash)); m.ProfilesNew++; }
                else if (Chase(p.IdentityId) != id) { profileRows.Add((records[idx].SourceSystem, records[idx].SourceRecordId, id, hash)); m.ProfilesReassigned++; }
                else if (p.Hash != hash || p.Removed) { profileRows.Add((records[idx].SourceSystem, records[idx].SourceRecordId, id, hash)); m.ProfilesRehashed++; }
                else if (p.IdentityId != id) { profileRows.Add((records[idx].SourceSystem, records[idx].SourceRecordId, id, hash)); m.ProfilesReassigned++; }   // stale stored ID (pre-compression) — flatten it
            }
        // removals: scoped to PRESENT sources, and only rows not already soft-removed
        var removedKeys = new List<string>();
        foreach (var (k, p) in priorProfile)
            if (!p.Removed && !currentKeys.Contains(k) && presentSources.Contains(k[..k.IndexOf('|')]))
                removedKeys.Add(k);
        m.ProfilesRemoved = removedKeys.Count;

        // ---- guard: a mass removal/deactivation wave is an outage signature, not a data state
        if (priorProfile.Count > 0 && !Forced)
        {
            double frac = (double)(m.ProfilesRemoved + m.Deactivated) / priorProfile.Count;
            if (frac > MassChangeThreshold)
                throw new InvalidOperationException(
                    $"Refusing to resolve: {m.ProfilesRemoved:N0} removals + {m.Deactivated:N0} deactivations = {frac:P1} of the registry " +
                    $"(threshold {MassChangeThreshold:P0}). If this is a genuine data change, set DARLASTIC_FORCE=1.");
        }

        // ---- projection staging: four artifact families, one hash-guarded delta discipline
        var priorArtifacts = new Dictionary<string, Dictionary<string, (string Hash, bool HasPayload)>>(StringComparer.Ordinal)
        {
            [ArtGolden] = new(StringComparer.Ordinal), [ArtVinLinks] = new(StringComparer.Ordinal),
            [ArtVinOwner] = new(StringComparer.Ordinal), [ArtStamp] = new(StringComparer.Ordinal),
        };
        using (var cmd = new SqlCommand("SELECT ArtifactType, ArtifactKey, ContentHash, CASE WHEN Payload IS NULL THEN 0 ELSE 1 END FROM Darlastic.ProjectionState", conn))
        using (var r = cmd.ExecuteReader())
            while (r.Read())
                if (priorArtifacts.TryGetValue(r.GetString(0), out var d))
                    d[r.GetString(1)] = (r.GetString(2).TrimEnd(), r.GetInt32(3) == 1);

        var artifactRows = new List<(string Type, string Key, string Hash, string? Payload)>();   // changed only
        // Re-emit on hash change OR missing payload (one-off backfill for rows staged before the
        // Payload column existed) — the drain must never find a pending row it can't pump.
        void Stage(string type, string key, string canonical)
        {
            string hash = Sha(canonical);
            if (!priorArtifacts[type].TryGetValue(key, out var prev) || prev.Hash != hash || !prev.HasPayload)
                artifactRows.Add((type, key, hash, canonical));
        }

        // Pass 1 over clusters: golden canonicals + survived display names + per-(identity, VIN)
        // evidence aggregates + GoldenCustomerID stamps.
        var goldenName = new Dictionary<long, string>();
        var vinAgg = new Dictionary<(long IdentityId, string Vin), VinEvidence>();
        foreach (var root in orderedRoots)
        {
            long id = finalId[root];
            var survived = Merge.SurviveGolden(clusters[root].Select(i => records[i]).ToList());
            goldenName[id] = survived.FirstOrDefault(g => g.AttrType == "full_name")?.Value ?? "";
            Stage(ArtGolden, id.ToString(CultureInfo.InvariantCulture), GoldenCanonical(id, survived, Key, clusters[root]));

            string gid = id.ToString(CultureInfo.InvariantCulture);
            foreach (var idx in clusters[root])
            {
                // stamp: pending only when this profile's golden assignment is new or changed
                string pkey = Key(idx);
                if (!priorArtifacts[ArtStamp].TryGetValue(pkey, out var prevStamp) || prevStamp.Hash != gid)
                    artifactRows.Add((ArtStamp, pkey, gid, "{\"goldenId\":" + gid + "}"));

                // Dealer-self / placeholder org rows carry stock and internal service traffic, not
                // customer ownership — their VIN ties are noise here (and a dealer-self cluster
                // with thousands of VINs would overflow the 2MB doc cap anyway).
                if (records[idx].IsOrgPlaceholder || records[idx].VinLinks is not { Length: > 0 } links) continue;
                foreach (var v in links)
                {
                    if (string.IsNullOrEmpty(v.Vin)) continue;   // an empty key is unaddressable downstream
                    if (!vinAgg.TryGetValue((id, v.Vin), out var e)) vinAgg[(id, v.Vin)] = e = new VinEvidence();
                    if (v.Source == VinSource.Sale) { e.Sales++; e.FirstSale = MinDate(e.FirstSale, v.First); e.LastSale = MaxDate(e.LastSale, v.Last ?? v.First); }
                    else { e.Services++; e.FirstService = MinDate(e.FirstService, v.First); e.LastService = MaxDate(e.LastService, v.Last ?? v.First); }
                    e.Sources.Add(Key(idx));
                }
            }
        }

        // Stamps for profiles NOT in the current input (soft-removed rows; frozen absent-source
        // rows): the stored assignment may have been redirected away — this run or earlier — and
        // the landing doc must follow to the LIVE identity, or its forward link dangles on a golden
        // doc the drain deletes. One extra hop covers this run's redirects (targets are always live).
        var newRedirect = redirects.ToDictionary(r => r.Old, r => r.New);
        foreach (var (k, p) in priorProfile)
        {
            if (currentKeys.Contains(k)) continue;
            long live = Chase(p.IdentityId);
            if (newRedirect.TryGetValue(live, out var moved)) live = moved;
            string gid = live.ToString(CultureInfo.InvariantCulture);
            if (!priorArtifacts[ArtStamp].TryGetValue(k, out var prevStamp) || prevStamp.Hash != gid)
                artifactRows.Add((ArtStamp, k, gid, "{\"goldenId\":" + gid + "}"));
        }

        // Pass 2: the per-VIN ownership timeline (P7). Only sale-grade evidence opens an ownership
        // period; a later sale to a DIFFERENT golden closes the previous owner's period (transfer).
        // Service-only evidence is a "service-contact" observation window, never an ownership claim.
        var byVin = new Dictionary<string, List<(long Id, VinEvidence E)>>(StringComparer.Ordinal);
        foreach (var kv in vinAgg)
            (byVin.TryGetValue(kv.Key.Vin, out var l) ? l : byVin[kv.Key.Vin] = []).Add((kv.Key.IdentityId, kv.Value));
        var linkPeriod = new Dictionary<(long, string), (DateOnly? From, DateOnly? To, string Role)>();
        foreach (var (vin, entries) in byVin)
        {
            var owners = entries.Where(x => x.E.Sales > 0)
                .OrderBy(x => x.E.FirstSale ?? DateOnly.MinValue).ThenBy(x => x.Id).ToList();
            for (int i = 0; i < owners.Count; i++)
                linkPeriod[(owners[i].Id, vin)] = (owners[i].E.FirstSale, i + 1 < owners.Count ? owners[i + 1].E.FirstSale : null, "owner");
            foreach (var x in entries.Where(x => x.E.Sales == 0))
                linkPeriod[(x.Id, vin)] = (x.E.FirstService, x.E.LastService, "service-contact");
        }

        // Pass 3: emit the two ownership projections from the shared timeline, so both docs always
        // agree on periods. Note the cross-identity coupling is real and intended: golden B buying a
        // VIN closes golden A's period, so A's by-customer doc re-stages — the delta discipline
        // absorbs exactly this kind of ripple.
        var byIdentity = new Dictionary<long, List<(string Vin, VinEvidence E)>>();
        foreach (var kv in vinAgg)
            (byIdentity.TryGetValue(kv.Key.IdentityId, out var l) ? l : byIdentity[kv.Key.IdentityId] = []).Add((kv.Key.Vin, kv.Value));
        var sb = new StringBuilder(1024);
        foreach (var (id, links) in byIdentity)
        {
            sb.Clear();
            sb.Append("{\"goldenId\":").Append(id).Append(",\"links\":[");
            bool first = true;
            foreach (var (vin, e) in links.OrderBy(x => x.Vin, StringComparer.Ordinal))
            {
                if (!first) sb.Append(','); first = false;
                AppendLink(sb, vin, id, goldenName[id], linkPeriod[(id, vin)], e);
            }
            sb.Append("]}");
            Stage(ArtVinLinks, id.ToString(CultureInfo.InvariantCulture), sb.ToString());
        }
        foreach (var (vin, entries) in byVin)
        {
            sb.Clear();
            sb.Append("{\"vin\":\"").Append(J(vin)).Append("\",\"owners\":[");
            bool first = true;
            // owners in timeline order, then service-contacts by golden id — deterministic
            var ordered = entries.Where(x => x.E.Sales > 0).OrderBy(x => x.E.FirstSale ?? DateOnly.MinValue).ThenBy(x => x.Id)
                .Concat(entries.Where(x => x.E.Sales == 0).OrderBy(x => x.Id));
            foreach (var (id, e) in ordered)
            {
                if (!first) sb.Append(','); first = false;
                AppendLink(sb, vin, id, goldenName[id], linkPeriod[(id, vin)], e);
            }
            sb.Append("]}");
            Stage(ArtVinOwner, vin, sb.ToString());
        }

        // golden tombstones: ONLY identities redirected away this run — the surviving identity's
        // doc replaces them, and their stamps re-point to the survivor above. DEACTIVATED identities
        // keep their last golden doc: there is no successor to re-point forward links at, so
        // deleting the doc would leave every landing doc stamped with a 404-ing GoldenCustomerID
        // (adversarial review 2026-07-18) — and the retention posture (P2) keeps identities forever
        // anyway. A revival simply resumes updating the standing doc. (A blanket "not live anymore"
        // sweep would also tombstone frozen absent-source identities.)
        foreach (var id in redirects.Select(r => r.Old))
        {
            string key = id.ToString(CultureInfo.InvariantCulture);
            if (!priorArtifacts[ArtGolden].TryGetValue(key, out var prev) || prev.Hash != Tombstone) artifactRows.Add((ArtGolden, key, Tombstone, null));
        }
        // vinlinks/vinowner tombstones are full-state (prior key no longer emitted -> delete), which
        // self-heals: a tombstone missed this run is regenerated next run. Frozen identities (absent
        // sources) aren't in the clusters, so their keys would LOOK gone — protect exactly those keys,
        // and their VINs (recovered from the frozen identities' own staged payloads), rather than
        // suppressing all ownership deletes: a permanently decommissioned source must never block
        // unrelated tombstones forever. Upserts still flow during an outage: a VIN shared between a
        // frozen and a live identity temporarily re-stages without the frozen side's entries and
        // heals on revival — same churn contract as golden docs.
        var frozenKeys = new HashSet<string>(frozenIdentities.Select(id => id.ToString(CultureInfo.InvariantCulture)), StringComparer.Ordinal);
        var frozenVins = new HashSet<string>(StringComparer.Ordinal);
        foreach (var chunk in frozenKeys.Chunk(500))
        {
            using var cmd = new SqlCommand(
                "SELECT Payload FROM Darlastic.ProjectionState WHERE ArtifactType = 'vinlinks' AND Payload IS NOT NULL AND ArtifactKey IN ("
                + string.Join(",", chunk.Select((_, i) => $"@k{i}")) + ")", conn);
            for (int i = 0; i < chunk.Length; i++) cmd.Parameters.AddWithValue($"@k{i}", chunk[i]);
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                using var doc = System.Text.Json.JsonDocument.Parse(r.GetString(0));
                foreach (var l in doc.RootElement.GetProperty("links").EnumerateArray())
                    frozenVins.Add(l.GetProperty("vin").GetString() ?? "");
            }
        }
        var liveVinLinks = new HashSet<string>(byIdentity.Keys.Select(id => id.ToString(CultureInfo.InvariantCulture)), StringComparer.Ordinal);
        foreach (var (key, prev) in priorArtifacts[ArtVinLinks])
            if (prev.Hash != Tombstone && !liveVinLinks.Contains(key) && !frozenKeys.Contains(key)) artifactRows.Add((ArtVinLinks, key, Tombstone, null));
        foreach (var (key, prev) in priorArtifacts[ArtVinOwner])
            if (prev.Hash != Tombstone && !byVin.ContainsKey(key) && !frozenVins.Contains(key)) artifactRows.Add((ArtVinOwner, key, Tombstone, null));
        // stamps are never tombstoned: a soft-removed profile keeps its (redirect-chased) assignment,
        // and clearing a landing doc's GoldenCustomerID would erase a still-valid forward link.

        m.ArtifactsPending = artifactRows.Count;
        foreach (var g in artifactRows.GroupBy(r => r.Type)) m.PendingByType[g.Key] = g.Count();

        // ---- guard: a wholesale ownership-tombstone wave is an outage signature the profile guard
        // can't see — e.g. the VIN-evidence join input missing while customer files load fine: every
        // record arrives with zero VinLinks, sources all look present, removals/deactivations are 0,
        // and the full-state sweep would delete the entire ownership corpus from Cosmos.
        int priorOwnership = priorArtifacts[ArtVinLinks].Count(kv => kv.Value.Hash != Tombstone)
                           + priorArtifacts[ArtVinOwner].Count(kv => kv.Value.Hash != Tombstone);
        int ownershipTombstones = artifactRows.Count(r => (r.Type is ArtVinLinks or ArtVinOwner) && r.Hash == Tombstone);
        if (priorOwnership > 0 && !Forced && (double)ownershipTombstones / priorOwnership > MassChangeThreshold)
            throw new InvalidOperationException(
                $"Refusing to resolve: {ownershipTombstones:N0} ownership-artifact tombstones = {(double)ownershipTombstones / priorOwnership:P1} " +
                $"of the projected ownership corpus (threshold {MassChangeThreshold:P0}). This is the signature of a missing/empty " +
                "VIN-evidence input (the customer files alone look healthy). If the change is genuine, set DARLASTIC_FORCE=1.");

        // ---- write everything (set-based, deltas only)
        m.RunId = Convert.ToInt32(Scalar(conn,
            "INSERT INTO Darlastic.ResolveRun (StartedUtc) OUTPUT INSERTED.RunID VALUES (SYSUTCDATETIME())"));
        using var tx = conn.BeginTransaction();
        WriteIdentities(conn, tx, newIdentities, redirects, deactivated, reactivated, m.RunId);
        WriteProfiles(conn, tx, profileRows, removedKeys, m.RunId);
        WriteArtifacts(conn, tx, artifactRows, m.RunId);
        WriteStewardQueue(conn, tx, records, merge.StewardPairs, m.RunId);
        m.StewardQueued = merge.StewardPairs.Count;
        using (var cmd = new SqlCommand("""
            UPDATE Darlastic.ResolveRun SET FinishedUtc = SYSUTCDATETIME(),
                Records=@r, Identities=@i, Minted=@mi, Inherited=@in, Redirected=@rd, Deactivated=@da, Reactivated=@ra,
                ProfilesNew=@pn, ProfilesReassigned=@pa, ProfilesRehashed=@ph, ProfilesRemoved=@pr, ArtifactsPending=@ap, Notes=@no
            WHERE RunID=@run
            """, conn, tx))
        {
            cmd.Parameters.AddWithValue("@r", m.Records); cmd.Parameters.AddWithValue("@i", m.Identities);
            cmd.Parameters.AddWithValue("@mi", m.Minted); cmd.Parameters.AddWithValue("@in", m.Inherited);
            cmd.Parameters.AddWithValue("@rd", m.Redirected); cmd.Parameters.AddWithValue("@da", m.Deactivated);
            cmd.Parameters.AddWithValue("@ra", m.Reactivated);
            cmd.Parameters.AddWithValue("@pn", m.ProfilesNew); cmd.Parameters.AddWithValue("@pa", m.ProfilesReassigned);
            cmd.Parameters.AddWithValue("@ph", m.ProfilesRehashed); cmd.Parameters.AddWithValue("@pr", m.ProfilesRemoved);
            cmd.Parameters.AddWithValue("@ap", m.ArtifactsPending); cmd.Parameters.AddWithValue("@run", m.RunId);
            cmd.Parameters.AddWithValue("@no", m.AbsentSources.Count > 0 ? $"absent sources frozen: {string.Join(",", m.AbsentSources)}" : (object)DBNull.Value);
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
        return m;
    }

    // ------------------------------------------------------------------ canonical forms & hashing

    /// <summary>Identity-relevant content of a source profile, canonically ordered. Any change here
    /// is a real upstream change worth recording; nothing volatile (no timestamps, no scores).
    /// Fields are length-prefixed so free-text content can never alias a field boundary; dates are
    /// invariant-culture (a host with a non-Gregorian default calendar must not rehash the corpus).</summary>
    private static string ProfileHash(RealRecord r)
    {
        var sb = new StringBuilder(256);
        void F(string? s) { s ??= ""; sb.Append(s.Length.ToString(CultureInfo.InvariantCulture)).Append(':').Append(s); }
        F(r.RawName);
        F(string.Join(',', r.Phones));
        F(string.Join(',', r.WeakPhones));
        F(r.NationalId);
        F(r.Dob is null ? "" : string.Create(CultureInfo.InvariantCulture, $"{r.Dob.P1}/{r.Dob.P2}/{r.Dob.Year}"));
        F(r.RawAddress);
        F(r.Gender);
        F(r.IsOrgPlaceholder ? "org" : "");
        if (r.VinLinks is { Length: > 0 })
            foreach (var v in r.VinLinks.OrderBy(v => v.Vin, StringComparer.Ordinal).ThenBy(v => v.Source))
                F(v.Vin + "|" + v.Source + "|"
                  + (v.First?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "")
                  + "|" + (v.Last?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? ""));
        // Emails / same-as refs are appended ONLY when present, so profiles from sources without
        // them hash byte-identically to the pre-field era (no registry-wide rehash wave).
        if (r.Emails is { Length: > 0 })
            foreach (var e in r.Emails.OrderBy(e => e, StringComparer.Ordinal)) F("em:" + e);
        if (r.SameAsRefs is { Length: > 0 })
            foreach (var s in r.SameAsRefs.OrderBy(s => s, StringComparer.Ordinal)) F("ref:" + s);
        return Sha(sb.ToString());
    }

    /// <summary>The golden artifact as it would project to Cosmos: identity + survived attributes +
    /// member backlinks. Canonical (sorted, culture-free) so identical state hashes identically.</summary>
    private static string GoldenCanonical(long identityId, List<Merge.GoldenAttr> golden, Func<int, string> key, List<int> memberIdxs)
    {
        var sb = new StringBuilder(512);
        sb.Append("{\"goldenId\":").Append(identityId).Append(",\"attrs\":[");
        bool first = true;
        foreach (var g in golden.OrderBy(g => g.AttrType, StringComparer.Ordinal).ThenBy(g => g.Value, StringComparer.Ordinal))
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append("{\"t\":\"").Append(J(g.AttrType)).Append("\",\"v\":\"").Append(J(g.Value)).Append("\"}");
        }
        sb.Append("],\"members\":[");
        first = true;
        foreach (var k in memberIdxs.Select(key).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(J(k)).Append('"');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>Per-(identity, VIN) evidence aggregate collected in pass 1 of projection staging.
    /// Sources is sorted so canonical output is member-order independent.</summary>
    private sealed class VinEvidence
    {
        public int Sales, Services;
        public DateOnly? FirstSale, LastSale, FirstService, LastService;
        public readonly SortedSet<string> Sources = new(StringComparer.Ordinal);
    }

    /// <summary>One canonical ownership-link element, shared by the by-customer (vinlinks) and
    /// by-VIN (vinowner) artifacts so both projections always agree field-for-field.</summary>
    private static void AppendLink(StringBuilder sb, string vin, long identityId, string name,
        (DateOnly? From, DateOnly? To, string Role) period, VinEvidence e)
    {
        sb.Append("{\"vin\":\"").Append(J(vin))
          .Append("\",\"goldenId\":").Append(identityId)
          .Append(",\"name\":\"").Append(J(name))
          .Append("\",\"role\":\"").Append(period.Role)
          .Append("\",\"from\":\"").Append(D(period.From))
          .Append("\",\"to\":\"").Append(D(period.To))
          .Append("\",\"sales\":").Append(e.Sales)
          .Append(",\"services\":").Append(e.Services)
          .Append(",\"firstSale\":\"").Append(D(e.FirstSale))
          .Append("\",\"lastSale\":\"").Append(D(e.LastSale))
          .Append("\",\"firstService\":\"").Append(D(e.FirstService))
          .Append("\",\"lastService\":\"").Append(D(e.LastService))
          .Append("\",\"sources\":[");
        bool first = true;
        foreach (var s in e.Sources)
        {
            if (!first) sb.Append(',');
            first = false;
            sb.Append('"').Append(J(s)).Append('"');
        }
        sb.Append("]}");
    }

    private static string J(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    private static string D(DateOnly? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
    private static DateOnly? MinDate(DateOnly? a, DateOnly? b) => a is null ? b : b is null ? a : a < b ? a : b;
    private static DateOnly? MaxDate(DateOnly? a, DateOnly? b) => a is null ? b : b is null ? a : a > b ? a : b;

    private static string Sha(string s) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s))).ToLowerInvariant();

    // ------------------------------------------------------------------ set-based writers (deltas only)

    private static void WriteIdentities(SqlConnection conn, SqlTransaction tx,
        List<long> minted, List<(long Old, long New)> redirects, List<long> deactivated, List<long> reactivated, int runId)
    {
        if (minted.Count > 0)
        {
            var t = NewTable(("IdentityID", typeof(long)));
            foreach (var id in minted) t.Rows.Add(id);
            Bulk(conn, tx, t, "#NewIdentity", "CREATE TABLE #NewIdentity (IdentityID BIGINT NOT NULL PRIMARY KEY)");
            Exec(conn, tx, $"INSERT INTO Darlastic.[Identity] (IdentityID, Status, CreatedRunID, LastChangedRunID) SELECT IdentityID, 1, {runId}, {runId} FROM #NewIdentity; DROP TABLE #NewIdentity");
        }
        if (redirects.Count > 0)
        {
            var t = NewTable(("OldIdentityID", typeof(long)), ("NewIdentityID", typeof(long)));
            foreach (var (o, n) in redirects) t.Rows.Add(o, n);
            Bulk(conn, tx, t, "#Redirect", "CREATE TABLE #Redirect (OldIdentityID BIGINT NOT NULL PRIMARY KEY, NewIdentityID BIGINT NOT NULL)");
            // Chain compression: any existing redirect pointing AT a newly retired identity is
            // re-pointed to its final target, so single-hop resolution stays valid forever. Sound
            // because a same-run redirect target is always live (a cluster's winner elected it).
            // The insert is unguarded on purpose: an OldIdentityID that already exists means the
            // "profiles never reference retired identities" invariant broke — PK violation = assert.
            Exec(conn, tx, $"""
                UPDATE x SET NewIdentityID = r.NewIdentityID, RunID = {runId}
                    FROM Darlastic.IdentityRedirect x JOIN #Redirect r ON x.NewIdentityID = r.OldIdentityID;
                INSERT INTO Darlastic.IdentityRedirect (OldIdentityID, NewIdentityID, RunID)
                    SELECT OldIdentityID, NewIdentityID, {runId} FROM #Redirect;
                UPDATE i SET Status = 2, LastChangedRunID = {runId} FROM Darlastic.[Identity] i JOIN #Redirect r ON r.OldIdentityID = i.IdentityID;
                DROP TABLE #Redirect
                """);
        }
        if (deactivated.Count > 0)
        {
            var t = NewTable(("IdentityID", typeof(long)));
            foreach (var id in deactivated) t.Rows.Add(id);
            Bulk(conn, tx, t, "#Deact", "CREATE TABLE #Deact (IdentityID BIGINT NOT NULL PRIMARY KEY)");
            Exec(conn, tx, $"UPDATE i SET Status = 3, LastChangedRunID = {runId} FROM Darlastic.[Identity] i JOIN #Deact d ON d.IdentityID = i.IdentityID; DROP TABLE #Deact");
        }
        if (reactivated.Count > 0)
        {
            var t = NewTable(("IdentityID", typeof(long)));
            foreach (var id in reactivated) t.Rows.Add(id);
            Bulk(conn, tx, t, "#React", "CREATE TABLE #React (IdentityID BIGINT NOT NULL PRIMARY KEY)");
            Exec(conn, tx, $"UPDATE i SET Status = 1, LastChangedRunID = {runId} FROM Darlastic.[Identity] i JOIN #React d ON d.IdentityID = i.IdentityID; DROP TABLE #React");
        }
    }

    private static void WriteProfiles(SqlConnection conn, SqlTransaction tx,
        List<(string Src, string RecId, long IdentityId, string Hash)> rows, List<string> removedKeys, int runId)
    {
        if (rows.Count > 0)
        {
            var t = NewTable(("SourceSystem", typeof(string)), ("SourceRecordId", typeof(string)),
                             ("IdentityID", typeof(long)), ("ContentHash", typeof(string)));
            foreach (var r in rows) t.Rows.Add(r.Src, r.RecId, r.IdentityId, r.Hash);
            Bulk(conn, tx, t, "#Profile", $"""
                CREATE TABLE #Profile (SourceSystem NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                                       SourceRecordId NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                                       IdentityID BIGINT NOT NULL, ContentHash VARCHAR(64) NOT NULL,
                                       PRIMARY KEY (SourceSystem, SourceRecordId))
                """);
            Exec(conn, tx, $"""
                MERGE Darlastic.SourceProfile WITH (HOLDLOCK) AS tgt
                USING #Profile AS src ON tgt.SourceSystem = src.SourceSystem AND tgt.SourceRecordId = src.SourceRecordId
                WHEN MATCHED THEN UPDATE SET IdentityID = src.IdentityID, ContentHash = src.ContentHash,
                                             Removed = 0, RemovedRunID = NULL, LastChangedRunID = {runId}
                WHEN NOT MATCHED THEN INSERT (SourceSystem, SourceRecordId, IdentityID, ContentHash, Removed, RemovedRunID, FirstRunID, LastChangedRunID)
                    VALUES (src.SourceSystem, src.SourceRecordId, src.IdentityID, src.ContentHash, 0, NULL, {runId}, {runId});
                DROP TABLE #Profile
                """, timeoutSeconds: 300);
        }
        if (removedKeys.Count > 0)
        {
            var t = NewTable(("SourceSystem", typeof(string)), ("SourceRecordId", typeof(string)));
            foreach (var k in removedKeys) { int p = k.IndexOf('|'); t.Rows.Add(k[..p], k[(p + 1)..]); }
            Bulk(conn, tx, t, "#Removed", $"""
                CREATE TABLE #Removed (SourceSystem NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                                       SourceRecordId NVARCHAR(64) COLLATE {KeyCollation} NOT NULL,
                                       PRIMARY KEY (SourceSystem, SourceRecordId))
                """);
            // Soft-remove: the row keeps its last IdentityID so a revived record votes its identity
            // back to life instead of minting a stranger. Hard DELETE made every transient source
            // glitch permanently burn golden IDs.
            Exec(conn, tx, $"UPDATE tgt SET Removed = 1, RemovedRunID = {runId}, LastChangedRunID = {runId} FROM Darlastic.SourceProfile tgt JOIN #Removed r ON tgt.SourceSystem = r.SourceSystem AND tgt.SourceRecordId = r.SourceRecordId; DROP TABLE #Removed");
        }
    }

    private static void WriteArtifacts(SqlConnection conn, SqlTransaction tx, List<(string Type, string Key, string Hash, string? Payload)> rows, int runId)
    {
        if (rows.Count == 0) return;
        var t = NewTable(("ArtifactType", typeof(string)), ("ArtifactKey", typeof(string)), ("ContentHash", typeof(string)), ("Payload", typeof(string)));
        foreach (var r in rows) t.Rows.Add(r.Type, r.Key, r.Hash, (object?)r.Payload ?? DBNull.Value);
        // ArtifactType COLLATE DATABASE_DEFAULT: a temp table takes tempdb's collation, but the MERGE
        // below joins it column-to-column against ProjectionState.ArtifactType, which takes the HOST
        // database's collation. The engine used to create its own database (always the instance
        // default, matching tempdb); mounted in a host's database it controls neither side, and a
        // host DB collated differently from the instance dies here with error 468. DATABASE_DEFAULT
        // pins the temp column to the current database, which is what the target column is.
        Bulk(conn, tx, t, "#Artifact", $"CREATE TABLE #Artifact (ArtifactType VARCHAR(32) COLLATE DATABASE_DEFAULT NOT NULL, ArtifactKey NVARCHAR(140) COLLATE {KeyCollation} NOT NULL, ContentHash VARCHAR(64) NOT NULL, Payload NVARCHAR(MAX) NULL, PRIMARY KEY (ArtifactType, ArtifactKey))");
        Exec(conn, tx, $"""
            MERGE Darlastic.ProjectionState WITH (HOLDLOCK) AS tgt
            USING #Artifact AS src ON tgt.ArtifactType = src.ArtifactType AND tgt.ArtifactKey = src.ArtifactKey
            WHEN MATCHED THEN UPDATE SET ContentHash = src.ContentHash, Payload = src.Payload, Pending = 1, UpdatedRunID = {runId}
            WHEN NOT MATCHED THEN INSERT (ArtifactType, ArtifactKey, ContentHash, Pending, UpdatedRunID, ProjectedRunID, Payload)
                VALUES (src.ArtifactType, src.ArtifactKey, src.ContentHash, 1, {runId}, NULL, src.Payload);
            DROP TABLE #Artifact
            """, timeoutSeconds: 600);
    }

    /// <summary>
    /// Persist the steward queue — the [0.80, 0.90) band pairs with their two normalized source
    /// records — as a DERIVED projection of this resolve run. This is what makes a stateless
    /// steward surface possible: a UI loads ~10⁴ queue records from SQL and serves cases with
    /// live trace recompute, instead of re-scoring the whole corpus in memory at startup (the
    /// case browser's minutes/GBs dev-loop shape). Replaced wholesale every run — the queue is a
    /// function of the run, not a delta-disciplined source artifact; IsZeroDelta ignores it.
    /// </summary>
    private static void WriteStewardQueue(SqlConnection conn, SqlTransaction tx,
        IReadOnlyList<RealRecord> records, List<(int A, int B, float Score)> pairs, int runId)
    {
        Exec(conn, tx, "DELETE FROM Darlastic.StewardQueue; DELETE FROM Darlastic.StewardRecord");
        if (pairs.Count == 0) return;

        var recIdxs = new HashSet<int>();
        foreach (var p in pairs) { recIdxs.Add(p.A); recIdxs.Add(p.B); }

        var rt = NewTable(("SourceSystem", typeof(string)), ("SourceRecordId", typeof(string)), ("RunID", typeof(int)), ("Payload", typeof(string)));
        foreach (var i in recIdxs.OrderBy(i => i))
        {
            var r = records[i];
            rt.Rows.Add(r.SourceSystem, r.SourceRecordId, runId, System.Text.Json.JsonSerializer.Serialize(r));
        }
        BulkInto(conn, tx, rt, "Darlastic.StewardRecord");

        var qt = NewTable(("PairKey", typeof(string)), ("RunID", typeof(int)), ("Score", typeof(float)),
            ("SourceSystemA", typeof(string)), ("SourceRecordIdA", typeof(string)),
            ("SourceSystemB", typeof(string)), ("SourceRecordIdB", typeof(string)));
        foreach (var (a, b, score) in pairs)
        {
            var (x, y) = CanonicalPair(records[a], records[b]);
            qt.Rows.Add($"{x.SourceSystem}:{x.SourceRecordId}~{y.SourceSystem}:{y.SourceRecordId}",
                runId, score, x.SourceSystem, x.SourceRecordId, y.SourceSystem, y.SourceRecordId);
        }
        BulkInto(conn, tx, qt, "Darlastic.StewardQueue");
    }

    /// <summary>Same canonical pair order the case browser's PairKey/audit keys use.</summary>
    private static (RealRecord X, RealRecord Y) CanonicalPair(RealRecord a, RealRecord b)
    {
        int c = string.CompareOrdinal(a.SourceSystem, b.SourceSystem);
        if (c == 0) c = string.CompareOrdinal(a.SourceRecordId, b.SourceRecordId);
        return c <= 0 ? (a, b) : (b, a);
    }

    /// <summary>
    /// Re-stage every projected artifact as Pending, for use after the downstream store has been
    /// purged out from under the registry.
    ///
    /// ProjectionState is an assertion about what Cosmos HOLDS ("artifact K is live there at hash H"),
    /// and the whole delta-out discipline rests on it: a resolve that finds nothing changed stages
    /// nothing, and the drain writes only what is staged. Clear Cosmos without clearing that claim and
    /// the registry is confidently wrong — every subsequent run reports zero-delta while the store
    /// stays empty, and no amount of re-ingesting fixes it. So a purge and this call belong together.
    ///
    /// TOMBSTONE rows are DELETED rather than re-staged: they exist to remove a document downstream,
    /// and the purge already removed it — draining them would just issue deletes for ids that are gone.
    /// Returns (restaged, tombstonesDropped).
    /// </summary>
    public static (long Restaged, long TombstonesDropped) RestageProjections()
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        AssertTenant(conn, stampIfMissing: false);   // never re-stage a registry that isn't ours

        int runId = Convert.ToInt32(Scalar(conn, "SELECT ISNULL(MAX(RunID), 0) FROM Darlastic.ResolveRun"));
        long dropped, restaged;
        using (var cmd = new SqlCommand($"DELETE FROM Darlastic.ProjectionState WHERE ContentHash = '{Tombstone}'", conn))
            dropped = cmd.ExecuteNonQuery();
        using (var cmd = new SqlCommand(
            $"UPDATE Darlastic.ProjectionState SET Pending = 1, ProjectedRunID = NULL, UpdatedRunID = {runId} WHERE Pending = 0", conn)
            { CommandTimeout = 300 })
            restaged = cmd.ExecuteNonQuery();
        return (restaged, dropped);
    }

    /// <summary>The steward queue as persisted by the last resolve, records rehydrated —
    /// a display surface can build its case index from THIS subset alone (seconds, ~10⁴ records)
    /// instead of the full corpus.</summary>
    public sealed class StewardQueueData
    {
        public required List<RealRecord> Records;
        public int PairCount;
        public int RunId;
    }

    /// <summary>Read-only, no applock, no schema create — degrade like <see cref="LoadSnapshot"/>.
    /// The scan runs lock-free against a writer that full-replaces both tables inside the resolve
    /// tx, so a commit landing mid-scan could yield a mixed-run record set — detected by re-reading
    /// the run stamp after the scan and retrying (review 2026-07-19).</summary>
    public static StewardQueueData LoadStewardQueue()
    {
        using var conn = new SqlConnection(ConnectionString);
        conn.Open();
        AssertTenant(conn, stampIfMissing: false);
        for (int attempt = 0; ; attempt++)
        {
            int runBefore = Convert.ToInt32(Scalar(conn, "SELECT ISNULL(MAX(RunID), 0) FROM Darlastic.StewardRecord"));
            var records = new List<RealRecord>();
            using (var cmd = new SqlCommand("SELECT Payload FROM Darlastic.StewardRecord ORDER BY SourceSystem, SourceRecordId", conn))
            using (var r = cmd.ExecuteReader())
                while (r.Read())
                {
                    var rec = System.Text.Json.JsonSerializer.Deserialize<RealRecord>(r.GetString(0))
                        ?? throw new InvalidOperationException("Corrupt StewardRecord payload.");
                    records.Add(rec with { Idx = records.Count });
                }
            int pairs = Convert.ToInt32(Scalar(conn, "SELECT COUNT(*) FROM Darlastic.StewardQueue"));
            int runId = Convert.ToInt32(Scalar(conn, "SELECT ISNULL(MAX(RunID), 0) FROM Darlastic.StewardQueue"));
            int runAfter = Convert.ToInt32(Scalar(conn, "SELECT ISNULL(MAX(RunID), 0) FROM Darlastic.StewardRecord"));
            if ((runBefore == runAfter && runId == runAfter) || attempt >= 3)
                return new StewardQueueData { Records = records, PairCount = pairs, RunId = runId };
            // a resolve committed mid-scan — re-read the now-stable state
        }
    }

    // ------------------------------------------------------------------ plumbing

    private static System.Data.DataTable NewTable(params (string Name, Type T)[] cols)
    {
        var t = new System.Data.DataTable();
        foreach (var (name, type) in cols) t.Columns.Add(name, type);
        return t;
    }

    private static void Bulk(SqlConnection conn, SqlTransaction tx, System.Data.DataTable t, string dest, string createSql)
    {
        Exec(conn, tx, createSql);
        BulkInto(conn, tx, t, dest);
    }

    private static void BulkInto(SqlConnection conn, SqlTransaction tx, System.Data.DataTable t, string dest)
    {
        using var bulk = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, tx)
        { DestinationTableName = dest, BulkCopyTimeout = 300, BatchSize = 50_000 };
        foreach (System.Data.DataColumn c in t.Columns) bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);
        bulk.WriteToServer(t);
    }

    private static void Exec(SqlConnection conn, string sql) { using var c = new SqlCommand(sql, conn); c.ExecuteNonQuery(); }
    private static void Exec(SqlConnection conn, SqlTransaction tx, string sql, int timeoutSeconds = 120)
    { using var c = new SqlCommand(sql, conn, tx) { CommandTimeout = timeoutSeconds }; c.ExecuteNonQuery(); }
    private static object Scalar(SqlConnection conn, string sql) { using var c = new SqlCommand(sql, conn); return c.ExecuteScalar()!; }
}
