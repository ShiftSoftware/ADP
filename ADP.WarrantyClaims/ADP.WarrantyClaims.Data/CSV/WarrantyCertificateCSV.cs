using FileHelpers;
using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Data.Entities;
using System.Text;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.CSV;

/// <summary>
/// One row of the certificate CSV export. There is one row for each warranty claim on a certificate.
/// The certificate and invoice fields repeat on every row, so a finance system can import the file
/// without a second lookup. The invoice date is empty when the certificate is not invoiced yet.
/// </summary>
[DelimitedRecord(",")]
public class WarrantyCertificateCSV
{
    [FieldCaption("CertificateNo")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public long? CertificateNo { get; set; }

    [FieldCaption("DistributorCertificateNo")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? DistributorCertificateNo { get; set; }

    [FieldCaption("CertificateDate")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
    public DateTime? CertificateDate { get; set; }

    [FieldCaption("InvoiceDate")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }

    [FieldCaption("PeriodStartDate")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
    public DateTime? PeriodStartDate { get; set; }

    [FieldCaption("PeriodEndDate")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
    public DateTime? PeriodEndDate { get; set; }

    [FieldCaption("DealerCode")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? DealerCode { get; set; }

    [FieldCaption("DealerName")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? DealerName { get; set; }

    [FieldCaption("ClaimNumber")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? ClaimNumber { get; set; }

    [FieldCaption("DealerClaimNo")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? DealerClaimNo { get; set; }

    [FieldCaption("RepairOrderNo")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? RepairOrderNo { get; set; }

    [FieldCaption("RepairDate")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    [FieldConverter(ConverterKind.Date, "yyyy-MM-dd")]
    public DateTime? RepairDate { get; set; }

    [FieldCaption("VIN")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? VIN { get; set; }

    [FieldCaption("WarrantyType")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string? WarrantyType { get; set; }

    [FieldCaption("LaborAmount")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public decimal? LaborAmount { get; set; }

    [FieldCaption("SubletAmount")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public decimal? SubletAmount { get; set; }

    [FieldCaption("PartsAmount")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public decimal? PartsAmount { get; set; }

    [FieldCaption("TotalAmount")]
    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Builds the rows for one certificate. The amounts are the same claim amounts the certificate and
    /// invoice printouts show. The dealer name comes from the same lookup the printouts use.
    /// </summary>
    public static IEnumerable<WarrantyCertificateCSV> FromCertificate(Certificate certificate, IEnumerable<WarrantyClaim> claims, CompanyPrintInfo? dealer)
    {
        return claims.Select(claim => new WarrantyCertificateCSV
        {
            CertificateNo = certificate.CertificateNo,
            DistributorCertificateNo = certificate.DisplayDistributorCertificateNo,
            CertificateDate = certificate.CertificateDate,
            InvoiceDate = certificate.InvoiceDate,
            PeriodStartDate = certificate.PeriodStartDate,
            PeriodEndDate = certificate.PeriodEndDate,
            // The dealer code saved on the claim when it was created. The claim list and the manufacturer
            // CSV (DLR) show this same value.
            DealerCode = claim.DealerCode,
            DealerName = dealer?.Name,
            ClaimNumber = claim.ClaimNumber,
            DealerClaimNo = claim.DealerClaimNo,
            RepairOrderNo = claim.RepairOrderNo,
            RepairDate = claim.RepairDate,
            VIN = claim.VIN,
            WarrantyType = claim.WarrantyType,
            LaborAmount = claim.LaborTotalAmount,
            SubletAmount = claim.SubletTotalAmount,
            PartsAmount = claim.PartsTotalAmount,
            TotalAmount = claim.TotalClaimAmount,
        });
    }

    /// <summary>
    /// Writes the rows with a header line, as UTF-8 without a byte order mark. This is the same encoding
    /// the manufacturer CSV export uses.
    /// </summary>
    public static async Task<MemoryStream> WriteAsync(IEnumerable<WarrantyCertificateCSV> rows)
    {
        var memoryStream = new MemoryStream();

        var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        using (var streamWriter = new StreamWriter(memoryStream, utf8WithoutBom, leaveOpen: true))
        {
            var engine = new FileHelperEngine<WarrantyCertificateCSV>(utf8WithoutBom);

            engine.HeaderText = engine.GetFileHeader();

            engine.WriteStream(streamWriter, rows);

            await streamWriter.FlushAsync();
        }

        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }
}
