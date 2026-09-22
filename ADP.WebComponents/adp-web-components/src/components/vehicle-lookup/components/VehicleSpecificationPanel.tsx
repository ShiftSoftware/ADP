import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import specificationSchema from '~locales/vehicleLookup/specification/type';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { ArrowIcon } from '~assets/arrow-icon';

import { ColourEntry } from '~features/colour-catalogue';
import { LookupHeadWait, PanelVerdict, StatusBadge } from '~features/vehicle-info-layout';

import { BADGE_GLYPHS } from './glyphs';

export type SpecificationLocale = InferType<typeof specificationSchema>;

/**
 * The only part of a `VehicleLookupDTO` this panel is allowed to see — the barrier, as a type, so a
 * field cannot cross it without changing something a reviewer reads.
 *
 * `vin` is the wrapper's screen-reader identifier and tells the panel a vehicle is loaded;
 * `isAuthorized` is the honesty rule; the other three carry the build record. Warranty, campaigns,
 * service history, sales, paint readings, accessories and service menus are other panels'
 * questions and are never read here — not to render, not to hint, not to derive a state.
 *
 * `identifiers.brandID` is an internal hash id from the host's identity system, not a
 * specification: it is never displayed, never spoken, never put in a title or a data attribute and
 * never logged. Its one legitimate use is as the key into a host's colour catalogue.
 */
export type SpecificationRecord = Pick<VehicleLookupDTO, 'vin' | 'isAuthorized' | 'identifiers' | 'vehicleVariantInfo' | 'vehicleSpecification'>;

/** What the strip — the band between the head and the body — shows. */
export type LeadKind = 'skeleton' | 'caption' | 'notice';

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
  /** The locale's language tag, for the production date's month name. */
  language: string;
  /**
   * This vehicle's brand's exterior paint table, already resolved through the host's `brandSlugs`
   * map — a plain code → colour table with no brand id in it, so the barrier holds by type. Absent
   * whenever the host configured no map, the brand is unmapped, or the chunk has not arrived, and
   * every one of those reads as "no catalogue", which is a complete state.
   */
  exteriorColours?: Record<string, ColourEntry>;
  /** The groups this vehicle has, from `detailGroups` — empty when it has no tier-2 value at all. */
  groups: DetailGroup[];
  /**
   * What the details block keeps rendering while it is shut: the last vehicle's groups, so they
   * slide away with the block instead of blinking out on the frame the data changed.
   */
  retainedGroups: DetailGroup[];
  /** The reader's choice, not the vehicle's: it survives a lookup and is reset only by `clearData()`. */
  detailsOpen: boolean;
  onToggleDetails: () => void;
};

/**
 * One trim, and for the three fields the feed writes a placeholder zero into, a zero is nothing.
 *
 * 1. null / undefined is empty; a string is trimmed, because the feed pads (`" 3500 "`, `" "`).
 * 2. An empty string is empty.
 * 3. `numeric` — for `cylinders`, `doors`, `tankCap` and `fuelLiter` — makes a value that parses to
 *    zero empty: the feed writes `" 0 "` where it has no figure, and a vehicle with zero doors is
 *    not a fact, it is a placeholder.
 * 4. Nothing else. Case is shown as sent, and a sentinel the record carries (`UNKNOWN`) is shown as
 *    sent too: mapping it to a dash would be the panel editing a record to mean something else, and
 *    the dash is reserved for a field the record left empty.
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
 * The model year, and the record's own figure when the two disagree.
 *
 * `vehicleVariantInfo.modelYear` wins. Both come from the same vehicle entry: the specification's
 * is the entry's `ModelYear` column copied through, and the variant info's is a positional parse of
 * the entry's variant code which the API *itself* falls back to the column for. The API has already
 * stated a precedence, and a panel that read the column first would contradict the API's own
 * derived field.
 *
 * The panel adds the one fallback the API cannot: when the parse gave up entirely — a variant of
 * another shape, so there is no `vehicleVariantInfo` at all — but the entry carries a year.
 *
 * When they disagree the cell shows both: the parse as the value, the column as a note. The two are
 * columns of one record and the panel cannot adjudicate which is the typo; showing one would assert
 * it, and hiding the other would hide a fact the advisor may need, since the model year decides
 * parts and campaign eligibility.
 */
