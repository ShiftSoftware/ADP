export interface ImageViewerInterface {
  el: HTMLElement;
  expandedImage?: string;
  originalImage: HTMLImageElement;
  closeImage: (event?: KeyboardEvent) => void;
  closeImageListener: (event?: KeyboardEvent) => void;
}

export function openImageViewer(context: ImageViewerInterface, target: HTMLImageElement, imageSrc: string) {
  if (context.expandedImage === imageSrc) return;

  context.originalImage = target;

  const expandedImageRef = context.el.shadowRoot.getElementById('expanded-image') as HTMLImageElement;

  expandedImageRef.src = target.src;

  document.addEventListener('keydown', context.closeImageListener);

  const rect = target.getBoundingClientRect();

  document.body.style.overflow = 'hidden';

  Object.assign(expandedImageRef.style, {
    top: `${rect.top}px`,
    pointerEvents: 'auto',
    left: `${rect.left}px`,
    transitionDuration: '0s',
    width: `${rect.width}px`,
    height: `${rect.height}px`,
  });

  setTimeout(() => {
    const naturalWidth = target.naturalWidth;
    const naturalHeight = target.naturalHeight;

    const maxWidth = window.innerWidth - 160;
    const maxHeight = window.innerHeight - 32;

    const aspectRatio = naturalWidth / naturalHeight;

    let width, height;

    if (maxWidth / aspectRatio <= maxHeight) {
      width = maxWidth;
      height = maxWidth / aspectRatio;
    } else {
      height = maxHeight;
      width = maxHeight * aspectRatio;
    }

    expandedImageRef.style.transitionDuration = '0.3s';

    Object.assign(expandedImageRef.style, {
      opacity: '1',
      width: `${width}px`,
      height: `${height}px`,
      top: `${(window.innerHeight - height) / 2}px`,
      left: `${(window.innerWidth - width) / 2}px`,
    });

    context.expandedImage = imageSrc;
  }, 200);
}

export function closeImageViewer(context?: ImageViewerInterface, event?: KeyboardEvent) {
  if (event && event.key !== 'Escape') return;

  document.removeEventListener('keydown', context.closeImageListener);

  const expandedImageRef = context.el.shadowRoot.getElementById('expanded-image') as HTMLImageElement;

  const rect = context.originalImage.getBoundingClientRect();

  Object.assign(expandedImageRef.style, {
    top: `${rect.top}px`,
    pointerEvents: 'none',
    left: `${rect.left}px`,
    width: `${rect.width}px`,
    height: `${rect.height}px`,
    transitionDuration: '0.3s',
  });

  setTimeout(() => {
    expandedImageRef.src = '';
    expandedImageRef.style.opacity = '0';
    expandedImageRef.style.transitionDuration = '0s';
  }, 300);
  document.body.style.overflow = '';
  context.expandedImage = null;
}
