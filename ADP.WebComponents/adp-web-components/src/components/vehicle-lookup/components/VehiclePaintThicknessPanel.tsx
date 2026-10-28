import { FunctionalComponent, VNode, h } from '@stencil/core';
import { InferType } from 'yup';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import { PaintThicknessInspectionDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-dto';
import { PaintThicknessCertificateUrlDTO } from '~types/generated/vehicle-lookup/paint-thickness-certificate-url-dto';

import { ArrowIcon } from '~assets/arrow-icon';
import { PrintIcon } from '~assets/print-icon';
import { SearchIcon } from '~assets/search-icon';
import { EmptyTableIcon } from '~assets/empty-table-icon';
import { QuestionMark } from '~assets/question-mark';
import { TriangleAlertIcon } from '~assets/triangle-alert';

import { LookupHeadWait, PanelVerdict, StatusBadge } from '~features/vehicle-info-layout';
import { LookupColumn, LookupTable, PlaceholderKind, RowDrawer, RowSlot, TablePlaceholder, drawerId, placeholderKind, shownBody, visibleColumns } from '~features/lookup-table';
import type { LanguageKeys } from '~features/multi-lingual';

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
  /** The vehicle's model description and model year, for the head's title. */
  model?: string | null;
  modelYear?: number | null;
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

const fill = (template: string | undefined, values: Record<string, string | number>): string =>
  Object.entries(values)
    .reduce((text, [name, value]) => text.split(`{${name}}`).join(String(value)), template ?? '')
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

const COUNT_KEYS = {
  zero: 'inspectionCountOther',
  one: 'inspectionCountOne',
  two: 'inspectionCountTwo',
  few: 'inspectionCountFew',
  many: 'inspectionCountMany',
  other: 'inspectionCountOther',
} as const;

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

/** The head's title: the vehicle's model and its year, or the panel's name when the lookup carries no model. */
export const headTitle = (state: PanelState, locale: PaintThicknessLocale): string => {
  const model = state.vehicleLoaded ? (state.model ?? '').trim() : '';
  return model ? [model, state.modelYear || ''].filter(Boolean).join(' ') : locale.paintThickness;
};

/** The head's second line: "2 inspections · last 2026-03-12"; empty when there is nothing listed. */
export const headCount = (state: PanelState, locale: PaintThicknessLocale, language: string): string => {
  const inspections = listed(state);
  if (!inspections.length) return '';
  const count = (locale[COUNT_KEYS[pluralCategory(inspections.length, language)]] || locale.inspectionCountOther).replace('{count}', String(inspections.length));
  const latest = inspections.map(inspection => isoDay(inspection.inspectionDate)).reduce<string | null>((last, day) => (day && (!last || day > last) ? day : last), null);
  return latest ? `${count} · ${locale.lastInspection.replace('{date}', latest)}` : count;
};

/** The inspection the table shows; a stale index (a shorter answer) falls back to the first. */
export const shownInspection = (state: PanelState): PaintThicknessInspectionDTO | undefined => {
  const all = listed(state);
  return all[state.shownIndex] ?? all[0];
};

/** The table's rows: the shown inspection's readings. An inspection with no readings has none, and is not a verdict. */
export const shownRows = (state: PanelState): PaintRow[] => paintRows(shownInspection(state));

/** `rows` when the shown inspection has readings; `none` otherwise (idle, unauthorized, error, nothing on file). The body shuts for `none`. */
export const panelBody = (state: PanelState): 'rows' | 'none' => (shownRows(state).length > 0 ? 'rows' : 'none');

/** Visible columns: panel, position and the two sides are fixed; the photos column is the host's to hide. */
export const columnCount = (hidden: ReadonlySet<string>): number => (hidden.has('photos') ? 4 : 5);

/**
 * What the body says when there is no list: the table's `placeholderKind` fed this panel's state. `idle`
 * before a lookup, `empty` for an authorized vehicle whose shown inspection has no readings (never a
 * verdict), `unauthorized` for a vehicle not in the distributor's records, `error` for a failed lookup,
 * null above rows. `leaving` (a clear, or a failure on a settled card) opens the idle one as the rows shut.
 */
export const panelPlaceholder = (state: PanelState, leaving = false): PlaceholderKind | null =>
  placeholderKind({
    columns: columnCount(state.hidden),
    loaded: state.vehicleLoaded,
    failed: !!state.error,
    authorized: state.authorized,
    records: shownRows(state).length,
    leaving,
  });

/** The icon and the one line of each outcome, from the panel's locale. */
export const placeholderContent = (locale: PaintThicknessLocale): TablePlaceholder['content'] => ({
  idle: { icon: SearchIcon, text: locale.idleHint },
  empty: { icon: EmptyTableIcon, text: locale.emptyHint },
  error: { icon: TriangleAlertIcon, text: locale.errorHint },
  unauthorized: { icon: QuestionMark, text: locale.unauthorizedHint },
});

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
// The tab strip is the shared <shift-tabs>; the panel only names the tabpanel and the tabs' ids
// ---------------------------------------------------------------------------------------------

export const TABPANEL_ID = 'paint-tabpanel';
export const TAB_ID_PREFIX = 'paint-tab';
export const tabId = (index: number) => `${TAB_ID_PREFIX}-${index}`;

/** The row whose photos the phone's bottom sheet holds, and whether it is open (kept mounted while it slides away, `open` false). */
export type PaintSheet = { index: number; open: boolean };

/** A photos sheet's title: the row's name and its photo count, e.g. "Door · Front — 4 photos". */
export const sheetPhotosTitle = (row: PaintRow, locale: PaintThicknessLocale, language: string): string =>
  fill(locale.sheetPhotosTitle, { name: rowName(locale, row), photos: photoCountText(rowPhotoCount(row), locale, language) });

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
  /** The placeholder's last kind, kept a settle after a list opened over it so its words slide away with their region. */
  retainedPlaceholder: PlaceholderKind | null;
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
  /** The section's pictures exist in the DOM: it is open, still shutting, or has been open and is now folded (so opening it again creates nothing). Never drawn before the first time it is open. */
  photosRendered: boolean;
  retainedGroups: PhotoGroup[];
  onTogglePhotos: () => void;
  onSelectTab: (index: number) => void;
  certificateOpen: boolean;
  onToggleCertificate: () => void;
  onCloseCertificate: (restoreFocus: boolean) => void;
  onOpenPhoto?: OpenPhoto;
  /** The viewport is a phone (<= 720px): the certificate menu and a row's photos open as a bottom sheet, not a floating menu and an inline drawer. */
  phone?: boolean;
  /** The row photos sheet (phone), or null: none held. */
  sheet?: PaintSheet | null;
  /** The reader asked the photos sheet to close (Escape, the scrim, the button). */
  onCloseSheet?: () => void;
};

