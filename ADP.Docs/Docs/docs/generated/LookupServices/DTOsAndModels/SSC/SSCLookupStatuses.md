---
hide:
    - toc
---
The result status of an SSC (Special Service Campaign) / safety recall lookup.

| Value | Summary |
|-------|---------|
| NoRecall | No active recalls found for this vehicle. |
| RecallExists | One or more active recalls exist for this vehicle. |
| NoApplicableVehicleFound | The vehicle was not found in the manufacturer's recall database. |
| TmcErrorResponse | An error occurred while communicating with the manufacturer's (TMC) recall system. |
| RelayServerNoResponse | No response was received from the relay server. |
| PendingTMCLookup | The VIN is unauthorized and a TMC lookup has been queued. Results will be available on the next lookup. |
