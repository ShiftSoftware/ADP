# Free Service Items Matched into the Menus

Generated 2026-09-02 14:44 UTC in 209s. Regenerate any time with `dotnet run --project ADP.Menus/samples/ADP.Menus.Sample.FreeServiceParity -- --duckdb <store>`; the detail rows are in [free-service-menu-parity-details.csv](free-service-menu-parity-details.csv).

## The question

The service-items system is filled **by hand** from the exported menu; the menu definitions are
meant to **generate** those services automatically, and the menu lookup exists to remove the manual
step. Before switching over, every FREE service item must be findable in the menu. The identity
both sides share is the **menu code**: the item's `PackageCode` was transcribed from the very
`Code` the menu generator produces — so this audit looks each free item up among ALL of its
model's generated menu lines (every variant; the free-of-charge flag is not authored yet and is
not consulted). The audit is **one-way**: menu lines no free item points at are expected — the
menu also prices paid work — and are never counted against parity.

It is asked of **live vehicles only**. A VIN is compared only when at least one of its free service
items is still **pending**; one whose entitlements are all spent — processed, expired, cancelled —
or that carries none at all is skipped whole. Those were transcribed against older menu exports,
many of them without a code, and the menu side is not being asked to reproduce them.

## How it was measured

- One **bulk vehicle lookup** per batch — the real `VehicleLookupService` pipeline over the DuckDB
  store, the whole service menu attached (`FreeFilter = All`); both sides come out of the same
  `VehicleLookupDTO`.
- Free service items are deduplicated exactly as the service-items report does (best row per
  `ServiceItemID`; items with no id still count).
- **Scope gate — at least one PENDING free item.** A VIN whose free items are all processed,
  expired or cancelled, and a VIN with no free items at all, is skipped whole: no CSV rows, no
  share of any number below. Inside a VIN that IS in scope, **every** free item is compared
  whatever its own status — the pending one says the record is current, so its spent siblings are
  still evidence about the same transcription.
- **Match**: the item's menu code equals a generated line's `Code` (trimmed, case-insensitive).
  Lines are not consumed — a catalog line can answer any number of entitlements.
- **Then compare** (reported, never match-breaking): mileage (`MaximumMileage` vs the line's
  interval KM), description (item name vs line description), and price (item cost vs line total,
  only when the item carries a cost).
- An item with **no menu code at all** is its own category — it cannot be looked up, and is exactly
  the manual-entry gap this migration is meant to close.

Run parameters: database `DataSource=C:\mounts\adp-sync-agent-destination\company-data-write.duckdb;ACCESS_MODE=READ_ONLY`, language `en`, country `default`, broker-stock lookup `on`, every distinct VIN in the store.

## Verdict

**67,731** VINs compared, of 230,177 requested — 162,446 (70.6 %) skipped for carrying nothing pending, taking 460,757 spent free items out of the picture with them. Of the compared VINs' **636,648** free service items (328,997 of them still pending): **0** (0.0 %) matched a menu line with every property agreeing, **548,601** (86.2 %) matched with property differences, **6,242** (1.0 %) carry no menu code, and **81,805** (12.8 %) carry a code the menu did not generate.

| Scope | VINs | Share of requested |
|---|---:|---:|
| Requested | 230,177 | |
| Skipped — no free service items at all | 52,030 | 22.6 % |
| Skipped — free items, none pending | 110,416 | 48.0 % |
| **Compared** (≥ 1 pending free item) | **67,731** | 29.4 % |

The compared VINs' verdicts — shares are of that compared population:

| Outcome | VINs | Share | Meaning |
|---|---:|---:|---|
| MatchWithDifferences | 28,095 | 41.5 % | every free item found its menu line by code — with property differences to review in the CSV |
| Mismatch | 35,480 | 52.4 % | at least one free item has no code, or a code the menu did not generate — see the CSV |
| MenuNotFound | 4,156 | 6.1 % | the VIN has free items but no menu is authored under its derived basic model code |

| Totals (compared VINs only) | |
|---|---:|
| Free service items | 636,648 |
| …of them still pending | 328,997 |
| Matched, all properties agree | 0 |
| Matched, with property differences | 548,601 |
| Items with NO menu code | 6,242 |
| Items whose code the menu did not generate | 81,805 |
| Menu lines generated (context — most serve paid work) | 9,626,539 |
| Free items excluded with the skipped VINs | 460,757 |

## Where the unmatched items are

Mismatching VINs grouped by their derived basic model code (top 20). "Code unmatched" on a
model-shaped cluster points at menu authoring or code transcription drift; "no code" points at
service-item data entry.

