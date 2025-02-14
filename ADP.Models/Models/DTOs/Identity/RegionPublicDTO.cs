using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.DTOs.Identity;

public class RegionPublicDTO : ShiftEntityDTOBase
{
    [RegionHashIdConverter]
    public override string ID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public string ExternalId { get; set; } = default!;
    public string ShortCode { get; set; }
}
