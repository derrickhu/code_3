import { MIN_TAP } from '@/ui/theme';

export type PropKind = 'expose' | 'trap';

export interface Rect {
  x: number;
  y: number;
  w: number;
  h: number;
}

export interface ExposeDef {
  caption: string;
  claim: string;
  line: string;
  sting: string;
  closed: string;
  record: string;
}

export interface TrapDef {
  line: string;
  why: string;
}

export interface PropDef {
  id: string;
  name: string;
  kind: PropKind;
  rect: Rect;
  expose?: ExposeDef;
  trap?: TrapDef;
}

export interface SceneDef {
  id: string;
  title: string;
  source: string;
  caption: string;
  brief: string;
  ask: string;
  hint: string;
  guide?: boolean;
  seconds?: number;
  props: PropDef[];
  nextByRecord?: Record<string, string>;
  defaultNext?: string;
  captionByRecord?: Record<string, string>;
}

export const ROUND_SECONDS = 32;
export const FIRST_SECONDS = 40;
export const MAX_MISSES = 3;
export const MIN_PROP = MIN_TAP;

function box(x: number, y: number, w: number, h: number): Rect {
  if (w < MIN_PROP || h < MIN_PROP) {
    throw new Error(`prop too small ${w}x${h}`);
  }
  return { x, y, w, h };
}

