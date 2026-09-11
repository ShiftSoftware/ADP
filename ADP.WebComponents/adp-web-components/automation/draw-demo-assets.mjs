/**
 * Draws the neutral demo images the generated fixtures point at — paint panels, company badges,
 * claim documents and accessories — into src/features/mocks/data/assets/<family>/<file>.svg.
 * Every file is deterministic (same script, same bytes) and original: no photographs, no
 * third-party artwork, no embedded fonts. Which file a fixture value maps to is decided by
 * ADP.TestData/Generator/DemoAssets.cs; a drawing added here is reachable only once that known
 * set names it. The families and the mapping are described in the assets README.
 *
 *   npm run draw:demo-assets            # writes the set
 *   node automation/draw-demo-assets.mjs <dir>   # writes it somewhere else
 */
import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = path.join(path.dirname(fileURLToPath(import.meta.url)), '..');
const out = process.argv[2] ?? path.join(root, 'src', 'features', 'mocks', 'data', 'assets');

const FONT = 'font-family="system-ui,-apple-system,\'Segoe UI\',Roboto,sans-serif"';
const files = new Map();
const emit = (rel, svg) => files.set(rel, svg.replace(/\n\s*/g, '\n').trim() + '\n');

/* ------------------------------------------------------------------ paint panels */

// Top-down car, nose up, in a 336 x 600 portrait frame (the gallery thumbnail is 84 x 150).
const car = {
  body: { x0: 78, x1: 258, y0: 44, y1: 484, rf: 36, rr: 28 },
  hood: { x: 104, y: 56, w: 128, h: 118, rx: 18 },
  roof: { x: 104, y: 230, w: 128, h: 138, rx: 8 },
  tailGate: { x: 104, y: 416, w: 128, h: 56, rx: 14 },
  side: { w: 20, left: 80, right: 236 },
  strips: {
    fenderFront: { y: 62, h: 112 },
    doorFront: { y: 180, h: 98 },
    doorRear: { y: 284, h: 98 },
    fenderRear: { y: 388, h: 86 },
  },
};

// panel slug -> { label, rect } ; slug = type[-position][-side]
const panels = {
  'hood': { label: 'Hood', rect: car.hood },
  'roof': { label: 'Roof', rect: car.roof },
  'tail-gate': { label: 'Tail gate', rect: car.tailGate },
};
for (const [strip, name] of [
  ['fenderFront', 'fender-front'],
  ['doorFront', 'door-front'],
  ['doorRear', 'door-rear'],
  ['fenderRear', 'fender-rear'],
]) {
  for (const side of ['left', 'right']) {
    const s = car.strips[strip];
    const [type, position] = name.split('-');
    panels[`${name}-${side}`] = {
      label: `${side[0].toUpperCase() + side.slice(1)} ${position} ${type}`,
      rect: { x: car.side[side], y: s.y, w: car.side.w, h: s.h, rx: 6 },
    };
  }
}

const bodyPath = ({ x0, x1, y0, y1, rf, rr }) =>
  `M${x0 + rf} ${y0}H${x1 - rf}Q${x1} ${y0} ${x1} ${y0 + rf}V${y1 - rr}Q${x1} ${y1} ${x1 - rr} ${y1}H${x0 + rr}Q${x0} ${y1} ${x0} ${y1 - rr}V${y0 + rf}Q${x0} ${y0} ${x0 + rf} ${y0}Z`;

const rect = (r, cls) => `<rect class="${cls}" x="${r.x}" y="${r.y}" width="${r.w}" height="${r.h}" rx="${r.rx ?? 0}"/>`;

function carDrawing(highlight) {
  const parts = [];
  // wheels first, under the body
  for (const [x, y] of [
    [64, 88],
    [250, 88],
    [64, 400],
    [250, 400],
  ])
    parts.push(`<rect class="w" x="${x}" y="${y}" width="22" height="60" rx="8"/>`);
  parts.push(`<path class="b" d="${bodyPath(car.body)}"/>`);
  // mirrors
  parts.push('<ellipse class="w" cx="70" cy="204" rx="9" ry="6"/><ellipse class="w" cx="266" cy="204" rx="9" ry="6"/>');
  // glass
  parts.push('<path class="g" d="M98 178H238L228 226H108Z"/><path class="g" d="M108 372H228L238 412H98Z"/>');
  for (const [slug, p] of Object.entries(panels)) parts.push(rect(p.rect, slug === highlight ? 'p h' : 'p'));
  // front chevron
  parts.push('<path class="c" d="M160 34L168 22L176 34"/>');
  return parts.join('');
}

