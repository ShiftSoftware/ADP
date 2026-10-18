import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import accessoriesSchema from '~locales/vehicleLookup/accessories/type';
import { AccessoryDTO } from '~types/generated/vehicle-lookup/accessory-dto';

import { LookupHead, PanelVerdict } from '~features/vehicle-info-layout';
import { LeadKind, LookupColumn, LookupTable, NoticeTone, shownBody, tableLead, visibleColumns } from '~features/lookup-table';

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
};

export type PanelNotice = { tone: NoticeTone; text: string };

/** The hiddenFields names, in column order: the host's whole vocabulary for this panel. */
export const FIELDS = ['partNumber', 'description', 'image', 'jobNumber', 'invoiceNumber', 'companyName'] as const;

/** What a host sees without configuring anything: the three columns this panel always had. */
export const DEFAULT_HIDDEN_FIELDS = 'jobNumber,invoiceNumber,companyName';

/**
 * A list longer than this shows its first rows and offers the rest behind a button. A vehicle
 * carries a handful of accessories to about twenty; pages would hide rows from find-in-page and
 * print, so the rest is a disclosure, not a page (owner, 2026-10-18).
 */
export const COLLAPSED_ROWS = 10;

// ---------------------------------------------------------------------------------------------
// Columns
// ---------------------------------------------------------------------------------------------

