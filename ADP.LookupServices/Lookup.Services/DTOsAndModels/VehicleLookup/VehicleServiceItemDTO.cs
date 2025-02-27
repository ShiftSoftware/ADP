using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model;
using System;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

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
    public string MenuCode { get; set; }
    public DateTime? RedeemDate { get; set; }
    public long? ModelCostID { get; set; }
    public long ServiceItemID { get; set; }
    public long? PaidServiceInvoiceLineID { get; set; }
    public string CompanyIntegrationID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string CompanyName { get; set; }
    public string InvoiceNumber { get; set; }
    public string JobNumber { get; set; }
    public long? MaximumMileage { get; set; }
    public bool SkipZeroTrust { get; set; }
    public int? ActiveFor { get; set; }
    public string ActiveForInterval { get; set; } = default!;
}
