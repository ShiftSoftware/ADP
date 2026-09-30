using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// Represents a service item available for a vehicle — includes its type (free/paid), status (pending/processed/expired),
/// validity period, cost, claimability, and an HMAC signature for secure claiming.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleServiceItemDTO
{
    private const string ActivationAndExpiryDateFormat = "yyyy-MM-dd";

    /// <summary>The <see cref="VehicleServiceItemGroup">group</see> this service item belongs to (for UI tab grouping).</summary>
    public VehicleServiceItemGroup Group { get; set; }
    /// <summary>Whether to show a document uploader when claiming this item.</summary>
    public bool ShowDocumentUploader { get; set; }
    /// <summary>A list of <see cref="VehicleItemWarning">warnings</see> to display before claiming this item.</summary>
    public List<VehicleItemWarning>? Warnings { get; set; }
    /// <summary>URL for printing the service item certificate.</summary>
    public string? PrintUrl { get; set; }
    /// <summary>Whether the document uploader is required (not just shown) when claiming.</summary>
    public bool DocumentUploaderIsRequired { get; set; }
    /// <summary>The localized name of the service item.</summary>
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; }
    /// <summary>The localized description of the service item.</summary>
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Description { get; set; }
    /// <summary>The localized printout title.</summary>
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Title { get; set; }
    /// <summary>The localized image URL for the service item.</summary>
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Image { get; set; } = default!;
    /// <summary>The human-readable type label (e.g., "Free", "Paid").</summary>
    public string Type { get; set; }
    /// <summary>The service item type as an enum value.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehcileServiceItemTypes TypeEnum { get; set; } = default!;
    /// <summary>The date this service item was activated for this vehicle.</summary>
    [JsonCustomDateTime(ActivationAndExpiryDateFormat)]
    public DateTime ActivatedAt { get; set; }
    /// <summary>The date this service item expires. Null if no expiration.</summary>
    [JsonCustomDateTime(ActivationAndExpiryDateFormat)]
    public DateTime? ExpiresAt { get; set; }
    /// <summary>The human-readable status label (e.g., "Pending", "Processed", "Expired").</summary>
    public string Status { get; set; }
    /// <summary>The service item status as an enum value.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehcileServiceItemStatuses StatusEnum { get; set; } = default!;
    /// <summary>The ID of the campaign this item belongs to.</summary>
    public long? CampaignID { get; set; }
    /// <summary>The unique reference of the parent campaign.</summary>
    public string CampaignUniqueReference { get; set; }
    /// <summary>The package code grouping related service items together.</summary>
    public string PackageCode { get; set; }
    /// <summary>The cost of this service item for this vehicle.</summary>
    public decimal? Cost { get; set; }
    /// <summary>The date this item was claimed, if already claimed.</summary>
    public DateTimeOffset? ClaimDate { get; set; }
    /// <summary>The model-specific cost ID used when costing type is 'Per Model'.</summary>
    public long? ModelCostID { get; set; }
    /// <summary>The unique identifier of the service item definition.</summary>
    public string ServiceItemID { get; set; }
    /// <summary>The paid service invoice line ID, if this is a paid service item.</summary>
    public string? PaidServiceInvoiceLineID { get; set; }
    /// <summary>The localized company name that performed or will perform the service.</summary>
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string CompanyName { get; set; }
    /// <summary>The invoice number associated with the claim.</summary>
    public string InvoiceNumber { get; set; }
    /// <summary>The job number associated with the claim.</summary>
    public string JobNumber { get; set; }
    /// <summary>The maximum mileage for sequential validity calculations.</summary>
    public long? MaximumMileage { get; set; }
    /// <summary>Whether this service item can currently be claimed.</summary>
    public bool Claimable { get; set; }
    /// <summary>
    /// Why this item is shown without being claimable, or null when it is an ordinary offered item.
    /// A locked or missed item always carries <see cref="Claimable"/> false, and shows no expiry —
    /// a reward whose validity starts when it unlocks has no honest date to show before then.
    /// </summary>
    public VehicleServiceItemLockDTO? Lock { get; set; }

    [DocIgnore]
    [JsonIgnore]
    [TypeScriptIgnore]
    public int? ActiveFor { get; set; }

    [DocIgnore]
    [JsonIgnore]
    [TypeScriptIgnore]
    public DurationType? ActiveForDurationType { get; set; } = default!;

    [DocIgnore]
    [JsonIgnore]
    [TypeScriptIgnore]
    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }

    [DocIgnore]
    [JsonIgnore]
    [TypeScriptIgnore]
    public ClaimableItemCampaignActivationTypes CampaignActivationType { get; set; }

    [DocIgnore]
    [JsonIgnore]
    [TypeScriptIgnore]
    public ClaimableItemValidityMode ValidityModeEnum { get; set; }

    /// <summary>
    /// When this item's eligibility prerequisites were completed, or null when it has none or has
    /// not met them. Carries the anchor its validity window is measured from — see the item
    /// evaluator's unlock anchoring — and is not part of the payload.
    /// </summary>
    [DocIgnore]
    [JsonIgnore]
    [TypeScriptIgnore]
    public DateTime? UnlockedOn { get; set; }

    /// <summary>The method used to claim this item (e.g., QR Code scan, Invoice + Job Number).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemClaimingMethod ClaimingMethodEnum { get; set; }
    /// <summary>The vehicle inspection ID associated with this claim, if applicable.</summary>
    public string VehicleInspectionID { get; set; }
    /// <summary>The vehicle inspection type ID required for claiming, if applicable.</summary>
    public string VehicleInspectionTypeID { get; set; }
    /// <summary>The campaign VIN entry ID that activated this item, if applicable (ManualVinEntry trigger).</summary>
    public string CampaignVinEntryID { get; set; }
    /// <summary>The HMAC signature used to securely validate claim requests.</summary>
    public string Signature { get; set; }
    /// <summary>The UTC expiry time of the signature.</summary>
    public DateTime SignatureExpiry { get; set; }

    public string GenerateSignature(string vin, string secretKey)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        var messageBytes = Encoding.UTF8.GetBytes(this.BuildStringToSign(vin));

        using var hmac = new HMACSHA256(keyBytes);

        var hash = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// The text that <see cref="GenerateSignature"/> signs. The lookup host signs it and the claim
    /// endpoint checks it, and the two can run under different cultures. So every value is written
    /// with the invariant culture. With the current culture, a cost of 1500.50 is written as
    /// "1500,50" in many cultures, and a date gets a year such as 2569 in a culture whose calendar
    /// is not Gregorian.
    /// For a VIN in ASCII characters, the text is byte for byte the text that the earlier code
    /// produced under en-US or the invariant culture, so signatures issued before this change still
    /// verify.
    /// </summary>
    internal string BuildStringToSign(string vin)
    {
        var invariant = CultureInfo.InvariantCulture;

        return string.Join(
            ",",
            vin.ToUpperInvariant(),
            ((int) this.TypeEnum).ToString(invariant),
            this.ActivatedAt.ToString(ActivationAndExpiryDateFormat, invariant),
            this.ExpiresAt?.ToString(ActivationAndExpiryDateFormat, invariant) ?? string.Empty,
            ((int) this.StatusEnum).ToString(invariant),
            this.ModelCostID?.ToString(invariant),
            this.ServiceItemID,
            this.PaidServiceInvoiceLineID,
            // The enum name, as before. The name of a defined value is the same in every culture.
            this.ClaimingMethodEnum.ToString(),
            this.VehicleInspectionID,
            // "True" or "False" in every culture.
            this.Claimable.ToString(),
            this.SignatureExpiry.Ticks.ToString(invariant),
            this.Cost?.ToString(invariant),
            this.CampaignID?.ToString(invariant),
            this.CampaignVinEntryID
        );
    }

    public bool ValidateSignature(string vin, string secretKey)
    {
        if (DateTime.UtcNow > this.SignatureExpiry)
            return false;

        var generatedSignature = GenerateSignature(vin, secretKey);

        return generatedSignature == this.Signature;
    }

    public VehicleServiceItemDTO Clone()
    {
        return (VehicleServiceItemDTO) this.MemberwiseClone();
    }
}
