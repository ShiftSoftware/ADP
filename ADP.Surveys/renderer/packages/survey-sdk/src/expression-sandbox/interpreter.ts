/**
 * Tree-walking interpreter. No host access — the only I/O is the answer map and
 * the hard-coded built-ins. Mirror of
 * `ADP.Surveys.Shared/Evaluation/ExpressionSandbox/Interpreter.cs`.
 */

import type { ExprNode, BinaryOpNode, UnaryOpNode, CallNode } from './ast.js';
import type { AnswerMap } from '../schema.js';
import { areEqual, compare, toBool, unwrap } from './value-helper.js';

export function evaluate(node: ExprNode, answers: AnswerMap): unknown {
  switch (node.kind) {
    case 'Literal':
      return node.value;
    case 'AnswersAccess':
      return readAnswer(node.key, answers);
    case 'UnaryOp':
      return evaluateUnary(node, answers);
    case 'BinaryOp':
      return evaluateBinary(node, answers);
    case 'Call':
      return evaluateCall(node, answers);
    case 'Array':
      return node.items.map((item) => evaluate(item, answers));
  }
}

function evaluateUnary(node: UnaryOpNode, answers: AnswerMap): unknown {
  const operand = evaluate(node.operand, answers);
  if (node.op === '!') return !toBool(operand);
  throw new Error(`Unknown unary operator '${node.op}'.`);
}

function evaluateBinary(node: BinaryOpNode, answers: AnswerMap): unknown {
  // Short-circuit logicals
  if (node.op === '&&') {
    const l = evaluate(node.left, answers);
    if (!toBool(l)) return false;
    return toBool(evaluate(node.right, answers));
  }
  if (node.op === '||') {
    const l = evaluate(node.left, answers);
    if (toBool(l)) return true;
    return toBool(evaluate(node.right, answers));
  }

  const left = evaluate(node.left, answers);
  const right = evaluate(node.right, answers);

  switch (node.op) {
    case '==':
      return areEqual(left, right);
    case '!=':
      return !areEqual(left, right);
    case '<':
      return compare(left, right) < 0;
    case '>':
      return compare(left, right) > 0;
    case '<=':
      return compare(left, right) <= 0;
    case '>=':
      return compare(left, right) >= 0;
    default:
      throw new Error(`Unknown binary operator '${node.op as string}'.`);
  }
}

function evaluateCall(node: CallNode, answers: AnswerMap): unknown {
  switch (node.name) {
    case 'has':
    case 'isSet':
      return callIsSet(node, answers);
    case 'isNotSet':
      return !callIsSet(node, answers);
    case 'in':
      return callIn(node, answers);
    default:
      throw new Error(`Unknown function '${node.name}'.`);
  }
}

function callIsSet(node: CallNode, answers: AnswerMap): boolean {
  if (node.args.length !== 1) throw new Error(`${node.name}() takes one argument.`);
  const first = node.args[0];
  if (!first) return false;
  const key = evaluate(first, answers);
  if (typeof key !== 'string') return false;
  return key in answers && answers[key] !== null && answers[key] !== undefined;
}

function callIn(node: CallNode, answers: AnswerMap): boolean {
  if (node.args.length !== 2) throw new Error('in() takes two arguments: in(value, [array]).');
  const a0 = node.args[0];
  const a1 = node.args[1];
  if (!a0 || !a1) return false;
  const value = evaluate(a0, answers);
  const collection = evaluate(a1, answers);
  if (!Array.isArray(collection)) return false;
  return collection.some((item) => areEqual(value, item));
}

function readAnswer(key: string, answers: AnswerMap): unknown {
  if (!(key in answers)) return null;
  return unwrap(answers[key]);
}
