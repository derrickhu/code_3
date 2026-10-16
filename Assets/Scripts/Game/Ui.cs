using System;
using System.Collections.Generic;
using Rescue.Sim;
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

        /// <summary>卡通描边块：底下一层深色描边，上面一层面色。返回面色那层。</summary>
        public static Image Framed(Transform parent, Vector2 size, Vector2 pos, Color face, Color edge, float thick = 6f, string name = "framed")
        {
            var holder = Rect(parent, name);
            holder.sizeDelta = size;
            holder.anchoredPosition = pos;
            Box(holder, size + Vector2.one * thick * 2f, Vector2.zero, edge, Art.Rounded, "edge");
            return Box(holder, size, Vector2.zero, face, Art.Rounded, "face");
        }

        public static Text Stroke(Text t, Color c, float d = 3f)
        {
            var a = t.gameObject.AddComponent<Outline>();
            a.effectColor = c;
            a.effectDistance = new Vector2(d, -d);
            var b = t.gameObject.AddComponent<Outline>();
            b.effectColor = c;
            b.effectDistance = new Vector2(-d, d);
            return t;
        }

        public static Image Icon(Transform parent, string img, float size, Vector2 pos, string name = "icon")
        {
            var i = Box(parent, Vector2.one * size, pos, Color.white, Art.Img(img), name);
            i.type = Image.Type.Simple;
            i.preserveAspect = true;
            return i;
        }

        /// <summary>给任意一块加点击，带按压回弹。</summary>
        public static void Tap(Graphic g, Action onClick)
        {
            g.raycastTarget = true;
            var b = g.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            var t = g.transform.parent != null && g.transform.parent.name.StartsWith("framed") ? g.transform.parent : g.transform;
            b.onClick.AddListener(() =>
            {
                Sfx.Play(Snd.Tap);
                Tween.Punch(t, 0.12f);
                onClick?.Invoke();
            });
        }

        public static void Anchor(RectTransform rt, float x, float y)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(x, y);
        }

        public static readonly Color Edge = Pal.Hex("2E2119");
        public static readonly Color BtnRed = Pal.Hex("E8573F");
        public static readonly Color BtnDark = Pal.Hex("4A4542");
        public static readonly Color BtnYellow = Pal.Hex("F6C531");
        public static readonly Color BtnOrange = Pal.Hex("F59B2E");
        public static readonly Color BtnBlue = Pal.Hex("5B86C9");
        public static readonly Color BtnGreen = Pal.Hex("5FB548");

        /// <summary>卡通按钮：底影、粗描边、面色、顶部高光，可在文字左边放图标（广告按钮放视频图标）。</summary>
        public static RectTransform CartoonBtn(Transform parent, string text, Vector2 size, Vector2 pos, Color face, Action onClick,
            int fontSize = 52, string icon = null, Color? fg = null, Font font = null)
        {
            var holder = Rect(parent, "framedBtn");
            holder.sizeDelta = size;
            holder.anchoredPosition = pos;
            Box(holder, size + new Vector2(12, 12), new Vector2(0, -10), Pal.Shadow.A(0.5f), null, "shadow");
            Box(holder, size + new Vector2(12, 12), Vector2.zero, Edge, null, "edge");
            var f = Box(holder, size, Vector2.zero, face, null, "face");
            Box(holder, new Vector2(size.x - 16, size.y * 0.42f), new Vector2(0, -size.y * 0.25f + 4), Color.Lerp(face, Color.black, 0.12f), null, "under");
            Box(holder, new Vector2(size.x - 36, Mathf.Max(10, size.y * 0.13f)), new Vector2(0, size.y * 0.32f), Color.white.A(0.3f), null, "gloss");
            float iconW = icon != null ? size.y * 0.62f : 0f;
            var label = Label(holder, text, fontSize, fg ?? Pal.Paper, new Vector2(iconW * 0.55f, 2), new Vector2(size.x - iconW - 20, size.y),
                TextAnchor.MiddleCenter, font ?? Fonts.Brush, "text");
            var ink = fg ?? Pal.Paper;
            if (ink.grayscale < 0.45f) Stroke(label, Color.Lerp(face, Color.white, 0.55f).A(0.9f), 1.5f);
            else Stroke(label, Color.Lerp(face, Color.black, 0.72f), 2.5f);
            if (icon != null)
            {
                float textW = Mathf.Min(size.x - iconW - 20, label.preferredWidth);
                Icon(holder, icon, iconW, new Vector2(iconW * 0.55f - textW * 0.5f - iconW * 0.62f, 2), "btnIcon");
            }
            Tap(f, onClick);
            return holder;
        }

        public static void SetBtnText(RectTransform btn, string s) => btn.Find("text").GetComponent<Text>().text = s;

        public static void SetBtnFace(RectTransform btn, Color c)
        {
            btn.Find("face").GetComponent<Image>().color = c;
            btn.Find("under").GetComponent<Image>().color = Color.Lerp(c, Color.black, 0.12f);
        }

        /// <summary>竖向内容高 needH，屏幕矮（平板、横向留白多的机型）放不下时整体等比缩小。返回缩放层。</summary>
        public static RectTransform Fit(Transform parent, float needH, string name = "fit")
        {
            var rt = Rect(parent, name);
            float h = Root.rect.height;
            if (h > 0 && needH > h) rt.localScale = Vector3.one * (h / needH);
            return rt;
        }

        /// <summary>木框面板：深色外描边、木色框、深色内线、面色。返回放内容的那层（中心为原点）。</summary>
        public static RectTransform WoodPanel(Transform parent, Vector2 size, Vector2 pos, Color? wood = null, Color? face = null, string name = "wood")
        {
            var holder = Rect(parent, name);
            holder.sizeDelta = size;
            holder.anchoredPosition = pos;
            Box(holder, size + new Vector2(14, 14), new Vector2(0, -14), Pal.Shadow.A(0.6f), null, "shadow");
            Box(holder, size + new Vector2(14, 14), Vector2.zero, Edge, null, "edge");
            Box(holder, size, Vector2.zero, wood ?? Pal.Hex("8C6B4E"), null, "frame");
            Box(holder, size - new Vector2(52, 52), new Vector2(0, -6), Pal.Hex("5E4532"), null, "innerEdge");
            Box(holder, size - new Vector2(64, 64), new Vector2(0, -6), face ?? Pal.Hex("E9DDBF"), null, "inner");
            return holder;
        }

        /// <summary>面板标题：粗描边大字，压在面板上沿。</summary>
        public static Text Title(Transform parent, string s, Vector2 pos, int size = 92, Color? c = null)
        {
            var t = Label(parent, s, size, c ?? Pal.Hex("F7A928"), pos, new Vector2(800, size + 40), TextAnchor.MiddleCenter, Fonts.Brush, "title");
            Stroke(t, Edge, 4.5f);
            t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(0, -6);
            return t;
        }

        public static Image CloseX(Transform parent, Vector2 pos, Action onClick, float size = 116)
        {
            var x = Icon(parent, "ui_close", size, pos, "close");
            Tap(x, onClick);
            return x;
        }

        /// <summary>小圆角标签（稀有度、兵器名）。</summary>
        public static Image Tag(Transform parent, string s, Color c, Vector2 pos, float w = 0, int font = 30)
        {
            if (w <= 0) w = s.Length * font + 34;
            var holder = Rect(parent, "tag");
            holder.anchoredPosition = pos;
            Box(holder, new Vector2(w + 8, font + 26), Vector2.zero, Edge, null, "edge").pixelsPerUnitMultiplier = 1f;
            var face = Box(holder, new Vector2(w, font + 18), Vector2.zero, c, null, "face");
            face.pixelsPerUnitMultiplier = 1f;
            Stroke(Label(holder, s, font, Pal.Paper, new Vector2(0, 1), new Vector2(w + 20, font + 18)), Color.Lerp(c, Color.black, 0.55f), 2f);
            return face;
        }

        /// <summary>屏幕中下方的深色提示条，自动消失。</summary>
        public static void Toast(string s, float y = -260)
        {
            int len = Mathf.Max(6, s.Length);
            var bg = Box(Root, new Vector2(Mathf.Min(1000, len * 42 + 120), 104), new Vector2(0, y), Pal.Ink.A(0.86f), null, "toast");
            Label(bg.transform, s, 42, Pal.Paper, Vector2.zero, new Vector2(980, 100));
            bg.transform.SetAsLastSibling();
            Tween.Pop(bg.transform, 0.2f);
            Tween.Run(bg, 1.6f, k => { }, () => UnityEngine.Object.Destroy(bg.gameObject));
        }

        /// <summary>转圈的光芒（结算、抽奖中奖）。</summary>
        public static void Spin(Transform t, float degPerSec)
        {
            Tween.Run(t, 6f, k => { if (t) t.localRotation = Quaternion.Euler(0, 0, -k * degPerSec * 6f); }, () => { if (t) Spin(t, degPerSec); });
        }

        public static string Stars(int n, int total, string on = "FFD84A", string off = "4A4232")
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < total; i++) sb.Append($"<color=#{(i < n ? on : off)}>★</color>");
            return sb.ToString();
        }

        /// <summary>座次星条：普通小阶按满星数画，天魁星不封顶写成「★×N」。</summary>
        public static string RankStars(int tier, int n, string on = "FFD84A", string off = "4A4232")
        {
            int cap = RankRules.StarsAt(tier);
            return cap > 0 ? Stars(n, cap, on, off) : $"<color=#{on}>★</color>×{n}";
        }

        static readonly Color[] MajorColors =
        {
            Pal.Hex("8A8076"), Pal.Hex("6FA05A"), Pal.Hex("4F9A7A"), Pal.Hex("3F86B8"), Pal.Hex("4F6BC2"), Pal.Hex("2E8FB0"),
            Pal.Hex("8A5BC4"), Pal.Hex("B0569A"), Pal.Hex("D06A2C"), Pal.Hex("7B4FA8"), Pal.Hex("C8962E"), Pal.Hex("D9452F"),
        };

        static readonly Color[] Grades = { Pal.Hex("8A8076"), Pal.Hex("4FA05A"), Pal.Hex("3F86B8"), Pal.Hex("D9822B") };

        /// <summary>兵器品级色：凡器灰、利器绿、宝器蓝、神兵橙金。</summary>
        public static Color GradeColor(int grade) => Grades[Mathf.Clamp(grade, 0, Grades.Length - 1)];

        /// <summary>大阶的徽记色：从灰绿到金红，越高越亮。</summary>
        public static Color RankColor(int tier) => MajorColors[RankRules.Major(tier)];

        static readonly string[] Seals = { "喽", "校", "都", "提", "步", "水", "马", "骠", "虎", "煞", "罡", "魁" };

        /// <summary>座次勋章：金边圆章挂两条绶带，章心写大阶的一个字，下面一到三颗金点是小阶；旁边另写全名。</summary>
        public static RectTransform RankBadge(Transform parent, int tier, Vector2 pos, float scale = 1f)
        {
            var holder = Rect(parent, "rankBadge");
            holder.anchoredPosition = pos;
            holder.localScale = Vector3.one * scale;
            var c = RankColor(tier);
            var deep = Color.Lerp(c, Color.black, 0.45f);
            foreach (float side in new[] { -1f, 1f })
            {
                var tail = Rect(holder, "ribbon");
                tail.anchoredPosition = new Vector2(side * 26, -58);
                tail.localRotation = Quaternion.Euler(0, 0, side * 18);
                Box(tail, new Vector2(40, 64), Vector2.zero, Edge, null, "edge").pixelsPerUnitMultiplier = 1f;
                Box(tail, new Vector2(30, 56), new Vector2(0, 2), deep, null, "face").pixelsPerUnitMultiplier = 1f;
            }
            Simple(Box(holder, new Vector2(136, 136), Vector2.zero, Edge, Art.Circle, "edge"));
            Simple(Box(holder, new Vector2(126, 126), Vector2.zero, Pal.Hex("E8B84A"), Art.Circle, "rim"));
            Simple(Box(holder, new Vector2(104, 104), Vector2.zero, Edge, Art.Circle, "inner"));
            Simple(Box(holder, new Vector2(98, 98), Vector2.zero, c, Art.Circle, "face"));
            int major = RankRules.Major(tier);
            Stroke(Label(holder, Seals[Mathf.Clamp(major, 0, Seals.Length - 1)], 62, Pal.Paper, new Vector2(0, 10), new Vector2(110, 80),
                TextAnchor.MiddleCenter, Fonts.Brush), Color.Lerp(c, Color.black, 0.65f), 2.5f);
            if (major < RankRules.TopMajor)
            {
                int pips = RankRules.Sub(tier) + 1;
                for (int i = 0; i < pips; i++)
                    Simple(Box(holder, new Vector2(14, 14), new Vector2((i - (pips - 1) * 0.5f) * 18, -36), Pal.Hex("FFE36A"), Art.Circle, "pip"));
            }
            else Label(holder, "★", 30, Pal.Hex("FFE36A"), new Vector2(0, -33), new Vector2(60, 40));
            return holder;
        }

        static void Simple(Image img) => img.type = Image.Type.Simple;

        /// <summary>竖向滚动区：返回放内容的 content（顶端对齐，宽同 view），高度由调用方设。</summary>
        public static RectTransform Scroll(Transform parent, Vector2 size, Vector2 pos, float contentHeight, string name = "scroll")
        {
            var view = Rect(parent, name);
            view.sizeDelta = size;
            view.anchoredPosition = pos;
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            view.gameObject.AddComponent<RectMask2D>();
            var content = Rect(view, "content");
            content.anchorMin = new Vector2(0.5f, 1f);
            content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.sizeDelta = new Vector2(size.x, Mathf.Max(size.y, contentHeight));
            content.anchoredPosition = Vector2.zero;
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.content = content;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.scrollSensitivity = 40f;
            sr.decelerationRate = 0.12f;
            view.gameObject.AddComponent<ScrollHold>().Init(sr);
            return content;
        }

        /// <summary>把滚动区停在离顶 y 处（弹出动画期间也保持住）。</summary>
        public static void ScrollTo(RectTransform content, float y)
        {
            var hold = content.parent.GetComponent<ScrollHold>();
            float max = Mathf.Max(0, content.sizeDelta.y - ((RectTransform)content.parent).sizeDelta.y);
            y = Mathf.Clamp(y, 0, max);
            content.anchoredPosition = new Vector2(0, y);
            if (hold) hold.Y = y;
        }

        /// <summary>content 里按“离顶多远”摆子物体（子物体锚点在 content 中心）。</summary>
        public static Vector2 Top(RectTransform content, float x, float yFromTop) => new Vector2(x, content.sizeDelta.y * 0.5f - yFromTop);
    }

    /// <summary>
    /// 面板弹出时父节点从零缩放，ScrollRect 会按退化的边界把内容弹走；
    /// 开头这一小段时间里钉住起始位置，玩家一碰就放手。
    /// </summary>
    public sealed class ScrollHold : MonoBehaviour
    {
        public float Y;
        ScrollRect _sr;
        float _t = 0.6f;

        public void Init(ScrollRect sr) => _sr = sr;

        void LateUpdate()
        {
            if (_sr == null || _t <= 0) return;
            _t -= Time.unscaledDeltaTime;
            if (Input.GetMouseButton(0) || Input.touchCount > 0) { _t = 0; return; }
            _sr.StopMovement();
            _sr.content.anchoredPosition = new Vector2(0, Y);
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
