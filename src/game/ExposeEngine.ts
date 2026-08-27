/**
 * 揭状规则。渲染层只读这里的状态。
 * 状态机里不允许出现 leftover / remaining / lane / leak / baseHp。
 */
import {
  FIRST_SECONDS,
  MAX_MISSES,
  ROUND_SECONDS,
  SLICE_SCENES,
  sceneById,
  type PropDef,
  type SceneDef,
} from '@/balance/slice';

export type TryKind = 'expose' | 'trap' | 'idle';
export type DayThread = 'overtime' | 'books' | '';
export type Fit = 'write' | 'ok' | 'clash' | 'trap' | 'idle';

const OVERTIME_MARK = new Set(['sign', 'roster', 'badge']);
const BOOKS_MARK = new Set(['cash', 'camera', 'drawer']);

export type Phase = 'brief' | 'play' | 'react' | 'won' | 'lost';

export interface ExposeAttempt {
  kind: TryKind;
  sceneId: string;
  propId: string;
  name: string;
  claim: string;
  line: string;
  sting: string;
  closed: string;
  from: string;
  to: string;
  record: string;
  fit: Fit;
}

export interface RunState {
  phase: Phase;
  sceneId: string;
  title: string;
  source: string;
  caption: string;
  hint: string;
  record: string[];
  missesLeft: number;
  timeLeft: number;
  history: ExposeAttempt[];
  lastAttempt: ExposeAttempt | null;
  reactUntil: number;
  briefUntil: number;
}

export interface TryResult {
  state: RunState;
  attempt: ExposeAttempt;
}

/** 揭开/告错必须看结果再点继续，不靠倒计时偷偷切走 */
const REACT: Record<TryKind, number> = {
  expose: 99,
  trap: 99,
  idle: 0.2,
};

export const BRIEF_SECONDS = 2.6;

function clone<T>(v: T): T {
  return JSON.parse(JSON.stringify(v)) as T;
}

export function currentScene(state: RunState): SceneDef {
  return sceneById(state.sceneId);
}

export function sceneSeconds(scene: SceneDef): number {
  return scene.seconds ?? ROUND_SECONDS;
}

function captionOf(scene: SceneDef, record: string[]): string {
  for (let i = record.length - 1; i >= 0; i--) {
    const hit = scene.captionByRecord?.[record[i]];
    if (hit) return hit;
  }
  return scene.caption;
}

function applyScene(state: RunState, id: string): void {
  const scene = sceneById(id);
  state.sceneId = scene.id;
  state.title = scene.title;
  state.source = scene.source;
  state.caption = captionOf(scene, state.record);
  state.hint = scene.ask;
  state.timeLeft = sceneSeconds(scene);
  state.lastAttempt = null;
  state.phase = 'brief';
  state.briefUntil = BRIEF_SECONDS;
}

export function createRun(): RunState {
  const state: RunState = {
    phase: 'brief',
    sceneId: SLICE_SCENES[0].id,
    title: '',
    source: '',
    caption: '',
    hint: '',
    record: [],
    missesLeft: MAX_MISSES,
    timeLeft: FIRST_SECONDS,
    history: [],
    lastAttempt: null,
    reactUntil: 0,
    briefUntil: BRIEF_SECONDS,
  };
  applyScene(state, SLICE_SCENES[0].id);
  return state;
}

export function skipBrief(state: RunState): RunState {
  if (state.phase !== 'brief') return state;
  return { ...state, phase: 'play', briefUntil: 0 };
}

function findProp(scene: SceneDef, propId: string): PropDef | undefined {
  return scene.props.find((p) => p.id === propId);
}

