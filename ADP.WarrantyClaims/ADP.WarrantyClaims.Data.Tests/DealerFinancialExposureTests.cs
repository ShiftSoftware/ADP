using System.Reflection;
using System.Text.Json;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Core;
using Xunit;
using Entities = ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// The permanent guard on the dealer/distributor financial split.
///
/// <para>
/// <b>What this protects.</b> <c>DealerFinancialListDTO</c> is declared
/// <c>: DistributorFinancialListDTO { }</c> - an empty subclass that adds and removes nothing. Five
/// distributor-side figures are withheld from the dealer audience purely by the mapper, and the
/// entity carries a real value for every one of them. If those five stop being withheld, the
/// endpoint still returns <b>200</b>, the response shape is <b>unchanged</b>, and <b>no compiler
/// diagnostic fires</b> - the only symptom is that dealers begin receiving the distributor's margin
/// figures. That is a data-exposure regression with no natural alarm, which is why it gets a
/// dedicated test rather than relying on the endpoint suite.
/// </para>
///
/// <para>
/// <b>Why it diffs the two projections instead of listing five names.</b> A test that only asserts
/// "these five are null" passes just as happily if the dealer map silently stops mapping something
/// else - blanking too much is as wrong as blanking too little. Projecting the SAME entity through
/// BOTH mappers and diffing every property asserts the stronger and actually-intended property: the
/// dealer list differs from the distributor list by <b>exactly</b> those five members and nothing
/// else. A member added to either map in future is covered the moment it is declared.
/// </para>
/// </summary>
public class DealerFinancialExposureTests
{
    /// <summary>
    /// The complete set of members withheld from dealers, transcribed from the five
    /// <c>.ForMember(..., x =&gt; x.Ignore())</c> calls on the pre-migration dealer map.
    /// </summary>
    private static readonly string[] WithheldFromDealer =
    {
        "DistComment1",
        "HourTotalDistributor",
        "LaborTotalAmountDistributor",
        "SubletTotalAmountDistributor",
        "PartsTotalAmountDistributor",
    };

    /// <summary>
    /// A claim with every withheld column populated, plus the shared members, so that "withheld"
    /// is observable rather than vacuous: if the mapper stops withholding, these values appear.
    /// </summary>
    private static Entities.WarrantyClaim SeedClaim() => new()
    {
        VIN = "EXPOSURETESTVIN001",
        ClaimNumber = "EXPOSURE-0001",
        WarrantyType = "W",
        Franchise = "F",
        RepairOrderNo = "RO-1",
        DataID = "D",
        Condition = "C",
        Cause = "C",
        Remedy = "R",
        VIN_WMI = "A",
        VIN_VDS = "B",
        VIN_CD = "C",
        VIN_VIS = "D",

        // ---- the five that must NOT reach a dealer ----
        DistComment1 = "MUST-NOT-LEAK",
        HourTotalDistributor = 11.11m,
        LaborTotalAmountDistributor = 2222.22m,
        SubletTotalAmountDistributor = 3333.33m,
        PartsTotalAmountDistributor = 4444.44m,

        // ---- shared members, mapped on BOTH maps ----
        ProcessDate = new DateTime(2024, 3, 4, 5, 6, 7, DateTimeKind.Unspecified),
        DistributorProcessDate = new DateTime(2024, 4, 5, 6, 7, 8, DateTimeKind.Unspecified),

        // The list projection reaches through this navigation unguarded, exactly as the old profile
        // did. In production the projection runs as SQL, where a missing reference yields null; in
        // memory it would dereference null, so the test supplies one.
        ReferenceWarrantyClaim = new Entities.WarrantyClaim
        {
            ClaimNumber = "REF-0001",
            VIN = "REF", WarrantyType = "W", Franchise = "F", RepairOrderNo = "RO",
            DataID = "D", Condition = "C", Cause = "C", Remedy = "R",
            VIN_WMI = "A", VIN_VDS = "B", VIN_CD = "C", VIN_VIS = "D",
        },
    };

