Feature: Authorized Initial Stock
  A VIN that does not exist in dealer stock is still considered Authorized if it exists in initial stock.
  
  Scenario: Authorized Initial Stock
    Given A VIN "1FDKF37GXVEB34368" that exists in initial stock
    When looking it up
    Then it should be marked as Authorized