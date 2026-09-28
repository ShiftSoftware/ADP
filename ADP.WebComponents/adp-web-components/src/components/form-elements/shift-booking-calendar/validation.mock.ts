// validation.ts declares a top-level `require`, which Jest's CommonJS wrapper cannot load. Imported first by the specs that reach the form layer.
jest.mock('../../forms/defaults/validation', () => {
  const key = (suffix: string) => (name: string) => `${name}-${suffix}`;
  const keys = { label: key('label'), format: key('format'), require: key('require'), size: key('size'), max: key('max'), upload: key('upload'), placeholder: key('placeholder') };

  return { y: { ...keys, condition: (name: string) => `$${name}Required`, meta: (name: string) => ({ label: keys.label(name), placeholder: keys.placeholder(name) }) } };
});
