import { describe, expect, it } from 'vitest';
import { computeNext, resolveNavigationListTarget } from '../src/navigation.js';
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
