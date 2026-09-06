# Fonts

`harness.src.css` declares one web font:

| Family | Weight | File this directory expects | Used for |
|---|---|---|---|
| Speda | 700 | `speda-bold.woff2` | `html[lang="ar"]` and `html[lang="ku"]` |

The file is **not committed** — it is not ours to redistribute. Two ways to get it
rendering:

1. **Install it on the machine.** The `@font-face` lists `local('Speda Bold')` first,
   so an installed copy is used with no request at all.
2. **Self-host it.** Drop `speda-bold.woff2` in this directory. Nothing else to
   change; the `url()` already points here, relative to the stylesheet, so it works
   in dev and at any mount of the built site.

With neither, the stack falls through to `Noto Kufi Arabic` and then the platform
Arabic face. Nothing breaks — the two RTL languages just render in a different
typeface, and the browser logs one 404 for the missing woff2.

Latin and Cyrillic (`en`, `ru`) are unaffected: they keep the system stack.
