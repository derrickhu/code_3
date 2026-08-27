import * as PIXI from 'pixi.js';
import { Game } from '@/core/Game';
import { Platform } from '@/core/PlatformService';
import type { Scene } from '@/core/SceneManager';
import { buzz, playSfx, unlockSfx } from '@/core/SfxPlayer';
import { Ease, TweenManager } from '@/core/TweenManager';
import { preloadSceneArt } from '@/core/TextureLoader';
import { SCENE_H, SCENE_W, SceneView } from '@/fx/SceneView';
import { wrapWord } from '@/fx/paint';
import { clientEventToDesign } from '@/utils/clientEventToDesign';
import { containsDesignPoint } from '@/utils/hitTestDesign';
import { getTouchCanvas } from '@/utils/touchCanvas';
import {
  createRun,
  currentScene,
  dayMap,
  fitOf,
  hintFor,
  peekNextTitle,
  previewOf,
  ruleFor,
  sceneIndex,
  settleReact,
  skipBrief,
  tick,
  tryExpose,
  winningNames,
  type ExposeAttempt,
  type RunState,
} from '@/game/ExposeEngine';
import { C, FONT } from '@/ui/theme';

function label(text: string, size: number, color: number, weight: '600' | '700' = '600'): PIXI.Text {
  const t = new PIXI.Text(text, {
    fontFamily: FONT,
    fontSize: size,
    fontWeight: weight,
    fill: color,
  });
  t.anchor.set(0.5);
  return t;
}

type ChipHit = { id: string; target: PIXI.Container };

export class PlayScene implements Scene {
  readonly name = 'play';
  readonly container = new PIXI.Container();

  private _state: RunState = createRun();
  private _scene = new SceneView();
  private _caption!: PIXI.Text;
  private _ruleText!: PIXI.Text;
  private _askText!: PIXI.Text;
  private _previewText!: PIXI.Text;
  private _mapText!: PIXI.Text;
  private _progressText!: PIXI.Text;
  private _livesText!: PIXI.Text;
  private _stamps!: PIXI.Graphics;
  private _timerText!: PIXI.Text;
  private _timerFill!: PIXI.Graphics;
  private _recordText!: PIXI.Text;
  private _nudge!: PIXI.Text;
  private _choices = new PIXI.Container();
  private _confirmBtn = new PIXI.Container();
  private _briefLayer = new PIXI.Container();
  private _verdictLayer = new PIXI.Container();
  private _settle = new PIXI.Container();
  private _lastPhase = '';
  private _lastScene = '';
  private _timerTrack = { x: 40, y: 128, w: 670 };
  private _pulse = 0;
  private _startedAt = 0;
  private _unbindTap: (() => void) | null = null;
  private _settleBtn: PIXI.Container | null = null;
  private _verdictBtn: PIXI.Container | null = null;
  private _chips: ChipHit[] = [];
  private _picked: string | null = null;
  private _nudgeUntil = 0;

  onEnter(): void {
    this.container.removeChildren();
    this._state = createRun();
    this._lastPhase = '';
    this._lastScene = '';
    this._pulse = 0;
    this._picked = null;
    this._nudgeUntil = 0;
    this._startedAt = Date.now();
    this._settle.visible = false;
    this._settleBtn = null;
    this._verdictBtn = null;
    preloadSceneArt();
    this._buildChrome();
    this._bindCanvasTap();
    this._sync(true);
    console.log('[expose] run_start', { theme: 'store-day', scene: this._state.sceneId });
  }

  onExit(): void {
    this._unbindTap?.();
    this._unbindTap = null;
    this._scene.dispose();
    this.container.removeChildren();
  }

  update(dt: number): void {
    const before = this._state.phase;
    this._state = tick(this._state, dt);
    this._pulse += dt;
    if (this._nudgeUntil > 0) this._nudgeUntil -= dt;
    if (this._state.phase !== before || this._state.phase === 'play' || this._state.phase === 'react' || this._state.phase === 'brief') {
      this._sync(false);
    }
    this._pulseChips();
    this._nudge.visible = this._state.phase === 'play' && this._nudgeUntil > 0;
  }

  private _H(): number {
    return Game.logicHeight;
  }

