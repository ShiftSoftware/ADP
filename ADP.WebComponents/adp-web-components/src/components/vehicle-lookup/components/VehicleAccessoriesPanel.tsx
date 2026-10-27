import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import { PackageIcon } from '~assets/package-icon';
import { QuestionMark } from '~assets/question-mark';
import { SearchIcon } from '~assets/search-icon';
import { TriangleAlertIcon } from '~assets/triangle-alert';
import accessoriesSchema from '~locales/vehicleLookup/accessories/type';
import { AccessoryDTO } from '~types/generated/vehicle-lookup/accessory-dto';

import { LookupHead, PanelVerdict } from '~features/vehicle-info-layout';
import {
  LeadKind,
  LookupColumn,
  LookupPager,
  LookupTable,
  NoticeTone,
  PageSize,
  PlaceholderKind,
  RowSlot,
  TablePlaceholder,
  isPaginated,
  placeholderKind,
  shownBody,
  visibleColumns,
} from '~features/lookup-table';

export type AccessoriesLocale = InferType<typeof accessoriesSchema>;

/**
 * Everything the panel is decided from, assembled by the owner. `hidden` is the *applied* set (the
 * owner swaps it while the body is shut), so a column change never reaches the render half-done.
 */
export type PanelState = {
  /** A vehicle is on screen and its lookup did not fail. */
  vehicleLoaded: boolean;
  /** `isAuthorized` of that vehicle; `false` means the distributor has no record of it. */
  authorized?: boolean;
  /** The DTO's list, whatever it holds: a list the distributor may not show is dropped by `listed`. */
  accessories?: AccessoryDTO[] | null;
  hidden: ReadonlySet<string>;
  /** The lookup failed: the sentence the strip says in the negative tone (the pill carries the same words). */
  error?: string;
};

export type PanelNotice = { tone: NoticeTone; text: string };

/** The hiddenFields names, in column order: the host's whole vocabulary for this panel. */
export const FIELDS = ['partNumber', 'description', 'image'] as const;

/** What a host sees without configuring anything: every column. */
export const DEFAULT_HIDDEN_FIELDS = '';

/** Where the reader's choice is kept, per browser: the panel opens on it next time. */
export const PAGE_SIZE_STORAGE_KEY = 'shift-components:vehicle-accessories:page-size';

// ---------------------------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------------------------

/** The panel's columns, all of them; hiddenFields takes some away (`visibleColumns`). */
export const accessoryColumns = (locale: AccessoriesLocale): LookupColumn<AccessoryDTO>[] => [
  { field: 'partNumber', label: locale.partNumber, kind: 'code', identity: true, value: accessory => accessory.partNumber },
  { field: 'description', label: locale.description, kind: 'text', grow: true, value: accessory => accessory.description },
  { field: 'image', label: locale.image, kind: 'media', value: accessory => accessory.image },
];

/** How many columns a hidden set leaves. Names the panel does not know are ignored. */
export const columnCount = (hidden: ReadonlySet<string>): number => FIELDS.filter(field => !hidden.has(field)).length;

/** Whether two hidden sets leave the same columns: an unknown name changing is not a layout change. */
export const sameColumns = (a: ReadonlySet<string>, b: ReadonlySet<string>): boolean => FIELDS.every(field => a.has(field) === b.has(field));

// ---------------------------------------------------------------------------------------------
// The three derivations (design language § 20): what the strip says, what the body holds, the notice
// ---------------------------------------------------------------------------------------------

/**
 * The rows the panel may list. An unauthorized vehicle has none whatever the DTO holds: the list
 * is not the distributor's to show (vehicle-lookup-invariants.md), and a fixture that carries one
 * is a data fault the panel does not follow.
 */
export const listed = (state: PanelState): AccessoryDTO[] => (state.vehicleLoaded && state.authorized !== false ? (state.accessories ?? []) : []);

/** Whether the vehicle has accessories on record. Judged on the whole list, not on the rows a disclosure shows. */
export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

/** `rows` when there is a list and at least one column to draw it in; `none` otherwise. The body shuts for `none`. */
export const panelBody = (state: PanelState): 'rows' | 'none' => (hasRecords(state) && columnCount(state.hidden) > 0 ? 'rows' : 'none');

