using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using System.Text.Json.Serialization;

namespace ShiftSoftware.ADP.Models.DTOs.Identity;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CityPublicDTO : ShiftEntityDTOBase
{
    [CityHashIdConverter]
    public override string ID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public RegionPublicDTO Region { get; set; }
}
