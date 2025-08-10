import { FunctionalComponent, h } from '@stencil/core';
import { JSXBase } from '@stencil/core/internal';

import cn from '~lib/cn';

type ImageViewerProps = {
  expandedImage: string;
  closeImageViewer: () => void;
  style?: JSXBase.HTMLAttributes<HTMLDivElement>['style'];
  imageStyle?: JSXBase.HTMLAttributes<HTMLDivElement>['style'];
};

export const ImageViewer: FunctionalComponent<ImageViewerProps> = ({ expandedImage, style = {}, imageStyle = {}, closeImageViewer }) => {
  return (
    <div>
      <div onClick={closeImageViewer} class={cn('image-viewer-overlay', { active: expandedImage })} style={{ backdropFilter: expandedImage ? 'blur(3px)' : 'blur(0px)', ...style }}>
        <button class="image-viewer-close-btn">
          <div class="image-viewer-close-line first" />
          <div class="image-viewer-close-line second" />
        </button>
      </div>

      <img style={imageStyle} alt="" class="image-viewer-expanded-image" />
    </div>
  );
};
