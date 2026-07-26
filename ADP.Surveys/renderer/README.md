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

`npm run build --workspaces` is **not** topologically ordered. On a cold checkout the first
pass fails typechecking `survey-renderer` against a `survey-sdk/dist` that doesn't exist yet;
run it twice, or build `survey-sdk` first. The tests resolve the sdk through `dist/` too, so
build before testing. (`apps/standalone` is exempt — it aliases the packages to their `src/`.)

## Deploying the customer-facing app

`apps/standalone` is what respondents actually open. One deployment per consumer, as an
Azure Static Web App — see `apps/standalone/azure-pipelines.yml` (tag `release-survey-app-*`).

Two things make or break a deployment:

- **`VITE_API_BASE`** must be set at build time to that deployment's API root
  (`https://<host>/api/Surveys`). The production build **fails** without it rather than
  quietly baking in the localhost dev default.
- **`public/staticwebapp.config.json`** carries the navigation fallback. `/s/{publicId}` is a
  client-side route; without the rewrite, every survey link 404s on first load.

The server side needs `SurveyApiOptions.PublicSurveyUrlTemplate` pointed at the same
deployment, or the links the dashboard copies and the scheduler sends won't match where the
app actually lives.

Note the app is deliberately framable — agent-assist embeds it in an iframe — so the SWA
config sets no `X-Frame-Options`. Don't add one without checking that path.

## Design notes

- **Expression sandbox parity** — `packages/survey-sdk/src/expression-sandbox/` is a TypeScript mirror of `ADP.Surveys.Shared/Evaluation/ExpressionSandbox/`. The grammar, AST shapes, and operator semantics are the spec; both implementations must stay in lock-step. Parity tests live in `packages/survey-sdk/tests/parity/`.
- **Logic evaluator parity** — `packages/survey-sdk/src/logic-evaluator.ts` mirrors `ADP.Surveys.Shared/Evaluation/LogicEvaluator.cs`. Decision #10 discipline: broken rules fall through as `false`, never block navigation.
- **Wire format** — consumes the JSON served by `GET /surveys/instances/{instanceId}/schema` (the resolved, frozen `SurveyDto`). Wire types in `schema.ts` mirror the `[JsonPropertyName]` shape of the C# DTOs, not the C# class names.
