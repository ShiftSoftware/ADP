# Service contracts sample: multi country

A host for the Claimable Items module's service contracts, shaped like a distributor that sells in **several
countries**. The contract form asks which country's menu prices the contract and for the transfer rate, which
the menu lookup uses to scale the consumable charge. The [SingleCountry](../SingleCountry/README.md) sample is the
other shape; apart from the countries, the two are the same, and that README has the full detail.

**The vehicle lookup is not wired yet.** `Services/LookupEndpointContractMenuProvider.cs` throws
`NotImplementedException`, so the contract form cannot load a vehicle until it is implemented against a
multi-country vehicle lookup endpoint (sending the country and the transfer rate the user picked).

- `ADP.ClaimableItems.Sample.MultiCountry.API`: the API on `http://localhost:5171`. It also serves the Blazor
  client, so this is the only project to run.
- `ADP.ClaimableItems.Sample.MultiCountry.Web`: the Blazor WebAssembly client.

## Settings

As in SingleCountry, the `appsettings.Development.json` files hold the local identity server's development values,
copied from its own development settings.

### API: `appsettings.Development.json`

`ShiftIdentity:Issuer`, `ShiftIdentity:PublicKey` and `ShiftIdentity:HashIds` as in SingleCountry, and
`Sample:Countries`: the countries the menus are priced for, each `{ "Id": <its ID on the identity server>,
"Name": "..." }`. With one or none configured the sample behaves like SingleCountry. The optional settings are the
same as SingleCountry's.

### Blazor client: `wwwroot/appsettings.Development.json`

`ShiftIdentity:AppId` (`claimable-items-sample-dev`, registered on the identity server with
`http://localhost:5171` as its redirect), `ShiftIdentity:BaseURL` and `ShiftIdentity:FrontEndBaseURL`.

## Run

```bash
dotnet run --project ADP.ClaimableItems.Sample.MultiCountry.API
```

Open `http://localhost:5171`, sign in, and go to **Service Contracts**. After the VIN and the mileage, pick the
country and enter the transfer rate; the menu reloads for them (once the provider is implemented).

`ADP.ClaimableItems.Sample.MultiCountry.API.http` has the API calls. Permissions and migrations work as in
SingleCountry.
