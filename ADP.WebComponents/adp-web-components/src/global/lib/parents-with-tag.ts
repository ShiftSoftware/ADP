export function parentsWithTag<T>(root: Node, tag: string): T[] {
  const matchedParents: T[] = [];
  let current: Node | null = root;

  while (current) {
    if (current.nodeType === Node.ELEMENT_NODE) {
      const el = current as HTMLElement;
      if (el.tagName.toLowerCase().startsWith(tag) && el !== root) matchedParents.push(el as T);
    }

    if (current.parentNode) current = current.parentNode;
    else if ((current as ShadowRoot).host) current = (current as ShadowRoot).host;
    else break;
  }

  return matchedParents;
}
