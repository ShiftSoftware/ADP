// Exports embed a reviewed snapshot. Only a source preview may check live files.
(() => {
  'use strict';
  const embedded = document.getElementById('explorer-evidence');
  const offline = !!embedded || location.protocol === 'file:';
  window.ExplorerEvidence = {
    offline,
    async load() {
      if (embedded) return JSON.parse(embedded.textContent);
      if (offline) throw new Error('Open an exported Explorer HTML file, or run the source preview server.');
      const response = await fetch('evidence.json', {cache:'no-store'});
      if (!response.ok) throw new Error('Evidence snapshot unavailable');
      return response.json();
    },
    async freshness() {
      if (offline) return null;
      try {
        const response = await fetch('evidence-status.json', {cache:'no-store'});
        return response.ok ? await response.json() : null;
      } catch { return null; }
    }
  };
})();
