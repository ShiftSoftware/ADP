---
hide:
    - toc
---
Represents a monetary value with currency and formatting information.

| Property | Summary |
|----------|---------|
| Value <div><strong>``decimal?``</strong></div> | The numeric price value. |
| CurrencySymbol <div><strong>``string``</strong></div> | The currency symbol (e.g., $, د.ع). |
| CultureName <div><strong>``string``</strong></div> | The culture name used for formatting (e.g., "en-US", "ar-IQ"). |
| UnitPrices <div><strong>``IEnumerable<PartUnitPriceDTO>``</strong></div> | The price broken down by selling unit (e.g. each, box). Only populated for retail prices.
 Unit names are unique and exactly one entry is marked as the default. |
| FormattedValue <div><strong>``string``</strong></div> | The price formatted as a currency string using the associated culture. |
