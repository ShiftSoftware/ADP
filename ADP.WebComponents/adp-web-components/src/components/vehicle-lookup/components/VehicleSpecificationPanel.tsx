import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import specificationSchema from '~locales/vehicleLookup/specification/type';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { ArrowIcon } from '~assets/arrow-icon';

import { LookupHeadWait, PanelVerdict, StatusBadge } from '~features/vehicle-info-layout';

import { BADGE_GLYPHS } from './glyphs';

export type SpecificationLocale = InferType<typeof specificationSchema>;

/**
 * The barrier as a type: the only part of a `VehicleLookupDTO` this panel may see. Warranty,
 * campaigns, service history, sales, paint readings and accessories are other panels' questions.
 * `identifiers.brandID` is an internal hash id — never rendered, spoken, titled or logged.
 */
export type SpecificationRecord = Pick<VehicleLookupDTO, 'vin' | 'isAuthorized' | 'identifiers' | 'vehicleVariantInfo' | 'vehicleSpecification'>;

/** What the strip — the band between the head and the body — shows. */

/** Everything the panel's strip is decided from. The pill and the accent are the family's `recordVerdict`. */
export type SpecPanelState = {
  locale: SpecificationLocale;
  /** The five allowed sub-objects; `undefined` before any lookup and after a clear. */
  record?: SpecificationRecord;
  /** The lookup failed: the translated message, which the pill carries in the negative tone. */
  error?: string;
};

type Props = SpecPanelState & {
  /** A lookup is in flight, or the panel is being cleared: the head leaves the band and the strip goes to the skeleton. */
  loading: boolean;
  /** The pill's state and words. The accent is the owner's; the wrapper draws it. */
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
  /** The locale's BCP-47 tag (`sharedLocales.lang`), for the production date's month name. */
  lang: string;
  /** The groups this vehicle has, from `detailGroups` — empty when it has no tier-2 value at all. */
  groups: DetailGroup[];
  /**
   * Kept while the block is shut, so the groups slide away rather than blink out.
   */
  retainedGroups: DetailGroup[];
  /** The reader's choice, not the vehicle's: it survives a lookup and is reset only by `clearData()`. */
  detailsOpen: boolean;
  onToggleDetails: () => void;
};

/**
 * Trim, and treat the feed's padded `" 0 "` as empty for `numeric` fields (cylinders, doors,
 * tankCap, fuelLiter) — a placeholder, not a figure. Nothing else is rewritten: a sentinel the
 * record carries (`UNKNOWN`) is shown as sent, and the dash is reserved for an empty field.
 */
export const normalise = (raw: string | number | null | undefined, numeric = false): string => {
  if (raw === null || raw === undefined) return '';

  const text = String(raw).trim();
  if (!text) return '';
  if (!numeric) return text;

  const parsed = Number(text);
  return Number.isFinite(parsed) && parsed === 0 ? '' : text;
};

/**
 * `vehicleVariantInfo.modelYear` wins — the API states that precedence and already falls back to
 * the record's column itself. The panel adds the one case the API cannot: no variant info at all.
 * On a disagreement both show (parse as the value, column as a note): the panel cannot say which is
 * the typo, and the year decides parts and campaign eligibility.
 */
export const modelYearOf = (record?: SpecificationRecord): { year?: number; recordYear?: number } => {
  const parsed = record?.vehicleVariantInfo?.modelYear ?? undefined;
  const entry = record?.vehicleSpecification?.modelYear ?? undefined;

  if (parsed === undefined) return { year: entry };
  return parsed === entry || entry === undefined ? { year: parsed } : { year: parsed, recordYear: entry };
};

/**
 * The head's subject: the resolved description, else a code standing in for a name — still the
 * honest answer. Set in the title's typography, not the code role's, which shouts at title size.
 */
export const headValue = (record?: SpecificationRecord): string =>
  normalise(record?.vehicleSpecification?.modelDescription) ||
  normalise(record?.vehicleVariantInfo?.modelCode) ||
  normalise(record?.vehicleSpecification?.modelCode) ||
  normalise(record?.identifiers?.katashiki);

