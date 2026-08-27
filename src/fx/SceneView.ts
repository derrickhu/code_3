import * as PIXI from 'pixi.js';
import type { PropDef, SceneDef } from '@/balance/slice';
import { fillCover, sceneTex, watchArt } from '@/core/TextureLoader';
import { currentScene, type RunState } from '@/game/ExposeEngine';
import { C } from '@/ui/theme';
import { fill, round, word } from './paint';

export const SCENE_W = 662;
export const SCENE_H = 820;

const PLATE: Record<string, Record<string, string>> = {
  store: { '': 'sc_store', sign: 'sc_store_sign', cash: 'sc_store_cash' },
  overtime: { '': 'sc_overtime', roster: 'sc_overtime_roster', badge: 'sc_overtime_badge' },
  books: { '': 'sc_books', camera: 'sc_books_camera', drawer: 'sc_books_drawer' },
  dinner: { '': 'sc_dinner', bill: 'sc_dinner_bill', phone: 'sc_dinner_phone' },
  wrap: { '': 'sc_wrap', oldsign: 'sc_wrap_oldsign', dropped: 'sc_wrap_dropped' },
};

function plaque(parent: PIXI.Container, x: number, y: number, w: number, h: number, text: string, hot = false): void {
  const g = new PIXI.Graphics();
  round(g, x, y, w, h, 12, hot ? C.accentSoft : C.paper, hot ? C.accent : C.ink, hot ? 5 : 3.4);
  fill(g, hot ? C.accent : 0x6b3a22);
  g.drawRect(x + 18, y - 10, 10, 18);
  g.drawRect(x + w - 28, y - 10, 10, 18);
  g.endFill();
  parent.addChild(g);
  const t = word(text, text.length > 6 ? 26 : 34, C.ink, '700');
  t.position.set(x + w / 2, y + h / 2);
  parent.addChild(t);
}

export class SceneView {
  readonly root = new PIXI.Container();
  readonly hits = new Map<string, PIXI.Container>();

  private _art = new PIXI.Container();
  private _hitLayer = new PIXI.Container();
  private _unwatch: (() => void) | null = null;
  private _state: RunState | null = null;

  constructor() {
    this.root.eventMode = 'static';
    this.root.interactiveChildren = true;
    this.root.cursor = 'pointer';
    this.root.hitArea = new PIXI.Rectangle(0, 0, SCENE_W, SCENE_H);
    this.root.addChild(this._art);
    this.root.addChild(this._hitLayer);
    this._unwatch = watchArt(() => {
      if (this._state) this.rebuild(this._state);
    });
  }

  /** 现场本地坐标命中；贴图刷新重建热区后仍可用，不依赖被拆掉的点击盒 */
  propAt(lx: number, ly: number): string | null {
    if (!this._state) return null;
    const scene = currentScene(this._state);
    for (let i = scene.props.length - 1; i >= 0; i--) {
      const prop = scene.props[i];
      const r = prop.rect;
      if (lx >= r.x && lx <= r.x + r.w && ly >= r.y && ly <= r.y + r.h) {
        return prop.id;
      }
    }
    return null;
  }

  dispose(): void {
    this._unwatch?.();
    this._unwatch = null;
  }

  rebuild(state: RunState): void {
    this._state = state;
    this._art.removeChildren();
    this._hitLayer.removeChildren();
    this.hits.clear();
    const scene = currentScene(state);
    const last = state.lastAttempt;
    const rewritten = last?.kind === 'expose' ? last.record : '';
    this._paintPlate(scene.id, rewritten);
    if (scene.id === 'store') {
      plaque(this._art, 160, 64, 340, 108, state.caption, rewritten === 'sign');
    }
    this._hits(scene, rewritten);
  }

  pulse(ids: string[], t: number): void {
    for (const id of ids) {
      const box = this.hits.get(id);
      if (!box) continue;
      box.scale.set(1 + Math.sin(t * 5) * 0.03);
    }
  }

  shake(id: string): void {
    const box = this.hits.get(id);
    if (!box) return;
    box.x += (Math.random() - 0.5) * 10;
  }

  private _paintPlate(sceneId: string, rewritten: string): void {
    const table = PLATE[sceneId] ?? PLATE.wrap;
    const id = table[rewritten] ?? table[''];
    const texture = sceneTex(id) ?? sceneTex(table['']);
    const g = new PIXI.Graphics();
    if (texture) {
      fillCover(g, texture, 0, 0, SCENE_W, SCENE_H);
    } else {
      g.beginFill(C.night).drawRoundedRect(0, 0, SCENE_W, SCENE_H, 18).endFill();
    }
    g.lineStyle(4, C.ink, 0.85);
    g.drawRoundedRect(2, 2, SCENE_W - 4, SCENE_H - 4, 18);
    this._art.addChild(g);
  }

  private _hits(scene: SceneDef, rewritten: string): void {
    for (const prop of scene.props) {
      const box = new PIXI.Container();
      box.position.set(prop.rect.x + prop.rect.w / 2, prop.rect.y + prop.rect.h / 2);
      const g = new PIXI.Graphics();
      const hot = rewritten === prop.expose?.record;
      g.beginFill(0xffffff, 0.001);
      g.drawRoundedRect(-prop.rect.w / 2, -prop.rect.h / 2, prop.rect.w, prop.rect.h, 14);
      g.endFill();
      if (hot) {
        g.lineStyle(5, C.accent, 0.95);
        g.drawRoundedRect(-prop.rect.w / 2, -prop.rect.h / 2, prop.rect.w, prop.rect.h, 14);
      }
      box.addChild(g);
      if (hot) {
        const stamp = word('揭开了', 20, C.accent, '700');
        stamp.position.set(0, -prop.rect.h / 2 + 22);
        box.addChild(stamp);
      }
      box.eventMode = 'none';
      box.hitArea = new PIXI.Rectangle(
        -prop.rect.w / 2,
        -prop.rect.h / 2,
        prop.rect.w,
        prop.rect.h,
      );
      (box as PIXI.Container & { __prop?: PropDef }).__prop = prop;
      this._hitLayer.addChild(box);
      this.hits.set(prop.id, box);
    }
  }
}
