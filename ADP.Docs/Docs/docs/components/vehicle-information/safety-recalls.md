## Safety Recalls

A recall is issued when a vehicle or part of a vehicle is found to be defective in a way that could affect the safety of the vehicle. 

In most cases, the manufacturer will provide a remedy for the defect, which may include repairing or replacing the defective part.  
 
Vehicles that are subject to a recall can be looked up by their VIN using the **Vehicle Lookup Feature**.


## Recalls on **Authorized** Vehicles
The distributor has precise recall records for Authorized Vehicles. Making the lookup process straightforward by simply checking the database for the VIN.


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