/**
 * Every value the panel would draw, normalised. `vin` and `brandID` are deliberately absent.
 */
const recordValues = (record?: SpecificationRecord): string[] => {
  const spec = record?.vehicleSpecification;
  const variant = record?.vehicleVariantInfo;
  const identifiers = record?.identifiers;
  const { year } = modelYearOf(record);

  return [
    normalise(spec?.modelDescription),
    normalise(variant?.modelCode),
    normalise(spec?.modelCode),
    normalise(identifiers?.katashiki),
    normalise(spec?.variantDescription),
    normalise(identifiers?.variant),
    year === undefined ? '' : String(year),
    normalise(spec?.productionDate),
    normalise(variant?.sfx),
    normalise(identifiers?.color),
    normalise(spec?.exteriorColor),
    normalise(identifiers?.trim),
    normalise(spec?.interiorColor),
    normalise(spec?.engine),
    normalise(spec?.engineType),
    normalise(spec?.cylinders, true),
    normalise(spec?.fuel),
    normalise(spec?.tankCap, true) || normalise(spec?.fuelLiter, true),
    normalise(spec?.transmission),
    normalise(spec?.class),
    normalise(spec?.bodyType),
    normalise(spec?.style),
    normalise(spec?.doors, true),
    normalise(spec?.side),
    normalise(spec?.lightHeavyType),
  ];
};

/**
 * Whether the distributor holds a record — **by value, not by object**. The evaluators build these
 * objects for every VIN, so an unknown one arrives as `{}`; testing the objects painted the accent
 * green over a blank card, which is a verdict read off an absence.
 */
export const hasRecords = (record?: SpecificationRecord): boolean => recordValues(record).some(Boolean);

/**
 * The notice band: a statement that stands above the record, not inside it. One case today; the
 * shape is general so a second reads the same way. Shut while a lookup is in flight and before the
 * first — a notice is about one vehicle and must not hang over the next.
 */
export const panelNotice = (state: { authorized?: boolean }, busy: boolean, locale: SpecificationLocale): { open: boolean; tone: 'neutral'; message: string } => ({
  open: !busy && state.authorized === false,
  tone: 'neutral',
  message: locale.unauthorizedNotice,
});

/**
 * One labelled value: `text`, an optional smaller `note`, or `colour` for the two code+name cells.
 */
export type SpecCell = {
  key: string;
  label: string;
  /** The typographic role: a code, a figure, or a word. An empty cell takes the grey dash's. */
  role: 'code' | 'figure' | 'word';
  text: string;
  note?: string;
  colour?: SpecColour;
};

/**
 * The code, and the name the distributor resolved for it. Nothing else: no invented colour, no
 * "unknown" label, and no depiction of the paint — a code alone knows neither model nor year.
 */
export type SpecColour = {
  code: string;
  name: string;
};

/** A tier-2 group and the cells that will actually render in it. */
export type DetailGroup = { key: 'powertrain' | 'body'; label: string; cells: SpecCell[] };

const same = (a: string, b: string) => !!a && a.trim().toUpperCase() === b.trim().toUpperCase();

/** Whether the record left this slot empty — a dash, not a blank. */
export const cellIsEmpty = (cell: SpecCell): boolean => (cell.colour ? !cell.colour.code && !cell.colour.name : !cell.text);

/**
 * Intl locales for one of the family's four languages. Must be handed `sharedLocales.lang`, never
 * `.language`: "english" / "arabic" are valid subtags Intl has no data for, so it silently resolves
 * them to the runtime default. A list, not a tag, so Intl falls back on its own — Sorani (`ckb`) is
 * patchy on older Android WebViews and Arabic is a better last resort there than English.
 */
const INTL_LOCALES: Record<string, string[]> = {
  en: ['en'],
  ar: ['ar'],
  ku: ['ckb', 'ku', 'ar'],
  ru: ['ru'],
};

export const intlLocalesFor = (lang: string): string[] => INTL_LOCALES[lang] ?? INTL_LOCALES.en;

