import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { SurveyRenderer } from '@shiftsoftware/survey-renderer';
import '@shiftsoftware/survey-renderer/styles.css';
import { fixtureSurvey } from './fixture.js';
import { showcaseSurvey } from './showcaseFixture.js';

const root = document.getElementById('root');
if (!root) throw new Error('Missing #root');

/** `?showcase=1` renders a one-screen fixture containing every question type.
 *  `?locale=ar` switches to Arabic/RTL (any locale defined in the fixture).
 *  `?webcomponent=1` mounts the `<shift-survey>` custom element instead of
 *  the React renderer directly — proves the web-component wrapper works end-to-end. */
const params = new URLSearchParams(window.location.search);
const useShowcase = params.get('showcase') === '1';
const useWebComponent = params.get('webcomponent') === '1';
const locale = params.get('locale') ?? undefined;

if (useWebComponent) {
  // Lazy-import so the web-component bundle (which ships React) doesn't load
  // on the default React-renderer code path.
  void (async () => {
    await import('@shiftsoftware/survey-web-component');
    const el = document.createElement('shift-survey') as HTMLElement & {
      schema?: unknown;
      onSubmit?: (s: unknown) => unknown;
    };
    if (locale) el.setAttribute('locale', locale);
    el.schema = useShowcase ? showcaseSurvey : fixtureSurvey;
    el.onSubmit = (submission) => {
      // eslint-disable-next-line no-console
      console.log('submission', submission);
      return Promise.resolve();
    };
    for (const t of ['survey:loaded', 'survey:screen-changed', 'survey:completed', 'survey:error'] as const) {
      el.addEventListener(t, (e) => {
        // eslint-disable-next-line no-console
        console.log(t, (e as CustomEvent).detail);
      });
    }
    root.appendChild(el);
  })();
} else {
  function App() {
    return (
      <SurveyRenderer
        schema={useShowcase ? showcaseSurvey : fixtureSurvey}
        {...(locale ? { locale } : {})}
        onSubmit={(submission) => {
          // eslint-disable-next-line no-console
          console.log('submission', submission);
          return Promise.resolve();
        }}
        onScreenChange={(screenId) => {
          // eslint-disable-next-line no-console
          console.log('screen', screenId);
        }}
      />
    );
  }

  createRoot(root).render(
    <StrictMode>
      <App />
    </StrictMode>,
  );
}
