# Conventions — mapper migration recipe & audit checklist

Everything here is derived from the completed `ADP.Menus` migration (commit `14caf7c9`), the state
of the files it touched, and the emitted `.g.cs` under `ADP.Menus/ADP.Menus.Data/obj/`.

**Reference implementation on disk: `ADP.Menus/`.** When this document and the Menus tree disagree,
read the Menus tree — but check §7 first, because parts of that migration were left unfinished and
must not be copied as precedent.

---

## 1. What the upgrade forces

- AutoMapper is gone from the framework. `ShiftSoftware.ShiftEntity.EFCore.AutoMapper` was never
  published. **There is no fallback and no partial adoption.**
- **No new package is added.** The generator ships as an analyzer inside `ShiftSoftware.ShiftEntity`
  and is pulled transitively by `ShiftEntity.EFCore`. It runs for package-mode consumers.
- **Nothing is registered by hand.** Every generated mapper registers itself into
  `ShiftEntityMapperRegistry` from a `[ModuleInitializer]`. Nothing goes into DI.
- The generator is **already running today at `2026.7.31.1`**, alongside AutoMapper, and already
  emits advisory diagnostics — the 10 baseline `SHENGEN004` warnings in `ClaimableItems.Data` (5),
  `Surveys.Data` (3) and `WarrantyClaims.Data` (2). **Those are pre-existing noise, not upgrade
  fallout.** They are, however, an exact preview of which members will need `ForView` once
  AutoMapper is removed. Each step file lists its group's warnings verbatim.

### The mapping-mode switch does not exist

The framework's own migration notes assume a `ShiftEntityOptions.MappingMode` switch
(`AutoMapperFirst` / `GeneratedFirst` / `GeneratedOnly`) to stage the flip. **That switch is not in
`2026.8.30.1`.** There is no gradual cutover. Do not plan around one.

---

## 2. Per-group sequence

Work in this order. It is the order the Menus migration actually took, with the two things it got
wrong corrected.

1. **Read the group's profiles end-to-end first.** Build a table keyed by **repository**, not by
   `CreateMap`. Do *not* drive this from a `CreateMap` codemod: triples exist that have no
   `CreateMap` at all and still need a mapper, and `CreateMap`s exist that serve no triple.
   Give the reverse-map `Ignore()` list its own column (Trap 3-write) and the forward-map `Ignore()`
   list another (Trap 3-read). Build both lists **before** touching anything.
2. Bump every `ShiftSoftware.Shift*` reference in the group. This is the group step's own first
   commit, using the package-line table in that step file — there is no solution-wide bump.
3. Delete the `AutoMapperProfiles/` directory whole, including any `.keep`, and the sample's
   profiles.
4. Remove `o.AddAutoMapper(...)` from the group's `*ApiExtensions.cs`, and
   `x.AddAutoMapper(...)` / `x.AddShiftIdentityAutoMapper()` / `services.AddAutoMapper(_ => { })`
   from every sample/host `Program.cs`. Update the surrounding comments, do not just delete lines.
5. Port any `AutoMapper.IMapper` injection sites (§6). Separate, larger work — budget for it.
6. Rewrite the Cosmos replication delegates (§6b) — these are a **compile break**, not a warning.
7. Build. Work the `SHENGEN` list top to bottom, **`SHENGEN010` first** — it is the data-corrupting
   one.
8. **Emit and audit the generated code** (§4). Traps 1, 2, 3-write and 3-read produce **zero**
   diagnostics. The build log is not the audit.
9. Run the group's tests.
10. `grep -rn "AutoMapper\|AfterMap\|mapping profile\|MappingProfile"` over the group and fix the
    prose. Menus did not finish this; see §7.

---

## 3. The rewrite itself

### Where configuration goes

The hook is `ShiftRepositoryOptions<TEntity,TListDTO,TViewDTO>.UseGeneratedMapper(...)`, called from
the repository's `base(db, …)` options lambda. Three shapes, all present in Menus:

| Existing constructor | What to do |
|---|---|
| `base(db)` — no options lambda | add one: `base(db, x => x.UseGeneratedMapper(map => map …))` |
| `base(db, x => x.Something(...))` — single expression | convert to a block lambda, append the call |
| `base(db, i => { … })` — already a block | append `i.UseGeneratedMapper(...)` using that lambda's existing parameter name |

