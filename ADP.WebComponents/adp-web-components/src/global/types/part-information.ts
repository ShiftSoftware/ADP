export type PartInformation = {
  partNumber: string;
  partDescription: string;
  localDescription: string;
  group: string;
  pnc: string;
  pncLocalName: string;
  binCode: string;
  dimension1: number;
  dimension2: number;
  dimension3: number;
  netWeight: number;
  grossWeight: number;
  cubicMeasure: number;
  hsCode: string;
  uzHsCode: string;
  origin: string;
  supersededTo: string[];
  stockParts: StockPart[];
  prices: PartPrice[];
  deadStock: DeadStock[];
};

export type PartPrice = {
  countryIntegrationID: string;
  countryName: string;
  regionIntegrationID: string;
  regionName: string;
  purchasePrice: UnitData;
  retailPrice: UnitData;
  warrantyPrice: UnitData;
};

type UnitData = {
  value: number;
  cultureName: string;
  formattedValue: string;
  currecntySymbol: string;
};

export type DeadStock = {
  companyIntegrationID: string;
  companyName: string;
  branchDeadStock: BranchDeadStock[];
};

export type BranchDeadStock = {
  companyBranchIntegrationID: string;
  companyBranchName: string;
  quantity: number;
};

export type StockPart = {
  quantityLookUpResult: string;
  locationID: string;
  locationName: string;
};
