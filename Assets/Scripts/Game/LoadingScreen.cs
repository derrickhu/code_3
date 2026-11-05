using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>启动页：水墨标题图，底下是健康游戏忠告全文。云存档回来并且最短展示时间到了才进主界面。</summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        public const float MinSeconds = 2.6f;

        const string AdviceTitle = "健康游戏忠告";
        const string Advice =
            "抵制不良游戏，拒绝盗版游戏。\n注意自我保护，谨防受骗上当。\n适度游戏益脑，沉迷游戏伤身。\n合理安排时间，享受健康生活。";

        const float BarW = 480f;
        const float BarH = 18f;

        RectTransform _root;
        CanvasGroup _group;
        RectTransform _fill;
        float _born;
        bool _hold;

        public static LoadingScreen Open(Transform host)
        {
            var go = new GameObject("Loading");
            go.transform.SetParent(host, false);
            var s = go.AddComponent<LoadingScreen>();
            s.Build();
            return s;
        }

        void Build()
        {
            _born = Time.realtimeSinceStartup;
            _root = Ui.Rect(Ui.Root, "Loading");
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;
            _group = _root.gameObject.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = true;

            var paper = Ui.Rect(_root, "paper");
            Ui.PinBleed(paper);
            var paperImg = paper.gameObject.AddComponent<Image>();
            paperImg.color = Pal.Paper;
            paperImg.raycastTarget = false;

            var bg = Ui.Rect(_root, "art");
            Ui.PinBleed(bg);
            var img = bg.gameObject.AddComponent<Image>();
            img.sprite = Splash();
            img.color = Color.white;
            img.raycastTarget = true;
            img.preserveAspect = false;
            if (img.sprite == null) img.color = Pal.Paper;

            var title = Ui.Label(_root, AdviceTitle, 36, Pal.Ink, Vector2.zero, new Vector2(920, 52),
                TextAnchor.MiddleCenter, Fonts.Brush, "adviceTitle");
            var body = Ui.Label(_root, Advice, 30, Pal.Ink, Vector2.zero, new Vector2(980, 176),
                TextAnchor.UpperCenter, Fonts.Kai, "advice");
            body.lineSpacing = 1.08f;
            DockBottom(title.rectTransform, 248, 52);
            DockBottom(body.rectTransform, 52, 176);
            BuildBar();
            AgeBadge();
            _root.SetAsLastSibling();
        }

        /// <summary>细一条朱红，停在健康忠告上面。Mac 上这就是唯一的启动页。</summary>
        void BuildBar()
        {
            var track = Ui.Box(_root, new Vector2(BarW, BarH), Vector2.zero, Pal.Hex("CDBFA8"), Art.White, "barTrack");
            DockBottom(track.rectTransform, 348, BarH);
            var fill = Ui.Box(track.transform, new Vector2(8, BarH), Vector2.zero, Pal.Hex("8E3A30"), Art.White, "barFill");
            _fill = fill.rectTransform;
            _fill.anchorMin = _fill.anchorMax = new Vector2(0f, 0.5f);
            _fill.pivot = new Vector2(0f, 0.5f);
            _fill.anchoredPosition = Vector2.zero;
            _fill.sizeDelta = new Vector2(8f, BarH);
        }

        public void SetFill(float p)
        {
            _hold = true;
            ApplyFill(p);
        }

        void ApplyFill(float p)
        {
            if (_fill == null) return;
            _fill.sizeDelta = new Vector2(Mathf.Lerp(8f, BarW, Mathf.Clamp01(p)), BarH);
        }

        /// <summary>12+ 适龄标，和后台适龄一致。图还没导入时用一块蓝底代替。</summary>
        void AgeBadge()
        {
            var badge = Ui.Rect(_root, "age");
            badge.anchorMin = badge.anchorMax = new Vector2(0f, 1f);
            badge.pivot = new Vector2(0f, 1f);
            badge.sizeDelta = new Vector2(108, 108);
            badge.anchoredPosition = new Vector2(36, -48);
            var img = badge.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            var sprite = Resources.Load<Sprite>("Art/age12");
            if (sprite != null)
            {
                img.sprite = sprite;
                img.color = Color.white;
                return;
            }
            img.sprite = Art.Rounded;
            img.color = Pal.Hex("1F5FD6");
            Ui.Label(badge, "12+", 52, Color.white, Vector2.zero, new Vector2(120, 80), TextAnchor.MiddleCenter, Fonts.Brush);
        }

        /// <summary>手写的 meta 若没导成 Sprite，退回整张贴图，避免铺成一块白圆角。</summary>
        static Sprite Splash()
        {
            var sprite = Resources.Load<Sprite>("Art/loading");
            if (sprite != null && sprite.texture != null && sprite.texture.width > 128) return sprite;
            var tex = Resources.Load<Texture2D>("Art/loading");
            if (tex == null || tex.width < 128) return null;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        static void DockBottom(RectTransform rt, float y, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, h);
            rt.anchoredPosition = new Vector2(0f, y);
        }

        void Update()
        {
            if (_hold || _fill == null) return;
            float p = Mathf.Clamp01((Time.realtimeSinceStartup - _born) / MinSeconds);
            ApplyFill(Mathf.Lerp(0.06f, 0.9f, p));
        }

        public void BringToFront()
        {
            if (_root) _root.SetAsLastSibling();
        }

        public IEnumerator WaitMin()
        {
            while (Time.realtimeSinceStartup - _born < MinSeconds) yield return null;
        }

        public IEnumerator FadeOut()
        {
            float t = 0f;
            const float dur = 0.4f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                if (_group) _group.alpha = 1f - Mathf.Clamp01(t / dur);
                yield return null;
            }
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (_root) Destroy(_root.gameObject);
        }
    }
}