    [Fact]
    public void The_five_distributor_figures_are_withheld_from_the_dealer_list()
    {
        var dealer = ProjectOne<DealerFinancialListDTO>(SeedClaim());

        foreach (var member in WithheldFromDealer)
        {
            var value = typeof(DistributorFinancialListDTO).GetProperty(member)!.GetValue(dealer);
            Assert.True(value is null,
                $"{member} must be null on the dealer list. The entity has a value for it, so a " +
                $"non-null here means the mapper stopped withholding it and dealers are now being " +
                $"served distributor-side figures. Got: {value}");
        }
    }

    [Fact]
    public void The_shared_members_are_still_configured_on_the_dealer_map()
    {
        // Guards the other direction: blanking too much is as wrong as blanking too little.
        // ProcessDate, DistributorProcessDate and ReferenceWarrantyClaimNumber were configured on
        // BOTH old maps, so the dealer map must still carry them.
        //
        // This asserts against the generator's own record of what the repository configured
        // (__shiftBakedCustom) rather than against projected VALUES, and deliberately so: those
        // three are supplied by the repository's UseGeneratedMapper config, which a directly
        // constructed mapper does not carry - only the ignores are baked into the type itself.
        // Asserting values here would need a live DbContext and would test the harness more than
        // the mapper.
        var baked = BakedCustom(MapperTypeFor<DealerFinancialListDTO>());

        foreach (var member in new[] { "ProcessDate", "DistributorProcessDate", "ReferenceWarrantyClaimNumber" })
            Assert.Contains(member, baked);
    }

    [Fact]
    public void Dealer_and_distributor_lists_differ_by_exactly_those_five_members()
    {
        var dealer = ProjectOne<DealerFinancialListDTO>(SeedClaim());
        var distributor = ProjectOne<DistributorFinancialListDTO>(SeedClaim());

        var differing = typeof(DistributorFinancialListDTO)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            // Compared by serialized VALUE, not by reference: ShiftEntityListDTO's own collection
            // members are distinct instances on two separately-projected DTOs and would otherwise
            // register as differences on every run.
            .Where(p => JsonSerializer.Serialize(p.GetValue(dealer)) != JsonSerializer.Serialize(p.GetValue(distributor)))
            .Select(p => p.Name)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            WithheldFromDealer.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            differing);
    }

    /// <summary>
    /// Runs a triple's real list projection over a single entity. <c>AsQueryable</c> makes the
    /// generated <c>__shiftListProjection</c> expression execute in memory, so this exercises the
    /// same projection the endpoint splices into SQL rather than a re-implementation of it.
    /// </summary>
    private static TListDTO ProjectOne<TListDTO>(Entities.WarrantyClaim entity)
    {
        var mapper = (IShiftEntityMapper<Entities.WarrantyClaim, TListDTO, WarrantyClaimDTO>)
            Activator.CreateInstance(MapperTypeFor<TListDTO>(), nonPublic: true)!;

        return mapper.MapToList(new[] { entity }.AsQueryable()).Single();
    }

    /// <summary>
    /// The generated mapper type for a triple, found by its closed interface. Narrowed to the
    /// generated-mappers namespace because <c>ShiftRepository</c> also satisfies that interface and
    /// constructing one would need a DbContext.
    /// </summary>
    private static Type MapperTypeFor<TListDTO>()
    {
        var closed = typeof(IShiftEntityMapper<Entities.WarrantyClaim, TListDTO, WarrantyClaimDTO>);

        return Assert.Single(
            typeof(Entities.WarrantyClaim).Assembly.GetTypes(),
            t => t is { IsAbstract: false, IsInterface: false }
                 && t.Namespace == "ShiftSoftware.ShiftEntity.GeneratedMappers"
                 && closed.IsAssignableFrom(t));
    }

    /// <summary>
    /// The generator's own record of which members the repository configured explicitly. Reading it
    /// is the same device item J uses to audit the emitted code.
    /// </summary>
    private static string[] BakedCustom(Type mapperType) =>
        (string[])mapperType
            .GetField("__shiftBakedCustom", BindingFlags.NonPublic | BindingFlags.Static)!
            .GetValue(null)!;
}