export const modelYearOf = (record?: SpecificationRecord): { year?: number; recordYear?: number } => {
  const parsed = record?.vehicleVariantInfo?.modelYear ?? undefined;
  const entry = record?.vehicleSpecification?.modelYear ?? undefined;

  if (parsed === undefined) return { year: entry };
  return parsed === entry || entry === undefined ? { year: parsed } : { year: parsed, recordYear: entry };
};

/**
 * The head's answer to "what is this vehicle": the distributor's description if it resolved one,
 * else a code standing in for a name. A model code or a katashiki is still the honest answer when
 * no description was resolved, and it is shown in the title's typography rather than the code
 * role's — at title size the code role's weight would shout.
 */
export const headValue = (record?: SpecificationRecord): string =>
  normalise(record?.vehicleSpecification?.modelDescription) ||
  normalise(record?.vehicleVariantInfo?.modelCode) ||
  normalise(record?.vehicleSpecification?.modelCode) ||
  normalise(record?.identifiers?.katashiki);

/**
 * Every value the panel would draw for this vehicle, normalised — the barrier audit as code.
 * `vin` and `brandID` are deliberately absent: the VIN is the wrapper's, and `brandID` is an
 * internal key, so neither is a record of anything about the build.
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
 * Whether the distributor holds a build record for this vehicle — **by value, not by object**.
 *
 * The evaluators build `identifiers` and `vehicleSpecification` for every VIN, including one the
 * distributor has no entry for: those arrive as `{}`, and an empty object is not a record. Testing
 * the objects made the accent green over a blank card for the eight VINs in the fixtures that carry
 * no value at all — a verdict read off an absence, which is the one thing the family forbids.
 */
export const hasRecords = (record?: SpecificationRecord): boolean => recordValues(record).some(Boolean);

/**
 * The strip: a flat skeleton before any lookup, while one is in flight and on an error; the amber
 * notice for a vehicle the distributor has no record of; otherwise the identity grid's caption.
 *
 * A caption rather than a notice for both the records and the no-records states, because records
 * are shown, not judged: there is no tone to say them in, the no-records fact is already said by
 * the pill in grey, and amber would claim the vehicle is unknown, which it is not.
 */
export const panelLead = (state: { vehicleLoaded: boolean; authorized?: boolean; error?: string }, busy: boolean): LeadKind => {
  if (busy || state.error || !state.vehicleLoaded) return 'skeleton';
  return state.authorized === false ? 'notice' : 'caption';
};

/**
 * One labelled value. `text` is the plain value and is empty when the record has nothing for the
 * slot; `note` is a second, smaller line inside the same block; `colour` replaces `text` for the two
 * cells that pair an identity code with a resolved name.
 */
export type SpecCell = {
  key: string;
  label: string;
  /** The typographic role: a code, a figure, or a word. An empty cell takes the grey dash's. */
  role: 'code' | 'figure' | 'word';
  text: string;
  note?: string;
  colour?: { code: string; name: string };
};

/** A tier-2 group and the cells that will actually render in it. */
export type DetailGroup = { key: 'powertrain' | 'body'; label: string; cells: SpecCell[] };

const same = (a: string, b: string) => !!a && a.trim().toUpperCase() === b.trim().toUpperCase();

/** Whether the record left this slot empty — a dash, not a blank. */
export const cellIsEmpty = (cell: SpecCell): boolean => (cell.colour ? !cell.colour.code && !cell.colour.name : !cell.text);

