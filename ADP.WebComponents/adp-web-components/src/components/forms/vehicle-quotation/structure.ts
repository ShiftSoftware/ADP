import { vehicleQuotationElementNames } from './element-mapper';
import { FormElementStructure } from '~features/form-hook';

const tiq: FormElementStructure<vehicleQuotationElementNames> = {
  data: {
    brandId: 'kWrKw',
    vehicleIdQueryParam: 'cId',
    quotationType: 'NewVehiclePurchase',
    recaptchaKey: '6Lehq6IpAAAAAETTDS2Zh60nHIT1a8oVkRtJ2WsA',
    vehicleApi: 'https://tiq-publications-functions.azurewebsites.net/api/models',
    currentVehiclesApi: 'https://tiq-vehicles-functions-staging.azurewebsites.net/api/BrandAndModels',
    requestUrl: 'https://tiq-tickets.azurewebsites.net/api/external/website/vehicle-quotation',
    dealerApi: 'https://tiq-identity-server.azurewebsites.net/api/public/company-branch?brands=TYT&services=new-vehicle-sale',
  },
  tag: 'div',
  id: 'container',
  children: [
    'choose',
    { tag: 'div', id: 'vehicle-wrapper', children: ['vehicle', 'vehicleImage'] },
    { tag: 'hr' },
    'contact information',
    { tag: 'div', id: 'inputs_wrapper', children: ['name', 'phone', 'dealer', 'paymentType'] },
    { tag: 'hr' },
    'current car',
    { tag: 'div', id: 'inputs_wrapper', children: ['ownVehicle', 'currentVehicleBrand', 'currentVehicleModel'] },
    { name: 'submit', id: 'Submit' },
  ],
};

export const VehicleQuotationStructures = { tiq };
