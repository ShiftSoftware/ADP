/** Resolve the API base URL in this priority:
 *   1. `?api=` query-string override (dev-only escape hatch)
 *   2. `VITE_API_BASE` compile-time env var
 *   3. Hard-coded dev default.
 *
 * Trailing slashes are trimmed so the SDK can append `/SurveyInstances/...` cleanly.
 */
export function resolveApiBase(): string {
  const search = typeof window !== 'undefined' ? window.location.search : '';
  const params = new URLSearchParams(search);
  const override = params.get('api');
  if (override) return override.replace(/\/+$/, '');

  const envBase = import.meta.env.VITE_API_BASE as string | undefined;
  if (envBase) return envBase.replace(/\/+$/, '');

  return 'http://localhost:5134/api/Surveys';
}

/** Parse `/s/:publicId` from the current path. Returns null for any other path. */
export function parseSurveyRoute(): { publicId: string } | null {
  if (typeof window === 'undefined') return null;
  const match = window.location.pathname.match(/^\/s\/([^/]+)\/?$/);
  if (!match || !match[1]) return null;
  return { publicId: decodeURIComponent(match[1]) };
}

/** Pick up an optional `?locale=` override. Falls through to the schema's
 *  `defaultLocale` in the renderer when absent. */
export function resolveLocale(): string | undefined {
  if (typeof window === 'undefined') return undefined;
  const params = new URLSearchParams(window.location.search);
  return params.get('locale') ?? undefined;
}

/** `?mode=agent` flag — recorded on the response meta per Decision #7. */
export function isAgentMode(): boolean {
  if (typeof window === 'undefined') return false;
  return new URLSearchParams(window.location.search).get('mode') === 'agent';
}
