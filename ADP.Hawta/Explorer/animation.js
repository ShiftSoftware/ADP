(() => {
  'use strict';
  const explanations = {
    collect: ['↓', 'Collect & stage', 'Collect from the systems that know.', 'This example brings dealer views, file feeds, app tables and document sources into one ingestion flow. Hawta schedules each source and limits parallel fetching to keep the work bounded.'],
    snapshot: ['≡', 'Reconcile the snapshot', 'Keep the changes. Skip the repeated work.', 'Hawta compares incoming records with its DuckDB snapshot and merges changes in sets. Unchanged file feeds can skip the read, while the snapshot preserves what each source said.'],
    serving: ['⇢', 'Shape for consumers', 'Keep the source. Build a useful view.', 'Serving projections turn source-shaped tables into ADP models. Consumers share those prepared views; the original source tables remain available alongside them.'],
    cosmos: ['↗', 'Cosmos DB', 'Deliver changes to application lookups.', 'Hawta replicates changes from source or serving tables for the data families it owns. App-owned records keep their existing replication path, so every family has one Cosmos writer.'],
    publish: ['↗', 'Versioned Parquet', 'Give reporting a consistent picture.', 'Hawta publishes a versioned set of source and serving tables in its output directory. Reports, analytics and health checks can read that committed set. The host configures storage delivery, independently of Cosmos serving.']
  };
  const diagram = document.querySelector('.diagram');
  const svg = document.querySelector('.connections');
  const routesGroup = document.querySelector('.routes');
  const packetsGroup = document.querySelector('.packets');
  const stageButtons = [...document.querySelectorAll('[data-stage]')];
  const toggle = document.getElementById('toggle');
  const themeToggle = document.getElementById('theme-toggle');
  const overviewInspector = document.getElementById('overview-inspector');
  const overviewInspectorToggle = document.getElementById('overview-inspector-toggle');
  const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)');
  let elapsed = 0;
  let playing = !reducedMotion.matches;
  let explicitlyEnabledMotion = false;
  let previous = null;
  let routes = [];
  let pathScheduled = false;

  function selectStage(stage, reveal = false) {
    const [icon, label, title, copy] = explanations[stage];
    document.getElementById('explanation-icon').textContent = icon;
    document.getElementById('selected-label').textContent = label;
    document.getElementById('explanation-title').textContent = title;
    document.getElementById('explanation-copy').textContent = copy;
    document.body.dataset.selectedStage = stage;
    document.getElementById('overview-drawer-label').textContent = label;
    stageButtons.forEach(button => {
      const selected = button.dataset.stage === stage;
      button.setAttribute('aria-pressed', String(selected));
      button.parentElement.classList.toggle('selected', selected);
    });
    if (reveal) {
      setOverviewInspector(true);
      document.getElementById('overview-inspector-body').scrollTop = 0;
    }
  }

  function setOverviewInspector(open, restoreFocus = false) {
    overviewInspector.dataset.open = String(open);
    overviewInspectorToggle.setAttribute('aria-expanded', String(open));
    if (restoreFocus) stageButtons.find(button => button.dataset.stage === document.body.dataset.selectedStage)?.focus({preventScroll:true});
  }
  overviewInspectorToggle.addEventListener('click', () => setOverviewInspector(overviewInspector.dataset.open !== 'true'));
  document.getElementById('overview-inspector-close').addEventListener('click', () => setOverviewInspector(false, true));
  document.getElementById('explore-inside').addEventListener('click', () => setOverviewInspector(false));
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && window.innerWidth <= 850 && document.body.dataset.view === 'overview' && overviewInspector.dataset.open === 'true') setOverviewInspector(false, true);
  });

  function updateThemeControl() {
    const dark = document.documentElement.dataset.theme === 'dark';
    themeToggle.setAttribute('aria-label', 'Switch to ' + (dark ? 'light' : 'dark') + ' theme');
    document.getElementById('theme-label').textContent = dark ? 'Light theme' : 'Dark theme';
  }
  themeToggle.addEventListener('click', () => {
    const theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = theme;
    try { localStorage.setItem('hawta-demo-theme', theme); } catch { /* Saving is optional. */ }
    updateThemeControl();
  });

  function svgElement(name, attributes) {
    const element = document.createElementNS('http://www.w3.org/2000/svg', name);
    for (const [key, value] of Object.entries(attributes)) element.setAttribute(key, value);
    return element;
  }

  function anchor(id, side, offset = 0) {
    const bounds = document.getElementById(id).getBoundingClientRect();
    const base = diagram.getBoundingClientRect();
    if (side === 'right') return {x: bounds.right - base.left, y: bounds.top - base.top + bounds.height / 2 + offset};
    if (side === 'left') return {x: bounds.left - base.left, y: bounds.top - base.top + bounds.height / 2 + offset};
    if (side === 'bottom') return {x: bounds.left - base.left + bounds.width / 2 + offset, y: bounds.bottom - base.top};
    return {x: bounds.left - base.left + bounds.width / 2 + offset, y: bounds.top - base.top};
  }

  function connect(from, to, index, directSource = false) {
    const vertical = window.innerWidth <= 850;
    const start = anchor(from, vertical ? 'bottom' : 'right');
    const end = anchor(to, vertical ? 'top' : 'left', to === 'collect' ? (index - 1.5) * (vertical ? 22 : 11) : 0);
    const distance = vertical ? Math.max(25, Math.abs(end.y - start.y) * .48) : Math.max(20, Math.abs(end.x - start.x) * .48);
    const d = vertical
      ? 'M ' + start.x + ' ' + start.y + ' C ' + start.x + ' ' + (start.y + distance) + ', ' + end.x + ' ' + (end.y - distance) + ', ' + end.x + ' ' + end.y
      : 'M ' + start.x + ' ' + start.y + ' C ' + (start.x + distance) + ' ' + start.y + ', ' + (end.x - distance) + ' ' + end.y + ', ' + end.x + ' ' + end.y;
    const path = svgElement('path', {d, class: 'route' + (directSource ? ' direct-source' : ''), 'data-from': from, 'data-to': to});
    routesGroup.append(path);
    const packets = Array.from({length: 3}, (_, packetIndex) => {
      const dot = svgElement('circle', {r: packetIndex === 0 ? 3.4 : 2.4, class: 'packet', opacity: 0});
      packetsGroup.append(dot);
      return dot;
    });
    routes.push({path, packets, length: path.getTotalLength(), offset: index * .17});
  }

  function drawPaths() {
    pathScheduled = false;
    const bounds = diagram.getBoundingClientRect();
    svg.setAttribute('viewBox', '0 0 ' + bounds.width + ' ' + bounds.height);
    routesGroup.replaceChildren();
    packetsGroup.replaceChildren();
    routes = [];
    ['dms', 'feeds', 'apps', 'logs'].forEach((id, index) => connect(id, 'collect', index));
    connect('serving', 'cosmos', 0);
    connect('snapshot', 'cosmos', 3, true);
    connect('snapshot', 'publish', 1, true);
    connect('serving', 'publish', 2);
    animatePackets();
  }

  // Animation never changes the selected explanation. Every route runs together.
  function animatePackets() {
    for (const route of routes) {
      route.packets.forEach((packet, index) => {
        const t = (elapsed / 3400 + index / 3 + route.offset) % 1;
        const point = route.path.getPointAtLength(route.length * t);
        packet.setAttribute('cx', point.x);
        packet.setAttribute('cy', point.y);
        packet.setAttribute('opacity', .95 * Math.sin(Math.PI * t));
      });
    }
  }

  function playbackState() {
    document.body.dataset.playing = String(playing);
    document.body.dataset.motionVisible = String(!reducedMotion.matches || explicitlyEnabledMotion);
    toggle.setAttribute('aria-label', playing ? 'Pause motion' : 'Resume motion');
    document.getElementById('toggle-text').textContent = playing ? 'Pause motion' : 'Resume motion';
    document.getElementById('toggle-symbol').textContent = playing ? 'Ⅱ' : '▶';
    document.dispatchEvent(new CustomEvent('hawta:motion-state', {detail: {playing}}));
  }
  toggle.addEventListener('click', () => {
    playing = !playing;
    if (playing) explicitlyEnabledMotion = true;
    previous = null;
    playbackState();
  });
  window.HawtaMotion = {
    isPlaying: () => playing,
    toggle: () => toggle.click(),
    pause: () => { if (playing) toggle.click(); },
    resume: () => { if (!playing) toggle.click(); }
  };
  stageButtons.forEach(button => button.addEventListener('click', () => selectStage(button.dataset.stage, true)));
  reducedMotion.addEventListener('change', () => {
    if (reducedMotion.matches) {
      playing = false;
      explicitlyEnabledMotion = false;
    }
    previous = null;
    playbackState();
  });
  document.addEventListener('visibilitychange', () => { previous = null; });
  new ResizeObserver(() => {
    if (!pathScheduled) {
      pathScheduled = true;
      requestAnimationFrame(drawPaths);
    }
  }).observe(diagram);

  function frame(timestamp) {
    if (playing && !document.hidden && previous !== null) {
      elapsed = (elapsed + Math.min(timestamp - previous, 100)) % 340000;
      animatePackets();
    }
    previous = timestamp;
    requestAnimationFrame(frame);
  }

  selectStage('collect');
  updateThemeControl();
  playbackState();
  drawPaths();
  requestAnimationFrame(frame);
})();
