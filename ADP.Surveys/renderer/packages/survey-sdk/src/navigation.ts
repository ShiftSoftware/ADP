/**
 * Next-button navigation engine. Wraps `evaluateNext` with the full fallback chain
 * per the Phase 3 plan's Part C.1: logic rule → `screen.nextScreen` → sequential
 * array order → end of survey. Unresolvable targets (a `goto` or `nextScreen`
 * pointing at a screen id that doesn't exist) fall through rather than throw, so a
 * stale rule never blocks the survey.
 */

import type { AnswerMap, NavigationOption, Survey } from './schema.js';
import { evaluateNext } from './logic-evaluator.js';

export type NextStep =
  | { kind: 'screen'; screenId: string }
  | { kind: 'end' };

/** Compute what to show after the user taps Next on `currentScreenId`.
 *  Priority: cross-screen logic rule → screen.nextScreen → next screen by array
 *  order → end of survey. Unknown screen ids fall through to the next tier. */
export function computeNext(
  schema: Survey,
  currentScreenId: string,
  answers: AnswerMap,
): NextStep {
  const screenIds = new Set(schema.screens.map((s) => s.id));

  // 1. Logic rule. A rule pointing back at the current screen is a no-op —
  // after the rule fires it has served its purpose and we should move on.
  // Without this guard, a rule like `nps <= 6 → thanks-default` keeps re-
  // firing once the user arrives on thanks-default, trapping navigation.
  const logicTarget = evaluateNext(schema, answers);
  if (logicTarget && logicTarget !== currentScreenId && screenIds.has(logicTarget)) {
    return { kind: 'screen', screenId: logicTarget };
  }

  // 2. Current screen's explicit nextScreen.
  const current = schema.screens.find((s) => s.id === currentScreenId);
  if (current?.nextScreen && current.nextScreen !== currentScreenId && screenIds.has(current.nextScreen)) {
    return { kind: 'screen', screenId: current.nextScreen };
  }

  // 2b. Zero-question screens without an explicit nextScreen are terminal.
  // Author intent: a screen with no questions and no onward path is the end of
  // the flow. Sequential fallback would chain into the next thanks screen — which
  // is never what authors want when they have per-branch thank-you pages. A
  // splash screen that needs to chain must set `nextScreen` explicitly.
  if (current && (!current.questions || current.questions.length === 0) && !current.nextScreen) {
    return { kind: 'end' };
  }

  // 3. Sequential array order.
  const idx = schema.screens.findIndex((s) => s.id === currentScreenId);
  if (idx >= 0 && idx + 1 < schema.screens.length) {
    const next = schema.screens[idx + 1];
    if (next) return { kind: 'screen', screenId: next.id };
  }

  // 4. End of survey.
  return { kind: 'end' };
}

/** Resolve the destination when a user selects a `navigationList` option.
 *  Per Phase 3 Part C.2: option.nextScreen wins if it resolves. Otherwise fall
 *  back to `computeNext` so the author still gets a valid next screen. */
export function resolveNavigationListTarget(
  option: NavigationOption,
  schema: Survey,
  currentScreenId: string,
  answers: AnswerMap,
): NextStep {
  const screenIds = new Set(schema.screens.map((s) => s.id));
  if (option.nextScreen && screenIds.has(option.nextScreen)) {
    return { kind: 'screen', screenId: option.nextScreen };
  }
  return computeNext(schema, currentScreenId, answers);
}
