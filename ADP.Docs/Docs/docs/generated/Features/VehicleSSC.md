---
hide:
    - toc
---

# Vehicle Safety Service Campaigns (SSC)

Safety Service Campaigns (SSCs) are manufacturer-issued recalls or safety notices.  
Each SSC is checked for repair status by looking at three sources, in order; the first that holds  
decides both the verdict and the repair date, and is reported as the repair source:

1. Direct RepairDate on the SSC record
2. Matching warranty claim (by campaign code in distributor comment, or by labor code)
3. Matching labor line (by labor code with invoice status X or C)

A labor code matches directly or through a configured group of interchangeable codes: dealer  
systems sometimes book a campaign under a sibling operation code, and the deployment declares  
which codes are used interchangeably so those repairs are still recognised. Labor codes are  
compared trimmed and case-insensitively.  
When tracing is requested, every SSC carries a trace of the evidence behind its verdict.

??? note "Rule: Repair detected via direct RepairDate"

    ```gherkin
    Scenario: SSC repaired via direct RepairDate
    	Given SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | RepairDate |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | 2024-03-15 |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as repaired
    	And SSC "SSC-001" has repair date "2024-03-15"
    ```

??? note "Rule: Repair detected via matching warranty claim"

    ```gherkin
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
    ```

??? note "Rule: Repair detected via matching labor line"

    ```gherkin
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
    ```

??? note "Rule: No repair evidence"

    ```gherkin
    Scenario: SSC not repaired when no evidence found
    	Given SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as not repaired
    
    Scenario: No SSC records returns null
    	When Checking "1G1ZC5E17BF283048"
    	Then there are no SSC records
    ```

??? note "Rule: Result composition"

    ```gherkin
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
    ```

??? note "Rule: Backward compatibility with legacy numbered fields"

    ```gherkin
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
    ```

??? note "Rule: Interchangeable labor codes"

    ```gherkin
    Scenario: SSC repaired via warranty claim carrying an interchangeable labor code
    	Given interchangeable SSC labor codes:
    		| Codes                    |
    		| LAB001, LAB001A, LAB0010 |
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And warranty claims:
    		| ClaimStatus | RepairCompletionDate | LaborCode |
    		| Accepted    | 2024-05-10           | LAB001A   |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as repaired
    	And SSC "SSC-001" has repair date "2024-05-10"
    	And SSC "SSC-001" repair source is "WarrantyClaim"
    
    Scenario: SSC repaired via labor line carrying an interchangeable labor code
    	Given interchangeable SSC labor codes:
    		| Codes           |
    		| LAB001, LAB001A |
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus |
    		| LAB001A   | 2024-06-01  | X             |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as repaired
    	And SSC "SSC-001" has repair date "2024-06-01"
    	And SSC "SSC-001" repair source is "ServiceHistory"
    
    Scenario: Interchangeability is symmetric so the campaign may list either code
    	Given interchangeable SSC labor codes:
    		| Codes           |
    		| LAB001, LAB001A |
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001A    |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus |
    		| LAB001    | 2024-06-01  | C             |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as repaired
    
    Scenario: Interchangeability is transitive across groups that share a code
    	Given interchangeable SSC labor codes:
    		| Codes            |
    		| LAB001, LAB001A  |
    		| LAB001A, LAB001B |
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus |
    		| LAB001B   | 2024-06-01  | X             |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as repaired
    
    Scenario: A code outside the interchangeable group is not repair evidence
    	Given interchangeable SSC labor codes:
    		| Codes           |
    		| LAB001, LAB001A |
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus |
    		| LAB001B   | 2024-06-01  | X             |
    	And warranty claims:
    		| ClaimStatus | RepairCompletionDate | LaborCode |
    		| Accepted    | 2024-05-10           | LAB001B   |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as not repaired
    
    Scenario: Interchangeable codes do not change the campaign's own labor code list
    	Given interchangeable SSC labor codes:
    		| Codes           |
    		| LAB001, LAB001A |
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" has 1 labor codes
    
    Scenario: Labor code matching ignores case
    	Given SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus |
    		| lab001    | 2024-06-01  | X             |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as repaired
    ```

