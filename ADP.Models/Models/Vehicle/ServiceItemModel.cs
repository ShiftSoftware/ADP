using ShiftSoftware.ADP.Models.Enums;
using System;
using System.Collections.Generic;

namespace ShiftSoftware.ADP.Models.Vehicle;

public class ServiceItemModel
{
    public string id { get; set; } = default!;
    public long Id { get; set; }
    public Dictionary<string, string> Name { get; set; }
    public Dictionary<string, string> Photo { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? PublishDate { get; set; }
    public DateTime? ExpireDate { get; set; }
    public Dictionary<string, string> PrintoutTitle { get; set; }
    public Dictionary<string, string> PrintoutDescription { get; set; }
    public IEnumerable<Brands> Brands { get; set; }
    public IEnumerable<string> BrandIntegrationIDs { get; set; }
    public bool SkipZeroTrust { get; set; }
    public int? ActiveFor { get; set; }
    public string ActiveForInterval { get; set; }
    public long? MaximumMileage { get; set; }
    public string MenuCode { get; set; }
    public IEnumerable<ServiceItemCostModel> ModelCosts { get; set; }
}