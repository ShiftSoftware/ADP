Feature: Shared Logic

Scenario: Data Parser
	Given a dealer with the following vehicles in their dealer stock (coming from their DMS):
		| VIN               |
		| 5TDJK3DC1BS013795 |
	And a dealer with the following vehicles in official SSC Vehciles (Provided by the vehicle manufacturer):
		| VIN               |
		| 1N4AL11D65N937700 |
	And a dealer with the following vehicles as initial stock:
		| VIN               |
		| 1G1AP87H2DL161084 |
	When Checking "5TDJK3DC1BS013795"
	Then The Vehicle is in VehicleEntries
	When Checking "1N4AL11D65N937700"
	Then The Vehicle is in SSCAffectedVINs
	When Checking "1G1AP87H2DL161084"
	Then The Vehicle is in InitialOfficialVINs