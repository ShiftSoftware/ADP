namespace ShiftSoftware.ADP.Lookup.Services.Enums;

public enum QuantityLookUpResults
{
    LookupIsSkipped = 0,
    Available = 1,
    PartiallyAvailable = 2,
    NotAvailable = 3,
    QuantityNotWithinLookupThreshold = 4,
}