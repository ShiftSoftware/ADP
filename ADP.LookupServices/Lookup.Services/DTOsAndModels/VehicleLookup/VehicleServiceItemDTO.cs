using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model;
using System;
using System.Collections.Generic;
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

    /// <summary>The method used to claim this item (e.g., QR Code scan, Invoice + Job Number).</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemClaimingMethod ClaimingMethodEnum { get; set; }
    /// <summary>The vehicle inspection ID associated with this claim, if applicable.</summary>
    public string VehicleInspectionID { get; set; }
    /// <summary>The vehicle inspection type ID required for claiming, if applicable.</summary>
    public string VehicleInspectionTypeID { get; set; }
    /// <summary>The HMAC signature used to securely validate claim requests.</summary>
    public string Signature { get; set; }
    /// <summary>The UTC expiry time of the signature.</summary>
    public DateTime SignatureExpiry { get; set; }

    public string GenerateSignature(string vin, string secretKey)
    {
        string stringToSign = string.Join(
            ",",
            vin.ToUpper(),
            (int) this.TypeEnum,
            this.ActivatedAt.ToString(ActivationAndExpiryDateFormat),
            this.ExpiresAt?.ToString(ActivationAndExpiryDateFormat) ?? string.Empty,
            (int) this.StatusEnum,
            this.ModelCostID,
            this.ServiceItemID,
            this.PaidServiceInvoiceLineID,
            this.ClaimingMethodEnum,
            this.VehicleInspectionID,
            this.Claimable,
            this.SignatureExpiry.Ticks,
            this.Cost,
            this.CampaignID
        );

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        var messageBytes = Encoding.UTF8.GetBytes(stringToSign);

        using var hmac = new HMACSHA256(keyBytes);

        var hash = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hash);
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
