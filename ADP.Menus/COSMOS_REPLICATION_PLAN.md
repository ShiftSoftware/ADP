# Menu → Cosmos Replication — Implementation Plan

> Status: **plan / not yet implemented.** Agreed design for projecting the service-menu catalog into
> Cosmos DB so a vehicle lookup can turn a **basic model code** into a set of **menu codes + prices**.
>
> **Chosen model: read-time generation, with a shared source-agnostic generation service.**
> We replicate the menu tables into Cosmos as flat, per-row, itemType-discriminated documents sharing
> the **basic model code** as their partition key. The menu codes/prices are generated **on lookup** by
> a *base generation service* whose input is a **neutral generic model** — the same service the DMS
> export uses. Each consumer adapts its own data in and out; only the generation logic + its generic
> contract are shared.
>
> This repo is public, generic and multi-tenant. Every example value here (`ABC12`, `4471`, brand
> `Z`, …) is synthetic. Never put real client codes, hostnames, system names or branch/warehouse
> codes into code, comments, tests or sample data.

---

## 1. What we are building, and how

The menu **code** and line **prices** are a *fold* across ~10 tables, produced today by
[`MenuExportService.GenerateMenuLines`](ADP.Menus.Data/DataServices/MenuExportService.cs) for the DMS
export. The requirement: codes served from the lookup are **identical** to that export.

### 1.1 Three model layers — keep them separate

This is the central design idea (and the correction to an earlier draft that fused layers 1 and 2):

| # | Layer | Lives in | Purpose |
|---|---|---|---|
| **1** | **Generic generation contract** — input models, the `MenuCodeGenerator` service, the generic result | **`ADP.Menus.Generation`** (NEW, netstandard2.0, menu-owned) | the *shared logic*; source- and sink-agnostic. Knows nothing of EF, Cosmos or reports |
| **2** | **Cosmos sync models** — flat, per-row, itemType docs | `ADP.Models` (netstandard2.0) `…/Service/Cosmos/` | *persistence only*: what replication writes and the lookup reads |
| **3** | **Consumer models** — the export's report rows; the lookup's DTOs | their own projects | each consumer's own shape |

The generation logic is menu-specific, so it lives in a **menu-owned** project — but that project must be
`netstandard2.0` (§1.2), so it is a **new** project (`ADP.Menus.Generation`), *not* the existing
`ADP.Menus.Shared`, which is `net10.0` and pulls FluentValidation + validators that would not multi-target
cleanly to `netstandard2.0`.

**The generation service is the only shared logic.** Its input is the **layer-1 generic model** — never
the Cosmos models, never EF entities. Every consumer aggregates its own data *into* the generic input and
maps the generic result *out* to its own models:

```
 DMS export:   EF entities ──aggregate──▶ [generic input] ──MenuCodeGenerator──▶ [generic result] ──map──▶ report rows ─▶ Excel
 Lookup:       Cosmos docs ──aggregate──▶ [generic input] ──MenuCodeGenerator──▶ [generic result] ──map──▶ lookup DTOs
 Replication:  EF entities ──map (per row)──▶ Cosmos sync models         (no generation — persistence only)
```

Because both the export and the lookup call the **same** `MenuCodeGenerator` over the **same** generic
contract, "identical to the export" is structural. The parameters and objects each side passes are its
own; only the logic is shared.

### 1.2 Why the generation must be `netstandard2.0`

| Project | Target |
|---|---|
| `ADP.LookupServices/Lookup.Services` | **netstandard2.0** |
| `ADP.Models` | **netstandard2.0** |
| `ADP.Menus.Shared`, `ADP.Menus.Data` | net10.0 |

The lookup cannot reference a `net10.0` assembly, so the shared generation must be `netstandard2.0`. The
existing `ADP.Menus.Shared` is `net10.0` (FluentValidation 12 + validators), so we add a **new**
`netstandard2.0` menu project, **`ADP.Menus.Generation`**, for layer 1. Layer 2 (Cosmos models) stays in
`ADP.Models` (`netstandard2.0`). Both are reachable by the lookup *and* by the `net10.0` menus host
(net10.0 can reference netstandard2.0). The DMS export is refactored to call the same layer-1 service so
the two paths cannot drift.

