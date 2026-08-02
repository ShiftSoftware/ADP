---
hide:
    - toc
---
The service menu for one basic model code — LAYER 3 of ADP.Menus/COSMOS_REPLICATION_PLAN.md §1.1,
 the vehicle lookup's own shape.

 The menu codes and labour codes here are produced by the SAME generator the DMS export uses, over
 data replicated per row into Cosmos, so a code served here is the code the dealer's DMS received.

| Property | Summary |
|----------|---------|
| BasicModelCode <div><strong>``string``</strong></div> | The basic model code the menu was looked up by. |
| CountryID <div><strong>``long``</strong></div> | The country the part prices and labour rate were resolved for. |
| Language <div><strong>``string``</strong></div> | The language the codes and descriptions were generated in. |
| TransferRate <div><strong>``decimal``</strong></div> | The transfer rate the consumable was scaled by. 1 means unscaled. |
| Variants <div><strong>``List<ServiceMenuVariantDTO>``</strong></div> | The menu variants authored for this model. Usually one; several when a model has variant-specific menus. Ordered by variant id. |
| NotFound <div><strong>``bool``</strong></div> | True when no menu documents exist for this model code at all — as opposed to a menu that exists but generates no lines (every variant deleted, or no interval whose group carries a labour detail). Lets a UI distinguish "this model has no menu" from "nothing is due". |
