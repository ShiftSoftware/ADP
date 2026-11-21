using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using ShiftSoftware.ADP.Cases.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Data.Extensions;
using ShiftSoftware.ADP.WarrantyClaims.Data.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Data.Services;
using ShiftSoftware.ADP.WarrantyClaims.Shared;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using Xunit;
using Entities = ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// The currency of a claim: where it comes from, the rate stored on the claim, and the USD equivalents.
/// XTS is the ISO 4217 code reserved for testing.
/// </summary>
public class ClaimCurrencyTests
{
    private const string Local = "XTS";

    /// <summary>A host with a local currency whose labor rate is a USD figure converted at the claim's rate.</summary>
    private class LocalCurrencyResolver : IWarrantyClaimCurrencyResolver
    {
        public ValueTask<string> GetCurrencyAsync(long? companyBranchID) => new(Local);

        public ValueTask<decimal> GetLaborRateAsync(WarrantyClaimLaborRateContext context) =>
            new(DocumentCurrency.FromUSD(25, context.Currency, context.CurrencyRate));
    }

    private static Task<decimal?> NoRateLookup(string currency) =>
        throw new InvalidOperationException("The rate table must not be read here.");

    private static WarrantyClaimDTO ADealerClaim() => new()
    {
        WarrantyType = "VE",
        Franchise = "F",
        RepairOrderNo = "RO-1",
        Condition = "C",
        Cause = "C",
        Remedy = "R",
        VIN_WMI = "AAA",
        VIN_VDS = "BBBBB",
        VIN_CD = "0",
        VIN_VIS = "00000001",
        WarrantyClaimLaborLines = [new() { PayCode = "1", MainOperation = true, OperationNumber = "OP-1", Hour = 1.5m }],
        WarrantyClaimSubletLines = [new() { PayCode = "1", SubletType = "PT", InvoiceNo = "S-1", Description = "D", Amount = 100_000 }],
        WarrantyClaimPartLines = [new() { PayCode = "1", OFP = true, LocalF = "L", PartNumber = "P-1", PartDescription = "D", Qty = 2, Price = 250_000 }],
    };

    /// <summary>The save path of WarrantyClaimRepository.UpsertAsync, without the database.</summary>
    private static async Task<Entities.WarrantyClaim> SaveAsync(
        WarrantyClaimService service, ActionTypes actionType, Entities.WarrantyClaim claim, WarrantyClaimDTO dto, Func<string, Task<decimal?>> currentRate, bool canSetRate = false)
    {
        await service.ApplyCurrencyAsync(actionType, claim, dto, companyBranchID: 12, canSetRate, currentRate);
        service.WarrantyLinesValidationAndTransformation(actionType, claim, dto, isDistributor: false);

        ModuleMapper.For<Entities.WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>().MapToEntity(dto, claim);

        service.UpdateCurrencyAmounts(claim);

        return claim;
    }

    [Fact]
    public async Task A_host_without_a_resolver_keeps_USD_claims_at_labor_rate_25()
    {
        var claim = await SaveAsync(new WarrantyClaimService(), ActionTypes.Insert, new(), ADealerClaim(), NoRateLookup);

        Assert.Equal("USD", claim.Currency);
        Assert.Equal(1, claim.CurrencyRate);
        Assert.Equal(25, claim.LaborRate);
        Assert.Equal(37.5m, claim.LaborTotalAmount);
        Assert.Equal(37.5m, claim.LaborTotalAmountDistributor);
        Assert.Equal(100_000, Assert.Single(claim.WarrantyClaimSubletLines).DistributorAmount);
        Assert.Equal(600_037.5m, claim.TotalClaimAmount);

        Assert.Equal(claim.LaborRate, claim.LaborRateUSD);
        Assert.Equal(claim.LaborTotalAmount, claim.LaborTotalAmountUSD);
        Assert.Equal(claim.SubletTotalAmount, claim.SubletTotalAmountUSD);
        Assert.Equal(claim.PartsTotalAmount, claim.PartsTotalAmountUSD);
        Assert.Equal(claim.TotalClaimAmount, claim.TotalClaimAmountUSD);
        Assert.Equal(250_000, Assert.Single(claim.WarrantyClaimPartLines).PriceUSD);
    }

