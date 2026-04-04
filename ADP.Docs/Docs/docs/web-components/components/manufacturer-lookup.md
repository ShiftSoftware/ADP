# Manufacturer Lookup

The `<manufacturer-lookup>` component allows looking up parts directly from the manufacturer's system. It handles order submission and status tracking for manufacturer part lookups.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a part number:</strong>
    <button onclick="document.getElementById('demo-manufacturer').fetchData('SU00302474')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">SU00302474</button>
    <button onclick="document.getElementById('demo-manufacturer').fetchData('04152-YZZA1')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">04152-YZZA1</button>
  </p>

  <manufacturer-lookup id="demo-manufacturer" language="en"></manufacturer-lookup>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/part-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-manufacturer');
          el.isDev = true;
          el.setMockData(mockData);
        });
    });
  </script>
</div>

---

## Usage

### Bundle CDN

```html
<script type="module"
  src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js">
</script>

<manufacturer-lookup
  base-url="https://your-api.com/"
  language="en">
</manufacturer-lookup>
```

### With Mock Data (Development)

```html
<manufacturer-lookup
  is-dev="true"
  mock-url="/path/to/part-lookup.json"
  language="en">
</manufacturer-lookup>
```

---

## Properties

| Property          | Attribute          | Type      | Default | Description                                                |
|-------------------|--------------------|-----------|---------|------------------------------------------------------------|
| `isDev`           | `is-dev`           | `boolean` | `false` | Enables development mode with mock data                    |
| `mockUrl`         | `mock-url`         | `string`  | `''`    | URL to load mock JSON data from (used when `isDev` is true)|
| `language`        | `language`         | `string`  | `'en'`  | Language code for localization                              |
| `coreOnly`        | `core-only`        | `boolean` | `false` | Renders a slim layout without the search input             |
| `hiddenFields`    | `hidden-fields`    | `string`  | `''`    | Comma-separated list of fields to hide                     |
| `localizationName`| `localization-name`| `string`  | `''`    | Localization variant name                                  |

!!! note
    The manufacturer lookup requires `EnableManufacturerLookup` to be set in `LookupOptions` on the server side. When this is not enabled, the component will not display manufacturer-specific data.
