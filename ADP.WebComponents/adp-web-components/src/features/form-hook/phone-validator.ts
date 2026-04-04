import { AsYouType } from 'libphonenumber-js';

export type PhoneValidator = AsYouType & {
  default: string;
  formattedOutput: string;
  metadata: {
    numberingPlan: { metadata: [string] };
  };
  geIsValidPhoneNumber: () => boolean;
};
