# Dead Stock Lookup

The `<dead-stock-lookup>` component displays dead stock information for parts, grouped by company and branch. It is a standalone part-lookup component with its own data loading.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a part number:</strong>
    <button onclick="document.getElementById('demo-dead-stock').fetchData('SU00302474')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">SU00302474</button>
    <button onclick="document.getElementById('demo-dead-stock').fetchData('04152-YZZA1')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">04152-YZZA1</button>
  </p>

  <dead-stock-lookup id="demo-dead-stock" language="en"></dead-stock-lookup>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/part-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-dead-stock');
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

<dead-stock-lookup
  base-url="https://your-api.com/"
  language="en">
</dead-stock-lookup>
```

### With Mock Data (Development)

```html
<dead-stock-lookup
  is-dev="true"
  mock-url="/path/to/part-lookup.json"
  language="en">
</dead-stock-lookup>
```

---

## Properties

| Property    | Attribute    | Type      | Default | Description                                                |
|-------------|--------------|-----------|---------|-----------------------------------------------------------|
| `isDev`     | `is-dev`     | `boolean` | `false` | Enables development mode with mock data                    |
| `mockUrl`   | `mock-url`   | `string`  | `''`    | URL to load mock JSON data from (used when `isDev` is true)|
| `language`  | `language`   | `string`  | `'en'`  | Language code for localization                              |
| `coreOnly`  | `core-only`  | `boolean` | `false` | Renders a slim layout without the search input             |

---

## Mock Data Format

When using `is-dev="true"` with `mock-url`, the JSON file should be a dictionary keyed by part number:

```json
{
  "SU00302474": {
    "partNumber": "SU00302474",
    "partDescription": "CLAMP",
    "deadStock": [
      {
        "companyName": "City Auto",
        "branches": [
          {
            "branchName": "Downtown Branch",
            "quantity": 8
          }
        ]
      }
    ]
  }
}
```

See the [standard-dealer](../demo-data/standard-dealer/part-lookup.json) and [broker-dealer](../demo-data/broker-dealer/part-lookup.json) demo data files for complete examples.
