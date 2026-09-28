const COLLAPSED_KEY = 'adp-docs-sider-collapsed';
const RAIL = window.matchMedia('(min-width: 64rem)');
const REDUCED = window.matchMedia('(prefers-reduced-motion: reduce)');

const svg = paths => `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${paths}</svg>`;

export const ICONS = {
  overview: svg('<path d="M2 5h6a4 4 0 0 1 4 4v11a3 3 0 0 0-3-3H2z"/><path d="M22 5h-6a4 4 0 0 0-4 4v11a3 3 0 0 1 3-3h7z"/>'),
  installation: svg('<path d="M12 3v12"/><path d="m7 10 5 5 5-5"/><path d="M4 20h16"/>'),
  examples: svg('<path d="m8 7-5 5 5 5"/><path d="m16 7 5 5-5 5"/>'),
  playground: svg('<path d="M4 6h10M18 6h2M4 12h2M10 12h10M4 18h10M18 18h2"/><circle cx="16" cy="6" r="2"/><circle cx="8" cy="12" r="2"/><circle cx="16" cy="18" r="2"/>'),
  appearances: svg(
    '<rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><circle cx="17.5" cy="17.5" r="3.5"/>',
  ),
  theming: svg('<path d="M12 3s6 6.4 6 11a6 6 0 0 1-12 0c0-4.6 6-11 6-11z"/><path d="M9 14a3 3 0 0 0 3 3"/>'),
  dates: svg('<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/><path d="m9 15 2 2 4-4"/>'),
  api: svg('<path d="M8 4H7a2 2 0 0 0-2 2v4l-2 2 2 2v4a2 2 0 0 0 2 2h1"/><path d="M16 4h1a2 2 0 0 1 2 2v4l2 2-2 2v4a2 2 0 0 1-2 2h-1"/>'),
  accessibility: svg('<rect x="2" y="6" width="20" height="12" rx="2"/><path d="M6 10h.01M10 10h.01M14 10h.01M18 10h.01M7 14h10"/>'),
  localization: svg('<circle cx="12" cy="12" r="9"/><path d="M3.5 9h17M3.5 15h17M12 3a15 15 0 0 1 0 18 15 15 0 0 1 0-18"/>'),
  forms: svg('<rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V3h6v1"/><path d="M9 10h6M9 14h6M9 18h3"/>'),
  demo: svg('<rect x="3" y="4" width="18" height="13" rx="2"/><path d="M8 21h8M12 17v4"/><path d="m10 8 4 2.5-4 2.5z"/>'),
  about: svg('<circle cx="12" cy="12" r="9"/><path d="M12 11v5"/><path d="M12 8h.01"/>'),
  section: svg('<path d="M5 9h14M5 15h14M10 4 8 20M16 4l-2 16"/>'),
  calendar: svg('<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/><path d="M8 14h.01M12 14h.01M16 14h.01M8 17.5h.01M12 17.5h.01"/>'),
  component: svg('<path d="m12 3 8 4.5v9L12 21l-8-4.5v-9z"/><path d="m4 7.5 8 4.5 8-4.5M12 12v9"/>'),
};

const TRIGGER = svg('<path d="M21 5H3M21 19H3M21 12h-9"/><path class="docs-trigger-arrow" d="m7 8-4 4 4 4"/>');

