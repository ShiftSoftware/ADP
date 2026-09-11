# Demo images for the generated fixtures

The generated fixtures (`../generated/<environment>/*.json`) point every picture at this
directory through the CDN copy of the package:

```
https://cdn.jsdelivr.net/npm/adp-web-components@latest/dist/mocks/assets/<family>/<file>.svg
```

The Stencil build copies `features/mocks/data` to `dist/mocks` (and to `www/mocks` for the dev
server), so the fixtures, the drawings and the components ship together: one URL works in the dev
showcase, the docs site and any host running `isDev`, with no storage account, no token and no
expiry. On the CDN a new file resolves from the first `release-web-components-*` tag that carries
it; a dev build rewrites the prefix to `http://localhost:3000/mocks/` (`../mock-assets.ts`), so
locally the pictures render straight away.

Which file a fixture value maps to is decided by `ADP.TestData/Generator/DemoAssets.cs` — the
mapping is deterministic, so regenerated fixtures stay byte-stable. Keep the two in step: a drawing
added here is only reachable once the generator's known set names it.

## Families

One family per use, so no generic picture is reused across unrelated things.

| Family | Files | Frame | Used by |
|---|---|---|---|
| `paint-panels/` | `<slug>.svg` (plan view: top-down car, that panel highlighted and labelled) and `<slug>-detail.svg` (close-up with a gauge marker) for `hood`, `roof`, `tail-gate`, `fender-{front,rear}-{left,right}`, `door-{front,rear}-{left,right}`; `panel.svg` is the unhighlighted fallback | 336 × 600, portrait — the gallery thumbnail is 84 × 150 | paint-thickness panel gallery and the certificate |
| `badges/` | `badge-1.svg` … `badge-6.svg`, abstract marks (no wordmark: company names differ per environment) | 320 × 160 | extended-warranty provider logo on the warranty timeline (`CompanyLogoResolver`) |
| `documents/` | `signed-claim-document.svg` (a claim form with a customer signature), `service-invoice-qr.svg` (an invoice with a QR placeholder — the pattern is not a real code) | 640 × 480 | the claim warnings' `imageUrl` (`StandardItemClaimWarnings` in the environment files) |
| `accessories/` | `side-steps`, `roof-rails`, `roof-rack`, `floor-mats`, `mud-guards`, `tow-bar`; `accessory.svg` is the fallback carton | 400 × 300 | accessories panel (`AccessoryImageUrlResolver`) |

## How a fixture reaches a file

- **Paint panels** — the stored key is an upload path whose stem names the panel the way inspection
  tools do, `[Side_][Position_]Type_N.jpg` (`Left_Front_Fender_1.jpg`, `Hood_2.jpg`,
  `Tail_Gate_1.jpg`). Side and position tokens become the slug; an odd index is the plan view, an
  even one the close-up, so the two photos an inspection stores per panel differ. Unknown stems get
  `panel.svg`.
- **Badges** — `badge-((companyId − 1) mod 6) + 1`.
- **Accessories** — the stem of the stored key (`Uploads/accessories/<VIN>/side-steps.jpg`) names
  the drawing; unknown stems get `accessory.svg`; a null key stays null (no picture).
- **Documents** — referenced by URL directly from the environment files.

## Source and licence

Every file is an original vector drawing made for this repository, written deterministically by
`automation/draw-demo-assets.mjs` (`npm run draw:demo-assets`; re-run it after editing the script
rather than hand-editing an SVG). No photographs, no third-party artwork, no embedded fonts — text
uses the viewer's system font. Licensed with the package (MIT, `package.json`). No file may
reference a real brand, client, storage account or place; a CC0 photograph added later must be
listed here with its source and licence.

Budget: the whole set is under 100 KB; keep it well under 500 KB so the package does not grow
noticeably.