| Basic model code | Mismatching VINs | Items w/o menu code | Item codes unmatched |
|---|---:|---:|---:|
| TGN121 | 11,983 | 1,166 | 15,322 |
| GRJ300 | 3,637 | 348 | 4,404 |
| TGN126 | 3,436 | 440 | 4,411 |
| ZRE211 | 2,776 | 715 | 2,475 |
| ZVG10 | 1,693 | 205 | 1,705 |
| VJA310 | 1,602 | 1,214 | 5,699 |
| VJA300 | 1,294 | 180 | 1,364 |
| GGN125 | 1,039 | 195 | 1,072 |
| TJA250 | 1,033 | 0 | 1,225 |
| AXVA80 | 924 | 0 | 1,248 |
| AZSH30 | 923 | 479 | 894 |
| AXAH54 | 821 | 110 | 804 |
| AXAA54 | 642 | 95 | 642 |
| VJH310 | 574 | 0 | 1,935 |
| TZSH35 | 421 | 175 | 388 |
| GRH322 | 386 | 24 | 521 |
| AXVH80 | 340 | 0 | 437 |
| FG212X5 | 304 | 5 | 358 |
| GRJ79 | 248 | 98 | 189 |
| GRJ150 | 229 | 203 | 86 |

## Matches whose properties differ

The identity holds — the item's code found its menu line — but mileage, description or price
disagrees. Filter the CSV to `MatchedWithDifferences` and read the `Differences` column.

| Basic model code | VINs affected | Differing matches |
|---|---:|---:|
| TGN121 | 17,820 | 129,130 |
| GRJ300 | 8,390 | 89,991 |
| ZRE211 | 6,385 | 49,091 |
| TGN126 | 5,270 | 38,411 |
| TJA250 | 2,211 | 27,408 |
| VJA300 | 2,762 | 26,575 |
| VJA310 | 1,602 | 23,137 |
| ZVG10 | 3,014 | 22,627 |
| GGN125 | 2,639 | 20,360 |
| AZSH30 | 1,406 | 15,482 |
| AXVA80 | 1,769 | 13,476 |
| AXAH54 | 1,675 | 12,660 |
| GRJ79 | 1,194 | 9,385 |
| AXAA54 | 1,199 | 9,054 |
| VJH310 | 574 | 8,397 |
| TZSH35 | 713 | 8,072 |
| VJH300 | 512 | 7,433 |
| AXVH80 | 677 | 5,153 |
| FG212X5 | 631 | 4,796 |
| GRH322 | 593 | 4,381 |

## Free items whose model has no menu at all

These VINs carry free service items, but no menu is authored under their derived basic model
code — nothing the menu side could ever generate for them (top 20 models by VIN count).

| Basic model code | VINs | Free items on them |
|---|---:|---:|
| AXUH78 | 1,800 | 14,521 |
| FG242XN | 543 | 4,594 |
| GRH320 | 502 | 4,029 |
| AXAL64 | 221 | 2,028 |
| FG211X5 | 209 | 1,675 |
| FG241XN | 174 | 1,500 |
| MXAA64 | 144 | 1,354 |
| AALH15 | 143 | 2,205 |
| GRJ76 | 131 | 1,120 |
| GRJ78 | 125 | 1,002 |
| GRH303 | 121 | 990 |
| VXFA50 | 15 | 247 |
| AXAL62 | 12 | 116 |
| GRJ71 | 10 | 80 |
| ZN8 | 5 | 40 |
| GXPA16 | 1 | 8 |

## Reading the numbers honestly

- **These are live-vehicle numbers, not fleet-wide ones.** VINs with nothing pending never entered
  the comparison; the scope table above says how many, and how many free items went with them.
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

`free-service-menu-parity-details.csv` — one row per free service item **on a compared VIN**
(skipped VINs appear nowhere in it): `MatchResult` ∈ `Matched` /
`MatchedWithDifferences` / `FreeItemWithoutMenuCode` / `FreeItemCodeUnmatched`; the `Differences`
column spells out property disagreements. The columns are laid out for reading: each compared pair
sits side by side, the item's value immediately left of the menu's —
`ItemMenuCode | MenuLineCode`, `ItemMaximumMileage | MenuIntervalKm`,
`ServiceItemName | MenuDescription`, `ItemCost | MenuTotalPrice` — followed by the item-only
context (`ServiceItemId`…`ItemClaimDate`) and the menu-only context (`MenuVariantId`…, including
`MenuVariantIsFree` for when the flag starts being authored).
