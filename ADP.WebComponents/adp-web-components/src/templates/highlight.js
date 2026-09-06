/**
 * adp-web-components — the showcase syntax highlighter.
 *
 * A highlighter in about fifty lines of rules, because the alternative is a CDN
 * script tag and §3 of the design language exists to keep those off these pages.
 *
 * One pass per snippet: a single alternation walks the string, so a token
 * already emitted is never re-scanned. Sequential .replace() calls would match
 * `class="tok-key"` inside their own output. Every pattern uses NON-CAPTURING
 * groups only — the group index IS the rule index, which is how a match is
 * traced back to the rule that produced it.
 *
 * Five token classes exist and no more. They are styled in
 * `templates/harness.src.css`, which is COMPILED into the gitignored
 * harness.css, so a sixth class here would render as plain body text and a
 * renamed one would silently lose its colour:
 *
 *   tok-key      magenta   keywords, selectors, at-rules, literals
 *   tok-str      green     strings and literal values
 *   tok-attr     blue      names you address — attributes, JSON keys, CSS
 *                          properties, shadow parts
 *   tok-comment  grey      comments
 *   tok-punct    grey      structural punctuation
 *
 * Kept deliberately dumb. None of these grammars is a parser: they carry no
 * state, so they cannot know whether they are inside a rule block or a selector
 * list, and each is tuned for the shape the showcase actually prints —
 * hand-written, one declaration per line. Minified input degrades; it does not
 * break.
 *
 * Usage:
 *
 *   import { highlight } from '../highlight.js';
 *
 *   element.innerHTML = highlight(source, 'css');
 */

export const GRAMMARS = {
  HTML: [
    { token: 'comment', pattern: '<!--[\\s\\S]*?-->' },
    { token: 'str', pattern: '"[^"]*"' },
    { token: 'key', pattern: '</?[a-zA-Z][\\w-]*' },
    { token: 'attr', pattern: '[a-zA-Z-]+(?==)' },
    { token: 'punct', pattern: '/?>' },
  ],
  JavaScript: [
    { token: 'comment', pattern: '//[^\\n]*' },
    { token: 'str', pattern: "'[^']*'" },
    { token: 'key', pattern: '\\b(?:import|from|await|const|let|new|return|try|catch|document)\\b' },
    { token: 'attr', pattern: '\\b[a-zA-Z_$][\\w$]*(?=\\s*:)' },
    { token: 'punct', pattern: '[{}();,]' },
  ],
  Shell: [
    { token: 'comment', pattern: '#[^\\n]*' },
    { token: 'key', pattern: '^\\S+' },
  ],

  /*
   * JSON. A key is a string sitting in front of a colon, and that is the one
   * distinction worth drawing: a structure file is read by its keys, and
   * painting them the same green as every value turns it into a wall.
   */
  JSON: [
    { token: 'attr', pattern: '"(?:\\\\.|[^"\\\\])*"(?=\\s*:)' },
    { token: 'str', pattern: '"(?:\\\\.|[^"\\\\])*"' },
    { token: 'key', pattern: '\\b(?:true|false|null)\\b' },
    { token: 'key', pattern: '-?\\b\\d+(?:\\.\\d+)?(?:[eE][+-]?\\d+)?\\b' },
    { token: 'punct', pattern: '[{}\\[\\],:]' },
  ],

  /*
   * CSS, where the ordering IS the grammar.
   *
   *   `::part(name)` is first among the selector rules because it is the thing a
   *   theme file is FOR. It takes the blue "name you address" colour so it reads
   *   apart from the magenta selector it hangs off.
   *
   *   A property is told from a pseudo-class with no state at all, by what
   *   follows the colon: `color: red;` reaches a `;` or a `}` with no brace in
   *   between, while `a:hover {` hits the `{` first. That rule is anchored to
   *   the start of a line (the scanner runs with `m`) and swallows the colon, so
   *   `color:red` cannot leave a bare `:red` behind for the pseudo-class rule to
   *   claim. The leading indentation rides along inside the span, which is
   *   invisible — colour only reaches glyphs.
   *
   *   The hex rule outranks the id-selector rule because `#fff` is a colour far
   *   more often than it is an id. `#abc` is genuinely ambiguous and goes to the
   *   colour; both are coloured, just not the same.
   */
  CSS: [
    { token: 'comment', pattern: '/\\*[\\s\\S]*?\\*/' },
    { token: 'str', pattern: '"[^"]*"|\'[^\']*\'' },
    { token: 'key', pattern: '@[a-zA-Z-]+' },
    { token: 'attr', pattern: '::part\\([^()]*\\)' },
    { token: 'key', pattern: '::?[a-zA-Z][a-zA-Z-]*(?:\\((?:[^()]|\\([^()]*\\))*\\))?' },
    { token: 'key', pattern: '!\\s*important\\b' },
    { token: 'attr', pattern: '^[ \\t]*[a-zA-Z-][a-zA-Z0-9-]*\\s*:(?=[^;{}]*[;}])' },
    { token: 'key', pattern: '^[ \\t]*[a-zA-Z][a-zA-Z0-9-]*(?=[^;{}]*\\{)' },
    { token: 'str', pattern: '#[0-9a-fA-F]{3,8}\\b' },
    { token: 'key', pattern: '[.#][a-zA-Z_-][a-zA-Z0-9_-]*' },
    { token: 'key', pattern: '\\[[^\\]]*\\]' },
    { token: 'str', pattern: '\\b\\d+(?:\\.\\d+)?[a-zA-Z%]*' },
    { token: 'punct', pattern: '[{}();,:]' },
  ],
};

