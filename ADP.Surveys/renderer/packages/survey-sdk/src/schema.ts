/**
 * Wire-format TypeScript types mirroring ADP.Surveys.Shared DTOs. Naming matches the
 * JSON property names the C# `[JsonPropertyName]` attributes emit — not the C# class
 * names — so these types can be applied directly to the payload served by
 * `GET /surveys/instances/{instanceId}/schema`.
 *
 * Only the subset the SDK uses at runtime is modelled here. The renderer's UI layer
 * may add richer presentational types on top; those belong in the renderer package.
 */

export type LogicOperator =
  | '=='
  | '!='
  | '>'
  | '>='
  | '<'
  | '<='
  | 'in'
  | 'notIn'
  | 'isSet'
  | 'isNotSet';

export type QuestionType =
  | 'text'
  | 'paragraph'
  | 'number'
  | 'rating'
  | 'nps'
  | 'singleChoice'
  | 'multiChoice'
  | 'dropdown'
  | 'date'
  | 'dateTime'
  | 'file'
  | 'signature'
  | 'yesNo'
  | 'navigationList';

/** Accumulated answer map passed to the logic evaluator and the sandbox. Keys are
 *  question ids (banked or inline); values are the raw JSON-like answer values. */
export type AnswerMap = Record<string, unknown>;

export interface PredicateCondition {
  questionId: string;
  op: LogicOperator;
  value?: unknown;
}

export interface CompositeCondition {
  all?: LogicCondition[];
  any?: LogicCondition[];
}

export interface ExpressionCondition {
  expression: string;
}

export type LogicCondition = PredicateCondition | CompositeCondition | ExpressionCondition;

export interface LogicAction {
  goto?: string;
}

export interface LogicRule {
  if: LogicCondition;
  then: LogicAction;
}

export interface LocalizedString {
  [locale: string]: string;
}

/** Minimal screen shape the SDK navigates across. Renderer augments with question rendering. */
export interface Screen {
  id: string;
  title?: LocalizedString;
  description?: LocalizedString;
  questions?: unknown[];
  nextScreen?: string;
}

/** Per-option shape on a `navigationList` question — the minimum the SDK needs to
 *  route on selection. Presentation fields (label, icon, biColumn) belong with the
 *  renderer package's richer per-question types. Matches `NavigationListOptionDto`
 *  on the C# side. */
export interface NavigationOption {
  id: string;
  nextScreen?: string;
}

/** External options endpoint for choice-type questions — matches `OptionsSourceDto`
 *  on the C# side. Fetched client-side at render time with `Accept-Language` set
 *  from the active locale; the endpoints must be public and CORS-open (this block
 *  ships verbatim in the public schema — no secrets). `nextScreen` applies to
 *  `navigationList` only: every fetched option shares it as its destination. */
export interface OptionsSource {
  url: string;
  queryParams?: Record<string, string>;
  headers?: Record<string, string>;
  /** Dot-path to the item array inside the response; empty when the body IS the array. */
  itemsPath?: string;
  /** Dot-path to each item's answer value. Default: `ID`. */
  valuePath?: string;
  /** Dot-path to each item's display label. Default: `Name`. */
  labelPath?: string;
  nextScreen?: string;
}

/** Branding block served with the schema — per-survey authored branding merged
 *  over the deployment's `SurveyApiOptions.DefaultBranding` by the API at serve
 *  time. The renderer maps colors onto CSS custom properties and shows the logo. */
export interface Branding {
  primaryColor?: string;
  secondaryColor?: string;
  logoUrl?: string;
  faviconUrl?: string;
}

/** Minimal survey envelope — only the fields the SDK touches to drive navigation. */
export interface Survey {
  id: string;
  version?: number;
  defaultLocale?: string;
  locales?: string[];
  branding?: Branding;
  screens: Screen[];
  logic?: LogicRule[];
}

/** Discriminators used at runtime to distinguish the three condition shapes.
 *  Matches the logic in `LogicConditionDtoConverter` on the server side. */
export function isPredicateCondition(c: LogicCondition): c is PredicateCondition {
  return typeof (c as PredicateCondition).questionId === 'string';
}

export function isCompositeCondition(c: LogicCondition): c is CompositeCondition {
  const cc = c as CompositeCondition;
  return Array.isArray(cc.all) || Array.isArray(cc.any);
}

export function isExpressionCondition(c: LogicCondition): c is ExpressionCondition {
  return typeof (c as ExpressionCondition).expression === 'string';
}
