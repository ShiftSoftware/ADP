import { Component, Element, Host, Method, Prop, State, Watch, forceUpdate, h } from '@stencil/core';

import { LookupHead, VehicleInfoLayout } from '~features/vehicle-info-layout';
import {
  ClosingDrawer,
  PageSize,
  PlaceholderKind,
  closingDrawers,
  drawerAfterLookup,
  drawerAfterPage,
  pageForSize,
  pageRows,
  readStoredPageSize,
  revealDrawer,
  storePageSize,
} from '~features/lookup-table';
import type { LanguageKeys } from '~features/multi-lingual';
import { createHeightChangeAnnouncer } from '~lib/flexible-parents';

import { MANY_VISITS, SHOWCASE_STRINGS, VISITS, Visit, showcaseDirection } from './lookup-table-showcase-fixtures';
import { Outcome, SCENARIOS, SHOWCASE_PAGE_SIZE_KEY, ShowcaseContext, placeholderFor, visitKey } from './lookup-table-showcase-scenarios';

const DEFAULT_SETTLE_MS = 480;
/** How long a "look up again" takes to answer. */
const LOOKUP_MS = 1600;

/**
 * The lookup table's dev showcase: a host that renders `LookupTable` (and the pager) from a named
 * scenario, in the family's own card, so the functional component can be seen outside a panel. It
 * is the page `templates/vehicle-lookup/lookup-table.html` drives, and it is not published: there
 * is no dev-only component mechanism, so the tag ships in the bundle as an undocumented lazy
 * element and does nothing until a page puts it on screen (lookup-table.md § 13, question 5).
 *
 * The scenarios are `SCENARIOS` in lookup-table-showcase-scenarios.tsx; `catalogue()` lists them.
 * The host owns only what a scenario needs to be interactive: which button is open or spinning and
 * the pager's page and size. For the drawer scenarios (those with a `dataset`) it is also the
 * *owner* of the drawer, and the reference for what a panel wires: the open key, the closing list
 * (`closingDrawers`) cleared a settle after each change, the open key after a lookup
 * (`drawerAfterLookup`) and after a page change (`drawerAfterPage`), `revealDrawer` from
 * `componentDidRender` when the open key changes, and the height announcer.
 */
@Component({
  shadow: true,
  tag: 'lookup-table-showcase',
  styleUrl: 'lookup-table-showcase.css',
})
export class LookupTableShowcase {
  @Element() el: HTMLElement;

  /** The language of the words, and the direction: Arabic and Kurdish flip the card to right-to-left. */
  @Prop() language: LanguageKeys = 'en';

  /** The name of a scenario (a key of `SCENARIOS`; see `catalogue()`). An unknown name shows the first. */
  @Prop({ mutable: true }) scenario: string = 'loaded';

  @State() expandedKey: string | null = null;
  @State() fetchingKey: string | null = null;
  @State() page = 0;
  @State() pageSize: PageSize = readStoredPageSize(SHOWCASE_PAGE_SIZE_KEY);

  /** Drawers sliding shut: their content stays mounted until it has shut (`RowActions.closing`). */
  @State() closing: ClosingDrawer<Visit>[] = [];
  /** The answer a "look up again" has brought: an index into the scenario's `lookups`. */
  @State() lookupIndex = 0;
  @State() looking = false;
  @State() fading = false;
  /** The placeholder scenario's last outcome: an index into the scenario's `outcomes`. */
  @State() outcomeIndex = 0;
  /** The panel is clearing: the rows are shutting under the skeleton and the idle placeholder opens. */
  @State() clearing = false;
  /** Rows an outcome without rows is still shutting over, kept a settle so they slide away with the body (rule 5). */
  @State() heldRows: Visit[] = [];

