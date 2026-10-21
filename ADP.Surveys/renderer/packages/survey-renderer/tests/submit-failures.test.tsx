import { describe, expect, it, vi } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { SurveyClientError, type Survey } from '@shift-software/survey-sdk';
import { SurveyRenderer } from '../src/SurveyRenderer.js';

/** One question, then a zero-question closing screen that submits on arrival. */
function fixture(): Survey {
  return {
    id: 's',
    version: 1,
    defaultLocale: 'en',
    locales: ['en'],
    screens: [
      {
        id: 'rate',
        title: { en: 'Rate us' },
        questions: [{ type: 'nps', id: 'nps', title: { en: 'Score' }, required: true }],
        nextScreen: 'thanks',
      },
      { id: 'thanks', title: { en: 'Thank you for your feedback!' }, questions: [] },
    ],
  };
}

function clientError(status: number, code: SurveyClientError['code']) {
  return new SurveyClientError({
    status,
    code,
    message: `POST /SurveyInstances/abc/responses → ${status}: server wording`,
  });
}

async function answerAndSubmit() {
  const user = userEvent.setup();
  await user.click(screen.getByRole('radio', { name: '8' }));
  await user.click(screen.getByRole('button', { name: 'Submit' }));
  return user;
}

describe('submitting from a closing screen', () => {
  it('reads "sending" until the answers are accepted, and only then shows the thank-you', async () => {
    let accept!: () => void;
    const onSubmit = vi.fn(() => new Promise<void>((resolve) => (accept = resolve)));
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    await answerAndSubmit();

    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(1));
    expect(screen.getByRole('status')).toHaveTextContent('Sending your answers…');
    expect(screen.queryByText('Thank you for your feedback!')).not.toBeInTheDocument();

    accept();
    expect(await screen.findByRole('heading', { name: 'Thank you for your feedback!' })).toBeInTheDocument();
  });

  it('an already-answered survey (409) ends calmly, without the thank-you or the server wording', async () => {
    const error = clientError(409, 'conflict');
    const onSubmit = vi.fn().mockRejectedValue(error);
    const onSubmitError = vi.fn();
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} onSubmitError={onSubmitError} />);
    await answerAndSubmit();

    expect(await screen.findByRole('heading', { name: 'Already answered' })).toBeInTheDocument();
    expect(screen.getByText(/already received your answers/)).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();
    expect(screen.queryByText(/server wording|409/)).not.toBeInTheDocument();
    expect(screen.queryByText('Thank you for your feedback!')).not.toBeInTheDocument();
    expect(onSubmitError).toHaveBeenCalledWith(error);
  });

  it('a closed survey (410) ends with the closed message', async () => {
    const onSubmit = vi.fn().mockRejectedValue(clientError(410, 'gone'));
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    await answerAndSubmit();

    expect(await screen.findByRole('heading', { name: 'This survey has closed' })).toBeInTheDocument();
    expect(screen.queryByText(/server wording/)).not.toBeInTheDocument();
  });

  it('a connection failure offers Retry, which sends the same answers again', async () => {
    const onSubmit = vi
      .fn()
      .mockRejectedValueOnce(new SurveyClientError({ status: 0, code: 'network', message: 'Failed to fetch' }))
      .mockResolvedValueOnce(undefined);
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = await answerAndSubmit();

    expect(await screen.findByRole('alert')).toHaveTextContent('We could not send your answers.');
    expect(screen.queryByText(/Failed to fetch/)).not.toBeInTheDocument();
    expect(screen.queryByText('Thank you for your feedback!')).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Retry' }));
    expect(await screen.findByRole('heading', { name: 'Thank you for your feedback!' })).toBeInTheDocument();
    expect(onSubmit).toHaveBeenCalledTimes(2);
    expect(onSubmit.mock.calls[1]![0].answers).toEqual(onSubmit.mock.calls[0]![0].answers);
  });

  it("a host's own exception is treated as worth retrying", async () => {
    const onSubmit = vi.fn().mockRejectedValue(new Error('boom'));
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    await answerAndSubmit();

    expect(await screen.findByRole('button', { name: 'Retry' })).toBeInTheDocument();
    expect(screen.queryByText(/boom/)).not.toBeInTheDocument();
  });

  it('a refused submission (400) has no Retry, but Back is offered so the respondent is not stranded', async () => {
    const onSubmit = vi.fn().mockRejectedValue(clientError(400, 'badRequest'));
    render(<SurveyRenderer schema={fixture()} onSubmit={onSubmit} />);
    const user = await answerAndSubmit();

    expect(await screen.findByRole('alert')).toHaveTextContent('Something went wrong');
    expect(screen.queryByRole('button', { name: 'Retry' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Back' }));
    expect(screen.getByRole('heading', { name: 'Rate us' })).toBeInTheDocument();
    expect(screen.queryByRole('alert')).not.toBeInTheDocument();

    // Walking back to the closing screen submits again.
    await user.click(screen.getByRole('button', { name: 'Submit' }));
    await waitFor(() => expect(onSubmit).toHaveBeenCalledTimes(2));
  });
});

describe('submitting from a question screen', () => {
  it('a failure shows the friendly message under the Submit button, never the server wording', async () => {
    const schema: Survey = {
      id: 's',
      version: 1,
      defaultLocale: 'en',
      locales: ['en'],
      screens: [
        {
          id: 'rate',
          title: { en: 'Rate us' },
          questions: [{ type: 'nps', id: 'nps', title: { en: 'Score' }, required: true }],
        },
      ],
    };
    const onSubmit = vi.fn().mockRejectedValue(clientError(500, 'server'));
    render(<SurveyRenderer schema={schema} onSubmit={onSubmit} />);
    await answerAndSubmit();

    expect(await screen.findByRole('alert')).toHaveTextContent('We could not send your answers.');
    expect(screen.queryByText(/server wording/)).not.toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Rate us' })).toBeInTheDocument();
  });
});
