using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore;

using ShiftSoftware.ADP.Menus.Data;
using ShiftSoftware.ADP.Menus.Data.DataServices;
using ShiftSoftware.ADP.Menus.Data.Entities;

namespace ShiftSoftware.ADP.Menus.Tests;

/// <summary>
/// Compiles the DMS export's query — soft-delete filters and all — to SQL, OFFLINE.
///
/// <para><c>ToQueryString()</c> runs the whole EF pipeline (model building, include expansion, filtered-
/// include validation, expression translation) and stops just before opening a connection. No database,
/// no emulator, nothing to install: a fake connection string is enough, because nothing is executed.</para>
///
/// <para><b>Why this test exists.</b> Filtered includes fail at QUERY TIME, not compile time, and the
/// two failure modes here are both invisible to the compiler:</para>
/// <list type="bullet">
/// <item><c>Items</c> is included three times, and EF Core throws if the same navigation is included
/// more than once with different filters. The three predicates have to stay identical, and nothing but
/// running the query says whether they are.</item>
/// <item>A predicate that reaches through a reference navigation (<c>item.ReplacementItemVehicleModel
/// .ReplacementItem.IsDeleted</c>) has to be translatable. If it is not, EF throws rather than falling
/// back to client evaluation.</item>
/// </list>
///
/// <para>Without this, the first sign of either would be a 500 from a live export.</para>
/// </summary>
public class MenuExportIncludesTests
{
    /// <summary>
    /// Never connected to — <c>ToQueryString()</c> compiles the query and stops. The value only has to
    /// parse as a connection string.
    /// </summary>
    private const string UnusedConnectionString = "Server=(localdb)\\unused;Database=unused;Trusted_Connection=True;";

    private static MenuDB Database()
    {
        var options = new DbContextOptionsBuilder<MenuDB>()
            .UseSqlServer(UnusedConnectionString)
            .Options;

        return new MenuDB(options);
    }

    /// <summary>
    /// The export's query as SQL.
    /// </summary>
    /// <param name="singleQuery">
    /// The shipped query is <c>AsSplitQuery</c>, and <c>ToQueryString()</c> returns only the FIRST
    /// statement of a split query — so asserting on the whole graph needs it collapsed. Overriding it
    /// here changes only how the rows are fetched, never which rows or which predicates, so what is
    /// asserted below is what the split version filters on too.
    /// </param>
    private static string ExportSql(bool singleQuery = false)
    {
        using var database = Database();

        var query = MenuExportIncludes
            .Apply(database.Set<MenuVariant>().AsNoTracking().Where(x => !x.IsDeleted && !x.Menu.IsDeleted));

        return (singleQuery ? query.AsSingleQuery() : query).ToQueryString();
    }

    /// <summary>Every table the query reads, mapped to the alias SQL Server gave it.</summary>
    private static ILookup<string, string> AliasesByTable(string sql) =>
        Regex.Matches(sql, @"(?:FROM|JOIN) \[\w+\]\.\[(\w+)\] AS \[(\w+)\]")
            .Select(match => (Table: match.Groups[1].Value, Alias: match.Groups[2].Value))
            .ToLookup(x => x.Table, x => x.Alias);

    /// <summary>
    /// Whether the alias carries a soft-delete PREDICATE — not merely an <c>IsDeleted</c> column in the
    /// SELECT list, which every soft-deletable table has and which proves nothing.
    /// </summary>
    private static bool HasDeletePredicate(string sql, string alias) =>
        sql.Contains($"[{alias}].[IsDeleted] = CAST(0", StringComparison.Ordinal)
        || sql.Contains($"NOT ([{alias}].[IsDeleted])", StringComparison.Ordinal);

    /// <summary>
    /// The whole point: the query translates. A repeated include with a differing filter, or an
    /// untranslatable predicate, throws here instead of in production.
    /// </summary>
    [Fact]
    public void ExportQuery_Translates()
    {
        var sql = ExportSql();

        Assert.False(string.IsNullOrWhiteSpace(sql));
        Assert.Contains("SELECT", sql, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// The filters actually reached the SQL. Without this the includes could be silently unfiltered and
    /// every other test would still pass — the adapter would clean up afterwards and the OUTPUT would be
    /// identical. Only the SQL says whether the rows stopped travelling, which is the entire point of
    /// filtering here.
    /// </summary>
    [Theory]
    [InlineData("MenuVariants")]
    [InlineData("Menus")]
    [InlineData("MenuItems")]
    [InlineData("ReplacementItemVehicleModels")]
    [InlineData("ReplacementItems")]
    [InlineData("MenuItemParts")]
    [InlineData("MenuItemPartCountryPrices")]
    [InlineData("ReplacementItemServiceIntervalGroup")]
    [InlineData("ServiceIntervalGroups")]
    [InlineData("ServiceIntervals")]
    [InlineData("MenuPeriodicAvailability")]
    [InlineData("MenuLabourDetails")]
    [InlineData("MenuVariantLabourRates")]
    public void EverySoftDeletableTable_IsFilteredInTheDatabase(string table)
    {
        var sql = ExportSql(singleQuery: true);
        var aliases = AliasesByTable(sql)[table].ToList();

        Assert.NotEmpty(aliases);

        // EVERY occurrence, not just one: ServiceIntervals and ServiceIntervalGroups are each joined
        // more than once (via the replacement item and via the labour details), and one of them being
        // filtered says nothing about the other.
        Assert.All(aliases, alias => Assert.True(
            HasDeletePredicate(sql, alias),
            $"{table} (alias {alias}) is loaded without a soft-delete predicate."));
    }

    /// <summary>
    /// The documented limit, pinned: <c>StandaloneReplacementItemGroup</c> is a REFERENCE navigation, so
    /// EF cannot filter it — and the rule is not "drop the item" anyway, it is "keep the item, drop its
    /// group", which no query can express. A deleted group is loaded here and nulled by
    /// <see cref="EfToGenerationAggregator"/>.
    ///
    /// Asserted as an ABSENCE so that if this ever starts being filtered, the failure points whoever did
    /// it at the adapter rule they are about to break — deleting the group would then drop the item's
    /// standalone line instead of ungrouping it.
    /// </summary>
    [Fact]
    public void TheStandaloneGroup_IsDeliberatelyNotFilteredInTheDatabase()
    {
        var sql = ExportSql(singleQuery: true);
        var aliases = AliasesByTable(sql)["StandaloneReplacementItemGroup"].ToList();

        Assert.NotEmpty(aliases);
        Assert.All(aliases, alias => Assert.False(HasDeletePredicate(sql, alias)));
    }

    /// <summary>
    /// The database filter must not be the ONLY one. <c>EfToGenerationAggregator</c> re-applies the same
    /// rule, and that is what the in-memory agreement test can reach — this asserts the belt is still
    /// there, so removing it later is a deliberate act rather than a quiet regression.
    /// </summary>
    [Fact]
    public void TheAdapterStillFilters_EvenThoughTheQueryDoes()
    {
        // The fixture's graph is built in memory, so it has never been through the query — every
        // soft-deleted row is present. If the adapter had stopped filtering, they would survive.
        var request = EfToGenerationAggregator.Build(
            MenuGraphFixture.Build().Variants,
            MenuGraphFixture.Build().LabourRateMappings,
            MenuGraphFixture.Build().BrandMappings);

        var variant = request.Variants.Single();

        Assert.Equal([900, 901, 902], variant.Items.Select(item => item.MenuItemID));
        Assert.DoesNotContain(
            variant.Items.SelectMany(item => item.Parts),
            part => part.PartNumber == "PN-0003");
    }
}