const STYLE_PAINT = `<style>.f{fill:#f4f4f5}.b{fill:#d4d4d8;stroke:#71717a;stroke-width:2}.w{fill:#52525b}.g{fill:#cbd5e1;stroke:#94a3b8;stroke-width:1.5}.p{fill:#fafafa;stroke:#a1a1aa;stroke-width:1.5}.h{fill:#f59e0b;stroke:#b45309;stroke-width:2}.c{fill:none;stroke:#71717a;stroke-width:2;stroke-linecap:round;stroke-linejoin:round}.t{fill:#18181b;font-size:22px;font-weight:700}.s{fill:#71717a;font-size:12px;letter-spacing:.08em}.m{fill:none;stroke:#1e3a8a;stroke-width:3}.tag{fill:#1e3a8a}.tt{fill:#fff;font-size:13px;font-weight:700}</style>`;

const labelBand = (title, sub) =>
  `<rect class="f" x="0" y="520" width="336" height="80"/><line x1="24" y1="520" x2="312" y2="520" stroke="#d4d4d8"/><text class="t" x="168" y="556" text-anchor="middle" ${FONT}>${title}</text><text class="s" x="168" y="582" text-anchor="middle" ${FONT}>${sub}</text>`;

function paintPlan(slug, title) {
  return `<svg xmlns="http://www.w3.org/2000/svg" width="336" height="600" viewBox="0 0 336 600">
${STYLE_PAINT}
<rect class="f" width="336" height="600"/>
${carDrawing(slug)}
${labelBand(title, 'PAINT THICKNESS · PLAN VIEW')}
</svg>`;
}

