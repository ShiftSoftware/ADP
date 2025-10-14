import { FunctionalComponent, h } from '@stencil/core';

import cn from '~lib/cn';

import deadStockSchema from '~locales/partLookup/deadStock/type';

import { ComponentLocale } from '~features/multi-lingual';

import { DeadStockDTO } from '~types/generated/part/dead-stock-dto';

interface DeadStockItemProps {
  key?: string;
  icon?: Node;
  isOpened?: boolean;
  item?: DeadStockDTO;
  toggleAccordion: (name: string) => void;
  locale: ComponentLocale<typeof deadStockSchema>;
}

export const DeadStockItem: FunctionalComponent<DeadStockItemProps> = ({ isOpened, item, key, icon, locale, toggleAccordion }) => (
  <div key={key} class="dead-stock-item">
    <button
      onClick={() => toggleAccordion(item?.companyName)}
      class={cn('button', {
        'cursor-default': !item?.branchDeadStock?.length,
        'bg-slate-100': isOpened,
      })}
    >
      <div class="shift-skeleton flex-1 header">{item?.companyName ? item?.companyName : <span>&nbsp;</span>}</div>
      <div
        class={cn('shift-skeleton icon-container', {
          'rotate-0': isOpened,
        })}
      >
        {icon}
      </div>
    </button>
    {!!item?.branchDeadStock?.length && (
      <flexible-container isOpened={isOpened}>
        <table class="dead-stock-table">
          <thead>
            <tr>
              <th class="dead-stock-table-header">{locale.branch}</th>
              <th class="dead-stock-table-header">{locale.availableQuantity}</th>
            </tr>
          </thead>

          <tbody>
            {item?.branchDeadStock?.map(branchDeadStock => (
              <tr
                class="dead-stock-table-row"
                // @ts-ignore
                key={branchDeadStock?.companyBranchIntegrationID}
              >
                <td>
                  <span class="shift-skeleton">{branchDeadStock?.companyBranchName}</span>
                </td>

                <td>
                  <strong class="shift-skeleton">{branchDeadStock?.quantity}</strong>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </flexible-container>
    )}
  </div>
);
