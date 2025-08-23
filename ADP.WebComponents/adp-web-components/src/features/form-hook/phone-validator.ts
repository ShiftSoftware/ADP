import { AsYouType } from 'libphonenumber-js';

export const getPhoneValidator = () => {
  const phoneValidator = new AsYouType('IQ') as AsYouType & {
    default: string;
    metadata: {
      numberingPlan: { metadata: [string] };
    };
  };
  phoneValidator.default = '+' + phoneValidator.metadata.numberingPlan.metadata[0];

  phoneValidator.input(phoneValidator.default);

  return phoneValidator;
};