/**
 * Month and year: the record's date is a first-of-month, so printing a day would be a claim.
 */
export const productionMonth = (raw: string | undefined, lang: string): string => {
  const value = normalise(raw);
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  try {
    return date.toLocaleDateString(intlLocalesFor(lang), { year: 'numeric', month: 'long' });
  } catch {
    return '';
  }
};

/**
 * Tier 1 — the identity grid: the same eight titled slots in every state, only their values change.
 * `readable` false (no vehicle, an error, or no record) empties every cell; the renderer decides
 * whether that reads as a dash ("nothing here") or a blank ("no vehicle yet").
 */
export const exteriorColour = (record: SpecificationRecord | undefined): SpecColour => ({
  code: normalise(record?.identifiers?.color),
  // The name is the host's own backend's, and nothing else's. A reference table keyed by code
  // alone cannot name a car: 543 of 701 published codes carry more than one name, because a code
  // is unique only with the model and the year it was built under.
  name: normalise(record?.vehicleSpecification?.exteriorColor),
});

export const identityCells = (record: SpecificationRecord | undefined, locale: SpecificationLocale, lang: string, readable: boolean): SpecCell[] => {
  const spec = readable ? record?.vehicleSpecification : undefined;
  const variant = readable ? record?.vehicleVariantInfo : undefined;
  const identifiers = readable ? record?.identifiers : undefined;
  const { year, recordYear } = readable ? modelYearOf(record) : {};

  // The head's label is "Model" and carries the description; this cell carries the code. Where the
  // two resolve to the same string — a catalogue with no separate code, or a vehicle whose
  // description never resolved and whose head therefore shows the code — the cell dashes rather
  // than print one string under two labels.
  const modelCode = normalise(variant?.modelCode) || normalise(spec?.modelCode);

  return [
    { key: 'modelCode', label: locale.modelCode, role: 'code', text: same(modelCode, headValue(readable ? record : undefined)) ? '' : modelCode },
    { key: 'variant', label: locale.variant, role: 'code', text: normalise(identifiers?.variant) },
    { key: 'katashiki', label: locale.katashiki, role: 'code', text: normalise(identifiers?.katashiki) },
    {
      key: 'modelYear',
      label: locale.modelYear,
      role: 'figure',
      // Four digits, never a locale grouping separator: a year is not a quantity.
      text: year === undefined ? '' : String(year),
      note: recordYear === undefined ? undefined : `${locale.recordYearNote} ${recordYear}`,
    },
    { key: 'productionDate', label: locale.productionDate, role: 'figure', text: productionMonth(spec?.productionDate, lang) },
    { key: 'sfx', label: locale.sfx, role: 'code', text: normalise(variant?.sfx) },
    // The paint code and the trim code are `identifiers`, the same object as the variant and the
    // katashiki: they *are* identity, and the paint is the first thing an advisor uses to find the
    // car in the lot. Both read as the code and whatever the distributor resolved for it.
    { key: 'exteriorColour', label: locale.exteriorColour, role: 'code', text: '', colour: exteriorColour(readable ? record : undefined) },
    { key: 'interiorColour', label: locale.interiorColour, role: 'code', text: '', colour: { code: normalise(identifiers?.trim), name: normalise(spec?.interiorColor) } },
  ];
};

/**
 * Tier 2 — the other twelve fields in two groups, empty fields and empty groups dropped. The
 * variable structure, so it lives in a `.collapsible`. The summary row reads this same result, so
 * its promise cannot drift from what opening it delivers.
 *
 * Coded values render verbatim: `side` reads `LHD` or `1`, `fuel` `Petrol` or `P`. The panel is not
 * a decoder — expanding `1` would be a guess at a coding it has never been told.
 */
