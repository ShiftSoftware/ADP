import { FunctionalComponent, h } from '@stencil/core';
import { InferType } from 'yup';

import cn from '~lib/cn';

import sscSchema from '~locales/vehicleLookup/ssc/type';
import { SscDTO } from '~types/generated/vehicle-lookup/ssc-dto';
import { SscRepairTraceDTO } from '~types/generated/vehicle-lookup/ssc-repair-trace-dto';
import { SscRepairTraceLaborLineDTO } from '~types/generated/vehicle-lookup/ssc-repair-trace-labor-line-dto';
import { SscRepairTraceWarrantyClaimDTO } from '~types/generated/vehicle-lookup/ssc-repair-trace-warranty-claim-dto';

import { BADGE_GLYPHS } from './glyphs';

export type SscLocale = InferType<typeof sscSchema>;

type RepairSource = SscDTO['repairSource'];

/** The manufacturer's answer for a vehicle the distributor has no record of. */
export type ManufacturerCheckStatus = 'noRecall' | 'recallExists' | 'noApplicableVehicleFound';

/** Identifies a campaign row across renders. Campaign codes are expected to be unique per vehicle; the index guards the odd feed that repeats one. */
export const sscTraceKey = (item: SscDTO | undefined, index: number) => `${item?.sscCode ?? ''}#${index}`;

/** What the card's body — everything below the lead strip — holds. */
export type BodyKind = 'rows' | 'check' | 'run' | 'none';

/** What the lead strip — the band between the card's header and its body — shows. */
export type LeadKind = 'skeleton' | 'columns' | 'notice';

/**
 * `neutral` is a statement without a verdict; `attention` is a required action not taken yet — the
 * check was not run — and has a hue of its own so it is never read as a verdict or as a statement.
 */
export type VerdictState = 'positive' | 'negative' | 'neutral' | 'attention' | 'idle';

export type PanelVerdict = { state: VerdictState; text: string };

type Tone = 'positive' | 'negative' | 'neutral' | 'attention';

/** Everything the panel's verdict, lead and body are decided from. */
export type PanelState = {
  locale: SscLocale;
  vehicleLoaded: boolean;
  /**
   * `isAuthorized` of the loaded vehicle. `false` means the distributor has no record of it, and
   * then nothing in `campaigns` may be shown or summarised — see the invariant on `panelVerdict`.
   */
  authorized?: boolean;
  /** Undefined until a vehicle is loaded; an empty list on an authorized vehicle means it is clear. */
  campaigns?: SscDTO[];
  /**
   * The other panels show a vehicle but this panel's own lookup was not run for it (a wrapper with
   * `sscQueryString` only counts the SSC tab's own request as a campaign check). Nothing is known,
   * so the panel says so and offers to run the check — it must never look like "no campaigns".
   */
  skipped: boolean;
  /** Whether the host configured the manufacturer check (a reCAPTCHA site key) for unauthorized vehicles. */
  checkAvailable: boolean;
  /** The manufacturer's answer, once it arrived. */
  checkStatus?: ManufacturerCheckStatus | null;
};

type Props = PanelState & {
  /** A lookup is in flight, or the panel is being cleared: chip and lead go to the sheen and the body shuts. */
  loading: boolean;
  /** The manufacturer check is in flight: chip and lead go to the sheen; the body stays open on the widget and its spinner. */
  checking: boolean;
  /**
   * What the body keeps rendering while it is shut — the content on its way out, so it slides away
   * instead of vanishing. The owner remembers the last non-empty body and drops it once shut.
   */
  retainedBody: BodyKind;
  onRunLookup: () => void;
  showTrace: boolean;
  openTraceKey?: string;
  traces: Record<string, SscRepairTraceDTO>;
  traceLoading: boolean;
  traceError?: string;
  onToggleTrace: (key: string) => void;
};

const same = (a?: string, b?: string) => (a || '').trim().toUpperCase() === (b || '').trim().toUpperCase();

const campaignsOf = (campaigns?: SscDTO[]) => (campaigns || []).filter(Boolean);

/**
 * The row whose evidence drawer is open: the wanted trace once there is something to show — the
 * evidence, or the failure to get it. While it is being fetched the row's button spins instead and
 * the drawer stays shut, so it never swaps a spinner for content mid-air. The owner watches this to
 * bring the row into view as the drawer opens.
 */