export function tryExpose(state: RunState, propId: string): TryResult {
  if (state.phase === 'brief') state = skipBrief(state);
  if (state.phase !== 'play') {
    return { state, attempt: idleAttempt(state, propId) };
  }

  const scene = currentScene(state);
  const prop = findProp(scene, propId);
  if (!prop) {
    return { state, attempt: idleAttempt(state, propId) };
  }

  const fit = fitOf(state, prop);
  if (fit === 'clash' && prop.expose) {
    const thread = threadName(dayThread(state.record));
    const attempt: ExposeAttempt = {
      kind: 'trap',
      sceneId: scene.id,
      propId: prop.id,
      name: prop.name,
      claim: prop.expose.claim,
      line: '这告不成',
      sting: `今天是${thread}的故事。${prop.expose.claim}对不上。`,
      closed: '',
      from: state.caption,
      to: state.caption,
      record: '',
      fit,
    };
    return { state: applyAttempt(state, attempt), attempt };
  }

  if (prop.kind === 'expose' && prop.expose) {
    const attempt: ExposeAttempt = {
      kind: 'expose',
      sceneId: scene.id,
      propId: prop.id,
      name: prop.name,
      claim: prop.expose.claim,
      line: prop.expose.line,
      sting: prop.expose.sting,
      closed: prop.expose.closed,
      from: state.caption,
      to: prop.expose.caption,
      record: prop.expose.record,
      fit,
    };
    return { state: applyAttempt(state, attempt), attempt };
  }

  if (prop.kind === 'trap' && prop.trap) {
    const attempt: ExposeAttempt = {
      kind: 'trap',
      sceneId: scene.id,
      propId: prop.id,
      name: prop.name,
      claim: '',
      line: prop.trap.line,
      sting: prop.trap.why,
      closed: '',
      from: state.caption,
      to: state.caption,
      record: '',
      fit: 'trap',
    };
    return { state: applyAttempt(state, attempt), attempt };
  }

  return { state, attempt: idleAttempt(state, propId) };
}

function idleAttempt(state: RunState, propId: string): ExposeAttempt {
  return {
    kind: 'idle',
    sceneId: state.sceneId,
    propId,
    name: '',
    claim: '',
    line: '',
    sting: '',
    closed: '',
    from: state.caption,
    to: state.caption,
    record: '',
    fit: 'idle',
  };
}

function applyAttempt(state: RunState, attempt: ExposeAttempt): RunState {
  const next = clone(state);
  next.lastAttempt = attempt;
  next.phase = 'react';
  next.reactUntil = REACT[attempt.kind];

  if (attempt.kind === 'expose') {
    next.caption = attempt.to;
    next.record.push(attempt.record);
    next.history.push(attempt);
    return next;
  }

  if (attempt.kind === 'trap') {
    next.missesLeft -= 1;
    if (next.missesLeft <= 0) {
      next.phase = 'lost';
      next.reactUntil = 0;
    }
  }

  return next;
}

function nextSceneId(state: RunState): string | null {
  const scene = currentScene(state);
  const last = state.history[state.history.length - 1];
  if (last?.record && scene.nextByRecord?.[last.record]) {
    return scene.nextByRecord[last.record];
  }
  return scene.defaultNext ?? null;
}

export function settleReact(state: RunState): RunState {
  if (state.phase !== 'react') return state;
  const next = clone(state);
  next.reactUntil = 0;
  next.lastAttempt = state.lastAttempt;

  if (state.lastAttempt?.kind === 'expose') {
    const following = nextSceneId(state);
    if (!following) {
      next.phase = 'won';
      return next;
    }
    applyScene(next, following);
    return next;
  }

  next.phase = 'play';
  return next;
}

export function tick(state: RunState, dt: number): RunState {
  if (state.phase === 'won' || state.phase === 'lost') return state;

  if (state.phase === 'brief') {
    const left = state.briefUntil - dt;
    if (left <= 0) return tick({ ...state, phase: 'play', briefUntil: 0 }, -left);
    return { ...state, briefUntil: left };
  }

  if (state.phase === 'react') {
    const left = state.reactUntil - dt;
    if (left <= 0) return settleReact(state);
    return { ...state, reactUntil: left };
  }

  const timeLeft = state.timeLeft - dt;
  if (timeLeft <= 0) {
    return {
      ...state,
      timeLeft: 0,
      phase: 'lost',
      lastAttempt: {
        kind: 'idle',
        sceneId: state.sceneId,
        propId: '',
        name: '',
        claim: '',
        line: '这场合没揭完',
        sting: '',
        closed: '',
        from: state.caption,
        to: state.caption,
        record: '',
        fit: 'idle',
      },
    };
  }
  return { ...state, timeLeft };
}