function paintDetail(slug, title) {
  const r = panels[slug].rect;
  const margin = 40;
  const s = Math.min(336 / (r.w + margin * 2), 500 / (r.h + margin * 2), 3.4);
  const cx = r.x + r.w / 2,
    cy = r.y + r.h / 2;
  let tx = 168 - cx * s,
    ty = 250 - cy * s;
  // Centre the panel, but never leave an empty band where the enlarged car would fill the frame:
  // a close-up of the hood shows the nose against the top edge, not floating mid-frame.
  const pad = 16,
    carX = [64, 272],
    carY = [22, 484];
  const clamp = (v, lo, hi) => Math.min(Math.max(v, lo), hi);
  if ((carX[1] - carX[0]) * s >= 336 - 2 * pad) tx = clamp(tx, 336 - pad - carX[1] * s, pad - carX[0] * s);
  if ((carY[1] - carY[0]) * s >= 520 - 2 * pad) ty = clamp(ty, 520 - pad - carY[1] * s, pad - carY[0] * s);
  const fmt = n => Math.round(n * 100) / 100;
  const mx = fmt(cx * s + tx),
    my = fmt(cy * s + ty);
  // gauge marker at the panel centre, drawn in frame coordinates so it keeps its size
  const marker = `<g transform="translate(${mx} ${my})"><circle class="m" r="16"/><path class="m" d="M0 -24V-12M0 12V24M-24 0H-12M12 0H24"/><rect class="tag" x="14" y="-44" width="52" height="24" rx="6"/><text class="tt" x="40" y="-27" text-anchor="middle" ${FONT}>µm</text></g>`;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="336" height="600" viewBox="0 0 336 600">
${STYLE_PAINT}
<rect class="f" width="336" height="600"/>
<g transform="translate(${fmt(tx)} ${fmt(ty)}) scale(${fmt(s)})">${carDrawing(slug)}</g>
${marker}
${labelBand(title, 'PAINT THICKNESS · CLOSE-UP')}
</svg>`;
}

for (const [slug, p] of Object.entries(panels)) {
  emit(`paint-panels/${slug}.svg`, paintPlan(slug, p.label));
  emit(`paint-panels/${slug}-detail.svg`, paintDetail(slug, p.label));
}
emit('paint-panels/panel.svg', paintPlan(null, 'Vehicle panel'));

/* ------------------------------------------------------------------ badges */

const badges = [
  {
    bg: '#1e3a5f',
    ring: '#3b6ea5',
    mark: '<path d="M160 36L206 54V88C206 116 184 136 160 146C136 136 114 116 114 88V54Z" fill="#fff"/><path d="M136 92L160 70L184 92" fill="none" stroke="#1e3a5f" stroke-width="10" stroke-linecap="round" stroke-linejoin="round"/>',
  },
  { bg: '#14532d', ring: '#3f8f5f', mark: '<path d="M160 34L204 57V103L160 126L116 103V57Z" fill="#fff"/><path d="M160 54L172 88H148Z M160 106L148 72H172Z" fill="#14532d"/>' },
  {
    bg: '#7f1d1d',
    ring: '#b45353',
    mark: '<circle cx="160" cy="80" r="46" fill="none" stroke="#fff" stroke-width="12"/><rect x="104" y="72" width="112" height="16" rx="8" fill="#fff"/>',
  },
  { bg: '#27272a', ring: '#71717a', mark: '<path d="M160 30L212 80L160 130L108 80Z" fill="#fff"/><path d="M160 58L182 80L160 102L138 80Z" fill="#27272a"/>' },
  {
    bg: '#134e4a',
    ring: '#3d8b84',
    mark: '<circle cx="160" cy="80" r="22" fill="#fff"/><path d="M130 80C110 60 86 56 62 62C86 74 108 84 130 80Z M190 80C210 60 234 56 258 62C234 74 212 84 190 80Z" fill="#fff"/>',
  },
  {
    bg: '#312e81',
    ring: '#6b67b8',
    mark: '<path d="M104 118L138 58L162 96L176 74L216 118Z" fill="#fff"/><path d="M104 44Q160 16 216 44" fill="none" stroke="#fff" stroke-width="8" stroke-linecap="round"/>',
  },
];
badges.forEach((b, i) => {
  emit(
    `badges/badge-${i + 1}.svg`,
    `<svg xmlns="http://www.w3.org/2000/svg" width="320" height="160" viewBox="0 0 320 160">
<rect width="320" height="160" rx="24" fill="${b.bg}"/>
<rect x="8" y="8" width="304" height="144" rx="18" fill="none" stroke="${b.ring}" stroke-width="3"/>
${b.mark}
</svg>`,
  );
});

/* ------------------------------------------------------------------ documents */

const STYLE_DOC = `<style>.desk{fill:#e7e5e4}.page{fill:#fff;stroke:#d6d3d1}.ln{fill:#d6d3d1}.hd{fill:#3f3f46;font-size:20px;font-weight:700;letter-spacing:.04em}.sm{fill:#71717a;font-size:11px}.ink{fill:none;stroke:#1d4ed8;stroke-width:2.2;stroke-linecap:round}.rule{stroke:#a8a29e;stroke-width:1}.qr{fill:#18181b}</style>`;

const lines = (x, y, widths, gap = 14) => widths.map((w, i) => `<rect class="ln" x="${x}" y="${y + i * gap}" width="${w}" height="6" rx="3"/>`).join('');

emit(
  'documents/signed-claim-document.svg',
  `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="480" viewBox="0 0 640 480">
${STYLE_DOC}
<rect class="desk" width="640" height="480"/>
<rect x="150" y="26" width="340" height="428" rx="4" fill="#0000001a"/>
<rect class="page" x="144" y="20" width="340" height="428" rx="4"/>
<text class="hd" x="314" y="60" text-anchor="middle" ${FONT}>SERVICE CLAIM FORM</text>
<line class="rule" x1="176" y1="74" x2="452" y2="74"/>
${lines(176, 92, [200, 150, 240, 120])}
<rect x="176" y="160" width="276" height="120" rx="3" fill="none" stroke="#d6d3d1"/>
${lines(190, 176, [140, 220, 180, 230, 160, 200, 120])}
<line class="rule" x1="176" y1="300" x2="452" y2="300"/>
${lines(176, 318, [90])}
<path class="ink" d="M190 372C204 342 222 344 226 362C230 380 216 388 238 378C260 368 262 348 278 360C290 370 306 366 320 352C332 340 342 358 356 366C372 376 388 352 402 360"/>
<line class="rule" x1="176" y1="392" x2="420" y2="392"/>
<text class="sm" x="176" y="408" ${FONT}>Customer signature</text>
<text class="sm" x="372" y="408" ${FONT}>Date</text>
${lines(372, 318, [48])}
<circle cx="440" cy="352" r="28" fill="none" stroke="#dc2626" stroke-width="3" opacity=".75"/>
<text x="440" y="357" text-anchor="middle" fill="#dc2626" opacity=".75" font-size="12" font-weight="700" ${FONT}>SIGNED</text>
<g transform="rotate(-24 560 300)"><rect x="548" y="200" width="18" height="190" rx="9" fill="#27272a"/><path d="M548 390L557 418L566 390Z" fill="#a1a1aa"/></g>
</svg>`,
);

// QR placeholder: three finder patterns plus a fixed module pattern (not a real code).
const qrModules = [
  '0000000011000000000000',
  '0000000001010000000000',
  '0000000010010000000000',
  '0000000011100000000000',
  '0000000000110000000000',
  '0000000001010000000000',
  '0000000010100000000000',
  '0000000000000000000000',
  '1010110010111001011010',
  '0110100101010110101101',
  '1001011010001010110010',
  '0101101101110101001101',
  '1100110010101100110110',
  '0011001101010011001001',
  '0000000010110000000000',
  '0000000001001001101011',
  '0000000011010110010110',
  '0000000000111011101001',
  '0000000010100101001110',
  '0000000001011010110001',
  '0000000011000110101010',
  '0000000010111001010101',
];
function qr(x, y, size) {
  const n = qrModules.length,
    m = size / n;
  const cells = [];
  for (let r = 0; r < n; r++) for (let c = 0; c < n; c++) if (qrModules[r][c] === '1') cells.push(`M${x + c * m} ${y + r * m}h${m}v${m}h-${m}z`);
  const finder = (fx, fy) =>
    `<rect class="qr" x="${fx}" y="${fy}" width="${7 * m}" height="${7 * m}"/><rect x="${fx + m}" y="${fy + m}" width="${5 * m}" height="${5 * m}" fill="#fff"/><rect class="qr" x="${fx + 2 * m}" y="${fy + 2 * m}" width="${3 * m}" height="${3 * m}"/>`;
  return `<rect x="${x - m}" y="${y - m}" width="${size + 2 * m}" height="${size + 2 * m}" fill="#fff"/><path class="qr" d="${cells.join('')}"/>${finder(x, y)}${finder(x + (n - 7) * m, y)}${finder(x, y + (n - 7) * m)}`;
}

emit(
  'documents/service-invoice-qr.svg',
  `<svg xmlns="http://www.w3.org/2000/svg" width="640" height="480" viewBox="0 0 640 480">
${STYLE_DOC}
<rect class="desk" width="640" height="480"/>
<rect x="150" y="26" width="340" height="428" rx="4" fill="#0000001a"/>
<rect class="page" x="144" y="20" width="340" height="428" rx="4"/>
<text class="hd" x="176" y="60" ${FONT}>SERVICE INVOICE</text>
${lines(176, 78, [120, 90, 150])}
${qr(372, 44, 80)}
<line class="rule" x1="176" y1="140" x2="452" y2="140"/>
<rect x="176" y="150" width="276" height="16" fill="#f4f4f5"/>
${lines(184, 155, [60])}${lines(300, 155, [40])}${lines(392, 155, [40])}
${lines(184, 182, [100, 130, 90, 120, 110])}${lines(300, 182, [40, 40, 40, 40, 40])}${lines(392, 182, [44, 44, 44, 44, 44])}
<line class="rule" x1="176" y1="262" x2="452" y2="262"/>
<rect x="340" y="276" width="112" height="34" rx="3" fill="none" stroke="#a8a29e"/>
${lines(350, 290, [40])}${lines(404, 290, [40])}
${lines(176, 340, [180, 140, 200])}
<text class="sm" x="176" y="420" ${FONT}>Scan the QR code to submit the claim</text>
</svg>`,
);

/* ------------------------------------------------------------------ accessories */

const STYLE_ACC = `<style>.bg{fill:#f4f4f5}.k{fill:#27272a}.k2{fill:#3f3f46}.k3{fill:#52525b}.al{fill:#a1a1aa}.al2{fill:#d4d4d8}.ol{fill:none;stroke:#18181b;stroke-width:2;stroke-linejoin:round}.hl{fill:#f59e0b}.cap{fill:#71717a;font-size:11px;letter-spacing:.1em}</style>`;
const accFrame = (name, body) => `<svg xmlns="http://www.w3.org/2000/svg" width="400" height="300" viewBox="0 0 400 300">
${STYLE_ACC}
<rect class="bg" width="400" height="300"/>
${body}
<text class="cap" x="200" y="282" text-anchor="middle" ${FONT}>GENUINE ACCESSORY · ${name}</text>
</svg>`;

// Side steps: a running board in shallow perspective with two brackets and a tread pattern.
emit(
  'accessories/side-steps.svg',
  accFrame(
    'SIDE STEPS',
    `
<path class="k2" d="M60 150L340 120L352 138L72 170Z"/>
<path class="k" d="M72 170L352 138L352 154L72 186Z"/>
<path class="al" d="M60 150L340 120L340 128L60 158Z"/>
${[0, 1, 2, 3, 4, 5, 6, 7]
  .map(i => {
    const x = 88 + i * 32;
    const y = 157 - (x - 66) * 0.11;
    return `<path class="al2" d="M${x} ${y}l14 -1.5v6l-14 1.5z"/>`;
  })
  .join('')}
<rect class="k3" x="110" y="176" width="14" height="40" rx="3"/><rect class="k3" x="280" y="158" width="14" height="40" rx="3"/>
<rect class="k" x="100" y="212" width="34" height="10" rx="3"/><rect class="k" x="270" y="194" width="34" height="10" rx="3"/>
`,
  ),
);

// Roof rails: two longitudinal rails with feet, seen from a raised side angle.
emit(
  'accessories/roof-rails.svg',
  accFrame(
    'ROOF RAILS',
    `
<path class="al" d="M70 132Q200 96 330 132L330 142Q200 108 70 142Z"/>
<path class="al2" d="M70 132Q200 96 330 132L330 136Q200 102 70 136Z"/>
<path class="k" d="M64 132h20v18h-20z M170 108h20v18h-20z M316 132h20v18h-20z"/>
<path class="al" d="M70 186Q200 150 330 186L330 196Q200 162 70 196Z"/>
<path class="al2" d="M70 186Q200 150 330 186L330 190Q200 156 70 190Z"/>
<path class="k" d="M64 186h20v18h-20z M170 162h20v18h-20z M316 186h20v18h-20z"/>
`,
  ),
);

// Roof rack: a basket with cross bars.
emit(
  'accessories/roof-rack.svg',
  accFrame(
    'ROOF RACK',
    `
<path class="ol" d="M90 96L310 96L340 190L60 190Z"/>
<path class="ol" d="M90 96L90 84L310 84L310 96 M60 190L60 176L340 176L340 190"/>
${[0, 1, 2, 3, 4].map(i => `<path class="ol" d="M${96 + i * 52} 96L${68 + i * 66} 190"/>`).join('')}
<path class="k2" d="M84 84h232v6H84z M56 176h288v6H56z"/>
<rect class="k" x="112" y="196" width="14" height="30" rx="3"/><rect class="k" x="274" y="196" width="14" height="30" rx="3"/>
<rect class="k3" x="100" y="226" width="38" height="8" rx="3"/><rect class="k3" x="262" y="226" width="38" height="8" rx="3"/>
`,
  ),
);

// Floor mats: two overlapping mats with a ribbed surface and a retaining clip.
emit(
  'accessories/floor-mats.svg',
  accFrame(
    'FLOOR MATS',
    `
<path class="k2" d="M150 92L300 78L332 214L188 236Z"/>
${[0, 1, 2, 3, 4, 5].map(i => `<path d="M${168 + i * 8} ${112 + i * 20}L${314 + i * 4} ${98 + i * 20}" stroke="#52525b" stroke-width="3" stroke-linecap="round"/>`).join('')}
<path class="k" d="M62 118L212 104L244 240L100 262Z"/>
${[0, 1, 2, 3, 4, 5].map(i => `<path d="M${80 + i * 6} ${138 + i * 20}L${226 + i * 4} ${124 + i * 20}" stroke="#52525b" stroke-width="3" stroke-linecap="round"/>`).join('')}
<circle class="hl" cx="98" cy="128" r="7"/><circle cx="98" cy="128" r="3" fill="#7c2d12"/>
`,
  ),
);

// Mud guards: a front and a rear flap, curved, with bolt holes.
emit(
  'accessories/mud-guards.svg',
  accFrame(
    'MUD GUARDS',
    `
<path class="k" d="M96 70Q70 130 92 210Q120 232 170 226Q160 160 196 100Q150 60 96 70Z"/>
<path class="k3" d="M108 88Q88 140 104 200Q124 214 156 208Q152 160 176 108Q140 78 108 88Z"/>
${[
  [112, 100],
  [104, 150],
  [116, 198],
  [156, 206],
]
  .map(([x, y]) => `<circle cx="${x}" cy="${y}" r="4" fill="#d4d4d8"/>`)
  .join('')}
<path class="k" d="M236 100Q224 154 240 218Q264 238 316 234Q306 174 322 114Q290 78 236 100Z"/>
<path class="k3" d="M250 116Q240 154 250 208Q270 222 300 216Q294 174 306 128Q282 104 250 116Z"/>
${[
  [252, 124],
  [246, 170],
  [258, 212],
  [298, 216],
]
  .map(([x, y]) => `<circle cx="${x}" cy="${y}" r="4" fill="#d4d4d8"/>`)
  .join('')}
`,
  ),
);

// Tow bar: a hitch bracket with a tow ball.
emit(
  'accessories/tow-bar.svg',
  accFrame(
    'TOW BAR',
    `
<rect class="k2" x="60" y="128" width="280" height="22" rx="6"/>
<rect class="k" x="180" y="150" width="40" height="46" rx="4"/>
<path class="k" d="M170 196h60l10 22h-80z"/>
<rect class="al" x="196" y="86" width="8" height="42"/>
<circle class="al" cx="200" cy="74" r="18"/><circle cx="192" cy="66" r="6" fill="#fff" opacity=".6"/>
${[92, 132, 268, 308].map(x => `<circle cx="${x}" cy="139" r="5" fill="#d4d4d8"/>`).join('')}
<rect class="al2" x="120" y="200" width="160" height="6" rx="3"/>
`,
  ),
);

// Fallback for an accessory the set has no drawing for: a parts carton.
emit(
  'accessories/accessory.svg',
  accFrame(
    'PART',
    `
<path class="k3" d="M110 120L200 80L290 120L200 160Z"/>
<path class="k" d="M110 120L200 160V240L110 200Z"/>
<path class="k2" d="M290 120L200 160V240L290 200Z"/>
<path d="M155 100L245 140" stroke="#a1a1aa" stroke-width="12"/>
<path d="M200 160V240" stroke="#f59e0b" stroke-width="4"/>
`,
  ),
);

/* ------------------------------------------------------------------ write */

let total = 0;
for (const [rel, svg] of files) {
  const file = path.join(out, rel);
  fs.mkdirSync(path.dirname(file), { recursive: true });
  fs.writeFileSync(file, svg);
  total += Buffer.byteLength(svg);
}
console.log(`${files.size} files, ${(total / 1024).toFixed(1)} KB`);