export const openDrawerKey = ({
  campaigns,
  showTrace,
  openTraceKey,
  traces,
  traceLoading,
}: Pick<Props, 'campaigns' | 'showTrace' | 'openTraceKey' | 'traces' | 'traceLoading'>): string | undefined => {
  if (!showTrace || !openTraceKey) return undefined;

  const items = campaignsOf(campaigns);
  const index = items.findIndex((item, i) => sscTraceKey(item, i) === openTraceKey);
  if (index < 0) return undefined;

  const trace = traces[openTraceKey] || items[index].trace;
  return !trace && traceLoading ? undefined : openTraceKey;
};

/**
 * What the panel is allowed to assert about the vehicle, and in which colour.
 *
 * THE INVARIANT THIS ENCODES — read before touching it:
 *
 * Campaign status is a legal matter, and this panel must never give a verdict it has not earned.
 * There are two separate paths and a vehicle is on exactly one of them:
 *
 *  1. An AUTHORIZED vehicle (`isAuthorized !== false`) is in the distributor's records. Its
 *     campaigns are the list in `campaigns`; an empty list is a genuine "no campaign affects this
 *     vehicle" — the distributor looked and found none.
 *
 *  2. An UNAUTHORIZED vehicle (`isAuthorized === false`) is NOT in the distributor's records. The
 *     distributor knows nothing about its campaigns, so an empty `campaigns` here is *absence of
 *     data*, not a clean bill. The only verdict available is the manufacturer's own answer, obtained
 *     through the reCAPTCHA-gated check, and it is a yes/no with no list behind it. Until that answer
 *     arrives the panel says "not in the distributor's records" and nothing more — never a green
 *     "no campaign", never a table, never a count. A `campaigns` list on an unauthorized vehicle is a
 *     data fault (the server derives `isAuthorized` partly *from* having SSC records) and is ignored
 *     rather than displayed next to the check.
 *
 * And a third case that is neither: the lookup was SKIPPED for this panel (`skipped`). The other
 * panels show the vehicle, this one holds nothing, and it says "not checked yet" and offers the
 * check — a blank panel would read as "no campaigns", which is the same lie as case 2's.
 *
 * Two sessions independently built a green "no campaign" out of an empty list before this was
 * written down; `SscCampaigns.spec.tsx` now fails if it happens again. The wider rule is in
 * .shift/repos/adp/web-components/vehicle-lookup-invariants.md.
 */
export const panelVerdict = ({ locale, vehicleLoaded, authorized, campaigns, checkStatus, skipped }: PanelState): PanelVerdict => {
  if (!vehicleLoaded) return skipped ? { state: 'attention', text: locale.skipped } : { state: 'idle', text: '' };

  if (authorized === false) {
    switch (checkStatus) {
      case 'recallExists':
        return { state: 'negative', text: locale.pendingCampaign };
      case 'noRecall':
        return { state: 'positive', text: locale.noPendingCampaign };
      case 'noApplicableVehicleFound':
        return { state: 'neutral', text: locale.vehicleNotFound };
      default:
        return { state: 'neutral', text: locale.notInRecords };
    }
  }

  const items = campaignsOf(campaigns);
  const openCount = items.filter(item => !item.repaired).length;

  if (items.length === 0) return { state: 'positive', text: locale.noPendingCampaign };

  return { state: openCount > 0 ? 'negative' : 'positive', text: `${openCount} ${locale.open} · ${items.length - openCount} ${locale.repaired}` };
};

/** What belongs below the lead strip for this state; `none` shuts the body. */
export const panelBody = ({ vehicleLoaded, authorized, campaigns, checkAvailable, checkStatus, skipped }: PanelState): BodyKind => {
  if (!vehicleLoaded) return skipped ? 'run' : 'none';
  if (authorized === false) return checkAvailable && !checkStatus ? 'check' : 'none';
  return campaignsOf(campaigns).length > 0 ? 'rows' : 'none';
};

/** The strip reads as the column headings only above a list; every other statement is a notice, and in flight it is a skeleton. */
export const panelLead = (state: PanelState, busy: boolean): LeadKind => {
  if (busy) return 'skeleton';
  if (!state.vehicleLoaded && !state.skipped) return 'skeleton';
  return panelBody(state) === 'rows' ? 'columns' : 'notice';
};

