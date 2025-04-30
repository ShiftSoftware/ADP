using ShiftSoftware.ADP.Lookup.Services.Enums;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleServiceItemDTO
{
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Description { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Title { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Image { get; set; } = default!;
    public string Type { get; set; }
    public VehcileServiceItemTypes TypeEnum { get; set; } = default!;

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime ActivatedAt { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? ExpiresAt { get; set; }
    public string Status { get; set; }
    public VehcileServiceItemStatuses StatusEnum { get; set; } = default!;
    public string CampaignCode { get; set; }
    public string PackageCode { get; set; }
    public DateTime? ClaimDate { get; set; }
    public long? ModelCostID { get; set; }
    public long ServiceItemID { get; set; }
    public long? PaidServiceInvoiceLineID { get; set; }
    public string CompanyID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string CompanyName { get; set; }
    public string InvoiceNumber { get; set; }
    public string JobNumber { get; set; }
    public long? MaximumMileage { get; set; }
    public bool SkipZeroTrust { get; set; }
    public int ActiveFor { get; set; }
    public DurationType? ActiveForInterval { get; set; } = default!;
    public ClaimableItemCampaignActivationTrigger CampaignActivationTrigger { get; set; }
    public ClaimableItemCampaignActivationTypes CampaignActivationType { get; set; }
    public string Signature { get; set; }

    public string GenerateSignature(string vin, string secretKey)
    {
        string stringToSign = string.Join(
            ",",
            vin.ToUpper(),
            (int) this.TypeEnum,
            this.ActivatedAt.ToString("O"),
            this.ExpiresAt?.ToString("O") ?? string.Empty,
            (int) this.StatusEnum,
            this.ModelCostID,
            this.ServiceItemID,
            this.PaidServiceInvoiceLineID,
            this.SkipZeroTrust
        );

        var keyBytes = Encoding.UTF8.GetBytes(secretKey);

        var messageBytes = Encoding.UTF8.GetBytes(stringToSign);

        using var hmac = new HMACSHA256(keyBytes);

        var hash = hmac.ComputeHash(messageBytes);

        return Convert.ToBase64String(hash);
    }

    public bool ValidateSignature(string vin, string secretKey)
    {
        var generatedSignature = GenerateSignature(vin, secretKey);

        return generatedSignature == this.Signature;
    }

    public VehicleServiceItemDTO Clone()
    {
        return (VehicleServiceItemDTO) this.MemberwiseClone();
    }
}