All four `ADP.Surveys` repositories are the first shape (`base(db)`), so every Surveys repository
that needs customization gains an options lambda it does not have today.

**Repositories that need no call at all.** Four of Menus' ten got zero changes even though the old
profile had `CreateMap`s for them. Convention covers them. **Do not add an empty
`UseGeneratedMapper(map => map)` to be tidy** — the repository resolves the generated mapper from the
registry on its own.

### Builder surface

| Direction | Customize a member | Compose a child | Exclude |
|---|---|---|---|
| `MapToView` — in-memory, full C# | `ForView(d => …, e => …)` | `ForViewChild` / `ForViewChildren` | `IgnoreView` |
| `MapToList` — **SQL projection, expression-only** | `ForList(d => …, e => …)` | `ForListChild` / `ForListChildren` | `IgnoreList` |
| `MapToEntity` — upsert | `ForEntity(e => …, dto => …)`, plus an **existing-aware** overload `(dto, entity, ctx) => …` | `ForEntityChild(ren)` — **REPLACE-WITH-NEW** | `IgnoreEntity` |
| `CopyEntity` — ReloadAfterSave | `ForCopy` | — | `IgnoreCopy` |
| all four | — | — | `Ignore` (view-DTO selector, matched by name) |
| whole map | `AfterEntity((dto, entity, ctx) => …)`, `MaxDepth(n)`, `CaseSensitive()` | | |

`ForList` is spliced into the single generated SQL projection, so OData `$filter` / `$orderby` /
paging keep working — **but it must be EF-translatable. No method calls.** This is the constraint
behind SPIKE-3.

`Ignore*`, `MaxDepth` and `CaseSensitive` are **build-time markers**: the generator reads the call
syntax and bakes the decision. A conditional or non-literal registration is `SHENGEN005` /
`SHENGEN009`, not a silent no-op.

### What to delete outright

Conversions the convention now performs for free — verified against emitted code:

| Old profile line | Emitted convention |
|---|---|
| `MapFrom(s => s.SomeID.HasValue ? s.SomeID.ToString() : null)` | inlined `long? → string` in the list projection |
| `MapFrom(s => new ShiftEntitySelectDTO { Value = s.SomeID.ToString() })` | `MappingHelpers.ToSelectDTO(entity.SomeID)` — **and it fills `Text` too** |
| reverse: `MapFrom(s => s.Nav != null ? s.Nav.Value : null)` | `MappingHelpers.ToNullableForeignKey(dto.Nav)` |
| reverse: `MapFrom(src => src.SomeID.ToLong())` | `MappingHelpers.ToLong(dto.SomeID)` |
| `.ForMember(x => x.ID, x => x.Ignore())` on a `ReverseMap` | already gone — the generator never writes `ID` or any `ShiftEntity<>` base member in `MapToEntity` |
| a bare `CreateMap<Entity, ListDTO>()` with no `ForMember` | delete; the generator emits the whole projection |

Note the third row is a *behaviour improvement*, not a no-op: the selector now carries `Text` where
the old profile left it null. That will show up as a diff in the harness. It is expected — record it
as an accepted change, do not suppress it.

### `AfterMap` → `AfterEntity`

Signature is fixed and the argument order **flips**: `AfterEntity((dto, entity, ctx) => { … })` —
DTO first, entity second. Old `AfterMap((src, dest, ctx) => …)` bodies transcribe with `src`→`dto`,
`dest`→`entity` and nothing else changes.

**The one thing that does not transcribe:** `ctx.Mapper.Map<T>(item)` and
`ctx.Mapper.Map(item, existing)`. `MappingContext` has **no** `Mapper`. Every such call must be
written out by hand — as an inline object initializer where the child is small, or as a
`private static void ApplyX(XDto src, X dest)` helper on the repository where the child had its own
`AfterMap`.

### Tracked children with required FKs

Every `.ForMember(x => x.SomeCollection, x => x.Ignore())` + `AfterMap` pair becomes
`IgnoreEntity(e => e.SomeCollection)` + `AfterEntity`, mechanically. ShiftEntity forces
`DeleteBehavior.Restrict` on non-ownership FKs, so severing one throws `HandleConceptualNulls`
rather than deleting a row. `SHENGEN010` flags these, but reconcile by **business key**, never
replace-with-new.

---

## 4. Emit and audit the generated code

