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
in both modes; every panel's skeleton is a `.loading .x` descendant selector.

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

While `isLoading`, the head stays where it is: every element a panel marks `.lookup-skeleton`
(lookup-motion.css — give it to anything, it is inert until the wrapper is `.loading`) keeps its box
and becomes a sheen bar in place, the way the pill does. On a group it is one bar; on the parts, one
each. A panel's own `.loading` rules are for its body and for anything verdict-coloured beside that
text (the timeline greys its KPI rail).

### The standard head and the tab region

`LookupHead` (`lookup-head.tsx` + `lookup-head.css`) is the family's head — title, split-cap pill,
any control beside the pill — for a panel that has no head of its own yet; `recordVerdict` is the
verdict of a panel that shows records rather than a judgement. A panel marks the region under its
head `.lookup-slide`.

`LookupTabs` + `createTabRegion` (`lookup-tabs.tsx`) is a composite's tab region: every panel mounted
always, one active and in flow, the rest hidden, inert and parked a short travel to one side
(`data-tab-park` on the panel's host, physical `left`/`right`, from `tabPark(tag, active, order,
direction)`). On a switch the card and every head band stay put; the head content hands over
through the band (up and out, in from below, over `--tab-head-settle`) and each panel's
`.lookup-slide` changes in sequence, barely overlapping: the outgoing fades as it lifts
`--tab-exit-travel` (8px) over `--tab-exit` (220ms, `--tab-exit-ease`); after `--tab-enter-delay`
(160ms) the incoming fades in as it rises `--tab-enter-travel` (12px) into place over `--tab-enter`
(400ms) with `--tab-ease`, a long even deceleration. 560ms in all, inside the head's hand-over (the
tab durations are the non-loop durations longer than `--settle`, a deliberate exception). The
region's height goes from the height it is showing to the incoming tab's over `--tab-height-settle`
and is released to `auto` on settle. A `ResizeObserver` on the active host follows later growth the same way. Every value is
read from the live box, so a switch mid-switch reverses from wherever the content is.
