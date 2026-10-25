using ShiftSoftware.ADP.Lookup.Services.Aggregate;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Lookup.Services.Evaluators;
using ShiftSoftware.ADP.Models.Part;
using ShiftSoftware.ADP.Models.Service;

namespace Lookup.Services.Tests;

public class ServiceHistoryLineCountTests
{
    private static readonly DateTime Date = new(2026, 1, 1);

    private static OrderLaborLineModel Labor(string invoice, int numberOfPartLines) => new()
    {
        VIN = "SYNTHETIC-VEHICLE", CompanyID = 1, BranchID = 10, InvoiceNumber = invoice, OrderDocumentNumber = "JOB-" + invoice,
        InvoiceDate = Date, NumberOfPartLines = numberOfPartLines,
    };

    private static OrderPartLineModel Part(string invoice, int numberOfLaborLines) => new()
    {
        VIN = "SYNTHETIC-VEHICLE", CompanyID = 1, BranchID = 10, InvoiceNumber = invoice, OrderDocumentNumber = "JOB-" + invoice,
        InvoiceDate = Date, NumberOfLaborLines = numberOfLaborLines,
    };

    private static List<string> Invoices(CompanyDataAggregateModel aggregate) =>
        VehicleServiceHistoryEvaluator.GetInvoices(aggregate, ConsistencyLevels.Strong)
            .Select(x => x.InvoiceNumber).OrderBy(x => x, StringComparer.Ordinal).ToList();

    [Fact]
    public void Strong_keeps_invoices_whose_expected_counts_are_unknown()
    {
        var aggregate = new CompanyDataAggregateModel
        {
            LaborLines = [Labor("UNKNOWN", -1), Labor("UNKNOWN", -1), Labor("LABOR-ONLY", -1)],
            PartLines = [Part("UNKNOWN", -1), Part("PART-ONLY", -1)],
        };

        Assert.Equal(["LABOR-ONLY", "PART-ONLY", "UNKNOWN"], Invoices(aggregate));
    }

    [Fact]
    public void Strong_still_checks_the_side_whose_count_is_known()
    {
        var aggregate = new CompanyDataAggregateModel
        {
            // Labor knows 2 part lines but only 1 arrived; the part's unknown labor count does not rescue it.
            LaborLines = [Labor("MISSING-PART", 2), Labor("COMPLETE", 1)],
            PartLines = [Part("MISSING-PART", -1), Part("COMPLETE", -1)],
        };

        Assert.Equal(["COMPLETE"], Invoices(aggregate));
    }
}
