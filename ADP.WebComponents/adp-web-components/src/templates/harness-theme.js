/**
 * Shared theme state for the showcase pages.
 *
 * Loaded as a plain blocking script in <head> — deliberately not a module. A
 * module is deferred, which would let the page paint in the wrong theme before
 * the stored choice is applied.
 *
 * The names map onto the daisyUI themes declared in harness.src.css; 'system'
 * removes the attribute entirely so `prefersdark` takes over.
 */
(function () {
  const KEY = 'adp-harness-theme';
  const THEMES = { light: 'harness', dark: 'harness-dark' };

  function read() {
    // Private windows and blocked site data both throw rather than return null.
    try {
      return localStorage.getItem(KEY);
    } catch {
      return null;
    }
  }

  function apply(theme) {
    const resolved = theme || 'system';

    if (resolved === 'system') delete document.documentElement.dataset.theme;
    else document.documentElement.dataset.theme = THEMES[resolved] ?? resolved;

    try {
      localStorage.setItem(KEY, resolved);
    } catch {
      // Theme still applies for this page; it just will not survive navigation.
    }
  }

  function current() {
    return read() || 'system';
  }

  window.harnessTheme = { apply, current };

  apply(current());
})();
