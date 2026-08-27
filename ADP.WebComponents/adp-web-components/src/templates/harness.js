/**
 * adp-web-components — dev harness controls (Alpine).
 *
 * Provides the `harness()` Alpine data factory and stamps the shared chrome into
 * the placeholder elements below, so a page describes what it is testing instead
 * of re-implementing navigation and the same six controls:
 *
 *   <div data-harness-nav x-cloak></div>
 *
 *   <section x-data="harness({ subject: '#subject', mocks: 'vehicle-lookup' })">
 *     <div data-harness-bar x-cloak></div>
 *     <div class="frame">…the component…</div>
 *     <div data-harness-rail x-cloak></div>
 *   </section>
 *
 * There are three stamped pieces:
 *
 *   [data-harness-nav]   The sticky bar at the top of the page: one dropdown per
 *                        area, every page in it, read from catalog.json. It sits
 *                        outside the harness scope and carries its own x-data.
 *
 * …and the controls, split by how often you touch them:
 *
 *   [data-harness-bar]   Mode and Fixtures, inline above the component. Reached
 *                        in one click, because that is every interaction.
 *   [data-harness-rail]  Language, theme, platform width and the event log, in a
 *                        drawer behind a tab on the right edge (a button on a
 *                        phone). Set once, then forgotten.
 *
 * The rail used to be a 300px aside, which cost the component under test a third
 * of the page on every demo. Its stamped markup is all `position: fixed`, so the
 * placeholder div can go anywhere inside the harness scope.
 *
 * `x-data` goes on a wrapper around BOTH the frame and the rail, not on the rail
 * itself — that way a page-level control (an external submit button, a tab strip
 * driving `activeElement`) binds to the same state the rail does.
 *
 * A page that needs a different rail writes its own markup against the same
 * state — every method below is public. A page script with no Alpine scope at
 * all can still reach the log:
 *
 *   window.dispatchEvent(new CustomEvent('harness:log', { detail: { label: 'GET', detail: url } }));
 *
 * Control order is fixed on purpose (R5), and the language control flips page
 * direction because half of the supported set is RTL (R6). See
 * .shift/repos/adp/web-components/templates-design-language.md.
 */

/** Mirrors a component's loading flag onto the harness, so page controls can disable. */
function track(isLoading) {
  this.loading = isLoading;

  return [isLoading ? 'loading' : 'idle', ''];
}

/**
 * Same idea for the error message: held on the harness so a page can render it
 * next to its own input. Components clear the error by reporting an empty one,
 * which is worth no log line.
 */
function fail(message) {
  this.error = message ? String(message) : '';

  return this.error ? ['error', this.error] : null;
}

/**
 * The four component shapes these demos drive. A profile is only a bundle of
 * defaults — every field it sets can still be overridden per page.
 *
 * `events` maps a component callback property to a log line. Handlers run with
 * the harness as `this`, and one that returns nothing logs nothing, which is how
 * a callback can be used purely for its side effect.
 */