  private _buildChrome(): void {
    const H = this._H();
    const top = Game.safeTop;
    const headerY = Game.safeHeaderCenterY;
    const right = Game.contentRightX(12);

    const g = new PIXI.Graphics();
    g.beginFill(C.bg).drawRect(0, 0, 750, H).endFill();
    g.beginFill(0x241e18).drawRect(0, 0, 750, top).endFill();
    this.container.addChild(g);

    const title = label('不对劲', 26, C.paper, '700');
    title.position.set(88, headerY);
    this.container.addChild(title);

    this._livesText = label('告错', 18, C.accentSoft, '600');
    this._livesText.anchor.set(1, 0.5);
    this._livesText.position.set(right - 118, headerY);
    this.container.addChild(this._livesText);
    this._stamps = new PIXI.Graphics();
    this.container.addChild(this._stamps);

    this._progressText = label('', 20, C.mute);
    this._progressText.position.set(200, top + 18);
    this.container.addChild(this._progressText);

    this._ruleText = wrapWord('', 18, C.accentSoft, 700, '600');
    this._ruleText.position.set(375, top + 52);
    this.container.addChild(this._ruleText);

    this._mapText = wrapWord('', 18, C.paper, 700, '600');
    this._mapText.position.set(375, top + 86);
    this.container.addChild(this._mapText);

    this._timerText = label('', 20, C.mute);
    this._timerText.position.set(540, top + 18);
    this.container.addChild(this._timerText);

    this._timerTrack = { x: 40, y: top + 36, w: 670 };
    const timerBg = new PIXI.Graphics();
    timerBg.beginFill(0x2c261f)
      .drawRoundedRect(this._timerTrack.x, this._timerTrack.y, this._timerTrack.w, 8, 4)
      .endFill();
    this.container.addChild(timerBg);
    this._timerFill = new PIXI.Graphics();
    this.container.addChild(this._timerFill);

    this._caption = label('', 32, C.paper, '700');
    this._caption.position.set(375, top + 114);
    this.container.addChild(this._caption);

    const sceneTop = top + 142;
    const avail = Math.max(300, H - sceneTop - Game.safeBottom - 320);
    const scale = Math.min(1, (750 - 56) / SCENE_W, avail / SCENE_H);
    this._scene.root.scale.set(scale);
    this._scene.root.position.set((750 - SCENE_W * scale) / 2, sceneTop);
    this.container.addChild(this._scene.root);

    const under = sceneTop + SCENE_H * scale + 10;
    this._askText = wrapWord('', 24, C.paper, 680, '700');
    this._askText.position.set(375, under + 16);
    this.container.addChild(this._askText);

    this._previewText = wrapWord('', 20, C.accentSoft, 680, '600');
    this._previewText.position.set(375, under + 56);
    this.container.addChild(this._previewText);

    this._choices.position.set(0, under + 70);
    this.container.addChild(this._choices);

    this._confirmBtn.position.set(375, under + 248);
    this.container.addChild(this._confirmBtn);

    this._nudge = label('先点一张状，看预告，再揭开。', 18, C.accentSoft, '600');
    this._nudge.position.set(375, under + 292);
    this._nudge.visible = false;
    this.container.addChild(this._nudge);

    this._recordText = label('', 18, C.mute);
    this._recordText.position.set(375, under + 292);
    this.container.addChild(this._recordText);

    this._briefLayer.visible = false;
    this.container.addChild(this._briefLayer);
    this._verdictLayer.visible = false;
    this.container.addChild(this._verdictLayer);
    this._settle.visible = false;
    this.container.addChild(this._settle);
  }