**This is the part that catches the silent traps. It is not optional.**

```bash
# Delete obj/ first — see the orphan trap below.
rm -rf ADP.Surveys/ADP.Surveys.Data/obj
dotnet build ADP.Surveys/ADP.Surveys.Data -p:EmitCompilerGeneratedFiles=true
```

`EmitCompilerGeneratedFiles` is not set in any csproj, props or targets in this repo. Pass it on the
command line, per data project.

Output lands in:

```
<Group>/<Group>.Data/obj/Debug/net10.0/generated/
  ShiftSoftware.ShiftEntity.SourceGenerator/
    ShiftSoftware.ShiftEntity.SourceGenerator.ShiftEntityMapperGenerator/
```

Two file-name shapes — triples `Generated_<Entity>_<ListDTO>_<ViewDTO>_<hash>.g.cs`, pairs
`ShiftSoftware_ShiftEntity_GeneratedMappers_Generated_Pair_<ChildEntity>_<ChildDTO>_<hash>.g.cs`.

### The orphan trap — this bites

MSBuild never prunes `obj/`, and the generator **changed its pair file-naming scheme** between
releases. Both an old-scheme and a current-scheme file can sit there for the same pair. In the live
Menus tree, six such orphans exist right now. **Reading an orphan yields a confident wrong
conclusion.** Always `rm -rf obj/` before the emit build, or sort by timestamp and read only the
newest generation.

### What to read

| Checking | Where |
|---|---|
| view direction | `MapToViewGenerated(entity, context)` |
| **write direction — the audit target** | `MapToEntityGenerated(dto, existing, context)` |
| list SQL projection | `__shiftListProjection` |
| ReloadAfterSave copy | `CopyEntityGenerated(source, target, context)` |
| whether your fluent call was *seen at build time* | `__shiftBakedCustom` / `__shiftBakedIgnored` string arrays near the top |
| child composition | `__shiftPair_<hash>.Map(…)` / `.MapBack(…)` call sites |

`__shiftBakedCustom` / `__shiftBakedIgnored` are the fastest sanity check that a builder call
actually took effect. If a member you configured is absent from both arrays, your call did not bake.

---

## 5. Per-map audit checklist

Run once **per `CreateMap` in the old profile**, and once **per repository triple that had no
`CreateMap`**.

### Trap 1 — auto-composed child collections do not filter soft-deleted rows

- **Grep the old profile for:** `IsDeleted` inside any `MapFrom` or `.Where(`; and any collection
  member with **no** `ForMember` at all whose child entity has `IsDeleted`.
- **Look for in the emitted code:** a bare deep composition with no predicate in
  `MapToViewGenerated` / `__shiftListProjection`.
- **Fix:** `ForView` (replace the whole collection) or `ForViewChildren` (keep the pair, filter the
  source enumerable in the second argument).
- **Decide, do not assume.** Menus deliberately left two collections unfiltered *because the profile
  did not filter them either*. **Parity with the old profile is the standard — not "always
  filter".**

### Trap 2 — pair mappers apply name conventions to the child's own `ID`

- **Grep the old profile for:** any `MapFrom` building a DTO whose `ID` or `Value` comes from a
  **foreign key** on a link row (`s.SomethingID.ToString()`) rather than from `s.ID`.
- **Look for in the emitted pair file:** `dto.ID = source.ID.ToString();` — the link row's own PK.
- **Fix:** take the collection over with an explicit `ForView`.
- Silent and total: the form round-trips ids of the wrong entity, and the value is a well-formed,
  plausible hash id. Nothing but a value-level diff distinguishes it from correct.

### Trap 3-write — `Ignore()`d reverse-map members are now written from the request body

- **Grep the old profile for:** every `.ForMember(x => x.Y, x => x.Ignore())` appearing **after** a
  `.ReverseMap()`.
- **Look for in the emitted code:** an `existing.Y = …` line in `MapToEntityGenerated`.
- **Fix:** `IgnoreEntity(e => e.Y)`, plus `AfterEntity` if the member still needs setting from
  repository-derived state.
- **No diagnostic fires for this.** `SHENGEN008` reports the opposite asymmetry. Manual diff, every
  time.

### Trap 3-read — `Ignore()`d **forward**-map members are now populated by convention

**This trap is not in the Menus taxonomy. It was found during this survey and it is the most severe
item in the plan.**

