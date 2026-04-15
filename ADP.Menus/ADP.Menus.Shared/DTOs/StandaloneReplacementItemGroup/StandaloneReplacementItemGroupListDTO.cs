using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ShiftSoftware.ADP.Menus.Shared.DTOs.StandaloneReplacementItemGroup;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class StandaloneReplacementItemGroupListDTO : ShiftEntityListDTO
{
    [StandaloneReplacementItemGroupHashId]
    public override string? ID { get; set; }
 
    public string Name { get; set; } = default!;

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string MenuCode { get; set; } = default!;

    public string LabourCode { get; set; } = default!;
}
