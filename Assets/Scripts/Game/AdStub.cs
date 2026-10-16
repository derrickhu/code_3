using System;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>激励视频占位：小游戏版换成 wx.createRewardedVideoAd / tt.createRewardedVideoAd。
    /// 样子照平台激励视频：顶栏「广告 | N秒后可获得奖励」+ 关闭，看满才发奖励，中途关要二次确认。</summary>
    public static class AdStub
    {
        public const float Seconds = 3f;

        public static void Show(string reason, Action onReward, Action onCancel = null)
        {
            string scene = Analytics.Scene(reason);
            Analytics.AdRequest(scene);
            Analytics.AdShow(scene);
            var layer = Ui.Fill(Ui.Root, Color.black, true, "ad");
            var video = Ui.Box(layer, new Vector2(1080, 1300), new Vector2(0, 120), Color.white, Art.Img("home_bg2"), "video");
            video.type = Image.Type.Simple;
            video.preserveAspect = false;
            Ui.Box(layer, new Vector2(1080, 1300), new Vector2(0, 120), new Color(0, 0, 0, 0.35f), Art.White, "dim").type = Image.Type.Simple;
            var bust = Ui.Icon(layer, "merchant_bust", 420, new Vector2(0, 300));
            Tween.Run(bust, 2.4f, k => { if (bust) bust.transform.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI * 4) * 0.04f); });
            var slogan = Ui.Label(layer, "梁山好汉 等你来救", 84, Pal.GoldBright, new Vector2(0, 0), new Vector2(1000, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(slogan, Ui.Edge, 4f);
            Ui.Label(layer, "激励视频广告位（演示）· " + reason, 34, Color.white.A(0.75f), new Vector2(0, -100), new Vector2(1000, 60));

            var top = Ui.Rect(layer, "top");
            Ui.Anchor(top, 0.5f, 1f);
            Ui.Box(top, new Vector2(480, 70), new Vector2(-270, -70), new Color(0, 0, 0, 0.55f), null, "pill");
            var count = Ui.Label(top, "", 32, Color.white, new Vector2(-270, -70), new Vector2(470, 70));
            var closeBg = Ui.Box(top, new Vector2(150, 70), new Vector2(430, -70), new Color(0, 0, 0, 0.55f), null, "closePill");
            Ui.Label(closeBg.transform, "关闭", 32, Color.white, Vector2.zero, new Vector2(150, 70));

            var card = Ui.Framed(layer, new Vector2(960, 300), new Vector2(0, -760), Color.white, Pal.Hex("DDDDDD"), 2f, "framedCard");
            var logo = Ui.Box(card.transform, new Vector2(150, 150), new Vector2(-370, 50), Pal.Red, null, "logo");
            Ui.Label(logo.transform, "宋", 96, Pal.Paper, Vector2.zero, new Vector2(150, 150), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(card.transform, "营救宋江 · 水浒对战", 42, Pal.Ink, new Vector2(20, 80), new Vector2(560, 60), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            Ui.Label(card.transform, "摆字聚义，抢先救出宋江哥哥！", 30, Pal.InkSoft, new Vector2(20, 20), new Vector2(560, 50), TextAnchor.MiddleLeft);
            var dl = Ui.Box(card.transform, new Vector2(880, 90), new Vector2(0, -90), Pal.Hex("07C160"), null, "download");
            Ui.Label(dl.transform, "立即体验", 40, Color.white, Vector2.zero, new Vector2(880, 90));

            float left = Seconds;
            bool done = false, closed = false;
            RectTransform ask = null;
            void Finish(bool reward)
            {
                if (closed) return;
                closed = true;
                Analytics.AdClose(scene, reward);
                UnityEngine.Object.Destroy(layer.gameObject);
                if (reward) onReward?.Invoke();
                else onCancel?.Invoke();
            }
            void Tick()
            {
                if (!layer) return;
                if (ask == null) left -= 0.1f;
                if (left <= 0f && !done)
                {
                    done = true;
                    count.text = "已获得奖励";
                    count.color = Pal.Hex("7CE38B");
                    Sfx.Play(Snd.Rain, 0.6f);
                    if (Save.Volatile) Tween.Run(layer, 0.4f, k => { }, () => Finish(true));
                    return;
                }
                if (!done) count.text = $"广告 | {Mathf.CeilToInt(left)}秒后可获得奖励";
                if (!done) Tween.Run(layer, 0.1f, k => { }, Tick);
            }
            Tick();

            Ui.Tap(closeBg, () =>
            {
                if (done) { Finish(true); return; }
                if (ask != null) return;
                ask = Ui.Fill(layer, new Color(0, 0, 0, 0.6f), true, "ask");
                var box = Ui.Framed(ask, new Vector2(760, 420), Vector2.zero, Color.white, Pal.Hex("CCCCCC"), 2f, "askBox");
                Ui.Label(box.transform, "再看一会儿就能拿到奖励了\n现在关闭将无法获得奖励", 38, Pal.Ink, new Vector2(0, 70), new Vector2(700, 160));
                var keep = Ui.Box(box.transform, new Vector2(320, 100), new Vector2(-170, -120), Pal.Hex("07C160"), null, "keep");
                Ui.Label(keep.transform, "继续观看", 38, Color.white, Vector2.zero, new Vector2(320, 100));
                var quit = Ui.Box(box.transform, new Vector2(320, 100), new Vector2(170, -120), Pal.Hex("EEEEEE"), null, "quit");
                Ui.Label(quit.transform, "放弃奖励", 38, Pal.InkSoft, Vector2.zero, new Vector2(320, 100));
                Ui.Tap(keep, () => { UnityEngine.Object.Destroy(ask.gameObject); ask = null; });
                Ui.Tap(quit, () => Finish(false));
            });
        }
    }
}
