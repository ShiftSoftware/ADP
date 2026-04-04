---
hide:
    - toc
---
Represents a broker's access permissions for a specific brand.
 Controls whether the broker is active for the brand, the account period, and which companies/branches provide service.

| Property | Summary |
|----------|---------|
| BrandID <div><strong>``long``</strong></div> | The brand ID this access record applies to. |
| Active <div><strong>``bool``</strong></div> | Whether the broker's access to this brand is currently active. |
| AccountStartDate <div><strong>``DateTime?``</strong></div> | The date from which the broker's account for this brand became active. |
| TerminationDate <div><strong>``DateTime?``</strong></div> | The date the broker's access to this brand was terminated. Null if still active. |
| LocationCode <div><strong>``string?``</strong></div> | The location code associated with this brand access. |
| CompanyID <div><strong>``long?``</strong></div> | The company ID associated with this brand access. |
| CompanyBranchID <div><strong>``long?``</strong></div> | The company branch ID associated with this brand access. |
| ServiceCompanies <div><strong>``List<long>``</strong></div> | The list of company IDs that provide service for this broker's vehicles under this brand. |