`ADP.Menus.Generation` references `ADP.Models` (one-way) so it can also host the `CosmosToGenerationAggregator`
(§5). It is dependency-light — POCO models + pure logic + the small ported text helpers — so it does **not**
pull in FluentValidation, EF, or ShiftEntity. The lookup gains one new, deliberate, minimal dependency on it.

### 1.3 Replication mechanism

`ShiftSoftware.ShiftEntity.CosmosDbReplication`, as already used by
[`ADP.ClaimableItems`](../ADP.ClaimableItems) and [`ADP.WarrantyClaims`](../ADP.WarrantyClaims). Each
menu table is its own simple, **manual**, per-row `Replicate` into a layer-2 Cosmos model. No fold at
write time; no `UpdateReference` recompute cascade.

### 1.4 Locked decisions

| Decision | Choice | Reason |
|---|---|---|
| Generation timing | **read-time** | simple per-row replication; raw data reusable |
| Generation input | **neutral generic model** (layer 1), not Cosmos/EF types | one shared service, many adapters |
| Storage | one container, per-row docs, `ItemType`-discriminated, partitioned by `/BasicModelCode` | one query returns a whole model's graph |
| Document `id` | the source row's DB id | soft-delete safe |
| Shared reference tables | replicated to a `__REF__` partition, **cached** in the reader | keeps replication pure per-row (no denormalization fan-out) |
| Mapping | **manual** everywhere (no AutoMapper) | explicit, reviewable |
| Fields | menu code, labour code, labour rate/allowed-time/consumable/labour-total, parts (number/qty/price/line-total), parts-total, discount, menu-total | DMS margin/cost/profit excluded |

---

## 2. Where the code lives

```
ADP.Menus/ADP.Menus.Generation/  (NEW — netstandard2.0, menu-owned, reachable by the lookup)
  Generation/                            # LAYER 1 — the shared contract + logic
    MenuGenerationRequest.cs             #   generic input (nested variant graph + reference data)
    MenuGenerationConfig.cs              #   country / transferRate / language / usePrimaryLabourRate
    GeneratedMenuLine.cs                 #   generic result (core fields only)
    MenuCodeGenerator.cs                 #   the fold, ported from MenuExportService (source-agnostic)
    MenuTextHelpers.cs                   #   ported GetAllowedTimeText + LocalizedText (netstandard2.0, single source)
  Cosmos/
    CosmosToGenerationAggregator.cs      #   static: Cosmos docs → MenuGenerationRequest (references ADP.Models)

ADP.Models/  (netstandard2.0 — reachable by the lookup AND the menus host)
  Models/Service/Cosmos/                 # LAYER 2 — persistence only (sync targets / lookup source)
    MenuVariantCosmosModel.cs  MenuPeriodCosmosModel.cs
    MenuLabourCosmosModel.cs   MenuItemCosmosModel.cs
    ServiceIntervalCosmosModel.cs  ServiceIntervalGroupCosmosModel.cs
    LabourRateMappingCosmosModel.cs  BrandMappingCosmosModel.cs
  Models/Constants/NoSQLConstants.cs     # EDIT — Containers.ServiceMenus + PartitionKeys + __REF__
  Models/ModelTypes.cs                   # EDIT — one PartitionedItemType per itemType

ADP.Menus/ADP.Menus.Data/  (net10.0 — the export + the replication producer)
  ADP.Menus.Data.csproj                  # EDIT — add CosmosDbReplication + ADP.Models refs
  Entities/*.cs                          # EDIT — replicated tables implement IShiftEntityReplication
  Repositories/*Repository.cs            # EDIT — IShiftEntityPrepareForReplicationAsync where denormalizing
  DataServices/
    MenuExportService.cs                 # EDIT — EF → generic input → MenuCodeGenerator → report rows
    EfToGenerationAggregator.cs          # NEW — EF entities → MenuGenerationRequest (export's adapter)
  Replication/
    MenuCosmosMappers.cs                 # NEW — manual EF-entity → Cosmos model (per table)
    MenuReplicationService.cs            # NEW — reusable replicate: per-table + all (backfill)
  Extensions/
    MenuReplicationExtensions.cs         # NEW — one Add*Replication per table + AddMenuReplications

ADP.LookupServices/Lookup.Services/  (netstandard2.0 — the reader, FUTURE phase)
  Services/ServiceMenuReferenceCache.cs  # NEW — caches the __REF__ partition
  Services/ServiceMenuLookupService.cs   # NEW — read partition → CosmosToGenerationAggregator → generate → lookup DTO
```