/** The notice the lead strip carries — null above a list, where the headings speak instead. */
const leadNotice = ({ locale, vehicleLoaded, authorized, campaigns, skipped, checkAvailable, checkStatus }: PanelState): { tone: Tone; text: string } | null => {
  if (!vehicleLoaded) return skipped ? { tone: 'attention', text: locale.skippedNotice } : null;

  if (authorized === false) {
    switch (checkStatus) {
      case 'recallExists':
        return { tone: 'negative', text: locale.recallExists };
      case 'noRecall':
        return { tone: 'positive', text: locale.noRecall };
      case 'noApplicableVehicleFound':
        return { tone: 'neutral', text: locale.noApplicableVehicleFound };
      default:
        return { tone: 'neutral', text: checkAvailable ? locale.unauthorizedCheck : locale.unauthorizedNoCheck };
    }
  }

  if (campaignsOf(campaigns).length === 0) return { tone: 'positive', text: locale.noCampaigns };

  return null;
};

const Glyph = ({ kind, className }: { kind: keyof typeof BADGE_GLYPHS; className: string }) => (
  <svg class={className} viewBox="0 0 512 512" aria-hidden="true" focusable="false">
    <path fill="currentColor" d={BADGE_GLYPHS[kind]} />
  </svg>
);

const BADGE_GLYPH: Record<VerdictState, keyof typeof BADGE_GLYPHS | null> = { positive: 'positive', negative: 'negative', neutral: 'question', attention: 'alert', idle: null };

const TONE_GLYPH: Record<Tone, keyof typeof BADGE_GLYPHS> = { positive: 'positive', negative: 'negative', neutral: 'question', attention: 'alert' };

/**
 * The split-cap verdict pill shared with the warranty timeline: a solid cap carrying the glyph
 * against a tinted body. Idle keeps the shape and asserts nothing, so a row does not move when
 * the verdict lands; neutral is a statement without a verdict ("not in the records").
 */
const StatusBadge = ({ state, text }: { state: VerdictState; text: string }) => {
  const glyph = BADGE_GLYPH[state];

  return (
    <span class={`status-badge is-${state}`} aria-hidden={state === 'idle' ? 'true' : null}>
      <svg class="badge-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
        {glyph && <path fill="currentColor" d={BADGE_GLYPHS[glyph]} />}
      </svg>
      <span>{state === 'idle' ? '' : text}</span>
    </span>
  );
};

type ChipTone = 'accent' | 'neutral' | 'positive' | 'negative';

const Chip = ({ tone, text, note, title, icon }: { tone: ChipTone; text: string; note?: string; title?: string; icon?: 'positive' | 'negative' }) => (
  <span class={`ssc-chip is-${tone}`} title={title}>
    {icon && <Glyph kind={icon} className="chip-icon" />}
    <span class="chip-text">{text}</span>
    {note && <span class="chip-note">{note}</span>}
  </span>
);

const sourceLabel = (source: RepairSource | undefined, locale: SscLocale): string | undefined => {
  switch (source) {
    case 'SSCRecord':
      return locale.sourceSSCRecord;
    case 'WarrantyClaim':
      return locale.sourceWarrantyClaim;
    case 'ServiceHistory':
      return locale.sourceServiceHistory;
    default:
      return undefined;
  }
};

const RepairStatus = ({ item, locale }: { item: SscDTO; locale: SscLocale }) => {
  const source = item.repaired ? sourceLabel(item.repairSource, locale) : undefined;
  const text = item.repaired ? (item.repairDate ? `${locale.repaired} · ${item.repairDate}` : locale.repaired) : locale.open;

  return (
    <div class="ssc-status-cell">
      <StatusBadge state={item.repaired ? 'positive' : 'negative'} text={text} />
      {source && <span class="ssc-status-source">{source}</span>}
    </div>
  );
};

/** Three availability states, matching the repair-status icon language: in stock, not in stock, and not checked (neutral, no icon). */
const PartChip = ({ partNumber, isAvailable, locale }: { partNumber: string; isAvailable?: boolean; locale: SscLocale }) => {
  if (isAvailable === true) return <Chip tone="positive" icon="positive" text={partNumber} title={locale.partAvailable} />;
  if (isAvailable === false) return <Chip tone="negative" icon="negative" text={partNumber} title={locale.partUnavailable} />;
  return <Chip tone="neutral" text={partNumber} title={locale.partNotChecked} />;
};

