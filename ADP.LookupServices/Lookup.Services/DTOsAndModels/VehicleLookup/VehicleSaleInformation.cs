using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

public class VehicleSaleInformation
{
    public string CompanyIntegrationID { get; set; }
    public string CompanyName { get; set; }
    public List<ShiftFileDTO> CompanyLogo { get; set; }
    public string BranchIntegrationID { get; set; }
    public string BranchName { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }
    public int InvoiceNumber { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public decimal? InvoiceTotal { get; set; }


    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string Status { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string Location { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string SaleType { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    public string AccountNumber { get; set; }
    public VehicleBrokerSaleInformation Broker { get; set; }
    public string RegionIntegrationID { get; set; }
}