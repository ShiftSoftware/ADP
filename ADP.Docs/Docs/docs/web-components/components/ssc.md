# Special Service Campaigns

The `<vehicle-ssc>` component lists the Special Service Campaigns (safety recalls) affecting a vehicle: each campaign's code, description, repair status, labor operation codes and parts. It replaces the campaign half of the retired `<vehicle-warranty-details>`; warranty coverage is shown by [`<vehicle-warranty-timeline>`](vehicle-lookup.md).

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.4.0/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a VIN:</strong>
    <button onclick="document.getElementById('demo-ssc').fetchVin('JTMHX01J8L4198293')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMHX01J8L4198293</button>
    <button onclick="document.getElementById('demo-ssc').fetchVin('JTMW43FV10D123456')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMW43FV10D123456</button>
  </p>

  <vehicle-ssc show-trace="true" id="demo-ssc" language="en"></vehicle-ssc>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/vehicle-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-ssc');
          el.isDev = true;
          el.setMockData(mockData);
        });
    });
  </script>
</div>

---

## Standalone Usage

```html
<vehicle-ssc
  base-url="https://your-api.com/"
  show-trace="false"
  language="en">
</vehicle-ssc>
```

When used inside `<vehicle-lookup>`, no additional props are needed; pass campaign-specific settings through `children-props` under the `vehicle-ssc` key.

---

## Properties

