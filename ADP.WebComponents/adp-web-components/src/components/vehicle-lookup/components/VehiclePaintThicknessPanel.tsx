import { FunctionalComponent, VNode, h } from '@stencil/core';
import { InferType } from 'yup';

import paintThicknessSchema from '~locales/vehicleLookup/paintThickness/type';

import { PaintThicknessInspectionDTO } from '~types/generated/vehicle-lookup/paint-thickness-inspection-dto';
import { PaintThicknessCertificateUrlDTO } from '~types/generated/vehicle-lookup/paint-thickness-certificate-url-dto';

import { ArrowIcon } from '~assets/arrow-icon';
import { PrintIcon } from '~assets/print-icon';
import { PictureOffIcon } from '~assets/picture-off-icon';
import { CameraSolidIcon } from '~assets/camera-solid-icon';
import { ChevronDownIcon } from '~assets/chevron-down-icon';
import { SearchIcon } from '~assets/search-icon';
import { EmptyTableIcon } from '~assets/empty-table-icon';
import { QuestionMark } from '~assets/question-mark';
import { TriangleAlertIcon } from '~assets/triangle-alert';

import { LookupHeadWait, PanelVerdict, StatusBadge } from '~features/vehicle-info-layout';
import { LookupColumn, LookupTable, PlaceholderKind, RowDrawer, RowSlot, TablePlaceholder, placeholderKind, shownBody, visibleColumns } from '~features/lookup-table';
import type { LanguageKeys } from '~features/multi-lingual';
import type { PopoverSheetChangeDetail } from '../../components/shift-popover/shift-popover';

export type PaintThicknessLocale = InferType<typeof paintThicknessSchema>;

export type PanelState = {
  vehicleLoaded: boolean;
  authorized?: boolean;
  inspections?: PaintThicknessInspectionDTO[] | null;
  shownIndex: number;
  hidden: ReadonlySet<string>;
  showSummary: boolean;
  certificateAvailable?: boolean;
  certificateUrls?: PaintThicknessCertificateUrlDTO[] | null;
  showCertificateButton: boolean;
  error?: string;
  model?: string | null;
  modelYear?: number | null;
};

export const FIELDS = ['inspectionDate', 'source', 'photos', 'certificate'] as const;

export const DEFAULT_HIDDEN_FIELDS = '';

export const sameConfig = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => FIELDS.every(field => a.has(field) === b.has(field));

export const reshapesTable = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => a.has('photos') !== b.has('photos');

export type Side = 'left' | 'right' | 'centre';

export type PaintReading = {
  side: Side;
  value: number;
  images: string[];
};

export type PaintRow = {
  key: string;
  type: string;
  position: string | null;
  left?: PaintReading;
  right?: PaintReading;
  centre?: PaintReading;
};

const sideOf = (side?: string | null): Side => (side === 'Left' ? 'left' : side === 'Right' ? 'right' : 'centre');

const fits = (row: PaintRow, side: Side): boolean => (side === 'centre' ? !row.left && !row.right && !row.centre : !row[side] && !row.centre);

const ROWS_OF = new WeakMap<PaintThicknessInspectionDTO, PaintRow[]>();
const NO_ROWS: PaintRow[] = [];

