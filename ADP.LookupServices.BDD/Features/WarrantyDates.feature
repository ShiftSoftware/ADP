Feature: Warranty and Free Service Dates
  Warranty and free service start dates are determined by the vehicle's sale
  circumstances. The system checks service activation records, warranty activation
  dates, and invoice dates (in that priority order). Broker sales have separate
  logic. Date shifts can override calculated dates. Extended warranty entries
  are tracked independently.

  Extended warranty has two independent outputs. ExtendedWarranties lists every
  coverage — persisted entries plus any awarded by a configured definition. The
  flat HasExtendedWarranty/ExtendedWarrantyStartDate/ExtendedWarrantyEndDate
  fields are older output that describes only the latest-ending *persisted*
  entry, and only while that entry is still running. Configured coverage never
  reaches them.

# --- Normal Sale (no broker) ---

Scenario: Warranty date from service activation
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-02-01"

Scenario: Warranty date falls back to vehicle warranty activation date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | WarrantyActivationDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 2024-01-20             |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-01-20"

Scenario: Warranty date defaults to invoice date when enabled
  Given warranty start date defaults to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-01-15"

Scenario: Warranty date is null when no activation and defaulting disabled
  Given warranty start date does not default to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is empty

# --- Warranty End Date ---

Scenario: Default warranty period is 3 years
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty end date is "2027-02-01"

Scenario: Brand-specific warranty period
  Given brand 1 has a warranty period of 5 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty end date is "2029-02-01"

# --- Date Shifts ---

Scenario: Warranty date shift overrides calculated start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And warranty date shifts:
    | NewDate    |
    | 2023-06-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2023-06-01"

Scenario: Free service date shift overrides free service start date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And free service item date shifts:
    | NewDate    |
    | 2023-09-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2023-09-01"

# --- Extended Warranty ---

Scenario: Extended warranty dates from entries
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty entries:
    | ID       | CompanyID | StartDate  | EndDate    |
    | EW-ENTRY | 1         | 2027-02-01 | 2029-02-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the extended warranty start date is "2027-02-01"
  And the extended warranty end date is "2029-02-01"

Scenario: Multiple extended warranties preserve provider details
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And extended warranty entries:
    | ID       | CompanyID | StartDate  | EndDate    |
    | EW-LATE  | 202       | 2029-06-01 | 2031-06-01 |
    | EW-EARLY | 101       | 2027-02-01 | 2028-02-01 |
  And company logos resolve as:
    | CompanyID | Logo                      |
    | 101       | https://images.test/a.png |
    | 202       | https://images.test/b.png |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle has extended warranty
  # Legacy fields describe EW-LATE alone — not 2027-02-01 → 2031-06-01 across both.
  And the extended warranty start date is "2029-06-01"
  And the extended warranty end date is "2031-06-01"
  And extended warranties are:
    | ID       | ProviderCompanyID | ProviderCompanyLogo          | StartDate  | EndDate    |
    | EW-LATE  | 202               | https://images.test/b.png    | 2029-06-01 | 2031-06-01 |
    | EW-EARLY | 101               | https://images.test/a.png    | 2027-02-01 | 2028-02-01 |

Scenario: The vehicle lookup API returns resolved provider detail
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1         | 1       |
  And extended warranty entries:
    | ID     | CompanyID | StartDate  | EndDate    |
    | EW-API | 101       | 2027-02-01 | 2028-02-01 |
  And company logos resolve as:
    | CompanyID | Logo                        |
    | 101       | https://images.test/api.png |
  When looking up warranty details for "1FDKF37GXVEB34368"
  Then extended warranties are:
    | ID     | ProviderCompanyID | ProviderCompanyLogo        | StartDate  | EndDate    |
    | EW-API | 101               | https://images.test/api.png | 2027-02-01 | 2028-02-01 |

Scenario: Historical extended coverage stays listed but clears the legacy flag
  Given the current UTC time is "2035-01-01 00:00:00"
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And extended warranty entries:
    | ID     | CompanyID | StartDate  | EndDate    |
    | EW-OLD | 101       | 2027-02-01 | 2029-02-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle does not have extended warranty
  And there are 1 extended warranties
  And the extended warranty start date is "2027-02-01"
  And the extended warranty end date is "2029-02-01"

