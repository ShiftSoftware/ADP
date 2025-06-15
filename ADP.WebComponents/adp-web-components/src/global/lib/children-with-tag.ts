export default function childrenWithTag<T>(root: Node, tag: string): T[] {
  const matchedElements: T[] = [];

  function traverse(node: Node): void {
    if (node.nodeType === Node.ELEMENT_NODE) {
      const el = node as HTMLElement;

      if (el.tagName.toLowerCase().startsWith(tag) && root !== el) {
        matchedElements.push(el as T);
      }

      const shadowRoot = (el as Element & { shadowRoot?: ShadowRoot })?.shadowRoot;
      if (shadowRoot) traverse(shadowRoot);

      Array.from(el.children).forEach(traverse);
    } else if (node.nodeType === Node.DOCUMENT_FRAGMENT_NODE) {
      Array.from((node as DocumentFragment).children).forEach(traverse);
    }
  }

  traverse(root);

  return matchedElements;
}