    /// <summary>
    /// A claim as hosts and their tests build it, and as a row reads before the data migration has run:
    /// the labor rate set, none of the USD members.
    /// </summary>
    [Fact]
    public void A_USD_claim_without_USD_equivalents_exports_as_before()
    {
        var claim = new Entities.WarrantyClaim { LaborRate = 25, HourTotalDistributor = 2 };
        claim.WarrantyClaimSubletLines.Add(new() { Amount = 10.5m, DistributorAmount = 10.5m });

        new WarrantyClaimService().UpdateAmounts(new WarrantyRatesDTO { PRR = 1, LaborExchangeRate = 150.5m, SubletExchangeRate = 150, PartExchangeRate = 150 }, claim);

        Assert.Equal(3_763, claim.LaborRateJPY);
        Assert.Equal(50, claim.LaborTotalAmountDistributor);
        Assert.Equal(7_526, claim.LaborTotalAmountDistributorJPY);
        Assert.Equal(1_575, claim.SubletTotalAmountJPY);
    }

    [Fact]
    public async Task A_local_currency_claim_stores_its_rate_and_the_USD_equivalents()
    {
        var service = new WarrantyClaimService(currencyResolver: new LocalCurrencyResolver());

        var claim = await SaveAsync(service, ActionTypes.Insert, new(), ADealerClaim(), _ => Task.FromResult<decimal?>(12_500));

        Assert.Equal(Local, claim.Currency);
        Assert.Equal(12_500, claim.CurrencyRate);

        // Dealer side, in the claim's currency.
        Assert.Equal(312_500, claim.LaborRate);
        Assert.Equal(468_750, claim.LaborTotalAmount);
        Assert.Equal(1_068_750, claim.TotalClaimAmount);

        Assert.Equal(25, claim.LaborRateUSD);
        Assert.Equal(37.5m, claim.LaborTotalAmountUSD);
        Assert.Equal(8, claim.SubletTotalAmountUSD);
        Assert.Equal(40, claim.PartsTotalAmountUSD);
        Assert.Equal(85.5m, claim.TotalClaimAmountUSD);
        Assert.Equal(20, Assert.Single(claim.WarrantyClaimPartLines).PriceUSD);

        // Distributor side stays in USD: the labor total, and the amount a dealer's sublet defaults to.
        var sublet = Assert.Single(claim.WarrantyClaimSubletLines);
        Assert.Equal(8, sublet.AmountUSD);
        Assert.Equal(8, sublet.DistributorAmount);
        Assert.Equal(37.5m, claim.LaborTotalAmountDistributor);

        // The manufacturer export converts the USD figures to yen, never the local ones.
        service.UpdateAmounts(new WarrantyRatesDTO { PRR = 1, LaborExchangeRate = 150, SubletExchangeRate = 150, PartExchangeRate = 150 }, claim);

        Assert.Equal(3_750, claim.LaborRateJPY);
        Assert.Equal(1_200, sublet.AmountJPY);
        Assert.Equal(37.5m, claim.LaborTotalAmountDistributor);
    }

    [Fact]
    public async Task A_saved_claim_keeps_its_rate_and_a_new_rate_prices_the_labor_again()
    {
        var service = new WarrantyClaimService(currencyResolver: new LocalCurrencyResolver());

        var claim = await SaveAsync(service, ActionTypes.Insert, new(), ADealerClaim(), _ => Task.FromResult<decimal?>(12_500));

        // The rate table has moved on; a save that sends no rate does not read it.
        var unchanged = ADealerClaim();
        unchanged.CurrencyRate = null;
        unchanged.LaborRate = 1;

        await SaveAsync(service, ActionTypes.Update, claim, unchanged, NoRateLookup);

        Assert.Equal(12_500, claim.CurrencyRate);
        Assert.Equal(312_500, claim.LaborRate);

        // A rate from someone who may not set it is not taken.
        var fromDealer = ADealerClaim();
        fromDealer.CurrencyRate = 13_000;

        await SaveAsync(service, ActionTypes.Update, claim, fromDealer, NoRateLookup);

        Assert.Equal(12_500, claim.CurrencyRate);

        var newRate = ADealerClaim();
        newRate.CurrencyRate = 13_000;

        await SaveAsync(service, ActionTypes.Update, claim, newRate, NoRateLookup, canSetRate: true);

        Assert.Equal(Local, claim.Currency);
        Assert.Equal(13_000, claim.CurrencyRate);
        Assert.Equal(325_000, claim.LaborRate);
        Assert.Equal(25, claim.LaborRateUSD);
        Assert.Equal(7.69m, claim.SubletTotalAmountUSD);
    }

