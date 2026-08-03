# Menu → Cosmos Replication — Implementation Plan

> Status: **Phases 0–6 implemented.** (§22 is Phase 6.)
> Agreed design for projecting the service-menu catalog into Cosmos DB so a vehicle lookup can turn a
> **basic model code** into a set of **menu codes + prices**.
>
> **Chosen model: read-time generation, with a shared source-agnostic generation service.**
> We replicate the menu tables into Cosmos as per-row documents; the menu codes/prices are generated
> **on lookup** by a *base generation service* whose input is a **neutral generic model** — the same
> service the DMS export uses. Each consumer adapts its own data in and out; only the generation logic
> + its generic contract are shared.
>
> **The container design is §16** (one container per master entity + a fully denormalized `ServiceMenus`
> container). **§16 supersedes §3 and §5**, which describe the single-container `__REFERENCE__` scheme
> that was built first and has since been replaced — they are kept only as the record of what changed
> and why. §17 records the rewrite.
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
| Storage | **§16:** one container per master entity + `ServiceMenus`, per-row docs, `ItemType`-discriminated, partitioned by `/BasicModelCode` | one query returns a whole model's graph |
| Document `id` | the source row's DB id | soft-delete safe |
| Shared master tables | **§16:** own container each, and their fields **denormalized** into the menu documents, kept fresh by `UpdateReference` | the lookup is one partition query; no reader-side cache to go stale |
| Mapping | **manual** everywhere (no AutoMapper) | explicit, reviewable |
| Fields | menu code, labour code, labour rate/allowed-time/consumable/labour-total, parts (number/qty/price/line-total), parts-total, discount, menu-total | DMS margin/cost/profit excluded |

---

## 2. Where the code lives

```
ADP.Menus/ADP.Menus.Generation/  (NEW — netstandard2.0, menu-owned, reachable by the lookup)
  Generation/                            # LAYER 1 — the shared contract + logic
    MenuGenerationRequest.cs             #   generic input (nested variant graph + reference data)
    MenuGenerationConfig.cs              #   country / transferRate / language / usePrimaryLabourRate
    GeneratedMenuLine.cs                 #   generic result (codes + every component that composed them)
    MenuCodeGenerator.cs                 #   the fold, ported from MenuExportService (source-agnostic)
    MenuTextHelpers.cs                   #   ported GetAllowedTimeText + LocalizedText (netstandard2.0, single source)
  Cosmos/
    CosmosToGenerationAggregator.cs      #   static: Cosmos docs → MenuGenerationRequest (references ADP.Models)

ADP.Models/  (netstandard2.0 — reachable by the lookup AND the menus host)
  Models/Service/Cosmos/
    ServiceMenuCosmosModels.cs           # LAYER 2 — all 10 document types in one file (§16/§17)
  Models/Constants/NoSQLConstants.cs     # EDIT — the 7 containers + their partition keys
  Models/ModelTypes.cs                   # EDIT — the 4 ServiceMenus itemType discriminators

ADP.Menus/ADP.Menus.Data/  (net10.0 — the export + the replication producer)
  ADP.Menus.Data.csproj                  # EDIT — ADP.Menus.Generation ref (P2); CosmosDbReplication + ADP.Models refs (P3)
  Entities/*.cs                          # EDIT — replicated tables implement IShiftEntityReplication
  Repositories/*Repository.cs            # EDIT — IShiftEntityPrepareForReplicationAsync where denormalizing
  DataServices/
    MenuExportService.cs                 # EDIT — EF → generic input → MenuCodeGenerator → report rows
    EfToGenerationAggregator.cs          # NEW — EF entities → MenuGenerationRequest (export's adapter)
    MenuLineMargins.cs                   # NEW — the report's derived margin/profit arithmetic (moved off the DTO)
  Replication/
    MenuCosmosMappers.cs                 # NEW — manual EF-entity → Cosmos model + the UpdateReference appliers
    MenuReplicationReload.cs             # NEW — per-table include graphs + the variant's master-data lookup
    MenuReplicationFinders.cs            # NEW — the UpdateReference queries, named so they can be pinned
    MenuReplicationService.cs            # STEP 2 — reusable replicate: per-table + all (backfill)
  Extensions/
    MenuReplicationExtensions.cs         # NEW — one Add*Replication per table + AddMenuReplications

ADP.LookupServices/Lookup.Services/  (netstandard2.0 — the reader; §20)
  Lookup.Services.csproj                 # EDIT — ADP.Menus.Generation ref; Microsoft.Extensions.Options
  ServiceMenuLookupOptions.cs            # NEW — the menu lookup's OWN options (O6). Not on LookupOptions
  ServiceMenuExceptions.cs               # NEW — container-not-provisioned + generation-failed, both named
  DTOsAndModels/ServiceMenu/             # LAYER 3 — one type per file. No cost/margin/profit fields
    ServiceMenuLookupDTO.cs  ServiceMenuVariantDTO.cs  ServiceMenuLineDTO.cs  ServiceMenuPartDTO.cs
    ServiceMenuLineType.cs   ServiceMenuLookupRequest.cs  ServiceMenuCountrySettings.cs
  Services/
    ServiceMenuCosmosService.cs          # NEW — the single-partition prefix read, split by ItemType
    ServiceMenuLookupService.cs          # NEW — read → generate → schedule/price → DTO
  Evaluators/
    ServiceMenuGenerationEvaluator.cs    # NEW — config resolution + aggregate + the SHARED generator
    ServiceMenuPricingEvaluator.cs       # NEW — parts/labour/discount/total; the export's formulas, cost-free
    ServiceMenuScheduleEvaluator.cs      # NEW — group by variant, order the schedule by distance
  Extensions/
    ServiceMenuLookupServiceCollectionExtensions.cs  # NEW — AddServiceMenuLookup, its own registration
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

## 3. Container structure — SUPERSEDED BY §16

> **This section describes the design that was built in Phase 3 step 1 and then replaced.** It is kept
> as the record of what changed. The live design — one container per master entity, plus a fully
> denormalized `ServiceMenus` — is **§16**, implemented as described in **§17**.

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

**Reference documents** (layer 2) — small, shared, in the sentinel `__REFERENCE__` partition, cached by the reader:

| ItemType | From table | `id` |
|---|---|---|
| `ServiceInterval` | `ServiceInterval` | interval id |
| `ServiceIntervalGroup` | `ServiceIntervalGroup` (+ interval-id membership) | group id |
| `LabourRateMapping` | `LabourRateMapping` | `{brandId}:{rate}` |
| `BrandMapping` | `BrandMapping` | brand id |

**Why:** `Menu.BasicModelCode` is uniquely indexed
([`MenuModelBuilderExtensions.cs`](ADP.Menus.Data/Extensions/MenuModelBuilderExtensions.cs)), so all of
a model's rows share one L1 partition; `WHERE c.BasicModelCode = @code` is single-partition. `ItemType`
as L2 is the repo convention (`Vehicles` = VIN/ItemType/CompanyID). Reference tables have no
`BasicModelCode`, so instead of denormalizing their fields onto every model-scoped doc (which would
force a fan-out on every shared edit), they get the `__REFERENCE__` partition and are cached in the reader — a
shared edit updates exactly one reference doc and touches no model-scoped doc. `id` = DB row id is
soft-delete safe and gives `LastReplicationStamp` a stable key (§9).

**Worked example** — model `ABC12`, variant `4471`, periodic availability `9082` on interval `501`:

```
id="4471"  pk=["ABC12","MenuVariant"]
id="9082"  pk=["ABC12","MenuPeriod"]      { variantId:4471, serviceIntervalId:501 }
id="501"   pk=["__REFERENCE__","ServiceInterval"]   { code:"...", valueInMeter:10000, groupId:77 }

Lookup:  SELECT * FROM c WHERE c.BasicModelCode="ABC12"   (+ cached __REFERENCE__)
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
    public List<MenuGenerationVariant> Variants { get; set; } = [];
    public MenuGenerationReferenceData Reference { get; set; } = new();
}

public class MenuGenerationVariant
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
    public bool IsFree { get; set; }                           // carried, never priced on
    public bool HasStandaloneItems { get; set; }
    public List<MenuGenerationCountryLabourRate> CountryLabourRates { get; set; } = [];
    public List<MenuGenerationPeriod> Periods { get; set; } = [];
    public List<MenuGenerationLabour> Labours { get; set; } = [];
    public List<MenuGenerationItem>   Items   { get; set; } = [];
}
public class MenuGenerationCountryLabourRate { public long CountryID; public decimal LabourRate; }
public class MenuGenerationPeriod { public long ServiceIntervalID; }
public class MenuGenerationLabour { public long ServiceIntervalGroupID; public decimal AllowedTime, Consumable; }
public class MenuGenerationItem
{
    public long ReplacementItemVehicleModelID; public bool ReplacementItemDeleted;
    public decimal StandaloneAllowedTime;
    public List<long> ReplacementItemServiceIntervalGroupIDs = [];
    public string StandaloneOperationCode, StandaloneLabourCode, FriendlyName;
    public MenuGenerationStandaloneGroup StandaloneGroup;                  // null when ungrouped
    public List<MenuGenerationPart> Parts = [];
}
public class MenuGenerationStandaloneGroup { public long ID; public string MenuCode, LabourCode, Name; }
public class MenuGenerationPart { public string PartNumber; public decimal? PeriodicQuantity, StandaloneQuantity; public List<MenuGenerationPartPrice> CountryPrices = []; }
public class MenuGenerationPartPrice { public long CountryID; public decimal PartPrice, PartFinalPrice; }

public class MenuGenerationReferenceData
{
    public IReadOnlyDictionary<long, MenuGenerationServiceInterval> Intervals { get; set; }
    public IReadOnlyDictionary<long, MenuGenerationServiceIntervalGroup> Groups { get; set; }
    public IReadOnlyDictionary<string, string> LabourRateCodes { get; set; }   // key "{brandId}:{rate}" → Code
    public IReadOnlyDictionary<long, string> BrandAbbreviations { get; set; }  // brandId → abbreviation
}
public class MenuGenerationServiceInterval { public string Code, Description; public int ValueInMeter; public long GroupID; }
public class MenuGenerationServiceIntervalGroup { public string LabourCode; public HashSet<long> ServiceIntervalIDs = []; }
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

## 5. Layer 2 — Cosmos sync models — SUPERSEDED BY §16