const paintRows = (inspection?: PaintThicknessInspectionDTO | null): PaintRow[] => {
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

const readingsOf = (row: PaintRow): PaintReading[] => [row.left, row.right, row.centre].filter((reading): reading is PaintReading => !!reading);

const rowPhotoCount = (row: PaintRow): number => readingsOf(row).reduce((sum, reading) => sum + reading.images.length, 0);

export const rowHasPhotos = (row: PaintRow): boolean => rowPhotoCount(row) > 0;

const formatReading = (value: number | null | undefined): string | null => (typeof value === 'number' && Number.isFinite(value) ? String(Math.round(value * 10) / 10) : null);

const isoDay = (date?: string | null): string | null => (date && date.trim() ? date.trim().slice(0, 10) : null);

const PANEL_KEYS = { Door: 'panelDoor', Fender: 'panelFender', Roof: 'panelRoof', Hood: 'panelHood', TailGate: 'panelTailGate' } as const;
const POSITION_KEYS = { Front: 'positionFront', Middle: 'positionMiddle', Rear: 'positionRear' } as const;

const humanise = (raw: string): string => raw.replace(/([a-z0-9])([A-Z])/g, '$1 $2').trim();

export const panelLabel = (locale: PaintThicknessLocale, type: string): string => {
  const key = PANEL_KEYS[type as keyof typeof PANEL_KEYS];
  return (key && locale[key]) || humanise(type);
};

const positionLabel = (locale: PaintThicknessLocale, position: string | null | undefined): string | null => {
  if (!position) return null;
  const key = POSITION_KEYS[position as keyof typeof POSITION_KEYS];
  return (key && locale[key]) || humanise(position);
};

const sideLabel = (locale: PaintThicknessLocale, side: Side): string => (side === 'left' ? locale.left : side === 'right' ? locale.right : locale.centre);

const fill = (template: string | undefined, values: Record<string, string | number>): string =>
  Object.entries(values)
    .reduce((text, [name, value]) => text.split(`{${name}}`).join(String(value)), template ?? '')
    .replace(/\s{2,}/g, ' ')
    .trim();

export const rowName = (locale: PaintThicknessLocale, row: PaintRow): string => {
  const position = positionLabel(locale, row.position);
  const name = fill(locale.rowName, { panel: panelLabel(locale, row.type), position: position ?? '' });
  return position ? name : name.replace(/\s*·\s*$/, '');
};

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

export const pluralCategory = (count: number, language: string): Intl.LDMLPluralRule => {
  try {
    return new Intl.PluralRules(language).select(count);
  } catch {
    return count === 1 ? 'one' : 'other';
  }
};

const photoCountText = (count: number, locale: PaintThicknessLocale, language: string): string =>
  (locale[PLURAL_KEYS[pluralCategory(count, language)]] || locale.photoCountOther).replace('{count}', String(count));

const COUNT_KEYS = {
  zero: 'inspectionCountOther',
  one: 'inspectionCountOne',
  two: 'inspectionCountTwo',
  few: 'inspectionCountFew',
  many: 'inspectionCountMany',
  other: 'inspectionCountOther',
} as const;

export const listed = (state: PanelState): PaintThicknessInspectionDTO[] => (state.vehicleLoaded && state.authorized !== false ? (state.inspections ?? []) : []);

export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

const headTitle = (state: PanelState, locale: PaintThicknessLocale): string => {
  const model = state.vehicleLoaded ? (state.model ?? '').trim() : '';
  return model ? [model, state.modelYear || ''].filter(Boolean).join(' ') : locale.paintThickness;
};

const headCount = (state: PanelState, locale: PaintThicknessLocale, language: string): string => {
  const inspections = listed(state);
  if (!inspections.length) return '';
  const count = (locale[COUNT_KEYS[pluralCategory(inspections.length, language)]] || locale.inspectionCountOther).replace('{count}', String(inspections.length));
  const latest = inspections.map(inspection => isoDay(inspection.inspectionDate)).reduce<string | null>((last, day) => (day && (!last || day > last) ? day : last), null);
  return latest ? `${count} · ${locale.lastInspection.replace('{date}', latest)}` : count;
};

const shownInspection = (state: PanelState): PaintThicknessInspectionDTO | undefined => {
  const all = listed(state);
  return all[state.shownIndex] ?? all[0];
};

export const shownRows = (state: PanelState): PaintRow[] => paintRows(shownInspection(state));

export const panelBody = (state: PanelState): 'rows' | 'none' => (shownRows(state).length > 0 ? 'rows' : 'none');

export const columnCount = (hidden: ReadonlySet<string>): number => (hidden.has('photos') ? 4 : 5);

export const panelPlaceholder = (state: PanelState, leaving = false): PlaceholderKind | null =>
  placeholderKind({
    columns: columnCount(state.hidden),
    loaded: state.vehicleLoaded,
    failed: !!state.error,
    authorized: state.authorized,
    records: shownRows(state).length,
    leaving,
  });

const placeholderContent = (locale: PaintThicknessLocale): TablePlaceholder['content'] => ({
  idle: { icon: SearchIcon, text: locale.idleHint },
  empty: { icon: EmptyTableIcon, text: locale.emptyHint },
  error: { icon: TriangleAlertIcon, text: locale.errorHint },
  unauthorized: { icon: QuestionMark, text: locale.unauthorizedHint },
});

export const photosSectionOpen = (state: PanelState, groupCount: number, leaving: boolean, reshaping: boolean): boolean =>
  !state.hidden.has('photos') && groupCount > 0 && panelBody(state) === 'rows' && !leaving && !reshaping;

export const bodyOpen = (state: PanelState, leaving: boolean, reshaping: boolean): boolean => !leaving && !reshaping && panelBody(state) === 'rows';

export type PaintTab = {
  key: string;
  title: string;
  subtitle: string | null;
};

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

const tabsOpen = (state: PanelState): boolean => hasRecords(state);

export type CertificateKind = 'menu' | 'none' | 'hidden';

export const certificateKind = (state: PanelState): CertificateKind => {
  if (!state.showCertificateButton || state.hidden.has('certificate') || !hasRecords(state)) return 'hidden';
  if ((state.certificateUrls?.length ?? 0) > 0) return 'menu';
  return state.certificateAvailable === false ? 'none' : 'hidden';
};

export type CertificateSnapshot = { kind: Exclude<CertificateKind, 'hidden'>; urls: PaintThicknessCertificateUrlDTO[] };

export type ReadingSummary = { count: number; lowest: number; highest: number };

export const summaryOf = (rows: PaintRow[]): ReadingSummary | null => {
  const values = rows.flatMap(readingsOf).map(reading => reading.value);
  const finite = values.filter(value => Number.isFinite(value));
  return finite.length ? { count: finite.length, lowest: Math.min(...finite), highest: Math.max(...finite) } : null;
};

const summaryParts = (summary: ReadingSummary, locale: PaintThicknessLocale): string[] => [
  fill(locale.summaryCount, { value: summary.count }),
  fill(locale.summaryLowest, { value: `${formatReading(summary.lowest)} ${locale.unit}` }),
  fill(locale.summaryHighest, { value: `${formatReading(summary.highest)} ${locale.unit}` }),
];

export type PhotoGroup = { key: string; name: string; value: number; images: string[] };

export const allPhotoGroups = (rows: PaintRow[], locale: PaintThicknessLocale): PhotoGroup[] =>
  rows.flatMap(row =>
    readingsOf(row)
      .filter(reading => reading.images.length > 0)
      .map(reading => ({ key: `${row.key}:${reading.side}`, name: photoGroupName(locale, row, reading.side), value: reading.value, images: reading.images })),
  );

const groupsPhotoCount = (groups: PhotoGroup[]): number => groups.reduce((sum, group) => sum + group.images.length, 0);

const upper = (text: string, language: string): string => {
  try {
    return text.toLocaleUpperCase(language);
  } catch {
    return text.toUpperCase();
  }
};

const paintColumns = (locale: PaintThicknessLocale, language: string): LookupColumn<PaintRow>[] => [
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
    icon: CameraSolidIcon,
    value: row => (rowHasPhotos(row) ? photoCountText(rowPhotoCount(row), locale, language) : locale.noPhotos),
  },
];