/**
 * The strip's layer: always the column headings (they depend on hiddenFields alone, never on the
 * data). An outcome without rows is said by the pill and the body's placeholder, never by covering the
 * headings. The one exception is a host that hid every column: there are no headings to show, and the
 * strip carries the sentence.
 */
export const panelLead = (state: PanelState, _busy: boolean): LeadKind => (columnCount(state.hidden) === 0 ? 'notice' : 'columns');

/**
 * The strip's sentence when there is nothing to list. Never a verdict: an unauthorized vehicle is
 * amber ("not in the records"), an authorized one with nothing on file is grey and says only that,
 * and a host that hid every column is told so in grey -- before a lookup too, since the headings it
 * would otherwise show do not exist. A failed lookup is the one negative sentence. Null above rows
 * and before a lookup.
 */
export const leadNotice = (state: PanelState, locale: AccessoriesLocale): PanelNotice | null => {
  if (state.error) return { tone: 'negative', text: state.error };
  if (!state.vehicleLoaded) return columnCount(state.hidden) === 0 ? { tone: 'idle', text: locale.noFieldsShown } : null;
  if (state.authorized === false) return { tone: 'neutral', text: locale.unauthorizedNotice };
  if (!hasRecords(state)) return { tone: 'idle', text: locale.noAccessories };
  if (columnCount(state.hidden) === 0) return { tone: 'idle', text: locale.noFieldsShown };
  return null;
};

/** It holds through a lookup: the state is still the last answer's until the next one lands. */
export const panelPlaceholder = (state: PanelState, leaving = false): PlaceholderKind | null =>
  placeholderKind({
    columns: columnCount(state.hidden),
    loaded: state.vehicleLoaded,
    failed: !!state.error,
    authorized: state.authorized,
    records: listed(state).length,
    leaving,
  });

export const placeholderContent = (locale: AccessoriesLocale): TablePlaceholder['content'] => ({
  idle: { icon: SearchIcon, text: locale.idleHint },
  empty: { icon: PackageIcon, text: locale.emptyHint },
  error: { icon: TriangleAlertIcon, text: locale.errorHint },
  unauthorized: { icon: QuestionMark, text: locale.unauthorizedHint },
});

// ---------------------------------------------------------------------------------------------
// The count, in real plural forms
// ---------------------------------------------------------------------------------------------

/**
 * The total, only when there is something to count: loaded, the distributor's own, at least one
 * accessory. Never for idle, unauthorized, an error or an empty list: "0 accessories" would be a
 * claim, and the empty list already has its statement. It does not move with hiddenFields or with
 * the disclosure: it is the vehicle's total, not the rows on screen.
 */
export const totalCount = (state: PanelState): number | null => {
  const total = listed(state).length;
  return total > 0 ? total : null;
};

type PluralKey = 'accessoryCountZero' | 'accessoryCountOne' | 'accessoryCountTwo' | 'accessoryCountFew' | 'accessoryCountMany' | 'accessoryCountOther';

const PLURAL_KEYS: Record<Intl.LDMLPluralRule, PluralKey> = {
  zero: 'accessoryCountZero',
  one: 'accessoryCountOne',
  two: 'accessoryCountTwo',
  few: 'accessoryCountFew',
  many: 'accessoryCountMany',
  other: 'accessoryCountOther',
};

/** The language's plural category for a whole number; `one` / `other` if the runtime does not know the language. */
export const pluralCategory = (count: number, language: string): Intl.LDMLPluralRule => {
  try {
    return new Intl.PluralRules(language).select(count);
  } catch {
    return count === 1 ? 'one' : 'other';
  }
};

/** "9 accessories", in the locale's form for that number. A category the locale left blank falls back to `other`. */
export const countText = (count: number, locale: AccessoriesLocale, language: string): string => {
  const template = locale[PLURAL_KEYS[pluralCategory(count, language)]] || locale.accessoryCountOther;
  return template.replace('{count}', String(count));
};

// ---------------------------------------------------------------------------------------------
// Pages
// ---------------------------------------------------------------------------------------------

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