export const detailGroups = (record: SpecificationRecord | undefined, locale: SpecificationLocale, readable: boolean): DetailGroup[] => {
  const spec = readable ? record?.vehicleSpecification : undefined;

  const bodyType = normalise(spec?.bodyType);
  const style = normalise(spec?.style);
  // One cell for two fields: both are litres, and `fuelLiter` is not populated by the evaluator
  // today — the fallback is there for a host that fills it.
  const capacity = normalise(spec?.tankCap, true) || normalise(spec?.fuelLiter, true);

  const groups: DetailGroup[] = [
    {
      key: 'powertrain',
      label: locale.powertrain,
      cells: [
        // Verbatim after the trim. A bare number is never suffixed "cc": the feed does not say what
        // unit it is.
        { key: 'engine', label: locale.engine, role: 'word', text: normalise(spec?.engine) },
        { key: 'engineType', label: locale.engineType, role: 'word', text: normalise(spec?.engineType) },
        { key: 'cylinders', label: locale.cylinders, role: 'figure', text: normalise(spec?.cylinders, true) },
        { key: 'fuel', label: locale.fuel, role: 'word', text: normalise(spec?.fuel) },
        { key: 'fuelCapacity', label: locale.fuelCapacity, role: 'figure', text: capacity ? `${capacity} ${locale.litreUnit}` : '' },
        { key: 'transmission', label: locale.transmission, role: 'word', text: normalise(spec?.transmission) },
      ],
    },
    {
      key: 'body',
      label: locale.body,
      cells: [
        { key: 'class', label: locale.class, role: 'word', text: normalise(spec?.class) },
        { key: 'bodyType', label: locale.bodyType, role: 'word', text: bodyType },
        // One name per object: a style that repeats the body type is dropped rather than shown twice.
        { key: 'style', label: locale.style, role: 'word', text: same(style, bodyType) ? '' : style },
        { key: 'doors', label: locale.doors, role: 'figure', text: normalise(spec?.doors, true) },
        { key: 'steering', label: locale.steering, role: 'word', text: normalise(spec?.side) },
        { key: 'vehicleType', label: locale.vehicleType, role: 'word', text: normalise(spec?.lightHeavyType) },
      ],
    },
  ];

  return groups.map(group => ({ ...group, cells: group.cells.filter(cell => !cellIsEmpty(cell)) })).filter(group => group.cells.length > 0);
};

/**
 * What the row promises: the groups that will render and the count of cells that will appear. The
 * plural word is the locale's, composed with the figure — never a suffix on a stem.
 */
export const detailsSummary = (groups: DetailGroup[], locale: SpecificationLocale): { names: string; count: string } => {
  const count = groups.reduce((total, group) => total + group.cells.length, 0);

  return {
    names: groups.map(group => group.label).join(' · '),
    count: `— ${count} ${count === 1 ? locale.detailsOne : locale.detailsMany}`,
  };
};

/**
 * One labelled value, in the labelled-card form at every width: a record's fields are labelled
 * values, not columns. `blank` is the idle case — the box is kept with a non-breaking space, since
 * a dash would claim the record is empty for a vehicle that does not exist yet.
 *
 * `shift-skeleton` goes on this block and nothing else, with `resize-settle` beside it so the block
 * eases between the old and new value's height while the cover is up.
 */
const SpecCellView: FunctionalComponent<{ cell: SpecCell; blank: boolean; key?: string }> = ({ cell, blank }) => {
  const empty = cellIsEmpty(cell);

  return (
    <div class="spec-cell" data-label={cell.label}>
      {/* A real element rather than a ::before caption: generated content is announced by most
          screen readers, so the CSS caption plus the sr-only span the language suggests would be
          heard twice. The data-label stays as the stable hook. */}
      <span class="spec-cell-label">{cell.label}</span>

      <span class="spec-value shift-skeleton resize-settle" data-role={empty ? 'empty' : cell.role}>
        <span class="spec-value-content" data-empty={blank ? 'true' : 'false'}>
          {blank ? ' ' : <SpecCellValue cell={cell} />}
        </span>
      </span>
    </div>
  );
};