### 2.1 New project `ADP.Menus.Generation` (netstandard2.0, published package)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <Import Project="..\..\GlobalSettings.props" />
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <RootNamespace>ShiftSoftware.ADP.Menus.Generation</RootNamespace>
    <AssemblyName>ShiftSoftware.ADP.Menus.Generation</AssemblyName>
    <PackageId>ShiftSoftware.ADP.Menus.Generation</PackageId>
    <Version>$(ADPVersion)</Version>
    <IsPackable>true</IsPackable>
    <!-- + Title/Description/Icon/License to match sibling packable projects -->
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\ADP.Models\Models\Models.csproj"
                      Condition="Exists('..\..\ADP.Models\Models\Models.csproj')" />
    <PackageReference Include="ShiftSoftware.ADP.Models" Version="$(ADPVersion)"
                      Condition="!Exists('..\..\ADP.Models\Models\Models.csproj')" />
  </ItemGroup>
</Project>
```

Also wire it in like every other published project: add to `ADP.sln`, add a `dotnet pack` + push step in
`azure-pipeline.yml`, and copy the packable metadata block + icon from a sibling. Keep dependencies to a
minimum — POCO models + pure logic + the ported text helpers only; **no** FluentValidation / EF /
ShiftEntity. (Verify the ported helpers need nothing beyond BCL; `System.Text.Json` is available on
netstandard2.0 via package if `LocalizedText` keeps its JSON parsing.)

### 2.2 `ADP.Menus.Data.csproj` additions (mirror `ADP.ClaimableItems.Data.csproj`)

```xml
<PackageReference Include="ShiftSoftware.ShiftEntity.CosmosDbReplication" Version="2026.7.21.1" />
<ProjectReference Include="..\..\..\ADP.Models\Models\Models.csproj"
                  Condition="Exists('..\..\..\ADP.Models\Models\Models.csproj')" />
<PackageReference Include="ShiftSoftware.ADP.Models" Version="$(ADPVersion)"
                  Condition="!Exists('..\..\..\ADP.Models\Models\Models.csproj')" />
<!-- + the same conditional ProjectReference/PackageReference pair for ADP.Menus.Generation -->
```

`ADP.Menus.Data` (net10.0) references `ADP.Menus.Generation` (netstandard2.0) for the shared generator;
`ADP.LookupServices/Lookup.Services` (netstandard2.0) references it too.

---

## 3. Container structure

| Property | Value |
|---|---|
| Database | `NoSQLConstants.Databases.Services` |
| Container | `NoSQLConstants.Containers.ServiceMenus` = `"ServiceMenus"` |
| Partition key | **2-level hierarchical**: `/BasicModelCode` then `/ItemType` |
| Document `id` | the source row's DB id (as string); reference docs use a natural id |
| Throughput | dedicated container throughput (`Services` DB shares manual throughput with `ServiceItems`/`FlatRate`, both on the lookup path) |

**Model-scoped documents** (layer 2) — all share the model's `BasicModelCode` (L1):

| ItemType | From table | `id` |
|---|---|---|
| `MenuVariant` | `MenuVariant` (+ denormalized Menu/VehicleModel, country labour rates embedded) | variant id |
| `MenuPeriod` | `MenuPeriodicAvailability` | row id |
| `MenuLabour` | `MenuLabourDetails` | row id |
| `MenuItem` | `MenuItem` (+ `MenuItemPart` + prices embedded, + replacement-item slice) | item id |

**Reference documents** (layer 2) — small, shared, in the sentinel `__REF__` partition, cached by the reader:

| ItemType | From table | `id` |
|---|---|---|
| `SvcInterval` | `ServiceInterval` | interval id |
| `SvcIntervalGroup` | `ServiceIntervalGroup` (+ interval-id membership) | group id |
| `LabourRateMap` | `LabourRateMapping` | `{brandId}:{rate}` |
| `BrandMap` | `BrandMapping` | brand id |

**Why:** `Menu.BasicModelCode` is uniquely indexed
([`MenuModelBuilderExtensions.cs`](ADP.Menus.Data/Extensions/MenuModelBuilderExtensions.cs)), so all of
a model's rows share one L1 partition; `WHERE c.BasicModelCode = @code` is single-partition. `ItemType`
as L2 is the repo convention (`Vehicles` = VIN/ItemType/CompanyID). Reference tables have no
`BasicModelCode`, so instead of denormalizing their fields onto every model-scoped doc (which would
force a fan-out on every shared edit), they get the `__REF__` partition and are cached in the reader — a
shared edit updates exactly one reference doc and touches no model-scoped doc. `id` = DB row id is
soft-delete safe and gives `LastReplicationStamp` a stable key (§9).

**Worked example** — model `ABC12`, variant `4471`, periodic availability `9082` on interval `501`:

```
id="4471"  pk=["ABC12","MenuVariant"]
id="9082"  pk=["ABC12","MenuPeriod"]      { variantId:4471, serviceIntervalId:501 }
id="501"   pk=["__REF__","SvcInterval"]   { code:"...", valueInMeter:10000, groupId:77 }

