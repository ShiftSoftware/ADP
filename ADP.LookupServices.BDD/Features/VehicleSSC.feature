Feature: Vehicle Safety Service Campaigns (SSC)
	Safety Service Campaigns (SSCs) are manufacturer-issued recalls or safety notices.
	Each SSC is checked for repair status by looking at three sources:
	1. Direct RepairDate on the SSC record
	2. Matching warranty claim (by campaign code in distributor comment, or by labor code)
	3. Matching labor line (by labor code with invoice status X or C)

Rule: Repair detected via direct RepairDate

Scenario: SSC repaired via direct RepairDate
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | RepairDate |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 2024-03-15 |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-001" has repair date "2024-03-15"

Rule: Repair detected via matching warranty claim

Scenario: SSC repaired via warranty claim matching campaign code in comment
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	And warranty claims:
		| ClaimStatus | RepairCompletionDate | DistributorComment      |
		| Accepted    | 2024-04-01           | Repair for SSC-001 done |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-001" has repair date "2024-04-01"

Scenario: SSC repaired via warranty claim matching labor code
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	And warranty claims:
		| ClaimStatus | RepairCompletionDate | LaborCode |
		| Certified   | 2024-05-10           | LAB001    |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired

Scenario: Warranty claim with non-matching status is ignored
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	And warranty claims:
		| ClaimStatus          | RepairCompletionDate | DistributorComment |
		| RejectedPermanently  | 2024-04-01           | Repair for SSC-001 |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as not repaired

Rule: Repair detected via matching labor line

Scenario: SSC repaired via labor line
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	And labor lines:
		| LaborCode | InvoiceDate | InvoiceStatus |
		| LAB001    | 2024-06-01  | X             |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-001" has repair date "2024-06-01"

Scenario: SSC repaired via labor line with status C
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	And labor lines:
		| LaborCode | InvoiceDate | InvoiceStatus |
		| LAB001    | 2024-07-01  | C             |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-001" has repair date "2024-07-01"

Scenario: SSC labor code with surrounding whitespace still matches the labor line
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | AURCM      |
	And the SSC "SSC-001" labor code carries a trailing space
	And labor lines:
		| LaborCode | InvoiceDate | InvoiceStatus |
		| AURCM     | 2024-06-01  | X             |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-001" has repair date "2024-06-01"

Scenario: Labor line with non-matching status is ignored
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	And labor lines:
		| LaborCode | InvoiceDate | InvoiceStatus |
		| LAB001    | 2024-06-01  | O             |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as not repaired

Rule: No repair evidence

Scenario: SSC not repaired when no evidence found
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as not repaired

Scenario: No SSC records returns null
	When Checking "1G1ZC5E17BF283048"
	Then there are no SSC records

Rule: Result composition

Scenario: Multiple SSCs with mixed repair status
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description     | LaborCode1 | RepairDate |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall   | LAB001     | 2024-03-15 |
		| 1G1ZC5E17BF283048 | SSC-002      | Seatbelt recall | LAB002     |            |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-002" is marked as not repaired

Scenario: SSC parts and labor codes appear in result
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 | LaborCode2 | PartNumber1 | PartNumber2 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     | LAB002     | PRT001      | PRT002      |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" has 2 labor codes
	And SSC "SSC-001" has 2 part numbers

Scenario: SSC exposes more than three parts and labor codes
	Given SSC affected vehicles:
		| VIN               | CampaignCode | Description   | LaborCode1 | LaborCode2 | LaborCode3 | LaborCode4 | LaborCode5 | LaborCode6 | PartNumber1 | PartNumber2 | PartNumber3 | PartNumber4 | PartNumber5 | PartNumber6 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     | LAB002     | LAB003     | LAB004     | LAB005     | LAB006     | PRT001      | PRT002      | PRT003      | PRT004      | PRT005      | PRT006      |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" has 6 labor codes
	And SSC "SSC-001" has 6 part numbers

Rule: Backward compatibility with legacy numbered fields

Scenario: SSC stored in legacy numbered fields is still evaluated
	Given SSC affected vehicles in legacy numbered format:
		| VIN               | CampaignCode | Description   | LaborCode1 | LaborCode2 | PartNumber1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     | LAB002     | PRT001      |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" has 2 labor codes
	And SSC "SSC-001" has 1 part numbers

Scenario: Legacy labor code still drives repair detection
	Given SSC affected vehicles in legacy numbered format:
		| VIN               | CampaignCode | Description   | LaborCode1 |
		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
	And labor lines:
		| LaborCode | InvoiceDate | InvoiceStatus |
		| LAB001    | 2024-06-01  | X             |
	When Checking "1G1ZC5E17BF283048"
	Then SSC "SSC-001" is marked as repaired
	And SSC "SSC-001" has repair date "2024-06-01"
