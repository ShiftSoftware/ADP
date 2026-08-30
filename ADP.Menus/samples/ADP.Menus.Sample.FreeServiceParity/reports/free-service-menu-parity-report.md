# Free Service Items Matched into the Menus

Generated 2026-08-30 11:18 UTC in 193s. Regenerate any time with `dotnet run --project ADP.Menus/samples/ADP.Menus.Sample.FreeServiceParity -- --duckdb <store>`; the detail rows are in [free-service-menu-parity-details.csv](free-service-menu-parity-details.csv).

## The question

The service-items system is filled **by hand** from the exported menu; the menu definitions are
meant to **generate** those services automatically, and the menu lookup exists to remove the manual
step. Before switching over, every FREE service item must be findable in the menu. The identity
both sides share is the **menu code**: the item's `PackageCode` was transcribed from the very
`Code` the menu generator produces — so this audit looks each free item up among ALL of its
model's generated menu lines (every variant; the free-of-charge flag is not authored yet and is
not consulted). The audit is **one-way**: menu lines no free item points at are expected — the
menu also prices paid work — and are never counted against parity.

## How it was measured

- One **bulk vehicle lookup** per batch — the real `VehicleLookupService` pipeline over the DuckDB
  store, the whole service menu attached (`FreeFilter = All`); both sides come out of the same
  `VehicleLookupDTO`.
- Free service items are deduplicated exactly as the service-items report does (best row per
  `ServiceItemID`; items with no id still count); **all statuses count** — an expired or claimed
  free item is still an entitlement the menu should generate.
- **Match**: the item's menu code equals a generated line's `Code` (trimmed, case-insensitive).
  Lines are not consumed — a catalog line can answer any number of entitlements.
- **Then compare** (reported, never match-breaking): mileage (`MaximumMileage` vs the line's
  interval KM), description (item name vs line description), and price (item cost vs line total,
  only when the item carries a cost).
- An item with **no menu code at all** is its own category — it cannot be looked up, and is exactly
  the manual-entry gap this migration is meant to close.

Run parameters: database `DataSource=C:\mounts\adp-sync-agent-destination\company-data-write.duckdb;ACCESS_MODE=READ_ONLY`, language `en`, country `default`, broker-stock lookup `on`, every distinct VIN in the store.

## Verdict

**230,177** VINs answered, of 230,177 requested. Of their **1,097,405** free service items: **0** (0.0 %) matched a menu line with every property agreeing, **773,630** (70.5 %) matched with property differences, **194,533** (17.7 %) carry no menu code, and **129,242** (11.8 %) carry a code the menu did not generate.

| Outcome | VINs | Share | Meaning |
|---|---:|---:|---|
| MatchWithDifferences | 34,674 | 15.1 % | every free item found its menu line by code — with property differences to review in the CSV |
| Mismatch | 135,512 | 58.9 % | at least one free item has no code, or a code the menu did not generate — see the CSV |
| NoFreeItems | 52,030 | 22.6 % | the VIN carries no free service items — nothing to look up |
| MenuNotFound | 7,961 | 3.5 % | the VIN has free items but no menu is authored under its derived basic model code |

| Totals | |
|---|---:|
| Free service items | 1,097,405 |
| Matched, all properties agree | 0 |
| Matched, with property differences | 773,630 |
| Items with NO menu code | 194,533 |
| Items whose code the menu did not generate | 129,242 |
| Menu lines generated (context — most serve paid work) | 30,632,484 |

## Where the unmatched items are

Mismatching VINs grouped by their derived basic model code (top 20). "Code unmatched" on a
model-shaped cluster points at menu authoring or code transcription drift; "no code" points at
service-item data entry.

| Basic model code | Mismatching VINs | Items w/o menu code | Item codes unmatched |
|---|---:|---:|---:|
| TGN121 | 32,537 | 33,699 | 33,226 |
| ZRE211 | 21,043 | 40,319 | 6,993 |
| TGN126 | 18,153 | 27,164 | 14,413 |
| GRJ300 | 14,556 | 20,514 | 5,386 |
| VJA300 | 6,395 | 9,304 | 1,948 |
| GRJ150 | 6,128 | 11,557 | 329 |
| GGN125 | 6,039 | 9,519 | 3,762 |
| ZVG10 | 5,228 | 5,628 | 2,619 |
| GRJ79 | 4,485 | 8,439 | 230 |
| AXAA54 | 3,193 | 4,579 | 1,003 |
| GRJ200 | 2,714 | 5,385 | 0 |
| AXAH54 | 2,238 | 1,896 | 1,315 |
| AXVA70 | 1,820 | 3,223 | 190 |
| VJA310 | 1,723 | 1,445 | 6,096 |
| GRH322 | 1,338 | 1,617 | 792 |
| AZSH30 | 1,256 | 911 | 1,103 |
| TJA250 | 1,242 | 2 | 1,450 |
| AXVA80 | 1,093 | 0 | 1,465 |
| VJH310 | 575 | 0 | 1,943 |
| AXVH71 | 575 | 922 | 109 |

## Matches whose properties differ

The identity holds — the item's code found its menu line — but mileage, description or price
disagrees. Filter the CSV to `MatchedWithDifferences` and read the `Differences` column.

| Basic model code | VINs affected | Differing matches |
|---|---:|---:|
| TGN121 | 26,190 | 186,758 |
| GRJ300 | 11,025 | 109,231 |
| ZRE211 | 12,294 | 92,330 |
| TGN126 | 8,560 | 60,825 |
| VJA300 | 4,408 | 38,767 |
| ZVG10 | 4,803 | 35,661 |
| GGN125 | 4,403 | 33,115 |
| TJA250 | 2,603 | 30,318 |
| VJA310 | 1,723 | 24,864 |
| AXAH54 | 2,670 | 19,943 |
| AZSH30 | 1,619 | 17,734 |
| AXVA80 | 2,048 | 15,491 |
| AXAA54 | 1,927 | 14,358 |
| GRJ79 | 1,391 | 10,785 |
| GRJ150 | 1,334 | 9,741 |
| TZSH35 | 760 | 8,597 |
| VJH310 | 575 | 8,407 |
| GRH322 | 1,050 | 7,553 |
| VJH300 | 512 | 7,433 |
| AXVH80 | 778 | 5,880 |

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
- **Menu lines without a free item are not misses.** The menu prices the whole service programme,
  paid work included; only the free service items' side is being audited.
- **The audit runs with library-default `LookupOptions`** — no host resolvers, no warranty-period
  or distributor configuration. Item *statuses* (expired, activation-required) can differ from a
  production host; the *set* of items generally does not.
- **The store is as fresh as its last sync.** Both sides read the same DuckDB file, so the
  comparison is internally consistent even when the file lags the source systems.

## The detail file

`free-service-menu-parity-details.csv` — one row per free service item: `MatchResult` ∈ `Matched` /
`MatchedWithDifferences` / `FreeItemWithoutMenuCode` / `FreeItemCodeUnmatched`; the `Differences`
column spells out property disagreements; the menu-side columns (`MenuVariantId`…`MenuTotalPrice`,
including `MenuVariantIsFree` for when the flag starts being authored) describe the line the item's
code found.
