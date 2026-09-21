import { FunctionalComponent, h } from '@stencil/core';

import { SharedLocales } from '~features/multi-lingual';

// The panels' shared glyph set; see the note in vehicle-info-layout.tsx.
import { BADGE_GLYPHS } from '../../components/vehicle-lookup/components/glyphs';

import { VerdictState } from './interface';
import { LookupHeadWait } from './vehicle-info-layout';

/**
 * The pill and the accent bar, decided together. `state` and `text` are the pill's: a verdict with
 * no `text` has nothing to say, and the pill is not rendered. `accent` is the card's bar, which
 * always has a colour — the two part ways on a record panel with records on file, where the pill
 * says nothing and the bar says the lookup succeeded (owner, 2026-09-21).
 */
export type PanelVerdict = { state: VerdictState; text: string; accent: VerdictState };

/** Everything a record panel's verdict is decided from. */
export type RecordPanelState = {
  /** The shared locale, for the pill's words. */
  locale: SharedLocales;
  vehicleLoaded: boolean;
  /** `isAuthorized` of the loaded vehicle; `false` means the distributor has no record of it. */
  authorized?: boolean;
  /** Whether this panel has anything on record for the vehicle. */
  hasRecords: boolean;
  /** The lookup failed: the translated message, which the pill then carries in the negative tone. */
  error?: string;
  /**
   * What the records say about the vehicle, once it is the distributor's and there are records on
   * file — the accent bar's colour for that one state, since the pill says nothing there. A panel
   * with a judgement to make writes it from its own data (a reading over its threshold, a service
   * overdue: `negative`); one with none leaves it out and the bar is green — the lookup succeeded
   * and the panel holds what it asked for. Not read in any other state: those colour the bar from
   * the pill.
   */
  recordsVerdict?: 'positive' | 'negative';
};

/**
 * The verdict of a panel that shows records — a specification, accessories, a sale, a service
 * history, paint readings, claimable items — rather than a judgement. What it may say, in order:
 *
 *  - no vehicle yet: idle, nothing asserted, an empty pill;
 *  - a vehicle the distributor has no record of: neutral, "not in the distributor's records" —
 *    whatever the list happens to hold, it is not the distributor's to show
 *    (.shift/repos/adp/web-components/vehicle-lookup-invariants.md);
 *  - records on file: the pill is idle and says nothing — the records themselves are the
 *    statement, and a green "on record" on every panel that had anything at all said nothing the
 *    body did not (owner, 2026-09-21). The slot stays for a detail a panel may want to say here
 *    later. The accent bar does speak: green, the lookup succeeded and the records are there, or
 *    the panel's own `recordsVerdict` where it has a judgement to make (owner, 2026-09-21);
 *  - an authorized vehicle with nothing on file: idle, in words — "no records" is a fact about the
 *    records and is said in grey, never in green (a verdict read off an empty list) and never in
 *    amber (which would say the vehicle is unknown).
 */
export const recordVerdict = ({ locale, vehicleLoaded, authorized, hasRecords, error, recordsVerdict }: RecordPanelState): PanelVerdict => {
  if (error) return { state: 'negative', text: error, accent: 'negative' };
  if (!vehicleLoaded) return { state: 'idle', text: '', accent: 'idle' };
  if (authorized === false) return { state: 'neutral', text: locale.notInRecords, accent: 'neutral' };
  if (hasRecords) return { state: 'idle', text: '', accent: recordsVerdict ?? 'positive' };
  return { state: 'idle', text: locale.noRecords, accent: 'idle' };
};

const BADGE_GLYPH: Record<VerdictState, keyof typeof BADGE_GLYPHS | null> = { positive: 'positive', negative: 'negative', neutral: 'question', attention: 'alert', idle: null };

/**
 * The split-cap verdict pill (§ 16): a solid leading cap carrying the glyph against a tinted body.
 * Rendered only when it has words — the pill is a statement, not furniture, and a record panel
 * with nothing to say shows none (owner, 2026-09-21). An idle pill with words ("no records") is a
 * grey statement and is read like any other. It has no transitions of its own: the head's content
 * is out of the band while a verdict changes (the wrapper's `.loading .lookup-head-content`), so
 * the pill is never seen changing.
 */
export const StatusBadge: FunctionalComponent<Pick<PanelVerdict, 'state' | 'text'>> = ({ state, text }) => {
  if (!text) return null;
  const glyph = BADGE_GLYPH[state];

  return (
    <span class={{ 'status-badge': true, [`is-${state}`]: true }}>
      <svg class="badge-icon" viewBox="0 0 512 512" aria-hidden="true" focusable="false">
        {glyph && <path fill="currentColor" d={BADGE_GLYPHS[glyph]} />}
      </svg>
      <span>{text}</span>
    </span>
  );
};

type LookupHeadProps = {
  /** The panel's name. A class on a span, never a heading element. */
  title: string;
  /** The pill's `state` and `text`; the head does not draw the `accent`, the wrapper's card does. */
  verdict: Pick<PanelVerdict, 'state' | 'text'>;
};

/**
 * The standard head (§ 14): the title on the reading edge, the verdict pill on the other, and
 * between them any control the panel keeps beside its verdict (the children). An anchor: it is
 * rendered in every state and only its content changes. The two content blocks are marked for a
 * composite's tab switch (vehicle-info-layout.css): the band stays, the content changes hands. The
 * same two blocks are what leave while a lookup is in flight — dropping out of the band as they
 * fade, the wrapper's .loading .lookup-head-content rule — and the band's wait spinner rises in
 * their place, so nothing in here needs a loading treatment of its own.
 */
export const LookupHead: FunctionalComponent<LookupHeadProps> = ({ title, verdict }, children) => (
  <header class="lookup-head lookup-head-band">
    <span class="lookup-title lookup-head-content">{title}</span>
    <div class="lookup-summary lookup-head-content">
      {children}
      <StatusBadge state={verdict.state} text={verdict.text} />
    </div>
    <LookupHeadWait />
  </header>
);
