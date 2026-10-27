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

        /// <summary>这个入口今天还能看几次；-1 表示不限。</summary>
        public static int Left(string reason) => LiveConfig.AdLeft(Analytics.Scene(reason));

        /// <summary>按钮上的小字：「今日剩 N 次」。不限次数时返回空串。</summary>
        public static string LeftText(string reason)
        {
            int n = Left(reason);
            return n < 0 ? "" : n > 0 ? $"今日剩{n}次" : "今日已用完";
        }

        public static void Show(string reason, Action onReward, Action onCancel = null)
        {
            string scene = Analytics.Scene(reason);
            if (LiveConfig.AdLeft(scene) == 0)
            {
                Ui.Nope("今天这里的广告次数用完了，明天再来");
                onCancel?.Invoke();
                return;
            }
            Analytics.AdRequest(scene);
            void Reward()
            {
                Save.NoteAd(scene);
                onReward?.Invoke();
            }
            if (WxAd.Available && !Save.Volatile)
            {
                var block = Ui.Fill(Ui.Root, Color.black.A(0.35f), true, "adWait");
                bool wasPaused = AudioListener.pause;
                AudioListener.pause = true;
                Analytics.AdShow(scene);
                WxAd.Show(ended =>
                {
                    AudioListener.pause = wasPaused;
                    if (block) UnityEngine.Object.Destroy(block.gameObject);
                    Analytics.AdClose(scene, ended);
                    if (ended) Reward();
                    else
                    {
                        Ui.Nope("没看完，这次没有奖励");
                        onCancel?.Invoke();
                    }
                }, () =>
                {
                    AudioListener.pause = wasPaused;
                    if (block) UnityEngine.Object.Destroy(block.gameObject);
                    Analytics.AdClose(scene, false);
                    Ui.Nope("暂时没有广告，稍后再试");
                    onCancel?.Invoke();
                });
                return;
            }
            Demo(reason, scene, Reward, onCancel);
        }

        /// <summary>广告按钮右上角的小红签「剩N」。不限次数的入口不显示，用完变灰。</summary>
        public static void Badge(Transform parent, Vector2 pos, string reason)
        {
            if (Left(reason) < 0) return;
            var bg = Ui.Box(parent, new Vector2(78, 40), pos, Pal.Hex("E8392B"), null, "adLeft");
            bg.pixelsPerUnitMultiplier = 2f;
            var edge = bg.gameObject.AddComponent<Outline>();
            edge.effectColor = Pal.Hex("FFF6E2");
            edge.effectDistance = new Vector2(2f, -2f);
            var text = Ui.Label(bg.transform, "", 24, Color.white, new Vector2(0, 1), new Vector2(78, 40));
            text.fontStyle = FontStyle.Bold;
            var b = bg.gameObject.AddComponent<LeftBadge>();
            b.Reason = reason;
            b.Bg = bg;
            b.Text = text;
            b.Tick();
        }

        sealed class LeftBadge : MonoBehaviour
        {
            public string Reason;
            public Image Bg;
            public Text Text;
            float _t;

            void Update()
            {
                _t -= Time.unscaledDeltaTime;
                if (_t <= 0f) Tick();
            }

            public void Tick()
            {
                _t = 0.5f;
                int n = Left(Reason);
                Bg.enabled = Text.enabled = n >= 0;
                Text.text = "剩" + Mathf.Max(0, n);
                Bg.color = n > 0 ? Pal.Hex("E8392B") : Pal.Hex("8E8A86");
            }
        }

        static void Demo(string reason, string scene, Action onReward, Action onCancel)
        {
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
