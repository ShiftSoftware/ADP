import { Component, Element, h, Host, Prop, State, Watch } from '@stencil/core';

import cn from '~lib/cn';

@Component({
  shadow: false,
  tag: 'shift-tab-content',
  styleUrl: 'shift-tab-content.css',
})
export class ShiftTabContent {
  @Prop() activeComponent: string;
  @Prop() components: { [key: string]: Node };

  @Element() el: HTMLElement;
  @State() lastActiveComponent: string;

  private ChildUpdatesActionTimeout: ReturnType<typeof setTimeout>;

  private clearAnimationClasses = (elementName: string): HTMLElement => {
    const element = this.el.getElementsByClassName(`tab-${elementName}`)[0] as HTMLElement;

    element.classList.remove('slide-content-out');
    element.classList.remove('slide-content-in');

    return element;
  };

  @Watch('activeComponent')
  async onActiveIndexChange(newActiveComponent: string, oldActiveComponent: string) {
    this.lastActiveComponent = oldActiveComponent;

    clearTimeout(this.ChildUpdatesActionTimeout);

    const oldElement = this.clearAnimationClasses(oldActiveComponent);
    const newElement = this.clearAnimationClasses(newActiveComponent);

    oldElement.classList.add('slide-content-out');
    newElement.classList.add('slide-content-in');
  }

  render() {
    return (
      <Host>
        <flexible-container>
          {Object.entries(this.components).map(([componentName, component]) => (
            <div
              onAnimationEnd={() => this.clearAnimationClasses(componentName)}
              class={cn('!w-full transition !duration-0', `tab-${componentName}`, {
                'absolute opacity-0 top-0 !pointer-events-none [&_*]:!pointer-events-none translate-x-[-200%] translate-y-[200%]': componentName !== this.activeComponent,
              })}
            >
              {component}
            </div>
          ))}
        </flexible-container>
      </Host>
    );
  }
}
