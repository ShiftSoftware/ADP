export type StockPartDTO = {
    quantityLookUpResult: 'LookupIsSkipped' | 'Available' | 'PartiallyAvailable' | 'NotAvailable' | 'QuantityNotWithinLookupThreshold';
    locationID: string;
    locationName: string;
};