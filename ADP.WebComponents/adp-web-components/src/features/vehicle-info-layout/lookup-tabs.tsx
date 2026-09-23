import { FunctionalComponent, h } from '@stencil/core';

/** The --settle token (lookup-tokens.css) when the stylesheet cannot be read, as in tests. */
const DEFAULT_SETTLE_MS = 480;

/** Which side of the active tab an inactive one rests on, in physical terms — the stylesheet needs no RTL rule. */
export type TabPark = 'left' | 'right';

/**
 * Where an inactive tab parks: the side its content will leave to and return from. A tab later in
 * the strip rests past the trailing edge, an earlier one past the leading edge; under RTL the strip
 * runs the other way, so the sides swap. A tab not in the order at all parks past the leading edge.
 */
export const tabPark = (tag: string, active: string, order: readonly string[], direction: string): TabPark => {
  const position = order.indexOf(tag);
  const current = order.indexOf(active);
  const after = position >= 0 && current >= 0 ? position > current : false;
  const rtl = direction === 'rtl';

  return after !== rtl ? 'right' : 'left';
};

type LookupTabsProps = {
  /** The tag of the tab showing. Anything else is inactive. */
  active: string;
  /** The tabs, in render order; each value is the panel element. */
  panels: [string, unknown][];
  regionRef: (el: HTMLDivElement | undefined) => void;
};

/**
 * The tab region of a composite: every panel is mounted always, one of them active and in flow,
 * the rest absolutely positioned under it, hidden and inert. A switch changes the attributes and
 * nothing else — the transitions run from the live on-screen values, so a click mid-transition
 * reverses from wherever the content is. The card around the
 * region and the head band inside each panel never move: only each panel's `.lookup-slide` region
 * changes — the outgoing leaves, then the incoming arrives (a rule in vehicle-info-layout.css,
 * driven by `data-tab-park` / `data-tab-leaving` on the panel's host) — and the region's height
 * follows the incoming panel (`createTabRegion`).
 */
export const LookupTabs: FunctionalComponent<LookupTabsProps> = ({ active, panels, regionRef }) => (
  <div class="lookup-tabs" ref={regionRef}>
    {panels.map(([tag, panel]) => {
      const inactive = tag !== active;

      return (
        <div key={tag} class="lookup-tab" data-tab={tag} data-state={inactive ? 'inactive' : 'active'} aria-hidden={inactive ? 'true' : null} inert={inactive}>
          {panel}
        </div>
      );
    })}
  </div>
);

/**
 * Drives the region's height. At rest the region is `height: auto` and the active tab, in flow,
 * sets it. A change — a tab switch, or the active panel growing (a drawer opening) — is animated
 * from the height the region is actually showing to the new one, then released to `auto` so the
 * next change starts from a true measurement:
 *
 *  - `beforeSwitch` runs before the DOM is patched (componentWillRender): it reads the region's
 *    current height and the incoming tab's, once, and pins the region to the current value with
 *    the transition off, so the patch that puts the incoming tab in flow moves nothing on screen;
 *  - `afterSwitch` runs after the patch (componentDidRender): one forced reflow, then the target
 *    with the transition on;
 *  - `observe` watches the active panel with a ResizeObserver and RELEASES the region on a resize
 *    rather than animating it — the panel already animates that distance on the same clock.
 *
 * Nothing here gates input: the settle timer only releases the pinned height.
 */
/** A panel's head band, inside its shadow root — the composite may look into its own children. */
const bandOf = (host: Element | undefined) => (host?.shadowRoot?.querySelector('.lookup-head-band') as HTMLElement | null) ?? undefined;

