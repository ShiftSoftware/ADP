import { SurveyRoute } from './SurveyRoute.js';
import { parseSurveyRoute } from './config.js';

/** Single-route shell: `/s/:publicId` mounts the survey; anything else shows a
 *  minimal landing with a link-required message. Deliberately no react-router
 *  for a single path — that's a bundle-size trap at this stage. */
export function App() {
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
  return <SurveyRoute publicId={route.publicId} />;
}
