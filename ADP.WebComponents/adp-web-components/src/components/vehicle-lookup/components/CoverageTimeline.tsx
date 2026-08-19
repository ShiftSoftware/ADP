import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import warrantyTimelineSchema from '~locales/vehicleLookup/warrantyTimeline/type';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

type TimelineLocale = InferType<typeof warrantyTimelineSchema>;

type Tone = {
  base: string;
  dark: string;
  tint: string;
  border: string;
};

type Coverage = {
  id: string;
  kind: 'standard' | 'extended';
  label: string;
  start: string;
  end: string;
  tone: Tone;
  /** Alternates the border treatment so two neighbouring extended bands stay distinct. */
  alt: boolean;
  logo?: string;
  providerName?: string;
};

type Props = {
  isAuthorized: boolean;
  locale: TimelineLocale;
  vehicleInformation?: VehicleLookupDTO;
  /** Overrides the snapshot date. Supplied by tests so rendered positions are deterministic. */
  today?: string;
};

const TONE_STANDARD: Tone = {
  base: 'var(--green)',
  dark: 'var(--green-dark)',
  tint: 'rgba(232, 244, 237, 0.9)',
  border: 'rgba(44, 138, 88, 0.55)',
};

// Extended coverages cycle through these. The prototype hard-coded one class per
// provider; a vehicle can hold more than two, so the tone repeats and the segment's
// alternating border style keeps neighbouring bands apart.
const TONES_EXTENDED: Tone[] = [
  { base: 'var(--blue)', dark: 'var(--blue-dark)', tint: 'rgba(232, 241, 248, 0.88)', border: 'rgba(48, 113, 169, 0.64)' },
  { base: 'var(--violet)', dark: 'var(--violet-dark)', tint: 'rgba(240, 236, 247, 0.9)', border: 'rgba(117, 90, 165, 0.65)' },
];

const toTimestamp = (isoDate: string) => Date.parse(`${isoDate}T00:00:00Z`);

/** DTO dates are serialized as yyyy-MM-dd, but tolerate a full timestamp. */
const asDate = (value?: string) => (value || '').slice(0, 10);

const todayInUtc = () => new Date().toISOString().slice(0, 10);

const clampPercentage = (value: number) => Math.min(100, Math.max(0, value));

const asPercentage = (value: number) => `${value.toFixed(6)}%`;

const monthsBetween = (start: string, end: string) => {
  const [startYear, startMonth, startDay] = start.split('-').map(Number);
  const [endYear, endMonth, endDay] = end.split('-').map(Number);
  const months = (endYear - startYear) * 12 + (endMonth - startMonth) - (endDay < startDay ? 1 : 0);

  return Math.max(0, months);
};

const formatDuration = (months: number, locale: TimelineLocale) => {
  const years = Math.floor(months / 12);
  const remainingMonths = months % 12;
  const parts: string[] = [];

  if (years) parts.push(`${years} ${years === 1 ? locale.year : locale.years}`);
  if (remainingMonths || !years) parts.push(`${remainingMonths} ${remainingMonths === 1 ? locale.month : locale.months}`);

  return parts.join(' ');
};

const buildCoverages = (vehicleInformation: VehicleLookupDTO | undefined, locale: TimelineLocale): Coverage[] => {
  const warranty = vehicleInformation?.warranty;
  const coverages: Coverage[] = [];

  const standardStart = asDate(warranty?.warrantyStartDate);
  const standardEnd = asDate(warranty?.warrantyEndDate);

  if (standardStart && standardEnd && toTimestamp(standardEnd) > toTimestamp(standardStart))
    coverages.push({
      id: 'standard',
      kind: 'standard',
      label: locale.standardWarranty,
      start: standardStart,
      end: standardEnd,
      tone: TONE_STANDARD,
      alt: false,
    });

  (warranty?.extendedWarranties || [])
    .map(coverage => ({
      id: coverage?.id || '',
      name: coverage?.name || '',
      providerName: coverage?.providerCompanyName || '',
      logo: coverage?.providerCompanyLogo || '',
      start: asDate(coverage?.startDate),
      end: asDate(coverage?.endDate),
    }))
    .filter(coverage => coverage.start && coverage.end && toTimestamp(coverage.end) > toTimestamp(coverage.start))
    .sort((a, b) => toTimestamp(a.start) - toTimestamp(b.start))
    .forEach((coverage, index) => {
      coverages.push({
        id: coverage.id || `extended-${index}`,
        kind: 'extended',
        // The identifier is not a display string. A configured definition supplies a name;
        // a persisted entry has none, so those fall back to the generic label.
        label: coverage.name || locale.extendedWarranty,
        start: coverage.start,
        end: coverage.end,
        tone: TONES_EXTENDED[index % TONES_EXTENDED.length],
        alt: index % 2 === 1,
        logo: coverage.logo || undefined,
        providerName: coverage.providerName || undefined,
      });
    });

  return coverages;
};