- **Grep the old profile for:** every `.ForMember(x => x.Y, x => x.Ignore())` on a **forward**
  (entity → DTO) map, especially on a **list** DTO.
- **Look for in the emitted code:** a `Y = entity.Y` line in `__shiftListProjection` or
  `MapToViewGenerated`.
- **Fix:** `IgnoreList(d => d.Y)` / `IgnoreView(d => d.Y)`.
- **Why it is worse than the others:** this is not data *loss*, it is data *exposure*. A field
  deliberately blanked for a lower-privilege audience becomes populated. Status 200, correct shape,
  no diagnostic — `SHENGEN008` will not fire because the member *is* mapped now, and
  `SHENGEN004`/`007` will not fire because nothing is unmapped.
- **The known instance is in `ADP.WarrantyClaims`** — see `05-warranty-claims.md`. Audit every
  forward-map `Ignore()` in every group anyway.

### Trap 4 — tracked children with required FKs

- **Grep the old profile for:** `AfterMap` blocks that `Remove()`/`Add()` into a collection, and any
  `ForMember(collection, Ignore())`.
- **Look for:** `existing.Children = … MapBack(d, new Child(), ctx)` — replace-with-new.
  `SHENGEN010` also flags it.
- **Fix:** `IgnoreEntity` + `AfterEntity` reconciliation by business key.

### Trap 5 — repository code that *narrates* the mapper

Comments and doc comments that describe "the AutoMapper profile" / "the AfterMap" are now wrong.
`grep -rn "AutoMapper\|AfterMap\|mapping profile"` over the group when you finish and rewrite the
prose. Menus did not finish this — §7.

---

## 6. Things outside the builder

### 6a. `AutoMapper.IMapper` injection sites

Four sites outside `ADP.SyncAgent`, all in groups yet to migrate:

| Site | Call | Disposition |
|---|---|---|
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/Repositories/ItemClaimCertificateRepository.cs:74` | `mapper.Map<List<ItemClaimListDTO>>(claims)` | must be ported |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/Repositories/WarrantyCertificateRepository.cs:61` | `mapper.Map<List<WarrantyCertificateLineDTO>>(claims)` | must be ported |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/Repositories/WarrantyRatesRepository.cs:32` | `mapper.Map<WarrantyRatesDTO>(rates)` | must be ported |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/Services/WarrantyClaimService.cs:23,26,29` | **none — the field is assigned and never used** | **delete the field, the parameter and the assignment.** Verified: no `.Map` call anywhere in the file. |

For the three real ones the framework's answer is a
`[ShiftEntityMapper] partial class : IShiftObjectMapper<TEntity, TDto>` — its
`MapBack(dto, existing, ctx)` is the exact match for `IMapper.Map(dto, entity)` — or routing the call
through the repository's generated mapper. These are **hard compile breaks**, not warnings.

### 6b. Cosmos replication delegates — compile break plus a silent-corruption class

`Replicate<T>`, `UpdateReference<T>` and `UpdatePropertyReference<T>` changed their `mapping`
parameter from `Func<...>? mapping = null` to a **required** `Func<...> mapping`. Six ADP call sites
pass no delegate and rely on the removed AutoMapper fallback:

| File | Call sites |
|---|---|
| `ADP.ClaimableItems/ADP.ClaimableItems.Data/Extensions/ClaimableItemsReplicationExtensions.cs` | lines 30, 41, 45, 55, 83 — `Replicate<ServiceItemModel>`, `Replicate<ServiceCampaignModel>`, `UpdateReference<ServiceItemModel>`, `Replicate<CampaignVinEntryModel>`, `Replicate<ItemClaimModel>` |
| `ADP.WarrantyClaims/ADP.WarrantyClaims.Data/Extensions/WarrantyClaimsReplicationExtensions.cs` | line 31 — `Replicate<WarrantyClaimModel>` |

Each needs a hand-written entity → Cosmos-document projection. **There is no generator for these** —
the target is a plain Cosmos model, not a ShiftEntity DTO triple. The old profiles' entity → model
`CreateMap`s are the specification to transcribe.

Treat this as its own verification workstream: failures on this path are **swallowed by a catch** and
surface as permanently-dirty rows under a clean watermark, not as exceptions. The endpoint harness
cannot see them at all.

