import { VNode, h } from '@stencil/core';

import { revealBelow } from '~lib/reveal-below';

// The panels' shared glyph set (the features/ folder reaches into it already: lookup-head.tsx).
import { BADGE_GLYPHS, MEDIA_GLYPHS } from '../../components/vehicle-lookup/components/glyphs';

/**
 * The lookup table: the design language's table (§ 15 strip, § 18 rows, § 22 layouts) taken out of
 * `vehicle-ssc` and made reusable. A pure functional component with no state, no timers and no
 * memory; the panel that owns it runs the choreography (retainedBody, the leave, the reshape) and
 * hands it flags. Spec: .shift/repos/adp/web-components/lookup-table.md.
 *
 * It is a function and not an element because the strip's headings and the body's rows must be
 * `subgrid`s of one grid, inside the panel's own shadow root, between the panel's head and its
 * body. The stylesheet is `lookup-table.css`, a fragment that the panel `@import`s.
 *
 * What it never does: decide a verdict, pick a notice, or read meaning off an empty list. The panel
 * says what the strip shows (`lead`, `notice`) and what the body holds (`body`, `open`).
 */

// ---------------------------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------------------------

/**
 * Built kinds. 'chip' is specified and deferred to a named consumer (spec § 7).
 *
 * - `date`: an ISO calendar date as the DTO sends it, read, not computed (`dateText`).
 * - `number`: a quantity or a reading (odometer), grouped for the table's `lang`, end-aligned.
 * - `action`: the row's expand button (`RowActions`); it has no value, only the row it belongs to.
 */
export type CellKind = 'text' | 'code' | 'media' | 'date' | 'number' | 'action';

export type NoticeTone = 'positive' | 'negative' | 'neutral' | 'attention' | 'idle';

/**
 * What the strip shows. The column headings are a fixed structure (they depend on `hiddenFields`
 * only, never on the data), so they are there before the answer is known; only an outcome that has
 * no rows to head replaces them with its notice. There is no skeleton: a skeleton would stand for a
 * shape that is not in doubt (owner, F1, 2026-10-18).
 */
export type LeadKind = 'columns' | 'notice';

export type LookupColumn<Row> = {
  /** The hiddenFields name and the column's stable key: 'partNumber', 'description', ... */
  field: string;
  /** Heading in the strip and caption on the narrow card (data-label). Already localised. */
  label: string;
  kind: CellKind;
  /**
   * The cell's value; for 'media' the image URL. null, undefined, '' or whitespace is blank. An
   * `action` column has no value and may leave this out; a `number` column's value must be a
   * finite number or a string that is one, anything else is blank.
   */
  value?: (row: Row) => string | number | null | undefined;
  /** The row's name: Code type, uncaptioned on a card, first in the stacked layout. At most one. */
  identity?: boolean;
  /** The one column that absorbs the slack (§ 18). At most one, and only on a 'text' column. */
  grow?: boolean;
};

export type LookupTableStrings = {
  /** The strip's one word at 720px and under, e.g. "Accessory list". */
  columnsLabel: string;
  /** Media cell, no URL on record. */
  noImage: string;
  /** Media cell, the URL did not load. */
  imageFailed: string;
  /** The media button's label, followed by the row's identity value. */
  openImage: string;
  /** The action button's label while its drawer is shut ("Show details"), followed by the row's identity value. Required by a table with an `action` column. */
  expandRow?: string;
  /** The same while the drawer is open ("Hide details"). */
  collapseRow?: string;
};

/**
 * What an `action` column needs from the panel: which row's drawer is open and which row is being
 * fetched, and where a press goes. Keys are the panel's row identity (`keyOf`, an invoice number,
 * not a position), so an open drawer can follow its row through a lookup (run decision 2). The table
 * draws the button; the drawer it controls is the panel's, a row-spanning region whose element id is
 * `drawerId(key)`.
 */
export type RowActions<Row> = {
  /** The key of the row whose drawer is open, or null. The button says `aria-expanded` and the row tints. */
  expandedKey: string | null;
  /** The key of the row whose details are being fetched: its button spins, whatever else it shows. Optional: a drawer with nothing to fetch never sets it. */
  fetchingKey?: string | null;
  /** A press on a row's button. The table keeps no open state of its own. */
  onToggle: (key: string) => void;
  /**
   * The row's identity. Omitted, the table's own `rowKey` (the one the vdom uses) is the identity.
   * It must be unique per row: the open drawer, the spinning button and the row's tint are all
   * found by this key, so two rows with the same one open and tint together. Compose it from
   * whatever makes a row unique (company + branch + invoice), not just the field that looks like an
   * id. The table does not repair a duplicate; it warns on the console, once per render, naming it.
   */
  keyOf?: (row: Row, index: number) => string;
  /**
   * Drawers that are sliding shut: their content stays mounted until the owner clears the list a
   * settle after the close (motion.md rule 5: content leaves only once its region has shut). A row
   * is a slot at a position, so the record names the slot (`index`) and carries the row the drawer
   * was showing, which may no longer be the row at that slot (a lookup replaced it). Build it with
   * `closingDrawers`. Only read when the table has a `drawer`.
   */
  closing?: ClosingDrawer<Row>[];
};

/** A drawer that is shutting: the slot it hangs from and the row whose content it still shows. */
export type ClosingDrawer<Row> = { index: number; row: Row };

export type Density = 'comfortable' | 'compact';

