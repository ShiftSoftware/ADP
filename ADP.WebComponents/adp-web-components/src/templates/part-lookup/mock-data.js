const mockData = {
  'SU00302474': {
    partNumber: 'SU00302474',
    partDescription: 'CLAMP',
    localDescription: 'Неприменимо',
    productGroup: 'F',
    pnc: '',
    pnc: '',
    binType: null,
    length: 80,
    width: 50,
    height: 15,
    netWeight: 250,
    grossWeight: null,
    cubicMeasure: 600,
    hsCode: null,
    hsCode: null,
    origin: 'JP',
    supersededTo: ['1110109263', '137410PH00', '138010PH00', '139350PH00'],
    stockParts: [
      {
        locationName: 'Besten Stock',
        quantityLookUpResult: 'Available',
      },
      {
        locationName: 'Besten Stock',
        quantityLookUpResult: 'Available',
      },
      {
        locationName: 'Besten Stock',
        quantityLookUpResult: 'PartiallyAvailable',
      },
      {
        locationName: 'Besten Stock 2',
        quantityLookUpResult: 'PartiallyAvailable',
      },
    ],
    prices: [
      {
        countryIntegrationID: 'TM',
        countryName: 'Turkmenistan ',
        regionIntegrationID: 'TK',
        regionName: 'Turkmenistan ',
        warrantyPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$12.49',
        },
        purchasePrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$22.49',
        },
        retailPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$21.49',
        },
      },
      {
        countryIntegrationID: 'TJ',
        countryName: 'Tajikistan',
        regionIntegrationID: 'TJ',
        regionName: 'Tajikistan',
        warrantyPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: null,
        },
        purchasePrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$23.49',
        },
        retailPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$22.49',
        },
      },
      {
        countryIntegrationID: 'UZ',
        countryName: 'Uzbekistan',
        regionIntegrationID: 'UZ',
        regionName: 'Uzbekistan',
        warrantyPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$23.49',
        },
        purchasePrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$42.49',
        },
        retailPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: null,
        },
      },
    ],
    deadStock: [
      {
        companyIntegrationID: 'COMP-1001',
        companyName: 'Alpha Motors',
        branchDeadStock: [
          { companyBranchIntegrationID: 'BR-1001', companyBranchName: 'North Branch', quantity: 12 },
          { companyBranchIntegrationID: 'BR-1002', companyBranchName: 'East Branch', quantity: 5 },
        ],
      },
      {
        companyIntegrationID: 'COMP-1002',
        companyName: 'Beta Auto Supplies',
        branchDeadStock: [{ companyBranchIntegrationID: 'BR-2001', companyBranchName: 'Main Branch', quantity: 33 }],
      },
      {
        companyIntegrationID: 'COMP-1003',
        companyName: 'Gamma Car Parts',
        branchDeadStock: [
          { companyBranchIntegrationID: 'BR-3001', companyBranchName: 'City Center', quantity: 20 },
          { companyBranchIntegrationID: 'BR-3002', companyBranchName: 'Suburb Branch', quantity: 11 },
        ],
      },
      {
        companyIntegrationID: 'COMP-1004',
        companyName: 'Delta Garage Supplies',
        branchDeadStock: [{ companyBranchIntegrationID: 'BR-4001', companyBranchName: 'Depot 1', quantity: 9 }],
      },
      {
        companyIntegrationID: 'COMP-1005',
        companyName: 'Epsilon Mechanics',
        branchDeadStock: [
          { companyBranchIntegrationID: 'BR-5001', companyBranchName: 'Warehouse A', quantity: 14 },
          { companyBranchIntegrationID: 'BR-5002', companyBranchName: 'Warehouse B', quantity: 7 },
        ],
      },
      {
        companyIntegrationID: 'COMP-1006',
        companyName: 'Zeta Tools',
        branchDeadStock: [{ companyBranchIntegrationID: 'BR-6001', companyBranchName: 'West Station', quantity: 6 }],
      },
      {
        companyIntegrationID: 'COMP-1007',
        companyName: 'Eta Engineering',
        branchDeadStock: [{ companyBranchIntegrationID: 'BR-7001', companyBranchName: 'Garage Alpha', quantity: 18 }],
      },
      {
        companyIntegrationID: 'COMP-1008',
        companyName: 'Theta Parts Co.',
        branchDeadStock: [
          { companyBranchIntegrationID: 'BR-8001', companyBranchName: 'South Branch', quantity: 4 },
          { companyBranchIntegrationID: 'BR-8002', companyBranchName: 'Northwest Branch', quantity: 21 },
        ],
      },
      {
        companyIntegrationID: 'COMP-1009',
        companyName: 'Iota Car Solutions',
        branchDeadStock: [{ companyBranchIntegrationID: 'BR-9001', companyBranchName: 'Market Road', quantity: 27 }],
      },
      {
        companyIntegrationID: 'COMP-1010',
        companyName: 'Kappa Auto Inc.',
        branchDeadStock: [
          { companyBranchIntegrationID: 'BR-10001', companyBranchName: 'Tech Park Branch', quantity: 13 },
          { companyBranchIntegrationID: 'BR-10002', companyBranchName: 'Service Lane Branch', quantity: 15 },
        ],
      },
    ],
  },
  '0400007660/1': {
    partNumber: '0400007660',
    partDescription: 'REPLACEMENT KIT,',
    localDescription: 'Неприменимо',
    productGroup: 'J',
    pnc: '',
    pnc: '',
    binType: null,
    length: 75,
    width: 60,
    height: 10,
    netWeight: 226,
    grossWeight: null,
    cubicMeasure: 450,
    hsCode: null,
    hsCode: null,
    origin: 'JP',
    supersededTo: ['1110109263', '137410PH00', '138010PH00', '139350PH00'],
    stockParts: [
      {
        quantityLookUpResult: 'Available',
        locationID: 'UZ-UZ-BS',
        locationName: 'Besten Stock',
      },
    ],
    prices: [
      {
        countryIntegrationID: 'TM',
        countryName: 'Turkmenistan ',
        regionIntegrationID: 'TK',
        regionName: 'Turkmenistan ',
        warrantyPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$24.49',
        },
        purchasePrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$25.49',
        },
        retailPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$23.49',
        },
      },
      {
        countryIntegrationID: 'TJ',
        countryName: 'Tajikistan',
        regionIntegrationID: 'TJ',
        regionName: 'Tajikistan',
        warrantyPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$25.49',
        },
        purchasePrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$26.49',
        },
        retailPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$22.49',
        },
      },
      {
        countryIntegrationID: 'UZ',
        countryName: 'Uzbekistan',
        regionIntegrationID: 'UZ',
        regionName: 'Uzbekistan',
        warrantyPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$26.49',
        },
        purchasePrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$21.49',
        },
        retailPrice: {
          value: 2.4875,
          currecntySymbol: '$',
          cultureName: 'en-US',
          formattedValue: '$21.49',
        },
      },
    ],
    deadStock: null,
    logId: 'd864e3b6-4b6b-4d22-be99-7d64063446eb',
  },
};