Lookup:  SELECT * FROM c WHERE c.BasicModelCode="ABC12"   (+ cached __REF__)
         → CosmosToGenerationAggregator → MenuCodeGenerator → lines
```

Provision the container with paths `["/BasicModelCode","/ItemType"]` before the trigger runs.

---

## 4. Layer 1 — the generic generation contract + service (`netstandard2.0`, `ADP.Menus.Generation`)

The neutral input the fold needs, as a nested variant graph (mirrors how `GenerateMenuLines` walks the
data today) plus resolved reference lookups. **No Cosmos or EF types appear here.**

```csharp
namespace ShiftSoftware.ADP.Menus.Generation;

public class MenuGenerationRequest
{
    public List<GenVariant> Variants { get; set; } = [];
    public GenReferenceData Reference { get; set; } = new();
}

public class GenVariant
{
    public long VariantID { get; set; }
    public string BasicModelCode { get; set; }
    public long? BrandID { get; set; }
    public string Model { get; set; }
    public string VariantName { get; set; }
    public string MenuPrefix { get; set; } public string MenuPostfix { get; set; }
    public string StandaloneMenuPrefix { get; set; } public string StandaloneMenuPostfix { get; set; }
    public decimal LabourRate { get; set; }                    // primary
    public decimal? DiscountPercentage { get; set; }
    public bool HasStandaloneItems { get; set; }
    public List<GenCountryLabourRate> CountryLabourRates { get; set; } = [];
    public List<GenPeriod> Periods { get; set; } = [];
    public List<GenLabour> Labours { get; set; } = [];
    public List<GenItem>   Items   { get; set; } = [];
}
public class GenCountryLabourRate { public long CountryID; public decimal LabourRate; }
public class GenPeriod { public long ServiceIntervalID; }
public class GenLabour { public long ServiceIntervalGroupID; public decimal AllowedTime, Consumable; }
public class GenItem
{
    public long ReplacementItemVehicleModelID; public bool ReplacementItemDeleted;
    public decimal StandaloneAllowedTime;
    public List<long> ReplacementItemServiceIntervalGroupIDs = [];
    public string StandaloneOperationCode, StandaloneLabourCode, FriendlyName;
    public GenStandaloneGroup StandaloneGroup;                  // null when ungrouped
    public List<GenPart> Parts = [];
}
public class GenStandaloneGroup { public long ID; public string MenuCode, LabourCode, Name; }
public class GenPart { public string PartNumber; public decimal? PeriodicQuantity, StandaloneQuantity; public List<GenPartPrice> CountryPrices = []; }
public class GenPartPrice { public long CountryID; public decimal PartPrice, PartFinalPrice; }

public class GenReferenceData
{
    public IReadOnlyDictionary<long, GenServiceInterval> Intervals { get; set; }
    public IReadOnlyDictionary<long, GenServiceIntervalGroup> Groups { get; set; }
    public IReadOnlyDictionary<string, string> LabourRateCodes { get; set; }   // key "{brandId}:{rate}" → Code
    public IReadOnlyDictionary<long, string> BrandAbbreviations { get; set; }  // brandId → abbreviation
}
public class GenServiceInterval { public string Code, Description; public int ValueInMeter; public long GroupID; }
public class GenServiceIntervalGroup { public string LabourCode; public HashSet<long> ServiceIntervalIDs = []; }
```

Config and result:

```csharp
public class MenuGenerationConfig { public long CountryID; public decimal TransferRate = 1m; public bool UsePrimaryLabourRate; public string Language; }

