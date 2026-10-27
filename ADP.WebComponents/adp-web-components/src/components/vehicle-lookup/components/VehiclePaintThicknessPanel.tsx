import { FunctionalComponent, VNode, h } from '@stencil/core';
import { InferType } from 'yup';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import { PaintThicknessInspectionDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-dto';
import { PaintThicknessCertificateUrlDTO } from '~types/generated/vehicle-lookup/paint-thickness-certificate-url-dto';

import { ArrowIcon } from '~assets/arrow-icon';
import { PrintIcon } from '~assets/print-icon';
import { SearchIcon } from '~assets/search-icon';
import { EmptyTableIcon } from '~assets/empty-table-icon';

import { LookupHead, PanelVerdict } from '~features/vehicle-info-layout';
import { LookupColumn, LookupTable, RowDrawer, RowSlot, shownBody, visibleColumns } from '~features/lookup-table';

import { MEDIA_GLYPHS } from './glyphs';

export type PaintThicknessLocale = InferType<typeof paintThicknessSchema>;

// ---------------------------------------------------------------------------------------------
// The state the panel is decided from, and the host's vocabulary
// ---------------------------------------------------------------------------------------------

/**
 * Everything the derivations read, assembled by the owner. `hidden` and `showSummary` are the
 * *applied* values (the owner swaps them under a fade), so a live configuration change never reaches
 * the render half-done.
 */
export type PanelState = {
  /** A vehicle is on screen and its lookup did not fail. */
  vehicleLoaded: boolean;
  /** `isAuthorized` of that vehicle; `false` means the distributor has no record of it. */
  authorized?: boolean;
  /** The DTO's list, whatever it holds: a list the distributor may not show is dropped by `listed`. */
  inspections?: PaintThicknessInspectionDTO[] | null;
  /** The inspection whose readings the table shows: the tab whose content has landed, not the one just pressed. */
  shownIndex: number;
  hidden: ReadonlySet<string>;
  showSummary: boolean;
  /** `paintThicknessCertificateAvailable`: undefined when the server did not say. */
  certificateAvailable?: boolean;
  /** The signed print links, one per language: only issued to a caller allowed to print. */
  certificateUrls?: PaintThicknessCertificateUrlDTO[] | null;
  /** The host's gate (a feature flag): off, the control is not part of this host's panel at all. */
  showCertificateButton: boolean;
  /** The lookup failed: the sentence the pill carries in the negative tone. */
  error?: string;
};

/** The hiddenFields names, the host's whole vocabulary for this panel: every part of the panel that can be left out. */
export const FIELDS = ['inspectionDate', 'source', 'photos', 'certificate'] as const;

/** What a host sees without configuring anything: everything. */
export const DEFAULT_HIDDEN_FIELDS = '';

/** Whether two configurations draw the same panel: an unknown name changing is not a layout change. */
export const sameConfig = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => FIELDS.every(field => a.has(field) === b.has(field));

/** Whether a configuration change reshapes the table (the photos column and the drawers), not only the tabs and the certificate. */
export const reshapesTable = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => a.has('photos') !== b.has('photos');

// ---------------------------------------------------------------------------------------------
// Rows: one per panel type and position, with up to two sides
// ---------------------------------------------------------------------------------------------

export type Side = 'left' | 'right' | 'centre';

export type PaintReading = {
  side: Side;
  value: number;
  images: string[];
};

/**
 * One row of the table: a body panel and its position, with the reading of each side. A panel with no
 * side (a roof, a hood) or a "centre" one has `centre` alone. The server sends one record per reading;
 * grouping them is the panel's.
 */
export type PaintRow = {
  key: string;
  type: string;
  position: string | null;
  left?: PaintReading;
  right?: PaintReading;
  centre?: PaintReading;
};

const sideOf = (side?: string | null): Side => (side === 'Left' ? 'left' : side === 'Right' ? 'right' : 'centre');

/** Whether `row` has room for a reading on `side`: a centre reading shares a row with nothing, a side with the other side. */
const fits = (row: PaintRow, side: Side): boolean => (side === 'centre' ? !row.left && !row.right && !row.centre : !row[side] && !row.centre);

/**
 * Remembered per inspection object, so the same answer always yields the same row objects: the owner
 * decides which row slots stay, arrive or leave by comparing rows, and a fresh object on every render
 * would read as every row changing. Never mutate what it returns.
 */
const ROWS_OF = new WeakMap<PaintThicknessInspectionDTO, PaintRow[]>();
const NO_ROWS: PaintRow[] = [];

/**
 * The inspection's readings as table rows, in the server's order. Lossless: a reading whose slot in
 * its row is taken (two left readings of one panel) starts another row rather than replacing the first.
 */
export const paintRows = (inspection?: PaintThicknessInspectionDTO | null): PaintRow[] => {
  if (!inspection) return NO_ROWS;
  const cached = ROWS_OF.get(inspection);
  if (cached) return cached;

  const rows: PaintRow[] = [];
  const byBase = new Map<string, PaintRow[]>();

  for (const panel of inspection?.panels ?? []) {
    const side = sideOf(panel.panelSide);
    const base = `${panel.panelType}|${panel.panelPosition ?? ''}`;
    const group = byBase.get(base) ?? [];
    let row = group.find(candidate => fits(candidate, side));

    if (!row) {
      row = { key: group.length ? `${base}#${group.length + 1}` : base, type: panel.panelType, position: panel.panelPosition ?? null };
      group.push(row);
      byBase.set(base, group);
      rows.push(row);
    }

    row[side] = { side, value: panel.measuredThickness, images: (panel.images ?? []).filter(Boolean) };
  }

  ROWS_OF.set(inspection, rows);
  return rows;
};

/** The row's readings, left then right then centre. */
export const readingsOf = (row: PaintRow): PaintReading[] => [row.left, row.right, row.centre].filter((reading): reading is PaintReading => !!reading);

export const rowPhotoCount = (row: PaintRow): number => readingsOf(row).reduce((sum, reading) => sum + reading.images.length, 0);

export const rowHasPhotos = (row: PaintRow): boolean => rowPhotoCount(row) > 0;

/** A reading as the table prints it: at most one decimal (the server mixes `100` and `125.5`), no unit (the heading has it). */
export const formatReading = (value: number | null | undefined): string | null =>
  typeof value === 'number' && Number.isFinite(value) ? String(Math.round(value * 10) / 10) : null;

/** The ISO day of a date or timestamp, as the DTO sends it; null for none (the panel shows a dash). */
export const isoDay = (date?: string | null): string | null => (date && date.trim() ? date.trim().slice(0, 10) : null);

// ---------------------------------------------------------------------------------------------
// Words: nothing the reader sees is a raw enum
// ---------------------------------------------------------------------------------------------

const PANEL_KEYS = { Door: 'panelDoor', Fender: 'panelFender', Roof: 'panelRoof', Hood: 'panelHood', TailGate: 'panelTailGate' } as const;
const POSITION_KEYS = { Front: 'positionFront', Middle: 'positionMiddle', Rear: 'positionRear' } as const;

/** "TailGate" -> "Tail Gate": the fallback for a panel type this version has no word for, so a new server value is readable, not raw. */
const humanise = (raw: string): string => raw.replace(/([a-z0-9])([A-Z])/g, '$1 $2').trim();

export const panelLabel = (locale: PaintThicknessLocale, type: string): string => {
  const key = PANEL_KEYS[type as keyof typeof PANEL_KEYS];
  return (key && locale[key]) || humanise(type);
};

export const positionLabel = (locale: PaintThicknessLocale, position: string | null | undefined): string | null => {
  if (!position) return null;
  const key = POSITION_KEYS[position as keyof typeof POSITION_KEYS];
  return (key && locale[key]) || humanise(position);
};

export const sideLabel = (locale: PaintThicknessLocale, side: Side): string => (side === 'left' ? locale.left : side === 'right' ? locale.right : locale.centre);

const fill = (template: string, values: Record<string, string | number>): string =>
  Object.entries(values)
    .reduce((text, [name, value]) => text.split(`{${name}}`).join(String(value)), template)
    .replace(/\s{2,}/g, ' ')
    .trim();

/** "Door · Front": the row's name, in the locale's order. */
export const rowName = (locale: PaintThicknessLocale, row: PaintRow): string => {
  const position = positionLabel(locale, row.position);
  const name = fill(locale.rowName, { panel: panelLabel(locale, row.type), position: position ?? '' });
  // A panel with no position leaves the template's separator dangling ("Roof ·").
  return position ? name : name.replace(/\s*·\s*$/, '');
};

/** "Front Left Door": one reading's name, for the all-photos groups. */
export const photoGroupName = (locale: PaintThicknessLocale, row: PaintRow, side: Side): string =>
  fill(locale.photoGroupName, { position: positionLabel(locale, row.position) ?? '', side: side === 'centre' ? '' : sideLabel(locale, side), panel: panelLabel(locale, row.type) });

type PluralKey = 'photoCountZero' | 'photoCountOne' | 'photoCountTwo' | 'photoCountFew' | 'photoCountMany' | 'photoCountOther';

const PLURAL_KEYS: Record<Intl.LDMLPluralRule, PluralKey> = {
  zero: 'photoCountZero',
  one: 'photoCountOne',
  two: 'photoCountTwo',
  few: 'photoCountFew',
  many: 'photoCountMany',
  other: 'photoCountOther',
};

/** The language's plural category for a whole number; `one` / `other` if the runtime does not know the language. */
export const pluralCategory = (count: number, language: string): Intl.LDMLPluralRule => {
  try {
    return new Intl.PluralRules(language).select(count);
  } catch {
    return count === 1 ? 'one' : 'other';
  }
};

/** "4 photos", in the locale's form for that number. A category the locale left blank falls back to `other`. */
export const photoCountText = (count: number, locale: PaintThicknessLocale, language: string): string =>
  (locale[PLURAL_KEYS[pluralCategory(count, language)]] || locale.photoCountOther).replace('{count}', String(count));

// ---------------------------------------------------------------------------------------------
// The derivations (design language § 20): what the panel may say, what the body holds, the tabs
// ---------------------------------------------------------------------------------------------

/**
 * The inspections the panel may list. An unauthorized vehicle has none whatever the DTO holds: the
 * list is not the distributor's to show (vehicle-lookup-invariants.md), and a fixture that carries
 * one is a data fault the panel does not follow.
 */
export const listed = (state: PanelState): PaintThicknessInspectionDTO[] => (state.vehicleLoaded && state.authorized !== false ? (state.inspections ?? []) : []);

/** Whether the vehicle has an inspection on record. Judged on the list, never on one tab's rows. */
export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

/** The inspection the table shows; a stale index (a shorter answer) falls back to the first. */
export const shownInspection = (state: PanelState): PaintThicknessInspectionDTO | undefined => {
  const all = listed(state);
  return all[state.shownIndex] ?? all[0];
};

/** The table's rows: the shown inspection's readings. An inspection with no readings has none, and is not a verdict. */
export const shownRows = (state: PanelState): PaintRow[] => paintRows(shownInspection(state));

/** `rows` when the shown inspection has readings; `none` otherwise (idle, unauthorized, error, nothing on file). The body shuts for `none`. */
export const panelBody = (state: PanelState): 'rows' | 'none' => (shownRows(state).length > 0 ? 'rows' : 'none');

/**
 * What fills the space under the strip when there is no list: `idle` before a lookup, after a failed
 * one and for a vehicle not in the distributor's records ("look a vehicle up": the pill says what
 * happened), `empty` for an authorized vehicle with no readings (what would be listed here), none
 * when there is a list. Absence of data is never a verdict (vehicle-lookup-invariants.md).
 */
export const panelPlaceholder = (state: PanelState): 'idle' | 'empty' | null => {
  if (!state.vehicleLoaded || state.authorized === false) return 'idle';
  return panelBody(state) === 'rows' ? null : 'empty';
};

/**
 * Whether the all-photos section is on screen: the host did not hide photos, the shown inspection has
 * a photo, and the panel is showing rows (not leaving, not reshaping). Whether it is *expanded* is the reader's.
 */
export const photosSectionOpen = (state: PanelState, groupCount: number, leaving: boolean, reshaping: boolean): boolean =>
  !state.hidden.has('photos') && groupCount > 0 && panelBody(state) === 'rows' && !leaving && !reshaping;

/** Whether the body is open on rows: a list to show, not shutting for a clear, not swapping columns. In flight it stays open. */
export const bodyOpen = (state: PanelState, leaving: boolean, reshaping: boolean): boolean => !leaving && !reshaping && panelBody(state) === 'rows';

// -- Tabs ---------------------------------------------------------------------------------------

export type PaintTab = {
  key: string;
  /** The line on top: the source, or the date or "Inspection n" when the source is hidden or missing. */
  title: string;
  /** The small line beneath: the date (a dash when none), or null when the title already is the date or the date is hidden. */
  subtitle: string | null;
};

/**
 * One tab per inspection: the source name on top, the date small beneath (a dash when it is null:
 * `inspectionDate` is nullable). `hiddenFields` takes either line away; with both hidden a tab is
 * named by its place, so two tabs are never two blanks.
 */
export const tabsOf = (state: PanelState, locale: PaintThicknessLocale): PaintTab[] =>
  listed(state).map((inspection, index) => {
    const showSource = !state.hidden.has('source');
    const showDate = !state.hidden.has('inspectionDate');
    const source = inspection.source?.trim() || null;
    const date = isoDay(inspection.inspectionDate);

    const title = showSource && source ? source : showDate ? (date ?? '—') : `${locale.inspection} ${index + 1}`;
    const subtitle = showSource && source && showDate ? (date ?? '—') : null;

    return { key: `${index}`, title, subtitle };
  });

/** The tab strip is open whenever there is an inspection to choose between, including one. */
export const tabsOpen = (state: PanelState): boolean => hasRecords(state);

/**
 * The tab a key press moves to, or null when the key is not one the tabs answer to. Left and right
 * follow the reading direction (an rtl panel reverses them); home and end go to the ends; a move
 * wraps.
 */
export const tabForKey = (key: string, current: number, count: number, rtl: boolean): number | null => {
  if (count < 1) return null;
  const step = key === 'ArrowRight' ? (rtl ? -1 : 1) : key === 'ArrowLeft' ? (rtl ? 1 : -1) : 0;
  if (step !== 0) return (current + step + count) % count;
  if (key === 'Home') return 0;
  if (key === 'End') return count - 1;
  return null;
};

// -- Certificate ----------------------------------------------------------------------------------

export type CertificateKind = 'menu' | 'none' | 'hidden';

/**
 * What the certificate control says. A record panel states an absence in words rather than going
 * silent (design language § 2, § 17, § 18): when the server says no certificate can be issued for a
 * vehicle that has inspections, the control stays, grey and without a menu, and says so, so a
 * missing button is never read as a broken one.
 *
 *  - `menu`: links were issued: the outlined button and its language menu.
 *  - `none`: the server said `paintThicknessCertificateAvailable === false` (explicitly): the grey
 *    "No certificate" chip.
 *  - `hidden`: the host turned it off (`showCertificateButton`, or `hiddenFields`), there is nothing
 *    to attach it to (idle, unauthorized, error, no inspection), or the server said a certificate
 *    exists but issued no links to this caller: that is not an absence to assert, so it says nothing.
 */
export const certificateKind = (state: PanelState): CertificateKind => {
  if (!state.showCertificateButton || state.hidden.has('certificate') || !hasRecords(state)) return 'hidden';
  if ((state.certificateUrls?.length ?? 0) > 0) return 'menu';
  return state.certificateAvailable === false ? 'none' : 'hidden';
};

/** What a certificate slot last held, kept a settle after it shut so its content slides away. */
export type CertificateSnapshot = { kind: Exclude<CertificateKind, 'hidden'>; urls: PaintThicknessCertificateUrlDTO[] };

// -- Summary --------------------------------------------------------------------------------------

export type ReadingSummary = { count: number; lowest: number; highest: number };

/** The shown inspection's readings counted, with the lowest and the highest: facts, with no threshold asserted. Null with no readings. */
export const summaryOf = (rows: PaintRow[]): ReadingSummary | null => {
  const values = rows.flatMap(readingsOf).map(reading => reading.value);
  const finite = values.filter(value => Number.isFinite(value));
  return finite.length ? { count: finite.length, lowest: Math.min(...finite), highest: Math.max(...finite) } : null;
};

/** The summary's three parts as words, each with its figure; the unit rides with the lowest and highest. */
export const summaryParts = (summary: ReadingSummary, locale: PaintThicknessLocale): string[] => [
  fill(locale.summaryCount, { value: summary.count }),
  fill(locale.summaryLowest, { value: `${formatReading(summary.lowest)} ${locale.unit}` }),
  fill(locale.summaryHighest, { value: `${formatReading(summary.highest)} ${locale.unit}` }),
];

// -- Photos ---------------------------------------------------------------------------------------

export type PhotoGroup = { key: string; name: string; value: number; images: string[] };

/** Every reading that has photos, named in words, in row order: the all-photos section's groups. */
export const allPhotoGroups = (rows: PaintRow[], locale: PaintThicknessLocale): PhotoGroup[] =>
  rows.flatMap(row =>
    readingsOf(row)
      .filter(reading => reading.images.length > 0)
      .map(reading => ({ key: `${row.key}:${reading.side}`, name: photoGroupName(locale, row, reading.side), value: reading.value, images: reading.images })),
  );

export const groupsPhotoCount = (groups: PhotoGroup[]): number => groups.reduce((sum, group) => sum + group.images.length, 0);

// ---------------------------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------------------------

/** Upper-cased in the locale's own rules (no-op for the scripts without case). */
const upper = (text: string, language: string): string => {
  try {
    return text.toLocaleUpperCase(language);
  } catch {
    return text.toUpperCase();
  }
};

/**
 * The panel's columns, all of them (hiddenFields can take only the photos column away): panel,
 * position, left, right, photos. A one-reading panel prints its reading in the left column with a
 * small grey "Centre" chip. The unit is in the two reading headings and nowhere else in the table.
 * The two headings are upper-cased here and not by the table's `text-transform`, which would turn
 * the micro sign into a Greek capital mu.
 */
export const paintColumns = (locale: PaintThicknessLocale, language: string): LookupColumn<PaintRow>[] => [
  { field: 'panel', label: locale.panel, kind: 'text', identity: true, value: row => panelLabel(locale, row.type) },
  { field: 'position', label: locale.position, kind: 'text', value: row => positionLabel(locale, row.position) },
  {
    field: 'left',
    label: `${upper(locale.left, language)} (${locale.unit})`,
    kind: 'text',
    value: row => formatReading((row.left ?? row.centre)?.value),
    note: row => (row.left ? null : row.centre ? locale.centre : null),
  },
  { field: 'right', label: `${upper(locale.right, language)} (${locale.unit})`, kind: 'text', value: row => formatReading(row.right?.value) },
  {
    field: 'photos',
    label: locale.photos,
    kind: 'action',
    glyph: MEDIA_GLYPHS.camera,
    // A row with photos says how many (the toggle); a row without says so in plain words, with nothing to open.
    value: row => (rowHasPhotos(row) ? photoCountText(rowPhotoCount(row), locale, language) : locale.noPhotos),
  },
];

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

const Glyph = ({ path, className }: { path: string; className: string }) => (
  <svg class={className} viewBox="0 0 512 512" aria-hidden="true" focusable="false">
    <path fill="currentColor" d={path} />
  </svg>
);

/**
 * A photo's fetch state -- loading, then ready or failed -- is a fact about the browser's request,
 * not about the data, so it lives on the DOM, exactly as the table's media cell does it (see
 * `settleMedia` in lookup-table.tsx for why a re-render does not undo it): the vdom always says
 * "loading" and the image's own events move the button on. The picture fades in over --settle and a
 * failed one cross-fades to the struck-picture stand-in, so a thumbnail never pops.
 */
const settlePhoto = (image: HTMLImageElement, state: 'ready' | 'failed') => {
  const photo = image.closest('.paint-photo');
  if (!photo || photo.getAttribute('data-state') !== 'loading') return;
  photo.setAttribute('data-state', state);
  if (state === 'failed') photo.setAttribute('disabled', '');
};

/** A cached picture can finish before the listener is attached; this catches it one microtask later, when the element is in its button. */
const settleIfComplete = (image: HTMLImageElement | undefined) => {
  if (!image) return;
  queueMicrotask(() => {
    if (image.complete === true && image.naturalWidth > 0) settlePhoto(image, 'ready');
  });
};

type OpenPhoto = (image: HTMLImageElement, src: string, label: string) => void;

/** One picture: a portrait box, a grey rim at rest and the interaction blue on hover or focus. Never covered by an overlay. */
const Photo = ({ src, label, onOpenPhoto }: { src: string; label: string; onOpenPhoto?: OpenPhoto }) => (
  <button
    key={src}
    type="button"
    class="paint-photo"
    data-state="loading"
    aria-label={label}
    onClick={event => {
      const image = (event.currentTarget as HTMLElement).querySelector('img');
      if (image) onOpenPhoto?.(image, src, label);
    }}
  >
    {/* alt="": the button names it. Lazy and async: only an open drawer renders these at all, and a long list must not fetch every full-size file at once. */}
    <img
      class="paint-photo-image"
      src={src}
      alt=""
      loading="lazy"
      decoding="async"
      ref={settleIfComplete}
      onLoad={event => settlePhoto(event.currentTarget as HTMLImageElement, 'ready')}
      onError={event => settlePhoto(event.currentTarget as HTMLImageElement, 'failed')}
    />
    <span class="paint-photo-stand" aria-hidden="true">
      <Glyph path={MEDIA_GLYPHS.pictureOff} className="paint-photo-glyph" />
    </span>
  </button>
);

type PhotosProps = { images: string[]; name: string; locale: PaintThicknessLocale; onOpenPhoto?: OpenPhoto };

const Thumbs = ({ images, name, locale, onOpenPhoto }: PhotosProps) => (
  <div class="paint-thumbs">
    {images.map((src, index) => (
      <Photo src={src} label={fill(locale.openPhotoLabel, { index: index + 1, total: images.length, name })} onOpenPhoto={onOpenPhoto} />
    ))}
  </div>
);

/** A reading beside its caption: the side and the figure with its unit (the table's headings carry the unit; out here there is no heading). */
const sideCaption = (locale: PaintThicknessLocale, side: Side, value: number): VNode => (
  <div class="paint-side-caption">
    {sideLabel(locale, side)} ·{' '}
    <span class="paint-figure">
      {formatReading(value)} {locale.unit}
    </span>
  </div>
);

/**
 * A row's photos, grouped by side, each side with its reading. A side with no photos says so, so a
 * missing picture is visible, not just absent. Pure content with no box of its own: the drawer under
 * a row puts it in its panel today, and a bottom sheet on a phone will put the same nodes in the sheet.
 */
export const rowPhotosContent = (row: PaintRow, locale: PaintThicknessLocale, onOpenPhoto?: OpenPhoto): VNode => (
  <div class="paint-sides">
    {readingsOf(row).map(reading => (
      <div class="paint-side">
        {sideCaption(locale, reading.side, reading.value)}
        {reading.images.length ? (
          <Thumbs images={reading.images} name={photoGroupName(locale, row, reading.side)} locale={locale} onOpenPhoto={onOpenPhoto} />
        ) : (
          <span class="paint-note">{locale.noPhotos}</span>
        )}
      </div>
    ))}
  </div>
);

/** A row's drawer: its name and photo count above its photos (`rowPhotosContent`). */
export const drawerContent = (row: PaintRow, locale: PaintThicknessLocale, language: string, onOpenPhoto?: OpenPhoto): VNode[] => [
  <div class="paint-drawer-head">
    <span class="paint-drawer-name">{rowName(locale, row)}</span>
    <span class="paint-note">{photoCountText(rowPhotoCount(row), locale, language)}</span>
  </div>,
  rowPhotosContent(row, locale, onOpenPhoto),
];

/**
 * The certificate menu's content: one item per print language, each a plain anchor to that language's
 * server-issued signed URL. It opens in a new tab with no fetch, so there is no auth plumbing and no
 * popup-blocker interception. Pure content with no box of its own, so the popover under the head's
 * button and a bottom sheet on a phone can both hold it. `focusable` is whether the items may be
 * tabbed to (the menu is open); `onChoose` runs on a click, to close the menu.
 */
export const certificateMenuContent = (urls: PaintThicknessCertificateUrlDTO[], focusable: boolean, onChoose: () => void): VNode[] =>
  urls.map(({ language: code, name, url }) => (
    <a key={code} role="menuitem" class="paint-certificate-item" href={url} target="_blank" rel="noopener noreferrer" tabIndex={focusable ? 0 : -1} onClick={onChoose}>
      {name || code}
    </a>
  ));

// ---------------------------------------------------------------------------------------------
// The tab strip: self-contained, so it can be lifted into a shared tabs component
// ---------------------------------------------------------------------------------------------

export const TABPANEL_ID = 'paint-tabpanel';
export const tabId = (index: number) => `paint-tab-${index}`;

type TabsProps = {
  tabs: PaintTab[];
  /** The tab pressed: the one `aria-selected` says, and the one in the tab order. */
  selected: number;
  disabled: boolean;
  rtl: boolean;
  /** The strip's accessible name. */
  label: string;
  /** The panel the tabs control. */
  panelId: string;
  onSelect: (index: number) => void;
};

/**
 * A tab strip: a label per tab with an optional caption beneath (the date). Its markup and classes
 * (`paint-tabs*`) stand on their own: nothing in the table's stylesheet styles it. The full tabs
 * contract: `tablist` / `tab`, `aria-selected`, `aria-controls`, a roving tabindex (only the
 * selected tab is in the tab order), Left / Right (reversed in a right-to-left panel), Home and End,
 * wrapping. Selection follows focus. The caption is its own slot that opens and shuts, so a hidden
 * date closes the line instead of removing it in one frame.
 */
export const PaintTabs: FunctionalComponent<TabsProps> = ({ tabs, selected, disabled, rtl, label, panelId, onSelect }) => {
  const current = Math.min(selected, Math.max(0, tabs.length - 1));

  return (
    <div class="paint-tabs" role="tablist" aria-label={label}>
      {tabs.map((tab, index) => {
        const isSelected = index === current;

        return (
          <button
            key={tab.key}
            type="button"
            role="tab"
            class="paint-tabs-tab"
            id={tabId(index)}
            aria-selected={isSelected ? 'true' : 'false'}
            aria-controls={panelId}
            tabIndex={isSelected ? 0 : -1}
            data-selected={isSelected ? 'true' : 'false'}
            aria-disabled={disabled ? 'true' : null}
            onClick={() => {
              if (!disabled) onSelect(index);
            }}
            onKeyDown={event => {
              if (disabled) return;
              const next = tabForKey(event.key, index, tabs.length, rtl);
              if (next === null) return;
              event.preventDefault();
              onSelect(next);
              (event.currentTarget as HTMLElement).parentElement?.querySelectorAll<HTMLElement>('[role="tab"]')[next]?.focus();
            }}
          >
            {/* One cover per tab: the labels are the answer's, so they go under the skeleton with it (§ 21a). */}
            <span class="paint-tabs-text shift-skeleton">
              <span class="paint-tabs-label">{tab.title}</span>
              <span class="paint-tabs-caption" data-open={tab.subtitle !== null ? 'true' : 'false'} aria-hidden={tab.subtitle !== null ? null : 'true'}>
                <span class="paint-tabs-caption-body">
                  <span class="paint-tabs-date">{tab.subtitle}</span>
                </span>
              </span>
            </span>
          </button>
        );
      })}
    </div>
  );
};

type Props = {
  locale: PaintThicknessLocale;
  /** The language key, for plural rules and casing. */
  language: string;
  direction: string;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  /** A lookup is in flight or the panel is leaving: the head leaves; what is on screen goes under the skeleton where it stands. */
  busy: boolean;
  /** The panel is leaving (cleared, or failing from a settled card): the body shuts over its rows. */
  leaving: boolean;
  /** A live configuration change is in progress: what it changes fades out, is swapped while out and fades back. */
  reconfiguring: boolean;
  /** That change reshapes the table (the photos column): the body is shut and the headings fade while it swaps. */
  reshaping: boolean;
  /** An inspection is being switched: nothing is fetched, so the values fade out and the next inspection's come in. */
  swapping: boolean;
  /** The body's last kind, kept a settle after it shut so its content slides away instead of blinking out. */
  retainedBody: string;
  /** The tab pressed, which is the one `aria-selected` says; the table's own tab follows once the values are out (`state.shownIndex`). */
  selectedTab: number;
  /** The last tabs, kept so a strip that is shutting still has its labels. */
  retainedTabs: PaintTab[];
  retainedCertificate: CertificateSnapshot | null;
  /** The last reading summary, kept so the line that is shutting still has its words. */
  retainedSummary: ReadingSummary | null;
  /** The rows in the table's slots: the shown inspection's, then any rows that are leaving (the owner's). */
  slotRows: PaintRow[];
  /** One per slot row: open or shut, and how its values are veiled (the owner's `tableSlots`). */
  slots: RowSlot[];
  /** The open drawer's slot, the ones still rendered (it and any shutting), and the row to draw for a slot that has lost its photos. */
  drawerOpen: number | null;
  drawerShown: readonly number[];
  drawerRowFor: (row: PaintRow, index: number) => PaintRow;
  onToggleDrawer: (index: number) => void;
  /** The all-photos section: open or shut, and the last groups, kept while it shuts. */
  photosOpen: boolean;
  /** The section's pictures exist in the DOM: it is open, or still shutting. Closed, no picture is rendered, so none is requested. */
  photosRendered: boolean;
  retainedGroups: PhotoGroup[];
  onTogglePhotos: () => void;
  onSelectTab: (index: number) => void;
  certificateOpen: boolean;
  onToggleCertificate: () => void;
  onCloseCertificate: (restoreFocus: boolean) => void;
  onOpenPhoto?: OpenPhoto;
};

const CERTIFICATE_MENU_ID = 'paint-certificate-menu';
const PHOTOS_REGION_ID = 'paint-photos-region';
const DRAWER_PREFIX = 'paint';

export const VehiclePaintThicknessPanel: FunctionalComponent<Props> = props => {
  const {
    locale,
    language,
    direction,
    state,
    verdict,
    busy,
    leaving,
    reconfiguring,
    reshaping,
    swapping,
    retainedBody,
    selectedTab,
    retainedTabs,
    retainedCertificate,
    retainedSummary,
    slotRows,
    slots,
    drawerOpen,
    drawerShown,
    drawerRowFor,
    onToggleDrawer,
    photosOpen,
    photosRendered,
    retainedGroups,
    onTogglePhotos,
    onSelectTab,
    certificateOpen,
    onToggleCertificate,
    onCloseCertificate,
    onOpenPhoto,
  } = props;

  const photosShown = !state.hidden.has('photos');
  const columns = visibleColumns(paintColumns(locale, language), state.hidden);

  const body = panelBody(state);
  const open = bodyOpen(state, leaving, reshaping);
  const placeholder = leaving ? 'idle' : panelPlaceholder(state);

  const rows = shownRows(state);
  const summaryNow = state.showSummary ? summaryOf(rows) : null;
  const summary = summaryNow ?? retainedSummary;

  // The strip and its labels hold while a lookup is in flight (they are the last answer, § 21a); when the new
  // state has none, the last ones stay rendered until the strip has shut.
  const tabsNow = tabsOf(state, locale);
  const tabs = tabsNow.length ? tabsNow : retainedTabs;
  const tabsAreOpen = tabsOpen(state) && !leaving;

  const kind = certificateKind(state);
  const certificate: CertificateSnapshot | null = kind === 'hidden' || leaving ? retainedCertificate : { kind, urls: state.certificateUrls ?? [] };
  const summaryIsOpen = !!summaryNow && !leaving;
  const certificateAreOpen = kind !== 'hidden' && !leaving;
  const menuOpen = certificateOpen && certificateAreOpen && certificate?.kind === 'menu';

  const groups = allPhotoGroups(rows, locale);
  const shownGroups = groups.length ? groups : retainedGroups;
  const photosSection = photosSectionOpen(state, groups.length, leaving, reshaping);
  const photosBodyOpen = photosSection && photosOpen;

  const drawer: RowDrawer<PaintRow> | undefined = photosShown
    ? {
        has: row => rowHasPhotos(row),
        render: (row, index) => drawerContent(drawerRowFor(row, index), locale, language, onOpenPhoto),
        label: (row, _index, expanded) =>
          fill(expanded ? locale.hidePhotosLabel : locale.showPhotosLabel, { photos: photoCountText(rowPhotoCount(row), locale, language), panel: rowName(locale, row) }),
        regionLabel: row => fill(locale.photosRegionLabel, { panel: rowName(locale, row) }),
        open: drawerOpen,
        shown: drawerShown,
        onToggle: onToggleDrawer,
        idPrefix: DRAWER_PREFIX,
      }
    : undefined;

  const rtl = direction === 'rtl';

  return (
    <section
      class="paint-card"
      data-verdict={verdict.state}
      data-phase={busy ? 'busy' : 'settled'}
      data-body={body}
      data-swapping={swapping ? 'true' : 'false'}
      data-reconfiguring={reconfiguring ? 'true' : 'false'}
      data-certificate={kind}
    >
      {/* The anchor for the certificate menu: the head clips its own content (overflow: hidden), so the menu is the head's sibling, hung from the head's bottom edge. */}
      <div class="paint-head">
        <LookupHead title={locale.paintThickness} verdict={verdict}>
          {/* The certificate: an action in the head's trailing group, before the pill (owner decision 16, extending § 14). It rides in the head's content block, so it leaves
              and returns with every lookup, and it is a slot that fades: the head is the same height with it and without it. */}
          <div class="paint-certificate-slot" data-open={certificateAreOpen ? 'true' : 'false'} aria-hidden={certificateAreOpen ? null : 'true'}>
            <div class="paint-certificate" data-kind={certificate?.kind ?? 'none'}>
              {!certificate ? null : certificate.kind === 'none' ? (
                <span class="paint-certificate-none" title={locale.noCertificateHint}>
                  <PrintIcon fill="currentColor" viewBox="0 0 24 24" class="paint-certificate-print" />
                  <span>{locale.noCertificate}</span>
                  <span class="sr-only">{locale.noCertificateHint}</span>
                </span>
              ) : (
                <button
                  type="button"
                  class="paint-certificate-button"
                  aria-haspopup="menu"
                  aria-expanded={menuOpen ? 'true' : 'false'}
                  aria-controls={CERTIFICATE_MENU_ID}
                  aria-label={locale.printCertificate}
                  disabled={!certificateAreOpen || busy}
                  onClick={onToggleCertificate}
                >
                  <PrintIcon fill="currentColor" viewBox="0 0 24 24" class="paint-certificate-print" />
                  <span class="paint-certificate-label">{locale.certificate}</span>
                  <ArrowIcon class="paint-certificate-arrow" />
                </button>
              )}
            </div>
          </div>
        </LookupHead>

        <div
          id={CERTIFICATE_MENU_ID}
          class="paint-certificate-menu"
          role="menu"
          aria-label={locale.printCertificate}
          data-open={menuOpen ? 'true' : 'false'}
          aria-hidden={menuOpen ? null : 'true'}
        >
          {certificateMenuContent(certificate?.urls ?? [], menuOpen, () => onCloseCertificate(false))}
        </div>
      </div>

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
          {/* The strip of inspections. A slot like any other: open when there is something to choose between, shut over its last labels when there is not. */}
          <div class="paint-bar collapsible" data-open={tabsAreOpen ? 'true' : 'false'} aria-hidden={tabsAreOpen ? null : 'true'}>
            <div class="collapsible-body">
              <PaintTabs tabs={tabs} selected={selectedTab} disabled={busy} rtl={rtl} label={locale.inspections} panelId={TABPANEL_ID} onSelect={onSelectTab} />
            </div>
          </div>

          {/* The reading summary (off by default): a quiet line between the tabs and the table, a slot that opens and shuts. Its figures are the answer's, so they go under the skeleton in flight. */}
          <div class="paint-summary-slot collapsible" data-open={summaryIsOpen ? 'true' : 'false'} aria-hidden={summaryIsOpen ? null : 'true'}>
            <div class="collapsible-body">
              <p class="paint-summary shift-skeleton">
                {(summary ? summaryParts(summary, locale) : []).map((part, index) => [
                  index > 0 && (
                    <span class="paint-summary-dot" aria-hidden="true">
                      ·
                    </span>
                  ),
                  <span class="paint-summary-part">{part}</span>,
                ])}
              </p>
            </div>
          </div>

          <div
            class="paint-table"
            id={TABPANEL_ID}
            role={tabsAreOpen ? 'tabpanel' : null}
            aria-labelledby={tabsAreOpen ? tabId(Math.min(selectedTab, Math.max(0, tabs.length - 1))) : null}
          >
            <LookupTable
              columns={columns}
              rows={slotRows}
              slots={slots}
              // Keyed by slot, not by panel: a slot is a place on the card that stays while its values change.
              rowKey={(_, index) => `slot-${index}`}
              lead="columns"
              notice={null}
              busy={busy}
              body={shownBody(body, retainedBody)}
              open={open}
              reshaping={reshaping}
              strings={{ columnsLabel: locale.columnsLabel, noImage: '', imageFailed: '', openImage: '' }}
              drawer={drawer}
            />
          </div>

          {/* The placeholder under the strip when there is no list (panelPlaceholder). Always mounted; it slides open and shut and its two sentences cross-fade, so it never pops. Its words go under the skeleton in flight: "look a vehicle up" is not true then. */}
          <div class="paint-placeholder collapsible" data-open={placeholder ? 'true' : 'false'} aria-hidden={placeholder ? null : 'true'}>
            <div class="collapsible-body">
              <div class="paint-placeholder-stack layer-stack">
                <div class="layer paint-placeholder-layer" data-active={placeholder === 'empty' ? 'false' : 'true'} aria-hidden={placeholder === 'idle' ? null : 'true'}>
                  <span class="paint-placeholder-mark">
                    <SearchIcon class="paint-placeholder-glyph" />
                  </span>
                  <p class="paint-placeholder-text shift-skeleton">{locale.idleHint}</p>
                </div>
                <div class="layer paint-placeholder-layer" data-active={placeholder === 'empty' ? 'true' : 'false'} aria-hidden={placeholder === 'empty' ? null : 'true'}>
                  <span class="paint-placeholder-mark" aria-hidden="true">
                    <EmptyTableIcon class="paint-placeholder-glyph" />
                  </span>
                  <p class="paint-placeholder-text shift-skeleton">{locale.emptyHint}</p>
                </div>
              </div>
            </div>
          </div>

          {/* All photos: the end of the panel. Collapsed by default, and nothing in it is rendered as an image until it opens. A slot like the rows: open while the inspection has any photo. */}
          <section class="paint-photos collapsible" data-open={photosSection ? 'true' : 'false'} aria-hidden={photosSection ? null : 'true'} aria-label={locale.allPhotos}>
            <div class="collapsible-body">
              <button
                type="button"
                class="paint-photos-toggle"
                aria-expanded={photosBodyOpen ? 'true' : 'false'}
                aria-controls={PHOTOS_REGION_ID}
                disabled={!photosSection}
                onClick={onTogglePhotos}
              >
                <Glyph path={MEDIA_GLYPHS.camera} className="paint-photos-glyph" />
                <span class="paint-photos-label">{locale.allPhotos}</span>
                <span class="paint-photos-count shift-skeleton">{photoCountText(groupsPhotoCount(shownGroups), locale, language)}</span>
                <Glyph path="M256 368 96 208l32-32 128 128 128-128 32 32z" className="paint-photos-chevron" />
              </button>

              <div id={PHOTOS_REGION_ID} class="paint-photos-region collapsible" data-open={photosBodyOpen ? 'true' : 'false'} aria-hidden={photosBodyOpen ? null : 'true'}>
                <div class="collapsible-body">
                  {/* Rendered only while open or shutting: a closed section requests no picture. The skeleton covers the groups only; the band above and the section's chrome stay. */}
                  <div class="paint-photos-content shift-skeleton">
                    {(photosRendered ? shownGroups : []).map(group => (
                      <div key={group.key} class="paint-group">
                        <div class="paint-group-name">
                          <span class="paint-group-title">{group.name}</span>
                          <span class="paint-note">
                            {formatReading(group.value)} {locale.unit}
                          </span>
                        </div>
                        <Thumbs images={group.images} name={group.name} locale={locale} onOpenPhoto={onOpenPhoto} />
                      </div>
                    ))}
                  </div>
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>
    </section>
  );
};