const PROFILES = {
  // vehicle-lookup family: a VIN goes in, one response comes back.
  lookup: {
    apply: (subject, data) => subject.setMockData(data),
    select: (subject, key) => subject.fetchVin(key),
    endpoint: subject => subject.baseUrl,
    events: {
      loadingStateChange: track,
      errorCallback: fail,
      loadedResponse: response => ['loaded', summarise(response)],
    },
  },

  // part-lookup family: a part number goes in, and the component loads its own
  // mock file when `isDev` flips rather than being handed one.
  part: {
    apply: null,
    select: (subject, key) => subject.fetchData(key),
    endpoint: subject => subject.endpoint?.url,
    events: {
      loadingStateChange: track,
      errorCallback: fail,
      loadedResponse: response => ['loaded', summarise(response)],
      loadedMockDatas(data) {
        const keys = Object.keys(data ?? {});

        this.setFixtures(keys, data ?? {});

        return ['mocks', `${keys.length} part ${keys.length === 1 ? 'number' : 'numbers'}`];
      },
    },
  },

  // forms: no fixture list — the structure JSON is the input, and what matters
  // is the submit lifecycle.
  form: {
    apply: null,
    select: null,
    endpoint: subject => subject.structureUrl || subject.getAttribute('structure-url'),
    events: {
      formReadyCallback() {
        this.ready = true;

        return ['ready', 'structure loaded'];
      },
      loadingChanges(loading) {
        this.loading = loading;

        return [loading ? 'submitting' : 'idle', ''];
      },
      successCallback: data => ['success', summarise(data)],
      errorCallback(error, message) {
        this.error = String(message || error || '');

        return ['error', this.error];
      },
    },
  },

  // vehicle-lookup: the shell that holds the eight single-vehicle components and
  // shows one at a time. It reports its own state rather than the children's.
  composite: {
    apply: null,
    endpoint: subject => subject.baseUrl,
    // In development, push the fixture into every tab at once instead of only the
    // active one, which is what `fetchVin` does — otherwise switching tabs shows
    // whatever the previous search left behind. Empty and Error have no fixture,
    // so they go the normal route and let each child render its own state.
    select: (subject, key, harness) => {
      const mock = harness.mode === 'dev' && harness.data?.[key];

      return mock ? subject.handleLoadData(mock, null) : subject.fetchVin(key, { headers: 'headers-value' });
    },
    events: {
      loadingStateChanged: track,
      errorStateListener: fail,
      dynamicClaimActivate: information => ['claim activate', information?.vin ?? '(no vin)'],
    },
  },
};

const DEFAULTS = {
  subject: '#subject',
  profile: 'lookup',
  mocks: null,
  labels: {},
  // Fixture keys to offer in production mode, where there is no mock file to
  // generate them from. Development mode always uses the generated list (R7).
  samples: [],
  // Fixture to run as soon as the subject is ready, so a page can deep-link a
  // scenario (`?vin=…`) instead of asking whoever opened the link to click.
  start: null,
  // Which fixtures are worth offering on THIS page. One mock file feeds eight
  // components and most VINs carry data for only some of them, so a page says
  // what its component actually needs — `has: vehicle => vehicle.accessories?.length`
  // — and the rest are counted out rather than sitting there rendering nothing.
  has: null,
  languages: ['en', 'ku', 'ar', 'ru'],
  rtl: ['ar', 'ku'],
  platforms: [
    { label: '360', value: '360px' },
    { label: '414', value: '414px' },
    { label: '768', value: '768px' },
    { label: 'Full', value: '100%' },
  ],
  language: true,
  theme: true,
  mode: true,
  platform: true,
  log: true,
  fixtures: null,
  events: null,
  // Runs once the subject has upgraded — the place for object-valued props that
  // cannot be written as an attribute, such as part-lookup's `endpoint`.
  setup: null,
  // `undefined` means "take the profile's"; `null` means "this page has none".
  apply: undefined,
  select: undefined,
  endpoint: undefined,
};

