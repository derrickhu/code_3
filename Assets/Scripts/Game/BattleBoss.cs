using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>头目绝技的表现：喊招时头顶盖一枚红印，落到棋盘上的乌云、封条、铁链、妖雨一直挂到失效。</summary>
    public sealed partial class BattleView
    {
        sealed class HexCell
        {
            public Transform Fog, Seal;
            public SpriteRenderer[] FogSr;
            public float[] FogBase;
            public SpriteRenderer[] SealSr;
            public TextMesh SealText;
            public float FogA, SealAge = -1f;
        }

        readonly HexCell[][,] _hexCells = { new HexCell[Board.Cols, Board.Rows], new HexCell[Board.Cols, Board.Rows] };
        readonly SpriteRenderer[] _rainTint = new SpriteRenderer[2];
        readonly float[] _rainAcc = new float[2];
        static readonly Color FogInk = Pal.Hex("1E1B24");
        static readonly Color SealPaper = Pal.Hex("F2DFA8");

        HexCell NewHexCell()
        {
            var h = new HexCell();
            h.Fog = new GameObject("fog").transform;
            h.Fog.SetParent(_fxRoot, false);
            h.FogSr = new[]
            {
                Gfx.Sprite(h.Fog, Art.Blob, FogInk, Vector2.zero, Vector2.one * 1.55f, 38),
                Gfx.Sprite(h.Fog, Art.Img("fx_dust"), FogInk, new Vector2(0.15f, 0.1f), Vector2.one * 1.5f, 39),
                Gfx.Sprite(h.Fog, Art.Blob2, Pal.Hex("3A3346"), new Vector2(-0.2f, -0.1f), Vector2.one * 1.2f, 39),
            };
            h.FogBase = new[] { 0.78f, 0.6f, 0.55f };
            foreach (var sr in h.FogSr) sr.transform.localRotation = Quaternion.Euler(0, 0, Rnd(0, 360));
            h.Fog.gameObject.SetActive(false);

            h.Seal = new GameObject("seal").transform;
            h.Seal.SetParent(_fxRoot, false);
            var a = Gfx.Sprite(h.Seal, Art.White, SealPaper, Vector2.zero, new Vector2(1.08f, 0.28f), 26);
            a.transform.localRotation = Quaternion.Euler(0, 0, 32f);
            var b = Gfx.Sprite(h.Seal, Art.White, SealPaper, Vector2.zero, new Vector2(1.08f, 0.28f), 26);
            b.transform.localRotation = Quaternion.Euler(0, 0, -32f);
            var ring = Gfx.Sprite(h.Seal, Art.Rounded, Pal.Red, Vector2.zero, new Vector2(0.46f, 0.46f), 27);
            h.SealSr = new[] { a, b, ring };
            h.SealText = Gfx.Text(h.Seal, "封", Fonts.Brush, 0.34f, Pal.Paper, Vector2.zero, 28);
            h.Seal.gameObject.SetActive(false);
            return h;
        }

        /// <summary>每帧把乌云、封条、妖雨和棋盘状态对齐。</summary>
        void TickBossHex(float dt)
        {
            for (int s = 0; s < 2; s++)
            {
                var side = M.Sides[s];
                var b = side.Board;
                for (int c = 0; c < Board.Cols; c++)
                    for (int r = 0; r < Board.Rows; r++)
                    {
                        float fog = b.Fog[c, r], seal = b.Seal[c, r];
                        var h = _hexCells[s][c, r];
                        if (h == null)
                        {
                            if (fog <= 0 && seal <= 0) continue;
                            h = _hexCells[s][c, r] = NewHexCell();
                        }
                        var at = CellWorld(s, c, r);

                        float want = fog > 0 ? Mathf.Clamp01(fog / 0.7f) : 0f;
                        h.FogA = Mathf.MoveTowards(h.FogA, want, dt * 3.5f);
                        bool fogOn = h.FogA > 0.01f;
                        if (h.Fog.gameObject.activeSelf != fogOn) h.Fog.gameObject.SetActive(fogOn);
                        if (fogOn)
                        {
                            float t = Time.time + c * 0.7f + r * 1.3f;
                            h.Fog.localPosition = at + new Vector2(Mathf.Sin(t * 0.9f) * 0.08f, Mathf.Cos(t * 0.7f) * 0.06f);
                            h.Fog.localScale = Vector3.one * (0.85f + 0.15f * h.FogA + 0.04f * Mathf.Sin(t * 1.3f));
                            for (int i = 0; i < h.FogSr.Length; i++)
                            {
                                h.FogSr[i].color = h.FogSr[i].color.A(h.FogBase[i] * h.FogA);
                                h.FogSr[i].transform.Rotate(0, 0, dt * (i % 2 == 0 ? 12f : -9f));
                            }
                        }

                        bool sealOn = seal > 0;
                        if (h.Seal.gameObject.activeSelf != sealOn)
                        {
                            h.Seal.gameObject.SetActive(sealOn);
                            h.SealAge = sealOn ? 0f : -1f;
                        }
                        if (sealOn)
                        {
                            h.SealAge += dt;
                            h.Seal.localPosition = at;
                            float pop = h.SealAge < 0.25f ? Mathf.Lerp(1.8f, 1f, Tween.OutCubic(h.SealAge / 0.25f)) : 1f;
                            h.Seal.localScale = Vector3.one * pop;
                            float a = Mathf.Clamp01(seal / 0.5f) * Mathf.Clamp01(h.SealAge / 0.12f);
                            h.SealSr[0].color = SealPaper.A(0.95f * a);
                            h.SealSr[1].color = SealPaper.A(0.95f * a);
                            h.SealSr[2].color = Pal.Red.A(a);
                            h.SealText.color = Pal.Paper.A(a);
                        }
                    }

                bool rain = side.RainT > 0;
                if (rain && _rainTint[s] == null)
                    _rainTint[s] = Gfx.Sprite(_fxRoot, Art.White, Pal.Hex("1E2A3A"), BoardCenter(s), new Vector2(Board.Cols + 0.4f, Board.Rows + 0.3f), 35, "rainTint");
                if (_rainTint[s] != null)
                {
                    var tint = _rainTint[s];
                    tint.transform.localPosition = BoardCenter(s);
                    float want = rain ? Mathf.Clamp01(side.RainT / 0.5f) * 0.22f : 0f;
                    tint.color = tint.color.A(Mathf.MoveTowards(tint.color.a, want, dt));
                    tint.enabled = tint.color.a > 0.005f;
                }
                if (rain && dt > 0f)
                {
                    _rainAcc[s] += dt * (s == 0 ? 55f : 30f);
                    while (_rainAcc[s] >= 1f) { _rainAcc[s] -= 1f; RainDrop(s); }
                }
            }
        }

        void RainDrop(int side)
        {
            var c = BoardCenter(side);
            var start = c + new Vector2(Rnd(-5f, 3.5f), Rnd(1.6f, 3.2f));
            var fx = NewSpriteFx(Art.White, Pal.Hex("B9CFE0"), start, new Vector2(0.035f, Rnd(0.35f, 0.55f)), 46, Rnd(0.4f, 0.6f), (f, k) =>
            {
                f.Go.transform.localPosition = f.A + new Vector2(1.1f, -5f) * k;
                f.Sr.color = f.C.A(0.7f * Mathf.Sin(k * Mathf.PI));
            });
            fx.A = start;
            fx.Go.transform.localRotation = Quaternion.Euler(0, 0, 12f);
            if (UnityEngine.Random.value < 0.12f)
            {
                var land = start + new Vector2(1.1f, -5f) * Rnd(0.6f, 1f);
                After(0.4f, () => Ring(land, 0.35f, Pal.Hex("B9CFE0").A(0.7f), 0.3f, 34));
            }
        }

        /// <summary>头顶盖一枚红印：绝技名。</summary>
        void SkillStamp(Vector2 at, string name, float h)
        {
            float w = h * (name.Length + 0.8f);
            NewSpriteFx(Art.Rounded, Pal.Red, at, new Vector2(w, h * 1.5f), 48, 1.4f, (f, k) =>
            {
                float s = k < 0.12f ? Mathf.Lerp(1.8f, 1f, Tween.OutCubic(k / 0.12f)) : 1f;
                f.Sr.size = new Vector2(w, h * 1.5f) * s;
                f.Sr.color = Pal.Red.A(k < 0.8f ? 0.95f : (1f - k) * 4.75f);
            });
            TextFx(name, h, Pal.Paper, at, 49, 1.4f, (f, k) =>
            {
                float s = k < 0.12f ? Mathf.Lerp(1.8f, 1f, Tween.OutCubic(k / 0.12f)) : 1f;
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Tm.color = f.C.A(k < 0.8f ? 1f : (1f - k) * 5f);
            }, false);
        }

        /// <summary>头目喊招：红印 + 喊话 + 每门绝技自己的起手。</summary>
        void BossCastFx(SimEvent ev)
        {
            var pos = World(ev.Side, ev.P);
            bool mine = ev.Side == 0;
            var skill = (BossSkill)ev.B;
            if (_enemies.TryGetValue(ev.A, out var bv)) { bv.Flash(); bv.Swell(0.35f); }
            Impact(pos, 1.6f, Pal.RedDeep.A(0.85f), Pal.Red, 4);
            Ring(pos, 3.2f, Pal.Red, 0.5f, 46);
            SkillStamp(pos + new Vector2(0, 1.05f), BossDef.SkillName(skill), mine ? 0.34f : 0.26f);
            string shout = ev.Text ?? "";
            int colon = shout.IndexOf('：');
            if (colon >= 0) shout = shout.Substring(colon + 1);
            if (shout.Length > 0) FloatText(pos + new Vector2(0, -0.75f), shout, Pal.Red, mine ? 0.4f : 0.3f, 1.2f);
            if (mine)
            {
                Sfx.Play(Snd.Warn, 0.9f);
                _shake = Mathf.Max(_shake, 1.1f);
            }
            else Sfx.Play(Snd.Warn, 0.35f, 1.2f);

            switch (skill)
            {
                case BossSkill.Haste:
                    for (int i = 0; i < 12; i++)
                    {
                        float a = i * 30f * Mathf.Deg2Rad;
                        var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                        Streak(pos + dir * 0.5f, pos + dir * 3.2f, 0f, 0.1f, Pal.Red, Pal.GoldBright, 0.35f, 45, 0f);
                    }
                    Ring(pos, 6.8f, Pal.Red.A(0.8f), 0.6f, 44);
                    break;
                case BossSkill.Resist:
                    for (int i = 0; i < 3; i++)
                    {
                        int k = i;
                        After(i * 0.12f, () => Ink("fx_enso", Pal.GoldBright.A(0.9f), pos, 1.6f + k * 0.35f, 0.9f, Rnd(0, 360), 46, 1.4f, 220f, 0.5f));
                    }
                    Ink("ui_shield", Color.white.A(0.85f), pos + Vector2.up * 0.1f, 1.1f, 0.9f, 0, 47, 0.5f, 0f, 0.6f);
                    break;
                case BossSkill.Charge:
                    for (int i = 0; i < 8; i++)
                    {
                        int k = i;
                        After(i * 0.06f, () =>
                        {
                            if (!_enemies.TryGetValue(ev.A, out var v) || v.Root == null) return;
                            var p = (Vector2)v.Root.localPosition;
                            DustPuff(p + new Vector2(Rnd(-0.2f, 0.2f), -0.3f), 0.8f);
                            if (k % 2 == 0) Ink("fx_lines", Pal.Red.A(0.8f), p, 1.4f, 0.2f, Rnd(0, 360), 45, 0.6f);
                        });
                    }
                    break;
                case BossSkill.Rain:
                {
                    var c = BoardCenter(ev.Side);
                    for (int i = 0; i < 6; i++)
                        Ink("fx_dust", FogInk.A(0.75f), new Vector2(Left + 0.7f + i * 1.35f, c.y + 2.5f), 2.4f, 1.6f, Rnd(0, 360), 40, 0.4f, Rnd(-20, 20), 0.7f);
                    After(0.2f, () => UltDim(ev.Side, Color.white, 0.4f, 0.12f));
                    After(0.2f, () => Bolt(c + new Vector2(Rnd(-3f, 3f), Rnd(-1.5f, 1.5f)), 0f, mine, 0.8f));
                    break;
                }
                case BossSkill.Fog:
                case BossSkill.Daze:
                    UltDim(ev.Side, skill == BossSkill.Fog ? FogInk : Pal.Purple, 0.3f, 0.8f);
                    break;
            }
        }

        /// <summary>绝技落点：从头目身上飞出去，落到哪一格、哪一个字。</summary>
        void BossHexFx(SimEvent ev)
        {
            var at = World(ev.Side, ev.P);
            var from = World(ev.Side, ev.Q);
            if (_enemies.TryGetValue(ev.A, out var bv) && bv.Root != null) from = bv.Root.localPosition;
            bool mine = ev.Side == 0;
            var skill = (BossSkill)ev.B;
            switch (skill)
            {
                case BossSkill.Summon:
                    Ink("fx_crack", Pal.RedDeep.A(0.8f), at, 1.1f, 0.8f, Rnd(0, 360), 31, 0.5f, 0f, 0.5f);
                    DustPuff(at, 0.8f);
                    Ring(at, 1.4f, Pal.Red, 0.4f, 44);
                    break;
                case BossSkill.Daze:
                {
                    Streak(from, at, 0f, 0.12f, Pal.RedDeep, Pal.Purple, 0.4f, 47);
                    Lasso(from, at, Pal.Ink, 0.02f);
                    After(0.18f, () =>
                    {
                        Impact(at, 1.2f, Pal.Purple.A(0.8f), Pal.Red, 3);
                        Ring(at, 1.8f, Pal.Purple, 0.4f, 46);
                        TextFx("定", 0.8f, Pal.Red, at, 49, 0.8f, (f, k) =>
                        {
                            float s = k < 0.15f ? Mathf.Lerp(2.2f, 1f, Tween.OutCubic(k / 0.15f)) : 1f;
                            f.Go.transform.localScale = new Vector3(s, s, 1);
                            f.Tm.color = f.C.A(k < 0.6f ? 1f : (1f - k) / 0.4f);
                        });
                        if (_pieces.TryGetValue(ev.Flag, out var pv)) pv.Punch(0.5f);
                        if (mine) { Sfx.Play(Snd.Lasso, 0.6f, 0.7f); _shake = Mathf.Max(_shake, 0.6f); }
                    });
                    break;
                }
                case BossSkill.Fog:
                {
                    for (int i = 0; i < 7; i++)
                    {
                        var dst = at + new Vector2(Rnd(-1.4f, 1.4f), Rnd(-0.8f, 0.8f));
                        var fx = Ink("fx_dust", FogInk.A(0.8f), from, Rnd(1.3f, 1.9f), 0.55f, Rnd(0, 360), 40, 0.4f, Rnd(-90, 90), 0.6f);
                        var step = fx.Step;
                        fx.Step = (f, k) => { step(f, k); f.Go.transform.localPosition = Vector2.Lerp(from, dst, Tween.OutCubic(Mathf.Min(1f, k * 1.6f))); };
                    }
                    After(0.35f, () => TextFx("乌云压阵", 0.46f, Pal.Paper, at, 49, 1.1f, (f, k) =>
                    {
                        f.Go.transform.localPosition = f.A + Vector2.up * k * 0.3f;
                        f.Tm.color = f.C.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
                    }));
                    if (mine) Sfx.Play(Snd.UltWind, 0.7f, 0.7f);
                    break;
                }
                case BossSkill.Seal:
                {
                    float spin = Rnd(-500f, 500f);
                    const float flight = 0.32f;
                    var paper = NewSpriteFx(Art.White, SealPaper, from, new Vector2(0.4f, 0.62f), 48, flight, null);
                    paper.Step = (f, k) =>
                    {
                        f.Go.transform.localPosition = Vector2.Lerp(from, at, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.9f;
                        f.Go.transform.localRotation = Quaternion.Euler(0, 0, spin * (1f - k));
                    };
                    TextFx("封", 0.3f, Pal.Red, from, 49, flight, (f, k) =>
                    {
                        f.Go.transform.localPosition = paper.Go.transform.localPosition;
                        f.Go.transform.localRotation = paper.Go.transform.localRotation;
                        f.Tm.color = f.C;
                    }, false);
                    After(flight, () =>
                    {
                        Ring(at, 1.3f, Pal.Red, 0.3f, 44);
                        Ink("fx_lines", Pal.Red.A(0.8f), at, 1.2f, 0.2f, Rnd(0, 360), 45, 0.6f);
                        if (mine) Sfx.Play(Snd.Tap, 0.8f, 0.6f);
                    });
                    break;
                }
                case BossSkill.Crush:
                    SkyDrop("wp_hammer", at, 1.7f, 0.05f, 0f, () =>
                    {
                        GroundSmash(at, 1.3f, Pal.Ink, mine);
                        Impact(at, 1.3f, Pal.Ink.A(0.9f), Pal.Red, 6);
                        FloatText(at + new Vector2(0, 0.7f), "降为" + ToCn((int)ev.Val) + "级", Pal.Red, 0.36f, 1.2f);
                        if (_pieces.TryGetValue(ev.Flag, out var pv)) pv.Punch(0.6f);
                        if (mine) Sfx.Play(Snd.Hammer, 0.8f, 0.8f);
                    }, 0.2f);
                    break;
                case BossSkill.Steal:
                {
                    int n = Mathf.Max(0, (int)ev.Val);
                    if (mine && n > 0)
                    {
                        var hud = BunsWorld;
                        for (int i = 0; i < Mathf.Min(8, n); i++)
                        {
                            var fx = NewSpriteFx(Art.Img("bun"), Color.white, hud, Vector2.one * 0.34f, 47, 0.55f + i * 0.05f, (f, k) =>
                            {
                                f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, k * k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 1.2f;
                                f.Sr.color = Color.white.A(k < 0.85f ? 1f : (1f - k) / 0.15f);
                            });
                            fx.A = hud;
                            fx.B = from;
                        }
                        Tween.Punch(_bunIcon, 0.2f, 0.3f, _bunIconScale);
                        FloatText(hud + new Vector2(0.5f, -0.5f), "被抢 -" + n, Pal.Red, 0.36f, 1.2f);
                        Sfx.Play(Snd.Leak, 0.5f, 1.3f);
                    }
                    else if (n > 0) FloatText(at + new Vector2(0, -0.6f), "抢走炊饼 " + n, Pal.RedDeep, 0.26f, 1f);
                    break;
                }
                case BossSkill.Devour:
                    if (ev.Flag != ev.A)
                    {
                        if (_enemies.TryGetValue(ev.Flag, out var vv) && vv.Root != null)
                        {
                            var start = (Vector2)vv.Root.localPosition;
                            TextFx(vv.GlyphText, vv.GlyphHeight, vv.GlyphColor, start, 47, 0.4f, (f, k) =>
                            {
                                float e = k * k;
                                f.Go.transform.localPosition = Vector2.Lerp(start, from, e);
                                f.Go.transform.localScale = Vector3.one * (1f - e * 0.8f);
                                f.Go.transform.localRotation = Quaternion.Euler(0, 0, k * 540f);
                                f.Tm.color = f.C.A(1f - e * 0.5f);
                            }, false);
                            Streak(start, from, 0f, 0.08f, Pal.RedDeep.A(0.7f), Pal.Red, 0.35f, 44, 0f);
                        }
                    }
                    else
                    {
                        After(0.4f, () =>
                        {
                            if (_enemies.TryGetValue(ev.A, out var v)) v.Swell(0.7f);
                            Ring(from, 2.6f, Pal.Willow, 0.45f, 46);
                            if (ev.Val >= 1f) FloatText(from + new Vector2(0, 1f), "回血 +" + Mathf.RoundToInt(ev.Val), Pal.Willow, mine ? 0.38f : 0.28f, 1.1f);
                            if (mine) Sfx.Play(Snd.Poison, 0.6f, 0.6f);
                        });
                    }
                    break;
                case BossSkill.Revive:
                {
                    var ghost = Pal.Hex("7FD6C2");
                    Ink("fx_fire", ghost.A(0.85f), at + Vector2.up * 0.15f, 1.1f, 0.8f, 0, 45, 0.3f, 0f, 0.4f);
                    Ink("fx_burst", Pal.Ink.A(0.5f), at, 1.1f, 0.6f, Rnd(0, 360), 31, 0.3f);
                    for (int i = 0; i < 4; i++)
                    {
                        var p = at + new Vector2(Rnd(-0.3f, 0.3f), 0);
                        var fx = NewSpriteFx(Art.SoftDot, ghost, p, Vector2.one * 0.2f, 46, 0.9f, (f, k) =>
                        {
                            f.Go.transform.localPosition = f.A + new Vector2(Mathf.Sin(k * 7f + f.F) * 0.12f, k * 1.2f);
                            f.Sr.color = f.C.A(0.8f * (1f - k));
                        });
                        fx.A = p;
                        fx.F = Rnd(0, 6);
                    }
                    FloatText(at + new Vector2(0, 0.5f), "魂", ghost, 0.3f, 0.8f);
                    break;
                }
            }
        }

        /// <summary>头目血量过半：一圈血光炸开，从此通体发红。</summary>
        void BossRageFx(SimEvent ev)
        {
            var pos = World(ev.Side, ev.P);
            bool mine = ev.Side == 0;
            if (_enemies.TryGetValue(ev.A, out var v)) { v.Flash(); v.Swell(0.8f); }
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                After(i * 0.12f, () => Ring(pos, 2.4f + k * 1.4f, Pal.Red, 0.5f, 46));
            }
            Impact(pos, 2.2f, Pal.RedDeep.A(0.9f), Pal.Red, 10);
            TextFx("狂暴！", mine ? 0.6f : 0.42f, Pal.Red, pos + new Vector2(0, 1.1f), 49, 1.2f, (f, k) =>
            {
                float s = k < 0.15f ? Mathf.Lerp(2f, 1f, Tween.OutCubic(k / 0.15f)) : 1f;
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Go.transform.localPosition = f.A + new Vector2(Mathf.Sin(k * 60f) * 0.03f * (1f - k), 0);
                f.Tm.color = f.C.A(k < 0.75f ? 1f : (1f - k) * 4f);
            });
            if (mine)
            {
                UltDim(0, Pal.RedDeep, 0.25f, 0.6f);
                Sfx.Play(Snd.Warn, 1f, 0.8f);
                _shake = Mathf.Max(_shake, 1.3f);
            }
        }

        /// <summary>截图验收：让我方场上的头目当场放一门绝技；rage 为真时顺带打到半血狂暴。</summary>
        public void ShowcaseBoss(BossSkill skill, bool rage = false)
        {
            var s = M.Player;
            Enemy boss = null;
            foreach (var e in s.Enemies) if (e.Boss != null && !e.Dead) boss = e;
            if (boss == null) boss = M.SpawnOn(s, EnemyKind.Boss, 3f);
            if (boss == null) return;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    s.Board.Fog[c, r] = 0f;
                    s.Board.Seal[c, r] = 0f;
                    if (s.Board.Pieces[c, r] != null) s.Board.Pieces[c, r].StunT = 0f;
                }
            s.RainT = 0f;
            boss.Boss.Skills = new[] { skill };
            boss.SkillIx = 0;
            boss.SkillT = 0f;
            if (rage) boss.Hp = boss.MaxHp * 0.45f;
            M.Step(0.02f);
            FlushEvents();
        }
    }
}
