using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using ShiftSoftware.ADP.Models.JsonConverters;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class VehicleSaleInformation
{
    public string DealerIntegrationID { get; set; }
    public string DealerName { get; set; }
    public List<ShiftFileDTO>? DealerLogo { get; set; }
    public string BranchIntegrationID { get; set; }
    public string BranchName { get; set; }
    public string CustomerAccount { get; set; }
    public string CustomerID { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }
    public int InvoiceNumber { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public decimal? InvoiceTotal { get; set; }


    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string ProgressCode { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string LocationCode { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string ACSStatus { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string SaleType { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string InvoiceAccount { get; set; }

    public VehicleBrokerSaleInformation Broker { get; set; }

    public string RegionIntegrationId { get; set; }
}
