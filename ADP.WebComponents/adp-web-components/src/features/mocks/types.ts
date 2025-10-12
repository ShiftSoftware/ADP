export const MockFiles = {
  'part-lookup': 'part-lookup.json',
  'vehicle-lookup': 'vehicle-lookup.json',
} as const;

export type MockFileName = keyof typeof MockFiles;
