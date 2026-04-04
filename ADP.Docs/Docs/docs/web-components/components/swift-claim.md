# Swift Claim

The `<vehicle-claimable-items>` component displays service items that can be claimed for a vehicle, handles claim submission, and tracks claim status.

## Live Demo

<div markdown="0">
  <script type="module" src="https://cdn.jsdelivr.net/npm/adp-web-components@0.1.85/dist/shift-components/shift-components.esm.js"></script>

  <p style="margin-bottom:8px">
    <strong>Try a VIN:</strong>
    <button onclick="document.getElementById('demo-swift-claim').fetchVin('JTMHX01J8L4198293')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMHX01J8L4198293</button>
    <button onclick="document.getElementById('demo-swift-claim').fetchVin('JTMW43FV10D123456')" style="cursor:pointer;padding:4px 12px;margin:4px;border:1px solid #ccc;border-radius:4px;background:#f5f5f5">JTMW43FV10D123456</button>
  </p>

  <vehicle-claimable-items id="demo-swift-claim" language="en"></vehicle-claimable-items>

  <script>
    document.addEventListener('DOMContentLoaded', function () {
      fetch('../demo-data/standard-dealer/vehicle-lookup.json')
        .then(function (res) { return res.json(); })
        .then(function (mockData) {
          var el = document.getElementById('demo-swift-claim');
          el.isDev = true;
          el.setMockData(mockData);
        });
    });
  </script>
</div>

---

## Standalone Usage

```html
<vehicle-claimable-items
  base-url="https://your-api.com/"
  language="en">
</vehicle-claimable-items>
```

When used inside `<vehicle-lookup>`, no additional props are needed.

---

## Properties

| Property                             | Attribute                                | Type      | Default                      | Description                                                |
|--------------------------------------|------------------------------------------|-----------|------------------------------|------------------------------------------------------------|
| `isDev`                              | `is-dev`                                 | `boolean` | `false`                      | Enables development mode with simulated claim responses      |
| `baseUrl`                            | `base-url`                               | `string`  | `''`                         | Base URL for the vehicle lookup API                          |
| `language`                           | `language`                               | `string`  | `'en'`                       | Language code for localization                                |
| `disableVinValidation`               | `disable-vin-validation`                 | `boolean` | `false`                      | Disables VIN format validation                                |
| `queryString`                        | `query-string`                           | `string`  | `''`                         | Additional query string for API requests                      |
| `coreOnly`                           | `core-only`                              | `boolean` | `false`                      | Renders a slim layout without the search input                |
| `claimEndPoint`                      | `claim-end-point`                        | `string`  | `'api/vehicle/swift-claim'`  | API endpoint for claim submission                             |
| `uploadMultipleDocumentsAtTheForm`   | `upload-multiple-documents-at-the-form`  | `boolean` | `true`                       | Allow multiple document uploads per claim                     |
| `maximumDocumentFileSizeInMb`        | `maximum-document-file-size-in-mb`       | `number`  | `30`                         | Maximum file size for document uploads (MB)                   |

---

## Dev Mode Behavior

When `is-dev="true"`, the component simulates claim submission:

- File upload progress is animated
- Claim responses are randomly generated (approved, rejected, or pending)
- No actual API calls are made