const escape = value => String(value).replace(/[&<>"]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' })[character]);

const language = () => document.documentElement.lang || 'en';

function t(key, fallback) {
  const text = window.siteLocales?.t(language(), key);

  return !text || text === key ? (fallback ?? key) : text;
}

function storeCollapsed(value) {
  try {
    localStorage.setItem(COLLAPSED_KEY, String(value));
  } catch {
    // The rail still toggles; it just will not be remembered.
  }
}

const slug = text =>
  String(text)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-|-$/g, '') || 'section';

function uniqueId(base) {
  let id = base;

  while (document.getElementById(id)) id += '-1';

  return id;
}

function collectEntries(showcase) {
  const tabs = [...showcase.querySelectorAll('[data-doc-tab]')];

  if (tabs.length) {
    return tabs.map(panel => {
      // Not the tab's own name: a panel with that id would make the browser jump to it before the tab is shown.
      panel.id ||= `tab-${panel.dataset.docTab}`;

      return {
        id: panel.dataset.docTab,
        prefix: panel.dataset.docHashPrefix,
        key: `docs.tab.${panel.dataset.docTab}`,
        label: panel.dataset.docLabel || panel.dataset.docTab,
        icon: panel.dataset.docIcon,
        target: panel,
      };
    });
  }

  const stage = showcase.querySelector('.showcase-stage');

  // A page with no declared pages is one page; its headings still go to the section map.
  return stage ? [{ id: (stage.id ||= uniqueId('page')), key: 'docs.demo', label: 'Demo', icon: 'demo', target: stage }] : [];
}

function entryMarkup(entry) {
  const label = entry.key ? t(entry.key, entry.label) : entry.label;
  const translated = entry.key ? ` data-t="${escape(entry.key)}" data-fallback="${escape(entry.label)}"` : '';

  return `
    <li>
      <a class="docs-item" href="#${escape(entry.id)}" data-entry="${escape(entry.id)}">
        ${ICONS[entry.icon] ?? ICONS.section}
        <span class="docs-label"${translated}>${escape(label)}</span>
      </a>
    </li>`;
}

const CATALOG = new URL('catalog.json', import.meta.url);

const pageUrl = path => new URL(String(path).replace(/^\/templates\//, ''), import.meta.url);

async function familyPages(name) {
  try {
    const response = await fetch(CATALOG);
    const catalog = response.ok ? await response.json() : { pages: [] };

    return catalog.pages.filter(page => page.family === name).sort((a, b) => (a.familyOrder ?? Infinity) - (b.familyOrder ?? Infinity) || a.title.localeCompare(b.title));
  } catch {
    return [];
  }
}

function familyMarkup(name, pages, entries, title) {
  const here = pages.find(page => pageUrl(page.path).pathname === window.location.pathname);
  const listed = here ? pages : [{ path: '', title }];
  const label = name.charAt(0).toUpperCase() + name.slice(1);

  const item = page =>
    page === here || !here
      ? `
    <li class="docs-family-page" data-family-current>
      <a class="docs-item docs-family-self" href="${escape(page.path ? pageUrl(page.path).href : window.location.pathname)}" aria-current="page">${ICONS.component}<span class="docs-label">${escape(page.title)}</span></a>
      <ul class="docs-list docs-sublist">${entries.map(entryMarkup).join('')}</ul>
    </li>`
      : `
    <li class="docs-family-page">
      <a class="docs-item" href="${escape(pageUrl(page.path).href)}">${ICONS.component}<span class="docs-label">${escape(page.title)}</span></a>
    </li>`;

  return `
      <p class="docs-heading" data-t="docs.family.${escape(name)}" data-fallback="${escape(label)}"></p>
      <ul class="docs-list docs-family">${listed.map(item).join('')}</ul>`;
}

const mapLabel = heading => (heading.querySelector('[data-map-label]') ?? heading).textContent.replace(/\s+/g, ' ').trim();

let mounted = false;

// Opt-in: the page writes the skeleton (.docs-shell[data-docs-layout] > .docs-sider, .docs-main > .docs-bar + .showcase, .docs-map) so its columns exist before first paint.
export function mountDocsSidebar() {
  const shell = document.querySelector('.docs-shell[data-docs-layout]');
  const showcase = shell?.querySelector('.showcase');

  if (mounted || !showcase) return;
  mounted = true;

  const entries = collectEntries(showcase);

  for (const entry of entries) {
    entry.target.querySelectorAll('h3').forEach(heading => (heading.id ||= uniqueId(`${entry.id}-${slug(mapLabel(heading))}`)));
  }
  const title = showcase.querySelector('.showcase-title h1')?.textContent.trim() || document.title.split('—')[0].trim();
  const tag = showcase.querySelector('#subject')?.localName ?? '';
  const icon = ICONS[showcase.querySelector('[data-doc-icon]:not([data-doc-tab])')?.dataset.docIcon] ?? ICONS.component;
  const family = document.querySelector('meta[name="docs-family"]')?.content.trim() ?? '';

  shell.dataset.drawer = 'closed';

  shell.querySelector('.docs-sider').innerHTML = `
    <div class="docs-brand">
      <span class="docs-brand-tile">${icon}</span>
      <span class="docs-brand-text">
        <span class="docs-brand-title">${escape(title)}</span>
        ${tag.includes('-') ? `<span class="docs-brand-tag">&lt;${escape(tag)}&gt;</span>` : ''}
      </span>
    </div>
    <nav class="docs-nav">${
      family
        ? familyMarkup(family, [], entries, title)
        : `
      <p class="docs-heading" data-t="docs.pages"></p>
      <ul class="docs-list">${entries.map(entryMarkup).join('')}</ul>`
    }
    </nav>`;

  shell.querySelector('.docs-bar').innerHTML = `
    <button type="button" class="docs-trigger" aria-controls="docs-sider">${TRIGGER}</button>
    <p class="docs-crumb"><span class="docs-crumb-group">${escape(title)}</span><span class="docs-crumb-current" data-crumb-current></span></p>`;

  shell.querySelector('.docs-map').innerHTML = `
    <nav aria-labelledby="docs-map-title">
      <p class="docs-map-title" id="docs-map-title" data-t="docs.onThisPage"></p>
      <ul class="docs-map-list" data-map></ul>
    </nav>`;

  shell.insertAdjacentHTML('beforeend', '<div class="docs-scrim" aria-hidden="true"></div><div class="docs-tip" aria-hidden="true"></div>');

  const sider = shell.querySelector('.docs-sider');
  const brand = shell.querySelector('.docs-brand');
  const nav = shell.querySelector('.docs-nav');
  const trigger = shell.querySelector('.docs-trigger');
  const bar = shell.querySelector('.docs-bar');
  const tip = shell.querySelector('.docs-tip');
  const crumbCurrent = shell.querySelector('[data-crumb-current]');
  const mapList = shell.querySelector('[data-map]');
  let mapHeadings = [];
  let pinned = '';
  let mapTimer = 0;

  let current = null;
  let drawerOpen = false;

  const collapsed = () => RAIL.matches && document.documentElement.dataset.docsCollapsed === 'true';
  const labelOf = entry => (entry.key ? t(entry.key, entry.label) : entry.label);

  function syncTrigger() {
    const expanded = RAIL.matches ? !collapsed() : drawerOpen;
    const label = RAIL.matches ? t(collapsed() ? 'docs.expand' : 'docs.collapse') : t(drawerOpen ? 'nav.close' : 'docs.open');

    trigger.setAttribute('aria-expanded', String(expanded));
    trigger.setAttribute('aria-label', label);
    trigger.title = label;
  }

  function syncCrumb() {
    const entry = entries.find(item => item.id === current);

    crumbCurrent.textContent = entry ? labelOf(entry) : '';
  }

  function translate() {
    shell.querySelectorAll('[data-t]').forEach(node => (node.textContent = t(node.dataset.t, node.dataset.fallback)));
    nav.setAttribute('aria-label', t('docs.nav'));
    syncTrigger();
    syncCrumb();
  }

  function mark(id, value) {
    current = id;
    shell.querySelectorAll('[data-entry]').forEach(link => {
      if (link.dataset.entry === id) link.setAttribute('aria-current', value);
      else link.removeAttribute('aria-current');
    });
    syncCrumb();
  }

  function tabFor(hash) {
    const id = decodeURIComponent(hash.replace(/^#/, ''));
    const direct = entries.find(entry => entry.id === id);

    if (direct || !id) return { entry: direct ?? entries[0], inner: null };

    const inner = document.getElementById(id);
    const owner = (inner && entries.find(entry => entry.target.contains(inner))) || entries.find(entry => entry.prefix && id.startsWith(entry.prefix));

    return { entry: owner ?? entries[0], inner: owner && inner ? inner : null };
  }

  function showTab(fromNavigation) {
    const { entry, inner } = tabFor(window.location.hash);
    const changed = entry.id !== current;

    entries.forEach(item => (item.target.hidden = item !== entry));
    mark(entry.id, 'page');
    window.docsSidebar.current = entry.id;

    if (changed) {
      pinned = '';
      window.dispatchEvent(new CustomEvent('docs:tab', { detail: { id: entry.id } }));
      mapObserver?.disconnect();
      mapObserver?.observe(entry.target, { childList: true, subtree: true, attributes: true, attributeFilter: ['hidden', 'style'] });
      queueMap();
    }

    if (inner) inner.scrollIntoView();
    else if (fromNavigation && changed) {
      const top = shell.querySelector('.docs-main').getBoundingClientRect().top;
      const navH = parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--nav-h')) || 0;

      if (top < navH) window.scrollTo({ top: window.scrollY + top - navH });
    }
  }

  function mapRoot() {
    return entries.find(entry => entry.id === current)?.target;
  }

  function buildMap() {
    const root = mapRoot();

    mapHeadings = root ? [...root.querySelectorAll('h3')].filter(heading => heading.getClientRects().length && mapLabel(heading)) : [];
    mapHeadings.forEach(heading => (heading.id ||= uniqueId(slug(mapLabel(heading)))));
    mapList.innerHTML = mapHeadings
      .map(heading => `<li><a class="docs-map-item" dir="auto" href="#${escape(heading.id)}" data-map-item="${escape(heading.id)}">${escape(mapLabel(heading))}</a></li>`)
      .join('');
    shell.dataset.map = mapHeadings.length ? 'true' : 'false';
    spyMap();
  }

  function queueMap() {
    clearTimeout(mapTimer);
    mapTimer = setTimeout(buildMap, 120);
  }

  function spyMap() {
    const edge = (parseFloat(getComputedStyle(document.documentElement).getPropertyValue('--nav-h')) || 0) + bar.getBoundingClientRect().height + 24;
    let active = mapHeadings.find(heading => heading.id === pinned) ?? mapHeadings[0];

    if (pinned && active?.id === pinned) return markMap(active);

    for (const heading of mapHeadings) if (heading.dataset.mapSpy !== 'off' && heading.getBoundingClientRect().top <= edge) active = heading;
    if (window.innerHeight + window.scrollY >= document.documentElement.scrollHeight - 2 && mapHeadings.length) active = mapHeadings.at(-1);

    markMap(active);
  }

  function markMap(active) {
    mapList.querySelectorAll('[data-map-item]').forEach(link => {
      if (link.dataset.mapItem === active?.id) link.setAttribute('aria-current', 'location');
      else link.removeAttribute('aria-current');
    });
  }

  function setCollapsed(value) {
    document.documentElement.dataset.docsCollapsed = String(value);
    storeCollapsed(value);
    hideTip();
    syncTrigger();
  }

  function setDrawer(open, { focus = true } = {}) {
    drawerOpen = open;
    shell.dataset.drawer = open ? 'open' : 'closed';
    document.documentElement.classList.toggle('docs-drawer-open', open);
    syncTrigger();

    if (!focus) return;
    if (open) (sider.querySelector('[data-entry][aria-current]') ?? sider.querySelector('a'))?.focus({ preventScroll: true });
    else trigger.focus({ preventScroll: true });
  }

  function hideTip() {
    tip.dataset.show = 'false';
  }

  function showTip(item) {
    const label = item.querySelector('.docs-label');

    if (!collapsed() && !(label && label.scrollWidth > label.clientWidth)) return;

    const rect = item.getBoundingClientRect();
    const rtl = getComputedStyle(shell).direction === 'rtl';
    const text = item === brand ? title : label?.textContent;

    tip.textContent = text ?? '';
    tip.style.top = `${rect.top + rect.height / 2}px`;
    tip.style.left = rtl ? '' : `${rect.right + 10}px`;
    tip.style.right = rtl ? `${window.innerWidth - rect.left + 10}px` : '';
    tip.dataset.show = 'true';
  }

  trigger.addEventListener('click', () => (RAIL.matches ? setCollapsed(!collapsed()) : setDrawer(!drawerOpen)));
  shell.querySelector('.docs-scrim').addEventListener('click', () => setDrawer(false));

  sider.addEventListener('click', event => {
    const link = event.target.closest('[data-entry]');

    if (!link || !drawerOpen) return;

    setDrawer(false, { focus: false });

    const target = entries.find(entry => entry.id === link.dataset.entry)?.target;

    if (target) {
      target.tabIndex = -1;
      setTimeout(() => target.focus({ preventScroll: true }), 50);
    }
  });

  sider.addEventListener('pointerover', event => {
    const item = event.target.closest('.docs-item, .docs-brand');

    if (item) showTip(item);
    else hideTip();
  });
  sider.addEventListener('pointerleave', hideTip);
  sider.addEventListener('focusin', event => {
    const item = event.target.closest('.docs-item');
    if (item) showTip(item);
  });
  sider.addEventListener('focusout', hideTip);
  nav.addEventListener('scroll', hideTip, { passive: true });
  window.addEventListener('scroll', hideTip, { passive: true });

  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && drawerOpen) setDrawer(false);
  });

  RAIL.addEventListener('change', () => {
    if (drawerOpen) setDrawer(false, { focus: false });
    hideTip();
    syncTrigger();
  });

  window.addEventListener('site-locales:change', translate);

  window.docsSidebar = { current: null };

  const mapObserver = typeof MutationObserver === 'undefined' ? null : new MutationObserver(queueMap);

  mapList.addEventListener('click', event => {
    const link = event.target.closest('[data-map-item]');
    const heading = link && document.getElementById(link.dataset.mapItem);

    if (!heading) return;

    event.preventDefault();
    window.dispatchEvent(new CustomEvent('docs:map-select', { detail: { id: heading.id } }));
    // Near the end of a page a heading cannot reach the top, so the clicked item stays marked until the reader scrolls.
    pinned = heading.id;
    heading.scrollIntoView({ behavior: REDUCED.matches ? 'auto' : 'smooth', block: 'start' });
    spyMap();
    window.history.replaceState(window.history.state, '', `#${heading.id}`);
  });

  let spyFrame = 0;

  window.addEventListener('docs:map-pin', event => {
    pinned = event.detail.id;
    spyMap();
  });

  for (const type of ['wheel', 'touchstart', 'keydown']) window.addEventListener(type, () => (pinned = ''), { passive: true });

  window.addEventListener(
    'scroll',
    () => {
      cancelAnimationFrame(spyFrame);
      spyFrame = requestAnimationFrame(spyMap);
    },
    { passive: true },
  );

  window.addEventListener('hashchange', () => showTab(true));
  showTab(false);

  if (typeof ResizeObserver !== 'undefined') {
    new ResizeObserver(() => {
      const sticky = getComputedStyle(bar).position === 'sticky';
      document.documentElement.style.setProperty('--docs-bar-h', sticky ? `${bar.getBoundingClientRect().height}px` : '0px');
    }).observe(bar);
  }

  translate();

  // Panels appear once Alpine has rendered them, so filling them in never pushes content that was already painted.
  // A page lists the faces its content needs (<meta name="adp-docs-fonts">), since shadow content asks for them only after it renders.
  const faces = (document.querySelector('meta[name="adp-docs-fonts"]')?.content ?? '')
    .split(';')
    .map(face => face.trim())
    .filter(Boolean);
  const fontsSettled = () =>
    Promise.race([Promise.all([...faces.map(face => document.fonts?.load(face).catch(() => null)), document.fonts?.ready]), new Promise(resolve => setTimeout(resolve, 3000))]);
  // The family group is filled from the catalog before the shell shows, so the sidebar never grows after first paint.
  const familyFilled = family
    ? familyPages(family).then(pages => {
        if (!pages.length) return;
        nav.innerHTML = familyMarkup(family, pages, entries, title);
        mark(current, 'page');
        translate();
      })
    : Promise.resolve();
  const reveal = () => Promise.all([fontsSettled(), familyFilled]).then(() => requestAnimationFrame(() => (shell.dataset.mounted = 'true')));

  if (window.Alpine?.version || !document.querySelector('[x-data]')) reveal();
  else document.addEventListener('alpine:initialized', reveal, { once: true });

  // Transitions only after the stored state has painted, so a remembered rail does not animate in.
  requestAnimationFrame(() => requestAnimationFrame(() => (shell.dataset.ready = 'true')));
}
