using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

[TypeScriptModel]
public class VehicleSaleInformation
{
    [ShiftSoftware.ShiftEntity.Model.HashIds.CountryHashIdConverter]
    public string CountryID { get; set; }
    public string CountryName { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string CompanyID { get; set; }
    public string CompanyName { get; set; }
    //public List<ShiftFileDTO> CompanyLogo { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyBranchHashIdConverter]
    public string BranchID { get; set; }
    public string BranchName { get; set; }
    public string CustomerAccountNumber { get; set; }
    public string CustomerID { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? InvoiceDate { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? WarrantyActivationDate { get; set; }

    public string InvoiceNumber { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public decimal? InvoiceTotal { get; set; }


    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string Status { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string Location { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string SaleType { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    [Newtonsoft.Json.JsonIgnore]
    [TypeScriptIgnore]
    public string AccountNumber { get; set; }
    public VehicleBrokerSaleInformation Broker { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.RegionHashIdConverter]
    public string RegionID { get; set; }
}