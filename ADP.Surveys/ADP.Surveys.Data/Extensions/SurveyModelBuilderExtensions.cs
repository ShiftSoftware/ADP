using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ADP.Surveys.Data.Entities;

namespace ShiftSoftware.ADP.Surveys.Data.Extensions;

public static class SurveyModelBuilderExtensions
{
    public static ModelBuilder ConfigureSurveyEntities(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Survey>(x =>
        {
            x.HasIndex(s => s.Name).IsUnique()
                .HasFilter($"{nameof(Survey.IsDeleted)} = 0");
        });

        modelBuilder.Entity<SurveyVersion>(x =>
        {
            x.HasOne(v => v.Survey)
                .WithMany(s => s.Versions)
                .HasForeignKey(v => v.SurveyID)
                .OnDelete(DeleteBehavior.Cascade);

            // Version numbers are monotonic per survey. Unique lets us detect races on publish.
            x.HasIndex(v => new { v.SurveyID, v.Version }).IsUnique();

            x.HasIndex(v => v.Hash);
        });

        modelBuilder.Entity<BankQuestion>(x =>
        {
            // Stable GUID anchor referenced by SurveyAnswer.BankEntryID (Decision #11).
            x.HasIndex(b => b.BankEntryID).IsUnique();

            // Human-readable key — surveys reference this via bankRef. Unique among non-deleted.
            x.HasIndex(b => b.Key).IsUnique()
                .HasFilter($"{nameof(BankQuestion.IsDeleted)} = 0");
        });

        modelBuilder.Entity<ScreenTemplate>(x =>
        {
            x.HasIndex(t => t.Key).IsUnique()
                .HasFilter($"{nameof(ScreenTemplate.IsDeleted)} = 0");
        });

        modelBuilder.Entity<SurveyInstance>(x =>
        {
            // The public capability — the GUID that appears in the URL handed to the customer.
            x.HasIndex(i => i.PublicID).IsUnique();

            x.HasOne(i => i.Survey)
                .WithMany(s => s.Instances)
                .HasForeignKey(i => i.SurveyID)
                .OnDelete(DeleteBehavior.Restrict);

            x.HasOne(i => i.SurveyVersion)
                .WithMany(v => v.Instances)
                .HasForeignKey(i => i.SurveyVersionID)
                .OnDelete(DeleteBehavior.Restrict);

            x.HasIndex(i => i.CustomerRef);
            x.HasIndex(i => i.Status);
        });

        modelBuilder.Entity<SurveyResponse>(x =>
        {
            x.HasOne(r => r.SurveyInstance)
                .WithMany(i => i.Responses)
                .HasForeignKey(r => r.SurveyInstanceID)
                .OnDelete(DeleteBehavior.Cascade);

            x.HasIndex(r => r.CompletedAt);
        });

        modelBuilder.Entity<SurveyAnswer>(x =>
        {
            x.HasOne(a => a.SurveyResponse)
                .WithMany(r => r.Answers)
                .HasForeignKey(a => a.SurveyResponseID)
                .OnDelete(DeleteBehavior.Cascade);

            // FK points at BankQuestion.BankEntryID (not the long PK) so the GUID
            // is the stable anchor per Decision #11. Nullable for inline answers.
            x.HasOne(a => a.BankQuestion)
                .WithMany(b => b.Answers)
                .HasForeignKey(a => a.BankEntryID)
                .HasPrincipalKey(b => b.BankEntryID)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            // The BI export's primary join key.
            x.HasIndex(a => a.BankEntryID);
            x.HasIndex(a => a.KeyAtSubmission);
        });

        // Place every entity owned by the Surveys package under the "Surveys" SQL
        // schema (and the matching temporal history table schema for entities that
        // carry [TemporalShiftEntity]). Mirrors the ADP.Menus pattern.
        var surveysAssembly = typeof(SurveyModelBuilderExtensions).Assembly;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (entityType.ClrType.Assembly != surveysAssembly)
                continue;

            entityType.SetSchema("Surveys");
            entityType.SetHistoryTableSchema("Surveys");
        }

        return modelBuilder;
    }
}
