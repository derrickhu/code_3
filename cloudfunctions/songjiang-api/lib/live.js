const { httpError } = require('./http');
const { requireUser } = require('./auth');
const { getApp } = require('./db');
const { getCollectionName } = require('./config');

/**
 * 运营配置：集合 songjiang_config 里 _id = "live" 的一条文档，没有就用默认值。
 *   { noticeTitle, notice, noticeVer, adUnitId, adCaps: [{ scene, cap }] }
 * 排行榜：集合 songjiang_rank，一人一条，按 userId 唯一。
 */

const DEFAULT_CAPS = [
  { scene: 'stamina', cap: 8 },
  { scene: 'double_coin', cap: 10 },
  { scene: 'keep_star', cap: 5 },
  { scene: 'free_item', cap: 6 },
];

const REGIONS = new Set([
  '北京', '天津', '河北', '山西', '内蒙古', '辽宁', '吉林', '黑龙江', '上海', '江苏', '浙江', '安徽',
  '福建', '江西', '山东', '河南', '湖北', '湖南', '广东', '广西', '海南', '重庆', '四川', '贵州',
  '云南', '西藏', '陕西', '甘肃', '青海', '宁夏', '新疆', '台湾', '香港', '澳门', '江湖',
]);

const MAX_SCORE = 100000;
const LIST_LIMIT = 50;

function db() {
  return getApp().database();
}

function shanghaiDay(ms) {
  const d = new Date(ms + 8 * 3600 * 1000);
  const y = d.getUTCFullYear();
  const m = String(d.getUTCMonth() + 1).padStart(2, '0');
  const day = String(d.getUTCDate()).padStart(2, '0');
  return `${y}${m}${day}`;
}

async function readLive() {
  try {
    const res = await db().collection(getCollectionName('config')).doc('live').get();
    const doc = res && Array.isArray(res.data) ? res.data[0] : res && res.data;
    return doc || {};
  } catch (_) {
    return {};
  }
}

async function handleConfig() {
  const now = Date.now();
  const live = await readLive();
  const caps = Array.isArray(live.adCaps) && live.adCaps.length > 0 ? live.adCaps : DEFAULT_CAPS;
  return {
    serverNow: now,
    day: shanghaiDay(now),
    noticeTitle: String(live.noticeTitle || ''),
    notice: String(live.notice || ''),
    noticeVer: Number(live.noticeVer) || 0,
    adUnitId: String(live.adUnitId || ''),
    adCaps: caps
      .filter((c) => c && typeof c.scene === 'string')
      .map((c) => ({ scene: c.scene, cap: Math.max(0, Math.floor(Number(c.cap) || 0)) })),
  };
}

function cleanRegion(r) {
  const v = String(r || '').trim();
  return REGIONS.has(v) ? v : '江湖';
}

function cleanName(n) {
  const v = String(n || '').replace(/[\u0000-\u001f<>]/g, '').trim().slice(0, 12);
  return v || '好汉';
}

async function handleRankSubmit(req) {
  const { userId } = requireUser(req);
  const body = req.body || {};
  const score = Math.floor(Number(body.score));
  if (!Number.isFinite(score) || score < 0 || score > MAX_SCORE) {
    throw httpError(400, 'BAD_SCORE', 'score 非法');
  }
  const doc = {
    userId,
    name: cleanName(body.name),
    region: cleanRegion(body.region),
    score,
    tier: Math.max(0, Math.floor(Number(body.tier) || 0)),
    stars: Math.max(0, Math.floor(Number(body.stars) || 0)),
    wins: Math.max(0, Math.floor(Number(body.wins) || 0)),
    updatedAt: Date.now(),
  };
  const col = db().collection(getCollectionName('rank'));
  const existing = await col.where({ userId }).limit(1).get();
  const row = existing && Array.isArray(existing.data) ? existing.data[0] : null;
  if (row && row._id) await col.doc(row._id).update(doc);
  else await col.add(doc);
  return { ok: true };
}

async function handleRankList(req) {
  const { userId } = requireUser(req);
  const body = req.body || {};
  const scope = String(body.region || '') === '全国' ? '' : cleanRegion(body.region);
  const col = db().collection(getCollectionName('rank'));
  const _ = db().command;
  const filter = scope ? { region: scope } : {};

  const topRes = await col.where(filter).orderBy('score', 'desc').orderBy('updatedAt', 'asc').limit(LIST_LIMIT).get();
  const top = (topRes && topRes.data ? topRes.data : []).map((r) => ({
    name: r.name,
    region: r.region,
    score: r.score,
    tier: r.tier,
    stars: r.stars,
    me: r.userId === userId,
  }));

  const mine = await col.where({ userId }).limit(1).get();
  const me = mine && Array.isArray(mine.data) ? mine.data[0] : null;
  let rank = 0;
  let total = 0;
  if (me) {
    const above = await col.where({ ...filter, score: _.gt(me.score) }).count();
    rank = ((above && above.total) || 0) + 1;
  }
  const all = await col.where(filter).count();
  total = (all && all.total) || 0;
  return {
    region: scope || '全国',
    top,
    rank,
    total,
  };
}

module.exports = {
  handleConfig,
  handleRankSubmit,
  handleRankList,
  shanghaiDay,
};