  private _sync(rebuild: boolean): void {
    const s = this._state;
    const scene = currentScene(s);
    this._progressText.text = `${sceneIndex(s)} ${s.title}`;
    this._ruleText.text = ruleFor(s);
    this._mapText.text = `案底 ${dayMap(s)}`;
    this._timerText.text = s.phase === 'brief' ? '先看现场'
      : s.phase === 'react' ? '看结果'
        : `${Math.ceil(s.timeLeft)} 秒`;
    this._caption.text = s.caption;
    this._caption.style.fill = s.phase === 'react' && s.lastAttempt?.kind === 'trap' ? C.trap
      : s.phase === 'react' && s.lastAttempt?.kind === 'expose' ? C.accentSoft
        : C.paper;
    this._paintStamps(s.missesLeft);

    const playing = s.phase === 'play';
    const picked = playing ? this._pickedProp() : null;
    this._askText.text = playing ? hintFor(s) : '';
    this._askText.visible = playing;
    this._previewText.visible = playing && !!picked;
    this._previewText.text = picked ? previewOf(s, picked) : '';
    if (picked) {
      const fit = fitOf(s, picked);
      this._previewText.style.fill = fit === 'trap' || fit === 'clash' ? C.trap : C.good;
    }
    this._choices.visible = playing;
    this._confirmBtn.visible = playing && !!picked;
    this._recordText.text = s.record.length
      ? `今天揭了 ${winningNames(s).join(' · ')}`
      : '今天还没揭';
    this._recordText.visible = playing && this._nudgeUntil <= 0;

    const ratio = s.phase === 'brief' || s.phase === 'react'
      ? 1
      : Math.max(0, s.timeLeft / Math.max(1, scene.seconds ?? 32));
    this._timerFill.clear();
    this._timerFill.beginFill(ratio < 0.25 && s.phase === 'play' ? C.trap : C.accent);
    this._timerFill.drawRoundedRect(
      this._timerTrack.x,
      this._timerTrack.y,
      this._timerTrack.w * ratio,
      8,
      4,
    );
    this._timerFill.endFill();

    if (rebuild || this._lastPhase !== s.phase || this._lastScene !== s.sceneId) {
      if (s.phase !== 'play' || this._lastScene !== s.sceneId) this._picked = null;
      this._rebuildScene();
      this._rebuildChoices();
      this._rebuildConfirm();
      this._rebuildBrief();
      this._rebuildVerdict();
    }

    if ((s.phase === 'won' || s.phase === 'lost') && this._lastPhase !== s.phase) {
      playSfx(s.phase === 'won' ? 'win' : 'lose');
      this._showSettle();
    }
    this._lastPhase = s.phase;
    this._lastScene = s.sceneId;
  }

  private _pickedProp() {
    if (!this._picked) return null;
    return currentScene(this._state).props.find((p) => p.id === this._picked) ?? null;
  }

  private _paintStamps(left: number): void {
    const right = Game.contentRightX(12);
    const y = Game.safeHeaderCenterY;
    this._stamps.clear();
    for (let i = 0; i < 3; i++) {
      const x = right - 88 + i * 32;
      const alive = i < left;
      this._stamps.beginFill(alive ? C.accent : C.stampDead);
      this._stamps.drawCircle(x, y, 11);
      this._stamps.endFill();
      if (!alive) {
        this._stamps.lineStyle(3, C.trap, 1);
        this._stamps.moveTo(x - 6, y - 6);
        this._stamps.lineTo(x + 6, y + 6);
        this._stamps.moveTo(x + 6, y - 6);
        this._stamps.lineTo(x - 6, y + 6);
      }
    }
  }

  private _rebuildScene(): void {
    this._scene.rebuild(this._state);
  }

  private _rebuildChoices(): void {
    this._choices.removeChildren();
    this._chips = [];
    if (this._state.phase !== 'play') return;
    const scene = currentScene(this._state);
    const exposes = scene.props.filter((p) => p.kind === 'expose').slice();
    exposes.sort((a, b) => {
      const fa = fitOf(this._state, a) === 'clash' ? 0 : 1;
      const fb = fitOf(this._state, b) === 'clash' ? 0 : 1;
      return fa - fb;
    });
    const traps = scene.props.filter((p) => p.kind === 'trap');
    exposes.forEach((prop, i) => {
      this._addChip(prop.id, prop.expose?.claim ?? prop.name, i === 0 ? 196 : 554, 40, fitOf(this._state, prop));
    });
    traps.forEach((prop) => {
      this._addChip(prop.id, `看着怪 · ${prop.name}`, 375, 136, 'trap');
    });
  }

  private _addChip(id: string, claim: string, x: number, y: number, fit: ReturnType<typeof fitOf>): void {
    const on = this._picked === id;
    const fail = fit === 'trap' || fit === 'clash';
    const chip = new PIXI.Container();
    const w = fit === 'trap' ? 400 : 318;
    const h = 80;
    const bg = new PIXI.Graphics();
    const fillColor = on ? (fail ? C.trap : C.accent) : C.paper;
    bg.beginFill(fillColor).drawRoundedRect(-w / 2, -h / 2, w, h, 14).endFill();
    bg.lineStyle(3, on ? C.paper : (fail ? C.trap : C.accent), 0.9);
    bg.drawRoundedRect(-w / 2, -h / 2, w, h, 14);
    chip.addChild(bg);
    const tag = fail ? (fit === 'clash' ? '说反了' : '说得通') : (on ? '已选' : '能揭');
    const name = label(tag, 16, on ? C.paper : (fail ? C.trap : C.accent), '700');
    name.position.set(0, -20);
    chip.addChild(name);
    const body = wrapWord(claim, 20, on ? C.paper : C.ink, w - 36, '700');
    body.position.set(0, 14);
    chip.addChild(body);
    chip.position.set(x, y);
    chip.eventMode = 'static';
    chip.cursor = 'pointer';
    chip.hitArea = new PIXI.Rectangle(-w / 2, -h / 2, w, h);
    this._choices.addChild(chip);
    this._chips.push({ id, target: chip });
  }

