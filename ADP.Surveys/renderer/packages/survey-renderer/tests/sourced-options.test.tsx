import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import type { Survey } from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';
import { clearSourcedOptionsCache } from '../src/questions/SourcedOptionsGate.js';

const tiqBody = [
  { ID: 'b1', Name: 'Branch One' },
  { ID: 'b2', Name: 'Branch Two' },
];

function okJson(body: unknown) {
  return { ok: true, status: 200, json: async () => body } as Response;
}

function navListSchema(): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    screens: [
      {
        id: 'pick',
        title: { en: 'Pick a branch' },
        questions: [
          {
            type: 'navigationList',
            id: 'branch',
            title: { en: 'Branches' },
            optionsSource: {
              url: 'https://api.test/public/company-branch',
              queryParams: { services: 'body-and-paint' },
              nextScreen: 'thanks',
            },
          },
        ],
      },
      { id: 'thanks', title: { en: 'All done!' }, questions: [] },
    ],
    logic: [],
  };
}

describe('sourced options (optionsSource)', () => {
  beforeEach(() => clearSourcedOptionsCache());
  afterEach(() => vi.unstubAllGlobals());

  it('shows a loading state, renders fetched options, and routes taps via the source nextScreen', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okJson(tiqBody));
    vi.stubGlobal('fetch', fetchMock);
    const onSubmit = vi.fn().mockResolvedValue(undefined);
    render(<SurveyRenderer schema={navListSchema()} onSubmit={onSubmit} />);

    expect(screen.getByText('Loading options…')).toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /Branch One/ })).toBeInTheDocument(),
    );
    // The variation params travel on the request; locale rides Accept-Language.
    expect(fetchMock).toHaveBeenCalledWith(
      'https://api.test/public/company-branch?services=body-and-paint',
      expect.objectContaining({ headers: { 'Accept-Language': 'en' } }),
    );

    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: /Branch Two/ }));
    // 'thanks' is a terminal zero-question screen → auto-submits with the answer.
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(onSubmit.mock.calls[0]![0].answers).toMatchObject({ branch: 'b2' });
    expect(screen.getByRole('heading', { name: 'All done!' })).toBeInTheDocument();
  });

  it('renders an inline error with Retry; a successful retry recovers', async () => {
    const fetchMock = vi
      .fn()
      .mockRejectedValueOnce(new Error('network down'))
      .mockResolvedValue(okJson(tiqBody));
    vi.stubGlobal('fetch', fetchMock);
    render(<SurveyRenderer schema={navListSchema()} onSubmit={vi.fn()} />);

    await waitFor(() => expect(screen.getByText('Could not load the options.')).toBeInTheDocument());
    const user = userEvent.setup();
    await user.click(screen.getByRole('button', { name: 'Retry' }));
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /Branch One/ })).toBeInTheDocument(),
    );
    expect(fetchMock).toHaveBeenCalledTimes(2);
  });

  it('serves repeat mounts from the session cache (one fetch per url+locale)', async () => {
    const fetchMock = vi.fn().mockResolvedValue(okJson(tiqBody));
    vi.stubGlobal('fetch', fetchMock);
    const first = render(<SurveyRenderer schema={navListSchema()} onSubmit={vi.fn()} />);
    await waitFor(() =>
      expect(screen.getByRole('button', { name: /Branch One/ })).toBeInTheDocument(),
    );
    first.unmount();

    render(<SurveyRenderer schema={navListSchema()} onSubmit={vi.fn()} />);
    expect(screen.getByRole('button', { name: /Branch One/ })).toBeInTheDocument();
    expect(fetchMock).toHaveBeenCalledTimes(1);
  });

  it('materializes dropdown options from the endpoint', async () => {
    const fetchMock = vi.fn().mockResolvedValue(
      okJson([
        { ID: 'L0VEX', Name: 'Erbil' },
        { ID: 'MWjQ0', Name: 'Baghdad' },
      ]),
    );
    vi.stubGlobal('fetch', fetchMock);
    const schema: Survey = {
      id: 's',
      version: 1,
      defaultLocale: 'en',
      locales: ['en'],
      screens: [
        {
          id: 'one',
          title: { en: 'Where?' },
          questions: [
            {
              type: 'dropdown',
              id: 'city',
              title: { en: 'City' },
              optionsSource: { url: 'https://api.test/public/city' },
            },
          ],
        },
      ],
      logic: [],
    };
    render(<SurveyRenderer schema={schema} onSubmit={vi.fn()} />);
    await waitFor(() => expect(screen.getByRole('option', { name: 'Erbil' })).toBeInTheDocument());
    expect(screen.getByRole('option', { name: 'Baghdad' })).toBeInTheDocument();

    const user = userEvent.setup();
    await user.selectOptions(screen.getByRole('combobox'), 'MWjQ0');
    expect((screen.getByRole('combobox') as HTMLSelectElement).value).toBe('MWjQ0');
  });
});
