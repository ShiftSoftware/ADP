using System;

namespace ShiftSoftware.ADP.Models.Vehicle.DuckDBModels;

public class PaidServiceInvoiceDuckDBModel : PaidServiceInvoiceModel
{
    public new long id { get; set; }
    public DateTime? LastSaveDate { get; set; }
}
