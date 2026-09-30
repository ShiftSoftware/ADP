using ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;
using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace LookupServices.BDD;

/// <summary>
/// The service item signature must not depend on the culture of the thread. A lookup host signs
/// each item, and later the claim endpoint checks the signature. Each runs under its own thread
/// culture, and a host that applies request localization takes that culture from the request, so
/// the two can differ. Before this was fixed, the signed text used the current culture: a cost of
/// 1500.50 was "1500,50" under ru-RU and "1500٫50" under ar-SA, and a date in 2026 had the year
/// 2569 under th-TH and 1448 under ar-SA. A claim then failed whenever the two cultures wrote these
/// values differently.
/// </summary>
public class ServiceItemSignatureCultureTests
{
    private const string SigningKey = "test-signing-key";

    private const string PaidItemVin = "JTMHV05J604123456";

    // Lower case, so that the upper-casing of the VIN is part of the signed text.
    private const string FreeItemVin = "jtmhv05j604123456";

    // The text and the signature that the code before this change (release 1.16.10) produced for
    // these items under en-US. The invariant culture gave the same values. Hosts that still run
    // the earlier code issue these signatures, so the current code must produce the same bytes.
    private const string PaidItemText =
        "JTMHV05J604123456,1,2026-09-30,2027-03-31,2,1234567,4521,INV-77/3,ClaimByEnteringInvoiceAndJobNumber,,True,639263684961234567,1500.50,98765,vin-entry-5";

    private const string PaidItemSignature = "EqyCXIwoCqCqqfcCse697skYH3bS8AG/i2IjaTbWS04=";

    private const string FreeItemText =
        "JTMHV05J604123456,0,2028-02-29,,0,,17,,ClaimByScanningQRCode,insp-3,False,639028224000000000,,5,";

    private const string FreeItemSignature = "1qJoGA00EltiE+g/z6sQWM1TYfAhdaLaqONf7UATiTg=";

    /// <summary>
    /// en-US and the invariant culture, which the earlier code already matched. Then cultures that
    /// changed the earlier output: a comma decimal separator (ru-RU), an Arabic decimal separator
    /// with the Umm al-Qura calendar (ar-SA), the Thai Buddhist calendar (th-TH), the Persian
    /// calendar (fa-IR), an Arabic decimal separator with the Gregorian calendar (ar-IQ, ckb-IQ), a
    /// special upper case for "i" (tr-TR), and a minus sign that is not a hyphen (sv-SE).
    /// </summary>
    public static TheoryData<string> AllCultures => new()
    {
        "en-US", "invariant", "ru-RU", "ar-SA", "th-TH", "fa-IR", "ar-IQ", "ckb-IQ", "tr-TR", "sv-SE",
    };

    public static TheoryData<string> OtherCultures => new() { "ru-RU", "ar-SA", "th-TH" };

    /// <summary>
    /// The culture that signs, the culture while the lookup host writes its response, and the
    /// culture of the claim request. The writing culture matters because the dates travel as JSON
    /// text.
    /// </summary>
    public static TheoryData<string, string, string> ClaimRoundTrips => new()
    {
        { "ru-RU", "ru-RU", "en-US" },
        { "ar-SA", "ar-SA", "en-US" },
        { "th-TH", "th-TH", "en-US" },
        { "th-TH", "en-US", "en-US" },
        { "en-US", "en-US", "ru-RU" },
        { "en-US", "en-US", "ar-SA" },
        { "en-US", "en-US", "th-TH" },
    };

    [Fact]
    public void TheTestCulturesStillChangeTheOutputOfTheCurrentCulture()
    {
        // If the culture data of the platform changes, the tests below could pass without testing
        // anything. This test fails first in that case.
        Assert.NotEqual("1500.50", InCulture("ru-RU", () => 1500.50m.ToString()));
        Assert.NotEqual("1500.50", InCulture("ar-SA", () => 1500.50m.ToString()));
        Assert.NotEqual("2026-09-30", InCulture("ar-SA", () => new DateTime(2026, 9, 30).ToString("yyyy-MM-dd")));
        Assert.NotEqual("2026-09-30", InCulture("th-TH", () => new DateTime(2026, 9, 30).ToString("yyyy-MM-dd")));
    }

    [Theory]
    [MemberData(nameof(AllCultures))]
    public void TheSignedTextIsTheSameInEveryCulture(string culture)
    {
        Assert.Equal(PaidItemText, InCulture(culture, () => PaidItem().BuildStringToSign(PaidItemVin)));
        Assert.Equal(FreeItemText, InCulture(culture, () => FreeItem().BuildStringToSign(FreeItemVin)));
    }

    [Theory]
    [MemberData(nameof(AllCultures))]
    public void TheVinIsUpperCasedTheSameWayInEveryCulture(string culture)
    {
        // A real VIN has no "i", but under tr-TR the current culture turned "i" into "İ" (U+0130).
        var text = InCulture(culture, () => FreeItem().BuildStringToSign("abcdefghijklmnopqrstuvwxyz0123456789"));

        Assert.StartsWith("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789,", text, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(AllCultures))]
    public void TheSignatureIsTheOneTheEarlierCodeIssuedUnderEnUs(string culture)
    {
        Assert.Equal(PaidItemSignature, InCulture(culture, () => PaidItem().GenerateSignature(PaidItemVin, SigningKey)));
        Assert.Equal(FreeItemSignature, InCulture(culture, () => FreeItem().GenerateSignature(FreeItemVin, SigningKey)));
    }

    [Theory]
    [MemberData(nameof(OtherCultures))]
    public void ASignatureMadeUnderAnotherCultureVerifiesUnderEnUs(string signingCulture)
    {
        var item = SignedPaidItem(signingCulture);

        Assert.True(InCulture("en-US", () => item.ValidateSignature(PaidItemVin, SigningKey)));
    }