/** The panel's columns, all of them; hiddenFields takes some away (`visibleColumns`). */
export const accessoryColumns = (locale: AccessoriesLocale): LookupColumn<AccessoryDTO>[] => [
  { field: 'partNumber', label: locale.partNumber, kind: 'code', identity: true, value: accessory => accessory.partNumber },
  { field: 'description', label: locale.description, kind: 'text', grow: true, value: accessory => accessory.description },
  { field: 'image', label: locale.image, kind: 'media', value: accessory => accessory.image },
  // Missing or null in the JSON when the accessory has none; the table renders either as a dash.
  { field: 'jobNumber', label: locale.jobNumber, kind: 'ref', value: accessory => accessory.jobNumber },
  { field: 'invoiceNumber', label: locale.invoiceNumber, kind: 'ref', value: accessory => accessory.invoiceNumber },
  { field: 'companyName', label: locale.companyName, kind: 'text', value: accessory => accessory.companyName },
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

/** The strip's layer: the skeleton while busy or idle, the headings above rows, a notice otherwise. */
export const panelLead = (state: PanelState, busy: boolean): LeadKind => tableLead({ busy, idle: !state.vehicleLoaded, body: panelBody(state) });

/**
 * The strip's sentence when there is nothing to list. Never a verdict: an unauthorized vehicle is
 * amber ("not in the records"), an authorized one with nothing on file is grey and says only that,
 * and a host that hid every column is told so in grey. Null above rows and before a lookup.
 */
export const leadNotice = (state: PanelState, locale: AccessoriesLocale): PanelNotice | null => {
  if (!state.vehicleLoaded) return null;
  if (state.authorized === false) return { tone: 'neutral', text: locale.unauthorizedNotice };
  if (!hasRecords(state)) return { tone: 'idle', text: locale.noAccessories };
  if (columnCount(state.hidden) === 0) return { tone: 'idle', text: locale.noFieldsShown };
  return null;
};

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
// The disclosure ("Show all 23 accessories")
// ---------------------------------------------------------------------------------------------

/** Whether the list is long enough to be collapsed. */
export const isDisclosable = (total: number): boolean => total > COLLAPSED_ROWS;

/** The rows the table is given: all of them, or the first `COLLAPSED_ROWS` while a long list is collapsed. */
export const visibleRows = (rows: AccessoryDTO[], showAll: boolean): AccessoryDTO[] => (showAll || !isDisclosable(rows.length) ? rows : rows.slice(0, COLLAPSED_ROWS));

/** The button's label while collapsed: it carries the number, in the count's own plural form. */
export const showAllText = (total: number, locale: AccessoriesLocale, language: string): string => locale.showAll.replace('{count}', countText(total, locale, language));

/**
 * Whether collapsing needs the page to follow. The rows that go were above the button, so it rises
 * by what they took; if that lifts it off the top of the screen, the reader is left looking at
 * nothing and the button's row is brought back (motion.md rule 10).
 */
export const collapseNeedsReveal = (buttonTop: number, removedHeight: number): boolean => buttonTop - removedHeight < 0;

// ---------------------------------------------------------------------------------------------
// Render
// ---------------------------------------------------------------------------------------------

// A chevron as stroked path data in the family's 512 box: it takes the button's colour and turns over on expand.
const CHEVRON_PATH = 'M112 192l144 144 144-144';

type Props = {
  locale: AccessoriesLocale;
  /** The language key, for plural rules. */
  language: string;
  state: PanelState;
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  /** A lookup is in flight or the panel is leaving: the strip sweeps and the body is shut. */
  busy: boolean;
  /** A live hiddenFields change is in progress: the body is shut while the columns swap. */
  reconfiguring: boolean;
  /** The body's last kind, kept a settle after it shut so its content slides away instead of blinking out. */
  retainedBody: string;
  /** The strip's last sentence, kept so a notice that is leaving fades out with its words, not blank. */
  retainedNotice: PanelNotice | null;
  /** The disclosure: the reader asked for every row. */
  expanded: boolean;
  /** Every row is rendered: expanded, or collapsing (the extra rows stay until the fold has shut). */
  showingAll: boolean;
  onToggle: () => void;
  onOpenMedia?: (image: HTMLImageElement, src: string) => void;
};

export const VehicleAccessoriesPanel: FunctionalComponent<Props> = props => {
  const { locale, language, state, verdict, busy, reconfiguring, retainedBody, retainedNotice, expanded, showingAll, onToggle, onOpenMedia } = props;

  const columns = visibleColumns(accessoryColumns(locale), state.hidden);
  const rows = listed(state);
  const total = rows.length;
  const count = totalCount(state);

  const body = panelBody(state);
  const lead = panelLead(state, busy);
  // The body shuts while a lookup is in flight, while columns swap, and whenever there is nothing to list.
  const open = !busy && !reconfiguring && body === 'rows';
  const moreOpen = open && isDisclosable(total);

  const notice = leadNotice(state, locale) ?? retainedNotice;

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
          {/* The fold: where a long list's height is eased when the rest of it is shown or put away (resize-settle). */}
          <div class="accessories-fold resize-settle" id="accessories-list">
            <LookupTable
              columns={columns}
              rows={visibleRows(rows, showingAll)}
              rowKey={(accessory, index) => `${accessory.partNumber ?? ''}#${index}`}
              lead={lead}
              notice={notice}
              busy={busy}
              body={shownBody(body, retainedBody)}
              open={open}
              reshaping={reconfiguring}
              strings={{ columnsLabel: locale.columnsLabel, noImage: locale.noImage, imageFailed: locale.imageFailed, openImage: locale.openImage }}
              onOpenMedia={onOpenMedia}
            />
          </div>

          {/* Always mounted, shut when there is nothing to disclose: it slides with the body, never pops. */}
          <div class="accessories-more collapsible" data-open={moreOpen ? 'true' : 'false'} aria-hidden={moreOpen ? null : 'true'}>
            <div class="collapsible-body">
              <div class="accessories-more-row">
                <button type="button" class="accessories-more-button" aria-expanded={expanded ? 'true' : 'false'} aria-controls="accessories-list" onClick={() => onToggle()}>
                  <svg class="accessories-more-chevron" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
                    <path d={CHEVRON_PATH} fill="none" stroke="currentColor" stroke-width="64" stroke-linecap="round" stroke-linejoin="round" />
                  </svg>
                  {/* Both labels share one cell and cross-fade, so the words never pop and the button never changes width. */}
                  <span class="accessories-more-labels">
                    <span class="accessories-more-label" data-active={expanded ? 'false' : 'true'} aria-hidden={expanded ? 'true' : null}>
                      {showAllText(total, locale, language)}
                    </span>
                    <span class="accessories-more-label" data-active={expanded ? 'true' : 'false'} aria-hidden={expanded ? null : 'true'}>
                      {locale.showFewer}
                    </span>
                  </span>
                </button>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
