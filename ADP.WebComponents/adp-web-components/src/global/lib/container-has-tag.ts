export default function containerHasTag(root: Node, tag: string): boolean {
  /**
   * Checks if the given root node or any of its descendants has an element with the specified tag.
   * @param root - The root node to start the search from.
   * @param tag - The tag name to search for (case-insensitive).
   * @returns True if an element with the specified tag is found, otherwise false.
   */

  function traverse(node: Node): boolean {
    if (node.nodeType === Node.ELEMENT_NODE) {
      const el = node as HTMLElement;

      if (el.tagName.toLowerCase().startsWith(tag)) return true;

      const shadowRoot = (el as Element & { shadowRoot?: ShadowRoot })?.shadowRoot;
      if (shadowRoot) return traverse(shadowRoot);

      return Array.from(el.children).some(traverse);
    } else if (node.nodeType === Node.DOCUMENT_FRAGMENT_NODE) {
      return Array.from((node as DocumentFragment).children).some(traverse);
    }
    return false;
  }

  return traverse(root);
}
