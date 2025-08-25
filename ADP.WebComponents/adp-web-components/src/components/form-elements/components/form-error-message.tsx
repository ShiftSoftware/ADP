import { FunctionalComponent, h } from '@stencil/core';

import cn from '~lib/cn';

interface FormErrorMessageProps {
  isError: boolean;
  errorMessage: string;
}

export const FormErrorMessage: FunctionalComponent<FormErrorMessageProps> = ({ errorMessage, isError }) => (
  <div
    part="form-error-message"
    class={cn('error-message', {
      'display-error-message': isError,
    })}
  >
    {errorMessage}
  </div>
);
