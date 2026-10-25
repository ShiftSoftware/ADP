import { reviveDormantMeasurements } from './lookup-tabs';

/**
 * A dormant tab is display: none, so a <flexible-container> that measured its content there pinned
 * itself at 0px. Waking the tab releases those, and only those (service history came back as a bare
 * title, 2026-10-25).
 */
const container = (opened: boolean, height: string) => {
  const host = document.createElement('flexible-container') as HTMLElement & { isOpened?: boolean };
  host.isOpened = opened;
  const inner = document.createElement('div');
  inner.className = 'flexible-container';
  inner.style.height = height;
  host.appendChild(inner);
  return { host, inner };
};

describe('reviveDormantMeasurements', () => {
  it('releases an open container pinned at 0, and leaves closed and measured ones alone', () => {
    const root = document.createElement('div');
    const pinned = container(true, '0px');
    const closed = container(false, '0px');
    const measured = container(true, '120px');
    root.append(pinned.host, closed.host, measured.host);

    reviveDormantMeasurements(root);

    expect(pinned.inner.style.height).toBe('auto');
    expect(closed.inner.style.height).toBe('0px');
    expect(measured.inner.style.height).toBe('120px');
  });

  it('reaches containers inside nested shadow roots', () => {
    const root = document.createElement('div');
    const panel = document.createElement('div');
    const shadow = panel.attachShadow({ mode: 'open' });
    const pinned = container(true, '0px');
    shadow.appendChild(pinned.host);
    root.appendChild(panel);

    reviveDormantMeasurements(root);

    expect(pinned.inner.style.height).toBe('auto');
  });
});