    [Fact]
    public async Task A_new_rate_moves_a_default_distributor_sublet_amount_but_not_a_typed_one()
    {
        var service = new WarrantyClaimService(currencyResolver: new LocalCurrencyResolver());

        var claim = await SaveAsync(service, ActionTypes.Insert, new(), ADealerClaim(), _ => Task.FromResult<decimal?>(12_500));
        claim.WarrantyClaimSubletLines.Single().ID = 1;

        async Task DistributorSaveAsync(decimal rate, decimal distributorAmount)
        {
            var dto = ADealerClaim();
            dto.CurrencyRate = rate;
            dto.WarrantyClaimLaborLines.ForEach(x => x.DistributorHour = x.Hour);
            dto.WarrantyClaimSubletLines.ForEach(x => { x.ID = 1; x.DistributorAmount = distributorAmount; });
            dto.WarrantyClaimPartLines.ForEach(x => x.DistributorPrice = 20);

            await service.ApplyCurrencyAsync(ActionTypes.Update, claim, dto, companyBranchID: 12, canSetRate: true, NoRateLookup);
            service.WarrantyLinesValidationAndTransformation(ActionTypes.Update, claim, dto, isDistributor: true);
            ModuleMapper.For<Entities.WarrantyClaim, WarrantyClaimListDTO, WarrantyClaimDTO>().MapToEntity(dto, claim);
            service.UpdateCurrencyAmounts(claim);
        }

        await DistributorSaveAsync(13_000, distributorAmount: 8);
        Assert.Equal(7.69m, claim.WarrantyClaimSubletLines.Single().DistributorAmount);

        await DistributorSaveAsync(12_000, distributorAmount: 9);
        Assert.Equal(9, claim.WarrantyClaimSubletLines.Single().DistributorAmount);
    }

    [Fact]
    public async Task A_certified_claim_rejects_a_new_rate()
    {
        var service = new WarrantyClaimService(currencyResolver: new LocalCurrencyResolver());

        var claim = await SaveAsync(service, ActionTypes.Insert, new(), ADealerClaim(), _ => Task.FromResult<decimal?>(12_500));
        claim.ClaimStatus = ShiftSoftware.ADP.Models.Enums.ClaimStatus.Certified;

        var newRate = ADealerClaim();
        newRate.CurrencyRate = 13_000;

        var exception = await Assert.ThrowsAsync<ShiftEntityException>(() =>
            service.ApplyCurrencyAsync(ActionTypes.Update, claim, newRate, companyBranchID: 12, canSetRate: true, NoRateLookup));

        Assert.Equal("CurrencyRateLocked", exception.AdditionalData!["ErrorCode"]);
        Assert.Equal(12_500, claim.CurrencyRate);
    }

    /// <summary>The distributor's figures are in USD, so the margin is taken against the dealer total in USD.</summary>
    [Fact]
    public void The_margin_compares_the_distributor_total_with_the_dealer_total_in_USD()
    {
        var local = new Shared.DTOs.Financial.DistributorFinancialListDTO
        {
            Currency = Local,
            TotalClaimAmount = 1_068_750,
            TotalClaimAmountUSD = 85.5m,
            TotalClaimAmountDistributor = 100,
        };

        Assert.Equal(14.5m, local.DistributorMargin);
        Assert.Equal(85.5m, local.OutstandingDealerPayment);

        // A USD claim reads its own total, whether or not the USD equivalent was ever filled.
        var usd = new Shared.DTOs.Financial.DistributorFinancialListDTO { Currency = "USD", TotalClaimAmount = 80, TotalClaimAmountDistributor = 100 };

        Assert.Equal(20, usd.DistributorMargin);
    }

