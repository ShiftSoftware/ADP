/**
 * Parity tests for the logic evaluator. Mirror of
 * `ADP.Surveys.Shared.Tests/LogicEvaluatorTests.cs`.
 */

import { describe, expect, it } from 'vitest';
import { evaluateCondition, evaluateNext } from '../src/logic-evaluator.js';
import type {
  AnswerMap,
  CompositeCondition,
  ExpressionCondition,
  LogicRule,
  PredicateCondition,
  Survey,
} from '../src/schema.js';

describe('predicate conditions', () => {
  it('== matches answer value', () => {
    const p: PredicateCondition = { questionId: 'brand', op: '==', value: 'toyota' };
    expect(evaluateCondition(p, { brand: 'toyota' })).toBe(true);
    expect(evaluateCondition(p, { brand: 'honda' })).toBe(false);
  });

  it('>= on numbers', () => {
    const p: PredicateCondition = { questionId: 'nps', op: '>=', value: 9 };
    expect(evaluateCondition(p, { nps: 10 })).toBe(true);
    expect(evaluateCondition(p, { nps: 9 })).toBe(true);
    expect(evaluateCondition(p, { nps: 8 })).toBe(false);
  });

  it('isSet and isNotSet', () => {
    const isSet: PredicateCondition = { questionId: 'x', op: 'isSet' };
    const isNotSet: PredicateCondition = { questionId: 'x', op: 'isNotSet' };
    expect(evaluateCondition(isSet, { x: 1 })).toBe(true);
    expect(evaluateCondition(isSet, {})).toBe(false);
    expect(evaluateCondition(isNotSet, { x: 1 })).toBe(false);
    expect(evaluateCondition(isNotSet, {})).toBe(true);
  });

  it('in checks against array value', () => {
    const p: PredicateCondition = {
      questionId: 'brand',
      op: 'in',
      value: ['toyota', 'honda'],
    };
    expect(evaluateCondition(p, { brand: 'toyota' })).toBe(true);
    expect(evaluateCondition(p, { brand: 'bmw' })).toBe(false);
  });
});

describe('composite conditions', () => {
  it('all requires every child true', () => {
    const c: CompositeCondition = {
      all: [
        { questionId: 'nps', op: '>=', value: 9 },
        { questionId: 'brand', op: '==', value: 'toyota' },
      ],
    };
    expect(evaluateCondition(c, { nps: 10, brand: 'toyota' })).toBe(true);
    expect(evaluateCondition(c, { nps: 5, brand: 'toyota' })).toBe(false);
  });

  it('any requires one child true', () => {
    const c: CompositeCondition = {
      any: [
        { questionId: 'nps', op: '<=', value: 6 },
        { questionId: 'brand', op: '==', value: 'toyota' },
      ],
    };
    expect(evaluateCondition(c, { nps: 3, brand: 'honda' })).toBe(true);
    expect(evaluateCondition(c, { nps: 8, brand: 'honda' })).toBe(false);
  });
});

describe('expression condition', () => {
  it('delegates to the sandbox', () => {
    const c: ExpressionCondition = {
      expression: "answers['nps'] >= 9 && answers['has-car'] == 'yes'",
    };
    expect(evaluateCondition(c, { nps: 10, 'has-car': 'yes' })).toBe(true);
    expect(evaluateCondition(c, { nps: 10, 'has-car': 'no' })).toBe(false);
  });
});

describe('evaluateNext', () => {
  const buildSurvey = (...logic: LogicRule[]): Survey => ({
    id: 's',
    screens: [{ id: 's1' }],
    logic,
  });

  it('first matching rule wins; null when nothing matches', () => {
    const survey = buildSurvey(
      {
        if: {
          all: [
            { questionId: 'nps', op: '>=', value: 9 },
            { questionId: 'brand', op: '==', value: 'toyota' },
          ],
        },
        then: { goto: 'screen-toyota-promoter' },
      },
      {
        if: { questionId: 'nps', op: '>=', value: 9 },
        then: { goto: 'screen-promoter' },
      },
    );

    expect(evaluateNext(survey, { nps: 10, brand: 'toyota' })).toBe('screen-toyota-promoter');
    expect(evaluateNext(survey, { nps: 10, brand: 'honda' })).toBe('screen-promoter');
    expect(evaluateNext(survey, { nps: 3, brand: 'toyota' })).toBeNull();
  });

  it('cross-screen answer can drive a late rule', () => {
    const survey = buildSurvey({
      if: { questionId: 'screen-1-answer', op: '==', value: 'magic' },
      then: { goto: 'screen-99' },
    });
    expect(evaluateNext(survey, { 'screen-1-answer': 'magic' })).toBe('screen-99');
  });

  it('broken expression in a rule falls through as false, never blocks', () => {
    const survey = buildSurvey({
      if: { expression: 'this is not valid syntax' },
      then: { goto: 'never' },
    });
    expect(evaluateNext(survey, {})).toBeNull();
  });
});