export type LookupTableProps<Row> = {
  /** Already filtered: pass `visibleColumns(all, hidden)`. */
  columns: LookupColumn<Row>[];
  rows: Row[];
  rowKey: (row: Row, index: number) => string;
  /** The panel's own lead (`tableLead(...)`, or a wrapper of it for a panel with more states). */
  lead: LeadKind;
  notice: { tone: NoticeTone; text: string } | null;
  /** `data-phase="busy"`: a lookup is in flight. The strip does not change for it (the headings stay); the attribute is a state hook for the panel's own rules. */
  busy: boolean;
  /** What the body shows: 'rows', or the panel's own kind (rendered from `children`). */
  body: string;
  /** The body's `data-open`. */
  open: boolean;
  /** A live column change is in progress: headings fade, roles go (§ 5.3). */
  reshaping?: boolean;
  strings: LookupTableStrings;
  /** The owner's `openImageViewer`; the table only reports which image was asked for. */
  onOpenMedia?: (image: HTMLImageElement, src: string) => void;
  /**
   * The family's short language code (en, ar, ku, ru; never `.language`, see NUMBER_LOCALES): the
   * digits and grouping of a `number` cell. Omitted, English.
   */
  lang?: string;
  /** `compact`: the nested-table rhythm (spec § 14.3), set through tokens on the grid. Default comfortable. */
  density?: Density;
  /**
   * An embedded table (spec § 7): the headings are its first row and there is no strip, no notice,
   * no collapsible body and no slots, because it lives inside a drawer that already is the region
   * that slides. A column empty on every row is dropped (`nonEmptyColumns`, run decision 12). It
   * ignores `lead`, `notice`, `body`, `open`, `reshaping` and `slots`; `rows: []` renders an empty grid.
   */
  embedded?: boolean;
  /** An accessible name for the table's `role="table"`, for a table that has no heading of its own (an embedded one). */
  label?: string;
  /** The action column's state and callback (see RowActions). */
  actions?: RowActions<Row>;
  /**
   * The drawer under each row (design language § 18): a row-spanning `.collapsible` inside the row's
   * slot, opened by `actions.expandedKey` and by a press on the row itself. Returns the drawer's
   * content, which the table sets inside the drawer's panel (margin, paper, rim and rail are the
   * table's) and puts under the skeleton while the row is. It is called only for the open row and
   * for a closing one (`actions.closing`), never for a shut row, so a long list builds one drawer.
   * Return null while the content is not there yet: a panel opens the drawer (sets `expandedKey`) when
   * it has something to show, and spins the button (`fetchingKey`) while it fetches. Needs `actions`.
   * Not used by an embedded table. A table without it renders exactly as before.
   */
  drawer?: (row: Row, key: string) => VNode | null;
  /**
   * One entry per row in `rows`: whether the row's slot is open and how its values are veiled.
   * Omitted, every row is open and unveiled. A panel that keeps its rows on screen through a lookup
   * (owner, 2026-10-25) passes `tableSlots(...)` and keys its rows by slot, so a row is a place that
   * stays while its values change under a cover, and only the rows that come or go slide.
   */
  slots?: RowSlot[];
};

/**
 * How a row's values are veiled. `cover`: the skeleton is over them (a lookup is in flight, or the
 * row is leaving after one); `fade`: they are faded out (a page is changing, nothing is fetched);
 * `none`: they are read.
 */
export type RowVeil = 'none' | 'cover' | 'fade';

export type RowSlot = { open: boolean; veil: RowVeil };

// ---------------------------------------------------------------------------------------------
// Derivations: pure, exported, unit-tested, no timers
// ---------------------------------------------------------------------------------------------

/**
 * The slots for `current` rows followed by `outgoing` ones (rows the body held before and no longer
 * does, rendered after the current ones so they can leave where they stood).
 *
 * - The current rows' first `opened` slots are open; the rest are new and start shut, and the owner
 *   raises `opened` a frame later so they slide open rather than appear.
 * - Outgoing rows shut on their own while the body stays open. When the body itself shuts (the
 *   outcome has no rows), they stay open inside it, so the region leaves as one movement and not
 *   one inside another.
 */
export const tableSlots = ({
  current,
  outgoing,
  opened,
  bodyOpen,
  currentVeil,
  outgoingVeil,
}: {
  current: number;
  outgoing: number;
  opened: number;
  bodyOpen: boolean;
  currentVeil: RowVeil;
  outgoingVeil: RowVeil;
}): RowSlot[] => [
  ...Array.from({ length: current }, (_, index) => ({ open: index < opened, veil: currentVeil })),
  ...Array.from({ length: outgoing }, () => ({ open: !bodyOpen, veil: outgoingVeil })),
];

/** Comma-separated field names, trimmed, empty names dropped. Moved here from the sale panel. */
export const parseHiddenFields = (raw?: string): Set<string> =>
  new Set(
    (raw ?? '')
      .split(',')
      .map(field => field.trim())
      .filter(Boolean),
  );

/** Definition order kept; a hidden field removes its column entirely; unknown names are ignored. */
export const visibleColumns = <Row,>(columns: LookupColumn<Row>[], hidden: ReadonlySet<string>): LookupColumn<Row>[] => columns.filter(column => !hidden.has(column.field));

