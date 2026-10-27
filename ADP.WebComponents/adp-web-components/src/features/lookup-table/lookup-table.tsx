import { FunctionalComponent, VNode, h } from '@stencil/core';

import { ChevronRightIcon } from '~assets/chevron-right-icon';

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
  /** The action button's label, open and shut, followed by the row's identity value. */
  expandRow?: string;
  collapseRow?: string;
};

export type RowActions<Row> = {
  expandedKey: string | null;
  fetchingKey?: string | null;
  onToggle: (key: string) => void;
  /** The record's key, so a drawer survives a lookup that returns the same record. */
  keyOf?: (row: Row, index: number) => string;
  /** Drawers still sliding shut: their content stays mounted until the owner clears them. */
  closing?: ClosingDrawer<Row>[];
};

export type ClosingDrawer<Row> = { index: number; row: Row };

export type PlaceholderKind = 'idle' | 'empty' | 'error' | 'unauthorized';

export type PlaceholderContent = { icon: FunctionalComponent<{ class?: string }>; text: string };

export type TablePlaceholder = {
  kind: PlaceholderKind | null;
  /** The kind shown last, kept while the region slides shut. */
  retained?: PlaceholderKind | null;
  content: Record<PlaceholderKind, PlaceholderContent>;
};

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
  lang?: string;
  density?: Density;
  /** A compact table inside a drawer: no strip and no body region, and empty columns are dropped. */
  embedded?: boolean;
  label?: string;
  actions?: RowActions<Row>;
  /** Built only for the open and the closing rows. */
  drawer?: (row: Row, key: string) => VNode | null;
  placeholder?: TablePlaceholder;
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
const DATE = 'minmax(120px, 0.8fr)';
const NUMBER = 'minmax(110px, 0.8fr)';
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

/** The day as the DTO sent it: no time zone conversion, so the date never shifts by a day. */
export const dateText = (raw: string): string => raw.trim().slice(0, 10);

// Latin digits in every language; Kurdish falls back to Arabic grouping where the runtime lacks ckb.
const NUMBER_LOCALES: Record<string, string[]> = { en: ['en'], ar: ['ar'], ku: ['ckb', 'ku', 'ar'], ru: ['ru'] };

const numberFormats = new Map<string, Intl.NumberFormat>();

export const formatNumber = (value: number, lang: string = 'en'): string => {
  let format = numberFormats.get(lang);
  if (!format) {
    try {
      format = new Intl.NumberFormat(NUMBER_LOCALES[lang] ?? NUMBER_LOCALES.en, { maximumFractionDigits: 3, numberingSystem: 'latn' });
    } catch {
      format = new Intl.NumberFormat('en', { maximumFractionDigits: 3, numberingSystem: 'latn' });
    }
    numberFormats.set(lang, format);
  }
  return format.format(value);
};

export const cellText = <Row,>(column: LookupColumn<Row>, row: Row): string | null => {
  const value = column.value?.(row);
  if (value === null || value === undefined) return null;
  const text = String(value);
  if (text.trim() === '') return null;
  if (column.kind === 'date') return dateText(text);
  if (column.kind === 'number') return Number.isFinite(Number(text)) ? String(Number(text)) : null;
  return text;
};

/** Drops a column empty on every row; the identity and the action always stay. */
export const nonEmptyColumns = <Row,>(columns: LookupColumn<Row>[], rows: Row[]): LookupColumn<Row>[] =>
  columns.filter(column => column.identity || column.kind === 'action' || rows.some(row => cellText(column, row) !== null));

/** A valid, collision-free id for any key. */
export const drawerId = (key: string): string => `lt-drawer-${key.replace(/[^A-Za-z0-9-]/g, character => `_${character.charCodeAt(0).toString(16)}_`)}`;

export const drawerButtonId = (key: string): string => `${drawerId(key)}-button`;

/** The drawer stays open only if the rows shown after a lookup still have its record. */
export const drawerAfterLookup = <Row,>(openKey: string | null, nextRows: Row[], keyOf: (row: Row, index: number) => string): string | null =>
  openKey !== null && nextRows.some((row, index) => keyOf(row, index) === openKey) ? openKey : null;

export const drawerAfterPage = (openKey: string | null, pageKeys: readonly string[]): string | null => (openKey !== null && pageKeys.includes(openKey) ? openKey : null);

/** The drawers sliding shut after a change, so their content stays while they shut. */
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

  return [...kept.filter(entry => entry.index !== index), { index, row: previousRows[index] }];
};

const INTERACTIVE = 'a[href], button, input, select, textarea, label, summary, [role="button"], [role="link"], [contenteditable=""], [contenteditable="true"]';

