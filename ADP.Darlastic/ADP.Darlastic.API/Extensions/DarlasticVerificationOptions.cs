using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ADP.Darlastic.Shared;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace ShiftSoftware.ADP.Darlastic.API.Extensions;

/// <summary>
/// Verification: recording that someone proved a claim about a golden customer (this is the person, this phone
/// is theirs, this vehicle is theirs), and answering what is verified. A concept of its own beside unification:
/// nothing here changes which records are merged, and no resolve reads it.
///
/// <para>The module holds the facts and the rules; HOW a claim is proven is the host's. A host runs its own
/// check (a code sent to the phone, a document reader, a rule over its own data, a staff tick box) and reports
/// the result to <c>VerificationService</c>, or, when it allows that, over HTTP.</para>
/// </summary>
public class DarlasticVerificationOptions
{
    /// <summary>
    /// Off by default. Off, the two verification tables are not part of the host's model (so a host that does
    /// not use verification needs no migration when it takes a module version that has it), the service refuses
    /// to work and the endpoints answer 404. Turning it on needs a host migration that creates the tables.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// The kinds of claim and their settings, by name. Defaults: <c>person</c> (not exclusive, no value),
    /// <c>phone</c> and <c>vehicle</c> (exclusive: verifying the value for one customer ends it for any other).
    /// A host changes a kind's settings in place, removes a kind or adds its own.
    /// </summary>
    public Dictionary<string, VerificationKindOptions> Kinds { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [VerificationKind.Person] = new()
        {
            Exclusive = false,
            ValueRequired = false,
            HashValue = true,
            Normalize = v => Norm.Id(v) is { Length: > 0 } id ? id : null,
        },
        [VerificationKind.Phone] = new()
        {
            Exclusive = true,
            ValueRequired = true,
            Normalize = Norm.Phone,
            Pii = PiiKind.Phone,
        },
        [VerificationKind.Vehicle] = new()
        {
            Exclusive = true,
            ValueRequired = true,
            Normalize = Norm.Vin,
        },
    };

    /// <summary>
    /// What "verified" means per context of the host (a service visit, a sale ...): the kinds that must hold.
    /// The entry under the empty name answers a question that names no context, and any context without an
    /// entry. Default: the person is verified. The context only selects a rule; the stored facts are the same
    /// for every context.
    /// </summary>
    public Dictionary<string, List<VerificationRequirement>> Rules { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        [""] = [new(VerificationKind.Person)],
    };

    /// <summary>
    /// Key for the keyed hash a <see cref="VerificationKindOptions.HashValue"/> kind stores instead of its value
    /// (by default: the document number a person was verified with). Null or empty (the default): such a kind
    /// stores no value at all. Set, the module can tell that one document was verified on two customers, at the
    /// cost of holding one more piece of data derived from a personal identifier.
    /// </summary>
    public string? ValueHashKey { get; set; }

    /// <summary>
    /// Accept a "verified" or "failed" report over HTTP from a caller holding the write action. Off by default:
    /// a report over HTTP is a person's word that a claim was proven, which is right for a host whose method is
    /// a staff check and wrong for one whose method is a code or a document reader. Such a host reports from
    /// its own server code, after its own check passed. Revoking is always available to a caller holding the
    /// delete action.
    /// </summary>
    public bool AcceptReportsOverHttp { get; set; }

    /// <summary>The rule for a context: its own, else the default rule, else nothing required.</summary>
    public IReadOnlyList<VerificationRequirement> RuleFor(string? context) =>
        Rules.TryGetValue(context?.Trim() ?? "", out var rule) ? rule
        : Rules.TryGetValue("", out var fallback) ? fallback
        : [];
}

/// <summary>One kind of claim.</summary>
public class VerificationKindOptions
{
    /// <summary>The value is verified for one customer at a time: verifying it for another ends the first.</summary>
    public bool Exclusive { get; set; }

    /// <summary>A report must carry a value (a phone, a VIN).</summary>
    public bool ValueRequired { get; set; }

    /// <summary>The value's stored key, or null when the value cannot be one. Null leaves the value trimmed.
    /// A host with its own phone format sets this for <c>phone</c>.</summary>
    public Func<string, string?>? Normalize { get; set; }

    /// <summary>Store a keyed hash of the value, never the value (see
    /// <see cref="DarlasticVerificationOptions.ValueHashKey"/>). The value is then never shown either.</summary>
    public bool HashValue { get; set; }

    /// <summary>The method names a report may carry for this kind; empty accepts any.</summary>
    public HashSet<string> Methods { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>How long a verified claim holds; null (the default) never expires.</summary>
    public TimeSpan? Validity { get; set; }

    /// <summary>The value is personal data of this kind: shown masked to a caller without the host's reveal
    /// action. Null shows it to every caller who may read verification.</summary>
    public PiiKind? Pii { get; set; }
}

/// <summary>One requirement of a "verified" rule.</summary>
/// <param name="Kind">The kind that must hold.</param>
/// <param name="SpecificValue">The visit's own value of that kind must be the verified one (the vehicle in for
/// service), not just any value.</param>
public record VerificationRequirement(string Kind, bool SpecificValue = false);