/** A region the card gains and loses. The grid track animates so it slides rather than pops (lookup-motion.css). */
const Collapsible: FunctionalComponent<{ open: boolean; className?: string }> = ({ open, className }, children) => (
  <div class={cn('collapsible', className)} data-open={open ? 'true' : 'false'} aria-hidden={open ? null : 'true'}>
    <div class="collapsible-body">{children}</div>
  </div>
);

const EvidencePill = ({ positive, text }: { positive: boolean; text: string }) => (
  <span class={cn('evidence-pill', positive ? 'is-positive' : 'is-negative')}>
    <Glyph kind={positive ? 'positive' : 'negative'} className="evidence-pill-icon" />
    <span>{text}</span>
  </span>
);

const MatchedCode = ({ laborCode, campaignLaborCode, locale }: { laborCode: string; campaignLaborCode: string; locale: SscLocale }) => (
  <span>
    {locale.traceMatchedCode} <code class="trace-code">{laborCode}</code>
    {!same(laborCode, campaignLaborCode) && (
      <span class="trace-muted">
        {' '}
        ({locale.traceInterchangeableWith} <code class="trace-code">{campaignLaborCode}</code>)
      </span>
    )}
  </span>
);

const ClaimEvidence = ({ claim, locale }: { claim: SscRepairTraceWarrantyClaimDTO; locale: SscLocale }) => (
  <li class={cn('trace-item', { 'is-selected': claim.selected, 'is-dim': !claim.matches && !claim.selected })}>
    <div class="trace-item-head">
      <strong class="trace-item-title">
        {locale.traceClaim} {claim.claimNumber || claim.dealerClaimNumber || '—'}
      </strong>
      <EvidencePill positive={claim.statusQualifies} text={`${claim.claimStatus} · ${claim.statusQualifies ? locale.traceStatusQualifies : locale.traceStatusNotQualifies}`} />
      {claim.repairCompletionDate && (
        <span class="trace-date">
          {locale.traceCompleted} {claim.repairCompletionDate}
        </span>
      )}
      {claim.selected && <span class="trace-decided">{locale.traceDecided}</span>}
    </div>
    <ul class="trace-facts">
      {claim.campaignCodeInComment && <li class="is-hit">{locale.traceCampaignInComment}</li>}
      {(claim.matchedLaborCodes || []).map(match => (
        <li class="is-hit" key={match.laborCode}>
          <MatchedCode laborCode={match.laborCode} campaignLaborCode={match.campaignLaborCode} locale={locale} />
        </li>
      ))}
      {!claim.matches && <li class="is-miss">{locale.traceNoReference}</li>}
    </ul>
  </li>
);

const LineEvidence = ({ line, locale }: { line: SscRepairTraceLaborLineDTO; locale: SscLocale }) => (
  <li class={cn('trace-item', { 'is-selected': line.selected })}>
    <div class="trace-item-head">
      <strong class="trace-item-title">
        {locale.traceInvoice} {line.invoiceNumber || '—'}
      </strong>
      <EvidencePill
        positive={line.statusQualifies}
        text={`${locale.traceInvoiceStatus} ${line.invoiceStatus || '—'} · ${line.statusQualifies ? locale.traceStatusQualifies : locale.traceStatusNotQualifies}`}
      />
      {line.invoiceDate && <span class="trace-date">{line.invoiceDate}</span>}
      {line.selected && <span class="trace-decided">{locale.traceDecided}</span>}
    </div>
    <ul class="trace-facts">
      <li class="is-hit">
        <MatchedCode laborCode={line.laborCode} campaignLaborCode={line.campaignLaborCode} locale={locale} />
      </li>
    </ul>
  </li>
);

const verdictText = (source: RepairSource | undefined, locale: SscLocale) => {
  switch (source) {
    case 'SSCRecord':
      return locale.traceVerdictSSCRecord;
    case 'WarrantyClaim':
      return locale.traceVerdictWarrantyClaim;
    case 'ServiceHistory':
      return locale.traceVerdictServiceHistory;
    default:
      return locale.traceVerdictOpen;
  }
};

const DecidedMark = ({ locale }: { locale: SscLocale }) => (
  <span class="trace-step-decided">
    <Glyph kind="positive" className="trace-step-decided-icon" />
    {locale.traceDecided}
  </span>
);

/**
 * The evidence behind one campaign's verdict, in the order the evaluator consulted it. Each of the
 * three sources is a numbered step; the one that decided is marked, and the ones that could not
 * are shown with why — a rejected claim, an un-invoiced job — so the reader sees the near misses,
 * not only the hit. Loading is not shown here: the drawer only opens once there is something to
 * show, and the row's button spins meanwhile, so the drawer never swaps its content mid-air.
 */
