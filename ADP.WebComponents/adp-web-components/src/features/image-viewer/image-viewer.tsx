import { FunctionalComponent, h } from '@stencil/core';

import cn from '~lib/cn';

type ImageViewerProps = {
  expandedImage: string;
  closeImageViewer: () => void;
};

export const ImageViewer: FunctionalComponent<ImageViewerProps> = ({ expandedImage, closeImageViewer }) => {
  return (
    <div>
      <div onClick={closeImageViewer} class={cn('image-viewer-overlay', { active: expandedImage })} style={{ backdropFilter: expandedImage ? 'blur(3px)' : 'blur(0px)' }}>
        <button class="image-viewer-close-btn">
          <div class="image-viewer-close-line first" />
          <div class="image-viewer-close-line second" />
        </button>
      </div>

      <img alt="" class="image-viewer-expanded-image" />
    </div>
  );
};
