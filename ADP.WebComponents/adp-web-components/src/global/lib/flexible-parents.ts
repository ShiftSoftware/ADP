import { parentsWithTag } from './parents-with-tag';

/** The part of <flexible-container>'s API a nested element uses to announce its own animation. */
type FlexibleParent = {
  addChildrenAnimation: (child: unknown) => Promise<void>;
  removeChildrenAnimation: (child: unknown) => Promise<void>;
  onAnimationPlayChanges: (isAnimationStopped: boolean) => Promise<void>;
};

/**
 * A <flexible-container> pins its height to its content and animates between measurements, but it
 * measures on light-DOM mutations only: a height change that happens inside a child's shadow root —
 * a drawer sliding open on a CSS transition — is invisible to it, and the container keeps clipping
 * at the old height until something unrelated (a tab switch, a window resize) makes it look again.
 *
 * A nested <flexible-container> solves this for itself by announcing its animation to every
 * enclosing one, which then stops clipping (height auto, no transition) until the child has
 * settled and measures again. This is that protocol for a panel that animates with CSS instead:
 * call `announce` whenever a change that moves the panel's height begins; the announcement is
 * held open for the duration and extended by any further call inside it.
 */
export const createHeightChangeAnnouncer = (el: HTMLElement) => {
  const token = {};
  let timer: ReturnType<typeof setTimeout> | undefined;
  let announced: FlexibleParent[] = [];

  const release = () => {
    timer = undefined;
    announced.forEach(parent => {
      void parent.onAnimationPlayChanges(false);
      void parent.removeChildrenAnimation(token);
    });
    announced = [];
  };

  return {
    announce(durationMs: number) {
      if (timer) clearTimeout(timer);

      if (!announced.length) {
        announced = parentsWithTag<FlexibleParent>(el, 'flexible-container').filter(parent => typeof parent.addChildrenAnimation === 'function');
        announced.forEach(parent => void parent.addChildrenAnimation(token));
      }

      timer = setTimeout(release, durationMs);
    },
    dispose() {
      if (timer) clearTimeout(timer);
      release();
    },
  };
};
