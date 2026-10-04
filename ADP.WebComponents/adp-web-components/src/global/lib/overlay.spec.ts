import { isCaged } from './overlay';

describe('overlay ancestor detection', () => {
  let styles: Map<Element, Partial<CSSStyleDeclaration>>;

  beforeEach(() => {
    styles = new Map();
    jest.spyOn(globalThis, 'getComputedStyle').mockImplementation(element => (styles.get(element) ?? {}) as CSSStyleDeclaration);
  });

  afterEach(() => jest.restoreAllMocks());

  it('detects an isolated panel body across a shadow root', () => {
    const body = document.createElement('div');
    const panel = document.createElement('div');
    const overlay = document.createElement('div');
    body.appendChild(panel);
    panel.attachShadow({ mode: 'open' }).appendChild(overlay);
    styles.set(body, { isolation: 'isolate' });

    expect(isCaged(overlay)).toBe(true);
  });

  it('leaves ordinary ancestors in the normal stacking order', () => {
    const body = document.createElement('div');
    const overlay = document.createElement('div');
    body.appendChild(overlay);
    styles.set(body, { isolation: 'auto', overflow: 'hidden', position: 'relative' });

    expect(isCaged(overlay)).toBe(false);
  });
});
