# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ADP (Automotive Dealer Platform) by ShiftSoftware. A multi-project .NET solution with a Stencil.js web components frontend. Published as NuGet packages (`ShiftSoftware.ADP.Models`, `ShiftSoftware.ADP.Lookup.Services`, `ShiftSoftware.ADP.SyncAgent`) and an NPM package (`adp-web-components`).

> **This is a public, generic, multi-tenant repo — keep it client-agnostic.** Never name a specific client
> (or its URLs, hostnames, internal system/repo names, warehouse/branch codes, or action-tree namespaces) in
> code, comments, doc comments, test/scenario names, sample/mock data, or commit messages. Describe the *shape*
> of the behaviour instead (e.g. "some deployments store parts T-prefixed"). Client-specific facts belong in
> private planning, not here. Hosts consume these packages from their own private repos.

## Build & Test Commands

### .NET (from repo root)
```bash
dotnet build                                    # Build all projects
dotnet build --configuration Release            # Release build
dotnet test ADP.LookupServices.BDD              # Run BDD tests (Reqnroll/xUnit)
# NOTE: ADP.Models/Models.Tests currently executes NOTHING. It has no test-framework
# reference and <Compile Remove>s all three of its own files, so `dotnet test` on it
# reports "No test is available" and EXITS 0 - green in any scripted loop. The two
# [Fact]s it contains are dead: they target ShiftSoftware.ADP.Models.DealerData, a
# namespace deleted in the Phase 1-3 refactors, and CacheableCSVEngine has since moved
# to ADP.SyncAgent. ADP.Models is covered by the compiler and by its consumers' suites.
dotnet test ADP.Models/Models.Tests             # NO-OP today - see the note above
dotnet test ADP.LookupServices/Lookup.Services.Tests  # Run lookup service unit tests
dotnet pack ADP.Models/Models --configuration Release  # Pack Models NuGet
```

### Web Components (from `ADP.WebComponents/adp-web-components/`)
```bash
npm install                  # Install dependencies
npm run build                # Production build
npm start                    # Dev server + template watchers (port 3000)
npm run start:stencil        # Dev server only, no template watchers
npm run build:templates      # One-off build of the dev-showcase assets
npm run watch:templates      # Showcase stylesheet watcher only
npm test                     # Run spec tests (Jest)
npm run test.watch           # Watch mode tests
npm run typecheck            # tsc --noEmit
npm run lint                 # ESLint (flat config in eslint.config.mjs)
npm run lint:fix             # ESLint with --fix
npm run format               # Prettier format all source files
npm run prettier             # Check formatting without writing
npm run release              # Build the public integration site into ./website
npm run preview              # Serve that built site on :3335 (--mount= to test a subpath)
npm run deploy               # wrangler deploy (CI does this; local needs `wrangler login`)
```

`npm start` runs `automation/dev.mjs`, which builds the showcase assets, then runs the
Tailwind and catalog watchers alongside the Stencil dev server. `npm run build:templates`
is only needed if you edited `src/templates` with the dev server down.

