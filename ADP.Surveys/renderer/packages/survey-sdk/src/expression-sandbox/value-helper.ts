/**
 * Value-domain helpers shared by the interpreter and the predicate-based logic
 * evaluator. Mirror of `ADP.Surveys.Shared/Evaluation/JsonValueHelper.cs`.
 *
 * Runtime value domain: number | string | boolean | null | unknown[]. JSON answer
 * values arrive already-unwrapped (since JS JSON.parse yields these types), so
 * unlike the C# side there is no `JsonElement` unwrap step — `unwrap` is the
 * identity on the already-unwrapped shape with a defensive guard for plain objects.
 */

export type SandboxValue = number | string | boolean | null | unknown[];

export function unwrap(v: unknown): unknown {
  if (v === undefined) return null;
  if (v === null) return null;
  if (typeof v === 'boolean') return v;
  if (typeof v === 'number') return v;
  if (typeof v === 'string') return v;
  if (Array.isArray(v)) return v.map(unwrap);
  // Plain objects have no meaningful comparison in the expression language.
  if (typeof v === 'object') return null;
  return null;
}

export function areEqual(left: unknown, right: unknown): boolean {
  const l = unwrap(left);
  const r = unwrap(right);
  if (l === null || r === null) return l === null && r === null;
  if (typeof l === 'number' && typeof r === 'number') return l === r;
  if (typeof l === 'string' && typeof r === 'string') return l === r;
  if (typeof l === 'boolean' && typeof r === 'boolean') return l === r;
  if (Array.isArray(l) && Array.isArray(r)) {
    if (l.length !== r.length) return false;
    for (let i = 0; i < l.length; i++) if (!areEqual(l[i], r[i])) return false;
    return true;
  }
  return false;
}

/**
 * Returns -1 / 0 / 1 for number-vs-number and string-vs-string comparisons.
 * Throws for mismatched types — comparison across types is ambiguous, and logic-rule
 * authors should make intent explicit. Matches C# `JsonValueHelper.Compare`.
 */
export function compare(left: unknown, right: unknown): number {
  const l = unwrap(left);
  const r = unwrap(right);
  if (typeof l === 'number' && typeof r === 'number') {
    if (l < r) return -1;
    if (l > r) return 1;
    return 0;
  }
  if (typeof l === 'string' && typeof r === 'string') {
    if (l < r) return -1;
    if (l > r) return 1;
    return 0;
  }
  throw new Error('Comparison operators require two numbers or two strings.');
}

export function toBool(value: unknown): boolean {
  const v = unwrap(value);
  if (v === null) return false;
  if (typeof v === 'boolean') return v;
  if (typeof v === 'number') return v !== 0;
  if (typeof v === 'string') return v.length > 0;
  if (Array.isArray(v)) return v.length > 0;
  return true;
}