/**
 * The spellings a caller is likely to have to hand — a file extension, a `lang`
 * attribute, a lower-cased label. The canonical keys above stay display-cased
 * because they are also what the landing page prints above a snippet.
 */
const ALIASES = Object.assign(Object.create(null), {
  html: 'HTML',
  javascript: 'JavaScript',
  js: 'JavaScript',
  shell: 'Shell',
  sh: 'Shell',
  bash: 'Shell',
  json: 'JSON',
  css: 'CSS',
});

/*
 * One compiled scanner per grammar. Keyed by the rules array rather than by name
 * so an alias cannot compile a second copy, and weak so a caller passing its own
 * grammar array does not pin it here forever.
 */
const scanners = new WeakMap();

export function escapeHtml(text) {
  return String(text).replace(/[&<>]/g, character => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' })[character]);
}

/**
 * Marks a snippet up for the `.code` block styling.
 *
 * @param {string} code the snippet, as written
 * @param {string} lang a grammar name or alias. Anything unrecognised comes back
 *                      escaped and unmarked, which is the right answer for a
 *                      language nothing here understands.
 * @returns {string} HTML — escaped text with `<span class="tok-…">` around every
 *                   recognised token.
 */
export function highlight(code, lang) {
  const rules = grammarFor(lang);

  if (!rules) return escapeHtml(code);

  const scanner = scannerFor(rules);

  let output = '';
  let index = 0;
  let match;

  // The scanner is shared and carries lastIndex between calls; nothing here is
  // re-entrant, so rewinding once at the top is enough.
  scanner.lastIndex = 0;

  while ((match = scanner.exec(code)) !== null) {
    // A zero-length match would loop forever; nothing here can produce one, but
    // the guard costs a line and the failure is a hung tab.
    if (match[0] === '') break;

    const rule = rules[match.slice(1).findIndex(group => group !== undefined)];

    output += escapeHtml(code.slice(index, match.index));
    output += '<span class="tok-' + rule.token + '">' + escapeHtml(match[0]) + '</span>';
    index = match.index + match[0].length;
  }

  return output + escapeHtml(code.slice(index));
}

/** Own properties only — `GRAMMARS['constructor']` is not a grammar. */
function grammarFor(lang) {
  const name = String(lang ?? '');

  if (Object.prototype.hasOwnProperty.call(GRAMMARS, name)) return GRAMMARS[name];

  const canonical = ALIASES[name.toLowerCase()];

  return canonical ? GRAMMARS[canonical] : null;
}

function scannerFor(rules) {
  let scanner = scanners.get(rules);

  if (!scanner) {
    scanner = new RegExp(rules.map(rule => '(' + rule.pattern + ')').join('|'), 'gm');
    scanners.set(rules, scanner);
  }

  return scanner;
}