/**
 * Month and year in the locale's words.
 *
 * The production date in every record is a first-of-month: it is a month-granularity fact, and
 * printing a day the record does not mean would be a claim. Tolerant of a full timestamp.
 */
export const productionMonth = (raw: string | undefined, language: string): string => {
  const value = normalise(raw);
  if (!value) return '';

  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';

  try {
    return date.toLocaleDateString(language, { year: 'numeric', month: 'long' });
  } catch {
    return '';
  }
};

/**
 * Tier 1 — the identity grid: the same eight titled slots for every vehicle, in every state. This is
 * the fixed structure, so the slots never come and go; only their values change, under a cover.
 *
 * `readable` false (no vehicle, an error, or a vehicle the distributor has no record of) empties
 * every cell, and the renderer decides whether an empty cell reads as a dash or as a blank: a dash
 * says "the record has nothing here", a blank says "there is no vehicle yet", and the two must not
 * be confused.
 */
export const identityCells = (record: SpecificationRecord | undefined, locale: SpecificationLocale, language: string, readable: boolean): SpecCell[] => {
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
    { key: 'productionDate', label: locale.productionDate, role: 'figure', text: productionMonth(spec?.productionDate, language) },
    { key: 'sfx', label: locale.sfx, role: 'code', text: normalise(variant?.sfx) },
    // The paint code and the trim code are `identifiers`, the same object as the variant and the
    // katashiki: they *are* identity, and the paint is the first thing an advisor uses to find the
    // car in the lot. The resolved name is the backend's; a catalogue name is its fallback.
    { key: 'exteriorColour', label: locale.exteriorColour, role: 'code', text: '', colour: { code: normalise(identifiers?.color), name: normalise(spec?.exteriorColor) } },
    { key: 'interiorColour', label: locale.interiorColour, role: 'code', text: '', colour: { code: normalise(identifiers?.trim), name: normalise(spec?.interiorColor) } },
  ];
};

/**
 * Tier 2 — the details: the other twelve specification fields, in two groups, with every empty
 * field and every empty group dropped. This is the variable structure, so it lives in a
 * `.collapsible` and the mounts and unmounts happen while that region is shut.
 *
 * The summary row's names and count are read from this same result, so what the row promises cannot
 * drift from what opening it delivers.
 *
 * Coded values are rendered verbatim, after nothing but a trim: `side` reads `LHD` or `1`, `fuel`
 * reads `Petrol` or `P`, `class` reads `Sedan` or `P`. The panel is not a decoder and never guesses
 * — "LHD" expanded to "Left-hand drive" is a translation it cannot verify, and the feed that writes
 * `1` uses a coding it has never been told. A host whose feed is coded can make its feed say words.
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
 * What the summary row promises: the names of the groups that will actually render, and the count of
 * cells that will actually appear. The word is the locale's, composed with the figure — never a
 * suffix on a stem.
 */
export const detailsSummary = (groups: DetailGroup[], locale: SpecificationLocale): { names: string; count: string } => {
  const count = groups.reduce((total, group) => total + group.cells.length, 0);

  return {
    names: groups.map(group => group.label).join(' · '),
    count: `— ${count} ${count === 1 ? locale.detailsOne : locale.detailsMany}`,
  };
};

