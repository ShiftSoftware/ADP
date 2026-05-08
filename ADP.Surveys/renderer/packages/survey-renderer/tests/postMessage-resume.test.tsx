/**
 * Tests for the iframe host-bridge protocol and the localStorage resume flow.
 * Mocks `window.parent.postMessage` and a `ResumeStorage` so neither needs a
 * real browser host.
 */

import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';
import type { ResumeStorage } from '../src/resume.js';
import { storageKey } from '../src/resume.js';

function fixture(): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    screens: [
      { id: 'welcome', title: { en: 'Welcome' }, questions: [{ type: 'text', id: 'name', title: { en: 'Your name' } }], nextScreen: 'rate' },
      { id: 'rate', title: { en: 'Rate' }, questions: [{ type: 'nps', id: 'nps', title: { en: 'NPS' }, min: 0, max: 10 }], nextScreen: 'thanks' },
      { id: 'thanks' },
    ],
  };
}

function makeStorage(): ResumeStorage & { store: Record<string, string> } {
  const store: Record<string, string> = {};
  return {
    store,
    getItem: (k) => store[k] ?? null,
    setItem: (k, v) => {
      store[k] = v;
    },
    removeItem: (k) => {
      delete store[k];
    },
  };
}

describe('postMessage host bridge', () => {
  it('emits loaded + screen-changed + completed when forced enabled', async () => {
    const posts: Array<{ msg: unknown; origin: string }> = [];
    const fakeParent = {
      postMessage: (msg: unknown, origin: string) => posts.push({ msg, origin }),
    } as unknown as Window;

    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={onSubmit}
        emitHostMessages
        hostMessageTarget={fakeParent}
        hostMessageOrigin="https://host.example"
      />,
    );

    // Loaded + first screen-changed fire on mount.
    await waitFor(() => {
      expect(posts.find((p) => (p.msg as { type: string }).type === 'survey:loaded')).toBeTruthy();
      expect(
        posts.find(
          (p) =>
            (p.msg as { type: string }).type === 'survey:screen-changed' &&
            ((p.msg as { payload: { screenId: string } }).payload.screenId === 'welcome'),
        ),
      ).toBeTruthy();
    });

    // Every message carries the namespace envelope + honors the origin prop.
    for (const p of posts) {
      const m = p.msg as { source: string; version: number };
      expect(m.source).toBe('adp-surveys');
      expect(m.version).toBe(1);
      expect(p.origin).toBe('https://host.example');
    }

    // Walk to rate screen.
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Next' }));
    await waitFor(() => {
      expect(
        posts.find(
          (p) =>
            (p.msg as { type: string }).type === 'survey:screen-changed' &&
            (p.msg as { payload: { screenId: string } }).payload.screenId === 'rate',
        ),
      ).toBeTruthy();
    });

    // Pick NPS + advance → auto-submit on thanks.
    await user.click(screen.getByRole('radio', { name: '8' }));
    await user.click(screen.getByRole('button', { name: 'Next' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());

    // Completed message fired with the answers.
    await waitFor(() => {
      const completed = posts.find(
        (p) => (p.msg as { type: string }).type === 'survey:completed',
      );
      expect(completed).toBeTruthy();
      const payload = (completed!.msg as { payload: { answers: Record<string, unknown> } })
        .payload;
      expect(payload.answers.nps).toBe(8);
    });
  });

  it('error message fires when the submission throws', async () => {
    const posts: Array<{ msg: unknown }> = [];
    const fakeParent = {
      postMessage: (msg: unknown) => posts.push({ msg }),
    } as unknown as Window;

    const onSubmit = vi.fn().mockRejectedValue(new Error('boom'));
    render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={onSubmit}
        emitHostMessages
        hostMessageTarget={fakeParent}
      />,
    );

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Next' })); // → rate
    await user.click(screen.getByRole('radio', { name: '8' }));
    await user.click(screen.getByRole('button', { name: 'Next' })); // → thanks, submit throws
    await waitFor(() => {
      const err = posts.find((p) => (p.msg as { type: string }).type === 'survey:error');
      expect(err).toBeTruthy();
      expect((err!.msg as { payload: { message: string } }).payload.message).toBe('boom');
    });
  });

  it('no messages when emitHostMessages is explicitly false', () => {
    const posts: Array<unknown> = [];
    const fakeParent = {
      postMessage: (msg: unknown) => posts.push(msg),
    } as unknown as Window;

    render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={vi.fn()}
        emitHostMessages={false}
        hostMessageTarget={fakeParent}
      />,
    );
    expect(posts).toEqual([]);
  });
});

describe('localStorage resume', () => {
  it('persists answers + current screen; restores on a fresh render', async () => {
    const storage = makeStorage();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    const { unmount } = render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={onSubmit}
        resumeKey="inst-123"
        storage={storage}
      />,
    );

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Your name'), 'Aza');
    await user.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByRole('heading', { name: 'Rate' })).toBeInTheDocument();

    // After advancing we should have persisted { answers: {name: 'Aza'}, currentScreenId: 'rate' }.
    await waitFor(() => {
      const raw = storage.store[storageKey('inst-123')];
      expect(raw).toBeTruthy();
      const parsed = JSON.parse(raw!);
      expect(parsed.answers.name).toBe('Aza');
      expect(parsed.currentScreenId).toBe('rate');
    });

    // Simulate a reload: unmount + remount against the same storage.
    unmount();
    render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={onSubmit}
        resumeKey="inst-123"
        storage={storage}
      />,
    );
    // Restored to the `rate` screen, not `welcome`.
    expect(screen.getByRole('heading', { name: 'Rate' })).toBeInTheDocument();
  });

  it('clears resume state on successful submission', async () => {
    const storage = makeStorage();
    const onSubmit = vi.fn().mockResolvedValue(undefined);

    render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={onSubmit}
        resumeKey="inst-456"
        storage={storage}
      />,
    );

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Next' })); // → rate
    await user.click(screen.getByRole('radio', { name: '9' }));
    await user.click(screen.getByRole('button', { name: 'Next' })); // → thanks, auto-submit

    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    await waitFor(() => {
      expect(storage.store[storageKey('inst-456')]).toBeUndefined();
    });
  });

  it('drops resume state whose screen id is no longer in the schema', () => {
    const storage = makeStorage();
    storage.setItem(
      storageKey('inst-789'),
      JSON.stringify({
        answers: { name: 'old' },
        currentScreenId: 'a-screen-that-does-not-exist',
        savedAt: Date.now(),
      }),
    );
    render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={vi.fn()}
        resumeKey="inst-789"
        storage={storage}
      />,
    );
    // Answers still restored; we just fall back to the first screen.
    expect(screen.getByRole('heading', { name: 'Welcome' })).toBeInTheDocument();
    expect((screen.getByLabelText('Your name') as HTMLInputElement).value).toBe('old');
  });
});
