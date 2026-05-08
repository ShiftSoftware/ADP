import type React from 'react';
import type { LocalizedString, NavigationOption } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

/** NavigationList presents a single-choice question as a forward-action list:
 *  tapping a row both records the answer (option id) and triggers a screen
 *  transition via the enclosing screen's navigation callback. The transition
 *  itself is delegated — this component only emits the selection event.
 *  See Phase 3 plan Part B.1 + C.2. */
export interface NavigationListOptionSelectedDetail {
  questionId: string;
  option: NavigationOption;
}

export function NavigationListQuestion({ question }: QuestionProps) {
  const { locale, schema } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const options = (question['options'] as Array<Record<string, unknown>> | undefined) ?? [];

  const dispatchSelection = (
    e: React.MouseEvent<HTMLButtonElement>,
    option: Record<string, unknown>,
  ) => {
    const detail: NavigationListOptionSelectedDetail = {
      questionId: id,
      option: {
        id: option['id'] as string,
        nextScreen: option['nextScreen'] as string | undefined,
      },
    };
    // Dispatch on the clicked element so the event bubbles up through the
    // SurveyRenderer's root and its listener picks it up.
    e.currentTarget.dispatchEvent(
      new CustomEvent<NavigationListOptionSelectedDetail>('survey:navigationListSelect', {
        detail,
        bubbles: true,
      }),
    );
  };

  return (
    <div className="survey-question survey-question--navlist">
      <div className="survey-question__label">
        {localize(title, locale, schema.defaultLocale)}
      </div>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <ul className="survey-navlist" role="radiogroup" aria-description="Selecting an option navigates to the next screen.">
        {options.map((option) => {
          const optionId = option['id'] as string;
          const label = option['label'] as LocalizedString | undefined;
          return (
            <li key={optionId} className="survey-navlist__row">
              <button
                type="button"
                className="survey-navlist__button"
                onClick={(e) => dispatchSelection(e, option)}
              >
                <span className="survey-navlist__label">
                  {localize(label, locale, schema.defaultLocale)}
                </span>
                <span aria-hidden="true" className="survey-navlist__chevron">›</span>
              </button>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
