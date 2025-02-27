using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class VehicleServiceHistoryDTO
{
    public string ServiceType { get; set; } = default!;

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? ServiceDate { get; set; }
    public int? Mileage { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string CompanyName { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string BranchName { get; set; }
    public string CompanyIntegrationID { get; set; }
    public string BranchIntegrationID { get; set; }
    public string AccountNumber { get; set; } = default!;
    public int? InvoiceNumber { get; set; }
    public int? JobNumber { get; set; }
    public IEnumerable<VehicleLaborDTO> LaborLines { get; set; }
    public IEnumerable<VehiclePartDTO> PartLines { get; set; }
}