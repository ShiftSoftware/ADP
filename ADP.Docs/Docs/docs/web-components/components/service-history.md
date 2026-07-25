# Service History

`<vehicle-service-history>` displays a vehicle's service visits, including labor lines, part lines, mileage, and invoice details. It is a self-contained lookup component.

## Host responsibilities

The host loads the pinned component module, supplies its approved base URL and language, and calls `fetchVin(vin)`. It does not call the lookup endpoint, construct a request payload, parse the lookup DTO, or render service-history rows.

The component owns request handling, response interpretation, loading and error state, localization, and service-history presentation.

## Production sequence

1. Read `dist/integration-manifest.json` for the released package version.
2. Load the flat `dist/components/vehicle-service-history.js` module through the package host loader.
3. Wait for the `vehicle-service-history` custom element definition.
4. Configure properties and call `fetchVin(vin)`.

Do not mix this per-component module with the general `shift-components` bundle in one browser document.

## Properties

| Property               | Attribute                | Default | Description                                                                                          |
| ---------------------- | ------------------------ | ------- | ---------------------------------------------------------------------------------------------------- |
| `baseUrl`              | `base-url`               | `''`    | The host-approved vehicle lookup base URL. It must include the separator required by the host route. |
| `language`             | `language`               | `'en'`  | Component locale. The current component supports `en`, `ar`, `ku`, and `ru`.                         |
| `coreOnly`             | `core-only`              | `false` | Uses the slim component layout.                                                                      |
| `disableVinValidation` | `disable-vin-validation` | `false` | Disables built-in VIN validation.                                                                    |
| `queryString`          | `query-string`           | `''`    | Adds a host-controlled query string to a lookup request.                                             |

## Methods

| Method                      | Description                                     |
| --------------------------- | ----------------------------------------------- |
| `fetchVin(vin)`             | Looks up and renders service history for a VIN. |
| `setErrorMessage(errorKey)` | Shows a component-supported error state.        |

## Development fixtures

The source development template includes mocks and debugging controls. It is for package development only. Do not copy it into a production host. Use the published production template and integration manifest instead.
