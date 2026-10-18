using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>棋盘环境：草丛花石点缀、路边卵石、水面涟漪荷叶、出兵口战旗、飘落花瓣和云影。</summary>
    public sealed partial class BattleView
    {
        sealed class Sway
        {
            public Transform T;
            public float Phase, Amp, Speed;
            public Vector3 BaseScale;
            public bool Stretch;
        }

        sealed class Drifter
        {
            public SpriteRenderer Sr;
            public Vector2 Pos, Vel;
            public float Spin, Phase, Size;
        }

        readonly List<Sway> _sways = new List<Sway>();
        readonly List<Drifter> _drifters = new List<Drifter>();
        readonly List<Drifter> _clouds = new List<Drifter>();

        static uint CellHash(int side, int c, int r, int salt)
        {
            uint h = (uint)(side * 7919 + c * 104729 + r * 1299709 + salt * 15485863);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return h;
        }

        static float Hash01(int side, int c, int r, int salt) => (CellHash(side, c, r, salt) & 0xffff) / 65535f;

        void AddSway(Transform t, float amp, float speed, bool stretch = false)
        {
            _sways.Add(new Sway { T = t, Phase = Random.Range(0f, 6.28f), Amp = amp, Speed = speed, BaseScale = t.localScale, Stretch = stretch });
        }

        /// <summary>按格子类型撒点缀；用格子坐标做种子，每局同一张图摆放一致。</summary>
        void Decorate(Transform t, int side, Cell kind, int c, int r)
        {
            switch (kind)
            {
                case Cell.Grass:
                case Cell.Marsh:
                {
                    float roll = Hash01(side, c, r, 1);
                    var corner = new Vector2(Hash01(side, c, r, 2) < 0.5f ? -0.3f : 0.3f, Hash01(side, c, r, 3) < 0.5f ? -0.3f : 0.28f);
                    if (roll < 0.34f)
                    {
                        var g = Gfx.Sprite(t, Art.Img("map_grass"), Color.white, corner, Vector2.one * (0.3f + Hash01(side, c, r, 4) * 0.12f), 9, "tuft");
                        AddSway(g.transform, 6f, 1.6f);
                    }
                    else if (roll < 0.46f)
                    {
                        var f = Gfx.Sprite(t, Art.Img("map_flowers"), Color.white, corner, Vector2.one * 0.34f, 9, "flower");
                        AddSway(f.transform, 5f, 1.3f);
                    }
                    else if (roll < 0.54f)
                        Gfx.Sprite(t, Art.Img("map_pebbles"), Color.white, corner, Vector2.one * 0.26f, 9, "pebble");
                    else if (roll < 0.58f)
                        Gfx.Sprite(t, Art.Img("map_stump"), Color.white, corner, Vector2.one * 0.36f, 9, "stump");
                    if (kind == Cell.Marsh && Hash01(side, c, r, 5) < 0.6f)
                    {
                        var reed = Gfx.Sprite(t, Art.Img("map_reeds"), Color.white, -corner * 0.9f, Vector2.one * 0.42f, 9, "reed");
                        AddSway(reed.transform, 7f, 1.2f);
                    }
                    break;
                }
                case Cell.Water:
                    if (Hash01(side, c, r, 6) < 0.45f)
                    {
                        var p = new Vector2(Hash01(side, c, r, 7) - 0.5f, Hash01(side, c, r, 8) - 0.5f) * 0.5f;
                        var lotus = Gfx.Sprite(t, Art.Img("map_lotus"), Color.white, p, Vector2.one * 0.34f, 10, "lotus");
                        AddSway(lotus.transform, 4f, 0.8f);
                    }
                    break;
                case Cell.Rock:
                    Gfx.Sprite(t, Art.Img("map_boulder"), Color.white, new Vector2(0, 0.04f), Vector2.one * 0.86f, 10, "boulder");
                    break;
            }
        }

        /// <summary>路边一溜卵石，比一根线更像原作的石子路沿。</summary>
        void PathPebbles(Transform t, Vector2 delta, int side, int c, int r, int salt)
        {
            bool vertical = Mathf.Abs(delta.x) > 0.01f;
            var along = vertical ? Vector2.up : Vector2.right;
            for (int i = 0; i < 4; i++)
            {
                float u = -0.39f + i * 0.26f + (Hash01(side, c, r, salt * 10 + i) - 0.5f) * 0.08f;
                float s = 0.09f + Hash01(side, c, r, salt * 10 + i + 5) * 0.05f;
                var p = delta * 0.93f + along * u;
                Gfx.Sprite(t, Art.Circle, Pal.InkSoft.A(0.85f), p + new Vector2(0.01f, -0.015f), new Vector2(s * 1.25f, s), 10);
                Gfx.Sprite(t, Art.Circle, Pal.Hex("B9AE9C"), p, new Vector2(s * 1.05f, s * 0.8f), 10);
            }
        }

        /// <summary>出兵口：草丛后插一面迎风飘的官军战旗。</summary>
        void BuildSpawn(Transform parent, Vector2 sp)
        {
            var pole = new GameObject("banner").transform;
            pole.SetParent(parent, false);
            pole.localPosition = sp + new Vector2(0.28f, 0.42f);
            var flag = Gfx.Sprite(pole, Art.Img("map_banner"), Color.white, new Vector2(0.18f, 0f), Vector2.one * 0.62f, 12);
            Gfx.Text(flag.transform, "官", Fonts.Brush, 0.46f, Pal.Paper, new Vector2(0.1f, 0.06f), 13);
            AddSway(flag.transform, 5f, 3.2f, true);
            Gfx.Sprite(parent, Art.Img("bush"), Color.white, sp + new Vector2(0, 0.05f), Vector2.one * 1.2f, 13, "spawn");
        }

        /// <summary>城墙上插几面旗，两端摆木桶栅栏。</summary>
        void DecorateWall(float wallY, float wallH)
        {
            float h = Mathf.Min(0.62f, wallH * 0.85f);
            for (int i = 0; i < 2; i++)
            {
                var flag = Gfx.Sprite(_frameRoot, Art.Img("map_banner"), Color.white, new Vector2(i == 0 ? -2.75f : 2.75f, wallY + 0.02f), Vector2.one * h, 11, "wallflag");
                AddSway(flag.transform, 5f, 2.6f, true);
            }
            Gfx.Sprite(_frameRoot, Art.Img("map_barrel"), Color.white, new Vector2(Left + 0.32f, wallY + 0.02f), Vector2.one * 0.42f, 11);
            Gfx.Sprite(_frameRoot, Art.Img("map_fence"), Color.white, new Vector2(-Left - 0.42f, wallY + 0.02f), Vector2.one * 0.55f, 11);
        }

        void BuildAmbient()
        {
            for (int i = 0; i < 9; i++)
            {
                var d = new Drifter { Size = Random.Range(0.13f, 0.2f) };
                d.Sr = Gfx.Sprite(_root, Art.Img(i % 3 == 0 ? "map_leaf" : "map_petal"), Color.white.A(0.9f), Vector2.zero, Vector2.one * d.Size, 38, "drift");
                Respawn(d, true);
                _drifters.Add(d);
            }
            for (int i = 0; i < 2; i++)
            {
                var d = new Drifter { Size = Random.Range(3.5f, 5f) };
                d.Sr = Gfx.Sprite(_root, Art.SoftDot, new Color(0.1f, 0.12f, 0.1f, 0.09f), Vector2.zero, new Vector2(d.Size * 1.6f, d.Size), 12, "cloud");
                d.Pos = new Vector2(Random.Range(-6f, 6f), MyBottom + 2f + i * 6f);
                d.Vel = new Vector2(Random.Range(0.18f, 0.3f), 0);
                _clouds.Add(d);
            }
        }

        void Respawn(Drifter d, bool anywhere)
        {
            float top = OppBottom + Board.Rows;
            d.Pos = anywhere
                ? new Vector2(Random.Range(Left, -Left), Random.Range(MyBottom, top))
                : new Vector2(Random.Range(Left - 1f, -Left - 2f), top + Random.Range(0.2f, 1.2f));
            d.Vel = new Vector2(Random.Range(0.25f, 0.55f), -Random.Range(0.35f, 0.6f));
            d.Spin = Random.Range(-90f, 90f);
            d.Phase = Random.Range(0f, 6.28f);
        }

        void TickMap(float dt)
        {
            float time = Time.time;
            for (int i = _sways.Count - 1; i >= 0; i--)
            {
                var s = _sways[i];
                if (s.T == null) { _sways.RemoveAt(i); continue; }
                float w = Mathf.Sin(time * s.Speed + s.Phase);
                if (s.Stretch)
                {
                    s.T.localScale = new Vector3(s.BaseScale.x * (1f + w * 0.07f), s.BaseScale.y * (1f - w * 0.03f), 1);
                    s.T.localRotation = Quaternion.Euler(0, 0, w * s.Amp * 0.4f);
                }
                else s.T.localRotation = Quaternion.Euler(0, 0, w * s.Amp);
            }
            if (Paused) return;
            float bottom = MyBottom - 0.5f;
            foreach (var d in _drifters)
            {
                d.Pos += (d.Vel + new Vector2(Mathf.Sin(time * 1.3f + d.Phase) * 0.3f, 0)) * dt;
                if (d.Pos.y < bottom || d.Pos.x > -Left + 0.8f) Respawn(d, false);
                var t = d.Sr.transform;
                t.localPosition = d.Pos;
                t.localRotation = Quaternion.Euler(0, 0, time * d.Spin + d.Phase * 57f);
                float flip = Mathf.Sin(time * 2.2f + d.Phase);
                t.localScale = new Vector3(d.Size * (0.35f + 0.65f * Mathf.Abs(flip)), d.Size, 1);
            }
            foreach (var d in _clouds)
            {
                d.Pos += d.Vel * dt;
                if (d.Pos.x > 9f) d.Pos.x = -9f;
                d.Sr.transform.localPosition = d.Pos;
            }
        }
    }
}
