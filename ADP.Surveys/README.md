# ADP.Surveys

A **dynamic survey** module for the ADP platform. Models surveys, screens, questions, a canonical Question Bank, reusable Screen Templates, cross-screen logic and `navigationList` branching — shipped as four NuGet packages you plug into any Shift-based ASP.NET Core API and Blazor WebAssembly app.

| Package | Purpose |
|---|---|
| [`ShiftSoftware.ADP.Surveys.Shared`](ADP.Surveys.Shared) | DTOs, validators, schema types, Question Bank + Screen Template contracts |
| [`ShiftSoftware.ADP.Surveys.Data`](ADP.Surveys.Data) | EF Core entities, AutoMapper profiles, response storage |
| [`ShiftSoftware.ADP.Surveys.API`](ADP.Surveys.API) | ASP.NET Core controllers + `AddSurveysApiServices` registration |
| [`ShiftSoftware.ADP.Surveys.Web`](ADP.Surveys.Web) | Blazor WebAssembly pages + `AddSurveysBlazorServices` registration |

All packages target **.NET 10**.

The public-facing survey **renderer** (React + Vite) lives separately and talks to the API's anonymous `/schema` + `/responses` endpoints. See the dynamic-surveys plan for the full design.

## Sample app

A working end-to-end sample lives in [`samples/ADP.Surveys.Sample.API`](samples/ADP.Surveys.Sample.API) and [`samples/ADP.Surveys.Sample.Web`](samples/ADP.Surveys.Sample.Web).

## Status

Phase 0 scaffold — projects exist and compile. Schema, entities, builder and renderer are built out in Phases 1–5.

## License

Apache-2.0 © Shift Software
