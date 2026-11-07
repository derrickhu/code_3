using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>
    /// 启动页：和微信封面同一张图（tools/build_loading_art.py 合成，标题、进度槽、健康忠告、适龄标都在图上），
    /// 这里只往图上的槽里画金色进度和一行状态字。云存档回来并且最短展示时间到了才进主界面。
    /// </summary>
    public sealed class LoadingScreen : MonoBehaviour
    {
        public const float MinSeconds = 2.6f;

        /// <summary>图是 1080x1920，槽的位置和 build_loading_art.py 一致（720 设计尺寸 ×1.5）。</summary>
        const float ArtW = 1080f, ArtH = 1920f;
        const float BarW = 600f, BarH = 15f, BarBottom = 321f;
        const float TextBottom = 348f;

        RectTransform _root, _frame, _stage;
        CanvasGroup _group;
        RectTransform _fill, _head;
        Text _status;
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

            var night = Ui.Rect(_root, "night");
            Ui.PinBleed(night);
            var nightImg = night.gameObject.AddComponent<Image>();
            nightImg.color = Pal.Hex("140D09");
            nightImg.raycastTarget = true;

            // 整张图等比铺满（和微信 noBorder 一样），不拉伸；长屏裁左右，要紧的东西都在中间。
            _frame = Ui.Rect(_root, "frame");
            Ui.PinBleed(_frame);
            _stage = Ui.Rect(_frame, "art");
            _stage.anchorMin = _stage.anchorMax = new Vector2(0.5f, 0.5f);
            _stage.sizeDelta = new Vector2(ArtW, ArtH);
            var img = _stage.gameObject.AddComponent<Image>();
            img.sprite = Splash();
            img.raycastTarget = false;
            if (img.sprite == null) img.color = Pal.Hex("140D09");

            BuildBar();
            Fit();
            _root.SetAsLastSibling();
        }

        /// <summary>金色进度落在图上画好的黑漆槽里，前端一颗亮点；上面一行状态字。</summary>
        void BuildBar()
        {
            var track = Ui.Box(_stage, new Vector2(BarW, BarH), Vector2.zero, Pal.Hex("000000").A(0.45f), Art.Rounded, "barTrack");
            DockBottom(track.rectTransform, BarBottom, BarH);
            var fill = Ui.Box(track.transform, new Vector2(BarH, BarH), Vector2.zero, Pal.Hex("E6B54A"), Art.Rounded, "barFill");
            _fill = fill.rectTransform;
            _fill.anchorMin = _fill.anchorMax = new Vector2(0f, 0.5f);
            _fill.pivot = new Vector2(0f, 0.5f);
            _fill.anchoredPosition = Vector2.zero;
            var shine = Ui.Box(_fill, new Vector2(10, BarH * 0.32f), Vector2.zero, Color.white.A(0.35f), Art.Rounded, "shine");
            shine.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            shine.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            shine.rectTransform.sizeDelta = new Vector2(-BarH * 0.6f, BarH * 0.32f);
            shine.rectTransform.anchoredPosition = new Vector2(0f, BarH * 0.16f);
            var head = Ui.Box(_fill, new Vector2(BarH * 3.2f, BarH * 3.2f), Vector2.zero, Pal.Hex("FFE7A8").A(0.8f), Art.SoftDot, "head");
            _head = head.rectTransform;
            _head.anchorMin = _head.anchorMax = new Vector2(1f, 0.5f);
            _head.anchoredPosition = new Vector2(-BarH * 0.4f, 0f);
            _status = Ui.Label(_stage, "正在加载资源", 26, Pal.Hex("EBDDBE"), Vector2.zero, new Vector2(BarW, 34), TextAnchor.MiddleCenter, null, "status");
            DockBottom(_status.rectTransform, TextBottom, 34);
            foreach (var bar in new[] { track, fill, shine }) bar.pixelsPerUnitMultiplier = 18f / (BarH * 0.5f);
            shine.pixelsPerUnitMultiplier *= 2f;
            ApplyFill(0f);
        }

        void Fit()
        {
            if (_frame == null || _stage == null) return;
            var size = _frame.rect.size;
            if (size.x < 1f || size.y < 1f) return;
            float k = Mathf.Max(size.x / ArtW, size.y / ArtH);
            _stage.localScale = new Vector3(k, k, 1f);
        }

        public void SetFill(float p)
        {
            _hold = true;
            ApplyFill(p);
        }

        void ApplyFill(float p)
        {
            if (_fill == null) return;
            p = Mathf.Clamp01(p);
            _fill.sizeDelta = new Vector2(Mathf.Lerp(BarH, BarW, p), BarH);
            if (_status != null) _status.text = p >= 0.999f ? "开始游戏" : $"正在加载资源　{Mathf.RoundToInt(p * 100)}%";
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
            Fit();
            if (_head != null)
            {
                float pulse = 0.85f + 0.15f * Mathf.Sin(Time.realtimeSinceStartup * 9f);
                _head.localScale = new Vector3(pulse, pulse, 1f);
            }
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