    [Fact]
    public async Task A_local_currency_claim_cannot_be_created_without_a_rate()
    {
        var service = new WarrantyClaimService(currencyResolver: new LocalCurrencyResolver());

        var exception = await Assert.ThrowsAsync<ShiftEntityException>(() =>
            SaveAsync(service, ActionTypes.Insert, new(), ADealerClaim(), _ => Task.FromResult<decimal?>(null)));

        Assert.Equal("MissingCurrencyRate", exception.AdditionalData!["ErrorCode"]);
    }

    private static FastReport.Report Template(string name)
    {
        var assembly = typeof(Marker).Assembly;
        var report = new FastReport.Report();

        using var stream = assembly.GetManifestResourceStream(assembly.GetManifestResourceNames().Single(x => x.EndsWith($".{name}.frx")))!;
        report.Load(stream);

        return report;
    }

    private static List<string> Formats(FastReport.Report report) =>
        report.AllObjects.OfType<FastReport.TextObjectBase>()
            .Select(x => x.Format).OfType<FastReport.Format.CustomFormat>().Select(x => x.Format).Distinct().ToList();

    private static List<string> Labels(FastReport.Report report) =>
        report.AllObjects.OfType<FastReport.TextObjectBase>().Select(x => x.Text).Where(x => !string.IsNullOrWhiteSpace(x) && !x.Contains('[')).ToList();

    /// <summary>The embedded templates are written for USD. Another currency replaces their dollar formats and names itself on the claim's money labels.</summary>
    [Theory]
    [InlineData("WarrantyCertificate")]
    [InlineData("WarrantyInvoice")]
    public void A_certificate_or_invoice_prints_its_totals_in_the_claims_currency(string template)
    {
        var usd = Template(template);
        var before = Formats(usd);
        usd.UseClaimCurrency("USD");

        Assert.Contains("$#,0.00", before);
        Assert.Equal(before, Formats(usd));

        var local = Template(template);
        local.UseClaimCurrency(Local);

        Assert.DoesNotContain(Formats(local), x => x.StartsWith('$'));
        Assert.Contains("#,0.00 'XTS'", Formats(local));

        // The line amounts carry no format in the template; local figures get separators and stay on one line.
        // A USD printout is unchanged (the formats compared above), and so are its line cells.
        Assert.Null(usd.AllObjects.OfType<FastReport.TextObject>().Single(x => x.Text == "[Table.Parts]").Format as FastReport.Format.CustomFormat);
        var parts = (FastReport.TextObject)local.AllObjects.OfType<FastReport.TextObject>().Single(x => x.Text == "[Table.Parts]");
        Assert.Equal("#,0.##", Assert.IsType<FastReport.Format.CustomFormat>(parts.Format).Format);
        Assert.False(parts.WordWrap);
    }

    [Fact]
    public void A_claim_printout_names_the_currency_on_its_money_labels()
    {
        var usd = Template("WarrantyClaim");
        var before = Labels(usd);
        usd.UseClaimCurrency("USD");

        Assert.Equal(before, Labels(usd));

        var local = Template("WarrantyClaim");
        local.UseClaimCurrency(Local);

        Assert.Contains("Total Claim Amount (XTS)", Labels(local));
        Assert.Contains("Labor Total (Amount) (XTS)", Labels(local));
        Assert.Equal(before.Count, Labels(local).Count);
    }