Scenario: Configured coverage joins the collection without moving the legacy fields
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty entries:
    | ID        | CompanyID | StartDate  | EndDate    |
    | EW-STORED | 101       | 2026-01-01 | 2027-06-01 |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then there are 2 extended warranties
  # CFG-REWARD runs to 2028-02-01 but the legacy end date stays on EW-STORED.
  And the extended warranty start date is "2026-01-01"
  And the extended warranty end date is "2027-06-01"

Scenario: Both coverage sources carry display labels for the rail
  # The shape a host sees once it configures a reward alongside its existing purchased
  # coverage: one persisted entry and one configured definition, different providers.
  # A definition supplies its own name; a persisted entry has none and stays null, which
  # is what tells a consumer to use its own generic wording instead of the identifier.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And company logos resolve as:
    | CompanyID | Logo                             |
    | 101       | https://images.test/provider.png |
    | 901       | https://images.test/reward.png   |
  And company names resolve as:
    | CompanyID | Name                |
    | 101       | Coverage Partner    |
    | 901       | Sample Distributor  |
  And extended warranty entries:
    | ID        | CompanyID | StartDate  | EndDate    |
    | EW-STORED | 101       | 2026-01-01 | 2027-06-01 |
  And extended warranty definitions:
    | ID         | Name                      | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | Distributor Service Reward | 901              | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then extended warranties are:
    | ID         | Name                       | ProviderCompanyID | ProviderCompanyLogo              | ProviderCompanyName | StartDate  | EndDate    |
    | EW-STORED  |                            | 101               | https://images.test/provider.png | Coverage Partner    | 2026-01-01 | 2027-06-01 |
    | CFG-REWARD | Distributor Service Reward | 901               | https://images.test/reward.png   | Sample Distributor  | 2027-02-01 | 2028-02-01 |

Scenario: A configured provider company replaces the storing dealer on persisted coverage
  # A persisted entry carries the CompanyID of whoever stored the row, which for a deployment that
  # runs extended warranty as one programme is the selling dealer, not the provider. The option
  # redirects persisted coverage to the real provider; a configured definition keeps its own.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And persisted extended warranties are provided by company 700
  And company names resolve as:
    | CompanyID | Name             |
    | 101       | Storing Dealer   |
    | 700       | Coverage Partner |
    | 901       | Sample Distributor |
  And company logos resolve as:
    | CompanyID | Logo                            |
    | 101       | https://images.test/dealer.png  |
    | 700       | https://images.test/partner.png |
    | 901       | https://images.test/reward.png  |
  And extended warranty entries:
    | ID        | CompanyID | StartDate  | EndDate    |
    | EW-STORED | 101       | 2026-01-01 | 2027-06-01 |
  And extended warranty definitions:
    | ID         | Name                       | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | Distributor Service Reward | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  # The storing dealer 101 appears nowhere: neither its name nor its logo reaches the coverage.
  Then extended warranties are:
    | ID         | Name                       | ProviderCompanyID | ProviderCompanyLogo             | ProviderCompanyName | StartDate  | EndDate    |
    | EW-STORED  |                            | 700               | https://images.test/partner.png | Coverage Partner    | 2026-01-01 | 2027-06-01 |
    | CFG-REWARD | Distributor Service Reward | 901               | https://images.test/reward.png  | Sample Distributor  | 2027-02-01 | 2028-02-01 |

Scenario: The name resolver names persisted coverages without overriding a definition's own name
  # A persisted entry has no name field at all, so the host is the only party that knows what its
  # purchased coverage is called. A configured definition already carries one and must win.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty entries:
    | ID        | CompanyID | StartDate  | EndDate    |
    | EW-STORED | 101       | 2026-01-01 | 2027-06-01 |
  And extended warranty definitions:
    | ID         | Name              | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | Definition's Name | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  And extended warranty names resolve as:
    | ID         | Name              |
    | EW-STORED  | Purchased Cover   |
    | CFG-REWARD | Resolver's Name   |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then extended warranties are:
    | ID         | Name              | ProviderCompanyID | StartDate  | EndDate    |
    | EW-STORED  | Purchased Cover   | 101               | 2026-01-01 | 2027-06-01 |
    | CFG-REWARD | Definition's Name | 901               | 2027-02-01 | 2028-02-01 |

