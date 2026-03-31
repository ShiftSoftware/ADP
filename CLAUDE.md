# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ADP (Automotive Dealer Platform) by ShiftSoftware. A multi-project .NET solution with a Stencil.js web components frontend. Published as NuGet packages (`ShiftSoftware.ADP.Models`, `ShiftSoftware.ADP.Lookup.Services`, `ShiftSoftware.ADP.SyncAgent`) and an NPM package (`adp-web-components`).

## Build & Test Commands

### .NET (from repo root)
```bash
dotnet build                                    # Build all projects
dotnet build --configuration Release            # Release build
dotnet test ADP.LookupServices.BDD              # Run BDD tests (Reqnroll/xUnit)
dotnet test ADP.Models/Models.Tests             # Run model unit tests
dotnet test ADP.LookupServices/Lookup.Services.Tests  # Run lookup service unit tests
dotnet pack ADP.Models/Models --configuration Release  # Pack Models NuGet
```

### Web Components (from `ADP.WebComponents/adp-web-components/`)
```bash
npm install                  # Install dependencies
npm run build                # Production build
npm start                    # Dev server with watch (port 3000)
npm test                     # Run spec tests (Jest)
npm run test.watch           # Watch mode tests
npm run format               # Prettier format all source files
npm run prettier             # Check formatting without writing
```

### Automation scripts (web components directory)
```bash
npm run create:type          # Generate a new TypeScript type file
npm run create:locale        # Generate a new locale
npm run update:locale        # Update existing locale
npm run delete:locale        # Delete a locale
npm run create:locale-mapper # Generate locale mapper
```

## Architecture

### .NET Projects
- **ADP.Models** — Shared DTOs and domain models (targets .NET Standard 2.0). Models decorated with `[TypeScriptModel]` are auto-generated to TypeScript.
- **ADP.LookupServices** — Business logic and data access layer. Uses Cosmos DB, SQL Server (EF Core), DuckDB, and Parquet files. Configured via `LookupOptions`.
- **ADP.SyncAgent** — Data synchronization engine. Uses Azure Storage (Blobs, File Shares), LibGit2Sharp, and Polly for resilience.
- **ADP.LookupServices.BDD** — BDD tests using Reqnroll (formerly SpecFlow) with xUnit v3. Feature files in `Features/`.

### Web Components (`ADP.WebComponents/adp-web-components/`)
Built with **Stencil.js** (namespace: `shift-components`), Tailwind CSS, and SCSS.

**Source layout under `src/`:**
- `components/` — Base UI components (cards, accordions, tabs, checkboxes, etc.)
- `form-elements/` — Form field components (inputs, selects, date pickers)
- `forms/` — Complex form compositions (general-inquiry, service-booking, vehicle-quotation)
- `part-lookup/` — Parts lookup feature
- `vehicle-lookup/` — Vehicle lookup feature
- `vin-extractor/` — VIN extraction utilities
- `features/` — Complex features (form hooks, mocks, multi-lingual, image viewer)
- `global/lib/` — Utilities (validation, DOM, API calls, formatting)
- `global/api/` — API endpoint configurations
- `global/types/` — TypeScript types (`generated/` subdir is auto-generated from C# models)
- `locales/` — Multi-language support files
- `templates/` — HTML/JSX templates

**Path aliases** (configured in `stencil.config.ts`):
`~api`, `~lib`, `~locales`, `~features`, `~types`, `~assets`

### Code Generation Pipeline
The `WebComponentModelGenerator` (C# console app) uses Roslyn to scan C# models with `[TypeScriptModel]` attribute and generates TypeScript types into `src/global/types/generated/`. Models with `[TypeScriptIgnore]` are excluded. This runs automatically post-build.

## Versioning
- .NET version: `ADPVersion` property in `GlobalSettings.props` (currently 1.7.8)
- Web components version: `version` field in `package.json` (currently 0.1.78)
- `GlobalSettings.props` also defines `ImportADPPackagesViaProjectReference=true` for local development

## CI/CD
- **NuGet pipeline** (`azure-pipeline.yml`): Triggered by `release-nuget-*` tags. Builds, runs BDD tests, packs and publishes NuGet packages.
- **Web components pipeline** (`ADP.WebComponents/adp-web-components/azure-pipelines.yml`): Triggered by `release-web-components-*` tags. Publishes to NPM.
- **Docs pipeline** (`.github/workflows/docs-gh-pages.yml`): Triggered by `release-docs-*` tags. Deploys mkdocs to GitHub Pages.

## Stencil.js Conventions
- Component tags use dash-case with one component per directory matching the tag name
- Shadow DOM enabled by default (`shadow: true`)
- Props are immutable; use `mutable: true` sparingly or pair with `@State`
- `@Method` decorated methods must be `async`
- Events use camelCase naming with typed `EventEmitter<T>`
- Wrap top-level render output in `<Host>` when setting attributes/classes on the custom element
- Test files use `.spec.ts` suffix for unit tests and `.e2e.ts` for E2E tests
- Use `newSpecPage` for unit tests and `newE2EPage` for E2E tests

## BDD Tests

BDD tests use Reqnroll (xUnit) to verify evaluator logic. Run with:
```bash
dotnet test ADP.LookupServices.BDD
```
