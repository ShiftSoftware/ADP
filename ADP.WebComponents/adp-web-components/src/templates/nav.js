/**
 * Navigation for the frozen prototypes.
 *
 * They predate the harness, and §8 says they are records rather than pages
 * waiting to be restyled — so they cannot link `harness.css`. Tailwind's
 * preflight alone would rewrite every one of them, which is precisely the record
 * being destroyed.
 *
 * So this is a shadow-DOM custom element with its own styles: nothing leaks in,
 * nothing leaks out, and it is `position: fixed`, so it contributes no layout and
 * the page underneath renders exactly as it did before. A prototype adopts it
 * with one line and no other change:
 *
 *   <script type="module" src="/templates/nav.js"></script>
 *
 * Harness pages do not use this — they get the integrated sticky bar from
 * harness.js. Both read the same `catalog.json`, so neither can go stale.
 *
 * See .shift/repos/adp/web-components/templates-design-language.md §8.
 */

const STYLES = /* css */ `
  :host {
    /* The harness tokens, hardcoded: this file cannot import the stylesheet that
       defines them without dragging Tailwind in with it. */
    --surface: #ffffff;
    --ground: #eef1f5;
    --line: #dde4ec;
    --ink: #121922;
    --muted: #7b8794;
    --accent: #7a5d08;
    --gold: #f9cb4b;
    --on-gold: #2a2000;

    position: fixed;
    z-index: 2147483000;
    bottom: 1rem;
    left: 1rem;
    font-family: ui-sans-serif, system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
    font-size: 13px;
    line-height: 1.4;
  }

  @media (prefers-color-scheme: dark) {
    :host {
      --surface: #151b23;
      --ground: #0c1016;
      --line: #262f3a;
      --ink: #e8eef4;
      --muted: #76838f;
      --accent: #f9cb4b;
    }
  }

  * {
    box-sizing: border-box;
  }

  /* Dashed, mono, uppercase: a cropped screenshot must not read as prototype UI (R2). */
  .launcher {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    padding: 0.4rem 0.7rem;
    color: var(--ink);
    background: var(--surface);
    border: 1px dashed var(--line);
    border-radius: 999px;
    box-shadow: 0 6px 20px rgb(18 25 34 / 18%);
    cursor: pointer;
    font: inherit;
    font-family: ui-monospace, 'SFMono-Regular', Menlo, Consolas, monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    transition: border-color 150ms ease, color 150ms ease;
  }

  .launcher:hover {
    color: var(--accent);
    border-color: var(--accent);
  }

  .launcher svg {
    display: block;
    width: 13px;
    height: 13px;
    color: var(--accent);
  }

  .panel {
    position: absolute;
    bottom: calc(100% + 0.5rem);
    left: 0;
    width: min(22rem, calc(100vw - 2rem));
    max-height: min(70vh, 34rem);
    overflow-y: auto;
    padding: 0.4rem;
    color: var(--ink);
    background: var(--surface);
    border: 1px solid var(--line);
    border-radius: 14px;
    box-shadow: 0 18px 45px rgb(18 25 34 / 22%);
    overscroll-behavior: contain;
  }

  [hidden] {
    display: none !important;
  }

  .home {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    margin-bottom: 0.25rem;
    padding: 0.45rem 0.6rem;
    color: var(--ink);
    background: var(--ground);
    border-radius: 9px;
    font-weight: 600;
    text-decoration: none;
  }

  .home:hover {
    color: var(--accent);
  }

  details {
    border-top: 1px solid var(--line);
  }

  details:first-of-type {
    border-top: 0;
  }

  summary {
    display: flex;
    gap: 0.5rem;
    align-items: center;
    padding: 0.45rem 0.6rem;
    color: var(--muted);
    cursor: pointer;
    font-family: ui-monospace, 'SFMono-Regular', Menlo, Consolas, monospace;
    font-size: 11px;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    list-style: none;
  }

  summary::-webkit-details-marker {
    display: none;
  }

  summary::before {
    color: var(--muted);
    content: '▸';
    font-size: 9px;
    transition: transform 150ms ease;
  }

  details[open] summary::before {
    transform: rotate(90deg);
  }

  .count {
    margin-inline-start: auto;
    font-variant-numeric: tabular-nums;
    opacity: 0.7;
  }

  a.page {
    display: block;
    padding: 0.4rem 0.6rem 0.4rem 1.5rem;
    overflow: hidden;
    color: var(--ink);
    border-radius: 8px;
    text-decoration: none;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  a.page:hover {
    background: var(--ground);
  }

  a.page[aria-current='page'] {
    color: var(--on-gold);
    background: var(--gold);
    font-weight: 600;
  }

  .empty {
    padding: 0.6rem;
    color: var(--muted);
  }

  :focus-visible {
    outline: 2px solid var(--accent);
    outline-offset: 2px;
  }

  @media (prefers-reduced-motion: reduce) {
    * {
      transition-duration: 0.01ms !important;
    }
  }
`;

