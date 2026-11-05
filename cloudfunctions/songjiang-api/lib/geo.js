const IP2Region = require('ip2region').default;

/**
 * 按请求 IP 定省份。ip2region 离线库，IPv4 / IPv6 都能查，不出网。
 * 查不到（内网、海外、头里没 IP）返回空串，由调用方决定退回什么。
 */

const REGIONS = new Set([
  '北京', '天津', '河北', '山西', '内蒙古', '辽宁', '吉林', '黑龙江', '上海', '江苏', '浙江', '安徽',
  '福建', '江西', '山东', '河南', '湖北', '湖南', '广东', '广西', '海南', '重庆', '四川', '贵州',
  '云南', '西藏', '陕西', '甘肃', '青海', '宁夏', '新疆', '台湾', '香港', '澳门', '江湖',
]);

let query = null;

function db() {
  if (!query) query = new IP2Region();
  return query;
}

function clientIp(req) {
  const h = (req && req.headers) || {};
  const raw = h['x-forwarded-for'] || h['x-real-ip'] || h['x-client-ip'] || '';
  let ip = typeof raw === 'string' ? raw.split(',')[0].trim() : '';
  if (!ip) {
    const ctx = req && req.raw && req.raw.requestContext;
    ip = (ctx && ctx.sourceIp) || '';
  }
  return ip.replace(/^::ffff:/, '');
}

function province(name) {
  return String(name || '')
    .replace(/(维吾尔|壮族|回族)?自治区$/, '')
    .replace(/特别行政区$/, '')
    .replace(/[省市]$/, '')
    .trim();
}

function regionOfIp(ip) {
  if (!ip) return '';
  let hit;
  try {
    hit = db().search(ip);
  } catch (_) {
    return '';
  }
  if (!hit) return '';
  const country = province(hit.country);
  if (REGIONS.has(country)) return country;
  if (country !== '中国') return '';
  const p = province(hit.province);
  return REGIONS.has(p) ? p : '';
}

function regionOf(req) {
  return regionOfIp(clientIp(req));
}

module.exports = {
  REGIONS,
  clientIp,
  regionOf,
  regionOfIp,
};