  private _rebuildConfirm(): void {
    this._confirmBtn.removeChildren();
    const prop = this._pickedProp();
    if (!prop || this._state.phase !== 'play') {
      this._confirmBtn.visible = false;
      return;
    }
    const fit = fitOf(this._state, prop);
    const fail = fit === 'trap' || fit === 'clash';
    const bg = new PIXI.Graphics();
    bg.beginFill(fail ? C.trap : C.good).drawRoundedRect(-170, -34, 340, 68, 12).endFill();
    this._confirmBtn.addChild(bg);
    this._confirmBtn.addChild(label(fail ? `还要告${prop.name}？` : `揭开${prop.name}`, 24, C.paper, '700'));
    this._confirmBtn.eventMode = 'static';
    this._confirmBtn.cursor = 'pointer';
    this._confirmBtn.hitArea = new PIXI.Rectangle(-170, -34, 340, 68);
    this._confirmBtn.visible = true;
  }

  private _rebuildBrief(): void {
    this._briefLayer.removeChildren();
    const show = this._state.phase === 'brief';
    this._briefLayer.visible = show;
    if (!show) return;
    const H = this._H();
    const dim = new PIXI.Graphics();
    dim.beginFill(0x120f0c, 0.55).drawRect(0, 0, 750, H).endFill();
    this._briefLayer.addChild(dim);
    const card = new PIXI.Graphics();
    card.beginFill(C.paper).drawRoundedRect(48, H * 0.32, 654, 280, 16).endFill();
    this._briefLayer.addChild(card);
    const scene = currentScene(this._state);
    const title = label(scene.title, 22, C.inkSoft, '600');
    title.position.set(375, H * 0.32 + 48);
    this._briefLayer.addChild(title);
    const body = wrapWord(scene.brief, 30, C.ink, 580, '700');
    body.position.set(375, H * 0.32 + 130);
    this._briefLayer.addChild(body);
    const tap = wrapWord('点一下看规则。先选一张状，预告会告诉你告不告得成。', 20, C.accent, 580, '700');
    tap.position.set(375, H * 0.32 + 230);
    this._briefLayer.addChild(tap);
  }

  private _rebuildVerdict(): void {
    this._verdictLayer.removeChildren();
    this._verdictBtn = null;
    const show = this._state.phase === 'react' && !!this._state.lastAttempt;
    this._verdictLayer.visible = show;
    if (!show || !this._state.lastAttempt) return;

    const H = this._H();
    const ok = this._state.lastAttempt.kind === 'expose';
    const dim = new PIXI.Graphics();
    dim.beginFill(0x120f0c, ok ? 0.28 : 0.45).drawRect(0, 0, 750, H).endFill();
    this._verdictLayer.addChild(dim);

    const cardH = 420;
    const py = Math.max(Game.safeTop + 80, H * 0.42);
    const card = new PIXI.Graphics();
    card.beginFill(C.paper).drawRoundedRect(40, py, 670, cardH, 18).endFill();
    card.lineStyle(6, ok ? C.good : C.trap, 1);
    card.drawRoundedRect(40, py, 670, cardH, 18);
    this._verdictLayer.addChild(card);

    const a = this._state.lastAttempt;
    const stampText = !ok
      ? (a.fit === 'clash' ? '和今天说反了' : '这告不成')
      : (a.fit === 'write' ? `今天写成了${a.to}` : '案底接上了');
    const stamp = label(stampText, ok ? 36 : 40, ok ? C.good : C.trap, '700');
    stamp.position.set(375, py + 52);
    this._verdictLayer.addChild(stamp);
    const change = wrapWord(
      ok ? `${a.from} → ${a.to}` : a.name,
      28,
      C.ink,
      600,
      '700',
    );
    change.position.set(375, py + 118);
    this._verdictLayer.addChild(change);

    const why = wrapWord(
      ok ? `${a.line}。${a.sting}` : `${a.line}。${a.sting}`,
      22,
      C.ink,
      600,
      '600',
    );
    why.position.set(375, py + 196);
    this._verdictLayer.addChild(why);

    const extra = wrapWord(
      ok ? `${a.closed}  案底 ${dayMap(this._state)}` : `还能告错 ${this._state.missesLeft} 次。换一处揭。`,
      20,
      ok ? C.inkSoft : C.trap,
      600,
      '600',
    );
    extra.position.set(375, py + 276);
    this._verdictLayer.addChild(extra);

    const btn = new PIXI.Container();
    const bg = new PIXI.Graphics();
    bg.beginFill(ok ? C.good : C.accent).drawRoundedRect(-150, -34, 300, 68, 12).endFill();
    btn.addChild(bg);
    const scene = currentScene(this._state);
    const last = this._state.history[this._state.history.length - 1];
    const hasNext = !!(last?.record && scene.nextByRecord?.[last.record]) || !!scene.defaultNext;
    btn.addChild(label(
      ok ? (hasNext ? `去：${peekNextTitle(this._state)}` : '看今天') : '换一处揭',
      24,
      C.paper,
      '700',
    ));
    btn.position.set(375, py + cardH - 56);
    btn.eventMode = 'static';
    btn.cursor = 'pointer';
    btn.hitArea = new PIXI.Rectangle(-150, -34, 300, 68);
    this._verdictBtn = btn;
    this._verdictLayer.addChild(btn);
  }

