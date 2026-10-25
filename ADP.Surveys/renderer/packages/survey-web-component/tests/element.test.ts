import { describe, expect, it, vi } from 'vitest';
import { waitFor } from '@testing-library/dom';
import type { Survey } from '@shift-software/survey-sdk';
import { ShiftSurveyElement, registerShiftSurvey } from '../src/index.js';

// Ensure the element is registered before any test mounts it.
registerShiftSurvey();

function fixture(): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    screens: [
      { id: 'welcome', title: { en: 'Welcome' }, questions: [{ type: 'text', id: 'name', title: { en: 'Name' } }] },
      { id: 'thanks' },
    ],
  };
}

describe('<shift-survey>', () => {
  it('registers under the custom-elements registry', () => {
    expect(customElements.get('shift-survey')).toBe(ShiftSurveyElement);
  });

  it('schema-mode: mounts the renderer and dispatches survey:loaded', async () => {
    const el = document.createElement('shift-survey') as ShiftSurveyElement;
    const loaded = vi.fn();
    el.addEventListener('survey:loaded', loaded);
    document.body.appendChild(el);
    el.schema = fixture();
    el.onSubmit = vi.fn();

    // Wait for React's async render to commit.
    await waitFor(() => {
      const root = el.shadowRoot!.querySelector('.survey-root');
      expect(root).toBeTruthy();
    });
    const root = el.shadowRoot!.querySelector('.survey-root');
    expect(root!.getAttribute('lang')).toBe('en');
    expect(loaded).toHaveBeenCalledTimes(1);
    document.body.removeChild(el);
  });

  it('dispatches survey:screen-changed with the screen id', async () => {
    const el = document.createElement('shift-survey') as ShiftSurveyElement;
    const events: Array<{ screenId: string | null }> = [];
    el.addEventListener('survey:screen-changed', (e) => events.push((e as CustomEvent).detail));
    document.body.appendChild(el);
    el.schema = fixture();
    el.onSubmit = vi.fn();

    await waitFor(() => {
      expect(events.at(-1)?.screenId).toBe('welcome');
    });
    document.body.removeChild(el);
  });

  it('locale attribute threads into the renderer (dir=rtl + Arabic chrome)', async () => {
    const el = document.createElement('shift-survey') as ShiftSurveyElement;
    el.setAttribute('locale', 'ar');
    document.body.appendChild(el);
    el.schema = {
      ...fixture(),
      defaultLocale: 'en',
      locales: ['en', 'ar'],
      screens: [
        { id: 'w', title: { en: 'W', ar: 'و' }, questions: [{ type: 'text', id: 'n', title: { en: 'N', ar: 'ن' } }] },
      ],
    };
    el.onSubmit = vi.fn();

    await waitFor(() => {
      const root = el.shadowRoot!.querySelector('.survey-root') as HTMLElement | null;
      expect(root).toBeTruthy();
      expect(root!.getAttribute('dir')).toBe('rtl');
    });
    const root = el.shadowRoot!.querySelector('.survey-root') as HTMLElement;
    expect(root.getAttribute('lang')).toBe('ar');
    document.body.removeChild(el);
  });

  it('api-mode: an answered link (409) reads "Already answered" in the link language, not the API wording', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      new Response(JSON.stringify({ Message: 'This survey has already been completed.' }), {
        status: 409,
        headers: { 'Content-Type': 'application/json' },
      }),
    );
    vi.stubGlobal('fetch', fetchMock);
    try {
      const el = document.createElement('shift-survey') as ShiftSurveyElement;
      el.setAttribute('api-base', 'https://surveys.example/api/Surveys');
      el.setAttribute('instance-id', '00000000-0000-0000-0000-000000000001');
      el.setAttribute('locale', 'ar');
      document.body.appendChild(el);

      await waitFor(() => {
        expect(el.shadowRoot!.querySelector('.survey-screen__title')?.textContent).toBe('تمت الإجابة مسبقاً');
      });
      expect(el.shadowRoot!.textContent).not.toContain('already been completed');
      expect(el.shadowRoot!.querySelector('.survey-root')!.getAttribute('dir')).toBe('rtl');
      document.body.removeChild(el);
    } finally {
      vi.unstubAllGlobals();
    }
  });

  it('shadow DOM has a style tag injected (content is inlined at build time)', () => {
    // NOTE: we verify the `<style>` tag exists with the data-shift-survey
    // marker; content is empty in vitest (Vite's `?inline` transform doesn't
    // run under vitest's CSS pipeline) but fully populated in production
    // bundles. A separate build-time smoke (via the preview app screenshot)
    // confirms styles reach the shadow root in a real browser.
    const el = document.createElement('shift-survey') as ShiftSurveyElement;
    document.body.appendChild(el);
    const style = el.shadowRoot!.querySelector('style[data-shift-survey]');
    expect(style).toBeTruthy();
    document.body.removeChild(el);
  });
});
