import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

export function NpsQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const min = Number(question['min'] ?? 0);
  const max = Number(question['max'] ?? 10);
  const lowLabel = question['lowLabel'] as LocalizedString | undefined;
  const highLabel = question['highLabel'] as LocalizedString | undefined;
  const selected = answers[id];

  const steps: number[] = [];
  for (let i = min; i <= max; i++) steps.push(i);

  return (
    <fieldset className="survey-question survey-question--nps">
      <legend className="survey-question__label">
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </legend>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <div className="survey-question__nps-scale" role="radiogroup">
        {steps.map((n) => {
          const isSelected = selected === n;
          return (
            <button
              type="button"
              role="radio"
              aria-checked={isSelected}
              key={n}
              className={
                'survey-question__nps-step' +
                (isSelected ? ' survey-question__nps-step--selected' : '')
              }
              onClick={() => setAnswer(id, n)}
            >
              {n}
            </button>
          );
        })}
      </div>
      {(lowLabel || highLabel) && (
        <div className="survey-question__nps-labels">
          <span>{lowLabel ? localize(lowLabel, locale, schema.defaultLocale) : ''}</span>
          <span>{highLabel ? localize(highLabel, locale, schema.defaultLocale) : ''}</span>
        </div>
      )}
    </fieldset>
  );
}
