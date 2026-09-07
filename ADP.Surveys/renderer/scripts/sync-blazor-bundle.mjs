/**
 * Copy the built `<shift-survey>` bundle into the Blazor dashboard's static assets.
 *
 * The dashboard does not resolve the web component from npm — it serves a checked-in
 * copy from `ADP.Surveys.Web/wwwroot/js/shift-survey/`, so that hosts consuming the
 * `ShiftSoftware.ADP.Surveys.Web` NuGet package get the element without a node
 * toolchain. The cost of that is a copy that can silently fall behind the source.
 *
 * It did: the bundle sat at a 2026-07-25 build for six weeks, so a renderer feature
 * landed, passed its own tests, and still did not appear in the dashboard's survey
 * dialog. Hence this script (one command instead of two `cp`s to remember) plus the
 * staleness check in ADP.Surveys.Web.csproj, which warns when the source is newer
 * than the copy.
 *
 * Run: npm run sync:blazor   (from renderer/, builds then copies)
 */
import { copyFile, readFile, writeFile, stat } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';

const dist = new URL('../packages/survey-web-component/dist/', import.meta.url);
const target = new URL('../../ADP.Surveys.Web/wwwroot/js/shift-survey/', import.meta.url);

const BUNDLE = 'shift-survey.js';
const MAP = `${BUNDLE}.map`;

async function main() {
  const source = new URL('index.js', dist);
  try {
    await stat(source);
  } catch {
    throw new Error(
      'packages/survey-web-component/dist/index.js is missing — run the build first ' +
        '(`npm run build -w @shiftsoftware/survey-web-component`).',
    );
  }

  // The bundle is renamed on the way in, so its own sourceMappingURL comment (which
  // says `index.js.map`) would 404 in devtools. Rewrite it to the name it lands under.
  const code = await readFile(source, 'utf8');
  await writeFile(
    new URL(BUNDLE, target),
    code.replace(/\/\/# sourceMappingURL=index\.js\.map/, `//# sourceMappingURL=${MAP}`),
    'utf8',
  );
  await copyFile(new URL('index.js.map', dist), new URL(MAP, target));

  const { size } = await stat(new URL(BUNDLE, target));
  console.log(
    `Copied ${BUNDLE} (${(size / 1024).toFixed(0)} kB) → ${fileURLToPath(target)}`,
  );
  console.log('Commit both files — the dashboard serves them as checked-in static assets.');
}

main().catch((e) => {
  console.error(`\nsync-blazor-bundle failed: ${e.message}`);
  process.exit(1);
});
