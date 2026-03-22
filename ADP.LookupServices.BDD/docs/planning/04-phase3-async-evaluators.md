# Phase 3: Async Evaluators (+ IServiceProvider)

**Goal:** Add BDD coverage for async evaluators that use resolver delegates via `LookupOptions` and require a mocked `IServiceProvider`.

**Prerequisite:** Phase 2 complete

---

## Evaluators in Scope

| Evaluator | Returns | Key Logic |
|-----------|---------|-----------|
| VehicleAccessoriesEvaluator | `IEnumerable<AccessoryDTO>` | Maps accessories with image URL resolution via `AccessoryImageUrlResolver` |
| VehiclePaintThicknessEvaluator | `IEnumerable<PaintThicknessInspectionDTO>` | Maps paint thickness inspections with panel image URL resolution |
| VehicleServiceHistoryEvaluator | `IEnumerable<VehicleServiceHistoryDTO>` | Groups labor + part lines into invoices; resolves company/branch names |

---

## Mocking Infrastructure

### Support/MockSetup.cs (new)

Helper class for configuring resolver delegates on `LookupOptions`:

```csharp
public static class MockSetup
{
    public static void SetupAccessoryImageResolver(
        LookupOptions options, Dictionary<string, string> imageUrlMappings)
    {
        options.AccessoryImageUrlResolver = (model) =>
        {
            var url = imageUrlMappings.TryGetValue(model.Value, out var mapped) ? mapped : model.Value;
            return new ValueTask<string?>(url);
        };
    }

    public static void SetupCompanyNameResolver(
        LookupOptions options, Dictionary<long?, string> companyNames)
    {
        options.CompanyNameResolver = (model) =>
        {
            var name = companyNames.TryGetValue(model.Value, out var n) ? n : null;
            return new ValueTask<string?>(name);
        };
    }

    // Similar for CompanyBranchNameResolver, PaintThickneesImageUrlResolver, etc.
}
```

### StepDefinitions/LookupOptionsStepDefinitions.cs (extend — created in Phase 2)

Add Given steps for configuring mock resolvers:

```gherkin
Given the accessory image resolver maps "img001.jpg" to "https://cdn.example.com/img001.jpg"
Given company 1 is named "Toyota Motors"
Given branch 10 is named "Downtown Service Center"
```

---

## New Given Steps

### Data Setup Steps (SharedStepDefinitions.cs)

```gherkin
Given accessories:
  | PartNumber | PartDescription  | Image      |
  | ACC-001    | Floor mats       | img001.jpg |
  | ACC-002    | Roof rack        | img002.jpg |
```
Maps to `VehicleAccessoryModel` → populates `Aggregate.Accessories`.

```gherkin
Given paint thickness inspections:
  | InspectionDate | Source    |
  | 2024-03-15     | Dealer   |
```
Maps to `PaintThicknessInspectionModel` → populates `Aggregate.PaintThicknessInspections`.

**Note:** Paint thickness panels are nested data. Consider a separate Given step:
```gherkin
Given paint thickness panels for inspection on "2024-03-15":
  | PanelType | PanelSide | PanelPosition | MeasuredThickness | Images         |
  | Hood      | Front     | Center        | 120               | img1.jpg       |
```

```gherkin
Given part lines:
  | PartNumber | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | SoldQuantity | PackageCode |
  | PRT-001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | 2            | PKG-A       |
```
Maps to `OrderPartLineModel` → populates `Aggregate.PartLines`.

---

## Feature Files

### VehicleAccessories.feature

