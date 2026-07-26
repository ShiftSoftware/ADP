import { useRef } from 'react';
import type { LocalizedString } from '@shiftsoftware/survey-sdk';
import { useSurveyContext } from '../SurveyContext.js';
import { localize } from '../locale.js';
import { formatUi } from '../i18n.js';
import type { QuestionProps } from './registry.js';

/** Records `{ name, size, type }` of the selected file into the answer map. The file's
 *  CONTENT is not uploaded anywhere — presigned-URL upload (Phase 3 Part B.3) was never
 *  built.
 *
 *  The metadata-only shape is fine as a stepping stone but silently loses data if a real
 *  survey ships with it, so publishing a survey containing a file question is blocked
 *  server-side unless the deployment sets `FileUploadsSupported`. A deployment that opts
 *  in has taken on the upload path itself; the note below tells its respondents what
 *  actually happens rather than implying the file was received. */
export function FileQuestion({ question }: QuestionProps) {
  const { locale, schema, answers, setAnswer, ui } = useSurveyContext();
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
        <p className="survey-question__file-name">
          {formatUi(ui.fileRecordedName, { name: current.name })}
        </p>
      )}
    </div>
  );
}
