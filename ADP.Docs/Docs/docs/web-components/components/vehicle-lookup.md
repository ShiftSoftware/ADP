# Vehicle Lookup

The `<vehicle-lookup>` component is a **wrapper** that composes all of the vehicle information
components (specification, sale information, accessories, warranty &amp; SSC, service history, paint
thickness, claimable items) into one coordinated unit.

Unlike the individual components, the wrapper does **not** render its own tab bar &mdash; it exposes
an `activeElement` property and renders whichever child is active. The host page is responsible for
the tab UI. This lets integrators style the navigation to match their own design system.

When data is loaded for the active child, the wrapper automatically distributes the response to the
other children, so switching tabs is instant and does not re-fetch.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.97/dist/shift-components/shift-components.esm.js"></script>

  <style>
    .vl-demo-tabs {
      display: flex;
      flex-wrap: wrap;
      gap: 0;
      border-bottom: 1px solid var(--md-default-fg-color--lightest, #e0e0e0);
      margin: 16px 0 0;
    }
    .vl-demo-tab {
      background: none;
      border: none;
      padding: 12px 18px;
      cursor: pointer;
      font-size: 13px;
      font-weight: 500;
      color: var(--md-default-fg-color--light, #666);
      border-bottom: 2px solid transparent;
      transition: color 0.2s, border-color 0.2s, background 0.2s;
      margin-bottom: -1px;
      font-family: inherit;
      white-space: nowrap;
    }
    .vl-demo-tab:hover:not([disabled]) {
      color: var(--md-primary-fg-color, #1976d2);
      background: var(--md-default-fg-color--lightest, #f5f5f5);
    }
    .vl-demo-tab.active {
      color: var(--md-primary-fg-color, #1976d2);
      border-bottom-color: var(--md-primary-fg-color, #1976d2);
    }
    .vl-demo-tab[disabled] { opacity: 0.5; cursor: wait; }

    .vl-demo-vins {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 8px;
      margin: 16px 0;
    }
    .vl-demo-vins-label {
      font-size: 13px;
      font-weight: 500;
      color: var(--md-default-fg-color--light, #666);
      margin-right: 4px;
    }
    .vl-demo-chip {
      background: var(--md-default-fg-color--lightest, #f5f5f5);
      border: 1px solid var(--md-default-fg-color--lighter, #e0e0e0);
      border-radius: 16px;
      padding: 5px 14px;
      font-size: 12px;
      cursor: pointer;
      font-family: var(--md-code-font-family, ui-monospace, monospace);
      transition: background 0.2s, border-color 0.2s;
      color: var(--md-default-fg-color, #333);
    }
    .vl-demo-chip:hover:not([disabled]) {
      background: var(--md-primary-fg-color--light, #e3f2fd);
      border-color: var(--md-primary-fg-color, #1976d2);
    }
    .vl-demo-chip[disabled] { opacity: 0.5; cursor: wait; }
    .vl-demo-status {
      font-size: 12px;
      font-style: italic;
      color: var(--md-default-fg-color--light, #999);
      margin-left: 4px;
    }
  </style>

  <div class="vl-demo-tabs" id="vl-demo-tabs">
    <button class="vl-demo-tab active" data-tab="vehicle-specification" disabled>Specification</button>
    <button class="vl-demo-tab" data-tab="vehicle-sale-information" disabled>Sale Information</button>
    <button class="vl-demo-tab" data-tab="vehicle-accessories" disabled>Accessories</button>
    <button class="vl-demo-tab" data-tab="vehicle-warranty-details" disabled>Warranty &amp; SSC</button>
    <button class="vl-demo-tab" data-tab="vehicle-service-history" disabled>Service History</button>
    <button class="vl-demo-tab" data-tab="vehicle-paint-thickness" disabled>Paint Thickness</button>
    <button class="vl-demo-tab" data-tab="vehicle-claimable-items" disabled>Claimable Items</button>
  </div>

  <div class="vl-demo-vins" id="vl-demo-vins">
    <span class="vl-demo-vins-label">Try a VIN:</span>
    <button class="vl-demo-chip" data-vin="JTMHX01J8L4198293" disabled>JTMHX01J8L4198293</button>
    <button class="vl-demo-chip" data-vin="JTMW43FV10D123456" disabled>JTMW43FV10D123456</button>
    <span class="vl-demo-status" id="vl-demo-status">Loading components&hellip;</span>
  </div>

  <vehicle-lookup
    id="demo-vehicle-lookup"
    language="en"
    disable-vin-validation="true"
    active-element="vehicle-specification">
  </vehicle-lookup>

  <script>
    (function () {
      var el = document.getElementById('demo-vehicle-lookup');
      var tabs = document.querySelectorAll('#vl-demo-tabs .vl-demo-tab');
      var chips = document.querySelectorAll('#vl-demo-vins .vl-demo-chip');
      var status = document.getElementById('vl-demo-status');
      var defaultVin = 'JTMHX01J8L4198293';
      var mockData = null;

      function setActive(tag) {
        el.activeElement = tag;
        tabs.forEach(function (t) {
          t.classList.toggle('active', t.getAttribute('data-tab') === tag);
        });
      }

      function loadVin(vin) {
        if (!mockData || !mockData[vin]) return;
        // Distribute the DTO to all children directly (activeElement=null means
        // every child receives the data, including the currently visible one).
        // This bypasses the wrapper's own mock-loading code path which has a
        // layout-thrashing scrollTop bug in adp-web-components 0.1.97.
        try { el.handleLoadData(mockData[vin], null); } catch (e) { /* ignore */ }
      }

      tabs.forEach(function (t) {
        t.addEventListener('click', function () { setActive(t.getAttribute('data-tab')); });
      });
      chips.forEach(function (c) {
        c.addEventListener('click', function () { loadVin(c.getAttribute('data-vin')); });
      });

      // Pre-fetch the mock JSON ourselves (mirrors the leaf-component demo pages)
      var mockPromise = fetch('../demo-data/standard-dealer/vehicle-lookup.json')
        .then(function (r) { return r.json(); })
        .then(function (data) { mockData = data; });

      // Pin the wrapper's internal body scrollTop to 0. Background:
      // adp-web-components 0.1.97's shift-tab-content has a layout-thrashing bug
      // where it scrolls .vehicle-info-body to a stale offset whenever child
      // sizes change, pushing the active tab off-screen. The active child is in
      // normal flow at position 0, so 0 is the correct value — we just keep
      // forcing it back to 0 whenever the buggy code moves it.
      var rawScrollSetter = Object.getOwnPropertyDescriptor(Element.prototype, 'scrollTop').set;
      var pinnedBody = null;
      function forceZero() {
        if (pinnedBody && pinnedBody.scrollTop !== 0) rawScrollSetter.call(pinnedBody, 0);
      }
      function pinScrollTop() {
        var body = el.shadowRoot && el.shadowRoot.querySelector('.vehicle-info-body');
        if (!body) return false;
        pinnedBody = body;
        // Reset on every animation frame for 4 seconds (covers initial settle).
        var frames = 0;
        (function tick() {
          forceZero();
          if (frames++ < 240) requestAnimationFrame(tick);
        })();
        // After the burst, watch for size changes (tab switches, VIN changes)
        // and reset again.
        if (window.ResizeObserver) {
          var ro = new ResizeObserver(function () {
            forceZero();
            // Also reset across the next few frames since layout settles async
            for (var i = 1; i <= 6; i++) setTimeout(forceZero, i * 50);
          });
          ro.observe(body);
          var content = body.querySelector('.vehicle-info-content');
          if (content) ro.observe(content);
        }
        return true;
      }

      // Wait for both: custom element defined AND mock fetched
      Promise.all([
        window.customElements && customElements.whenDefined
          ? customElements.whenDefined('vehicle-lookup')
          : new Promise(function (r) { setTimeout(r, 500); }),
        mockPromise
      ]).then(function () {
        // Poll for .vehicle-info-body to appear (wrapper's componentDidLoad has run)
        return new Promise(function (resolve) {
          var tries = 0;
          (function check() {
            if (pinScrollTop() || ++tries > 50) resolve();
            else setTimeout(check, 50);
          })();
        });
      }).then(function () {
        tabs.forEach(function (t) { t.removeAttribute('disabled'); });
        chips.forEach(function (c) { c.removeAttribute('disabled'); });
        if (status) status.textContent = '';
        loadVin(defaultVin);
      });
    })();
  </script>
</div>

---

## Usage

The wrapper renders one child at a time based on `activeElement`. Your page supplies the tab UI and
updates `activeElement` on click. A single VIN search populates all children.

```html
<!-- Your own tab bar -->
<nav>
  <button onclick="lookup.activeElement = 'vehicle-specification'">Specification</button>
  <button onclick="lookup.activeElement = 'vehicle-sale-information'">Sale Information</button>
  <button onclick="lookup.activeElement = 'vehicle-accessories'">Accessories</button>
  <button onclick="lookup.activeElement = 'vehicle-warranty-details'">Warranty &amp; SSC</button>
  <button onclick="lookup.activeElement = 'vehicle-service-history'">Service History</button>
  <button onclick="lookup.activeElement = 'vehicle-paint-thickness'">Paint Thickness</button>
  <button onclick="lookup.activeElement = 'vehicle-claimable-items'">Claimable Items</button>
</nav>

<vehicle-lookup
  id="lookup"
  language="en"
  base-url="https://your-api.com/"
  active-element="vehicle-specification">
</vehicle-lookup>

<script>
  const lookup = document.getElementById('lookup');
  lookup.fetchVin('JTMHX01J8L4198293');
</script>
```

---

## Properties

| Property               | Attribute                | Type                      | Default | Description                                                                                           |
|------------------------|--------------------------|---------------------------|---------|-------------------------------------------------------------------------------------------------------|
| `activeElement`        | `active-element`         | `string`                  | `''`    | Tag of the child to render. One of `vehicle-specification`, `vehicle-sale-information`, `vehicle-accessories`, `vehicle-warranty-details`, `vehicle-service-history`, `vehicle-paint-thickness`, `vehicle-claimable-items`. |
| `baseUrl`              | `base-url`               | `string`                  | `''`    | Base URL for the vehicle lookup API. Passed down to the active child.                                  |
| `isDev`                | `is-dev`                 | `boolean`                 | `false` | Enables development mode. Loads mock data from `mockUrl` (or the published CDN mocks) instead of hitting the API. |
| `mockUrl`              | `mock-url`               | `string`                  | `''`    | Custom URL for the mock data file. Only used when `isDev` is `true`. If empty, the wrapper loads the mock file published with the NPM package. |
| `language`             | `language`               | `string`                  | `'en'`  | Language code for localization (`en`, `ku`, `ar`, `ru`).                                               |
| `disableVinValidation` | `disable-vin-validation` | `boolean`                 | `false` | Disables VIN format validation on the active child.                                                    |
| `queryString`          | `query-string`           | `string`                  | `''`    | Extra query string appended to API requests.                                                           |
| `childrenProps`        | `children-props`         | `string` &#124; `object`  | &mdash; | JSON string (or object) of per-child prop overrides. See below.                                        |
| `errorStateListener`   | &mdash;                  | `(error: string) => void` | &mdash; | Callback invoked whenever the wrapper's error message changes.                                         |
| `loadingStateChanged`  | &mdash;                  | `(isLoading: boolean) => void` | &mdash; | Callback invoked whenever any child enters / leaves the loading state.                             |
| `dynamicClaimActivate` | &mdash;                  | `(vehicle: VehicleLookupDTO) => void` | &mdash; | Callback fired when a user activates a dynamic claim from the claimable items tab.       |

---

## Methods

| Method                                   | Description                                                                                      |
|------------------------------------------|--------------------------------------------------------------------------------------------------|
| `fetchVin(vin, headers?)`                | Fetches data for the given VIN via the active child. Other children are hydrated from the response. |
| `handleLoadData(response, activeChild?)` | Distributes a pre-loaded `VehicleLookupDTO` to the children without making an HTTP call.         |
| `setBlazorRef(ref)`                      | Registers a `DotNetObjectReference` so Blazor hosts can receive callbacks by name.               |

---

## Passing Props to Children (`childrenProps`)

Some child components accept extra props that are not part of the wrapper's own surface (for
example, the `recaptchaKey` for warranty details, or the `claimEndPoint` for dynamic claim).
Use `childrenProps` to forward them:

```html
<vehicle-lookup
  active-element="vehicle-specification"
  base-url="https://your-api.com/"
  children-props='{
    "vehicle-warranty-details": {
      "showSsc": true,
      "recaptchaKey": "YOUR_RECAPTCHA_SITE_KEY"
    },
    "vehicle-claimable-items": {
      "claimEndPoint": "https://your-api.com/service-claim",
      "claim-via-barcode-scanner": "false"
    }
  }'>
</vehicle-lookup>
```

The top-level keys must be the child tag name. Any props not listed here fall back to the
wrapper's defaults.
