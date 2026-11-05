#if (UNITY_MINIGAME || WEIXINMINIGAME || UNITY_WEIXINMINIGAME || MINIGAME_SUBPLATFORM_WEIXIN) && !UNITY_EDITOR
#define RESCUE_WX_AD
#endif
using System;
using UnityEngine;
#if RESCUE_WX_AD
using WeChatWASM;
#endif

namespace Rescue
{
    /// <summary>
    /// 微信激励视频。八个入口各用一个广告位，流量主后台能分开看。
    /// 同时只留一个实例：换入口先拆掉上一个。multiton 必须开，否则微信只认第一次的广告位，看完不发奖。
    /// 编辑器和截图不走这里，AdStub 用演示片。
    /// </summary>
    public static class WxAd
    {
        const string Stamina = "adunit-620e330f8924afba";
        const string DoubleCoin = "adunit-493ca5070431c9e3";
        const string KeepStar = "adunit-e309e187b94b6ec0";
        const string DoubleSalary = "adunit-b09d9b54e9f142b1";
        const string FreeItem = "adunit-75054ad70cf8599e";
        const string Lottery = "adunit-bb7600d7dbafe069";
        const string Shovel = "adunit-03a4dd4220040f82";
        const string RescueSpell = "adunit-6a75fb891527e4fe";

        /// <summary>经分场景名对应的广告位。没有这个入口时返回空串。</summary>
        public static string UnitOf(string scene)
        {
            switch (scene)
            {
                case "stamina": return Stamina;
                case "double_coin": return DoubleCoin;
                case "keep_star": return KeepStar;
                case "double_salary": return DoubleSalary;
                case "free_item": return FreeItem;
                case "lottery": return Lottery;
                case "shovel": return Shovel;
                case "rescue": return RescueSpell;
                default: return "";
            }
        }

#if RESCUE_WX_AD
        static WXRewardedVideoAd _ad;
        static string _unit;
        static Action _shown;
        static Action<bool> _closed;
        static Action _failed;
        static bool _busy;
        static bool _didShow;
        static int _gen;

        public static bool Available => true;
        public static bool Busy => _busy;

        /// <summary>shown 在广告真正播出来时调一次。closed(true) 表示看完；旧基础库不带回执时也算看完。failed 表示没播出来。</summary>
        public static void Show(string scene, Action shown, Action<bool> closed, Action failed)
        {
            string unit = UnitOf(scene);
            if (string.IsNullOrEmpty(unit))
            {
                failed?.Invoke();
                return;
            }
            if (_busy)
            {
                failed?.Invoke();
                return;
            }
            try
            {
                Ensure(unit);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WxAd] create failed " + e.Message);
                failed?.Invoke();
                return;
            }
            _busy = true;
            _didShow = false;
            _shown = shown;
            _closed = closed;
            _failed = failed;
            int gen = ++_gen;
            _ad.Show(_ => Shown(), _ => LoadThenShow(gen));
        }

        static void Ensure(string unit)
        {
            if (_ad != null && _unit == unit) return;
            if (_ad != null)
            {
                try { _ad.Destroy(); }
                catch (Exception e) { Debug.LogWarning("[WxAd] destroy " + e.Message); }
                _ad = null;
            }
            _unit = unit;
            _ad = WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam { adUnitId = unit, multiton = true });
            _ad.OnClose(OnClose);
            _ad.OnError(e => Debug.LogWarning("[WxAd] " + (e != null ? e.errCode + " " + e.errMsg : "?")));
            _ad.Load(_ => { }, e => Debug.LogWarning("[WxAd] preload " + (e != null ? e.errCode + " " + e.errMsg : "?")));
        }

        static void Shown()
        {
            if (_didShow) return;
            _didShow = true;
            _shown?.Invoke();
        }

        /// <summary>还没加载好时 show 会失败，补拉一次再播。</summary>
        static void LoadThenShow(int gen)
        {
            if (!_busy || gen != _gen || _ad == null) return;
            _ad.Load(
                _ =>
                {
                    if (!_busy || gen != _gen || _ad == null) return;
                    _ad.Show(__ => Shown(), __ => Fail(gen));
                },
                _ => Fail(gen));
        }

        static void OnClose(WXRewardedVideoAdOnCloseResponse r)
        {
            if (!_busy) return;
            int gen = _gen;
            bool ended = r == null || r.isEnded;
            var ad = _ad;
            var closed = _closed;
            Settle(gen);
            closed?.Invoke(ended);
            if (_ad == ad && ad != null)
            {
                try { ad.Load(_ => { }, _ => { }); }
                catch (Exception) { }
            }
        }

        static void Fail(int gen)
        {
            if (!_busy || gen != _gen) return;
            var failed = _failed;
            Settle(gen);
            failed?.Invoke();
        }

        static void Settle(int gen)
        {
            if (!_busy || gen != _gen) return;
            _busy = false;
            _gen++;
            _shown = null;
            _closed = null;
            _failed = null;
        }
#else
        public static bool Available => false;
        public static bool Busy => false;

        public static void Show(string scene, Action shown, Action<bool> closed, Action failed) => failed?.Invoke();
#endif
    }
}
