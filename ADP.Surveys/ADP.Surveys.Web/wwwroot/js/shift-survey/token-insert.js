// Caret-aware "insert a personalization token" support for LocalizedStringField.
//
// Why this goes through the DOM rather than through C#: ShiftBlazor's
// LocalizedInput reads its `Value` parameter only in OnInitialized and keeps its
// own per-locale dictionary afterwards. Setting a new LocalizedString from Blazor
// would update our model and leave the visible textbox stale until something forced
// a remount — and a remount loses the caret, which is the one thing an author
// inserting mid-sentence cares about. Driving the real <input>/<textarea> and
// dispatching its own events instead means the value flows back through the
// component's own change handler, so both sides stay in step.

// Per-container memory of which locale box the author last had the caret in.
const lastFocused = new WeakMap();

/** Start remembering the focused field inside `container`. Idempotent — Blazor
 *  calls this on every render of the field. */
export function trackTokenTargets(container) {
  if (!container || container.dataset.tokenTracked === 'true') return;
  container.dataset.tokenTracked = 'true';
  container.addEventListener('focusin', (event) => {
    const el = event.target;
    if (el instanceof HTMLInputElement || el instanceof HTMLTextAreaElement) {
      lastFocused.set(container, el);
    }
  });
}

function editableFields(container) {
  return Array.from(container.querySelectorAll('textarea, input')).filter(
    (el) =>
      !el.disabled &&
      !el.readOnly &&
      (el instanceof HTMLTextAreaElement || el.type === 'text' || el.type === ''),
  );
}

/**
 * Insert `text` at the caret of the last-focused field in `container`.
 *
 * With no caret to work from — the author clicked the menu before ever putting the
 * cursor in a box — we append to the first field rather than doing nothing, so the
 * click always has a visible effect. Returns false only when there is no field to
 * write into at all.
 */
export function insertToken(container, text) {
  if (!container || !text) return false;

  let el = lastFocused.get(container);
  if (!el || !container.contains(el) || el.disabled || el.readOnly) {
    el = editableFields(container)[0];
  }
  if (!el) return false;

  const value = el.value ?? '';
  const hasCaret = typeof el.selectionStart === 'number' && typeof el.selectionEnd === 'number';
  const start = hasCaret ? el.selectionStart : value.length;
  const end = hasCaret ? el.selectionEnd : value.length;

  el.value = value.slice(0, start) + text + value.slice(end);

  el.focus();
  const caret = start + text.length;
  try {
    el.setSelectionRange(caret, caret);
  } catch {
    // Some input types refuse selection APIs — the text still landed.
  }

  // `input` covers MudTextField with Immediate; `change` covers the default
  // bind-on-blur. Firing both means the value reaches Blazor either way, and the
  // component's own handler is what updates its internal per-locale state.
  el.dispatchEvent(new Event('input', { bubbles: true }));
  el.dispatchEvent(new Event('change', { bubbles: true }));
  return true;
}
