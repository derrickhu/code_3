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

        RectTransform _root;
        CanvasGroup _group;
        float _born;

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
            _root.SetAsLastSibling();
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