/** Coverage name, qualified by the provider when one resolved. Used for the accessible text only. */
const describe = (coverage: Coverage) => (coverage.providerName ? `${coverage.label} · ${coverage.providerName}` : coverage.label);

const coverageStatus = (coverage: Coverage, today: string) => {
  const snapshot = toTimestamp(today);

  if (snapshot < toTimestamp(coverage.start)) return 'upcoming';
  if (snapshot >= toTimestamp(coverage.end)) return 'expired';

  return 'active';
};

/** Track drawn past the snapshot so the marker sits in the rail rather than on its edge. */
const RAIL_HEADROOM = 0.04;

/**
 * The span the rail draws. Coverage fixes one end; today is always inside it.
 *
 * A rail that stopped at the last expiry date would put "today" hard against its edge and lose the
 * one thing a lapsed warranty most needs to show — how long ago cover ran out. Drawing through to
 * today instead turns that into what it is: empty track, as wide as the time that has passed.
 */
const railRange = (coverages: Coverage[], today: string) => {
  const first = toTimestamp(coverages[0].start);
  const last = coverages.reduce((latest, coverage) => Math.max(latest, toTimestamp(coverage.end)), toTimestamp(coverages[0].end));
  const snapshot = toTimestamp(today);
  const headroom = Math.round((Math.max(last, snapshot) - Math.min(first, snapshot)) * RAIL_HEADROOM);

  return {
    from: first <= snapshot ? first : snapshot - headroom,
    to: last >= snapshot ? last : snapshot + headroom,
  };
};

const toneVariables = (tone: Tone) => ({
  '--tone': tone.base,
  '--tone-dark': tone.dark,
  '--tone-tint': tone.tint,
  '--tone-border': tone.border,
});

/** Clearance between two axis dates before they read as one smudge. */
const AXIS_GAP = 10;

/**
 * Hides the axis dates that would collide, always keeping the closing one.
 *
 * Crowding is not only a narrow-screen problem: consecutive coverages routinely hand over within a
 * week of each other on a rail spanning years, and those two labels overlap at any width. The
 * decision needs the measured width of rendered text, which the render pass cannot know, so it is
 * applied to the DOM afterwards and re-applied whenever the rail is resized.
 */
