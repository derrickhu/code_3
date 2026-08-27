import { describe, expect, it } from 'vitest';
import {
  MAX_MISSES,
  MIN_PROP,
  SLICE_SCENES,
  exposesOf,
  trapsOf,
} from '@/balance/slice';
import {
  createRun,
  currentScene,
  forbiddenKeys,
  hintFor,
  settleReact,
  skipBrief,
  tick,
  tryExpose,
  winningNames,
} from '@/game/ExposeEngine';

function expose(state = createRun(), propId?: string) {
  const scene = currentScene(state);
  const id = propId ?? exposesOf(scene)[0].id;
  return tryExpose(state, id);
}

function playExpose(state = createRun(), propId?: string) {
  return settleReact(expose(state, propId).state);
}

describe('ExposeEngine', () => {
  it('揭招牌把休息改成加班，揭收银把现场写成走账', () => {
    const sign = tryExpose(createRun(), 'sign');
    expect(sign.attempt.kind).toBe('expose');
    expect(sign.state.caption).toBe('今日加班');
    expect(sign.attempt.to).toBe('今日加班');
    expect(sign.state.record).toEqual(['sign']);

    const cash = tryExpose(createRun(), 'cash');
    expect(cash.state.caption).toBe('他在走账');
    expect(cash.attempt.to).not.toBe(sign.attempt.to);
    expect(cash.state.record).toEqual(['cash']);
  });

  it('揭开后停在结果上，不自动切走', () => {
    const { state } = tryExpose(createRun(), 'sign');
    expect(state.phase).toBe('react');
    expect(state.caption).toBe('今日加班');
    expect(tick(state, 3).phase).toBe('react');
    expect(tick(state, 3).sceneId).toBe('store');
    const next = settleReact(state);
    expect(next.sceneId).toBe('overtime');
  });

  it('两种揭法走出两条下一场', () => {
    const overtime = playExpose(createRun(), 'sign');
    expect(overtime.sceneId).toBe('overtime');
    expect(overtime.caption).toBe('谁让店加班');

    const books = playExpose(createRun(), 'cash');
    expect(books.sceneId).toBe('books');
    expect(books.caption).toBe('账对不上');
  });

  it('陷阱告不成，扣机会，口气不动', () => {
    const { state, attempt } = tryExpose(createRun(), 'bread');
    expect(attempt.kind).toBe('trap');
    expect(attempt.line).toBe('这告不成');
    expect(state.caption).toBe('今日休息');
    expect(state.missesLeft).toBe(MAX_MISSES - 1);
    expect(state.history).toHaveLength(0);
  });

  it('点空地不扣机会', () => {
    const { state, attempt } = tryExpose(createRun(), 'floor');
    expect(attempt.kind).toBe('idle');
    expect(state.missesLeft).toBe(MAX_MISSES);
    expect(state.phase).toBe('play');
  });

  it('告错次数用尽判负', () => {
    let state = createRun();
    for (let i = 0; i < MAX_MISSES; i++) {
      state = tryExpose(state, 'bread').state;
      if (state.phase === 'react') state = settleReact(state);
    }
    expect(state.phase).toBe('lost');
    expect(state.missesLeft).toBe(0);
  });

  it('揭完一天则通关，结算能点出物件', () => {
    let state = createRun();
    const path = ['sign', 'roster', 'bill', 'oldsign'];
    for (const id of path) {
      state = playExpose(state, id);
    }
    expect(state.phase).toBe('won');
    expect(winningNames(state)).toEqual(['招牌', '排班表', '账单', '改过的牌']);
  });

  it('收工说反了告不成，对上了才通关', () => {
    let state = createRun();
    for (const id of ['sign', 'roster', 'bill']) {
      state = playExpose(state, id);
    }
    const clash = tryExpose(state, 'dropped');
    expect(clash.attempt.kind).toBe('trap');
    expect(clash.attempt.fit).toBe('clash');
    expect(clash.state.phase).toBe('react');
    state = settleReact(clash.state);
    state = playExpose(state, 'oldsign');
    expect(state.phase).toBe('won');
  });

  it('走账线也能通关，且和加班线揭的不是同一套', () => {
    let state = createRun();
    for (const id of ['cash', 'camera', 'phone', 'dropped']) {
      state = playExpose(state, id);
    }
    expect(state.phase).toBe('won');
    expect(winningNames(state)).toEqual(['收银袖口', '监控', '手机', '掉出来的钱']);
  });

  it('开局先摊开矛盾，两处揭法是两种现场而不是点高亮', () => {
    const run = createRun();
    expect(run.phase).toBe('brief');
    expect(hintFor(run)).toMatch(/休息|灯/);
    const ask = hintFor(skipBrief(run));
    expect(ask).toMatch(/加班/);
    expect(ask).toMatch(/走账/);
    expect(ask).not.toMatch(/都能揭/);
    const sign = SLICE_SCENES[0].props.find((p) => p.id === 'sign')?.expose;
    const cash = SLICE_SCENES[0].props.find((p) => p.id === 'cash')?.expose;
    expect(sign?.claim).toBeTruthy();
    expect(cash?.claim).toBeTruthy();
    expect(sign?.closed).toMatch(/走账/);
    expect(cash?.closed).toMatch(/加班/);
  });

  it('超时判负，失败来自没揭完，不是没找着', () => {
    const state = tick(createRun(), 99);
    expect(state.phase).toBe('lost');
    expect(state.lastAttempt?.line).toBe('这场合没揭完');
  });

  it('状态机没有找茬 / 塔防字段', () => {
    expect(forbiddenKeys(createRun())).toEqual([]);
    const raw = JSON.stringify(createRun());
    expect(raw).not.toMatch(/leftover|remaining|magnifier|lane|leak|baseHp|path/);
  });

  it('每场至少两处能揭、一处陷阱，改写不同，物件够大', () => {
    expect(SLICE_SCENES.length).toBeGreaterThanOrEqual(4);
    for (const scene of SLICE_SCENES) {
      const exposes = exposesOf(scene);
      const traps = trapsOf(scene);
      expect(exposes.length).toBeGreaterThanOrEqual(2);
      expect(traps.length).toBeGreaterThanOrEqual(1);
      expect(exposes[0].expose!.caption).not.toBe(exposes[1].expose!.caption);
      expect(exposes[0].expose!.record).not.toBe(exposes[1].expose!.record);
      for (const prop of scene.props) {
        expect(prop.rect.w).toBeGreaterThanOrEqual(MIN_PROP);
        expect(prop.rect.h).toBeGreaterThanOrEqual(MIN_PROP);
      }
    }
  });
});
