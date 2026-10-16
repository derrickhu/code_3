using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Rescue
{
    public static class Ui
    {
        public const float DesignW = 1080f;
        public const float DesignH = 1920f;

        public static Canvas Canvas { get; private set; }
        public static RectTransform Root { get; private set; }

        static RectTransform _canvasRt;

        public static void Init(Transform host)
        {
            var go = new GameObject("UiCanvas");
            go.transform.SetParent(host, false);
            Canvas = go.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 100;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(DesignW, DesignH);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            _canvasRt = (RectTransform)go.transform;

            var phone = Rect(_canvasRt, "Phone");
            phone.anchorMin = phone.anchorMax = new Vector2(0.5f, 0.5f);
            phone.pivot = new Vector2(0.5f, 0.5f);
            phone.sizeDelta = new Vector2(DesignW, DesignH);
            Root = phone;
            FitPhone();

            if (UnityEngine.Object.FindObjectOfType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.transform.SetParent(host, false);
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
        }

        /// <summary>把 1080×1920 的手机画面缩进当前窗口正中，多出来的地方是桌面底。</summary>
        public static void FitPhone()
        {
            if (_canvasRt == null || Root == null) return;
            var size = _canvasRt.rect.size;
            if (size.x < 1f || size.y < 1f)
            {
                Canvas.ForceUpdateCanvases();
                size = _canvasRt.rect.size;
                if (size.x < 1f || size.y < 1f) return;
            }
            float scale = Mathf.Min(size.x / DesignW, size.y / DesignH);
            if (Mathf.Abs(Root.localScale.x - scale) < 0.0001f) return;
            Root.localScale = new Vector3(scale, scale, 1f);
            Root.anchoredPosition = Vector2.zero;
        }

        /// <summary>与手机画面对齐的相机视口。窗口比 9:16 宽就左右留边，更高就上下留边。</summary>
        public static Rect CameraViewport()
        {
            float window = Screen.width / Mathf.Max(1f, Screen.height);
            float target = DesignW / DesignH;
            if (window > target)
            {
                float w = target / window;
                return new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
            float h = window / target;
            return new Rect(0f, (1f - h) * 0.5f, 1f, h);
        }

        public static bool PointerOverUi()
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = Input.mousePosition };
            var hits = new List<RaycastResult>();
            es.RaycastAll(data, hits);
            return hits.Count > 0;
        }

        public static RectTransform Rect(Transform parent, string name = "rect")
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Fill(Transform parent, Color c, bool block = true, string name = "fill")
        {
            var rt = Rect(parent, name);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = block;
            return rt;
        }

        public static Image Box(Transform parent, Vector2 size, Vector2 pos, Color c, Sprite sprite = null, string name = "box")
        {
            var rt = Rect(parent, name);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Art.Rounded;
            img.type = img.sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            img.pixelsPerUnitMultiplier = 0.35f;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public static Text Label(Transform parent, string s, int size, Color c, Vector2 pos, Vector2 box,
            TextAnchor align = TextAnchor.MiddleCenter, Font font = null, string name = "label")
        {
            var rt = Rect(parent, name);
            rt.sizeDelta = box;
            rt.anchoredPosition = pos;
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font ?? Fonts.Kai;
            t.fontSize = size;
            t.color = c;
            t.alignment = align;
            t.text = s;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.lineSpacing = 1.1f;
            return t;
        }

        public static Button Btn(Transform parent, string text, Vector2 size, Vector2 pos, Color bg, Action onClick,
            int fontSize = 48, Color? fg = null, Font font = null)
        {
            var holder = Rect(parent, "btn_" + text);
            holder.sizeDelta = size;
            holder.anchoredPosition = pos;
            Box(holder, size, new Vector2(0, -8), Pal.Shadow, Art.Rounded, "shadow");
            var img = Box(holder, size, Vector2.zero, bg, Art.Rounded, "face");
            img.raycastTarget = true;
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            Label(img.transform, text, fontSize, fg ?? Color.white, Vector2.zero, size, TextAnchor.MiddleCenter, font, "text");
            b.onClick.AddListener(() =>
            {
                Sfx.Play(Snd.Tap);
                Tween.Punch(holder, 0.12f);
                onClick?.Invoke();
            });
            return b;
        }

        public static void SetText(Button b, string s) => b.transform.Find("text").GetComponent<Text>().text = s;

        public static void Anchor(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
        }
    }

    /// <summary>极简补间：不依赖第三方库，用不受时间缩放影响的时钟。</summary>
    public sealed class Tween : MonoBehaviour
    {
        sealed class Job
        {
            public Func<float, bool> Step;
            public float T, Dur, Delay;
            public Action Done;
            public UnityEngine.Object Owner;
        }

        static Tween _i;
        readonly List<Job> _jobs = new List<Job>();

        public static void Init(GameObject host) { if (_i == null) _i = host.AddComponent<Tween>(); }

        public static void Run(UnityEngine.Object owner, float dur, Action<float> step, Action done = null, float delay = 0)
        {
            _i._jobs.Add(new Job { Owner = owner, Dur = Mathf.Max(0.0001f, dur), Delay = delay, Done = done, Step = k => { step(k); return true; } });
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            for (int i = _jobs.Count - 1; i >= 0; i--)
            {
                var j = _jobs[i];
                if (j.Owner == null) { _jobs.RemoveAt(i); continue; }
                if (j.Delay > 0) { j.Delay -= dt; continue; }
                j.T += dt;
                float k = Mathf.Clamp01(j.T / j.Dur);
                j.Step(k);
                if (k >= 1f)
                {
                    _jobs.RemoveAt(i);
                    j.Done?.Invoke();
                }
            }
        }

        public static float OutBack(float k) { const float s = 1.70158f; k -= 1; return k * k * ((s + 1) * k + s) + 1; }
        public static float OutCubic(float k) { k -= 1; return k * k * k + 1; }

        public static void Pop(Transform t, float dur = 0.35f, float delay = 0)
        {
            t.localScale = Vector3.zero;
            Run(t, dur, k => t.localScale = Vector3.one * OutBack(k), null, delay);
        }

        public static void Punch(Transform t, float dur = 0.2f, float amount = 0.15f, Vector3? restScale = null)
        {
            var baseScale = restScale ?? Vector3.one;
            Run(t, dur, k => t.localScale = baseScale * (1f + Mathf.Sin(k * Mathf.PI) * amount), () => { if (t) t.localScale = baseScale; });
        }

        public static void FadeGroup(CanvasGroup g, float from, float to, float dur, Action done = null, float delay = 0)
        {
            g.alpha = from;
            Run(g, dur, k => g.alpha = Mathf.Lerp(from, to, k), done, delay);
        }
    }
}
