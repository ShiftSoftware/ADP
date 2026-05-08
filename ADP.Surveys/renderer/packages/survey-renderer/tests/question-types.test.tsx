/**
 * One smoke test per new question type. Renders the type through the full
 * SurveyRenderer so the test captures the context wiring + setAnswer path, then
 * interacts once and asserts the answer map reaches onSubmit.
 */

import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';

function schemaWith(question: Record<string, unknown>): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    screens: [
      { id: 'q', title: { en: 'Q' }, questions: [question] },
      { id: 'thanks' },
    ],
  };
}

async function advance() {
  const user = userEvent.setup();
  await user.click(screen.getByRole('button', { name: 'Next' }));
}

describe('question-type smoke tests', () => {
  it('paragraph records typed text', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({ type: 'paragraph', id: 'notes', title: { en: 'Notes' } })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Notes'), 'hello world');
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.notes).toBe('hello world');
  });

  it('number records numeric value', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({ type: 'number', id: 'age', title: { en: 'Age' }, min: 0, max: 120 })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Age'), '42');
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.age).toBe(42);
  });

  it('rating records clicked star (1..max)', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({ type: 'rating', id: 'stars', title: { en: 'Stars' }, max: 5 })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.click(screen.getByRole('radio', { name: '4' }));
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.stars).toBe(4);
  });

  it('singleChoice records selected option id', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({
          type: 'singleChoice',
          id: 'brand',
          title: { en: 'Brand' },
          options: [
            { id: 'toyota', label: { en: 'Toyota' } },
            { id: 'honda', label: { en: 'Honda' } },
          ],
        })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.click(screen.getByLabelText('Honda'));
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.brand).toBe('honda');
  });

  it('multiChoice records selected option ids as array', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({
          type: 'multiChoice',
          id: 'tags',
          title: { en: 'Tags' },
          options: [
            { id: 'a', label: { en: 'A' } },
            { id: 'b', label: { en: 'B' } },
            { id: 'c', label: { en: 'C' } },
          ],
        })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.click(screen.getByLabelText('A'));
    await user.click(screen.getByLabelText('C'));
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.tags).toEqual(['a', 'c']);
  });

  it('multiChoice enforces maxSelected', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({
          type: 'multiChoice',
          id: 'tags',
          title: { en: 'Tags' },
          maxSelected: 2,
          options: [
            { id: 'a', label: { en: 'A' } },
            { id: 'b', label: { en: 'B' } },
            { id: 'c', label: { en: 'C' } },
          ],
        })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.click(screen.getByLabelText('A'));
    await user.click(screen.getByLabelText('B'));
    await user.click(screen.getByLabelText('C')); // should be ignored
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.tags).toEqual(['a', 'b']);
  });

  it('dropdown records selected option id', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({
          type: 'dropdown',
          id: 'country',
          title: { en: 'Country' },
          options: [
            { id: 'iq', label: { en: 'Iraq' } },
            { id: 'jp', label: { en: 'Japan' } },
          ],
        })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.selectOptions(screen.getByLabelText('Country'), 'jp');
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.country).toBe('jp');
  });

  it('date records the ISO yyyy-mm-dd', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({ type: 'date', id: 'dob', title: { en: 'DOB' } })}
        onSubmit={onSubmit}
      />,
    );
    const input = screen.getByLabelText('DOB') as HTMLInputElement;
    // userEvent.type struggles with date inputs; set the value directly.
    input.focus();
    const user = userEvent.setup();
    await user.type(input, '2026-04-20');
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.dob).toBe('2026-04-20');
  });

  it('yesNo records true / false', async () => {
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(
      <SurveyRenderer
        schema={schemaWith({ type: 'yesNo', id: 'smokes', title: { en: 'Smokes?' } })}
        onSubmit={onSubmit}
      />,
    );
    const user = userEvent.setup();
    await user.click(screen.getByRole('radio', { name: 'No' }));
    await advance();
    await waitFor(() => expect(onSubmit).toHaveBeenCalled());
    expect(onSubmit.mock.calls[0]![0].answers.smokes).toBe(false);
  });

  it('unknown type renders a placeholder, not a crash', () => {
    const onSubmit = vi.fn();
    render(
      <SurveyRenderer
        schema={schemaWith({ type: 'notARealType', id: 'x', title: { en: 'X' } })}
        onSubmit={onSubmit}
      />,
    );
    expect(screen.getByText(/Unsupported question type: notARealType/)).toBeInTheDocument();
  });
});
