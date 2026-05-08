import { describe, expect, it } from 'vitest';
import { SurveyClient, SurveyClientError } from '../src/client.js';
import type { Survey } from '../src/schema.js';

function mockFetch(
  handler: (url: string, init?: RequestInit) => Partial<Response> & { bodyText?: string },
) {
  return (async (url: RequestInfo | URL, init?: RequestInit) => {
    const u = typeof url === 'string' ? url : url.toString();
    const r = handler(u, init);
    const text = r.bodyText ?? '';
    return {
      ok: r.ok ?? true,
      status: r.status ?? 200,
      url: u,
      text: async () => text,
      json: async () => (text ? JSON.parse(text) : undefined),
    } as Response;
  }) as typeof fetch;
}

describe('SurveyClient.fetchSchema', () => {
  it('assembles URL correctly and parses body', async () => {
    let seenUrl = '';
    const client = new SurveyClient({
      baseUrl: 'http://api.example/api/Surveys',
      fetch: mockFetch((url) => {
        seenUrl = url;
        const survey: Survey = { id: 's', version: 1, screens: [{ id: 'w' }] };
        return { ok: true, status: 200, bodyText: JSON.stringify(survey) };
      }),
    });
    const schema = await client.fetchSchema('abc-123');
    expect(seenUrl).toBe('http://api.example/api/Surveys/SurveyInstances/abc-123/schema');
    expect(schema.id).toBe('s');
    expect(schema.screens[0]!.id).toBe('w');
  });

  it('strips trailing slashes from baseUrl', async () => {
    let seenUrl = '';
    const client = new SurveyClient({
      baseUrl: 'http://api.example/api/Surveys///',
      fetch: mockFetch((url) => {
        seenUrl = url;
        return { ok: true, status: 200, bodyText: '{"id":"s","screens":[]}' };
      }),
    });
    await client.fetchSchema('abc');
    expect(seenUrl).toBe('http://api.example/api/Surveys/SurveyInstances/abc/schema');
  });

  it('URL-encodes the publicId', async () => {
    let seenUrl = '';
    const client = new SurveyClient({
      baseUrl: 'http://api.example',
      fetch: mockFetch((url) => {
        seenUrl = url;
        return { ok: true, status: 200, bodyText: '{"id":"s","screens":[]}' };
      }),
    });
    await client.fetchSchema('a b/c?');
    expect(seenUrl).toBe('http://api.example/SurveyInstances/a%20b%2Fc%3F/schema');
  });
});

describe('SurveyClient error mapping', () => {
  const cases: Array<[number, SurveyClientError['code']]> = [
    [404, 'notFound'],
    [410, 'gone'],
    [409, 'conflict'],
    [400, 'badRequest'],
    [500, 'server'],
    [502, 'server'],
  ];
  for (const [status, code] of cases) {
    it(`HTTP ${status} → code=${code}`, async () => {
      const client = new SurveyClient({
        baseUrl: 'http://api.example',
        fetch: mockFetch(() => ({
          ok: false,
          status,
          bodyText: JSON.stringify({ Message: `fake ${status}` }),
        })),
      });
      await expect(client.fetchSchema('x')).rejects.toMatchObject({
        name: 'SurveyClientError',
        code,
        status,
        serverMessage: `fake ${status}`,
      });
    });
  }

  it('validation errors surface in a typed array', async () => {
    const client = new SurveyClient({
      baseUrl: 'http://api.example',
      fetch: mockFetch(() => ({
        ok: false,
        status: 400,
        bodyText: JSON.stringify({
          Message: 'Answer validation failed.',
          Errors: [
            { QuestionId: 'nps', Message: 'Value must be between 0 and 10.' },
            { QuestionId: 'brand', Message: 'Required.' },
          ],
        }),
      })),
    });
    await client.submitResponse('id', { schemaVersion: 1, answers: {} }).catch((e: SurveyClientError) => {
      expect(e.code).toBe('badRequest');
      expect(e.validationErrors).toEqual([
        { questionId: 'nps', message: 'Value must be between 0 and 10.' },
        { questionId: 'brand', message: 'Required.' },
      ]);
    });
  });

  it('non-JSON error body is preserved in raw', async () => {
    const client = new SurveyClient({
      baseUrl: 'http://api.example',
      fetch: mockFetch(() => ({ ok: false, status: 500, bodyText: '<html>oops</html>' })),
    });
    await client.fetchSchema('x').catch((e: SurveyClientError) => {
      expect(e.code).toBe('server');
      expect(e.raw).toBe('<html>oops</html>');
    });
  });

  it('network errors become code=network, status=0', async () => {
    const client = new SurveyClient({
      baseUrl: 'http://api.example',
      fetch: (async () => {
        throw new Error('ECONNREFUSED');
      }) as typeof fetch,
    });
    await client.fetchSchema('x').catch((e: SurveyClientError) => {
      expect(e.code).toBe('network');
      expect(e.status).toBe(0);
      expect(e.message).toContain('ECONNREFUSED');
    });
  });
});

describe('SurveyClient.submitResponse', () => {
  it('posts JSON body with the submission', async () => {
    let seenBody: string | undefined;
    let seenMethod: string | undefined;
    const client = new SurveyClient({
      baseUrl: 'http://api.example',
      fetch: mockFetch((_, init) => {
        seenMethod = init?.method;
        seenBody = init?.body as string | undefined;
        return { ok: true, status: 200, bodyText: '' };
      }),
    });
    await client.submitResponse('pub-id', {
      schemaVersion: 2,
      answers: { nps: 10 },
      meta: { completedAt: '2026-01-01T00:00:00Z' },
    });
    expect(seenMethod).toBe('POST');
    expect(seenBody).toBeDefined();
    const parsed = JSON.parse(seenBody!);
    expect(parsed.schemaVersion).toBe(2);
    expect(parsed.answers.nps).toBe(10);
    expect(parsed.meta.completedAt).toBe('2026-01-01T00:00:00Z');
  });
});

describe('SurveyClient.getStatus', () => {
  it('normalizes PascalCase and lowercase status fields', async () => {
    const client = new SurveyClient({
      baseUrl: 'http://api.example',
      fetch: mockFetch(() => ({
        ok: true,
        status: 200,
        bodyText: JSON.stringify({ Status: 'Completed', SchemaVersion: 3, TriggeredAt: '2026-01-01T00:00:00Z' }),
      })),
    });
    const s = await client.getStatus('id');
    expect(s.status).toBe('Completed');
    expect(s.schemaVersion).toBe(3);
    expect(s.triggeredAt).toBe('2026-01-01T00:00:00Z');
  });
});
