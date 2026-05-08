import type { ComponentType } from 'react';
import type { QuestionType } from '@shiftsoftware/survey-sdk';

/** The raw, schema-shaped question record passed to each component. Typed as
 *  `Record<string, unknown>` at this layer so new question types can be added
 *  without touching the registry contract — each component narrows internally. */
export interface QuestionProps {
  question: Record<string, unknown>;
}

/** A question component's contract. Reads the question + (indirectly) the
 *  current answer via context, writes the answer via `setAnswer`. Navigation
 *  (Next button, navigationList option selection) is NOT this component's job —
 *  that's owned by the enclosing screen. */
export type QuestionComponent = ComponentType<QuestionProps>;

export type QuestionRegistry = Partial<Record<QuestionType, QuestionComponent>>;
