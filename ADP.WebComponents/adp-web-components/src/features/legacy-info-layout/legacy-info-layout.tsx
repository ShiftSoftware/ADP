import { FunctionalComponent, h } from '@stencil/core';

/**
 * The frame the vehicle-lookup panels used before the wrapper became the design language's card: a
 * 5px-radius box with a grey band carrying the identifier (or the error, in red) and the content
 * below it. Kept for the part-lookup family only, unchanged, until that family is redesigned.
 */
type LegacyInfoLayoutProps = {
  header: string;
  isError: boolean;
  direction: string;
  coreOnly?: boolean;
  isLoading: boolean;
  errorMessage: string;
  headerRight?: any;
};

export const LegacyInfoLayout: FunctionalComponent<LegacyInfoLayoutProps> = (props, children) =>
  props.coreOnly ? (
    <div class={{ loading: props.isLoading }}>{children}</div>
  ) : (
    <div dir={props.direction} part="vehicle-info-container" class={{ 'vehicle-info-container': true, 'loading': props.isLoading }}>
      <div part="vehicle-info-header" class="vehicle-info-header">
        <strong part="vehicle-info-header-vin" class="vehicle-info-header-vin load-animation">
          {props.isError ? (
            <span dir={props.direction} style={{ color: 'red' }}>
              {props.errorMessage}
            </span>
          ) : (
            props.header
          )}
        </strong>
        {props.headerRight && <div class="vehicle-info-header-action">{props.headerRight}</div>}
      </div>

      <div part="vehicle-info-body" class="vehicle-info-body">
        <div part="vehicle-info-content" class="vehicle-info-content">
          {children}
        </div>
      </div>
    </div>
  );
