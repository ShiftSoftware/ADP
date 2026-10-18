// Shared DOM and controls. Content, routes and scene behavior belong to each Explorer.
(() => {
  'use strict';
  const header = document.querySelector('[data-explorer-product]');
  if (!header) return;
  const {explorerProduct: product, explorerIcon: icon, explorerTheme: themeKey} = header.dataset;
  header.outerHTML = `<header class="masthead">
      <div class="brand"><img src="${icon}" alt="" width="42" height="42"><span>ADP.<strong>${product}</strong></span></div>
      <div class="header-actions">
        <div class="context">Engineering <span>/</span> Explorer</div>
        <button type="button" id="theme-toggle" class="theme-toggle" aria-label="Switch to dark theme"><span aria-hidden="true">◐</span><span id="theme-label">Dark theme</span></button>
      </div>
    </header>`;
  const overview = document.querySelector('[data-explorer-overview]');
  const diagram = overview.querySelector('.diagram');
  const boundary = overview.querySelector('.existing-path');
  const overviewInspector = overview.querySelector('.overview-inspector-body');
  overview.innerHTML = `
    <div class="overview-toolbar">
      <h1 id="page-title">${overview.dataset.overviewTitle}</h1>
      <button type="button" id="toggle" class="motion-toggle" aria-label="Pause motion"><span id="toggle-symbol" aria-hidden="true">Ⅱ</span><span id="toggle-text">Pause motion</span></button>
    </div>
    <div class="overview-workspace">
    <div class="overview-canvas">
    <div class="overview-caption"><span>Select a stage to inspect it.</span><span>Illustrative flow</span></div>
    <div data-overview-diagram-slot></div>

    <div data-overview-boundary-slot></div>

    </div>
    <aside id="overview-inspector" class="overview-inspector" aria-label="Overview inspector" data-open="false">
      <div class="overview-inspector-bar"><span class="overview-inspector-heading">STAGE INSPECTOR</span><button type="button" id="overview-inspector-toggle" aria-controls="overview-inspector-body" aria-expanded="false"><span id="overview-drawer-label">Collect &amp; stage</span><span>Details ↗</span></button><button type="button" id="overview-inspector-close" aria-label="Close overview inspector">×</button></div>
      <div data-overview-inspector-slot></div>
    </aside>
    </div>
    `;
  overview.querySelector('[data-overview-diagram-slot]').replaceWith(diagram);
  overview.querySelector('[data-overview-boundary-slot]').replaceWith(boundary);
  overview.querySelector('[data-overview-inspector-slot]').replaceWith(overviewInspector);
  const detail = document.querySelector('[data-explorer-detail]');
  const inspectorBody = detail.querySelector('.inspector-body');
  const fourth = detail.dataset.fourthLayer || 'direction';
  const fourthLabel = detail.dataset.fourthLabel || 'Change brief';
  detail.innerHTML = `      <div class="explorer-bar">
        <nav class="breadcrumbs" aria-label="Breadcrumb"><a href="#">← Overview</a></nav>
        <label class="control-field">Mechanism<select id="mechanism-picker" aria-label="Explore a mechanism"></select></label>
        <label class="control-field scenario-field">Scenario<select id="scenario-picker"></select></label>
        <button type="button" id="detail-motion" class="motion-toggle">Pause motion</button>
      </div>
      <div class="engineering-workspace">
        <section class="visual-workspace" aria-label="Mechanism diagram">
          <div class="canvas-caption"><h1 id="mechanism-title"></h1><span>Illustrative scenario</span></div>
          <div id="scene" class="scene" aria-label="Animated mechanism"></div>
          <div class="diagram-footer"><span>Select a node or row to inspect it.</span><span id="scenario-status"></span></div>
        </section>
        <aside id="inspector" class="inspector" aria-label="Mechanism inspector" data-open="false">
          <nav class="layer-tabs" aria-label="Inspection layers">
            <button type="button" data-layer="behavior" aria-pressed="true">Behavior</button><button type="button" data-layer="evidence" aria-pressed="false">Evidence</button><button type="button" data-layer="choices" aria-pressed="false">Choices</button><button type="button" data-layer="${fourth}" aria-pressed="false">${fourthLabel}</button><button type="button" id="close-inspector" aria-label="Close inspector">×</button>
          </nav>
<div data-inspector-slot></div></aside></div>`;
  detail.querySelector('[data-inspector-slot]').replaceWith(inspectorBody);
  function updateTheme() {
    const dark = document.documentElement.dataset.theme === 'dark';
    document.getElementById('theme-toggle').setAttribute('aria-label', 'Switch to ' + (dark ? 'light' : 'dark') + ' theme');
    document.getElementById('theme-label').textContent = dark ? 'Light theme' : 'Dark theme';
  }
  document.getElementById('theme-toggle').addEventListener('click', () => {
    const theme = document.documentElement.dataset.theme === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = theme;
    try { localStorage.setItem(themeKey, theme); } catch { /* Saving is optional. */ }
    updateTheme();
  });
  window.ExplorerShell = {
    detailMotion(playing) {
      const button = document.getElementById('detail-motion');
      button.textContent = playing ? 'Ⅱ Pause motion' : '▶ Resume motion';
      button.setAttribute('aria-label', playing ? 'Pause detail motion' : 'Resume detail motion');
    }
  };
  updateTheme();
})();
