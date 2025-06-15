export function scrollIntoContainerView(item: HTMLElement, container: HTMLElement) {
  const itemRect = item.getBoundingClientRect();
  const containerRect = container.getBoundingClientRect();

  const isPartiallyHidden = itemRect.left < containerRect.left || itemRect.right > containerRect.right;

  if (isPartiallyHidden) {
    const scrollLeft = item.offsetLeft - container.clientWidth / 2 + item.clientWidth / 2;

    container.scrollTo({
      left: scrollLeft,
      behavior: 'smooth',
    });
  }
}
