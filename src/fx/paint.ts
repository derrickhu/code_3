import * as PIXI from 'pixi.js';
import { C, FONT } from '@/ui/theme';

export function ink(g: PIXI.Graphics, width = 3.2, color: number = C.ink, alpha = 1): void {
  g.lineStyle({ width, color, alpha, alignment: 0.5, native: false });
}

export function fill(g: PIXI.Graphics, color: number, alpha = 1): void {
  g.beginFill(color, alpha);
}

export function round(
  g: PIXI.Graphics,
  x: number,
  y: number,
  w: number,
  h: number,
  r = 10,
  color: number,
  stroke: number = C.ink,
  sw = 3.2,
): void {
  fill(g, color);
  g.drawRoundedRect(x, y, w, h, r);
  g.endFill();
  ink(g, sw, stroke);
  g.drawRoundedRect(x, y, w, h, r);
}

export function ellipse(
  g: PIXI.Graphics,
  x: number,
  y: number,
  rx: number,
  ry: number,
  color: number,
  sw = 3,
): void {
  fill(g, color);
  g.drawEllipse(x, y, rx, ry);
  g.endFill();
  ink(g, sw);
  g.drawEllipse(x, y, rx, ry);
}

export function word(
  text: string,
  size: number,
  color: number,
  weight: '600' | '700' = '700',
): PIXI.Text {
  const t = new PIXI.Text(text, {
    fontFamily: FONT,
    fontSize: size,
    fontWeight: weight,
    fill: color,
  });
  t.anchor.set(0.5);
  return t;
}

export function wrapWord(
  text: string,
  size: number,
  color: number,
  width: number,
  weight: '600' | '700' = '600',
): PIXI.Text {
  const t = new PIXI.Text(text, {
    fontFamily: FONT,
    fontSize: size,
    fontWeight: weight,
    fill: color,
    wordWrap: true,
    wordWrapWidth: width,
    align: 'center',
    lineHeight: size + 6,
  });
  t.anchor.set(0.5);
  return t;
}

/** 大脑袋小人。面向：'left' | 'right' | 'front' */
export function person(
  g: PIXI.Graphics,
  x: number,
  y: number,
  opts: {
    face?: 'left' | 'right' | 'front';
    cloth?: number;
    hair?: number;
    scale?: number;
  } = {},
): void {
  const s = opts.scale ?? 1;
  const face = opts.face ?? 'front';
  const cloth = opts.cloth ?? C.cloth;
  const hair = opts.hair ?? C.hair;
  const hx = x;
  const hy = y - 38 * s;

  ellipse(g, x, y + 52 * s, 28 * s, 10 * s, 0x000000, 0);
  fill(g, 0x000000, 0.18);
  g.drawEllipse(x, y + 52 * s, 28 * s, 10 * s);
  g.endFill();

  fill(g, cloth);
  g.drawRoundedRect(x - 22 * s, y - 8 * s, 44 * s, 52 * s, 12 * s);
  g.endFill();
  ink(g, 3);
  g.drawRoundedRect(x - 22 * s, y - 8 * s, 44 * s, 52 * s, 12 * s);

  fill(g, C.skin);
  g.drawCircle(hx, hy, 26 * s);
  g.endFill();
  ink(g, 3);
  g.drawCircle(hx, hy, 26 * s);

  fill(g, hair);
  g.drawEllipse(hx, hy - 16 * s, 24 * s, 12 * s);
  g.endFill();

  const eyeY = hy + 2 * s;
  const eyeD = face === 'left' ? -7 * s : face === 'right' ? 7 * s : 0;
  fill(g, C.ink);
  g.drawCircle(hx - 8 * s + eyeD, eyeY, 2.4 * s);
  g.drawCircle(hx + 8 * s + eyeD, eyeY, 2.4 * s);
  g.endFill();

  ink(g, 2);
  g.moveTo(hx - 6 * s, hy + 12 * s);
  g.lineTo(hx + 6 * s, hy + 12 * s);
}
