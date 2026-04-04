# Distributor Lookup

The `<distributor-lookup>` component displays distributor stock availability for parts, including quantity, location, and pricing information. It is a standalone part-lookup component.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a part number:</strong>
    <button onclick="document.getElementById('demo-distributor').fetchData('SU00302474')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">SU00302474</button>
    <button onclick="document.getElementById('demo-distributor').fetchData('04152-YZZA1')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">04152-YZZA1</button>
  </p>

  <distributor-lookup id="demo-distributor" language="en"></distributor-lookup>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/part-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-distributor');
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

<distributor-lookup
  base-url="https://your-api.com/"
  language="en">
</distributor-lookup>
```

### With Mock Data (Development)

```html
<distributor-lookup
  is-dev="true"
  mock-url="/path/to/part-lookup.json"
  language="en">
</distributor-lookup>
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

---

## Data Displayed

For each part number, the component shows:

- Part description and product group
- Distributor purchase price
- Stock availability by warehouse location with quantities
- Regional pricing (retail, purchase, warranty prices)
- Supersession information (superseded to / from)
