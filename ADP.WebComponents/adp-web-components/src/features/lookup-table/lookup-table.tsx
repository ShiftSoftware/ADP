import { FunctionalComponent, VNode, h } from '@stencil/core';

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
 *
 * DRAWER MODE (opt-in, `drawer` prop; a table that does not pass it renders exactly what it always
 * did). A row can carry a drawer: a row-spanning `.collapsible` under it, holding content the
 * consumer renders. At most one is open. The table owns the toggle (the `action` cell's button),
 * the motion (the shared `.collapsible` primitive), the ARIA wiring and the card layout; the OWNER
 * holds the state, as it holds `retainedBody`, and hands it down:
 *
 *   drawer={{
 *     has: (row, i) => boolean,            // does this row have drawer content? no -> no button
 *     render: (row, i) => VNode | VNode[], // the content; called ONLY for a drawer in `shown`
 *     label: (row, i, expanded) => string, // the button's aria-label, localised, e.g. "Show 3 photos"
 *     regionLabel: (row, i) => string,     // the drawer region's aria-label, localised
 *     open: number | null,                 // the slot index that is open (single open)
 *     shown: readonly number[],            // `open` plus any drawer still shutting (shownDrawers)
 *     onToggle: (i) => void,               // the owner: open = toggleDrawer(open, i), retainDrawers(...)
 *     idPrefix: string,                    // unique in the shadow root; drawerId(idPrefix, i) is the region's id
 *   }}
 *   columns: [{ field, label, kind: 'action', value: row => '3 photos', glyph?: path512 }, ...]
 *
 * The open drawer is a slot index, because rows are slots keyed by position (§ 21a). Busy does not
 * change it: an open drawer stays open into a lookup, under the skeleton with the rest of its row
 * (the row's veil is the drawer's), and at the answer the owner runs `settleDrawer`, which keeps it
 * if its slot still has drawer content and shuts it if not. A page change is the same with the
 * `fade` veil. A clear shuts it at once.
 *
 * A closed drawer costs nothing: `render` is not called for it, it mounts no content and its content block
 * carries no skeleton (no cover, no pseudo-elements, no transition under `.loading`), so a
 * consumer's photos are not even requested until the reader opens one. A consumer that puts
 * images in a drawer should still use `loading="lazy"` and `decoding="async"` on them.
 */

// ---------------------------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------------------------

/**
 * Built kinds. 'number' | 'date' | 'chip' are specified and deferred to a named consumer.
 * 'action' is the drawer's toggle: its `value(row)` is the button's visible text.
 */
export type CellKind = 'text' | 'code' | 'media' | 'action';

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
  /** The cell's value; for 'media' the image URL. null, undefined, '' or whitespace is blank. */
  value: (row: Row) => string | number | null | undefined;
  /** The row's name: Code type, uncaptioned on a card, first in the stacked layout. At most one. */
  identity?: boolean;
  /** The one column that absorbs the slack (§ 18). At most one, and only on a 'text' column. */
  grow?: boolean;
  /** 'action' only: the glyph on the toggle, as 512 x 512 path data (like `BADGE_GLYPHS`). Without one the toggle is a chevron. */
  glyph?: string;
  /**
   * 'text' only: a small grey chip after the value, naming what the value is (a lone reading that
   * belongs to no side: "Centre"). Null or blank draws none, and a blank value draws no chip. Inside
   * the value's own block, so the skeleton covers it with the value.
   */
  note?: (row: Row) => string | null | undefined;
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
};

/**
 * What the body says in words when there is no list to show (owner, 2026-10-27: it overrides design
 * language § 17 "a notice that only repeats the pill is not shown" for table panels). Before a lookup the
 * reader needs to know where a list will appear; after one, an empty region next to a VIN reads as
 * broken, so each outcome without rows is a large muted icon and one line of text, in the colour of its
 * role (design language § 10).
 *
 * - `idle`: before any lookup. Asserts nothing about a vehicle. Grey.
 * - `empty`: an authorized vehicle with nothing on file. "No records", never a verdict. Grey.
 * - `error`: the lookup failed. Red, the role of a failed lookup (the pill and the accent bar are negative for it too).
 * - `unauthorized`: the vehicle is not in the distributor's records, which is not "none". Amber.
 */
