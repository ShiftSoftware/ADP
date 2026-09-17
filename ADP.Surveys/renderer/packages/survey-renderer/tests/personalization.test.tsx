import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';
import { clearSourcedOptionsCache } from '../src/questions/SourcedOptionsGate.js';

/**
 * Answer references end to end through the renderer: copy on the current and the
 * next screen, option labels, and a sourced question whose request depends on an
 * earlier answer. The engine itself is covered in the SDK's personalization tests;
 * this pins that the renderer actually feeds it and re-renders on every answer.
 */

function okJson(body: unknown) {
  return { ok: true, status: 200, json: async () => body } as Response;
}

function schema(): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    variables: [{ name: 'answers.name', fallback: { en: 'friend' } }],
    screens: [
      {
        id: 'intro',
        title: { en: 'Hello {{answers.name}}' },
        description: { en: 'Brand: {{answers.brand.label|not picked yet}}' },
        questions: [
          { type: 'text', id: 'name', title: { en: 'Your name' } },
          {
            type: 'singleChoice',
            id: 'brand',
            title: { en: 'Brand for {{answers.name|you}}' },
            options: [
              { id: 'toyota', label: { en: 'Toyota' } },
              { id: 'lexus', label: { en: 'Lexus' } },
            ],
          },
        ],
        nextScreen: 'model',
      },
      {
        id: 'model',
        title: { en: '{{answers.brand.label}} models for {{answers.name}}' },
        questions: [
          {
            type: 'dropdown',
            id: 'model',
            title: { en: 'Model' },
            optionsSource: {
              url: 'https://api.test/models/{{answers.brand}}',
              queryParams: { who: '{{answers.name}}' },
              method: 'POST',
              body: '{"brand":"{{answers.brand}}","name":"{{answers.name}}"}',
            },
          },
        ],
        nextScreen: 'done',
      },
      { id: 'done', title: { en: 'You picked {{answers.model.label}} ({{answers.model}})' }, questions: [] },
    ],
    logic: [],
  };
}

describe('answer references in the renderer', () => {
  beforeEach(() => clearSourcedOptionsCache());
  afterEach(() => vi.unstubAllGlobals());

  it('fills copy live on the same screen, from the declared fallback until the answer exists', async () => {
    render(<SurveyRenderer schema={schema()} onSubmit={vi.fn()} />);
    // No answer yet → the declared fallback; the description's inline fallback wins for brand.
    expect(screen.getByRole('heading', { name: 'Hello friend' })).toBeInTheDocument();
    expect(screen.getByText('Brand: not picked yet')).toBeInTheDocument();
    expect(screen.getByText('Brand for you')).toBeInTheDocument();

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Your name'), 'Sara');
    expect(screen.getByRole('heading', { name: 'Hello Sara' })).toBeInTheDocument();
    expect(screen.getByText('Brand for Sara')).toBeInTheDocument();

    await user.click(screen.getByLabelText('Lexus'));
    expect(screen.getByText('Brand: Lexus')).toBeInTheDocument();
  });

  it('feeds earlier answers into the next screen, into a sourced request, and labels the pick afterwards', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okJson([{ ID: 'lx', Name: 'LX 600' }]));
    vi.stubGlobal('fetch', fetchMock);
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={schema()} onSubmit={onSubmit} />);

    const user = userEvent.setup();
    await user.type(screen.getByLabelText('Your name'), 'Sara "S"');
    await user.click(screen.getByLabelText('Lexus'));
    await user.click(screen.getByRole('button', { name: 'Next' }));

    expect(screen.getByRole('heading', { name: 'Lexus models for Sara "S"' })).toBeInTheDocument();
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    const [url, init] = fetchMock.mock.calls[0] as [string, RequestInit];
    // Path token percent-encoded by the substitution, query value encoded by the URL builder.
    expect(url).toBe('https://api.test/models/lexus?who=Sara+%22S%22');
    expect(init.method).toBe('POST');
    expect(init.body).toBe('{"brand":"lexus","name":"Sara \\"S\\""}');
    expect(init.headers).toEqual({ 'Accept-Language': 'en', 'Content-Type': 'application/json' });

    await waitFor(() => expect(screen.getByRole('combobox')).toBeInTheDocument());
    await user.selectOptions(screen.getByRole('combobox'), 'lx');
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    // The end screen names the fetched label even though the sourced question is gone.
    expect(screen.getByRole('heading', { name: 'You picked LX 600 (lx)' })).toBeInTheDocument();
    expect(onSubmit.mock.calls[0]![0].answers).toMatchObject({ brand: 'lexus', model: 'lx' });
  });

  it('refetches a sourced question when an answer its request depends on changes', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okJson([{ ID: 'x', Name: 'X' }]));
    vi.stubGlobal('fetch', fetchMock);
    const twoQuestions: Survey = {
      ...schema(),
      screens: [
        {
          id: 'one',
          questions: [
            {
              type: 'singleChoice',
              id: 'brand',
              title: { en: 'Brand' },
              options: [
                { id: 'toyota', label: { en: 'Toyota' } },
                { id: 'lexus', label: { en: 'Lexus' } },
              ],
            },
            {
              type: 'dropdown',
              id: 'model',
              title: { en: 'Model' },
              optionsSource: { url: 'https://api.test/models', queryParams: { brand: '{{answers.brand}}' } },
            },
          ],
        },
      ],
    };
    render(<SurveyRenderer schema={twoQuestions} onSubmit={vi.fn()} />);
    // Unanswered → the parameter is sent empty, never as raw braces.
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1));
    expect(fetchMock.mock.calls[0]![0]).toBe('https://api.test/models?brand=');

    const user = userEvent.setup();
    await user.click(screen.getByLabelText('Toyota'));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(2));
    expect(fetchMock.mock.calls[1]![0]).toBe('https://api.test/models?brand=toyota');

    await user.click(screen.getByLabelText('Lexus'));
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(3));
    expect(fetchMock.mock.calls[2]![0]).toBe('https://api.test/models?brand=lexus');

    // Back to a request already seen → served from the session cache, no fourth call.
    await user.click(screen.getByLabelText('Toyota'));
    await waitFor(() => expect(screen.getByRole('combobox')).toBeInTheDocument());
    expect(fetchMock).toHaveBeenCalledTimes(3);
  });
});