Lint/format policy: ESLint owns code correctness, Prettier owns layout — there is no
formatting rule in the ESLint config, so run `npm run lint:fix` then `npm run format`.
Generated output (`src/components.d.ts`, `src/locale-mapper.ts`,
`src/global/types/generated/`, `dist/`, `loader/`, `www/`) is excluded from linting.

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
- `global/types/` — TypeScript types (`generated/` subdir is auto-generated from C# models)
- `locales/` — Multi-language support files
- `templates/` — Dev-only showcase pages (see below)

### Dev showcase (`src/index.html` + `src/templates/`)
Dev-only pages that demonstrate each component. They are styled by a **compiled**
Tailwind 4 + daisyUI 5 stylesheet (`templates/harness.css`) and driven by Alpine — all
served locally, never from a CDN. Controls come from `templates/harness.js`; the page
index is generated into `templates/catalog.json` by scanning `src/templates`.

`harness.css`, `templates/vendor/` and `catalog.json` are **generated and gitignored** —
`npm start` builds them. Do not commit them, and do not hand-edit `harness.css`; its
source is `templates/harness.src.css`.

Conventions when touching these pages:
- daisyUI semantic tokens only (`bg-base-200`, `text-base-content`) — never raw Tailwind
  palette (`text-gray-800`, `from-blue-50`), which ignores the theme.
- The brand gold is a fill, not a text colour: `bg-primary`/`btn-primary` for fills,
  `text-accent` for anything that must read as a colour on a light page.
- Do not infer the brand colour from existing component CSS — the most common hexes there
  are Bootstrap 3 defaults, not branding.

### Public site build (`npm run release`)
`automation/build-website.mjs` turns the showcase into a static site under `website/`
(gitignored). It resolves the **published** npm version (not `package.json`, which is
the version being prepared), repoints `/build/…` at the CDN, drops the dead `nomodule`
tag, and rewrites `/templates/…` asset paths depth-relative so the site works at any
mount. Links and the catalog fetch are not rewritten — `harness.js` and `nav.js` resolve
those against `import.meta.url`, so dev and the built site share one code path.

Only pages that opt in ship. Each template declares it:

```html
<meta name="adp-publish" content="true" />
```

It is an allow-list: a page that says nothing stays off the public site. A directory
under `templates/` whose pages are all unpublished is pruned entirely, so its mock data,
fixtures and form structures do not ship either. `--all` builds everything for preview;
`--version=`, `--local`, `--out=`, `--base-url=` and `--mount=` are also available.

`src/404.html` ships alongside the landing page — a static host serves it for any
unmatched path, which here is usually a demo that exists but is not published yet. It is
the one page that gets **mount-absolute** asset paths rather than depth-relative ones,
because it is served at every URL rather than at its own; that is what `--mount=` is for.

### Landing page copy and languages
All landing-page and 404 text lives in `src/templates/site-locales.js` — `en`, `ar`, `ku`,
`ru`, matching the components’ own locale set. A user-visible literal string in the HTML
is a string that cannot be translated, so there are none. The file is a blocking script,
like `harness-theme.js`, because text direction is layout and correcting it after first
paint is a visible jump. The non-English copy is machine-written and wants a native pass.

Arabic and Kurdish render in **Speda Bold**. The font file is not in the repo — the
`@font-face` tries `local()` first and then `assets/fonts/speda-bold.woff2`, falling back
to Noto Kufi Arabic. See `src/templates/assets/fonts/README.md`.

Full design language, rules and migration plan:
`.shift/repos/adp/web-components/templates-design-language.md`

State invariants for the vehicle-lookup panels — what a panel may assert about a vehicle that is
not loaded yet, or that the distributor has no records for (an empty list is never a verdict):
`.shift/repos/adp/web-components/vehicle-lookup-invariants.md`. Read it before touching a panel's
empty, idle or unauthorized state, or a fixture.

Motion — **nothing on screen appears or disappears without a transition**, a loading state must not
presume the outcome, and every state pair (not only idle → loaded) is a transition to design. The
owner reviews every component change against this, with pairs of still + mid-transition
screenshots. The rule, its reasoning, the shared primitives (`src/components/style/lookup-motion.css`)
and the review checklist: `.shift/repos/adp/web-components/motion.md`. Read it before adding a
state to a component or changing what a state renders.

**Path aliases** (configured in `stencil.config.ts`):
`~lib`, `~locales`, `~features`, `~types`, `~assets`

### Code Generation Pipeline
The `WebComponentModelGenerator` (C# console app) uses Roslyn to scan C# models with `[TypeScriptModel]` attribute and generates TypeScript types into `src/global/types/generated/`. Models with `[TypeScriptIgnore]` are excluded. This runs automatically post-build.

## Versioning
- .NET version: `ADPVersion` property in `GlobalSettings.props`
- Web components version: `version` field in `package.json`
- `GlobalSettings.props` also defines `ImportADPPackagesViaProjectReference=true` for local development

## CI/CD
- **NuGet pipeline** (`azure-pipeline.yml`): Triggered by `release-nuget-*` tags. Builds, runs BDD tests, packs and publishes NuGet packages.
- **Web components pipeline** (`ADP.WebComponents/adp-web-components/azure-pipelines.yml`): Triggered by `release-web-components-*` tags. Publishes to NPM, then waits for registry propagation and runs `npm run purge` to flush the `@latest` jsDelivr URLs. The purge step is `continueOnError` — the publish is already irreversible by then, so a CDN hiccup warns instead of failing the release.
- **Docs pipeline** (`.github/workflows/docs-gh-pages.yml`): Triggered by `release-docs-*` tags. Deploys mkdocs to GitHub Pages at `https://adp-docs.shift.software` — the custom domain comes from `ADP.Docs/Docs/docs/CNAME`, which every deploy copies over (GitHub's own Pages setting is overwritten by it).
- **Website pipeline** (`ADP.WebComponents/adp-web-components/azure-pipelines-website.yml`): Triggered by `release-website-*` tags. Builds the integration site against the `package.json` version and deploys it to Cloudflare at `https://adp.shift.software` — see below.


### Cloudflare deploy
The site lives at **`https://adp.shift.software`** — a custom domain bound to the Worker in the
Cloudflare dashboard (Worker → Settings → Domains & Routes), which is also why the Worker has to
sit in the account that owns the `shift.software` zone. The pipeline passes `--site-url` so the
canonical tag, `robots.txt` and the sitemap name that address rather than the `workers.dev` one.

The site is an **assets-only Worker** — `wrangler.jsonc` has no `main`, so Cloudflare serves
`website/` straight from the edge without invoking JavaScript. `not_found_handling` is
`404-page`, which is what makes `src/404.html` answer unmatched paths with a real 404.

**It deploys on a `release-website-*` tag**, from the third Azure pipeline
(`ADP.WebComponents/adp-web-components/azure-pipelines-website.yml`). Nothing deploys on push.
The pipeline reads the version from `package.json`, runs `npm run release -- --version=<it>
--wait=600` — which keeps checking for up to ten minutes that the version is on npm and on the
CDN, then fails rather than pin an older one — and runs `wrangler deploy` with
`CLOUDFLARE_API_TOKEN` (an API token with *Workers Scripts: Edit*) and `CLOUDFLARE_ACCOUNT_ID`
from the `Deployment` variable group.

Release order: tag `release-web-components-x.y.z`, let it publish, then tag
`release-website-<n>`. The site has its own counter, so page-only changes ship with the second
tag alone.

Cloudflare's own Workers Builds git integration is **disconnected**. It could only build on
push, and the version-bump commit reaches `master` before the package is published, so every
push-build pinned the previous version. If it is ever reconnected: the Worker name in the
dashboard must match `name` in `wrangler.jsonc`, and the build image ships Yarn 4, which
migrates the committed Yarn 1 lockfile and then rejects it (`YN0028`) — `"packageManager":
"yarn@1.22.22"` in `package.json` exists for that reason.

A preview without promoting: `npm run release` locally, then `npm run deploy:preview`
(`wrangler versions upload`).

`website/_headers` and `robots.txt` are **written by the build**, not committed — the output
directory is wiped on every run. `_headers` deliberately sets no `Cache-Control` (assets are
not content-hashed) and no CSP (inline scripts plus an unknown operator origin would make it
either useless or breaking); see the note in `automation/prerender.mjs`.

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
