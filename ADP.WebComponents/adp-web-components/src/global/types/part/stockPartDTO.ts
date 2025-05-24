export type stockPartDTO = {
    quantityLookUpResult: 'lookupIsSkipped' | 'available' | 'partiallyAvailable' | 'notAvailable' | 'quantityNotWithinLookupThreshold';
    locationID: string;
    locationName: string;
};