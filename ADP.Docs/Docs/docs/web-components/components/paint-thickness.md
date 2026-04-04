# Paint Thickness

The `<vehicle-paint-thickness>` component displays paint thickness inspection data with panel-by-panel measurements, inspection dates, and source information.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a VIN:</strong>
    <button onclick="document.getElementById('demo-paint-thickness').fetchVin('JTMHX01J8L4198293')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMHX01J8L4198293</button>
  </p>

  <vehicle-paint-thickness id="demo-paint-thickness" language="en"></vehicle-paint-thickness>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/vehicle-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-paint-thickness');
          el.isDev = true;
          el.setMockData(mockData);
        });
    });
  </script>
</div>

---

## Standalone Usage

```html
<vehicle-paint-thickness
  base-url="https://your-api.com/"
  language="en">
</vehicle-paint-thickness>
```

When used inside `<vehicle-lookup>`, no additional props are needed.

---

## Properties

| Property               | Attribute                | Type      | Default | Description                                          |
|------------------------|--------------------------|-----------|---------|------------------------------------------------------|
| `isDev`                | `is-dev`                 | `boolean` | `false` | Enables development mode                              |
| `baseUrl`              | `base-url`               | `string`  | `''`    | Base URL for the vehicle lookup API                    |
| `language`             | `language`               | `string`  | `'en'`  | Language code for localization                          |
| `disableVinValidation` | `disable-vin-validation` | `boolean` | `false` | Disables VIN format validation                         |
| `queryString`          | `query-string`           | `string`  | `''`    | Additional query string for API requests               |
| `coreOnly`             | `core-only`              | `boolean` | `false` | Renders a slim layout without the search input         |

---

## Data Displayed

Each inspection record includes:

- Inspection source (e.g., PDI) and date
- Model year, description, and color code
- Panel measurements:
    - Panel type (Hood, Fender, Door, TailGate, Roof)
    - Panel side (Left, Right, Center) and position (Front, Middle, Rear)
    - Measured thickness in microns
    - Inspection images (when available)
