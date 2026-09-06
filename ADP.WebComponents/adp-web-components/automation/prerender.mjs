/**
 * Resolves the English copy into the built HTML, and writes the search-engine
 * directives that go with it.
 *
 * ── Why ───────────────────────────────────────────────────────────────────────
 * Every user-visible string on the landing page is injected by Alpine through
 * `x-text="t('key')"`, so the file a client actually receives contains three
 * words of body text: "SHFT", "your day" and an arrow. That is fine for a browser
 * and useless for everything else — a link-preview scraper (Slack, Teams,
 * WhatsApp) does not execute JavaScript, nor do most AI answer engines, nor a
 * reader whose corporate proxy strips the vendored Alpine.
 *
 * This resolves the one binding shape that is safe to resolve — a bare
 * `t('key')` on an EMPTY element — and leaves the binding in place, so Alpine
 * writes the same value back on init. There is still exactly one source of truth.
 *
 * ── The traps, all of them load-bearing ───────────────────────────────────────
 *
 *   • `<template>` must be MASKED, not merely detected. Alpine's `x-for` appends
 *     its clones as siblings of the template and starts from an empty lookup, so
 *     anything pre-rendered inside one becomes a permanent duplicate rather than
 *     a head start. Matches are tested against a byte mask.
 *   • `x-html` is markup, not text — both instances are SVG path geometry. They
 *     live inside templates, so the mask already excludes them. Do not "improve"
 *     this later by handling them.
 *   • Only the empty-element form is safe. The pattern requires `…></tag>`
 *     adjacency; `<p x-text="t('k')">fallback</p>` would end up with two copies.
 *   • Runtime expressions stay untouched: `t('theme.' + theme)`, `snippet`,
 *     `version` and friends are state, not copy.
 *   • It fails loudly. A whitespace reflow that puts a newline between `>` and
 *     `</p>` would silently switch the whole thing off, and nobody would notice
 *     for months — so a page that resolves fewer strings than expected is a
 *     build error, not a warning.
 */

/** A bare `t('key')` on an element with no content of its own. */
const TEXT_BINDING = /(<([a-z0-9-]+)\b[^>]*\bx-text="t\('([^']+)'\)"[^>]*>)(<\/\2>)/gi;

/** The same shape as an attribute binding — emit the plain attribute alongside. */
const ATTR_BINDING = /:(aria-label|placeholder|title)="t\('([^']+)'\)"/g;