export type PlaceholderKind = 'idle' | 'empty' | 'error' | 'unauthorized';

/** An icon component from `~assets` (`class` is the only prop it needs). */
export type PlaceholderIcon = FunctionalComponent<{ class?: string }>;

/** One outcome's icon (an `~assets` component, or 512 x 512 path data in `glyph`) and its single line of text, already localised. */
export type PlaceholderContent = { icon?: PlaceholderIcon; glyph?: string; text: string };

export type TablePlaceholder = {
  /** The outcome to say, or null when the body holds rows (or there is nothing to say): from `placeholderKind`. Non-null opens the region; null slides it shut. */
  kind: PlaceholderKind | null;
  /**
   * The kind the region last showed. The owner keeps it a settle after `kind` went null, so the words
   * slide away with their region instead of blinking out (motion.md rule 5), and drops it once the region
   * has shut. Omitted, a shutting region fades its words out with it.
   */
  retained?: PlaceholderKind | null;
  /** The icon and the line of every outcome the panel can be in; the four layers cross-fade in place. */
  content: Record<PlaceholderKind, PlaceholderContent>;
};

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
   * One entry per row in `rows`: whether the row's slot is open and how its values are veiled.
   * Omitted, every row is open and unveiled. A panel that keeps its rows on screen through a lookup
   * (owner, 2026-10-25) passes `tableSlots(...)` and keys its rows by slot, so a row is a place that
   * stays while its values change under a cover, and only the rows that come or go slide.
   */
  slots?: RowSlot[];
  /**
   * The body's words for an outcome without rows (see `TablePlaceholder`): a region under the body that
   * slides open for `kind` and shut for null, one layer per outcome, cross-fading. Always mounted for a
   * panel that has one. Its icon and words are each a `shift-skeleton`, covered only under a `.loading`
   * ancestor: the owner's wrapper carries `.loading` while a lookup is in flight. `busy` sets
   * `aria-busy` on the region (a reader waits for the settled layer), not the cover. Omit it and the
   * table is exactly as before.
   */
  placeholder?: TablePlaceholder;
  /** Drawer mode. Omitted, no row has a drawer and no DOM of it exists. */
  drawer?: RowDrawer<Row>;
};

