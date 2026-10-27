using System;
using System.Collections;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>
    /// 运营配置：服务器时间、远程公告、广告位和各入口每日次数，以及排行榜读写。
    /// 拉不到就用上次缓存，再不行用写死的默认值，断网照样能玩。
    /// </summary>
    public sealed class LiveConfig : MonoBehaviour
    {
        [Serializable]
        public sealed class AdCapRow
        {
            public string scene;
            public int cap;
        }

        [Serializable]
        public sealed class LiveData
        {
            public long serverNow;
            public string day;
            public string noticeTitle;
            public string notice;
            public int noticeVer;
            public string adUnitId;
            public AdCapRow[] adCaps;
        }

        [Serializable]
        sealed class LiveReply
        {
            public bool ok;
            public LiveData data;
        }

        [Serializable]
        public sealed class RankRow
        {
            public string name;
            public string region;
            public int score;
            public int tier;
            public int stars;
            public bool me;
        }

        [Serializable]
        public sealed class RankList
        {
            public string region;
            public RankRow[] top;
            public int rank;
            public int total;
        }

        [Serializable]
        sealed class RankReply
        {
            public bool ok;
            public RankList data;
        }

        [Serializable]
        sealed class RankSubmit
        {
            public string name;
            public string region;
            public int score;
            public int tier;
            public int stars;
            public int wins;
        }

        [Serializable]
        sealed class RankQuery
        {
            public string region;
        }

        static readonly Dictionary<string, int> DefaultCaps = new Dictionary<string, int>
        {
            { "stamina", 8 }, { "double_coin", 10 }, { "keep_star", 5 }, { "free_item", 6 },
        };

        static LiveConfig _it;
        static long _offsetMs;
        static LiveData _data;
        static float _fetchedAt = -999f;
        static int _lastScore = -1;
        static string _lastRegion;

        public static bool Synced { get; private set; }

        /// <summary>校正过的现在。没拉到服务器时间时就是本机时间。</summary>
        public static DateTimeOffset Now => DateTimeOffset.UtcNow.AddMilliseconds(_offsetMs);

        public static string AdUnitId => Data.adUnitId ?? "";
        public static string NoticeTitle => string.IsNullOrEmpty(Data.noticeTitle) ? "公告" : Data.noticeTitle;
        public static string Notice => Data.notice ?? "";

        static LiveData Data
        {
            get
            {
                if (_data != null) return _data;
                _data = LoadCache() ?? new LiveData();
                return _data;
            }
        }

        /// <summary>这个入口每天最多看几次。0 表示不限（抽奖、铲子、作法等另有自己的次数）。</summary>
        public static int AdCap(string scene)
        {
            var caps = Data.adCaps;
            if (caps != null && caps.Length > 0)
            {
                foreach (var c in caps) if (c != null && c.scene == scene) return Mathf.Max(0, c.cap);
                return 0;
            }
            return DefaultCaps.TryGetValue(scene, out var n) ? n : 0;
        }

        /// <summary>今天还能看几次；-1 表示不限。</summary>
        public static int AdLeft(string scene)
        {
            int cap = AdCap(scene);
            if (cap <= 0) return -1;
            return Mathf.Max(0, cap - Save.AdUsed(scene));
        }

        static LiveConfig It
        {
            get
            {
                if (_it != null) return _it;
                var go = new GameObject("LiveConfig");
                DontDestroyOnLoad(go);
                _it = go.AddComponent<LiveConfig>();
                return _it;
            }
        }

        /// <summary>开机和回到主界面时调用。十分钟内拉过就不再拉。</summary>
        public static void Refresh(Action done = null)
        {
            if (Save.Volatile || Application.isBatchMode) { done?.Invoke(); return; }
            if (Time.realtimeSinceStartup - _fetchedAt < 600f) { done?.Invoke(); return; }
            _fetchedAt = Time.realtimeSinceStartup;
            It.StartCoroutine(It.Fetch(done));
        }

        IEnumerator Fetch(Action done)
        {
            long sent = Backend.NowMs();
            long status = 0;
            string text = null;
            yield return Backend.CallText(CloudConfig.LivePath, "{}", (s, t, e) => { status = s; text = t; });
            long back = Backend.NowMs();
            if (status == 200 && !string.IsNullOrEmpty(text))
            {
                LiveReply reply = null;
                try { reply = JsonUtility.FromJson<LiveReply>(text); }
                catch (Exception) { reply = null; }
                if (reply != null && reply.ok && reply.data != null)
                {
                    _data = reply.data;
                    if (reply.data.serverNow > 0)
                    {
                        _offsetMs = reply.data.serverNow - (sent + back) / 2;
                        Synced = true;
                    }
                    PlayerPrefs.SetString(CloudConfig.LiveCacheKey, JsonUtility.ToJson(reply.data));
                    PlayerPrefs.Save();
                }
            }
            else _fetchedAt = -999f;
            done?.Invoke();
        }

        static LiveData LoadCache()
        {
            string raw = PlayerPrefs.GetString(CloudConfig.LiveCacheKey, "");
            if (string.IsNullOrEmpty(raw)) return null;
            try { return JsonUtility.FromJson<LiveData>(raw); }
            catch (Exception) { return null; }
        }

        // ─────────────────────────── 排行榜 ───────────────────────────

        public static readonly string[] Regions =
        {
            "北京", "天津", "河北", "山西", "内蒙古", "辽宁", "吉林", "黑龙江", "上海", "江苏", "浙江", "安徽",
            "福建", "江西", "山东", "河南", "湖北", "湖南", "广东", "广西", "海南", "重庆", "四川", "贵州",
            "云南", "西藏", "陕西", "甘肃", "青海", "宁夏", "新疆", "台湾", "香港", "澳门", "江湖",
        };

        /// <summary>座次分：大阶小阶加星，再加一点胜场（封顶三十），和以前的假榜同一口径。</summary>
        public static int MyScore => RankRules.Score(Save.D.tier, Save.D.stars) + Mathf.Min(Save.D.wins, 30);

        public static string MyRegion => string.IsNullOrEmpty(Save.D.region) ? "江湖" : Save.D.region;

        /// <summary>榜上的名字：好汉加四位数，从账号算出来，换机不变。</summary>
        public static string MyName
        {
            get
            {
                string id = Backend.UserId;
                if (string.IsNullOrEmpty(id)) id = SystemInfo.deviceUniqueIdentifier ?? "local";
                uint h = 2166136261;
                foreach (var ch in id) h = (h ^ ch) * 16777619;
                return "好汉" + (1000 + h % 9000);
            }
        }

        /// <summary>分数或地区变了才上报。断网就算了，下回再报。</summary>
        public static void SubmitRank(bool force = false)
        {
            if (Save.Volatile || Application.isBatchMode) return;
            int score = MyScore;
            string region = MyRegion;
            if (!force && score == _lastScore && region == _lastRegion) return;
            It.StartCoroutine(It.Submit(score, region));
        }

        IEnumerator Submit(int score, string region)
        {
            var body = new RankSubmit { name = MyName, region = region, score = score, tier = Save.D.tier, stars = Save.D.stars, wins = Save.D.wins };
            long status = 0;
            yield return Backend.CallText(CloudConfig.RankSubmitPath, JsonUtility.ToJson(body), (s, t, e) => status = s);
            if (status == 200)
            {
                _lastScore = score;
                _lastRegion = region;
            }
        }

        /// <summary>region 传「全国」拿总榜，传省名拿省榜。失败时 done(null)。</summary>
        public static void LoadRank(string region, Action<RankList> done)
        {
            if (Save.Volatile) { done(ShotSample(region)); return; }
            if (Application.isBatchMode) { done(null); return; }
            It.StartCoroutine(It.List(region, done));
        }

        /// <summary>验收截图不联网，用一份固定的榜单看排版。</summary>
        static RankList ShotSample(string region)
        {
            var rows = new List<RankRow>();
            int score = 3120;
            string[] provinces = { "山东", "河北", "江苏", "浙江", "广东", "四川", "湖北", "河南" };
            for (int i = 0; i < 12; i++)
            {
                bool me = i == 4;
                rows.Add(new RankRow
                {
                    name = me ? MyName : "好汉" + (2301 + i * 377 % 7000),
                    region = region == "全国" ? provinces[i % provinces.Length] : region,
                    score = me ? MyScore : score,
                    tier = me ? Save.D.tier : RankRules.Clamp(26 - i),
                    stars = me ? Save.D.stars : (5 - i % 4),
                    me = me,
                });
                score -= 40 + i * 13;
            }
            return new RankList { region = region, top = rows.ToArray(), rank = 5, total = 1286 };
        }

        IEnumerator List(string region, Action<RankList> done)
        {
            if (_lastScore != MyScore || _lastRegion != MyRegion) yield return Submit(MyScore, MyRegion);
            long status = 0;
            string text = null;
            yield return Backend.CallText(CloudConfig.RankListPath, JsonUtility.ToJson(new RankQuery { region = region }), (s, t, e) => { status = s; text = t; });
            RankList list = null;
            if (status == 200 && !string.IsNullOrEmpty(text))
            {
                try
                {
                    var reply = JsonUtility.FromJson<RankReply>(text);
                    if (reply != null && reply.ok) list = reply.data;
                }
                catch (Exception) { list = null; }
            }
            done(list);
        }
    }
}
