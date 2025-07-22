import yupTypeMapper from '~lib/yup-type-mapper';

const generalTicketTypesSchema = yupTypeMapper([
  'TLP',
  'Complaint',
  'PartInquiry',
  'SalesCampaign',
  'GeneralInquiry',
  'ServiceCampaign',
  'InstallmentPayment',
  'ServicePriceInquiry',
]);

export default generalTicketTypesSchema;
