// JS-interop surface for the `<shift-survey>` custom element. Blazor calls these
// via IJSRuntime; the element itself is registered by `shift-survey.js`, which
// must be loaded before these helpers are used.

// Module-level flag set by `installPreviewSubmitHandler`'s onSubmit handler and
// consumed by the next `setSchema` call. See the comment on
// `installPreviewSubmitHandler` for the full rationale.
const remountAfterNextSchema = new WeakSet();

// In Create mode, the <shift-survey> element renders for the first time only
// when the author adds their first screen — and the very first Blazor interop
// call can race the customElements.define('shift-survey', ...) registration in
// shift-survey.js. If we set `.schema` / `.onSubmit` on the raw HTMLElement
// before upgrade, those values become instance properties that the class's
// setters never see once upgraded. Awaiting whenDefined here guarantees we
// always assign through the upgraded class. After Save the form re-renders
// the element after registration has already completed, which is why the
// workaround "save once" makes the live preview start working.
async function ensureDefined() {
  if (typeof customElements === 'undefined') return;
  try {
    await customElements.whenDefined('shift-survey');
  } catch {
    // Element not registered in this environment — fall through; the assignment
    // is still attempted so a future registration via direct DOM use can catch it.
  }
}

/** Assign a schema (parsed JSON) to a `<shift-survey>` element. Pass the raw
 *  JSON string — we parse here so JSON serialization settings can be owned by
 *  the caller (matches `SurveySchemaSerializer` on the .NET side).
 *
 *  If the element's previous render auto-submitted (preview mode), we'll
 *  remount it by nulling schema first so SurveyRenderer's internal `done`
 *  state resets — otherwise the next push wouldn't leave the thank-you
 *  state even if the author added a question back. */
export async function setSchema(element, schemaJson) {
  if (!element) return;
  await ensureDefined();
  if (!schemaJson) {
    element.schema = null;
    return;
  }
  let parsed;
  try {
    parsed = JSON.parse(schemaJson);
  } catch (e) {
    // Parse errors are common mid-edit as the author types — swallow silently.
    // The Blazor form already shows the JSON parse error inline.
    return;
  }

  // Detect a "stuck in done state" renderer two ways:
  //   1. The explicit flag set by our onSubmit handler (fast path).
  //   2. The `--done` class on `.survey-root` (reliable fallback — the flag
  //      path has a race: onSubmit is async, so if the user's next action
  //      lands before onSubmit's awaits complete, the flag isn't set yet,
  //      but by the time we get here the next render may already have
  //      committed the done state).
  const flagged = remountAfterNextSchema.has(element);
  const rootEl = element.shadowRoot?.querySelector('.survey-root');
  const renderedDone = !!rootEl && rootEl.classList.contains('survey-root--done');
  if (flagged || renderedDone) {
    remountAfterNextSchema.delete(element);
    // Null first → React unmounts SurveyRenderer (done/currentScreenId state
    // discarded). Second tick → set the new schema so it re-mounts fresh.
    // 80ms gives React's scheduler time to commit the unmount before we
    // re-set; setTimeout(0) has been empirically insufficient.
    element.schema = null;
    setTimeout(() => {
      // If yet another push raced in between, don't overwrite it.
      if (element.schema === null) element.schema = parsed;
    }, 80);
  } else {
    element.schema = parsed;
  }
}

export async function setLocale(element, locale) {
  if (!element) return;
  await ensureDefined();
  if (locale) element.setAttribute('locale', locale);
  else element.removeAttribute('locale');
}

/** Jump the preview to a screen (editor-selection sync). If the preview is
 *  sitting in its done state (auto-submitted thank-you), the renderer's done
 *  flag would swallow the jump — remount fresh first with the same schema
 *  (same 80ms dance as setSchema) and apply the jump after the remount. */
export async function setActiveScreen(element, screenId) {
  if (!element) return;
  await ensureDefined();

  const rootEl = element.shadowRoot?.querySelector('.survey-root');
  const renderedDone = !!rootEl && rootEl.classList.contains('survey-root--done');
  if (renderedDone && element.schema) {
    const schema = element.schema;
    remountAfterNextSchema.delete(element);
    element.schema = null;
    setTimeout(() => {
      if (element.schema === null) element.schema = schema;
      element.activeScreenId = screenId ?? null;
    }, 80);
    return;
  }

  element.activeScreenId = screenId ?? null;
}

/** Attach a submit handler for preview mode.
 *
 *  Preview should never lock into `done=true`. That happens when the renderer
 *  auto-submits a zero-question terminal screen — if the author then adds a
 *  question back, the preview stays stuck in thank-you state because `done`
 *  lives in React useState inside the renderer.
 *
 *  Strategy: on submit, record a "remount on next schema push" flag.
 *  `setSchema` consumes the flag and nulls schema first so SurveyRenderer
 *  unmounts, restoring fresh state on the next tick.
 *
 *  Why not remount inside onSubmit itself? Because if every screen is
 *  zero-question, the remount would trigger another auto-submit, which would
 *  schedule another remount, which would ... — a tight loop. By deferring
 *  remount to the next Blazor-initiated setSchema, we only remount on
 *  authoring intent, never on the renderer's own lifecycle. */
/** Test-run dialog: notify .NET when the embedded survey completes. In API mode
 *  the element only fires `survey:completed` after the public submit endpoint
 *  accepted the response, so this is a reliable "answer recorded" signal. */
export async function installCompletionHandler(element, dotNetRef) {
  if (!element || !dotNetRef) return;
  await ensureDefined();
  element.addEventListener('survey:completed', () => {
    dotNetRef.invokeMethodAsync('OnEmbeddedSurveyCompleted').catch(() => {
      /* dialog already closed / .NET ref disposed — nothing to notify */
    });
  });
}

export async function installPreviewSubmitHandler(element) {
  if (!element) return;
  await ensureDefined();
  element.onSubmit = async (submission) => {
    // eslint-disable-next-line no-console
    console.log('[preview] submission (not sent):', submission);
    remountAfterNextSchema.add(element);
  };
}
