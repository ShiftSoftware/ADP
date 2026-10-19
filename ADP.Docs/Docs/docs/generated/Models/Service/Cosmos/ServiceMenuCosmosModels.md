---
hide:
    - toc
---
A service interval. Its `Code` is a component of every periodic menu code, and its
 `Description` is the periodic line's description.

| Property | Summary |
|----------|---------|
| ServiceIntervalID <div><strong>``long``</strong></div> |  |
| Code <div><strong>``string``</strong></div> |  |
| PaidDescription <div><strong>``string``</strong></div> | The periodic-line description for a `MenuType.Paid` variant. The three descriptions are picked by the variant's `MenuVariantCosmosModel.MenuType` and used verbatim — never language-resolved. |
| FreeDescription <div><strong>``string``</strong></div> | The periodic-line description for a `MenuType.Free` variant. |
| ContractDescription <div><strong>``string``</strong></div> | The periodic-line description for a `MenuType.Contract` variant. |
| ValueInMeter <div><strong>``int``</strong></div> |  |
| ServiceIntervalGroupID <div><strong>``long``</strong></div> |  |
| IsDeleted <div><strong>``bool``</strong></div> |  |
