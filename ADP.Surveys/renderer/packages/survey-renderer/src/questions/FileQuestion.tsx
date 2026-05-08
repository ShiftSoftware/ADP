import { useRef } from 'react';
import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import type { QuestionProps } from './registry.js';

/** First-slice File implementation: records `{ name, size, type }` of the selected
 *  file into the answer map. Actual upload via a provider-supplied presigned URL
 *  is deferred — see Phase 3 Part B.3. The recorded shape is enough for the
 *  builder to show "one file attached" previews and for the API to reject files
 *  that exceed `maxSizeBytes` on the server side. */
export function FileQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer } = useSurveyContext();
  const id = question['id'] as string;
  const title = question['title'] as LocalizedString | undefined;
  const help = question['help'] as LocalizedString | undefined;
  const required = Boolean(question['required']);
  const acceptedTypes = question['acceptedTypes'] as string[] | undefined;
  const fileRef = useRef<HTMLInputElement | null>(null);
  const current = answers[id] as { name?: string } | undefined;

  const accept = acceptedTypes && acceptedTypes.length > 0 ? acceptedTypes.join(',') : undefined;

  return (
    <div className="survey-question survey-question--file">
      <label className="survey-question__label" htmlFor={`q-${id}`}>
        {localize(title, locale, schema.defaultLocale)}
        {required && <span aria-label="required" className="survey-question__required"> *</span>}
      </label>
      {help && (
        <p className="survey-question__help">{localize(help, locale, schema.defaultLocale)}</p>
      )}
      <input
        ref={fileRef}
        id={`q-${id}`}
        className="survey-question__file"
        type="file"
        required={required}
        accept={accept}
        onChange={(e) => {
          const file = e.target.files?.[0];
          if (!file) {
            setAnswer(id, null);
            return;
          }
          setAnswer(id, { name: file.name, size: file.size, type: file.type });
        }}
      />
      {current?.name && (
        <p className="survey-question__file-name">Selected: {current.name}</p>
      )}
    </div>
  );
}
