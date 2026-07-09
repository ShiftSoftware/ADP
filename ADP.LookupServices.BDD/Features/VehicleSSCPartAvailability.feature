Feature: SSC Part Availability
	Parts of open (not-yet-repaired) SSC (safety recall) records are flagged in-stock for Hub requesters who
	have a stock scope (their region / branch). Each part ends in one of three states: available (a positive
	AvailableQuantity in an in-scope stock location), not available (a scope was checked but no in-scope stock
	has a positive quantity), or not checked (the requester has no stock scope, or the recall is already
	repaired) — the last is left null so the UI can show it neutrally rather than as "unavailable". Part numbers
	are matched after mapping them to the deployment's stored key (by default: dashes removed, upper-cased).

Rule: Availability is scoped to the requester's stock locations

Scenario: Part with stock in the requester's scope is available
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118WB001  |
	And stock for part "90118WB001":
		| Location | AvailableQuantity |
		| region-a | 4                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a"
	Then SSC "SSC-001" part "90118WB001" is available

Scenario: Part with stock only outside the requester's scope is not available
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118WB001  |
	And stock for part "90118WB001":
		| Location | AvailableQuantity |
		| region-b | 9                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a"
	Then SSC "SSC-001" part "90118WB001" is not available

Scenario: Part with zero quantity in scope is not available
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118WB001  |
	And stock for part "90118WB001":
		| Location | AvailableQuantity |
		| region-a | 0                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a"
	Then SSC "SSC-001" part "90118WB001" is not available

Scenario: Availability aggregates across multiple in-scope locations
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118WB001  |
	And stock for part "90118WB001":
		| Location | AvailableQuantity |
		| region-a | 0                 |
		| region-b | 3                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a,region-b"
	Then SSC "SSC-001" part "90118WB001" is available

Rule: The feature has a global on/off switch (EnableSSCPartAvailability)

Scenario: No availability is checked when the feature is globally disabled
	Given SSC part availability is globally disabled
	And the SSC stock scope resolver must not be called
	And SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118WB001  |
	When SSC part availability enrichment runs
	Then SSC "SSC-001" part "90118WB001" availability is not checked

Rule: A requester with no stock scope never sees availability

Scenario: Availability is not checked when the requester has no stock scope
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118WB001  |
	And stock for part "90118WB001":
		| Location | AvailableQuantity |
		| region-a | 5                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope ""
	Then SSC "SSC-001" part "90118WB001" availability is not checked

Rule: Recall part numbers are mapped to the deployment's stored key

Scenario: SSC part matches T-prefixed dash-stripped stock (deployment-specific storage key)
	Given stock part numbers are stored T-prefixed and dash-stripped
	And SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 04007-07212 |
	And stock for part "T0400707212":
		| Location | AvailableQuantity |
		| region-a | 3                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a"
	Then SSC "SSC-001" part "04007-07212" is available

Scenario: SSC part with no T-prefixed stock in scope is not available (deployment-specific storage key)
	Given stock part numbers are stored T-prefixed and dash-stripped
	And SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 04007-07212 |
	And stock for part "T0400707212":
		| Location | AvailableQuantity |
		| region-b | 5                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a"
	Then SSC "SSC-001" part "04007-07212" is not available

Rule: The default transform ignores dashes and case

Scenario: Dashed recall part number matches dashless stock part number
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118-WB001 |
	And stock for part "90118WB001":
		| Location | AvailableQuantity |
		| region-a | 2                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a"
	Then SSC "SSC-001" part "90118-WB001" is available

Rule: Availability is only evaluated for recalls that are not yet repaired

Scenario: A repaired recall does not report part availability even when stock exists
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | PartNumber1 | RepairDate |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 90118WB001  | 2020-05-01 |
	And stock for part "90118WB001":
		| Location | AvailableQuantity |
		| region-a | 7                 |
	When Checking "1G1ZC5E17BF283048"
	And SSC part availability is applied for stock scope "region-a"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-001" part "90118WB001" availability is not checked
