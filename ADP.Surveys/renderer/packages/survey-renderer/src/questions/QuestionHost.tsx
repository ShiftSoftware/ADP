import type { QuestionType } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import type { QuestionRegistry } from './registry.js';
import { SourcedOptionsGate } from './SourcedOptionsGate.js';

/** Dispatches to the right question component for the question's `type`
 *  discriminator. Unknown types render a placeholder so a missing registry
 *  entry is visible during development instead of failing silently. */
export function QuestionHost({
  question,
  registry,
}: {
  question: Record<string, unknown>;
  registry: QuestionRegistry;
}) {
  const { ui } = useSurveyContext();
  const type = question['type'] as QuestionType | undefined;
  const Component = type ? registry[type] : undefined;
  if (!Component) {
    return (
      <div className="survey-question survey-question--unknown">
        <em>{ui.unsupportedQuestion} {String(type ?? 'missing')}</em>
      </div>
    );
  }
  // Sourced questions route through the fetch gate; authored inline options
  // (if any) win over the source so a hybrid question degrades gracefully.
  const hasInlineOptions =
    Array.isArray(question['options']) && (question['options'] as unknown[]).length > 0;
  if (question['optionsSource'] != null && !hasInlineOptions) {
    return <SourcedOptionsGate question={question} Component={Component} />;
  }
  return <Component question={question} />;
}