/** A row click toggles unless it lands on something interactive, ends a text selection, or the table is busy. */
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

/** The table roles exist only while there is a table to land in: the body is rows, open, and not reshaping (§ 8). */
export const tableRoles = (body: string, open: boolean, reshaping: boolean): boolean => body === 'rows' && open && !reshaping;

/**
 * The strip shows the headings whenever there is something to head: idle, in flight, and above rows
 * (they depend on the configuration alone, so the answer cannot change them). It shows a notice
 * only for an outcome that has no rows (authorized and empty, unauthorized, a failed lookup, a
 * panel's own body) and when every column is hidden, where there are no headings to show. § 20's
 * `panelLead`, generalised, with the skeleton leg removed. A panel with a placeholder keeps its headings.
 */
export const tableLead = ({ busy, idle, body, columns, placeholder = false }: { busy: boolean; idle: boolean; body: string; columns: number; placeholder?: boolean }): LeadKind =>
  columns === 0 ? 'notice' : busy || idle || placeholder || body === 'rows' ? 'columns' : 'notice';

/** What the body renders: it keeps its outgoing kind while it shuts, so the content slides away instead of blinking out. */
export const shownBody = (body: string, retained: string): string => (body === 'none' ? retained : body);

/** What the body says without rows. Nothing reads `busy`, so it holds through a lookup. */
export const placeholderKind = ({
  columns,
  loaded,
  failed,
  authorized,
  records,
  leaving = false,
}: {
  columns: number;
  loaded: boolean;
  failed: boolean;
  authorized?: boolean;
  records: number;
  leaving?: boolean;
}): PlaceholderKind | null => {
  if (columns === 0) return null;
  if (failed) return 'error';
  if (!loaded) return 'idle';
  if (authorized === false) return 'unauthorized';
  if (records === 0) return 'empty';
  return leaving ? 'idle' : null;
};

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

type RowAction = { key: string; expanded: boolean; fetching: boolean; disabled: boolean; onToggle: (key: string) => void };

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
      <ChevronRightIcon class="lt-action-icon" />
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

const PLACEHOLDER_KINDS: PlaceholderKind[] = ['idle', 'empty', 'error', 'unauthorized'];

const Placeholder = ({ placeholder, busy }: { placeholder: TablePlaceholder; busy: boolean }) => {
  const shown = shownPlaceholder(placeholder.kind, placeholder.retained);
  const open = placeholder.kind !== null;

  return (
    <div class="lt-placeholder collapsible" data-open={open ? 'true' : 'false'} data-kind={shown ?? 'none'} aria-hidden={open ? null : 'true'} aria-busy={busy ? 'true' : null}>
      <div class="collapsible-body">
        <div class="lt-placeholder-stack layer-stack">
          {PLACEHOLDER_KINDS.map(kind => {
            const active = shown === kind;
            const Icon = placeholder.content[kind].icon;
            return (
              <div
                class={{ 'layer': true, 'lt-placeholder-layer': true, [`is-${kind}`]: true }}
                data-kind={kind}
                data-active={active ? 'true' : 'false'}
                role="status"
                aria-hidden={active ? null : 'true'}
              >
                <span class="lt-placeholder-mark shift-skeleton">
                  <Icon class="lt-placeholder-glyph" />
                </span>
                <p class="lt-placeholder-text shift-skeleton">{placeholder.content[kind].text}</p>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export function LookupTable<Row>(props: LookupTableProps<Row>, children?: VNode[]): VNode {
  if (props.embedded) return EmbeddedTable(props);

  const { columns, rows, rowKey, lead, notice, busy, body, open, reshaping = false, strings, onOpenMedia, slots, lang, density, label, actions, drawer, placeholder } = props;

  const keyOfRow = (row: Row, index: number, fallback?: string): string => (actions?.keyOf ? actions.keyOf(row, index) : (fallback ?? rowKey(row, index)));

  const slotOf = (index: number): RowSlot => slots?.[index] ?? { open: true, veil: 'none' };

  // `rows: []` is not a table: no row, so no role for a reader to land on (§ 10). A row whose slot is
  // shut (arriving, or leaving) is not one either.
  const roles = tableRoles(body, open, reshaping) && rows.some((_, index) => slotOf(index).open);
  const tracks = tableTracks(columns);
  const identityColumn = columns.find(column => column.identity);
  const hasActionColumn = columns.some(column => column.kind === 'action');
  const seen = new Set<string>();

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
                // skeleton (`.loading` is the shift-skeleton switch), `fade` fades its values.
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

      {placeholder && <Placeholder placeholder={placeholder} busy={busy} />}
    </div>
  );
}