window.harness = function harness(options = {}) {
  const config = { ...DEFAULTS, ...options };
  const profile = PROFILES[config.profile] ?? PROFILES.lookup;

  const apply = config.apply === undefined ? profile.apply : config.apply;
  const select = config.select === undefined ? profile.select : config.select;
  const endpoint = config.endpoint === undefined ? profile.endpoint : config.endpoint;
  // `false` wires nothing: a page demonstrating the Blazor bridge needs those
  // same callback props left as the attribute strings the component dispatches
  // through the .NET ref, and it feeds the log with `harness:log` instead.
  const events = config.events === false ? {} : { ...profile.events, ...config.events };

  return {
    config,
    subject: null,

    languages: config.languages,
    platforms: config.platforms,
    show: {
      language: config.language,
      theme: config.theme,
      mode: config.mode,
      platform: config.platform,
      log: config.log,
      fixtures: (config.fixtures ?? Boolean(config.mocks || config.samples.length)) && Boolean(select),
      // The bar only earns its space when it has something in it.
      get bar() {
        return this.mode || this.fixtures;
      },
    },

    language: config.languages[0],
    theme: window.harnessTheme?.current() ?? 'system',
    // Development is the default: it is the only mode that works without a local
    // API running, which is how these pages are opened most of the time.
    mode: 'dev',
    // The rail is a drawer, not a column: it would otherwise take a third of the
    // width away from the thing under test on every page.
    open: false,
    width: '100%',
    loading: false,
    // Last error the subject reported, so a page can show it beside its own input.
    error: '',
    // The loaded mock set, for a page whose `select` needs the fixture itself and
    // not just its key.
    data: null,
    keys: [],
    // How many fixtures `has` filtered out. Shown, never silently dropped.
    hidden: 0,
    fixture: null,
    // Forms flip this from formReadyCallback; an external submit button waits on it.
    ready: false,
    lines: [],

    get dir() {
      return config.rtl.includes(this.language) ? 'rtl' : 'ltr';
    },

    /** Empty and Error are always offered; the rest is generated, never listed (R7). */
    get fixtures() {
      const keys = this.mode === 'dev' ? this.keys : config.samples;

      return [{ value: '', label: 'Empty' }, { value: 'error', label: 'Error' }, ...keys.map(key => ({ value: key, label: key, note: config.labels[key] }))];
    },

    async init() {
      // Any script on the page can write to the log without an Alpine scope.
      window.addEventListener('harness:log', event => this.write(event.detail?.label, event.detail?.detail));

      // A gallery page drives several elements itself and has no single subject.
      if (config.subject === false) return;

      this.subject = document.querySelector(config.subject);

      if (!this.subject) return this.write('harness', `no element matches ${config.subject}`);

      // Properties set before upgrade would be shadowed by the prototype
      // accessors, and @Method members do not exist yet. Wait for both.
      await customElements.whenDefined(this.subject.localName);
      await this.subject.componentOnReady?.();

      await config.setup?.(this.subject, this);

      if (config.language) this.setLanguage(this.subject.language || this.language);

      // Before isDev, not after: flipping it makes some components load their own
      // mock file and call straight back, so the listeners have to be in place.
      this.observe();
      this.subject.isDev = true;

      if (config.mocks) await this.loadFixtures();
      if (config.start) this.run(config.start);
    },

    /* ---------- controls ---------- */

    setLanguage(language) {
      this.language = language;
      this.subject.language = language;

      document.documentElement.lang = language;
      document.documentElement.dir = this.dir;

      // Kept in the URL so a reload lands in the same language — and because the
      // form components read `?lang=` themselves when no language prop is set.
      const url = new URL(window.location.href);

      url.searchParams.set('lang', language);
      window.history.replaceState({}, '', url);

      this.write('language', `${language} · ${this.dir}`);
    },

    setOpen(open) {
      this.open = open;

      // Locked only where the drawer is a full-screen modal. On a desktop it opens
      // on hover, and locking there would freeze the page under the pointer and
      // shift the layout as the scrollbar goes. 64rem is Tailwind's `lg`.
      const isModal = !window.matchMedia('(min-width: 64rem)').matches;

      document.body.classList.toggle('overflow-hidden', open && isModal);
    },

    setTheme(theme) {
      this.theme = theme;

      // harness-theme.js owns this so the choice survives navigation. Pages that
      // do not load it still get a working control, just not a sticky one.
      if (window.harnessTheme) window.harnessTheme.apply(theme);
      else if (theme === 'system') delete document.documentElement.dataset.theme;
      else document.documentElement.dataset.theme = theme === 'dark' ? 'harness-dark' : 'harness';

      this.write('theme', theme);
    },

    setMode(mode) {
      this.mode = mode;
      this.subject.isDev = mode === 'dev';

      this.write('mode', mode === 'dev' ? 'development · mock data' : `production · ${endpoint?.(this.subject) || 'no endpoint set'}`);
    },

    setWidth(width) {
      this.width = width;

      // Every frame on the page, so a gallery of several configurations resizes
      // together instead of only wherever the subject happens to be.
      document.querySelectorAll('.frame').forEach(frame => frame.style.setProperty('--frame-w', width));

      this.write('width', width);
    },

    /* ---------- fixtures ---------- */

    async loadFixtures() {
      let data = {};

      try {
        data = (await window.loadMockData(config.mocks)) ?? {};
        this.data = data;

        await apply?.(this.subject, data);
      } catch (error) {
        this.write('fixtures', `failed to load — ${error.message}`);
      }

      // Generated from the mock keys, never hand-listed: a hardcoded list drifts
      // the moment someone adds a scenario (R7). Labels only annotate.
      this.setFixtures(Object.keys(data), data);
    },

    /** Public, so a component that pushes its own mock set can feed the rail. */
    setFixtures(keys, data) {
      if (data) this.data = data;

      // An annotated fixture always survives the filter. A label means someone
      // picked that key deliberately — often precisely to show an empty or null
      // state, which is the one case `has` would otherwise throw away.
      const keep = key => Boolean(config.labels[key]) || Boolean(config.has(this.data?.[key], key));

      this.keys = config.has ? keys.filter(keep) : keys;
      this.hidden = keys.length - this.keys.length;
    },

    run(key) {
      this.fixture = key;
      this.write('fetch', key === '' ? '(empty)' : key);

      select?.(this.subject, key, this);
    },

    reload() {
      this.run(this.fixture ?? '');
    },

    submit() {
      this.subject?.submit?.();
    },

    /* ---------- live events ---------- */

    observe() {
      for (const [property, handler] of Object.entries(events)) {
        this.subject[property] = (...args) => {
          const line = handler.apply(this, args);

          if (line) this.write(line[0], line[1]);

          // The form components read this back — `!!callback(…)` is what decides
          // whether the built-in success/error dialog opens, and true is what
          // they do when no callback is set at all, so observing must not
          // silently suppress it. A handler returning false opts out; every
          // other callback ignores the value.
          return line !== false;
        };
      }
    },

    write(label, detail) {
      // Bound with x-text, so component-supplied text is never parsed as markup.
      this.lines.unshift({ id: `${label}-${this.lines.length}-${detail}`, label, detail: String(detail ?? '') });

      if (this.lines.length > 200) this.lines.pop();
    },
  };
};

