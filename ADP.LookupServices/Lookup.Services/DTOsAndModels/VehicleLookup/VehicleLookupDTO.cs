using Newtonsoft.Json;
using ShiftSoftware.ADP.Models.JsonConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.DTOs.VehicleLookupDTOs;

public class VehicleLookupDTO
{
    public string VIN { get; set; } = default!;

    public VehicleIdentifiersDTO Identifiers { get; set; }

    public VehicleSaleInformation SaleInformation { get; set; }

    public PaintThicknessDTO PaintThickness { get; set; }

    public bool IsAuthorized { get; set; }

    public VehicleWarrantyDTO Warranty { get; set; }

    [JsonCustomDateTime("yyyy-MM-dd")]
    public DateTime? NextServiceDate { get; set; }

    public IEnumerable<VehicleServiceHistoryDTO> ServiceHistory { get; set; }


    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [System.Text.Json.Serialization.JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? SSCLogId { get; set; }

    public IEnumerable<SSCDTO> SSC { get; set; }

    public VehicleVariantInfoDTO VehicleVariantInfo
    {
        get
        {
            try
            {
                var variant = Identifiers.Variant;

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
            catch
            {
                return null;
            }
        }
    }

    public VehicleSpecificationDTO VehicleSpecification { get; set; }

    public IEnumerable<VehicleServiceItemDTO> ServiceItems { get; set; }
    public IEnumerable<AccessoryDTO> Accessories { get; set; }

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