using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Cases.Data.Entities;

namespace ShiftSoftware.ADP.Cases.Data.Extensions;

public static class CasesModelBuilderExtensions
{
    /// <summary>
    /// Registers the ADP.Cases entities into the consumer's model. NOTE: a consumer whose DbContext
    /// already declares <c>DbSet&lt;Certificate&gt; Certificates</c> (the original host application does — that DbSet name is
    /// what preserves the prod table name) does not strictly need this call while schema is null;
    /// it exists for consumers without DbSets and for schema-isolated hosts (sample/schema-isolated style).
    /// </summary>
    /// <param name="schema">Schema for the module's tables + temporal history tables.
    /// Pass null (as the original host application does) to keep everything in the default schema (dbo) — required for the
    /// pre-existing prod temporal tables.</param>
    public static ModelBuilder ConfigureCasesEntities(this ModelBuilder modelBuilder, string? schema = "Cases")
    {
        // Explicit table name: the prod table is "Certificates" (plural, from the host's DbSet name),
        // while the CLR conventional name would be "Certificate".
        var certificate = modelBuilder.Entity<Certificate>();
        certificate.ToTable("Certificates");

        if (schema is not null)
        {
            certificate.Metadata.SetSchema(schema);
        }

        return modelBuilder;
    }
}