    [Theory]
    [MemberData(nameof(OtherCultures))]
    public void ASignatureMadeUnderEnUsVerifiesUnderAnotherCulture(string verifyingCulture)
    {
        var item = SignedPaidItem("en-US");

        Assert.True(InCulture(verifyingCulture, () => item.ValidateSignature(PaidItemVin, SigningKey)));
    }

    [Theory]
    [MemberData(nameof(OtherCultures))]
    public void AChangedCostDoesNotVerifyInAnyCulture(string verifyingCulture)
    {
        var item = SignedPaidItem("en-US");
        item.Cost = 1500.51m;

        Assert.False(InCulture(verifyingCulture, () => item.ValidateSignature(PaidItemVin, SigningKey)));
    }

    [Theory]
    [MemberData(nameof(ClaimRoundTrips))]
    public void AClaimVerifiesWhenTheLookupAndTheClaimRunUnderDifferentCultures(
        string signingCulture, string writingCulture, string verifyingCulture)
    {
        var item = SignedPaidItem(signingCulture);

        // The lookup host writes the item as JSON. The web component sends it back unchanged in the
        // "serviceItem" field of the claim payload, next to the VIN of the lookup response.
        var itemJson = InCulture(writingCulture, () => JsonSerializer.SerializeToNode(item, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var payload = new JsonObject { ["vin"] = PaidItemVin, ["serviceItem"] = itemJson }.ToJsonString();

        // The claim endpoint reads the payload in the same way (ItemClaimController.Claim).
        var valid = InCulture(verifyingCulture, () =>
        {
            var claim = JsonSerializer.Deserialize<ItemClaimDTO>(payload, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

            return claim.ServiceItem!.ValidateSignature(claim.VIN!, SigningKey);
        });

        Assert.True(valid);
    }

    [Theory]
    [MemberData(nameof(AllCultures))]
    public void TheDateConverterWritesAndReadsGregorianDatesInEveryCulture(string culture)
    {
        var json = InCulture(culture, () => JsonSerializer.Serialize(PaidItem()));

        using (var document = JsonDocument.Parse(json))
        {
            Assert.Equal("2026-09-30", document.RootElement.GetProperty(nameof(VehicleServiceItemDTO.ActivatedAt)).GetString());
            Assert.Equal("2027-03-31", document.RootElement.GetProperty(nameof(VehicleServiceItemDTO.ExpiresAt)).GetString());
        }

        var read = InCulture(culture, () => JsonSerializer.Deserialize<VehicleServiceItemDTO>("""{"ActivatedAt":"2026-09-30","ExpiresAt":"2027-03-31"}"""))!;

        Assert.Equal(new DateTime(2026, 9, 30), read.ActivatedAt);
        Assert.Equal(new DateTime(2027, 3, 31), read.ExpiresAt);
    }

    private static VehicleServiceItemDTO SignedPaidItem(string signingCulture)
    {
        var item = PaidItem();

        // ValidateSignature refuses an expired signature, so this one must still be valid.
        item.SignatureExpiry = DateTime.UtcNow.AddHours(3);
        item.Signature = InCulture(signingCulture, () => item.GenerateSignature(PaidItemVin, SigningKey));

        return item;
    }

    /// <summary>A paid item with every value that the current culture used to change.</summary>
    private static VehicleServiceItemDTO PaidItem() => new()
    {
        TypeEnum = VehcileServiceItemTypes.Paid,
        ActivatedAt = new DateTime(2026, 9, 30),
        ExpiresAt = new DateTime(2027, 3, 31),
        StatusEnum = VehcileServiceItemStatuses.Pending,
        ModelCostID = 1234567,
        ServiceItemID = "4521",
        PaidServiceInvoiceLineID = "INV-77/3",
        ClaimingMethodEnum = ClaimableItemClaimingMethod.ClaimByEnteringInvoiceAndJobNumber,
        VehicleInspectionID = null,
        Claimable = true,
        SignatureExpiry = new DateTime(2026, 9, 30, 12, 34, 56, DateTimeKind.Utc).AddTicks(1234567),
        Cost = 1500.50m,
        CampaignID = 98765,
        CampaignVinEntryID = "vin-entry-5",
    };

    /// <summary>A free item with no expiry, no cost and no model cost, on 29 February.</summary>
    private static VehicleServiceItemDTO FreeItem() => new()
    {
        TypeEnum = VehcileServiceItemTypes.Free,
        ActivatedAt = new DateTime(2028, 2, 29),
        ExpiresAt = null,
        StatusEnum = VehcileServiceItemStatuses.Processed,
        ModelCostID = null,
        ServiceItemID = "17",
        PaidServiceInvoiceLineID = null,
        ClaimingMethodEnum = ClaimableItemClaimingMethod.ClaimByScanningQRCode,
        VehicleInspectionID = "insp-3",
        Claimable = false,
        SignatureExpiry = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Cost = null,
        CampaignID = 5,
        CampaignVinEntryID = null,
    };

    /// <summary>
    /// Runs <paramref name="action"/> with the current culture and UI culture set to
    /// <paramref name="culture"/> ("invariant" is the invariant culture), and puts the previous
    /// cultures back afterwards.
    /// </summary>
    private static T InCulture<T>(string culture, Func<T> action)
    {
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUICulture = CultureInfo.CurrentUICulture;
        var target = culture == "invariant" ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture);

        CultureInfo.CurrentCulture = target;
        CultureInfo.CurrentUICulture = target;

        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUICulture;
        }
    }
}