Scenario Outline: A configured extended warranty uses the shared package suffix grammar
  Given brand 1 has a warranty period of 3 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson  |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | ["<Suffix>"] |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode  |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | <PackageCode> |
  And company logos resolve as:
    | CompanyID | Logo                                |
    | 901       | https://images.test/distributor.png |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  # No persisted entry, so the legacy flag stays false however the definition matches.
  Then the vehicle does not have extended warranty
  And extended warranties are:
    | ID         | ProviderCompanyID | ProviderCompanyLogo                    | StartDate  | EndDate    |
    | CFG-REWARD | 901               | https://images.test/distributor.png   | 2027-02-01 | 2028-02-01 |

  Examples:
    | Suffix | PackageCode |
    |  60K   | MODEL 60K   |
    |  75K   | MODEL 75K   |

Scenario: A configured warranty can fall back to the tenant distributor as provider
  Given the distributor company id is 901
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD |                   | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  And company logos resolve as:
    | CompanyID | Logo                                |
    | 901       | https://images.test/distributor.png |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then extended warranties are:
    | ID         | ProviderCompanyID | ProviderCompanyLogo                    | StartDate  | EndDate    |
    | CFG-REWARD | 901               | https://images.test/distributor.png   | 2027-02-01 | 2028-02-01 |

Scenario Outline: A configured warranty fails closed when its package condition does not match
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith  | Latest    | 1     | [" 60K"]  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode  |
    | 1         | 10       | INV-OTHER     | JOB-OTHER           | 2026-03-01  | <PackageCode> |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle does not have extended warranty
  And there are 0 extended warranties

  Examples:
    | PackageCode |
    | MODEL 55K   |
    | 160K        |

Scenario Outline: A milestone reward is kept or lost according to the scope it is given
  # A reward for reaching a service milestone has to survive the vehicle's next visit. Scoped to
  # the latest invoice the condition asks "was the last service the 60K one", which is true for
  # exactly as long as it takes the customer to come back — the reward appears at 60K and is
  # withdrawn at 65K. Scoped to all of history it asks "has the vehicle had its 60K service",
  # which is the question the programme is actually about.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection   | Count   | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith   | <Selection> | <Count> | [" 60K"]   |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
    | 1         | 10       | INV-065       | JOB-065             | 2026-06-01  | MODEL 65K   |
    | 1         | 10       | INV-080       | JOB-080             | 2026-09-01  | MODEL 80K   |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then there are <Count of coverages> extended warranties

  Examples:
    | Selection | Count | Count of coverages |
    | All       |       | 1                  |
    | Latest    | 1     | 0                  |

Scenario: A count alongside the all-history scope fails closed
  # All already takes every invoice, so a window size here means the author meant a different
  # selection. Honouring one of the two and discarding the other would award coverage on a rule
  # nobody wrote.
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Selection | Count | ValuesJson |
    | serviceHistory.laborLines.packageCode | ContainsAll | EndsWith   | All       | 1     | [" 60K"]   |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60K   |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle does not have extended warranty
  And there are 0 extended warranties

Scenario: An extended warranty definition without conditions fails closed
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the vehicle does not have extended warranty
  And there are 0 extended warranties

# --- Milestone-based extended warranty ---
#
# A configured coverage is gated by the same declarative grammar service items use, so a reward for
# reaching a service milestone can be written the same way: read the milestone out of the package
# code and compare it as a number, rather than match the text the code happens to end with.
#
# The distinction is the whole point. A network that appends a spec token writes the 60,000 km
# service as "MODEL 60KS3", and to a suffix comparison that is a vehicle which never had one — the
# customer is denied a coverage they earned, and nothing anywhere reports a problem. Every scenario
# above this line matches on the suffix; the shape that survives such codes had been available to a
# coverage all along and was never shown here, which is its own way of not existing.
#
# Codes below are invented, and read through the convention the harness declares
# (TestContext.ScenarioServiceCodePattern).

