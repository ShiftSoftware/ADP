using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ShiftSoftware.ADP.Surveys.Web.Components;

/// <summary>
/// Thin owner of <c>token-insert.js</c> for a component that hosts editable fields
/// and a <see cref="TokenInsertMenu"/>: tracks which field inside a container the
/// author last had the caret in, and splices a token in at that caret. Insertion
/// goes through the DOM rather than the bound model on purpose — see the header
/// of the JS file, and of <c>LocalizedStringField</c>.
/// </summary>
public sealed class TokenInsertInterop : IAsyncDisposable
{
    private static readonly string ModulePath =
        $"./_content/ShiftSoftware.ADP.Surveys.Web/js/shift-survey/token-insert.js?v={typeof(TokenInsertInterop).Assembly.GetName().Version}";

    private readonly IJSRuntime js;
    private IJSObjectReference? module;
    private bool tracking;

    public TokenInsertInterop(IJSRuntime js)
    {
        this.js = js;
    }

    /// <summary>
    /// Starts caret tracking inside <paramref name="container"/>. Idempotent — call it
    /// from <c>OnAfterRenderAsync</c> on every render; only the first does work.
    /// </summary>
    public async Task TrackAsync(ElementReference container)
    {
        if (tracking) return;
        try
        {
            module ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await module.InvokeVoidAsync("trackTokenTargets", container);
            tracking = true;
        }
        catch (JSDisconnectedException) { /* navigated away mid-import */ }
        catch (JSException)
        {
            // No caret tracking available — InsertAsync still appends to the first
            // box, so the menu keeps working, just less precisely.
        }
    }

    /// <summary>Inserts <paramref name="text"/> at the caret of the last-focused field in <paramref name="container"/>.</summary>
    public async Task InsertAsync(ElementReference container, string text)
    {
        if (module is null) return;
        try
        {
            await module.InvokeAsync<bool>("insertToken", container, text);
        }
        catch (JSDisconnectedException) { }
        catch (JSException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (module is null) return;
        try { await module.DisposeAsync(); }
        catch (JSDisconnectedException) { }
        catch (ObjectDisposedException) { }
        module = null;
    }
}
