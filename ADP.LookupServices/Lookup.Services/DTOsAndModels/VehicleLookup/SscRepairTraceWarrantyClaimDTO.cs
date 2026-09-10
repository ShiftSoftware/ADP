using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.Enums;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// One warranty claim on the vehicle as the SSC repair check saw it. Every claim on the VIN is listed, whether or
/// not it matched, so a reader can see why a claim was or was not taken as repair evidence.
/// </summary>
[TypeScriptModel]
[Docable]
public class SscRepairTraceWarrantyClaimDTO
{
    /// <summary>The distributor's claim number.</summary>
    public string ClaimNumber { get; set; }
    /// <summary>The dealer's own claim number, when different.</summary>
    public string DealerClaimNumber { get; set; }
    /// <summary>The claim's status at lookup time.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ClaimStatus ClaimStatus { get; set; }
    /// <summary>When the claimed repair was completed. This becomes the SSC repair date when the claim is selected.</summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? RepairCompletionDate { get; set; }
    /// <summary>Whether the status is one that counts as repair evidence (Accepted, Certified or Invoiced).</summary>
    public bool StatusQualifies { get; set; }
    /// <summary>Whether the campaign code was found in the claim's distributor comment.</summary>
    public bool CampaignCodeInComment { get; set; }
    /// <summary>The distributor comment that was searched for the campaign code.</summary>
    public string DistributorComment { get; set; }
    /// <summary>The claim's labor lines whose code is one of the campaign's codes, or interchangeable with one. Empty when none match.</summary>
    public List<SscRepairTraceLaborCodeDTO> MatchedLaborCodes { get; set; } = new List<SscRepairTraceLaborCodeDTO>();
    /// <summary>Whether the claim references the campaign at all — by comment or by labor code — regardless of status.</summary>
    public bool Matches { get; set; }
    /// <summary>Whether this is the claim that decided the verdict: the most recently completed claim that both qualifies by status and matches.</summary>
    public bool Selected { get; set; }
}