const SscTrace: FunctionalComponent<{ locale: SscLocale; item: SscDTO; trace?: SscRepairTraceDTO; error?: string }> = ({ locale, item, trace, error }) => {
  if (!trace)
    return (
      <div class="ssc-trace">
        <div class="trace-head">
          <span class="trace-label">{locale.traceTitle}</span>
          <span class="trace-head-code">{item.sscCode}</span>
        </div>
        <div class={cn('ssc-trace-status', { 'is-error': !!error })}>{error || locale.traceUnavailable}</div>
      </div>
    );

  const source = trace.repairSource;
  const repaired = source && source !== 'None';
  const claims = trace.warrantyClaims || [];
  const lines = trace.serviceHistoryLaborLines || [];

  return (
    <div class="ssc-trace" data-source={source || 'None'}>
      <div class="trace-head">
        <span class="trace-label">{locale.traceTitle}</span>
        <span class="trace-head-code">{item.sscCode}</span>
      </div>

      <div class={cn('trace-verdict', repaired ? 'is-positive' : 'is-negative')}>
        <Glyph kind={repaired ? 'positive' : 'negative'} className="trace-verdict-icon" />
        <strong>{verdictText(source, locale)}</strong>
        {repaired && item.repairDate && <span class="trace-verdict-date">{item.repairDate}</span>}
      </div>

      <p class="trace-order">{locale.traceOrder}</p>

      <div class="trace-codes">
        <span class="trace-label">{locale.traceCodes}</span>
        <div class="ssc-chips">
          {(trace.campaignLaborCodes || []).map(code => (
            <Chip tone="accent" text={code} title={locale.traceCampaignCode} />
          ))}
          {(trace.interchangeableLaborCodes || []).map(alternative => (
            <Chip
              tone="neutral"
              text={alternative.laborCode}
              note={`↔ ${alternative.campaignLaborCode}`}
              title={`${locale.traceInterchangeableWith} ${alternative.campaignLaborCode}`}
            />
          ))}
        </div>
      </div>

      <ol class="trace-steps">
        <li class="trace-step" data-decided={source === 'SSCRecord' ? 'true' : 'false'}>
          <div class="trace-step-head">
            <span class="trace-step-index">1</span>
            <span class="trace-step-title">{locale.traceRecord}</span>
            {source === 'SSCRecord' && <DecidedMark locale={locale} />}
          </div>
          <div class="trace-step-body">
            {trace.recordRepairDate ? (
              <span class="trace-kv">
                <span>{locale.traceRecordDate}</span>
                <strong>{trace.recordRepairDate}</strong>
              </span>
            ) : (
              <span class="trace-muted">{locale.traceRecordNone}</span>
            )}
          </div>
        </li>

        <li class="trace-step" data-decided={source === 'WarrantyClaim' ? 'true' : 'false'}>
          <div class="trace-step-head">
            <span class="trace-step-index">2</span>
            <span class="trace-step-title">{locale.traceClaims}</span>
            <span class="trace-count">{claims.length}</span>
            {source === 'WarrantyClaim' && <DecidedMark locale={locale} />}
          </div>
          <div class="trace-step-body">
            {claims.length === 0 ? (
              <span class="trace-muted">{locale.traceClaimsNone}</span>
            ) : (
              <ul class="trace-evidence">
                {claims.map(claim => (
                  <ClaimEvidence claim={claim} locale={locale} />
                ))}
              </ul>
            )}
          </div>
        </li>

        <li class="trace-step" data-decided={source === 'ServiceHistory' ? 'true' : 'false'}>
          <div class="trace-step-head">
            <span class="trace-step-index">3</span>
            <span class="trace-step-title">{locale.traceHistory}</span>
            <span class="trace-count">
              {trace.serviceHistoryLaborLinesExamined} {locale.traceLinesExamined} · {lines.length} {locale.traceLinesMatched}
            </span>
            {source === 'ServiceHistory' && <DecidedMark locale={locale} />}
          </div>
          <div class="trace-step-body">
            {lines.length === 0 ? (
              <span class="trace-muted">{locale.traceHistoryNone}</span>
            ) : (
              <ul class="trace-evidence">
                {lines.map(line => (
                  <LineEvidence line={line} locale={locale} />
                ))}
              </ul>
            )}
          </div>
        </li>
      </ol>
    </div>
  );
};

