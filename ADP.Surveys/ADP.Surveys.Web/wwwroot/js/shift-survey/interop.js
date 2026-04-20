// JS-interop surface for the `<shift-survey>` custom element. Blazor calls these
// via IJSRuntime; the element itself is registered by `shift-survey.js`, which
// must be loaded before these helpers are used.

/** Assign a schema (parsed JSON) to a `<shift-survey>` element. Pass the raw
 *  JSON string — we parse here so JSON serialization settings can be owned by
 *  the caller (matches `SurveySchemaSerializer` on the .NET side). */
export function setSchema(element, schemaJson) {
  if (!element) return;
  if (!schemaJson) {
    element.schema = null;
    return;
  }
  try {
    element.schema = JSON.parse(schemaJson);
  } catch (e) {
    // Parse errors are common mid-edit as the author types — swallow silently.
    // The Blazor form already shows the JSON parse error inline.
  }
}

export function setLocale(element, locale) {
  if (!element) return;
  if (locale) element.setAttribute('locale', locale);
  else element.removeAttribute('locale');
}

/** Attach a submit handler that just logs — preview mode shouldn't POST to the
 *  API. Blazor would rarely need to override this. */
export function installPreviewSubmitHandler(element) {
  if (!element) return;
  element.onSubmit = (submission) => {
    // eslint-disable-next-line no-console
    console.log('[preview] submission (not sent):', submission);
    return Promise.resolve();
  };
}
