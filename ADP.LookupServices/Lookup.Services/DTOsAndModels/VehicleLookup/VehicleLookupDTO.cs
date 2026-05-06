using Newtonsoft.Json;
using ShiftSoftware.ADP.Lookup.Services.Diagnostics;
using ShiftSoftware.ADP.Models;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Lookup.Services.DTOsAndModels.VehicleLookup;

/// <summary>
/// The main result returned by the vehicle lookup service.
/// Contains all vehicle data including identifiers, sale information, warranty, service history, service items, accessories, and safety recalls.
/// </summary>
[TypeScriptModel]
[Docable]
public class VehicleLookupDTO
{
    /// <summary>
    /// The Vehicle Identification Number (VIN) that was looked up.
    /// </summary>
    public string VIN { get; set; } = default!;

    /// <summary>
    /// The vehicle's <see cref="VehicleIdentifiersDTO">identifiers</see> (VIN, Variant, Katashiki, Color, Trim, Brand).
    /// </summary>
    public VehicleIdentifiersDTO Identifiers { get; set; }

    /// <summary>
    /// The vehicle's <see cref="VehicleSaleInformation">sale information</see> including invoice date, dealer, and broker details.
    /// </summary>
    public VehicleSaleInformation SaleInformation { get; set; }

    /// <summary>
    /// A list of <see cref="PaintThicknessInspectionDTO">paint thickness inspections</see> performed on this vehicle.
    /// </summary>
    public IEnumerable<PaintThicknessInspectionDTO> PaintThicknessInspections { get; set; }

    [DocIgnore]
    [Obsolete("This property is deprecated. Use PaintThicknessInspections instead.")]
    public LegacyPaintThicknessDTO PaintThickness { get; set; }

    /// <summary>
    /// Indicates whether the vehicle is authorized (has official VIN entries or SSC records).
    /// </summary>
    public bool IsAuthorized { get; set; }

    /// <summary>
    /// The vehicle's <see cref="VehicleWarrantyDTO">warranty information</see> including start/end dates and extended warranty.
    /// </summary>
    public VehicleWarrantyDTO Warranty { get; set; }

    /// <summary>
    /// The next scheduled service date for this vehicle.
    /// </summary>
    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? NextServiceDate { get; set; }

    /// <summary>
    /// The vehicle's <see cref="VehicleServiceHistoryDTO">service history</see> — a list of past service invoices with labor and part lines.
    /// </summary>
    public IEnumerable<VehicleServiceHistoryDTO> ServiceHistory { get; set; }

    [DocIgnore]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? SSCLogId { get; set; }

    /// <summary>
    /// A list of <see cref="SscDTO">Special Service Campaigns (SSC)</see> / safety recalls affecting this vehicle.
    /// </summary>
    public IEnumerable<SscDTO> SSC { get; set; }

    /// <summary>
    /// Parsed <see cref="VehicleVariantInfoDTO">variant information</see> (ModelCode, SFX, ModelYear) derived from the Variant string.
    /// </summary>
    public VehicleVariantInfoDTO VehicleVariantInfo
    {
        get
        {
            try
            {
                var variant = Identifiers?.Variant;

                if (!string.IsNullOrWhiteSpace(variant))
                {
                    var modelCode = variant.Substring(0, variant.Length - 8);
                    var sfx = variant.Substring(variant.Length - 8, 2);
                    var yearModel = variant.Substring(variant.Length - 6, 4);

                    return new VehicleVariantInfoDTO
                    {
                        ModelCode = modelCode,
                        SFX = sfx,
                        ModelYear = int.Parse(yearModel)
                    };
                }
                else
                {
                    return new VehicleVariantInfoDTO
                    {
                        ModelCode = this.VehicleSpecification?.ModelCode,
                        ModelYear = this.VehicleSpecification?.ModelYear,
                        SFX = null,
                    };
                }
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// The vehicle's <see cref="VehicleSpecificationDTO">technical specifications</see> (model, body, engine, transmission, colors).
    /// </summary>
    public VehicleSpecificationDTO VehicleSpecification { get; set; }

    /// <summary>
    /// The <see cref="VehicleServiceItemDTO">service items</see> available for this vehicle (free and paid).
    /// </summary>
    public IEnumerable<VehicleServiceItemDTO> ServiceItems { get; set; }

    /// <summary>
    /// The <see cref="AccessoryDTO">accessories</see> installed on this vehicle.
    /// </summary>
    public IEnumerable<AccessoryDTO> Accessories { get; set; }

    /// <summary>
    /// Diagnostic trace of the service-item evaluator (filters, expansions, status,
    /// post-processing). Populated only when
    /// <see cref="VehicleLookupRequestOptions.TraceServiceItemEvaluation"/> is true.
    /// Excluded from production responses and from the generated TypeScript model.
    /// Render via <see cref="ServiceItemTraceRenderer"/>.
    /// </summary>
    [DocIgnore]
    [TypeScriptIgnore]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ServiceItemTrace ServiceItemTrace { get; set; }

    /// <summary>
    /// The basic model code extracted from the Katashiki (first segment before the hyphen, with trailing L/R removed).
    /// </summary>
    public string BasicModelCode
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Identifiers.Katashiki))
                return null;

            var partOne = Identifiers.Katashiki.Split('-')[0];

            //Remove L and R at the end if length more than 5
            if (partOne.Length > 5 && (partOne.Last() == 'L' || partOne.Last() == 'R'))
                partOne = partOne.Substring(0, partOne.Length - 1);

            return partOne;
        }
    }
}