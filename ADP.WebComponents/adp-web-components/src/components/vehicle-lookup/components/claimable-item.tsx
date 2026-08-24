import { InferType } from 'yup';
import { h, FunctionalComponent, VNode } from '@stencil/core';

import cn from '~lib/cn';

import { VehicleServiceItemDTO } from '~types/generated/vehicle-lookup/vehicle-service-item-dto';

import { XIcon } from '~assets/x-icon';
import { BanIcon } from '~assets/ban-icon';
import { LockIcon } from '~assets/lock-icon';
import { PowerIcon } from '~assets/power-icon';
import { PauseIcon } from '~assets/pause-icon';
import { CheckIcon } from '~assets/check-icon';
import { HourglassIcon } from '~assets/hourglass-icon';
import { CircleXIcon } from '~assets/circle-x-icon';
import { CircleBanIcon } from '~assets/circle-ban-icon';
import { CircleCheckIcon } from '~assets/circle-check-icon';
import { CirclePauseIcon } from '~assets/circle-pause-icon';
import { CirclePowerIcon } from '~assets/circle-power-icon';

import dynamicClaimSchema from '~locales/vehicleLookup/claimableItems/type';

/**
 * The two halves of one status, drawn twice on every card: outlined inside a ring above the lane,
 * and solid inside the node on it. Same glyph both times, so the eye ties the label to the dot
 * without having to read either.
 *
 * These replaced the flat .svg artwork the header used to load through `<img>`. That artwork came
 * with its colours baked in, which meant a card could not be drained to grey for "not your turn
 * yet" and a glyph could not be dropped onto a filled node in white. Both of these are drawn in
 * `currentColor`, so one CSS variable per status now paints all of it.
 *
 * Factories rather than shared nodes: each glyph is rendered twice per card, and Stencil hangs the
 * rendered element off the vdom node itself — one shared node would have the second render steal
 * the first's element.
 */
const statusIcons: { [status: string]: () => VNode } = {
  expired: () => <CircleXIcon />,
  pending: () => <CirclePauseIcon />,
  processed: () => <CircleCheckIcon />,
  cancelled: () => <CircleBanIcon />,
  activationRequired: () => <CirclePowerIcon />,
};

const nodeGlyphs: { [status: string]: () => VNode } = {
  expired: () => <XIcon />,
  pending: () => <PauseIcon />,
  processed: () => <CheckIcon />,
  cancelled: () => <BanIcon />,
  activationRequired: () => <PowerIcon />,
};

const lockGlyphs = {
  Locked: () => <LockIcon />,
  Missed: () => <HourglassIcon />,
};

/**
 * A label is read as two lines — the milestone, then what is owed at it — but it arrives as one
 * string: "1,000 KM Free Service". This splits the milestone off the front so the two lines say
 * "1,000 KM" and "Free Service" rather than repeating the whole name and then a bare "Free".
 *
 * Deliberately narrow: a name that does not open with a distance keeps its whole name on the first
 * line and falls back to the item's own type underneath. "Return Reward" is not a milestone and
 * should not be forced to look like one.
 */
const MILESTONE_LABEL = /^(\d[\d.,\s]*(?:KM|MILES?|MI)\b)\s*(.+)$/i;

// The only two words this feature adds to the screen, kept together so moving them into the locale
// files is one edit. Left untranslated deliberately for now — see the locked-items plan.
const lockStateLabels = {
  Locked: 'Locked',
  Missed: 'Missed',
};

type ClaimableItemProps = {
  addStatusClass: boolean;
  item: VehicleServiceItemDTO;
  locale: InferType<typeof dynamicClaimSchema>;
  setClaimableItemPopover: (showPopover: boolean, claimableItem?: VehicleServiceItemDTO, claimableItemPopoverRef?: HTMLDivElement) => void;
};

export const ClaimableItem: FunctionalComponent<ClaimableItemProps> = ({ item, locale, addStatusClass, setClaimableItemPopover }) => {
  const removeLoadAnimationClass = (event: AnimationEvent) => {
    const component = event.target as HTMLDivElement;
    component.classList.remove('load-animation');
  };

  let columnEl: HTMLDivElement;

  // Anchored to the whole column, not to the status block: the popover opens clear of the icon, the
  // chip, the node and the name, so the item the customer is pointing at stays readable while they
  // read the card about it.
  const openPopover = () => setClaimableItemPopover(true, item, columnEl);

  const closePopover = () => setClaimableItemPopover(false);

  // The column is reachable by keyboard, and focus alone opens the card — but a thing announced as a
  // button has to answer to Enter and Space, and a card that opened on focus has to be dismissable
  // without tabbing away from the item it describes.
  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'Enter' || event.key === ' ') {
      event.preventDefault();
      openPopover();
      return;
    }

    if (event.key === 'Escape') closePopover();
  };

  // A locked or missed item carries an ordinary status underneath — usually pending, because
  // nothing about its lifecycle changed. The lock is what the customer is being told, so it takes
  // over the card: the status would say "Pending" about a reward they cannot claim.
  const lockState = item?.lock?.state;

  const statusLabel = lockState ? lockStateLabels[lockState] : locale[item?.status];

  const [, milestone, descriptor] = item?.name?.match(MILESTONE_LABEL) ?? [];

  const nameLabel = milestone ?? item?.name;

  // Either the rest of the name or, where there was no milestone to split off, the item's own type.
  // Both come off the record in whatever case the source system wrote them, so the casing is
  // normalised in CSS rather than by rewriting the value.
  const typeLabel = descriptor ?? item?.type;

  return (
    <div class={cn('claimable-item', { [item.status]: addStatusClass && !lockState, [`lock-${lockState}`]: !!lockState })}>
      {/* One hover surface for the whole column. The status block, the node and the name are three
          separate boxes with air between them, and a pointer resting anywhere in that column — most
          of all on the node itself, which is what the eye goes to — has to count as hovering the
          item. See .claimable-item-container::after for the reach. */}
      <div
        role="button"
        tabindex="0"
        onBlur={closePopover}
        onClick={openPopover}
        onFocus={openPopover}
        onKeyDown={onKeyDown}
        onMouseEnter={openPopover}
        onMouseLeave={closePopover}
        aria-label={`${item?.name} — ${statusLabel}`}
        ref={el => (columnEl = el as HTMLDivElement)}
        class="claimable-item-container"
      >
        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-header load-animation">
          <div class={cn('claimable-item-icon', { 'claimable-item-lock-icon': !!lockState })}>{lockState ? lockGlyphs[lockState]() : statusIcons[item?.status]?.()}</div>
          <div class="claimable-item-status">{statusLabel}</div>
        </div>

        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-node load-animation">
          <div class="claimable-item-node-glyph">{lockState ? lockGlyphs[lockState]() : nodeGlyphs[item?.status]?.()}</div>
        </div>

        <div onAnimationEnd={removeLoadAnimationClass} class="claimable-item-footer load-animation">
          <div class="claimable-item-dot" />
          <div class="claimable-item-name">{nameLabel}</div>
          {typeLabel && <div class="claimable-item-type">{typeLabel}</div>}
        </div>
      </div>
    </div>
  );
};
