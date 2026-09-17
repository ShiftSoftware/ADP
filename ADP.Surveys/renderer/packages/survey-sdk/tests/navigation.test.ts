import { describe, expect, it } from 'vitest';
import { computeNext, replayPathTo, resolveNavigationListTarget } from '../src/navigation.js';
import type { AnswerMap, Survey } from '../src/schema.js';

const noAnswers: AnswerMap = {};

function makeSchema(overrides: Partial<Survey> = {}): Survey {
  return {
    id: 's',
    screens: [
      { id: 'welcome', nextScreen: 'feedback', questions: [{ type: 'text', id: 'name' }] },
      { id: 'feedback', nextScreen: 'brand', questions: [{ type: 'nps', id: 'nps' }] },
      { id: 'brand', questions: [{ type: 'navigationList', id: 'b' }] },
      { id: 'thanks-toyota' },
      { id: 'thanks-default' },
    ],
    ...overrides,
  };
}

describe('computeNext', () => {
  it('logic rule wins over screen.nextScreen', () => {
    const schema = makeSchema({
      logic: [{ if: { questionId: 'nps', op: '<=', value: 6 }, then: { goto: 'thanks-default' } }],
    });
    const step = computeNext(schema, 'feedback', { nps: 3 });
    expect(step).toEqual({ kind: 'screen', screenId: 'thanks-default' });
  });

  it('screen.nextScreen wins when no logic rule matches', () => {
    const schema = makeSchema({
      logic: [{ if: { questionId: 'nps', op: '<=', value: 6 }, then: { goto: 'thanks-default' } }],
    });
    const step = computeNext(schema, 'feedback', { nps: 9 });
    expect(step).toEqual({ kind: 'screen', screenId: 'brand' });
  });

  it('sequential order when no rule, no nextScreen', () => {
    const schema = makeSchema();
    const step = computeNext(schema, 'brand', noAnswers);
    expect(step).toEqual({ kind: 'screen', screenId: 'thanks-toyota' });
  });

  it('end of survey when on the last screen with nothing set', () => {
    const schema = makeSchema();
    const step = computeNext(schema, 'thanks-default', noAnswers);
    expect(step).toEqual({ kind: 'end' });
  });

  it('unknown logic goto target falls through (does not throw)', () => {
    const schema = makeSchema({
      logic: [{ if: { questionId: 'x', op: 'isSet' }, then: { goto: 'screen-that-does-not-exist' } }],
    });
    const step = computeNext(schema, 'feedback', { x: 'anything' });
    // Should fall through to screen.nextScreen
    expect(step).toEqual({ kind: 'screen', screenId: 'brand' });
  });

  it('unknown nextScreen on current screen falls through to sequential', () => {
    const schema = makeSchema({
      screens: [
        { id: 'a', nextScreen: 'ghost' },
        { id: 'b' },
      ],
    });
    const step = computeNext(schema, 'a', noAnswers);
    expect(step).toEqual({ kind: 'screen', screenId: 'b' });
  });

  it('current screen not found → end', () => {
    const schema = makeSchema();
    const step = computeNext(schema, 'not-in-this-survey', noAnswers);
    expect(step).toEqual({ kind: 'end' });
  });

  it('zero-question screen with no nextScreen is terminal (skips sequential)', () => {
    // Multi-thank-you-screen pattern: the navigationList option lands the user
    // on one of several terminal screens, none of which should chain into the
    // next terminal by array order. Without this guard the promoter seeing
    // "Thanks — Toyota fan!" would chain into "Thanks!" on Next.
    const schema = makeSchema({
      screens: [
        { id: 'q', nextScreen: 'thanks-a', questions: [{ type: 'text', id: 'a' }] },
        { id: 'thanks-a' },
        { id: 'thanks-b' },
      ],
    });
    expect(computeNext(schema, 'thanks-a', noAnswers)).toEqual({ kind: 'end' });
    expect(computeNext(schema, 'thanks-b', noAnswers)).toEqual({ kind: 'end' });
  });

  it('sticky logic rule cannot drag navigation out of a zero-question end screen', () => {
    // A global rule keeps matching the final answer map forever. Once the
    // respondent reaches a terminal screen, re-firing it would strand the
    // survey: auto-submit sees "next is a screen" and never runs, and the
    // Next button is hidden on zero-question screens. Shape from the
    // "Service Visit Feedback" demo: nps<=6 → recover, recover falls through
    // sequentially to a zero-question thanks.
    const schema = makeSchema({
      screens: [
        { id: 'overall', nextScreen: 'comments', questions: [{ type: 'nps', id: 'nps' }] },
        { id: 'comments', questions: [{ type: 'paragraph', id: 'open-comments' }] },
        { id: 'recover', questions: [{ type: 'paragraph', id: 'recover-details' }] },
        { id: 'thanks' },
      ],
      logic: [{ if: { questionId: 'nps', op: '<=', value: 6 }, then: { goto: 'recover' } }],
    });
    const lowScore: AnswerMap = { nps: 0, 'recover-details': 'it was slow' };
    // From recover the rule self-guards → sequential → thanks…
    expect(computeNext(schema, 'recover', lowScore)).toEqual({ kind: 'screen', screenId: 'thanks' });
    // …and thanks must END even though the rule still matches and points elsewhere.
    expect(computeNext(schema, 'thanks', lowScore)).toEqual({ kind: 'end' });
  });

  it('zero-question splash screen WITH explicit nextScreen still chains', () => {
    const schema = makeSchema({
      screens: [
        { id: 'splash', nextScreen: 'q1' },
        { id: 'q1', questions: [{ type: 'text', id: 'a' }] },
      ],
    });
    expect(computeNext(schema, 'splash', noAnswers)).toEqual({ kind: 'screen', screenId: 'q1' });
  });

  it('logic rule that points back at the current screen is ignored (no loop)', () => {
    // Without this guard a rule like `nps<=6 → thanks-default` would re-fire
    // forever once the user arrives on thanks-default, trapping navigation.
    const schema = makeSchema({
      screens: [
        { id: 'a', nextScreen: 'b', questions: [{ type: 'text', id: 'q1' }] },
        { id: 'b', questions: [{ type: 'text', id: 'q2' }] },
        { id: 'done' },
      ],
      logic: [{ if: { questionId: 'flag', op: '==', value: true }, then: { goto: 'b' } }],
    });
    const step = computeNext(schema, 'b', { flag: true });
    // Rule target equals current — falls through to sequential → 'done'.
    expect(step).toEqual({ kind: 'screen', screenId: 'done' });
  });
});

