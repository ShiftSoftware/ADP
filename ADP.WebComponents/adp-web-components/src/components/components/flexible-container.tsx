import { Component, Element, Prop, Watch, State, h, Method } from '@stencil/core';

import cn from '~lib/cn';
import { parentsWithTag } from '~lib/parents-with-tag';

@Component({
  shadow: false,
  tag: 'flexible-container',
  styleUrl: 'flexible-container.css',
})
export class FlexibleContainer {
  @Prop() classes?: string;
  @Prop() alwaysStrict?: boolean;
  @Prop() containerClasses?: string;
  @Prop() isOpened?: boolean = true;
  @Prop() stopAnimation?: boolean = false;
  @Prop() height?: number | 'auto' = 'auto';
  @Prop() onlyForMounting = false;

  @State() stopWorking = false;

  content: HTMLDivElement;
  container: HTMLDivElement;
  @Element() el: HTMLElement;

  @State() childrenAnimatingList: FlexibleContainer[] = [];

  private resizeListener: () => void;

  private mutationObserver: MutationObserver;

  private animationTimeoutRef: ReturnType<typeof setTimeout>;
  private ChildUpdatesActionTimeout: ReturnType<typeof setTimeout>;

  private initialStyle: any;

  async componentDidLoad() {
    this.container = this.el.querySelector('.flexible-container');
    this.content = this.el.querySelector('.flexible-container-content');

    const mustUpdate = () => this.isOpened && this.handleChildUpdates();

    this.mutationObserver = new MutationObserver(mustUpdate);

    this.mutationObserver.observe(this.content, {
      subtree: true,
      childList: true,
      attributes: true,
      characterData: true,
      attributeOldValue: true,
      characterDataOldValue: true,
    });

    this.resizeListener = mustUpdate;

    window.addEventListener('resize', this.resizeListener);

    if (this.isOpened)
      setTimeout(() => {
        this.startTransition();
      }, 200);
  }

  async disconnectedCallback() {
    if (this.mutationObserver) this.mutationObserver.disconnect();
    if (this.resizeListener) window.removeEventListener('resize', this.resizeListener);
  }

  private startTransition = (staticHeight?: number) => {
    if (staticHeight !== -1) {
      const parents = parentsWithTag<FlexibleContainer>(this.el, 'flexible-container');

      parents.forEach(parent => {
        parent.isOpened && parent.addChildrenAnimation(this);
      });

      clearTimeout(this.animationTimeoutRef);

      this.animationTimeoutRef = setTimeout(() => {
        parents.forEach(parent => {
          parent.onAnimationPlayChanges(false);
          parent.removeChildrenAnimation(this);
        });
      }, 510);
    }

    if (!this.isOpened) {
      this.container.style.height = '0px';
      this.container.style.opacity = '0.1';
      return;
    }

    this.container.style.opacity = '1';
    if (staticHeight && staticHeight !== -1) return (this.container.style.height = `${staticHeight}px`);
    if (this.height !== 'auto') return;
    else this.container.style.height = `${this.content.clientHeight}px`;
  };

  private handleChildUpdates = (staticHeight?: number) => {
    clearTimeout(this.ChildUpdatesActionTimeout);
    this.ChildUpdatesActionTimeout = setTimeout(() => {
      this.startTransition(staticHeight);

      setTimeout(() => {
        if (this.isOpened && this.onlyForMounting) this.stopWorking = true;
      }, 600);
    }, 50);
  };

  @Watch('isOpened')
  async handleOpenChanges() {
    this.handleChildUpdates();
  }

  @Watch('height')
  async handleHeightChanges(newHeight: number | 'auto') {
    if (newHeight === 'auto') this.handleChildUpdates();
    else if (typeof newHeight === 'number') this.handleChildUpdates(newHeight);
  }

  @Method()
  @Watch('stopAnimation')
  async onAnimationPlayChanges(isAnimationStopped: boolean) {
    if (!isAnimationStopped) this.container.style.height = `${this.content.clientHeight}px`;
  }

  @Method()
  async addChildrenAnimation(child: FlexibleContainer) {
    this.childrenAnimatingList = [...this.childrenAnimatingList, child];
  }

  @Method()
  async removeChildrenAnimation(child: FlexibleContainer) {
    this.childrenAnimatingList = this.childrenAnimatingList.filter(x => x !== child);
  }

  render() {
    if (!this.initialStyle) this.initialStyle = !this.isOpened ? { height: '0px' } : { height: 'auto' };

    return (
      <div
        style={this.initialStyle}
        class={cn(
          'flexible-container duration-500 w-full min-w-full',

          {
            'transition-all overflow-hidden !duration-500': !this.stopWorking,
            '!h-auto !duration-0 !transition-none': this.stopWorking || this.stopAnimation || !!this.childrenAnimatingList.length,
          },
          this.containerClasses,
        )}
      >
        <div class={cn('flexible-container-content', this.classes)}>
          <slot></slot>
        </div>
      </div>
    );
  }
}
