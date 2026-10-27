/**
 * 营救宋江后端（CloudBase 事件云函数 + HTTP 访问），照 huahua-api。
 * gameKey 与经分一致：songjiang。
 *
 *   POST /login
 *   POST /save/pull
 *   POST /save/push
 *   POST /config        服务器时间、公告、广告位和各入口次数
 *   POST /rank/submit   上报座次分
 *   POST /rank/list     省榜 / 全国榜
 *   GET|POST /health
 *
 * 环境变量：GAME_KEY=songjiang，SONGJIANG_JWT_SECRET 必填。
 * 微信真机再配 SONGJIANG_WX_APPID / SONGJIANG_WX_SECRET。
 * 集合：songjiang_playerData（userId 唯一索引）、songjiang_rank（userId 唯一）、songjiang_config（_id=live）
 */

const { handleLogin } = require('./lib/auth');
const { handlePull, handlePush } = require('./lib/save');
const { handleConfig, handleRankSubmit, handleRankList } = require('./lib/live');
const { respond, parseEvent, preflight } = require('./lib/http');

const ROUTES = {
  'GET /health': async () => ({ ok: true, ts: Date.now() }),
  'POST /health': async () => ({ ok: true, ts: Date.now() }),
  'POST /login': handleLogin,
  'POST /save/pull': handlePull,
  'POST /save/push': handlePush,
  'POST /config': handleConfig,
  'GET /config': handleConfig,
  'POST /rank/submit': handleRankSubmit,
  'POST /rank/list': handleRankList,
};

exports.main = async (event, context) => {
  try {
    if (event && event.httpMethod === 'OPTIONS') return preflight();

    const req = parseEvent(event);
    const key = `${req.method} ${req.path}`;
    const handler = ROUTES[key];
    if (!handler) {
      return respond(404, { ok: false, code: 'NOT_FOUND', error: `no route: ${key}` });
    }

    const result = await handler(req, context);
    if (result && typeof result === 'object' && 'statusCode' in result) return result;
    return respond(200, { ok: true, data: result });
  } catch (error) {
    const code = error && error.code ? error.code : 'INTERNAL';
    const status = error && error.status ? error.status : 500;
    const message = (error && error.message) || String(error);
    console.error('[songjiang-api] error:', code, message, error && error.stack);
    const out = { ok: false, code, error: message };
    if (error && error.data !== undefined) out.data = error.data;
    return respond(status, out);
  }
};
