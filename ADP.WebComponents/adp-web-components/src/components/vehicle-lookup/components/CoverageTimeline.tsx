import { h } from '@stencil/core';
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
  providerID?: string;
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
      providerID: coverage?.providerCompanyID || '',
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
        // The lookup DTO carries no display name for a coverage, so the identifier is
        // the only host-meaningful text available. Falls back to the generic label.
        label: coverage.id || locale.extendedWarranty,
        start: coverage.start,
        end: coverage.end,
        tone: TONES_EXTENDED[index % TONES_EXTENDED.length],
        alt: index % 2 === 1,
        logo: coverage.logo || undefined,
        providerID: coverage.providerID || undefined,
      });
    });

  return coverages;
};

const coverageStatus = (coverage: Coverage, today: string) => {
  const snapshot = toTimestamp(today);

  if (snapshot < toTimestamp(coverage.start)) return 'upcoming';
  if (snapshot >= toTimestamp(coverage.end)) return 'expired';

  return 'active';
};

const toneVariables = (tone: Tone) => ({
  '--tone': tone.base,
  '--tone-dark': tone.dark,
  '--tone-tint': tone.tint,
  '--tone-border': tone.border,
});

/**
 * Centres the snapshot marker once per data set. The timeline has a min-width and
 * scrolls horizontally on narrow hosts, so "today" would otherwise sit off-screen.
 */
const revealToday = (scroller: HTMLElement | undefined, todayPosition: number, key: string) => {
  if (!scroller || scroller.dataset.centeredOn === key) return;

  scroller.dataset.centeredOn = key;

  const centre = () => {
    const overflow = scroller.scrollWidth - scroller.clientWidth;

    if (overflow <= 0) {
      scroller.scrollLeft = 0;
      return;
    }

    const target = scroller.scrollWidth * (clampPercentage(todayPosition) / 100) - scroller.clientWidth / 2;
    scroller.scrollLeft = Math.min(Math.max(target, 0), overflow);
  };

  if (typeof requestAnimationFrame === 'function') requestAnimationFrame(centre);
  else centre();
};

