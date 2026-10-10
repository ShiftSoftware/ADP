using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ADP.Darlastic.Engine;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;

namespace ShiftSoftware.ADP.Darlastic.API.Controllers;

/// <summary>
/// The golden PII contract, applied to the record evidence the steward and case browser surfaces
/// serve. One definition for every such controller, so the surfaces cannot drift apart on what a
/// caller without the reveal action sees.
///
/// <para>Names, phones, ID numbers, e-mails, birth dates and addresses are masked with the
/// framework's masker unless the caller holds the host's PII reveal action
/// (<see cref="PiiOptions.Action"/>). Searching by part of a name or phone needs the partial-search
/// action (<see cref="PiiOptions.PartialSearchAction"/>); without it a search matches whole values
/// only. Fails closed: a host without TypeAuth cannot show that anyone holds either action.</para>
/// </summary>
internal sealed class RecordPiiView
{
    private readonly IPiiMasker masker;
    private readonly PiiOptions options;

    public bool Reveal { get; }
    public bool PartialSearch { get; }

    public RecordPiiView(IPiiMasker masker, PiiOptions options, bool reveal, bool partialSearch)
    {
        this.masker = masker;
        this.options = options;
        Reveal = reveal;
        PartialSearch = partialSearch;
    }

    /// <summary>What the request's own principal may see, asked of TypeAuth now.</summary>
    public static RecordPiiView ForCaller(HttpContext? http, IPiiMasker masker, PiiOptions options)
    {
        var typeAuth = http?.RequestServices.GetService<ITypeAuthService>();
        return new(masker, options,
            reveal: typeAuth?.CanAccess(options.Action) == true,
            partialSearch: typeAuth?.CanAccess(options.PartialSearchAction) == true);
    }

    /// <summary>The value as this caller may see it. Empty stays empty, so "no name" still reads as such.</summary>
    public string? Show(PiiKind kind, string? raw) =>
        Reveal || string.IsNullOrEmpty(raw) ? raw : masker.Mask(kind, raw);

    public List<string> Show(PiiKind kind, IEnumerable<string>? raw) =>
        raw is null ? [] : [.. raw.Select(v => Show(kind, v)!)];

    /// <summary>
    /// How the engine's explanations write a record's values for this caller: as recorded for a
    /// caller who may see them, otherwise masked exactly like the same value in the record itself.
    /// The engine hands over each value whole, so nothing depends on how the surrounding text quotes
    /// it — a name containing quotes or any other punctuation is replaced like any other.
    /// </summary>
    public ValueQuoter? Quoter => Reveal ? null : (kind, value) => Show(KindOf(kind), value) ?? "";

    /// <summary>What normalization did to the record, worded by the engine, values as this caller may see them.</summary>
    public List<NormalizeStep> Normalize(RealRecord record) => Normalization.Steps(record, Quoter);

    /// <summary>A trace sink for explaining one pair to this caller.</summary>
    public MatchTrace NewTrace() => new(Quoter);

    /// <summary>The masker kind for a value an explanation quotes. A birth date is masked as an ID
    /// number, the way the record's own birth date is.</summary>
    internal static PiiKind KindOf(QuotedValue kind) => kind switch
    {
        QuotedValue.Name => PiiKind.Name,
        QuotedValue.Phone => PiiKind.Phone,
        QuotedValue.Email => PiiKind.Email,
        QuotedValue.Address => PiiKind.Address,
        _ => PiiKind.Identifier,
    };

    /// <summary>A blocking key ("P:&lt;phone&gt;", "N:&lt;name prefix&gt;") keeps its kind and hides its value.</summary>
    public string BlockKey(string key) =>
        Reveal ? key : key[..(key.IndexOf(':') + 1)] + options.HiddenText;
}
