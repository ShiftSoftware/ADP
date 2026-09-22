## Vehicle info layout — the card

`VehicleInfoLayout` is the card of the component design language, drawn once for every panel in the
vehicle-lookup family. A panel renders it inside its own shadow root and `@import`s
`vehicle-info-layout.css` into its own stylesheet.

```
.lookup-panel[dir][.loading]                     no padding of its own: the card fills the element
  section.lookup-card[data-phase][data-verdict]  --paper, 1px --line, radius 13, --shadow, overflow hidden
    ::before                                     4px accent bar in the verdict's fill; --line idle; fades while busy
    span.vehicle-info-header-vin.sr-only         the VIN, screen readers only — no visible identifier row
    div.lookup-body                              overflow hidden; scoped scrollbars
      div.lookup-content                         the panel's head, strip and body
```

With `coreOnly` the wrapper renders `div.lookup-core[.loading]` around the children and nothing else —
for a panel embedded in a composite that draws the card itself. `loading` is on the outermost element
in both modes; every panel's in-flight rule is a `.loading .x` descendant selector.

### Props

| Prop                      | Purpose                                                                                                     |
| ------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `coreOnly`                | children only, no card                                                                                      |
| `isLoading`               | `loading` class; `data-phase="busy"` on the card, which fades the accent                                    |
| `direction`               | `dir` on the panel root                                                                                     |
| `isError`, `errorMessage` | the error band, open while `isError`, with the translated message                                           |
| `header`                  | the VIN, rendered screen-reader-only                                                                        |
| `verdict`                 | `VerdictState` (`positive`, `negative`, `neutral`, `attention`, `idle`); colours the accent. Default `idle` |

`headerRight` no longer exists: a panel owns its head even when embedded, so anything that used to sit
in the identifier band (claimable-items' trace trigger) lives in the panel's own head.

### `::part` names

| Part                      | Element                                                       |
| ------------------------- | ------------------------------------------------------------- |
| `vehicle-info-container`  | `.lookup-card`                                                |
| `vehicle-info-error`      | `.lookup-error`                                               |
| `vehicle-info-body`       | `.lookup-body`                                                |
| `vehicle-info-content`    | `.lookup-content` (standalone) or `.lookup-core` (`coreOnly`) |
| `vehicle-info-header-vin` | the screen-reader-only VIN span                               |

`vehicle-info-header` names no element any more. There is no header row: the panel's head is the head.

### The panel's side

```typescript
  // #region Vehicle info layout prop

  @Prop() coreOnly: boolean = false;

  // #endregion
```

The panel passes its own verdict from an exported pure function (`panelVerdict` in its functional
component; `SscCampaigns.panelVerdict` is the model) and emits `verdictChange` whenever it changes, so
a composite can colour its one card from the active panel.

### The head in flight

While `isLoading`, the head's content (`.lookup-head-content` — the title and the summary, the
same blocks the tab switch moves; the family's `LookupHead` and the two reference panels' own heads
alike) leaves: it drops out of the band's bottom edge (`transform: translateY(…)`,
`--head-leave-travel` = 1.25 of its own height) as it fades to 0 over `--settle`, and the band's
wait spinner (`LookupHeadWait`, rendered last in every head band, in every state) rises from under
the band's clip into the content's place on the same clock. When the lookup lands the spinner
sinks back out and the new content comes up from below as it fades in. The band, its rule and its
height stay; a transform, so nothing under it moves. The drop is on `transform` and the tab
switch's slide on `translate` so each keeps its own clock at rest. There is no head skeleton. A
panel's own `.loading` rules are for its body.

### The standard head and the tab region

`LookupHead` (`lookup-head.tsx` + `lookup-head.css`) is the family's head — title, split-cap pill,
any control beside the pill — for a panel that has no head of its own yet; `recordVerdict` is the
verdict of a panel that shows records rather than a judgement. It decides the pill (`state`,
`text`) and the accent bar (`accent`) together, and they part ways in one state: an authorized
vehicle with records on file has no pill — the records are the statement — and a green bar, the
lookup succeeded and the panel holds what it asked for. A panel with a judgement of its own to make
there (a reading over its threshold, a service overdue) passes `recordsVerdict: 'negative'` from its
data; the other states colour the bar from the pill and ignore it. The panel gives the wrapper and
`verdictChange` the `accent`, and `LookupHead` the pill. A panel marks the region under its head
`.lookup-slide`.

`LookupTabs` + `createTabRegion` (`lookup-tabs.tsx`) is a composite's tab region: every panel mounted
always, one active and in flow, the rest hidden, inert and parked a short travel to one side
(`data-tab-park` on the panel's host, physical `left`/`right`, from `tabPark(tag, active, order,
direction)`). On a switch the card and every head band stay put; the head content hands over
through the band (up and out, in from below, fading to nothing at both ends) and each panel's
`.lookup-slide` changes with it: the outgoing fades as it lifts `--tab-exit-travel` (8px), the
incoming fades in as it rises `--tab-enter-travel` (12px) into place. All of it — head content, the
band's height, the region's height, both bodies — is on the language's one clock, `--settle` with
`ease`, the same movement as a lookup's leave and arrive (owner, 2026-09-22; the switch had four
clocks and a front-loaded curve of its own before that, and read as abrupt beside the lookup). The
region's height goes from the height it is showing to the incoming tab's and is released to `auto`
on settle. A `ResizeObserver` on the active host follows later growth the same way. Every value is
read from the live box, so a switch mid-switch reverses from wherever the content is.
