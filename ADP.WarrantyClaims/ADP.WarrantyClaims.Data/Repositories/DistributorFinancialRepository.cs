using ShiftEntity.Print;
using ShiftSoftware.ADP.Cases.Data.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using ShiftSoftware.ADP.WarrantyClaims.Data.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.Financial;
using ShiftSoftware.ADP.WarrantyClaims.Shared.DTOs.WarrantyClaim;
using ShiftSoftware.ShiftEntity.EFCore;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Repositories;

/// <summary>
/// The distributor-side financial analytics repository over the warranty claim. Moved from the
/// original host application (Phase 3 Slice 3.5, D23).
/// </summary>
/// <remarks>
/// Printing (<see cref="PrintAsync"/>) renders the embedded FinancialReport template (overridable via
/// <c>WarrantyClaimsApiOptions.ReportOverrides.FinancialReportFrxPath</c>) with the consumer's
/// <see cref="Cases.Shared.Printing.ICompanyInfoProvider"/> (required) and
/// <see cref="Cases.Shared.Printing.IPrintoutDateFormatter"/> (module default).
/// </remarks>
public class DistributorFinancialRepository : ShiftRepository<ShiftDbContext, WarrantyClaim, DistributorFinancialListDTO, WarrantyClaimDTO>
{
    public DistributorFinancialRepository(ShiftDbContext db) : base(db)
    {
    }

    public async Task<Stream> PrintAsync(List<DistributorFinancialListDTO> claims)
    {
        var companyInfoProvider = PrintingServices.GetRequiredCompanyInfoProvider(this.db);
        var printOutDateFormatter = PrintingServices.GetPrintoutDateFormatter(this.db);
        var reportOverrides = PrintingServices.GetService<WarrantyClaimsReportOverrides>(this.db);

        //Workaround to prevent fastreport from throwing an error during data binding
        var fakeClaims = false;

        if (claims.Count == 0)
        {
            fakeClaims = true;
            claims.Add(new DistributorFinancialListDTO { InvoiceNo = "" });
        }

        var distributorInfo = await companyInfoProvider.GetDistributorAsync("en");

        return await new FastReportBuilder()
            .AddFastReportFile(WarrantyClaimsReports.FinancialReport(reportOverrides))
            .AddDataList("Invoices", "DataInvoices", claims.GroupBy(x => x.InvoiceNo).Select(x => new
            {
                InvoiceNo = x.Key,
            }).ToList<object>())
            .AddDataList("Claims", "DataClaims", claims.Select(x => new
            {
                x.InvoiceNo,
                ClaimNumber = x.ReferenceWarrantyClaimNumber is not null ? $"{x.ReferenceWarrantyClaimNumber}-{x.ClaimNumber}" : x.ClaimNumber,
                x.CertificateCertificateNo,
                x.DealerCode,
                CertificateInvoiceDate = printOutDateFormatter.GetFormattedDate(x.CertificateInvoiceDate),
                Franchise = x.Franchise == null ? "" : x.Franchise?.Substring(0, 1),
                x.VIN,
                LaborTotalAmountDistributor = x.LaborTotalAmountDistributor.ToCurrencyFormat(),
                SubletTotalAmountDistributor = x.SubletTotalAmountDistributor.ToCurrencyFormat(),
                PartsTotalAmountDistributor = x.PartsTotalAmountDistributor.ToCurrencyFormat(),
                TotalClaimAmountDistributor = x.TotalClaimAmountDistributor.ToCurrencyFormat(),
                TotalClaimAmount = x.TotalClaimAmount.ToCurrencyFormat(),
                DistributorMargin = x.DistributorMargin.ToCurrencyFormat(),
                ManufacturerSettledTotalClaimAmount = (x.ManufacturerSettledTotalClaimAmount ?? 0m).ToCurrencyFormat(),
                GainsAndLosses = x.RealizedGainsAndLosses.ToCurrencyFormat(),
            }).ToList<object>(), 3, "[Invoices.InvoiceNo] == [Claims.InvoiceNo]")
            .AddDataList("InvoicesSummary", "DataInvoicesSummary", claims.GroupBy(x => x.InvoiceNo).Select(x => new
            {
                InvoiceNo = x.Key,
                Count = x.Count(),
                Labor = x.Sum(x => x.LaborTotalAmountDistributor).ToCurrencyFormat(),
                Sublet = x.Sum(x => x.SubletTotalAmountDistributor).ToCurrencyFormat(),
                Parts = x.Sum(x => x.PartsTotalAmountDistributor).ToCurrencyFormat(),
                Total = x.Sum(x => x.TotalClaimAmountDistributor).ToCurrencyFormat(),
                DealerTotal = x.Sum(x => x.TotalClaimAmount).ToCurrencyFormat(),
                ManufacturerSettled = x.Sum(x => x.ManufacturerSettledTotalClaimAmount ?? 0m).ToCurrencyFormat(),
                GainsAndLosses = x.Sum(x => x.RealizedGainsAndLosses).ToCurrencyFormat(),
                DistributorMargin = x.Sum(x => x.DistributorMargin).ToCurrencyFormat()
            }).ToList<object>(), 3, "[Invoices.InvoiceNo] == [InvoicesSummary.InvoiceNo]")
            .GetPDFStream(report =>
            {
                (report.FindObject("TextTitle") as FastReport.TextObject)!.Text =
                distributorInfo.Name;
                (report.FindObject("PictureLogo") as FastReport.PictureObject)!.ImageLocation =
                distributorInfo.Logo;

                if (fakeClaims)
                {
                    (report.FindObject("CellManufacturerPayment") as FastReport.Table.TableCell)!.Text = "";

                    (report.FindObject("TextInvoiceSummaryHeader") as FastReport.TextObject)!.Text = "Summary";
                    (report.FindObject("CellClaimTotal") as FastReport.Table.TableCell)!.Text = "0";
                }
            });
    }
}
