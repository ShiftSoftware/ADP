using ShiftSoftware.ADP.Models.Enums;
using System;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class VehicleInspectionModel : IPartitionedItem
{
    public string id { get; set; }
    public long VehicleInspectionTypeID { get; set; }
    public string VIN { get; set; } = default!;
    public DateTimeOffset InspectionDate { get; set; }
    public string Model { get; set; }
    public int ModelYear { get; set; }
    public string ModelCode { get; set; } = default!;
    public string JobNumber { get; set; }
    public string TechnicianName { get; set; }
    public string QualityControlName { get; set; }
    public string FrontPhoto { get; set; }
    public string RearPhoto { get; set; }
    public long? CustomerCountryID { get; set; }
    public long? CustomerCityID { get; set; }
    public CustomerTypes? CustomerType { get; set; }
    public string OrganizationName { get; set; }
    public string CustomerFirstName { get; set; } = default!;
    public string CustomerMiddleName { get; set; } = default!;
    public string CustomerLastName { get; set; } = default!;
    public string CustomerPhone { get; set; } = default!;
    public string CustomerEmail { get; set; }
    public Genders CustomerGender { get; set; }
    public bool IsDeleted { get; set; }
    public string ItemType => ModelTypes.VehicleInspection;
}