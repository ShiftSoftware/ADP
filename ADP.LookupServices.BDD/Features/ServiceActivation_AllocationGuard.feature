Feature: Service Activation Allocation Guard
  When LookupOptions.RequireAllocationForActivation is enabled, warranty activation is only
  offered to a requester whose company has a vehicle entry for the vehicle (i.e. it has been
  allocated to them). Otherwise activation is blocked, and with no requesting company the
  affordance is suppressed. With the guard off, behaviour is unchanged (Required whenever due).

Scenario: Guard off - activation is offered whenever it is due (legacy behaviour)
  Given activation is due
  When resolving the warranty activation status
  Then the warranty activation status is "Required"

Scenario: Guard on - offered when the vehicle is allocated to the requesting company
  Given vehicles in dealer stock:
    | VIN               | CompanyID |
    | 1FDKF37GXVEB34368 | 1         |
  And LookupOptions has require-allocation-for-activation enabled
  And the requesting company is "1"
  And activation is due
  When resolving the warranty activation status
  Then the warranty activation status is "Required"

Scenario: Guard on - blocked when the vehicle is not allocated to the requesting company
  Given vehicles in dealer stock:
    | VIN               | CompanyID |
    | 1FDKF37GXVEB34368 | 1         |
  And LookupOptions has require-allocation-for-activation enabled
  And the requesting company is "2"
  And activation is due
  When resolving the warranty activation status
  Then the warranty activation status is "BlockedNotAllocated"

Scenario: Guard on - suppressed when there is no requesting company
  Given vehicles in dealer stock:
    | VIN               | CompanyID |
    | 1FDKF37GXVEB34368 | 1         |
  And LookupOptions has require-allocation-for-activation enabled
  And activation is due
  When resolving the warranty activation status
  Then the warranty activation status is "NotRequired"

Scenario: Activation not due - no affordance regardless of guard or company
  Given vehicles in dealer stock:
    | VIN               | CompanyID |
    | 1FDKF37GXVEB34368 | 1         |
  And LookupOptions has require-allocation-for-activation enabled
  And the requesting company is "1"
  And activation is not due
  When resolving the warranty activation status
  Then the warranty activation status is "NotRequired"
