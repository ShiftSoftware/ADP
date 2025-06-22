import { MockJson } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

import { ErrorKeys } from '~features/multi-lingual';

export const vehicleRequestHeaders = {
  cityId: 'City-Id',
  userId: 'User-Id',
  companyId: 'Company-Id',
  customerName: 'Customer-Name',
  customerPhone: 'Customer-Phone',
  customerEmail: 'Customer-Email',
  companyBranchId: 'Company-Branch-Id',
  cityIntegrationId: 'City-Integration-Id',
  brandIntegrationId: 'Brand-Integration-Id',
  companyIntegrationId: 'Company-Integration-Id',
  companyBranchIntegrationId: 'Company-Branch-Integration-Id',
} as const;

type VehicleRequestHeaders = Partial<Record<keyof typeof vehicleRequestHeaders, string>>;

interface VehicleLookupComponent extends VehicleRequestHeaders {
  isDev: boolean;
  baseUrl: string;
  headers: object;
  isError: boolean;
  isLoading: boolean;
  queryString: string;
  errorMessage?: ErrorKeys;
  vehicleLookup?: VehicleLookupDTO;
  mockData: MockJson<VehicleLookupDTO>;

  el: HTMLElement;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  onLoadingChange: (newValue: boolean) => void;
  setErrorMessage: (message: ErrorKeys) => void;
  loadingStateChange?: (isLoading: boolean) => void;
  errorCallback?: (errorMessage: ErrorKeys) => void;
  loadedResponse?: (response: VehicleLookupDTO) => void;
  fetchData: (requestedVin: string, headers?: object) => Promise<void>;
  setMockData: (newMockData: MockJson<VehicleLookupDTO>) => Promise<void>;
  setData: (newData: VehicleLookupDTO | string, headers?: object) => Promise<void>;

  /** static content ( can be extended )
   
  @Prop() isDev: boolean;
  @Prop() baseUrl: string;
  @Prop() headers: object = {};
  @Prop() queryString: string = '';

  @Prop() errorCallback?: (errorMessage: ErrorKeys) => void;
  @Prop() loadingStateChange?: (isLoading: boolean) => void;
  @Prop() loadedResponse?: (response: VehicleLookupDTO) => void;

  @State() isError: boolean = false;
  @State() errorMessage?: ErrorKeys;
  @State() isLoading: boolean = false;
  @State() vehicleLookup?: VehicleLookupDTO;

  @Element() el: HTMLElement;

  mockData;

  abortController: AbortController;
  networkTimeoutRef: ReturnType<typeof setTimeout>;

  @Method()
  async setMockData(newMockData: MockJson<VehicleLookupDTO>) {
    this.mockData = newMockData;
  }

  @Method()
  async setData(newData: VehicleLookupDTO | string, headers: any = {}) {
    await setVehicleLookupData(this, newData, headers);
  }

  @Method()
  async setErrorMessage(message: ErrorKeys) {
    setVehicleLookupErrorState(this, message);
  }

  @Watch('isLoading')
  onLoadingChange(newValue: boolean) {
    if (this.loadingStateChange) this.loadingStateChange(newValue);
  }

  @Method()
  async fetchData(requestedVin: string, headers: any = {}) {
    await this.setData(requestedVin, headers);
  }

  */
}

export default VehicleLookupComponent;