const thinAxis = (axis: HTMLElement | undefined) => {
  if (!axis) return;

  const apply = () => {
    const dates = Array.from(axis.children) as HTMLElement[];
    const railWidth = axis.clientWidth;

    // jsdom reports no layout, so specs leave every date alone rather than hiding all of them.
    if (dates.length < 2 || !railWidth) return;

    dates.forEach(date => date.classList.remove('is-crowded'));

    const last = dates.length - 1;

    const boxOf = (date: HTMLElement) => {
      const at = (parseFloat(date.style.getPropertyValue('--at')) / 100) * railWidth;
      const width = date.offsetWidth;

      // Mirrors the stylesheet: a date at the rail's start runs rightwards from its tick, one at
      // its end leftwards from its own, and everything between is centred on its tick.
      if (date.dataset.align === 'start') return [at, at + width];
      if (date.dataset.align === 'end') return [at - width, at];

      return [at - width / 2, at + width / 2];
    };

    const boxes = dates.map(boxOf);
    const survives = new Set([0, last]);
    let occupiedTo = boxes[0][1];

    for (let index = 1; index < last; index++) {
      const [left, right] = boxes[index];

      if (left < occupiedTo + AXIS_GAP) continue;
      if (right + AXIS_GAP > boxes[last][0]) continue;

      survives.add(index);
      occupiedTo = right;
    }

    // A rail too narrow to hold even the two bounds keeps the closing date: when the cover runs out
    // is the question the panel is answering.
    if (boxes[0][1] + AXIS_GAP > boxes[last][0]) survives.delete(0);

    dates.forEach((date, index) => date.classList.toggle('is-crowded', !survives.has(index)));
  };

  const schedule = () => (typeof requestAnimationFrame === 'function' ? requestAnimationFrame(apply) : apply());

  schedule();

  // Both triggers are needed and neither is enough on its own: the rail resizes without the dates
  // changing, and switching vehicle replaces the dates without the rail resizing.
  if ((axis as any).__axisWatched) return;
  (axis as any).__axisWatched = true;

  if (typeof ResizeObserver !== 'undefined') new ResizeObserver(schedule).observe(axis);
  if (typeof MutationObserver !== 'undefined') new MutationObserver(schedule).observe(axis, { childList: true });
};

/** Pulls the snapshot pill back onto the rail when centring it would overhang either edge. */
const positionTodayPill = (head: HTMLElement | undefined, todayPosition: number) => {
  if (!head) return;

  const apply = () => {
    const pill = head.firstElementChild as HTMLElement | null;
    if (!pill || !head.clientWidth) return;

    const pillCentre = (head.clientWidth * clampPercentage(todayPosition)) / 100;
    head.classList.toggle('is-near-end', pillCentre + pill.offsetWidth / 2 > head.clientWidth);
    head.classList.toggle('is-near-start', pillCentre - pill.offsetWidth / 2 < 0);
  };

  if (typeof requestAnimationFrame === 'function') requestAnimationFrame(apply);
  else apply();
};

// Inline rather than an ~assets import: the glyph takes its colour from the badge
// state via currentColor, which an <img src> cannot do.
const BADGE_GLYPHS = {
  positive:
    'M256 512A256 256 0 1 0 256 0a256 256 0 1 0 0 512zM369 209L241 337c-9.4 9.4-24.6 9.4-33.9 0l-64-64c-9.4-9.4-9.4-24.6 0-33.9s24.6-9.4 33.9 0l47 47L335 175c9.4-9.4 24.6-9.4 33.9 0s9.4 24.6 0 33.9z',
  negative:
    'M256 512A256 256 0 1 0 256 0a256 256 0 1 0 0 512zM175 175c9.4-9.4 24.6-9.4 33.9 0l47 47 47-47c9.4-9.4 24.6-9.4 33.9 0s9.4 24.6 0 33.9l-47 47 47 47c9.4 9.4 9.4 24.6 0 33.9s-24.6 9.4-33.9 0l-47-47-47 47c-9.4 9.4-24.6 9.4-33.9 0s-9.4-24.6 0-33.9l47-47-47-47c-9.4-9.4-9.4-24.6 0-33.9z',
};

/**
 * Why the warranty has not started. Possession is not a sale: while the distributor, an intermediary
 * or an un-invoiced broker still holds the vehicle, coverage deliberately has not begun and the
 * holder's invoice date is not used — the customer would lose that period off the front. The panel
 * says so rather than showing an unexplained empty rail.
 *
 * Only the broker case is derivable from the response; the supply-chain cases depend on the host's
 * own company classification, so the reason is read from the DTO. An older API that does not send
 * `startState` reads as started and shows nothing, exactly as before.
 *
 * `AwaitingActivation` is withheld from an unauthorized vehicle. That message says a step is
 * outstanding, and on a vehicle this dealer is not authorized for the step is never coming — the
 * card would be promising an activation that will not happen. The other two are statements about
 * where the vehicle sits in the supply chain, which is true no matter who is asking, so they stand.
 */