/**
 * The campaigns of one vehicle. The card is three fixed parts, and every state is a different
 * content of the same three — nothing is added to or removed from the card between states, which
 * is what lets every change be a movement (the rule is in lookup-motion.css and
 * .shift/repos/adp/web-components/motion.md):
 *
 *  - the header: the title and the verdict pill. Idle the pill is a blank shape; in flight it
 *    carries the sheen; settled it names the verdict.
 *  - the lead strip: one band that is a skeleton (idle, in flight), the column headings (above a
 *    list), or a notice (everything the panel has to say in a sentence — not in the records, the
 *    manufacturer's answer, no campaign affects this vehicle, the check was skipped). The three are
 *    layers of one cell and cross-fade; the strip never changes height.
 *  - the body: a region that slides open and shut. Open it holds the campaign rows, the
 *    manufacturer-check widget, or the "run the check" action; it shuts while a lookup is in
 *    flight — with the outgoing content still inside it, so that content leaves with it — and opens
 *    again on the new content once the lookup has settled.
 *
 * What the panel may assert is decided by `panelVerdict` — see the invariant there. The rows are
 * a CSS grid with subgrid so the headings in the lead strip and the cells in the body share one
 * set of column tracks: columns take what their content needs, the description absorbs the slack.
 * Below the container breakpoint each row becomes a labelled card and the strip a single label.
 */
