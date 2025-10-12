import { fakerEN as faker } from '@faker-js/faker';
import { maybe } from '~lib/maybe';
import { BranchDeadStockDTO } from '~types/generated/part/branch-dead-stock-dto';
import { DeadStockDTO } from '~types/generated/part/dead-stock-dto';
import { HSCodeDTO } from '~types/generated/part/hs-code-dto';
import { PartPriceDTO } from '~types/generated/part/part-price-dto';
import { PriceDTO } from '~types/generated/part/price-dto';
import { StockPartDTO } from '~types/generated/part/stock-part-dto';
import { PartLookupDTO } from '~types/generated/part/part-lookup-dto';

// Helpers
function fakePrice(): PriceDTO {
  const value = maybe(faker.number.float({ min: 5, max: 500 }));
  const currency = faker.finance.currencyCode();
  return {
    value,
    currecntySymbol: currency,
    cultureName: 'en-US',
    formattedValue: `${currency} ${value?.toFixed(2)}`,
  };
}

function fakeStock(): StockPartDTO {
  const statuses: StockPartDTO['quantityLookUpResult'][] = ['Available', 'PartiallyAvailable', 'NotAvailable', 'LookupIsSkipped', 'QuantityNotWithinLookupThreshold'];
  return {
    locationID: faker.string.uuid(),
    locationName: faker.location.city(),
    quantityLookUpResult: faker.helpers.arrayElement(statuses),
  };
}

function fakeHSCode(): HSCodeDTO {
  return {
    countryName: faker.location.country(),
    countryIntegrationID: faker.string.uuid(),
    hsCode: faker.string.alphanumeric(8).toUpperCase(),
  };
}

function fakeDeadStock(): DeadStockDTO {
  const branchDeadStock: BranchDeadStockDTO[] = Array.from({ length: faker.number.int({ min: 1, max: 3 }) }, () => ({
    companyBranchIntegrationID: faker.string.uuid(),
    companyBranchName: faker.company.name(),
    quantity: faker.number.int({ min: 0, max: 50 }),
  }));

  return {
    companyIntegrationID: faker.string.uuid(),
    companyName: faker.company.name(),
    branchDeadStock,
  };
}

function fakePartPrice(): PartPriceDTO {
  return {
    countryID: faker.string.uuid(),
    countryName: faker.location.country(),
    regionID: faker.string.uuid(),
    regionName: faker.location.state(),
    retailPrice: fakePrice(),
    purchasePrice: fakePrice(),
    warrantyPrice: fakePrice(),
  };
}

export function fakePartLookupData(count = 2, overrides?: Partial<PartLookupDTO>): Record<string, PartLookupDTO> {
  const items: Record<string, PartLookupDTO> = {};

  for (let i = 1; i <= count; i++) {
    const partNumber = faker.string.alphanumeric(8).toUpperCase();
    const partQuantity = faker.number.int({ min: 1, max: 5 });

    const item: PartLookupDTO = {
      partNumber,
      partDescription: faker.commerce.productName(),
      localDescription: faker.commerce.productDescription(),
      productGroup: faker.commerce.department(),
      pnc: faker.string.alphanumeric(5).toUpperCase(),
      binType: faker.helpers.arrayElement(['A', 'B', 'C']),
      distributorPurchasePrice: maybe(faker.number.float({ min: 10, max: 500 })),
      length: maybe(faker.number.float({ min: 5, max: 200 })),
      width: maybe(faker.number.float({ min: 5, max: 200 })),
      height: maybe(faker.number.float({ min: 5, max: 200 })),
      netWeight: maybe(faker.number.float({ min: 1, max: 100 })),
      grossWeight: maybe(faker.number.float({ min: 1, max: 120 })),
      cubicMeasure: maybe(faker.number.float({ min: 0.1, max: 2 })),
      hsCode: faker.string.alphanumeric(8).toUpperCase(),
      origin: faker.location.country(),
      supersededTo: Array.from({ length: faker.number.int({ min: 0, max: 3 }) }, () => faker.string.alphanumeric(8).toUpperCase()),
      stockParts: Array.from({ length: faker.number.int({ min: 1, max: 4 }) }, fakeStock),
      prices: Array.from({ length: faker.number.int({ min: 1, max: 3 }) }, fakePartPrice),
      hsCodes: Array.from({ length: faker.number.int({ min: 1, max: 3 }) }, fakeHSCode),
      deadStock: Array.from({ length: faker.number.int({ min: 1, max: 2 }) }, fakeDeadStock),
      logId: maybe(faker.string.uuid()),
      ...overrides,
    };

    if (!partQuantity) items[partNumber] = item;
    else items[`${partNumber}/${partQuantity}`] = item;
  }

  return items;
}
