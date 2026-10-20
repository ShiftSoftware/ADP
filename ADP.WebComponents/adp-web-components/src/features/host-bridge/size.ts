import { dispatchFormEvent } from './events';

type Size = { width: number; height: number };

const observed = new WeakSet<Element>();

const nextFrame = (callback: () => void) => (typeof requestAnimationFrame === 'function' ? requestAnimationFrame(callback) : setTimeout(callback, 16));

function measure(el: HTMLElement, entry: ResizeObserverEntry): Size {
  const boxes = entry?.borderBoxSize as unknown;
  const box = (Array.isArray(boxes) ? boxes[0] : boxes) as ResizeObserverSize | undefined;

  if (box && typeof box.inlineSize === 'number') return { width: Math.round(box.inlineSize), height: Math.round(box.blockSize) };

  return { width: Math.round(el.offsetWidth || 0), height: Math.round(el.offsetHeight || 0) };
}

export function observeSize(el: HTMLElement | undefined) {
  if (!el || observed.has(el) || typeof ResizeObserver !== 'function') return;
  observed.add(el);

  let last: Size | undefined;
  let pending: Size | undefined;
  let waiting = false;

  const publish = (size: Size) => {
    if (last && last.width === size.width && last.height === size.height) return false;

    last = size;
    dispatchFormEvent(el, 'adp-form-size-change', { width: size.width, height: size.height });
    return true;
  };

  const settle = () => {
    waiting = false;
    const next = pending;
    pending = undefined;

    if (next && publish(next)) {
      waiting = true;
      nextFrame(settle);
    }
  };

  const observer = new ResizeObserver(entries => {
    if (!el.isConnected) {
      observer.disconnect();
      observed.delete(el);
      return;
    }

    const entry = entries[entries.length - 1];
    const size = measure(el, entry);

    if (waiting) {
      pending = size;
      return;
    }

    if (publish(size)) {
      waiting = true;
      nextFrame(settle);
    }
  });

  observer.observe(el, { box: 'border-box' });
}
