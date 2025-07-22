import { Component, Element, h, Host, Prop, Watch } from '@stencil/core';

import cn from '~lib/cn';
import containerHasTag from '~lib/container-has-tag';

import { FlexibleContainer } from './flexible-container';

@Component({
  shadow: false,
  tag: 'shift-slider',
  styleUrl: 'shift-slider.css',
})
export class ShiftSlider {
  @Prop() components: (() => Node)[];
  @Prop() activeIndex: number;

  @Element() el: HTMLElement;

  flexibleContainerRef: FlexibleContainer;

  private ChildUpdatesActionTimeout: ReturnType<typeof setTimeout>;

  async componentDidLoad() {
    this.flexibleContainerRef = this.el.getElementsByTagName('flexible-container')[0] as unknown as FlexibleContainer;
  }

  @Watch('activeIndex')
  async onActiveIndexChange(newActiveIndex: number) {
    clearTimeout(this.ChildUpdatesActionTimeout);

    const newActiveElement = this.el.getElementsByClassName('element-number-' + newActiveIndex)[0] as HTMLElement;

    if (newActiveElement && containerHasTag(newActiveElement, 'flexible-container')) {
      this.flexibleContainerRef.stopAnimation = false;
      this.ChildUpdatesActionTimeout = setTimeout(() => {
        this.flexibleContainerRef.stopAnimation = true;
      }, 600);
    } else this.flexibleContainerRef.stopAnimation = false;
  }

  render() {
    return (
      <Host>
        <flexible-container stopAnimation classes="relative">
          {this.components.map((listElement, idx) => (
            <div
              class={cn('w-full transition !duration-1000', 'element-number-' + idx, {
                'opacity-0 absolute top-0 !pointer-events-none [&_*]:!pointer-events-none': this.activeIndex !== idx,
                'translate-x-[110%]': this.activeIndex < idx,
                '-translate-x-[110%]': this.activeIndex > idx,
              })}
            >
              {listElement()}
            </div>
          ))}
        </flexible-container>
      </Host>
    );
  }
}
