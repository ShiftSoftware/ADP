# ADP.Menus

An **automotive service menu** (parts & labour) module for the ADP platform. Models vehicle service menus, variants, parts, labour rates, service intervals and produces Excel reports — shipped as four NuGet packages you plug into any Shift-based ASP.NET Core API and Blazor WebAssembly app.

| Package | Purpose |
|---|---|
| [`ShiftSoftware.ADP.Menus.Shared`](ADP.Menus.Shared) | DTOs, validators, action tree, country/language contracts |
| [`ShiftSoftware.ADP.Menus.Data`](ADP.Menus.Data) | EF Core entities, source-generated mappers, export services |
| [`ShiftSoftware.ADP.Menus.API`](ADP.Menus.API) | ASP.NET Core controllers + `AddMenuApiServices` registration |
| [`ShiftSoftware.ADP.Menus.Web`](ADP.Menus.Web) | Blazor WebAssembly pages + `AddMenuBlazorServices` registration |

All packages target **.NET 10**.

## Install

```bash
dotnet add package ShiftSoftware.ADP.Menus.API     # in your API project
dotnet add package ShiftSoftware.ADP.Menus.Web     # in your Blazor WASM project
```

`ADP.Menus.Data` and `ADP.Menus.Shared` come in transitively.

## Documentation

Full integration guide — registration, endpoints, country modes, authorization, domain model — lives in the ADP docs site under **Menus → Integration**. The conceptual overview of menus and menu generation is under **Menus → Intro / Menu Generation**.

Projecting the menu catalog into Cosmos DB for the vehicle lookup — provisioning, the save trigger, the catch-up backfill and how to verify it — is under **Menus → Cosmos Replication**.

## Sample app

Working end-to-end samples live in [`samples/SingleCountry`](samples/SingleCountry) (one country: the menu screens never ask for one) and [`samples/MultiCountry`](samples/MultiCountry) (several countries from `Sample:Countries`: the screens ask which country's prices apply and for the transfer rate). Each has an API, a Blazor front end and a Functions host. The API hosts ShiftIdentity internally (login with `SuperUser` / `OneTwo`) and creates its schema on first run. Demo data is not seeded at startup — load it when you want it with `dotnet test ADP.Menus/ADP.Menus.Tests --filter SampleDataSeeding`, which adds only the rows the database is missing; it targets the single-country database unless `ADP_MENUS_SAMPLE_SQL_CONNECTION` points it elsewhere. `ADP.Aspire/ADP.Aspire.AppHost` runs both samples together.

## License

Apache-2.0 © Shift Software
