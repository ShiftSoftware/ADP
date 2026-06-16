import { object, InferType } from 'yup';

import yupTypeMapper from '~lib/yup-type-mapper';

export const claimFormSchema = yupTypeMapper([
  'uploadSingleDocument',
  'uploadMultipleDocument',
  'uploadHint',
  'serviceType',
  'name',
  'vin',
  'print',
  'expand',
  'documentLimitError',
  'documentRequiredError',
  'activationDate',
  'expireDate',
  'packageCode',
  'scanTheVoucher',
  'scanHint',
  'qrCode',
  'processing',
  'enterServiceInfo',
  'invoice',
  'jobNumber',
  'claim',
  'claimFailed',
  'ok',
]);

export type ClaimFormType = InferType<typeof claimFormSchema>;

const claimableItemsSchema = object({
  claimForm: claimFormSchema,
}).concat(
  yupTypeMapper([
    'serviceType',
    'activationDate',
    'expireDate',
    'claimAt',
    'claimingCompany',
    'invoiceNumber',
    'jobNumber',
    'packageCode',
    'claim',
    'processed',
    'expired',
    'cancelled',
    'pending',
    'warning',
    'activateNow',
    'print',
    'successFulClaimMessage',
    'activationRequired',
    'warrantyAndServicesNotActivated',
    'activationBlockedNotAllocated',
    'viewTrace',
    'traceTitle',
    'traceLoading',
    'traceFailed',
  ]),
);

export default claimableItemsSchema;
