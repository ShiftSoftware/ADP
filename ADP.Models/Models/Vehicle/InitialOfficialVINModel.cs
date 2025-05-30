﻿using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class InitialOfficialVINModel: IPartitionedItem
{
    public string id { get; set; } = default!;
    public string VIN { get; set; } = default!;
    public string Model { get; set; } = default!;
    public DateTime Date { get; set; }
    public string ItemType => ModelTypes.InitialOfficialVIN;
}