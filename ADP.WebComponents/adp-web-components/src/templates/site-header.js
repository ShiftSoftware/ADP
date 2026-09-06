/**
 * The landing page's top bar, as one implementation that two pages can mount.
 *
 * It used to be 340 lines of markup inside src/index.html plus the half of
 * `landing()` that fed it. A second documentation page needed the same
 * navigation, and the only honest way to give it one is to have one copy — so
 * the markup, the state and the path arithmetic all live here, and index.html
 * renders this rather than owning it.
 *
 * ── Mounting ──────────────────────────────────────────────────────────────────
 * A page with no Alpine state of its own needs one module tag and nothing else:
 *
 *   <script src="/templates/harness-theme.js"></script>
 *   <script src="/templates/site-locales.js"></script>
 *   <script type="module">
 *     import { mountSiteHeader } from '../site-header.js';
 *     mountSiteHeader();
 *   </script>
 *   <script defer src="/templates/vendor/alpine.js"></script>
 *
 * That stamps the header wrapped in its own `x-data` scope. Order matters twice
 * over. The two classic scripts are BLOCKING because theme and text direction
 * are decided before first paint. And this module must sit ahead of the Alpine
 * tag: deferred and module scripts share one queue in document order, Alpine
 * evaluates `x-data` at walk time, and a module behind it stamps markup that
 * Alpine has already walked past.
 *
 * A page that already has a scope covering the header passes
 * `{ alpineScope: false }` and builds its own data object out of `siteHeader()`.
 * index.html does, because `landing()` drives the body from the same catalog and
 * the `:inert` on <main> and <footer> reads the header's `mobile` — a second
 * nested scope would shadow it.
 *
 * ── Paths ─────────────────────────────────────────────────────────────────────
 * Catalog paths are site-absolute (`/templates/…`), which only resolves when the
 * site IS the server root. index.html sits at depth 0, a demo page at depth 2,
 * and the built site can be mounted under any prefix — so every path is
 * re-resolved against the directory this module was loaded from. `nav.js` and
 * `harness.js` carry the same two lines for the same reason: the release build
 * rewrites `src=` and `href=` attributes but never an import specifier or a
 * string, so anything computed has to compute itself.
 */

/** The site root: this file is always <root>/templates/site-header.js. */
const SITE_ROOT = new URL('../', import.meta.url);

/**
 * How far the CURRENT page sits below that root, as a relative prefix.
 *
 * Relative, rather than the absolute `.pathname` that nav.js produces, because
 * index.html's own `site()` produced relative hrefs and this has to render the
 * identical attribute. Depth 0 gives '', depth 2 gives '../../', and a mount
 * under /docs/ cancels out of both sides.
 */
const PATH_PREFIX = (() => {
  const root = SITE_ROOT.pathname;
  const here = new URL('.', window.location.href).pathname;
  const depth = here.startsWith(root) ? here.slice(root.length).split('/').filter(Boolean).length : 0;

  return '../'.repeat(depth);
})();

/*
 * The three product areas, in presentation order. Declared here rather than read
 * from the catalog on purpose: a group with nothing published has no catalog
 * entry at all, and disappearing from the menu is a different message from "the
 * demo is pending".
 */
const COMPONENT_GROUPS = ['vehicle-lookup', 'part-lookup', 'forms'];

/**
 * Neither file the header reads is required to exist: the build stamp ships only
 * in a released site, and the catalog only after `npm start` or `npm run
 * release` has run. Either one failing has to leave a readable page.
 */
async function read(url) {
  try {
    const response = await fetch(url);

    return response.ok ? await response.json() : null;
  } catch {
    return null;
  }
}

/**
 * The header's Alpine state, plus anything the host page adds.
 *
 * `extra` is merged through property DESCRIPTORS rather than spread, because a
 * spread invokes every getter and copies the result — `version`, `groups` and
 * `pageCount` here, and several more on the page that hosts this, would all
 * freeze at their boot-time values.
 */
