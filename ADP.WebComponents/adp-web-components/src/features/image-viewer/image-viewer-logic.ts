import { ImageViewerInterface } from './interface';

let closeImageListener;

export function openImageViewer(this: ImageViewerInterface, target: HTMLImageElement, imageSrc: string) {
  if (this.expandedImage === imageSrc) return;

  this.originalImage = target;

  const expandedImageRef = this.el.shadowRoot.querySelector('.image-viewer-expanded-image') as HTMLImageElement;

  expandedImageRef.src = target.src;

  document.removeEventListener('keydown', closeImageListener);

  closeImageListener = () => closeImageViewer.bind(this)();

  document.addEventListener('keydown', closeImageListener);

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

    this.expandedImage = imageSrc;
  }, 200);
}

export function closeImageViewer(this: ImageViewerInterface, event?: KeyboardEvent) {
  event?.preventDefault();

  if (event && event.key !== 'Escape') return;

  document.removeEventListener('keydown', closeImageListener);

  const expandedImageRef = this.el.shadowRoot.querySelector('.image-viewer-expanded-image') as HTMLImageElement;

  const rect = this.originalImage.getBoundingClientRect();

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
  this.expandedImage = null;
}