// The track recipes, restated here because the set of columns varies with hiddenFields and so the
// strings cannot live in the stylesheet.
//
// Every track is a floor and a share of the width, never the content's own size (owner, 2026-10-25,
// replacing § 18's `minmax(auto, max-content)`). Content-sized tracks fit whatever values were on
// screen, so a lookup or a page that brought a longer part number or company re-laid every column
// out sideways on the frame the values changed. A share of the width depends only on the panel's
// width: the columns stand still while the values under them change. Each floor holds its kind's
// longest ordinary value and its heading, so a column is not crushed on a narrow panel; past the
// floors, the grid scrolls sideways inside the card (`overflow-x: auto`), as before.
/** Part numbers and codes: about eighteen characters at 760 with its padding. */
const CODE = 'minmax(180px, 1.1fr)';
/** The one column allowed to grow: the largest share of the slack, with a floor so it cannot become a sliver. */
const GROW = 'minmax(200px, 2.4fr)';
/** Other text: wraps at its spaces inside its share. */
const TEXT = 'minmax(150px, 1.2fr)';
/** The thumbnail: exactly its 4:3 box and the cell's padding, whatever the width. */
const MEDIA = 'calc(var(--lt-media-width) + 24px)';
/** A date: ten tabular characters (about 80px at 13px) with its padding; a floor and a small share, like every track. */
const DATE = 'minmax(120px, 0.8fr)';
/** A figure: an odometer reading with its grouping separators and the widest heading ("ODOMETER", small caps with tracking) fit in it. */
const NUMBER = 'minmax(110px, 0.8fr)';
/** The expand button: exactly its round box and the cell's padding, whatever the width. */
const ACTION = 'calc(var(--lt-action-size) + 24px)';
/** The stacked layout's first column (identity above its description): the slack, with a floor. */
const STACK_FIRST = 'minmax(160px, 1fr)';
/** No visible column at all: one track, so the strip's notice still spans the card. */
const SINGLE = 'minmax(0, 1fr)';

const growing = <Row,>(column: LookupColumn<Row>): boolean => !!column.grow && column.kind === 'text';

const TRACK_BY_KIND: Partial<Record<CellKind, string>> = { code: CODE, media: MEDIA, date: DATE, number: NUMBER, action: ACTION };

const track = <Row,>(column: LookupColumn<Row>): string => (growing(column) ? GROW : (TRACK_BY_KIND[column.kind] ?? TEXT));

/**
 * The `grid-template-columns` strings. `stacked` is the 720–960 layout, where the description sits
 * under its code: it exists only when the first visible column is the identity and a different,
 * growing column is visible. Otherwise it is null and the wide layout simply continues.
 */
export const tableTracks = <Row,>(columns: LookupColumn<Row>[]): { wide: string; stacked: string | null } => {
  if (columns.length === 0) return { wide: SINGLE, stacked: null };

  // A media column is a fixed width; if it is the only column, it still has to reach the card's edge.
  const wide = columns.length === 1 && columns[0].kind === 'media' ? SINGLE : columns.map(track).join(' ');

  const first = columns[0];
  const stackable = !!first.identity && columns.some(column => column !== first && growing(column));
  const stacked = stackable ? [STACK_FIRST, ...columns.filter(column => column !== first && !growing(column)).map(track)].join(' ') : null;

  return { wide, stacked };
};

/**
 * A date as the DTO sends it, read: ISO `yyyy-MM-dd`, tolerant of a full timestamp (`.slice(0, 10)`),
 * no time zone and no locale conversion (design language § 23; the same rule as the sale panel's
 * `asDate`). A day is not computed, so it cannot shift.
 */
export const dateText = (raw: string): string => raw.trim().slice(0, 10);

/** The digits and grouping of one of the family's four languages. A list, not a tag, so Intl falls back on its own (Sorani, `ckb`, is patchy on older Android WebViews and Arabic is the better last resort there). Handed the short code (`sharedLocales.lang`), never `.language`: "english" is a valid subtag Intl has no data for, and it silently resolves to the runtime default. The same table as the specification panel's `intlLocalesFor`. */
const NUMBER_LOCALES: Record<string, string[]> = { en: ['en'], ar: ['ar'], ku: ['ckb', 'ku', 'ar'], ru: ['ru'] };

const numberFormats = new Map<string, Intl.NumberFormat>();

/** A figure in the language's digits and grouping ("101,328"; fractions up to three places, so a quantity of 4.2 stays 4.2). No unit: the record carries none. */
export const formatNumber = (value: number, lang: string = 'en'): string => {
  let format = numberFormats.get(lang);
  if (!format) {
    try {
      format = new Intl.NumberFormat(NUMBER_LOCALES[lang] ?? NUMBER_LOCALES.en, { maximumFractionDigits: 3 });
    } catch {
      format = new Intl.NumberFormat('en', { maximumFractionDigits: 3 });
    }
    numberFormats.set(lang, format);
  }
  return format.format(value);
};

/**
 * The cell's value as text, or null when it is blank: null, undefined, '' or whitespace, and for a
 * `number` column anything that is not a finite number. **0 is a value**: a "no value" zero is
 * mapped to null upstream, never guessed here. A `date` is cut to its day (`dateText`); the figure
 * of a `number` is returned unformatted (`formatNumber` does the locale at render, so the blank
 * test does not depend on the language). An `action` column has no value, so its text is null.
 */
export const cellText = <Row,>(column: LookupColumn<Row>, row: Row): string | null => {
  const value = column.value?.(row);
  if (value === null || value === undefined) return null;
  const text = String(value);
  if (text.trim() === '') return null;
  if (column.kind === 'date') return dateText(text);
  if (column.kind === 'number') return Number.isFinite(Number(text)) ? String(Number(text)) : null;
  return text;
};