> **Superseded.** The model set below is the `__REFERENCE__`-partition one. The live models — six master
> documents plus four fully denormalized `ServiceMenus` documents — are described in §16 and listed in
> §17. `ServiceMenuReferencePartition` no longer exists.

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
    public bool IsFree { get; set; } public bool HasStandaloneItems { get; set; }
    public List<MenuCosmosCountryLabourRate> CountryLabourRates { get; set; } = [];
}
// MenuPeriodCosmosModel { VariantID, ServiceIntervalID }
// MenuLabourCosmosModel { VariantID, ServiceIntervalGroupID, AllowedTime, Consumable(unscaled) }
// MenuItemCosmosModel   { VariantID, StandaloneAllowedTime, ReplacementItem slice, Parts[] embedded }
// ServiceIntervalCosmosModel / ServiceIntervalGroupCosmosModel(+ interval-id membership) /
// LabourRateMappingCosmosModel { BrandID, LabourRate, Code } / BrandMappingCosmosModel { BrandID, Abbreviation }
```

Reference models set `public string BasicModelCode => NoSQLConstants.ServiceMenuReferencePartition;` ("__REFERENCE__")
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
public const string ServiceMenuReferencePartition = "__REFERENCE__";
// Containers.ServiceMenus = "ServiceMenus";  PartitionKeys.ServiceMenus.{Level1="/BasicModelCode",Level2="/ItemType"}
// ModelTypes: MenuVariant, MenuPeriod, MenuLabour, MenuItem, ServiceInterval, ServiceIntervalGroup, LabourRateMapping, BrandMapping
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
        LabourRate = v.LabourRate, DiscountPercentage = v.DiscountPercentage,
        IsFree = v.IsFree, HasStandaloneItems = v.HasStandaloneItems,
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

## 8. Read-time: aggregate → generate → map (the lookup) — IMPLEMENTED, see §20

> **Partly superseded by §16, and now built — §20 is what actually shipped.** `ServiceMenuReferenceCache`
> is **dropped**: the documents are fully denormalized, so the read is a single partition query and there
> is no reference partition to cache. The `CosmosToGenerationAggregator` signature below changed
> accordingly — it takes the four `ServiceMenus` document types and builds `MenuGenerationReferenceData`
> from what they already carry. The sketch below is otherwise accurate; §20 records the four things it
> did not anticipate.

`ADP.LookupServices/Lookup.Services` (netstandard2.0):

- ~~**`ServiceMenuReferenceCache`** loads the `__REFERENCE__` partition once into a `ServiceMenuReferenceSnapshot`;
  TTL + explicit invalidate (O4/O10).~~ **Dropped — see §16.**
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

## 9. Deletes, renames, soft-delete

`LastReplicationStamp` stores each row's id + partition-key levels, so an id or partition-key change
(e.g. a `BasicModelCode` rename) deletes the stale document before writing the new one. Per-row, that
works independently per table.

**Correction (verified against the framework source in Phase 3):** an earlier draft of this section
claimed a soft delete removes the document. It does not.

| Operation | What replication does |
|---|---|
| Insert / update | upsert |
| **Soft delete** (`IsDeleted = true`) | an ordinary UPDATE → the document is **upserted, still present** |
| **Hard delete** (EF `Remove`) | document deleted, using the coordinates in `LastReplicationStamp` |
| id / partition-key change | stale document deleted, new one written |

So **every Cosmos model carries `IsDeleted` and readers must filter on it** — that is not optional
bookkeeping. This costs nothing at read time: the generation contract already carries soft-delete flags
(§4), because the generator owns every inclusion rule.

Two consequences worth knowing:

- A row whose `LastReplicationStamp` is null (never replicated, or replicated by an older build) has
  its document **orphaned** on hard delete — there are no coordinates to delete by.
- Replication runs fire-and-forget on a detached scope. Failures are logged, never surfaced to the
  user's save, and the request can complete before replication does. Tests must not assert on Cosmos
  state immediately after `SaveChanges`.

## 10. Master data staleness — RESOLVED BY §16

An earlier design read shared reference data through a reader-side cache, which was the one read-time
correctness dependency: a `ServiceInterval.Code`, `ServiceIntervalGroup.LabourCode`/membership,
`LabourRateMapping.Code` or `BrandMapping.BrandAbbreviation` edit changes generated codes, and a stale
cache yields stale codes with **no error**.

§16 removes the cache entirely: those fields are denormalized into the menu documents and refreshed by
`UpdateReference` fan-outs when the master row is edited. **Staleness moved from the reader to the
write path**, where it is bounded and enumerable rather than time-based — the remaining gaps are listed
in §17 ("Known gaps"), and the backfill service (step 2) is the sweep that closes them.

---

## 11. Open items / decisions

| # | Item | Status / recommendation |
|---|---|---|
| O1 | Missing `LabourRateMapping` `(brand,rate)` — the source throws. | **DECIDED (Phase 1): keep throwing.** Ported verbatim; the failure mode is preserved rather than softened. Revisit only if a real deployment hits it — it is a one-line change in `ResolveLabourRateCode`. |
| O2 | Do item/part edits touch the `MenuItem` row? | **RESOLVED (Phase 3 step 1): mostly yes, with one confirmed bypass.** The variant save stamps `LastPropagatedAt` on every item (so any part/price edit re-replicates the item), and propagation stamps it too. **`MenuController.UpdatePartsPrice` does not** — it mutates country prices without touching `MenuItem`, and it is system-wide, so one run stales every item document. Also, deleting a variant/menu does not cascade to items/parts, orphaning their documents. Both are step-2 work. The third part of this item — the replacement-item slice going stale — is **closed** by §16's `ReplacementItem` fan-out. |
| O3 | Derived Katashiki code vs authored `Menu.BasicModelCode` match rate in real data. | **STILL OPEN, no longer a gate (Phase 6).** It gated Phase 6 on the assumption that a bad hit rate would make the section not worth building. That was the wrong shape: the measurement needs the join to exist, and the join is what Phase 6 builds. Phase 6 ships the *instrument* instead — `VehicleServiceMenuStatus` reports `Found` / `NotFound` / `NoBasicModelCode` per lookup, so hit rate is `Found / (Found + NotFound)` over a deployment's own traffic — and makes the section **opt-in per request**, so shipping it before the number is known costs a deployment nothing. Measure host-side; keep the report in private planning. |
| O4 | Reference-cache freshness/invalidation in the reader. | **CLOSED by §16.** There is no reader-side cache: the documents are fully denormalized and `UpdateReference` keeps the copies fresh. See §10. |
| O5 | Do all replicated tables need the two `IShiftEntityReplication` columns? | **RESOLVED (Phase 3 step 1): yes, all 10.** The trigger is constrained on `IShiftEntityReplication`, so a table without the columns is simply never replicated. |
| O6 | `transferRate` / country at read time. | **RESOLVED (Phase 5), amended (Phase 6).** `ServiceMenuLookupOptions.CountrySettingsResolver` (+ `DefaultCountryID`) → `MenuGenerationConfig`; `Consumable` stays unscaled in Cosmos and is scaled in the generator, pinned by test. The resolver exists because a menus host normalizes these two settings from its *configured country list*, which the lookup cannot see. Getting it wrong moves money, never codes. **Phase 6 made the resolver's transfer rate a default rather than a veto** — a caller that supplies one wins, so the vehicle lookup can take it from the UI (§22). `UsePrimaryLabourRate` stays the host's outright. (The Phase-5 note here named these on `LookupOptions`; they were on `ServiceMenuLookupOptions` from the start — §20.) |
| O7 | `GetAllowedTimeText` culture sensitivity (feeds labour code). | **DECIDED: leave as-is.** Ported verbatim, ambient culture included. Judged not worth the risk of changing codes already issued to a DMS for a case the deployments do not hit. Pinned by test so the behaviour is at least visible. |
| O8 | `MenuLabourDetails.FirstOrDefault` nondeterminism. | **RESOLVED structurally, no behaviour change.** The generic input is `List<>`-ordered, so "first match" is now a function of the aggregator's ordering rather than of EF/`HashSet` iteration. No `OrderBy` was added — that would have changed output. |
| O9 | Read-time cost: full fold every lookup. | **CLOSED (Phase 5), no cache added.** The read is one single-partition prefix query and then a pure in-memory fold over one model's documents. A per-model result cache was deliberately NOT built: it would need an invalidation story that replication does not provide (documents change without the reader being told), and it would re-introduce the staleness §16 removed at some remove. A caller sweeping many models caches its own results. |
| O10 | Raw (non-language-resolved) `ServiceInterval.Description` on periodic lines. | **DECIDED: leave as-is**, ported verbatim. Only bites if an interval description is ever authored multi-language. |

---

## 12. Phased checklist

- **Phase 0 — golden contract test (prerequisite). ✅ DONE.** [`ADP.Menus.Tests`](ADP.Menus.Tests/)
  (in `ADP.sln`, and run in CI via `azure-pipeline.yml`). 29 tests, all green. See §13 for exactly
  what is pinned and which open items are now characterised.
- **Phase 1 — layer 1 (new `ADP.Menus.Generation`, netstandard2.0). ✅ DONE.** See §14.
- **Phase 2 — export refactor. ✅ DONE.** `EfToGenerationAggregator` (EF → generic); `MenuExportService`
  calls `MenuCodeGenerator`; margins moved to the report layer. No output change — Phase 0 green. See §15.
- **Phase 3 — layer 2 + replication.** Split into two steps.
  - **Step 1 — trigger replication, on the §16 container design. ✅ DONE.** See §17.
  - **Step 2 — catch-up replication (the backfill). ✅ DONE.** See §18. What remains of the original
    step-2 list: the `UpdatePartsPrice` and delete-cascade gaps (O2) — now *recoverable* by a full
    sweep rather than fixed at source — and `CosmosToGenerationAggregator`, which belongs with the
    read side (Phase 5).
- **Phase 4 — host wiring + backfill + provisioning. ✅ DONE.** See §19. The three moving parts a host
  needs are now one call each — `MenuCosmosProvisioning.EnsureContainersAsync`,
  `AddMenuReplications`, `ReplicateAllAsync` — plus `MenuReplicationStatus.ReadAsync`, which is the
  verification step the plan asked for and nothing in the pipeline provided.
- **Phase 5 — read side (the lookup). ✅ DONE.** See §20. `ServiceMenuLookupService` (one partition read
  → `CosmosToGenerationAggregator` → `MenuCodeGenerator` → lookup DTO), four evaluators, and a
  round-trip test that proves the read path reproduces the export's lines. No reference cache — §16.
- **Phase 6 — vehicle-lookup integration. ✅ DONE (back end).** See §22. `VehicleServiceMenuEvaluator`, the
  flat `[TypeScriptModel]` DTOs, and `AddLookupService` now registering the menu lookup. **The
  web-component section is deliberately not built** — the response is shaped for a renderer and the
  TypeScript types are generated, but the rendering stays host-side (§22). O3 is **not** closed by this
  phase — Phase 6 ships the instrument (`VehicleServiceMenuStatus`) and makes the section opt-in so it can
  ship before the measurement, which only a real deployment can make.

---

## 13. Phase 0 — what the golden contract pins (DONE)

Project [`ADP.Menus.Tests`](ADP.Menus.Tests/) — net10.0, xunit.v3, referenced from `ADP.sln`, run in CI
(`azure-pipeline.yml`, step *"ADP.Menus menu-code generation golden tests"*). **29 tests, all green.**
No production code was changed in this phase — it only characterises current behaviour.

| File | Role |
|---|---|
| [`MenuGraphFixture.cs`](ADP.Menus.Tests/MenuGraphFixture.cs) | Builds the synthetic in-memory `MenuVariant` graph. `GenerateMenuLines` is a pure static over object graphs, so **no database or EF is needed**. Every navigation collection is a `List<>` (not the entities' default `HashSet`) so the fixture adds no ordering nondeterminism. |
| [`MenuLineFormatter.cs`](ADP.Menus.Tests/MenuLineFormatter.cs) | Canonical, diffable rendering of the generated lines. Invariant-culture, decimals unformatted so **scale is pinned**, line order preserved (never sorted). |
| [`MenuGenerationGoldenTests.cs`](ADP.Menus.Tests/MenuGenerationGoldenTests.cs) | Three full-output snapshots + behavioural assertions. |
| [`MenuTextHelperTests.cs`](ADP.Menus.Tests/MenuTextHelperTests.cs) | Pins `Utility.GetAllowedTimeText` and `LocalizedText.Resolve` — both feed generated codes and both are ported verbatim in Phase 1. |

**Graph coverage:** periodic lines; standalone ungrouped; standalone grouped (folded from two items);
multi-language JSON prefixes / operation code / group menu code; a JSON-blob interval description;
0-country and multi-country price rows; soft-deleted item / replacement-item link / part / country
price / country labour rate; zero and null periodic quantities; an item with no replacement-item link;
an interval whose group has no labour details.

**Three snapshots:** (1) country 2 / rate 1 / `en` / country labour rate; (2) country 0 / rate 2.5 /
`ar` / primary labour rate; (3) unmapped brand → `Z` abbreviation fallback.

### Behaviours now locked (Phase 1's port must reproduce these exactly)

- Periodic code shape `"{prefix} {basicModelCode} {intervalCode} {postfix}"`, trimmed; labour code
  shape `"{groupLabourCode}{allowedTimeText}{labourRateCode}{brandAbbrev}"`.
- An interval whose group has **no** `MenuLabourDetails` is silently skipped (`continue`) — a missing
  `Include` in a later refactor would drop lines exactly this quietly, hence an explicit test.
- The standalone **grouped** line takes its allowed time from the **first** item in the group.
- `Consumable` is scaled by `transferRate` and rounded to 2dp; parts collapse to price 0 when no
  country price row matches (no throw, no skip).
- All `MenuLineDTO` computed getters (`LabourCost = 10 × AllowedTime`, margin/profit percentages,
  `MenuTotalPrice` discount arithmetic) — so the Phase 2 move of margins into the report layer is
  provably output-preserving.

### Open items characterised here — all subsequently DECIDED as "preserve verbatim"

Phase 0 surfaced three quirks. The decision taken before Phase 1 was to **preserve all of them**: they
affect rare cases the deployments do not hit, and changing any would alter menu codes a DMS has
already received. The tests below therefore assert current behaviour permanently, not temporarily.

- **O1** — `MissingLabourRateMapping_Throws_O1`: a missing `(brand, rate)` pair makes generation
  throw `KeyNotFoundException`. Kept: the port throws identically.
- **O7** — `GetAllowedTimeText_IsCultureSensitive_O7`: `0.5` yields `"05"` under invariant
  culture but `"0,5"` under `de-DE`, so a comma can reach the labour code. Kept as-is by decision;
  the test documents it so it is at least visible rather than latent.
- **Whole-hour collision** — the trailing-zero trim makes `1` and `10` (and `2` and `20`) collide onto
  the same allowed-time text, so two different allowed times can produce the *same* labour code. Kept.
- **Raw interval description** (O10) — the periodic line assigns `ServiceInterval.Description`
  verbatim, without language resolution, so a multi-language JSON blob surfaces raw. The golden pins
  `Description="{"en":"Description EN","ar":"Description AR"}"`. Kept; only bites if an interval
  description is ever authored multi-language.

---

## 14. Phase 1 — the shared generation service (DONE)

New project **[`ADP.Menus.Generation`](ADP.Menus.Generation/)** — netstandard2.0, in `ADP.sln`, packed
in CI as `ShiftSoftware.ADP.Menus.Generation`. **61 tests green** (Phase 0's 29 + 32 new).
No production behaviour changed: `MenuExportService` is untouched — Phase 2 repoints it.

| File | Role |
|---|---|
| [`MenuGenerationRequest.cs`](ADP.Menus.Generation/Generation/MenuGenerationRequest.cs) | The neutral input: `MenuGenerationVariant` graph + `MenuGenerationReferenceData`. No EF, no Cosmos, no report types. |
| [`MenuGenerationConfig.cs`](ADP.Menus.Generation/Generation/MenuGenerationConfig.cs) | Country / transfer rate / language / primary-rate switch. |
| [`GeneratedMenuLine.cs`](ADP.Menus.Generation/Generation/GeneratedMenuLine.cs) | The generic result. |
| [`MenuTextHelpers.cs`](ADP.Menus.Generation/Generation/MenuTextHelpers.cs) | `GetAllowedTimeText` + `Resolve`, ported verbatim. |
| [`MenuCodeGenerator.cs`](ADP.Menus.Generation/Generation/MenuCodeGenerator.cs) | The fold. Pure, deterministic, no ambient state. |

**`ADP.Models` is untouched by this phase.** An earlier draft of the checklist put the `NoSQLConstants`
and `ModelTypes` entries here, but nothing in Phase 1 uses them — `ADP.Menus.Generation` does not even
reference `ADP.Models` — so they were dead constants describing types that did not exist yet (which is
why their partition-key paths had to be string literals rather than `nameof`). They moved to Phase 3,
alongside the Cosmos models they describe.

### How the port is proven

[`MenuCodeGeneratorPortTests`](ADP.Menus.Tests/MenuCodeGeneratorPortTests.cs) are **differential**: the
same logical data goes to the original `MenuExportService.GenerateMenuLines` as EF entities and to
`MenuCodeGenerator` as a hand-built request, and the outputs are compared field by field across 10
configurations (countries with/without price rows, both languages, null and unknown language, transfer
rates including a rounding case, primary vs country labour rate, unmapped brand).

Two fixtures are maintained independently *on purpose* — a shared adapter would let a bug cancel out on
both sides and prove nothing. **Keep [`MenuGraphFixture`](ADP.Menus.Tests/MenuGraphFixture.cs) and
[`MenuGenerationRequestFixture`](ADP.Menus.Tests/MenuGenerationRequestFixture.cs) in lockstep.**

The suite was mutation-checked: removing the consumable rounding from the generator fails 3 tests, so
the comparison has real teeth rather than passing trivially.

### The result carries every input that composed a code

`GeneratedMenuLine` is deliberately **detail-rich**, not minimal. Its consumers live outside this
package — the report exporter is in the host application, the lookup is in `ADP.LookupServices` — so
they must be able to render or re-compose a line without re-reading the menus database or being handed
the mapping dictionaries on the side. (Today `IMenuReportExporter` takes `BrandMappings` and
`LabourRateMappings` as separate context precisely because the line does not carry them.)

So alongside `Code` / `LabourCode` / `Description`, the line carries:

| Group | Fields |
|---|---|
| Menu-code components | `MenuCodePrefix`, `MenuCodeSegment`, `MenuCodePostfix` (+ `BasicModelCode`) |
| Labour-code components | `LabourOperationCode`, `AllowedTimeText`, `LabourRateCode`, `BrandAbbreviation` |
| Brand | `BrandID`, `BrandCode` (the company code — feeds no generated code, but the DMS export writes it as a column), `BrandAbbreviation` |
| Shape + source rows | `LineType`, `ServiceIntervalID`, `ServiceIntervalGroupID`, `MenuItemID`, `StandaloneGroupID` |
| Money inputs | `LabourRate`, `PrimaryLabourRate` (the mapping key — fixed while the line rate follows the country), `AllowedTime`, `Consumable`, `RawConsumable` (unscaled), `DiscountPercentage` |
| Config echo | `CountryID`, `TransferRate`, `Language` — so lines merged from several runs stay self-describing |
| Per part | `MenuItemID` (which item it came from — grouped lines fold several), `SortOrder`, `HasCountryPrice` (distinguishes "priced 0" from "no price row"), `Cost`/`TotalCost` (nullable, opt-in — see below) |

**Dealer cost is opt-in and OFF by default.** `GeneratedMenuPart.Cost` and `TotalCost` are `decimal?`
and stay null unless the caller sets `MenuGenerationConfig.IncludePartCost`. Dealer cost belongs to the
DMS export only and must never reach the vehicle lookup or a public web component, so the *safe* case
is the default: a consumer can leak cost only by explicitly asking for it, never by forgetting to strip
it. The export sets the flag; the lookup leaves it alone. Note `null` therefore means "not requested" —
when cost IS requested, an unpriced part falls back to `0` exactly as the export does, and
`HasCountryPrice` is what distinguishes "no price row".

Recomposition is covered by tests: the components really do rebuild `Code` and `LabourCode` exactly.
Note the menu-code segment and the model code **swap places** between periodic and standalone shapes,
so reconstruction needs `LineType`:

```
Periodic:            "{MenuCodePrefix} {BasicModelCode} {MenuCodeSegment} {MenuCodePostfix}".Trim()
Standalone (either): "{MenuCodePrefix} {MenuCodeSegment} {BasicModelCode} {MenuCodePostfix}".Trim()
Labour code:         "{LabourOperationCode}{AllowedTimeText}{LabourRateCode}{BrandAbbreviation}".Trim()
```

`MenuLineType` also recovers a distinction the old `IsStandalone` flag lost — ungrouped vs grouped
standalone. `IsStandalone` remains as a computed convenience.

Still deliberately absent: the DMS report's derived margin/profit arithmetic. Those are pure functions
of the fields above, so the report layer computes them rather than the generator carrying presentation.

### Three deliberate refinements to the §4 sketch

1. **`GeneratedMenuPart.Cost` exists but is opt-in** (the sketch omitted it entirely). Cost comes from
   the same country-price row as the retail price, so excluding it outright would force the DMS export
   to re-resolve prices and duplicate the very logic this type exists to share. Instead it is nullable
   and gated behind `MenuGenerationConfig.IncludePartCost`, defaulting to OFF — so the lookup never
   receives dealer cost even by accident, rather than relying on a layer-3 DTO to remember to drop it.
2. **`MenuGenerationLabourRateKey` is a struct, not a `"{brand}:{rate}"` string.** The source keys this lookup by
   `decimal` equality, under which `12.5` and `12.50` are the *same* key; a string key would treat them
   as different and silently miss mappings the export resolves. Pinned by `LabourRateKey_IgnoresDecimalScale`.
3. **Layer 1 does not reference `ADP.Models`.** It needs nothing from it, and staying free of the Cosmos
   assembly keeps the dependency direction honest. Phase 3 adds the reference (or a separate small
   project) when `CosmosToGenerationAggregator` lands.

### One fidelity trap found and avoided

The labour-rate lookup **must not be hoisted out of the standalone loops**. It throws when the mapping
is missing, and the source only performs it once it has a line to emit — so hoisting makes a variant
with `HasStandaloneItems = true` but no qualifying items throw where the source does not. Caught during
implementation and locked by `PortedGenerator_DoesNotResolveLabourRate_WhenNoLinesAreProduced`.

### `LineKey`

`GeneratedMenuLine.LineKey` (`P|{variant}|{interval}`, `S|{variant}|{item}`, `G|{variant}|{group}`)
identifies a line **independently of language**, so the same line can be correlated across language
runs. Never key on `Code` for that — it is language-dependent by construction.

---

## 15. Phase 2 — the export refactor (DONE)

The DMS export now goes through the shared generator. **81 tests green** (Phase 0's 29 + Phase 1's 32 +
20 new). **The Phase 0 golden snapshots were not touched**, and they run through the new path — which is
precisely the proof that the export's output did not change.

```
BEFORE:  EF entities ──MenuExportService.GenerateMenuLines (the fold)──▶ MenuLineDTO (+ margin getters)
AFTER:   EF entities ──EfToGenerationAggregator──▶ MenuGenerationRequest
                     ──MenuCodeGenerator (SHARED)──▶ GeneratedMenuLine
                     ──MenuExportService.MapToReportLine──▶ MenuLineDTO (plain data)
                                                            + MenuLineMargins (report layer)
