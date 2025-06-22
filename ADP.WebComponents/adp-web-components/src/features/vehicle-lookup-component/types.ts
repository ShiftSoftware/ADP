import { MockJson } from '~types/components';
import { VehicleLookupDTO } from '~types/generated/vehicle-lookup/vehicle-lookup-dto';

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

export type VehicleRequestHeaders = Partial<Record<keyof typeof vehicleRequestHeaders, string>>;

export type VehicleLookupMock = MockJson<VehicleLookupDTO>;
