/**
 * Parity tests for the expression sandbox. Every case here mirrors a case in
 * `ADP.Surveys.Shared.Tests/ExpressionSandboxTests.cs` — if you add one side you
 * must add the other, so the TS + C# implementations cannot drift.
 */

import { describe, expect, it } from 'vitest';
import {
  evaluateBoolean,
  parse,
  ExpressionSyntaxError,
} from '../src/expression-sandbox/index.js';
import type { AnswerMap } from '../src/schema.js';

const NO_ANSWERS: AnswerMap = {};

describe('literals and operators', () => {
  it.each<[string, boolean]>([
    ['true', true],
    ['false', false],
    ['!true', false],
    ['!!true', true],
    ['1 == 1', true],
    ['1 != 1', false],
    ['2 > 1', true],
    ['2 >= 2', true],
    ['1 < 2 && 2 < 3', true],
    ['1 < 2 && 3 < 2', false],
    ['1 > 2 || 1 < 2', true],
    ["'a' == 'a'", true],
    ['\'a\' == "a"', true],
    ["'a' != 'b'", true],
    ['(1 == 1) && true', true],
  ])('%s evaluates to %s', (source, expected) => {
    expect(evaluateBoolean(source, NO_ANSWERS)).toBe(expected);
  });
});

describe('answers access', () => {
  it('dot notation', () => {
    const answers: AnswerMap = { nps: 9 };
    expect(evaluateBoolean('answers.nps >= 9', answers)).toBe(true);
    expect(evaluateBoolean('answers.nps < 9', answers)).toBe(false);
  });

  it('bracket notation works for keys with dashes', () => {
    const answers: AnswerMap = { 'has-car': 'yes' };
    expect(evaluateBoolean("answers['has-car'] == 'yes'", answers)).toBe(true);
  });

  it('missing answer is null, not truthy', () => {
    expect(evaluateBoolean("answers.missing == 'x'", NO_ANSWERS)).toBe(false);
    expect(evaluateBoolean('answers.missing == null', NO_ANSWERS)).toBe(true);
  });
});

describe('built-in functions', () => {
  it('has and isSet', () => {
    const answers: AnswerMap = { nps: 9 };
    expect(evaluateBoolean("has('nps')", answers)).toBe(true);
    expect(evaluateBoolean("isSet('nps')", answers)).toBe(true);
    expect(evaluateBoolean("has('gone')", answers)).toBe(false);
    expect(evaluateBoolean("isNotSet('gone')", answers)).toBe(true);
  });

  it('in accepts array literal', () => {
    const answers: AnswerMap = { brand: 'toyota' };
    expect(evaluateBoolean("in(answers.brand, ['toyota', 'honda'])", answers)).toBe(true);
    expect(evaluateBoolean("in(answers.brand, ['bmw', 'audi'])", answers)).toBe(false);
  });
});

describe('short-circuit evaluation', () => {
  it('OR stops at first truthy', () => {
    // Second operand would throw (string-vs-number compare); OR must not touch it.
    expect(evaluateBoolean("true || 'a' > 5", NO_ANSWERS)).toBe(true);
  });

  it('AND stops at first falsy', () => {
    expect(evaluateBoolean("false && 'a' > 5", NO_ANSWERS)).toBe(false);
  });
});

describe('runtime and parse errors', () => {
  it('runtime error falls through as false (Decision #10)', () => {
    // Comparing string to number throws inside the interpreter; treat as rule-did-not-match.
    expect(evaluateBoolean("'abc' > 5", NO_ANSWERS)).toBe(false);
  });

  it('parse errors throw ExpressionSyntaxError from parse()', () => {
    expect(() => parse('answers.nps >=')).toThrow(ExpressionSyntaxError);
    expect(() => parse('answers = 5')).toThrow(ExpressionSyntaxError);
    expect(() => parse("'unterminated")).toThrow(ExpressionSyntaxError);
  });
});

describe('pre-parsed AST', () => {
  it('can be evaluated multiple times', () => {
    const ast = parse('answers.nps >= 9');
    expect(evaluateBoolean(ast, { nps: 9 })).toBe(true);
    expect(evaluateBoolean(ast, { nps: 5 })).toBe(false);
  });
});

describe('schema example from shared-schema.md', () => {
  it('works end-to-end', () => {
    const src = "answers['nps'] >= 9 && answers['has-car'] == 'yes'";
    expect(evaluateBoolean(src, { nps: 10, 'has-car': 'yes' })).toBe(true);
    expect(evaluateBoolean(src, { nps: 10, 'has-car': 'no' })).toBe(false);
    expect(evaluateBoolean(src, { nps: 8, 'has-car': 'yes' })).toBe(false);
  });
});
