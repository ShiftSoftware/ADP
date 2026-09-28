export interface Box {
  top: number;
  left: number;
  bottom: number;
  right: number;
}

export interface Size {
  width: number;
  height: number;
}

export interface Placement {
  top: number;
  left: number;
  side: 'below' | 'above';
  maxHeight: number | null;
}

export interface PlaceOptions {
  offset: number;
  margin: number;
  rtl: boolean;
}

const clamp = (value: number, low: number, high: number) => Math.max(low, Math.min(value, Math.max(low, high)));

export function placePanel(anchor: Box, panel: Size, viewport: Size, { offset, margin, rtl }: PlaceOptions): Placement {
  const roomBelow = Math.max(0, viewport.height - margin - (anchor.bottom + offset));
  const roomAbove = Math.max(0, anchor.top - offset - margin);

  // Never over the anchor: without room on either side the panel takes the larger one and scrolls inside.
  const side: Placement['side'] = panel.height <= roomBelow ? 'below' : panel.height <= roomAbove || roomAbove > roomBelow ? 'above' : 'below';
  const height = Math.min(panel.height, side === 'below' ? roomBelow : roomAbove);
  const top = side === 'below' ? anchor.bottom + offset : anchor.top - offset - height;
  const left = clamp(rtl ? anchor.right - panel.width : anchor.left, margin, viewport.width - margin - panel.width);

  return { top: Math.round(top), left: Math.round(left), side, maxHeight: height < panel.height ? Math.floor(height) : null };
}

export function intersect(a: Box, b: Box): Box | null {
  const box = { top: Math.max(a.top, b.top), left: Math.max(a.left, b.left), bottom: Math.min(a.bottom, b.bottom), right: Math.min(a.right, b.right) };

  return box.bottom > box.top && box.right > box.left ? box : null;
}

export function visiblePart(anchor: Box, clips: Box[]): Box | null {
  return clips.reduce<Box | null>((visible, clip) => (visible ? intersect(visible, clip) : null), anchor);
}
