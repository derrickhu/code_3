const { httpError } = require('./http');
const { requireUser } = require('./auth');
const { getApp } = require('./db');
const { getCollectionName } = require('./config');
const { REGIONS, regionOf } = require('./geo');

/**
 * 运营配置：集合 songjiang_config 里 _id = "live" 的一条文档，没有就用默认值。
 *   { noticeTitle, notice, noticeVer, adUnitId, adCaps: [{ scene, cap }] }
 * 排行榜：集合 songjiang_rank，一人一条，按 userId 唯一。
 * 地区不让玩家选，按请求 IP 定省份；查不到时沿用上次的，再没有就是「江湖」。
 */

const DEFAULT_CAPS = [
  { scene: 'stamina', cap: 8 },
  { scene: 'double_coin', cap: 10 },
  { scene: 'keep_star', cap: 5 },
  { scene: 'free_item', cap: 6 },
];

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

async function handleConfig(req) {
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
    region: regionOf(req),
  };
}

function cleanName(n) {
  const v = Array.from(String(n || '').replace(/[\u0000-\u001f<>]/g, '').trim()).slice(0, 12).join('');
  return v || '好汉';
}

function cleanAvatar(u) {
  const v = String(u || '').trim();
  if (!/^https:\/\/\S{8,2000}$/.test(v)) return '';
  return v;
}

async function findMine(col, userId) {
  const res = await col.where({ userId }).limit(1).get();
  return res && Array.isArray(res.data) ? res.data[0] : null;
}

function regionFor(req, row) {
  const seen = regionOf(req);
  if (seen) return seen;
  if (row && REGIONS.has(row.region)) return row.region;
  return '江湖';
}

async function handleRankSubmit(req) {
  const { userId } = requireUser(req);
  const body = req.body || {};
  const score = Math.floor(Number(body.score));
  if (!Number.isFinite(score) || score < 0 || score > MAX_SCORE) {
    throw httpError(400, 'BAD_SCORE', 'score 非法');
  }
  const col = db().collection(getCollectionName('rank'));
  const row = await findMine(col, userId);
  const region = regionFor(req, row);
  const doc = {
    userId,
    name: cleanName(body.name),
    avatar: cleanAvatar(body.avatar) || (row && cleanAvatar(row.avatar)) || '',
    region,
    score,
    tier: Math.max(0, Math.floor(Number(body.tier) || 0)),
    stars: Math.max(0, Math.floor(Number(body.stars) || 0)),
    wins: Math.max(0, Math.floor(Number(body.wins) || 0)),
    updatedAt: Date.now(),
  };
  if (row && row._id) await col.doc(row._id).update(doc);
  else await col.add(doc);
  return { ok: true, region };
}

/** body.region 传「全国」拿总榜，其它都按自己所在的省拿省榜。 */
async function handleRankList(req) {
  const { userId } = requireUser(req);
  const body = req.body || {};
  const col = db().collection(getCollectionName('rank'));
  const _ = db().command;

  let me = await findMine(col, userId);
  const myRegion = regionFor(req, me);
  if (me && me._id && me.region !== myRegion) {
    await col.doc(me._id).update({ region: myRegion });
    me = { ...me, region: myRegion };
  }

  const all = String(body.region || '') === '全国';
  const filter = all ? {} : { region: myRegion };

  const topRes = await col.where(filter).orderBy('score', 'desc').orderBy('updatedAt', 'asc').limit(LIST_LIMIT).get();
  const top = (topRes && topRes.data ? topRes.data : []).map((r) => ({
    name: r.name,
    avatar: cleanAvatar(r.avatar),
    region: r.region,
    score: r.score,
    tier: r.tier,
    stars: r.stars,
    me: r.userId === userId,
  }));

  let rank = 0;
  if (me) {
    const at = top.findIndex((r) => r.me);
    if (at >= 0) {
      // 人就在这页里时，名次就是列表里的位置，底下「我」和上面对得上。
      rank = at + 1;
    } else {
      // 落到 50 名开外：分高的在前，同分按先上报的在前，和 orderBy 一致。
      const higher = await col.where({ ...filter, score: _.gt(me.score) }).count();
      const earlier = await col.where({ ...filter, score: me.score, updatedAt: _.lt(Number(me.updatedAt) || 0) }).count();
      rank = ((higher && higher.total) || 0) + ((earlier && earlier.total) || 0) + 1;
    }
  }
  const count = await col.where(filter).count();
  return {
    region: all ? '全国' : myRegion,
    myRegion,
    top,
    rank,
    total: (count && count.total) || 0,
  };
}

const https = require('https');
const { URL } = require('url');

// 真机的 request 域名里没有微信头像域名，客户端改走这里中转。
const AVATAR_HOSTS = new Set(['thirdwx.qlogo.cn', 'wx.qlogo.cn', 'mmbiz.qpic.cn', 'thirdqq.qlogo.cn']);
const AVATAR_MAX = 262144;

function avatarUrlOf(raw) {
  let u;
  try { u = new URL(String(raw || '').trim()); } catch (_) { return null; }
  if (u.protocol !== 'https:' || !AVATAR_HOSTS.has(u.hostname)) return null;
  return u;
}

function isImage(buf) {
  if (!buf || buf.length < 8) return false;
  if (buf[0] === 0xff && buf[1] === 0xd8) return true;
  if (buf[0] === 0x89 && buf[1] === 0x50 && buf[2] === 0x4e && buf[3] === 0x47) return true;
  if (buf[0] === 0x47 && buf[1] === 0x49 && buf[2] === 0x46) return true;
  return false;
}

function readAvatar(url, redirects) {
  return new Promise((resolve, reject) => {
    const u = avatarUrlOf(url);
    if (!u) {
      reject(httpError(400, 'BAD_AVATAR', '头像地址不对'));
      return;
    }
    const req = https.get(u, { timeout: 8000, headers: { 'User-Agent': 'songjiang' } }, (res) => {
      const code = res.statusCode || 0;
      if (code >= 300 && code < 400 && res.headers.location && redirects < 2) {
        res.resume();
        let next = '';
        try { next = new URL(res.headers.location, u).toString(); } catch (_) { next = ''; }
        resolve(readAvatar(next, redirects + 1));
        return;
      }
      if (code !== 200) {
        res.resume();
        reject(httpError(502, 'AVATAR_FETCH', '头像下不来'));
        return;
      }
      const chunks = [];
      let size = 0;
      let failed = false;
      res.on('data', (c) => {
        if (failed) return;
        size += c.length;
        if (size > AVATAR_MAX) {
          failed = true;
          req.destroy();
          reject(httpError(413, 'AVATAR_BIG', '头像太大'));
          return;
        }
        chunks.push(c);
      });
      res.on('end', () => {
        if (failed) return;
        const buf = Buffer.concat(chunks);
        if (!isImage(buf)) {
          reject(httpError(502, 'AVATAR_FETCH', '头像下不来'));
          return;
        }
        resolve(buf);
      });
    });
    req.on('error', () => reject(httpError(502, 'AVATAR_FETCH', '头像下不来')));
    req.on('timeout', () => {
      req.destroy();
      reject(httpError(502, 'AVATAR_FETCH', '头像下不来'));
    });
  });
}

async function handleAvatar(req) {
  requireUser(req);
  const buf = await readAvatar(req.body && req.body.url, 0);
  return { b64: buf.toString('base64') };
}

module.exports = {
  handleConfig,
  handleRankSubmit,
  handleRankList,
  handleAvatar,
  shanghaiDay,
};
