---
hide:
    - toc
---
Used to define the price of a part in a specific region.

| Property | Summary |
|----------|---------|
| RegionHashID <div><strong>``string``</strong></div> | The Region Hash ID from the Identity System. |
| RetailPrice <div><strong>``decimal?``</strong></div> | The Retail Price of the part in the region. |
| PurchasePrice <div><strong>``decimal?``</strong></div> | The retailer purchase price of the part in the region. (Alos known as the distributor sell price) |
| WarrantyPrice <div><strong>``decimal?``</strong></div> | The warranty price of the part in the region. (As reimbursed by the distributor) |
| RetailUnitPrices <div><strong>``IEnumerable<PartUnitPriceModel>``</strong></div> | The retail price of the part in the region broken down by selling unit (e.g. each, box). When provided, unit names must be unique and at most one entry may be marked as the default. These rules are validated on read (the path consumers and serializers go through) so they hold against the current contents of the list, including items added after assignment; a violation throws an [InvalidOperationException](/generated/System/InvalidOperationException.html). |
