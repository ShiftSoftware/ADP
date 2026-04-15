# ADP.Menus

An **automotive service menu** (parts & labour) module for the ADP platform. Models vehicle service menus, variants, parts, labour rates, service intervals and produces Excel reports — shipped as four NuGet packages you plug into any Shift-based ASP.NET Core API and Blazor WebAssembly app.

| Package | Purpose |
|---|---|
| [`ShiftSoftware.ADP.Menus.Shared`](ADP.Menus.Shared) | DTOs, validators, action tree, country/language contracts |
| [`ShiftSoftware.ADP.Menus.Data`](ADP.Menus.Data) | EF Core entities, AutoMapper profiles, export services |
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

## Sample app

A working end-to-end sample lives in [`samples/ADP.Menus.Sample.API`](samples/ADP.Menus.Sample.API) and [`samples/ADP.Menus.Sample.Web`](samples/ADP.Menus.Sample.Web). It hosts ShiftIdentity internally (login with `SuperUser` / `OneTwo`) and seeds demo data on first run.

## License

Apache-2.0 © Shift Software
