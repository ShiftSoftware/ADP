import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import specificationSchema from '~locales/vehicleLookup/specification/type';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

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
export const VehicleSpecificationPanel: FunctionalComponent<Props> = (props, children) => {
  const { locale, record, error, loading, verdict } = props;

  const vehicleLoaded = !!record?.vin && !error;
  /**
   * Whether the panel may speak for this vehicle at all. An unauthorized VIN reads `—` everywhere
   * *whatever the DTO carries*: the distributor has no record of it, so nothing in the response is
   * the distributor's to assert.
   */
  const readable = vehicleLoaded && record?.isAuthorized !== false;
  const lead = panelLead({ vehicleLoaded, authorized: record?.isAuthorized, error }, loading);
  const statement = headStatement(record, readable);

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

          {children}
        </div>
      </div>
    </section>
  );
};
