/**
 * Hand-rolled recursive-descent parser. Grammar is described on ast.ts. Mirror of
 * `ADP.Surveys.Shared/Evaluation/ExpressionSandbox/Parser.cs`.
 */

import type { ExprNode, BinaryOperator } from './ast.js';
import { type Token, ExpressionSyntaxError } from './lexer.js';

export function parse(tokens: Token[]): ExprNode {
  let pos = 0;

  const peek = (): Token => {
    const t = tokens[pos];
    if (!t) throw new ExpressionSyntaxError('unexpected end of tokens.', 0);
    return t;
  };

  const advance = (): Token => {
    const t = peek();
    if (t.kind !== 'EndOfInput') pos++;
    return t;
  };

  const match = (kind: Token['kind']): boolean => {
    if (peek().kind !== kind) return false;
    advance();
    return true;
  };

  const expect = (kind: Token['kind']): Token => {
    const t = peek();
    if (t.kind !== kind) {
      throw new ExpressionSyntaxError(`expected ${kind}, got '${t.text}'.`, t.position);
    }
    advance();
    return t;
  };

  const parseOr = (): ExprNode => {
    let left = parseAnd();
    while (match('Or')) {
      left = { kind: 'BinaryOp', op: '||', left, right: parseAnd() };
    }
    return left;
  };

  const parseAnd = (): ExprNode => {
    let left = parseEquality();
    while (match('And')) {
      left = { kind: 'BinaryOp', op: '&&', left, right: parseEquality() };
    }
    return left;
  };

  const parseEquality = (): ExprNode => {
    let left = parseComparison();
    while (true) {
      const kind = peek().kind;
      let op: BinaryOperator | null = null;
      if (kind === 'Eq' || kind === 'StrictEq') op = '==';
      else if (kind === 'NotEq' || kind === 'StrictNotEq') op = '!=';
      if (op === null) break;
      advance();
      left = { kind: 'BinaryOp', op, left, right: parseComparison() };
    }
    return left;
  };

  const parseComparison = (): ExprNode => {
    let left = parseUnary();
    while (true) {
      const kind = peek().kind;
      let op: BinaryOperator | null = null;
      if (kind === 'Lt') op = '<';
      else if (kind === 'Gt') op = '>';
      else if (kind === 'LtEq') op = '<=';
      else if (kind === 'GtEq') op = '>=';
      if (op === null) break;
      advance();
      left = { kind: 'BinaryOp', op, left, right: parseUnary() };
    }
    return left;
  };

  const parseUnary = (): ExprNode => {
    if (match('Not')) return { kind: 'UnaryOp', op: '!', operand: parseUnary() };
    return parsePrimary();
  };

  const parseArray = (): ExprNode => {
    expect('LBracket');
    const items: ExprNode[] = [];
    if (peek().kind !== 'RBracket') {
      items.push(parseOr());
      while (match('Comma')) items.push(parseOr());
    }
    expect('RBracket');
    return { kind: 'Array', items };
  };

  const parseAnswersAccess = (position: number): ExprNode => {
    let key: string;
    if (match('Dot')) {
      const ident = expect('Identifier');
      key = ident.text;
    } else if (match('LBracket')) {
      const str = expect('String');
      expect('RBracket');
      key = str.literal as string;
    } else {
      throw new ExpressionSyntaxError(
        "'answers' must be followed by .key or ['key'].",
        position,
      );
    }
    return { kind: 'AnswersAccess', key };
  };

  const parseIdentifierExpression = (): ExprNode => {
    const ident = advance();
    if (ident.text === 'answers') return parseAnswersAccess(ident.position);

    // Otherwise must be a function call: ident(...)
    expect('LParen');
    const args: ExprNode[] = [];
    if (peek().kind !== 'RParen') {
      args.push(parseOr());
      while (match('Comma')) args.push(parseOr());
    }
    expect('RParen');
    return { kind: 'Call', name: ident.text, args };
  };

  const parsePrimary = (): ExprNode => {
    const tok = peek();
    switch (tok.kind) {
      case 'Number':
      case 'String':
      case 'True':
      case 'False':
      case 'Null':
        advance();
        return { kind: 'Literal', value: tok.literal };
      case 'LParen': {
        advance();
        const inner = parseOr();
        expect('RParen');
        return inner;
      }
      case 'LBracket':
        return parseArray();
      case 'Identifier':
        return parseIdentifierExpression();
      default:
        throw new ExpressionSyntaxError(`unexpected token '${tok.text}'.`, tok.position);
    }
  };

  const expr = parseOr();
  expect('EndOfInput');
  return expr;
}
