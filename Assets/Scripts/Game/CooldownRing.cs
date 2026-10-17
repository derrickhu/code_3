using UnityEngine;
using UnityEngine.Rendering;

namespace Rescue
{
    /// <summary>
    /// 圆钮上的冷却。从十二点顺时针转：大招是暖色扇形涨满（和聚义同一套涨满），
    /// 道具是暗色遮罩转完揭开图标。前沿有一道会呼吸的金边。
    /// </summary>
    public sealed class CooldownRing
    {
        public readonly Transform Root;

        const int Seg = 48;
        const int ArcN = 10;

        static readonly Color FillLo = Pal.Hex("8A5649");
        static readonly Color FillHi = Pal.Hex("E88862");
        static readonly Color Veil = new Color(0.07f, 0.05f, 0.04f, 0.76f);

        readonly Mesh _pie, _edge;
        readonly MeshRenderer _pieOn, _edgeOn;
        readonly Vector3[] _pieV = new Vector3[Seg + 2];
        readonly Color[] _pieC = new Color[Seg + 2];
        readonly int[] _pieT = new int[Seg * 3];
        readonly Vector3[] _arcV = new Vector3[(ArcN + 1) * 2];
        readonly Color[] _arcC = new Color[(ArcN + 1) * 2];
        readonly int[] _arcT = new int[ArcN * 6];
        readonly Vector2[] _uvPie, _uvArc;

        static Material _mat;

        public CooldownRing(Transform parent, float diameter, int order)
        {
            var go = new GameObject("cd");
            Root = go.transform;
            Root.SetParent(parent, false);
            Root.localScale = new Vector3(diameter, diameter, 1f);
            _uvPie = new Vector2[_pieV.Length];
            _uvArc = new Vector2[_arcV.Length];
            for (int i = 0; i < _uvPie.Length; i++) _uvPie[i] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < _uvArc.Length; i++) _uvArc[i] = new Vector2(0.5f, 0.5f);
            for (int i = 0; i < ArcN; i++)
            {
                int inner0 = i, inner1 = i + 1;
                int outer0 = ArcN + 1 + i, outer1 = ArcN + 2 + i;
                int o = i * 6;
                _arcT[o] = inner0; _arcT[o + 1] = outer0; _arcT[o + 2] = outer1;
                _arcT[o + 3] = inner0; _arcT[o + 4] = outer1; _arcT[o + 5] = inner1;
            }
            _pieOn = AddMesh(Root, "pie", order, out _pie, _pieV, _pieC, _pieT, _uvPie);
            _edgeOn = AddMesh(Root, "edge", order + 1, out _edge, _arcV, _arcC, _arcT, _uvArc);
            Root.gameObject.SetActive(false);
        }

        /// <summary>progress 0 刚进冷却，1 转满。扇形从十二点顺时针涨。</summary>
        public void ShowFill(float progress)
        {
            progress = Mathf.Clamp01(progress);
            Show();
            var c = Color.Lerp(FillLo, FillHi, progress);
            c.a = 0.96f;
            if (progress < 0.008f) _pieOn.enabled = false;
            else
            {
                _pieOn.enabled = true;
                Sector(0f, progress * 360f, c, true);
            }
            Arc(progress * 360f);
        }

        /// <summary>remain 1 整圈盖住，0 揭开。暗色是还没转完的那一截。</summary>
        public void ShowVeil(float remain)
        {
            remain = Mathf.Clamp01(remain);
            if (remain < 0.008f) { Hide(); return; }
            Show();
            _pieOn.enabled = true;
            float progress = 1f - remain;
            Sector(progress * 360f, remain * 360f, Veil, false);
            Arc(progress * 360f);
        }

        public void Hide() => Root.gameObject.SetActive(false);

        void Show()
        {
            Root.gameObject.SetActive(true);
            _edgeOn.enabled = true;
        }

        void Sector(float startDeg, float spanDeg, Color color, bool heatEnd)
        {
            int n = Mathf.Clamp(Mathf.CeilToInt(spanDeg / 360f * Seg), 1, Seg);
            _pieV[0] = Vector3.zero;
            var mid = color;
            mid.r *= 0.82f; mid.g *= 0.82f; mid.b *= 0.82f;
            _pieC[0] = mid;
            var hot = Color.Lerp(color, Color.white, 0.32f);
            hot.a = color.a;
            for (int i = 0; i <= n; i++)
            {
                float u = i / (float)n;
                var d = Dir(startDeg + spanDeg * u);
                _pieV[i + 1] = new Vector3(d.x * 0.5f, d.y * 0.5f, 0f);
                float k = heatEnd ? u * u : 0f;
                _pieC[i + 1] = Color.Lerp(color, hot, k);
            }
            for (int i = n + 2; i < _pieV.Length; i++)
            {
                _pieV[i] = Vector3.zero;
                _pieC[i] = Color.clear;
            }
            for (int i = 0; i < n; i++)
            {
                _pieT[i * 3] = 0;
                _pieT[i * 3 + 1] = i + 1;
                _pieT[i * 3 + 2] = i + 2;
            }
            for (int i = n * 3; i < _pieT.Length; i++) _pieT[i] = 0;
            _pie.vertices = _pieV;
            _pie.colors = _pieC;
            _pie.triangles = _pieT;
            _pie.RecalculateBounds();
        }

        void Arc(float atDeg)
        {
            float pulse = 0.78f + 0.22f * Mathf.Sin(Time.time * 7.5f);
            const float half = 17f;
            var hot = new Color(1f, 0.95f, 0.78f, pulse);
            var gold = new Color(0.89f, 0.66f, 0.22f, 0.4f * pulse);
            for (int i = 0; i <= ArcN; i++)
            {
                float u = i / (float)ArcN;
                var d = Dir(atDeg - half + 2f * half * u);
                float k = 1f - Mathf.Abs(u - 0.5f) * 2f;
                k *= k;
                _arcV[i] = new Vector3(d.x * 0.30f, d.y * 0.30f, 0f);
                _arcV[ArcN + 1 + i] = new Vector3(d.x * 0.5f, d.y * 0.5f, 0f);
                var c = Color.Lerp(gold, hot, k);
                c.a *= 0.35f + 0.65f * k;
                _arcC[i] = c;
                var rim = c;
                rim.a = Mathf.Min(1f, c.a + 0.2f);
                _arcC[ArcN + 1 + i] = rim;
            }
            _edge.vertices = _arcV;
            _edge.colors = _arcC;
            _edge.RecalculateBounds();
        }

        static Vector2 Dir(float degClockwiseFromTop)
        {
            float rad = (90f - degClockwiseFromTop) * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }

        static MeshRenderer AddMesh(Transform parent, string name, int order, out Mesh mesh,
            Vector3[] verts, Color[] cols, int[] tris, Vector2[] uv)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var mf = go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mesh = new Mesh { name = name };
            mesh.MarkDynamic();
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.uv = uv;
            mesh.triangles = tris;
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            mf.sharedMesh = mesh;
            mr.sharedMaterial = Mat();
            mr.sortingOrder = order;
            mr.shadowCastingMode = ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.lightProbeUsage = LightProbeUsage.Off;
            mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return mr;
        }

        static Material Mat()
        {
            if (_mat != null) return _mat;
            var shader = Shader.Find("Sprites/Default");
            _mat = shader != null ? new Material(shader) : new Material(Shader.Find("Unlit/Transparent"));
            _mat.mainTexture = Texture2D.whiteTexture;
            _mat.color = Color.white;
            return _mat;
        }
    }
}
