using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Darlastic.Shared.DTOs.GoldenCustomer;

/// <summary>
/// Every phone a golden customer's source records carry. The golden's survived values hold ONE phone (the
/// primary: most attested, or a steward's override); matching and search use all of them, and this is where a
/// surface can show them. Read from the records the last resolve staged, so it needs a resolve that stages
/// per-golden evidence (every record); with only some records staged it lists those records' phones.
/// </summary>
public class GoldenCustomerPhonesDTO
{
    /// <summary>The live customer (after following a merge).</summary>
    public string? ID { get; set; }

    /// <summary>The primary first, then by how many records carry the number.</summary>
    public List<GoldenCustomerPhoneDTO> Phones { get; set; } = new();
}

public class GoldenCustomerPhoneDTO
{
    /// <summary>The number in the tenant's key form, masked for a caller who may not see personal data.</summary>
    [Pii(PiiKind.Phone)]
    public PiiFieldDTO? Phone { get; set; }

    /// <summary>The golden's primary phone (the one its survived values carry).</summary>
    public bool Primary { get; set; }

    /// <summary>An incomplete number (a source's truncated form): matched on its last digits only.</summary>
    public bool Incomplete { get; set; }

    /// <summary>How many of the customer's source records carry it.</summary>
    public int Records { get; set; }

    /// <summary>The source systems of those records.</summary>
    public List<string> Sources { get; set; } = new();
}