const settlePhoto = (image: HTMLImageElement, state: 'ready' | 'failed') => {
  const photo = image.closest('.paint-photo');
  if (!photo || photo.getAttribute('data-state') !== 'loading') return;
  photo.setAttribute('data-state', state);
  if (state === 'failed') photo.setAttribute('disabled', '');
};

const settleIfComplete = (image: HTMLImageElement | undefined) => {
  if (!image) return;
  queueMicrotask(() => {
    if (image.complete === true && image.naturalWidth > 0) settlePhoto(image, 'ready');
  });
};

export type OpenPhoto = (image: HTMLImageElement, src: string, label: string) => void;

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
      <PictureOffIcon class="paint-photo-glyph" />
    </span>
  </button>
);

type PhotosProps = { images: string[]; name: string; locale: PaintThicknessLocale; onOpenPhoto?: OpenPhoto; skeleton?: boolean };

const Thumbs = ({ images, name, locale, onOpenPhoto, skeleton = false }: PhotosProps) => (
  <div class={{ 'paint-thumbs': true, 'shift-skeleton': skeleton }}>
    {images.map((src, index) => (
      <Photo src={src} label={fill(locale.openPhotoLabel, { index: index + 1, total: images.length, name })} onOpenPhoto={onOpenPhoto} />
    ))}
  </div>
);