Scenario Outline: A qualified milestone code is read as the service it records
  Given brand 1 has a warranty period of 3 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch   | Qualifier   | QualifierValues   | Selection | ValuesJson   |
    | serviceHistory.laborLines.packageCode | ContainsAll | <ValueMatch> | <Qualifier> | <QualifierValues> | All       | <ValuesJson> |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60KS3 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then there are <Coverages> extended warranties

  Examples:
    | Grammar                        | ValueMatch | Qualifier | QualifierValues | ValuesJson | Coverages |
    | milestone, any qualifier       | Milestone  | Any       |                 | ["60000"]  | 1         |
    | milestone, only unqualified    | Milestone  | None      |                 | ["60000"]  | 0         |
    | milestone, that spec allowed   | Milestone  | Only      | S3              | ["60000"]  | 1         |
    | milestone, that spec excluded  | Milestone  | Except    | S3              | ["60000"]  | 0         |
    | the package suffix             | EndsWith   |           |                 | [" 60K"]   | 0         |
    | the package suffix, spec glued | EndsWith   |           |                 | [" 60KS3"] | 1         |

Scenario Outline: A milestone coverage counts the service whichever programme booked it
  # Omitting the programme filter is how a deployment says the milestone is what earns the coverage:
  # a 60,000 km service is a 60,000 km service whichever programme the branch booked it under. Naming
  # a programme narrows it back to that programme's own codes, and the same visit stops counting.
  Given brand 1 has a warranty period of 3 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | 901               | 1         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Program   | Qualifier | Selection | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | <Program> | Any       | All       | 60000  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode   |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | ALTSERV 60KS3 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then there are <Coverages> extended warranties

  Examples:
    | Rule                     | Program | Coverages |
    | any programme counts     |         | 1         |
    | only the named programme | PGM     | 0         |

Scenario: A milestone-earned coverage runs from the end of the standard warranty
  # The same anchor a suffix-matched definition uses. Nothing about reading the milestone changes
  # when the coverage starts or how long it lasts.
  Given brand 1 has a warranty period of 3 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And extended warranty definitions:
    | ID         | Name                       | ProviderCompanyID | ActiveFor | DurationType |
    | CFG-REWARD | Distributor Service Reward | 901               | 2         | Years        |
  And extended warranty definition "CFG-REWARD" has eligibility conditions:
    | Field                                 | Operator    | ValueMatch | Qualifier | Selection | Values |
    | serviceHistory.laborLines.packageCode | ContainsAll | Milestone  | Any       | All       | 60000  |
  And labor lines:
    | CompanyID | BranchID | InvoiceNumber | OrderDocumentNumber | InvoiceDate | PackageCode |
    | 1         | 10       | INV-050       | JOB-050             | 2025-11-01  | MODEL 50K   |
    | 1         | 10       | INV-060       | JOB-060             | 2026-03-01  | MODEL 60KS3 |
    | 1         | 10       | INV-065       | JOB-065             | 2026-07-01  | MODEL 65KS3 |
  And company logos resolve as:
    | CompanyID | Logo                                |
    | 901       | https://images.test/distributor.png |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  # Scoped to all of history, so the later 65K visit does not withdraw what the 60K one earned.
  Then extended warranties are:
    | ID         | Name                       | ProviderCompanyID | ProviderCompanyLogo                 | StartDate  | EndDate    |
    | CFG-REWARD | Distributor Service Reward | 901               | https://images.test/distributor.png | 2027-02-01 | 2029-02-01 |

# --- De Facto Service Start Date ---
# The earliest non-deleted ItemClaim.ClaimDate is always exposed as DeFactoServiceStartDate.
# When the regular fallback chain leaves FreeServiceStartDate=null (typically broker-without-invoice
# + IgnoreBrokerStock=false), the de facto value is used as the effective FreeServiceStartDate so
# downstream service items still project. FreeServiceItemDateShifts still override.

Scenario: De facto date exposed even when the regular chain produces a date (direct sale)
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And vehicle service activations:
    | WarrantyActivationDate | CompanyID |
    | 2024-02-01             | 1         |
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-08-10 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-02-01"
  And the de facto service start date is "2024-08-10"

