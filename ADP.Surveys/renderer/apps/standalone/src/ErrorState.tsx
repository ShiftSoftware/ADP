import type { SurveyClientError } from '@shiftsoftware/survey-sdk';

/** Error-state UI for unhappy paths after a schema fetch. Matches the
 *  `PublicSurveyController` semantics: 404 = wrong link, 410 = expired,
 *  409 = already completed (comes from submit, not fetch — here for completeness).
 *  Anything else surfaces a generic "something went wrong" copy. */
export function ErrorState({ error }: { error: SurveyClientError | Error }) {
  const isSdk = error.name === 'SurveyClientError';
  const code = isSdk ? (error as SurveyClientError).code : 'server';

  let title: string;
  let body: string;
  if (code === 'notFound') {
    title = 'Survey link is invalid';
    body = 'The link you used does not match any survey. Please check the URL.';
  } else if (code === 'gone') {
    title = 'Survey link has expired';
    body = 'This survey is no longer accepting responses.';
  } else if (code === 'conflict') {
    title = 'Already completed';
    body = 'This survey has already been submitted and cannot be resubmitted.';
  } else if (code === 'network') {
    title = 'Could not reach the server';
    body = 'Check your connection and try again.';
  } else {
    title = 'Something went wrong';
    body = error.message ?? 'Please try again shortly.';
  }

  return (
    <div className="survey-host__error" data-testid="survey-error">
      <h1 className="survey-host__error-title">{title}</h1>
      <p className="survey-host__error-body">{body}</p>
    </div>
  );
}
