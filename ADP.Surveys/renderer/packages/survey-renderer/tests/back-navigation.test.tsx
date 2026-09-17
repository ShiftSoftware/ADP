/**
 * Back navigation: the history stack behind the Back button, and the "path"
 * semantics it gives answers — an answer given on a branch the respondent then
 * backed out of is parked (kept in the input, but neither routing logic nor
 * tokens nor the submission see it) until they walk that branch again.
 */

import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';
import type { ResumeStorage } from '../src/resume.js';
import { storageKey } from '../src/resume.js';

/** welcome → what-next (navigationList) → branch-a | branch-b → city → done,
 *  with a logic rule off branch A's `vip` answer and a token on the city title
 *  that names branch A's note. */
function fixture(): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    screens: [
      {
        id: 'welcome',
        title: { en: 'Welcome' },
        questions: [{ type: 'text', id: 'name', title: { en: 'Your name' } }],
        nextScreen: 'what-next',
      },
      {
        id: 'what-next',
        title: { en: 'What next?' },
        questions: [
          {
            type: 'navigationList',
            id: 'intent',
            title: { en: 'Pick one' },
            options: [
              { id: 'a', label: { en: 'Branch A' }, nextScreen: 'branch-a' },
              { id: 'b', label: { en: 'Branch B' }, nextScreen: 'branch-b' },
            ],
          },
        ],
      },
      {
        id: 'branch-a',
        title: { en: 'Branch A' },
        questions: [
          { type: 'text', id: 'a-note', title: { en: 'A note' } },
          { type: 'yesNo', id: 'vip', title: { en: 'VIP?' } },
        ],
        nextScreen: 'city',
      },
      {
        id: 'branch-b',
        title: { en: 'Branch B' },
        questions: [{ type: 'text', id: 'b-note', title: { en: 'B note' } }],
        nextScreen: 'city',
      },
      {
        id: 'city',
        title: { en: 'City — {{answers.a-note|no note}}' },
        questions: [{ type: 'text', id: 'city', title: { en: 'Your city' } }],
        nextScreen: 'done',
      },
      { id: 'vip-lounge', title: { en: 'VIP lounge' }, questions: [] },
      { id: 'done', title: { en: 'Done' }, questions: [] },
    ],
    logic: [{ if: { questionId: 'vip', op: '==', value: true }, then: { goto: 'vip-lounge' } }],
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

const back = () => screen.getByRole('button', { name: 'Back' });
const next = () => screen.getByRole('button', { name: 'Next' });
const heading = (name: string) => screen.getByRole('heading', { name });
const noBack = () => expect(screen.queryByRole('button', { name: 'Back' })).toBeNull();