type Props = {
  locale: AccessoriesLocale;
  /** The language key, for plural rules. */
  language: string;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  /** A lookup is in flight or the panel is leaving: the head leaves; rows on screen go under the skeleton where they stand. */
  busy: boolean;
  /** The panel is leaving (cleared, or failing from a settled card): the body shuts over its rows. */
  leaving: boolean;
  /** A live hiddenFields change is in progress: the body is shut while the columns swap. */
  reconfiguring: boolean;
  /** The body's last kind, kept a settle after it shut so its content slides away instead of blinking out. */
  retainedBody: string;
  /** The strip's last sentence, kept so a notice that is leaving fades out with its words, not blank. */
  retainedNotice: PanelNotice | null;
  retainedPlaceholder: PlaceholderKind | null;
  /** The rows in the table's slots: the page on screen, then any rows that are leaving (the owner's). */
  slotRows: AccessoryDTO[];
  /** One per slot row: open or shut, and how its values are veiled (the owner's `tableSlots`). */
  slots: RowSlot[];
  page: number;
  pageSize: PageSize;
  onPage: (page: number) => void;
  onPageSize: (size: PageSize) => void;
  onOpenMedia?: (image: HTMLImageElement, src: string) => void;
};

/** Whether the body is open on rows: a list to show, not shutting for a clear, not swapping columns. In flight it stays open. */
export const bodyOpen = (state: PanelState, leaving: boolean, reconfiguring: boolean): boolean => !leaving && !reconfiguring && panelBody(state) === 'rows';

export const VehicleAccessoriesPanel: FunctionalComponent<Props> = props => {
  const {
    locale,
    language,
    state,
    verdict,
    busy,
    leaving,
    reconfiguring,
    retainedBody,
    retainedNotice,
    retainedPlaceholder,
    slotRows,
    slots,
    page,
    pageSize,
    onPage,
    onPageSize,
    onOpenMedia,
  } = props;

  const columns = visibleColumns(accessoryColumns(locale), state.hidden);
  const total = listed(state).length;
  const count = totalCount(state);

  const body = panelBody(state);
  const lead = panelLead(state, busy);
  const open = bodyOpen(state, leaving, reconfiguring);
  const pagerOpen = open && isPaginated(total);

  const notice = leadNotice(state, locale) ?? retainedNotice;
  const placeholder = panelPlaceholder(state, leaving);

  return (
    <section class="accessories-card" data-verdict={verdict.state} data-phase={busy ? 'busy' : 'settled'} data-lead={lead} data-body={body}>
      <LookupHead title={locale.vehicleAccessories} verdict={verdict}>
        {/* A fact, not a verdict: it rides in the head's content block, which leaves and returns with every lookup, so it never changes in view. */}
        {count !== null && (
          <span class="accessories-count" data-count={String(count)}>
            {countText(count, locale, language)}
          </span>
        )}
      </LookupHead>

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
          <div id="accessories-list">
            <LookupTable
              columns={columns}
              rows={slotRows}
              slots={slots}
              // Keyed by slot, not by part: a slot is a place on the card that stays while its values change.
              rowKey={(_, index) => `slot-${index}`}
              lead={lead}
              notice={notice}
              busy={busy}
              body={shownBody(body, retainedBody)}
              open={open}
              reshaping={reconfiguring}
              strings={{ columnsLabel: locale.columnsLabel, noImage: locale.noImage, imageFailed: locale.imageFailed, openImage: locale.openImage }}
              onOpenMedia={onOpenMedia}
              placeholder={{ kind: placeholder, retained: retainedPlaceholder, content: placeholderContent(locale) }}
            />
          </div>

          {/* The page controls. Always mounted, shut when the list fits on the smallest page: they slide with the body, never pop. */}
          <LookupPager
            strings={{ rowsPerPage: locale.rowsPerPage, pageAll: locale.pageAll, previousPage: locale.previousPage, nextPage: locale.nextPage, pageRange: locale.pageRange }}
            controls="accessories-list"
            page={page}
            pageSize={pageSize}
            total={total}
            open={pagerOpen}
            busy={busy}
            onPage={onPage}
            onPageSize={onPageSize}
          />
        </div>
      </div>
    </section>
  );
};
