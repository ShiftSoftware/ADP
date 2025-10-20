Feature: Authorized Vehicles
Authorized & Unauthorized Cars
	A car is considered "Authorized" or "Official" if it is imported by the official distributor for a specific geographic region.
	In contrast, cars not imported by the official distributor are considered "Unauthorized", "Unofficial", or "Grey Imports".

Scenario: Authorized From Initial Stock
	Given a dealer with the following vehicles as initial stock:
		| VIN               |
		| 1FMZU72E12UB00984 |
		| 1FMCU0F73AKB12345 |
	When Checking "1FMZU72E12UB00984"
	Then The Vehicle is considered Authroized

Scenario: Authorized From Dealer Stock (Dealer DMS)
	Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
		| VIN               |
		| 1FDKF37GXVEB34368 |
		| 1FTFW1EFXEKD12345 |
	When Checking "1FDKF37GXVEB34368"
	Then The Vehicle is considered Authroized

Scenario: Authorized From SSC
	Given a dealer with the following vehicles in official SSC Vehciles (Provided by the vehicle manufacturer):
		| VIN               |
		| 1G1ZC5E17BF283048 |
	When Checking "1G1ZC5E17BF283048"
	Then The Vehicle is considered Authroized


Scenario: Unauthorized
	Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
		| VIN               |
		| 2C3CCAGG1DH549029 |
	And a dealer with the following vehicles in official SSC Vehciles (Provided by the vehicle manufacturer):
		| VIN               |
		| 1HGCD5630TA078763 |
	And a dealer with the following vehicles as initial stock:
		| VIN               |
		| 1FDKF37G8VEB34451 |
	When Checking "WMWZB3C55BWM46667"
	Then The Vehicle is considered Unauthroized
	When Checking "1FMYU60EXYUA30399"
	Then The Vehicle is considered Unauthroized
	When Checking "5GAKRBKD5EJ376173"
	Then The Vehicle is considered Unauthroized
	When Checking "3VWCD21Y33M352232"
	Then The Vehicle is considered Unauthroized