| Property                           | Attribute                              | Type       | Default | Description                                                                                                      |
|------------------------------------|----------------------------------------|------------|---------|------------------------------------------------------------------------------------------------------------------|
| `isDev`                            | `is-dev`                               | `boolean`  | `false` | Enables development mode                                                                                          |
| `baseUrl`                          | `base-url`                             | `string`   | `''`    | Base URL for the vehicle lookup API                                                                                |
| `language`                         | `language`                             | `string`   | `'en'`  | Language code for localization                                                                                    |
| `showTrace`                        | `show-trace`                           | `boolean`  | `false` | Shows a "why this status?" control on every campaign row. See [Repair-status trace](#repair-status-trace).        |
| `recaptchaKey`                     | `recaptcha-key`                        | `string`   | `''`    | reCAPTCHA site key for the unauthorized-vehicle manufacturer check                                                |
| `unauthorizedSscLookupBaseUrl`     | `unauthorized-ssc-lookup-base-url`     | `string`   | `''`    | Endpoint of the reCAPTCHA-gated manufacturer check for vehicles the distributor has no record of                  |
| `unauthorizedSscLookupQueryString` | `unauthorized-ssc-lookup-query-string` | `string`   | `''`    | Query string appended to that endpoint                                                                            |
| `mockRecaptcha`                    | `mock-recaptcha`                       | `boolean`  | `false` | Renders a click-to-pass stand-in for the reCAPTCHA widget                                                         |
| `requestHeadersProvider`           | —                                      | `function` | —       | Asked for the current request headers before every request the component makes itself (the trace fetch, the manufacturer check) |
| `blazorRequestHeadersProvider`     | `blazor-request-headers-provider`      | `string`   | `''`    | Name of a `[JSInvokable]` method that answers with the current request headers                                    |
| `disableVinValidation`             | `disable-vin-validation`               | `boolean`  | `false` | Disables VIN format validation                                                                                    |
| `queryString`                      | `query-string`                         | `string`   | `''`    | Additional query string for API requests                                                                          |
| `lookupQueryString`                | `lookup-query-string`                  | `string`   | `''`    | Appended to the campaign lookup request only, never to the trace request. Where a lookup-logging flag belongs; `<vehicle-lookup>` passes its `ssc-query-string` here. |
| `coreOnly`                         | `core-only`                            | `boolean`  | `false` | Renders a slim layout without the panel chrome                                                                    |

---

## Data Displayed

- **Campaign** — the campaign code.
- **Description** — the campaign's description. The table uses the browser's automatic column layout, so this column absorbs the free width while the code and status columns stay as narrow as their content.
- **Repair status** — *Repaired* with the repair date, or *Open*. A repaired campaign also names the evidence that decided it: the campaign record, a warranty claim, or the service history.
- **Labor codes** — the labor operation codes the campaign lists.
- **Parts** — the part numbers, colour-coded by stock availability when the host has enabled it: in stock, not in stock, or not checked.

### Three widths

The table fits itself to the panel's width, not the window's. Above 960px every column stands on its own. Between 720px and 960px, a tablet or a narrow host column, the description moves under its campaign code, so five columns fit where six would not. At 720px and below each campaign becomes a labelled card. A width the columns still cannot fit, a long locale on a narrow host, scrolls sideways rather than being cut off by the card.

### Authorized and unauthorized vehicles are two different things

An **authorized** vehicle is in the distributor's records, so its campaign list is the verdict: an empty list is a genuine "no campaign affects this vehicle".

An **unauthorized** vehicle (`isAuthorized: false`) is not in the distributor's records, so the distributor cannot say anything about its campaigns. The panel shows no table, no headers and no count for it — only "not in the distributor's records" and, when the host configured a site key, the reCAPTCHA check that asks the manufacturer. The manufacturer's answer is the only verdict there is: pending campaign, no pending campaign, or vehicle not found. An empty list on an unauthorized vehicle is absence of data, never a clean bill, and the component never renders it as one; a populated list on an unauthorized vehicle is a data fault and is ignored.

### When the campaign check was skipped

Some hosts only count a lookup as a campaign check when it is made from the SSC tab — typically because that request is the one they log for their KPIs. Inside [`<vehicle-lookup>`](vehicle-lookup.md), `ssc-query-string` expresses that: it is appended to the SSC panel's own request only, and a search made from any other tab does not hydrate the SSC panel at all.

The panel must not sit blank next to a VIN in that case — a blank panel reads as "no campaigns". It is told the lookup was skipped (`skipLookup(vin)`), names the vehicle, says the check was not run, and offers a **Check campaigns** button that runs the panel's own lookup with its own query string. That lookup goes through the same `loadedResponse` callback as a search, so a wrapper hydrates the other panels from it.

### How the card moves between states

The card is three fixed parts — the header with its verdict pill, a lead strip, and a body — and every state is a different content of the same three. While a lookup is in flight the pill and the strip carry a loading sheen and the body slides shut over whatever it was showing; the result arrives by the strip cross-fading to the column headings or a notice and the body sliding open on the rows, the reCAPTCHA widget, or the "check campaigns" action. Nothing appears or disappears without a transition, which is a rule of the whole component library rather than of this panel.

---

## Methods

| Method                     | Description                                                                                                                             |
|----------------------------|-----------------------------------------------------------------------------------------------------------------------------------------|
| `fetchVin(vin, headers?)`  | Looks the vehicle up, or hydrates the panel from a `VehicleLookupDTO` passed in place of the VIN.                                        |
| `skipLookup(vin)`          | Tells the panel a vehicle was looked up without it: it names the VIN, says the check was not run, and offers to run it. `<vehicle-lookup>` calls this when `ssc-query-string` is set and the search came from another tab. |
| `clearData()`              | Back to the empty state. The body slides shut over its content first.                                                                    |
| `setMockData(data)`        | Fixtures for development mode (`is-dev`).                                                                                                 |
| `setBlazorRef(ref)`        | Registers a `DotNetObjectReference` so a Blazor host can receive callbacks by name.                                                       |

---

## Repair-status trace

With `show-trace="true"`, every row carries a control that opens the evidence behind its status:

1. **Campaign record** — whether the record itself carries a repair date.
2. **Warranty claims** — every claim on the vehicle, with its status, whether that status counts as evidence, and whether the claim references the campaign (by campaign code in the distributor comment, or by a labor code).
3. **Service history** — how many labor lines were examined and which ones carry a campaign labor code, with their invoice status.

The sources are checked in that order and the first one that holds decides. Labor codes match directly or through the interchangeable groups the host configured in `LookupOptions.SSCInterchangeableLaborCodeGroups`; an interchangeable match names both codes.

The trace is fetched on first use with `?trace=ssc` appended to the lookup request, so the ordinary response stays small. The host's endpoint must set `VehicleLookupRequestOptions.TraceSSCEvaluation` when it sees that query, and should do so only for callers permitted to see claim and invoice details — the component has no permission concept, so the host gates `show-trace` on its own check as well.

Opening a drawer brings its row to the top of the page with the browser's own kind of scroll, eased and timed by the distance, and holds it there while the drawer slides open beneath it, the way an expanded service-history line behaves, so the evidence is in view once it has settled rather than below the fold. A host whose fixed header covers the top of the page sets `scroll-padding-top` on its scroll container, as it would for `scrollIntoView`, and the component keeps that room; under `prefers-reduced-motion` the page lands at once, and a wheel, touch or key from the reader cancels the scroll.

### Lookup logs and traces

Hosts that count campaign lookups for their KPIs log a lookup when its request carries their logging flag. Put that flag in `lookup-query-string` (inside `<vehicle-lookup>`, in `ssc-query-string`), not in `query-string`: the trace request re-reads a lookup that was already logged, and the component never lets `lookup-query-string` join it, so opening a campaign's evidence is never counted as another lookup. The server holds the same line: `VehicleLookupService` ignores `InsertSSCLog` and the customer-lookup log flag whenever the request asks for a trace, whatever the query string says, so a host that keeps its flag on every tab's request is covered too.

### Request headers

The component makes follow-up requests of its own — the trace, the manufacturer check — long after the search that loaded the vehicle. A host whose access token expires should supply a provider, so those requests carry a fresh token rather than the one handed over at search time:

```html
<vehicle-lookup blazor-request-headers-provider="GetRequestHeaders" ...></vehicle-lookup>
```

```csharp
[JSInvokable]
public async Task<Dictionary<string, string>> GetRequestHeaders()
{
    var token = await IdentityStore.GetTokenAsync(); // refreshes when expired
    return new() { ["Authorization"] = $"Bearer {token!.Token}" };
}
```

A JavaScript host sets `requestHeadersProvider` to a function returning the headers (or a promise of them). Without a provider, the headers passed with the most recent `fetchVin` call are reused.