describe('Back navigation', () => {
  it('offers no Back on the first screen; after Next it returns there with the answer intact', async () => {
    render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} />);
    const user = userEvent.setup();
    noBack();

    await user.type(screen.getByLabelText('Your name'), 'Sara');
    await user.click(next());
    heading('What next?');

    await user.click(back());
    heading('Welcome');
    expect((screen.getByLabelText('Your name') as HTMLInputElement).value).toBe('Sara');
    noBack();
  });

  it('shows Back on a navigationList screen, which has no Next, and returns from a tapped branch with the pick highlighted', async () => {
    render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} />);
    const user = userEvent.setup();

    await user.click(next());
    heading('What next?');
    expect(screen.queryByRole('button', { name: 'Next' })).toBeNull();
    expect(back()).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /Branch A/ }));
    heading('Branch A');

    await user.click(back());
    heading('What next?');
    expect(screen.getByRole('button', { name: /Branch A/ })).toHaveAttribute('aria-pressed', 'true');
    expect(screen.getByRole('button', { name: /Branch B/ })).toHaveAttribute('aria-pressed', 'false');
  });

  it('parks the answers of a branch the respondent backed out of: no routing, no tokens, not submitted — but still there on re-entry', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();

    // Walk into branch A, answer both questions, then change course to branch B.
    await user.click(next());
    await user.click(screen.getByRole('button', { name: /Branch A/ }));
    await user.type(screen.getByLabelText('A note'), 'alpha');
    await user.click(screen.getByRole('radio', { name: 'Yes' }));
    await user.click(back());
    await user.click(screen.getByRole('button', { name: /Branch B/ }));
    heading('Branch B');
    await user.type(screen.getByLabelText('B note'), 'beta');

    // `vip == true` is parked on branch A, so the rule must not fire from branch B …
    await user.click(next());
    // … and the token naming branch A's note falls back.
    heading('City — no note');

    // Re-entering branch A finds its answers where they were left.
    await user.click(back());
    await user.click(back());
    heading('What next?');
    await user.click(screen.getByRole('button', { name: /Branch A/ }));
    expect((screen.getByLabelText('A note') as HTMLInputElement).value).toBe('alpha');
    expect(screen.getByRole('radio', { name: 'Yes' })).toBeChecked();

    // Back to branch B and through to the end: the submission carries the path's
    // answers only.
    await user.click(back());
    await user.click(screen.getByRole('button', { name: /Branch B/ }));
    await user.click(next());
    await user.type(screen.getByLabelText('Your city'), 'Erbil');
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers).toEqual({ intent: 'b', 'b-note': 'beta', city: 'Erbil' });
  });

  it('with the branch A answers on the path, the rule and the token both see them', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();

    await user.click(next());
    await user.click(screen.getByRole('button', { name: /Branch A/ }));
    await user.type(screen.getByLabelText('A note'), 'alpha');
    await user.click(next());
    heading('City — alpha');

    await user.click(back());
    await user.click(screen.getByRole('radio', { name: 'Yes' }));
    // The press that routes to the zero-question VIP screen is the committing one.
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers).toEqual({ intent: 'a', 'a-note': 'alpha', vip: true });
    heading('VIP lounge');
    noBack();
  });

  it('Back ignores the required gate and clears its flag', async () => {
    const schema = fixture();
    (schema.screens[0]!.questions![0] as Record<string, unknown>)['required'] = true;
    render(<SurveyRenderer schema={schema} onSubmit={vi.fn()} activeScreenId="what-next" />);
    const user = userEvent.setup();

    // The jump is a forward hop, so Back leads to the (unanswered, required) welcome screen.
    await user.click(back());
    heading('Welcome');
    await user.click(next());
    expect(screen.getByRole('alert')).toHaveTextContent('This question is required.');

    // Leaving is always allowed; the flag is gone when the screen shows again.
    await user.type(screen.getByLabelText(/Your name/), 'x');
    await user.click(next());
    heading('What next?');
    await user.click(back());
    expect(screen.queryByRole('alert')).toBeNull();
  });

  it('persists the history with the resume state, so Back survives a reload', async () => {
    const storage = makeStorage();
    const { unmount } = render(
      <SurveyRenderer schema={fixture()} onSubmit={vi.fn()} resumeKey="inst-1" storage={storage} />,
    );
    const user = userEvent.setup();
    await user.click(next());
    await user.click(screen.getByRole('button', { name: /Branch B/ }));
    heading('Branch B');
    await waitFor(() => {
      expect(JSON.parse(storage.store[storageKey('inst-1')]!).history).toEqual(['welcome', 'what-next']);
    });

    unmount();
    render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} resumeKey="inst-1" storage={storage} />);
    heading('Branch B');
    await user.click(back());
    heading('What next?');
    await user.click(back());
    heading('Welcome');
    noBack();
  });

  it('replays the path for resume state saved before histories were persisted', async () => {
    const storage = makeStorage();
    storage.setItem(
      storageKey('inst-2'),
      JSON.stringify({ answers: { name: 'old', intent: 'b', 'b-note': 'beta' }, currentScreenId: 'city', savedAt: 1 }),
    );
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} resumeKey="inst-2" storage={storage} />);
    const user = userEvent.setup();
    heading('City — no note');

    // welcome → what-next → branch-b → city, rebuilt from the answers.
    await user.click(back());
    heading('Branch B');
    expect((screen.getByLabelText('B note') as HTMLInputElement).value).toBe('beta');
    await user.click(back());
    heading('What next?');
    await user.click(back());
    heading('Welcome');
    noBack();
  });

  it('builder jump: to a walked screen rewinds the history; anywhere else it is a forward hop', async () => {
    const { rerender } = render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} activeScreenId={null} />);
    const user = userEvent.setup();
    await user.click(next());
    await user.click(screen.getByRole('button', { name: /Branch A/ }));
    heading('Branch A');

    // Rewind to a screen already walked: the screens after it leave the path.
    rerender(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} activeScreenId="welcome" activeScreenJumpToken={1} />);
    heading('Welcome');
    noBack();

    // A hop to an unwalked screen keeps where the preview was as the way back.
    rerender(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} activeScreenId="city" activeScreenJumpToken={2} />);
    heading('City — no note');
    await user.click(back());
    heading('Welcome');
  });

  it('hides Back while a submission is in flight', async () => {
    // Two screens, the second one ending the survey outright, so the committing
    // press is the screen's own button and stays visible while it runs.
    const schema: Survey = {
      id: 's',
      defaultLocale: 'en',
      screens: [
        { id: 'one', title: { en: 'One' }, questions: [{ type: 'text', id: 'a', title: { en: 'A' } }] },
        { id: 'two', title: { en: 'Two' }, questions: [{ type: 'text', id: 'b', title: { en: 'B' } }] },
      ],
    };
    let release!: () => void;
    const onSubmit = vi.fn(() => new Promise<void>((resolve) => (release = resolve)));
    render(<SurveyRenderer schema={schema} onSubmit={onSubmit} activeScreenId="two" />);
    const user = userEvent.setup();
    heading('Two');
    expect(back()).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Submit' }));
    expect(screen.getByRole('button', { name: 'Submitting…' })).toBeDisabled();
    noBack();

    release();
    await waitFor(() => expect(document.querySelector('.survey-root--done')).not.toBeNull());
    noBack();
  });
});
