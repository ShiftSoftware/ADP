/**
 * Getting an overlay out of whatever container the host page put this component in.
 *
 * These components are published to unknown hosts, so an overlay can end up nested in an animated
 * modal, a slide-up footer, an accordion mid-transition or a virtualised list. Any ancestor with a
 * `transform`, `filter`, `backdrop-filter`, `perspective`, `contain` or a `will-change` naming one
 * of them becomes the containing block for `position: fixed`, and the overlay is laid out and
 * clipped inside that ancestor instead of against the viewport.
 *
 * The browser's top layer is the only way out that does not move the element: it paints above every
 * stacking context, is clipped by no ancestor, and ignores caging ancestors entirely — while the
 * element stays in its shadow root, so scoped styles, refs and measurements keep working.
 *
 * The two flavours are not interchangeable:
 *
 *  - `modal` (dialog.showModal) inerts the rest of the page. Right for a claim form, fatal for a
 *    hover card — it would make every other card unhoverable.
 *  - `popover` (popover="manual" + showPopover) is non-modal, so the page stays interactive.
 *
 * Promotion is not free: the top layer cannot be overridden by a host's z-index, so a host can no
 * longer place its own toast or nav above us. A modal should outrank those anyway, so it always
 * promotes. A hover card should not, so it promotes only when staying put would actually clip it.
 */

const cagingWillChange = /\b(transform|perspective|filter|backdrop-filter|contain)\b/;
const cagingContain = /\b(paint|layout|strict|content)\b/;

const isCagingStyle = (style: CSSStyleDeclaration) => {
  if (!style) return false;

  if (style.transform && style.transform !== 'none') return true;
  if (style.perspective && style.perspective !== 'none') return true;
  if (style.filter && style.filter !== 'none') return true;

  const backdropFilter = style.backdropFilter || (style as unknown as Record<string, string>).webkitBackdropFilter;
  if (backdropFilter && backdropFilter !== 'none') return true;

  if (style.contain && cagingContain.test(style.contain)) return true;
  if (style.willChange && cagingWillChange.test(style.willChange)) return true;

  return false;
};

/**
 * Walks up from the element, stepping out of every shadow root on the way, looking for an ancestor
 * that would become the containing block for a `position: fixed` child.
 */
export const isCaged = (element?: Element | null): boolean => {
  if (!element || typeof getComputedStyle !== 'function') return false;

  let current: Element | null = element.parentElement;
  let root = element.getRootNode?.();

  while (true) {
    if (!current) {
      // Out of parents: hop through the shadow boundary and keep going up the host's tree.
      if (!(root instanceof ShadowRoot)) return false;
      current = root.host;
      root = current?.getRootNode?.();
      if (!current) return false;
    }

    if (isCagingStyle(getComputedStyle(current))) return true;

    current = current.parentElement;
  }
};

export const supportsModalDialog = typeof HTMLDialogElement !== 'undefined' && typeof HTMLDialogElement.prototype?.showModal === 'function';

export const supportsPopover = typeof HTMLElement !== 'undefined' && typeof HTMLElement.prototype?.showPopover === 'function';

/**
 * Shows a modal overlay in the top layer, falling back to the `open` attribute where <dialog> is
 * unimplemented — WebKit only shipped it in 15.4, which on iOS means Safari, every WKWebView and
 * every installed PWA on the device. There the overlay renders in place exactly as it did before
 * the top layer existed: correct, just clippable.
 *
 * Returns whether the top layer was actually used, so callers can supply what it would have given
 * them for free (page inerting, Escape) on the path where it did not.
 */
export const openModalOverlay = (dialog?: HTMLDialogElement | null): boolean => {
  if (!dialog || dialog.open) return supportsModalDialog;

  if (supportsModalDialog) {
    dialog.showModal();
    return true;
  }

  dialog.setAttribute('open', '');
  return false;
};

export const closeModalOverlay = (dialog?: HTMLDialogElement | null) => {
  if (!dialog) return;

  if (supportsModalDialog && dialog.open) dialog.close();
  else dialog.removeAttribute('open');
};

/**
 * Promotes a non-modal overlay, but only when an ancestor would otherwise cage it — so in the
 * ordinary case the host keeps the ability to stack its own UI above ours.
 *
 * The `popover` attribute is added here rather than in markup on purpose: it carries
 * `[popover]:not(:popover-open) { display: none }`, which would hide an overlay that never gets
 * promoted, and a set of UA box styles that only need overriding while it is applied.
 */
export const promoteOverlayIfCaged = (element?: HTMLElement | null): boolean => {
  if (!element || !supportsPopover || element.hasAttribute('popover')) return false;
  if (!isCaged(element)) return false;

  element.setAttribute('popover', 'manual');

  try {
    element.showPopover();
    return true;
  } catch {
    // Not connected, or already shown by something else — leave it laid out normally.
    element.removeAttribute('popover');
    return false;
  }
};

export const demoteOverlay = (element?: HTMLElement | null) => {
  if (!element || !element.hasAttribute('popover')) return;

  try {
    element.hidePopover();
  } catch {
    /* already hidden */
  }

  element.removeAttribute('popover');
};

/**
 * Escape handling for the fallback path only. A modal dialog reports Escape as a `cancel` event;
 * without `showModal` there is no such event, so the key has to be watched directly.
 *
 * Capture, deliberately: an overlay that stacks something of its own on top (the claim form's image
 * viewer) listens on `document`, and a bubble-phase listener on `window` would run *after* that one
 * had already dismissed it and cleared the state this handler checks — closing both at once.
 * Capture at `window` runs first, so the inner overlay still owns the key.
 */
export const bindEscapeFallback = (handler: (event: KeyboardEvent) => void) => {
  const listener = (event: KeyboardEvent) => {
    if (event.key === 'Escape') handler(event);
  };

  window.addEventListener('keydown', listener, true);

  return () => window.removeEventListener('keydown', listener, true);
};
