import yupTypeMapper from '~lib/yup-type-mapper';

const generalTicketTypesSchema = yupTypeMapper(['TLP', 'PartInquiry', 'SalesCampaign', 'ServiceCampaign', 'InstallmentPayment', 'ServicePriceInquiry']);

export default generalTicketTypesSchema;
