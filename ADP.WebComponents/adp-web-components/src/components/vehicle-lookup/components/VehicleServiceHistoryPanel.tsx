import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import serviceHistorySchema from '~locales/vehicleLookup/serviceHistory/type';
import { VehicleServiceHistoryDTO } from '~types/generated/vehicle-lookup/vehicle-service-history-dto';

import { SearchIcon } from '~assets/search-icon';
import { CalendarDaysIcon } from '~assets/calendar-days-icon';

import { SharedLocales } from '~features/multi-lingual';
import { LookupHead, PanelVerdict, recordVerdict } from '~features/vehicle-info-layout';
import { LeadKind, LookupColumn, LookupTable, RowSlot, shownBody } from '~features/lookup-table';

export type ServiceHistoryLocale = InferType<typeof serviceHistorySchema>;

/** Everything the panel is decided from, assembled by the owner. */
export type PanelState = {
  /** A vehicle is on screen and its lookup did not fail. */
  vehicleLoaded: boolean;
  /** `isAuthorized` of that vehicle; `false` means the distributor has no record of it. */
  authorized?: boolean;
  /** The DTO's list, whatever it holds: a list the distributor may not show is dropped by `listed`. */
  visits?: VehicleServiceHistoryDTO[] | null;
  /** The lookup failed: the sentence the pill carries in the negative tone. */
  error?: string;
};

// ---------------------------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------------------------

/** Latin digits with grouping in every locale, like the VINs and dates beside them: "71,828". */
const MILEAGE_FORMAT = new Intl.NumberFormat('en-US', { maximumFractionDigits: 0 });

/** ISO `yyyy-MM-dd`, as the DTO sends it, tolerant of a full timestamp. */
export const visitDate = (visit: VehicleServiceHistoryDTO): string | null => visit.serviceDate?.slice(0, 10) ?? null;

/** The odometer, grouped. 0 is a reading; only a missing one is blank. */
export const visitMileage = (visit: VehicleServiceHistoryDTO): string | null =>
  typeof visit.mileage === 'number' && Number.isFinite(visit.mileage) ? MILEAGE_FORMAT.format(visit.mileage) : null;

/** A visit reads date → what was done → odometer → where; the date names it. */
export const serviceHistoryColumns = (locale: ServiceHistoryLocale): LookupColumn<VehicleServiceHistoryDTO>[] => [
  { field: 'serviceDate', label: locale.date, kind: 'date', identity: true, value: visitDate },
  { field: 'serviceType', label: locale.serviceType, kind: 'text', grow: true, value: visit => visit.serviceType },
  { field: 'mileage', label: locale.odometer, kind: 'number', value: visitMileage },
  { field: 'companyName', label: locale.dealer, kind: 'text', value: visit => visit.companyName },
  { field: 'branchName', label: locale.branch, kind: 'text', value: visit => visit.branchName },
  { field: 'invoiceNumber', label: locale.invoiceNumber, kind: 'code', value: visit => visit.invoiceNumber },
];

// ---------------------------------------------------------------------------------------------
// The derivations (design language § 20)
// ---------------------------------------------------------------------------------------------

/**
 * The visits the panel may list, in the data's order. An unauthorized vehicle has none whatever
 * the DTO holds: the list is not the distributor's to show (vehicle-lookup-invariants.md).
 */
export const listed = (state: PanelState): VehicleServiceHistoryDTO[] => (state.vehicleLoaded && state.authorized !== false ? (state.visits ?? []) : []);

export const hasRecords = (state: PanelState): boolean => listed(state).length > 0;

/** The pill and the accent bar: a record panel's (`recordVerdict`), judged on the visits it may list. */
export const panelVerdict = (state: PanelState, shared: SharedLocales): PanelVerdict =>
  recordVerdict({ locale: shared, error: state.error, vehicleLoaded: state.vehicleLoaded, authorized: state.authorized, hasRecords: hasRecords(state) });

/** `rows` when there are visits to list; `none` otherwise, and the body shuts. */
export const panelBody = (state: PanelState): 'rows' | 'none' => (hasRecords(state) ? 'rows' : 'none');

/** The strip always carries the headings: the columns are fixed, and an outcome without rows is said by the pill (owner, 2026-10-25). */
export const panelLead = (): LeadKind => 'columns';

/** Whether the body is open on rows. In flight it stays open: the rows stay under the skeleton (§ 21a). */
export const bodyOpen = (state: PanelState, leaving: boolean): boolean => !leaving && panelBody(state) === 'rows';

/**
 * What fills the space under the strip when there is no list: `idle` before a lookup, after a
 * failed one and for a vehicle not in the distributor's records (the pill says what happened);
 * `empty` for an authorized vehicle with no visits on file. It holds through a lookup.
 */
export const panelPlaceholder = (state: PanelState): 'idle' | 'empty' | null => {
  if (!state.vehicleLoaded || state.authorized === false) return 'idle';
  return hasRecords(state) ? null : 'empty';
};