  private fetchTimer: ReturnType<typeof setTimeout> | undefined;
  private lookupTimer: ReturnType<typeof setTimeout> | undefined;
  private closingTimer: ReturnType<typeof setTimeout> | undefined;
  private pageTimer: ReturnType<typeof setTimeout> | undefined;
  private heldTimer: ReturnType<typeof setTimeout> | undefined;
  private placeholderTimer: ReturnType<typeof setTimeout> | undefined;
  /** The placeholder's last kind, kept a settle after the region shut, as an owner keeps it. Not state: render reads it and the timer forces a render. */
  private retainedPlaceholder: PlaceholderKind | null = null;

  private heightAnnouncer?: ReturnType<typeof createHeightChangeAnnouncer>;
  private heightSignature?: string;
  /** The open key the page is following, and the way to let go of it. */
  private followed: string | null = null;
  private stopFollowing?: () => void;

  private settleMs(): number {
    const raw = typeof getComputedStyle === 'function' ? getComputedStyle(this.el).getPropertyValue('--settle') : '';
    const parsed = parseFloat(raw);
    return (Number.isFinite(parsed) && parsed > 0 ? parsed : DEFAULT_SETTLE_MS) + 40;
  }

  componentWillLoad() {
    this.reset();
  }

  /** The scenarios, in the order they are defined: what the page builds its chips from. */
  @Method()
  async catalogue(): Promise<{ name: string; label: string; note: string }[]> {
    return Object.entries(SCENARIOS).map(([name, { label, note }]) => ({ name, label, note }));
  }

  /** A new scenario starts from rest: nothing open, nothing spinning, the first page. */
  @Watch('scenario')
  reset() {
    clearTimeout(this.fetchTimer);
    clearTimeout(this.lookupTimer);
    clearTimeout(this.closingTimer);
    clearTimeout(this.pageTimer);
    clearTimeout(this.heldTimer);
    clearTimeout(this.placeholderTimer);
    this.placeholderTimer = undefined;
    this.retainedPlaceholder = null;
    this.stopFollowing?.();
    this.stopFollowing = undefined;

    const initial = this.current().initialOpen ?? null;
    this.expandedKey = initial;
    // Starting open is a state, not a change: the page is not taken to the row on load.
    this.followed = initial;
    this.fetchingKey = null;
    this.page = 0;
    this.closing = [];
    this.lookupIndex = 0;
    this.looking = false;
    this.fading = false;
    this.outcomeIndex = 0;
    this.clearing = false;
    this.heldRows = [];
  }

  componentDidRender() {
    // The page follows the row whose drawer opens, from the frame it opens; closing, or another key, lets go.
    if (this.expandedKey !== this.followed) {
      this.stopFollowing?.();
      this.stopFollowing = undefined;
      this.followed = this.expandedKey;
      if (this.expandedKey && this.el.shadowRoot) this.stopFollowing = revealDrawer(this.el.shadowRoot, this.expandedKey, this.settleMs());
    }

    // Everything that moves the card's height, joined: a render that changes none of it announces nothing.
    const signature = [
      this.expandedKey,
      this.closing.length,
      this.looking,
      this.fading,
      this.lookupIndex,
      this.page,
      this.pageSize,
      this.outcomeIndex,
      this.clearing,
      this.heldRows.length,
    ].join('|');
    const changed = this.heightSignature !== undefined && this.heightSignature !== signature;
    this.heightSignature = signature;
    if (!changed) return;

    this.heightAnnouncer ??= createHeightChangeAnnouncer(this.el);
    this.heightAnnouncer.announce(this.settleMs() + 80);
  }

  disconnectedCallback() {
    clearTimeout(this.fetchTimer);
    clearTimeout(this.lookupTimer);
    clearTimeout(this.closingTimer);
    clearTimeout(this.pageTimer);
    clearTimeout(this.heldTimer);
    clearTimeout(this.placeholderTimer);
    this.stopFollowing?.();
    this.heightAnnouncer?.dispose();
  }

  private current() {
    return SCENARIOS[this.scenario] ?? Object.values(SCENARIOS)[0];
  }