const sideCaption = (locale: PaintThicknessLocale, side: Side, value: number, skeleton: boolean): VNode => (
  <div class="paint-side-caption">
    {sideLabel(locale, side)} ·{' '}
    <span class={{ 'paint-figure': true, 'shift-skeleton': skeleton }}>
      {formatReading(value)} {locale.unit}
    </span>
  </div>
);

export const rowPhotosContent = (row: PaintRow, locale: PaintThicknessLocale, onOpenPhoto?: OpenPhoto, skeleton = false): VNode => (
  <div class="paint-sides">
    {readingsOf(row).map(reading => (
      <div class="paint-side">
        {sideCaption(locale, reading.side, reading.value, skeleton)}
        {reading.images.length ? (
          <Thumbs images={reading.images} name={photoGroupName(locale, row, reading.side)} locale={locale} onOpenPhoto={onOpenPhoto} skeleton={skeleton} />
        ) : (
          <span class={{ 'paint-note': true, 'shift-skeleton': skeleton }}>{locale.noPhotos}</span>
        )}
      </div>
    ))}
  </div>
);

const drawerContent = (row: PaintRow, locale: PaintThicknessLocale, language: string, onOpenPhoto?: OpenPhoto): VNode[] => [
  <div class="paint-drawer-head">
    <span class="paint-drawer-name">{rowName(locale, row)}</span>
    <span class="paint-note">{photoCountText(rowPhotoCount(row), locale, language)}</span>
  </div>,
  rowPhotosContent(row, locale, onOpenPhoto),
];

const TABPANEL_ID = 'paint-tabpanel';
const TAB_ID_PREFIX = 'paint-tab';
const tabId = (index: number) => `${TAB_ID_PREFIX}-${index}`;

export type PaintSheet = { index: number; open: boolean };

export const sheetPhotosTitle = (row: PaintRow, locale: PaintThicknessLocale, language: string): string =>
  fill(locale.sheetPhotosTitle, { name: rowName(locale, row), photos: photoCountText(rowPhotoCount(row), locale, language) });

type Props = {
  locale: PaintThicknessLocale;
  language: string;
  direction: string;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  busy: boolean;
  leaving: boolean;
  reconfiguring: boolean;
  reshaping: boolean;
  swapping: boolean;
  retainedBody: string;
  retainedPlaceholder: PlaceholderKind | null;
  selectedTab: number;
  retainedTabs: PaintTab[];
  retainedCertificate: CertificateSnapshot | null;
  retainedSummary: ReadingSummary | null;
  slotRows: PaintRow[];
  slots: RowSlot[];
  drawerOpen: number | null;
  drawerShown: readonly number[];
  drawerRowFor: (row: PaintRow, index: number) => PaintRow;
  onToggleDrawer: (index: number) => void;
  photosOpen: boolean;
  photosRendered: boolean;
  retainedGroups: PhotoGroup[];
  onTogglePhotos: () => void;
  onSelectTab: (index: number) => void;
  certificateOpen: boolean;
  onToggleCertificate: () => void;
  onCloseCertificate: (restoreFocus: boolean) => void;
  onOpenPhoto?: OpenPhoto;
  sheetMode?: boolean;
  sheet?: PaintSheet | null;
  onCloseSheet?: () => void;
  onSheetChange?: (sheet: boolean) => void;
};

const PHOTOS_REGION_ID = 'paint-photos-region';
const DRAWER_PREFIX = 'paint';

