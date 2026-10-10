using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core.Pii;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.TypeAuth.Core;
using System.Text.RegularExpressions;

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

    private static readonly Regex QuotedText = new("\"[^\"]*\"", RegexOptions.Compiled);
    private static readonly Regex EmailLike = new(@"[^\s()""]+@[^\s()""]+", RegexOptions.Compiled);
    // A digit run standing on its own (a phone, an ID number). Not one ending a longer code: a VIN's
    // serial digits are part of the vehicle number, which the contract does not mask.
    private static readonly Regex LongNumber = new(@"(?<![A-Za-z0-9])\+?\d{6,}", RegexOptions.Compiled);
    private static readonly Regex DateLike = new(@"\b\d{1,2}/\d{1,2}/\d{4}\b", RegexOptions.Compiled);

    /// <summary>
    /// Engine explanation text quotes the records it compares: names and addresses in quotes,
    /// phones and ID numbers as digit runs, e-mails, birth dates. Masked callers get the reasoning
    /// with those values hidden; the engine's own wording and percentages survive.
    /// </summary>
    public string Scrub(string? text)
    {
        if (Reveal || string.IsNullOrEmpty(text)) return text ?? "";
        string hidden = options.HiddenText;
        text = QuotedText.Replace(text, $"\"{hidden}\"");
        text = EmailLike.Replace(text, hidden);
        text = DateLike.Replace(text, hidden);
        return LongNumber.Replace(text, hidden);
    }

    /// <summary>A blocking key ("P:&lt;phone&gt;", "N:&lt;name prefix&gt;") keeps its kind and hides its value.</summary>
    public string BlockKey(string key) =>
        Reveal ? key : key[..(key.IndexOf(':') + 1)] + options.HiddenText;
}
