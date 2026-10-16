using System.Collections.Generic;
using UnityEngine;

namespace Rescue
{
    public static class Pal
    {
        public static readonly Color Paper = Hex("EFE4CC");
        public static readonly Color PaperDark = Hex("E0D2B4");
        public static readonly Color Ink = Hex("1E1A16");
        public static readonly Color InkSoft = Hex("4A4238");
        public static readonly Color Grass = Hex("9DB89A");
        public static readonly Color GrassRival = Hex("AEBDA6");
        public static readonly Color Path = Hex("CDB48E");
        public static readonly Color PathEdge = Hex("A88D66");
        public static readonly Color Water = Hex("86AFC0");
        public static readonly Color Willow = Hex("4E7A45");
        public static readonly Color Earth = Hex("B98A6A");
        public static readonly Color Marsh = Hex("7FA7A0");
        public static readonly Color Raft = Hex("D8C08E");
        public static readonly Color Purple = Hex("7B4FA8");
        public static readonly Color Valid = Hex("F2D25C");
        public static readonly Color Tile = Hex("FBF6EA");
        public static readonly Color TileHero = Hex("F3DFA2");
        public static readonly Color Gold = Hex("B07A12");
        public static readonly Color GoldBright = Hex("E2B33C");
        public static readonly Color Red = Hex("B22222");
        public static readonly Color RedDeep = Hex("7A1F12");
        public static readonly Color Hp = Hex("C0392B");
        public static readonly Color Shadow = new Color(0.12f, 0.09f, 0.06f, 0.28f);

        static readonly Color[] LevelInk =
        {
            Hex("2B2620"), Hex("1F3A6F"), Hex("6B2D7B"), Hex("A0522D"), Hex("B22222"),
        };

        public static Color ForLevel(int level) => LevelInk[Mathf.Clamp(level - 1, 0, LevelInk.Length - 1)];

        public static Color Hex(string h)
        {
            ColorUtility.TryParseHtmlString("#" + h, out var c);
            return c;
        }

        public static Color A(this Color c, float a) { c.a = a; return c; }
    }

    public static class Fonts
    {
        static Font _brush, _kai;

        /// <summary>马善政楷书子集：兵种、好汉、标题。</summary>
        public static Font Brush => _brush ? _brush : (_brush = Load("Fonts/Brush"));

        /// <summary>文楷子集（已改名 Shuihu Kai）：正文、数字。</summary>
        public static Font Kai => _kai ? _kai : (_kai = Load("Fonts/Kai"));

        static Font Load(string path)
        {
            var f = Resources.Load<Font>(path);
            if (f == null)
            {
                Debug.LogError("缺字体 " + path + "，先跑 tools/build_font.py");
                f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }
            return f;
        }
    }

    /// <summary>运行时生成的程序化贴图，不依赖外部美术。</summary>
    public static class Art
    {
        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

        public static Sprite Rounded => Get("rounded", () => RoundRect(64, 16, 0));
        public static Sprite Frame => Get("frame", () => RoundRect(64, 18, 7));
        public static Sprite Circle => Get("circle", () => Disc(64, 0f));
        public static Sprite SoftDot => Get("softdot", () => Disc(64, 0.9f));
        public static Sprite RangeFill => Get("rangefill", RangeFillTex);
        public static Sprite Ring => Get("ring", () => RingTex(128, 0.86f));
        public static Sprite Blob => Get("blob", () => InkBlob(128, 7));
        public static Sprite Blob2 => Get("blob2", () => InkBlob(128, 99));
        public static Sprite Slash => Get("slash", SlashTex);
        public static Sprite Stroke => Get("stroke", StrokeTex);
        public static Sprite Heart => Get("heart", HeartTex);
        public static Sprite Paper => Get("paper", PaperTex);
        public static Sprite White => Get("white", () => Solid(4));
        public static Sprite Swirl => Get("swirl", SwirlTex);
        public static Sprite ArrowShaft => Get("arrow", ArrowTex);
        public static Sprite Dashed => Get("dashed", DashedTex);
        /// <summary>左侧圆角、右侧齐边的进度条，用来嵌进聚义按钮。</summary>
        public static Sprite Progress => Get("progress", ProgressCap);

