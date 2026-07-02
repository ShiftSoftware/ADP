import { createElement } from 'react';
import { createRoot, type Root } from 'react-dom/client';
import { SurveyRenderer } from '@shiftsoftware/survey-renderer';
// Relative path on purpose — the renderer's `exports` map is strict and doesn't
// whitelist `?inline`; reaching through the workspace path is the most robust
// way to inline the stylesheet at build time.
import rendererStyles from '../../survey-renderer/src/styles.css?inline';
import {
  SurveyClient,
  type Survey,
  type SurveySubmission,
} from '@shiftsoftware/survey-sdk';

/**
 * `<shift-survey>` — custom-element wrapper around `<SurveyRenderer>`.
 *
 * Two usage modes, pick whichever fits the host:
 *
 *   1. **API mode** (attribute-driven): the element fetches schema + submits
 *      responses against the Surveys API on its own.
 *      ```html
 *      <shift-survey
 *        instance-id="abc-123-guid"
 *        api-base="https://api.example.com/api/Surveys"
 *        locale="ar"></shift-survey>
 *      ```
 *
 *   2. **Schema mode** (property-driven): the host provides the schema + onSubmit
 *      directly as DOM properties. Used by the Blazor builder's live-preview
 *      (Phase 4) — the host pushes draft schemas via JS interop as the author
 *      types.
 *      ```ts
 *      const el = document.querySelector('shift-survey');
 *      el.schema = draftSchema;
 *      el.onSubmit = (submission) => { ... };
 *      ```
 *
 * Lifecycle events bubble up as CustomEvents on the element (`survey:loaded`,
 * `survey:screen-changed`, `survey:completed`, `survey:error`) — same vocabulary
 * as the postMessage protocol, but dispatched as DOM events for page-level hosts.
 * If the element also sits inside an iframe, postMessage emission still fires
 * for the iframe parent; the two channels coexist.
 */
export class ShiftSurveyElement extends HTMLElement {
  static get observedAttributes(): string[] {
    return ['instance-id', 'api-base', 'locale', 'mode', 'active-screen-id'];
  }

  /** Schema-mode setter. Assigning this swaps the element into schema mode and
   *  re-renders with the new schema immediately. */
  #schema: Survey | null = null;
  /** Schema-mode submit handler. In API mode the element manages this itself. */
  #onSubmit: ((submission: SurveySubmission) => void | Promise<void>) | null = null;

  #root: Root | null = null;
  #mount: HTMLDivElement | null = null;
  #apiSchema: Survey | null = null;
  #apiError: Error | null = null;

  constructor() {
    super();
    this.attachShadow({ mode: 'open' });
  }

  // ─── Lifecycle ───────────────────────────────────────────────────────────