const SpecCellValue: FunctionalComponent<{ cell: SpecCell }> = ({ cell }) => {
  if (cellIsEmpty(cell)) return <span>—</span>;

  if (cell.colour) {
    const { code, name } = cell.colour;

    return (
      <span class="spec-colour">
        {!!code && <code class="spec-colour-code">{code}</code>}
        {!!code && !!name && <span class="spec-colour-separator">{' · '}</span>}
        {!!name && <span class="spec-colour-name">{name}</span>}
      </span>
    );
  }

  return (
    <span>
      <span class="spec-value-text">{cell.text}</span>
      {/* The two model years are columns of one record and the panel cannot adjudicate which is the
          typo: showing one would assert it, hiding the other would hide a fact that decides parts
          and campaign eligibility. No hue and no glyph — a data-quality fault is not a verdict. */}
      {!!cell.note && <span class="spec-value-note">{cell.note}</span>}
    </span>
  );
};

/** The head's statement: what the vehicle is, in the distributor's words. Empty in every state the panel may not speak for. */
export const headStatement = (record: SpecificationRecord | undefined, readable: boolean) => ({
  value: readable ? headValue(record) : '',
  year: readable ? modelYearOf(record).year : undefined,
  // The grade is the variant *description*. Never `identifiers.variant`: that is a code, and codes
  // are the body's — the head reads the words, the body reads the codes.
  grade: readable ? normalise(record?.vehicleSpecification?.variantDescription) : '',
});

/**
 * The build record for one vehicle, in the component design language.
 *
 * The head is the statement — "Model: TALORA · 2024", the grade beside it — because a head that
 * reads "Vehicle Specification" says nothing about the vehicle, and on a record panel the pill says
 * nothing when there are records to show. The strip names what is below it. The body is the
 * evidence: the codes the words were resolved from.
 *
 * Every state is a different content of the same three parts, and the panel owns no in-flight
 * treatment above the strip — the wrapper's `.loading` drops the head's content out of the band and
 * raises its spinner, so nothing here has a loading state of its own.
 */