// ---------------------------------------------------------------------------------------------
// The count, in real plural forms
// ---------------------------------------------------------------------------------------------

/** The number of visits, only when there is one to count: "0 visits" would be a claim the empty state already makes. */
export const totalCount = (state: PanelState): number | null => {
  const total = listed(state).length;
  return total > 0 ? total : null;
};

type PluralKey = 'visitCountZero' | 'visitCountOne' | 'visitCountTwo' | 'visitCountFew' | 'visitCountMany' | 'visitCountOther';

const PLURAL_KEYS: Record<Intl.LDMLPluralRule, PluralKey> = {
  zero: 'visitCountZero',
  one: 'visitCountOne',
  two: 'visitCountTwo',
  few: 'visitCountFew',
  many: 'visitCountMany',
  other: 'visitCountOther',
};

/** The language's plural category for a whole number; `one` / `other` if the runtime does not know the language. */
export const pluralCategory = (count: number, language: string): Intl.LDMLPluralRule => {
  try {
    return new Intl.PluralRules(language).select(count);
  } catch {
    return count === 1 ? 'one' : 'other';
  }
};

/** "51 visits", in the locale's form for that number. A category the locale left blank falls back to `other`. */
export const countText = (count: number, locale: ServiceHistoryLocale, language: string): string => {
  const template = locale[PLURAL_KEYS[pluralCategory(count, language)]] || locale.visitCountOther;
  return template.replace('{count}', String(count));
};

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

type Props = {
  locale: ServiceHistoryLocale;
  /** The language key, for plural rules. */
  language: string;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  /** A lookup is in flight or the panel is leaving: the head leaves; rows on screen go under the skeleton where they stand. */
  busy: boolean;
  /** The panel is leaving (cleared, or failing from a settled card): the body shuts over its rows. */
  leaving: boolean;
  /** The body's last kind, kept a settle after it shut so its rows slide away instead of blinking out. */
  retainedBody: string;
  /** The rows in the table's slots: the visits on screen, then any that are leaving (the owner's). */
  slotRows: VehicleServiceHistoryDTO[];
  /** One per slot row (the owner's `tableSlots`). */
  slots: RowSlot[];
};

export const VehicleServiceHistoryPanel: FunctionalComponent<Props> = ({ locale, language, state, verdict, busy, leaving, retainedBody, slotRows, slots }) => {
  const body = panelBody(state);
  const lead = panelLead();
  const open = bodyOpen(state, leaving);
  const count = totalCount(state);
  // A leave always ends idle, so the placeholder is the idle one for the whole leave: from rows it
  // opens while they shut, as one movement.
  const placeholder = leaving ? 'idle' : panelPlaceholder(state);

  return (
    <section class="service-history-card" data-verdict={verdict.state} data-phase={busy ? 'busy' : 'settled'} data-lead={lead} data-body={body}>
      <LookupHead title={locale.serviceHistory} verdict={verdict}>
        {/* A fact, not a verdict: it rides in the head's content block, which leaves and returns with every lookup. */}
        {count !== null && (
          <span class="service-history-count" data-count={String(count)}>
            {countText(count, locale, language)}
          </span>
        )}
      </LookupHead>

      <div class="lookup-slide-clip">
        <div class="lookup-slide">
          <LookupTable
            columns={serviceHistoryColumns(locale)}
            rows={slotRows}
            slots={slots}
            // Keyed by slot, not by visit: a slot is a place on the card that stays while its values change.
            rowKey={(_, index) => `slot-${index}`}
            lead={lead}
            notice={null}
            busy={busy}
            body={shownBody(body, retainedBody)}
            open={open}
            // No media column, so the picture strings are never read.
            strings={{ columnsLabel: locale.columnsLabel, noImage: '', imageFailed: '', openImage: '' }}
          />

          {/* Always mounted: it slides open and shut and its two sentences cross-fade. Its words go under
              the skeleton in flight, where "look a vehicle up" is not true. */}
          <div class="service-history-placeholder collapsible" data-open={placeholder ? 'true' : 'false'} aria-hidden={placeholder ? null : 'true'}>
            <div class="collapsible-body">
              <div class="service-history-placeholder-stack layer-stack">
                <div class="layer service-history-placeholder-layer" data-active={placeholder === 'empty' ? 'false' : 'true'} aria-hidden={placeholder === 'idle' ? null : 'true'}>
                  <span class="service-history-placeholder-mark" aria-hidden="true">
                    <SearchIcon class="service-history-placeholder-glyph" />
                  </span>
                  <p class="service-history-placeholder-text shift-skeleton">{locale.idleHint}</p>
                </div>
                <div class="layer service-history-placeholder-layer" data-active={placeholder === 'empty' ? 'true' : 'false'} aria-hidden={placeholder === 'empty' ? null : 'true'}>
                  <span class="service-history-placeholder-mark" aria-hidden="true">
                    <CalendarDaysIcon class="service-history-placeholder-glyph" />
                  </span>
                  <p class="service-history-placeholder-text shift-skeleton">{locale.emptyHint}</p>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
