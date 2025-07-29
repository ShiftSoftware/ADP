Feature: Sale Information
Sale & Stock Information for a given vehicle

Scenario: Dealer Stock - Vehicle is Invoiced By Distributor to Dealer and sits at their stock
	Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
		| VIN               | Invoiced Since | Company ID     |
		| 1FDKF37GXVEB34368 | 1 Year Ago     | Distributor-ID |
		| 1FDKF37GXVEB34368 |                | Dealer-1-ID    |
	When Checking "1FDKF37GXVEB34368"
	Then The Vehicle Invoice Date is ""

Scenario: Dealer Stock - Vehicle is Invoiced By Distributor to Dealer and Sold to End-Customer
	Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
		| VIN               | Invoiced Since | Company ID     |
		| 1FDKF37GXVEB34369 | 2 Year Ago     | Distributor-ID |
		| 1FDKF37GXVEB34369 | 1 Year Ago     | Dealer-1-ID    |
	When Checking "1FDKF37GXVEB34369"
	Then The Vehicle Invoice Date is "4 Year Ago"