const notStartedMessage = (vehicleInformation: VehicleLookupDTO | undefined, locale: TimelineLocale, isAuthorized: boolean | undefined) => {
  const startState = vehicleInformation?.warranty?.startState;

  if (!vehicleInformation || !startState || startState === 'Started') return '';

  if (startState === 'AwaitingBrokerInvoice') return locale.awaitingBrokerInvoice + (vehicleInformation?.saleInformation?.broker?.brokerName || '');
  if (startState === 'AwaitingEndCustomerSale') return locale.awaitingEndCustomerSale;

  return isAuthorized === false ? '' : locale.awaitingActivation;
};

/**
 * The one row the card is allowed to gain and lose. The grid track animates the height rather than
 * the content, so the row still slides shut on the frame its message is cleared. Everything else
 * keeps its place and fades, which is what holds the card to a single height across vehicles.
 */
const Collapsible: FunctionalComponent<{ open: boolean }> = ({ open }, children) => (
  <div class="collapsible" data-open={open ? 'true' : 'false'} aria-hidden={open ? null : 'true'}>
    <div class="collapsible-body">{children}</div>
  </div>
);

const NoticeStrip = ({ text }: { text: string }) => (
  <div class="warranty-notice" role="status">
    <svg class="notice-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
      <path fill="currentColor" d={BADGE_GLYPHS.negative} />
    </svg>
    <span>{text}</span>
  </div>
);

type BadgeState = 'idle' | 'positive' | 'negative';

/**
 * A verdict, or the absence of one. Before a lookup there is no vehicle to judge, and an absent
 * `isAuthorized` reading as `false` opened the panel accusing every vehicle of being unauthorized
 * and out of cover. The idle chip keeps its box and its split cap so nothing shifts when the
 * verdict lands, and asserts nothing until it does — the treatment the older warranty panel
 * already gives its own cards.
 */
const StatusBadge = ({ state, text }: { state: BadgeState; text: string }) => {
  const idle = state === 'idle';

  return (
    <span class={`status-badge is-${state}`} aria-label={idle ? null : text} aria-hidden={idle ? 'true' : null}>
      <svg class="badge-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
        {!idle && <path fill="currentColor" d={state === 'positive' ? BADGE_GLYPHS.positive : BADGE_GLYPHS.negative} />}
      </svg>
      <span>{idle ? '' : text}</span>
    </span>
  );
};

