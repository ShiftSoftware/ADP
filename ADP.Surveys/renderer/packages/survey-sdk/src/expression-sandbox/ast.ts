/**
 * AST node types for the survey expression mini-language. Mirror of
 * `ADP.Surveys.Shared/Evaluation/ExpressionSandbox/ExprNode.cs`. Grammar
 * (precedence low → high):
 *
 *   expr       → or
 *   or         → and ('||' and)*
 *   and        → equality ('&&' equality)*
 *   equality   → comparison (('==' | '===' | '!=' | '!==') comparison)*
 *   comparison → unary (('<' | '>' | '<=' | '>=') unary)*
 *   unary      → '!' unary | primary
 *   primary    → literal | answers-access | call | array | '(' expr ')'
 */

export type ExprNode =
  | LiteralNode
  | AnswersAccessNode
  | BinaryOpNode
  | UnaryOpNode
  | CallNode
  | ArrayNode;

export type LiteralValue = number | string | boolean | null;

export interface LiteralNode {
  kind: 'Literal';
  value: LiteralValue;
}

export interface AnswersAccessNode {
  kind: 'AnswersAccess';
  key: string;
}

export type BinaryOperator = '==' | '!=' | '<' | '>' | '<=' | '>=' | '&&' | '||';

export interface BinaryOpNode {
  kind: 'BinaryOp';
  op: BinaryOperator;
  left: ExprNode;
  right: ExprNode;
}

export interface UnaryOpNode {
  kind: 'UnaryOp';
  op: '!';
  operand: ExprNode;
}

export interface CallNode {
  kind: 'Call';
  name: string;
  args: ExprNode[];
}

export interface ArrayNode {
  kind: 'Array';
  items: ExprNode[];
}
