import { VNode, h } from '@stencil/core';

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

/** Built kinds. 'number' | 'date' | 'chip' | 'action' are specified and deferred to a named consumer. */
export type CellKind = 'text' | 'code' | 'ref' | 'media';

export type NoticeTone = 'positive' | 'negative' | 'neutral' | 'attention' | 'idle';

/**
 * What the strip shows. The column headings are a fixed structure (they depend on `hiddenFields`
 * only, never on the data), so they are there before the answer is known; only an outcome that has
 * no rows to head replaces them with its notice. There is no skeleton: a skeleton would stand for a
 * shape that is not in doubt (owner, F1, 2026-10-18).
 */
export type LeadKind = 'columns' | 'notice';

export type LookupColumn<Row> = {
  /** The hiddenFields name and the column's stable key: 'partNumber', 'jobNumber', ... */
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
};

// ---------------------------------------------------------------------------------------------
// Derivations: pure, exported, unit-tested, no timers
// ---------------------------------------------------------------------------------------------

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

// The three track recipes of § 18, restated here because the set of columns varies with hiddenFields
// and so the strings cannot live in the stylesheet.
/** Fit the content; the browser does the fitting, no pixel widths. */
const FIT = 'minmax(auto, max-content)';
/** The one column allowed to grow: a floor so it cannot become a sliver, and it takes the slack. */
const GROW = 'minmax(200px, 1fr)';
/** The last column when nothing grows: without it the tracks stop short of the card and the strip's band ends mid-width. */
const LAST = 'minmax(auto, 1fr)';
/** The stacked layout's first column (identity above its description): the slack, with a floor. */
const STACK_FIRST = 'minmax(160px, 1fr)';
/** No visible column at all: one track, so the strip's notice still spans the card. */
const SINGLE = 'minmax(0, 1fr)';

const growing = <Row,>(column: LookupColumn<Row>): boolean => !!column.grow && column.kind === 'text';

/**
 * The `grid-template-columns` strings. `stacked` is the 720–960 layout, where the description sits
 * under its code: it exists only when the first visible column is the identity and a different,
 * growing column is visible. Otherwise it is null and the wide layout simply continues.
 */
export const tableTracks = <Row,>(columns: LookupColumn<Row>[]): { wide: string; stacked: string | null } => {
  if (columns.length === 0) return { wide: SINGLE, stacked: null };

  const anyGrow = columns.some(growing);
  const wide = columns.map((column, index) => (growing(column) ? GROW : !anyGrow && index === columns.length - 1 ? LAST : FIT)).join(' ');

  const first = columns[0];
  const stackable = !!first.identity && columns.some(column => column !== first && growing(column));
  const stacked = stackable ? [STACK_FIRST, ...columns.filter(column => column !== first && !growing(column)).map(() => FIT)].join(' ') : null;

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

const Cell = <Row,>({
  column,
  row,
  roles,
  identity,
  strings,
  onOpenMedia,
}: { column: LookupColumn<Row>; row: Row; roles: boolean; identity: string | null } & Pick<LookupTableProps<Row>, 'strings' | 'onOpenMedia'>) => {
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
      {media ? <MediaBox src={text} identity={identity} strings={strings} onOpenMedia={onOpenMedia} /> : text === null ? <span class="lt-none">—</span> : text}
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

export function LookupTable<Row>(props: LookupTableProps<Row>, children?: VNode[]): VNode {
  const { columns, rows, rowKey, lead, notice, busy, body, open, reshaping = false, strings, onOpenMedia } = props;

  // `rows: []` is not a table: no row, so no role for a reader to land on (§ 10).
  const roles = tableRoles(body, open, reshaping) && rows.length > 0;
  const tracks = tableTracks(columns);
  const identityColumn = columns.find(column => column.identity);
  const seen = new Set<string>();

  const style: Record<string, string> = { '--lt-tracks': tracks.wide };
  if (tracks.stacked) style['--lt-tracks-stacked'] = tracks.stacked;

  const glyph = notice && TONE_GLYPH[notice.tone];

  return (
    <div class="lt-grid" role={roles ? 'table' : null} data-phase={busy ? 'busy' : 'settled'} data-stack={tracks.stacked ? 'true' : 'false'} style={style}>
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

                return (
                  <div key={uniqueKey(seen, rowKey(row, index))} class="lt-row" role={roles ? 'row' : null}>
                    {columns.map(column => (
                      <Cell column={column} row={row} roles={roles} identity={identity} strings={strings} onOpenMedia={onOpenMedia} />
                    ))}
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