public class GeneratedMenuLine     // core fields only — NO DMS margin math
{
    public string LineKey;         // "P|{v}|{i}" | "S|{v}|{item}" | "G|{v}|{group}"
    public string Code;            // the menu code (one language per Generate call)
    public string LabourCode;
    public string Description;
    public bool IsStandalone;
    public string ServiceIntervalCode; public int? ServiceIntervalValueInMeter;
    public decimal LabourRate, AllowedTime, Consumable, DiscountPercentage;
    public List<GeneratedMenuPart> Parts = [];
}
public class GeneratedMenuPart { public string PartNumber; public decimal Quantity, Price, LineTotal; }

public static class MenuCodeGenerator
{
    public static IEnumerable<GeneratedMenuLine> Generate(MenuGenerationRequest request, MenuGenerationConfig cfg) { /* ported fold */ }
}
```

**Port notes** (faithful translation of
[`MenuExportService`](ADP.Menus.Data/DataServices/MenuExportService.cs), guarded by the Phase-0 golden
test): EF navigations become dictionary lookups, e.g.
`labourDetail.ServiceIntervalGroup.ServiceIntervals.Any(s => s.ID == period.ServiceIntervalID)` →
`request.Reference.Groups[labour.ServiceIntervalGroupID].ServiceIntervalIDs.Contains(period.ServiceIntervalID)`.
The labour-rate code lookup uses `TryGetValue` (O1). `GetAllowedTimeText` is pinned to
`InvariantCulture` + fixed scale (O7). `FirstOrDefault` matching gets a deterministic order (O8).

---

## 5. Layer 2 — Cosmos sync models (persistence only, `netstandard2.0`, `ADP.Models`)

Flat, per-row, one class per itemType. Conventions from
[`ServiceItemModel`](../ADP.Models/Models/Vehicle/ServiceItemModel.cs): `[Docable]`, `[DocIgnore]` on
`id`/`ItemType`, `IPartitionedItem`. These are **written by replication and read by the lookup** — they
are *not* the generation input.

```csharp
namespace ShiftSoftware.ADP.Models.Service.Cosmos;

[Docable] public class MenuVariantCosmosModel : IPartitionedItem
{
    [DocIgnore] public string id { get; set; }                 // MenuVariant.ID
    public string BasicModelCode { get; set; }                 // PK L1
    [DocIgnore] public string ItemType => ModelTypes.MenuVariant;
    public long VariantID { get; set; } public long? BrandID { get; set; }
    public string Model { get; set; } public string VariantName { get; set; }
    public string MenuPrefix { get; set; } public string MenuPostfix { get; set; }
    public string StandaloneMenuPrefix { get; set; } public string StandaloneMenuPostfix { get; set; }
    public decimal LabourRate { get; set; } public decimal? DiscountPercentage { get; set; }
    public bool HasStandaloneItems { get; set; }
    public List<MenuCosmosCountryLabourRate> CountryLabourRates { get; set; } = [];
}
// MenuPeriodCosmosModel { VariantID, ServiceIntervalID }
// MenuLabourCosmosModel { VariantID, ServiceIntervalGroupID, AllowedTime, Consumable(unscaled) }
// MenuItemCosmosModel   { VariantID, StandaloneAllowedTime, ReplacementItem slice, Parts[] embedded }
// ServiceIntervalCosmosModel / ServiceIntervalGroupCosmosModel(+ interval-id membership) /
// LabourRateMappingCosmosModel { BrandID, LabourRate, Code } / BrandMappingCosmosModel { BrandID, Abbreviation }
```

Reference models set `public string BasicModelCode => NoSQLConstants.ServiceMenuRefPartition;` ("__REF__")
and their own `ItemType`.

**`CosmosToGenerationAggregator`** (static, in `ADP.Menus.Generation`, which references `ADP.Models` for
the Cosmos types) turns a bag of these Cosmos docs + the cached reference docs into a
`MenuGenerationRequest` — the reader's adapter, shared and unit-testable:

```csharp
public static MenuGenerationRequest Build(
    IEnumerable<MenuVariantCosmosModel> variants, IEnumerable<MenuPeriodCosmosModel> periods,
    IEnumerable<MenuLabourCosmosModel> labours, IEnumerable<MenuItemCosmosModel> items,
    ServiceMenuReferenceSnapshot reference) { /* group children under variants; resolve reference dicts */ }
