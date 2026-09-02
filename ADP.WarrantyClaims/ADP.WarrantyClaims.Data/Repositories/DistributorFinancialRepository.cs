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
    public DistributorFinancialRepository(ShiftDbContext db) : base(db, i => i.UseGeneratedMapper(map => map

        // SHENGEN010 - resolved by taking the child write over explicitly rather than letting the
        // generator compose it. See WarrantyClaimLineWriter for why REPLACE (not business-key
        // reconciliation) is the correct answer for this aggregate: UpsertAsync deletes the existing
        // line rows before anything maps, so a fresh set is inserted rather than orphaned.
        .IgnoreEntity(e => e.WarrantyClaimLaborLines)
        .IgnoreEntity(e => e.WarrantyClaimSubletLines)
        .IgnoreEntity(e => e.WarrantyClaimPartLines)
        .AfterEntity((dto, entity, context) => Mapping.WarrantyClaimLineWriter.Write(dto, entity, context))

        // TWO MORE FLATTENINGS THE OLD PROFILE NEVER MENTIONED - AutoMapper derived them by name
        // convention (Certificate.CertificateNo -> CertificateCertificateNo), the generated
        // projection does not, and SHENGEN007 is what caught them.
        //
        // The pre-migration baseline pins the shape for a claim with NO certificate, which is what
        // the parity seed has: "CertificateCertificateNo": "" and "CertificateInvoiceDate": null -
        // an EMPTY STRING, not null, because AutoMapper's conversion to string renders a null source
        // as "". Reproduced literally; a plain `e.Certificate.CertificateNo` would return null there
        // and diff on every row.
        .ForList(d => d.CertificateCertificateNo, e => e.Certificate != null && e.Certificate.CertificateNo != null
            ? e.Certificate.CertificateNo.ToString()
            : "")
        .ForList(d => d.CertificateInvoiceDate, e => e.Certificate != null ? e.Certificate.InvoiceDate : null)

        // The distributor map is the dealer map WITHOUT the five IgnoreList calls - that difference,
        // and nothing else, is what separates the two audiences. See DealerFinancialRepository for
        // why those five matter; if you change one of these three, change it there too.
        //
        // TimeSpan.Zero is transcribed from the profile and is load-bearing: verification.md Rule 2
        // compares business dates literally, so a different offset, kind or precision shifts every
        // timestamp on this list.
        .ForList(d => d.ProcessDate, e => e.ProcessDate.HasValue
            ? new DateTimeOffset(e.ProcessDate.Value, TimeSpan.Zero)
            : (DateTimeOffset?)null)
        .ForList(d => d.DistributorProcessDate, e => e.DistributorProcessDate.HasValue
            ? new DateTimeOffset(e.DistributorProcessDate.Value, TimeSpan.Zero)
            : (DateTimeOffset?)null)

        // Pinned flattening - the member no longer decomposes to a valid navigation + property path
        // by convention after the generic rename. Omit it and the column comes back empty.
        .ForList(d => d.ReferenceWarrantyClaimNumber, e => e.ReferenceWarrantyClaim!.ClaimNumber)))
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
