/**
 * 不对劲 · 小游戏入口
 *
 * 第一版只做一件事：进来就能揭。没有登录、没有选关、没有 Loading。
 * 「十秒可懂」是直玩投放的硬门槛（docs/00-体验目标.md）。
 */
import '@/core/pixiUnsafeEvalPatch';
import { Game } from '@/core/Game';
import { SceneManager } from '@/core/SceneManager';
import { Platform } from '@/core/PlatformService';
import { preloadSceneArt } from '@/core/TextureLoader';
import { PlayScene } from '@/scenes/PlayScene';

declare const GameGlobal: any;

function resolveCanvas(): any {
  if (typeof GameGlobal !== 'undefined' && GameGlobal.canvas) return GameGlobal.canvas;
  if (typeof document === 'undefined') return null;
  let canvas = document.getElementById('game') as HTMLCanvasElement | null;
  if (!canvas) {
    canvas = document.createElement('canvas');
    canvas.id = 'game';
    document.body.appendChild(canvas);
  }
  const g = (typeof GameGlobal !== 'undefined' ? GameGlobal : (window as any).GameGlobal) ?? {};
  g.canvas = canvas;
  (window as any).GameGlobal = g;
  return canvas;
}

async function main(): Promise<void> {
  const canvas = resolveCanvas();
  if (!canvas) {
    console.error('[main] 找不到 canvas');
    return;
  }

  Game.init(canvas);
  if (!Game.app?.renderer) {
    console.error('[main] 渲染器初始化失败');
    return;
  }

  console.log(`[main] 启动 平台=${Platform.name} 版本=${__APP_VERSION__}`);
  preloadSceneArt();

  SceneManager.register(new PlayScene());
  SceneManager.switchTo('play');

  await Game.warmScenePresent();
  if (typeof GameGlobal !== 'undefined') GameGlobal.__gameRendered = true;
}

main().catch((e) => {
  console.error('[main] 启动失败:', e);
});
