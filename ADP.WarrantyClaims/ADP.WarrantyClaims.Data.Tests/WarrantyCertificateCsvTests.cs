using ShiftSoftware.ADP.Cases.Data.Entities;
using ShiftSoftware.ADP.Cases.Shared.Printing;
using ShiftSoftware.ADP.WarrantyClaims.Data.CSV;
using System.Globalization;
using Xunit;
using Entities = ShiftSoftware.ADP.WarrantyClaims.Data.Entities;

namespace ShiftSoftware.ADP.WarrantyClaims.Data.Tests;

/// <summary>
/// The certificate CSV export writes one row for each claim, with the certificate and invoice fields on every row.
/// </summary>
public class WarrantyCertificateCsvTests
{
    private static Certificate Certificate(DateTime? invoiceDate) => new()
    {
        CertificateNo = 13,
        DisplayDistributorCertificateNo = "DC-26-0062",
        CertificateDate = new DateTime(2026, 10, 15),
        InvoiceDate = invoiceDate,
        PeriodStartDate = new DateTime(2026, 9, 16),
        PeriodEndDate = new DateTime(2026, 9, 30),
    };

    private static Entities.WarrantyClaim Claim(string number, decimal total) => new()
    {
        ClaimNumber = number,
        DealerCode = "BR1",
        DealerClaimNo = "D-" + number,
        RepairOrderNo = "RO-" + number,
        RepairDate = new DateTime(2026, 9, 20),
        VIN = "AAABBBBB000000001",
        WarrantyType = "A",
        LaborTotalAmount = 10.5m,
        SubletTotalAmount = 0m,
        PartsTotalAmount = total - 10.5m,
        TotalClaimAmount = total,
    };

    // The company's short code (DL1) differs from the claim's dealer code (BR1), so the tests show which one the CSV uses.
    private static readonly CompanyPrintInfo Dealer = new("Dealer One, LLC", "", "", "", null, "DL1");

    private static async Task<string[]> WriteLines(IEnumerable<WarrantyCertificateCSV> rows)
    {
        using var stream = await WarrantyCertificateCSV.WriteAsync(rows);

        var bytes = stream.ToArray();

        // UTF-8 without a byte order mark, the same as the manufacturer CSV.
        Assert.False(bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF);

        return System.Text.Encoding.UTF8.GetString(bytes).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    [Fact]
    public async Task Writes_a_header_and_one_row_for_each_claim()
    {
        var rows = WarrantyCertificateCSV.FromCertificate(
            Certificate(new DateTime(2026, 10, 16)),
            new[] { Claim("0000001", 100m), Claim("0000002", 250.75m) },
            Dealer);

        var lines = await WriteLines(rows);

        Assert.Equal(3, lines.Length);
        Assert.Equal("CertificateNo,DistributorCertificateNo,CertificateDate,InvoiceDate,PeriodStartDate,PeriodEndDate,DealerCode,DealerName,ClaimNumber,DealerClaimNo,RepairOrderNo,RepairDate,VIN,WarrantyType,LaborAmount,SubletAmount,PartsAmount,TotalAmount", lines[0]);

        // DealerCode is the code saved on the claim. The dealer name has a comma, so it is quoted.
        Assert.Equal("13,DC-26-0062,2026-10-15,2026-10-16,2026-09-16,2026-09-30,BR1,\"Dealer One, LLC\",0000001,D-0000001,RO-0000001,2026-09-20,AAABBBBB000000001,A,10.5,0,89.5,100", lines[1]);
        Assert.EndsWith(",240.25,250.75", lines[2]);
    }

    [Fact]
    public async Task Leaves_the_invoice_date_empty_when_the_certificate_is_not_invoiced()
    {
        var rows = WarrantyCertificateCSV.FromCertificate(Certificate(invoiceDate: null), new[] { Claim("0000001", 100m) }, Dealer);

        var lines = await WriteLines(rows);

        Assert.StartsWith("13,DC-26-0062,2026-10-15,,2026-09-16,", lines[1]);
    }

    [Fact]
    public async Task Uses_a_dot_for_decimals_whatever_the_current_culture_is()
    {
        var originalCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ru-RU");

            var rows = WarrantyCertificateCSV.FromCertificate(Certificate(null), new[] { Claim("0000001", 250.75m) }, Dealer);

            var lines = await WriteLines(rows);

            Assert.EndsWith(",10.5,0,240.25,250.75", lines[1]);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