const ICON = /* html */ `
  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" aria-hidden="true">
    <path d="M4 6h16M4 12h16M4 18h16" />
  </svg>
`;

class HarnessNav extends HTMLElement {
  #open = false;

  connectedCallback() {
    if (this.shadowRoot) return;

    const root = this.attachShadow({ mode: 'open' });

    root.innerHTML = /* html */ `
      <style>${STYLES}</style>
      <button class="launcher" type="button" aria-expanded="false" aria-controls="panel">${ICON} Pages</button>
      <div class="panel" id="panel" hidden><p class="empty">Loading…</p></div>
    `;

    this.launcher = root.querySelector('.launcher');
    this.panel = root.querySelector('.panel');

    this.launcher.addEventListener('click', () => this.toggle(!this.#open));

    // `composedPath` because a click inside the shadow root reports this host as
    // its target from the outside.
    this.onDocumentClick = event => {
      if (this.#open && !event.composedPath().includes(this)) this.toggle(false);
    };

    this.onKeydown = event => {
      if (event.key === 'Escape') this.toggle(false);
    };

    document.addEventListener('click', this.onDocumentClick);
    document.addEventListener('keydown', this.onKeydown);

    this.load();
  }

  disconnectedCallback() {
    document.removeEventListener('click', this.onDocumentClick);
    document.removeEventListener('keydown', this.onKeydown);
  }

  toggle(open) {
    this.#open = open;
    this.panel.hidden = !open;
    this.launcher.setAttribute('aria-expanded', String(open));
  }

  async load() {
    let catalog;

    try {
      const response = await fetch('/templates/catalog.json');

      if (!response.ok) throw new Error(String(response.status));

      catalog = await response.json();
    } catch {
      this.panel.innerHTML = '<p class="empty">No catalog. Run <code>npm start</code>.</p>';
      return;
    }

    const here = window.location.pathname;
    const escape = value => String(value).replace(/[&<>"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[character]);

    const groups = catalog.areas.map(area => {
      const pages = catalog.pages.filter(page => page.area === area.id);
      const current = pages.some(page => page.path === here);

      const items = pages.map(page => `<a class="page" href="${escape(page.path)}"${page.path === here ? ' aria-current="page"' : ''}>${escape(page.title)}</a>`).join('');

      return `
        <details${current ? ' open' : ''}>
          <summary>${escape(area.label)}<span class="count">${pages.length}</span></summary>
          ${items}
        </details>
      `;
    });

    this.panel.innerHTML = `<a class="home" href="/">← Showcase home</a>${groups.join('')}`;
  }
}

customElements.define('harness-nav', HarnessNav);

// Self-injecting, so a page adopts it with a script tag and nothing else. Guarded
// because a page is free to place the element itself.
if (!document.querySelector('harness-nav')) {
  const mount = () => document.body.append(document.createElement('harness-nav'));

  if (document.body) mount();
  else document.addEventListener('DOMContentLoaded', mount);
}
