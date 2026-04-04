# Service History

The `<vehicle-service-history>` component displays a vehicle's service history, including service visits with labor lines, part lines, mileage, and invoice details.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a VIN:</strong>
    <button onclick="document.getElementById('demo-service-history').fetchVin('JTMHX01J8L4198293')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMHX01J8L4198293</button>
    <button onclick="document.getElementById('demo-service-history').fetchVin('JTMW43FV10D123456')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMW43FV10D123456</button>
  </p>

  <vehicle-service-history id="demo-service-history" language="en"></vehicle-service-history>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/vehicle-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-service-history');
          el.isDev = true;
          el.setMockData(mockData);
        });
    });
  </script>
</div>

---

## Standalone Usage

```html
<vehicle-service-history
  base-url="https://your-api.com/"
  language="en">
</vehicle-service-history>
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

Each service record shows:

- Service type / job description
- Service date and mileage (odometer)
- Company and branch name
- Invoice and work order numbers
- Labor lines with codes and descriptions
- Part lines with part numbers and quantities
