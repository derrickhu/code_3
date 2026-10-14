using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>激励视频占位：小游戏版换成 wx.createRewardedVideoAd / tt.createRewardedVideoAd。</summary>
    public static class AdStub
    {
        public static void Show(string reason, Action onReward)
        {
            var layer = Ui.Fill(Ui.Root, new Color(0, 0, 0, 0.88f), true, "ad");
            var label = Ui.Label(layer, "", 52, Color.white, new Vector2(0, 60), new Vector2(900, 300));
            Ui.Label(layer, "（演示版：模拟激励视频）\n" + reason, 36, new Color(1, 1, 1, 0.6f), new Vector2(0, -140), new Vector2(900, 200));
            float t = 2.2f;
            Tween.Run(layer, t, k =>
            {
                label.text = "广告播放中…  " + Mathf.CeilToInt(t * (1 - k));
            }, () =>
            {
                UnityEngine.Object.Destroy(layer.gameObject);
                onReward?.Invoke();
            });
        }
    }
}
