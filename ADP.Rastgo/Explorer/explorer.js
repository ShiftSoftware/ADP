(() => {
  'use strict';
  const {mechanisms} = globalThis.RastgoExplorer;
  const $ = id => document.getElementById(id);
  const escape = value => String(value).replace(/[&<>"']/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  const motion = window.ExplorerMotion;
  let topic = mechanisms[0], scenario = topic.scenarios[0], selected = 0;
  let activeTab = 'behavior', evidence = null, freshness = null;
  let selectedRow = null;
  const badge = status => '<span class="verdict" data-status="' + escape(status) + '">' + escape(status === 'Context' ? 'Architecture' : status) + '</span>';
  const context = {
    relationship: ['Publication boundary', 'The last committed fact limits what readers can see.'],
    checks: ['Measure contract', 'Scalar: v. Grouped: k + v. Missing groups are not invented.'],
    freshness: ['Policy boundaries', 'Strict > comparisons; equality does not breach.'],
    threshold: ['Policy boundaries', 'Below min or above max breaches; equality passes.'],
    comparison: ['Comparison contract', 'Union of keys; missing and null numbers become zero.'],
    sources: ['Host responsibility', 'Exact source registration and trusted queries.'],
    history: ['History identity', 'Stable check names; unique run IDs per write.'],
    authoring: ['Validation boundary', 'Strict authoring diagnostics differ from runtime loading.']
  };
  function updateMotion() {
    window.ExplorerShell.detailMotion(motion.isPlaying());
    $('scenario-status').textContent = motion.isPlaying() ? 'Illustrative motion' : 'Motion paused';
  }
  $('detail-motion').addEventListener('click', () => motion.toggle());
  document.addEventListener('explorer:motion-state', updateMotion);
  $('mechanism-picker').innerHTML = mechanisms.map(m => '<option value="' + m.id + '">' + escape(m.title) + '</option>').join('');
  $('mechanism-picker').addEventListener('change', () => { location.hash = $('mechanism-picker').value; });
  $('scenario-picker').addEventListener('change', () => { location.hash = topic.id + '/' + $('scenario-picker').value; });
  $('reference-list').innerHTML = mechanisms.map(m => '<p><a href="#' + m.id + '">' + escape(m.title) + '</a></p>').join('');
  $('explore-inside').addEventListener('click', () => {
    location.hash = {collect:'sources',snapshot:'checks',serving:'checks',cosmos:'history',publish:'history'}[document.body.dataset.selectedStage];
  });
  document.querySelectorAll('[data-layer]').forEach(button => button.addEventListener('click', () => showTab(button.dataset.layer, true)));
  $('close-inspector').addEventListener('click', () => {
    $('inspector').dataset.open = 'false';
    (selectedRow || document.querySelector('[data-node="' + selected + '"]'))?.focus({preventScroll:true});
  });
  document.addEventListener('keydown', event => {
    if (event.key === 'Escape' && innerWidth <= 900 && document.body.dataset.view === 'detail' && $('inspector').dataset.open === 'true') $('close-inspector').click();
  });
  function showTab(tab, reveal = false) {
    activeTab = tab;
    document.querySelectorAll('[data-layer]').forEach(button => button.setAttribute('aria-pressed', String(button.dataset.layer === tab)));
    ['behavior','evidence','choices','example'].forEach(id => { $('layer-' + id).hidden = id !== tab; });
    if (reveal) $('inspector').dataset.open = 'true';
    document.querySelector('.inspector-body').scrollTop = 0;
    if (tab === 'evidence') renderEvidence();
  }
  function selectNode(index, reveal = false) {
    selected = index; selectedRow = null;
    document.querySelectorAll('[data-node]').forEach(button => {
      button.setAttribute('aria-pressed', String(Number(button.dataset.node) === index));
      button.classList.toggle('selected', Number(button.dataset.node) === index);
    });
    const step = topic.steps[index];
    $('selection-title').textContent = step ? step.title : index === 3 ? context[topic.id][0] : scenario.title;
    $('selection-detail').textContent = step ? step.detail : index === 3 ? topic.limits : topic.summary;
    $('selection-result').innerHTML = '<span>In this scenario</span><p>' + escape(step ? scenario.values[index] : index === 3 ? context[topic.id][1] : scenario.detail) + '</p>' + (index === 4 ? badge(scenario.status) : '');
    if (reveal) showTab('behavior', true);
  }
  function render() {
    $('mechanism-picker').value = topic.id;
    $('mechanism-title').textContent = topic.title;
    $('mechanism-summary').textContent = topic.summary;
    $('scenario-picker').innerHTML = topic.scenarios.map(s => '<option value="' + s.id + '">' + escape(s.label) + '</option>').join('');
    $('scenario-picker').value = scenario.id;
    const nodes = [
      {index:0,title:topic.steps[0].title,copy:scenario.values[0],tag:'Step 01'},
      {index:3,title:context[topic.id][0],copy:context[topic.id][1],tag:'Context'},
      {index:1,title:topic.steps[1].title,copy:scenario.values[1],tag:'Step 02'},
      {index:2,title:topic.steps[2].title,copy:scenario.values[2],tag:'Step 03'},
      {index:4,title:scenario.title,copy:scenario.status === 'Context' ? 'Authored architecture example' : 'Selected scenario verdict',tag:scenario.status}
    ];
    $('scene').innerHTML = '<div class="scene-diagram graph-diagram rastgo-diagram"><svg class="scene-wires" aria-hidden="true"></svg>' + nodes.map(n =>
      '<button type="button" class="scene-node" data-node="' + n.index + '" aria-pressed="false"><span class="node-title">' + escape(n.title) + '</span><span class="node-copy">' + escape(n.copy) + '</span>' + (n.index === 4 ? badge(n.tag) : '<span class="node-state">' + escape(n.tag) + '</span>') + '</button>').join('') + '</div>' +
      (scenario.rows.length ? '<table class="sample-table result-table"><caption>Illustrative result rows</caption><thead><tr><th scope="col">Group</th><th scope="col">Observation</th><th scope="col">Verdict</th></tr></thead><tbody>' + scenario.rows.map((row, index) => '<tr><td><button type="button" data-row="' + index + '">' + escape(row[0]) + '</button></td><td>' + escape(row[1]) + '</td><td>' + badge(row[2]) + '</td></tr>').join('') + '</tbody></table>' : '');
    document.querySelectorAll('[data-node]').forEach(button => button.addEventListener('click', () => selectNode(Number(button.dataset.node), true)));
    document.querySelectorAll('[data-row]').forEach(button => button.addEventListener('click', () => {
      const row = scenario.rows[Number(button.dataset.row)];
      selectedRow = button;
      document.querySelectorAll('[data-node]').forEach(node => { node.setAttribute('aria-pressed', 'false'); node.classList.remove('selected'); });
      $('selection-title').textContent = row[0];
      $('selection-detail').textContent = scenario.detail;
      $('selection-result').innerHTML = '<span>Illustrative result row</span><p>' + escape(row[1]) + '</p>' + badge(row[2]);
      showTab('behavior', true);
    }));
    $('limits').textContent = topic.limits;
    $('choice-list').innerHTML = topic.choices.map(c => '<li>' + escape(c) + '</li>').join('');
    $('example-title').textContent = topic.exampleTitle;
    $('example-copy').textContent = topic.exampleCopy;
    $('example-code').textContent = topic.example;
    selectNode(selected); showTab(activeTab); updateMotion();
    requestAnimationFrame(drawWires);
  }
  function drawWires() {
    if (document.body.dataset.view !== 'detail') return;
    const graph = document.querySelector('.rastgo-diagram');
    if (!graph) return;
    const base = graph.getBoundingClientRect();
    const svg = graph.querySelector('svg');
    svg.setAttribute('viewBox', '0 0 ' + base.width + ' ' + base.height);
    // Context is explanatory, not a fourth measured input. Only the three steps
    // and their authored outcome carry flow; no live execution is implied.
    svg.innerHTML = [[0,1],[1,2],[2,4]].map(([from,to]) => {
      const a = graph.querySelector('[data-node="' + from + '"]').getBoundingClientRect();
      const b = graph.querySelector('[data-node="' + to + '"]').getBoundingClientRect();
      const vertical = to === 4 || innerWidth <= 600;
      const x1 = (vertical ? a.x+a.width/2 : a.right)-base.x;
      const y1 = (vertical ? a.bottom : a.y+a.height/2)-base.y;
      const x2 = (vertical ? b.x+b.width/2 : b.x)-base.x;
      const y2 = (vertical ? b.y : b.y+b.height/2)-base.y;
      const d = vertical ? `M${x1},${y1} C${x1},${(y1+y2)/2} ${x2},${(y1+y2)/2} ${x2},${y2}` : `M${x1},${y1} C${(x1+x2)/2},${y1} ${(x1+x2)/2},${y2} ${x2},${y2}`;
      return '<path class="scene-wire applicable" d="' + d + '"/><path class="rastgo-flow" d="' + d + '"/>';
    }).join('');
  }
  new ResizeObserver(drawWires).observe($('scene'));
  function route() {
    const [topicId,scenarioId] = location.hash.slice(1).split('/');
    const detail = !!topicId;
    document.body.dataset.view = detail ? 'detail' : 'overview';
    $('overview').hidden = detail; $('engineering').hidden = !detail;
    if (!detail) return;
    topic = mechanisms.find(m => m.id === topicId) || mechanisms[0];
    scenario = topic.scenarios.find(s => s.id === scenarioId) || topic.scenarios[0];
    selected = 0; $('inspector').dataset.open = 'false';
    render();
    if (topicId === 'workspace') $('scene').focus();
  }
  $('scene').tabIndex = 0;
  function renderEvidence() {
    if (!evidence) {
      $('evidence-status').textContent = 'Evidence snapshot unavailable or still loading. Serve the Explorer with server.mjs.';
      $('evidence-list').replaceChildren();
      return;
    }
    const currentCapture = freshness && freshness.capturedAt === evidence.capturedAt && freshness.hashAlgorithm === evidence.hashAlgorithm;
    $('evidence-status').textContent = window.ExplorerEvidence.offline ? 'Offline snapshot: current source files have not been checked. Freshness unverified.' : currentCapture ? 'Local comparison completed. “Unchanged” means the captured source file still matches.' : 'Captured evidence available. Local source freshness is unverified.';
    $('evidence-list').innerHTML = topic.evidence.map(id => {
      const entry = evidence.entries.find(e => e.id === id);
      if (!entry) return '<p>Missing evidence entry: ' + escape(id) + '</p>';
      const result = currentCapture && freshness.checks.find(c => c.id === id && c.sha256 === entry.sha256);
      const status = result ? result.status : 'unverified';
      return '<details class="evidence-card"><summary>' + escape(entry.path.split('/').pop()) + ' · lines ' + entry.start + '–' + entry.end +
        '</summary><p class="small">' + escape(entry.path) + '<br>Source: ' + escape(status) + '</p><pre><code>' + escape(entry.excerpt) + '</code></pre></details>';
    }).join('');
  }
  async function loadEvidence() {
    try {
      evidence = await window.ExplorerEvidence.load();
      $('capture-date').textContent = evidence.capturedAt.slice(0, 10);
      if (activeTab === 'evidence') renderEvidence();
      freshness = await window.ExplorerEvidence.freshness();
    } catch {
      $('capture-date').textContent = 'unavailable';
    }
    if (activeTab === 'evidence') renderEvidence();
  }
  window.addEventListener('hashchange', route);
  updateMotion(); route(); loadEvidence();
})();