  /** The rows the main table shows in a drawer scenario: the page of the dataset, or the last answer. None elsewhere. */
  private shownRows(): Visit[] {
    const scenario = this.current();
    if (!scenario.dataset) return [];

    const answer = scenario.lookups?.[this.lookupIndex] ?? scenario.dataset;
    return scenario.paged ? pageRows(answer, this.page, this.pageSize) : answer;
  }

  /** Lets the closing drawers go a settle after the change that shut them, when their region is shut. */
  private scheduleClosingClear() {
    clearTimeout(this.closingTimer);
    if (!this.closing.length) return;
    this.closingTimer = setTimeout(() => (this.closing = []), this.settleMs() + 80);
  }

  /** Moves the open key and keeps the drawer that gives way mounted while it slides shut. */
  private open(next: string | null, nextRows: Visit[] = this.shownRows()) {
    const rows = this.shownRows();
    this.closing = closingDrawers({ closing: this.closing, previousKey: this.expandedKey, nextKey: next, previousRows: rows, nextRows, keyOf: visitKey });
    this.expandedKey = next;
    this.scheduleClosingClear();
  }

  /** Single open: a key opens, the same key shuts, another key moves it. A scenario that fetches spins first. */
  private toggle = (key: string) => {
    if (this.fetchingKey !== null || this.looking || this.fading) return;
    if (this.expandedKey === key) {
      this.open(null);
      return;
    }

    if (!this.current().fetchOnOpen) {
      this.open(key);
      return;
    }

    this.fetchingKey = key;
    this.fetchTimer = setTimeout(() => {
      this.fetchingKey = null;
      this.open(key);
    }, 1400);
  };

  private setPage = (page: number) => {
    const scenario = this.current();
    if (!scenario.paged || this.fading) {
      this.page = page;
      return;
    }

    // Paging away (run decision 9): the open drawer shuts first, with the old rows still on screen and their values
    // fading; the page swaps once it has shut, and nothing is kept for the way back.
    const answer = scenario.lookups?.[this.lookupIndex] ?? scenario.dataset;
    const nextRows = pageRows(answer, page, this.pageSize);
    const nextKey = drawerAfterPage(
      this.expandedKey,
      nextRows.map((row, index) => visitKey(row, index)),
    );
    if (this.expandedKey === null) {
      this.page = page;
      return;
    }

    this.open(nextKey);
    this.fading = true;
    clearTimeout(this.pageTimer);
    this.pageTimer = setTimeout(() => {
      this.page = page;
      this.fading = false;
    }, this.settleMs());
  };

  private setPageSize = (size: PageSize) => {
    const scenario = this.current();
    const total = (scenario.dataset ?? MANY_VISITS).length;
    // The page that keeps the first row on screen in view, as the real panels do.
    const page = pageForSize(this.page, this.pageSize, size, total);

    if (scenario.paged) {
      const nextRows = pageRows(scenario.lookups?.[this.lookupIndex] ?? scenario.dataset, page, size);
      this.open(
        drawerAfterPage(
          this.expandedKey,
          nextRows.map((row, index) => visitKey(row, index)),
        ),
        nextRows,
      );
    }

    this.page = page;
    this.pageSize = size;
    storePageSize(SHOWCASE_PAGE_SIZE_KEY, size);
  };

  /**
   * "Look up again" (run decision 2): the open drawer stays open under the skeleton while the lookup is in
   * flight; when the next answer arrives the rows are swapped under the cover and the drawer stays open only
   * if a row with its key is among the rows SHOWN then, otherwise it slides shut. A paged panel goes back to its
   * first page on a lookup (run decision 8), so "shown" is the first page, not the whole answer: an invoice now
   * on page 3 is off screen and does not stay open.
   */
  private relookup = () => {
    const scenario = this.current();
    if (this.looking) return;

    // A placeholder scenario: the next outcome in its list, under the skeleton for the same 1.6 seconds.
    if (scenario.outcomes) {
      this.looking = true;
      this.lookupTimer = setTimeout(() => {
        const next = this.outcomeIndex + 1;
        const index = next < scenario.outcomes.length ? next : 1; // wrap past idle: Clear is the way back to it
        this.land(scenario.outcomes[index]);
        this.outcomeIndex = index;
        this.looking = false;
      }, LOOKUP_MS);
      return;
    }

    if (!scenario.lookups) return;

    this.looking = true;
    this.lookupTimer = setTimeout(() => {
      const index = (this.lookupIndex + 1) % scenario.lookups.length;
      const answer = scenario.lookups[index];
      const nextRows = scenario.paged ? pageRows(answer, 0, this.pageSize) : answer;
      const next = drawerAfterLookup(this.expandedKey, nextRows, visitKey);

      this.open(next, nextRows);
      this.lookupIndex = index;
      this.page = 0;
      this.looking = false;
    }, LOOKUP_MS);
  };

