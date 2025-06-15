import { FunctionalComponent, h } from '@stencil/core';

import cn from '~lib/cn';

export interface VehicleInfoLayoutInterface {
  coreOnly?: boolean;
}

interface VehicleInfoLayoutProps {
  vin: string;
  isError: boolean;
  direction: string;
  coreOnly?: boolean;
  isLoading: boolean;
  errorMessage: string;
}

export const VehicleInfoLayout: FunctionalComponent<VehicleInfoLayoutProps> = (props, children) =>
  props.coreOnly ? (
    <div class={cn({ loading: props.isLoading })}>{children}</div>
  ) : (
    <div dir={props.direction} part="vehicle-info-container" class={cn('vehicle-info-container', { loading: props.isLoading })}>
      <div part="vehicle-info-header" class="vehicle-info-header">
        <strong part="vehicle-info-header-vin" class="vehicle-info-header-vin load-animation">
          {props.isError ? (
            <span dir={props.direction} style={{ color: 'red' }}>
              {props.errorMessage}
            </span>
          ) : (
            props.vin
          )}
        </strong>
      </div>

      <div part="vehicle-info-body" class="vehicle-info-body">
        <div part="vehicle-info-content" class="vehicle-info-content">
          {children}
        </div>
      </div>
    </div>
  );
