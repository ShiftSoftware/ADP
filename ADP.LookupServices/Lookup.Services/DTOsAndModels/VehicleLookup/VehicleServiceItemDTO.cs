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

[TypeScriptModel]
public class VehicleServiceItemDTO
{
    private const string ActivationAndExpiryDateFormat = "yyyy-MM-dd";

    public VehicleServiceItemGroup Group { get; set; }

    public bool ShowDocumentUploader { get; set; }

    public List<VehicleItemWarning>? Warnings { get; set; }

    public string? PrintUrl { get; set; }

    public bool DocumentUploaderIsRequired { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Description { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Title { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Image { get; set; } = default!;
    public string Type { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehcileServiceItemTypes TypeEnum { get; set; } = default!;

    [JsonCustomDateTime(ActivationAndExpiryDateFormat)]
    public DateTime ActivatedAt { get; set; }

    [JsonCustomDateTime(ActivationAndExpiryDateFormat)]
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VehcileServiceItemStatuses StatusEnum { get; set; } = default!;
    public long? CampaignID { get; set; }
    public string CampaignUniqueReference { get; set; }
    public string PackageCode { get; set; }
    public decimal? Cost { get; set; }
    public DateTimeOffset? ClaimDate { get; set; }
    public long? ModelCostID { get; set; }
    public long ServiceItemID { get; set; }
    public long? PaidServiceInvoiceLineID { get; set; }
    //public string CompanyID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string CompanyName { get; set; }
    public string InvoiceNumber { get; set; }
    public string JobNumber { get; set; }
    public long? MaximumMileage { get; set; }

    public bool Claimable { get; set; }

    [JsonIgnore]
    [TypeScriptIgnore]
    public int? ActiveFor { get; set; }

    [JsonIgnore]
    [TypeScriptIgnore]
    public DurationType? ActiveForDurationType { get; set; } = default!;

    [JsonIgnore]
    [TypeScriptIgnore]
    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }

    [JsonIgnore]
    [TypeScriptIgnore]
    public ClaimableItemCampaignActivationTypes CampaignActivationType { get; set; }

    [JsonIgnore]
    [TypeScriptIgnore]
    public ClaimableItemValidityMode ValidityModeEnum { get; set; }


    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimableItemClaimingMethod ClaimingMethodEnum { get; set; }

    public string VehicleInspectionID { get; set; }
    public string Signature { get; set; }
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
