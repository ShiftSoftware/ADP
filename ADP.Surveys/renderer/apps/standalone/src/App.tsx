import { SurveyRoute } from './SurveyRoute.js';
import { parseSurveyRoute, resolveApiBase } from './config.js';

/** Single-route shell: `/s/:publicId` mounts the survey; anything else shows a
 *  minimal landing with a link-required message. Deliberately no react-router
 *  for a single path — that's a bundle-size trap at this stage.
 *
 *  API-base resolution happens HERE rather than inside `SurveyRoute` because a
 *  missing base is a hard stop, and bailing out early keeps `SurveyRoute`'s
 *  hooks unconditional. */
export function App() {
  const apiBase = resolveApiBase();
  if (apiBase === null) {
    return (
      <div className="survey-host__error" data-testid="survey-config-error">
        <h1 className="survey-host__error-title">Survey app is not configured</h1>
        <p className="survey-host__error-body">
          This deployment was built without an API address, so it cannot load surveys. Please
          contact whoever sent you this link.
        </p>
      </div>
    );
  }

  const route = parseSurveyRoute();
  if (!route) {
    return (
      <div className="survey-host__landing">
        <h1>Survey link required</h1>
        <p>
          This app is only reachable via a direct survey link — something like
          <code> /s/&lt;your-survey-id&gt;</code>.
        </p>
      </div>
    );
  }
  return <SurveyRoute publicId={route.publicId} apiBase={apiBase} />;
}
