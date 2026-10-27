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
    /// <summary>微信激励视频。广告位从远程配置下发；没有广告位或不在小游戏里时，AdStub 走演示版。</summary>
    public static class WxAd
    {
#if RESCUE_WX_AD
        static WXRewardedVideoAd _ad;
        static string _unit;
        static Action<bool> _closed;

        public static bool Available => !string.IsNullOrEmpty(LiveConfig.AdUnitId);

        static void Ensure()
        {
            string unit = LiveConfig.AdUnitId;
            if (_ad != null && _unit == unit) return;
            if (_ad != null)
            {
                try { _ad.Destroy(); }
                catch (Exception) { }
            }
            _unit = unit;
            _ad = WX.CreateRewardedVideoAd(new WXCreateRewardedVideoAdParam { adUnitId = unit, multiton = false });
            _ad.OnClose(r =>
            {
                var cb = _closed;
                _closed = null;
                cb?.Invoke(r != null && r.isEnded);
            });
            _ad.OnError(e => Debug.LogWarning("[WxAd] error " + (e != null ? e.errCode.ToString() : "?")));
        }

        /// <summary>closed(true) 表示看完了；failed 表示拉不到广告（没填充、网络断了）。</summary>
        public static void Show(Action<bool> closed, Action failed)
        {
            try
            {
                Ensure();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[WxAd] create failed " + e.Message);
                failed();
                return;
            }
            _closed = closed;
            _ad.Show(_ => { }, _ =>
            {
                _ad.Load(__ => _ad.Show(___ => { }, ___ => { _closed = null; failed(); }),
                    __ => { _closed = null; failed(); });
            });
        }
#else
        public static bool Available => false;

        public static void Show(Action<bool> closed, Action failed) => failed();
#endif
    }
}
