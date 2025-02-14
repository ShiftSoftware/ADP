using FileHelpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ShiftSoftware.ADP.Models;

namespace ShiftSoftware.ADP.Models.DealerData;

[DelimitedRecord(",", null)]
[IgnoreFirst]
public class TIQOfficalVINCSV : CacheableCSV
{
    public long ID { get; set; }

    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string VIN { get; set; } = default!;

    [FieldQuoted(QuoteMode.OptionalForBoth)]
    public string Model { get; set; } = default!;

    [FieldConverter(typeof(QlikDateConverter))]
    public DateTime Date { get; set; }
}
