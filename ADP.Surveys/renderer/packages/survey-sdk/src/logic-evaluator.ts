/**
 * Cross-screen logic evaluator. Mirror of
 * `ADP.Surveys.Shared/Evaluation/LogicEvaluator.cs`.
 *
 * Walks `survey.logic` in order and returns the first matching rule's goto target,
 * or null when nothing matches. Caller falls back to `screen.nextScreen` / sequential
 * / end-of-survey per the Phase 3 navigation engine.
 *
 * Decision #10 discipline: broken rules (unresolvable predicate types, compare-on-
 * mismatched-types, malformed expressions) fall through as false so survey
 * navigation always progresses.
 */

import type {
  AnswerMap,
  CompositeCondition,
  LogicCondition,
  LogicOperator,
  PredicateCondition,
  Survey,
} from './schema.js';
import { isCompositeCondition, isExpressionCondition, isPredicateCondition } from './schema.js';
import { evaluateBoolean as evaluateExpression } from './expression-sandbox/index.js';
import { areEqual, compare, unwrap } from './expression-sandbox/value-helper.js';

export function evaluateNext(survey: Survey, answers: AnswerMap): string | null {
  if (!survey.logic) return null;
  for (const rule of survey.logic) {
    if (evaluateCondition(rule.if, answers)) return rule.then?.goto ?? null;
  }
  return null;
}

export function evaluateCondition(condition: LogicCondition, answers: AnswerMap): boolean {
  try {
    if (isPredicateCondition(condition)) return evaluatePredicate(condition, answers);
    if (isCompositeCondition(condition)) return evaluateComposite(condition, answers);
    if (isExpressionCondition(condition)) return evaluateExpression(condition.expression, answers);
    return false;
  } catch {
    // Defensive: a broken rule must not block navigation. Caller falls through.
    return false;
  }
}

function evaluateComposite(composite: CompositeCondition, answers: AnswerMap): boolean {
  if (composite.all && composite.all.length > 0) {
    return composite.all.every((child) => evaluateCondition(child, answers));
  }
  if (composite.any && composite.any.length > 0) {
    return composite.any.some((child) => evaluateCondition(child, answers));
  }
  return false;
}

function evaluatePredicate(predicate: PredicateCondition, answers: AnswerMap): boolean {
  const answerPresent =
    predicate.questionId in answers &&
    answers[predicate.questionId] !== null &&
    answers[predicate.questionId] !== undefined;

  if (predicate.op === 'isSet') return answerPresent;
  if (predicate.op === 'isNotSet') return !answerPresent;

  // Remaining operators need a value; treat "no value provided" as non-matching.
  if (predicate.value === undefined) return false;

  const answerValue = answerPresent ? unwrap(answers[predicate.questionId]) : null;
  const predicateValue = unwrap(predicate.value);

  return applyOperator(predicate.op, answerValue, predicateValue);
}

function applyOperator(op: LogicOperator, answer: unknown, value: unknown): boolean {
  switch (op) {
    case '==':
      return areEqual(answer, value);
    case '!=':
      return !areEqual(answer, value);
    case '>':
      return compare(answer, value) > 0;
    case '>=':
      return compare(answer, value) >= 0;
    case '<':
      return compare(answer, value) < 0;
    case '<=':
      return compare(answer, value) <= 0;
    case 'in':
      return inList(value, answer);
    case 'notIn':
      return !inList(value, answer);
    default:
      return false;
  }
}

function inList(list: unknown, value: unknown): boolean {
  if (!Array.isArray(list)) return false;
  return list.some((item) => areEqual(value, item));
}
