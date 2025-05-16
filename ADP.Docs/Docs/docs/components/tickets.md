# Support Ticket System
A centralized ticketing system that accepts inquiries and routes them to the appropriate dealer or distributor departments for resolution.

## Ticket Types
There are five main types of tickets that can be created by users (Customers, Dealer, or Distributor Staff):

- **General Inquiry**: Requests for general information, complaints, or other feedback.
- **Vehicle Quotation**: Requests for a quotation for buying, selling, or trading a specific vehicle model.
- **SSC Inquiry**: Inquiries about the SSC information for an Unauthorized Vehicle.
- **Service Booking**: Requests for a service appointment for a vehicle.
- **Test Drive**: Requests for a test drive of a vehicle.

## Ticket Assignment
Any given ticket can have the following assignment statuses:

- **Unassigned**: The ticket has not been assigned yet. It must be reviewed by the distributor and assigned to a dealer or distributor staff.
- **Assigned to Dealer**: The ticket has been assigned to a dealer (Company) without a specific branch/team. The dealer can assign the ticket to a specific branch or team.
- **Assigned to Branch**: The ticket has been assigned to a specific branch of a dealer or distributor.
- **Assigned to Team**: The ticket has been assigned to a specific team of a dealer or distributor.

!!! note
    Dealers, Branches, and Teams are created and maintained on the [Identity System](identity.md).

## Ticket Sources
Tickets can be created from the following sources:

- **Web Portal**: The ticket is created by a user on the ticket web portal.
- **API**: The ticket is created using the API. The API can be called from any authorized external system (Website, Mobile App, Chatbot, etc.).

## Ticket Status
Some of the most common statuses that a ticket can have are the following:

- **New**: The ticket has been created and has not been reviewed yet.
- **In Progress**: The ticket is being reviewed and worked on by the assigned dealer or distributor staff.
- **On Hold**: The ticket is on hold and is not being worked on.
- **Pending Customer**: The ticket is waiting for a response from the customer.
- **Unresolved**: The ticket cannot be resolved and is closed.
- **Resolved**: The ticket has been resolved and closed.

## Communication Channels
The ticket system supports the following communication channels:

- **Chat Apps**: WhatsApp, Viber, Facebook Messenger, Telegram, etc.
- **Email**: Outgoing notifications only.

!!! warning
    Chat App integration does not come out of the box and must be implemented in accordance with the third-party vendor that the distributor is using.