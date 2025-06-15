export type VehicleInformation = {
  vin: Vin;
  paintThickness: PaintThickness;
  identifiers: Identifiers;
  saleInformation: SaleInformation;
  isAuthorized: boolean;
  warranty?: Warranty;
  accessories: Accessories[];
  nextServiceDate: string;
  serviceHistory: ServiceHistory[];
  ssc?: SSC[] | null;
  vehicleVariantInfo: VehicleVariantInfo;
  vehicleSpecification: VehicleSpecification;
  serviceItems: ServiceItem[];
  basicModelCode: string;
  sscLogId?: string;
};

export type ServiceItemGroup = {
  name?: string;
  tabOrder?: number;
  isDefault?: boolean;
  isSequential?: boolean;
};

export const dev = false;

export type Vin = string;

export type Identifiers = {
  vin: string;
  variant: string;
  katashiki: string;
  color: string;
  trim: string;
  brand: number;
  brandIntegrationID: string;
};

export type SaleInformation = {
  companyID: string;
  companyName: string;
  countryID: string;
  countryName: string;
  branchID: string;
  branchName: string;
  regionID: string;
  customerAccountNumber: string;
  customerID: string;
  invoiceDate: string;
  invoiceNumber?: string;
  warrantyActivationDate: string;
  broker: Broker;
};

export type Broker = {
  brokerId: number;
  brokerName: string;
  customerID: number;
  invoiceNumber?: number;
  invoiceDate: string;
};

export type Warranty = {
  hasActiveWarranty: boolean;
  warrantyStartDate: string;
  warrantyEndDate: string;
  hasExtendedWarranty: boolean;
  extendedWarrantyStartDate: any;
  extendedWarrantyEndDate: any;
};

export type ServiceHistory = {
  serviceType: string;
  serviceDate: string;
  mileage: number;
  companyName: string;
  branchName: string;
  companyId: number;
  branchId: number;
  account: string;
  invoiceNumber?: string;
  jobNumber: number;
  laborLines: LaborLine[];
  partLines: PartLine[];
};

export type LaborLine = {
  rtsCode: string;
  menuCode: string;
  serviceCode: string;
  description: string;
};

export type PartLine = {
  partNumber: string;
  qty: number;
  menuCode: string;
  partDescription: string;
};

export type VehicleVariantInfo = {
  modelCode: string;
  sfx: string;
  modelYear: number;
};

export type VehicleSpecification = {
  modelDesc: string;
  variantDesc: string;
  class: string;
  bodyType: string;
  engine: string;
  cylinders: string;
  lightHeavy: string;
  doors: string;
  fuel: string;
  transmission: string;
  side: string;
  engineType: string;
  tankCap: string;
  style: string;
  fuelLiter: any;
  color: string;
  trim: string;
  productionDate?: string;
};

export type SSC = {
  sscCode: string;
  description: string;
  labors: Labor[];
  repaired: boolean;
  repairDate: string;
  parts: Part[];
};

export type Part = {
  partNumber: string;
  partDescription: any;
  isAvailable: boolean;
};

export type Labor = {
  laborCode: string;
  laborDescription: string;
  allowedTime: number;
};

export interface PaintThickness {
  parts: PaintPart[];
  imageGroups: ImageGroups[];
}

export interface Accessories {
  image: string;
  partNumber: string;
  description: string;
}

export interface ImageGroups {
  name: string;
  images: string[];
}

export interface PaintPart {
  part: string;
  left: number;
  right: number;
}

export type ServiceItem = {
  group?: ServiceItemGroup;
  activatedAt?: string;
  activeFor: number;
  activeForInterval: string;
  campaignCode?: string;
  companyID?: string;
  companyName?: string;
  description?: string;
  expiresAt?: string;
  image?: string;
  invoiceNumber?: string;
  jobNumber?: string;
  maximumMileage?: string;
  modelCostID?: string;
  name?: string;
  packageCode?: string;
  paidServiceInvoiceLineID?: string;
  claimDate?: string;
  serviceItemID?: string;
  skipZeroTrust?: string;
  showDocumentUploader?: boolean;
  documentUploaderIsRequired?: boolean;
  status: 'processed' | 'expired' | 'cancelled' | 'pending' | 'activationRequired';
  claimable: boolean;
  statusEnum?: string;
  title?: string;
  type?: string;
  typeEnum?: string;
  claimingMethodEnum?: number;
};

export type ClaimPayload = {
  vin: string;
  invoice?: string;
  jobNumber?: string;
  qrCode?: string;
  document?: File;
  saleInformation: SaleInformation;
  serviceItem: ServiceItem;
  cancelledServiceItems: ServiceItem[];
};
