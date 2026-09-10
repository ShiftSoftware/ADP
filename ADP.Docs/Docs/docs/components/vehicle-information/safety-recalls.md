## Safety Recalls

A recall is issued when a vehicle or part of a vehicle is found to be defective in a way that could affect the safety of the vehicle. 

In most cases, the manufacturer will provide a remedy for the defect, which may include repairing or replacing the defective part.  
 
Vehicles that are subject to a recall can be looked up by their VIN using the **Vehicle Lookup Feature**.


## Recalls on **Authorized** Vehicles
The distributor has precise recall records for Authorized Vehicles, making the lookup process straightforward by simply checking the database for the VIN.

In addition to identifying whether a vehicle is affected by a recall, the system also determines whether the recall has been repaired or still requires action.
This is done through a multi-source repair status check:

- **Inline Data**: If the recall record itself directly indicates that the repair has been completed, this information is used.
- **Warranty Claims**: The system also checks warranty claims (if available) to confirm completion. This is determined by validating the Labor Operation Code and the presence of the Campaign Code in the warranty notes.
- **Labor Lines**: As a final step, the system reviews labor lines retrieved from the DMS (if available) for evidence that the recall-related work has been performed.

The sources are consulted in that order, and the first one that holds decides both the status and the repair date; the result names which source decided (`RepairSource`).

By combining these sources, the system ensures the most accurate status for each recall. This helps service teams and customers know not just whether a vehicle is affected, but also whether the recall has already been addressed.

### Interchangeable Labor Operation Codes
The recall feed lists one labor operation code per campaign, but a dealer system may book the same repair under a sibling code — a revision suffix, a superseded number. Left alone, such a repair goes unrecognised and the recall shows as still open on a vehicle that was fixed.

A deployment declares which codes are used interchangeably in `LookupOptions.SSCInterchangeableLaborCodeGroups`: each group is a set of equivalent codes, and a warranty claim or labor line carrying any member of a group counts as the campaign's repair whichever member the campaign itself lists. Groups that share a code are merged, and codes are compared trimmed and case-insensitively. The groups are deliberately not scoped to a campaign: in practice a code belongs to exactly one campaign family, and the campaign codes themselves are the least reliable key in hand-maintained data. The list is deployment-specific data and lives in the host's configuration, not in the platform.

### Explaining a Repair Status
When a request sets `VehicleLookupRequestOptions.TraceSSCEvaluation`, every recall in the response carries a trace of the evidence behind its status: the campaign's labor codes and the interchangeable codes accepted for them, every warranty claim on the vehicle with how it was judged, and the service-history labor lines that matched. The `<vehicle-ssc>` web component fetches this with `?trace=ssc` and shows it per campaign. The trace lists claim and invoice details, so hosts turn the option on only for callers permitted to see them.


## Recalls on **Unauthorized** Vehicles
In most cases, the distributor does not have precise recall records for Unauthorized Vehicles. However, the distributor may be able to check the VIN against the Manufacturer's system either through an integration or manually.

### Manual Manufacturer System Check
In the event that the distributor does not have an integration with the Manufacturer's system, the lookup process is as follows:

1. The user requests a recall check for an unauthorized vehicle and the VIN is logged as a ticket.
2. The distributor team will need to manually check the VIN against the manufacturer's system and update the ticket with the recall details.
3. After resolving the ticket, the results will be communicated to the user who made the request via email or other mediums.

### Integration with Manufacturer's System
In the event that the distributor has an integration with the Manufacturer's system, the lookup process is as follows:

1. The user requests a recall check for an unauthorized vehicle and the VIN is logged.
2. The system will automatically check the VIN against the manufacturer's system via and API call.
3. The Manufacturer's system may respond in one of the following ways:
	1.  Provide a list of recalls associated with the VIN.   
    	In this case, the user will be presented with all the details of the recalls, including the recall number, description, and repair status.
	2.  Provide a yes/no response indicating whether the VIN is subject to a recall.  
		In this case, a ticket will be created and the distributor team will need to manually check the VIN against the manufacturer's system and update the ticket with the recall details.
