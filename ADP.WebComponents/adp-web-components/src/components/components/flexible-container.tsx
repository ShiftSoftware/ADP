import { Component, Element, Prop, Watch, State, h, Method } from '@stencil/core';
import childrenWithTag from '~lib/children-with-tag';

import cn from '~lib/cn';

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

  content: HTMLDivElement;
  container: HTMLDivElement;
  @Element() el: HTMLElement;

  @State() initialTransitionDone: boolean = true;
  @State() childrenAnimatingList: FlexibleContainer[] = [];

  private resizeListener: () => void;

  private mutationObserver: MutationObserver;

  private parentListeners: FlexibleContainer[] = [];

  private ChildUpdatesActionTimeout: ReturnType<typeof setTimeout>;

  @Method()
  subscribeAsParent(parent: FlexibleContainer) {
    this.parentListeners.push(parent);
  }

  async componentDidLoad() {
    this.container = this.el.querySelector('.flexible-container');
    this.content = this.el.querySelector('.flexible-container-content');

    const flexibleChildren = childrenWithTag<FlexibleContainer>(this.el, 'flexible-container');

    flexibleChildren.forEach(child => child.subscribeAsParent(this));

    const mustUpdate = () => this.handleChildUpdates();

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

    setTimeout(() => {
      this.initialTransitionDone = false;
      this.startTransition();
    }, 200);
  }

  async disconnectedCallback() {
    if (this.mutationObserver) this.mutationObserver.disconnect();
    if (this.resizeListener) window.removeEventListener('resize', this.resizeListener);
  }

  private startTransition = (staticHeight?: number) => {
    if (staticHeight) return (this.container.style.height = `${staticHeight}px`);
    if (this.height !== 'auto') return;
    if (!this.isOpened) this.container.style.height = '0px';
    else this.container.style.height = `${this.content.clientHeight}px`;
  };

  private handleChildUpdates = (staticHeight?: number) => {
    clearTimeout(this.ChildUpdatesActionTimeout);
    this.ChildUpdatesActionTimeout = setTimeout(() => {
      this.startTransition(staticHeight);
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

  @Watch('stopAnimation')
  async onAnimationPlayChanges(isAnimationStopped: boolean) {
    if (!isAnimationStopped) this.container.style.height = `${this.content.clientHeight}px`;
  }

  @Method()
  addChildrenAnimation(child: FlexibleContainer) {
    this.childrenAnimatingList = [...this.childrenAnimatingList, child];
  }

  @Method()
  removeChildrenAnimation(child: FlexibleContainer) {
    this.childrenAnimatingList = this.childrenAnimatingList.filter(x => x !== child);
  }

  startTransitionAnimation = () => {
    if (!this.initialTransitionDone) {
      this.initialTransitionDone = true;
      return;
    }

    this.parentListeners.forEach(parent => parent.addChildrenAnimation(this));
  };
  stopTransitionAnimation = () => {
    this.parentListeners.forEach(parent => {
      parent.onAnimationPlayChanges(false);
      parent.removeChildrenAnimation(this);
    });
  };

  render() {
    return (
      <div
        onTransitionEnd={this.stopTransitionAnimation}
        onTransitionStart={this.startTransitionAnimation}
        class={cn(
          'flexible-container w-full min-w-full transition-all overflow-hidden duration-500',
          { 'h-0 opacity-0': !this.isOpened, '!h-auto !duration-0 !transition-none': this.stopAnimation || !!this.childrenAnimatingList.length },
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