/**
 * The columns that have something to say on at least one row (run decision 12: an embedded drawer
 * table never shows a column that is empty on every line, such as a part description the service
 * never fills). Empty is `cellText` being null. The identity column and an `action` column are never
 * dropped: the identity names the row, and the action is not a value. Definition order is kept.
 */
export const nonEmptyColumns = <Row,>(columns: LookupColumn<Row>[], rows: Row[]): LookupColumn<Row>[] =>
  columns.filter(column => column.identity || column.kind === 'action' || rows.some(row => cellText(column, row) !== null));

/**
 * The element id of a row's drawer, from the row's identity, for the action button's
 * `aria-controls` and for the panel to put on the drawer. Stable (the same key gives the same id)
 * and valid whatever the key holds: every character outside letters, digits and "-" becomes
 * `_<hex>_`, so two different keys never collide.
 */
export const drawerId = (key: string): string => `lt-drawer-${key.replace(/[^A-Za-z0-9-]/g, character => `_${character.charCodeAt(0).toString(16)}_`)}`;

/** The id of a row's action button: the drawer's `aria-labelledby` points at it, so the region is named by what opens it. */
export const drawerButtonId = (key: string): string => `${drawerId(key)}-button`;

/**
 * The open drawer after a lookup's rows arrive (run decision 2): it stays open only if a row with
 * the same identity is among them, wherever it now sits, and otherwise slides shut. A lookup that
 * is still in flight is not asked: the drawer stays open, veiled with its row, until the answer.
 *
 * `nextRows` are the rows SHOWN after the lookup, not the whole answer: a paged panel goes back to
 * its first page on a lookup (run decision 8), so pass the first page (`pageRows(answer, 0, size)`).
 * An invoice that is in the answer but now sits on page 3 is off screen and must not stay "open".
 */
export const drawerAfterLookup = <Row,>(openKey: string | null, nextRows: Row[], keyOf: (row: Row, index: number) => string): string | null =>
  openKey !== null && nextRows.some((row, index) => keyOf(row, index) === openKey) ? openKey : null;

/**
 * The open drawer after the page window changes (run decision 9): it goes when its row is not on
 * the page that is shown, so paging away closes it and coming back shows it shut; a page size that
 * keeps the row on screen leaves it open. `pageKeys` are the keys (`keyOf`) of the rows of the page
 * that is about to show.
 */
export const drawerAfterPage = (openKey: string | null, pageKeys: readonly string[]): string | null => (openKey !== null && pageKeys.includes(openKey) ? openKey : null);

/**
 * The drawers to keep mounted while they slide shut after the open key moves from `previousKey` to
 * `nextKey`. `previousRows` are the rows the table showed while the previous drawer was open and
 * `nextRows` the rows it will show (the same list when only the key changed, a new one when a lookup
 * arrived). The previous drawer is kept when its key changed, or when it did (a lookup) keep its key but
 * moved to another slot, in which case the old slot's drawer shuts and the new slot's opens. A
 * drawer that is being opened again is dropped from the list, and a slot is listed once (the drawer
 * that closes last replaces an older entry at its slot). The owner clears the list a settle
 * after the change.
 */
export const closingDrawers = <Row,>({
  closing,
  previousKey,
  nextKey,
  previousRows,
  nextRows,
  keyOf,
}: {
  closing: ClosingDrawer<Row>[];
  previousKey: string | null;
  nextKey: string | null;
  previousRows: Row[];
  nextRows: Row[];
  keyOf: (row: Row, index: number) => string;
}): ClosingDrawer<Row>[] => {
  const kept = nextKey === null ? closing : closing.filter(entry => keyOf(entry.row, entry.index) !== nextKey);
  if (previousKey === null) return kept;

  const index = previousRows.findIndex((row, i) => keyOf(row, i) === previousKey);
  if (index < 0) return kept;

  const nextIndex = nextRows.findIndex((row, i) => keyOf(row, i) === previousKey);
  const stays = previousKey === nextKey && nextIndex === index;
  if (stays) return kept;

  // A slot lists one drawer: an entry still shutting there is the older row's, and the drawer that
  // closes now is the one the slot was showing, so it takes the entry over.
  return [...kept.filter(entry => entry.index !== index), { index, row: previousRows[index] }];
};

const INTERACTIVE = 'a[href], button, input, select, textarea, label, summary, [role="button"], [role="link"], [contenteditable=""], [contenteditable="true"]';

/**
 * Whether a click on a row's body should toggle its drawer: the row-wide convenience on top of the
 * action button (spec § 7), for a pointer. It does not when the click landed on something
 * interactive inside the row (a link, the media button, a field), or ends a text selection (a reader
 * copying an invoice number), or when `enabled` is false (the table is busy). Pure over what it is
 * given: the element that took the click, the row it is in, and the text currently selected.
 */
export const shouldToggleOnRowClick = ({
  target,
  row,
  selectedText,
  enabled,
}: {
  target: Element | null;
  row: Element | null;
  selectedText: string;
  enabled: boolean;
}): boolean => {
  if (!enabled || !target || !row) return false;
  if (selectedText.length > 0) return false;

  const interactive = target.closest(INTERACTIVE);
  return !(interactive && row.contains(interactive));
};

const selectedTextNow = (): string => (typeof window !== 'undefined' && typeof window.getSelection === 'function' ? (window.getSelection()?.toString() ?? '') : '');

