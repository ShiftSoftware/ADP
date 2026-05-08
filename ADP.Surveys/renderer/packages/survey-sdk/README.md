# @shiftsoftware/survey-sdk

Runtime contract for the ADP Dynamic Surveys platform.

This package is the TypeScript mirror of the evaluation layer in `ADP.Surveys.Shared`. Same grammar, same operator semantics, same Decision #10 discipline (broken rules fall through as `false`, never block navigation). Parity tests in `tests/` pin the two implementations together.

## What's in here

- **Schema types** (`src/schema.ts`) — wire-format TS types for `Survey`, `Screen`, `LogicRule`, `LogicCondition`, etc. Names match the JSON served by `GET /surveys/instances/{instanceId}/schema`, not the C# class names.
- **Expression sandbox** (`src/expression-sandbox/`) — port of `Evaluation/ExpressionSandbox/`. Lexer, recursive-descent parser, tree-walking interpreter. No host-environment access; the only I/O is the answer map and the built-ins (`has`, `isSet`, `isNotSet`, `in`).
- **Logic evaluator** (`src/logic-evaluator.ts`) — port of `Evaluation/LogicEvaluator.cs`. `evaluateNext(survey, answers)` walks `survey.logic` in order and returns the first matching rule's goto target or `null`.

## Using it

```ts
import { evaluateNext, evaluateBoolean } from '@shiftsoftware/survey-sdk';

// Cross-screen routing
const next = evaluateNext(survey, answers); // → "screen-5" | null

// Ad-hoc expression check
const show = evaluateBoolean(
  "answers['nps'] >= 9 && answers['has-car'] == 'yes'",
  answers,
);
```

## Don't drift from C#

If you change the grammar or operator semantics, change both sides in the same commit and make sure both test suites cover the change. The grammar spec lives in `ast.ts` / `ExprNode.cs` (the same comment block at the top of each file).
