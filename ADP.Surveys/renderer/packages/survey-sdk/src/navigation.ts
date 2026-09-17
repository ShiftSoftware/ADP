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
  const current = schema.screens.find((s) => s.id === currentScreenId);

  // 0. Zero-question screens without an explicit nextScreen are ABSOLUTELY
  // terminal — checked before the logic tier on purpose. A sticky global rule
  // (e.g. `nps <= 6 → recover`) matches the final answer map forever, so once
  // the respondent reaches an end screen the rule would otherwise re-route
  // them out of it: auto-submit sees "next is a screen" and never fires, the
  // Next button is hidden on such screens, and the survey strands with no
  // submission. Author intent: a screen with no questions and no onward path
  // is the end of the flow, full stop. (Sequential fallback is skipped for
  // the same reason — per-branch thank-you screens must not chain. A splash
  // screen that needs to chain sets `nextScreen` explicitly.)
  if (current && (!current.questions || current.questions.length === 0) && !current.nextScreen) {
    return { kind: 'end' };
  }

  // 1. Logic rule. A rule pointing back at the current screen is a no-op —
  // after the rule fires it has served its purpose and we should move on.
  // Without this guard, a rule like `nps <= 6 → thanks-default` keeps re-
  // firing once the user arrives on thanks-default, trapping navigation.
  const logicTarget = evaluateNext(schema, answers);
  if (logicTarget && logicTarget !== currentScreenId && screenIds.has(logicTarget)) {
    return { kind: 'screen', screenId: logicTarget };
  }

  // 2. Current screen's explicit nextScreen.
  if (current?.nextScreen && current.nextScreen !== currentScreenId && screenIds.has(current.nextScreen)) {
    return { kind: 'screen', screenId: current.nextScreen };
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

/**
 * The screens a respondent passed through to reach `targetScreenId`, oldest
 * first, replayed from the first screen against the answers they hold — the
 * SDK twin of the server's `AnswerValidator.ComputeVisitedScreens` walk: an
 * answered `navigationList` dispatches on its option (a sourced one on the
 * source's shared `nextScreen`), everything else follows `computeNext`. Returns
 * `null` when the walk ends or loops before reaching the target, so a caller
 * can tell "no path" from "the target is the first screen".
 *
 * The renderer uses it to rebuild a navigation history for resume state saved
 * before histories were persisted; a live walk keeps its own exact stack.
 */
export function replayPathTo(schema: Survey, answers: AnswerMap, targetScreenId: string): string[] | null {
  const path: string[] = [];
  const seen = new Set<string>();
  let current: string | undefined = schema.screens[0]?.id;
  while (current !== undefined && !seen.has(current)) {
    if (current === targetScreenId) return path;
    seen.add(current);
    path.push(current);
    const step = replayStep(schema, current, answers);
    if (step.kind === 'end') return null;
    current = step.screenId;
  }
  return null;
}

/** One replay hop: navigationList dispatch first, then the Next-button chain. */
function replayStep(schema: Survey, screenId: string, answers: AnswerMap): NextStep {
  const screenIds = new Set(schema.screens.map((s) => s.id));
  const screen = schema.screens.find((s) => s.id === screenId);
  for (const raw of (screen?.questions ?? []) as Array<Record<string, unknown>>) {
    if (raw['type'] !== 'navigationList') continue;
    const picked = answers[raw['id'] as string];
    if (typeof picked !== 'string') continue;
    const options = (raw['options'] as NavigationOption[] | undefined) ?? [];
    const option = options.find((o) => o.id === picked);
    if (option?.nextScreen && screenIds.has(option.nextScreen)) {
      return { kind: 'screen', screenId: option.nextScreen };
    }
    // A sourced navigationList has no inline options — every fetched option
    // shares the source's nextScreen, so any answer routes there.
    const sourced = (raw['optionsSource'] as { nextScreen?: string } | undefined)?.nextScreen;
    if (!option && sourced && screenIds.has(sourced)) {
      return { kind: 'screen', screenId: sourced };
    }
  }
  return computeNext(schema, screenId, answers);
}
