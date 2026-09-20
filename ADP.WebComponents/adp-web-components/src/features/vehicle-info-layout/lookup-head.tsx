import { FunctionalComponent, h } from '@stencil/core';

import { SharedLocales } from '~features/multi-lingual';

// The panels' shared glyph set; see the note in vehicle-info-layout.tsx.
import { BADGE_GLYPHS } from '../../components/vehicle-lookup/components/glyphs';

import { VerdictState } from './interface';

/** `label` is what a screen reader hears when a pill shows no words on screen. */
export type PanelVerdict = { state: VerdictState; text: string; label?: string };

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
};

/**
 * The verdict of a panel that shows records — a specification, accessories, a sale, a service
 * history, paint readings, claimable items — rather than a judgement. What it may say, in order:
 *
 *  - no vehicle yet: idle, nothing asserted, an empty pill;
 *  - a vehicle the distributor has no record of: neutral, "not in the distributor's records" —
 *    whatever the list happens to hold, it is not the distributor's to show
 *    (.shift/repos/adp/web-components/vehicle-lookup-invariants.md);
 *  - records on file: positive, "on record";
 *  - an authorized vehicle with nothing on file: idle, in words — "no records" is a fact about the
 *    records and is said in grey, never in green (a verdict read off an empty list) and never in
 *    amber (which would say the vehicle is unknown).
 */
export const recordVerdict = ({ locale, vehicleLoaded, authorized, hasRecords, error }: RecordPanelState): PanelVerdict => {
  if (error) return { state: 'negative', text: error };
  if (!vehicleLoaded) return { state: 'idle', text: '' };
  if (authorized === false) return { state: 'neutral', text: locale.notInRecords };
  if (hasRecords) return { state: 'positive', text: locale.onRecord };
  return { state: 'idle', text: locale.noRecords };
};

const BADGE_GLYPH: Record<VerdictState, keyof typeof BADGE_GLYPHS | null> = { positive: 'positive', negative: 'negative', neutral: 'question', attention: 'alert', idle: null };

/**
 * Text width cannot be transitioned to, but a measured one can: the pill writes its natural width
 * (cap plus words) on itself as `--pill-width` on every render, and the stylesheet transitions
 * `width` between it and the skeleton's. Measured from the children, not `scrollWidth`, which
 * never reports less than the box the pill is still shrinking from. A `ref`, so it runs after the
 * words are in the DOM and before they are painted — passed as a fresh closure each render, since
 * Stencil skips a `ref` whose identity has not changed.
 */
export const measurePill = (pill?: HTMLElement) => {
  if (!pill) return;
  writePillWidth(pill);
  measured.add(pill);
  remeasureOnFonts(pill.ownerDocument);
};

const writePillWidth = (pill: HTMLElement) => {
  const width = Array.from(pill.children).reduce((sum, child) => sum + child.getBoundingClientRect().width, 0);
  pill.style.setProperty('--pill-width', `${width}px`);
};

/** Every pill measured so far; pruned of the ones no longer on the page when a font lands. */
const measured = new Set<HTMLElement>();
const watchedFontSets = new WeakSet<FontFaceSet>();

/**
 * The faces are web fonts (lookup-tokens.css), and one that arrives after the words were measured
 * changes their width: a pill measured in the fallback face would clip or float its words until
 * its next render. The page's font set says when a face has landed; every measured pill still on
 * the page is measured again, and its width transitions to the correction like any other change.
 * One listener per document, however many panels it holds.
 */
const remeasureOnFonts = (doc: Document) => {
  const fonts = doc?.fonts;
  if (!fonts || watchedFontSets.has(fonts)) return;
  watchedFontSets.add(fonts);
  fonts.addEventListener('loadingdone', () => measured.forEach(pill => (pill.isConnected ? writePillWidth(pill) : measured.delete(pill))));
};

/**
 * The split-cap verdict pill (§ 16): a solid leading cap carrying the glyph against a tinted body.
 * Idle keeps the shape and asserts nothing; an idle pill with words ("no records") is a grey
 * statement and is read, one without is furniture and is hidden from assistive readers.
 */
export const StatusBadge: FunctionalComponent<PanelVerdict> = ({ state, text, label }) => {
  const glyph = BADGE_GLYPH[state];
  const shown = text || '';
  const spoken = shown || label || '';

  return (
    <span
      class={{ 'status-badge': true, [`is-${state}`]: true, 'has-text': !!shown }}
      aria-hidden={spoken ? null : 'true'}
      aria-label={!shown && label ? label : null}
      ref={pill => measurePill(pill)}
    >
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
 * composite's tab switch (vehicle-info-layout.css): the band stays, the content changes hands. The
 * title carries lookup-skeleton (lookup-motion.css): while a lookup is in flight its box becomes a
 * sheen bar in place, as the pill's does.
 */
export const LookupHead: FunctionalComponent<LookupHeadProps> = ({ title, verdict }, children) => (
  <header class="lookup-head lookup-head-band">
    <span class="lookup-title lookup-head-content lookup-skeleton">{title}</span>
    <div class="lookup-summary lookup-head-content">
      {children}
      <StatusBadge state={verdict.state} text={verdict.text} label={verdict.label} />
    </div>
  </header>
);