export const createTabRegion = (region: () => HTMLElement | undefined) => {
  let pending: number | undefined;
  let settleTimer: ReturnType<typeof setTimeout> | undefined;
  /** The two hosts whose head bands are travelling to `bandTarget`, and the timer that releases them. */
  let bandHosts: HTMLElement[] = [];
  let bandTarget: number | undefined;
  let bandTimer: ReturnType<typeof setTimeout> | undefined;
  let observer: ResizeObserver | undefined;
  let observed: Element | undefined;
  let observedHeight: number | undefined;

  /** The one clock, plus the frame a transition starts on: the region's height and the head bands both travel on it. */
  const settleMs = (el: HTMLElement) => {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 80;
  };

  const heightOf = (el: Element | undefined) => (el ? el.getBoundingClientRect().height : 0);

  const releaseBands = () => {
    bandTimer = undefined;
    bandHosts.forEach(host => {
      host.style.removeProperty('--lookup-head-height');
      const band = bandOf(host);
      if (band) band.style.transition = '';
    });
    bandHosts = [];
    bandTarget = undefined;
  };

  const release = () => {
    settleTimer = undefined;
    const el = region();
    if (!el) return;
    el.style.height = '';
    el.style.transition = '';
    el.removeAttribute('data-settling');
  };

  /** Holds the region at `px` with no transition — the value it is showing now. */
  const pin = (el: HTMLElement, px: number) => {
    el.style.transition = 'none';
    el.style.height = `${px}px`;
  };

  /**
   * Sends the region to `px` with the stylesheet's transition, from the height it is showing at
   * this instant — read from the box, not assumed from the last target, so a change that lands
   * mid-transition retargets from the live value. The pin, one reflow and the target are written
   * in the same task; the reflow is what makes the pinned value the transition's start.
   */
  const settleTo = (el: HTMLElement, px: number) => {
    pin(el, heightOf(el));
    void el.offsetHeight;
    el.style.transition = '';
    // Marked before the target is written: the stylesheet's height transition is scoped to this
    // attribute, so setting it afterwards would leave the first frame untransitioned.
    el.setAttribute('data-settling', 'true');
    el.style.height = `${px}px`;

    if (settleTimer) clearTimeout(settleTimer);
    settleTimer = setTimeout(release, settleMs(el));
  };

  return {
    beforeSwitch(incoming: Element | undefined, hosts?: { incoming?: HTMLElement; outgoing?: HTMLElement }) {
      const el = region();
      if (!el) return;

      // The head bands: the incoming one is held at the outgoing's height through the patch (a
      // held band is what the incoming tab is measured with, so the region's target is the settled
      // figure either way — read the natural height first), then both travel to the incoming's.
      const inBand = bandOf(hosts?.incoming);
      const outBand = bandOf(hosts?.outgoing);
      const inHeight = heightOf(inBand);
      const outHeight = outBand ? heightOf(outBand) : inHeight;
      const naturalIncoming = heightOf(incoming);

      if (inBand && hosts?.incoming && Math.abs(inHeight - outHeight) >= 0.5) {
        if (bandTimer) clearTimeout(bandTimer);
        releaseBands();
        bandHosts = [hosts.incoming, hosts.outgoing].filter(Boolean) as HTMLElement[];
        bandTarget = inHeight;
        // Pinned with the transition off: a band laid out as a grid starts transitioning from its
        // laid-out height the moment a pixel height is written, and the pin never lands.
        bandHosts.forEach(host => {
          const band = bandOf(host);
          if (band) band.style.transition = 'none';
          host.style.setProperty('--lookup-head-height', `${outHeight}px`);
        });
      }

      // Coming back while still on its way out: park it below first, unseen, so it arrives from
      // below rather than returning from the top.
      if (hosts?.incoming?.hasAttribute('data-tab-leaving')) hosts.incoming.setAttribute('data-tab-reset', '');

      const from = heightOf(el);
      pending = naturalIncoming;
      if (Math.abs(from - pending) < 0.5) {
        pending = undefined;
        return;
      }
      // Hold the height the region shows through the patch that puts the incoming tab in flow.
      pin(el, from);
    },

    afterSwitch(incomingHost?: HTMLElement) {
      const el = region();
      if (!el) return;

      if (incomingHost?.hasAttribute('data-tab-reset')) {
        // One reflow with the content parked below, then the mark lifts and it arrives from there.
        const root = incomingHost.shadowRoot;
        root?.querySelectorAll('.lookup-head-content, .lookup-slide').forEach(node => void (node as HTMLElement).offsetHeight);
        incomingHost.removeAttribute('data-tab-reset');
      }

      if (bandTarget !== undefined && bandHosts.length) {
        // One reflow with the bands held, then both go to the incoming height with the slide.
        bandHosts.forEach(host => void bandOf(host)?.offsetHeight);
        const target = bandTarget;
        bandHosts.forEach(host => {
          const band = bandOf(host);
          if (band) band.style.transition = '';
          host.style.setProperty('--lookup-head-height', `${target}px`);
        });
        bandTimer = setTimeout(releaseBands, settleMs(el));
      }

      if (pending === undefined) return;
      const to = pending;
      pending = undefined;
      settleTo(el, to);
    },

    observe(host: Element | undefined) {
      if (host === observed) return;

      observer?.disconnect();
      observed = host;
      observedHeight = undefined;
      if (!host || typeof ResizeObserver === 'undefined') return;

      observer = new ResizeObserver(entries => {
        const el = region();
        if (!el) return;

        const entry = entries[entries.length - 1];
        const next = entry.borderBoxSize?.[0]?.blockSize ?? entry.contentRect.height;

        // The first notification is the baseline, not a change.
        if (observedHeight === undefined) {
          observedHeight = next;
          return;
        }
        observedHeight = next;

        // Follow the panel, do not re-animate it. The panel resizes its own body on the same clock
        // and the observer only hears about it a frame later, so animating the region too put a
        // second height transition on top of one already running and the shell trailed its content.
        // `height: auto` tracks it frame for frame. A tab SWITCH still animates: there the region
        // really does move between two panels' heights.
        // Only a leftover pinned height from a finished switch needs clearing, and only once:
        // releasing on every tick restored the inline height dozens of times and made it stutter.
        if (settleTimer || !el.style.height) return;
        release();
      });
      observer.observe(host);
    },

    dispose() {
      observer?.disconnect();
      observer = undefined;
      observed = undefined;
      if (settleTimer) clearTimeout(settleTimer);
      if (bandTimer) clearTimeout(bandTimer);
      releaseBands();
      release();
    },
  };
};
