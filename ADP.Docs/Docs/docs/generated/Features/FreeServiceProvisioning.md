---
hide:
    - toc
---

```gherkin
Feature: Free Service Provisioning
  Free service is evaluated in two views that are meant to be read together.

  The dealer's view (the default) starts free service on the end-customer sale only. A vehicle the
  dealer still holds in stock, a vehicle only a supply-chain entry has synced for, and a vehicle a
  broker holds without an invoice all have no free-service start, so they project no items. When a
  claim exists the de facto claim date stands in, and an operator's date shift moves the start. This
  is the liability as it actually activates.

  The provisioning view (FreeServiceProvisioning) is the distributor's: it books the free-service
  provision when it invoices a vehicle. So the free-service start is the distributor's own invoice
  date, whatever happens to the vehicle afterwards. A dealer's sale, a broker's invoice, a service
  activation, a claim or a date shift does not move it. The items project from that day: pending
  while their validity still runs, expired once it has run out, processed where a claim exists. A
  vehicle the distributor has not invoiced out projects nothing.

  A consumer holding both views has the provision booked at the invoice and the liability as it
  actually activated, and can true one up against the other. The warranty does not move in either
  view: it still starts on the end-customer sale, and its start state reports what it waits for.

Background:
  Given warranty start date defaults to invoice date
  And the distributor company id is 5

# --- Vehicles that have not reached a customer: only the provisioning view dates them ---

Scenario: A vehicle in dealer stock has no free-service start in the dealer's view
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 |             | 10        |               |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then the free service start date is empty
  And the warranty start date is empty
  And the warranty start state is "AwaitingActivation"

Scenario: A vehicle in dealer stock is provisioned from the distributor's invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 |             | 10        |               |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  # Only the free-service projection is dated. The warranty still waits for the dealer's sale.
  And the warranty start date is empty
  And the warranty start state is "AwaitingActivation"

Scenario: A vehicle in sub-dealer stock is provisioned from the distributor's invoice
  # The dealer (2) invoiced the vehicle to its sub-dealer (12), whose own entry is still in stock.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 2         | 70005078      |
    | ZW8UWF8J4TJ368365 |             | 12        |               |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  And the warranty start date is empty

Scenario: A vehicle only the distributor has invoiced is provisioned from that invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  And the warranty start date is empty
  And the warranty start state is "AwaitingEndCustomerSale"

Scenario: A vehicle still in the distributor's own stock is not provisioned
  # Not invoiced out yet, so nothing to provision from.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 |             | 5         |               |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is empty
  And the warranty start date is empty

# --- Vehicles that have reached a customer: the two views date them differently, by design ---

Scenario: A vehicle sold to the end customer is provisioned from the distributor's invoice, not the dealer's sale
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 72232475      |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  # The warranty is not part of the provisioning view: it still starts on the dealer's sale.
  And the warranty start date is "2026-05-25"

Scenario: The same sold vehicle starts free service on the dealer's sale in the dealer's view
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 72232475      |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then the free service start date is "2026-05-25"
  And the warranty start date is "2026-05-25"

Scenario: A service activation does not move the provision off the distributor's invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber | CountryID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      | 1         |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 72232475      | 1         |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID | CountryID |
    | 2026-06-02             | 10        | 1         |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  And the warranty start date is "2026-06-02"

Scenario: A vehicle a dealer sold to a broker that has not invoiced is provisioned from the distributor's invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock |
    | 100      | ABC Motors | true      |
  And LookupOptions has broker stock lookup enabled
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  And the warranty start date is empty
  And the warranty start state is "AwaitingBrokerInvoice"

Scenario: The same broker-held vehicle has no free-service start in the dealer's view
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock |
    | 100      | ABC Motors | true      |
  And LookupOptions has broker stock lookup enabled
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then the free service start date is empty
  And the warranty start state is "AwaitingBrokerInvoice"

Scenario: Ignoring broker stock is the dealer's lookup's own concession and does not affect the provisioning view
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock |
    | 100      | ABC Motors | true      |
  And LookupOptions has broker stock lookup enabled
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | IgnoreBrokerStock | FreeServiceProvisioning |
    | true              | true                    |
  Then the free service start date is "2026-05-20"

Scenario: Ignoring broker stock alone anchors the dealer's lookup on the dealer's sale to the broker
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock |
    | 100      | ABC Motors | true      |
  And LookupOptions has broker stock lookup enabled
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | IgnoreBrokerStock | FreeServiceProvisioning |
    | true              | false                   |
  Then the free service start date is "2026-05-25"
  And the warranty start date is empty

Scenario: A broker's invoice anchors the dealer's view but not the provisioning view
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And broker stock for brand 1:
    | BrokerID | BrokerName | IsAtStock | InvoiceDate | InvoiceNumber |
    | 100      | ABC Motors | false     | 2026-06-01  | 5001          |
  And LookupOptions has broker stock lookup enabled
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  And the warranty start date is "2026-06-01"

# --- Neither the de facto claim date nor a date shift moves the provision ---

Scenario: A claim does not move the provision off the distributor's invoice
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 |             | 10        |               |
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2026-06-10 |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"
  # The de facto date is still exposed for whoever wants to see it.
  And the de facto service start date is "2026-06-10"

Scenario: The dealer's view dates the same claimed-against vehicle from the claim
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 |             | 10        |               |
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2026-06-10 |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then the free service start date is "2026-06-10"

Scenario: A vehicle the distributor never invoiced is not provisioned even when it has been claimed against
  # The dealer's view dates it from the claim; the provisioning view has no invoice to provision from.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 |             | 5         |               |
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2026-06-10 |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is empty
  And the de facto service start date is "2026-06-10"

Scenario: An operator's free service date shift moves the dealer's view but not the provision
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 72232475      |
  And free service item date shifts:
    | VIN               | NewDate    |
    | ZW8UWF8J4TJ368365 | 2026-08-01 |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then the free service start date is "2026-05-20"

Scenario: The same shift is applied in the dealer's view
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 95912921      |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 72232475      |
  And free service item date shifts:
    | VIN               | NewDate    |
    | ZW8UWF8J4TJ368365 | 2026-08-01 |
  When looking up warranty details for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then the free service start date is "2026-08-01"

# --- The service items follow the view's date ---

Scenario: A dealer-stock vehicle projects no service items in the dealer's view
  Given the current UTC time is "2026-09-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 |             | 10        | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service | 1       | 24              | 5000           |
  When looking up service items for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then there are 0 service items

Scenario: A dealer-stock vehicle projects its service items from the distributor's invoice for provisioning
  Given the current UTC time is "2026-09-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 |             | 10        | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service | 1       | 24              | 5000           |
  When looking up service items for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then there are 1 service items
  And service item "SI-5K" has status "pending"
  And service item "SI-5K" has activation "2026-05-20"
  And service item "SI-5K" has expiration "2028-05-20"

Scenario: A vehicle that left the distributor long ago projects its items as expired for provisioning
  # The projection is honest about age: an item whose validity ran out since the distributor's invoice
  # is expired, so it carries no provision, while the vehicle still appears in the report.
  Given the current UTC time is "2026-09-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2023-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 |             | 10        | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service | 1       | 24              | 5000           |
  When looking up service items for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then there are 1 service items
  And service item "SI-5K" has status "expired"

Scenario: A sold and shifted vehicle's items are provisioned from the distributor's invoice while the dealer's view follows the shift
  # The service item evaluator applies the shift on its own for the dealer's view; the provisioning
  # view turns that off too, so the items' dates agree with the provisioning start.
  Given the current UTC time is "2026-09-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And free service item date shifts:
    | VIN               | NewDate    |
    | ZW8UWF8J4TJ368365 | 2026-08-01 |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service | 1       | 24              | 5000           |
  When looking up service items for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then service item "SI-5K" has activation "2026-05-20"
  And service item "SI-5K" has expiration "2028-05-20"

Scenario: The same shifted vehicle's items follow the shift in the dealer's view
  Given the current UTC time is "2026-09-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And free service item date shifts:
    | VIN               | NewDate    |
    | ZW8UWF8J4TJ368365 | 2026-08-01 |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service | 1       | 24              | 5000           |
  When looking up service items for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | false                   |
  Then service item "SI-5K" has activation "2026-08-01"
  And service item "SI-5K" has expiration "2028-08-01"

Scenario: A claimed item is processed in both views, so the provision only carries what is still owed
  Given the current UTC time is "2026-09-01 12:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | ZW8UWF8J4TJ368365 | 2026-05-20  | 5         | 1       |
    | ZW8UWF8J4TJ368365 | 2026-05-25  | 10        | 1       |
  And service items:
    | ServiceItemID | Name        | BrandID | ActiveForMonths | MaximumMileage |
    | SI-5K         | 5K Service  | 1       | 24              | 5000           |
    | SI-10K        | 10K Service | 1       | 24              | 10000          |
  And item claims:
    | ServiceItemID | ClaimDate  | JobNumber | InvoiceNumber |
    | SI-5K         | 2026-07-15 | JOB-001   | INV-001       |
  When looking up service items for "ZW8UWF8J4TJ368365" with request options:
    | FreeServiceProvisioning |
    | true                    |
  Then service item "SI-5K" has status "processed"
  And service item "SI-10K" has status "pending"
  And service item "SI-10K" has activation "2028-05-20"
```