??? note "Rule: Repair source and precedence"

    ```gherkin
    Scenario: The SSC record's own repair date outranks a matching warranty claim
    	Given SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 | RepairDate |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     | 2024-03-15 |
    	And warranty claims:
    		| ClaimStatus | RepairCompletionDate | LaborCode |
    		| Accepted    | 2024-04-01           | LAB001    |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as repaired
    	And SSC "SSC-001" has repair date "2024-03-15"
    	And SSC "SSC-001" repair source is "SSCRecord"
    
    Scenario: A matching warranty claim outranks a matching labor line
    	Given SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And warranty claims:
    		| ClaimStatus | RepairCompletionDate | LaborCode |
    		| Accepted    | 2024-04-01           | LAB001    |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus |
    		| LAB001    | 2024-06-01  | X             |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" has repair date "2024-04-01"
    	And SSC "SSC-001" repair source is "WarrantyClaim"
    
    Scenario: An open SSC reports no repair source
    	Given SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as not repaired
    	And SSC "SSC-001" repair source is "None"
    ```

??? note "Rule: Evaluation trace"

    ```gherkin
    Scenario: The trace is omitted unless requested
    	Given SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" has no trace
    
    Scenario: The trace explains a repair found through an interchangeable code on a warranty claim
    	Given SSC evaluation tracing is requested
    	And interchangeable SSC labor codes:
    		| Codes           |
    		| LAB001, LAB001A |
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And warranty claims:
    		| ClaimStatus         | RepairCompletionDate | LaborCode |
    		| Accepted            | 2024-05-10           | LAB001A   |
    		| RejectedPermanently | 2024-04-01           | LAB001    |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" trace lists interchangeable code "LAB001A" standing for "LAB001"
    	And SSC "SSC-001" trace warranty claim completed on "2024-05-10" is selected with labor code "LAB001A" standing for "LAB001"
    	And SSC "SSC-001" trace warranty claim completed on "2024-04-01" matches but does not qualify by status
    	And SSC "SSC-001" trace repair source is "WarrantyClaim"
    
    Scenario: The trace explains a repair found in service history
    	Given SSC evaluation tracing is requested
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus | InvoiceNumber |
    		| LAB001    | 2024-06-01  | X             | INV-1         |
    		| LAB999    | 2024-07-01  | X             | INV-2         |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" trace examined 2 service history labor lines and lists 1 matching
    	And SSC "SSC-001" trace labor line invoiced on "2024-06-01" is selected
    	And SSC "SSC-001" trace repair source is "ServiceHistory"
    
    Scenario: The trace explains why an SSC is still open
    	Given SSC evaluation tracing is requested
    	And SSC affected vehicles:
    		| VIN               | CampaignCode | Description   | LaborCode1 |
    		| 1G1ZC5E17BF283048 | SSC-001      | Airbag recall | LAB001     |
    	And warranty claims:
    		| ClaimStatus         | RepairCompletionDate | LaborCode |
    		| RejectedPermanently | 2024-04-01           | LAB001    |
    		| Accepted            | 2024-02-01           | LAB999    |
    	And labor lines:
    		| LaborCode | InvoiceDate | InvoiceStatus |
    		| LAB001    | 2024-06-01  | O             |
    		| LAB999    | 2024-07-01  | X             |
    	When Checking "1G1ZC5E17BF283048"
    	Then SSC "SSC-001" is marked as not repaired
    	And SSC "SSC-001" trace has 2 warranty claims and none selected
    	And SSC "SSC-001" trace warranty claim completed on "2024-04-01" matches but does not qualify by status
    	And SSC "SSC-001" trace warranty claim completed on "2024-02-01" qualifies by status but does not match
    	And SSC "SSC-001" trace examined 2 service history labor lines and lists 1 matching
    	And SSC "SSC-001" trace labor line invoiced on "2024-06-01" does not qualify by status
    	And SSC "SSC-001" trace repair source is "None"
    ```

