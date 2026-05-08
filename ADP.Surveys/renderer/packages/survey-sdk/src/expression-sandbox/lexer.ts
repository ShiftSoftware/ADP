/**
 * Hand-rolled lexer for the survey expression mini-language. Mirror of
 * `ADP.Surveys.Shared/Evaluation/ExpressionSandbox/Lexer.cs`.
 */

export type TokenKind =
  | 'Number'
  | 'String'
  | 'True'
  | 'False'
  | 'Null'
  | 'Identifier'
  | 'Eq'
  | 'StrictEq'
  | 'NotEq'
  | 'StrictNotEq'
  | 'Lt'
  | 'Gt'
  | 'LtEq'
  | 'GtEq'
  | 'And'
  | 'Or'
  | 'Not'
  | 'LParen'
  | 'RParen'
  | 'LBracket'
  | 'RBracket'
  | 'Comma'
  | 'Dot'
  | 'EndOfInput';

export interface Token {
  kind: TokenKind;
  text: string;
  literal: number | string | boolean | null;
  position: number;
}

export class ExpressionSyntaxError extends Error {
  readonly position: number;
  constructor(message: string, position: number) {
    super(`Expression syntax error at column ${position}: ${message}`);
    this.position = position;
    this.name = 'ExpressionSyntaxError';
  }
}

function isDigit(c: string): boolean {
  return c >= '0' && c <= '9';
}

function isLetter(c: string): boolean {
  return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
}

function isLetterOrDigit(c: string): boolean {
  return isLetter(c) || isDigit(c);
}

function isWhitespace(c: string): boolean {
  return c === ' ' || c === '\t' || c === '\n' || c === '\r' || c === '\f' || c === '\v';
}

export function tokenize(source: string): Token[] {
  const tokens: Token[] = [];
  let pos = 0;

  const atEnd = (): boolean => pos >= source.length;
  const peekAt = (offset = 0): string => source.charAt(pos + offset);

  const match = (s: string): boolean => {
    if (pos + s.length > source.length) return false;
    for (let i = 0; i < s.length; i++) {
      if (source.charAt(pos + i) !== s.charAt(i)) return false;
    }
    pos += s.length;
    return true;
  };

  const skipWhitespace = (): void => {
    while (!atEnd() && isWhitespace(peekAt())) pos++;
  };

  const readNumber = (start: number): Token => {
    while (!atEnd() && isDigit(peekAt())) pos++;
    if (!atEnd() && peekAt() === '.') {
      pos++;
      while (!atEnd() && isDigit(peekAt())) pos++;
    }
    const text = source.substring(start, pos);
    const value = parseFloat(text);
    return { kind: 'Number', text, literal: value, position: start };
  };

  const readString = (start: number, quote: string): Token => {
    pos++; // opening quote
    let sb = '';
    while (!atEnd() && peekAt() !== quote) {
      const c = peekAt();
      if (c === '\\' && pos + 1 < source.length) {
        const next = peekAt(1);
        const unescaped: Record<string, string> = {
          n: '\n',
          t: '\t',
          r: '\r',
          '\\': '\\',
          "'": "'",
          '"': '"',
        };
        const replacement = unescaped[next];
        if (replacement === undefined)
          throw new ExpressionSyntaxError(`unknown escape '\\${next}'.`, pos);
        sb += replacement;
        pos += 2;
      } else {
        sb += c;
        pos++;
      }
    }
    if (atEnd()) throw new ExpressionSyntaxError('unterminated string literal.', start);
    pos++; // closing quote
    return { kind: 'String', text: sb, literal: sb, position: start };
  };

  const readIdentifier = (start: number): Token => {
    while (!atEnd()) {
      const c = peekAt();
      if (c === '_' || c === '-' || isLetterOrDigit(c)) pos++;
      else break;
    }
    const text = source.substring(start, pos);
    if (text === 'true') return { kind: 'True', text, literal: true, position: start };
    if (text === 'false') return { kind: 'False', text, literal: false, position: start };
    if (text === 'null') return { kind: 'Null', text, literal: null, position: start };
    return { kind: 'Identifier', text, literal: null, position: start };
  };

  const nextToken = (): Token => {
    const start = pos;
    const c = peekAt();

    if (isDigit(c)) return readNumber(start);
    if (c === "'" || c === '"') return readString(start, c);
    if (c === '_' || isLetter(c)) return readIdentifier(start);

    switch (c) {
      case '(':
        pos++;
        return { kind: 'LParen', text: '(', literal: null, position: start };
      case ')':
        pos++;
        return { kind: 'RParen', text: ')', literal: null, position: start };
      case '[':
        pos++;
        return { kind: 'LBracket', text: '[', literal: null, position: start };
      case ']':
        pos++;
        return { kind: 'RBracket', text: ']', literal: null, position: start };
      case ',':
        pos++;
        return { kind: 'Comma', text: ',', literal: null, position: start };
      case '.':
        pos++;
        return { kind: 'Dot', text: '.', literal: null, position: start };
      case '=':
        if (match('===')) return { kind: 'StrictEq', text: '===', literal: null, position: start };
        if (match('==')) return { kind: 'Eq', text: '==', literal: null, position: start };
        throw new ExpressionSyntaxError(
          "bare '=' is not a valid operator (use '==' or '===').",
          start,
        );
      case '!':
        if (match('!==')) return { kind: 'StrictNotEq', text: '!==', literal: null, position: start };
        if (match('!=')) return { kind: 'NotEq', text: '!=', literal: null, position: start };
        pos++;
        return { kind: 'Not', text: '!', literal: null, position: start };
      case '<':
        if (match('<=')) return { kind: 'LtEq', text: '<=', literal: null, position: start };
        pos++;
        return { kind: 'Lt', text: '<', literal: null, position: start };
      case '>':
        if (match('>=')) return { kind: 'GtEq', text: '>=', literal: null, position: start };
        pos++;
        return { kind: 'Gt', text: '>', literal: null, position: start };
      case '&':
        if (match('&&')) return { kind: 'And', text: '&&', literal: null, position: start };
        throw new ExpressionSyntaxError("expected '&&'.", start);
      case '|':
        if (match('||')) return { kind: 'Or', text: '||', literal: null, position: start };
        throw new ExpressionSyntaxError("expected '||'.", start);
    }

    throw new ExpressionSyntaxError(`unexpected character '${c}'.`, start);
  };

  while (true) {
    skipWhitespace();
    if (atEnd()) {
      tokens.push({ kind: 'EndOfInput', text: '', literal: null, position: pos });
      return tokens;
    }
    tokens.push(nextToken());
  }
}