/**
 * Brings a row to the top of the page and holds it there while its drawer slides open beneath it
 * (motion.md rule 10, the SSC's precedent). The table is a function with no host, so the owner runs
 * this: in `componentDidRender`, when the open key changes to a non-null key, call it with the panel's
 * shadow root, the key and how long the layout beneath takes to settle (`--settle` plus a margin),
 * keep the function it returns and call it when the drawer shuts or the panel leaves. The drawer is
 * found by `drawerId`; the anchor is its row. A key with no drawer in the tree does nothing.
 */
export const revealDrawer = (root: ParentNode, key: string, followMs: number): (() => void) => {
  const drawer = root.querySelector(`#${drawerId(key)}`);
  if (!drawer) return () => {};

  const anchor = drawer.closest('.lt-slot-body')?.querySelector('.lt-row') ?? drawer;
  return revealBelow(anchor, followMs);
};

/** The table roles exist only while there is a table to land in: the body is rows, open, and not reshaping (§ 8). */
export const tableRoles = (body: string, open: boolean, reshaping: boolean): boolean => body === 'rows' && open && !reshaping;

/**
 * The strip shows the headings whenever there is something to head: idle, in flight, and above rows
 * (they depend on the configuration alone, so the answer cannot change them). It shows a notice
 * only for an outcome that has no rows (authorized and empty, unauthorized, a failed lookup, a
 * panel's own body) and when every column is hidden, where there are no headings to show. § 20's
 * `panelLead`, generalised, with the skeleton leg removed.
 */
export const tableLead = ({ busy, idle, body, columns }: { busy: boolean; idle: boolean; body: string; columns: number }): LeadKind =>
  columns === 0 ? 'notice' : busy || idle || body === 'rows' ? 'columns' : 'notice';

/** What the body renders: it keeps its outgoing kind while it shuts, so the content slides away instead of blinking out. */
export const shownBody = (body: string, retained: string): string => (body === 'none' ? retained : body);

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

const TONE_GLYPH: Record<NoticeTone, keyof typeof BADGE_GLYPHS | null> = { positive: 'positive', negative: 'negative', neutral: 'question', attention: 'alert', idle: null };

const Glyph = ({ path, className }: { path: string; className: string }) => (
  <svg class={className} viewBox="0 0 512 512" aria-hidden="true" focusable="false">
    <path fill="currentColor" d={path} />
  </svg>
);

/**
 * A media cell's fetch state -- loading, then ready or failed -- is a fact about the browser's
 * request, not about the data, and a functional component has no state of its own. So it lives on
 * the DOM: the render always says `data-media="loading"` (and a disabled button, a hidden stand-in)
 * for a URL, and the image's own `load` / `error` events move the cell on.
 *
 * Why that survives a re-render: Stencil diffs each new vnode against the *previous vnode*, never
 * against the DOM, and a patch only writes an attribute whose vnode value changed. The vnode says
 * "loading" both times, so nothing writes over the "failed" the event set. A new picture is a new
 * element (the cell is keyed by its src) and so starts again at loading. Nothing is kept outside the
 * element, so a URL that failed once is tried again when its row is next rendered, rather than being
 * written off for the life of the page by a module-level cache.
 */
const settleMedia = (image: HTMLImageElement, state: 'ready' | 'failed') => {
  const cell = image.closest('.lt-cell');
  if (!cell || cell.getAttribute('data-media') !== 'loading') return;
  cell.setAttribute('data-media', state);

  const open = cell.querySelector('.lt-media-open');
  if (open) {
    if (state === 'ready') {
      open.removeAttribute('disabled');
      open.removeAttribute('aria-hidden');
    } else {
      // A dead button next to the "did not load" stand-in would announce the picture twice.
      open.setAttribute('disabled', '');
      open.setAttribute('aria-hidden', 'true');
    }
  }

  const stand = cell.querySelector('.lt-media-stand');
  if (stand) {
    if (state === 'failed') stand.removeAttribute('aria-hidden');
    else stand.setAttribute('aria-hidden', 'true');
  }
};

/**
 * An image that was already in the browser's cache can finish before the listener is attached; this
 * catches it. Stencil calls `ref` in createElm, before the element is appended, so the cell cannot
 * be found yet: the check waits one microtask, by which time the element is in its cell. A complete
 * picture with no width is a broken one; its `error` event settles it, so it is left at loading.
 */
const settleIfComplete = (image: HTMLImageElement | undefined) => {
  if (!image) return;
  queueMicrotask(() => {
    if (image.complete === true && image.naturalWidth > 0) settleMedia(image, 'ready');
  });
};

type MediaProps = {
  src: string | null;
  identity: string | null;
  strings: LookupTableStrings;
  onOpenMedia?: (image: HTMLImageElement, src: string) => void;
};

/**
 * The thumbnail box (§ 6.6). One structure in every state, so a state change is a fade inside it and
 * not a swap: the open button wrapping the picture, and a stand-in layer for the two states with no
 * picture to show. `none` (no URL on record) is derived from the data; `loading`, `ready` and
 * `failed` are settled on the DOM (see settleMedia).
 */
