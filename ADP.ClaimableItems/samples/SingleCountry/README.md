# Service contracts sample: single country

A host for the Claimable Items module's service contracts, shaped like a distributor that sells in **one
country**. The server prices every contract for that country with a transfer rate of 1, so the contract form
shows neither field. The [MultiCountry](../MultiCountry/README.md) sample is the other shape.

- `ADP.ClaimableItems.Sample.SingleCountry.API`: the API on `http://localhost:5161`. It also serves the Blazor
  client, so this is the only project to run.
- `ADP.ClaimableItems.Sample.SingleCountry.Web`: the Blazor WebAssembly client.

## What it uses

| Piece | Where it comes from |
|---|---|
| Sign-in, users, companies, branches, countries | An existing **ShiftIdentity server** (external hosting). The API only validates its tokens. |
| The vehicle and its contract-type menu | The distributor's **vehicle lookup endpoint** (`Sample:VehicleLookupUrl`), through `Services/LookupEndpointContractMenuProvider.cs`. It asks for the contract menu, server to server. |
| Contracts | Its own SQL database, on the local SQL Server Express instance (`localhost\SQLEXPRESS`) by default; migrations run at start-up. |
| The one country | `Sample:Country`. Left `null`, the menu is priced with no country (country 0), as for a host with no country entity. |
| Names on the print | `Sample:Distributor` (dealers and branches print as "Dealer {id}" / "Branch {id}"). |

## Settings

The `appsettings.Development.json` files hold the local identity server's development values, copied from its own
development settings, so the sample runs as checked out. To point it at another identity server, change them; the
API refuses to start, naming what is missing, if they are blank.

### API: `appsettings.Development.json`

- `ShiftIdentity:Issuer` and `ShiftIdentity:PublicKey`: the identity server's token issuer and RSA public key.
- `ShiftIdentity:HashIds:Salt` and `MinHashLength`: the identity server's hash-id settings. They must match, or
  the company, branch and country IDs in its tokens and lists will not decode.
- `Sample:Country`: `{ "Id": <the country's ID on the identity server>, "Name": "..." }`, or `null`.
- `Sample:VehicleLookupUrl`: the vehicle lookup endpoint, with `{vin}` where the VIN goes. The provider adds
  `includeServiceMenu=true&menuTypes=Contract`. The endpoint prices the menu itself, so no country or transfer rate
  is sent.
- `Sample:VehicleLookupFunctionKey`: the endpoint's function key, sent as `x-functions-key`. Leave it empty
  locally, where function keys are not checked.

Optional: `ConnectionStrings:SQLServer`, `Sample:ContractMinimumLines`, `Sample:ContractPeriodicMileageMargin`,
`Sample:TimeZone` (a Windows or IANA time zone ID) and the `Sample:Distributor` fields.

### Blazor client: `wwwroot/appsettings.Development.json`

- `ShiftIdentity:AppId`: `claimable-items-sample-dev`, which must be registered on the identity server with
  `http://localhost:5161` as its redirect.
- `ShiftIdentity:BaseURL`: the identity server's API, ending in `/api/`.
- `ShiftIdentity:FrontEndBaseURL`: the identity server's sign-in site.

Run the identity server locally as well; the sample signs in on it.

## Run

```bash
dotnet run --project ADP.ClaimableItems.Sample.SingleCountry.API
```

Open `http://localhost:5161`, sign in, and go to **Service Contracts**. Enter a VIN that the lookup knows and
whose model has a contract-type menu, then the mileage. Run the vehicle lookup endpoint locally too.
[test-vins.md](../test-vins.md) lists VINs with a contract-type menu in the local Cosmos emulator the endpoint
reads.

`ADP.ClaimableItems.Sample.SingleCountry.API.http` has the API calls, for a token copied from the browser.

## Permissions

The module's own action tree is not on the identity server, so the sample turns the contract permissions off:
any signed-in user may create, issue, cancel and print. A host that adds the module's nodes to its access trees
sets `EnableClaimableItemsActionTreeAuthorization`, `ClaimableItemContractsAction`, `IssueContractAction`,
`CancelContractAction` and `PrintContractAction` in both the API and the Web options.

## Migrations

`Migrations/` holds the Claimable Items and Cases tables, under the `ClaimableItems` and `Cases` schemas. After a
module model change:

```bash
dotnet ef migrations add <Name> --project ADP.ClaimableItems.Sample.SingleCountry.API
```
