using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>好汉大招：每一招都铺满它打到的那片地方，一眼看得出是谁的绝技。</summary>
    public sealed partial class BattleView
    {
        static readonly Color Thunder = Pal.Hex("D9CCFF");
        static readonly Color Snow = Pal.Hex("EAF6FF");
        float _cutInUntil;
        int _volleyFrame, _volleyHero, _volleyN;

        static Snd UltSound(HeroDef d)
        {
            bool water = d.Look == Look.Water || d.Look == Look.WaterFire || d.Look == Look.Fork;
            switch (d.UltKind)
            {
                case UltKind.Combo:
                    return d.Look == Look.Fist ? Snd.Fist : d.Look == Look.Spear ? Snd.Spear : d.Look == Look.Sword ? Snd.Sword : Snd.Dao;
                case UltKind.Area:
                    if (water) return Snd.UltWater;
                    switch (d.Look)
                    {
                        case Look.Fire: return Snd.UltFire;
                        case Look.Spear: return Snd.UltWind;
                        case Look.Poison: return Snd.UltMagic;
                        case Look.Magic: return Snd.UltThunder;
                        case Look.Whip: return Snd.Horse;
                        case Look.Hammer: case Look.Staff: return Snd.UltSmash;
                        default: return Snd.UltWhirl;
                    }
                case UltKind.Snipe: return Snd.Bow;
                case UltKind.Blast: return d.WKind == WeaponKind.Cannon ? Snd.UltCannon : Snd.UltFire;
                case UltKind.Pin: return water ? Snd.UltWater : Snd.UltSmash;
                case UltKind.Lasso: return Snd.Lasso;
                case UltKind.SlowAll: return Snd.UltWind;
                case UltKind.HasteAll: case UltKind.Upgrade: return Snd.UltMagic;
                case UltKind.Buns: return Snd.Merge;
                case UltKind.Volley: return d.WKind == WeaponKind.Stone ? Snd.Stone : d.Look == Look.Dart ? Snd.Dart : Snd.Bow;
                case UltKind.Storm: return Snd.UltThunder;
                case UltKind.Heal: return Snd.UltHeal;
                case UltKind.Rally: case UltKind.Shield: return Snd.UltDrum;
                case UltKind.Steal: return Snd.Dart;
                default: return Snd.UltSmash;
            }
        }

        static Vector2 BoardCenter(int side) => World(side, new V2(Board.Cols * 0.5f, Board.Rows * 0.5f));

        Vector2 HeroWorld(SimEvent ev)
        {
            foreach (var h in M.Sides[ev.Side].Heroes)
                if (h.Id == ev.A) return World(ev.Side, h.Pos);
            return World(ev.Side, ev.P);
        }

        /// <summary>整块半场压暗一下，大招打出来的那一瞬间压住别的东西。</summary>
        void UltDim(int side, Color c, float alpha, float life)
        {
            NewSpriteFx(Art.White, c, BoardCenter(side), new Vector2(Board.Cols + 0.4f, Board.Rows + 0.3f), 36, life, (f, k) =>
            {
                float a = k < 0.12f ? k / 0.12f : 1f - (k - 0.12f) / 0.88f;
                f.Sr.color = f.C.A(alpha * a);
            });
        }

        Fx TextFx(string s, float h, Color c, Vector2 pos, int order, float life, Action<Fx, float> step, bool rim = true)
        {
            var fx = new Fx { Life = life, C = c, A = pos };
            fx.Tm = Gfx.Text(_fxRoot, s, Fonts.Brush, h, c, pos, order, TextAnchor.MiddleCenter, "utx");
            fx.Go = fx.Tm.gameObject;
            TextMesh outline = null;
            if (rim)
            {
                outline = Gfx.Text(fx.Go.transform, s, Fonts.Brush, h, Pal.Ink.A(0.8f), new Vector2(0.03f, -0.03f), order - 1);
                outline.transform.localScale = Vector3.one;
            }
            fx.Step = (f, k) =>
            {
                step(f, k);
                if (outline != null) outline.color = Pal.Ink.A(0.8f * f.Tm.color.a);
            };
            _fx.Add(fx);
            return fx;
        }

        /// <summary>我方放大招：一道墨幅横穿半场，写着好汉名与绝技名。挤在一起放时只出第一道。</summary>
        void UltCutIn(HeroDef d, string word)
        {
            if (Time.time < _cutInUntil)
            {
                FloatText(HeroWorldFallback(d) + new Vector2(0, 0.8f), word, Pal.RedDeep, 0.42f, 1f);
                return;
            }
            _cutInUntil = Time.time + 0.9f;
            float y = (World(0, new V2(0f, Board.Rows)).y + OppBottom) * 0.5f;
            const float life = 0.85f;
            float Slide(float k) => k < 0.14f ? Mathf.Lerp(-10f, 0f, Tween.OutCubic(k / 0.14f))
                : k < 0.8f ? Mathf.Lerp(0f, 0.3f, (k - 0.14f) / 0.66f)
                : Mathf.Lerp(0.3f, 10f, (k - 0.8f) / 0.2f);
            var band = NewSpriteFx(Art.White, Pal.Ink.A(0.86f), new Vector2(-10f, y), new Vector2(9.5f, 1.15f), 70, life, null);
            band.Step = (f, k) => f.Go.transform.localPosition = new Vector2(Slide(k), y);
            foreach (float dy in new[] { 0.6f, -0.6f })
            {
                var line = NewSpriteFx(Art.White, Pal.GoldBright, new Vector2(-10f, y + dy), new Vector2(9.5f, 0.05f), 71, life, null);
                line.Step = (f, k) => f.Go.transform.localPosition = new Vector2(Slide(Mathf.Max(0f, k - 0.02f)) * 1.05f, y + dy);
            }
            var tint = d.Disha ? Pal.Hex("A57BD1") : Pal.GoldBright;
            var icon = NewSpriteFx(Art.Img(HeroWeaponSprite(d)), Color.white, new Vector2(-10f, y), Vector2.one * 1.05f, 73, life, null);
            icon.Step = (f, k) =>
            {
                f.Go.transform.localPosition = new Vector2(Slide(k) - 3.1f, y);
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, -20f + Mathf.Sin(k * 9f) * 6f);
            };
            TextFx(d.Name, 0.62f, Pal.Paper, new Vector2(-10f, y), 73, life, (f, k) =>
            {
                f.Go.transform.localPosition = new Vector2(Slide(Mathf.Max(0f, k - 0.03f)) - 1.35f, y + 0.02f);
                f.Tm.color = f.C;
            }, false);
            TextFx(word, 0.56f, tint, new Vector2(-10f, y), 73, life, (f, k) =>
            {
                f.Go.transform.localPosition = new Vector2(Slide(Mathf.Max(0f, k - 0.06f)) + 1.45f, y + 0.02f);
                float s = k < 0.2f ? Mathf.Lerp(1.6f, 1f, Tween.OutCubic(k / 0.2f)) : 1f;
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Tm.color = f.C;
            }, false);
            UltDim(0, Pal.Ink, 0.28f, 0.7f);
        }

        Vector2 HeroWorldFallback(HeroDef d)
        {
            foreach (var h in M.Player.Heroes) if (h.Def == d) return World(0, h.Pos);
            return BoardCenter(0);
        }

        /// <summary>兵器从天而降，扎进地里停一下再淡掉。</summary>
        void SkyDrop(string img, Vector2 at, float size, float delay, float tilt, Action onLand, float fall = 0.2f)
        {
            After(delay, () =>
            {
                var top = at + new Vector2(-tilt * 0.06f, 3.4f);
                bool landed = false;
                NewSpriteFx(Art.Img(img), Color.white, top, Vector2.one * size, 47, fall + 0.5f, (f, k) =>
                {
                    float t = k * (fall + 0.5f);
                    var p = t < fall ? Vector2.Lerp(top, at, (t / fall) * (t / fall)) : at;
                    f.Go.transform.localPosition = p + Vector2.up * size * 0.32f;
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, -135f + tilt + (t >= fall ? Mathf.Sin((t - fall) * 50f) * 3f * (1f - k) : 0f));
                    f.Sr.color = Color.white.A(k < 0.75f ? 1f : (1f - k) * 4f);
                    if (!landed && t >= fall) { landed = true; onLand?.Invoke(); }
                });
                Streak(top, at, 0f, size * 0.12f, Steel.A(0.6f), Color.white, fall + 0.08f, 45, 0f);
            });
        }

        /// <summary>一道折线雷：天上劈下来，落地一团白光。</summary>
        void Bolt(Vector2 at, float delay, bool mine, float scale = 1f)
        {
            After(delay, () =>
            {
                var top = at + new Vector2(Rnd(-0.6f, 0.6f), 4f);
                var a = Vector2.Lerp(top, at, 0.35f) + new Vector2(Rnd(-0.45f, 0.45f), 0);
                var b = Vector2.Lerp(top, at, 0.7f) + new Vector2(Rnd(-0.35f, 0.35f), 0);
                Streak(top, a, 0.05f, 0.2f * scale, Thunder, Color.white, 0.2f, 47, 1.7f);
                Streak(a, b, 0.05f, 0.2f * scale, Thunder, Color.white, 0.2f, 47, 1.7f);
                Streak(b, at, 0f, 0.2f * scale, Thunder, Color.white, 0.2f, 47, 1.7f);
                NewSpriteFx(Art.SoftDot, Color.white, at, Vector2.one * 1.6f * scale, 46, 0.25f, (f, k) =>
                {
                    float s = Mathf.Lerp(0.6f, 1.8f, k) * scale;
                    f.Go.transform.localScale = new Vector3(s, s * 0.7f, 1);
                    f.Sr.color = Color.white.A(1f - k);
                });
                Ink("fx_crack", Pal.Ink.A(0.7f), at, 1.1f * scale, 0.9f, Rnd(0, 360), 31, 0.5f, 0f, 0.6f);
                Kick(at, Vector2.down, 0.2f);
            });
        }

        /// <summary>大招按绝招类型和兵器定制；返回 false 时走通用墨旋风。</summary>
        bool HeroUltFx(SimEvent ev)
        {
            var d = HeroDef.Get((HeroId)ev.B);
            var pos = World(ev.Side, ev.P);
            var hero = HeroWorld(ev);
            float r = Mathf.Clamp(ev.Val, 1f, 6f);
            bool mine = ev.Side == 0;
            var targets = new List<Vector2>(EnemiesWithin(ev.Side, ev.P, r, 14));
            bool water = d.Look == Look.Water || d.Look == Look.WaterFire || d.Look == Look.Fork;
            switch (d.UltKind)
            {
                case UltKind.Combo: ComboFx(ev, d, mine); return true;
                case UltKind.Area:
                    if (d.Look == Look.Fork) { SpikeFx(pos, r, targets, mine); return true; }
                    if (water) { TideFx(ev.Side, pos, r, targets, mine, d.Look == Look.WaterFire); return true; }
                    switch (d.Look)
                    {
                        case Look.Spear:
                            SpearBurstFx(pos, r, targets, mine);
                            if (d.Id == HeroId.LinChong) BlizzardFx(ev.Side, pos, r, targets);
                            else if (d.UltStun > 0) foreach (var t in targets) After(0.35f, () => StunStars(t));
                            return true;
                        case Look.Axe: TornadoFx(pos, targets, mine); return true;
                        case Look.Sword: SwordRainFx(pos, r, targets, mine); return true;
                        case Look.Poison: SleepFx(pos, r, targets); return true;
                        case Look.Fire: BlastFx(hero, pos, r, mine); return true;
                        case Look.Magic: StormFx(ev.Side, targets, mine, false); return true;
                        case Look.Whip: CavalryFx(pos, r, targets, mine); return true;
                        case Look.Hammer: case Look.Staff: QuakeFx(pos, r, targets, mine, HeroWeaponSprite(d)); return true;
                        default: SweepFx(pos, r, targets, mine, HeroWeaponSprite(d), d.UltStun > 0); return true;
                    }
                case UltKind.Blast:
                    BlastFx(hero, pos, Mathf.Max(r, 1.6f), mine);
                    return true;
                case UltKind.Pin:
                    PinFx(pos, d, mine, water);
                    return true;
                case UltKind.Lasso:
                {
                    var c = d.Look == Look.Lasso ? Pal.Red : Pal.Hex("4A4A52");
                    int n = Mathf.Min(Mathf.Max(1, d.UltN), targets.Count);
                    for (int i = 0; i < n; i++)
                    {
                        var t = targets[i];
                        Lasso(pos, t, c, i * 0.06f);
                        After(0.35f + i * 0.06f, () =>
                        {
                            Ink("fx_lasso", c, t, 1.1f, 0.7f, 0, 46, 1.4f, 200f, 0.5f);
                            Kick(t, (pos - t).normalized, 0.35f);
                            StunStars(t);
                        });
                    }
                    if (mine) UltDim(ev.Side, Pal.RedDeep, 0.18f, 0.6f);
                    return true;
                }
                case UltKind.Throw:
                    ThrowFx(pos, r, targets, mine);
                    return true;
                case UltKind.SlowAll:
                    WindFx(ev.Side, mine);
                    return true;
                case UltKind.HasteAll:
                    HasteFx(ev.Side, hero);
                    return true;
                case UltKind.Upgrade:
                    Streak(pos + new Vector2(0, 3f), pos, 0f, 0.5f, Pal.GoldBright, Color.white, 0.45f, 46);
                    Ink("ui_sparkle", Color.white, pos, 1.6f, 0.8f, 0, 47, 0.3f, 120f, 0.4f);
                    Ring(pos, 2.4f, Pal.GoldBright, 0.6f, 45);
                    return true;
                case UltKind.Volley:
                    VolleyRainFx(ev.Side, hero, pos, r, targets, d, mine);
                    return true;
                case UltKind.Snipe:
                    ArrowRainFx(ev.Side, hero, new List<Vector2>(EnemiesWithin(ev.Side, ev.P, 99f, 16)), mine);
                    return true;
                case UltKind.Storm:
                {
                    var all = new List<Vector2>(EnemiesWithin(ev.Side, ev.P, 99f, 16));
                    StormFx(ev.Side, all, mine, d.Id == HeroId.FanRui);
                    return true;
                }
                case UltKind.Heal:
                {
                    var goal = World(ev.Side, M.Sides[ev.Side].Board.Goal.Center);
                    Streak(hero, goal, 0f, 0.25f, Pal.Hex("6FBF4A"), Pal.Hex("D6F5B8"), 0.4f, 46);
                    for (int i = 0; i < 3; i++) { int k = i; After(i * 0.15f, () => Ring(goal, 1.4f + k * 0.8f, Pal.Willow, 0.5f, 46)); }
                    for (int i = 0; i < 16; i++)
                    {
                        var p = goal + new Vector2(Rnd(-0.9f, 0.9f), Rnd(-0.5f, 0.3f));
                        var fx = NewSpriteFx(Art.Circle, Pal.Hex("9FE07A"), p, Vector2.one * Rnd(0.1f, 0.2f), 47, Rnd(0.8f, 1.2f), (f, k) =>
                        {
                            f.Go.transform.localPosition = f.A + new Vector2(Mathf.Sin(k * 6f + f.F) * 0.15f, k * 1.6f);
                            f.Sr.color = f.C.A(1f - k);
                        });
                        fx.A = p;
                        fx.F = Rnd(0, 6);
                    }
                    return true;
                }
                case UltKind.Rally:
                case UltKind.Shield:
                    BannerFx(ev.Side, d.UltKind == UltKind.Rally, mine);
                    return true;
                case UltKind.Steal:
                {
                    var from = BoardCenter(1 - ev.Side);
                    for (int i = 0; i < 6; i++)
                    {
                        var s = from + new Vector2(Rnd(-1.5f, 1.5f), Rnd(-0.8f, 0.8f));
                        var fx = NewSpriteFx(Art.Img("bun"), Color.white, s, Vector2.one * 0.36f, 47, 0.6f + i * 0.04f, (f, k) =>
                        {
                            f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, k * k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.8f;
                        });
                        fx.A = s;
                        fx.B = hero;
                    }
                    Ink("fx_dust", Pal.InkSoft.A(0.6f), hero, 1.4f, 0.6f, Rnd(0, 360), 40, 0.3f, 90f, 0.4f);
                    return true;
                }
                case UltKind.Buns:
                    NewSpriteFx(Art.Img("bun"), Color.white, hero, Vector2.one * 0.4f, 47, 0.7f, (f, k) =>
                    {
                        float s = k < 0.3f ? Mathf.Lerp(0.4f, 1.3f, Tween.OutBack(k / 0.3f)) : 1.3f;
                        f.Go.transform.localScale = new Vector3(s, s, 1);
                        f.Go.transform.localPosition = hero + Vector2.up * k * 0.5f;
                        f.Sr.color = Color.white.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
                    });
                    Ink("ui_rays", Pal.GoldBright.A(0.7f), hero, 2f, 0.7f, 0, 46, 0.4f, 90f, 0.4f);
                    return true;
                case UltKind.Shovel:
                    if (ev.Val <= 0.6f)
                    {
                        Ink("ui_rays", Pal.Willow.A(0.6f), hero, 1.8f, 0.7f, 0, 46, 0.4f, 90f, 0.4f);
                        return true;
                    }
                    QuakeFx(pos, r, targets, mine, "shovel");
                    return true;
            }
            return false;
        }

        // ─────────────────────────── 单个绝技 ───────────────────────────

        /// <summary>神箭：先在好汉身上聚一口气，一支金箭拖着长光直贯目标。</summary>
        void SnipeShotFx(SimEvent ev)
        {
            var from = World(ev.Side, ev.P);
            var to = World(ev.Side, ev.Q);
            var n = (to - from).sqrMagnitude > 1e-4f ? (to - from).normalized : Vector2.up;
            bool mine = ev.Side == 0;
            LungeHero(ev.A, n);
            Ink("fx_enso", Pal.GoldBright, from, 2.2f, 0.3f, 0, 46, 1f, -200f, 0.2f).Step += (f, k) =>
                f.Go.transform.localScale = Vector3.one * Mathf.Lerp(2.2f, 0.4f, k);
            if (mine) Sfx.Play(Snd.Bow, 0.8f, 0.7f);
            After(0.22f, () =>
            {
                Streak(from, to, 1.2f, 0.42f, Pal.GoldBright, Color.white, 0.5f, 46);
                InkArrow(from, to, Pal.Red, Pal.GoldBright, 2.0f, 0.16f, () =>
                {
                    StuckArrow(to, n, Pal.Red, 1.6f);
                    Impact(to, 2.6f, Pal.RedDeep.A(0.9f), Pal.GoldBright, 10);
                    Ring(to, 4.2f, Pal.GoldBright, 0.55f, 46);
                    Ink("fx_crack", Pal.RedDeep.A(0.75f), to, 2.2f, 0.9f, Rnd(0, 360), 31, 0.5f, 0f, 0.5f);
                    TextFx("神箭", 0.7f, Pal.Red, to + new Vector2(0, 1f), 49, 0.9f, (f, k) =>
                    {
                        float s = k < 0.15f ? Mathf.Lerp(2f, 1f, k / 0.15f) : 1f;
                        f.Go.transform.localScale = new Vector3(s, s, 1);
                        f.Tm.color = f.C.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
                    });
                    Kick(to, n, 0.45f);
                    if (mine) { _shake = Mathf.Max(_shake, 1.2f); HitStop(0.12f); Sfx.Play(Snd.Crit, 0.8f); }
                });
                for (int i = 1; i <= 2; i++)
                {
                    var off = new Vector2(-n.y, n.x) * (i == 1 ? 0.35f : -0.35f);
                    After(i * 0.04f, () => InkArrow(from + off, to + off * 0.3f, Pal.GoldBright.A(0.6f), Color.white.A(0.4f), 1.2f, 0.16f, null));
                }
            });
            if (_frames.TryGetValue(ev.A, out var uf)) uf.Pulse();
        }

        /// <summary>飞刀 / 飞石大招里的每一发：兵器本身飞过去，带一道金光。同一帧连发的错开出手。</summary>
        void VolleyShotFx(SimEvent ev)
        {
            if (_volleyFrame != Time.frameCount || _volleyHero != ev.A) { _volleyFrame = Time.frameCount; _volleyHero = ev.A; _volleyN = 0; }
            float delay = _volleyN++ * 0.07f;
            var from = World(ev.Side, ev.P);
            var to = World(ev.Side, ev.Q);
            var d = HeroDef.Get((HeroId)ev.B);
            bool mine = ev.Side == 0;
            string img = HeroWeaponSprite(d);
            After(delay, () =>
            {
                var n = (to - from).sqrMagnitude > 1e-4f ? (to - from).normalized : Vector2.up;
                Streak(from, to, 0.2f, 0.16f, Pal.GoldBright.A(0.85f), Color.white, 0.22f, 45);
                HurlWeapon(img, from, to, 0.95f);
                After(0.16f, () =>
                {
                    Impact(to, 1.0f, Pal.Ink.A(0.85f), Pal.GoldBright, 3);
                    Kick(to, n, 0.25f);
                    if (mine) Sfx.Play(d.WKind == WeaponKind.Stone ? Snd.Stone : Snd.Dart, 0.45f, 1f + _volleyN * 0.03f);
                });
                LungeHero(ev.A, n);
            });
        }

        /// <summary>满天飞石 / 飞刀：除了真打的那几发，再从半空泼下一阵。</summary>
        void VolleyRainFx(int side, Vector2 hero, Vector2 pos, float r, List<Vector2> targets, HeroDef d, bool mine)
        {
            string img = HeroWeaponSprite(d);
            var c = BoardCenter(side);
            float w = Mathf.Min(r, 4f), h = Mathf.Min(r, 2.5f);
            UltDim(side, Pal.Ink, 0.22f, 1f);
            for (int i = 0; i < 30; i++)
            {
                bool onFoe = targets.Count > 0 && i % 2 == 0;
                var at = onFoe ? targets[i / 2 % targets.Count] + UnityEngine.Random.insideUnitCircle * 0.3f
                    : c + new Vector2(Rnd(-w, w), Rnd(-h, h));
                var from = hero + new Vector2(Rnd(-0.3f, 0.3f), Rnd(0.2f, 0.6f));
                After(0.08f + i * 0.025f, () =>
                {
                    Streak(from, at, 0f, 0.1f, Pal.GoldBright.A(0.6f), Color.white, 0.2f, 44, 0f);
                    HurlWeapon(img, from, at, onFoe ? 0.85f : 0.65f);
                    After(0.18f, () =>
                    {
                        Impact(at, onFoe ? 0.8f : 0.5f, Pal.Ink.A(0.7f), Pal.GoldBright, onFoe ? 2 : 1);
                        if (onFoe) Kick(at, (at - from).normalized, 0.12f);
                    });
                });
            }
            Ring(hero, 2.4f, Pal.GoldBright, 0.5f, 45);
            Ink("ui_rays", Pal.GoldBright.A(0.6f), hero, 2.4f, 0.9f, 0, 45, 0.4f, 160f, 0.4f);
            After(0.3f, () => TextFx(d.UltName, 0.6f, Pal.GoldBright, c + new Vector2(0, 0.5f), 49, 0.9f, (f, k) =>
            {
                float s = k < 0.15f ? Mathf.Lerp(1.9f, 1f, Tween.OutCubic(k / 0.15f)) : 1f;
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Tm.color = f.C.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
            }));
            if (mine) After(0.25f, () => _shake = Mathf.Max(_shake, 0.9f));
        }

        /// <summary>连打：好汉的影子从四面八方一刀刀穿过最前面那个敌人，最后一下重击。</summary>
        /// <summary>万箭齐发：天边一排箭影压过来，整个半场斜斜插满，敌人身上箭箭见红。</summary>
        void ArrowRainFx(int side, Vector2 hero, List<Vector2> targets, bool mine)
        {
            var c = BoardCenter(side);
            float w = Board.Cols * 0.5f, h = Board.Rows * 0.5f;
            var slant = new Vector2(-1.1f, 3.2f);
            UltDim(side, Pal.Ink, 0.32f, 1.3f);
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                After(i * 0.06f, () => InkArrow(hero, hero + new Vector2(0.9f - k * 0.9f, 3.4f), Pal.GoldBright, Color.white, 1.2f, 0.18f, null));
            }
            const int count = 44;
            for (int i = 0; i < count; i++)
            {
                int idx = i;
                bool onFoe = targets.Count > 0 && i % 3 == 0;
                var at = onFoe ? targets[i / 3 % targets.Count] + UnityEngine.Random.insideUnitCircle * 0.25f
                    : c + new Vector2(Rnd(-w, w), Rnd(-h, h));
                var from = at + slant * Rnd(0.9f, 1.15f);
                var n = (at - from).normalized;
                After(0.25f + i * 0.02f + Rnd(0f, 0.08f), () =>
                {
                    InkArrow(from, at, Pal.Ink, onFoe ? Pal.GoldBright : Pal.Paper.A(0.6f), onFoe ? 1.05f : 0.8f, 0.14f, () =>
                    {
                        StuckArrow(at, n, onFoe ? Pal.Red : Pal.Ink, onFoe ? 0.9f : 0.7f);
                        if (onFoe)
                        {
                            Impact(at, 0.7f, Pal.RedDeep.A(0.8f), Pal.GoldBright, 2);
                            Kick(at, n, 0.15f);
                        }
                        else Ink("fx_dust", Pal.Ink.A(0.3f), at, 0.5f, 0.4f, Rnd(0, 360), 30, 0.5f, 0f, 0.5f);
                    });
                    if (mine && (idx & 7) == 0) Sfx.Play(Snd.Bow, 0.35f, Rnd(0.9f, 1.2f));
                });
            }
            After(0.4f, () => TextFx("万箭齐发", 0.75f, Pal.GoldBright, c + new Vector2(0, 0.4f), 49, 1.1f, (f, k) =>
            {
                float s = k < 0.12f ? Mathf.Lerp(2.2f, 1f, Tween.OutCubic(k / 0.12f)) : 1f + (k - 0.12f) * 0.08f;
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Tm.color = f.C.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
            }));
            if (mine) After(0.3f, () => _shake = Mathf.Max(_shake, 1f));
        }

        void ComboFx(SimEvent ev, HeroDef d, bool mine)
        {
            Enemy front = null;
            foreach (var e in M.Sides[ev.Side].Enemies)
                if (!e.Dead && !e.InBrush && (front == null || e.Progress > front.Progress)) front = e;
            var at = front != null ? World(ev.Side, front.Pos) : World(ev.Side, ev.P);
            int hits = Mathf.Clamp(d.UltN, 4, 18);
            bool fist = d.Look == Look.Fist;
            string img = HeroWeaponSprite(d);
            var hitSnd = fist ? Snd.Fist : d.Look == Look.Sword ? Snd.Sword : d.Look == Look.Spear ? Snd.Spear : Snd.Dao;
            float step = Mathf.Clamp(0.9f / hits, 0.045f, 0.09f);
            Ink("fx_enso", Pal.Ink.A(0.4f), at, 3.2f, hits * step + 0.4f, 0, 40, 0.8f, 0f, 0.8f);
            for (int i = 0; i < hits; i++)
            {
                int idx = i;
                bool last = i == hits - 1;
                After(i * step, () =>
                {
                    float a = idx * 137.5f * Mathf.Deg2Rad;
                    var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                    var p0 = at - dir * 1.8f;
                    var p1 = at + dir * 1.8f;
                    if (!last)
                    {
                        if (fist)
                        {
                            HurlWeapon(img, p0, at, 0.75f);
                            After(0.1f, () => Impact(at + UnityEngine.Random.insideUnitCircle * 0.2f, 0.75f, Pal.Ink.A(0.85f), Pal.GoldBright, 1));
                        }
                        else
                        {
                            Streak(p0, p1, 0f, 0.13f, Pal.Ink, Steel, 0.18f, 46);
                            var ghost = NewSpriteFx(Art.Img(img), Color.white.A(0.75f), p0, Vector2.one * 1.0f, 47, 0.16f, (f, k) =>
                            {
                                f.Go.transform.localPosition = Vector2.Lerp(p0, p1, k);
                                f.Sr.color = Color.white.A(0.75f * (1f - k));
                            });
                            ghost.Go.transform.localRotation = Quaternion.Euler(0, 0, Ang(dir) - 45f);
                            After(0.06f, () => Impact(at, 0.7f, Pal.Ink.A(0.8f), Steel, 1));
                        }
                        Kick(at, dir, 0.12f);
                        if (mine && idx % 2 == 0) Sfx.Play(hitSnd, 0.35f, 1.05f + idx * 0.02f);
                        if (idx > 0 && idx % 4 == 3)
                            TextFx((idx + 1) + "连", 0.34f + idx * 0.012f, Pal.GoldBright, at + new Vector2(0.7f, 0.7f), 49, 0.35f, (f, k) =>
                            {
                                f.Go.transform.localPosition = f.A + Vector2.up * k * 0.2f;
                                f.Tm.color = f.C.A(1f - k);
                            });
                    }
                    else
                    {
                        Streak(at + new Vector2(0, 2.6f), at, 0f, 0.4f, Pal.Ink, Pal.GoldBright, 0.3f, 47);
                        Impact(at, 2.4f, Pal.Ink.A(0.9f), Pal.GoldBright, 12);
                        Ring(at, 3.6f, Pal.GoldBright, 0.5f, 46);
                        Ink("fx_crack", Pal.Ink.A(0.85f), at, 2f, 0.9f, Rnd(0, 360), 31, 0.5f, 0f, 0.6f);
                        TextFx(hits + "连击！", 0.6f, Pal.Red, at + new Vector2(0, 1.1f), 49, 0.9f, (f, k) =>
                        {
                            float s = k < 0.15f ? Mathf.Lerp(1.8f, 1f, k / 0.15f) : 1f;
                            f.Go.transform.localScale = new Vector3(s, s, 1);
                            f.Tm.color = f.C.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
                        });
                        Kick(at, Vector2.up, 0.4f);
                        if (mine) { Sfx.Play(Snd.Crit, 0.8f, 0.85f); _shake = Mathf.Max(_shake, 1.1f); HitStop(0.1f); }
                    }
                });
            }
        }

        /// <summary>枪林：一圈枪芒从好汉身上同时扎出去，扎中的再补一记。</summary>
        void SpearBurstFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            for (int i = 0; i < 14; i++)
            {
                float a = (i * 360f / 14f + Rnd(-8f, 8f)) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                After(i * 0.02f, () => Streak(pos + dir * 0.4f, pos + dir * r, 0.2f, 0.12f, Steel, Pal.GoldBright, 0.3f, 45));
            }
            foreach (var t in targets)
            {
                var at = t;
                After(0.12f, () =>
                {
                    var dir = (at - pos).sqrMagnitude > 1e-3f ? (at - pos).normalized : Vector2.up;
                    ThrustWeapon("wp_spear", at - dir * 1.3f, at, dir, 1.4f);
                    Impact(at, 1.0f, Pal.Ink.A(0.85f), Pal.GoldBright, 2);
                    Kick(at, dir, 0.3f);
                });
            }
            Ring(pos, r * 2f, Steel, 0.4f, 44);
            if (mine) { _shake = Mathf.Max(_shake, 0.9f); HitStop(0.05f); }
        }

        /// <summary>风雪山神庙：整片地方刮起大雪，打中的敌人冻成冰坨。</summary>
        void BlizzardFx(int side, Vector2 pos, float r, List<Vector2> targets)
        {
            UltDim(side, Pal.Hex("6FA3C8"), 0.3f, 1.4f);
            for (int i = 0; i < 70; i++)
            {
                var start = pos + new Vector2(Rnd(-r - 1.5f, r), Rnd(-r * 0.5f, r + 1f));
                var end = start + new Vector2(Rnd(1.2f, 2.2f), -Rnd(1.5f, 2.6f));
                After(Rnd(0f, 0.6f), () =>
                {
                    var fx = NewSpriteFx(Art.Circle, Snow, start, Vector2.one * Rnd(0.07f, 0.17f), 47, Rnd(0.7f, 1.1f), (f, k) =>
                    {
                        f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, k) + new Vector2(Mathf.Sin(k * 8f + f.F) * 0.12f, 0);
                        f.Sr.color = Snow.A(k < 0.8f ? 0.95f : (1f - k) * 4.75f);
                    });
                    fx.A = start;
                    fx.B = end;
                    fx.F = Rnd(0, 6);
                });
            }
            Ink("fx_enso", Ice, pos, r * 2.2f, 1.0f, Rnd(0, 360), 44, 0.3f, -200f, 0.5f);
            foreach (var t in targets)
            {
                var at = t;
                After(0.3f, () =>
                {
                    var ice = NewSpriteFx(Art.Rounded, Ice.A(0.75f), at, Vector2.one * 0.75f, 37, 2.2f, (f, k) =>
                    {
                        float s = k < 0.08f ? Mathf.Lerp(0.2f, 0.75f, k / 0.08f) : 0.75f;
                        f.Go.transform.localScale = new Vector3(s, s, 1);
                        f.Sr.color = Ice.A(0.7f * (k < 0.85f ? 1f : (1f - k) / 0.15f));
                    });
                    ice.Go.transform.localRotation = Quaternion.Euler(0, 0, 45f);
                    Ink("fx_crack", Color.white.A(0.9f), at, 0.9f, 2.2f, Rnd(0, 360), 38, 0.5f, 0f, 0.85f);
                });
            }
        }

        void StunStars(Vector2 at)
        {
            for (int i = 0; i < 3; i++)
            {
                float a0 = i * 120f;
                NewSpriteFx(Art.Img("ui_sparkle"), Pal.GoldBright, at, Vector2.one * 0.24f, 47, 1.2f, (f, k) =>
                {
                    float a = (a0 + k * 720f) * Mathf.Deg2Rad;
                    f.Go.transform.localPosition = at + new Vector2(Mathf.Cos(a) * 0.35f, 0.45f + Mathf.Sin(a) * 0.1f);
                    f.Sr.color = Pal.GoldBright.A(k < 0.8f ? 1f : (1f - k) * 5f);
                });
            }
        }

        /// <summary>板斧旋风：一股墨色龙卷挟着两把板斧，从好汉身边一路卷过每个敌人。</summary>
        void TornadoFx(Vector2 start, List<Vector2> targets, bool mine)
        {
            var path = new List<Vector2> { start };
            var left = new List<Vector2>(targets);
            var cur = start;
            while (left.Count > 0 && path.Count < 9)
            {
                int bi = 0;
                for (int i = 1; i < left.Count; i++) if ((left[i] - cur).sqrMagnitude < (left[bi] - cur).sqrMagnitude) bi = i;
                cur = left[bi];
                left.RemoveAt(bi);
                path.Add(cur);
            }
            if (path.Count == 1) path.Add(start + Vector2.right * 1.5f);
            float life = 0.35f + 0.16f * path.Count;
            Vector2 At(float k)
            {
                float x = k * (path.Count - 1);
                int i = Mathf.Min(path.Count - 2, Mathf.FloorToInt(x));
                return Vector2.Lerp(path[i], path[i + 1], x - i);
            }
            int hit = 0;
            for (int layer = 0; layer < 3; layer++)
            {
                int L = layer;
                float sz = 1.9f - L * 0.45f;
                NewSpriteFx(Art.Swirl, Pal.Ink.A(0.7f - L * 0.12f), start, Vector2.one * sz, 44 + L, life, (f, k) =>
                {
                    f.Go.transform.localPosition = At(k) + new Vector2(Mathf.Sin(k * 20f + L) * 0.08f, 0.15f + L * 0.45f);
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, -k * 2400f * (1f + L * 0.3f));
                    float a = k < 0.1f ? k / 0.1f : k > 0.85f ? (1f - k) / 0.15f : 1f;
                    f.Sr.color = f.C.A(f.C.a * a);
                    if (L == 0)
                    {
                        int reach = Mathf.FloorToInt(k * (path.Count - 1) + 0.01f);
                        while (hit < reach)
                        {
                            hit++;
                            var at = path[hit];
                            Crescent(at, Vector2.left, 1.3f, Pal.Ink, Steel, false, 0.22f, 46);
                            Crescent(at, Vector2.right, 1.3f, Pal.Ink, Steel, true, 0.22f, 46);
                            Impact(at, 1.1f, Pal.Ink.A(0.85f), Steel, 3);
                            Kick(at, Vector2.up, 0.3f);
                            if (mine) Sfx.Play(Snd.Axe, 0.45f, 0.95f + hit * 0.03f);
                        }
                    }
                });
            }
            for (int i = 0; i < 2; i++)
            {
                float a0 = i * 180f;
                NewSpriteFx(Art.Img("wp_axe"), Color.white, start, Vector2.one * 0.95f, 48, life, (f, k) =>
                {
                    float a = (a0 - k * 1800f) * Mathf.Deg2Rad;
                    f.Go.transform.localPosition = At(k) + new Vector2(Mathf.Cos(a) * 0.75f, 0.45f + Mathf.Sin(a) * 0.3f);
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, -k * 2600f);
                    f.Sr.color = Color.white.A(k > 0.85f ? (1f - k) / 0.15f : 1f);
                });
            }
            for (int i = 0; i < 8; i++)
            {
                float t = i / 8f * life;
                After(t, () => DustPuff(At(t / life) + new Vector2(Rnd(-0.3f, 0.3f), -0.3f), Rnd(0.5f, 0.8f)));
            }
            if (mine) _shake = Mathf.Max(_shake, 1f);
        }

        /// <summary>大刀横扫：一道比人还宽的刀光从好汉身前扫过整片，扫到的敌人身上各留一刀。</summary>
        void SweepFx(Vector2 pos, float r, List<Vector2> targets, bool mine, string img, bool stun)
        {
            Vector2 aim = Vector2.right;
            if (targets.Count > 0)
            {
                var sum = Vector2.zero;
                foreach (var t in targets) sum += t - pos;
                if (sum.sqrMagnitude > 1e-3f) aim = sum.normalized;
            }
            Crescent(pos, aim, r * 1.9f, Pal.Ink, Pal.GoldBright, false, 0.42f, 45);
            After(0.16f, () => Crescent(pos, aim, r * 1.6f, Pal.RedDeep, Color.white, true, 0.36f, 45));
            float baseAng = Ang(aim);
            NewSpriteFx(Art.Img(img), Color.white, pos, Vector2.one * 2.1f, 48, 0.5f, (f, k) =>
            {
                float e = Tween.OutCubic(Mathf.Min(1f, k * 1.4f));
                float a = (baseAng + Mathf.Lerp(80f, -100f, e)) * Mathf.Deg2Rad;
                f.Go.transform.localPosition = pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.45f;
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, a * Mathf.Rad2Deg - 45f);
                f.Sr.color = Color.white.A(k < 0.75f ? 1f : (1f - k) * 4f);
            });
            foreach (var t in targets)
            {
                var at = t;
                After(0.12f + Rnd(0f, 0.1f), () =>
                {
                    var perp = new Vector2(-aim.y, aim.x);
                    Streak(at - perp * 0.9f, at + perp * 0.9f, 0f, 0.16f, Pal.RedDeep, Color.white, 0.25f, 46);
                    Impact(at, 1.1f, Pal.Ink.A(0.85f), Pal.GoldBright, 3);
                    Kick(at, aim, 0.32f);
                    if (stun) StunStars(at);
                });
            }
            Ring(pos, r * 2f, Pal.Ink, 0.4f, 44);
            After(0.42f, () =>
            {
                Crescent(pos, -aim, r * 2.3f, Pal.RedDeep, Pal.GoldBright, true, 0.45f, 45);
                foreach (var t in targets)
                {
                    Impact(t, 1.3f, Pal.RedDeep.A(0.85f), Pal.GoldBright, 4);
                    Kick(t, -aim, 0.25f);
                }
                if (mine) { _shake = Mathf.Max(_shake, 1.2f); HitStop(0.05f); }
            });
            for (int i = 0; i < 3; i++)
            {
                var perp = new Vector2(-aim.y, aim.x);
                var mid = pos + aim * r * (0.2f + i * 0.3f) + perp * Rnd(-0.4f, 0.4f);
                After(0.2f + i * 0.08f, () => Ink("fx_crack", Pal.Ink.A(0.55f), mid, r * 0.7f, 1.4f, Ang(perp), 31, 0.4f, 0f, 0.5f));
            }
            if (mine) { _shake = Mathf.Max(_shake, 1f); HitStop(0.06f); }
        }

        /// <summary>剑雨：一把把剑从天上插下来，落满整片。</summary>
        void SwordRainFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            int n = Mathf.Max(12, targets.Count * 2);
            for (int i = 0; i < n; i++)
            {
                var at = i < targets.Count ? targets[i] : pos + UnityEngine.Random.insideUnitCircle * r * 0.85f;
                bool real = i < targets.Count;
                bool loud = mine && i % 3 == 0;
                SkyDrop("wp_sword", at, real ? 1.2f : 0.9f, 0.05f + i * 0.045f, Rnd(-15f, 15f), () =>
                {
                    Impact(at, real ? 1.0f : 0.55f, Pal.Hex("2B3A55").A(0.85f), Color.white, real ? 3 : 1);
                    Ink("fx_lines", Color.white, at, 1f, 0.2f, 90, 46, 0.5f);
                    if (real) Kick(at, Vector2.down, 0.25f);
                    if (loud) Sfx.Play(Snd.Sword, 0.35f, 1.1f);
                }, 0.16f);
            }
            Ink("fx_enso", Pal.Hex("2B3A55").A(0.5f), pos, r * 2f, 1f, 0, 42, 0.5f, 0f, 0.6f);
            if (mine) _shake = Mathf.Max(_shake, 0.9f);
        }

        /// <summary>巨浪：一堵水墙从一侧推过整片，浪头经过的敌人被拍飞。</summary>
        void TideFx(int side, Vector2 pos, float r, List<Vector2> targets, bool mine, bool fire)
        {
            UltDim(side, WaterDeep, 0.22f, 1.1f);
            float x0 = pos.x - r - 0.8f, x1 = pos.x + r + 0.8f;
            const float life = 0.75f;
            var hitDone = new bool[targets.Count];
            for (int row = 0; row < 3; row++)
            {
                float y = pos.y + (row - 1) * r * 0.55f;
                int R = row;
                NewSpriteFx(Art.Img("fx_wave"), WaterDeep.A(0.92f), new Vector2(x0, y), new Vector2(2.4f, 2.2f), 45 + R, life, (f, k) =>
                {
                    float e = Tween.OutCubic(k);
                    float x = Mathf.Lerp(x0, x1, e);
                    f.Go.transform.localPosition = new Vector2(x, y + Mathf.Sin(k * 9f + R) * 0.1f);
                    float s = Mathf.Lerp(1.2f, 2.4f, Mathf.Sin(Mathf.Min(1f, k * 1.5f) * Mathf.PI * 0.5f));
                    f.Go.transform.localScale = new Vector3(s, s, 1);
                    f.Sr.color = f.C.A(0.92f * (k < 0.75f ? 1f : (1f - k) / 0.25f));
                    if (R != 1) return;
                    for (int i = 0; i < targets.Count; i++)
                    {
                        if (hitDone[i] || targets[i].x > x) continue;
                        hitDone[i] = true;
                        Splash(targets[i], 1.1f);
                        if (fire) FireBurst(targets[i], 0.8f);
                        Kick(targets[i], Vector2.right, 0.4f);
                    }
                });
            }
            for (int i = 0; i < 14; i++)
            {
                float t = i / 14f * life;
                var p = new Vector2(Mathf.Lerp(x0, x1, Tween.OutCubic(t / life)), pos.y + Rnd(-r * 0.7f, r * 0.7f));
                After(t, () => Splash(p, 0.6f));
            }
            if (mine) { _shake = Mathf.Max(_shake, 1f); HitStop(0.05f); }
        }

        /// <summary>地刺：每个敌人脚下猛地捅出一柄钢叉。</summary>
        void SpikeFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            Ring(pos, r * 2f, WaterDeep, 0.45f, 44);
            for (int i = 0; i < targets.Count; i++)
            {
                var at = targets[i];
                After(0.05f + i * 0.05f, () =>
                {
                    var from = at + Vector2.down * 0.9f;
                    NewSpriteFx(Art.Img("wp_fork"), Color.white, from, Vector2.one * 1.3f, 47, 0.5f, (f, k) =>
                    {
                        float e = k < 0.25f ? Tween.OutCubic(k / 0.25f) : 1f;
                        f.Go.transform.localPosition = Vector2.Lerp(from, at + Vector2.up * 0.25f, e);
                        f.Go.transform.localRotation = Quaternion.Euler(0, 0, 45f);
                        f.Sr.color = Color.white.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
                    });
                    Ink("fx_crack", Pal.Ink.A(0.8f), at + Vector2.down * 0.2f, 1.1f, 0.7f, Rnd(0, 360), 31, 0.5f, 0f, 0.5f);
                    Splash(at, 0.8f);
                    Kick(at, Vector2.up, 0.35f);
                });
            }
            if (mine) _shake = Mathf.Max(_shake, 0.8f);
        }

        /// <summary>蒙汗药：药罐一翻，紫烟铺满整片，敌人倒头就睡。</summary>
        void SleepFx(Vector2 pos, float r, List<Vector2> targets)
        {
            NewSpriteFx(Art.Img("it_menghan"), Color.white, pos + Vector2.up * 0.6f, Vector2.one * 1.2f, 47, 0.7f, (f, k) =>
            {
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(0f, -120f, Tween.OutCubic(Mathf.Min(1f, k * 2f))));
                f.Sr.color = Color.white.A(k < 0.6f ? 1f : (1f - k) / 0.4f);
            });
            for (int i = 0; i < 18; i++)
            {
                var p = pos + UnityEngine.Random.insideUnitCircle * r * 0.9f;
                After(0.15f + i * 0.03f, () =>
                {
                    var fx = Ink("fx_dust", Pal.Purple.A(0.45f), pos, Rnd(1.2f, 1.9f), 1.8f, Rnd(0, 360), 37, 0.3f, Rnd(-50, 50), 0.6f);
                    var step = fx.Step;
                    fx.Step = (f, k) => { step(f, k); f.Go.transform.localPosition = Vector2.Lerp(pos, p, Tween.OutCubic(Mathf.Min(1f, k * 2.5f))); };
                });
            }
            foreach (var t in targets)
            {
                var at = t;
                After(0.45f, () =>
                {
                    TextFx("Zz", 0.36f, Pal.Purple, at + new Vector2(0.3f, 0.5f), 49, 1.6f, (f, k) =>
                    {
                        f.Go.transform.localPosition = f.A + new Vector2(Mathf.Sin(k * 6f) * 0.08f, k * 0.5f);
                        f.Tm.color = f.C.A(k < 0.75f ? 1f : (1f - k) * 4f);
                    });
                });
            }
        }

        /// <summary>连环甲马：一排铁甲马从一头冲到另一头，碾过整片。</summary>
        void CavalryFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            float x0 = pos.x - r - 1.2f, x1 = pos.x + r + 1.2f;
            const float run = 0.85f;
            for (int wave = 0; wave < 2; wave++)
                for (int i = 0; i < 5; i++)
                {
                    float y = pos.y + (i - 2) * r * 0.3f + wave * r * 0.15f;
                    var from = new Vector2(x0, y);
                    var to = new Vector2(x1, y);
                    float size = wave == 0 ? 1.35f : 1.1f;
                    After(wave * 0.3f + i * 0.03f, () =>
                    {
                        NewSpriteFx(Art.Img("horse"), Pal.Hex("D9D2C4"), from, Vector2.one * size, 46, run, (f, k) =>
                        {
                            float gallop = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 9f));
                            f.Go.transform.localPosition = Vector2.Lerp(from, to, k) + Vector2.up * gallop * 0.12f;
                            f.Go.transform.localScale = new Vector3(-size, size, 1);
                            f.Sr.color = f.C.A(Mathf.Clamp01(Mathf.Min(k / 0.08f, (1f - k) / 0.15f)));
                        });
                        for (int j = 0; j < 5; j++)
                        {
                            float t = j * 0.16f;
                            After(t, () => DustPuff(Vector2.Lerp(from, to, t / run) + Vector2.down * 0.3f, 0.7f));
                        }
                    });
                }
            for (int i = 0; i < 4; i++)
            {
                float y = pos.y + (i - 1.5f) * r * 0.3f;
                NewSpriteFx(Art.White, Pal.Ink.A(0.7f), new Vector2(x0, y), new Vector2(0.05f, r * 0.3f), 45, run, (f, k) =>
                {
                    f.Go.transform.localPosition = new Vector2(Mathf.Lerp(x0, x1, k), y);
                    f.Sr.color = f.C.A(0.7f * Mathf.Clamp01(Mathf.Min(k / 0.08f, (1f - k) / 0.15f)));
                });
            }
            foreach (var t in targets)
            {
                var at = t;
                After(Mathf.Clamp01((at.x - x0) / (x1 - x0)) * run, () =>
                {
                    Impact(at, 1.0f, Pal.Ink.A(0.85f), Steel, 3);
                    Kick(at, Vector2.right, 0.4f);
                    if (mine) Sfx.Play(Snd.Horse, 0.3f, 1.1f);
                });
            }
            if (mine) _shake = Mathf.Max(_shake, 1f);
        }

        /// <summary>震地：重兵器从天上砸下，裂纹四散，一圈圈尘浪往外推。</summary>
        void QuakeFx(Vector2 pos, float r, List<Vector2> targets, bool mine, string img)
        {
            SkyDrop(img, pos, 2.3f, 0f, 0f, () =>
            {
                GroundSmash(pos, r * 1.1f, Pal.Ink, mine);
                for (int i = 0; i < 6; i++)
                {
                    float a = (i * 60f + Rnd(-15f, 15f)) * Mathf.Deg2Rad;
                    var p = pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.55f;
                    Ink("fx_crack", Pal.Ink.A(0.8f), p, 1.2f, 0.9f, Rnd(0, 360), 31, 0.5f, 0f, 0.6f);
                }
                for (int i = 0; i < 3; i++)
                {
                    int idx = i;
                    After(i * 0.1f, () => Ring(pos, r * (0.9f + idx * 0.65f), Dust.A(0.85f), 0.45f, 44));
                }
                for (int i = 0; i < 12; i++)
                {
                    var dir = new Vector2(Rnd(-1f, 1f), Rnd(0.4f, 1.3f));
                    var fx = NewSpriteFx(Art.Circle, Pal.Earth, pos, Vector2.one * Rnd(0.12f, 0.24f), 46, 0.7f, (f, k) =>
                    {
                        f.Go.transform.localPosition = f.A + (f.B - f.A) * k + new Vector2(0, -k * k * 2f);
                        f.Sr.color = f.C.A(1f - k * k);
                    });
                    fx.A = pos;
                    fx.B = pos + dir * r * 0.6f;
                }
                foreach (var t in targets)
                {
                    var at = t;
                    After(0.08f, () =>
                    {
                        Impact(at, 0.9f, Pal.Ink.A(0.8f), Steel, 2);
                        Kick(at, (at - pos).sqrMagnitude > 1e-3f ? (at - pos).normalized : Vector2.up, 0.4f);
                    });
                }
                if (mine) { _shake = Mathf.Max(_shake, 1.3f); HitStop(0.08f); }
            }, 0.22f);
        }

        /// <summary>霹雳：火弹划一道弧落进敌堆，白光一闪，一圈圈火浪炸开。</summary>
        void BlastFx(Vector2 from, Vector2 at, float r, bool mine)
        {
            const float flight = 0.36f;
            var ball = NewSpriteFx(Art.Circle, Pal.Ink, from, Vector2.one * 0.5f, 47, flight, null);
            ball.Step = (f, k) =>
            {
                var p = Vector2.Lerp(from, at, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 1.6f;
                f.Go.transform.localPosition = p;
                if (UnityEngine.Random.value < 0.6f) Ink("fx_fire", Fire.A(0.85f), p, 0.5f, 0.25f, Rnd(-20, 20), 46, 0.6f, 0f, 0.2f);
            };
            After(flight, () =>
            {
                NewSpriteFx(Art.SoftDot, Color.white, at, Vector2.one * r * 3f, 48, 0.22f, (f, k) =>
                {
                    float s = Mathf.Lerp(r * 1.5f, r * 3.4f, k);
                    f.Go.transform.localScale = new Vector3(s, s, 1);
                    f.Sr.color = Color.white.A(1f - k);
                });
                FireBurst(at, r * 1.4f);
                for (int i = 0; i < 8; i++)
                {
                    float a = (i * 45f + Rnd(-10, 10)) * Mathf.Deg2Rad;
                    var p = at + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * Rnd(0.5f, 0.9f);
                    After(0.04f + i * 0.025f, () => FireBurst(p, Rnd(0.8f, 1.2f)));
                }
                for (int i = 0; i < 3; i++)
                {
                    int idx = i;
                    After(i * 0.08f, () => Ring(at, r * (1.4f + idx * 0.7f), idx == 0 ? FireCore : Fire, 0.45f, 46));
                }
                Ink("fx_crack", Pal.Ink.A(0.85f), at, r * 1.6f, 1.0f, Rnd(0, 360), 31, 0.5f, 0f, 0.6f);
                for (int i = 0; i < 6; i++)
                {
                    var p = at + UnityEngine.Random.insideUnitCircle * r;
                    After(0.2f + i * 0.04f, () => Ink("fx_dust", Pal.InkSoft.A(0.5f), p, Rnd(1f, 1.5f), 1.1f, Rnd(0, 360), 40, 0.4f, 30f, 0.4f));
                }
                foreach (var t in EnemiesNearWorld(at, r + 0.3f)) Kick(t, (t - at).sqrMagnitude > 1e-3f ? (t - at).normalized : Vector2.up, 0.45f);
                if (mine) { _shake = Mathf.Max(_shake, 1.5f); HitStop(0.09f); Sfx.Play(Snd.UltCannon, 0.7f); }
            });
        }

        IEnumerable<Vector2> EnemiesNearWorld(Vector2 at, float r)
        {
            foreach (var v in _enemies.Values)
            {
                if (v.Root == null || !v.Root.gameObject.activeSelf) continue;
                var p = (Vector2)v.Root.localPosition;
                if ((p - at).sqrMagnitude <= r * r) yield return p;
            }
        }

        /// <summary>重创定住一人：兵器从天上斜插进目标，红圈锁死。</summary>
        void PinFx(Vector2 at, HeroDef d, bool mine, bool water)
        {
            string img = HeroWeaponSprite(d);
            SkyDrop(img, at, 2.4f, 0f, Rnd(-25f, 25f), () =>
            {
                if (water) Splash(at, 1.6f);
                Impact(at, 1.9f, Pal.RedDeep.A(0.9f), Pal.GoldBright, 8);
                Ring(at, 3f, Pal.Red, 0.5f, 46);
                Ink("fx_crack", Pal.Ink.A(0.85f), at, 1.6f, 1.0f, Rnd(0, 360), 31, 0.5f, 0f, 0.6f);
                Ink("fx_enso", Pal.Red.A(0.9f), at, 1.4f, 1.1f, 0, 46, 1.6f, 240f, 0.6f);
                if (d.UltStun > 0) StunStars(at);
                Kick(at, Vector2.down, 0.4f);
                if (mine) { HitStop(0.09f); _shake = Mathf.Max(_shake, 1.1f); }
            }, 0.16f);
        }

        /// <summary>相扑：把最壮的那个整个举过头顶，再狠狠摔回去。</summary>
        void ThrowFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            var t = targets.Count > 0 ? targets[0] : pos;
            var view = EnemyNear(t, 1f);
            if (view != null)
            {
                var glyph = view.GlyphText;
                var color = view.GlyphColor;
                float h = view.GlyphHeight * 1.3f;
                TextFx(glyph, h, color, t, 47, 0.55f, (f, k) =>
                {
                    float e = Mathf.Sin(k * Mathf.PI);
                    f.Go.transform.localPosition = t + Vector2.up * e * 1.8f;
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, k * 360f);
                    f.Tm.color = f.C.A(k < 0.9f ? 1f : (1f - k) * 10f);
                }, false);
            }
            After(0.5f, () =>
            {
                GroundSmash(t, 2f, Pal.Ink, mine);
                Impact(t, 1.8f, Pal.Ink.A(0.9f), Pal.GoldBright, 8);
                for (int i = 0; i < 6; i++) DustPuff(t + UnityEngine.Random.insideUnitCircle * 0.8f, Rnd(0.6f, 0.9f));
                TextFx("摔！", 0.55f, Pal.Red, t + new Vector2(0, 1f), 49, 0.8f, (f, k) =>
                {
                    float s = k < 0.15f ? Mathf.Lerp(1.8f, 1f, k / 0.15f) : 1f;
                    f.Go.transform.localScale = new Vector3(s, s, 1);
                    f.Tm.color = f.C.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
                });
                if (mine) { _shake = Mathf.Max(_shake, 1.2f); HitStop(0.08f); }
            });
        }

        /// <summary>神行：狂风卷着甲马符刮过整片半场。</summary>
        void WindFx(int side, bool mine)
        {
            var c = BoardCenter(side);
            UltDim(side, Ice, 0.18f, 1.2f);
            for (int i = 0; i < 34; i++)
            {
                var start = new Vector2(Left - 1f - Rnd(0f, 4f), c.y + Rnd(-2.6f, 2.6f));
                After(Rnd(0f, 0.4f), () =>
                {
                    var fx = NewSpriteFx(Art.Img("fx_thrust"), Ice.A(0.85f), start, new Vector2(Rnd(1.4f, 2.4f), 0.4f), 46, Rnd(0.55f, 0.85f), (f, k) =>
                    {
                        f.Go.transform.localPosition = f.A + Vector2.right * k * 13f;
                        f.Sr.color = f.C.A(0.85f * Mathf.Sin(k * Mathf.PI));
                    });
                    fx.A = start;
                });
            }
            for (int i = 0; i < 6; i++)
            {
                var start = new Vector2(Left - 1f - i * 0.7f, c.y + Rnd(-2f, 2f));
                float spin = Rnd(-400f, 400f);
                var paper = NewSpriteFx(Art.White, Pal.Paper, start, new Vector2(0.36f, 0.56f), 47, 1f, null);
                paper.A = start;
                paper.Step = (f, k) =>
                {
                    f.Go.transform.localPosition = f.A + new Vector2(k * 12f, Mathf.Sin(k * 7f) * 0.4f);
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, spin * k);
                };
                TextFx("马", 0.3f, Pal.Red, start, 48, 1f, (f, k) =>
                {
                    f.Go.transform.localPosition = paper.Go.transform.localPosition;
                    f.Go.transform.localRotation = paper.Go.transform.localRotation;
                    f.Tm.color = f.C;
                }, false);
            }
            if (mine) _shake = Mathf.Max(_shake, 0.6f);
        }

        /// <summary>智取 / 布阵：羽扇一挥，一道道金线连到每位好汉身上。</summary>
        void HasteFx(int side, Vector2 hero)
        {
            NewSpriteFx(Art.Img("wp_fan"), Color.white, hero, Vector2.one * 0.6f, 48, 0.8f, (f, k) =>
            {
                float s = k < 0.25f ? Mathf.Lerp(0.6f, 1.6f, Tween.OutBack(k / 0.25f)) : 1.6f;
                f.Go.transform.localScale = new Vector3(s, s, 1);
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 10f) * 25f);
                f.Sr.color = Color.white.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
            });
            Ink("ui_rays", Pal.GoldBright.A(0.7f), hero, 2.6f, 0.9f, 0, 45, 0.4f, 120f, 0.4f);
            var c = BoardCenter(side);
            UltDim(side, Pal.Hex("5A3A10"), 0.18f, 1.2f);
            for (int s = 0; s < 14; s++)
            {
                float y = c.y + Rnd(-Board.Rows * 0.5f, Board.Rows * 0.5f);
                float x = c.x + Rnd(-Board.Cols * 0.5f, Board.Cols * 0.5f - 1.5f);
                After(0.1f + s * 0.06f, () =>
                    Streak(new Vector2(x, y), new Vector2(x + Rnd(1.2f, 2.4f), y + 0.15f), 0f, 0.3f, Pal.GoldBright.A(0.7f), Color.white, 0.12f, 45, 0f));
            }
            int i = 0;
            foreach (var h in M.Sides[side].Heroes)
            {
                var p = World(side, h.Pos);
                if ((p - hero).sqrMagnitude < 0.01f) continue;
                int idx = i++;
                After(0.1f + idx * 0.05f, () =>
                {
                    Streak(hero, p, 0f, 0.12f, Pal.GoldBright, Color.white, 0.35f, 46);
                    Ring(p, 1.8f, Pal.GoldBright, 0.5f, 45);
                    Ink("ui_sparkle", Color.white, p, 0.9f, 0.6f, 0, 47, 0.3f, 90f, 0.4f);
                    if (_frames.TryGetValue(h.Id, out var fv)) fv.Glow();
                });
            }
        }

        /// <summary>天雷落遍全场：先压一层乌云，再一道道劈下来。樊瑞的连风带雨。</summary>
        void StormFx(int side, List<Vector2> targets, bool mine, bool rain)
        {
            var c = BoardCenter(side);
            UltDim(side, Pal.Hex("2A2140"), 0.45f, 1.5f);
            for (int i = 0; i < 6; i++)
            {
                var p = new Vector2(Left + 0.7f + i * 1.35f, c.y + 2.6f);
                Ink("fx_dust", Pal.Hex("2A2140").A(0.75f), p, 2.4f, 1.5f, Rnd(0, 360), 39, 0.5f, Rnd(-20, 20), 0.7f);
            }
            int n = Mathf.Max(6, targets.Count);
            for (int i = 0; i < n; i++)
            {
                var t = i < targets.Count ? targets[i] : c + new Vector2(Rnd(-3.5f, 3.5f), Rnd(-2.2f, 2.2f));
                Bolt(t, 0.15f + i * 0.05f, mine, i < targets.Count ? 1f : 0.7f);
            }
            for (int i = 0; i < Mathf.Min(6, targets.Count); i++) Bolt(targets[i], 0.75f + i * 0.06f, mine, 0.8f);
            for (int i = 0; i < 3; i++)
                After(0.15f + i * 0.3f, () => UltDim(side, Color.white, 0.35f, 0.12f));
            if (rain) RainFx(side, 1.3f, 60);
            if (mine) { _shake = Mathf.Max(_shake, 1.3f); After(0.15f, () => HitStop(0.06f)); }
        }

        /// <summary>斜雨：一阵细线从左上往右下扫过半场。</summary>
        void RainFx(int side, float life, int count)
        {
            var c = BoardCenter(side);
            for (int i = 0; i < count; i++)
            {
                var start = c + new Vector2(Rnd(-5f, 3.5f), Rnd(1.5f, 3.5f));
                After(Rnd(0f, life * 0.7f), () =>
                {
                    var fx = NewSpriteFx(Art.White, Pal.Hex("B9CFE0"), start, new Vector2(0.04f, Rnd(0.35f, 0.6f)), 46, Rnd(0.35f, 0.55f), (f, k) =>
                    {
                        f.Go.transform.localPosition = f.A + new Vector2(1.2f, -5.2f) * k;
                        f.Sr.color = f.C.A(0.75f * Mathf.Sin(k * Mathf.PI));
                    });
                    fx.A = start;
                    fx.Go.transform.localRotation = Quaternion.Euler(0, 0, 13f);
                });
            }
        }

        /// <summary>替天行道 / 护体：大旗从宋江身后升起，金光罩住老营。</summary>
        void BannerFx(int side, bool rally, bool mine)
        {
            var goal = World(side, M.Sides[side].Board.Goal.Center);
            Ink("ui_rays", Pal.GoldBright.A(0.8f), goal, 3.2f, 1.3f, 0, 43, 0.4f, 80f, 0.6f);
            if (rally)
            {
                var from = goal + new Vector2(0.4f, -0.5f);
                NewSpriteFx(Art.Img("ui_flag"), Color.white, from, Vector2.one * 2.2f, 47, 1.4f, (f, k) =>
                {
                    f.Go.transform.localPosition = Vector2.Lerp(from, goal + new Vector2(0.4f, 1.1f), Tween.OutBack(Mathf.Min(1f, k * 3f)));
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 12f) * 4f);
                    f.Sr.color = Color.white.A(k < 0.8f ? 1f : (1f - k) * 5f);
                });
                TextFx("替天行道", 0.75f, Pal.Red, BoardCenter(side), 49, 1.3f, (f, k) =>
                {
                    float s = k < 0.15f ? Mathf.Lerp(2f, 1f, Tween.OutCubic(k / 0.15f)) : 1f;
                    f.Go.transform.localScale = new Vector3(s, s, 1);
                    f.Tm.color = f.C.A(k < 0.75f ? 1f : (1f - k) * 4f);
                });
                for (int i = 0; i < 4; i++) { int k = i; After(i * 0.15f, () => Ring(BoardCenter(side), 3f + k * 1.6f, Pal.Red, 0.55f, 45)); }
            }
            else
            {
                NewSpriteFx(Art.Img("ui_shield"), Color.white, goal, Vector2.one * 0.6f, 47, 1.1f, (f, k) =>
                {
                    float s = k < 0.25f ? Mathf.Lerp(0.6f, 1.8f, Tween.OutBack(k / 0.25f)) : 1.8f;
                    f.Go.transform.localScale = new Vector3(s, s, 1);
                    f.Sr.color = Color.white.A(k < 0.75f ? 1f : (1f - k) * 4f);
                });
            }
            for (int i = 0; i < 3; i++) { int k = i; After(i * 0.18f, () => Ring(goal, 1.6f + k * 0.9f, Pal.GoldBright, 0.5f, 46)); }
            Ink("fx_enso", Pal.GoldBright.A(0.9f), goal, 2.4f, 1.1f, 0, 44, 0.3f, -300f, 0.6f);
            if (mine) _shake = Mathf.Max(_shake, 0.7f);
        }
    }
}