  /** The outcome the last lookup ended in; `idle` for a scenario that has none. */
  private outcome(): Outcome {
    return this.current().outcomes?.[this.outcomeIndex] ?? 'idle';
  }

  /** Moves to an outcome. Rows an outcome without rows replaces are kept a settle, so the body shuts over them. */
  private land(next: Outcome) {
    if (this.outcome() === 'rows' && next !== 'rows') {
      this.heldRows = VISITS;
      clearTimeout(this.heldTimer);
      this.heldTimer = setTimeout(() => (this.heldRows = []), this.settleMs());
    }
  }

  /**
   * An owner's retention of the placeholder (rule 5): remembers its kind, and forgets it a settle after the
   * region shut, so the words slide away with it and no stale sentence is under a region that opens next.
   */
  private retainPlaceholder(kind: PlaceholderKind | null) {
    if (kind !== null) {
      this.retainedPlaceholder = kind;
      clearTimeout(this.placeholderTimer);
      this.placeholderTimer = undefined;
      return;
    }

    if (this.retainedPlaceholder !== null && !this.placeholderTimer) {
      this.placeholderTimer = setTimeout(() => {
        this.placeholderTimer = undefined;
        this.retainedPlaceholder = null;
        forceUpdate(this);
      }, this.settleMs());
    }
  }

  /** Clear: the rows leave under the skeleton and the idle placeholder opens; after a settle the state is idle. */
  private clear = () => {
    const scenario = this.current();
    if (this.looking || !scenario.outcomes) return;

    this.looking = true;
    this.clearing = true;
    this.lookupTimer = setTimeout(() => {
      this.land('idle');
      this.outcomeIndex = 0;
      this.clearing = false;
      this.looking = false;
    }, this.settleMs());
  };

  render() {
    const scenario = this.current();
    const t = SHOWCASE_STRINGS[this.language] ?? SHOWCASE_STRINGS.en;

    // The placeholder scenario's table is an owner's: it keeps the kind a settle after its region shuts.
    const outcome = this.outcome();
    this.retainPlaceholder(scenario.outcomes ? placeholderFor(outcome, this.clearing) : null);

    const context: ShowcaseContext = {
      lang: this.language,
      t,
      expandedKey: this.expandedKey,
      fetchingKey: this.fetchingKey,
      page: this.page,
      pageSize: this.pageSize,
      rows: this.shownRows(),
      closing: this.closing,
      looking: this.looking,
      fading: this.fading,
      outcome,
      clearing: this.clearing,
      heldRows: this.heldRows,
      retainedPlaceholder: this.retainedPlaceholder,
      toggle: this.toggle,
      setPage: this.setPage,
      setPageSize: this.setPageSize,
      relookup: this.relookup,
      clear: this.clear,
    };

    return (
      <Host translate="no">
        <VehicleInfoLayout
          isError={scenario.verdict === 'negative'}
          isLoading={!!scenario.busy || this.looking}
          verdict={scenario.verdict ?? 'idle'}
          direction={showcaseDirection(this.language)}
          header={t.title}
        >
          <LookupHead title={t.title} verdict={{ state: 'idle', text: '' }} />
          {scenario.render(context)}
        </VehicleInfoLayout>
      </Host>
    );
  }
}
