# Service contracts sample: multi country

A host for the Claimable Items module's service contracts, shaped like a distributor that sells in **several
countries**. The contract form asks which country's menu prices the contract and for the transfer rate, which
the menu lookup uses to scale the consumable charge. The [SingleCountry](../SingleCountry/README.md) sample is the
other shape; apart from the countries, the two are the same, and that README has the full detail.

The vehicle and the contract menus come from the [multi-country menus sample](../../../ADP.Menus/samples/MultiCountry):
its lookup functions (`Sample:MenuLookupUrl`, `http://localhost:7072`), which get the country and the transfer rate
the user picked, and its API (`Sample:MenusApiUrl`, `http://localhost:5136`) for the model pickers.

- `ADP.ClaimableItems.Sample.MultiCountry.API`: the API on `http://localhost:5171`. It also serves the Blazor
  client, so this is the only project to run.
- `ADP.ClaimableItems.Sample.MultiCountry.Web`: the Blazor WebAssembly client.

## Settings

As in SingleCountry, the `appsettings.Development.json` files hold the local identity server's development values,
copied from its own development settings.

### API: `appsettings.Development.json`

`ShiftIdentity:Issuer`, `ShiftIdentity:PublicKey` and `ShiftIdentity:HashIds` as in SingleCountry, and
`Sample:Countries`: the countries the menus are priced for, each `{ "Id": <its ID on the identity server>,
"Name": "..." }`. With one or none configured the sample behaves like SingleCountry. `Sample:MenuLookupUrl`,
`Sample:MenuLookupFunctionKey` and `Sample:MenusApiUrl` are as in SingleCountry, and the Aspire AppHost supplies the
two URLs. The optional settings are the same as SingleCountry's.

### Blazor client: `wwwroot/appsettings.Development.json`

`ShiftIdentity:AppId` (`claimable-items-sample-dev`, registered on the identity server with
`http://localhost:5171` as its redirect), `ShiftIdentity:BaseURL` and `ShiftIdentity:FrontEndBaseURL`.

## Run

```bash
dotnet run --project ADP.ClaimableItems.Sample.MultiCountry.API
```

Open `http://localhost:5171`, sign in, and go to **Service Contracts**. After the VIN and the mileage, pick the
country and enter the transfer rate; the menu reloads for them.

`ADP.ClaimableItems.Sample.MultiCountry.API.http` has the API calls. Permissions and migrations work as in
SingleCountry.
