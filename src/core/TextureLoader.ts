/**
 * 本地贴图。微信必须走 createImage + src，不能 PIXI.Texture.from(路径)。
 * 浏览器走同一套 createImage（Platform 会退回 new Image）。
 * 路径一律 `images/xxx.jpg`，微信包和 vite publicDir 都挂在这个相对根。
 */
import * as PIXI from 'pixi.js';
import { Platform } from '@/core/PlatformService';

const cache = new Map<string, PIXI.Texture>();
const missing = new Set<string>();
const inflight = new Set<string>();
const readyWatchers = new Set<() => void>();

export const SCENE_PLATES = [
  'sc_store',
  'sc_store_sign',
  'sc_store_cash',
  'sc_overtime',
  'sc_overtime_roster',
  'sc_overtime_badge',
  'sc_books',
  'sc_books_camera',
  'sc_books_drawer',
  'sc_dinner',
  'sc_dinner_bill',
  'sc_dinner_phone',
  'sc_wrap',
  'sc_wrap_oldsign',
  'sc_wrap_dropped',
] as const;

export function watchArt(fn: () => void): () => void {
  readyWatchers.add(fn);
  return () => { readyWatchers.delete(fn); };
}

function notifyReady(): void {
  for (const fn of readyWatchers) fn();
}

export function platePath(id: string): string {
  return `images/${id}.jpg`;
}

export function tex(path: string): PIXI.Texture | null {
  const hit = cache.get(path);
  if (hit?.baseTexture.valid) return hit;
  if (!missing.has(path)) kick(path);
  return null;
}

export function sceneTex(id: string): PIXI.Texture | null {
  return tex(platePath(id));
}

export function preloadSceneArt(): void {
  for (const id of SCENE_PLATES) kick(platePath(id));
}

function kick(path: string): void {
  if (cache.has(path) || missing.has(path) || inflight.has(path)) return;
  inflight.add(path);

  const img = Platform.createImage();
  if (img) {
    img.onload = () => {
      try {
        cache.set(path, new PIXI.Texture(PIXI.BaseTexture.from(img)));
        notifyReady();
      } catch {
        missing.add(path);
      }
      inflight.delete(path);
    };
    img.onerror = () => {
      missing.add(path);
      inflight.delete(path);
    };
    img.src = path;
    return;
  }

  missing.add(path);
  inflight.delete(path);
}

export function fillCover(
  g: PIXI.Graphics,
  texture: PIXI.Texture,
  x: number,
  y: number,
  w: number,
  h: number,
): void {
  const tw = texture.width || 1;
  const th = texture.height || 1;
  if (tw <= 1 || th <= 1) return;
  const scale = Math.max(w / tw, h / th);
  const matrix = new PIXI.Matrix();
  matrix.scale(scale, scale);
  matrix.translate(x + (w - tw * scale) / 2, y + (h - th * scale) / 2);
  g.beginTextureFill({ texture, matrix });
  g.drawRoundedRect(x, y, w, h, 18);
  g.endFill();
}