```gherkin
Feature: Vehicle Accessories
  Vehicle accessories are listed with their part numbers, descriptions,
  and images. Image URLs are resolved through a configurable resolver.

Scenario: Accessories listed with resolved image URLs
  Given accessories:
    | PartNumber | PartDescription | Image      |
    | ACC-001    | Floor mats      | img001.jpg |
    | ACC-002    | Roof rack       | img002.jpg |
  And the accessory image resolver maps "img001.jpg" to "https://cdn.example.com/img001.jpg"
  And the accessory image resolver maps "img002.jpg" to "https://cdn.example.com/img002.jpg"
  When evaluating accessories with language "en"
  Then there are 2 accessories
  And accessory "ACC-001" has image "https://cdn.example.com/img001.jpg"

Scenario: Accessories without image resolver return raw image value
  Given accessories:
    | PartNumber | PartDescription | Image      |
    | ACC-001    | Floor mats      | img001.jpg |
  When evaluating accessories with language "en"
  Then accessory "ACC-001" has image "img001.jpg"

Scenario: No accessories returns empty list
  When evaluating accessories with language "en"
  Then there are 0 accessories
```

### VehiclePaintThickness.feature

```gherkin
Feature: Vehicle Paint Thickness Inspections
  Paint thickness inspection records show measured thickness values
  per panel, along with inspection images. Images are resolved via
  a configurable URL resolver.

Scenario: Inspection with panels and images
  Given paint thickness inspections:
    | InspectionDate | Source |
    | 2024-03-15     | Dealer |
  And paint thickness panels for inspection on "2024-03-15":
    | PanelType | PanelSide | PanelPosition | MeasuredThickness |
    | Hood      | Front     | Center        | 120               |
  When evaluating paint thickness with language "en"
  Then there is 1 inspection
  And the inspection on "2024-03-15" has 1 panel

Scenario: No inspections returns null
  When evaluating paint thickness with language "en"
  Then there are no paint thickness inspections
```

### VehicleServiceHistory.feature

```gherkin
Feature: Vehicle Service History
  Service history groups labor lines and part lines into invoices,
  identified by company, branch, invoice number, and order document number.

Scenario: Labor and part lines grouped into a single invoice
  Given labor lines:
    | LaborCode | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | ServiceDescription |
    | LAB001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | Oil change         |
  And part lines:
    | PartNumber | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | SoldQuantity |
    | PRT-001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | 2            |
  And company 1 is named "Toyota Motors"
  And branch 10 is named "Downtown Service"
  When evaluating service history with language "en"
  Then there is 1 service history invoice
  And invoice "INV-001" has 1 labor line and 1 part line
  And invoice "INV-001" company is "Toyota Motors"

Scenario: Lines from different invoices create separate entries
  Given labor lines:
    | LaborCode | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate |
    | LAB001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  |
    | LAB002    | 1         | 10       | INV-002       | JOB-002             | 2024-07-01  |
  When evaluating service history with language "en"
  Then there are 2 service history invoices

Scenario: Strong consistency excludes invoices with mismatched line counts
  Given labor lines:
    | LaborCode | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | NumberOfPartLines |
    | LAB001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | 2                 |
  And part lines:
    | PartNumber | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | NumberOfLaborLines |
    | PRT-001    | 1         | 10       | INV-001       | JOB-001             | 2024-06-01  | 1                  |
  When evaluating service history with language "en" and strong consistency
  Then there are 0 service history invoices

Scenario: No labor or part lines returns empty history
  When evaluating service history with language "en"
  Then there are 0 service history invoices
```

---

## Implementation Notes

- All three evaluators require `IServiceProvider` in their constructors. The `TestContext` already provides a NSubstitute mock from Phase 0.
- Resolver delegates are `Func<LookupOptionResolverModel<T>, ValueTask<string?>>`. The mock setup helpers configure these as simple lookup dictionaries.
- `VehicleServiceHistoryEvaluator.Evaluate()` takes a `ConsistencyLevels` enum parameter. Feature scenarios should test both default and `Strong` consistency levels.
- The `GetInvoices()` static method on `VehicleServiceHistoryEvaluator` is public — it could also be tested directly if needed.

---

## Verification

```bash
dotnet test ADP.LookupServices.BDD
```

All previous phase scenarios plus Phase 3 scenarios must pass.