export function winningNames(state: RunState): string[] {
  return state.history.filter((h) => h.kind === 'expose').map((h) => h.name);
}

export function hintFor(state: RunState): string {
  const scene = currentScene(state);
  if (state.phase === 'brief') return scene.brief;
  if (state.phase === 'react' && state.lastAttempt) {
    if (state.lastAttempt.kind === 'expose') {
      return state.lastAttempt.sting;
    }
    if (state.lastAttempt.kind === 'trap') {
      return `${state.lastAttempt.line}。${state.lastAttempt.sting}`;
    }
  }
  if (state.lastAttempt?.kind === 'trap') {
    return `${state.lastAttempt.line}，换一处揭`;
  }
  return scene.ask;
}

export function dayThread(record: string[]): DayThread {
  for (const id of record) {
    if (OVERTIME_MARK.has(id)) return 'overtime';
    if (BOOKS_MARK.has(id)) return 'books';
  }
  return '';
}

export function threadName(thread: DayThread): string {
  if (thread === 'overtime') return '加班';
  if (thread === 'books') return '走账';
  return '还没写';
}

export function wrapMatches(record: string[], wrapRecord: string): boolean {
  const thread = dayThread(record);
  if (!thread) return true;
  if (wrapRecord === 'oldsign') return thread === 'overtime';
  if (wrapRecord === 'dropped') return thread === 'books';
  return true;
}

export function fitOf(state: RunState, prop: PropDef): Fit {
  if (prop.kind === 'trap') return 'trap';
  if (state.sceneId === 'wrap' && prop.expose && !wrapMatches(state.record, prop.expose.record)) {
    return 'clash';
  }
  if (state.sceneId === 'store' && state.record.length === 0) return 'write';
  if (prop.kind === 'expose') return 'ok';
  return 'idle';
}

export function previewOf(state: RunState, prop: PropDef): string {
  const fit = fitOf(state, prop);
  if (fit === 'trap') return `告不成。${prop.trap?.why ?? '这说得通，揭不出假来。'}`;
  if (fit === 'clash') {
    return `和今天说反了。今天是${threadName(dayThread(state.record))}的故事。`;
  }
  if (fit === 'write' && prop.expose) {
    return `告成。今天写成「${prop.expose.caption}」，另一条今晚不开。`;
  }
  if (prop.expose) return `告成。现场变成「${prop.expose.caption}」。`;
  return '';
}

export function dayMap(state: RunState): string {
  if (state.history.length === 0) return '今日休息 → ？';
  return ['今日休息', ...state.history.map((h) => h.to)].join(' → ');
}

export function ruleFor(state: RunState): string {
  const thread = dayThread(state.record);
  if (state.sceneId === 'wrap') {
    return `收工要对上：今天是${threadName(thread)}的故事。说反了告不成。`;
  }
  if (thread) return `今天按${threadName(thread)}写。撒谎的才能揭，说得通的告不成。`;
  return '两处撒谎都能揭，写成不同的一天。说得通的，告不成。';
}

export function peekNextTitle(state: RunState): string {
  const id = nextSceneId(state);
  if (!id) return '看今天';
  return sceneById(id).title;
}

export function sceneIndex(state: RunState): string {
  const n = state.phase === 'react' && state.lastAttempt?.kind === 'expose'
    ? state.history.length
    : state.history.length + 1;
  return `${Math.min(4, Math.max(1, n))}/4`;
}

export function forbiddenKeys(state: RunState): string[] {
  return Object.keys(state).filter((k) => (
    k === 'lane' || k === 'leak' || k === 'baseHp' || k === 'path'
    || k === 'leftover' || k === 'remaining' || k === 'magnifier'
  ));
}