  connectedCallback(): void {
    if (!this.shadowRoot) return;
    // Inject the renderer's stylesheet once. Shadow DOM blocks the host page's
    // CSS from leaking in — we own our visual layer end-to-end.
    if (!this.shadowRoot.querySelector('style[data-shift-survey]')) {
      const style = document.createElement('style');
      style.setAttribute('data-shift-survey', '');
      style.textContent = rendererStyles;
      this.shadowRoot.appendChild(style);
    }
    if (!this.#mount) {
      this.#mount = document.createElement('div');
      this.#mount.className = 'shift-survey-mount';
      this.shadowRoot.appendChild(this.#mount);
    }
    if (!this.#root) this.#root = createRoot(this.#mount);
    this.#renderIfReady();
    this.#maybeFetchSchema();
  }

  disconnectedCallback(): void {
    // Defer the unmount — disconnectedCallback fires synchronously during DOM
    // moves, and React 19 complains if we tear down a root mid-render. A
    // microtask delay plus a re-check gives React room to settle. Guard against
    // test teardown races where `window` has been torn down by jsdom.
    queueMicrotask(() => {
      if (this.isConnected || typeof window === 'undefined') return;
      try {
        this.#root?.unmount();
      } catch {
        // jsdom tears down mid-unmount during test runs — no-op is safe.
      }
      this.#root = null;
    });
  }

  attributeChangedCallback(name: string, oldVal: string | null, newVal: string | null): void {
    if (oldVal === newVal) return;
    if (name === 'instance-id' || name === 'api-base') {
      // API-mode inputs changed — drop any cached schema and refetch.
      this.#apiSchema = null;
      this.#apiError = null;
      this.#maybeFetchSchema();
    }
    this.#renderIfReady();
  }

  // ─── Properties ──────────────────────────────────────────────────────────

  get schema(): Survey | null {
    return this.#schema;
  }
  set schema(value: Survey | null) {
    this.#schema = value;
    this.#renderIfReady();
  }

  get onSubmit(): ((submission: SurveySubmission) => void | Promise<void>) | null {
    return this.#onSubmit;
  }
  set onSubmit(value: ((submission: SurveySubmission) => void | Promise<void>) | null) {
    this.#onSubmit = value;
    this.#renderIfReady();
  }

  /** Builder-preview jump target. Assigning a screen id makes the renderer
   *  jump to that screen (answers preserved); the user can navigate freely
   *  afterwards. Mirrors the `active-screen-id` attribute; the property wins
   *  when both are set. */
  #activeScreenId: string | null = null;
  get activeScreenId(): string | null {
    return this.#activeScreenId ?? this.getAttribute('active-screen-id');
  }
  set activeScreenId(value: string | null) {
    this.#activeScreenId = value;
    this.#renderIfReady();
  }

  // ─── Internals ───────────────────────────────────────────────────────────

  #maybeFetchSchema(): void {
    // Only fetch if we're in API mode (instance-id set) AND not already served
    // via the schema property.
    if (this.#schema) return;
    const instanceId = this.getAttribute('instance-id');
    if (!instanceId) return;
    const apiBase = this.getAttribute('api-base');
    if (!apiBase) return;
    const client = new SurveyClient({ baseUrl: apiBase });
    client
      .fetchSchema(instanceId)
      .then((schema) => {
        this.#apiSchema = schema;
        this.#renderIfReady();
      })
      .catch((error: Error) => {
        this.#apiError = error;
        this.#dispatch('survey:error', { message: error.message });
        this.#renderIfReady();
      });
  }

  #renderIfReady(): void {
    if (!this.#root) return;

    const apiBase = this.getAttribute('api-base');
    const instanceId = this.getAttribute('instance-id');
    const locale = this.getAttribute('locale') ?? undefined;
    const agentMode = this.getAttribute('mode') === 'agent';

    // Prefer the explicit schema property (schema mode) over a fetched one.
    const schema = this.#schema ?? this.#apiSchema;

    if (this.#apiError && !schema) {
      this.#root.render(
        createElement(
          'div',
          { className: 'shift-survey-error', role: 'alert' },
          this.#apiError.message,
        ),
      );
      return;
    }
    if (!schema) {
      this.#root.render(
        createElement('div', { className: 'shift-survey-loading' }, 'Loading…'),
      );
      return;
    }

    // Wire onSubmit. Schema mode uses the property the host set; API mode hits
    // the public submit endpoint via SurveyClient.
    const onSubmit = this.#schema
      ? (this.#onSubmit ??
        ((submission: SurveySubmission) => {
          this.#dispatch('survey:completed', { ...submission });
        }))
      : async (submission: SurveySubmission) => {
          if (!apiBase || !instanceId)
            throw new Error('shift-survey: API mode requires both instance-id and api-base attributes.');
          const client = new SurveyClient({ baseUrl: apiBase });
          await client.submitResponse(instanceId, submission);
        };

    const activeScreenId = this.activeScreenId;

    this.#root.render(
      createElement(SurveyRenderer, {
        schema,
        onSubmit,
        ...(locale ? { locale } : {}),
        ...(activeScreenId ? { activeScreenId } : {}),
        // Let the element be the resume key in API mode so two surveys on the
        // same host page don't clobber each other.
        ...(instanceId ? { resumeKey: instanceId } : {}),
        ...(agentMode ? { submissionMeta: { mode: 'agent' } } : {}),
        // CustomEvents are the web-component's channel; postMessage stays opt-in
        // via iframe auto-detect on the enclosing page (unchanged).
        onScreenChange: (screenId: string | null) =>
          this.#dispatch('survey:screen-changed', { screenId }),
        onCompleted: (screenId: string | null) =>
          this.#dispatch('survey:completed', { screenId }),
      }),
    );

    if (!this.#loadedDispatched) {
      this.#loadedDispatched = true;
      this.#dispatch('survey:loaded', {});
    }
  }

  #loadedDispatched = false;

  #dispatch(type: string, detail: Record<string, unknown>): void {
    this.dispatchEvent(
      new CustomEvent(type, { detail, bubbles: true, composed: true }),
    );
  }
}
