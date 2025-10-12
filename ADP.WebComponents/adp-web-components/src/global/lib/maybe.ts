export function maybe<T>(value: T): T | undefined {
  const r = Math.random();
  if (r < 0.5) return undefined;

  return value;
}
