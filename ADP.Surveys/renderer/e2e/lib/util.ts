import { execFileSync } from 'node:child_process';

export const API = 'http://localhost:5134';
export const API_SURVEYS = `${API}/api/Surveys`;
export const DB_SERVER = String.raw`localhost\sqlexpress`;
export const DB_NAME = 'SurveysSample';
export const USERNAME = 'SuperUser';
export const PASSWORD = 'OneTwo';

export function step(name: string, fn: () => Promise<void> | void) {
  return async () => {
    const t0 = Date.now();
    try {
      await fn();
      console.log(`  ✔ ${name}  (${Date.now() - t0}ms)`);
    } catch (e) {
      console.log(`  ✘ ${name}  (${Date.now() - t0}ms)`);
      throw e;
    }
  };
}

export function assert(cond: unknown, message: string): asserts cond {
  if (!cond) throw new Error(`Assertion failed: ${message}`);
}

export function assertEq<T>(actual: T, expected: T, message: string) {
  if (actual !== expected) {
    throw new Error(
      `Assertion failed: ${message} — expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`,
    );
  }
}

export async function api(
  method: string,
  path: string,
  init: { token?: string; body?: unknown } = {},
): Promise<Response> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (init.token) headers['Authorization'] = `Bearer ${init.token}`;
  return fetch(`${API}${path}`, {
    method,
    headers,
    body: init.body === undefined ? undefined : JSON.stringify(init.body),
  });
}

export async function apiJson<T = unknown>(
  method: string,
  path: string,
  init: { token?: string; body?: unknown } = {},
): Promise<T> {
  const r = await api(method, path, init);
  const text = await r.text();
  if (!r.ok) throw new Error(`${method} ${path} → ${r.status}: ${text.slice(0, 500)}`);
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

/** sqlcmd wrapper. `-h -1` strips headers, `-W` trims whitespace, `-s "\t"` picks a
 *  tab separator so multi-column output can be `split('\t')` cleanly.
 *  The SET prefix matters: sqlcmd's defaults (QUOTED_IDENTIFIER OFF among others) reject
 *  any INSERT/UPDATE that touches a filtered index (msg 1934) — and SurveyInstance has
 *  two of those (UniqueHash unique index + the scheduler's Status/NextSendAt index). */
const FILTERED_INDEX_SET_OPTIONS =
  'SET ANSI_NULLS, ANSI_PADDING, ANSI_WARNINGS, ARITHABORT, CONCAT_NULL_YIELDS_NULL, QUOTED_IDENTIFIER ON; SET NUMERIC_ROUNDABORT OFF;';

export function sql(query: string): string {
  try {
    return execFileSync(
      'sqlcmd',
      ['-S', DB_SERVER, '-E', '-d', DB_NAME, '-h', '-1', '-W', '-s', '\t', '-Q', `SET NOCOUNT ON; ${FILTERED_INDEX_SET_OPTIONS} ${query}`],
      { encoding: 'utf8' },
    ).trim();
  } catch (e) {
    const err = e as { stdout?: Buffer; stderr?: Buffer; message?: string };
    throw new Error(
      `sqlcmd failed: ${err.message ?? ''}\n${err.stdout?.toString() ?? ''}\n${err.stderr?.toString() ?? ''}`,
    );
  }
}
