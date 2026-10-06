# Shared Engineering Explorer shell

Hawta and Rastgo use one browser shell, extracted from the Hawta Explorer.
No package installation, framework build or CDN is required.

- `theme.css` and `engineering.css` are the original Hawta styles, moved intact.
  They own themes, typography, icons' placement, overview column proportions,
  detail controls, canvas/inspector sizing, scene primitives and mobile drawers.
- `shell.js` mounts the same header, overview and detail DOM for both products.
  Product HTML supplies its title, icon, theme-storage key, diagram, responsibility
  boundary, inspector bodies and fourth inspection-layer label.
- `overview.js` owns selection, paths, illustrative particles, motion controls,
  reduced-motion handling and the overview drawer. Each product's `overview.js`
  supplies stage explanations and connections. It exposes `ExplorerMotion` and
  emits `explorer:motion-state`; detail views share that motion state.
- Product-specific scenarios, evidence, scene rendering and inspector content
  stay in each product's Explorer folder. Rastgo's local CSS only styles its
  verdicts, result rows and scene content; it does not redefine the shell.

The local servers explicitly map `/shared/` browser assets to this directory.
They never expose the directory generally. Keep `ADP.Explorer.Shared` alongside
`ADP.Hawta` and `ADP.Rastgo` when relocating a source checkout.

## Portable exports

Shared source assets do not require a server in the exported page. Both export
formats are built from the same maintained source:

| Entry point | Supporting files | Opening it | Source freshness |
| --- | --- | --- | --- |
| Checkout's product `Explorer/index.html` | Shared source directory and product assets | Run the product's `server.mjs`; direct opening of this source entry is unsupported | Live comparison through the server |
| Exported folder `index.html` | Keep the whole exported folder, including `shared/` | Open the exported file or use static HTTP hosting | Embedded snapshot; unverified |
| Exported single HTML | None | Open or share the one `.html` file | Embedded snapshot; unverified |

The source entry relies on `/shared/` routes provided by the local server. Earlier
asset-folder exports also fetched evidence JSON, which browsers can block for
`file://` pages. Both export formats now embed captured evidence; single-file
export additionally embeds every stylesheet, script and icon.

For portable **single HTML files**, choose new filenames under an existing parent:

```sh
node ADP.Explorer.Shared/export.mjs rastgo /path/to/rastgo.html --single-file
node ADP.Explorer.Shared/export.mjs hawta /path/to/hawta.html --single-file
```

No Node runtime or server is required to use these generated files. Their content
security policy blocks network connections; overview, mechanisms, scenarios,
themes, motion, inspection layers and captured source excerpts are all local.
Hawta's editable change brief also works locally. If a browser denies clipboard
access, select and copy the prepared draft manually using the existing fallback.

For an **asset-folder export**, choose a new directory under an existing parent:

```sh
node ADP.Explorer.Shared/export.mjs rastgo /path/to/new-rastgo-explorer
node ADP.Explorer.Shared/export.mjs hawta /path/to/new-hawta-explorer
```

Keep the folder's files together. Both formats contain no source checkout,
credentials or freshness API. Shared `evidence.js` reads the embedded snapshot
without fetching and explicitly says that current source files have not been
checked. Even over HTTP, an export never probes for source files. Use the normal
source preview when live source comparison is needed.

Exports are deterministic for the current source and captured evidence. The
command refuses existing destinations; regenerate to a new file/directory after
editing source or deliberately refreshing evidence. Generated artifacts can live
under the repository's ignored `.scratch/`; do not hand-edit or commit them.

After shared changes run `node ADP.Explorer.Shared/verify.mjs`, both product
verifiers and Hawta's portability test.
Compare both products at the same actual viewport and normal zoom, including
overview/detail, light/dark, mobile drawers and keyboard focus. Check Hawta
against its pre-change layout; content-specific diagrams need not be identical.
