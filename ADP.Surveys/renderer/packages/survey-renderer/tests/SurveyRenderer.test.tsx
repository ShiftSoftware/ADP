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
    // With NPS=3 the logic rule routes to thanks-default — a zero-question end
    // screen — so the answer-aware label already reads Submit, not Next.
    await user.click(screen.getByRole('button', { name: 'Submit' }));
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
              requiredError: 'Required!',
              yes: 'Yes',
              no: 'No',
            },
          },
        }}
      />,
    );
    expect(screen.getByRole('button', { name: 'Continue' })).toBeInTheDocument();
  });

  it('applies branding as CSS variables + logo, hides logo on load error', async () => {
    const branded: Survey = {
      ...fixture(),
      branding: {
        primaryColor: '#eb0a1e',
        secondaryColor: '#f4a300',
        logoUrl: 'https://brand.example/logo.png',
      },
    };
    const { container } = render(<SurveyRenderer schema={branded} onSubmit={vi.fn()} />);

    const root = container.querySelector('.survey-root') as HTMLElement;
    expect(root.style.getPropertyValue('--survey-primary')).toBe('#eb0a1e');
    expect(root.style.getPropertyValue('--survey-accent')).toBe('#f4a300');
    // Toyota red is dark — contrast text must be white.
    expect(root.style.getPropertyValue('--survey-primary-contrast')).toBe('#ffffff');
    // Hover shade derived, darker than the base.
    expect(root.style.getPropertyValue('--survey-primary-hover')).not.toBe('');

    const logo = container.querySelector('.survey-brand__logo') as HTMLImageElement;
    expect(logo).toBeInTheDocument();
    expect(logo.src).toBe('https://brand.example/logo.png');

    // Broken logo URL degrades to no logo, not a broken-image glyph.
    logo.dispatchEvent(new Event('error'));
    expect((container.querySelector('.survey-brand') as HTMLElement).style.display).toBe('none');
  });

  it('renders no logo header and keeps default styling without branding', () => {
    const { container } = render(<SurveyRenderer schema={fixture()} onSubmit={vi.fn()} />);
    expect(container.querySelector('.survey-brand')).not.toBeInTheDocument();
    const root = container.querySelector('.survey-root') as HTMLElement;
    expect(root.style.getPropertyValue('--survey-primary')).toBe('');
  });

  it('blocks Next while a required question is unanswered, clears once answered', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Next' })); // welcome → feedback (name not required)
    expect(screen.getByRole('heading', { name: 'How was it?' })).toBeInTheDocument();

    // NPS is required — Next must not navigate, and the inline error appears.
    await user.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByRole('heading', { name: 'How was it?' })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('This question is required.');

    // Answering clears the flag without another Next click.
    await user.click(screen.getByRole('radio', { name: '9' }));
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByRole('heading', { name: 'Which brand?' })).toBeInTheDocument();
  });

  it('auto-submits the full answer map on arrival at a terminal zero-question screen', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Your name'), 'Aza');
    await user.click(screen.getByRole('button', { name: 'Next' })); // → feedback
    await user.click(screen.getByRole('radio', { name: '3' }));
    await user.click(screen.getByRole('button', { name: 'Submit' })); // → thanks-default, auto-submits
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

  it('low-score sticky-rule flow still auto-submits on the end screen', async () => {
    // Regression for the "Service Visit Feedback" strand: a global rule
    // (nps<=6 → recover) matches the final answer map forever; the end screen
    // must terminate anyway or auto-submit never fires and the respondent is
    // stuck on a buttonless thanks screen with no API call.
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const schema: Survey = {
      id: 's',
      version: 1,
      defaultLocale: 'en',
      locales: ['en'],
      screens: [
        {
          id: 'overall',
          title: { en: 'Overall' },
          questions: [{ type: 'nps', id: 'nps', title: { en: 'Score' }, required: true }],
          nextScreen: 'thanks',
        },
        {
          id: 'recover',
          title: { en: 'We want to make it right' },
          questions: [
            { type: 'paragraph', id: 'recover-details', title: { en: 'What went wrong?' }, required: true },
          ],
        },
        { id: 'thanks', title: { en: 'Thank you!' }, questions: [] },
      ],
      logic: [{ if: { questionId: 'nps', op: '<=', value: 6 }, then: { goto: 'recover' } }],
    };
    render(<SurveyRenderer schema={schema} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    await user.click(screen.getByRole('radio', { name: '0' }));
    await user.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByRole('heading', { name: 'We want to make it right' })).toBeInTheDocument();
    await user.type(screen.getByLabelText(/What went wrong/), 'Slow service');
    // Recover falls through sequentially to the zero-question thanks screen —
    // that press is the commit (labeled Submit) and arrival auto-submits.
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit.mock.calls[0]![0].answers.nps).toBe(0);
    expect(screen.getByRole('heading', { name: 'Thank you!' })).toBeInTheDocument();
  });

  it('blocks Next on a constraint violation with a localized inline error, clears once fixed', async () => {
    const schema = fixture();
    // Tighten the name question: minLength 5.
    (schema.screens[0]!.questions![0] as Record<string, unknown>)['minLength'] = 5;
    render(<SurveyRenderer schema={schema} onSubmit={vi.fn()} />);
    const user = userEvent.setup();

    await user.type(screen.getByLabelText('Your name'), 'Aza');
    await user.click(screen.getByRole('button', { name: 'Next' }));

    // Still on the first screen, localized template rendered with n=5.
    expect(screen.getByRole('heading', { name: 'Welcome' })).toBeInTheDocument();
    expect(screen.getByRole('alert')).toHaveTextContent('Must be at least 5 characters.');

    // Fixing the answer clears the error live (no Next needed)...
    await user.type(screen.getByLabelText('Your name'), 'nya');
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();

    // ...and Next now advances.
    await user.click(screen.getByRole('button', { name: 'Next' }));
    expect(screen.getByRole('heading', { name: 'How was it?' })).toBeInTheDocument();
  });

  it('jumps to activeScreenId when the prop changes (builder preview sync)', () => {
    const schema = fixture();
    const { rerender } = render(
      <SurveyRenderer schema={schema} onSubmit={vi.fn()} activeScreenId="feedback" />,
    );
    // Mount-time jump straight to the selected screen.
    expect(screen.getByRole('heading', { name: 'How was it?' })).toBeInTheDocument();

    rerender(<SurveyRenderer schema={schema} onSubmit={vi.fn()} activeScreenId="brand" />);
    expect(screen.getByRole('heading', { name: 'Which brand?' })).toBeInTheDocument();

    // Unknown ids are ignored — stay put.
    rerender(<SurveyRenderer schema={schema} onSubmit={vi.fn()} activeScreenId="nope" />);
    expect(screen.getByRole('heading', { name: 'Which brand?' })).toBeInTheDocument();
  });

  it('labels the ending press Submit instead of Next (answer-aware)', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const schema: Survey = {
      id: 's',
      version: 1,
      defaultLocale: 'en',
      locales: ['en'],
      screens: [
        {
          id: 'one',
          title: { en: 'One' },
          questions: [{ type: 'text', id: 'a', title: { en: 'A' }, required: false }],
        },
        {
          id: 'two',
          title: { en: 'Two' },
          questions: [{ type: 'text', id: 'b', title: { en: 'B' }, required: false }],
        },
      ],
      logic: [],
    };
    render(<SurveyRenderer schema={schema} onSubmit={onSubmit} />);
    const user = userEvent.setup();
    // Screen one still has onward flow → Next.
    expect(screen.getByRole('button', { name: 'Next' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Submit' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Next' }));
    // Screen two is last in flow — this press is the submission.
    expect(screen.queryByRole('button', { name: 'Next' })).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
  });

  it('labels the press Submit when it routes to a zero-question end screen', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    const schema: Survey = {
      id: 's',
      version: 1,
      defaultLocale: 'en',
      locales: ['en'],
      screens: [
        {
          id: 'q',
          title: { en: 'Q' },
          questions: [{ type: 'text', id: 'a', title: { en: 'A' }, required: false }],
          nextScreen: 'done',
        },
        { id: 'done', title: { en: 'Thanks!' }, questions: [] },
      ],
      logic: [],
    };
    render(<SurveyRenderer schema={schema} onSubmit={onSubmit} />);
    // The press routes to 'done', which auto-submits on arrival — from the
    // respondent's seat this IS the committing press, so it must read Submit.
    expect(screen.queryByRole('button', { name: 'Next' })).not.toBeInTheDocument();
    await userEvent.setup().click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(screen.getByRole('heading', { name: 'Thanks!' })).toBeInTheDocument();
  });
});
