using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>
    /// 开局的官军探马：沿出兵路线走一整圈，边走边留脚印、喊话，到宋江跟前一晃就撤。
    /// 纯表现，不进模拟；跟着对局时间走，新手引导冻结时它也停下。
    /// </summary>
    public sealed partial class BattleView
    {
        sealed class Scout
        {
            public Transform Root;
            public TextMesh Glyph;
            public SpriteRenderer Bubble;
            public TextMesh BubbleText;
            public float LastPrint;
            public int Line = -1;
            public bool Gone;
        }

        sealed class Print
        {
            public SpriteRenderer Sr;
            public float Age;
        }

        static readonly string[] ScoutLines = { "探路！", "这边走！", "宋江在前头！" };
        const float PrintLife = 3.2f;
        const float PrintStep = 0.42f;

        readonly Scout[] _scouts = new Scout[2];
        readonly List<Print> _prints = new List<Print>();

        void BuildScouts()
        {
            for (int s = 0; s < 2; s++)
            {
                var sc = new Scout { Root = new GameObject("scout" + s).transform };
                sc.Root.SetParent(_root, false);
                Gfx.Sprite(sc.Root, Art.SoftDot, Pal.Shadow, new Vector2(0, -0.3f), new Vector2(0.6f, 0.18f), 29);
                Gfx.Sprite(sc.Root, Art.Circle, Pal.RedDeep, Vector2.zero, Vector2.one * 0.66f, 30, "rim");
                Gfx.Sprite(sc.Root, Art.Circle, Pal.Paper, Vector2.zero, Vector2.one * 0.58f, 31, "disc");
                Gfx.Sprite(sc.Root, Art.Img("map_banner"), Color.white, new Vector2(0.26f, 0.34f), Vector2.one * 0.36f, 32, "flag");
                sc.Glyph = Gfx.Text(sc.Root, "探", Fonts.Brush, 0.5f, Pal.RedDeep, new Vector2(0, 0.02f), 33);
                sc.Bubble = Gfx.Sprite(sc.Root, Art.Rounded, Color.white, new Vector2(0, 0.72f), new Vector2(1.5f, 0.46f), 70, "bubble");
                sc.BubbleText = Gfx.Text(sc.Bubble.transform, "", Fonts.Kai, 0.26f, Pal.Ink, Vector2.zero, 71);
                sc.Bubble.enabled = false;
                sc.BubbleText.text = "";
                sc.Root.localPosition = World(s, M.Sides[s].Board.PosAt(0f));
                sc.Root.localScale = Vector3.zero;
                _scouts[s] = sc;
            }
        }

        void TickScouts(float dt)
        {
            for (int i = _prints.Count - 1; i >= 0; i--)
            {
                var p = _prints[i];
                p.Age += dt;
                if (p.Sr == null || p.Age >= PrintLife)
                {
                    if (p.Sr != null) Destroy(p.Sr.gameObject);
                    _prints.RemoveAt(i);
                    continue;
                }
                float k = p.Age / PrintLife;
                p.Sr.color = Pal.Ink.A(0.6f * (1f - k * k));
            }

            for (int s = 0; s < 2; s++)
            {
                var sc = _scouts[s];
                if (sc == null || sc.Gone) continue;
                if (M.Wave > 0 || M.Sides[s].Dead)
                {
                    Retire(sc);
                    continue;
                }

                var board = M.Sides[s].Board;
                float t = M.Time - Tuning.ScoutStart;
                if (t < 0f) continue;
                float walk = Mathf.Clamp01(t / Tuning.ScoutWalk);
                float dist = walk * board.PathLength;
                var pos = World(s, board.PosAt(dist));
                float hop = walk < 1f ? Mathf.Abs(Mathf.Sin(Time.time * 11f + s)) * 0.07f : 0f;
                sc.Root.localPosition = pos + new Vector2(0, hop);
                if (sc.Root.localScale.x < 1f)
                    sc.Root.localScale = Vector3.one * Mathf.MoveTowards(sc.Root.localScale.x, 1f, dt * 5f);

                if (walk < 1f && dist - sc.LastPrint >= PrintStep)
                {
                    sc.LastPrint = dist;
                    var sr = Gfx.Sprite(_root, Art.Circle, Pal.Ink.A(0.6f), pos + new Vector2(0, -0.22f), new Vector2(0.16f, 0.11f), 11, "print");
                    _prints.Add(new Print { Sr = sr });
                }

                // 起步、半路、到宋江跟前各喊一句；只在自己这边冒泡，对面太小看不清。
                int line = walk >= 1f ? 2 : walk >= 0.45f ? 1 : t < 1.6f ? 0 : -1;
                if (s == 0 && line >= 0 && line != sc.Line)
                {
                    sc.Line = line;
                    sc.BubbleText.text = ScoutLines[line];
                    sc.Bubble.size = new Vector2(0.4f + ScoutLines[line].Length * 0.27f, 0.46f);
                    sc.Bubble.enabled = true;
                    Tween.Pop(sc.Bubble.transform, 0.25f);
                }
                if (s == 0 && sc.Bubble.enabled && line < 0) { sc.Bubble.enabled = false; sc.BubbleText.text = ""; }
                if (sc.Bubble.enabled)
                {
                    float half = sc.Bubble.size.x * 0.5f, x0 = sc.Root.localPosition.x;
                    sc.Bubble.transform.localPosition = new Vector2(Mathf.Clamp(x0, -3.94f + half, 3.94f - half) - x0, 0.72f);
                }

                if (walk >= 1f && t >= Tuning.ScoutWalk + 1.4f) Retire(sc);
            }
        }

        void Retire(Scout sc)
        {
            sc.Gone = true;
            var root = sc.Root;
            if (root == null) return;
            var start = root.localScale.x;
            Tween.Run(root, 0.3f, k => { if (root) root.localScale = Vector3.one * Mathf.Lerp(start, 0f, k); },
                () => { if (root) Destroy(root.gameObject); });
        }
    }
}
