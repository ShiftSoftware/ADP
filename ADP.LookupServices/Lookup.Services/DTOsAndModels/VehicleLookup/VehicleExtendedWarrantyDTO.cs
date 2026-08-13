using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>One extended-warranty coverage and its provider.</summary>
[TypeScriptModel]
[Docable]
public class VehicleExtendedWarrantyDTO
{
    /// <summary>The persisted warranty-entry or configured definition identifier.</summary>
    public string ID { get; set; }

    /// <summary>The Identity company ID of the warranty provider.</summary>
    [ShiftSoftware.ShiftEntity.Model.HashIds.CompanyHashIdConverter]
    public string ProviderCompanyID { get; set; }

    /// <summary>The resolved logo URL of the warranty provider.</summary>
    public string? ProviderCompanyLogo { get; set; }

    /// <summary>The first date covered by this extended warranty.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? StartDate { get; set; }

    /// <summary>The coverage end date.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? EndDate { get; set; }
}
