# ADP Dynamic Surveys — Renderer Workspace

React + Vite renderer, SDK, and web-component wrapper for the ADP Dynamic Surveys platform. Lives inside the ADP repo at `ADP.Surveys/renderer/` so the module stays cohesive with its .NET siblings (`ADP.Surveys.Shared`, `ADP.Surveys.Data`, `ADP.Surveys.API`, `ADP.Surveys.Web`).

## Layout

```
renderer/
├── packages/
│   ├── survey-sdk/            @shiftsoftware/survey-sdk — runtime contract, expression sandbox, logic evaluator
│   ├── survey-renderer/       @shiftsoftware/survey-renderer — React UI (Phase 3 Part A.2)
│   └── survey-web-component/  @shiftsoftware/survey-web-component — thin wrapper (Phase 3 Part A.3)
└── apps/
    └── standalone/            Deployable Vite app mounting the renderer at /s/:instanceId (Phase 3 Part D)
```

## Working on it

```bash
cd ADP.Surveys/renderer
npm install
npm test            # runs vitest across all packages
npm run build       # tsc across all packages
npm run typecheck
```

## Design notes

- **Expression sandbox parity** — `packages/survey-sdk/src/expression-sandbox/` is a TypeScript mirror of `ADP.Surveys.Shared/Evaluation/ExpressionSandbox/`. The grammar, AST shapes, and operator semantics are the spec; both implementations must stay in lock-step. Parity tests live in `packages/survey-sdk/tests/parity/`.
- **Logic evaluator parity** — `packages/survey-sdk/src/logic-evaluator.ts` mirrors `ADP.Surveys.Shared/Evaluation/LogicEvaluator.cs`. Decision #10 discipline: broken rules fall through as `false`, never block navigation.
- **Wire format** — consumes the JSON served by `GET /surveys/instances/{instanceId}/schema` (the resolved, frozen `SurveyDto`). Wire types in `schema.ts` mirror the `[JsonPropertyName]` shape of the C# DTOs, not the C# class names.
