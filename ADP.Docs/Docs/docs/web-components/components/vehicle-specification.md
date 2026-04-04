# Vehicle Specification

The `<vehicle-specification>` component displays vehicle model details including engine, transmission, body type, fuel type, and drivetrain information.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a VIN:</strong>
    <button onclick="document.getElementById('demo-vehicle-specification').fetchVin('JTMHX01J8L4198293')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMHX01J8L4198293</button>
    <button onclick="document.getElementById('demo-vehicle-specification').fetchVin('JTMW43FV10D123456')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMW43FV10D123456</button>
  </p>

  <vehicle-specification id="demo-vehicle-specification" language="en"></vehicle-specification>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/vehicle-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-vehicle-specification');
          el.isDev = true;
          el.setMockData(mockData);
        });
    });
  </script>
</div>

---

## Standalone Usage

When used outside the `<vehicle-lookup>` wrapper, this component manages its own data fetching:

```html
<vehicle-specification
  base-url="https://your-api.com/"
  language="en">
</vehicle-specification>
```

When used inside `<vehicle-lookup>`, no additional props are needed &mdash; the wrapper handles data loading and distribution.

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

The specification component shows the following fields when available:

- Model description
- Body type
- Engine
- Fuel type
- Transmission
- Number of doors
- Steering side (LHD/RHD)
