Feature: Service Item Claim Warnings
  Warnings shown on the claim form before an item is claimed. Every item
  carries the statically configured standard warnings. Two dynamic warnings
  are prepended per claimable item when the matching resolver is configured
  in LookupOptions: a skipped-items warning when claiming a free sequential
  (mileage-keyed) item would cancel lower-mileage pending items — the
  pre-claim mirror of dynamic cancellation — and an un-invoiced broker
  warning when the vehicle is held by a broker that has not invoiced it yet.
  Only free items with a maximum mileage can cancel or be cancelled: paid
  items and free items without a mileage never trigger the skipped-items
  warning and never appear in it. Without resolvers no dynamic warning is
  produced (opt-in).

Scenario: Standard warnings are attached to every service item
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
    | SI-20K        | 20K Service | 1       | 24              | 20000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-10K        | 2026-02-01 | JOB-001   | INV-001       |
  And standard item claim warnings:
    | Key     | BodyContent             | ConfirmationText |
    | std-doc | Upload signed documents | I confirm        |
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-10K" has status "processed"
  And service item "SI-20K" has status "pending"
  And service item "SI-10K" has a warning with key "std-doc"
  And service item "SI-20K" has a warning with key "std-doc"

Scenario: Claiming a higher-mileage item warns about the lower pending items it will cancel
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
    | SI-20K        | 20K Service | 1       | 24              | 20000          |
    | SI-30K        | 30K Service | 1       | 24              | 30000          |
  And a skipped items claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the warning with key "skippedItems" on service item "SI-30K" has body "SI-10K, SI-20K"
  And the warning with key "skippedItems" on service item "SI-20K" has body "SI-10K"
  And service item "SI-10K" has no warnings

Scenario: No skipped-items warning when the lower items are already processed or cancelled
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
    | SI-20K        | 20K Service | 1       | 24              | 20000          |
    | SI-30K        | 30K Service | 1       | 24              | 30000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-20K        | 2026-05-01 | JOB-001   | INV-001       |
  And a skipped items claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-10K" has status "cancelled"
  And service item "SI-20K" has status "processed"
  And service item "SI-30K" has status "pending"
  And service item "SI-30K" has no warnings

Scenario: Paid items and free items without a mileage never trigger the skipped-items warning
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name           | BrandID | ActiveForMonths | MaximumMileage |
    | SI-50K        | 50K Service    | 1       | 24              | 50000          |
    | SI-NOMILE     | Bonus Cleaning | 1       | 24              |                |
  And paid service invoices:
    | InvoiceDate | InvoiceNumber | ServiceItemID | ServiceItemName  | MaximumMileage |
    | 2026-03-15  | 1001          | PSI-001       | Extended Service | 1000           |
  And a skipped items claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-NOMILE" has status "pending"
  And service item "PSI-001" has status "pending"
  And service item "SI-50K" has no warnings
  And service item "PSI-001" has no warnings
  And service item "SI-NOMILE" has no warnings

Scenario: Items at the same mileage do not warn about each other
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name          | BrandID | ActiveForMonths | MaximumMileage |
    | SI-A          | 10K Service A | 1       | 24              | 10000          |
    | SI-B          | 10K Service B | 1       | 24              | 10000          |
  And a skipped items claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-A" has no warnings
  And service item "SI-B" has no warnings

Scenario: Vehicle held by a broker without an invoice warns on claimable items only
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
    | SI-20K        | 20K Service | 1       | 24              | 20000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-10K        | 2026-02-01 | JOB-001   | INV-001       |
  And the sale has a broker without invoice
  And an un-invoiced broker claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the warning with key "unInvoicedBroker" on service item "SI-20K" has body "Test Broker"
  And service item "SI-10K" has no warnings

Scenario: Vehicle invoiced by the broker produces no broker warning
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
  And the sale has a broker with invoice date "2026-02-01"
  And an un-invoiced broker claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then service item "SI-10K" has no warnings

Scenario: Dynamic warnings are not produced when no resolver is configured
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
    | SI-20K        | 20K Service | 1       | 24              | 20000          |
  And standard item claim warnings:
    | Key     | BodyContent             | ConfirmationText |
    | std-doc | Upload signed documents | I confirm        |
  And the sale has a broker without invoice
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the warnings on service item "SI-20K" in order are:
    | Key     |
    | std-doc |
  And the warnings on service item "SI-10K" in order are:
    | Key     |
    | std-doc |

Scenario: Dynamic warnings precede the standard warnings and do not leak onto other items
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
    | SI-20K        | 20K Service | 1       | 24              | 20000          |
  And standard item claim warnings:
    | Key     | BodyContent             | ConfirmationText |
    | std-doc | Upload signed documents | I confirm        |
  And a skipped items claim warning resolver is configured
  And the sale has a broker without invoice
  And an un-invoiced broker claim warning resolver is configured
  And the free service start date is "2026-01-15"
  When evaluating service items for "1FDKF37GXVEB34368" with language "en"
  Then the warnings on service item "SI-20K" in order are:
    | Key              |
    | skippedItems     |
    | unInvoicedBroker |
    | std-doc          |
  And the warning with key "skippedItems" on service item "SI-20K" has body "SI-10K"
  And the warnings on service item "SI-10K" in order are:
    | Key              |
    | unInvoicedBroker |
    | std-doc          |