export default function CoverageTimeline({ vehicleInformation, locale, isAuthorized, today }: Props) {
  const snapshot = today || todayInUtc();
  const coverages = buildCoverages(vehicleInformation, locale);
  const hasCoverage = coverages.length > 0;

  const dealerName = vehicleInformation?.saleInformation?.companyName || '';
  const notice = notStartedMessage(vehicleInformation, locale, isAuthorized);

  // The broker that actually anchored the warranty — a warranty fact, not a sale fact, so it is read
  // from the warranty DTO rather than inferred from the presence of a broker on the sale.
  const activatingBroker = vehicleInformation?.warranty?.activatedByBrokerName || '';

  // "Activated by" is only truthful once something started the coverage. Before that the company is
  // just the dealer holding it, and when a broker started it both parties matter.
  const dealerLabel = activatingBroker || notice ? locale.dealer : locale.activatedBy;

  const hasActiveWarranty = coverages.some(coverage => coverageStatus(coverage, snapshot) === 'active');

  // Both chips are verdicts about a vehicle, so neither can be given before there is one to judge.
  const verdict = (isPositive: boolean): BadgeState => (!vehicleInformation ? 'idle' : isPositive ? 'positive' : 'negative');

  const range = hasCoverage ? railRange(coverages, snapshot) : { from: 0, to: 1 };

  const positionInRange = (isoDate: string) => ((toTimestamp(isoDate) - range.from) / (range.to - range.from)) * 100;

  // The stripe is proportioned across the cover itself, not the rail. The rail stretches to reach
  // today, so sharing its scale would leave the stripe part-drawn, with the last band's colour
  // running out to the edge on any vehicle whose warranty has lapsed.
  const planFrom = hasCoverage ? toTimestamp(coverages[0].start) : 0;
  const planTo = hasCoverage ? coverages.reduce((latest, coverage) => Math.max(latest, toTimestamp(coverage.end)), planFrom + 1) : 1;
  const positionInPlan = (isoDate: string) => ((toTimestamp(isoDate) - planFrom) / (planTo - planFrom)) * 100;

  const ticks = hasCoverage ? Array.from(new Set(coverages.flatMap(coverage => [coverage.start, coverage.end]))).sort((a, b) => toTimestamp(a) - toTimestamp(b)) : [];

  // Inside the rail by construction; clamped anyway so malformed dates can never put an element at
  // `left: 190%`, which widens the rail to match and makes the card scroll sideways to reach it.
  const todayPosition = hasCoverage ? clampPercentage(positionInRange(snapshot)) : 0;

  const cardAccent = !hasCoverage
    ? // No bands means no coverage to describe, so the accent must not fall through to a
      // green/blue/violet gradient — that reads as a three-stage plan on a vehicle that has none.
      // Red when we are saying why coverage has not started, otherwise inert.
      notice
      ? 'var(--red)'
      : 'var(--line)'
    : coverages.length === 1
      ? TONE_STANDARD.base
      : `linear-gradient(90deg, ${coverages
          .map(coverage => `${coverage.tone.base} ${asPercentage(positionInPlan(coverage.start))} ${asPercentage(positionInPlan(coverage.end))}`)
          .join(', ')})`;

  const summary = coverages.map(coverage => `${describe(coverage)}: ${coverage.start} — ${coverage.end} (${coverageStatus(coverage, snapshot)})`).join('. ');

  // One structure for every vehicle. Blocks a vehicle has nothing to put in fade out in place
  // instead of being dropped, so moving between vehicles does not resize the card under the reader.
  return (
    <article class="coverage-timeline warranty-card" data-empty={hasCoverage ? 'false' : 'true'} style={{ '--card-accent': cardAccent }}>
      <header class="activation-header">
        <div class="activation-main">
          <p class="activation-title">
            <span>{dealerLabel}:</span> <strong>{dealerName || '—'}</strong>
          </p>

          {/* Holds its line whether or not a broker anchored the warranty, so moving between
              vehicles does not resize the header. Only the possession notice below is allowed to
              change the card's height, and it slides. */}
          <div class="broker-slot" data-empty={activatingBroker ? 'false' : 'true'} aria-hidden={activatingBroker ? null : 'true'}>
            <p class="activation-broker">
              <span>{locale.broker}:</span> <strong>{activatingBroker}</strong>
            </p>
          </div>
        </div>

        <div class="total-slot" data-empty={hasCoverage ? 'false' : 'true'} aria-hidden={hasCoverage ? null : 'true'}>
          <TotalCoverage coverages={coverages} locale={locale} />
        </div>
      </header>

      <Collapsible open={!!notice}>
        <NoticeStrip text={notice} />
      </Collapsible>

      <section class="journey" aria-label={locale.warrantyCoverage}>
        <div class="journey-head">
          <StatusBadge state={verdict(isAuthorized)} text={isAuthorized ? locale.authorized : locale.unauthorized} />
          <StatusBadge state={verdict(hasActiveWarranty)} text={hasActiveWarranty ? locale.activeWarranty : locale.notActiveWarranty} />
        </div>

        <div class="timeline-shell" data-empty={hasCoverage ? 'false' : 'true'} aria-hidden={hasCoverage ? null : 'true'}>
          <div class="timeline shift-skeleton" role="group" aria-label={locale.warrantyCoverage}>
            <div class="today-head" aria-hidden="true" style={{ '--at': asPercentage(todayPosition) }} ref={element => positionTodayPill(element, todayPosition)}>
              {hasCoverage && <span class="today-pill">{`${locale.today} · ${snapshot}`}</span>}
            </div>

            <div class="axis" aria-hidden="true" ref={thinAxis}>
              {ticks.map(tick => {
                const at = positionInRange(tick);

                // Ticks no longer necessarily reach the rail's ends — a lapsed warranty's dates all
                // sit left of today — so which way a date is set follows its position rather than
                // its place in the list.
                return (
                  <span class="axis-date" key={tick} data-align={at <= 0.5 ? 'start' : at >= 99.5 ? 'end' : 'mid'} style={{ '--at': asPercentage(at) }}>
                    {tick}
                  </span>
                );
              })}
            </div>

            <div class="lane">
              {hasCoverage && <span class="today-tail" aria-hidden="true" style={{ '--at': asPercentage(todayPosition) }} />}

              <div class="coverage-list" role="list">
                {coverages.map(coverage => {
                  const status = coverageStatus(coverage, snapshot);
                  const start = positionInRange(coverage.start);
                  const span = positionInRange(coverage.end) - start;

                  return (
                    <div
                      key={coverage.id}
                      role="listitem"
                      class="coverage-entry"
                      data-kind={coverage.kind}
                      data-status={status}
                      aria-label={`${describe(coverage)}, ${coverage.start} — ${coverage.end}`}
                      style={{ '--start': asPercentage(start), '--span': asPercentage(span), ...toneVariables(coverage.tone) }}
                    >
                      <div class={`segment ${coverage.kind === 'standard' ? 'is-standard' : 'is-extended'}${coverage.alt ? ' tone-alt' : ''}`}>
                        {/* Only the standard band carries text: it has no provider to show, and its
                            own mark is worded differently from the label beneath so the two do not
                            read as the same phrase twice. An extended band shows its provider's logo
                            or nothing at all — never the provider's name, which on a persisted
                            coverage is whichever company happened to store the row. The provider stays
                            in the entry's aria-label either way, so nothing is lost to a screen reader. */}
                        {coverage.kind === 'standard' ? (
                          <strong>{locale.standardWarrantyMark}</strong>
                        ) : coverage.logo ? (
                          // Decorative: the provider is already named in the entry's aria-label,
                          // so alt text here would announce it twice.
                          <img class="provider-logo" src={coverage.logo} alt="" loading="lazy" />
                        ) : null}
                      </div>

                      <span class="coverage-label">{coverage.label}</span>
                    </div>
                  );
                })}
              </div>

              {hasCoverage && <span class="past-wash" aria-hidden="true" style={{ '--to': asPercentage(todayPosition) }} />}

              <div class="boundary-layer" aria-hidden="true">
                {coverages.slice(1).map(coverage => (
                  <span key={coverage.id} class="boundary" style={{ '--at': asPercentage(positionInRange(coverage.start)), ...toneVariables(coverage.tone) }} />
                ))}
              </div>

              {hasCoverage && <span class="today-marker" aria-hidden="true" style={{ '--at': asPercentage(todayPosition) }} />}
            </div>

            {hasCoverage && <p class="sr-only">{summary}</p>}
          </div>
        </div>
      </section>
    </article>
  );
}

function TotalCoverage({ coverages, locale }: { coverages: Coverage[]; locale: TimelineLocale }) {
  const monthsFor = (kind: Coverage['kind']) =>
    coverages.filter(coverage => coverage.kind === kind).reduce((total, coverage) => total + monthsBetween(coverage.start, coverage.end), 0);

  const standardMonths = monthsFor('standard');
  const extendedMonths = monthsFor('extended');

  const mix = [
    standardMonths ? `${formatDuration(standardMonths, locale)} ${locale.standard}` : '',
    extendedMonths ? `${formatDuration(extendedMonths, locale)} ${locale.extended}` : '',
  ]
    .filter(Boolean)
    .join(' + ');

  // The block keeps its box on a vehicle with no coverage so the header does not resize, but it
  // reports no total rather than a total of zero — and a dash is narrow enough that it does not
  // reflow the label beside it, which is what made the invisible block taller than a real one.
  const total = mix ? formatDuration(standardMonths + extendedMonths, locale) : '—';

  return (
    <aside class="total-coverage" aria-label={`${locale.totalWarranty}: ${mix || total}`}>
      <span>{locale.totalWarranty}</span>
      <strong>{total}</strong>
      <span class="coverage-mix">{mix}</span>
    </aside>
  );
}
