import { useEffect, useMemo, useState } from 'react';
import {
  SurveyClient,
  SurveyClientError,
  type Survey,
  type SubmissionMeta,
  type SurveySubmission,
} from '@shiftsoftware/survey-sdk';
import { SurveyRenderer } from '@shiftsoftware/survey-renderer';
import { ErrorState } from './ErrorState.js';
import { isAgentMode, resolveApiBase, resolveLocale } from './config.js';

export function SurveyRoute({ publicId }: { publicId: string }) {
  const apiBase = useMemo(() => resolveApiBase(), []);
  const locale = useMemo(() => resolveLocale(), []);
  const client = useMemo(() => new SurveyClient({ baseUrl: apiBase }), [apiBase]);

  const [schema, setSchema] = useState<Survey | null>(null);
  const [error, setError] = useState<SurveyClientError | Error | null>(null);

  useEffect(() => {
    let alive = true;
    client
      .fetchSchema(publicId)
      .then((s) => {
        if (alive) setSchema(s);
      })
      .catch((e) => {
        if (alive) setError(e as SurveyClientError | Error);
      });
    return () => {
      alive = false;
    };
  }, [client, publicId]);

  if (error) return <ErrorState error={error} />;

  if (!schema) {
    return (
      <div className="survey-host__loading" data-testid="survey-loading">
        <p>Loading survey…</p>
      </div>
    );
  }

  const submissionMeta: SubmissionMeta | undefined = isAgentMode()
    ? { mode: 'agent' }
    : undefined;

  const onSubmit = async (submission: SurveySubmission) => {
    try {
      await client.submitResponse(publicId, submission);
    } catch (e) {
      // Surface the submission error inside SurveyRenderer so the user sees it.
      throw e;
    }
  };

  return (
    <SurveyRenderer
      schema={schema}
      onSubmit={onSubmit}
      submissionMeta={submissionMeta}
      // Resume state is scoped to the instance's public id, so two different
      // surveys on the same device don't clobber each other, and revisiting the
      // same URL restores mid-flow answers.
      resumeKey={publicId}
      {...(locale ? { locale } : {})}
    />
  );
}