```

**Constants:**

```csharp
public const string ServiceMenuRefPartition = "__REF__";
// Containers.ServiceMenus = "ServiceMenus";  PartitionKeys.ServiceMenus.{Level1="/BasicModelCode",Level2="/ItemType"}
// ModelTypes: MenuVariant, MenuPeriod, MenuLabour, MenuItem, SvcInterval, SvcIntervalGroup, LabourRateMap, BrandMap
```

None of these carry `[TypeScriptModel]`; the TS type is generated later from the lookup DTO (§8).

---

## 6. Source-side entity changes

Each replicated table implements `IShiftEntityReplication` (mirror
[`ClaimableItem`](../ADP.ClaimableItems/ADP.ClaimableItems.Data/Entities/ClaimableItem.cs)):

```csharp
public string? LastReplicationStamp { get; set; }
public DateTimeOffset? LastReplicationDate { get; set; }
```

Tables: `MenuVariant`, `MenuPeriodicAvailability`, `MenuLabourDetails`, `MenuItem`, `ServiceInterval`,
`ServiceIntervalGroup`, `LabourRateMapping`, `BrandMapping` — each needs a host EF migration adding the
two columns (O5). Where a Cosmos doc denormalizes related data (`MenuVariant`→Menu/VehicleModel;
`MenuItem`→replacement-item slice; `ServiceIntervalGroup`→interval membership), implement
`IShiftEntityPrepareForReplicationAsync<T>` on that repository to refetch with the needed `Include`s,
exactly like
[`WarrantyClaimRepository.PrepareForReplicationAsync`](../ADP.WarrantyClaims/ADP.WarrantyClaims.Data/Repositories/WarrantyClaimRepository.cs).

---

## 7. Reusable replication: mappers, extensions, service (manual, no AutoMapper)

### 7.1 Per-table manual mappers — `Replication/MenuCosmosMappers.cs` (EF entity → layer-2 Cosmos model)

```csharp
public static class MenuCosmosMappers
{
    public static MenuVariantCosmosModel Map(MenuVariant v) => new() {
        id = v.ID.ToString(), VariantID = v.ID, BasicModelCode = v.Menu.BasicModelCode,
        BrandID = v.Menu.VehicleModel?.BrandID, Model = v.Menu.VehicleModel?.Name, VariantName = v.Name,
        MenuPrefix = v.MenuPrefix, MenuPostfix = v.MenuPostfix,
        StandaloneMenuPrefix = v.StandaloneMenuPrefix, StandaloneMenuPostfix = v.StandaloneMenuPostfix,
        LabourRate = v.LabourRate, DiscountPercentage = v.DiscountPercentage, HasStandaloneItems = v.HasStandaloneItems,
        CountryLabourRates = v.LabourRates.Where(r => !r.IsDeleted)
            .Select(r => new MenuCosmosCountryLabourRate { CountryID = r.CountryID, LabourRate = r.LabourRate }).ToList(),
    };
    public static MenuPeriodCosmosModel Map(MenuPeriodicAvailability p) => /* … */;
    // …Labour, …Item(+parts+slice), …ServiceInterval, …Group(+membership), …LabourRateMapping, …BrandMapping
}
```

> The DMS export does **not** go through these Cosmos mappers. It has its own EF → **generic** adapter
> (`EfToGenerationAggregator`, §8) because its input is layer 1, not layer 2. Layer-2 mappers exist only
> for replication.

### 7.2 Trigger-wiring extensions (one per table + one for all) — `Extensions/MenuReplicationExtensions.cs`

Plain `Replicate` per table with the manual mapper — **no `UpdateReference`** (nothing denormalizes a
*shared* doc, so there is no cross-doc recompute). `AddMenuReplications` is the host's single entry point.

```csharp
public static ShiftEntityCosmosDbOptions AddMenuReplications<TDb>(this ShiftEntityCosmosDbOptions x, CosmosClient c)
    where TDb : ShiftDbContext
    => x.AddMenuVariantReplication<TDb>(c).AddMenuPeriodReplication<TDb>(c)
        .AddMenuLabourReplication<TDb>(c).AddMenuItemReplication<TDb>(c)
        .AddServiceIntervalReplication<TDb>(c).AddServiceIntervalGroupReplication<TDb>(c)
        .AddLabourRateMappingReplication<TDb>(c).AddBrandMappingReplication<TDb>(c);