function summarise(response) {
  if (!response) return '(nothing)';
  if (typeof response !== 'object') return String(response);

  const vin = response.vin ?? response.identifiers?.vin;
  const keys = Object.keys(response).length;

  return vin ? `${vin} · ${keys} keys` : `${keys} keys`;
}

/* ---------- the standard rail ---------- */

/**
 * The navigation, read from catalog.json — one dropdown per area, every page in
 * it. A hand-written nav on 26 pages is 26 places to forget a new demo, so the
 * same rule as fixtures applies: generated, never listed (R7).
 */
window.harnessNav = function harnessNav() {
  return {
    areas: [],
    open: null,
    path: window.location.pathname,

    async init() {
      try {
        const response = await fetch('/templates/catalog.json');

        if (!response.ok) return;

        const catalog = await response.json();

        this.areas = catalog.areas.map(area => ({ ...area, pages: catalog.pages.filter(page => page.area === area.id) }));
      } catch {
        // No catalog: the bar collapses to the wordmark, which still goes home.
      }
    },

    /** Which area the page being viewed belongs to, so the bar says where you are. */
    get area() {
      return this.areas.find(group => group.pages.some(page => page.path === this.path))?.id ?? null;
    },

    isCurrent(page) {
      return page.path === this.path;
    },

    toggle(id) {
      this.open = this.open === id ? null : id;
    },
  };
};

/** Chevron, shared by every navigation trigger. */
const CHEVRON = /* html */ `
  <svg class="h-3 w-3 opacity-60 transition-transform" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
    <path d="m6 9 6 6 6-6" />
  </svg>
`;