/** What the owner hands the table for drawer mode. See the block at the top of the file. */
export type RowDrawer<Row> = {
  /** Does this row have drawer content? A row without it renders no toggle: its action cell is plain text, or `—`. */
  has: (row: Row, index: number) => boolean;
  /**
   * The drawer's content. Called only for an index in `shown`, never for a closed drawer. While a
   * drawer shuts because its slot no longer has content, the owner returns the content it last
   * showed so it slides away instead of blinking out.
   */
  render: (row: Row, index: number) => VNode | VNode[] | null;
  /** The toggle's accessible name, already localised; it should contain the button's visible text. `expanded` is its current state, so it can say show or hide. */
  label: (row: Row, index: number, expanded: boolean) => string;
  /** The drawer region's accessible name, already localised. */
  regionLabel: (row: Row, index: number) => string;
  /** The open slot, or null. Single open. */
  open: number | null;
  /** The slots whose content is still rendered: the open one, plus any that are shutting. */
  shown: readonly number[];
  onToggle: (index: number) => void;
  /** Unique within the shadow root; the region's id is `drawerId(idPrefix, index)`. */
  idPrefix: string;
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

/** Single open: a click on the open row's toggle shuts it, a click on another row's opens that one (and shuts the first). */
export const toggleDrawer = (open: number | null, index: number): number | null => (open === index ? null : index);

/**
 * Which slot is open once an answer has landed (or the page has swapped, while the values are out).
 * `hasAt` says, per slot of the NEW answer, whether the row there has drawer content; a slot past
 * its end is a row that went. An open drawer stays open if its slot still has content (a lookup does
 * not shut it: owner, 2026-10-27), and shuts if not. A clear shuts it at once. Busy does not enter
 * into it: while a lookup is in flight the owner leaves `open` alone.
 */
export const settleDrawer = ({ open, hasAt, cleared = false }: { open: number | null; hasAt: readonly boolean[]; cleared?: boolean }): number | null =>
  cleared || open === null ? null : hasAt[open] === true ? open : null;

/**
 * The drawers still rendered after `open` changed from `previous` to `next`: the one that just shut
 * (or is shutting because another opened) keeps its content while it slides away, and the one that
 * is opening needs no keeping. The owner drops `retained` a settle later. Mirrors `shownBody`.
 */
export const retainDrawers = (retained: readonly number[], previous: number | null, next: number | null): number[] => {
  const kept = retained.filter(index => index !== next);
  return previous !== null && previous !== next && !kept.includes(previous) ? [...kept, previous] : kept;
};

/** What `RowDrawer.shown` is: the open drawer and the ones still shutting. */
export const shownDrawers = (open: number | null, retained: readonly number[]): number[] => (open === null ? [...retained] : [...retained.filter(index => index !== open), open]);

/** The id of the drawer region at `index`; the toggle's `aria-controls` names it. */
export const drawerId = (prefix: string, index: number): string => `${prefix}-drawer-${index}`;

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
/** The drawer's toggle: a fixed share like the rest (glyph 15 + gap 6 + "12 photos" ~56 + gap 6 + chevron 12 + button padding 17 = ~112, plus the cell's 12+12 = ~136, so the 150 floor leaves headroom), so it never moves with the data. */
const ACTION = 'minmax(150px, 0.9fr)';
/** The stacked layout's first column (identity above its description): the slack, with a floor. */
const STACK_FIRST = 'minmax(160px, 1fr)';
/** No visible column at all: one track, so the strip's notice still spans the card. */
const SINGLE = 'minmax(0, 1fr)';

const growing = <Row,>(column: LookupColumn<Row>): boolean => !!column.grow && column.kind === 'text';

const track = <Row,>(column: LookupColumn<Row>): string =>
  growing(column) ? GROW : column.kind === 'code' ? CODE : column.kind === 'media' ? MEDIA : column.kind === 'action' ? ACTION : TEXT;

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

/** null for null, undefined, '' or whitespace, else `String(value)`. **0 is a value**: a "no value" zero is mapped to null upstream, never guessed here. */
export const cellText = <Row,>(column: LookupColumn<Row>, row: Row): string | null => {
  const value = column.value(row);
  if (value === null || value === undefined) return null;
  const text = String(value);
  return text.trim() === '' ? null : text;
};

/** The table roles exist only while there is a table to land in: the body is rows, open, and not reshaping (§ 8). */
export const tableRoles = (body: string, open: boolean, reshaping: boolean): boolean => body === 'rows' && open && !reshaping;

/**
 * The strip shows the headings whenever there is something to head: idle, in flight, and above rows
 * (they depend on the configuration alone, so the answer cannot change them). It shows a notice
 * only for an outcome that has no rows to head (authorized and empty, unauthorized, a failed lookup,
 * a panel's own body) and when every column is hidden, where there are no headings to show. A panel
 * whose body says that outcome in words (`placeholder: true`, see `placeholderKind`) keeps its
 * headings for it, so the strip and the body never say the same thing twice (owner, 2026-10-27).
 * § 20's `panelLead`, generalised, with the skeleton leg removed.
 */
export const tableLead = ({ busy, idle, body, columns, placeholder = false }: { busy: boolean; idle: boolean; body: string; columns: number; placeholder?: boolean }): LeadKind =>
  columns === 0 ? 'notice' : busy || idle || placeholder || body === 'rows' ? 'columns' : 'notice';

/** What the body renders: it keeps its outgoing kind while it shuts, so the content slides away instead of blinking out. */
export const shownBody = (body: string, retained: string): string => (body === 'none' ? retained : body);

/**
 * What the body says when there is no list, from the panel's state. Pure: no timers, no memory.
 *
 * - No column to show (a host hid every one): null. The strip carries the sentence (`tableLead` answers `notice`).
 * - A failed lookup: `error`, whatever else the state holds.
 * - No vehicle on screen: `idle`.
 * - `authorized === false`: `unauthorized`, whatever the DTO's list holds. Not-in-records is never "empty".
 * - An authorized vehicle with no records on file: `empty`. A statement about the records, not a verdict.
 * - Rows to show: null, except while the panel is `leaving` (a clear, or a failure on a settled card):
 *   the rows are shutting, and the region opens on the idle words as they go, under the cover.
 *
 * During a lookup the inputs are still the last answer's, so the placeholder in place stays in place,
 * under the skeleton, and cross-fades when the answer arrives.
 */
export const placeholderKind = ({
  columns,
  loaded,
  failed,
  authorized,
  records,
  leaving = false,
}: {
  /** How many columns are visible (`visibleColumns(...).length`). */
  columns: number;
  /** A vehicle's answer is on screen: a lookup landed and did not fail. */
  loaded: boolean;
  /** The last lookup failed. */
  failed: boolean;
  /** The vehicle's `isAuthorized`; `false` is not in the records. */
  authorized?: boolean;
  /** How many records the answer holds, judged on the whole list (never on the rows a page shows). */
  records: number;
  /** The panel is on its way to idle or to an error: its rows are shutting. */
  leaving?: boolean;
}): PlaceholderKind | null => {
  if (columns === 0) return null;
  if (failed) return 'error';
  if (!loaded) return 'idle';
  if (authorized === false) return 'unauthorized';
  if (records === 0) return 'empty';
  return leaving ? 'idle' : null;
};

/** The kind the region renders: the current one, else the one it last showed while it slides shut. */
export const shownPlaceholder = (kind: PlaceholderKind | null, retained?: PlaceholderKind | null): PlaceholderKind | null => kind ?? retained ?? null;

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

/** The toggle's chevron, 512 x 512 like the family's glyphs. It rotates half a turn on open (CSS), so one path says both show and hide. */
const CHEVRON = 'M256 368 96 208l32-32 128 128 128-128 32 32z';

/** What a row's action cell needs from the drawer: null when the table has no drawer mode. */
type ActionState = { has: boolean; expanded: boolean; controls: string; label: string; covered: boolean; onToggle: () => void };

/**
 * The `action` cell's value. A row with drawer content gets the toggle: a real `<button type="button">`,
 * so the keyboard (Enter, Space) and the focus ring are the browser's. With text it is the pill
 * (glyph, the text, a chevron); with an empty value it is the 30px round icon-only button. A row
 * WITHOUT drawer content gets no button at all -- a dead control is a lie -- only its text, muted,
 * or the blank marker. Another module (a spinner, `aria-busy`, for a drawer whose content is
 * fetched on open) is deliberately not built: no consumer in this run has one.
 */
const ActionValue = ({ text, glyph, action }: { text: string | null; glyph?: string; action: ActionState | null }) => {
  if (!action || !action.has) return text === null ? <span class="lt-none">—</span> : <span class="lt-action-plain">{text}</span>;

  return (
    <button
      type="button"
      class="lt-action-button"
      data-shape={text === null ? 'round' : 'pill'}
      aria-expanded={action.expanded ? 'true' : 'false'}
      aria-controls={action.controls}
      aria-label={action.label}
      inert={action.covered}
      onClick={action.onToggle}
    >
      {glyph && <Glyph path={glyph} className="lt-action-glyph" />}
      {text !== null && <span class="lt-action-text">{text}</span>}
      {/* The chevron is the only mark on a round button with no glyph; beside a glyph and a text it is the "this opens" cue. */}
      {(text !== null || !glyph) && <Glyph path={CHEVRON} className="lt-action-chevron" />}
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
  action,
}: { column: LookupColumn<Row>; row: Row; roles: boolean; identity: string | null; action: ActionState | null } & Pick<LookupTableProps<Row>, 'strings' | 'onOpenMedia'>) => {
  const text = cellText(column, row);
  const media = column.kind === 'media';
  const actionKind = column.kind === 'action';
  const note = text !== null ? column.note?.(row)?.trim() : null;
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
      data-shape={actionKind ? (action?.has ? (text === null ? 'round' : 'pill') : 'plain') : null}
      ref={column.kind === 'code' ? keepUntranslated : undefined}
    >
      {/*
        The value, and only the value, in its own block: the skeleton covers this (shift-skeleton, the
        family's in-place cover) while the row's next value is fetched, and fades it while a page
        changes. The cell, its caption and its padding are the row's place and stay. The growing
        column may be clamped by the panel, so it carries its whole text as a title.
      */}
      <span class="lt-value shift-skeleton" title={growing(column) && text !== null ? text : null}>
        {actionKind ? (
          <ActionValue text={text} glyph={column.glyph} action={action} />
        ) : media ? (
          <MediaBox src={text} identity={identity} strings={strings} onOpenMedia={onOpenMedia} />
        ) : text === null ? (
          <span class="lt-none">—</span>
        ) : note ? (
          [text, <span class="lt-note">{note}</span>]
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

const PLACEHOLDER_KINDS: PlaceholderKind[] = ['idle', 'empty', 'error', 'unauthorized'];

/**
 * The body's words for an outcome without rows: one region that slides open and shut (a `.collapsible`,
 * on the clock of the body it replaces), holding one layer per outcome that cross-fade in place (a
 * `.layer-stack`, so the region is as tall as its tallest line and nothing below it moves).
 *
 * Announced once per outcome, on the pattern the strip's notice uses: every layer is a `role="status"`
 * and only the active one is exposed (`aria-hidden` on the rest and on a shut region). Icon and words
 * are each their own `shift-skeleton`, so a `.loading` ancestor covers them in flight: the last outcome
 * stays where it stands without being read as the next one's. `busy` makes the region `aria-busy`.
 */
const Placeholder = ({ placeholder, busy }: { placeholder: TablePlaceholder; busy: boolean }) => {
  const shown = shownPlaceholder(placeholder.kind, placeholder.retained);
  const open = placeholder.kind !== null;

  return (
    <div class="lt-placeholder collapsible" data-open={open ? 'true' : 'false'} data-kind={shown ?? 'none'} aria-hidden={open ? null : 'true'} aria-busy={busy ? 'true' : null}>
      <div class="collapsible-body">
        <div class="lt-placeholder-stack layer-stack">
          {PLACEHOLDER_KINDS.map(kind => {
            const active = shown === kind;
            const { icon: Icon, glyph, text } = placeholder.content[kind];
            return (
              <div
                class={{ 'layer': true, 'lt-placeholder-layer': true, [`is-${kind}`]: true }}
                data-kind={kind}
                data-active={active ? 'true' : 'false'}
                role="status"
                aria-hidden={active ? null : 'true'}
              >
                <span class="lt-placeholder-mark shift-skeleton">
                  {Icon ? <Icon class="lt-placeholder-glyph" /> : glyph ? <Glyph path={glyph} className="lt-placeholder-glyph" /> : null}
                </span>
                <p class="lt-placeholder-text shift-skeleton">{text}</p>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export function LookupTable<Row>(props: LookupTableProps<Row>, children?: VNode[]): VNode {
  const { columns, rows, rowKey, lead, notice, busy, body, open, reshaping = false, strings, onOpenMedia, slots, drawer, placeholder } = props;

  const slotOf = (index: number): RowSlot => slots?.[index] ?? { open: true, veil: 'none' };

  // `rows: []` is not a table: no row, so no role for a reader to land on (§ 10). A row whose slot is
  // shut (arriving, or leaving) is not one either.
  const roles = tableRoles(body, open, reshaping) && rows.some((_, index) => slotOf(index).open);
  const tracks = tableTracks(columns);
  const identityColumn = columns.find(column => column.identity);
  const seen = new Set<string>();

  const style: Record<string, string> = { '--lt-tracks': tracks.wide };
  if (tracks.stacked) style['--lt-tracks-stacked'] = tracks.stacked;

  const glyph = notice && TONE_GLYPH[notice.tone];

  return (
    <div
      class="lt-grid"
      role={roles ? 'table' : null}
      aria-busy={roles && busy ? 'true' : null}
      data-phase={busy ? 'busy' : 'settled'}
      data-stack={tracks.stacked ? 'true' : 'false'}
      data-placeholder={placeholder ? 'true' : null}
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
            <span
              key={column.field}
              class="lt-cell"
              role={roles ? 'columnheader' : null}
              data-kind={column.kind}
              data-identity={column.identity ? 'true' : null}
              data-grow={growing(column) ? 'true' : null}
            >
              {column.label}
            </span>
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

                // Drawer mode only. `drawerOpen` needs the row to still have content: if an answer took it
                // away, the drawer shuts on this very render, before the owner's settle has run.
                const drawerHas = drawer ? drawer.has(row, index) : false;
                const drawerOpen = drawerHas && drawer?.open === index;
                const id = drawer ? drawerId(drawer.idPrefix, index) : '';
                // A shut drawer that is not on its way out has no content, so it carries no skeleton and is not made inert:
                // a cover, its two pseudo-elements and their transitions on every shut shell are work with nothing to cover.
                const drawerShown = drawer ? drawer.shown.includes(index) : false;
                const action: ActionState | null = drawer
                  ? {
                      has: drawerHas,
                      expanded: drawerOpen,
                      controls: id,
                      label: drawerHas ? drawer.label(row, index, drawerOpen) : '',
                      covered: slot.veil === 'cover',
                      onToggle: () => drawer.onToggle(index),
                    }
                  : null;

                // Each row is its own slot: a .collapsible, so a row that comes or goes slides on its own
                // while the rest of the list stays where it is. `cover` puts the row under the family's
                // skeleton (`.loading` is the shift-skeleton switch), `fade` fades its values.
                return (
                  <div
                    key={uniqueKey(seen, rowKey(row, index))}
                    class="lt-slot collapsible"
                    data-open={slot.open ? 'true' : 'false'}
                    data-veil={slot.veil}
                    aria-hidden={slot.open ? null : 'true'}
                  >
                    <div class="collapsible-body lt-slot-body">
                      <div class={{ 'lt-row': true, 'loading': slot.veil === 'cover' }} role={rowRoles ? 'row' : null} data-open={drawerOpen ? 'true' : null}>
                        {columns.map(column => (
                          <Cell column={column} row={row} roles={rowRoles} identity={identity} strings={strings} onOpenMedia={onOpenMedia} action={action} />
                        ))}
                      </div>
                      {/*
                        The drawer: after its row in the DOM, so it is next in focus and reading order. It is
                        a `.collapsible` of its own inside the row's slot (the slot's `.collapsible-body`
                        clips it when the slot shuts), and it shares the row's veil: `.loading` for `cover`
                        puts the content under the skeleton, and the slot's `data-veil='fade'` fades it.
                        With table roles on it is a row holding one spanning cell, so it never breaks the
                        table; the region inside is what a reader lands on. The shell is rendered for
                        every row (a shut, empty, aria-hidden one costs a few nodes), so opening is a
                        state change and not a mount; the CONTENT is rendered only for `shown`.
                      */}
                      {drawer && (
                        <div
                          class={{ 'lt-drawer': true, 'collapsible': true, 'loading': slot.veil === 'cover' }}
                          data-open={drawerOpen ? 'true' : 'false'}
                          role={rowRoles ? 'row' : null}
                          aria-hidden={drawerOpen ? null : 'true'}
                          inert={slot.veil === 'cover' && drawerShown}
                        >
                          <div class="collapsible-body lt-drawer-body">
                            <div class="lt-drawer-cell" role={rowRoles ? 'cell' : null}>
                              <div class="lt-drawer-panel" id={id} role={drawerHas ? 'region' : null} aria-label={drawerHas ? drawer.regionLabel(row, index) : null}>
                                {/* The skeleton covers the content only; the panel's rail, rim and shadow are chrome and stay. */}
                                <div class={{ 'lt-drawer-content': true, 'shift-skeleton': drawerShown }}>{drawerShown ? drawer.render(row, index) : null}</div>
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

      {placeholder && <Placeholder placeholder={placeholder} busy={busy} />}
    </div>
  );
}
