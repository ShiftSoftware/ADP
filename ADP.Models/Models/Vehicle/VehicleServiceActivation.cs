using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleServiceActivation : IPartitionedItem
{
    public string id { get; set; }
    public string VIN { get; set; }
    public DateTime? WarrantyActivationDate { get; set; }
    public string CustomerFirstName { get; set; } = default!;
    public string CustomerMiddleName { get; set; } = default!;
    public string CustomerLastName { get; set; } = default!;
    public Genders CustomerGender { get; set; }
    public string CustomerPhone { get; set; } = default!;
    public string CustomerEmail { get; set; }
    public bool IsDeleted { get; set; }
    public string ItemType => ModelTypes.VehicleServiceActivation;
}