const NAV = /* html */ `
  <div class="bg-base-100/90 border-base-300 sticky top-0 z-30 border-b border-dashed backdrop-blur">
    <div class="mx-auto flex max-w-[1400px] items-center gap-2 px-4 py-2 sm:px-8" @click.outside="open = null" @keydown.escape.window="open = null">
      <a href="/" class="btn btn-sm btn-ghost px-2" aria-label="Showcase home">
        <span class="adp-logo text-accent h-4 w-[40px]" aria-hidden="true"></span>
      </a>

      <span class="bg-base-300 h-5 w-px" aria-hidden="true"></span>

      <!-- Wide: one dropdown per area, so a group is one click away. -->
      <nav class="hidden flex-wrap items-center gap-0.5 md:flex" aria-label="Demo pages">
        <template x-for="group in areas" :key="group.id">
          <div class="relative">
            <button
              type="button"
              class="btn btn-sm btn-ghost gap-1.5"
              :class="group.id === area && 'text-accent'"
              :aria-expanded="open === group.id"
              @click="toggle(group.id)"
            >
              <span x-text="group.label"></span>
              <span class="badge badge-xs badge-ghost tabular-nums" x-text="group.count"></span>
              <span :class="open === group.id && 'rotate-180'" class="transition-transform">${CHEVRON}</span>
            </button>

            <ul
              class="menu bg-base-100 border-base-300 rounded-box absolute start-0 top-full z-50 mt-1 w-72 flex-nowrap border p-1 shadow-xl"
              x-show="open === group.id"
              x-transition:enter="transition ease-out duration-150"
              x-transition:enter-start="opacity-0 -translate-y-1"
              x-transition:leave="transition ease-in duration-100"
              x-transition:leave-end="opacity-0"
            >
              <template x-for="page in group.pages" :key="page.path">
                <li>
                  <a :href="page.path" :class="isCurrent(page) && 'menu-active'" :aria-current="isCurrent(page) ? 'page' : null">
                    <span class="truncate" x-text="page.title"></span>
                    <span class="badge badge-xs badge-ghost ms-auto shrink-0" x-show="page.kind !== 'demo'" x-text="page.kind"></span>
                  </a>
                </li>
              </template>
            </ul>
          </div>
        </template>
      </nav>

      <!-- Narrow: the same tree, nested one level down, because six dropdowns do not fit. -->
      <div class="relative md:hidden">
        <button type="button" class="btn btn-sm btn-ghost gap-1.5" :aria-expanded="open === 'all'" @click="toggle('all')">
          <span x-text="areas.find(group => group.id === area)?.label ?? 'Pages'"></span>
          <span :class="open === 'all' && 'rotate-180'" class="transition-transform">${CHEVRON}</span>
        </button>

        <ul
          class="menu bg-base-100 border-base-300 rounded-box absolute start-0 top-full z-50 mt-1 max-h-[70vh] w-[min(20rem,calc(100vw-2rem))] flex-nowrap overflow-y-auto border p-1 shadow-xl"
          x-show="open === 'all'"
          x-transition:enter="transition ease-out duration-150"
          x-transition:enter-start="opacity-0 -translate-y-1"
          x-transition:leave="transition ease-in duration-100"
          x-transition:leave-end="opacity-0"
        >
          <template x-for="group in areas" :key="group.id">
            <li>
              <details :open="group.id === area">
                <summary>
                  <span x-text="group.label"></span>
                  <span class="badge badge-xs badge-ghost tabular-nums" x-text="group.count"></span>
                </summary>
                <ul>
                  <template x-for="page in group.pages" :key="page.path">
                    <li>
                      <a :href="page.path" :class="isCurrent(page) && 'menu-active'" :aria-current="isCurrent(page) ? 'page' : null" x-text="page.title"></a>
                    </li>
                  </template>
                </ul>
              </details>
            </li>
          </template>
        </ul>
      </div>
    </div>
  </div>
`;

/*
 * Mode and Fixtures sit ABOVE the component, not in the drawer. They are the two
 * controls that get touched on every single interaction, and making someone open
 * a panel to pick a VIN was the worst thing about the first pass. Everything
 * you set once and forget — language, theme, width, the log — stays in the drawer.
 */
