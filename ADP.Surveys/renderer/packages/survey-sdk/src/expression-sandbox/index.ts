/**
 * Public entry point for the survey expression mini-language. Mirror of
 * `ADP.Surveys.Shared/Evaluation/ExpressionSandbox/ExpressionSandbox.cs`.
 *
 * Grammar, AST, lexer, parser, and interpreter live beside this file. The spec is
 * enforced by parity tests — keep the TS and C# implementations in lock-step.
 */

import type { AnswerMap } from '../schema.js';
import type { ExprNode } from './ast.js';
import { tokenize, ExpressionSyntaxError } from './lexer.js';
import { parse as parseTokens } from './parser.js';
import { evaluate } from './interpreter.js';
import { toBool } from './value-helper.js';

export { ExpressionSyntaxError };
export type { ExprNode };

/** Parse source into an AST. Throws {@link ExpressionSyntaxError} on invalid syntax. */
export function parse(source: string): ExprNode {
  const tokens = tokenize(source);
  return parseTokens(tokens);
}

/**
 * Evaluate source against the answer map. Returns boolean truthiness of the result.
 * Runtime errors fall through as false so a broken expression defaults to "rule did
 * not match" — Decision #10.
 */
export function evaluateBoolean(source: string, answers: AnswerMap): boolean;
export function evaluateBoolean(ast: ExprNode, answers: AnswerMap): boolean;
export function evaluateBoolean(sourceOrAst: string | ExprNode, answers: AnswerMap): boolean {
  try {
    const ast = typeof sourceOrAst === 'string' ? parse(sourceOrAst) : sourceOrAst;
    return toBool(evaluate(ast, answers));
  } catch {
    return false;
  }
}
