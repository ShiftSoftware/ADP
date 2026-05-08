import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';

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
        questions: [{ type: 'text', id: 'name', title: { en: 'Your name' }, required: false }],
        nextScreen: 'feedback',
      },
      {
        id: 'feedback',
        title: { en: 'How was it?' },
        questions: [
          {
            type: 'nps',
            id: 'nps',
            title: { en: 'Rate us' },
            min: 0,
            max: 10,
            required: true,
          },
        ],
        nextScreen: 'brand',
      },
      {
        id: 'brand',
        title: { en: 'Which brand?' },
        questions: [
          {
            type: 'navigationList',
            id: 'brand-choice',
            title: { en: 'Pick one' },
            options: [
              { id: 'toyota', label: { en: 'Toyota' }, nextScreen: 'thanks-toyota' },
              { id: 'other', label: { en: 'Other' }, nextScreen: 'thanks-default' },
            ],
          },
        ],
      },
      { id: 'thanks-toyota', title: { en: 'Thanks — Toyota fan!' }, questions: [] },
      { id: 'thanks-default', title: { en: 'Thanks!' }, questions: [] },
    ],
    logic: [
      { if: { questionId: 'nps', op: '<=', value: 6 }, then: { goto: 'thanks-default' } },
    ],
  };
}

describe('SurveyRenderer', () => {
  it('renders the first screen with its question', () => {
    render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} />);
    expect(screen.getByRole('heading', { name: 'Welcome' })).toBeInTheDocument();
    expect(screen.getByLabelText('Your name')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Next' })).toBeInTheDocument();
  });

  it('advances to the next screen on Next', async () => {
    render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} />);
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Your name'), 'Aza');
    await user.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByRole('heading', { name: 'How was it?' })).toBeInTheDocument();
  });

  it('logic rule routes a detractor to thanks-default', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const onScreenChange = vi.fn();
    render(
      <SurveyRenderer schema={fixture()} onSubmit={onSubmit} onScreenChange={onScreenChange} />,
    );
    const user = userEvent.setup();
    // Advance past welcome with no input.
    await user.click(screen.getByRole('button', { name: 'Next' }));
    // On "feedback": pick NPS = 3.
    await user.click(screen.getByRole('radio', { name: '3' }));
    await user.click(screen.getByRole('button', { name: 'Next' }));
    // Logic rule routes us to thanks-default, skipping brand.
    expect(screen.getByRole('heading', { name: 'Thanks!' })).toBeInTheDocument();
    expect(onScreenChange).toHaveBeenCalledWith('thanks-default');
  });

  it('navigationList tap routes via per-option nextScreen', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Next' })); // welcome → feedback
    await user.click(screen.getByRole('radio', { name: '10' }));
    await user.click(screen.getByRole('button', { name: 'Next' })); // feedback → brand (promoter, no rule)
    expect(screen.getByRole('heading', { name: 'Which brand?' })).toBeInTheDocument();
    // Next button is hidden for navigationList-terminal screens.
    expect(screen.queryByRole('button', { name: 'Next' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Toyota/ }));
    expect(screen.getByRole('heading', { name: 'Thanks — Toyota fan!' })).toBeInTheDocument();
  });

  it('sets dir="rtl" + translated UI chrome when locale=ar', async () => {
    const { container } = render(
      <SurveyRenderer schema={fixture()} onSubmit={vi.fn()} locale="ar" />,
    );
    const root = container.querySelector('.survey-root');
    expect(root?.getAttribute('dir')).toBe('rtl');
    expect(root?.getAttribute('lang')).toBe('ar');
    // Built-in Arabic translation for "Next".
    expect(screen.getByRole('button', { name: 'التالي' })).toBeInTheDocument();
  });

  it('caller-supplied uiLocales override the built-in English strings', () => {
    render(
      <SurveyRenderer
        schema={fixture()}
        onSubmit={vi.fn()}
        uiLocales={{
          en: {
            direction: 'ltr',
            strings: {
              next: 'Continue',
              submitting: 'Submitting…',
              loading: 'Loading…',
              thankYou: 'Thanks!',
              selectPlaceholder: 'Select…',
              clearSignature: 'Clear',
              noScreens: 'No screens.',
              unsupportedQuestion: 'Unsupported:',
              couldNotSubmit: 'Error:',
              yes: 'Yes',
              no: 'No',
            },
          },
        }}
      />,
    );
    expect(screen.getByRole('button', { name: 'Continue' })).toBeInTheDocument();
  });

  it('auto-submits the full answer map on arrival at a terminal zero-question screen', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Your name'), 'Aza');
    await user.click(screen.getByRole('button', { name: 'Next' })); // → feedback
    await user.click(screen.getByRole('radio', { name: '3' }));
    await user.click(screen.getByRole('button', { name: 'Next' })); // → thanks-default, auto-submits
    // No more Next button on a zero-question terminal screen.
    expect(screen.queryByRole('button', { name: /Next/ })).not.toBeInTheDocument();
    // onSubmit fires on arrival via useEffect — wait for the async submission.
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    const call = onSubmit.mock.calls[0]![0];
    expect(call.answers.name).toBe('Aza');
    expect(call.answers.nps).toBe(3);
    expect(call.schemaVersion).toBe(1);
    expect(call.meta?.startedAt).toBeTruthy();
    expect(call.meta?.completedAt).toBeTruthy();
  });
});
