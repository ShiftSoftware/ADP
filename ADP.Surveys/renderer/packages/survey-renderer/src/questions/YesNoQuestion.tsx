import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

export function YesNoQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer, ui } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const yesLabel = question['yesLabel'] as LocalizedString | undefined;
  const noLabel = question['noLabel'] as LocalizedString | undefined;
  const selected = answers[id] as boolean | undefined;

  const yesText = yesLabel ? localize(yesLabel, locale, schema.defaultLocale) : ui.yes;
  const noText = noLabel ? localize(noLabel, locale, schema.defaultLocale) : ui.no;

  return (
    <fieldset className="survey-question survey-question--yesno">
      <legend className="survey-question__label">
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </legend>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <div className="survey-question__yesno" role="radiogroup">
        <button
          type="button"
          role="radio"
          aria-checked={selected === true}
          className={
            'survey-question__yesno-button' +
            (selected === true ? ' survey-question__yesno-button--selected' : '')
          }
          onClick={() => setAnswer(id, true)}
        >
          {yesText}
        </button>
        <button
          type="button"
          role="radio"
          aria-checked={selected === false}
          className={
            'survey-question__yesno-button' +
            (selected === false ? ' survey-question__yesno-button--selected' : '')
          }
          onClick={() => setAnswer(id, false)}
        >
          {noText}
        </button>
      </div>
    </fieldset>
  );
}