describe('resolveNavigationListTarget', () => {
  const schema = makeSchema();

  it("option.nextScreen short-circuits computeNext", () => {
    const step = resolveNavigationListTarget(
      { id: 'toyota', nextScreen: 'thanks-toyota' },
      schema,
      'brand',
      noAnswers,
    );
    expect(step).toEqual({ kind: 'screen', screenId: 'thanks-toyota' });
  });

  it('option without nextScreen delegates to computeNext', () => {
    const step = resolveNavigationListTarget(
      { id: 'oops' },
      schema,
      'brand',
      noAnswers,
    );
    // brand has no nextScreen; sequential is thanks-toyota
    expect(step).toEqual({ kind: 'screen', screenId: 'thanks-toyota' });
  });

  it('option with unknown nextScreen falls through (never throws)', () => {
    const step = resolveNavigationListTarget(
      { id: 'x', nextScreen: 'ghost-screen' },
      schema,
      'brand',
      noAnswers,
    );
    expect(step).toEqual({ kind: 'screen', screenId: 'thanks-toyota' });
  });
});

describe('replayPathTo', () => {
  // The renderer's fallback for resume state that predates persisted histories:
  // rebuild the screens walked from the answers, the way the server replays them.
  const branching = (): Survey => ({
    id: 's',
    screens: [
      { id: 'welcome', questions: [{ type: 'text', id: 'name' }] },
      {
        id: 'menu',
        questions: [
          {
            type: 'navigationList',
            id: 'intent',
            options: [
              { id: 'a', nextScreen: 'branch-a' },
              { id: 'b', nextScreen: 'branch-b' },
            ],
          },
        ],
      },
      { id: 'branch-a', questions: [{ type: 'text', id: 'a-note' }], nextScreen: 'city' },
      { id: 'branch-b', questions: [{ type: 'text', id: 'b-note' }], nextScreen: 'city' },
      { id: 'city', questions: [{ type: 'text', id: 'city' }] },
      { id: 'done' },
    ],
    logic: [{ if: { questionId: 'a-note', op: '==', value: 'vip' }, then: { goto: 'done' } }],
  });

  it('is empty for the first screen', () => {
    expect(replayPathTo(branching(), {}, 'welcome')).toEqual([]);
  });

  it('follows sequential order, then an answered navigationList option', () => {
    expect(replayPathTo(branching(), { intent: 'b' }, 'city')).toEqual(['welcome', 'menu', 'branch-b']);
  });

  it('applies logic rules on the way, like computeNext — against the final answers, as the server does', () => {
    // A global rule matches on every screen once the final answer map satisfies
    // it, so the replay leaves `welcome` for `done` directly. That is the same
    // approximation `AnswerValidator.ComputeVisitedScreens` makes.
    expect(replayPathTo(branching(), { intent: 'a', 'a-note': 'vip' }, 'done')).toEqual(['welcome']);
    expect(replayPathTo(branching(), { intent: 'a', 'a-note': 'vip' }, 'branch-a')).toBeNull();
  });

  it('routes a sourced navigationList on the source nextScreen', () => {
    const schema = branching();
    schema.screens[1] = {
      id: 'menu',
      questions: [{ type: 'navigationList', id: 'intent', options: [], optionsSource: { url: 'https://x.test', nextScreen: 'branch-b' } }],
    };
    expect(replayPathTo(schema, { intent: 'anything-fetched' }, 'city')).toEqual(['welcome', 'menu', 'branch-b']);
  });

  it('is null when the target is not reached: unanswered menu, past the end, or a loop', () => {
    // An unanswered navigationList falls through to sequential order: menu → branch-a → city.
    expect(replayPathTo(branching(), {}, 'branch-b')).toBeNull();
    // The walk ends at `done` (zero-question terminal) before reaching a screen that comes after it.
    const schema = branching();
    schema.screens.push({ id: 'unreachable' });
    expect(replayPathTo(schema, { intent: 'a', 'a-note': 'vip' }, 'unreachable')).toBeNull();
    // A cycle stops on the first repeat.
    const loop: Survey = {
      id: 'l',
      screens: [
        { id: 'a', questions: [{ type: 'text', id: 'x' }], nextScreen: 'b' },
        { id: 'b', questions: [{ type: 'text', id: 'y' }], nextScreen: 'a' },
        { id: 'c', questions: [{ type: 'text', id: 'z' }] },
      ],
    };
    expect(replayPathTo(loop, {}, 'c')).toBeNull();
  });
});