  private _bindCanvasTap(): void {
    this._unbindTap?.();
    const canvas = getTouchCanvas();
    const onEnd = (e: Event): void => {
      unlockSfx();
      if (this._settle.visible) {
        if (this._hitSettle(e)) this._restartDay();
        return;
      }
      if (this._state.phase === 'brief') {
        this._skipBrief();
        return;
      }
      if (this._state.phase === 'react') {
        if (this._hitVerdict(e)) this._continueVerdict();
        return;
      }
      if (this._state.phase !== 'play') return;

      if (this._picked && this._hitConfirm(e)) {
        this._commit(this._picked);
        return;
      }
      const chipId = this._hitChip(e);
      if (chipId) {
        this._select(chipId);
        return;
      }
      const propId = this._hitProp(e);
      if (propId) {
        this._select(propId);
        return;
      }
      this._nudgeUntil = 2.2;
      playSfx('ui_tap');
      this._sync(false);
    };
    if (Platform.isMinigame) {
      canvas.addEventListener('touchend', onEnd);
      this._unbindTap = () => canvas.removeEventListener('touchend', onEnd);
      return;
    }
    canvas.addEventListener('pointerup', onEnd);
    this._unbindTap = () => canvas.removeEventListener('pointerup', onEnd);
  }

  private _designOf(e: Event): { x: number; y: number } {
    if (Platform.isMinigame) return clientEventToDesign(e);
    const p = Game.pointerEventToStageLocal(e);
    return { x: p.x, y: p.y };
  }

  private _hitChip(e: Event): string | null {
    const d = this._designOf(e);
    for (const chip of this._chips) {
      if (containsDesignPoint(chip.target, d.x, d.y)) return chip.id;
    }
    return null;
  }

  private _hitConfirm(e: Event): boolean {
    if (!this._confirmBtn.visible) return false;
    const d = this._designOf(e);
    return containsDesignPoint(this._confirmBtn, d.x, d.y);
  }

  private _hitVerdict(e: Event): boolean {
    const btn = this._verdictBtn;
    if (!btn) return false;
    const d = this._designOf(e);
    return containsDesignPoint(btn, d.x, d.y);
  }

  private _hitProp(e: Event): string | null {
    const d = this._designOf(e);
    const local = this._scene.root.toLocal(new PIXI.Point(d.x, d.y), Game.stage);
    return this._scene.propAt(local.x, local.y);
  }

  private _hitSettle(e: Event): boolean {
    const btn = this._settleBtn;
    if (!btn) return false;
    const d = this._designOf(e);
    return containsDesignPoint(btn, d.x, d.y);
  }

  private _select(id: string): void {
    playSfx('ui_tap');
    this._picked = id;
    this._nudgeUntil = 0;
    this._rebuildChoices();
    this._rebuildConfirm();
    const picked = this._pickedProp();
    this._previewText.visible = !!picked;
    this._previewText.text = picked ? previewOf(this._state, picked) : '';
    if (picked) {
      const fit = fitOf(this._state, picked);
      this._previewText.style.fill = fit === 'trap' || fit === 'clash' ? C.trap : C.good;
    }
    this._confirmBtn.visible = !!picked;
    this._recordText.visible = false;
    Game.syncFrameToScreen();
  }