public static ShiftEntityCosmosDbOptions AddMenuVariantReplication<TDb>(this ShiftEntityCosmosDbOptions x, CosmosClient c)
    where TDb : ShiftDbContext
{
    x.SetUpReplication<TDb, MenuVariant>(c, NoSQLConstants.Databases.Services, null)
     .Replicate<MenuVariantCosmosModel>(
         NoSQLConstants.Containers.ServiceMenus,
         partitionKeyLevel1Expression: e => e.BasicModelCode,
         partitionKeyLevel2Expression: e => e.ItemType,
         mapper: w => MenuCosmosMappers.Map(w.Entity));   // MANUAL
    return x;
}
// the other seven follow the identical shape
```

### 7.3 Runtime replicate service (per table + all/backfill) — `Replication/MenuReplicationService.cs`

The trigger catches only go-forward `SaveChanges`; initial load / recovery need an explicit push:

```csharp
public interface IMenuReplicationService
{
    Task ReplicateModelAsync(string basicModelCode, CancellationToken ct = default);
    Task ReplicateReferenceDataAsync(CancellationToken ct = default);
    Task<int> ReplicateAllAsync(CancellationToken ct = default);   // full backfill (null-VehicleModel rows filtered + reported)
}
```

---

## 8. Read-time: aggregate → generate → map (the lookup, FUTURE phase)

`ADP.LookupServices/Lookup.Services` (netstandard2.0):

- **`ServiceMenuReferenceCache`** loads the `__REF__` partition once into a `ServiceMenuReferenceSnapshot`;
  TTL + explicit invalidate (O4/O10).
- **`ServiceMenuLookupService`** (modelled on
  [`GoldenCustomerLookupService`](../ADP.LookupServices/Lookup.Services/Services/GoldenCustomerLookupService.cs)):

```csharp
public async Task<IReadOnlyList<VehicleServiceMenuLineDTO>> GetMenuAsync(string basicModelCode, MenuGenerationConfig cfg)
{
    var docs = await QueryPartition(basicModelCode);                 // SELECT * WHERE c.BasicModelCode=@code
    if (docs.Count == 0) return [];

    var request = CosmosToGenerationAggregator.Build(               // Cosmos docs → generic input (layer 2 → layer 1)
        docs.OfType<MenuVariantCosmosModel>(), docs.OfType<MenuPeriodCosmosModel>(),
        docs.OfType<MenuLabourCosmosModel>(), docs.OfType<MenuItemCosmosModel>(), refCache.Snapshot);

    var lines = MenuCodeGenerator.Generate(request, cfg);           // the SAME service the export uses
    return lines.Select(MapToLookupDto).ToList();                   // generic result → lookup DTO (layer 1 → layer 3)
}
```

The join key already exists
([`VehicleLookupDTO.BasicModelCode`](../ADP.LookupServices/Lookup.Services/DTOsAndModels/VehicleLookup/VehicleLookupDTO.cs),
derived from the Katashiki). Measure its hit rate against authored `Menu.BasicModelCode` before building
the section (O3). `VehicleServiceMenuLineDTO` (layer 3) carries `[TypeScriptModel]` and flows to the web
components. Reads are heavier here than a write-time design (partition read + aggregate + full fold every
lookup) — keep the reference cache warm; optional short per-model result cache (O9).

---

## 9. Deletes, renames, soft-delete — per-row, free from the framework

`LastReplicationStamp` stores each row's id + partition-key levels; on the next sync, *"when the document
id or any partition-key level has changed, the stale document is [removed]."* Per-row, this Just Works
independently per table: a soft-deleted `MenuItem` removes its doc; a `BasicModelCode` rename moves every
model-scoped doc and deletes the old. No timer/lease/sweep/generation-stamp/circuit-breaker.

---

## 10. Reference data staleness

The reference cache is the one read-time correctness dependency. A `ServiceInterval.Code`,
`ServiceIntervalGroup.LabourCode`/membership, `LabourRateMapping.Code` or `BrandMapping.BrandAbbreviation`
edit changes generated codes; replication updates the reference doc immediately, but the **reader cache**
must refresh (TTL + invalidate). A stale cache yields stale codes with **no error** — surface cache age on
a health endpoint.

---

## 11. Open items / decisions

| # | Item | Recommendation |
|---|---|---|
| O1 | Missing `LabourRateMapping` `(brand,rate)` in the port (original throws). | `TryGetValue`; emit line with empty `LabourCode` + flag. Note divergence from the export's fail-closed pre-check. |
| O2 | Do item/part edits touch the `MenuItem` row (so its doc + replacement-item slice re-replicate)? | Trace save paths; add targeted re-replication if not. |
| O3 | Derived Katashiki code vs authored `Menu.BasicModelCode` match rate in real data. | Measure first (host-side/ad-hoc; keep report in private planning). |
| O4 | Reference-cache freshness/invalidation in the reader. | TTL + explicit invalidate; alert on cache age. |
| O5 | Do all replicated tables need the two `IShiftEntityReplication` columns? | Confirm against the trigger before migrations. |
| O6 | `transferRate` / country at read time. | `LookupOptions` resolver → `MenuGenerationConfig`; `Consumable` stored unscaled, scaled in the generator. |
| O7 | `GetAllowedTimeText` culture/scale sensitivity (feeds labour code). | Pin `InvariantCulture` + fix scale in the port; apply to export too. |
| O8 | `MenuLabourDetails.FirstOrDefault` nondeterminism. | Deterministic order (by ID) in the port; export inherits it. |
| O9 | Read-time cost: full fold every lookup. | Warm reference cache; optional short per-model result cache. |

---

## 12. Phased checklist

- **Phase 0 — golden contract test (prerequisite).** `ADP.Menus/ADP.Menus.Tests` (solution + CI).
  Synthetic `MenuVariant` graph (periodic + standalone ungrouped + grouped; multi-language prefixes;
  JSON-blob description; 0-country and ≥2-country). Pin the `(LineKey, Code, LabourCode, prices)` set
  from today's `GenerateMenuLines`. This is the equality target.
- **Phase 1 — layer 1 (new `ADP.Menus.Generation`, netstandard2.0).** Create the project (+ `.sln`,
  `azure-pipeline.yml` pack/push, packable metadata). `MenuGenerationRequest`/config/result; port
  `MenuCodeGenerator` + `MenuTextHelpers` (O1/O7/O8). Add `NoSQLConstants` + `ModelTypes` entries in
  `ADP.Models`. Prove `Generate` == Phase-0 golden output for a hand-built request.
- **Phase 2 — export refactor.** `EfToGenerationAggregator` (EF → generic); `MenuExportService` calls
  `MenuCodeGenerator`; margins move to the Excel report layer. No output change (Phase 0 green).
- **Phase 3 — layer 2 + replication.** Cosmos models (`ADP.Models`) + `CosmosToGenerationAggregator`
  (`ADP.Menus.Generation`); entities implement
  `IShiftEntityReplication` (+ migrations); `PrepareForReplicationAsync` where denormalizing;
  `MenuCosmosMappers`; `MenuReplicationExtensions` (per-table + `AddMenuReplications`);
  `MenuReplicationService`.
- **Phase 4 — host wiring + backfill + provisioning.** Provision `ServiceMenus` (2-level PK); register the
  trigger + `AddMenuReplications`; run `ReplicateAllAsync` + `ReplicateReferenceDataAsync`; verify.
- **Phase 5 — read side (lookup, future).** `ServiceMenuReferenceCache` + `ServiceMenuLookupService`
  (partition read → `CosmosToGenerationAggregator` → `MenuCodeGenerator` → lookup DTO). Tests:
  aggregation equals the golden generic request; normalization; stale cache; missing container.
- **Phase 6 — vehicle-lookup integration** (gated on O3): evaluator + flat `[TypeScriptModel]` DTO +
  web-component section; measure/monitor the join-key hit rate.
