/**
 * The booking question's calendar comes from adp-web-components, whose global script adds a
 * Google Fonts stylesheet to the page when it first loads. The survey sets its own font on the
 * calendar, so it opts out. This must be the entry's first import: imports run in order, before
 * any module body, and the library reads the flag as it loads.
 */
if (typeof window !== 'undefined') (window as { adpWebComponentsFonts?: boolean }).adpWebComponentsFonts ??= false;