  private _skipBrief(): void {
    playSfx('ui_tap');
    this._state = skipBrief(this._state);
    this._picked = null;
    this._sync(true);
    Game.syncFrameToScreen();
  }

  private _continueVerdict(): void {
    playSfx('ui_tap');
    this._state = settleReact(this._state);
    this._picked = null;
    this._sync(true);
    Game.syncFrameToScreen();
  }

  private _restartDay(): void {
    playSfx('ui_tap');
    const won = this._state.phase === 'won';
    console.log('[expose] run_end', {
      cleared: won,
      names: winningNames(this._state),
      misses: 3 - this._state.missesLeft,
      ms: Date.now() - this._startedAt,
    });
    this.onEnter();
  }

  private _pulseChips(): void {
    if (this._state.phase !== 'play') return;
    this._chips.forEach((chip) => {
      const on = chip.id === this._picked;
      const wave = on ? 0 : Math.sin(this._pulse * 4) * 0.012;
      chip.target.scale.set(1 + wave);
    });
  }

  private _commit(propId: string): void {
    if (this._state.phase !== 'play' && this._state.phase !== 'brief') return;
    const { state, attempt } = tryExpose(this._state, propId);
    this._state = state;
    this._picked = null;
    console.log('[expose] expose_try', {
      scene: attempt.sceneId,
      prop: attempt.propId,
      kind: attempt.kind,
      to: attempt.to,
    });
    if (attempt.kind === 'expose') {
      playSfx('expose');
      playSfx('rewrite', 120);
      buzz('medium');
      console.log('[expose] scene_rewrite', {
        from: attempt.from,
        to: attempt.to,
        record: attempt.record,
      });
      this._slam();
    }
    if (attempt.kind === 'trap') {
      playSfx('trap');
      buzz('heavy');
      this._scene.shake(propId);
    }
    this._sync(true);
    Game.syncFrameToScreen();
  }

  private _slam(): void {
    this._caption.scale.set(1.22);
    TweenManager.to({
      target: this._caption.scale,
      props: { x: 1, y: 1 },
      duration: 0.32,
      ease: Ease.easeOutBack,
    });
  }

  private _showSettle(): void {
    const H = this._H();
    this._settle.removeChildren();
    this._settle.visible = true;
    this._settle.eventMode = 'static';
    this._verdictLayer.visible = false;

    const dim = new PIXI.Graphics();
    dim.beginFill(0x120f0c, 0.86).drawRect(0, 0, 750, H).endFill();
    this._settle.addChild(dim);

    const won = this._state.phase === 'won';
    const names = winningNames(this._state);
    const rows = this._state.history;
    const panelH = Math.min(860, 380 + rows.length * 56);
    const py = Math.max(Game.safeTop + 36, (H - panelH) / 2);
    const panel = new PIXI.Graphics();
    panel.beginFill(C.paper).drawRoundedRect(48, py, 654, panelH, 16).endFill();
    this._settle.addChild(panel);

    const title = label(won ? '这一天是你揭的' : '这场合没揭完', 34, won ? C.good : C.trap, '700');
    title.position.set(375, py + 48);
    this._settle.addChild(title);

    const why = label(
      names.length > 0 ? `靠 ${names.join(' · ')} 写成这样` : '一处都没揭开',
      22,
      C.ink,
      '600',
    );
    why.position.set(375, py + 96);
    this._settle.addChild(why);

    rows.forEach((h: ExposeAttempt, i) => {
      const row = label(`${h.name}：${h.from} → ${h.to}`, 20, C.ink);
      row.position.set(375, py + 150 + i * 48);
      this._settle.addChild(row);
    });

    const note = label(won ? '换一处揭，就是另一天' : '看着怪的，不一定告得成', 20, C.inkSoft, '600');
    note.position.set(375, py + panelH - 112);
    this._settle.addChild(note);

    const btn = new PIXI.Container();
    const bg = new PIXI.Graphics();
    bg.beginFill(C.accent).drawRoundedRect(-140, -36, 280, 72, 12).endFill();
    btn.addChild(bg);
    btn.addChild(label(won ? '再走一条' : '再来一天', 26, C.paper, '700'));
    btn.position.set(375, py + panelH - 52);
    btn.eventMode = 'static';
    btn.cursor = 'pointer';
    btn.hitArea = new PIXI.Rectangle(-140, -36, 280, 72);
    this._settleBtn = btn;
    this._settle.addChild(btn);
    Game.syncFrameToScreen();
  }
}
