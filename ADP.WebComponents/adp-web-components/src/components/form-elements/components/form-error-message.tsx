import { FunctionalComponent, h } from '@stencil/core';

import cn from '~lib/cn';

interface FormErrorMessageProps {
  name?: string;
  isError: boolean;
  errorMessage: string;
}

export const FormErrorMessage: FunctionalComponent<FormErrorMessageProps> = ({ errorMessage, isError, name }) => (
  <div part={cn('form-error-message-container', { [`${name}-error-message-container`]: !!name })} class="error-message-container">
    <div
      part={cn('form-error-message', { [`${name}-error-message`]: !!name })}
      class={cn('error-message', {
        'display-error-message': isError,
      })}
    >
      {errorMessage}
    </div>
  </div>
);