export const VehicleSpecificationPanel: FunctionalComponent<Props> = props => {
  const { locale, record, error, loading, verdict, lang, groups, retainedGroups, detailsOpen, onToggleDetails } = props;

  const vehicleLoaded = !!record?.vin && !error;
  /**
   * Whether the panel may speak for this vehicle at all. An unauthorized VIN reads `—` everywhere
   * *whatever the DTO carries*: the distributor has no record of it, so nothing in the response is
   * the distributor's to assert.
   */
  const readable = vehicleLoaded && record?.isAuthorized !== false;
  const notice = panelNotice({ authorized: record?.isAuthorized }, loading, locale);
  const statement = headStatement(record, readable);
  const cells = identityCells(record, locale, lang, readable);

  // Two empties, two meanings: before any lookup the values keep their box with a blank, because a
  // dash would say "the record has nothing here" about a vehicle that does not exist yet.
  const blank = !vehicleLoaded;

  // The shell carries the summary row and the region; it shuts when the vehicle has no tier-2 value
  // at all, so "nothing renders" is true and it got there by sliding. The region inside it carries
  // the groups and answers to the reader. Both shut for a lookup, whatever the reader had chosen,
  // and both keep rendering the outgoing groups while they do.
  const shown = groups.length ? groups : retainedGroups;
  const shellOpen = groups.length > 0 && !loading;
  const regionOpen = shellOpen && detailsOpen;
  const summary = detailsSummary(shown, locale);

  return (
    <section class="spec-card" data-verdict={verdict.state} data-phase={loading ? 'busy' : 'settled'}>
      <header class="spec-head lookup-head-band">
        <div class="spec-main lookup-head-content">
          {/* One paragraph, so bidi orders the label, the value and the year for the locale without
              the panel doing anything; the colon and the middle dot are neutral characters. */}
          <p class="spec-title">
            <span class="spec-title-label">{locale.model}:</span> <strong class="spec-title-value">{statement.value || '—'}</strong>
            {statement.year !== undefined && <span class="spec-title-year">{` · ${statement.year}`}</span>}
          </p>

          {/* The grade is inline, not a second line, so the band is the family's 58px for every
              vehicle (owner, 2026-09-20). A vehicle without one keeps the slot and its flex share
              and simply goes invisible, so moving between vehicles never resizes the head. */}
          <div class="spec-grade-slot" data-empty={statement.grade ? 'false' : 'true'} aria-hidden={statement.grade ? null : 'true'}>
            <p class="spec-grade" title={statement.grade || null}>
              {statement.grade || ' '}
            </p>
          </div>
        </div>

        <div class="lookup-summary lookup-head-content">
          <StatusBadge state={verdict.state} text={verdict.text} />
        </div>

        <LookupHeadWait />
      </header>

      {/* lookup-slide: the region under the head that travels on a composite's tab switch, clipped
          so it passes under the head, never over it. A plain div, never a .collapsible — one
          transition list would replace the other depending on source order. */}
      <div class="lookup-slide-clip">
        <div class="spec-under-head lookup-slide">
          {/* Its own band above the strip, not a layer inside it: as a layer it replaced the
              "Identity" caption, so the section lost its title while the warning was up. */}
          <div class="spec-notice collapsible" data-open={notice.open ? 'true' : 'false'} data-tone={notice.tone} aria-hidden={notice.open ? null : 'true'}>
            <div class="collapsible-body">
              <p class="spec-notice-body" role="status">
                <svg class="notice-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
                  <path fill="currentColor" d={BADGE_GLYPHS.question} />
                </svg>
                <span>{notice.message}</span>
              </p>
            </div>
          </div>

          {/* The grid's title, and a fixed one: the eight labelled cells below are the same in every
              state, so it is never covered and never swapped for something else. */}
          <div class="spec-lead">
            <span class="spec-lead-caption-label">{locale.identity}</span>
          </div>

          {/* Tier 1 — the fixed structure. It never shuts: the same eight titled slots exist
              whatever the answer is, so in flight they are anchors with their values covered where
              they stand, and nothing but the values moves. */}
          <div class="spec-identity" role="group" aria-label={locale.identity}>
            {cells.map(cell => (
              <SpecCellView key={cell.key} cell={cell} blank={blank} />
            ))}
          </div>

          {/* Tier 2 — the variable structure, in two nested collapsibles. The shell carries the
              recessed band, its top rule, the summary row and the region, so a vehicle with no
              details has no block at all and got there by sliding rather than by vanishing. */}
          <div class="spec-details collapsible" data-open={shellOpen ? 'true' : 'false'} data-empty={groups.length ? 'false' : 'true'} aria-hidden={shellOpen ? null : 'true'}>
            <div class="collapsible-body">
              {/* The whole row is the control: the names and count are the promise it acts on, and
                  a 30px mark is a smaller target than the row it belongs to. */}
              <button
                type="button"
                class="spec-details-summary"
                aria-expanded={regionOpen ? 'true' : 'false'}
                aria-controls="spec-details-region"
                title={regionOpen ? locale.collapseDetails : locale.expandDetails}
                aria-label={regionOpen ? locale.collapseDetails : locale.expandDetails}
                onClick={onToggleDetails}
              >
                <span class="spec-details-caption">
                  <span class="spec-details-names">{summary.names}</span>
                  <span class="spec-details-count">{summary.count}</span>
                </span>

                {/* The SSC trace button's geometry, with a chevron rather than the question mark,
                    which here means "why this status?". A span: the row around it is the button. */}
                <span class="spec-details-button" aria-hidden="true">
                  <ArrowIcon class="spec-details-chevron" />
                </span>
              </button>

              <div id="spec-details-region" class="collapsible" data-open={regionOpen ? 'true' : 'false'} aria-hidden={regionOpen ? null : 'true'}>
                <div class="collapsible-body">
                  {shown.map(group => (
                    <section class="spec-group" data-group={group.key} key={group.key} role="group" aria-label={group.label}>
                      <span class="spec-group-label">{group.label}</span>
                      <div class="spec-group-grid">
                        {group.cells.map(cell => (
                          <SpecCellView key={cell.key} cell={cell} blank={false} />
                        ))}
                      </div>
                    </section>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </section>
  );
};
