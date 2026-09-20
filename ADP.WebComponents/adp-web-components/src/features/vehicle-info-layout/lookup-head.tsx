import { FunctionalComponent, h } from '@stencil/core';

import { SharedLocales } from '~features/multi-lingual';

// The panels' shared glyph set; see the note in vehicle-info-layout.tsx.
import { BADGE_GLYPHS } from '../../components/vehicle-lookup/components/glyphs';

import { VerdictState } from './interface';

export type PanelVerdict = { state: VerdictState; text: string };

/** Everything a record panel's verdict is decided from. */
export type RecordPanelState = {
  /** The shared locale, for the pill's words. */
  locale: SharedLocales;
  vehicleLoaded: boolean;
  /** `isAuthorized` of the loaded vehicle; `false` means the distributor has no record of it. */
  authorized?: boolean;
  /** Whether this panel has anything on record for the vehicle. */
  hasRecords: boolean;
};

/**
 * The verdict of a panel that shows records — a specification, accessories, a sale, a service
 * history, paint readings, claimable items — rather than a judgement. What it may say, in order:
 *
 *  - no vehicle yet: idle, nothing asserted, an empty pill;
 *  - a vehicle the distributor has no record of: neutral, "not in the distributor's records" —
 *    whatever the list happens to hold, it is not the distributor's to show
 *    (.shift/repos/adp/web-components/vehicle-lookup-invariants.md);
 *  - records on file: positive;
 *  - an authorized vehicle with nothing on file: idle, in words — "no records" is a fact about the
 *    records and is said in grey, never in green (a verdict read off an empty list) and never in
 *    amber (which would say the vehicle is unknown).
 */
export const recordVerdict = ({ locale, vehicleLoaded, authorized, hasRecords }: RecordPanelState): PanelVerdict => {
  if (!vehicleLoaded) return { state: 'idle', text: '' };
  if (authorized === false) return { state: 'neutral', text: locale.notInRecords };
  if (hasRecords) return { state: 'positive', text: locale.onRecord };
  return { state: 'idle', text: locale.noRecords };
};

const BADGE_GLYPH: Record<VerdictState, keyof typeof BADGE_GLYPHS | null> = { positive: 'positive', negative: 'negative', neutral: 'question', attention: 'alert', idle: null };

/**
 * The split-cap verdict pill (§ 16): a solid leading cap carrying the glyph against a tinted body.
 * Idle keeps the shape and asserts nothing; an idle pill with words ("no records") is a grey
 * statement and is read, one without is furniture and is hidden from assistive readers.
 */
export const StatusBadge: FunctionalComponent<PanelVerdict> = ({ state, text }) => {
  const glyph = BADGE_GLYPH[state];
  const shown = text || '';

  return (
    <span class={{ 'status-badge': true, [`is-${state}`]: true, 'has-text': !!shown }} aria-hidden={shown ? null : 'true'}>
      <svg class="badge-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
        {glyph && <path fill="currentColor" d={BADGE_GLYPHS[glyph]} />}
      </svg>
      <span>{shown}</span>
    </span>
  );
};

type LookupHeadProps = {
  /** The panel's name. A class on a span, never a heading element. */
  title: string;
  verdict: PanelVerdict;
};

/**
 * The standard head (§ 14): the title on the reading edge, the verdict pill on the other, and
 * between them any control the panel keeps beside its verdict (the children). An anchor: it is
 * rendered in every state and only its content changes. The two content blocks are marked for a
 * composite's tab switch (vehicle-info-layout.css): the band stays, the content changes hands.
 */
export const LookupHead: FunctionalComponent<LookupHeadProps> = ({ title, verdict }, children) => (
  <header class="lookup-head">
    <span class="lookup-title lookup-head-content" data-head-edge="start">
      {title}
    </span>
    <div class="lookup-summary lookup-head-content" data-head-edge="end">
      {children}
      <StatusBadge state={verdict.state} text={verdict.text} />
    </div>
  </header>
);

/** The "why?" glyph in the outlined round button, for a trace trigger beside the pill. */
export const TraceGlyph: FunctionalComponent = () => (
  <svg class="lookup-trace-button-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
    <path fill="currentColor" d={BADGE_GLYPHS.question} />
  </svg>
);