const BAR = /* html */ `
  <template x-if="show.bar">
    <div class="card border-base-300 bg-base-100 flex flex-col gap-2 border border-dashed p-3">
      <div class="flex flex-wrap items-start gap-x-5 gap-y-3">
        <template x-if="show.mode">
          <div class="flex shrink-0 items-center gap-2">
            <span class="eyebrow text-neutral">Mode</span>
            <div class="flex gap-1.5" role="group" aria-label="Mode">
              <template x-for="item in [{ value: 'dev', label: 'Development' }, { value: 'prod', label: 'Production' }]" :key="item.value">
                <button type="button" class="btn btn-xs" :class="mode === item.value ? 'btn-primary' : 'btn-outline'"
                        :aria-pressed="mode === item.value" @click="setMode(item.value)" x-text="item.label"></button>
              </template>
            </div>
          </div>
        </template>

        <template x-if="show.fixtures">
          <div class="flex min-w-0 flex-1 flex-wrap items-center gap-2">
            <span class="eyebrow text-neutral">Fixtures</span>
            <button type="button" class="btn btn-xs btn-outline" @click="reload()">Reload</button>

            <div class="flex flex-wrap gap-1.5" role="group" aria-label="Fixtures">
              <template x-for="item in fixtures" :key="item.value">
                <button type="button" class="btn btn-xs font-mono font-normal" :class="fixture === item.value ? 'btn-primary' : 'btn-outline'"
                        :aria-pressed="fixture === item.value" @click="run(item.value)" :title="item.note || item.label">
                  <span x-text="item.label"></span>
                  <span class="font-sans opacity-70" x-show="item.note" x-text="item.note"></span>
                </button>
              </template>
            </div>
          </div>
        </template>
      </div>

      <!-- Never a silent cut: a page that filters says how much it filtered. -->
      <p class="text-base-content/50 text-xs" x-show="hidden" x-text="hidden + ' fixture(s) hidden — no data for this component'"></p>
    </div>
  </template>
`;

/** Sliders glyph, shared by both drawer launchers. */
const ICON = /* html */ `
  <svg class="h-4 w-4 shrink-0" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
    <path d="M4 6h10M18 6h2M4 12h2M10 12h10M4 18h10M18 18h2" />
    <circle cx="16" cy="6" r="2" /><circle cx="8" cy="12" r="2" /><circle cx="16" cy="18" r="2" />
  </svg>
`;

/*
 * The rail is a drawer. On a desktop it advertises itself with a tab on the right
 * edge — an invisible hover zone is only usable by someone who already knows it
 * is there — and brushing anywhere along that edge opens it too. On a phone the
 * tab would be a thumb trap, so it becomes a floating button instead. Escape,
 * the close button and the scrim all get you back out.
 *
 * Everything here is pinned to the PHYSICAL right, not the inline end. The
 * language control flips the page to RTL for half the supported set, and chrome
 * that jumps to the other side of the screen when it does is disorienting — the
 * component under test is what should mirror, not the tooling around it.
 */
