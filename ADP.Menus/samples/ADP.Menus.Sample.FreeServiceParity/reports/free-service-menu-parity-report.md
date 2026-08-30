# Free Service Items vs Free Menu Items — Parity by Menu Code

Generated 2026-08-28 08:24 UTC in 289s. Regenerate any time with `dotnet run --project ADP.Menus/samples/ADP.Menus.Sample.FreeServiceParity -- --duckdb <store>`; the detail rows are in [free-service-menu-parity-details.csv](free-service-menu-parity-details.csv).

## The question

The service-items system is filled **by hand** from the exported menu; the menu definitions are
meant to **generate** those services automatically, and the menu lookup exists to remove the manual
step. Before switching over, both sides must provably produce the same FREE services. The identity
they share is the **menu code**: the service item's `PackageCode` was transcribed from the very
`Code` the menu generator produces — so this audit matches by menu code, and only by menu code.
The other properties are compared on matched pairs and reported, but a differing property never
breaks a code match: the codes matching is the parity that matters.

## How it was measured

- One **bulk vehicle lookup** per batch — the real `VehicleLookupService` pipeline over the DuckDB
  store, the service menu attached with `FreeFilter = FreeOnly`; both sides come out of the same
  `VehicleLookupDTO`.
- Free service items are deduplicated exactly as the service-items report does (best row per
  `ServiceItemID`; items with no id still count); **all statuses count** — an expired or claimed
  free item is still an entitlement the menu should generate.
- **Match**: the item's menu code equals a free menu line's generated `Code` (trimmed,
  case-insensitive); each line is consumable once.
- **Then compare** (reported, never match-breaking): mileage (`MaximumMileage` vs the line's
  interval KM), description (item name vs line description), and price (item cost vs line total,
  only when the item carries a cost).
- An item with **no menu code at all** is its own category — it cannot be matched to anything and
  is exactly the manual-entry gap this migration is meant to close.

Run parameters: database `DataSource=C:\mounts\adp-sync-agent-destination\company-data-write.duckdb;ACCESS_MODE=READ_ONLY`, language `en`, country `default`, broker-stock lookup `on`, every distinct VIN in the store.

## Verdict

**230,177** VINs answered, of 230,177 requested. (A VIN the store holds nothing about surfaces as an empty vehicle under `NoBasicModelCode` rather than being dropped, so the two counts usually agree.)

> **The menu side is empty everywhere.** Not one variant in this store's menus carries the
> free-of-charge flag, so `FreeFilter = FreeOnly` generates zero lines for every model. Until
> free variants are authored in the menus catalog, there is nothing on the menu side to match
> the free service items against.

| Outcome | VINs | Share | Meaning |
|---|---:|---:|---|
| Mismatch | 170,186 | 73.9 % | at least one entry on either side has no code match — see the CSV |
| NothingFree | 30,185 | 13.1 % | menu found, but no free items and no free menu lines |
| MenuNotFound | 29,806 | 12.9 % | no menu is authored under the VIN's derived basic model code |

| Totals | |
|---|---:|
| Free service items | 1,097,405 |
| Free menu lines | 0 |
| Matched by menu code, all properties agree | 0 |
| Matched by menu code, with property differences | 0 |
| Free items with NO menu code | 194,533 |
| Free items whose code matched no free menu line | 902,872 |
| Free menu lines no item's code matched | 0 |

## Where the mismatches are

Mismatching VINs grouped by their derived basic model code (top 20). A model-shaped cluster
points at menu authoring; a scatter points at per-VIN campaign data.

| Basic model code | Mismatching VINs | Items w/o menu code | Item codes unmatched | Menu lines unmatched |
|---|---:|---:|---:|---:|
| TGN121 | 39,538 | 33,699 | 219,984 | 0 |
| ZRE211 | 26,187 | 40,319 | 99,323 | 0 |
| TGN126 | 20,398 | 27,164 | 75,238 | 0 |
| GRJ300 | 20,041 | 20,514 | 114,617 | 0 |
| VJA300 | 8,415 | 9,304 | 40,715 | 0 |
| GGN125 | 8,021 | 9,519 | 36,877 | 0 |
| ZVG10 | 6,978 | 5,628 | 38,280 | 0 |
| GRJ150 | 6,345 | 11,557 | 10,070 | 0 |
| GRJ79 | 5,457 | 8,439 | 11,015 | 0 |
| AXAA54 | 3,941 | 4,579 | 15,361 | 0 |
| AXAH54 | 3,346 | 1,896 | 21,258 | 0 |
| GRJ200 | 2,714 | 5,385 | 0 | 0 |
| TJA250 | 2,603 | 2 | 31,768 | 0 |
| AXVA70 | 2,063 | 3,223 | 5,374 | 0 |
| AXVA80 | 2,048 | 0 | 16,956 | 0 |
| AZSH30 | 1,755 | 911 | 18,837 | 0 |
| VJA310 | 1,723 | 1,445 | 30,960 | 0 |
| GRH322 | 1,629 | 1,617 | 8,345 | 0 |
| TZSH35 | 825 | 331 | 9,017 | 0 |
| AXVH80 | 778 | 0 | 6,398 | 0 |

## Free items whose model has no menu at all

These VINs carry free service items, but no menu is authored under their derived basic model
code — nothing the menu side could ever generate for them (top 20 models by VIN count).

| Basic model code | VINs | Free items on them |
|---|---:|---:|
| AXUH78 | 3,062 | 21,682 |
| URJ202 | 1,010 | 1,996 |
| GRH320 | 601 | 4,511 |
| FG242XN | 543 | 4,594 |
| ZRE210 | 388 | 821 |
| ASV70 | 337 | 667 |
| F800LE | 260 | 509 |
| FG211X5 | 242 | 1,919 |
| AXAL64 | 221 | 2,028 |
| GRH303 | 198 | 1,241 |
| FG241XN | 175 | 1,508 |
| MXAA64 | 144 | 1,354 |
| AALH15 | 143 | 2,205 |
| GRJ76 | 137 | 1,168 |
| XZU710 | 132 | 256 |
| GRJ78 | 125 | 1,002 |
| VDJ200 | 85 | 170 |
| FG8JJ7A | 31 | 59 |
| N/A | 22 | 44 |
| VXFA50 | 21 | 345 |

## Reading the numbers honestly

- **Menu codes are language-dependent.** This run generated codes under `en`; a
  `PackageCode` transcribed from another language's export will not match — rerun with that
  `--language` before reading such misses as real.
- **A model with several free variants pools its lines.** Each variant contributes its full free
  line list; codes are matched across the pool, and a second free variant's unconsumed lines count
  as unmatched.
- **The audit runs with library-default `LookupOptions`** — no host resolvers, no warranty-period
  or distributor configuration. Item *statuses* (expired, activation-required) can differ from a
  production host; the *set* of items generally does not.
- **The store is as fresh as its last sync.** Both sides read the same DuckDB file, so the
  comparison is internally consistent even when the file lags the source systems.

## The detail file

`free-service-menu-parity-details.csv` — one row per match or unmatched entry: `MatchResult` ∈
`Matched` / `MatchedWithDifferences` / `FreeItemWithoutMenuCode` / `FreeItemCodeUnmatched` /
`MenuLineUnmatched`; the `Differences` column spells out property disagreements; item-side columns
(`ServiceItemId`…`ItemClaimDate`) and menu-side columns (`MenuVariantId`…`MenuTotalPrice`) are
filled when that side participates.
