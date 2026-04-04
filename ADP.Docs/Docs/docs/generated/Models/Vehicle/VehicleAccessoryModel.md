---
hide:
    - toc
---
Represents an accessory or aftermarket part installed on a vehicle.
 Tracks the part details, installation job/invoice references, and an optional image.

| Property | Summary |
|----------|---------|
| VIN <div><strong>``string``</strong></div> | The Vehicle Identification Number (VIN) this accessory is installed on. |
| PartNumber <div><strong>``string``</strong></div> | The part number of the installed accessory. |
| PartDescription <div><strong>``string``</strong></div> | A description of the accessory part. |
| JobNumber <div><strong>``int``</strong></div> | The job number from the dealer's service system for the accessory installation. |
| InvoiceNumber <div><strong>``int``</strong></div> | The invoice number for the accessory installation. |
| Image <div><strong>``string``</strong></div> | URL of an image of the installed accessory. |
| CompanyHashID <div><strong>``string``</strong></div> | The Company Hash ID from the Identity System. |