```

| File | Change |
|---|---|
| [`EfToGenerationAggregator.cs`](ADP.Menus.Data/DataServices/EfToGenerationAggregator.cs) | NEW — the export's EF → layer-1 adapter. A pure projection: no filtering, no reordering. |
| [`MenuExportService.cs`](ADP.Menus.Data/DataServices/MenuExportService.cs) | The ~200-line fold is gone. Aggregate → generate → map, and nothing else. Signature unchanged, so `MenuController` is untouched. |
| [`MenuLineMargins.cs`](ADP.Menus.Data/DataServices/MenuLineMargins.cs) | NEW — the derived margin/cost/profit arithmetic, moved verbatim off `MenuLineDTO`. |
| [`MenuLineDTO.cs`](ADP.Menus.Shared/DTOs/Menu/MenuLineDTO.cs) | Now plain data — 12 computed getters removed. |
| `ADP.Menus.Data.csproj` | References `ADP.Menus.Generation`. CI already packs Generation before Data, so the published `ShiftSoftware.ADP.Menus.Data` picks the dependency up automatically. |

### Margins: why they moved, and why nothing broke

They were computed properties on the DTO. Layer 1 (`GeneratedMenuLine`) already refuses to carry margin
arithmetic — it is presentation, and a pure function of fields the line already has (§14). With layer 1
refusing it, leaving it on layer 3 gave the same arithmetic two plausible homes; it now has one, in the
assembly that owns `IMenuReportExporter`, while `MenuLineDTO` stays plain data in the DTO package.

Be precise about the dealer-cost angle, because it is easy to overstate: `MenuLineDTO` is **export-only**
and was never itself a leak path. The concern is forward-looking — when Phase 6 writes the lookup's line
DTO, this type is the obvious template, and `PartsCost` / `PartsProfit` / `GrossProfit` cannot be copied
across without dragging dealer cost with them. Cost is kept out of the lookup at its real chokepoint,
`MenuGenerationConfig.IncludePartCost`.

They are now **C# 14 extension members** on `MenuLineDTO`, in the namespace that already holds
`IMenuReportExporter` and `MenuExportContext`. So a host exporter reading `line.MenuTotalPrice` — or
`oldLine?.MenuTotalPrice`, which also still binds — keeps compiling unchanged: any implementer of
`IMenuReportExporter` necessarily imports that namespace. A breaking change was available and was not
needed.

Every formula is the previous implementation verbatim, rounding and divide-by-zero guards included. The
golden snapshots render all twelve, and the test formatter now reaches them through the extension —
so "the move changed no value" is asserted, not assumed.

**One caveat.** Extension members are invisible to serializers and to reflection-driven mapping (JSON,
AutoMapper, reflection-based Excel writers). Nothing serializes `MenuLineDTO` today — every export
endpoint returns a byte array — but a host that starts doing so would get a payload without these
twelve figures, *silently*, rather than a compile error. That is the only way this move is not free.

### One fidelity bug found and fixed in the adapter

An early draft of the aggregator inferred interval-group membership from
`ServiceInterval.ServiceIntervalGroupID` **in addition to** `ServiceIntervalGroup.ServiceIntervals`. The
fold only ever consulted the latter, so on a partially-loaded graph the inference would resurrect
periodic lines the export never emitted — issuing menu codes no DMS had received. Membership now comes
exclusively from `ServiceIntervalGroup.ServiceIntervals`, pinned by
`GroupMembership_ComesFromTheServiceIntervalsNavigation_NotTheForeignKey`.

That test needs its own minimal graph, and the reason is worth remembering: in `MenuGraphFixture` the
group is reachable through both a labour detail *and* a replacement item, and the second `AddGroup`
overwrites — and therefore **masks** — any foreign-key inference. Written against the shared fixture the
test passes with the bug present. Verified by re-introducing the inference and watching it fail.

### Deliberate, output-neutral changes

- **The result is materialised** (`.ToList()`), where it used to be a lazily-`Append`ed chain. Report
  exporters walk the sequence twice — once to size the parts columns, once to write rows — so the fold
  used to run **twice** per export. It now runs once. The only observable difference is that a missing
  labour-rate mapping (O1) throws at the call rather than at first enumeration; both callers already
  propagate it identically.
- `MenuExportService.GetPartPriceByCountry` / `GetPartFinalPriceByCountry` were deleted — `internal`,
  and country price resolution now lives in the generator.
- `IncludePartCost = true` is set on the export's config, and only there. Cost still reaches the report;
  it stays off by default for everyone else (§14).

### What the new tests add

[`EfToGenerationAggregatorTests`](ADP.Menus.Tests/EfToGenerationAggregatorTests.cs) — the adapter's
output must generate **identically to the hand-built `MenuGenerationRequestFixture`** across the Phase 1
configuration matrix, so a disagreement means the adapter mis-reads the EF graph. Plus: membership comes
from the navigation not the FK; soft-delete state is *carried* rather than filtered; collection order is
preserved (O8 depends on it); the labour-rate key keeps its decimal-value semantics across the hop.

[`MenuLineMarginsTests`](ADP.Menus.Tests/MenuLineMarginsTests.cs) — the edges a snapshot cannot reach:
both divide-by-zero guards, the 2dp rounding scale, null `Parts`, null discount, and `LabourCost` being
a flat 10/hour rather than the dealer's labour rate.

Mutation-checked: dropping the part soft-delete flag from the adapter fails 20 tests — including the
Phase 0 goldens, which now guard the adapter as well as the generator.

### Note for Phase 3

The aggregator's class comment lists the EF navigations it reads. **A missing `Include` there loses menu
lines silently** — an unloaded interval group is an empty interval group, and an empty group emits no
periodic line. The same trap applies to the Cosmos reader in Phase 5: a partition read that misses a
document type degrades to "no lines" rather than an error.

---

## 16. REVISED container design — follow the ShiftIdentity pattern (supersedes §3 and §5). ✅ IMPLEMENTED

> **Implemented — see §17 for what was built, what deviated, and why.**

Phase 3 step 1 first shipped a single `ServiceMenus` container holding both model-scoped documents and
the shared reference tables, the latter forced into a synthetic `"__REFERENCE__"` partition. **That was
wrong and has been replaced.** A container's partition key must be something every document in it
genuinely has; the reference documents have no basic model code, and inventing a sentinel to force them
in was the tell.

### The pattern being followed

`ShiftIdentity`'s `CompanyBranches` container, which solves the identical problem:

```
Identity database
├── Companies / Countries / Brands / Teams / Users / Departments / Services
│        ← every master entity gets its OWN container, keyed by its own id
└── CompanyBranches            partition: /BranchID + /ItemType
      ItemType="Branch"        → CompanyBranchModel        (root; EMBEDS City + Company)
      ItemType="Service"    ┐
      ItemType="Department" ├─→ CompanyBranchSubItemModel  (the many-to-many links, in the
      ItemType="Brand"      ┘                               ROOT'S OWN PARTITION, carrying the
                                                            related entity's denormalized fields)
```

Three rules:

1. **Every master entity gets its own container**, keyed by its own id — never forced into another
   aggregate's partition.
2. **Parent / lookup references are EMBEDDED** as nested objects on the root document.
3. **Many-to-many becomes sibling documents in the root's own partition** — same partition key,
   different `ItemType` — each carrying the related entity's denormalized fields.

### Applied to menus

```
Services database
├── ServiceIntervals / ServiceIntervalGroups / ReplacementItems /
│   StandaloneReplacementItemGroups / LabourRateMappings / BrandMappings
│        ← master data, one container each
└── ServiceMenus               partition: /BasicModelCode + /ItemType
      ItemType="MenuVariant"   → root; embeds menu + vehicle model, owns country labour rates
      ItemType="MenuPeriod"    ┐
      ItemType="MenuLabour"    ├─→ sibling documents in the same model-code partition,
      ItemType="MenuItem"      ┘   fully denormalized (see below); MenuItem owns its parts/prices
```

`MenuPeriod` / `MenuLabour` / `MenuItem` **are** the many-to-many link documents — variant↔interval,
variant↔interval-group and variant↔replacement-item respectively — each carrying the related entity's
denormalized fields, exactly as identity's `CompanyBranchSubItemModel` does. (An earlier draft of this
section also listed a separate `MenuItemServiceIntervalGroup` link document for the replacement
item's interval groups. That one cannot exist — see §17.)

### Documents are FULLY denormalized — decided

Like identity's sub-items (which carry `Name` and `IntegrationId`, not just ids), the menu documents
carry everything generation needs: interval codes and descriptions, group labour codes, replacement-item
operation codes and friendly names, labour-rate codes, brand abbreviations.

**The lookup is therefore ONE partition query on the basic model code, and nothing else.** No reference
cache, no second round trip, no cache-staleness window.

Freshness is the replication's job, not the reader's: when a master row changes, `UpdateReference` fans
the change out to every document that denormalizes it — the mechanism
[`ClaimableItemsReplicationExtensions`](../ADP.ClaimableItems/ADP.ClaimableItems.Data/Extensions/ClaimableItemsReplicationExtensions.cs)
already uses (`Campaign` → `ServiceItemModel`). Note the framework runs `UpdateReference` only on
`ChangeType.Modified`, which is correct here: a newly inserted master row is not yet referenced.

### What this supersedes

- `NoSQLConstants.ServiceMenuReferencePartition` and the four `__REFERENCE__` document types — **gone**,
  replaced by one master container per entity.
- **O4 (reference-cache freshness) is closed** — there is no reader-side cache to go stale.
- **O9 is reduced** — the read is a single partition query; only the generation fold remains per lookup.
- §8's `ServiceMenuReferenceCache` is **dropped** from the Phase 5 design.
- Step 1's `MenuItem` no longer embeds a replacement-item copy that could rot; the slice is maintained
  by `UpdateReference` from `ReplacementItem`, which also closes the "no replication registration at
  all for ReplacementItem" gap step 1 left open.

---

## 17. Phase 3 step 1 — replication, as built (DONE)

Go-forward replication only: every menu table projects per row into Cosmos on `SaveChanges`, using the
§16 container design. Backfill, the O2 gaps and the read-side aggregator remain step 2.

**102 tests green.** The first cut of this step (a single `ServiceMenus` container with a synthetic
`__REFERENCE__` partition, per the then-current §3/§5) was rewritten to §16 before anything shipped, so
there is no migration to perform — only provisioning (below).

| File | Role |
|---|---|
| [`ServiceMenuCosmosModels.cs`](../ADP.Models/Models/Service/Cosmos/ServiceMenuCosmosModels.cs) | Layer 2 — 6 master documents + 4 `ServiceMenus` documents + owned parts/prices/labour rates |
| [`MenuCosmosMappers.cs`](ADP.Menus.Data/Replication/MenuCosmosMappers.cs) | Manual EF → document projection (`Map`) + the fan-out appliers (`ApplyTo`) |
| [`MenuReplicationReload.cs`](ADP.Menus.Data/Replication/MenuReplicationReload.cs) | Re-fetches each row with the navigations its projection needs; resolves the variant's master data |
| [`MenuReplicationFinders.cs`](ADP.Menus.Data/Replication/MenuReplicationFinders.cs) | "Which documents embed this master row?" — the `UpdateReference` queries |
| [`MenuCosmosContainers.cs`](ADP.Menus.Data/Replication/MenuCosmosContainers.cs) | The 7 containers and their partition keys — one declaration, read by hosts, the sample and the test |
| [`MenuReplicationExtensions.cs`](ADP.Menus.Data/Extensions/MenuReplicationExtensions.cs) | One `Add…Replication` per table + `AddMenuReplications` |
| [`Program.cs`](samples/ADP.Menus.Sample.API/Program.cs) | The sample host: registers the trigger and provisions the containers |
| [`ServiceMenusProvisioningTests.cs`](ADP.Menus.Tests/ServiceMenusProvisioningTests.cs) | Provisions and asserts all 7 containers; offline guards on the document shapes |
| [`MenuReplicationFinderTests.cs`](ADP.Menus.Tests/MenuReplicationFinderTests.cs) | Renders every finder to Cosmos SQL, offline |

Plus: `NoSQLConstants` (7 containers + their partition keys) and 4 `ModelTypes` discriminators in
`ADP.Models`; `IShiftEntityReplication` on the 10 replicated entities.

### The ten registrations

| Entity | Own document | Fan-outs (`UpdateReference`) |
|---|---|---|
| `MenuVariant` | `ServiceMenus` / `MenuVariant` | — |
| `MenuPeriodicAvailability` | `ServiceMenus` / `MenuPeriod` | — |
| `MenuLabourDetails` | `ServiceMenus` / `MenuLabour` | — |
| `MenuItem` | `ServiceMenus` / `MenuItem` | — |
| `ServiceInterval` | `ServiceIntervals` | → `MenuPeriod` |
| `ServiceIntervalGroup` | `ServiceIntervalGroups` | → `MenuLabour`, → `MenuItem` |
| `ReplacementItem` | `ReplacementItems` | → `MenuItem` (whole slice) |
| `StandaloneReplacementItemGroup` | `StandaloneReplacementItemGroups` | → `MenuItem` |
| `LabourRateMapping` | `LabourRateMappings` | → `MenuVariant` |
| `BrandMapping` | `BrandMappings` | → `MenuVariant` |

### Three things §16 did not anticipate

**1. The `MenuItemServiceIntervalGroup` link document cannot exist.** §16's sketch listed the replacement
item's interval-group membership as sibling link documents. It cannot be built: the source row
(`ReplacementItemServiceIntervalGroup`) has no basic model code, so no `Replicate` registration can
place it in a model partition, and `UpdateReference` only ever *updates documents it finds* — it cannot
create one. The membership is therefore **embedded on the `MenuItem` document** as
`ServiceIntervalGroups`, each entry carrying the group's labour code and full interval membership.

That membership has to travel with the item, not be recovered from the sibling `MenuLabour` documents:
generation asks "does group G contain interval I" for *every* group the item serves — including groups
the variant has no labour detail for — and it asks with a dictionary **indexer**, so a missing group
throws rather than degrading. The flat `ReplacementItem.ServiceIntervalGroupIDs` list alongside it is
what makes the group fan-out an `ARRAY_CONTAINS` rather than a full scan.

**2. The variant's master data needs a synchronous lookup.** A variant document embeds the labour-rate
mapping for its (brand, primary rate) pair and its brand's mapping, but has no navigation to either, so
the async reload hook cannot carry them. `MenuReplicationReload.VariantMasterData` resolves them inside
the (synchronous) `Replicate` mapper. Replication runs fire-and-forget on a background task with no
synchronization context, so a blocking query there cannot deadlock a request. A missing labour-rate
mapping is recorded as **null**, not as an invented code — that is what preserves O1's "generation
throws on a missing pair" once the reader builds its dictionary.

**3. The step-1 mappers were filtering soft-deletes, and that changed menu codes.** The export applies
**no** soft-delete filter to any of its includes and there are no global query filters anywhere, so its
generic input carries deleted interval memberships and deleted replacement-item links. The first cut
filtered them out, which silently dropped whole periodic lines the export still emitted. The mappers are
now pure projections — they copy, they do not decide — matching `EfToGenerationAggregator` exactly. The
only filtering left is on the labour-rate and brand mapping catalogues, which the export filters too.

> **The conclusion still stands; the premise it rested on has since changed.** §21 makes soft-deleted
> rows excluded from generated menus on BOTH paths — so the export no longer emits those lines either.
> The mappers are unchanged and still must be: they carry the flags, and the *generator* applies them.
> Filtering in a mapper would still be wrong, now for the further reason that a soft-deleted row must
> reach Cosmos to take effect at all (only a hard delete removes a document).

### The reload hook

`SetUpReplication`'s third argument is an **async entity pre-processor** that replaces the entity
everything downstream sees. That is what `MenuReplicationReload` uses. It matters because the trigger
hands the mapper whatever EF had tracked at save time — for a child row, usually the row alone with
every navigation null — while the projections denormalize heavily. Without it the documents would have
holes, silently.

It is also why no repository was added: the alternative,
`IShiftEntityPrepareForReplicationAsync<T>`, requires the derived repository to **re-declare the
interface** or the override is never called — and most of these tables have no repository at all.

Query filters are ignored so soft-deleted rows still re-read, and a row that has vanished (hard delete)
falls back to the passed-in entity, which still carries the stamp the pipeline deletes by.

### Why the finders are named methods with their own test

`UpdateReference` finders are the mechanism the whole §16 design rests on, and they are the one part
that **cannot fail loudly**: they run as translated SQL inside fire-and-forget replication, so a
predicate that stops translating, or that names a property the document no longer has, produces no error
anywhere. The query matches nothing, master edits quietly stop propagating, and the lookup serves stale
codes with no signal. As ordinary methods they can be rendered to SQL offline — which is what
`MenuReplicationFinderTests` does, no emulator required.

### Host wiring

```csharp
services.AddShiftEntityCosmosDbReplicationTrigger<DB>(x => x.AddMenuReplications<DB>(cosmosClient));
```

A consumer that never calls it replicates nothing — the same opt-in shape as
`AddClaimableItemsReplication`. The call also enables EFCore.Triggered on the context (the after-save
hook replication rides on), so the host's own `AddDbContext` does not need to — both option actions
apply whichever order they run in.

Before first run the host must:

- provision the **7 containers** in the `Services` database.
  [`MenuCosmosContainers.All`](ADP.Menus.Data/Replication/MenuCosmosContainers.cs) declares each one's
  partition key, so it is never retyped: `ServiceMenus` is `["/BasicModelCode", "/ItemType"]` and the
  six master containers are each `"/id"`. Both `ServiceMenusProvisioningTests` (which additionally
  asserts the keys — they cannot be changed after creation) and the sample read that list;
- add the two `IShiftEntityReplication` columns to the **10** replicated tables (the sample uses
  `EnsureCreatedAsync`, so an existing sample database needs recreating).

[`ADP.Menus.Sample.API/Program.cs`](samples/ADP.Menus.Sample.API/Program.cs) is the worked example: it
registers the trigger and provisions the containers at startup, both gated on
`ConnectionStrings:Cosmos` being set, with provisioning failures logged rather than fatal so the sample
still boots without an emulator.

### Known gaps, carried to step 2

- **`MenuController.UpdatePartsPrice` is a confirmed bypass.** It mutates country prices without
  touching `MenuItem`, and it is system-wide — one run stales every item document. Fix by stamping the
  owning item, or by re-driving the item from the backfill service afterwards. (O2)
- **Deletes do not cascade.** Deleting a variant or menu leaves items/parts/periods/labour-details
  untouched, so their documents outlive the parent. (O2)
- **`UpdateReference` fires only on `Modified`.** A newly INSERTED master row reaches no existing
  document — correct for a genuinely new row, but it also means a hard-DELETED master row leaves its
  embedded copies behind. The backfill service is the answer to both.
- **`ReplacementItemServiceIntervalGroup` edits reach Cosmos only via their parent.** Adding or removing
  a link refreshes the menu items only if the `ReplacementItem` row is itself saved (see deviation 1).
- **Re-keying a mapping strands the documents it used to serve.** The labour-rate and brand fan-outs
  find variants by the mapping's key — (brand, rate) and brand — not by "which document currently
  embeds this row". Editing a mapping's `BrandID` or `LabourRate` therefore refreshes the variants that
  match the NEW key, while the ones that matched the old key keep a copy that no longer applies to
  them. The right shape for the catalogue is insert + soft-delete rather than re-key; the backfill
  service is the sweep for when it happens anyway.
- **A labour-rate or brand mapping whose `BrandID` is NULL fans out to nothing** — Cosmos SQL evaluates
  `c.BrandID = null` to undefined, not true. Left as-is deliberately: a variant with no brand is
  excluded from the export by construction, so it generates no lines to keep fresh.

---

## 18. Phase 3 step 2 — catch-up replication (DONE)

The save trigger only ever sees rows as they are saved. A catalogue that existed before replication was
switched on never reaches Cosmos at all, and neither does anything missed while Cosmos was unreachable.
Catch-up replication is the sweep that closes that, and it is also the recovery path for every gap §17
lists. Modelled on ShiftIdentity's `IdentityCatchUpReplicationExtensions`.

| File | Role |
|---|---|
| [`MenuCatchUpReplicationExtensions.cs`](ADP.Menus.Data/Replication/MenuCatchUpReplicationExtensions.cs) | One `ReplicateXAsync` per table + `ReplicateAllAsync` |
| [`MenuReplicationIncludes.cs`](ADP.Menus.Data/Replication/MenuReplicationIncludes.cs) | The include graphs, now shared by BOTH paths |
| [`ADP.Menus.Sample.Functions`](samples/ADP.Menus.Sample.Functions/) | The sample host: 10 hourly timers + 1 on-demand HTTP backfill |

### Dirty-only by default, full on demand

`updateAll: false` (the hourly timers) syncs only rows whose `LastReplicationDate` is behind their
`LastSaveDate`, or absent — cheap, and the normal case. `updateAll: true` (`POST api/replicate-all`)
re-syncs every row: first switch-on, a rebuilt container, or after the system-wide parts-price update,
which leaves `MenuItem` rows **clean** and therefore invisible to a dirty-only pass.

### Nothing is written twice

The sweep shares everything with the trigger: include graphs from `MenuReplicationIncludes`,
projections from `MenuCosmosMappers`, fan-out queries from `MenuReplicationFinders`. Two
independently-written replication paths would drift, and the drift would stay invisible until a lookup
returned a wrong menu code. The include graphs were extracted from `MenuReplicationReload` for exactly
this reason — that file now wraps the shared shapers rather than owning its own copies.

### The one asymmetry: the variant's master data

A variant document embeds the labour-rate mapping for its (brand, primary rate) pair and its brand's
mapping, and a variant has **no navigation to either** — no include can bring them along. The trigger
resolves them with one query per row (it only ever handles one); a sweep would turn that into a query
per variant, so `ReplicateMenuVariantAsync` takes the `DbContext`, loads both catalogues **once**, and
applies the same selection rule in memory. The rule itself lives in one place
(`MenuReplicationReload.SelectLabourRateMapping` / `SelectBrandMapping`, taking an `IQueryable` so the
trigger runs it server-side and the sweep runs it over a list).

### Verified against the sample

`POST api/replicate-all` on a 27-variant imported catalogue, ~30s per run against the emulator:

| | Cosmos | SQL |
|---|---|---|
| `ServiceMenus`/`MenuVariant` | 57 | 57 |
| `ServiceMenus`/`MenuPeriod` | 2120 | 2120 |
| `ServiceMenus`/`MenuLabour` | 399 | 399 |
| `ServiceMenus`/`MenuItem` | 1188 | 1188 |
| `ServiceIntervals` | 40 | 40 |
| `ServiceIntervalGroups` | 7 | 7 |
| `ReplacementItems` | 57 | 57 |
| `StandaloneReplacementItemGroups` | 1 | 1 |
| `LabourRateMappings` | 2 | 2 |
| `BrandMappings` | 2 | 2 |

**The first run wrote only 1060 of the 1188 menu items** — the largest documents, written concurrently
with `AllowBulkExecution` into a 400 RU/s emulator container. The framework catches a failed row,
marks it unsuccessful and leaves `LastReplicationStamp` null, so the second run picked up exactly those
128 and the counts then matched. Worth knowing rather than being surprised by: **a single sweep is not
a guarantee**, it is idempotent and self-healing across runs. It is also silent — nothing is logged for
a row that fails this way, so `SELECT COUNT(*) … WHERE LastReplicationStamp IS NULL` is the way to see
whether a backfill actually finished.

### The sample Functions host

Ten hourly timers (one per table) plus `POST api/replicate-all`. Timers are disabled in dev **twice
over**: the host-honoured `AzureWebJobs.<FunctionName>.Disabled` entries in `local.settings.json` stop
them firing locally, and `RunTimerAsync` additionally no-ops in a Development host. A deployed host
reads neither, so replication runs there. The HTTP endpoint always works, which is how you replicate
while developing.

It maps the menu tables through its own [`MenuReplicationDB`](samples/ADP.Menus.Sample.Functions/MenuReplicationDB.cs)
rather than the module's `MenuDB`, and the reason is a trap worth recording: `MenuDB` declares `DbSet`
properties, and EF Core names a table after its DbSet property when one exists. The API host has no
menu DbSets — it picks the entities up through `MenuModelBuildingContributor` — so its tables are named
after the ENTITY types (`Menu.LabourRateMapping`, singular). Pointing `MenuDB` at that database fails
with *"Invalid object name 'Menu.LabourRateMappings'"*. **A sweep host must map the tables the same way
the writing host does.**

---

## 19. Phase 4 — host wiring, provisioning and verification (DONE)

**107 tests green** (Phase 3's 102 + 5 new).

Most of what this phase was scoped to do already existed when it started, which is worth stating plainly:
step 1 shipped `AddMenuReplications` and the `MenuCosmosContainers.All` declaration, step 2 shipped the
catch-up sweep, and both sample hosts were wired and verified against a 27-variant catalogue (§18). What
was *not* there was the difference between "a host can do this" and "a host calls one method": the
provisioning was a recipe, and the verification was a manual `SELECT COUNT(*)` recorded in prose.

| File | Role |
|---|---|
| [`MenuCosmosProvisioning.cs`](ADP.Menus.Data/Replication/MenuCosmosProvisioning.cs) | NEW — create the database + 7 containers, and **verify** every partition key |
| [`MenuReplicationStatus.cs`](ADP.Menus.Data/Replication/MenuReplicationStatus.cs) | NEW — per-table in-sync / pending / never-replicated counts |
| [`Program.cs`](samples/ADP.Menus.Sample.API/Program.cs) | The startup provisioning loop is now that one call |
| [`MenuReplicationFunctions.cs`](samples/ADP.Menus.Sample.Functions/Functions/MenuReplicationFunctions.cs) | `GET api/replication-status`; `replicate-all` now returns the status it finished at |
| [`ServiceMenusProvisioningTests.cs`](ADP.Menus.Tests/ServiceMenusProvisioningTests.cs) | Exercises the shipped provisioner, then re-reads every container independently |
| [`MenuReplicationStatusTests.cs`](ADP.Menus.Tests/MenuReplicationStatusTests.cs) | NEW — the reader must count every entity that opts into replication |
| `menus/cosmos-replication.md` (ADP.Docs) | NEW — the host-facing integration page, in the docs nav |

### Provisioning verifies rather than trusting

`CreateContainerIfNotExists` is a no-op when the container exists — including when it exists with a
**different partition key**, which it then accepts silently. Every document afterwards lands in the wrong
partition, and a partition key cannot be altered: the only repair is dropping and recreating the container
once real data is in it. So `EnsureContainersAsync` compares the key it got back against
`MenuCosmosContainers.All` and throws, naming every wrong container in one message rather than making an
operator rediscover them one restart at a time.

That check is also why the provisioning **test** did not simply become a call to the new method. If the
test only called it, a provisioner that compared the expected key against itself would pass on any
container. It calls the provisioner *and* re-reads each container's properties independently.

Worth being precise about what this buys, because it is not hypothetical: the step-1 design (§3) put every
document in one container and §16 replaced it with seven. Any environment provisioned against the older
shape is exactly the case this refuses to run on.

### Verification: reading the watermark, and what it cannot see

§18 recorded that a sweep silently wrote 1060 of 1188 menu items, and that
`SELECT COUNT(*) … WHERE LastReplicationStamp IS NULL` is how you notice. `MenuReplicationStatus` is that
query as a shipped call, per table; the `replicate-all` endpoint now runs it automatically and logs a
warning when the sweep it just finished left rows behind.

`LastReplicationDate` is a **watermark, not a timestamp** — the pipeline copies the replicated version's
`LastSaveDate` into it, so exact equality means "in sync". `Pending` therefore counts precisely what a
dirty-only sweep would pick up, and `NeverReplicated` (no stamp at all) is the sharper signal: after a full
pass it means the write for that row *failed*, not that a later edit outran it.

Both bookkeeping columns are reached through `EF.Property` with `nameof`, because they are declared on
interfaces rather than on the entity base — a direct member access is not reliably translatable for an open
generic, and a string literal would move a framework rename from a build error to a runtime query failure.

**What it cannot see, and this is the honest limit:** it compares SQL against SQL, so it reports what the
pipeline *believes*. A document staled by an edit that bypassed its owning row — O2's system-wide
parts-price update — leaves that row clean, so the report says "up to date" while the document is wrong.
That gap is unchanged from §17/§18, and `updateAll: true` remains its only answer.

`MenuReplicationStatusTests` guards the reader against the failure mode that would make it worse than
useless: a table added to the replication but forgotten in the reader is never counted, so the report would
say "up to date" about a table that had never reached Cosmos at all. The test derives the expected set from
`IShiftEntityReplication` — the same opt-in both replication paths constrain on — rather than restating the
list.

### Verified against the sample

`MenuCosmosProvisioning.EnsureContainersAsync` was run against a local emulator through
`ServiceMenusProvisioningTests` — all seven containers present, every partition key read back and
asserted — and `MenuReplicationStatus.ReadAsync` against the imported sample database:

| Table | Total | Never replicated | Pending |
|---|---|---|---|
| `MenuVariant` | 57 | 0 | 0 |
| `MenuPeriodicAvailability` | 2120 | 0 | 0 |
| `MenuLabourDetails` | 399 | 0 | 0 |
| `MenuItem` | 1188 | 0 | 0 |
| `ServiceInterval` | 40 | 0 | 0 |
| `ServiceIntervalGroup` | 7 | 0 | 0 |
| `ReplacementItem` | 57 | 0 | 0 |
| `StandaloneReplacementItemGroup` | 1 | 0 | 0 |
| `LabourRateMapping` | 2 | 0 | 0 |
| `BrandMapping` | 2 | 0 | 0 |
| **Total** | **3873** | **0** | **0** — `IsUpToDate` |

Those totals are the same ten numbers §18 counted **in Cosmos** after its backfill, which is the point:
the check now reports from SQL what previously had to be counted by hand in the Data Explorer, and the two
agree.

### What stays host-side

Two things cannot live in this repo, and the docs page says so rather than implying otherwise:

- the **EF migration** adding the two bookkeeping columns to the ten tables (the sample has one; a host
  needs its own);
- **when** to provision and **how often** to sweep. The sample provisions at startup and sweeps hourly
  because that makes "clean checkout + emulator" a working setup; a real host provisions through its
  deployment pipeline.

---

## 20. Phase 5 — the read side, as built (DONE)

**157 tests green** (Phase 4's 107 + 50 new). A vehicle lookup can now turn a basic model code into menu
codes, labour codes and prices, and those codes are the DMS export's codes.

```
GetMenuAsync("ABC12")
  ─▶ ServiceMenuCosmosService        one prefix query on /BasicModelCode → ServiceMenuDocuments
  ─▶ ServiceMenuGenerationEvaluator  CosmosToGenerationAggregator → MenuCodeGenerator (SHARED)
  ─▶ ServiceMenuScheduleEvaluator    group by variant; schedule by distance
  ─▶ ServiceMenuPricingEvaluator     parts / labour / discount / total  → ServiceMenuLookupDTO
```

| File | Role |
|---|---|
| [`CosmosToGenerationAggregator.cs`](ADP.Menus.Generation/Cosmos/CosmosToGenerationAggregator.cs) | LAYER 2 → LAYER 1. The mirror of `EfToGenerationAggregator`; where export parity is won or lost |
| [`ServiceMenuDocuments.cs`](ADP.Menus.Generation/Cosmos/ServiceMenuDocuments.cs) | One partition's documents, split by item type — the read path's single shape |
| `ServiceMenuCosmosService.cs` | The single-partition prefix read |
| `ServiceMenuLookupService.cs` | The three-step orchestration, plus `GetGeneratedLinesAsync` for callers wanting raw lines |
| `ServiceMenuGenerationEvaluator` / `…PricingEvaluator` / `…ScheduleEvaluator` | One decision each — see below |
| `DTOsAndModels/ServiceMenu/*` | Layer 3, one type per file, plus the request and country-settings contract |
| `ServiceMenuLookupOptions.cs` + `ServiceMenuLookupServiceCollectionExtensions.cs` | The feature's own options and its own `AddServiceMenuLookup` |
| `ServiceMenuExceptions.cs` | Provisioning fault and generation fault, each named and actionable |
| [`CosmosToGenerationAggregatorTests.cs`](ADP.Menus.Tests/CosmosToGenerationAggregatorTests.cs) | The round trip, plus every filtering/ordering rule |
| [`ServiceMenuEvaluatorTests.cs`](ADP.Menus.Tests/ServiceMenuEvaluatorTests.cs) | The evaluators, the pricing differential, and the cost guard |
| [`ServiceMenuLookupRegistrationTests.cs`](ADP.Menus.Tests/ServiceMenuLookupRegistrationTests.cs) | The DI wiring — offline, since a `CosmosClient` connects lazily |
| [`MenuCosmosDocumentFixture.cs`](ADP.Menus.Tests/MenuCosmosDocumentFixture.cs) | EF graph → documents, through the production mappers |
| `menus/service-menu-lookup.md` (ADP.Docs) | The host-facing page, in the docs nav |

### The test that matters: replicate, then read, then compare

`ReplicateThenRead_ProducesTheExportsLines` sends one menu graph two ways — straight to
`MenuExportService`, and through the production replication mappers into Cosmos documents, back out
through `CosmosToGenerationAggregator`, into the generator — and asserts the two outputs are identical
character for character, across the Phase-1 configuration matrix plus the unmapped-brand case.

That is a materially stronger claim than "both call the same generator", which is true by construction
and proves nothing: **the generator cannot disagree with itself, but the two adapters can.** Every
soft-delete and ordering rule in the Cosmos adapter exists to mirror one on the EF side, and this is
where a divergence surfaces — as a wrong menu code, at the point one would actually be issued.

The fixture goes through the production mappers **on purpose**, unlike `MenuGenerationRequestFixture`,
which is hand-built precisely so it cannot cancel out a bug. The two answer different questions: the
hand-built one asks "is the generator a faithful port?", where an independent second opinion is the whole
value; this one asks "does the round trip preserve the codes?", where the mappers are part of the round
trip being tested. Hand-authoring documents here would test a shape nothing writes.

### Four things §8 did not anticipate

**1. A deleted MENU was still serving menu codes.** The export selects
`!variant.IsDeleted && !variant.Menu.IsDeleted`. Only the first was on the document, and deleting a menu
does not cascade to its variants (§17), so the variant document stayed `IsDeleted = false` and the lookup
would have kept generating for a deleted menu — silently, indefinitely. Fixed by flattening the parent's
flag onto the variant document as `MenuIsDeleted`, projected by `MenuCosmosMappers`. Additive and
defaulting to `false`, so pre-existing documents keep their current behaviour until a sweep refreshes them.

*Remaining gap:* a menu soft-delete touches no `MenuVariant` row, so the row stays clean and a
**dirty-only sweep will not pick it up** — `updateAll: true` will. Same shape as O2's parts-price bypass.
The complete fix is to register `Menu` for replication with an `UpdateReference` fan-out onto its
variants, which needs the two bookkeeping columns on `Menu` and therefore a host migration; deliberately
not done here.

**2. Soft-deleted period and labour documents must STILL generate their lines** — matching the export,
which applies no soft-delete predicate to any of its includes. Exactly two things were filtered, because
exactly two are filtered by the export: the variant (its own flag and its menu's), and the labour-rate /
brand mapping catalogues.

> **Superseded by §21**, which changes the EXPORT's rule rather than the reader's: a soft-deleted row is
> now excluded from generated menus on both paths. The reasoning above was right about parity and wrong
> about which behaviour to standardise on.

**3. A Cosmos partition query has no order, and order is behaviour.** The periodic pass takes the FIRST
matching labour detail, and a grouped standalone line takes its allowed time from the FIRST item in its
group (O8). The aggregator therefore imposes its own total order — source row id ascending, everywhere —
which is both deterministic and the closest available match to how EF returns included collections.
`DocumentOrder_DoesNotAffectOutput` feeds the documents reversed and asserts the output is unchanged;
without the sorts, generated codes would depend on document layout.

**4. Where the read-side adapter lives.** §14 left this open ("Phase 3 adds the reference, or a separate
small project"). It went into `ADP.Menus.Generation`, per §2's layout, which required the `ADP.Models`
reference §2.1 had already sketched. Worth being honest that §2.1 undersold the cost: `ADP.Models` itself
carries ShiftEntity.Model, FileHelpers, libphonenumber and BouncyCastle, so "dependency-light" is no
longer quite true of the Generation package. It costs the two real consumers nothing — the export and the
lookup both already reference `ADP.Models` — and the alternatives were a third package for one file, or
duplicating the adapter in every Cosmos consumer. The symmetry argument for putting it in the lookup
instead (the export's adapter lives with the export) was the close call; keeping it beside the generator
won because it is then testable and reusable without dragging in the lookup.

### The evaluators

One cohesive decision per evaluator, composed by a thin service — the convention the repo's existing
evaluators already follow (`VehicleServiceItemEvaluator`, `PartPriceEvaluator`, …).

| Evaluator | Decides |
|---|---|
| `ServiceMenuGenerationEvaluator` | the `MenuGenerationConfig` (O6), then aggregate + generate |
| `ServiceMenuPricingEvaluator` | parts total, labour price/total, discount, menu total |
| `ServiceMenuScheduleEvaluator` | grouping by variant; schedule ordered by distance, standalone ungrouped-before-grouped |

**There is no variant-selection evaluator, and there should not be one.** A first cut had a
`ServiceMenuVariantEvaluator` narrowing the result to caller-supplied variant ids, with
`ServiceMenuLookupRequest.VariantIDs` to feed it. That was wrong on its face: **a menu variant's id is a
primary key inside the menus database and nothing outside it holds one** — a caller has a VIN or a model
code. It was a parameter no caller could populate. Removed; every live variant of the model is returned
and the caller picks from `ServiceMenuVariantDTO`, which carries the id and the authored name.

What the evaluator also did — dropping children orphaned by an uncascaded variant delete — was already
done by the aggregator, which groups children under variants so an orphan can never contribute
(`OrphanedChildDocuments_AreIgnored`). Deleting it lost no behaviour. If a rule for "which variant applies
to THIS vehicle" is ever established, it belongs in the request as that rule, shaped by what the rule
actually needs; `EveryVariantOfTheModel_IsReturned` pins that a filter is not reintroduced as a
convenience in the meantime.

**The variant does not echo the model name either.** `GeneratedMenuLine` carries it (the export writes it
as a report column), but `ServiceMenuVariantDTO` drops it: the caller looked the menu up BY the model, so
it already has a name for it — and the menus catalog's vehicle-model name is authored separately from the
vehicle database's, so returning it would put a second, occasionally disagreeing name next to the one the
caller is displaying. Layer 3 is the lookup's shape, not a mirror of layer 1; carrying a field only
because the generator happens to produce it is how a DTO accumulates fields nobody can safely use.

**Pricing is a differential test, not a snapshot.** `Pricing_MatchesTheExportsOwnArithmetic` computes each
line's money both ways — through `ServiceMenuPricingEvaluator` and through the export's own
`MenuLineMargins` extension members — and asserts they agree, over four country / transfer-rate
combinations. The two implementations live in different assemblies and would otherwise drift the first
time someone tidied one of them.

`DiscountAmount` is derived back OUT of the discounted total rather than computed separately, so the
reported amount always reconciles with the reported total instead of being a second rounding that can
disagree with it by a fraction.

### Its own options, its own registration, and no `IServiceProvider`

The first cut folded three settings onto `LookupOptions`, registered the service inside
`AddLookupService`, and passed `(LookupOptions, IServiceProvider)` into a constructor that then `new`-ed
its own evaluator. That copied the shape of the surrounding lookup services without asking whether the
menu lookup needed it — it does not, and the shape was wrong for it three times over. Replaced with:

- **`ServiceMenuLookupOptions`** — its own class, registered through the options pattern
  (`AddOptions<T>().Configure(...)`) and injected as `IOptions<ServiceMenuLookupOptions>`. Service menus
  are a self-contained feature over their own containers: a host can want menus without the vehicle
  lookup, or the vehicle lookup without menus. Folding the settings into the general options would make
  every host carry them and tie turning menus on to the whole lookup registration.
- **`AddServiceMenuLookup` / `AddServiceMenuLookup<TCosmosClient>`** — a separate opt-in call. It builds
  its own `LookUpCosmosClient` rather than the container-registered one, because each registration
  carries its own database-name suffix and sharing one instance would make the resolved suffix depend on
  which registration ran first. Everything is `TryAdd`, so it composes and is idempotent.
- **No `IServiceProvider` anywhere.** `CountrySettingsResolver` is `Func<long, ValueTask<…>>`. A host that
  needs its own services inside it configures the option *with* them —
  `AddOptions<ServiceMenuLookupOptions>().Configure<ICountryProvider>(…)` — which is what the options
  pattern exists for, and is pinned by
  `OptionsCanBeConfiguredFromTheHostsOwnServices_WithoutAServiceProviderParameter` so it stays the
  documented migration path for anyone reaching for a provider parameter.

`AddLookupService` therefore does **not** register the menu lookup today, and
`AddLookupService_DoesNotRegisterTheMenuLookup` pins that: a deployment that never provisioned the menu
containers must not be affected until it opts in. **Phase 6 flips this** — once menus are part of the
vehicle lookup result, the general registration calls `AddServiceMenuLookup` itself and
`ServiceMenuLookupOptions` is reachable from the general options. That day is a deliberate edit to those
two tests, not a surprise.

> **That day came — §22.** The test is now `AddLookupService_RegistersTheMenuLookup`. What kept it safe was
> not the registration staying out, but the *section* being opt-in per request: registering touches no
> Cosmos, and an unprovisioned deployment that opts in gets a status rather than an exception.

### Dealer cost: guarded at the chokepoint, and the guard is tested

`MenuGenerationConfig.IncludePartCost` is left at its `false` default, so cost is never populated on a
generated part — it is absent from the object graph rather than stripped from the output, and no mapper
has to remember anything. `DealerCost_NeverReachesTheLookup` asserts both halves: the evaluator never
requests cost, **and** the lookup DTOs have no property whose name contains Cost, Profit or Margin. The
second half is the one that survives a future refactor, since it fails if someone copies a field across
from `MenuLineDTO` — which §15 predicted would be the obvious template.

### Failure modes, and what stays silent

| Condition | Behaviour |
|---|---|
| Model has no documents | `NotFound = true`, no variants — an ordinary answer, not an exception |
| Container not provisioned | `ServiceMenuContainerNotFoundException`, naming `MenuCosmosProvisioning.EnsureContainersAsync` |
| Missing labour-rate mapping / interval / interval group | `ServiceMenuGenerationException` wrapping the generator's `KeyNotFoundException` (O1 preserved) |
| Partition missing a whole document type | **silent** — no periodic lines at all, no error |

`NotFound` describes the PARTITION, not the outcome: a menu whose every variant is deleted, or whose
intervals carry no labour details, exists and generates nothing — a different thing from having no menu,
and a UI renders the two differently.

The container fault is raised rather than folded into an empty result deliberately: an unprovisioned
deployment would otherwise be told "this model has no menu" for every model, permanently, with nothing
anywhere to indicate why. **Phase 6 must decide what the vehicle lookup does with that** — an
unprovisioned menu container should not be able to fail a whole VIN lookup.

> **Decided in §22:** the vehicle lookup contains it and reports `VehicleServiceMenuStatus.Unavailable`.
> This table is unchanged — `GetMenuAsync` still throws for a caller asking about a menu and nothing else.

The last row is the trap §15 predicted for this reader, now pinned by test
(`PartitionMissingLabourDocuments_LosesPeriodicLinesSilently`): with no `MenuLabour` documents there is
nothing to match an interval against, so every scheduled line disappears quietly — the same way a missing
`Include` loses lines on the export side. A model returning standalone services but no scheduled ones
means incomplete replication before it means anything about the catalog.

### Not done here, and why

- **`[TypeScriptModel]` on the lookup DTOs.** They carry `[Docable]` only. Adding the TS attribute
  regenerates web-component types on every build, which belongs with the component that consumes them —
  Phase 6. *(§22: the attribute went on the vehicle lookup's own FLAT types instead, not on these — the
  generator emits same-directory imports, so anything reachable from `VehicleLookupDTO` has to live
  beside it.)*
- **Vehicle-lookup integration.** Still gated on O3 (the derived-Katashiki → authored `BasicModelCode`
  hit rate), which is a measurement against real data and cannot be made from here. *(§22: the gate was
  wrong round — the measurement needs the join. Built, opt-in, with the hit rate reported per lookup.)*
- **A per-model result cache.** See O9 — deliberately refused.
- **Verification against a real catalogue.** Phases 3 and 4 were each verified against the 27-variant
  sample; Phase 5's tests are offline by construction (the round trip needs no Cosmos). The sample
  Functions host now exposes `GET api/menu/{basicModelCode}` alongside its replication endpoints, so
  replicate-then-look-up is one `.http` file away — but it has NOT been run against the emulator yet.
  That is the obvious next confidence step.

---

## 21. Soft deletes now exclude rows from generated menus (BEHAVIOUR CHANGE)

> **This changes the DMS export's output**, not only the lookup's. Read the first section before
> deploying it. **191 tests green** (Phase 5's 161 + 30 new).

Until now a soft-deleted **periodic availability, labour detail, service interval, interval group,
replacement item or standalone group still produced menu lines** — the export's query applies no
soft-delete predicate to any of its includes, and there are no global query filters, so those rows were
folded like live ones. Only the menu item, the replacement-item link, the part, the part country price
and the country labour rate were ever excluded, because those are the only flags the generation contract
carried.

That is now uniform: **a soft-deleted row of ANY replicated table is excluded from generated menus.**

### What a deployment should expect

A catalogue containing soft-deleted rows of the newly-covered tables will see **menu lines disappear from
the next export**, not only from the lookup. That is the intent of the change, but it is a change to
output a DMS has already received, so:

- a catalogue with no soft-deleted rows in those tables is unaffected — the Phase 0 golden snapshots are
  untouched and still pass, which is the evidence;
- a catalogue that does have them should be diffed (export before/after) before the release goes out;
- soft-deleting is now a genuinely destructive act on a menu, where it used to be inert for those tables.

### Where the rule lives: the two adapters, and the generator got cleaner

**The adapters filter; the generation contract holds live rows only.** `MenuGenerationRequest` carries no
`IsDeleted` at all now — not on the variant, period, labour, item, part, price, country rate, interval,
interval group or standalone group — and `MenuCodeGenerator` never mentions deletion. It applies menu
rules (quantity > 0, country matching, code composition) to the data it is handed. Each adapter has a
short, named filter per table — `LivePeriods`, `LiveLabours`, `LiveItems`, `LiveIntervalGroupLinks` — so
the EF and Cosmos versions can be read against each other side by side.

> **This reverses a first attempt** that put the flags on the contract and the rule in the generator. The
> argument for that was drift: two adapters, one rule, nothing structural keeping them equal. The
> argument against — and the one that won — is that the generator should be about generating menus, and
> that "what counts as deleted" belongs where the data is loaded, which is also the only place it can be
> pushed down into a query. Both readings are defensible; what makes this one safe is the test below.

The drift risk is real and is not designed away — it is **tested** away.
`ReplicateThenRead_AgreesWithTheExport_WhenARowIsSoftDeleted` soft-deletes one row of each of the nine
tables in turn, runs the graph through the export AND through replicate-then-read, and asserts identical
output — then asserts the delete actually changed the output, so a rule that silently does nothing on
both sides cannot pass by agreeing on unchanged text. **Add a table to the replication and you must add
it to both adapters and to that test.** The ninth case exists because the eight did not cover the
interval-group link, and that omission was a live bug — see below.

Two things the Cosmos adapter needs that the EF one does not:

- **Deletion is decided once, across every embedded copy.** A master row rides on several documents, so a
  partially landed fan-out leaves them disagreeing (see below). `DeletedMasterIds` collects the ids any
  copy reports as deleted before anything is filtered.
- **A deleted interval group must be stripped from the item's id list as well as from the reference
  dictionary.** The generator resolves those ids with an indexer, so leaving one behind throws instead of
  skipping — which is the correct signal for a *missing* group and the wrong one for a deleted one. Both
  adapters route the ids and the dictionary through one method for exactly this reason.

### The export also filters in SQL now

The include graph moved out of `MenuController.GenerateLinesAsync` into
[`MenuExportIncludes`](ADP.Menus.Data/DataServices/MenuExportIncludes.cs) — next to the
`EfToGenerationAggregator` predicates it mirrors — and every collection it loads is now filtered at the
database. **16 of the 18 tables the query touches carry a delete predicate**, verified from the generated
SQL. Deleted rows no longer travel.

The controller keeps only the ROOT filters (the two delete flags and the brand selection, which is a
per-run choice, not part of the shape).

**The adapter still filters, and that is not redundancy for its own sake** — the two do different amounts
of work:

- Only the database can avoid *loading* a deleted row. That is the whole win.
- Only the adapter can express **"keep the item, drop its deleted standalone group"**. That is a
  reference navigation, and the rule is not "drop the item" — no query can express it. It is
  unavoidably adapter-side, and it is one of the two tables the SQL deliberately does not filter (the
  other is `VehicleModels`, which the export never scoped by deletion).
- The agreement test builds its graph in memory and never runs this query. Move the rule to SQL *alone*
  and that test silently stops guarding the export.

Because the adapter re-applies the same rule, the SQL predicates can only remove rows it would remove
anyway: they cannot change the export's output, only how much of it travels.

**[`MenuExportIncludesTests`](ADP.Menus.Tests/MenuExportIncludesTests.cs) compiles the query to SQL
offline** — `ToQueryString()` runs model building, include expansion and translation, then stops before
opening a connection, so a fake connection string is enough. That closes the two failure modes a compiler
cannot see: `Items` is included three times (a chain must restart from `Include` to branch) and EF throws
at query time if repeated includes carry *different* filters; and a predicate reaching through a
reference navigation has to be translatable or EF throws rather than evaluating client-side. Without it,
the first sign of either would be a 500 from a live export.

Two details worth knowing about that test. `ToQueryString()` returns only the FIRST statement of a
split query, so it collapses the query with `AsSingleQuery()` to assert on the whole graph — which changes
how rows are fetched, never which. And it checks for a delete *predicate* per alias rather than for the
string `IsDeleted`, because every soft-deletable table has that column in its SELECT list; matching on the
column name would pass on a completely unfiltered query.

**Replication is unchanged and must stay unchanged.** Soft-deleted rows are still projected into Cosmos,
flag and all. Skipping them would be worse than useless: only a HARD delete removes a document, so a
skipped soft delete leaves the existing document untouched and stale, still generating its line. Carrying
the flag to Cosmos is what lets the reader's filter take effect at all.

### The one row replication has to filter: the interval-group link

Everywhere else the projection carries the flag and the reader decides. `ReplacementItemServiceIntervalGroup`
— the replacement-item ↔ interval-group link — is the exception, and it was a **bug found by asking
"is every flag mapped?"** rather than by a test.

That link has no document and no flag anywhere in the document shape: it contributes only its group id to
a flat `List<long>`. So a deleted link that gets projected is indistinguishable from a live one, and the
lookup kept pricing parts onto periodic lines the export had stopped pricing. Menu and labour CODES were
unaffected — the id list only decides which items' parts join a periodic line — but the parts and totals
diverged.

`MenuCosmosMappers.IntervalGroupLinks` now filters deleted links at projection. Widening the document
shape to carry a per-link flag was the alternative and was rejected: the flat id list is what makes the
interval-group fan-out an `ARRAY_CONTAINS` rather than a scan (§17), and a deleted link's group genuinely
should stop finding the document. The GROUP's own deletion stays a read-time decision, because a group
can be deleted long after the item was last replicated.

**Two things follow.** Deployments need a re-sweep for this to take effect on existing documents — and
editing a link still does not re-replicate its parent replacement item (§17's known gap), so it lands on
the item's next save or a catch-up pass. And the round-trip theory gained a ninth case,
`intervalGroupLink`; it was verified to FAIL without the mapper fix and pass with it, so the gap cannot
reopen silently.

### The one judgement call: a deleted standalone group

A soft-deleted standalone item group is treated as **no group**, so its items fall back to individual
standalone lines rather than disappearing. Deleting a *grouping* withdraws the grouping, not the items —
they are separate rows, still sellable, and still carry their own operation and labour codes. The
alternative (dropping them entirely) would silently remove sellable services because someone tidied up a
grouping. Pinned by `SoftDeletedStandaloneGroup_FallsBackToUngroupedLines` so it is a decision rather
than an accident.

### One thing the change exposed: disagreeing embedded copies

A master row is embedded in several documents — an interval group rides on every `MenuLabour` and every
`MenuItem` that serves it — and the reference dictionaries are last-write-wins. The first version of
`SoftDeletedIntervalGroup_…` failed because of it: flagging the group on the labour document alone was
undone by a stale item copy written later in the walk.

That is not a test artefact. A fan-out can land partially — §18 recorded a sweep silently writing 1060 of
1188 documents — so disagreeing copies are a real state, and deciding per copy would make the answer
depend on which document a given code path happened to be holding.

**Deletion is therefore decided once and stickily: any copy saying deleted wins** (`DeletedMasterIds`,
computed across every embedded copy before anything is filtered). A delete is monotonic — nothing
un-deletes — so this cannot be wrong in the way "whichever copy we read last" can, and it errs toward
withholding a menu code rather than issuing one for a withdrawn row. Other fields stay last-write-wins;
they have no safe direction to err in.
`PartiallyPropagatedDelete_IsStickyRegardlessOfDocumentOrder` runs it in both document orders. The EF
adapter needs none of this: its graph has one instance per row, so its copies cannot disagree.

### What the fixtures now say

`MenuGraphFixture` (the EF graph) is unchanged — it still contains a soft-deleted item, link, part,
country price and country labour rate, and the Phase 0 goldens still run through it. That they are
untouched and green is the evidence the change is surgical.

`MenuGenerationRequestFixture` (the hand-built layer-1 request) **lost those rows entirely**, because
there is no longer anywhere to express them: the contract has no `IsDeleted`. It is no longer a
row-for-row twin of the EF fixture, and that asymmetry is the design showing through rather than a
mistake — the deleted rows are the adapter's business now, asserted by
`SoftDeletedRows_AreFilteredOutByTheAdapter_NotLeftToTheGenerator`, which checks they are absent from the
REQUEST rather than merely absent from the lines. Asserting on the request is what pins where the rule
lives; asserting on the lines would pass either way.

---

## 22. Phase 6 — the vehicle lookup carries the menu (DONE)

**225 tests green** (§21's 191 + 34 new; plus 2 opt-in sample-seeding tests that need a local SQL database).
A VIN lookup can now return the model's service menu.

```
LookupAsync(vin, { ServiceMenuOptions = { Include = true } })
  ─▶ VehicleLookupDTO.BasicModelCode      the derived join key (Katashiki → basic model code)
  ─▶ VehicleServiceMenuEvaluator          → ServiceMenuLookupService (the Phase-5 pipeline, unchanged)
                                          → flatten variants into one list, stamp a Status
  ─▶ VehicleLookupDTO.ServiceMenu         → the host's own UI
```

**Back end only — no web component.** The original Phase-6 line called for one; it was built and then removed
deliberately (below). Everything a renderer needs ships: a flat list already in display order, a status that
distinguishes the empty cases, and generated TypeScript types.

| File | Role |
|---|---|
| `DTOsAndModels/VehicleLookup/VehicleServiceMenuDTO.cs` | The section: `Status`, the key it tried, country/language/rate, the flat services |
| `…/VehicleServiceMenuLineDTO.cs` + `…PartDTO.cs` | LAYER 3, flat. `[TypeScriptModel]` — the generated TS contract for the response |
| `…/VehicleServiceMenuStatus.cs` | Found / NotFound / NoBasicModelCode / Unavailable / NotRegistered |
| `…/VehicleServiceMenuRequestOptions.cs` | Include + country + transfer rate, grouped on `VehicleLookupRequestOptions.ServiceMenuOptions` |
| `Evaluators/VehicleServiceMenuEvaluator.cs` | The opt-in, the join, the containment, the flattening |
| `Extensions/IServiceCollectionExtensions.cs` | `AddLookupService` now calls `AddServiceMenuLookup` |
| [`VehicleServiceMenuEvaluatorTests.cs`](ADP.Menus.Tests/VehicleServiceMenuEvaluatorTests.cs) | The join, the containment, the contract a renderer sees |
| [`VehicleMenuLookupFunctions.cs`](samples/ADP.Menus.Sample.Functions/Functions/VehicleMenuLookupFunctions.cs) | `GET api/vehicle/{vin}` — the join, end to end, in the sample |
| `menus/service-menu-lookup.md` (ADP.Docs) | The host-facing page |

### The section is opt-in, and that is the whole safety story

`VehicleLookupRequestOptions.ServiceMenuOptions` is null by default, so upgrading the package changes no
existing response. It is per-**request** rather than per-deployment because the cost is per **vehicle**: one
single-partition read plus a fold. A bulk lookup would otherwise pay that once per VIN, silently, for a
section most of its callers never render.

That default is also what let this ship ahead of O3. A deployment turns the section on for the request that
renders a menu, measures its own hit rate from the responses, and decides. Nothing had to be measured
first, which is the trap the original gating created — the measurement needs the join, and the join is what
this phase builds.

### Where the switch lives

`ServiceMenuOptions.Include` turns the section on, and the country and transfer rate sit beside it on the
same object. `Include = true` alone is the common call: request's language, menu options' default country,
transfer rate 1.

The first cut put the flag on `VehicleLookupRequestOptions` as `IncludeServiceMenu`, beside the options
object — which is how the file already pairs a gate with a payload (`InsertSSCLog` + `SSCLogInfo`,
`InsertCustomerVehcileLookupLog` + `CustomerVehicleLookupLogInfo`). **That shape lets the two disagree**,
and the disagreement is silent in the worst way: fill in a country and a transfer rate, miss the flag on the
other object, and the settings do nothing. It is the same failure this phase argued against for the transfer
rate itself — "a value that is silently ignored is worse than one that is absent" — so reproducing it a
level up was inconsistent. The convention was not a good enough reason.

The cost is that "off" has two spellings: a null object, and an object with `Include` false. They mean the
same thing and the DTO says so. That redundancy is cheap; a caller that configures a menu and gets none
is not.

The gate itself moved into `VehicleServiceMenuEvaluator`, which now returns **null** when the request did
not ask. That keeps "did the caller want a menu" with every other menu decision instead of inline in
`VehicleLookupService`, and — the practical reason — makes it testable without standing up a whole vehicle
lookup. `NoSection_UnlessTheRequestAsksForOne` covers the case that matters: options supplied with
`Include` false must stay OFF. "Options were provided, so they must want it" is the obvious simplification
of that line, and it would quietly add a partition read per vehicle for every caller that set a country
without asking for a menu.

Language is deliberately **not** on the options object — it is `LanguageCode`, because a vehicle lookup
rendering in one language with menu codes in another is a bug, not a configuration.

### Transfer rate: the caller wins, and that is a change to the existing path

`ServiceMenuOptions.TransferRate` is caller-supplied and reaches the fold. Making it *work* required
inverting the precedence `ResolveConfigAsync` had used since Phase 5:

```
BEFORE:  settings?.TransferRate ?? request?.TransferRate ?? 1m     // the resolver always won
AFTER:   request?.TransferRate ?? settings?.TransferRate ?? 1m     // an explicit value wins
```

Without the inversion the field would be a no-op for exactly the deployments that configure things
properly — a setting that looks wired and does nothing, visible only as money that does not add up. The old
rule also meant a `GetMenuAsync` caller could set a transfer rate and be silently ignored, which was its own
trap; `ServiceMenuLookupRequest.TransferRate` documented that as intended behaviour and no longer does.

**This changes the existing menu-only path too**, and one rule for the setting is the point — a second
precedence for the vehicle path would be a bug waiting to happen. The blast radius is a host that both wires
a `CountrySettingsResolver` *and* passes a transfer rate, which previously got the resolver's value.
`CountrySettingsResolver_OverridesTheRequest` became `CountrySettingsResolver_SuppliesTheDefaults` plus
`AnExplicitTransferRate_WinsOverTheResolver`.

`UsePrimaryLabourRate` stays resolver-owned outright. The request has no way to express it, and it mirrors
the menus host's country normalisation rather than a caller's preference — the two halves of
`ServiceMenuCountrySettings` are deliberately no longer symmetric, and a test says so.

**The exposure is real and is the host's to manage.** The transfer rate scales the consumable, so it moves
the price quoted to a customer; it moves no menu or labour CODE, because the labour-rate mapping is always
keyed by the variant's primary rate. An endpoint that binds it from a query string is letting its callers
move the price. Both the DTO and the docs page say so, and a host that wants the resolver to be the sole
authority simply does not expose the field. That is a better boundary than the lookup silently discarding
what it was handed.
`TheRequestsTransferRate_ScalesTheConsumable` asserts on the generated LINE, not merely on the echoed
`TransferRate` — echoing a number the fold never saw would pass the weaker assertion.

### A menu fault cannot fail a VIN lookup

§20 left this open in as many words: *"an unprovisioned menu container should not be able to fail a whole
VIN lookup."* It cannot. The evaluator contains `ServiceMenuContainerNotFoundException`,
`ServiceMenuGenerationException` and `CosmosException`, and reports `Status = Unavailable`.

**Contained, not swallowed** — the status is in the response, so an unprovisioned deployment says so on
every vehicle rather than looking like a catalog nobody authored. And `ServiceMenuLookupService.GetMenuAsync`
is unchanged: a caller asking for a menu *and nothing else* still gets the exception. The asymmetry is the
point. The same fault is worth raising to one caller and not the other.

Containment stops at the menu subsystem's enumerated faults. Anything else propagates, and one case of that
is worth stating plainly: a host's `CountrySettingsResolver` now runs inside the VIN lookup, so a resolver
that throws takes the lookup with it. That is deliberate — a section quietly "unavailable" forever is a
worse failure than a loud one — and it is documented rather than defended against.
`AnUnexpectedFault_IsNotContained` pins the boundary.

### Five statuses, because an empty list means five things

`NotFound` is the O3 miss. `NoBasicModelCode` is **not** a miss — the vehicle never had a key to join on,
and counting it as one would understate the code agreement. `Unavailable` and `NotRegistered` are both
"could not be consulted" but their fixes differ (provision + sweep, versus register the lookup), which is
the only reason they are separate; a UI should word them identically, because the difference means nothing
to a customer. `Found` with an empty list is a menu that exists and generates nothing — the
distinction §20 built `NotFound` for, carried through.

Zero is `NoBasicModelCode`, not `Found`. A default-constructed or older-payload section must not claim a
menu it never looked up.

### Flat, and why it is a separate type rather than a reuse

The nested shape (`ServiceMenuLookupDTO` → variants → lines) is right for a caller *choosing* a variant.
A caller that started from a VIN is not choosing one; it wants a list it can render, so the variant travels
on the line and a UI groups client-side if it wants to. Order is preserved exactly — per variant, scheduled
by distance, then standalone — so a UI can render the list straight through.

It is a **separate type** for a mechanical reason worth recording, because it looks like duplication:
**the TypeScript generator emits same-directory imports.** Every type reachable from `VehicleLookupDTO` has
to live in `DTOsAndModels/VehicleLookup/`, or the generated `.ts` imports a path that does not exist — and
it fails in the browser, not at build. Enums are the exception: they are inlined as string unions, so
`ServiceMenuLineType` is reused as-is. `EveryTypeScriptTypeReachableFromTheVehicleLookup_LivesBesideIt`
pins this so a later "simplification" cannot quietly break the generated model.

Duplication of a DTO shape is a real cost and it is paid deliberately, with a test:
`TheFlatShape_CarriesEveryFieldOfTheNestedOne` fails if a field is added to the menu lookup's line or part
and not to the vehicle's. Without it, a new field would simply never reach the vehicle lookup's callers —
data loss with no error anywhere.

### Two bugs this phase surfaced

**1. `ServiceIntervalValueInMeter` is in KILOMETRES.** The name is `ServiceInterval.ValueInMeter`'s, carried
through five layers, and §20's DTO documented it as *"the interval's distance in metres"*. It is not: the
catalogue authors `ValueInMeter = 20000` next to `FullName = "20,000 KM"` (see the sample seed data). A
renderer that trusted the doc comment and divided by 1000 would quote a 20,000 km service as **20 km** — to
a customer. Nothing before this phase displayed the value, which is why it survived. The doc comments are
corrected on both DTOs, and `formatDistance` is a named function carrying the reason rather than an inline
expression. The property is **not renamed**: it matches the source column, and a rename would break every
consumer to fix a comment.

**2. `ServiceMenuLineDTO.LineType` serialized as a number.** Harmless while nothing typed it — but the
generated TypeScript renders an enum as a string union (`'Periodic' | …`), so the first `[TypeScriptModel]`
on this chain would have made the generated model quietly wrong: a `switch` that never matches. Both line
types now carry `[JsonConverter(typeof(JsonStringEnumConverter))]`, matching the repo's own convention on
every other enum a lookup DTO exposes. `TheEnumsSerializeAsStrings_MatchingTheGeneratedTypeScript` pins it.

### The registration flip, and the one thing it had to carry across

`AddLookupService` now calls `AddServiceMenuLookup<TCosmosClient>()`. §20 predicted this would be *"a
deliberate edit to those two tests"*; `AddLookupService_DoesNotRegisterTheMenuLookup` became
`AddLookupService_RegistersTheMenuLookup`, which also asserts `VehicleLookupService` resolves — the menu
service is an **optional** constructor parameter, so a registration the container cannot satisfy does not
fail, it silently passes null and every section reports `NotRegistered` forever.

The non-obvious part: `CosmosDatabaseNameSuffix` exists on **both** option classes and they do not know
about each other. A dev pointing the whole lookup at `-alt` databases means the menu containers too, so the
general registration seeds the menu one — with `??=`, so an explicit setting wins in either registration
order. Pinned both ways round.

### Registering menus without a way to configure them was a money bug

The first cut registered the menu lookup from `AddLookupService` and left configuring it to a second,
separate `AddServiceMenuLookup` call. That looks tidy and is quietly dangerous: **the defaults then apply to
any host that does not know the second call exists** — country 0 and no `CountrySettingsResolver`, which per
O6 charges a single-country deployment a country labour rate where its own DMS export charges the variant's
primary rate. Registering a feature while hiding its configuration makes the wrong answer the one you get
for doing nothing.

`LookupOptions.ConfigureServiceMenu` closes that: the settings are reachable from the one call every host
already makes, and `AddLookupService` forwards it into `AddServiceMenuLookup`.

**It is an `Action<ServiceMenuLookupOptions>`, not a `ServiceMenuLookupOptions` instance**, and the
distinction is load-bearing rather than stylistic:

- An instance would give the menu settings **two homes** — `LookupOptions.ServiceMenu` and
  `AddServiceMenuLookup(o => …)` — with order-dependent merge semantics, and either a field-by-field copy
  that silently drops whatever member is added next, or a whole-instance assignment that bypasses
  `AddOptions` entirely.
- Bypassing `AddOptions` would break
  `AddOptions<ServiceMenuLookupOptions>().Configure<TDependency>(…)`, which is the *supported* way to build
  a `CountrySettingsResolver` out of the host's own services without an `IServiceProvider` parameter (§20,
  and `OptionsCanBeConfiguredFromTheHostsOwnServices_WithoutAServiceProviderParameter` pins it).
  `LookupOptions` is constructed by the caller's lambda before DI exists, so an instance living there could
  never carry such a resolver.

As a delegate it is simply one more `Configure` step on the same builder: it composes with
`AddServiceMenuLookup` and with `.Configure<TDependency>` in registration order, last writer winning, and
there is still exactly one home for the settings themselves.

`AddServiceMenuLookup` stays — it is the only entry point for menus *without* the vehicle lookup, and
`AddLookupService_ComposesWithAnExplicitMenuRegistration` keeps the two working together in either order.

### The web component was built, then removed

A `<vehicle-service-menu>` Stencil component shipped in the first cut of this phase — variant picker,
scheduled services in odometer order, standalone services, parts behind an expander, four locales. It was
**deliberately removed**: rendering a service menu is the host's, and a component in this repo would fix
choices (a currency, a layout, a variant-picker idiom) that belong to whoever is quoting the customer.

What the back end keeps, because it is the part a renderer cannot re-derive:

- **`Services` is already in display order** — per variant, scheduled by odometer reading, then standalone —
  so a UI can render the list straight through. The variant travels on each line for client-side grouping.
- **`Status` distinguishes the empty cases.** Collapsing them into "no data" throws away the only signal
  separating "no menu published" from "the menu subsystem is misconfigured".
- **`HasUnpricedParts` marks an understated total.** A part with no price row is priced 0 rather than
  dropped. Quoting that total as if it were complete is the failure the whole chain exists to prevent, and
  the last mile of that chain is now the host's.
- **Generated TypeScript types.** `[TypeScriptModel]` stays on the flat DTOs — not for a component, but
  because the response carries them and the NPM package's `VehicleLookupDTO` type would otherwise import a
  file that does not exist. That is a hard constraint of the generator, not a preference.

Three renderer traps are written up on the docs page rather than lost with the component: word each status
separately, mark rather than quote an understated total, and do **not** divide
`ServiceIntervalValueInMeter` by 1000.

### Not done here, and why

- **The real-data hit rate (O3).** Unchanged: it needs a deployment's own catalogue and traffic. The
  instrument ships; the number does not.
- **A menu on the bulk lookup by default.** `ServiceMenuOptions.Include` is honoured there, so a caller
  *can* — but it is N partition reads, and nothing suggests a reporting caller wants menu codes.
- **`[TypeScriptModel]` on the nested `ServiceMenu` DTOs.** Only the flat types are generated, because only
  they are on the vehicle lookup's response. Generating a second, unused TS shape of the same data is how a
  generated folder starts accumulating types nobody imports.
- **A web component.** Removed on purpose — see above. Nothing in `ADP.WebComponents` changed except the
  three generated type files the `[TypeScriptModel]` attributes produce.
- **A demo mock carrying a menu.** `mocks/generated/standard-dealer/vehicle-lookup.json` comes from the test
  data generator, which has no menu Cosmos, so it has no `serviceMenu`. Nothing depends on one today; a host
  building a UI wants its own fixture anyway.
- **Vehicle containers in the sample.** `GET api/vehicle/{vin}` reads `CompanyData`/`Vehicles`, which menu
  replication does not fill — that data comes from a different pipeline. The endpoint answers 503 naming the
  missing containers rather than pretending; provisioning a second pipeline's containers to make one sample
  endpoint runnable would be its own project.

### The sample reaches the menu from both ends

`GET api/menu/{basicModelCode}` was already the read path. `GET api/vehicle/{vin}` adds the part that path
cannot show: the derived key. The response is trimmed to `katashiki` → `basicModelCode` → `serviceMenu`,
because those three next to each other are the whole O3 question, and a full `VehicleLookupDTO` would bury
them. `Program.cs` moved from `AddServiceMenuLookup` to `AddLookupService` with `ConfigureServiceMenu`,
which is also the worked example of that option.

### One thing found while the component existed, worth keeping

Re-running `npm run create:locale-mapper` **drops the `forms*` entry**, and `getSharedFormLocal` requests
exactly that key — every form would throw *"Locale file not found for component: forms\*"*. The generator
only emits a `<name>*` alias for a folder with more than one locale sub-folder, and `locales/forms/` no
longer has any, so the checked-in mapper is deliberately ahead of the generator. Nothing in this phase
touches that file any more, but the landmine is real and unrelated to menus: **do not regenerate
`src/locale-mapper.ts` until the generator emits `forms*` again.**