const RAIL = /* html */ `
  <!-- Phone: a floating button, because an edge tab sits exactly where a thumb scrolls. -->
  <button
    type="button"
    class="btn btn-sm btn-primary fixed right-4 bottom-4 z-40 gap-2 shadow-lg lg:hidden"
    aria-controls="harness-panel"
    :aria-expanded="open"
    @click="setOpen(!open)"
  >
    ${ICON}
    Harness
  </button>

  <!-- Desktop: a visible tab, so the drawer does not depend on anyone guessing it exists. -->
  <button
    type="button"
    class="border-base-300 bg-base-100 text-base-content/70 hover:text-accent hover:border-accent/50 fixed top-1/2 right-0 z-30 hidden -translate-y-1/2 cursor-pointer flex-col items-center gap-2 rounded-l-xl border border-r-0 border-dashed py-4 pr-1.5 pl-2 shadow-md transition-colors lg:flex"
    aria-controls="harness-panel"
    :aria-expanded="open"
    x-show="!open"
    @mouseenter="setOpen(true)"
    @click="setOpen(true)"
  >
    ${ICON}
    <span class="eyebrow [writing-mode:vertical-rl] tracking-[0.16em]">Harness</span>
  </button>

  <!-- …and brushing the edge anywhere along it opens the drawer as well. -->
  <div class="fixed inset-y-0 right-0 z-20 hidden w-2 lg:block" aria-hidden="true" @mouseenter="setOpen(true)"></div>

  <!-- Phone only: there the drawer is a modal, so it needs something to dismiss against. -->
  <div
    class="bg-base-content/25 fixed inset-0 z-40 lg:hidden"
    aria-hidden="true"
    x-show="open"
    x-transition:enter="transition ease-out duration-200"
    x-transition:enter-start="opacity-0"
    x-transition:leave="transition ease-in duration-150"
    x-transition:leave-end="opacity-0"
    @click="setOpen(false)"
  ></div>

  <aside
    id="harness-panel"
    class="border-base-300 bg-base-100 fixed inset-y-0 right-0 z-50 flex w-[21rem] max-w-[92vw] flex-col gap-4 overflow-y-auto border-l border-dashed p-4 shadow-2xl"
    role="dialog"
    aria-label="Harness controls"
    x-show="open"
    x-cloak
    x-transition:enter="transition ease-out duration-200"
    x-transition:enter-start="translate-x-full"
    x-transition:leave="transition ease-in duration-150"
    x-transition:leave-end="translate-x-full"
    @mouseleave="matchMedia('(hover: hover)').matches && setOpen(false)"
    @keydown.escape.window="setOpen(false)"
  >
    <div class="flex items-center justify-between gap-3">
      <p class="eyebrow text-neutral font-bold">Harness</p>
      <button type="button" class="btn btn-xs btn-ghost btn-square" aria-label="Close harness" @click="setOpen(false)">&times;</button>
    </div>

    <div class="border-base-300 flex flex-col gap-4 border-t pt-4">
      <template x-if="show.language">
        <div class="flex flex-col gap-1.5">
          <span class="eyebrow text-neutral" x-text="'Language · ' + dir.toUpperCase()"></span>
          <div class="flex flex-wrap gap-1.5" role="group" aria-label="Language">
            <template x-for="item in languages" :key="item">
              <button type="button" class="btn btn-sm" :class="language === item ? 'btn-primary' : 'btn-outline'"
                      :aria-pressed="language === item" @click="setLanguage(item)" x-text="item.toUpperCase()"></button>
            </template>
          </div>
        </div>
      </template>

      <template x-if="show.theme">
        <div class="flex flex-col gap-1.5">
          <span class="eyebrow text-neutral">Theme</span>
          <div class="flex flex-wrap gap-1.5" role="group" aria-label="Theme">
            <template x-for="item in ['system', 'light', 'dark']" :key="item">
              <button type="button" class="btn btn-sm capitalize" :class="theme === item ? 'btn-primary' : 'btn-outline'"
                      :aria-pressed="theme === item" @click="setTheme(item)" x-text="item"></button>
            </template>
          </div>
        </div>
      </template>

      <template x-if="show.platform">
        <div class="flex flex-col gap-1.5">
          <span class="eyebrow text-neutral">Platform width</span>
          <div class="flex flex-wrap gap-1.5" role="group" aria-label="Platform width">
            <template x-for="item in platforms" :key="item.value">
              <button type="button" class="btn btn-sm" :class="width === item.value ? 'btn-primary' : 'btn-outline'"
                      :aria-pressed="width === item.value" @click="setWidth(item.value)" x-text="item.label"></button>
            </template>
          </div>
        </div>
      </template>
    </div>

    <template x-if="show.log">
      <div class="border-base-300 flex flex-col gap-1.5 border-t pt-4">
        <span class="eyebrow text-neutral">Live events</span>
        <pre class="log rounded-field bg-base-200 text-base-content/70 border-base-300 m-0 border p-2.5"><template x-if="!lines.length"><span>waiting…</span></template><template x-for="line in lines" :key="line.id"><span><b class="text-accent" x-text="line.label"></b> <span x-text="line.detail"></span>
</span></template></pre>
      </div>
    </template>
  </aside>
`;

document.querySelectorAll('[data-harness-nav]').forEach(nav => {
  // Set before Alpine walks the tree — this module runs first, which is the same
  // ordering the bar and the rail already rely on.
  nav.setAttribute('x-data', 'harnessNav()');
  nav.innerHTML = NAV;
});

document.querySelectorAll('[data-harness-bar]').forEach(bar => {
  bar.innerHTML = BAR;
});

document.querySelectorAll('[data-harness-rail]').forEach(rail => {
  rail.innerHTML = RAIL;
});
