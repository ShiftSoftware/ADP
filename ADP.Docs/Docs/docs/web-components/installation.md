# Installation

ADP Web Components supports a pinned, per-component integration model. Load only the components a page needs, from one package version and one output family for the whole browser document.

## Required document contract

- Pin a released `adp-web-components` version. Do not use `@latest` in production.
- Use only flat per-component modules: `dist/components/<tag>.js`.
- Do not mix those modules with `dist/shift-components/shift-components.esm.js` in the same document.
- Wait for `customElements.whenDefined(tag)` before calling a component method.
- Upgrade every ADP component used by one browser document together.

The package publishes an integration manifest at `dist/integration-manifest.json`. It is generated during the package build from Stencil component metadata plus the ADP-owned stable integration contract. It describes the supported component tag, flat module path, host API, runtime assets, and the full emitted component API for the released package version.

## Browser host example

Replace the placeholders with your released package version and your own approved endpoint. The host configures and invokes the component. The component owns the request, response parsing, state, and rendering.

```html
<vehicle-service-history
  id="service-history"
  language="en"
  base-url="{{VEHICLE_LOOKUP_BASE_URL}}"
></vehicle-service-history>

<script type="module">
  import { loadComponent } from "https://cdn.jsdelivr.net/npm/adp-web-components@{{ADP_WEB_COMPONENTS_VERSION}}/dist/host-loader.js";

  const packageVersion = "{{ADP_WEB_COMPONENTS_VERSION}}";
  const component = document.getElementById("service-history");

  await loadComponent({
    moduleUrl: `https://cdn.jsdelivr.net/npm/adp-web-components@${packageVersion}/dist/components/vehicle-service-history.js`,
    tag: "vehicle-service-history",
    packageVersion,
  });

  component.fetchVin(vin);
</script>
```

The `host-loader` rejects a version or output-family mismatch in the same document and waits for element registration. It does not choose your endpoint or authentication policy.

## NPM imports

Bundlers can import the published entry points instead of a CDN URL:

```js
import { loadComponent } from "adp-web-components/host-loader";
import manifest from "adp-web-components/integration-manifest" with { type: "json" };
```

Use the manifest's `modulePath` when building a CDN URL. NPM export paths and CDN artifact paths are intentionally different.

## Templates

The package includes a generic production host template at `dist/templates/production-host/vehicle-service-history.html`. It contains no endpoint, mock response, sample identifier, or development flag. Development fixtures stay under the package's development templates.

## Next steps

- Read the [component list](components/components-list.md).
- Read the [Service History component reference](components/service-history.md).
- Read the [theming guide](theming.md) for visual customization.
