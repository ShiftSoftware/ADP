using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Darlastic.Data.Entities;

namespace ShiftSoftware.ADP.Darlastic.Data.Extensions;

public static class DarlasticModelBuilderExtensions
{
    /// <summary>
    /// Registry key columns use a binary collation: the engine compares keys with
    /// <c>StringComparer.Ordinal</c> in memory, and a case-insensitive database collation would
    /// let a case-changed source id MERGE-update one row and remove another — the same key must
    /// mean the same thing on both sides.
    /// </summary>
    public const string KeyCollation = "Latin1_General_100_BIN2";

    /// <summary>
    /// Configures the Darlastic registry entities on a host DbContext. Mirrors the schema the
    /// engine's dev bootstrap (<c>Registry.EnsureSchema</c>) creates — this model is the
    /// authoritative source for HOST migrations; the bootstrap is the dev-loop shortcut.
    /// </summary>
    public static ModelBuilder ConfigureDarlasticEntities(this ModelBuilder modelBuilder, string? schema = "Darlastic")
    {
        modelBuilder.Entity<GoldenIdentity>(x =>
        {
            x.ToTable("Identity", schema);
            x.HasKey(e => e.IdentityID);
            x.Property(e => e.IdentityID).ValueGeneratedNever();   // minted by the engine, deterministically
            x.Property(e => e.Status).HasColumnType("tinyint");
        });

        modelBuilder.Entity<IdentityRedirect>(x =>
        {
            x.ToTable("IdentityRedirect", schema);
            x.HasKey(e => e.OldIdentityID);
            x.Property(e => e.OldIdentityID).ValueGeneratedNever();
        });

        modelBuilder.Entity<SourceProfile>(x =>
        {
            x.ToTable("SourceProfile", schema);
            x.HasKey(e => new { e.SourceSystem, e.SourceRecordId });
            x.Property(e => e.SourceSystem).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordId).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.ContentHash).HasColumnType("varchar(64)");
            x.HasIndex(e => e.IdentityID).HasDatabaseName("IX_SourceProfile_Identity");
        });

        modelBuilder.Entity<ProjectionState>(x =>
        {
            x.ToTable("ProjectionState", schema);
            x.HasKey(e => new { e.ArtifactType, e.ArtifactKey });
            x.Property(e => e.ArtifactType).HasColumnType("varchar(32)");
            x.Property(e => e.ArtifactKey).HasMaxLength(140).UseCollation(KeyCollation);   // fits "src|recId" stamp keys (64+1+64)
            x.Property(e => e.ContentHash).HasColumnType("varchar(64)");
        });

        modelBuilder.Entity<ResolveRun>(x =>
        {
            x.ToTable("ResolveRun", schema);
            x.HasKey(e => e.RunID);
            x.Property(e => e.RunID).UseIdentityColumn();
            x.Property(e => e.Notes).HasMaxLength(400);
        });

        modelBuilder.Entity<AuditEntry>(x =>
        {
            x.ToTable("AuditEntry", schema);
            x.HasKey(e => e.AuditID);
            x.Property(e => e.AuditID).UseIdentityColumn();
            x.Property(e => e.Actor).HasMaxLength(128);
            x.Property(e => e.Action).HasColumnType("varchar(32)");
            x.Property(e => e.TargetKey).HasMaxLength(256);
        });

        modelBuilder.Entity<StewardDecision>(x =>
        {
            x.ToTable("StewardDecision", schema);
            x.HasKey(e => e.DecisionID);
            x.Property(e => e.DecisionID).UseIdentityColumn();
            x.Property(e => e.Actor).HasMaxLength(128);
            x.Property(e => e.Kind).HasColumnType("varchar(16)");
            x.Property(e => e.SourceSystem).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordId).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.AttrType).HasMaxLength(64);
            x.Property(e => e.Value).HasMaxLength(1024);
        });

        modelBuilder.Entity<StewardQueueEntry>(x =>
        {
            x.ToTable("StewardQueue", schema);
            x.HasKey(e => e.PairKey);
            x.Property(e => e.PairKey).HasMaxLength(300).UseCollation(KeyCollation);
            x.Property(e => e.SourceSystemA).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordIdA).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceSystemB).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordIdB).HasMaxLength(64).UseCollation(KeyCollation);
        });

        modelBuilder.Entity<StewardRecord>(x =>
        {
            x.ToTable("StewardRecord", schema);
            x.HasKey(e => new { e.SourceSystem, e.SourceRecordId });
            x.Property(e => e.SourceSystem).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordId).HasMaxLength(64).UseCollation(KeyCollation);
        });

        modelBuilder.Entity<IdentityEdge>(x =>
        {
            x.ToTable("IdentityEdge", schema);
            x.HasKey(e => e.EdgeID);
            x.Property(e => e.EdgeID).UseIdentityColumn();
            x.Property(e => e.SourceSystemA).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordIdA).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceSystemB).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordIdB).HasMaxLength(64).UseCollation(KeyCollation);
            // The only read that matters: "how was identity N assembled".
            x.HasIndex(e => e.IdentityID).HasDatabaseName("IX_IdentityEdge_Identity");
        });

        modelBuilder.Entity<CaseCatalogEntry>(x =>
        {
            x.ToTable("CaseCatalog", schema);
            x.HasKey(e => e.PairKey);
            x.Property(e => e.PairKey).HasMaxLength(300).UseCollation(KeyCollation);
            x.Property(e => e.SourceSystemA).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordIdA).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceSystemB).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordIdB).HasMaxLength(64).UseCollation(KeyCollation);
            // Sidebar filtering is a bitmask test, so this index earns its place only on the
            // score ordering the case list defaults to.
            x.HasIndex(e => e.Score).HasDatabaseName("IX_CaseCatalog_Score");
        });

        modelBuilder.Entity<IdentitySummary>(x =>
        {
            x.ToTable("IdentitySummary", schema);
            x.HasKey(e => e.IdentityID);
            x.Property(e => e.IdentityID).ValueGeneratedNever();
            x.Property(e => e.GoldenName).HasMaxLength(400);
            // The identities list defaults to biggest-first.
            x.HasIndex(e => e.MemberCount).HasDatabaseName("IX_IdentitySummary_MemberCount");
        });

        modelBuilder.Entity<ReviewFlag>(x =>
        {
            x.ToTable("ReviewFlag", schema);
            x.HasKey(e => e.FlagID);
            x.Property(e => e.FlagID).UseIdentityColumn();
            x.Property(e => e.Target).HasMaxLength(300).UseCollation(KeyCollation);
            x.Property(e => e.Topic).HasMaxLength(64);
            x.Property(e => e.Author).HasMaxLength(128);
            x.Property(e => e.ResponseBy).HasMaxLength(128);
            // One open flag per target — re-flagging edits rather than accumulating duplicates.
            x.HasIndex(e => e.Target).IsUnique().HasDatabaseName("UX_ReviewFlag_Target");
        });

        modelBuilder.Entity<LabelAudit>(x =>
        {
            x.ToTable("LabelAudit", schema);
            x.HasKey(e => e.AuditID);
            x.Property(e => e.AuditID).UseIdentityColumn();
            x.Property(e => e.PairKey).HasMaxLength(300).UseCollation(KeyCollation);
            x.Property(e => e.OldLabel).HasMaxLength(16);
            x.Property(e => e.NewLabel).HasMaxLength(16);
            x.Property(e => e.AuditedBy).HasMaxLength(128);
            x.Property(e => e.PanelVotes).HasMaxLength(32);
            x.Property(e => e.Status).HasMaxLength(16);
            // Deliberately NOT unique: a pair can be re-adjudicated, and the history is the point.
            x.HasIndex(e => e.PairKey).HasDatabaseName("IX_LabelAudit_PairKey");
        });

        modelBuilder.Entity<CaseCategoryCount>(x =>
        {
            x.ToTable("CaseCategoryCount", schema);
            x.HasKey(e => e.Category);
            x.Property(e => e.Category).HasMaxLength(32).UseCollation(KeyCollation);
        });

        modelBuilder.Entity<TenantMarker>(x =>
        {
            x.ToTable("TenantMarker", schema);
            // The engine's own bootstrap creates this table without a key (it only ever holds one
            // row, read with SELECT TOP 1). A host migration gets a real key instead — harmless to
            // the engine, and a keyless entity in a consumer's DbContext is a trap worth avoiding.
            x.HasKey(e => e.Tenant);
            x.Property(e => e.Tenant).HasMaxLength(64);
        });

        // Read-only projection over the golden artifacts (serves golden list/search surfaces such
        // as ADP.Darlastic.Web's GoldenCustomerList). ToView keeps it out of table migrations —
        // the view itself is created by a host migration running DarlasticViews SQL.
        modelBuilder.Entity<GoldenCustomer>(x =>
        {
            x.ToView("GoldenCustomer", schema);
            x.HasKey(e => e.ID);
        });

        return modelBuilder;
    }

    /// <summary>
    /// Adds the verification tables (the report log and the verified claims) to a host DbContext. Apart from
    /// <see cref="ConfigureDarlasticEntities"/> on purpose: a host gets these two tables, and the migration
    /// that creates them, only once it turns verification on. No resolve reads or writes them.
    /// </summary>
    public static ModelBuilder ConfigureDarlasticVerificationEntities(this ModelBuilder modelBuilder, string? schema = "Darlastic")
    {
        modelBuilder.Entity<VerificationEntry>(x =>
        {
            x.ToTable("VerificationLog", schema);
            x.HasKey(e => e.EntryID);
            x.Property(e => e.EntryID).UseIdentityColumn();
            x.Property(e => e.Actor).HasMaxLength(128);
            x.Property(e => e.Kind).HasColumnType("varchar(32)");
            x.Property(e => e.ValueKey).HasMaxLength(128).UseCollation(KeyCollation);
            x.Property(e => e.Method).HasMaxLength(64);
            x.Property(e => e.Outcome).HasColumnType("varchar(16)");
            x.Property(e => e.Location).HasMaxLength(128);
            x.Property(e => e.SourceSystem).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordId).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.EvidenceReference).HasMaxLength(256);
            x.HasIndex(e => new { e.IdentityID, e.AtUtc }).HasDatabaseName("IX_VerificationLog_Identity");
        });

        modelBuilder.Entity<VerifiedClaim>(x =>
        {
            x.ToTable("VerifiedClaim", schema);
            x.HasKey(e => e.ClaimID);
            x.Property(e => e.ClaimID).UseIdentityColumn();
            x.Property(e => e.Kind).HasColumnType("varchar(32)");
            x.Property(e => e.ValueKey).HasMaxLength(128).UseCollation(KeyCollation);
            x.Property(e => e.Method).HasMaxLength(64);
            x.Property(e => e.EndReason).HasColumnType("varchar(16)");
            x.Property(e => e.Location).HasMaxLength(128);
            x.Property(e => e.SourceSystem).HasMaxLength(64).UseCollation(KeyCollation);
            x.Property(e => e.SourceRecordId).HasMaxLength(64).UseCollation(KeyCollation);

            x.HasOne(e => e.StartedBy).WithMany().HasForeignKey(e => e.StartedByEntryID).OnDelete(DeleteBehavior.Restrict);
            x.HasOne(e => e.EndedBy).WithMany().HasForeignKey(e => e.EndedByEntryID).OnDelete(DeleteBehavior.Restrict);

            // A customer's claims, open and ended.
            x.HasIndex(e => new { e.IdentityID, e.Kind }).HasDatabaseName("IX_VerifiedClaim_Identity");
            // One open claim per customer and value: verifying again closes the old period first.
            x.HasIndex(e => new { e.Kind, e.ValueKey, e.IdentityID }).IsUnique()
                .HasFilter("[EndUtc] IS NULL").HasDatabaseName("UX_VerifiedClaim_OpenPerIdentity");
            // An exclusive value (a phone, a vehicle) is verified for one customer at a time.
            x.HasIndex(e => new { e.Kind, e.ValueKey }).IsUnique()
                .HasFilter("[EndUtc] IS NULL AND [Exclusive] = 1").HasDatabaseName("UX_VerifiedClaim_OpenExclusive");
        });

        return modelBuilder;
    }
}
