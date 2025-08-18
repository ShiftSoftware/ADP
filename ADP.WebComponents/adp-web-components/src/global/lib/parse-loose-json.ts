export function parseLooseJson(loose) {
  try {
    let s = loose.trim();

    // 1) remove trailing commas before } or ]
    s = s.replace(/,\s*([}\]])/g, '$1');

    // 2) quote unquoted keys: { tag: 'div' } -> { "tag": 'div' }
    s = s.replace(/([{,]\s*)([A-Za-z_]\w*)\s*:/g, '$1"$2":');

    // 3) turn 'single-quoted' strings into "double-quoted"
    s = s.replace(/'([^'\\]*(?:\\.[^'\\]*)*)'/g, (_, inner) => {
      // escape any double-quotes already inside
      return `"${inner.replace(/"/g, '\\"')}"`;
    });

    return JSON.parse(s);
  } catch (err) {
    console.error('Failed to parse loose JSON content:', err.message);
    return null;
  }
}