export const SLICE_SCENES: readonly SceneDef[] = [
  {
    id: 'store',
    title: '便利店夜班',
    source: '门口',
    caption: '今日休息',
    brief: '牌子写休息。灯亮着。人还在。两处都在撒谎，揭哪处今天就按哪处写。',
    ask: '今天写成加班，还是写成走账？',
    hint: '选一门状，现场就按它写死',
    guide: true,
    seconds: FIRST_SECONDS,
    props: [
      {
        id: 'sign',
        name: '招牌',
        kind: 'expose',
        rect: box(160, 64, 340, 108),
        expose: {
          caption: '今日加班',
          claim: '休息是假牌',
          line: '你揭开了招牌',
          sting: '休息背面写着加班。老板在回来的路上。',
          closed: '袖口没动。走账这条今晚没写。',
          record: 'sign',
        },
      },
      {
        id: 'cash',
        name: '收银袖口',
        kind: 'expose',
        rect: box(380, 280, 250, 300),
        expose: {
          caption: '他在走账',
          claim: '他趁关门走账',
          line: '你揭开了袖口',
          sting: '店是关给外人看的。钱顺着袖口走。',
          closed: '招牌没动。加班这条今晚没写。',
          record: 'cash',
        },
      },
      {
        id: 'bread',
        name: '过期面包',
        kind: 'trap',
        rect: box(80, 560, 220, 140),
        trap: {
          line: '这告不成',
          why: '过期和关门说得通，告不出假来。',
        },
      },
    ],
    nextByRecord: { sign: 'overtime', cash: 'books' },
  },
  {
    id: 'overtime',
    title: '谁让店加班',
    source: '后厨',
    caption: '谁让店加班',
    brief: '你揭了招牌。休息没了。谁拍的板？',
    ask: '是自愿留下，还是老板写上的？',
    hint: '揭开是谁把休息改成加班的',
    props: [
      {
        id: 'roster',
        name: '排班表',
        kind: 'expose',
        rect: box(40, 80, 240, 360),
        expose: {
          caption: '自愿是假的',
          claim: '自愿栏是写上去的',
          line: '你揭开了排班表',
          sting: '自愿两个字被写满了。没人签字。',
          closed: '名牌没揭。老板定的这条没写。',
          record: 'roster',
        },
      },
      {
        id: 'badge',
        name: '老板名牌',
        kind: 'expose',
        rect: box(380, 220, 250, 280),
        expose: {
          caption: '老板定的',
          claim: '加班栏挂着他的名字',
          line: '你揭开了名牌',
          sting: '加班那一栏，写的是老板。',
          closed: '排班表没揭。自愿是假的这条没写。',
          record: 'badge',
        },
      },
      {
        id: 'hongbao',
        name: '加班费',
        kind: 'trap',
        rect: box(240, 640, 180, 130),
        trap: {
          line: '这告不成',
          why: '发红包解释不了是谁定的。',
        },
      },
    ],
    defaultNext: 'dinner',
  },
  {
    id: 'books',
    title: '账对不上',
    source: '柜台里',
    caption: '账对不上',
    brief: '你揭了袖口。钱在走。走去哪？',
    ask: '镜头被拧了，还是抽屉里还有一叠？',
    hint: '揭开钱是怎么没的',
    props: [
      {
        id: 'camera',
        name: '监控',
        kind: 'expose',
        rect: box(40, 40, 240, 200),
        expose: {
          caption: '监控对着墙',
          claim: '有人把镜头拧歪了',
          line: '你揭开了监控',
          sting: '镜头对着墙。这段没有人。',
          closed: '抽屉没揭。夹层那叠今晚没写。',
          record: 'camera',
        },
      },
      {
        id: 'drawer',
        name: '抽屉夹层',
        kind: 'expose',
        rect: box(360, 400, 270, 240),
        expose: {
          caption: '走账还在',
          claim: '夹层里还有一叠',
          line: '你揭开了抽屉',
          sting: '夹层里还有一叠。账没走完。',
          closed: '监控没揭。镜头拧歪这条没写。',
          record: 'drawer',
        },
      },
      {
        id: 'ticket',
        name: '过期小票',
        kind: 'trap',
        rect: box(60, 620, 200, 140),
        trap: {
          line: '这告不成',
          why: '小票过期解释不了账。',
        },
      },
    ],
    defaultNext: 'dinner',
  },
  {
    id: 'dinner',
    title: '这顿我请',
    source: '饭局',
    caption: '这顿我请',
    brief: '他请你吃饭。请是假的。假在账单还是假在收款码？',
    ask: '请是假的，假在账单，还是假在收款码？',
    hint: '揭开到底谁请客',
    captionByRecord: {
      sign: '加班那头说这顿他请',
      roster: '加班那头说这顿他请',
      badge: '加班那头说这顿他请',
      cash: '走账那头说这顿他请',
      camera: '走账那头说这顿他请',
      drawer: '走账那头说这顿他请',
    },
    props: [
      {
        id: 'bill',
        name: '账单',
        kind: 'expose',
        rect: box(60, 400, 240, 220),
        expose: {
          caption: '他没请',
          claim: '单子塞到你手边了',
          line: '你揭开了账单',
          sting: '单子在你手边。请只是一句。',
          closed: '手机没揭。AA 那条今晚没写。',
          record: 'bill',
        },
      },
      {
        id: 'phone',
        name: '手机',
        kind: 'expose',
        rect: box(400, 320, 230, 250),
        expose: {
          caption: '口头请，实际 AA',
          claim: '群里已经在收钱',
          line: '你揭开了手机',
          sting: '群里已经在收钱。请是口头的。',
          closed: '账单没揭。他没请这条没写。',
          record: 'phone',
        },
      },
      {
        id: 'glass',
        name: '空酒杯',
        kind: 'trap',
        rect: box(260, 640, 180, 140),
        trap: {
          line: '这告不成',
          why: '喝光了不构成撒谎。',
        },
      },
    ],
    defaultNext: 'wrap',
  },
  {
    id: 'wrap',
    title: '今天到底怎么了',
    source: '店门口',
    caption: '今天到底怎么了',
    brief: '收工了。案底已经写死。对得上的才能收，说反了告不成。',
    ask: '收工要对上案底。哪一处是今天的故事？',
    hint: '揭开今天被写成了哪一种',
    captionByRecord: {
      sign: '还在问加班的事',
      roster: '还在问加班的事',
      badge: '还在问加班的事',
      cash: '还在问账的事',
      camera: '还在问账的事',
      drawer: '还在问账的事',
    },
    props: [
      {
        id: 'oldsign',
        name: '改过的牌',
        kind: 'expose',
        rect: box(50, 80, 280, 240),
        expose: {
          caption: '今天是加班的故事',
          claim: '休息被写成了加班',
          line: '你揭开了改过的牌',
          sting: '休息被掀掉了。今天按加班写死。',
          closed: '掉出来的钱没揭。走账那天没写。',
          record: 'oldsign',
        },
      },
      {
        id: 'dropped',
        name: '掉出来的钱',
        kind: 'expose',
        rect: box(380, 400, 250, 240),
        expose: {
          caption: '今天是走账的故事',
          claim: '袖口那一叠对上了',
          line: '你揭开了掉出来的钱',
          sting: '袖口那一叠对上了。今天按走账写死。',
          closed: '改过的牌没揭。加班那天没写。',
          record: 'dropped',
        },
      },
      {
        id: 'cat',
        name: '路过的猫',
        kind: 'trap',
        rect: box(220, 640, 200, 140),
        trap: {
          line: '这告不成',
          why: '猫不负责今天的事。',
        },
      },
    ],
  },
];

export function sceneById(id: string): SceneDef {
  const scene = SLICE_SCENES.find((s) => s.id === id);
  if (!scene) throw new Error(`unknown scene ${id}`);
  return scene;
}

export function exposesOf(scene: SceneDef): PropDef[] {
  return scene.props.filter((p) => p.kind === 'expose');
}

export function trapsOf(scene: SceneDef): PropDef[] {
  return scene.props.filter((p) => p.kind === 'trap');
}