const MediaBox = ({ src, identity, strings, onOpenMedia }: MediaProps) => {
  if (src === null) {
    return (
      <span class="lt-media-box">
        <span class="lt-media-stand" role="img" aria-label={strings.noImage} title={strings.noImage}>
          <Glyph path={MEDIA_GLYPHS.picture} className="lt-media-glyph" />
        </span>
      </span>
    );
  }

  const openLabel = identity ? `${strings.openImage} ${identity}` : strings.openImage;

  return (
    <span class="lt-media-box">
      <button
        type="button"
        class="lt-media-open"
        aria-label={openLabel}
        aria-hidden="true"
        disabled
        onClick={event => {
          const image = (event.currentTarget as HTMLElement).querySelector('img');
          if (image) onOpenMedia?.(image, src);
        }}
      >
        {/* alt="": the button names the picture. Lazy, so a long list does not fetch every full-size file while its body is shut. */}
        <img
          class="lt-media-image"
          src={src}
          alt=""
          loading="lazy"
          decoding="async"
          ref={settleIfComplete}
          onLoad={event => settleMedia(event.currentTarget as HTMLImageElement, 'ready')}
          onError={event => settleMedia(event.currentTarget as HTMLImageElement, 'failed')}
        />
      </button>
      <span class="lt-media-stand" role="img" aria-label={strings.imageFailed} title={strings.imageFailed} aria-hidden="true">
        <Glyph path={MEDIA_GLYPHS.pictureOff} className="lt-media-glyph" />
      </span>
    </span>
  );
};

/**
 * `translate="no"` on the cell, as well as on the panel's host, so a host page that re-enables
 * translation still leaves a part number alone. Through a ref, not a JSX attribute: `translate` is
 * not in Stencil's HTMLAttributes, and on a plain element Stencil would set it as a property
 * (`el.translate = 'no'`, a boolean, which reads as true) and never write the attribute.
 */
const keepUntranslated = (cell: HTMLElement | undefined) => cell?.setAttribute('translate', 'no');

/** One row's action button, as the cell needs it: the row's identity key and its state. Null where the table has no actions (or is embedded). */
type RowAction = { key: string; expanded: boolean; fetching: boolean; disabled: boolean; onToggle: (key: string) => void };

/** The chevron of the expand button. Stroked on a 24 box like the pager's, so the two read as one family. */
const ChevronDown = ({ className }: { className: string }) => (
  <svg class={className} viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true" focusable="false">
    <path d="m6 9 6 6 6-6" />
  </svg>
);

/**
 * The expand button (spec § 7, the SSC's trace button recipe): a real button with `aria-expanded`
 * and `aria-controls` pointing at the row's drawer, named from the strings and the row's identity.
 * The chevron turns with the state; while the row's details are being fetched a spinner takes the
 * chevron's place (`data-loading`, `aria-busy`) even if a drawer has nothing to fetch and never sets
 * it. Both glyphs live in the button always, so a state change is a cross-fade and not a swap.
 */
const ActionButton = ({ action, identity, strings, heading }: { action: RowAction; identity: string | null; strings: LookupTableStrings; heading: string }) => {
  const name = (action.expanded ? strings.collapseRow : strings.expandRow) ?? heading;
  const label = identity ? `${name} ${identity}` : name;

  return (
    <button
      type="button"
      id={drawerButtonId(action.key)}
      class="lt-action-button"
      aria-expanded={action.expanded ? 'true' : 'false'}
      aria-controls={drawerId(action.key)}
      aria-busy={action.fetching ? 'true' : null}
      aria-label={label}
      title={label}
      data-loading={action.fetching ? 'true' : 'false'}
      disabled={action.disabled}
      onClick={() => action.onToggle(action.key)}
    >
      <ChevronDown className="lt-action-icon" />
      <span class="lt-action-spinner" aria-hidden="true" />
    </button>
  );
};

const Cell = <Row,>({
  column,
  row,
  roles,
  identity,
  strings,
  onOpenMedia,
  lang,
  action,
  skeleton = true,
}: { column: LookupColumn<Row>; row: Row; roles: boolean; identity: string | null; lang?: string; action: RowAction | null; skeleton?: boolean } & Pick<
  LookupTableProps<Row>,
  'strings' | 'onOpenMedia'
>) => {
  const text = cellText(column, row);
  const media = column.kind === 'media';
  // A media cell is keyed by its picture, so a new picture is a new element starting at `loading`.
  const key = media ? `${column.field}:${text ?? ''}` : column.field;

  return (
    <div
      key={key}
      class={`lt-cell lt-${column.kind}`}
      role={roles ? 'cell' : null}
      data-kind={column.kind}
      data-label={column.label}
      data-identity={column.identity ? 'true' : null}
      data-grow={growing(column) ? 'true' : null}
      data-media={media ? (text === null ? 'none' : 'loading') : null}
      ref={column.kind === 'code' ? keepUntranslated : undefined}
    >
      {/*
        The value, and only the value, in its own block: the skeleton covers this (shift-skeleton, the
        family's in-place cover) while the row's next value is fetched, and fades it while a page
        changes. The cell, its caption and its padding are the row's place and stay. The growing
        column may be clamped by the panel, so it carries its whole text as a title.
      */}
      {/* An embedded table's values carry no skeleton of their own (`skeleton={false}`): it sits in a drawer, and the drawer's content is the one block the cover goes over. */}
      {/* The action button is chrome, not a value: it is never under the skeleton (a cover over a round button is a grey box), but it fades with the row's values when a page changes. */}
      <span class={{ 'lt-value': true, 'shift-skeleton': skeleton && column.kind !== 'action' }} title={growing(column) && text !== null ? text : null}>
        {column.kind === 'action' ? (
          action && <ActionButton action={action} identity={identity} strings={strings} heading={column.label} />
        ) : media ? (
          <MediaBox src={text} identity={identity} strings={strings} onOpenMedia={onOpenMedia} />
        ) : text === null ? (
          <span class="lt-none">—</span>
        ) : column.kind === 'number' ? (
          formatNumber(Number(text), lang)
        ) : (
          text
        )}
      </span>
    </div>
  );
};