export const SscCampaigns: FunctionalComponent<Props> = (props, children) => {
  const { locale, loading, checking, retainedBody, onRunLookup, showTrace, openTraceKey, traces, traceError, onToggleTrace } = props;

  const verdict = panelVerdict(props);
  const body = panelBody(props);
  const busy = loading || checking;
  const lead = panelLead(props, busy);
  const notice = leadNotice(props);

  // The body shuts while a lookup is in flight and whenever there is nothing to show; while shut
  // it keeps rendering what it last held, so that content slides away rather than blinking out.
  const bodyOpen = !loading && body !== 'none';
  const shown: BodyKind = body === 'none' ? retainedBody : body;

  const items = props.vehicleLoaded && props.authorized !== false ? campaignsOf(props.campaigns) : [];
  const openDrawer = openDrawerKey(props);

  return (
    <section class="ssc-card" data-verdict={verdict.state} data-phase={busy ? 'busy' : 'settled'}>
      <header class="ssc-head">
        <span class="ssc-title">{locale.title}</span>
        <div class="ssc-summary">
          <StatusBadge state={verdict.state} text={verdict.text} />
        </div>
      </header>

      <div class={cn('ssc-grid', { 'has-trace': showTrace })} role={shown === 'rows' && bodyOpen ? 'table' : null}>
        <div class="ssc-lead layer-stack" data-lead={lead}>
          <div class="layer ssc-lead-skeleton" data-active={lead === 'skeleton' ? 'true' : 'false'} aria-hidden="true" />

          <div
            class={cn('layer ssc-lead-notice', notice && `is-${notice.tone}`)}
            data-active={lead === 'notice' ? 'true' : 'false'}
            role="status"
            aria-hidden={lead === 'notice' ? null : 'true'}
          >
            {notice && <Glyph kind={TONE_GLYPH[notice.tone]} className="notice-icon" />}
            <span>{notice?.text ?? ''}</span>
          </div>

          <div class="layer ssc-lead-columns" data-active={lead === 'columns' ? 'true' : 'false'} role="row" aria-hidden={lead === 'columns' ? null : 'true'}>
            <span class="ssc-cell ssc-code" role="columnheader">
              {locale.code}
            </span>
            <span class="ssc-cell ssc-description" role="columnheader">
              {locale.description}
            </span>
            <span class="ssc-cell ssc-status" role="columnheader">
              {locale.status}
            </span>
            <span class="ssc-cell ssc-labors" role="columnheader">
              {locale.laborCodes}
            </span>
            <span class="ssc-cell ssc-parts" role="columnheader">
              {locale.parts}
            </span>
            {showTrace && (
              <span class="ssc-cell ssc-trace-cell" role="columnheader">
                <span class="sr-only">{locale.viewTrace}</span>
              </span>
            )}
            {/* Narrow panels drop the headings (each card labels its own fields) and keep one word on the strip. */}
            <span class="ssc-columns-label" aria-hidden="true">
              {locale.campaignList}
            </span>
          </div>
        </div>

        <div
          class="ssc-body collapsible"
          data-open={bodyOpen ? 'true' : 'false'}
          data-body={shown}
          role={shown === 'rows' && bodyOpen ? 'rowgroup' : null}
          aria-hidden={bodyOpen ? null : 'true'}
        >
          <div class="collapsible-body ssc-body-inner">
            {shown === 'rows' && (
              <div class="ssc-rows">
                {items.map((item, index) => {
                  const key = sscTraceKey(item, index);
                  const wanted = showTrace && openTraceKey === key;
                  const trace = traces[key] || item.trace;
                  // Fetching the evidence spins the button; the drawer opens only once there is
                  // something to show (openDrawerKey), so it never swaps a spinner for content mid-air.
                  const drawerOpen = openDrawer === key;
                  const fetching = wanted && !drawerOpen;

                  return [
                    <div class="ssc-row" role="row" data-open={drawerOpen ? 'true' : 'false'} data-trace-key={key} key={key}>
                      <div class="ssc-cell ssc-code" role="cell" data-label={locale.code}>
                        <strong>{item.sscCode}</strong>
                      </div>
                      <div class="ssc-cell ssc-description" role="cell" data-label={locale.description}>
                        {item.description}
                      </div>
                      <div class="ssc-cell ssc-status" role="cell" data-label={locale.status}>
                        <RepairStatus item={item} locale={locale} />
                      </div>
                      <div class="ssc-cell ssc-labors" role="cell" data-label={locale.laborCodes}>
                        <div class="ssc-chips">
                          {(item.labors || []).length ? (item.labors || []).map(labor => <Chip tone="accent" text={labor?.laborCode} />) : <span class="ssc-none">—</span>}
                        </div>
                      </div>
                      <div class="ssc-cell ssc-parts" role="cell" data-label={locale.parts}>
                        <div class="ssc-chips">
                          {(item.parts || []).length ? (
                            (item.parts || []).map(part => <PartChip partNumber={part?.partNumber} isAvailable={part?.isAvailable} locale={locale} />)
                          ) : (
                            <span class="ssc-none">—</span>
                          )}
                        </div>
                      </div>
                      {showTrace && (
                        <div class="ssc-cell ssc-trace-cell" role="cell">
                          <button
                            type="button"
                            class="ssc-trace-button"
                            data-loading={fetching ? 'true' : 'false'}
                            aria-busy={fetching ? 'true' : null}
                            aria-expanded={drawerOpen ? 'true' : 'false'}
                            title={drawerOpen ? locale.hideTrace : locale.viewTrace}
                            aria-label={drawerOpen ? locale.hideTrace : locale.viewTrace}
                            onClick={() => onToggleTrace(key)}
                          >
                            <Glyph kind="question" className="ssc-trace-button-icon" />
                            <span class="ssc-trace-button-spinner" aria-hidden="true" />
                          </button>
                        </div>
                      )}
                    </div>,
                    showTrace && (
                      <div class="ssc-trace-row" role="row" data-open={drawerOpen ? 'true' : 'false'} key={`${key}-trace`}>
                        <div role="cell">
                          <Collapsible open={drawerOpen}>
                            {/* Kept while the drawer shuts, so the evidence slides away rather than vanishing. */}
                            {(wanted || trace || traceError) && <SscTrace locale={locale} item={item} trace={trace} error={traceError} />}
                          </Collapsible>
                        </div>
                      </div>
                    ),
                  ];
                })}
              </div>
            )}

            {shown === 'check' && (
              <div class="ssc-check" data-checking={checking ? 'true' : 'false'}>
                {children}
                <Collapsible open={checking} className="ssc-checking-slot">
                  <div class="ssc-checking" role="status">
                    <span class="ssc-spinner" aria-hidden="true" />
                    <span>{locale.checkingTMC}</span>
                  </div>
                </Collapsible>
              </div>
            )}

            {shown === 'run' && (
              <div class="ssc-run">
                <button type="button" class="ssc-run-button" onClick={onRunLookup}>
                  <svg class="ssc-run-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
                    <circle cx="10.5" cy="10.5" r="6.5" fill="currentColor" fill-opacity="0.24" stroke="currentColor" stroke-width="1.6" />
                    <path d="M15.6 15.6L20.5 20.5" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" />
                  </svg>
                  <span>{locale.runCheck}</span>
                </button>
              </div>
            )}
          </div>
        </div>
      </div>
    </section>
  );
};