/**
 * One labelled value, in the language's labelled-card form — used at every width, because a record's
 * fields are labelled values, not columns: a one-row table would be a form pretending to be a table.
 *
 * `blank` is the idle case: the value keeps its box with a non-breaking space and its text pinned at
 * 0, because a dash beside a label would say "the record has nothing here" about a vehicle that does
 * not exist yet. A loaded vehicle with nothing in the slot gets the dash.
 *
 * `shift-skeleton` goes on this block and on nothing else — not the label, not the cell, not a
 * wrapper — and `resize-settle` beside it, so the block eases between the old value's height and the
 * new one while the cover is still up.
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
        {/*
         * SEAM for the colour catalogue (a separate change, landing after this one).
         *
         * What plugs in here, and nowhere else: a `span.spec-swatch` as the FIRST child of this
         * block — 16px, radius 4, `margin-inline-end: 6px`, the grey rim at .22, its `background`
         * the catalogue's `approxHex` set inline, `aria-hidden="true"`, and `title` carrying
         * `locale.swatchCaveat` (the key is already in all four locale files, unused until then).
         * A non-solid finish adds `data-finish` and the raised-band highlight over the rim.
         *
         * Everything it needs is already in place: the code and the name are here, the whole value
         * is one covered `.spec-value` block so the swatch arrives with its row and never on a beat
         * of its own, and `.resize-settle` already animates the block when the swatch makes it wrap.
         * It must NOT transition its background: a cross-fade would put a colour on screen that no
         * vehicle has.
         *
         * Not yet built here, deliberately: the `brandSlugs` prop, `slugOf`, the catalogue asset and
         * its lookup. Until they land the cell is code + resolved name, which is the honest
         * rendering of a code no catalogue has resolved — a complete state, not a degraded one.
         */}
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
  const { locale, record, error, loading, verdict, language, groups, retainedGroups, detailsOpen, onToggleDetails } = props;

  const vehicleLoaded = !!record?.vin && !error;
  /**
   * Whether the panel may speak for this vehicle at all. An unauthorized VIN reads `—` everywhere
   * *whatever the DTO carries*: the distributor has no record of it, so nothing in the response is
   * the distributor's to assert.
   */
  const readable = vehicleLoaded && record?.isAuthorized !== false;
  const lead = panelLead({ vehicleLoaded, authorized: record?.isAuthorized, error }, loading);
  const statement = headStatement(record, readable);
  const cells = identityCells(record, locale, language, readable);

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
          <div class="spec-lead layer-stack" data-lead={lead}>
            <div class="layer spec-lead-skeleton" data-active={lead === 'skeleton' ? 'true' : 'false'} aria-hidden="true" />

            <div class="layer spec-lead-caption" data-active={lead === 'caption' ? 'true' : 'false'} aria-hidden={lead === 'caption' ? null : 'true'}>
              <span class="spec-lead-caption-label">{locale.identity}</span>
            </div>

            {/* An anchor like the other two: mounted in every state, and its tone never changes in
                place — `panelLead` returns the skeleton while busy, so every change of tone is two
                cross-fades with the skeleton between them. */}
            <div class="layer spec-lead-notice is-neutral" data-active={lead === 'notice' ? 'true' : 'false'} role="status" aria-hidden={lead === 'notice' ? null : 'true'}>
              <svg class="notice-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
                <path fill="currentColor" d={BADGE_GLYPHS.question} />
              </svg>
              <span>{locale.unauthorizedNotice}</span>
            </div>
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
              <div class="spec-details-summary">
                {/* A heading for the region, not a label for the button: it reads the same open or
                    shut, so only the trigger's words and its chevron change. */}
                <span class="spec-details-caption">
                  <span class="spec-details-names">{summary.names}</span>
                  <span class="spec-details-count">{summary.count}</span>
                </span>

                {/* The language's outlined round trigger, the SSC's trace button's geometry,
                    colours, states, transitions and focus ring verbatim — with a chevron instead of
                    the question mark, which in this family means "why this status?", and with no
                    spinner, because nothing is fetched: the fields are already in the response. */}
                <button
                  type="button"
                  class="spec-details-button"
                  aria-expanded={regionOpen ? 'true' : 'false'}
                  aria-controls="spec-details-region"
                  title={detailsOpen ? locale.collapseDetails : locale.expandDetails}
                  aria-label={detailsOpen ? locale.collapseDetails : locale.expandDetails}
                  onClick={onToggleDetails}
                >
                  <ArrowIcon class="spec-details-chevron" />
                </button>
              </div>

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