// The row's photo toggle, found in the table by its slot: in dialog mode a toggle carries no id to point at.
const photoToggle = (index: number) => `.paint-table .lt-rows > :nth-child(${index + 1}) .lt-action-button`;

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
    sheetMode = false,
    sheet = null,
    onCloseSheet = () => {},
    onSheetChange = () => {},
  } = props;

  const photosShown = !state.hidden.has('photos');
  const columns = visibleColumns(paintColumns(locale, language), state.hidden);

  const body = panelBody(state);
  const open = bodyOpen(state, leaving, reshaping);
  const placeholder = panelPlaceholder(state, leaving);

  const rows = shownRows(state);
  const summaryNow = state.showSummary ? summaryOf(rows) : null;
  const summary = summaryNow ?? retainedSummary;

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
        open: sheetMode ? null : drawerOpen,
        shown: sheetMode ? [] : drawerShown,
        onToggle: onToggleDrawer,
        idPrefix: DRAWER_PREFIX,
        dialog: sheetMode ? { open: sheet?.open ? sheet.index : null } : undefined,
      }
    : undefined;

  const rtl = direction === 'rtl';
  const count = headCount(state, locale, language);

  const sheetRow = sheet ? drawerRowFor(slotRows[sheet.index], sheet.index) : undefined;
  const sheetTitle = sheetRow ? sheetPhotosTitle(sheetRow, locale, language) : '';
  const onPopoverSheet = (event: CustomEvent<PopoverSheetChangeDetail>) => onSheetChange(event.detail.sheet);

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
      <div class="paint-head">
        <header class="lookup-head lookup-head-band paint-head-band">
          <div class="paint-main lookup-head-content">
            <p class="lookup-title paint-title">{headTitle(state, locale)}</p>
            <div class="paint-count-slot" data-empty={count ? 'false' : 'true'} aria-hidden={count ? null : 'true'}>
              <p class="paint-count">{count || ' '}</p>
            </div>
          </div>
          <div class="lookup-summary lookup-head-content">
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
                    aria-haspopup={sheetMode ? 'dialog' : 'menu'}
                    aria-expanded={certificateExpanded ? 'true' : 'false'}
                    aria-label={locale.printCertificate}
                    disabled={!certificateAreOpen || busy}
                    onClick={onToggleCertificate}
                    onKeyDown={(event: KeyboardEvent) => {
                      if (event.key !== 'ArrowDown' || certificateExpanded) return;
                      event.preventDefault();
                      onToggleCertificate();
                    }}
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

        <shift-popover
          class={{ 'paint-popover': true, 'paint-certificate-popover': true, 'paint-popover-sheet': sheetMode }}
          mobileSheet
          align="end"
          offset={6}
          anchorAria={false}
          open={certificateExpanded}
          anchor=".paint-certificate-button"
          label={locale.printCertificate}
          language={language as LanguageKeys}
          onOpenChange={(event: CustomEvent<{ open: boolean }>) => !event.detail.open && onCloseCertificate(false)}
          onSheetChange={onPopoverSheet}
        >
          <vehicle-paint-thickness-popup
            kind="menu"
            sheet={sheetMode}
            heading={locale.printCertificate}
            closeLabel={locale.closeSheet}
            busy={busy}
            direction={direction}
            links={certificate?.kind === 'menu' ? certificate.urls : []}
            choose={() => onCloseCertificate(false)}
            dismiss={() => onCloseCertificate(false)}
          />
        </shift-popover>
      </div>

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
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
                <CameraSolidIcon class="paint-photos-glyph" />
                <span class="paint-photos-label">{locale.allPhotos}</span>
                <span class="paint-photos-count shift-skeleton">{photoCountText(groupsPhotoCount(shownGroups), locale, language)}</span>
                <ChevronDownIcon class="paint-photos-chevron" />
              </button>

              <div id={PHOTOS_REGION_ID} class="paint-photos-region collapsible" data-open={photosBodyOpen ? 'true' : 'false'} aria-hidden={photosBodyOpen ? null : 'true'}>
                <div class="collapsible-body">
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

      <shift-popover
        class={{ 'paint-popover': true, 'paint-photos-popover': true, 'paint-popover-sheet': sheetMode }}
        mobileSheet
        anchorAria={false}
        inside="shift-image-layer"
        open={!!sheet?.open && !!sheetRow}
        anchor={sheet ? photoToggle(sheet.index) : undefined}
        label={sheetTitle || locale.allPhotos}
        language={language as LanguageKeys}
        onOpenChange={(event: CustomEvent<{ open: boolean }>) => !event.detail.open && onCloseSheet()}
        onSheetChange={onPopoverSheet}
      >
        <vehicle-paint-thickness-popup
          kind="photos"
          sheet={sheetMode}
          heading={sheetTitle}
          closeLabel={locale.closeSheet}
          busy={busy}
          direction={direction}
          row={sheetRow}
          locale={locale}
          openPhoto={onOpenPhoto}
          dismiss={onCloseSheet}
        />
      </shift-popover>
    </section>
  );
};