const escapeHtml = value => String(value).replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
const escapeAttr = value => escapeHtml(value).replace(/"/g, '&quot;');

/**
 * A string the same length as the input, with `\0` wherever a `<template>`
 * element spans. Same length, so a match offset can be tested directly.
 */
function maskTemplates(html) {
  const mask = new Array(html.length).fill(' ');
  const opens = /<template\b/gi;

  let match;

  while ((match = opens.exec(html)) !== null) {
    const end = html.indexOf('</template>', match.index);
    const stop = end === -1 ? html.length : end + '</template>'.length;

    for (let index = match.index; index < stop; index += 1) mask[index] = '\0';
  }

  return mask.join('');
}

/**
 * Fills the resolvable bindings with `locales.t(language, key)`.
 * Returns the new HTML and how many text bindings were filled.
 */
export function prerender(html, locales, language = 'en') {
  const mask = maskTemplates(html);

  let filled = 0;

  const output = html
    .replace(TEXT_BINDING, (whole, open, tag, key, close, offset) => {
      if (mask[offset] === '\0') return whole;

      const value = locales.t(language, key);

      // `list()` keys resolve to arrays; those belong to x-for, not here.
      if (typeof value !== 'string') return whole;

      filled += 1;

      return open + escapeHtml(value) + close;
    })
    .replace(ATTR_BINDING, (whole, name, key, offset) => (mask[offset] === '\0' ? whole : `${whole} ${name}="${escapeAttr(locales.t(language, key))}"`));

  return { output, filled };
}

/**
 * The repeated lists — features, the stack, the quickstart steps, next steps —
 * are ~19 name/body pairs of real prose that `prerender` cannot reach, because
 * they live inside `x-for` templates. This emits them once into a `<noscript>`,
 * built from the same table, so the content a crawler sees is identical to the
 * content Alpine renders. Inert the moment JavaScript runs, so there is no
 * duplication and no flash.
 */
export function noscriptFallback(html, locales, language = 'en') {
  const lists = ['overview.elementItems', 'overview.youItems', 'quickstart.steps', 'features.items', 'versions.items', 'stack.items', 'support.items'];

  const blocks = lists.flatMap(key => {
    const items = locales.t(language, key);

    // Some lists are plain strings (the overview split), others are name/body pairs.
    return Array.isArray(items)
      ? items.map(item => (typeof item === 'string' ? `<p>${escapeHtml(item)}</p>` : `<h3>${escapeHtml(item.name)}</h3><p>${escapeHtml(item.body)}</p>`))
      : [];
  });

  if (!blocks.length || !html.includes('</main>')) return html;

  return html.replace('</main>', `  <noscript>\n      ${blocks.join('\n      ')}\n    </noscript>\n      </main>`);
}

/**
 * Robots, canonical and JSON-LD, injected after the viewport meta.
 *
 * Demo pages are `noindex` whatever the site-wide decision is: they are thin,
 * near-duplicate harnesses full of mock data, and one of them outranking the
 * landing page is a worse outcome than not being indexed at all.
 *
 * A page that declares its own robots meta (404.html does) is left alone.
 */
export function seoTags(html, { isLanding, noindex, siteUrl, packageName, version, description }) {
  if (/<meta[^>]+name=["']robots["']/i.test(html)) return html;

  const indexable = isLanding && !noindex;
  const tags = [`<meta name="robots" content="${indexable ? 'index, follow' : 'noindex, follow'}" />`];

  if (indexable && siteUrl) {
    tags.push(`<link rel="canonical" href="${siteUrl}/" />`, `<meta property="og:url" content="${siteUrl}/" />`, `<meta property="og:site_name" content="ADP web components" />`);

    const graph = {
      '@context': 'https://schema.org',
      '@type': 'SoftwareApplication',
      'name': packageName,
      'applicationCategory': 'DeveloperApplication',
      'operatingSystem': 'Any (web browser)',
      'softwareVersion': version,
      'description': description,
      'url': `${siteUrl}/`,
      'downloadUrl': `https://www.npmjs.com/package/${packageName}`,
      'programmingLanguage': 'JavaScript',
      'isAccessibleForFree': true,
      'publisher': { '@type': 'Organization', 'name': 'ShiftSoftware' },
    };

    tags.push(`<script type="application/ld+json">${JSON.stringify(graph)}</script>`);
  }

  return html.replace(/(<meta name="viewport"[^>]*\/?>)/i, `$1\n\n    ${tags.join('\n    ')}`);
}

/**
 * robots.txt and sitemap.xml.
 *
 * `pages` is the post-prune list, so a page that did not opt in with
 * `<meta name="adp-publish" content="true" />` cannot reach the sitemap. The
 * sitemap and the robots meta read the same rule, so a URL can never be both
 * listed as a destination and told not to be indexed.
 *
 * Never `Disallow: /templates/` — harness.css, alpine.js and site-locales.js all
 * live there, so blocking it makes a rendering crawler see the same blank page a
 * no-JS client does.
 */
export function robotsAndSitemap({ noindex, siteUrl, landingPages }) {
  const robots = noindex ? 'User-agent: *\nDisallow: /\n' : `User-agent: *\nAllow: /\n${siteUrl ? `\nSitemap: ${siteUrl}/sitemap.xml\n` : ''}`;

  if (noindex || !siteUrl || !landingPages.length) return { robots, sitemap: null };

  const today = new Date().toISOString().slice(0, 10);
  const urls = landingPages.map(location => `  <url>\n    <loc>${siteUrl}/${location}</loc>\n    <lastmod>${today}</lastmod>\n  </url>\n`).join('');

  return { robots, sitemap: `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}</urlset>\n` };
}

/**
 * `_headers`, which Cloudflare Workers parses out of the asset directory and
 * applies to static asset responses. Written by the build rather than committed,
 * because the output directory is wiped on every run.
 *
 * Deliberately NOT setting Cache-Control: harness.css and vendor/alpine.js are
 * not content-hashed, so a long max-age would pin a stale stylesheet in every
 * visitor`s browser across a release. Cloudflare`s revalidating default is the
 * right behaviour until the filenames carry a hash.
 *
 * Deliberately NOT setting a Content-Security-Policy either. The landing page
 * carries inline scripts (the theme and direction have to apply before first
 * paint), so any policy would need `unsafe-inline` and buy almost nothing — and
 * once demo pages ship, connect-src has to allow an operator base URL this build
 * does not know. A wrong CSP breaks the demos; an `unsafe-inline` one is
 * theatre. Revisit when the demos are published and the origins are known.
 */
export function headersFile({ noindex }) {
  const rules = ['/*', '  X-Content-Type-Options: nosniff', '  Referrer-Policy: strict-origin-when-cross-origin'];

  // Belt and braces with the robots meta: a header also covers non-HTML assets.
  if (noindex) rules.push('  X-Robots-Tag: noindex, nofollow');

  return rules.join('\n') + '\n';
}