Scenario: Broker without invoice and no claims leaves both dates empty
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is empty
  And the de facto service start date is empty

# --- Possession must not start the warranty ---
# While the vehicle is still held by a broker that has not invoiced it, the warranty has not
# officially started. The dealer's own invoice date must never stand in: the end customer would
# silently lose the whole dealer-to-broker-to-customer possession period off the front of their
# coverage. Once the broker invoices, that invoice — not the dealer's — is the anchor.

Scenario: A broker holding the vehicle un-invoiced leaves the warranty unstarted
  Given warranty start date defaults to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  # Not 2024-01-15: the dealer invoice only moved the car to the broker.
  Then the warranty start date is empty
  And the warranty end date is empty
  And the vehicle does not have active warranty
  # The panel can now say why, instead of showing an unexplained empty coverage.
  And the warranty start state is "AwaitingBrokerInvoice"
  And the warranty has no activating broker

Scenario: The broker invoice anchors the warranty, not the dealer invoice
  Given warranty start date defaults to invoice date
  And brand 1 has a warranty period of 3 years
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | BrandID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 1       |
  And the sale has a broker with invoice date "2024-02-10"
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  # The customer keeps the 26 days the broker held the car.
  Then the warranty start date is "2024-02-10"
  And the warranty end date is "2027-02-10"
  And the warranty start state is "Started"
  And the warranty was activated by broker "Test Broker"

Scenario: Supply-chain possession is reported as awaiting an end-customer sale
  # Only the distributor's entry has synced. Its invoice date exists but must not anchor the warranty,
  # so the panel needs to say the vehicle has not reached a customer rather than show nothing.
  Given warranty start date defaults to invoice date
  And the distributor company id is 5
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | InvoiceNumber |
    | JTMAB7BJ0T4224184 | 2026-05-25  | 5         | 30018218      |
  When evaluating warranty dates for "JTMAB7BJ0T4224184"
  Then the warranty start date is empty
  And the warranty start state is "AwaitingEndCustomerSale"
  And the warranty has no activating broker

Scenario: A dealer sale with nothing to date it is reported as awaiting activation
  # A real end-customer sale, but no activation, no sale activation date, and defaulting is off.
  # That is a missing-activation problem, not a possession one, and must not be conflated with it.
  Given warranty start date does not default to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 10        |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is empty
  And the warranty start state is "AwaitingActivation"

Scenario: A started warranty on a normal dealer sale names no broker
  Given warranty start date defaults to invoice date
  And vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID |
    | 1FDKF37GXVEB34368 | 2024-01-15  | 10        |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the warranty start date is "2024-01-15"
  And the warranty start state is "Started"
  And the warranty has no activating broker

Scenario: Broker without invoice falls back to the only claim date
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-06-15 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-06-15"
  And the de facto service start date is "2024-06-15"

Scenario: Broker without invoice picks earliest among multiple claims
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-A          | 2024-09-01 |
    | SI-B          | 2024-06-15 |
    | SI-C          | 2025-01-10 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-06-15"
  And the de facto service start date is "2024-06-15"

Scenario: Deleted claims are excluded from de facto computation
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  | IsDeleted |
    | SI-DELETED    | 2024-03-01 | true      |
    | SI-ACTIVE     | 2024-08-20 | false     |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-08-20"
  And the de facto service start date is "2024-08-20"

Scenario: Free service date shift still overrides the de facto fallback
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker without invoice
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-06-15 |
  And free service item date shifts:
    | NewDate    |
    | 2023-09-01 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2023-09-01"
  And the de facto service start date is "2024-06-15"

Scenario: Broker invoice still wins over the de facto fallback when both exist
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate |
    | 1FDKF37GXVEB34368 | 2024-01-15  |
  And the sale has a broker with invoice date "2024-02-10"
  And item claims:
    | ServiceItemID | ClaimDate  |
    | SI-OIL        | 2024-06-15 |
  When evaluating warranty dates for "1FDKF37GXVEB34368"
  Then the free service start date is "2024-02-10"
  And the de facto service start date is "2024-06-15"