The reference implementations are in **`ADP.Menus/ADP.Menus.Sync/`** —
`Extensions/MenuReplicationExtensions.cs`, `Replication/MenuCatchUpReplicationExtensions.cs`,
`Replication/MenuReplicationReload.cs` — **not** in `ADP.Menus.Data`. (SPIKE-9 confirms the exact
signature.)

### 6c. The one API-extensions edit

Delete `o.AddAutoMapper(typeof(...).Assembly);` and rewrite the surrounding comment to say why
nothing replaces it:

```
ADP.ClaimableItems/ADP.ClaimableItems.API/Extensions/ClaimableItemsApiExtensions.cs:51
ADP.Surveys/ADP.Surveys.API/Extensions/SurveyApiExtensions.cs:48
ADP.WarrantyClaims/ADP.WarrantyClaims.API/Extensions/WarrantyClaimsApiExtensions.cs:47
ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs:38   (x.AddAutoMapper)
ADP.Surveys/samples/ADP.Surveys.Sample.API/Program.cs:46   (x.AddShiftIdentityAutoMapper)
```

`o.AddDataAssembly(...)` **stays**. The mappers register themselves from module initializers, and
`RegisterShiftRepositories` validates at startup that every triple in the assembly resolves one.

### 6d. Prose that must be swept

Descriptions and READMEs still advertising AutoMapper profiles:

```
ADP.ClaimableItems/ADP.ClaimableItems.Data/ADP.ClaimableItems.Data.csproj:16   <Description>
ADP.Surveys/ADP.Surveys.Data/ADP.Surveys.Data.csproj:16                        <Description>
ADP.WarrantyClaims/ADP.WarrantyClaims.Data/ADP.WarrantyClaims.Data.csproj:16   <Description>
ADP.Surveys/README.md:8
ADP.Menus/README.md:8                          (left over from the Menus migration — see §7)
ADP.Docs/Docs/docs/menus/integration.md:10, 84, 86
```

`ADP.Docs/Docs/docs/integrations/sync-agent/getting-started.md` also documents AutoMapper, but that
is `ADP.SyncAgent`'s own independent AutoMapper use. **Leave it.**

---

## 7. Do not copy these from the Menus migration

Things `14caf7c9` left unfinished. They are precedent for nothing.

1. **Stale prose.** `ADP.Menus/ADP.Menus.Data/Repositories/MenuVariantRepository.cs:415` still says
   "The mapping profile syncs these collections…" while two other lines of the *same* doc comment
   were updated to say "the mapper" — the comment contradicts itself. `ADP.Menus/README.md:8` still
   lists the Data package as "AutoMapper profiles" though its csproj `<Description>` right beside it
   was fixed. `ADP.Menus/COSMOS_REPLICATION_PLAN.md:833` reproduces a caveat that the edited code
   comment now contradicts.
2. **An unreflowed edit** in `ADP.Menus/ADP.Menus.Data/DataServices/MenuLineMargins.cs` — deleting
   one word left a paragraph broken mid-line and a list that no longer reads as a list.
3. **BOM churn.** Almost every touched file gained a UTF-8 BOM, inflating the diff of 8 csprojs and
   3 `.cs` files that otherwise changed one line each. **Avoid this** — it makes the real edits
   unreviewable. Check with `git diff --stat` before committing; a one-line change should show as
   one line.
4. **File-scoped suppressions.** Two Menus files carry `#pragma warning disable` at *file* scope
   covering all of `SHENGEN004`/`007`/`008`. A new unmapped member added to either file later is
   silently swallowed. Acceptable there only because the justification comments enumerate every
   current member by name. **Re-take this decision per group rather than copying it.**

### Suppression style, when a suppression is genuinely warranted

`SHENGEN008` pairs members **by name**, so a DTO collection written back to a *differently named*
entity collection from an `AfterEntity` hook is a known false positive. Suppress only after proving
the case against the old profile, and always with a justification block naming every affected
member:

```csharp
// SHENGEN008 fires on <Member> and is a false positive here. The check pairs a view member with an
// entity member of the SAME name; this DTO member is written back to a DIFFERENTLY named entity
// collection (<EntityCollection>) from the AfterEntity hook below, which the generator cannot see
// into. Verified against the mapping profile this replaced.
#pragma warning disable SHENGEN008
public class SomeRepository : ShiftRepository<...>
{ ... }
#pragma warning restore SHENGEN008
```

