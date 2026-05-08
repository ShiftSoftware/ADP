import type { QuestionType } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import type { QuestionRegistry } from './registry.js';

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
  return <Component question={question} />;
}