export function siteHeader(extra = {}) {
  if (!window.siteLocales || !window.harnessTheme) {
    throw new Error('site-header.js needs templates/site-locales.js and templates/harness-theme.js loaded (blocking, in <head>) before it runs');
  }

  const state = {
    catalog: { areas: [], pages: [] },
    build: null,
    language: window.siteLocales.current(),
    languages: window.siteLocales.languages,
    theme: window.harnessTheme.current(),
    open: null,
    mobile: false,
    expanded: null,
    filter: '',

    async init() {
      const [catalog, build] = await Promise.all([read(new URL('catalog.json', import.meta.url)), read(new URL('build-info.json', SITE_ROOT))]);

      if (catalog) this.catalog = catalog;
      if (build) this.build = build;
    },

    /* ---------- copy ---------- */

    t(key) {
      return window.siteLocales.t(this.language, key);
    },

    get languageLabel() {
      return this.languages.find(option => option.code === this.language)?.label ?? this.language;
    },

    /* ---------- catalog ---------- */

    /*
     * The build stamp first. It records the version the site was BUILT against,
     * which is a published one; catalog.json carries package.json's version,
     * which is whatever is being prepared next and is routinely not installable
     * yet.
     */
    get version() {
      return this.build?.componentVersion ?? this.catalog.package?.version ?? '';
    },

    get pageCount() {
      return this.catalog.pages.length;
    },

    get groups() {
      const needle = this.filter.trim().toLowerCase();

      return COMPONENT_GROUPS.map(id => ({
        id,
        label: this.t('groups.' + id),
        blurb: this.t('blurb.' + id),
        pages: this.catalog.pages.filter(page => page.area === id && (!needle || (page.title + ' ' + page.path).toLowerCase().includes(needle))),
      }));
    },

    /* ---------- controls ---------- */

    toggle(id) {
      this.open = this.open === id ? null : id;
    },

    expand(id) {
      this.expanded = this.expanded === id ? null : id;
    },

    setLanguage(code) {
      this.language = window.siteLocales.apply(code);
      this.open = null;
    },

    setTheme(theme) {
      this.theme = theme;
      window.harnessTheme.apply(theme);
      this.open = null;
    },

    /** A catalog path, as an href valid from wherever this page sits. */
    // Is this catalog page the one being viewed? Resolved against the document so it
    // survives any mount depth, and tolerant of the trailing-slash/.html rewriting the
    // host does — /forms/ticket-forms, /forms/ticket-forms.html and /forms/ticket-forms/
    // are the same page.
    isCurrentPage(path) {
      if (typeof window === 'undefined' || !path) return false;

      const strip = p =>
        p
          .replace(/\/index\.html$/, '/')
          .replace(/\.html$/, '')
          .replace(/\/$/, '');

      try {
        return strip(new URL(this.site(path), window.location.href).pathname) === strip(window.location.pathname);
      } catch {
        return false;
      }
    },

    site(value) {
      const relative = PATH_PREFIX + String(value).replace(/^\//, '');

      return relative || './';
    },
  };

  Object.defineProperties(state, Object.getOwnPropertyDescriptors(extra));

  return state;
}

/*
 * Alpine resolves `x-data` against the global scope, and index.html's page script
 * is a CLASSIC script that cannot import — the same reason window.adpHighlight
 * exists. So the factory is hung on window as well as exported.
 */
window.siteHeader = siteHeader;

/**
 * The markup, lifted out of index.html — comments included, because they explain
 * choices the classes do not. The indentation is the six spaces it had in that
 * file, so the rendered DOM is what index.html produced before the extraction.
 *
 * ── The padding on the three popovers is geometry, not taste ──────────────────
 * Concentric corners only look concentric when the outer radius equals the inner
 * radius plus the padding between them. All three popovers are `rounded-box`
 * (--radius-box, 12px), so:
 *
 *   groups menu     rows are rounded-lg (8px)      -> p-1   (4px).  4 + 8 = 12
 *   language menu   daisyUI menu rows take          -> p-1.5 (6px).  6 + 6 = 12
 *   theme menu      --radius-field (6px)            -> p-1.5 (6px).  6 + 6 = 12
 *
 * Round the padding off to a "tidier" number and the rows stop tracking the
 * shell: too little and they look square inside a round box, too much and the
 * corners read as two unrelated curves. If --radius-box or --radius-field moves
 * in harness.src.css, these three move with it.
 */
const MARKUP = /* html */ `
      <header class="bg-base-100/90 border-base-300 sticky top-0 z-30 border-b backdrop-blur">
        <div class="mx-auto flex max-w-[1240px] items-center gap-3 px-5 py-2.5 sm:px-8">
          <!--
            The wordmark sits on a gold TILE rather than being painted gold.
            #F9CB4B measures 1.54:1 against white, so gold text is not a brand
            colour on a light page, it is invisible text — which is why the
            stylesheet splits the brand into \`primary\` (fills) and \`accent\`
            (anything that must read as a colour). A fill carrying ink is the one
            place the real gold appears at full strength: 10.48:1, and it matches
            the favicon and the docs site.
          -->
          <a :href="site('/')" class="group flex shrink-0 items-center gap-3" :aria-label="t('nav.home')">
            <span class="bg-primary flex items-center justify-center rounded-lg px-2.5 py-2 transition-opacity group-hover:opacity-85">
              <span class="adp-logo text-primary-content h-4 w-[42px]" aria-hidden="true"></span>
            </span>
            <span class="hidden text-base font-semibold tracking-tight sm:inline">
              <span>SHFT</span>
              <span class="text-base-content/60 group-hover:text-base-content font-normal transition-colors">your day</span>
            </span>
          </a>

          <span class="badge badge-sm badge-ghost font-mono" x-show="version" x-text="'v' + version"></span>

          <!-- ── wide: inline controls ── -->
          <div class="ms-auto hidden items-center gap-1 lg:flex" @click.outside="open = null">
            <div class="relative">
              <button type="button" class="btn btn-sm btn-ghost gap-1.5" :aria-expanded="open === 'groups'" aria-controls="menu-groups" @click="toggle('groups')">
                <span x-text="t('nav.components')"></span>
                <svg
                  class="h-3 w-3 opacity-60 transition-transform"
                  :class="open === 'groups' && 'rotate-180'"
                  viewBox="0 0 24 24"
                  fill="none"
                  stroke="currentColor"
                  stroke-width="2.5"
                  stroke-linecap="round"
                  aria-hidden="true"
                >
                  <path d="m6 9 6 6 6-6" />
                </svg>
              </button>

              <div
                id="menu-groups"
                class="bg-base-100 border-base-300 rounded-box absolute end-0 top-full z-50 mt-1 max-h-[70vh] w-[min(24rem,calc(100vw-2rem))] overflow-y-auto border p-1 shadow-xl"
                x-show="open === 'groups'"
                x-cloak
                x-transition:enter="transition ease-out duration-150"
                x-transition:enter-start="opacity-0 -translate-y-1"
                x-transition:leave="transition ease-in duration-100"
                x-transition:leave-end="opacity-0"
              >
                <label class="input input-sm mb-2 w-full" x-show="pageCount > 7">
                  <svg class="h-4 w-4 opacity-50" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                    <circle cx="11" cy="11" r="7" />
                    <path d="m20 20-3.5-3.5" stroke-linecap="round" />
                  </svg>
                  <input x-model="filter" type="search" :placeholder="t('nav.filter')" :aria-label="t('nav.filter')" />
                </label>

                <template x-for="group in groups" :key="group.id">
                  <div>
                    <!--
                      Three shapes, because a group's page count changes what the
                      row IS. None: a label, not a control — there is nothing to
                      open. One: a link, because a disclosure revealing a single
                      item is a click for nothing. More: a closed disclosure.
                    -->
                    <template x-if="!group.pages.length && !filter">
                      <p class="flex items-center justify-between gap-2 px-3 py-2">
                        <span class="text-base-content/70 text-sm" x-text="group.label"></span>
                        <span class="badge badge-xs badge-outline shrink-0" x-text="t('components.soon')"></span>
                      </p>
                    </template>

                    <template x-if="group.pages.length === 1">
                      <a
                        :href="site(group.pages[0].path)"
                        class="hover:bg-base-200 flex items-center justify-between gap-2 rounded-lg px-3 py-2 text-sm transition-colors"
                        :class="isCurrentPage(group.pages[0].path) && 'bg-primary/15 font-semibold'"
                        :aria-current="isCurrentPage(group.pages[0].path) ? 'page' : null"
                      >
                        <span x-text="group.label"></span>
                        <span class="text-base-content/65 truncate text-xs" x-text="group.pages[0].title"></span>
                      </a>
                    </template>

                    <template x-if="group.pages.length > 1">
                      <div>
                        <button
                          type="button"
                          class="hover:bg-base-200 flex w-full items-center gap-2 rounded-lg px-3 py-2 text-start text-sm transition-colors"
                          :aria-expanded="expanded === group.id"
                          :class="group.pages.some(p => isCurrentPage(p.path)) && 'font-semibold'"
                          @click="expand(group.id)"
                        >
                          <svg
                            class="h-3 w-3 shrink-0 opacity-50 transition-transform rtl:-scale-x-100"
                            :class="expanded === group.id && 'rotate-90'"
                            viewBox="0 0 24 24"
                            fill="none"
                            stroke="currentColor"
                            stroke-width="2.5"
                            stroke-linecap="round"
                            aria-hidden="true"
                          >
                            <path d="m9 6 6 6-6 6" />
                          </svg>
                          <span x-text="group.label"></span>
                          <span class="badge badge-xs badge-ghost ms-auto tabular-nums" x-text="group.pages.length"></span>
                        </button>

                        <ul
                          class="menu menu-sm w-full p-0 ps-4"
                          x-show="expanded === group.id"
                          x-transition:enter="transition ease-out duration-150"
                          x-transition:enter-start="opacity-0 -translate-y-1"
                          x-transition:leave="transition ease-in duration-100"
                          x-transition:leave-end="opacity-0"
                        >
                          <template x-for="page in group.pages" :key="page.path">
                            <li>
                              <a
                                :href="site(page.path)"
                                class="truncate"
                                :class="isCurrentPage(page.path) && 'bg-primary/15 font-semibold'"
                                :aria-current="isCurrentPage(page.path) ? 'page' : null"
                                x-text="page.title"
                              ></a>
                            </li>
                          </template>
                        </ul>
                      </div>
                    </template>
                  </div>
                </template>
              </div>
            </div>

            <a class="btn btn-sm btn-ghost" href="https://adp.shift.software/" target="_blank" rel="noreferrer" x-text="t('nav.docs')"></a>

            <span class="bg-base-300 mx-1 h-6 w-px" aria-hidden="true"></span>

            <div class="relative">
              <!-- The accessible name carries the CURRENT value, not just the
                   category: a bare aria-label="Language" overrides the visible
                   "English", so voice control cannot reach it by what it says. -->
              <button
                type="button"
                class="btn btn-sm btn-ghost gap-1.5"
                :aria-expanded="open === 'language'"
                aria-controls="menu-language"
                :aria-label="t('nav.language') + ': ' + languageLabel"
                @click="toggle('language')"
              >
                <svg class="h-4 w-4 opacity-70" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" aria-hidden="true">
                  <circle cx="12" cy="12" r="9" />
                  <path d="M3.5 9h17M3.5 15h17M12 3a15 15 0 0 1 0 18 15 15 0 0 1 0-18" />
                </svg>
                <span :lang="language" x-text="languageLabel"></span>
              </button>

              <ul
                id="menu-language"
                class="menu menu-sm bg-base-100 border-base-300 rounded-box absolute end-0 top-full z-50 mt-1 w-44 border p-1.5 shadow-xl"
                x-show="open === 'language'"
                x-cloak
                x-transition:enter="transition ease-out duration-150"
                x-transition:enter-start="opacity-0 -translate-y-1"
                x-transition:leave="transition ease-in duration-100"
                x-transition:leave-end="opacity-0"
              >
                <template x-for="option in languages" :key="option.code">
                  <li>
                    <button
                      type="button"
                      :class="language === option.code && 'menu-active'"
                      :aria-current="language === option.code ? 'true' : null"
                      @click="setLanguage(option.code)"
                    >
                      <span :lang="option.code" x-text="option.label"></span>
                      <span class="badge badge-xs badge-ghost ms-auto font-mono uppercase" x-text="option.code"></span>
                    </button>
                  </li>
                </template>
              </ul>
            </div>

            <div class="relative">
              <button
                type="button"
                class="btn btn-sm btn-ghost gap-1.5"
                :aria-expanded="open === 'theme'"
                aria-controls="menu-theme"
                :aria-label="t('nav.theme') + ': ' + t('theme.' + theme)"
                @click="toggle('theme')"
              >
                <svg class="h-4 w-4 opacity-70" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
                  <circle cx="12" cy="12" r="4.5" />
                  <path d="M12 2.5v2M12 19.5v2M2.5 12h2M19.5 12h2M5.2 5.2l1.4 1.4M17.4 17.4l1.4 1.4M18.8 5.2l-1.4 1.4M6.6 17.4l-1.4 1.4" />
                </svg>
                <span x-text="t('theme.' + theme)"></span>
              </button>

              <ul
                id="menu-theme"
                class="menu menu-sm bg-base-100 border-base-300 rounded-box absolute end-0 top-full z-50 mt-1 w-36 border p-1.5 shadow-xl"
                x-show="open === 'theme'"
                x-cloak
                x-transition:enter="transition ease-out duration-150"
                x-transition:enter-start="opacity-0 -translate-y-1"
                x-transition:leave="transition ease-in duration-100"
                x-transition:leave-end="opacity-0"
              >
                <template x-for="option in ['system', 'light', 'dark']" :key="option">
                  <li>
                    <button
                      type="button"
                      :class="theme === option && 'menu-active'"
                      :aria-current="theme === option ? 'true' : null"
                      @click="setTheme(option)"
                      x-text="t('theme.' + option)"
                    ></button>
                  </li>
                </template>
              </ul>
            </div>
          </div>

          <button
            type="button"
            class="btn btn-sm btn-ghost ms-auto lg:hidden"
            :aria-expanded="mobile"
            aria-controls="mobile-sheet"
            :aria-label="t('nav.menu')"
            @click="mobile = true"
          >
            <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
              <path d="M4 7h16M4 12h16M4 17h16" />
            </svg>
          </button>
        </div>
      </header>

      <!-- ═══════════════════════════ mobile sheet ═══════════════════════════ -->
      <div class="fixed inset-0 z-50 lg:hidden" x-show="mobile" x-cloak role="dialog" aria-modal="true" :aria-label="t('nav.menu')" id="mobile-sheet">
        <div class="bg-base-content/40 absolute inset-0 backdrop-blur-sm" x-show="mobile" x-transition.opacity @click="mobile = false"></div>

        <div
          class="bg-base-100 absolute inset-y-0 end-0 flex w-full flex-col shadow-2xl sm:w-[22rem]"
          x-show="mobile"
          x-transition:enter="transition ease-out duration-200"
          x-transition:enter-start="translate-x-full rtl:-translate-x-full"
          x-transition:leave="transition ease-in duration-150"
          x-transition:leave-end="translate-x-full rtl:-translate-x-full"
        >
          <div class="border-base-300 flex items-center justify-between border-b px-4 py-3">
            <span class="bg-primary flex items-center justify-center rounded-lg px-2.5 py-2">
              <span class="adp-logo text-primary-content h-4 w-[42px]" aria-hidden="true"></span>
            </span>
            <button type="button" class="btn btn-sm btn-ghost" :aria-label="t('nav.close')" @click="mobile = false">
              <svg class="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" aria-hidden="true">
                <path d="M6 6l12 12M18 6L6 18" />
              </svg>
            </button>
          </div>

          <div class="flex-1 overflow-y-auto overscroll-contain px-4 py-4">
            <p class="eyebrow text-base-content/65 mb-2" x-text="t('nav.components')"></p>

            <template x-for="group in groups" :key="group.id">
              <div class="mb-1">
                <template x-if="!group.pages.length && !filter">
                  <p class="flex items-center justify-between gap-2 py-2">
                    <span class="text-base-content/70 text-sm" x-text="group.label"></span>
                    <span class="badge badge-xs badge-outline shrink-0" x-text="t('components.soon')"></span>
                  </p>
                </template>

                <template x-if="group.pages.length === 1">
                  <a
                    :href="site(group.pages[0].path)"
                    class="hover:bg-base-200 -mx-2 block rounded-lg px-2 py-2 text-sm"
                    :class="isCurrentPage(group.pages[0].path) && 'bg-primary/15 font-semibold'"
                    :aria-current="isCurrentPage(group.pages[0].path) ? 'page' : null"
                    x-text="group.label"
                  ></a>
                </template>

                <template x-if="group.pages.length > 1">
                  <div>
                    <button
                      type="button"
                      class="hover:bg-base-200 -mx-2 flex w-full items-center gap-2 rounded-lg px-2 py-2 text-start text-sm"
                      :aria-expanded="expanded === group.id"
                      @click="expand(group.id)"
                    >
                      <svg
                        class="h-3 w-3 shrink-0 opacity-50 transition-transform rtl:-scale-x-100"
                        :class="expanded === group.id && 'rotate-90'"
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke="currentColor"
                        stroke-width="2.5"
                        stroke-linecap="round"
                        aria-hidden="true"
                      >
                        <path d="m9 6 6 6-6 6" />
                      </svg>
                      <span x-text="group.label"></span>
                      <span class="badge badge-xs badge-ghost ms-auto tabular-nums" x-text="group.pages.length"></span>
                    </button>

                    <ul
                      class="menu menu-sm w-full p-0 ps-4"
                      x-show="expanded === group.id"
                      x-transition:enter="transition ease-out duration-150"
                      x-transition:enter-start="opacity-0 -translate-y-1"
                    >
                      <template x-for="page in group.pages" :key="page.path">
                        <li>
                          <a
                            :href="site(page.path)"
                            class="truncate"
                            :class="isCurrentPage(page.path) && 'bg-primary/15 font-semibold'"
                            :aria-current="isCurrentPage(page.path) ? 'page' : null"
                            x-text="page.title"
                          ></a>
                        </li>
                      </template>
                    </ul>
                  </div>
                </template>
              </div>
            </template>

            <div class="border-base-300 mt-4 border-t pt-4">
              <a class="btn btn-sm btn-ghost w-full justify-start" href="https://adp.shift.software/" target="_blank" rel="noreferrer" x-text="t('nav.docs')"></a>
            </div>

            <p class="eyebrow text-base-content/65 mt-6 mb-2" x-text="t('nav.language')"></p>
            <div class="grid grid-cols-2 gap-1.5">
              <template x-for="option in languages" :key="option.code">
                <button
                  type="button"
                  class="btn btn-sm justify-start"
                  :class="language === option.code ? 'btn-primary' : 'btn-outline'"
                  :aria-pressed="language === option.code"
                  @click="setLanguage(option.code)"
                >
                  <span :lang="option.code" x-text="option.label"></span>
                </button>
              </template>
            </div>

            <p class="eyebrow text-base-content/65 mt-6 mb-2" x-text="t('nav.theme')"></p>
            <div class="grid grid-cols-3 gap-1.5">
              <template x-for="option in ['system', 'light', 'dark']" :key="option">
                <button
                  type="button"
                  class="btn btn-sm"
                  :class="theme === option ? 'btn-primary' : 'btn-outline'"
                  :aria-pressed="theme === option"
                  @click="setTheme(option)"
                  x-text="t('theme.' + option)"
                ></button>
              </template>
            </div>
          </div>
        </div>
      </div>
`.trim();

/** Whether the header has already been stamped into this document. */
let mounted = false;

/**
 * Stamps the header — and the mobile sheet that belongs to it — into the page.
 *
 *   target       element, or selector for one, to REPLACE. Defaults to the first
 *                `[data-site-header]` placeholder; with none in the page, the
 *                header is prepended to <body>. Replaced rather than filled, so
 *                the placeholder leaves no wrapper of its own behind.
 *   alpineScope  wrap the markup in its own `x-data`. Pass false when the page
 *                already has a scope built from `siteHeader()`.
 *
 * Returns the mounted <header>.
 */
export function mountSiteHeader({ target = '[data-site-header]', alpineScope = true } = {}) {
  if (mounted) return document.querySelector('header');

  const placeholder = typeof target === 'string' ? document.querySelector(target) : target;

  // A <template> parses without executing or fetching anything, and keeps the
  // comments and the whitespace text nodes exactly as written.
  const parsed = document.createElement('template');

  parsed.innerHTML = alpineScope ? `<div x-data="siteHeader()" @keydown.escape.window="open = null; mobile = false">\n${MARKUP}\n</div>` : MARKUP;

  const nodes = [...parsed.content.childNodes];

  if (placeholder) placeholder.replaceWith(...nodes);
  else document.body.prepend(...nodes);

  mounted = true;

  return document.querySelector('header');
}
