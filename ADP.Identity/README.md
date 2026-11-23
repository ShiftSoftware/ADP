# Sample identity servers

The ShiftIdentity servers the menus and claimable-items samples sign in with, one per shape. Each is a
ShiftIdentity API with a separate Blazor app for sign-in and the identity dashboard. The API keeps its users, companies, branches and
countries in **SQLite in memory**: the database is created and seeded at startup and is gone when the server stops,
so every run starts from the same data.

| Shape | API | Sign-in app and dashboard | Samples that trust it |
|---|---|---|---|
| Single country | `SingleCountry/ADP.Identity.Sample.SingleCountry.API`, `http://localhost:5105` | `SingleCountry/ADP.Identity.Sample.SingleCountry.Web`, `http://localhost:5106` | menus and claimable items, single country |
| Multi country | `MultiCountry/ADP.Identity.Sample.MultiCountry.API`, `http://localhost:5223` | `MultiCountry/ADP.Identity.Sample.MultiCountry.Web`, `http://localhost:5224` | menus and claimable items, multi country |

The `.Web` project is a Blazor WebAssembly app of its own, not served by the API; its
`wwwroot/appsettings.Development.json` points it at the API. Run both:

```bash
dotnet run --project ADP.Identity/SingleCountry/ADP.Identity.Sample.SingleCountry.API
dotnet run --project ADP.Identity/SingleCountry/ADP.Identity.Sample.SingleCountry.Web
```

The samples call the API (`ShiftIdentity:BaseUrl`) and send users to the sign-in app to sign in
(`ShiftIdentity:FrontEndBaseUrl`).

The Aspire AppHost (`ADP.Aspire/ADP.Aspire.AppHost`) runs all four projects, with the APIs started before the samples.

## Seed data

`identity-seed.json` in each API holds the data, read by `SeedShiftIdentitySqliteAsync`. The org data follows a
real deployment of each shape: countries, regions, cities, companies, branches, brands, departments and services
keep that deployment's IDs, company types and links (each branch's company, city, brands, departments and
services). Real data keyed by those IDs, such as imported menus or vehicles, therefore lines up. Every company and
branch name, code, phone number and address is invented. The users, teams, access tree and apps are the sample's
own. Every user's password is `OneTwo`, and `SuperUser` exists on both servers.

- **Single country:** Iraq, a distributor, its dealers and 51 branches. Brands: Toyota 1, Lexus 2, Hino 3, Other 4.
- **Multi country:** Uzbekistan, Turkmenistan and Tajikistan (country IDs 3, 4 and 5), plus the UAE and Japan, a
  regional distributor, its dealers and 14 branches. Brands: Toyota 2, Lexus 3.

Each server's identity hash-id salt is the one its deployment uses, so hashed IDs match too. The `Administrator` access tree grants every
registered action tree, including the Menu and Claimable Items trees.

Each sample Blazor app is registered twice, because it runs at two addresses. Under Aspire it has an address of
its own (for example `menus-sample` on `http://localhost:5094`). Run on its own, its API serves it, and it signs in
with the `-api` app ID (`menus-sample-api` on `http://localhost:5134`).

## Settings the samples copy

A sample that trusts one of these servers needs the following values from that server's
`appsettings.Development.json`:

- `Settings:TokenSettings:Issuer` and `PublicKey`, which the sample sets as `ShiftIdentity:Issuer` and
  `ShiftIdentity:PublicKey`.
- `Settings:IdentityHashIdSettings:Salt` and `MinHashLength`, which the sample sets as
  `ShiftIdentity:HashIds:Salt` and `ShiftIdentity:HashIds:MinHashLength`.

The keys and salts are development values only.
