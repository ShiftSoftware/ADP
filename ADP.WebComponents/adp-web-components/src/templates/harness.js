/**
 * adp-web-components — dev harness controls (Alpine).
 *
 * Provides the `harness()` Alpine data factory and stamps the standard rail
 * markup into any `[data-harness-rail]` element, so a page describes what it is
 * testing instead of re-implementing the same six controls:
 *
 *   <aside class="card border-base-300 bg-base-100 flex flex-col gap-4 border border-dashed p-4"
 *          data-harness-rail
 *          x-data="harness({ subject: '#subject', mocks: 'vehicle-lookup' })"></aside>
 *
 * A page that needs a different rail just writes its own markup against the same
 * state — every method below is public.
 *
 * Control order is fixed on purpose (R5), and the language control flips page
 * direction because half of the supported set is RTL (R6). See
 * .shift/repos/adp/web-components/templates-design-language.md.
 */

const DEFAULTS = {
  subject: '#subject',
  mocks: null,
  labels: {},
  languages: ['en', 'ku', 'ar', 'ru'],
  rtl: ['ar', 'ku'],
  platforms: [
    { label: '360', value: '360px' },
    { label: '414', value: '414px' },
    { label: '768', value: '768px' },
    { label: 'Full', value: '100%' },
  ],
  theme: true,
  mode: true,
  platform: true,
  log: true,
  apply: (subject, data) => subject.setMockData(data),
  select: (subject, key) => subject.fetchVin(key),
};

window.harness = function harness(options = {}) {
  const config = { ...DEFAULTS, ...options };

  return {
    config,
    subject: null,

    languages: config.languages,
    platforms: config.platforms,
    show: {
      theme: config.theme,
      mode: config.mode,
      platform: config.platform,
      log: config.log,
      fixtures: Boolean(config.mocks),
    },

    language: config.languages[0],
    theme: window.harnessTheme?.current() ?? 'system',
    // Development is the default: it is the only mode that works without a local
    // API running, which is how these pages are opened most of the time.
    mode: 'dev',
    width: '100%',
    fixtures: [],
    fixture: null,
    lines: [],

    get dir() {
      return config.rtl.includes(this.language) ? 'rtl' : 'ltr';
    },

    async init() {
      this.subject = document.querySelector(config.subject);

      if (!this.subject) return this.write('harness', `no element matches ${config.subject}`);

      // Properties set before upgrade would be shadowed by the prototype
      // accessors, and @Method members do not exist yet. Wait for both.
      await customElements.whenDefined(this.subject.localName);
      await this.subject.componentOnReady?.();

      this.setLanguage(this.subject.language || this.language);
      this.subject.isDev = true;

      if (config.log) this.observe();
      if (config.mocks) await this.loadFixtures();
    },

    /* ---------- controls ---------- */

    setLanguage(language) {
      this.language = language;
      this.subject.language = language;

      document.documentElement.lang = language;
      document.documentElement.dir = this.dir;

      this.write('language', `${language} · ${this.dir}`);
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

      this.write('mode', mode === 'dev' ? 'development · mock data' : `production · ${this.subject.baseUrl || 'no base-url set'}`);
    },

    setWidth(width) {
      this.width = width;
      this.subject.closest('.frame')?.style.setProperty('--frame-w', width);

      this.write('width', width);
    },

    /* ---------- fixtures ---------- */

    async loadFixtures() {
      let data = {};

      try {
        data = (await window.loadMockData(config.mocks)) ?? {};
        await config.apply(this.subject, data);
      } catch (error) {
        this.write('fixtures', `failed to load — ${error.message}`);
      }

      // Generated from the mock keys, never hand-listed: a hardcoded list drifts
      // the moment someone adds a scenario (R7). Labels only annotate.
      this.fixtures = [{ value: '', label: 'Empty' }, { value: 'error', label: 'Error' }, ...Object.keys(data).map(key => ({ value: key, label: key, note: config.labels[key] }))];
    },

    run(key) {
      this.fixture = key;
      this.write('fetch', key === '' ? '(empty)' : key);

      config.select(this.subject, key);
    },

    reload() {
      this.run(this.fixture ?? '');
    },

    /* ---------- live events ---------- */

    observe() {
      this.subject.loadingStateChange = isLoading => this.write(isLoading ? 'loading' : 'idle', '');
      this.subject.errorCallback = message => this.write('error', String(message));
      this.subject.loadedResponse = response => this.write('loaded', summarise(response));
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

  const vin = response.vin ?? response.identifiers?.vin;
  const keys = Object.keys(response).length;

  return vin ? `${vin} · ${keys} keys` : `${keys} keys`;
}

/* ---------- the standard rail ---------- */

const RAIL = /* html */ `
  <div class="flex items-center justify-between gap-3">
    <p class="eyebrow text-neutral font-bold">Harness</p>
    <a href="/" class="btn btn-xs btn-ghost gap-1.5" aria-label="Showcase home">
      <span class="adp-logo text-accent h-3.5 w-[34px]" aria-hidden="true"></span>
      Home
    </a>
  </div>

  <div class="flex flex-col gap-1.5">
    <span class="eyebrow text-neutral" x-text="'Language · ' + dir.toUpperCase()"></span>
    <div class="flex flex-wrap gap-1.5" role="group" aria-label="Language">
      <template x-for="item in languages" :key="item">
        <button type="button" class="btn btn-sm" :class="language === item ? 'btn-primary' : 'btn-outline'"
                :aria-pressed="language === item" @click="setLanguage(item)" x-text="item.toUpperCase()"></button>
      </template>
    </div>
  </div>

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

  <template x-if="show.mode">
    <div class="flex flex-col gap-1.5">
      <span class="eyebrow text-neutral">Mode</span>
      <div class="flex flex-wrap gap-1.5" role="group" aria-label="Mode">
        <template x-for="item in [{ value: 'dev', label: 'Development' }, { value: 'prod', label: 'Production' }]" :key="item.value">
          <button type="button" class="btn btn-sm" :class="mode === item.value ? 'btn-primary' : 'btn-outline'"
                  :aria-pressed="mode === item.value" @click="setMode(item.value)" x-text="item.label"></button>
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

  <template x-if="show.fixtures">
    <div class="flex flex-col gap-1.5">
      <span class="eyebrow text-neutral">Fixtures</span>
      <button type="button" class="btn btn-sm btn-outline self-start" @click="reload()">Reload</button>
      <div class="flex flex-wrap gap-1.5" role="group" aria-label="Fixtures">
        <template x-for="item in fixtures" :key="item.value">
          <button type="button" class="btn btn-sm font-mono font-normal" :class="fixture === item.value ? 'btn-primary' : 'btn-outline'"
                  :aria-pressed="fixture === item.value" @click="run(item.value)">
            <span x-text="item.label"></span>
            <span class="opacity-70 font-sans" x-show="item.note" x-text="item.note"></span>
          </button>
        </template>
      </div>
    </div>
  </template>

  <template x-if="show.log">
    <div class="flex flex-col gap-1.5">
      <span class="eyebrow text-neutral">Live events</span>
      <pre class="log m-0 p-2.5 rounded-field bg-base-200 text-base-content/70 border border-base-300"><template x-if="!lines.length"><span>waiting…</span></template><template x-for="line in lines" :key="line.id"><span><b class="text-accent" x-text="line.label"></b> <span x-text="line.detail"></span>
</span></template></pre>
    </div>
  </template>
`;

document.querySelectorAll('[data-harness-rail]').forEach(rail => {
  rail.innerHTML = RAIL;
});
