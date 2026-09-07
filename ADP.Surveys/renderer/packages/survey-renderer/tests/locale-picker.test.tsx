import { describe, expect, it, vi } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';
import { localeDisplayName } from '../src/i18n.js';

/**
 * Multi-lingual single-screen survey — the shape the picker exists for.
 *
 * The screen carries a question on purpose: a zero-question terminal screen
 * auto-submits on arrival, which lands the renderer in its `done` state before a
 * test can interact with anything.
 */
function multiLingual(locales: string[] = ['en', 'ar', 'ku']): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales,
    screens: [
      {
        id: 'welcome',
        title: {
          en: 'Warm greetings',
          ar: 'تحية طيبة',
          ku: 'سڵاوی گەرم',
        },
        description: {
          en: 'How was your visit?',
          ar: 'كيف كانت زيارتك؟',
          ku: 'سەردانەکەت چۆن بوو؟',
        },
        questions: [
          {
            type: 'text',
            id: 'name',
            title: { en: 'Your name', ar: 'اسمك', ku: 'ناوت' },
            required: false,
          },
        ],
      },
    ],
    logic: [],
  } as unknown as Survey;
}

function singleLocale(): Survey {
  const survey = multiLingual(['en']);
  return survey;
}

const noop = () => {};

describe('language picker', () => {
  it('is shown when the survey declares more than one locale', () => {
    render(<SurveyRenderer schema={multiLingual()} onSubmit={noop} />);
    expect(screen.getByRole('combobox', { name: 'Language' })).toBeInTheDocument();
  });

  it('is hidden for a single-locale survey', () => {
    render(<SurveyRenderer schema={singleLocale()} onSubmit={noop} />);
    expect(screen.queryByRole('combobox', { name: 'Language' })).not.toBeInTheDocument();
  });

  it('can be forced on and off regardless of locale count', () => {
    const { unmount } = render(
      <SurveyRenderer schema={singleLocale()} onSubmit={noop} showLocalePicker />,
    );
    expect(screen.getByRole('combobox', { name: 'Language' })).toBeInTheDocument();
    unmount();

    render(<SurveyRenderer schema={multiLingual()} onSubmit={noop} showLocalePicker={false} />);
    expect(screen.queryByRole('combobox', { name: 'Language' })).not.toBeInTheDocument();
  });

  it('lists every declared locale by its endonym', () => {
    render(<SurveyRenderer schema={multiLingual()} onSubmit={noop} />);

    expect(screen.getByRole('option', { name: 'English' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'العربية' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'کوردی' })).toBeInTheDocument();
  });

  it('switches the survey copy, the chrome and the direction together', async () => {
    const user = userEvent.setup();
    const { container } = render(<SurveyRenderer schema={multiLingual()} onSubmit={noop} />);

    expect(screen.getByText('Warm greetings')).toBeInTheDocument();
    expect(container.querySelector('.survey-root')).toHaveAttribute('dir', 'ltr');

    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'ar');

    // Schema copy…
    expect(screen.getByText('تحية طيبة')).toBeInTheDocument();
    expect(screen.queryByText('Warm greetings')).not.toBeInTheDocument();
    // …the renderer's own chrome (the picker's accessible name is a UI string)…
    expect(screen.getByRole('combobox', { name: 'اللغة' })).toBeInTheDocument();
    // …and the writing direction, which is the half a picker usually forgets.
    const root = container.querySelector('.survey-root');
    expect(root).toHaveAttribute('dir', 'rtl');
    expect(root).toHaveAttribute('lang', 'ar');
  });

  it('switches to RTL for Kurdish too', async () => {
    const user = userEvent.setup();
    const { container } = render(<SurveyRenderer schema={multiLingual()} onSubmit={noop} />);

    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'ku');

    expect(screen.getByText('سڵاوی گەرم')).toBeInTheDocument();
    expect(container.querySelector('.survey-root')).toHaveAttribute('dir', 'rtl');
  });

  it('reports the choice to the host', async () => {
    const user = userEvent.setup();
    const onLocaleChange = vi.fn();
    render(
      <SurveyRenderer schema={multiLingual()} onSubmit={noop} onLocaleChange={onLocaleChange} />,
    );

    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'ar');
    expect(onLocaleChange).toHaveBeenCalledWith('ar');
  });

  it('starts on the host locale, and the pick then wins over it', async () => {
    const user = userEvent.setup();
    render(<SurveyRenderer schema={multiLingual()} onSubmit={noop} locale="ar" />);

    expect(screen.getByText('تحية طيبة')).toBeInTheDocument();

    await user.selectOptions(screen.getByRole('combobox', { name: 'اللغة' }), 'en');
    expect(screen.getByText('Warm greetings')).toBeInTheDocument();
  });

  it('re-seeds when the host changes the locale prop', async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <SurveyRenderer schema={multiLingual()} onSubmit={noop} locale="en" />,
    );

    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'ar');
    expect(screen.getByText('تحية طيبة')).toBeInTheDocument();

    // A host that deliberately pushes a new locale (builder preview, route change)
    // still wins — the respondent's pick is an override, not a lock.
    rerender(<SurveyRenderer schema={multiLingual()} onSubmit={noop} locale="ku" />);
    expect(screen.getByText('سڵاوی گەرم')).toBeInTheDocument();
  });

  it('falls back to the schema default when the picked locale leaves the schema', async () => {
    const user = userEvent.setup();
    const { rerender } = render(<SurveyRenderer schema={multiLingual()} onSubmit={noop} />);

    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'ku');
    expect(screen.getByText('سڵاوی گەرم')).toBeInTheDocument();

    // The builder preview can swap the schema underneath a respondent mid-session.
    // Dropping ku must not strand them on a language with no copy.
    rerender(<SurveyRenderer schema={multiLingual(['en', 'ar'])} onSubmit={noop} />);
    expect(screen.getByText('Warm greetings')).toBeInTheDocument();
  });

  it('keeps answers across a language switch', async () => {
    const user = userEvent.setup();
    const schema = {
      id: 's',
      version: 1,
      defaultLocale: 'en',
      locales: ['en', 'ar'],
      screens: [
        {
          id: 'welcome',
          title: { en: 'Hello', ar: 'مرحبا' },
          questions: [
            { type: 'text', id: 'name', title: { en: 'Your name', ar: 'اسمك' }, required: false },
          ],
        },
      ],
      logic: [],
    } as unknown as Survey;

    render(<SurveyRenderer schema={schema} onSubmit={noop} />);

    await user.type(screen.getByRole('textbox'), 'Aza');
    await user.selectOptions(screen.getByRole('combobox', { name: 'Language' }), 'ar');

    // The switch really happened…
    expect(screen.getByText('مرحبا')).toBeInTheDocument();
    // …and it was not a reset. Answer state lives above the locale; losing it would
    // punish the respondent for reading their own copy.
    expect(screen.getByRole('textbox')).toHaveValue('Aza');
  });

  it('still renders a selectable entry for a locale nobody anticipated', () => {
    render(<SurveyRenderer schema={multiLingual(['en', 'zz'])} onSubmit={noop} />);
    expect(screen.getByRole('option', { name: localeDisplayName('zz') })).toBeInTheDocument();
  });
});