const PHOTOS_REGION_ID = 'paint-photos-region';
const DRAWER_PREFIX = 'paint';

type PhotosSheetProps = {
  sheet: PaintSheet | null;
  locale: PaintThicknessLocale;
  language: string;
  busy: boolean;
  slotRows: PaintRow[];
  drawerRowFor: (row: PaintRow, index: number) => PaintRow;
  onOpenPhoto?: OpenPhoto;
  onCloseSheet: () => void;
};

/**
 * A row's photos as a bottom sheet (<shift-popover>), on a phone. The row is drawn from the retained row
 * too, so a row that vanishes while the sheet is closing slides out on its old content. A lookup in
 * flight puts the title and the content under the skeleton where they stand and makes them inert, exactly
 * as a drawer's content is (the answer decides whether the sheet stays: the owner shuts it only when its content is gone).
 */
const PaintPhotosSheet = ({ sheet, locale, language, busy, slotRows, drawerRowFor, onOpenPhoto, onCloseSheet }: PhotosSheetProps) => {
  const row = sheet ? drawerRowFor(slotRows[sheet.index], sheet.index) : undefined;
  const title = row ? sheetPhotosTitle(row, locale, language) : '';

  return (
    <shift-popover
      class="paint-popover paint-photos-popover"
      data-phone="true"
      topLayer
      alwaysSheet
      open={!!sheet?.open && !!row}
      anchor={sheet ? `[aria-controls="${drawerId(DRAWER_PREFIX, sheet.index)}"]` : undefined}
      label={title || locale.allPhotos}
      heading={title}
      closeLabel={locale.closeSheet}
      language={language as LanguageKeys}
      onOpenChange={(event: CustomEvent<{ open: boolean }>) => !event.detail.open && onCloseSheet()}
    >
      <span slot="heading" class="paint-sheet-title shift-skeleton">
        {title}
      </span>
      {row && (
        <div class="paint-sheet-content shift-skeleton" inert={busy}>
          {rowPhotosContent(row, locale, onOpenPhoto)}
        </div>
      )}
    </shift-popover>
  );
};

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
    retainedPlaceholder,
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
    phone = false,
    sheet = null,
    onCloseSheet = () => {},
  } = props;

  const photosShown = !state.hidden.has('photos');
  const columns = visibleColumns(paintColumns(locale, language), state.hidden);

  const body = panelBody(state);
  const open = bodyOpen(state, leaving, reshaping);
  const placeholder = panelPlaceholder(state, leaving);

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
  const certificateExpanded = certificateOpen && certificateAreOpen && certificate?.kind === 'menu';

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
        // On a phone the row's photos open in the sheet, so no drawer opens under the row.
        open: phone ? null : drawerOpen,
        shown: phone ? [] : drawerShown,
        onToggle: onToggleDrawer,
        idPrefix: DRAWER_PREFIX,
      }
    : undefined;

  const rtl = direction === 'rtl';
  const count = headCount(state, locale, language);

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
      {/* The certificate menu is a popover under the button (a bottom sheet on a phone); it sits outside the head band, which clips its own content, though the top layer would not care. */}
      <div class="paint-head">
        <header class="lookup-head lookup-head-band paint-head-band">
          <div class="paint-main lookup-head-content">
            <p class="lookup-title paint-title">{headTitle(state, locale)}</p>
            <div class="paint-count-slot" data-empty={count ? 'false' : 'true'} aria-hidden={count ? null : 'true'}>
              <p class="paint-count">{count || ' '}</p>
            </div>
          </div>
          <div class="lookup-summary lookup-head-content">
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
                    aria-haspopup="dialog"
                    aria-expanded={certificateExpanded ? 'true' : 'false'}
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
            <StatusBadge state={verdict.state} text={verdict.text} />
          </div>
          <LookupHeadWait />
        </header>

        {certificate?.kind === 'menu' && (
          <shift-popover
            class="paint-popover paint-certificate-popover"
            data-phone={phone ? 'true' : 'false'}
            topLayer
            align="end"
            offset={6}
            alwaysSheet={phone}
            open={certificateExpanded}
            anchor=".paint-certificate-button"
            label={locale.printCertificate}
            heading={locale.printCertificate}
            closeLabel={locale.closeSheet}
            language={language as LanguageKeys}
            onOpenChange={(event: CustomEvent<{ open: boolean }>) => !event.detail.open && onCloseCertificate(false)}
          >
            <span slot="heading" class="paint-sheet-title shift-skeleton">
              {locale.printCertificate}
            </span>
            <div class="paint-menu shift-skeleton" role="menu" aria-label={locale.printCertificate} inert={busy}>
              {certificateMenuContent(certificate.urls, certificateExpanded && !busy, () => onCloseCertificate(false))}
            </div>
          </shift-popover>
        )}
      </div>

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
          {/* The strip of inspections. A slot like any other: open when there is something to choose between, shut over its last labels when there is not. */}
          <div class="paint-bar collapsible" data-open={tabsAreOpen ? 'true' : 'false'} aria-hidden={tabsAreOpen ? null : 'true'}>
            <div class="collapsible-body">
              <shift-tabs
                bare
                skeleton
                tabs={tabs.map(tab => ({ key: tab.key, label: tab.title, caption: tab.subtitle }))}
                activeTabIndex={selectedTab}
                disabled={busy}
                direction={rtl ? 'rtl' : 'ltr'}
                label={locale.inspections}
                panelId={TABPANEL_ID}
                tabIdPrefix={TAB_ID_PREFIX}
                changeActiveTab={({ idx }) => onSelectTab(idx)}
              />
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
              placeholder={{ kind: placeholder, retained: retainedPlaceholder, content: placeholderContent(locale) }}
            />
          </div>

          {/* All photos: the end of the panel. Expanded by default (owner, 2026-10-27). A slot like the rows: open while the inspection has any photo, and the reader may fold it. */}
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
                  {/* Drawn once the section has been open, and kept while it is folded (the owner's `photosRendered`), so opening it again creates no pictures. Folded, the region is hidden (the collapsible's delayed visibility, so nothing in it takes focus) and its lazy pictures sit in a clipped box and fetch nothing. The skeleton covers the groups only; the band above and the section's chrome stay. */}
                  <div class="paint-photos-content shift-skeleton">
                    {(photosRendered ? shownGroups : []).map(group => (
                      <div key={group.key} class="paint-group" style={{ '--paint-photos': `${group.images.length}` }}>
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

      {/* The phone's photos sheet: on a phone it is always mounted and shut, so opening it is a transition (and a settle longer once the viewport leaves the phone range); it holds the panel's own content (the same render functions, the same skeleton covers). */}
      {(phone || sheet) && (
        <PaintPhotosSheet
          sheet={sheet}
          locale={locale}
          language={language}
          busy={busy}
          slotRows={slotRows}
          drawerRowFor={drawerRowFor}
          onOpenPhoto={onOpenPhoto}
          onCloseSheet={onCloseSheet}
        />
      )}
    </section>
  );
};