/** Pulls the snapshot pill back onto the card when centring it would overflow the right edge. */
const positionTodayPill = (head: HTMLElement | undefined, todayPosition: number) => {
  if (!head) return;

  const apply = () => {
    const pill = head.firstElementChild as HTMLElement | null;
    if (!pill || !head.clientWidth) return;

    const pillCentre = (head.clientWidth * clampPercentage(todayPosition)) / 100;
    head.classList.toggle('is-near-end', pillCentre + pill.offsetWidth / 2 > head.clientWidth);
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

const StatusBadge = ({ isPositive, text }: { isPositive: boolean; text: string }) => (
  <span class={`status-badge ${isPositive ? 'is-positive' : 'is-negative'}`} aria-label={text}>
    <svg class="badge-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
      <path fill="currentColor" d={isPositive ? BADGE_GLYPHS.positive : BADGE_GLYPHS.negative} />
    </svg>
    <span>{text}</span>
  </span>
);

export default function CoverageTimeline({ vehicleInformation, locale, isAuthorized, today }: Props) {
  const snapshot = today || todayInUtc();
  const coverages = buildCoverages(vehicleInformation, locale);
  const dealerName = vehicleInformation?.saleInformation?.companyName || '';

  const header = (
    <header class="activation-header">
      <div class="activation-main">
        <p class="activation-title">
          <span>{locale.dealer}:</span> <strong>{dealerName || '—'}</strong>
        </p>
      </div>

      {coverages.length > 0 && <TotalCoverage coverages={coverages} locale={locale} />}
    </header>
  );

  const hasActiveWarranty = coverages.some(coverage => coverageStatus(coverage, snapshot) === 'active');

  const badges = (
    <div class="journey-head">
      <StatusBadge isPositive={isAuthorized} text={isAuthorized ? locale.authorized : locale.unauthorized} />
      <StatusBadge isPositive={hasActiveWarranty} text={hasActiveWarranty ? locale.activeWarranty : locale.notActiveWarranty} />
    </div>
  );

  if (coverages.length === 0)
    return (
      <article class="coverage-timeline warranty-card">
        {header}
        <section class="journey">{badges}</section>
      </article>
    );

  const rangeStart = coverages[0].start;
  const rangeEnd = coverages.reduce((latest, coverage) => (toTimestamp(coverage.end) > toTimestamp(latest) ? coverage.end : latest), coverages[0].end);
  const rangeSpan = toTimestamp(rangeEnd) - toTimestamp(rangeStart);

  const positionInRange = (isoDate: string) => ((toTimestamp(isoDate) - toTimestamp(rangeStart)) / rangeSpan) * 100;

  const ticks = Array.from(new Set(coverages.flatMap(coverage => [coverage.start, coverage.end]))).sort((a, b) => toTimestamp(a) - toTimestamp(b));

  const todayPosition = positionInRange(snapshot);
  const rangeKey = `${rangeStart}|${rangeEnd}|${coverages.length}`;

  const cardAccent =
    coverages.length === 1
      ? TONE_STANDARD.base
      : `linear-gradient(90deg, ${coverages
          .map(coverage => `${coverage.tone.base} ${asPercentage(positionInRange(coverage.start))} ${asPercentage(positionInRange(coverage.end))}`)
          .join(', ')})`;

  const summary = coverages.map(coverage => `${coverage.label}: ${coverage.start} — ${coverage.end} (${coverageStatus(coverage, snapshot)})`).join('. ');

  return (
    <article class="coverage-timeline warranty-card" style={{ '--card-accent': cardAccent }}>
      {header}

      <section class="journey" aria-label={locale.warrantyCoverage}>
        {badges}

        <div class="timeline-shell">
          <div class="timeline-scroll" tabindex="0" role="region" aria-label={locale.warrantyCoverage} ref={element => revealToday(element, todayPosition, rangeKey)}>
            <div class="timeline">
              <div class="today-head" aria-hidden="true" style={{ '--at': asPercentage(todayPosition) }} ref={element => positionTodayPill(element, todayPosition)}>
                <span class="today-pill">{`${locale.today} · ${snapshot}`}</span>
              </div>

              <div class="axis" aria-hidden="true">
                {ticks.map(tick => (
                  <span class="axis-date" key={tick} style={{ '--at': asPercentage(positionInRange(tick)) }}>
                    {tick}
                  </span>
                ))}
              </div>

              <div class="lane">
                <span class="today-tail" aria-hidden="true" style={{ '--at': asPercentage(todayPosition) }} />

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
                        aria-label={`${coverage.label}, ${coverage.start} — ${coverage.end}`}
                        style={{ '--start': asPercentage(start), '--span': asPercentage(span), ...toneVariables(coverage.tone) }}
                      >
                        <div class={`segment ${coverage.kind === 'standard' ? 'is-standard' : 'is-extended'}${coverage.alt ? ' tone-alt' : ''}`}>
                          {coverage.kind === 'standard' ? (
                            <strong>{coverage.label}</strong>
                          ) : coverage.logo ? (
                            <img class="provider-logo" src={coverage.logo} alt="" loading="lazy" />
                          ) : (
                            <strong>{coverage.providerID || ''}</strong>
                          )}
                        </div>

                        <span class="coverage-label">{coverage.label}</span>
                      </div>
                    );
                  })}
                </div>

                <span class="past-wash" aria-hidden="true" style={{ '--to': asPercentage(clampPercentage(todayPosition)) }} />

                <div class="boundary-layer" aria-hidden="true">
                  {coverages.slice(1).map(coverage => (
                    <span key={coverage.id} class="boundary" style={{ '--at': asPercentage(positionInRange(coverage.start)), ...toneVariables(coverage.tone) }} />
                  ))}
                </div>

                <span class="today-marker" aria-hidden="true" style={{ '--at': asPercentage(todayPosition) }} />
              </div>

              <p class="sr-only">{summary}</p>
            </div>
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

  return (
    <aside class="total-coverage" aria-label={`${locale.totalPlannedProtection}: ${mix}`}>
      <span>{locale.totalPlannedProtection}</span>
      <strong>{formatDuration(standardMonths + extendedMonths, locale)}</strong>
      <span class="coverage-mix">{mix}</span>
    </aside>
  );
}