---

## 8. Coding conventions for the rewrite

Established by the Menus diff. Follow them so the groups read alike.

- **Section banners** inside a builder chain, in this order:
  `// ── VIEW ────…`, `// ── LIST ────…`, `// ── ENTITY ────…`.
- **Record what the generator already gets right**, not only what you overrode. Every "needs
  nothing" comment in Menus exists to stop the next reader re-adding a redundant `ForView`. This is
  the single most valuable convention here — without it, the next person cannot tell "convention
  handles it" from "nobody checked".
- **State the reason, not the mechanism.** `IgnoreEntity(e => e.BrandID)` gets a comment saying the
  column is repository-derived and a client could otherwise pin a record to a value its parent does
  not permit — not a comment saying "ignore BrandID".
- Every `#pragma warning disable SHENGEN…` carries a block comment above the class naming each
  member and why it stays unmapped.
- Helper methods absorbing an old `AfterMap` get a doc comment saying exactly which profile map and
  which `AfterMap` they reproduce.
- **No BOMs.** No reflow-breaking edits. Keep one-line changes to one line.

---

## 9. Diagnostics reference

| ID | Meaning |
|---|---|
| `SHENGEN001` | `[ShiftEntityMapper]` class not declared `partial` |
| `SHENGEN002` | implements neither `IShiftEntityMapper<E,L,V>` nor `IShiftObjectMapper<E,D>` |
| `SHENGEN003` | deep mapping cycle; member skipped |
| `SHENGEN004` | **unmapped view members** — no convention or deep composition applies |
| `SHENGEN005` | conditional mapper configuration — register unconditionally, put the condition inside the value delegate |
| `SHENGEN006` | entity declares `IConfiguresShiftRepository<>` **and** the repository passes an options builder — the builder wins |
| `SHENGEN007` | **unmapped list members** — the column comes back empty |
| `SHENGEN008` | view members read but never written back — display fine, silently fail to save. **Known false positive** for differently-named collections |
| `SHENGEN009` | configuration cannot be baked (non-literal selector / non-constant depth) — the call does nothing |
| `SHENGEN010` | **deep write replaces tracked child rows** — will fail on the FK or orphan/duplicate rows |
| `SHENGEN011` | ambiguous member match (differ only by case); member is SKIPPED |

**Baseline:** 10 `SHENGEN004` today, all outside Menus. Zero `SHENGEN007` / `008` / `010` anywhere.
Any `SHENGEN010`, `007` or `008` appearing during this upgrade is new and must be resolved, not
suppressed on sight.

---

## 10. Behaviour changes that compile fine

These need no code edit but change runtime behaviour. Watch for them in harness diffs.

| Change | Effect |
|---|---|
| `CopyEntity` no longer falls back to `ShallowCopyTo` | **throws** `InvalidOperationException` when no mapper is configured. Breaks any revision/duplicate flow that relied on the implicit shallow copy. |
| `OdataList` now applies `.AsNoTracking()` before projection | safe for pure projections; breaks a custom `MapToList` that materializes entities expecting them tracked |
| `IsDeleted` captured before mapping and restored after, **on `Update` only** | a PUT body can no longer set or clear the soft-delete flag. **Removes undelete-via-PUT** if any flow used it. |
| Member matching is **case-insensitive by default** (`CaseSensitiveMatching = false`) | deliberate, matches old AutoMapper behaviour. A de-risking change, not a break. Opt in per mapper with `map.CaseSensitive()`. |
| Mapper resolution order | `UseGeneratedMapper` → DI `IShiftEntityMapper<E,L,V>` → registry → **throw**. The AutoMapper rung is gone. |
| `ShiftEntityMapperValidation` runs unconditionally from `RegisterShiftRepositories` | every triple must resolve a mapper **or the app does not start**, with the complete list in one boot-time error. Loud and complete rather than a first-request 500. |
| Generated mappers carry an ABI to the framework | a mapper is frozen at its own build day. **Every `.Data` package emitting mappers must be rebuilt and republished whenever ShiftEntity moves.** Reinforces the single-release shape. |
| `TagProjection` / `TaggableProjectionExtensions` moved namespace | a compat shim exists for extension-method call sites; a direct type reference needs a `using` change. ADP appears not to use tagging — verify with a grep. |
