export function getNestedValue(obj, path) {
  if (!path) return undefined;
  return path.split('.').reduce((acc, key) => acc?.[key], obj);
}
