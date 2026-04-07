const appendClasses = (currentValue: string, value?: DOMTokenList | null) => {
  if (value?.length) return `${currentValue} ${Array.from(value).join(' ')}`;
  return currentValue;
};

export default function getCustomClassesForPortal(element: any) {
  let current = element;

  let finaleClass = '';
  let foundHostParent = false;

  while (current && !foundHostParent) {
    const root = current.getRootNode?.();

    if (
      current.part?.contains('shift-form') ||
      current.classList?.contains('shift-form') ||
      current.part?.contains('shift-component') ||
      current.classList?.contains('shift-component')
    ) {
      finaleClass = appendClasses(finaleClass, current?.part ?? null);
      finaleClass = appendClasses(finaleClass, current?.classList ?? null);
    }

    if (current.parentElement) {
      current = current.parentElement;
      continue;
    }

    if (root instanceof ShadowRoot) {
      current = root.host;
      if (!!finaleClass) foundHostParent = true;
      continue;
    }

    break;
  }

  if (foundHostParent && current) {
    finaleClass = appendClasses(finaleClass, current?.part ?? null);
    finaleClass = appendClasses(finaleClass, current?.classList ?? null);
  }

  return finaleClass?.replaceAll('  ', ' ');
}
