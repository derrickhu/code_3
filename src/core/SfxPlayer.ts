/**
 * 切片短音效。文件在包内 `audio/*.mp3`。
 * 微信走 InnerAudioContext；浏览器走 HTMLAudio，第一次点击解锁。
 */
import { Platform } from '@/core/PlatformService';

const FILE: Readonly<Record<string, string>> = {
  ui_tap: 'audio/ui_tap.mp3',
  expose: 'audio/expose.mp3',
  trap: 'audio/trap.mp3',
  rewrite: 'audio/rewrite.mp3',
  win: 'audio/win.mp3',
  lose: 'audio/lose.mp3',
};

export type SfxName = keyof typeof FILE;

const lastAt = new Map<string, number>();
const dead = new Set<string>();
let unlocked = false;

export function unlockSfx(): void {
  if (unlocked) return;
  unlocked = true;
  try {
    const AC = (globalThis as { AudioContext?: typeof AudioContext; webkitAudioContext?: typeof AudioContext }).AudioContext
      ?? (globalThis as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (AC) {
      const ctx = new AC();
      void ctx.resume();
    }
  } catch {
    /* 小游戏没有 AudioContext 也没关系 */
  }
}

export function playSfx(name: SfxName, gapMs = 80): void {
  const src = dead.has(name) ? undefined : FILE[name];
  if (!src) return;
  const now = Date.now();
  if ((lastAt.get(name) ?? 0) + gapMs > now) return;
  lastAt.set(name, now);
  try {
    const ctx = Platform.createInnerAudioContext();
    if (!ctx) return;
    ctx.src = src;
    ctx.volume = 0.78;
    ctx.play();
    const drop = (): void => {
      try { ctx.destroy(); } catch { /* */ }
    };
    ctx.onEnded(drop);
    ctx.onError(() => {
      dead.add(name);
      drop();
    });
  } catch {
    /* 开发者工具没文件时不炸 */
  }
}

export function buzz(kind: 'light' | 'medium' | 'heavy' = 'light'): void {
  Platform.vibrateShort(kind);
}
