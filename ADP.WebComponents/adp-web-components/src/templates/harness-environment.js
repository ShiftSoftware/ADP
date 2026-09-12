const ENVIRONMENT_NAME = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

/** Keep the generated index as the only authority for names, anchors and files. */
export function environmentsFrom(index) {
  if (!Array.isArray(index?.environments)) return [];

  return index.environments
    .filter(environment => {
      if (!ENVIRONMENT_NAME.test(environment?.name || '') || typeof environment?.files !== 'object' || environment.files === null) return false;

      return Object.values(environment.files).every(keys => Array.isArray(keys));
    })
    .map(environment => ({ ...environment, label: environmentLabel(environment.name) }));
}

/** Query string wins, then this tab's memory, then the first generated entry. */
export function chooseEnvironment(environments, requested, stored) {
  return environments.find(environment => environment.name === requested) ?? environments.find(environment => environment.name === stored) ?? environments[0] ?? null;
}

export function environmentFileUrl(siteRoot, environment, file) {
  return new URL(`mocks/generated/${environment}/${file}.json`, siteRoot);
}

export function environmentLabel(name) {
  return String(name)
    .split('-')
    .filter(Boolean)
    .map(word => word[0].toUpperCase() + word.slice(1))
    .join(' ');
}