        /// <summary>Resources/Art 下的手绘图（生成后抠图），长边 = 1 个世界单位。</summary>
        public static Sprite Img(string name) => Get("img_" + name, () => Resources.Load<Sprite>("Art/" + name) ?? Rounded);

        /// <summary>地面贴图按格子切成 1×1 的小块，相邻格子取相邻区域，拼起来比较连贯。</summary>
        public static Sprite Ground(string tex, int c, int r)
        {
            int vx = ((c % 2) + 2) % 2, vy = ((r % 2) + 2) % 2;
            return Get($"g_{tex}_{vx}_{vy}", () =>
            {
                var src = Img(tex);
                var t = src.texture;
                float half = Mathf.Min(t.width, t.height) * 0.5f;
                var rect = new Rect(src.rect.x + vx * half, src.rect.y + vy * half, half, half);
                return Sprite.Create(t, rect, new Vector2(0.5f, 0.5f), half, 0, SpriteMeshType.FullRect);
            });
        }

        static Sprite Get(string key, System.Func<Sprite> make)
        {
            if (!Cache.TryGetValue(key, out var s) || s == null) Cache[key] = s = make();
            return s;
        }

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            return t;
        }

        static Sprite Make(Texture2D t, Vector4 border = default)
        {
            t.Apply(false, true);
            return Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), Mathf.Max(t.width, t.height), 0, SpriteMeshType.FullRect, border);
        }

        static Sprite Solid(int n)
        {
            var t = NewTex(n, n);
            var px = new Color32[n * n];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            t.SetPixels32(px);
            return Make(t);
        }

        static Sprite RoundRect(int n, float radius, float ring)
        {
            var t = NewTex(n, n);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(radius - (x + 0.5f), (x + 0.5f) - (n - radius)));
                    float dy = Mathf.Max(0, Mathf.Max(radius - (y + 0.5f), (y + 0.5f) - (n - radius)));
                    float d = Mathf.Sqrt(dx * dx + dy * dy) - radius;
                    float a = Mathf.Clamp01(0.5f - d);
                    if (ring > 0) a *= Mathf.Clamp01(d + ring + 0.5f);
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px);
            float b = radius + 2;
            return Make(t, new Vector4(b, b, b, b));
        }

        /// <summary>左圆右齐。九宫格拉宽时只拉中间，左端圆角保持住。</summary>
        static Sprite ProgressCap()
        {
            int n = 64;
            float radius = 16f;
            var t = NewTex(n, n);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float pxX = x + 0.5f, pxY = y + 0.5f;
                    float d;
                    if (pxX >= radius)
                        d = -1f;
                    else
                    {
                        float cy = Mathf.Clamp(pxY, radius, n - radius);
                        d = Vector2.Distance(new Vector2(pxX, pxY), new Vector2(radius, cy)) - radius;
                    }
                    float a = d <= 0f ? 1f : Mathf.Clamp01(0.5f - d);
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px);
            return Make(t, new Vector4(radius + 2, radius + 2, 2, radius + 2));
        }

        static Sprite Disc(int n, float soft)
        {
            var t = NewTex(n, n);
            var px = new Color[n * n];
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = soft > 0 ? Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) * 1.6f : Mathf.Clamp01((1f - d) * r);
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
                }
            t.SetPixels(px);
            return Make(t);
        }

        /// <summary>射程圆面：边缘抗锯齿的实心圆，半径正好铺满贴图。</summary>
        static Sprite RangeFillTex()
        {
            int n = 256;
            var t = NewTex(n, n);
            var px = new Color[n * n];
            float rad = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - rad) * (x + 0.5f - rad) + (y + 0.5f - rad) * (y + 0.5f - rad));
                    px[y * n + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(rad - 1.5f - d + 0.5f));
                }
            t.SetPixels(px);
            return Make(t);
        }

        /// <summary>
        /// 射程边：线宽按世界单位算，所以每个半径单独出图（按 0.05 取整缓存），大圆小圆的线一样细。
        /// 贴图外沿 = 圆外 OuterPad；圈内是白线 + 往里淡出的光晕，圈外一窄条深色投影，浅色地面上也压得住。
        /// </summary>
        public static Sprite RangeRim(float r)
        {
            float rq = Mathf.Max(0.3f, Mathf.Round(r * 20f) / 20f);
            return Get("rangerim" + rq.ToString("0.00"), () => RangeRimTex(rq));
        }

        public const float RangeRimPad = 0.08f;

        static Sprite RangeRimTex(float r)
        {
            int n = 512;
            var t = NewTex(n, n);
            var px = new Color[n * n];
            float half = n * 0.5f;
            float wpp = (r + RangeRimPad) / half;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - half, dy = y + 0.5f - half;
                    float e = r - Mathf.Sqrt(dx * dx + dy * dy) * wpp;
                    Color col;
                    if (e >= 0f)
                    {
                        float line = Mathf.Clamp01((0.028f - Mathf.Abs(e - 0.026f)) / wpp + 0.5f);
                        float glow = e < 0.4f ? 0.42f * (1f - e / 0.4f) * (1f - e / 0.4f) : 0f;
                        col = new Color(1f, 1f, 1f, Mathf.Max(line, glow));
                    }
                    else
                    {
                        float s = Mathf.Clamp01(1f + e / 0.07f);
                        col = new Color(0f, 0f, 0f, 0.38f * s * s);
                    }
                    px[y * n + x] = col;
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite RingTex(int n, float inner)
        {
            var t = NewTex(n, n);
            var px = new Color[n * n];
            float r = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01((1f - d) * r) * Mathf.Clamp01((d - inner) * r * 0.5f);
                    float wobble = 0.75f + 0.25f * Mathf.PerlinNoise(x * 0.08f, y * 0.08f);
                    px[y * n + x] = new Color(1, 1, 1, a * wobble);
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite InkBlob(int n, int seed)
        {
            var t = NewTex(n, n);
            var px = new Color[n * n];
            var rnd = new System.Random(seed);
            var blobs = new List<Vector3>();
            blobs.Add(new Vector3(n * 0.5f, n * 0.5f, n * 0.26f));
            for (int i = 0; i < 9; i++)
            {
                float ang = (float)rnd.NextDouble() * Mathf.PI * 2;
                float dist = n * (0.12f + 0.25f * (float)rnd.NextDouble());
                blobs.Add(new Vector3(n * 0.5f + Mathf.Cos(ang) * dist, n * 0.5f + Mathf.Sin(ang) * dist, n * (0.04f + 0.09f * (float)rnd.NextDouble())));
            }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float a = 0;
                    foreach (var b in blobs)
                    {
                        float d = Vector2.Distance(new Vector2(x, y), new Vector2(b.x, b.y));
                        a = Mathf.Max(a, Mathf.Clamp01((b.z - d) * 0.6f));
                    }
                    a *= 0.8f + 0.2f * Mathf.PerlinNoise(x * 0.15f + seed, y * 0.15f);
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite SlashTex()
        {
            int w = 128, h = 64;
            var t = NewTex(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)w;
                    float center = h * (0.25f + 0.5f * Mathf.Sin(u * Mathf.PI));
                    float thick = h * 0.22f * Mathf.Sin(u * Mathf.PI);
                    float d = Mathf.Abs(y - center);
                    float a = Mathf.Clamp01((thick - d) * 0.7f);
                    a *= 0.75f + 0.25f * Mathf.PerlinNoise(x * 0.2f, y * 0.2f);
                    px[y * w + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite StrokeTex()
        {
            int w = 64, h = 16;
            var t = NewTex(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = x / (float)(w - 1);
                    float thick = (h * 0.5f - 1) * Mathf.Lerp(0.35f, 1f, Mathf.Sin(Mathf.Clamp01(u) * Mathf.PI * 0.5f + 0.2f));
                    float d = Mathf.Abs(y + 0.5f - h * 0.5f);
                    float a = Mathf.Clamp01(thick - d) * Mathf.Clamp01(u * 8f) * Mathf.Clamp01((1 - u) * 6f);
                    px[y * w + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite HeartTex()
        {
            int n = 64;
            var t = NewTex(n, n);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float fx = (x + 0.5f) / n * 2.6f - 1.3f;
                    float fy = (y + 0.5f) / n * 2.6f - 1.4f;
                    float v = Mathf.Pow(fx * fx + fy * fy - 1, 3) - fx * fx * fy * fy * fy;
                    float a = Mathf.Clamp01(-v * 18f);
                    px[y * n + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px);
            return Make(t);
        }

        /// <summary>大招用的墨旋风：三道粗细不匀的弧形笔触绕成一圈。</summary>
        static Sprite SwirlTex()
        {
            int n = 192;
            var t = NewTex(n, n);
            var px = new Color[n * n];
            float c = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = x + 0.5f - c, dy = y + 0.5f - c;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / c;
                    float ang = Mathf.Atan2(dy, dx);
                    float u = Mathf.Repeat(ang / (Mathf.PI * 2f) * 3f, 1f);
                    float radius = 0.62f + 0.3f * u;
                    float thick = 0.13f * Mathf.Sin(u * Mathf.PI) + 0.01f;
                    float a = Mathf.Clamp01((thick - Mathf.Abs(d - radius)) * c * 0.5f);
                    a *= 0.7f + 0.3f * Mathf.PerlinNoise(x * 0.09f, y * 0.09f);
                    px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(a));
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite ArrowTex()
        {
            int w = 96, h = 24;
            var t = NewTex(w, h);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float cy = Mathf.Abs(y + 0.5f - h * 0.5f);
                    float a = 0;
                    if (x < 76 && cy < 1.6f) a = 1;
                    if (x >= 70) a = Mathf.Max(a, Mathf.Clamp01((h * 0.5f - 1) * (1f - (x - 70) / 26f) - cy + 0.5f));
                    if (x < 16 && cy < 2f + (16 - x) * 0.4f && cy > 1.2f) a = Mathf.Max(a, 0.75f);
                    px[y * w + x] = new Color(1, 1, 1, a);
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite DashedTex()
        {
            int n = 64;
            var t = NewTex(n, n);
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, n - 1 - x), Mathf.Min(y, n - 1 - y));
                    int along = (x + y) % 12;
                    px[y * n + x] = new Color(1, 1, 1, edge < 4 && along < 7 ? 1 : 0);
                }
            t.SetPixels(px);
            return Make(t);
        }

        static Sprite PaperTex()
        {
            int n = 256;
            var t = NewTex(n, n);
            t.wrapMode = TextureWrapMode.Repeat;
            var px = new Color[n * n];
            var rnd = new System.Random(3);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float noise = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.5f + Mathf.PerlinNoise(x * 0.2f, y * 0.2f) * 0.3f;
                    float fiber = rnd.NextDouble() < 0.004 ? 0.25f : 0f;
                    float v = 1f - noise * 0.12f - fiber;
                    px[y * n + x] = new Color(v, v * 0.985f, v * 0.96f, 1);
                }
            t.SetPixels(px);
            return Make(t);
        }
    }

    /// <summary>世界空间的绘制小工具：圆角块、文字。</summary>
    public static class Gfx
    {
        public static SpriteRenderer Sprite(Transform parent, Sprite s, Color c, Vector2 pos, Vector2 size, int order, string name = "sp")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.color = c;
            sr.sortingOrder = order;
            if (s.border != Vector4.zero)
            {
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = size;
            }
            else go.transform.localScale = new Vector3(size.x, size.y, 1);
            return sr;
        }

        public static void Resize(SpriteRenderer sr, Vector2 size)
        {
            if (sr.drawMode == SpriteDrawMode.Sliced) sr.size = size;
            else sr.transform.localScale = new Vector3(size.x, size.y, 1);
        }

        public const int FontPx = 96;

        public static TextMesh Text(Transform parent, string s, Font font, float height, Color c, Vector2 pos, int order,
            TextAnchor anchor = TextAnchor.MiddleCenter, string name = "tx")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = font;
            tm.fontSize = FontPx;
            tm.characterSize = height * 10f / FontPx;
            tm.anchor = anchor;
            tm.alignment = anchor == TextAnchor.MiddleLeft || anchor == TextAnchor.UpperLeft || anchor == TextAnchor.LowerLeft ? TextAlignment.Left
                : anchor == TextAnchor.MiddleRight || anchor == TextAnchor.UpperRight || anchor == TextAnchor.LowerRight ? TextAlignment.Right : TextAlignment.Center;
            tm.color = c;
            tm.text = s;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = font.material;
            mr.sortingOrder = order;
            return tm;
        }

        public static void SetHeight(TextMesh tm, float height) => tm.characterSize = height * 10f / FontPx;
    }
}
