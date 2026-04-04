# Warranty Details

The `<vehicle-warranty-details>` component displays warranty information, including standard warranty dates, extended warranty, and optionally Safety Service Campaigns (SSCs).

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a VIN:</strong>
    <button onclick="document.getElementById('demo-warranty-details').fetchVin('JTMHX01J8L4198293')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMHX01J8L4198293</button>
    <button onclick="document.getElementById('demo-warranty-details').fetchVin('JTMW43FV10D123456')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMW43FV10D123456</button>
  </p>

  <vehicle-warranty-details show-warranty="true" show-ssc="true" id="demo-warranty-details" language="en"></vehicle-warranty-details>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/vehicle-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-warranty-details');
          el.isDev = true;
          el.setMockData(mockData);
        });
    });
  </script>
</div>

---

## Standalone Usage

```html
<vehicle-warranty-details
  base-url="https://your-api.com/"
  show-warranty="true"
  show-ssc="true"
  language="en">
</vehicle-warranty-details>
```

When used inside `<vehicle-lookup>`, no additional props are needed.

---

## Properties

| Property               | Attribute                | Type      | Default | Description                                          |
|------------------------|--------------------------|-----------|---------|------------------------------------------------------|
| `isDev`                | `is-dev`                 | `boolean` | `false` | Enables development mode                              |
| `baseUrl`              | `base-url`               | `string`  | `''`    | Base URL for the vehicle lookup API                    |
| `language`             | `language`               | `string`  | `'en'`  | Language code for localization                          |
| `showWarranty`         | `show-warranty`          | `boolean` | `false` | Show warranty start/end date cards                     |
| `showSsc`              | `show-ssc`               | `boolean` | `false` | Show Safety Service Campaign section                   |
| `disableVinValidation` | `disable-vin-validation` | `boolean` | `false` | Disables VIN format validation                         |
| `queryString`          | `query-string`           | `string`  | `''`    | Additional query string for API requests               |
| `coreOnly`             | `core-only`              | `boolean` | `false` | Renders a slim layout without the search input         |

---

## Data Displayed

- **Warranty:** Start date, end date, active warranty status
- **Extended Warranty:** Start date, end date (when available)
- **Free Service Start Date**
- **SSCs:** Campaign code, description, repair status, labor codes, part numbers
