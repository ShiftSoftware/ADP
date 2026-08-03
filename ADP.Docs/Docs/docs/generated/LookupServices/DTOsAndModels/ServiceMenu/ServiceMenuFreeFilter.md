---
hide:
    - toc
---
Which menu variants a lookup should return, by the variant's "free of charge" flag.

| Value | Summary |
|-------|---------|
| All | Every variant, free or not. The default. |
| FreeOnly | Only variants flagged free of charge. |
| PaidOnly | Only variants NOT flagged free of charge. Note this is the complement of `FreeOnly` over the model's variants, not "variants that cost something" — a not-free variant whose lines all price to 0 is still returned here. |
