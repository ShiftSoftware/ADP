# BDD Testing Expansion Plan

This directory contains the planning documents for expanding BDD test coverage across all evaluators in `ADP.LookupServices`, with a shared test data architecture that serves BDD tests, web component demos, and documentation.

## Current State

The BDD project is a proof of concept that tests only `VehicleAuthorizationEvaluator`. There are **14 evaluators** total that need coverage. Test data is fragmented across .NET (inline Gherkin), JavaScript (hand-crafted mocks), and documentation (no demos yet).

## Plan Documents

| Document | Description |
|----------|-------------|
| [STATUS.md](STATUS.md) | **Start here** — Phase-by-phase progress tracker with checkboxes and open decisions |
| [00-current-state-and-problems.md](00-current-state-and-problems.md) | Analysis of the current BDD approach and data fragmentation |
| [shared-data-architecture.md](shared-data-architecture.md) | **Core document** — shared test data strategy across .NET, web components, and docs |
| [01-phase0-foundation.md](01-phase0-foundation.md) | Foundation refactoring + shared data directory setup |
| [02-phase1-simple-evaluators.md](02-phase1-simple-evaluators.md) | BDD coverage for evaluators that only need CompanyDataAggregateModel |
| [03-phase2-lookup-options.md](03-phase2-lookup-options.md) | BDD coverage for evaluators that also need LookupOptions |
| [04-phase3-async-evaluators.md](04-phase3-async-evaluators.md) | BDD coverage for async evaluators with mocked resolver delegates |
| [05-phase4-storage-service.md](05-phase4-storage-service.md) | BDD coverage for evaluators needing IVehicleLoockupStorageService |
| [06-phase5-part-evaluators.md](06-phase5-part-evaluators.md) | BDD coverage for part lookup evaluators |
| [conventions.md](conventions.md) | Feature file naming, step definition patterns, and team conventions |

## Architecture Overview

```
ADP.TestData/environments/                 (canonical source — input only)
├── standard-dealer.json
├── broker-dealer.json
└── edge-cases.json

         │  Generator (dotnet run) reads input, runs evaluators,
         │  writes generated output INTO each consumer's source tree:
         │
         ├──► adp-web-components/src/features/mocks/data/generated/
         │    (committed, built into npm package, served via CDN)
         │
         ├──► ADP.Docs/Docs/docs/web-components/demo-data/
         │    (committed, deployed with docs to GitHub Pages)
         │
         └──► BDD tests read environment JSON directly (no generated output needed)
              (verify evaluator logic produces correct results from input)
```

Each project is self-contained — `npm start`, `mkdocs serve`, and `dotnet test` all work independently.

## Evaluator Complexity Tiers

| Tier | Dependencies | Evaluators | Phase |
|------|-------------|------------|-------|
| **Simple** | CompanyDataAggregateModel only | VehicleAuthorizationEvaluator | 0 |
| **Simple** | CompanyDataAggregateModel only | VehicleEntryEvaluator, VehicleIdentifierEvaluator, VehicleSSCEvaluator | 1 |
| **Medium** | + LookupOptions | WarrantyAndFreeServiceDateEvaluator | 2 |
| **Complex** | + IServiceProvider (mocked resolvers) | VehicleAccessoriesEvaluator, VehiclePaintThicknessEvaluator, VehicleServiceHistoryEvaluator | 3 |
| **Full** | + IVehicleLoockupStorageService | VehicleSaleInformationEvaluator, VehicleSpecificationEvaluator*, VehicleServiceItemEvaluator | 4 |
| **Part** | PartAggregateCosmosModel + LookupOptions + IServiceProvider (all internal) | PartPriceEvaluator, PartDeadStockEvaluator, PartStockEvaluator | 5 |

\* `VehicleSpecificationEvaluator` only needs `IVehicleLoockupStorageService` (no aggregate or options).

## Decisions

- **Mocking framework:** NSubstitute
- **Typo fixes:** Fix all typos in Phase 0
- **Data location:** Dedicated `ADP.TestData/` directory (shared across projects)
- **Data format:** JSON (input fixtures + generated output DTOs)
- **Environments:** 2-3 (standard-dealer, broker-dealer, edge-cases)
- **Data pipeline:** .NET evaluators run on environment JSON to generate output JSON for web components and docs

## How to Use These Documents

1. Start with [shared-data-architecture.md](shared-data-architecture.md) — it's the architectural foundation
2. Read phase documents in order (each builds on the previous)
3. After implementing each phase, run `dotnet test ADP.LookupServices.BDD` to verify
4. Update the phase document with lessons learned before moving on
