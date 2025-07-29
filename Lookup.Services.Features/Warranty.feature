Feature: Official Vehicle Warranty
Official Vehicle Warranty 
	All authorized Toyota vehicles come with a standard warranty that is activated from the date of sale to the end customer (Invoice Date) and lasts for 3 Years.
	For Lexus vehicles, the standard warranty lasts for 4 Years.

Scenario: Official Toyota Vehicle with Active Warranty
	Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
		| VIN               | Invoiced Since       |
		| 1FDKF37GXVEB34368 | 1 Year Ago		   |
	When Checking "1FDKF37GXVEB34368"
	Then The Vehicle has Active Warranty


Scenario: Official Toyota Vehicle with Expired Warranty
	Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
		| VIN               | Invoiced Since       |
		| 1FTFW1EFXEKD12345 | 5 Years Ago		   |
	When Checking "1FTFW1EFXEKD12345"
	Then The Vehicle Does not have Active Warranty