    /// <summary>
    /// The first half of the data migration: the columns a host's migration adds default to USD at rate 1,
    /// which is what every existing row gets.
    /// </summary>
    [Fact]
    public void The_new_columns_default_an_existing_claim_to_USD_at_rate_1()
    {
        var model = new ModelBuilder(SqlServerConventionSetBuilder.Build());
        model.ConfigureWarrantyClaimsEntities();

        var claim = model.Model.FindEntityType(typeof(Entities.WarrantyClaim))!;

        Assert.Equal("USD", claim.FindProperty(nameof(Entities.WarrantyClaim.Currency))!.GetDefaultValue());
        Assert.Equal(1m, claim.FindProperty(nameof(Entities.WarrantyClaim.CurrencyRate))!.GetDefaultValue());
    }

    /// <summary>
    /// The second half: the script a host's migration runs copies the amounts into the USD equivalents.
    /// </summary>
    [Fact]
    public void Existing_claims_become_USD_claims_whose_USD_equivalents_are_their_amounts()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        void Run(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }

        T Scalar<T>(string sql)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
        }

        Run("""
            CREATE TABLE WarrantyClaim (ID INTEGER PRIMARY KEY, Currency TEXT NOT NULL DEFAULT 'USD', CurrencyRate NUMERIC NOT NULL DEFAULT 1,
                LaborRate NUMERIC, LaborRateUSD NUMERIC NOT NULL DEFAULT 0,
                LaborTotalAmount NUMERIC, LaborTotalAmountUSD NUMERIC, SubletTotalAmount NUMERIC, SubletTotalAmountUSD NUMERIC,
                PartsTotalAmount NUMERIC, PartsTotalAmountUSD NUMERIC, TotalClaimAmount NUMERIC, TotalClaimAmountUSD NUMERIC);
            CREATE TABLE WarrantyClaimPartLine (ID INTEGER PRIMARY KEY, WarrantyClaimID INTEGER, Price NUMERIC, PriceUSD NUMERIC NOT NULL DEFAULT 0);
            CREATE TABLE WarrantyClaimSubletLine (ID INTEGER PRIMARY KEY, WarrantyClaimID INTEGER, Amount NUMERIC, AmountUSD NUMERIC NOT NULL DEFAULT 0);

            INSERT INTO WarrantyClaim (ID, LaborRate, LaborTotalAmount, SubletTotalAmount, PartsTotalAmount, TotalClaimAmount) VALUES (1, 25, 50, 30, 20, 100);
            INSERT INTO WarrantyClaimPartLine (ID, WarrantyClaimID, Price) VALUES (1, 1, 20);
            INSERT INTO WarrantyClaimSubletLine (ID, WarrantyClaimID, Amount) VALUES (1, 1, 30);

            INSERT INTO WarrantyClaim (ID, Currency, CurrencyRate, LaborRate, LaborRateUSD, TotalClaimAmount, TotalClaimAmountUSD) VALUES (2, 'XTS', 12500, 312500, 25, 1068750, 85.5);
            INSERT INTO WarrantyClaimPartLine (ID, WarrantyClaimID, Price, PriceUSD) VALUES (2, 2, 250000, 20);
            """);

        foreach (var sql in WarrantyClaimsMigrationExtensions.MigrateToUSDSql())
            Run(sql);

        Assert.Equal("USD|1|25|50|30|20|100", Scalar<string>(
            "SELECT Currency || '|' || CurrencyRate || '|' || LaborRateUSD || '|' || LaborTotalAmountUSD || '|' || SubletTotalAmountUSD || '|' || PartsTotalAmountUSD || '|' || TotalClaimAmountUSD FROM WarrantyClaim WHERE ID = 1"));
        Assert.Equal(20, Scalar<decimal>("SELECT PriceUSD FROM WarrantyClaimPartLine WHERE ID = 1"));
        Assert.Equal(30, Scalar<decimal>("SELECT AmountUSD FROM WarrantyClaimSubletLine WHERE ID = 1"));

        // A claim in another currency is left alone.
        Assert.Equal("12500|25|85.5", Scalar<string>("SELECT CurrencyRate || '|' || LaborRateUSD || '|' || TotalClaimAmountUSD FROM WarrantyClaim WHERE ID = 2"));
        Assert.Equal(20, Scalar<decimal>("SELECT PriceUSD FROM WarrantyClaimPartLine WHERE ID = 2"));
    }
}
