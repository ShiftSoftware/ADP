export const MockFiles = {
  'part-lookup': 'generated/standard-dealer/part-lookup.json',
  'vehicle-lookup': 'generated/standard-dealer/vehicle-lookup.json',
} as const;

export type MockFileName = keyof typeof MockFiles;
