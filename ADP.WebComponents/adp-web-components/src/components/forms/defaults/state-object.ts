import { PhoneValidator } from 'components';

type StateObject = { phoneValidator: PhoneValidator };

export const getDefaultStateObject = (): StateObject => ({ phoneValidator: undefined });
