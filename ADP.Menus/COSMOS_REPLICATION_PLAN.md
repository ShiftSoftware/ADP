# Menu → Cosmos Replication — Implementation Plan

> Status: **Phases 0–2 and Phase 3 step 1 implemented; Phase 3 step 2 and Phases 4–6 still plan.**
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

ADP.LookupServices/Lookup.Services/  (netstandard2.0 — the reader, FUTURE phase)
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
    public bool HasStandaloneItems { get; set; }
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

> **Partly superseded by §16.** `ServiceMenuReferenceCache` is **dropped** — the documents are fully
> denormalized, so the read is a single partition query and there is no reference partition to cache.
> The `CosmosToGenerationAggregator` signature below changes accordingly: it takes the four
> `ServiceMenus` document types and builds `MenuGenerationReferenceData` from what they already carry.
> Everything else in this section stands.

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
| O3 | Derived Katashiki code vs authored `Menu.BasicModelCode` match rate in real data. | Measure first (host-side/ad-hoc; keep report in private planning). **Gates Phase 6.** |
| O4 | Reference-cache freshness/invalidation in the reader. | **CLOSED by §16.** There is no reader-side cache: the documents are fully denormalized and `UpdateReference` keeps the copies fresh. See §10. |
| O5 | Do all replicated tables need the two `IShiftEntityReplication` columns? | **RESOLVED (Phase 3 step 1): yes, all 10.** The trigger is constrained on `IShiftEntityReplication`, so a table without the columns is simply never replicated. |
| O6 | `transferRate` / country at read time. | `LookupOptions` resolver → `MenuGenerationConfig`; `Consumable` stored unscaled, scaled in the generator (the generic model already does this). **Phase 5.** |
| O7 | `GetAllowedTimeText` culture sensitivity (feeds labour code). | **DECIDED: leave as-is.** Ported verbatim, ambient culture included. Judged not worth the risk of changing codes already issued to a DMS for a case the deployments do not hit. Pinned by test so the behaviour is at least visible. |
| O8 | `MenuLabourDetails.FirstOrDefault` nondeterminism. | **RESOLVED structurally, no behaviour change.** The generic input is `List<>`-ordered, so "first match" is now a function of the aggregator's ordering rather than of EF/`HashSet` iteration. No `OrderBy` was added — that would have changed output. |
| O9 | Read-time cost: full fold every lookup. | **REDUCED by §16.** The read is one single-partition query with no second round trip; only the generation fold remains per lookup. Optional short per-model result cache. **Phase 5.** |
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
  - **Step 2 — still to do:** `MenuReplicationService` (per-model / master / full backfill), the
    `UpdatePartsPrice` and delete-cascade gaps (O2) plus the other gaps §17 lists, and
    `CosmosToGenerationAggregator`.
- **Phase 4 — host wiring + backfill + provisioning.** Provision all 7 containers (§17); register the
  trigger + `AddMenuReplications`; run the backfill; verify.
- **Phase 5 — read side (lookup, future).** `ServiceMenuLookupService` (one partition read →
  `CosmosToGenerationAggregator` → `MenuCodeGenerator` → lookup DTO). No reference cache — §16. Tests:
  aggregation equals the golden generic request; normalization; missing container; a partition missing
  a document type.
- **Phase 6 — vehicle-lookup integration** (gated on O3): evaluator + flat `[TypeScriptModel]` DTO +
  web-component section; measure/monitor the join-key hit rate.

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
filtered them out, which silently dropped whole periodic lines the export still emits. The mappers are
now pure projections — they copy, they do not decide — matching `EfToGenerationAggregator` exactly. The
only filtering left is on the labour-rate and brand mapping catalogues, which the export filters too.

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
