Feature: Service Activation Token
  When a lookup offers warranty activation (status Required) to the requesting company, it also returns an
  activation token signed with LookupOptions.SigningSecretKey. The token names the VIN and that company and
  expires after LookupOptions.SignatureValidityDuration, so the host's activation endpoint can accept an
  activation only where a lookup offered one.

Background:
  Given vehicles in dealer stock:
    | VIN               | InvoiceDate | CompanyID | BranchID | BrandID |
    | 1FDKF37GXVEB34368 | 2026-01-15  | 1         | 10       | 1       |
  And service items:
    | ServiceItemID | Name       | BrandID | ActiveForMonths | MaximumMileage |
    | SI-001        | Oil Change | 1       | 24              | 10000          |
  And LookupOptions has include-inactivated-free-service-items enabled
  And warranty start date does not default to invoice date
  And LookupOptions has require-allocation-for-activation enabled
  And LookupOptions has signature validity duration of 30 minutes
  And the current UTC time is "2026-03-01 09:00:00"

Scenario: An offered activation carries a token only that company can use, for that vehicle, until it expires
  Given the lookup signing key is "test-signing-key"
  And the requesting company is "1"
  When looking up "1FDKF37GXVEB34368" end-to-end
  Then the warranty activation status is "Required"
  And the warranty activation token is valid for "1FDKF37GXVEB34368" and company "1" at "2026-03-01 09:30:00"
  And the warranty activation token is not valid for "1FDKF37GXVEB34368" and company "2" at "2026-03-01 09:00:00"
  And the warranty activation token is not valid for "1HGCM82633A004352" and company "1" at "2026-03-01 09:00:00"
  And the warranty activation token is not valid for "1FDKF37GXVEB34368" and company "1" at "2026-03-01 09:30:01"
  And the warranty activation token with a later expiry written into it is not valid for "1FDKF37GXVEB34368" and company "1" at "2026-03-01 09:00:00"

Scenario: A blocked activation carries no token
  Given the lookup signing key is "test-signing-key"
  And the requesting company is "2"
  When looking up "1FDKF37GXVEB34368" end-to-end
  Then the warranty activation status is "BlockedNotAllocated"
  And no warranty activation token is issued

Scenario: No token is issued without a signing key
  Given the requesting company is "1"
  When looking up "1FDKF37GXVEB34368" end-to-end
  Then the warranty activation status is "Required"
  And no warranty activation token is issued