/** Row keys must be unique or the vdom moves the wrong element; a host key that repeats is suffixed rather than trusted. */
const uniqueKey = (seen: Set<string>, key: string): string => {
  let candidate = key;
  for (let n = 2; seen.has(candidate); n++) candidate = `${key}~${n}`;
  seen.add(candidate);
  return candidate;
};

/** A heading cell. An action column's heading is for assistive tech only (a glyph-only column, § 24). */
const Heading = <Row,>({ column, roles }: { column: LookupColumn<Row>; roles: boolean }) => (
  <span
    key={column.field}
    class="lt-cell"
    role={roles ? 'columnheader' : null}
    data-kind={column.kind}
    data-identity={column.identity ? 'true' : null}
    data-grow={growing(column) ? 'true' : null}
  >
    {column.kind === 'action' ? <span class="sr-only">{column.label}</span> : column.label}
  </span>
);

/**
 * The embedded table (spec § 7): headings as the first row, rows under them, nothing else. No strip
 * (a drawer has none to hang headings on), no notice, no collapsible body and no slots: the drawer
 * it sits in is the one region that slides, and its rows stay put while it does. A column empty on
 * every row is dropped (run decision 12), and `rows: []` is an empty grid, never a table with only
 * its headings.
 */
const EmbeddedTable = <Row,>(props: LookupTableProps<Row>): VNode => {
  const { rows, rowKey, busy, strings, onOpenMedia, lang, density, label } = props;

  const columns = nonEmptyColumns(props.columns, rows);
  const tracks = tableTracks(columns);
  const roles = rows.length > 0 && columns.length > 0;
  const identityColumn = columns.find(column => column.identity);
  const seen = new Set<string>();

  return (
    <div
      class="lt-grid"
      role={roles ? 'table' : null}
      aria-label={roles ? (label ?? null) : null}
      aria-busy={roles && busy ? 'true' : null}
      data-phase={busy ? 'busy' : 'settled'}
      data-stack="false"
      data-embedded="true"
      data-density={density === 'compact' ? 'compact' : null}
      style={{ '--lt-tracks': tracks.wide }}
    >
      {roles && (
        <div class="lt-columns" role="row">
          {columns.map(column => (
            <Heading column={column} roles={true} />
          ))}
        </div>
      )}
      {roles && (
        <div class="lt-rows" role="rowgroup">
          {rows.map((row, index) => {
            const identity = identityColumn ? cellText(identityColumn, row) : null;

            return (
              <div key={uniqueKey(seen, rowKey(row, index))} class="lt-row" role="row">
                {columns.map(column => (
                  <Cell column={column} row={row} roles={true} identity={identity} strings={strings} onOpenMedia={onOpenMedia} lang={lang} action={null} skeleton={false} />
                ))}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export function LookupTable<Row>(props: LookupTableProps<Row>, children?: VNode[]): VNode {
  if (props.embedded) return EmbeddedTable(props);

  const { columns, rows, rowKey, lead, notice, busy, body, open, reshaping = false, strings, onOpenMedia, slots, lang, density, label, actions, drawer } = props;

  // A row's identity for the action column and the drawer: the panel's `keyOf`, else the key the vdom uses.
  const keyOfRow = (row: Row, index: number, fallback?: string): string => (actions?.keyOf ? actions.keyOf(row, index) : (fallback ?? rowKey(row, index)));

  const slotOf = (index: number): RowSlot => slots?.[index] ?? { open: true, veil: 'none' };

  // `rows: []` is not a table: no row, so no role for a reader to land on (§ 10). A row whose slot is
  // shut (arriving, or leaving) is not one either.
  const roles = tableRoles(body, open, reshaping) && rows.some((_, index) => slotOf(index).open);
  const tracks = tableTracks(columns);
  const identityColumn = columns.find(column => column.identity);
  const hasActionColumn = columns.some(column => column.kind === 'action');
  const seen = new Set<string>();

  // `keyOf` must be unique per row (see RowActions); a repeat is the owner's bug, so say so once per pass.
  if (actions?.keyOf && body === 'rows') {
    const keys = new Set<string>();
    const repeated = new Set<string>();
    rows.forEach((row, index) => {
      const key = actions.keyOf(row, index);
      if (keys.has(key)) repeated.add(key);
      keys.add(key);
    });
    if (repeated.size > 0)
      console.warn(
        `LookupTable: actions.keyOf returned the same key for more than one row (${[...repeated].map(key => `"${key}"`).join(', ')}). It must be unique per row: compose it (e.g. company + branch + invoice).`,
      );
  }

  const style: Record<string, string> = { '--lt-tracks': tracks.wide };
  if (tracks.stacked) style['--lt-tracks-stacked'] = tracks.stacked;

  const glyph = notice && TONE_GLYPH[notice.tone];

  return (
    <div
      class="lt-grid"
      role={roles ? 'table' : null}
      aria-label={roles ? (label ?? null) : null}
      aria-busy={roles && busy ? 'true' : null}
      data-phase={busy ? 'busy' : 'settled'}
      data-stack={tracks.stacked ? 'true' : 'false'}
      data-density={density === 'compact' ? 'compact' : null}
      style={style}
    >
      <div class="lt-lead layer-stack" data-lead={lead}>
        <div
          class={{ 'layer': true, 'lt-notice': true, [`is-${notice?.tone ?? 'idle'}`]: true }}
          data-active={lead === 'notice' ? 'true' : 'false'}
          role="status"
          aria-hidden={lead === 'notice' ? null : 'true'}
        >
          {glyph && <Glyph path={BADGE_GLYPHS[glyph]} className="notice-icon" />}
          <span>{notice?.text ?? ''}</span>
        </div>

        {/*
          Accessibility of the headings, decided here (F1). They are on screen while idle and in flight,
          but there is no table then, so the roles follow the rule that already governs the rest: only
          with open rows (`roles`). Without them the layer is aria-hidden too: a bare "Part number
          Description Image" with no table around it names a structure that is not there, and the
          outcome is announced by the notice's role="status", not by headings. The headings are for the eye; a reader meets them as a table when the rows open.
        */}
        <div
          class="layer lt-columns"
          data-active={lead === 'columns' ? 'true' : 'false'}
          data-reshaping={reshaping ? 'true' : 'false'}
          role={roles ? 'row' : null}
          aria-hidden={lead === 'columns' && roles ? null : 'true'}
        >
          {columns.map(column => (
            <Heading column={column} roles={roles} />
          ))}
          {/* Narrow panels drop the headings (each card captions its own fields) and keep one word on the strip. */}
          <span class="lt-columns-label" aria-hidden="true">
            {strings.columnsLabel}
          </span>
        </div>
      </div>

      <div class="lt-body collapsible" data-open={open ? 'true' : 'false'} data-body={body} role={roles ? 'rowgroup' : null} aria-hidden={open ? null : 'true'}>
        <div class="collapsible-body lt-body-inner">
          {body === 'rows' ? (
            <div class="lt-rows">
              {rows.map((row, index) => {
                const identity = identityColumn ? cellText(identityColumn, row) : null;
                const slot = slotOf(index);
                const rowRoles = roles && slot.open;
                const vdomKey = uniqueKey(seen, rowKey(row, index));
                const actionKey = actions ? keyOfRow(row, index, vdomKey) : null;
                const expanded = actionKey !== null && actions.expandedKey === actionKey;
                const action: RowAction | null =
                  actions && actionKey !== null ? { key: actionKey, expanded, fetching: actions.fetchingKey === actionKey, disabled: busy, onToggle: actions.onToggle } : null;

                // The drawer, if the panel has one: the open row's, or the content of one that is sliding shut.
                const drawerKey = drawer && actionKey !== null ? actionKey : null;
                const closingHere = drawerKey !== null ? actions.closing?.find(entry => entry.index === index) : undefined;
                const drawerContent =
                  drawerKey === null ? null : expanded ? drawer(row, drawerKey) : closingHere ? drawer(closingHere.row, keyOfRow(closingHere.row, index)) : null;
                const toggleRow =
                  drawerKey === null
                    ? undefined
                    : (event: MouseEvent) => {
                        if (
                          shouldToggleOnRowClick({
                            target: event.target as Element | null,
                            row: event.currentTarget as Element | null,
                            selectedText: selectedTextNow(),
                            enabled: !busy,
                          })
                        )
                          actions.onToggle(drawerKey);
                      };

                // Each row is its own slot: a .collapsible, so a row that comes or goes slides on its own
                // while the rest of the list stays where it is. `cover` puts the row under the family's
                // skeleton (`.loading` is the shift-skeleton switch), `fade` fades its values. The drawer
                // belongs to the slot, so it is veiled, faded, shut and removed with its row.
                return (
                  <div key={vdomKey} class="lt-slot collapsible" data-open={slot.open ? 'true' : 'false'} data-veil={slot.veil} aria-hidden={slot.open ? null : 'true'}>
                    <div class="collapsible-body lt-slot-body">
                      <div
                        class={{ 'lt-row': true, 'loading': slot.veil === 'cover' }}
                        role={rowRoles ? 'row' : null}
                        data-expanded={expanded ? 'true' : null}
                        data-clickable={toggleRow && !busy ? 'true' : null}
                        onClick={toggleRow}
                      >
                        {columns.map(column => (
                          <Cell column={column} row={row} roles={rowRoles} identity={identity} strings={strings} onOpenMedia={onOpenMedia} lang={lang} action={action} />
                        ))}
                      </div>
                      {drawerKey !== null && (
                        <div
                          class={{ 'lt-drawer-row': true, 'loading': slot.veil === 'cover' && drawerContent !== null }}
                          role={rowRoles ? 'row' : null}
                          data-open={expanded ? 'true' : 'false'}
                          aria-hidden={expanded ? null : 'true'}
                          {...(expanded ? {} : { inert: true })}
                        >
                          <div class="lt-drawer-cell" role={rowRoles ? 'cell' : null} aria-colspan={rowRoles ? String(columns.length) : null}>
                            <div
                              class="lt-drawer collapsible"
                              id={drawerId(drawerKey)}
                              role="region"
                              aria-labelledby={hasActionColumn ? drawerButtonId(drawerKey) : null}
                              aria-label={hasActionColumn ? null : identity}
                              data-open={expanded ? 'true' : 'false'}
                            >
                              <div class="collapsible-body lt-drawer-body">
                                <div class="lt-drawer-panel">
                                  {/* The one block the cover goes over, and the fade of a page change: the panel's rim and rail are chrome and stay. */}
                                  <div class="lt-drawer-content shift-skeleton">{drawerContent}</div>
                                </div>
                              </div>
                            </div>
                          </div>
                        </div>
                      )}
                    </div>
                  </div>
                );
              })}
            </div>
          ) : (
            children
          )}
        </div>
      </div>
    </div>
  );
}
