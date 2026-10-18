using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;

namespace Rescue
{
    /// <summary>局内攻击与受击表现：水墨笔触的刀光、粗箭、枪芒、马冲，每位好汉按兵器定制。</summary>
    public sealed partial class BattleView
    {
        sealed class Later
        {
            public float T;
            public Action A;
        }

        readonly List<Later> _later = new List<Later>();
        float _hitStop;

        static readonly Color Steel = Pal.Hex("F4F1E6");
        static readonly Color WaterDeep = Pal.Hex("2F6E9E");
        static readonly Color Fire = Pal.Hex("E8641E");
        static readonly Color FireCore = Pal.Hex("FFD24A");
        static readonly Color Ice = Pal.Hex("A9D8F0");
        static readonly Color Dust = Pal.Hex("A07850");

        void After(float t, Action a) => _later.Add(new Later { T = t, A = a });

        void TickLater(float dt)
        {
            for (int i = _later.Count - 1; i >= 0; i--)
            {
                if (i >= _later.Count) continue;
                var l = _later[i];
                l.T -= dt;
                if (l.T > 0) continue;
                _later.RemoveAt(i);
                l.A();
            }
        }

        /// <summary>重击顿帧：模拟停几十毫秒，打击感来自这一下停顿。</summary>
        void HitStop(float t) => _hitStop = Mathf.Max(_hitStop, t);

        static float Ang(Vector2 d) => Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        static float Rnd(float a, float b) => UnityEngine.Random.Range(a, b);

        EnemyView EnemyNear(Vector2 p, float maxD = 0.7f)
        {
            EnemyView best = null;
            float bd = maxD * maxD;
            foreach (var v in _enemies.Values)
            {
                if (v.Root == null) continue;
                float d = ((Vector2)v.Root.localPosition - p).sqrMagnitude;
                if (d < bd) { bd = d; best = v; }
            }
            return best;
        }

        void Kick(Vector2 at, Vector2 dir, float amount)
        {
            var v = EnemyNear(at);
            if (v != null) v.Kick(dir, amount);
        }

        IEnumerable<Vector2> EnemiesWithin(int side, V2 center, float r, int max = 99)
        {
            int n = 0;
            foreach (var e in M.Sides[side].Enemies)
            {
                if (e.Dead || e.InBrush || V2.Dist(e.Pos, center) > r) continue;
                yield return World(side, e.Pos);
                if (++n >= max) yield break;
            }
        }

        // ─────────────────────────── 笔触部件 ───────────────────────────

        /// <summary>一笔墨团：先猛地张开再慢慢淡掉。</summary>
        Fx Ink(string img, Color c, Vector2 pos, float size, float life, float rot, int order = 44, float from = 0.45f, float spin = 0f, float hold = 0.35f)
        {
            var fx = NewSpriteFx(Art.Img(img), c, pos, Vector2.one * size * from, order, life, (f, k) =>
            {
                float s = size * Mathf.Lerp(from, 1f, Tween.OutCubic(Mathf.Min(1f, k * 3f)));
                f.Go.transform.localScale = new Vector3(s, s, 1);
                if (spin != 0f) f.Go.transform.localRotation = Quaternion.Euler(0, 0, rot + spin * Tween.OutCubic(k));
                f.Sr.color = f.C.A(f.C.a * (k < hold ? 1f : 1f - (k - hold) / (1f - hold)));
            });
            fx.Go.transform.localRotation = Quaternion.Euler(0, 0, rot);
            return fx;
        }

        /// <summary>命中点：墨爆 + 速度线 + 墨点，大小随伤害档位。</summary>
        void Impact(Vector2 pos, float size, Color ink, Color lines, int drops = 3)
        {
            Ink("fx_burst", ink, pos, size, 0.32f, Rnd(0, 360), 43, 0.35f);
            Ink("fx_lines", lines, pos, size * 1.25f, 0.2f, Rnd(0, 360), 45, 0.6f, 0f, 0.2f);
            if (drops > 0) InkDrops(pos, drops);
        }

        /// <summary>弯月刀光：外圈亮边 + 内圈墨，扫过 100° 后散掉。</summary>
        void Crescent(Vector2 pos, Vector2 dir, float size, Color ink, Color rim, bool mirror, float life = 0.24f, int order = 44)
        {
            float rot = Ang(dir) - 180f;
            float sweep = mirror ? -1f : 1f;
            for (int layer = 0; layer < 2; layer++)
            {
                bool back = layer == 0;
                float sz = size * (back ? 1.1f : 1f);
                var c = back ? rim : ink;
                var fx = NewSpriteFx(Art.Img("fx_crescent"), c, pos, Vector2.one * sz, order + layer, life, (f, k) =>
                {
                    float e = Tween.OutCubic(k);
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, rot + sweep * Mathf.Lerp(55f, -45f, e));
                    float s = sz * Mathf.Lerp(0.65f, 1.12f, e);
                    f.Go.transform.localScale = new Vector3(s, s * sweep, 1);
                    f.Sr.color = f.C.A(f.C.a * (k < 0.45f ? 1f : 1f - (k - 0.45f) / 0.55f));
                });
                fx.Go.transform.localRotation = Quaternion.Euler(0, 0, rot + sweep * 55f);
            }
        }

        /// <summary>一道由粗到尖的笔锋（枪芒、刺击、水箭）。从 from 向 to 方向长出去；rimK 为 0 时只画一层细线。</summary>
        void Streak(Vector2 from, Vector2 to, float extra, float thick, Color ink, Color rim, float life = 0.22f, int order = 43, float rimK = 1.3f)
        {
            var d = to - from;
            var n = d.sqrMagnitude > 1e-4f ? d.normalized : Vector2.up;
            float len = d.magnitude + extra;
            float rot = Ang(n);
            const float aspect = 89f / 288f;
            for (int layer = rimK > 0f ? 0 : 1; layer < 2; layer++)
            {
                bool back = layer == 0;
                float th = thick * (back ? rimK : 1f);
                float ln = len * (back ? 1.04f : 1f);
                var c = back ? rim : ink;
                var fx = NewSpriteFx(Art.Img("fx_thrust"), c, from, new Vector2(0.01f, th / aspect), order + layer, life, (f, k) =>
                {
                    float g = Tween.OutCubic(Mathf.Min(1f, k * 4f));
                    float l = Mathf.Max(0.01f, ln * g);
                    f.Go.transform.localPosition = from + n * (l * 0.5f + (back ? -0.04f : 0f));
                    f.Go.transform.localScale = new Vector3(l, th / aspect * (1f - k * 0.6f), 1);
                    f.Sr.color = f.C.A(f.C.a * (k < 0.4f ? 1f : 1f - (k - 0.4f) / 0.6f));
                });
                fx.Go.transform.localRotation = Quaternion.Euler(0, 0, rot);
            }
        }

        /// <summary>粗笔箭：带白描边、抛物线、拖尾，命中后插在敌人身上抖一下。</summary>
        void InkArrow(Vector2 from, Vector2 to, Color ink, Color rim, float size, float flight, Action onHit)
        {
            var d = to - from;
            float dist = d.magnitude;
            float arc = Mathf.Min(0.45f, dist * 0.1f);
            Vector2 At(float k) => Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * arc;
            float RotAt(float k) => Ang(d + Vector2.up * (arc * Mathf.PI * Mathf.Cos(k * Mathf.PI)));

            NewSpriteFx(Art.Img("fx_thrust"), rim.A(0.45f), from, new Vector2(0.01f, size * 0.3f), 41, flight, (f, k) =>
            {
                var p = At(k);
                var tail = At(Mathf.Max(0f, k - 0.45f));
                var seg = p - tail;
                float l = Mathf.Max(0.01f, seg.magnitude);
                f.Go.transform.localPosition = (p + tail) * 0.5f;
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, Ang(seg) + 180f);
                f.Go.transform.localScale = new Vector3(l, size * 0.4f, 1);
            });

            for (int layer = 0; layer < 2; layer++)
            {
                bool back = layer == 0;
                float sz = size * (back ? 1.14f : 1f);
                bool last = layer == 1;
                NewSpriteFx(Art.Img("fx_arrow"), back ? rim : ink, from, Vector2.one * sz, 42 + layer, flight, (f, k) =>
                {
                    f.Go.transform.localPosition = At(k);
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, RotAt(k));
                    if (last && k >= 1f) onHit?.Invoke();
                });
            }
        }

        /// <summary>箭插在目标身上：尾羽抖两下再淡出。</summary>
        void StuckArrow(Vector2 at, Vector2 dir, Color ink, float size)
        {
            float rot = Ang(dir);
            var p = at - dir.normalized * size * 0.25f;
            NewSpriteFx(Art.Img("fx_arrow"), ink, p, Vector2.one * size * 0.8f, 36, 0.4f, (f, k) =>
            {
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, rot + Mathf.Sin(k * 40f) * 10f * (1f - k));
                f.Sr.color = f.C.A(k < 0.5f ? 1f : (1f - k) * 2f);
            });
        }

        /// <summary>骑兵冲锋：战马从棋子冲到敌人面前、撞上、再跑回去，一路扬尘。</summary>
        void Charge(Vector2 from, Vector2 to, float size, Color tint, Action onHit)
        {
            var d = to - from;
            var n = d.sqrMagnitude > 1e-4f ? d.normalized : Vector2.up;
            var end = to - n * 0.3f;
            bool flip = n.x > 0;
            bool hit = false;
            var horse = NewSpriteFx(Art.Img("horse"), tint, from, Vector2.one * size, 46, 0.42f, null);
            horse.Step = (f, k) =>
            {
                Vector2 p;
                if (k < 0.42f) { float e = k / 0.42f; p = Vector2.Lerp(from, end, e * e); }
                else if (k < 0.55f) p = end + n * Mathf.Sin((k - 0.42f) / 0.13f * Mathf.PI) * 0.08f;
                else p = Vector2.Lerp(end, from, Tween.OutCubic((k - 0.55f) / 0.45f));
                float gallop = Mathf.Abs(Mathf.Sin(k * Mathf.PI * 5f));
                f.Go.transform.localPosition = p + Vector2.up * gallop * 0.08f;
                float lean = k < 0.42f ? -14f : k < 0.55f ? 8f : 0f;
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, (flip ? -lean : lean) + Mathf.Sin(k * 30f) * 4f);
                bool back = k >= 0.55f;
                float sx = size * ((flip ^ back) ? -1f : 1f);
                f.Go.transform.localScale = new Vector3(sx * (k < 0.42f ? 1.08f : 1f), size, 1);
                float a = Mathf.Clamp01(Mathf.Min(k / 0.08f, (1f - k) / 0.3f));
                f.Sr.color = tint.A(a);
                f.A = p;
                if (!hit && k >= 0.42f) { hit = true; onHit?.Invoke(); }
            };
            horse.A = from;

            NewSpriteFx(Art.Img("fx_thrust"), Steel.A(0.5f), from, new Vector2(0.01f, 0.3f), 45, 0.2f, (f, k) =>
            {
                var p = horse.A;
                var seg = p - from;
                float l = Mathf.Max(0.01f, seg.magnitude);
                f.Go.transform.localPosition = (p + from) * 0.5f;
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, Ang(n) + 180f);
                f.Go.transform.localScale = new Vector3(l, 0.45f * (1f - k), 1);
                f.Sr.color = f.C.A(0.5f * (1f - k));
            });

            for (int i = 0; i < 2; i++)
            {
                float t = 0.05f + i * 0.07f;
                var p = Vector2.Lerp(from, end, (t / 0.17f) * (t / 0.17f) * 0.8f) + new Vector2(Rnd(-0.1f, 0.1f), -0.22f);
                After(t, () => DustPuff(p, Rnd(0.38f, 0.5f)));
            }
        }

        void DustPuff(Vector2 p, float size)
        {
            var fx = Ink("fx_dust", Dust.A(0.5f), p, size, 0.5f, Rnd(-20, 20), 30, 0.4f, 0f, 0.15f);
            var drift = new Vector2(Rnd(-0.15f, 0.15f), Rnd(0.1f, 0.25f));
            var step = fx.Step;
            fx.Step = (f, k) =>
            {
                step(f, k);
                f.Go.transform.localPosition = p + drift * k;
            };
        }

        /// <summary>红锦套索：从好汉甩出一条下垂的绳子，套圈在敌人身上收紧。</summary>
        void Lasso(Vector2 from, Vector2 to, Color c, float delay = 0f)
        {
            After(delay, () =>
            {
                var d = to - from;
                const int segs = 8;
                for (int i = 0; i < segs; i++)
                {
                    int idx = i;
                    NewSpriteFx(Art.White, c, from, new Vector2(0.01f, 0.07f), 44, 0.45f, (f, k) =>
                    {
                        float reach = Tween.OutCubic(Mathf.Min(1f, k * 3f));
                        float t0 = idx / (float)segs * reach, t1 = (idx + 1) / (float)segs * reach;
                        float sag = 0.35f * (1f - Mathf.Min(1f, k * 1.5f));
                        Vector2 P(float t) => from + d * t + Vector2.down * Mathf.Sin(t * Mathf.PI) * sag;
                        var a = P(t0);
                        var b = P(t1);
                        var seg = b - a;
                        f.Go.transform.localPosition = (a + b) * 0.5f;
                        f.Go.transform.localRotation = Quaternion.Euler(0, 0, Ang(seg));
                        f.Go.transform.localScale = new Vector3(seg.magnitude + 0.02f, 0.07f, 1);
                        f.Sr.color = c.A(k < 0.7f ? 1f : (1f - k) / 0.3f);
                    });
                }
                After(0.13f, () =>
                {
                    NewSpriteFx(Art.Img("fx_lasso"), c, to, Vector2.one * 0.9f, 45, 0.5f, (f, k) =>
                    {
                        float s = Mathf.Lerp(0.9f, 0.5f, Tween.OutCubic(Mathf.Min(1f, k * 2.5f)));
                        f.Go.transform.localScale = new Vector3(s, s * 0.7f, 1);
                        f.Go.transform.localRotation = Quaternion.Euler(0, 0, k * 90f);
                        f.Sr.color = c.A(k < 0.6f ? 1f : (1f - k) / 0.4f);
                    });
                    Ink("fx_lines", c.A(0.8f), to, 0.9f, 0.2f, Rnd(0, 360), 45, 0.6f);
                });
            });
        }

        /// <summary>水浪：从好汉处卷到目标，落下溅起水花。</summary>
        void WaveTo(Vector2 from, Vector2 to, float size, Action onHit)
        {
            var d = to - from;
            bool flip = d.x < 0;
            NewSpriteFx(Art.Img("fx_wave"), WaterDeep.A(0.9f), from, Vector2.one * size * 0.5f, 44, 0.26f, (f, k) =>
            {
                float e = Tween.OutCubic(k);
                f.Go.transform.localPosition = Vector2.Lerp(from, to, e) + Vector2.up * Mathf.Sin(k * Mathf.PI) * 0.2f;
                float s = size * Mathf.Lerp(0.5f, 1f, e);
                f.Go.transform.localScale = new Vector3(flip ? -s : s, s, 1);
                f.Sr.color = f.C.A(0.9f * (k < 0.75f ? 1f : (1f - k) / 0.25f));
                if (k >= 1f) onHit?.Invoke();
            });
        }

        void Splash(Vector2 at, float size)
        {
            Ink("fx_burst", WaterDeep.A(0.75f), at, size, 0.35f, Rnd(0, 360), 43, 0.3f);
            Ink("fx_enso", Ice.A(0.9f), at, size * 1.2f, 0.35f, Rnd(0, 360), 42, 0.3f, 120f);
            for (int i = 0; i < 6; i++)
            {
                var dir = new Vector2(Rnd(-0.7f, 0.7f), Rnd(0.4f, 1.0f));
                var fx = NewSpriteFx(Art.Circle, Pal.Hex("6FB4DD"), at, Vector2.one * Rnd(0.07f, 0.13f), 46, 0.45f, (f, k) =>
                {
                    f.Go.transform.localPosition = f.A + (f.B - f.A) * k + new Vector2(0, -k * k * 0.9f);
                    f.Sr.color = f.C.A(1f - k);
                });
                fx.A = at;
                fx.B = at + dir * 0.7f;
            }
        }

        /// <summary>重兵器砸地：裂纹 + 尘圈 + 震屏。</summary>
        void GroundSmash(Vector2 at, float size, Color crack, bool mine)
        {
            Ink("fx_crack", crack.A(0.85f), at, size, 0.55f, Rnd(0, 360), 31, 0.5f, 0f, 0.5f);
            Ring(at, size * 1.3f, Dust.A(0.8f), 0.35f, 44);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + Rnd(-25, 25);
                var p = at + (Vector2)(Quaternion.Euler(0, 0, a) * Vector2.right) * size * 0.45f;
                DustPuff(p, Rnd(0.35f, 0.5f));
            }
            if (mine) { _shake = Mathf.Max(_shake, 0.45f); HitStop(0.05f); }
        }

        void FireBurst(Vector2 at, float size)
        {
            Ink("fx_fire", Fire.A(0.9f), at + Vector2.up * size * 0.15f, size, 0.45f, Rnd(-10, 10), 44, 0.3f, 0f, 0.3f);
            Ink("fx_fire", FireCore.A(0.95f), at + Vector2.up * size * 0.1f, size * 0.55f, 0.38f, Rnd(-10, 10), 45, 0.3f, 0f, 0.3f);
        }

        // ─────────────────────────── 小兵出手 ───────────────────────────

        void UnitAttackFx(SimEvent ev)
        {
            var from = World(ev.Side, ev.P);
            var to = World(ev.Side, ev.Q);
            var kind = (UnitKind)ev.B;
            var dir = to - from;
            var n = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector2.up;
            int lv = Mathf.Max(1, (int)ev.Val);
            if (_pieces.TryGetValue(ev.A, out var pv)) pv.Attack(kind, n);
            var lvc = Pal.ForLevel(lv);
            var ink = Color.Lerp(Pal.Ink, lvc, 0.55f);
            var rim = lv >= 4 ? Color.Lerp(Pal.GoldBright, Color.white, 0.35f) : Steel;
            float grow = 1f + (lv - 1) * 0.07f;
            bool mine = ev.Side == 0;
            switch (kind)
            {
                case UnitKind.Dao:
                    if (mine) Sfx.Play(Snd.Dao, 0.3f, 1.05f - lv * 0.03f);
                    Crescent(to, n, 0.82f * grow, ink, rim, UnityEngine.Random.value < 0.5f, 0.22f);
                    After(0.05f, () =>
                    {
                        Impact(to, 0.42f * grow, Pal.Ink.A(0.7f), rim, 2);
                        Kick(to, n, 0.12f);
                    });
                    break;
                case UnitKind.Qiang:
                    if (mine) Sfx.Play(Snd.Spear, 0.3f, 1.05f - lv * 0.03f);
                    After(0.05f, () => Streak(from + n * 0.7f, to, 0.25f, 0.07f * grow, rim.A(0.8f), rim, 0.15f, 43, 0f));
                    After(0.1f, () =>
                    {
                        Ink("fx_lines", rim, to, 0.6f * grow, 0.16f, Ang(n), 45, 0.5f);
                        foreach (var v in _enemies.Values)
                        {
                            var p = (Vector2)v.Root.localPosition;
                            var rel = p - from;
                            float along = Vector2.Dot(rel, n);
                            if (along < 0f || along > dir.magnitude + 0.7f) continue;
                            if ((rel - n * along).sqrMagnitude < 0.16f) v.Kick(n, 0.1f);
                        }
                    });
                    break;
                case UnitKind.Gong:
                    if (mine) Sfx.Play(Snd.Bow, 0.28f);
                    InkArrow(from + n * 0.2f, to, ink, rim, 0.52f * grow, 0.13f + dir.magnitude * 0.03f, () =>
                    {
                        if (mine) Sfx.Play(Snd.ArrowHit, 0.22f);
                        StuckArrow(to, n, ink, 0.52f * grow);
                        Impact(to, 0.36f * grow, Pal.Ink.A(0.65f), rim, 1);
                        Kick(to, n, 0.08f);
                    });
                    break;
                case UnitKind.Qi:
                    if (mine) Sfx.Play(Snd.Horse, 0.32f);
                    Charge(from, to, 0.85f * (0.9f + lv * 0.06f), Color.Lerp(Color.white, lvc, lv > 1 ? 0.25f : 0f), () =>
                    {
                        Impact(to, 0.46f * grow, Pal.Ink.A(0.7f), Steel, 2);
                        DustPuff(to + Vector2.down * 0.15f, 0.45f);
                        Kick(to, n, 0.28f);
                    });
                    break;
            }
        }

        // ─────────────────────────── 好汉出手 ───────────────────────────

        enum HeroSwing { Arc, Thrust, Loose, Hurl, Whip, Fist }

        /// <summary>普攻兵器：优先用这名好汉自己的兵器，没有兵器图时按出手样子挑。</summary>
        static string HeroWeaponSprite(HeroDef d)
        {
            string named = d.WKind switch
            {
                WeaponKind.Blade => "wp_blade",
                WeaponKind.Spear => "wp_spear",
                WeaponKind.Bow => "wp_bow",
                WeaponKind.Axe => "wp_axe",
                WeaponKind.Club => "wp_club",
                WeaponKind.Whip => "wp_whip",
                WeaponKind.Hammer => "wp_hammer",
                WeaponKind.Fork => "wp_fork",
                WeaponKind.Sword => "wp_sword",
                WeaponKind.Stone => "wp_stone",
                WeaponKind.Rope => "wp_rope",
                WeaponKind.Staff => "wp_staff",
                WeaponKind.Halberd => "wp_halberd",
                WeaponKind.Cannon => "wp_cannon",
                WeaponKind.Fist => "wp_fist",
                _ => null,
            };
            if (named != null) return named;
            return d.Look switch
            {
                Look.Spear => "wp_spear",
                Look.Bow => "wp_bow",
                Look.Axe => "wp_axe",
                Look.Staff => "wp_staff",
                Look.Whip => "wp_whip",
                Look.Hammer => "wp_hammer",
                Look.Fork => "wp_fork",
                Look.Sword => "wp_sword",
                Look.Dart => "wp_stone",
                Look.Lasso => "wp_rope",
                Look.Fist => "wp_fist",
                Look.Fire => "wp_cannon",
                _ => "wp_blade",
            };
        }

        static HeroSwing SwingOf(HeroDef d)
        {
            switch (d.WKind)
            {
                case WeaponKind.Spear:
                case WeaponKind.Fork: return HeroSwing.Thrust;
                case WeaponKind.Bow:
                case WeaponKind.Cannon: return HeroSwing.Loose;
                case WeaponKind.Stone: return HeroSwing.Hurl;
                case WeaponKind.Whip: return HeroSwing.Whip;
                case WeaponKind.Rope: return HeroSwing.Whip;
                case WeaponKind.Fist: return HeroSwing.Fist;
                case WeaponKind.Relic:
                case WeaponKind.None:
                    return d.Look switch
                    {
                        Look.Spear or Look.Fork => HeroSwing.Thrust,
                        Look.Bow or Look.Fire => HeroSwing.Loose,
                        Look.Dart => HeroSwing.Hurl,
                        Look.Whip or Look.Lasso => HeroSwing.Whip,
                        Look.Fist => HeroSwing.Fist,
                        _ => HeroSwing.Arc,
                    };
                default: return HeroSwing.Arc;
            }
        }

        /// <summary>兵器从好汉身上挥到目标。图是斜着画的，减 45° 才对准出手方向。</summary>
        void ShowHeroWeapon(HeroDef d, Vector2 from, Vector2 to, Vector2 n)
        {
            if ((d.WKind == WeaponKind.Relic || d.WKind == WeaponKind.None) && d.Look == Look.Poison)
            {
                HurlWeapon("fx_dust", from, to, 0.8f);
                return;
            }
            string img = HeroWeaponSprite(d);
            switch (SwingOf(d))
            {
                case HeroSwing.Thrust: ThrustWeapon(img, from, to, n, 1.9f); break;
                case HeroSwing.Loose: LooseWeapon(img, from, to, n, d.WKind == WeaponKind.Cannon || d.Look == Look.Fire); break;
                case HeroSwing.Hurl: HurlWeapon(img, from, to, 0.85f); break;
                case HeroSwing.Whip: WhipWeapon(img, from, to, n); break;
                case HeroSwing.Fist: FistWeapon(img, from, to, n); break;
                default: ArcWeapon(img, from, to, n); break;
            }
        }

        const int WeaponOrder = 48;

        void ArcWeapon(string img, Vector2 from, Vector2 to, Vector2 n)
        {
            float ang = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg;
            var start = from + n * 0.15f;
            var end = Vector2.Lerp(from, to, 0.78f);
            const float size = 1.75f;
            NewSpriteFx(Art.Img(img), Color.white, start, Vector2.one * size, WeaponOrder, 0.38f, (f, k) =>
            {
                float e = Tween.OutCubic(k);
                f.Go.transform.localPosition = Vector2.Lerp(start, end, e);
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, ang - 45f + Mathf.Lerp(80f, -110f, e));
                f.Sr.color = Color.white.A(Mathf.Clamp01(Mathf.Min(k / 0.08f, (1f - k) / 0.28f)));
            });
            Streak(start, end, 0.15f, 0.22f, Pal.GoldBright.A(0.9f), Color.white, 0.26f, WeaponOrder - 1);
        }

        void ThrustWeapon(string img, Vector2 from, Vector2 to, Vector2 n, float size)
        {
            float ang = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg - 45f;
            var start = from + n * 0.1f;
            var end = to - n * Mathf.Min(0.35f, (to - from).magnitude * 0.25f);
            NewSpriteFx(Art.Img(img), Color.white, start, Vector2.one * size, WeaponOrder, 0.32f, (f, k) =>
            {
                float e = Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI);
                f.Go.transform.localPosition = Vector2.Lerp(start, end, e);
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, ang);
                f.Sr.color = Color.white.A(Mathf.Clamp01(Mathf.Min(k / 0.08f, (1f - k) / 0.22f)));
            });
            Streak(start, end, 0.2f, 0.16f, Pal.GoldBright.A(0.75f), Color.white, 0.22f, WeaponOrder - 1, 0f);
        }

        void LooseWeapon(string img, Vector2 from, Vector2 to, Vector2 n, bool cannon)
        {
            float ang = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg - 45f;
            var at = from + n * 0.35f;
            const float size = 1.3f;
            NewSpriteFx(Art.Img(img), Color.white, at, Vector2.one * size, WeaponOrder, 0.34f, (f, k) =>
            {
                float draw = 1f - 0.22f * Mathf.Sin(Mathf.Min(1f, k * 1.8f) * Mathf.PI);
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, ang);
                f.Go.transform.localScale = new Vector3(size * draw, size, 1);
                f.Sr.color = Color.white.A(Mathf.Clamp01(Mathf.Min(k / 0.08f, (1f - k) / 0.25f)));
            });
            if (cannon) HurlWeapon("fx_fire", from + n * 0.5f, to, 0.7f);
        }

        void HurlWeapon(string img, Vector2 from, Vector2 to, float size)
        {
            float dist = (to - from).magnitude;
            float life = Mathf.Clamp(0.14f + dist * 0.045f, 0.16f, 0.42f);
            float arc = Mathf.Min(0.55f, dist * 0.12f);
            NewSpriteFx(Art.Img(img), Color.white, from, Vector2.one * size, WeaponOrder, life, (f, k) =>
            {
                var p = Vector2.Lerp(from, to, k) + Vector2.up * Mathf.Sin(k * Mathf.PI) * arc;
                f.Go.transform.localPosition = p;
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, k * 360f);
                f.Sr.color = Color.white.A(k < 0.85f ? 1f : (1f - k) / 0.15f);
            });
        }

        void WhipWeapon(string img, Vector2 from, Vector2 to, Vector2 n)
        {
            float ang = Mathf.Atan2(n.y, n.x) * Mathf.Rad2Deg - 45f;
            NewSpriteFx(Art.Img(img), Color.white, from + n * 0.25f, Vector2.one * 1.25f, WeaponOrder, 0.28f, (f, k) =>
            {
                f.Go.transform.localRotation = Quaternion.Euler(0, 0, ang + Mathf.Sin(k * Mathf.PI) * 35f);
                f.Sr.color = Color.white.A(Mathf.Clamp01(Mathf.Min(k / 0.08f, (1f - k) / 0.25f)));
            });
            Streak(from + n * 0.3f, to, 0.1f, 0.18f, Pal.Ink, Pal.GoldBright, 0.22f, WeaponOrder - 1);
        }

        void FistWeapon(string img, Vector2 from, Vector2 to, Vector2 n)
        {
            for (int i = 0; i < 2; i++)
            {
                var off = new Vector2(i == 0 ? -0.12f : 0.12f, 0f);
                After(i * 0.08f, () => HurlWeapon(img, from + n * 0.15f + off, to + off, 0.8f));
            }
        }

        void LungeHero(int heroId, Vector2 dir)
        {
            foreach (var v in _pieces.Values)
                if (v.Hero != null && v.Hero.Id == heroId) v.Lunge(dir);
        }

        void HeroAttackFx(SimEvent ev)
        {
            var from = World(ev.Side, ev.P);
            var to = World(ev.Side, ev.Q);
            var id = (HeroId)ev.B;
            var dir = to - from;
            var n = dir.sqrMagnitude > 1e-4f ? dir.normalized : Vector2.up;
            bool mine = ev.Side == 0;
            var hd = HeroDef.Get(id);
            var gold = Pal.GoldBright;
            var glow = Color.Lerp(Pal.GoldBright, Color.white, 0.45f);
            ShowHeroWeapon(hd, from, to, n);
            LungeHero(ev.A, n);

            if (ev.Flag == 2)
            {
                InkArrow(from, to, Pal.Red, Pal.GoldBright, 1.1f, 0.24f, () =>
                {
                    StuckArrow(to, n, Pal.Red, 1.0f);
                    Impact(to, 1.3f, Pal.RedDeep.A(0.85f), Pal.GoldBright, 6);
                    Ink("fx_crack", Pal.RedDeep.A(0.7f), to, 1.4f, 0.7f, Rnd(0, 360), 31, 0.5f, 0f, 0.5f);
                    Ring(to, 2.2f, Pal.Red, 0.45f, 46);
                    Kick(to, n, 0.35f);
                    if (mine) { _shake = Mathf.Max(_shake, 0.8f); HitStop(0.08f); Sfx.Play(Snd.Crit, 0.6f); }
                });
                if (mine) Sfx.Play(Snd.Bow, 0.6f, 0.85f);
                if (_frames.TryGetValue(ev.A, out var uf)) uf.Pulse();
                return;
            }

            switch (hd.Look)
            {
                case Look.Fist:
                {
                    if (mine) Sfx.Play(Snd.Fist, 0.45f);
                    for (int i = 0; i < 2; i++)
                    {
                        var off = new Vector2(Rnd(-0.18f, 0.18f), Rnd(-0.18f, 0.18f));
                        After(i * 0.08f, () =>
                        {
                            Streak(to - n * 0.8f + off, to + off, 0.1f, 0.18f, Pal.Ink, glow, 0.15f);
                            Impact(to + off, 0.68f, Pal.Ink.A(0.8f), gold, 2);
                            Kick(to, n, 0.1f);
                        });
                    }
                    break;
                }
                case Look.Dart:
                {
                    bool crit = ev.Flag == 1;
                    if (mine) Sfx.Play(hd.WKind == WeaponKind.Stone ? Snd.Stone : Snd.Dart, 0.4f);
                    Streak(to - n * 0.7f, to, 0.1f, crit ? 0.2f : 0.13f, Pal.Ink, glow, 0.15f);
                    if (crit)
                    {
                        Impact(to, 1.05f, Pal.RedDeep.A(0.85f), gold, 4);
                        FloatText(to + new Vector2(0, 0.7f), "暴击！", Pal.Red, 0.4f, 0.7f);
                        Kick(to, n, 0.3f);
                        if (mine) { _shake = Mathf.Max(_shake, 0.4f); HitStop(0.06f); Sfx.Play(Snd.Crit, 0.5f); }
                    }
                    else
                    {
                        Impact(to, 0.5f, Pal.Ink.A(0.75f), glow, 1);
                        Kick(to, n, 0.1f);
                    }
                    break;
                }
                case Look.Spear:
                {
                    if (mine) Sfx.Play(Snd.Spear, 0.5f, 0.85f);
                    var tip = to + n * 1.4f;
                    Streak(from + n * 0.75f, to, 1.4f, 0.1f, glow.A(0.85f), glow, 0.2f, 43, 0f);
                    After(0.05f, () =>
                    {
                        foreach (var v in _enemies.Values)
                        {
                            var p = (Vector2)v.Root.localPosition;
                            var rel = p - from;
                            float along = Vector2.Dot(rel, n);
                            if (along < 0f || along > (tip - from).magnitude) continue;
                            if ((rel - n * along).sqrMagnitude > 0.25f) continue;
                            v.Kick(n, 0.18f);
                            Ink("fx_lines", gold, p, 0.75f, 0.2f, Ang(n), 46, 0.5f);
                        }
                    });
                    break;
                }
                case Look.Axe:
                {
                    if (mine) Sfx.Play(Snd.Axe, 0.45f);
                    Crescent(to + new Vector2(-0.15f, 0), n, 1.15f, Pal.Ink, glow, false, 0.24f);
                    After(0.07f, () => Crescent(to + new Vector2(0.15f, 0), n, 1.15f, Pal.Ink, glow, true, 0.24f));
                    After(0.08f, () =>
                    {
                        Ink("fx_cross", Pal.Ink.A(0.8f), to, 0.95f, 0.3f, Rnd(-15, 15), 44, 0.4f);
                        InkDrops(to, 4);
                        foreach (var v in _enemies.Values)
                        {
                            var p = (Vector2)v.Root.localPosition;
                            if ((p - to).sqrMagnitude < 1.44f) v.Kick((p - to).sqrMagnitude > 1e-3f ? (p - to).normalized : n, 0.14f);
                        }
                        if (mine) { _shake = Mathf.Max(_shake, 0.3f); HitStop(0.03f); }
                    });
                    break;
                }
                case Look.Bow:
                    if (mine) Sfx.Play(hd.WKind == WeaponKind.Stone ? Snd.Stone : Snd.Bow, 0.45f);
                    InkArrow(from, to, Pal.Hex("8A1C1C"), gold, 0.8f, 0.18f, () =>
                    {
                        if (mine) Sfx.Play(Snd.ArrowHit, 0.4f);
                        StuckArrow(to, n, Pal.Hex("8A1C1C"), 0.75f);
                        Impact(to, 0.7f, Pal.RedDeep.A(0.8f), gold, 3);
                        Kick(to, n, 0.18f);
                    });
                    break;
                case Look.Fire:
                {
                    if (mine) Sfx.Play(Snd.FireHit, 0.45f);
                    Streak(to + new Vector2(0, 1.6f), to, 0.05f, 0.22f, Pal.Ink, Fire, 0.16f);
                    After(0.06f, () =>
                    {
                        GroundSmash(to, 1.5f, Pal.Ink, mine);
                        FireBurst(to, 0.9f);
                    });
                    break;
                }
                case Look.Water:
                    if (mine) Sfx.Play(Snd.Water, 0.4f);
                    WaveTo(from, to, 1.45f, () =>
                    {
                        Splash(to, 0.95f);
                        Kick(to, n, 0.15f);
                    });
                    break;
                case Look.WaterFire:
                    if (mine) Sfx.Play(Snd.Water, 0.4f, 0.9f);
                    WaveTo(from, to, 1.55f, () =>
                    {
                        Splash(to, 1.1f);
                        FireBurst(to, 0.6f);
                        Kick(to, n, 0.15f);
                    });
                    break;
                case Look.Fork:
                    if (mine) Sfx.Play(Snd.Water, 0.35f, 1.15f);
                    Streak(from + n * 0.3f, to, 0.4f, 0.17f, WaterDeep, Ice, 0.2f);
                    After(0.05f, () =>
                    {
                        Splash(to, 0.75f);
                        Kick(to, n, 0.12f);
                    });
                    break;
                case Look.Poison:
                    if (mine) Sfx.Play(Snd.Poison, 0.4f);
                    Crescent(to, n, 1.0f, Pal.Hex("5A1A12"), Pal.Hex("F2C6B8"), UnityEngine.Random.value < 0.5f);
                    After(0.05f, () =>
                    {
                        Impact(to, 0.6f, Pal.RedDeep.A(0.75f), Pal.Hex("F2C6B8"), 2);
                        Ink("fx_dust", Pal.Purple.A(0.35f), to, 0.8f, 0.6f, Rnd(0, 360), 30, 0.4f);
                        Kick(to, n, 0.12f);
                    });
                    break;
                case Look.Lasso:
                    if (mine) Sfx.Play(Snd.Lasso, 0.4f);
                    Lasso(from, to, Pal.Red);
                    After(0.15f, () => Kick(to, -n, 0.12f));
                    break;
                case Look.Staff:
                {
                    if (mine) Sfx.Play(Snd.Smash, 0.5f);
                    Crescent(to, n, 1.6f, Pal.Ink, glow, false, 0.3f);
                    After(0.08f, () =>
                    {
                        GroundSmash(to, 1.6f, Pal.Ink, mine);
                        Impact(to, 0.85f, Pal.Ink.A(0.8f), gold, 3);
                        foreach (var v in _enemies.Values)
                        {
                            var p = (Vector2)v.Root.localPosition;
                            if ((p - to).sqrMagnitude < 1.1f) v.Kick((p - to).sqrMagnitude > 1e-3f ? (p - to).normalized : n, 0.22f);
                        }
                    });
                    break;
                }
                case Look.Sword:
                {
                    if (mine) Sfx.Play(Snd.Sword, 0.42f);
                    bool m = UnityEngine.Random.value < 0.5f;
                    Crescent(to, n, 0.9f, Pal.Hex("2B3A55"), Color.white, m, 0.18f);
                    After(0.06f, () => Crescent(to, n, 0.8f, Pal.Hex("2B3A55"), glow, !m, 0.18f));
                    After(0.08f, () => { Ink("fx_lines", Color.white, to, 0.8f, 0.16f, Ang(n), 45, 0.5f); Kick(to, n, 0.1f); });
                    break;
                }
                case Look.Whip:
                {
                    if (mine) Sfx.Play(Snd.Whip, 0.45f);
                    Streak(to + new Vector2(-n.y, n.x) * 0.9f + Vector2.up * 0.6f, to, 0.1f, 0.2f, Pal.Ink, Steel, 0.16f);
                    After(0.06f, () =>
                    {
                        Impact(to, 0.7f, Pal.Ink.A(0.8f), Steel, 2);
                        Ink("fx_crack", Pal.Ink.A(0.55f), to, 0.8f, 0.4f, Rnd(0, 360), 31, 0.5f, 0f, 0.4f);
                        Kick(to, n, 0.12f);
                    });
                    break;
                }
                case Look.Hammer:
                {
                    if (mine) Sfx.Play(Snd.Hammer, 0.5f);
                    Streak(to + new Vector2(0, 1.4f), to, 0.05f, 0.3f, Pal.Ink, Steel, 0.14f);
                    After(0.06f, () =>
                    {
                        GroundSmash(to, 1.4f, Pal.Ink, mine);
                        Impact(to, 0.8f, Pal.Ink.A(0.85f), gold, 3);
                        Kick(to, n, 0.25f);
                    });
                    break;
                }
                case Look.Magic:
                {
                    if (mine) Sfx.Play(Snd.UltMagic, 0.25f, 1.3f);
                    Ink("fx_enso", Pal.Purple.A(0.8f), to, 1.1f, 0.45f, Rnd(0, 360), 43, 0.3f, 240f, 0.4f);
                    After(0.05f, () =>
                    {
                        Streak(to + new Vector2(Rnd(-0.3f, 0.3f), 2.2f), to, 0f, 0.14f, Pal.Hex("C9B6FF"), Color.white, 0.14f, 46);
                        Impact(to, 0.65f, Pal.Purple.A(0.7f), Pal.Hex("E6DAFF"), 2);
                    });
                    break;
                }
                default:
                    if (mine) Sfx.Play(Snd.Dao, 0.4f, 0.9f);
                    Crescent(to, n, 1.05f, ev.Flag == 1 ? Pal.Red : Pal.Ink, glow, false);
                    After(0.05f, () => Impact(to, 0.6f, Pal.Ink.A(0.8f), gold, 2));
                    break;
            }
            if (ev.Flag == 1 && hd.Look != Look.Dart)
            {
                After(0.06f, () => Impact(to, 1.0f, Pal.RedDeep.A(0.85f), gold, 4));
                FloatText(to + new Vector2(0, 0.7f), "暴击！", Pal.Red, 0.4f, 0.7f);
                if (mine) { HitStop(0.05f); Sfx.Play(Snd.Crit, 0.45f); }
            }
            if (mine && M.Player.Armed(hd) && hd.WeaponTag.Length > 0 && UnityEngine.Random.value < 0.3f)
                FloatText(from + new Vector2(0, 0.95f), "用" + hd.WeaponTag, Ui.GradeColor(hd.WGrade), 0.34f, 0.7f);
            if (_frames.TryGetValue(ev.A, out var fv)) fv.Pulse();
        }

        // ─────────────────────────── 好汉大招 ───────────────────────────

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
                        case Look.Hammer: case Look.Whip: case Look.Staff: return Snd.UltSmash;
                        default: return Snd.UltWhirl;
                    }
                case UltKind.Snipe: return Snd.Bow;
                case UltKind.Blast: return d.WKind == WeaponKind.Cannon ? Snd.UltCannon : Snd.UltFire;
                case UltKind.Pin: return water ? Snd.UltWater : Snd.Spear;
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

        static readonly Color Thunder = Pal.Hex("D9CCFF");

        /// <summary>大招按绝招类型和出手样子定制；返回 false 时走通用墨旋风。</summary>
        bool HeroUltFx(SimEvent ev)
        {
            var d = HeroDef.Get((HeroId)ev.B);
            var pos = World(ev.Side, ev.P);
            float r = Mathf.Clamp(ev.Val, 1f, 6f);
            bool mine = ev.Side == 0;
            var targets = new List<Vector2>(EnemiesWithin(ev.Side, ev.P, r, 12));
            bool water = d.Look == Look.Water || d.Look == Look.WaterFire || d.Look == Look.Fork;
            switch (d.UltKind)
            {
                case UltKind.Combo:
                {
                    Enemy front = null;
                    foreach (var e in M.Sides[ev.Side].Enemies)
                        if (!e.Dead && (front == null || e.Progress > front.Progress)) front = e;
                    var at = front != null ? World(ev.Side, front.Pos) : pos;
                    int hits = Mathf.Clamp(d.UltN, 4, 12);
                    var hitSnd = d.Look == Look.Fist ? Snd.Fist : d.Look == Look.Sword ? Snd.Sword : Snd.Dao;
                    for (int i = 0; i < hits; i++)
                    {
                        int idx = i;
                        bool last = i == hits - 1;
                        After(i * 0.06f, () =>
                        {
                            var off = new Vector2(Rnd(-0.3f, 0.3f), Rnd(-0.3f, 0.3f));
                            if (d.Look != Look.Fist && !last) Crescent(at + off, new Vector2(Rnd(-1f, 1f), Rnd(-1f, 1f)).normalized, 0.8f, Pal.Ink, Steel, idx % 2 == 0, 0.16f);
                            Impact(at + off, last ? 1.8f : 0.7f, Pal.Ink.A(0.85f), Pal.GoldBright, last ? 10 : 1);
                            if (mine && idx % 2 == 0 && !last) Sfx.Play(hitSnd, 0.35f, 1.1f + idx * 0.02f);
                            if (mine && last) { Sfx.Play(Snd.Crit, 0.7f, 0.9f); _shake = Mathf.Max(_shake, 0.9f); HitStop(0.08f); }
                        });
                    }
                    return true;
                }
                case UltKind.Area:
                    if (water) { TideFx(pos, r, targets, mine); return true; }
                    switch (d.Look)
                    {
                        case Look.Spear when d.UltStun > 0: IceFx(pos, r, targets); return true;
                        case Look.Poison: SleepFx(pos, r, targets); return true;
                        case Look.Fire: MeteorFx(pos, r, targets, mine); return true;
                        case Look.Magic: StormFx(targets, mine, pos, r); return true;
                        case Look.Hammer: case Look.Whip: case Look.Staff:
                            QuakeFx(pos, r, targets, mine); return true;
                        default:
                            WhirlFx(pos, r, targets, mine, Weapons.Icon(d.WKind)); return true;
                    }
                case UltKind.Blast:
                    MeteorFx(pos, Mathf.Max(r, 1.6f), targets, mine);
                    return true;
                case UltKind.Pin:
                {
                    var t = targets.Count > 0 ? targets[0] : pos;
                    if (water)
                    {
                        Streak(pos, t, 0.6f, 0.45f, WaterDeep, Ice, 0.3f, 46);
                        After(0.08f, () =>
                        {
                            Splash(t, 1.5f);
                            Ink("fx_lasso", WaterDeep.A(0.8f), t, 1.0f, 1.0f, 0, 45, 0.8f, 180f, 0.6f);
                            if (mine) HitStop(0.06f);
                        });
                    }
                    else
                    {
                        Streak(pos, t, 0.8f, 0.4f, Pal.Ink, Pal.GoldBright, 0.28f, 46);
                        After(0.08f, () =>
                        {
                            Impact(t, 1.5f, Pal.RedDeep.A(0.85f), Pal.GoldBright, 6);
                            Ink("fx_crack", Pal.Ink.A(0.8f), t, 1.2f, 1.0f, Rnd(0, 360), 31, 0.5f, 0f, 0.6f);
                            if (mine) { HitStop(0.07f); _shake = Mathf.Max(_shake, 0.7f); }
                        });
                    }
                    return true;
                }
                case UltKind.Lasso:
                {
                    var c = d.Look == Look.Lasso ? Pal.Red : d.Look == Look.Water || d.Look == Look.Fork ? WaterDeep : Pal.Hex("6B4A2B");
                    for (int i = 0; i < Mathf.Min(Mathf.Max(1, d.UltN), targets.Count); i++) Lasso(pos, targets[i], c, i * 0.07f);
                    return true;
                }
                case UltKind.Throw:
                {
                    var t = targets.Count > 0 ? targets[0] : pos;
                    Impact(t, 1.8f, Pal.Ink.A(0.85f), Pal.GoldBright, 8);
                    GroundSmash(t, 1.6f, Pal.Ink, mine);
                    return true;
                }
                case UltKind.SlowAll:
                {
                    float baseY = mine ? MyBottom : OppBottom;
                    for (int i = 0; i < 14; i++)
                    {
                        var start = new Vector2(Left - 1f - Rnd(0f, 3f), baseY + Rnd(0.3f, Board.Rows - 0.3f));
                        var fx = NewSpriteFx(Art.Img("fx_thrust"), Ice.A(0.8f), start, new Vector2(Rnd(1.2f, 2f), 0.45f), 46, Rnd(0.6f, 0.9f), (f, k) =>
                        {
                            f.Go.transform.localPosition = f.A + Vector2.right * k * 12f;
                            f.Sr.color = f.C.A(0.8f * Mathf.Sin(k * Mathf.PI));
                        });
                        fx.A = start;
                    }
                    return true;
                }
                case UltKind.HasteAll:
                case UltKind.Upgrade:
                {
                    float rr = d.UltKind == UltKind.Upgrade ? 1.2f : 2.4f;
                    Ink("fx_enso", Pal.GoldBright.A(0.85f), pos, rr * 1.6f, 1.0f, 0, 44, 0.3f, 360f, 0.6f);
                    Ring(pos, rr * 2f, Pal.GoldBright, 0.7f, 45);
                    if (d.UltKind == UltKind.Upgrade) Ink("ui_sparkle", Color.white, pos, 1.3f, 0.7f, 0, 47, 0.3f, 120f, 0.4f);
                    return true;
                }
                case UltKind.Volley:
                    Ring(pos, 1.6f, Pal.GoldBright, 0.4f, 45);
                    return true;
                case UltKind.Storm:
                {
                    var all = new List<Vector2>(EnemiesWithin(ev.Side, ev.P, 99f, 14));
                    StormFx(all, mine, pos, 3f);
                    return true;
                }
                case UltKind.Heal:
                {
                    Ink("fx_enso", Pal.Willow.A(0.8f), pos, 1.8f, 0.9f, 0, 44, 0.3f, 200f, 0.5f);
                    for (int i = 0; i < 8; i++)
                    {
                        var p = pos + new Vector2(Rnd(-0.6f, 0.6f), Rnd(-0.3f, 0.2f));
                        var fx = NewSpriteFx(Art.Circle, Pal.Hex("9FE07A"), p, Vector2.one * Rnd(0.1f, 0.18f), 47, Rnd(0.7f, 1.1f), (f, k) =>
                        {
                            f.Go.transform.localPosition = f.A + Vector2.up * k * 1.2f;
                            f.Sr.color = f.C.A(1f - k);
                        });
                        fx.A = p;
                    }
                    return true;
                }
                case UltKind.Rally:
                case UltKind.Shield:
                {
                    var goal = M.Sides[ev.Side].Board.Goal;
                    var gp = CellWorld(ev.Side, goal.c, goal.r);
                    for (int i = 0; i < 3; i++)
                    {
                        int idx = i;
                        After(i * 0.18f, () => Ring(gp, 1.6f + idx * 0.9f, Pal.Red, 0.5f, 46));
                    }
                    Ink("fx_enso", Pal.GoldBright.A(0.9f), gp, 2.2f, 1.1f, 0, 44, 0.3f, -300f, 0.6f);
                    if (d.UltKind == UltKind.Rally) Ink("fx_enso", Pal.Red.A(0.6f), pos, 2.6f, 1.0f, 0, 43, 0.3f, 300f, 0.6f);
                    if (mine) _shake = Mathf.Max(_shake, 0.6f);
                    return true;
                }
                case UltKind.Steal:
                    Ink("fx_dust", Pal.InkSoft.A(0.6f), pos, 1.2f, 0.6f, Rnd(0, 360), 40, 0.3f, 90f, 0.4f);
                    return true;
            }
            return false;
        }

        void IceFx(Vector2 pos, float r, List<Vector2> targets)
        {
            Ink("fx_enso", Ice, pos, r * 2f, 0.9f, Rnd(0, 360), 44, 0.3f, -300f, 0.5f);
            for (int i = 0; i < 26; i++)
            {
                var start = pos + new Vector2(Rnd(-r, r), Rnd(0.5f, r + 1f));
                var end = start + new Vector2(Rnd(-0.3f, 0.3f), -Rnd(1.2f, 2.4f));
                var fx = NewSpriteFx(Art.Circle, Color.white, start, Vector2.one * Rnd(0.08f, 0.16f), 47, Rnd(0.8f, 1.3f), (f, k) =>
                {
                    f.Go.transform.localPosition = Vector2.Lerp(f.A, f.B, k) + new Vector2(Mathf.Sin(k * 9f + f.F) * 0.1f, 0);
                    f.Sr.color = Color.white.A(k < 0.8f ? 0.95f : (1f - k) * 4.75f);
                });
                fx.A = start;
                fx.B = end;
                fx.F = Rnd(0, 6);
            }
            foreach (var t in targets) Ink("fx_crack", Ice.A(0.9f), t, 0.8f, 1.2f, Rnd(0, 360), 36, 0.4f, 0f, 0.7f);
        }

        /// <summary>兵器旋风：本人的兵器图标绕身飞转一圈砍过去。</summary>
        void WhirlFx(Vector2 pos, float r, List<Vector2> targets, bool mine, string icon)
        {
            Ink("fx_enso", Pal.Ink.A(0.75f), pos, r * 2f, 0.9f, Rnd(0, 360), 43, 0.3f, -720f, 0.6f);
            var sprite = Art.Img(icon);
            for (int i = 0; i < 6; i++)
            {
                float a0 = i * 60f;
                NewSpriteFx(sprite, Color.white, pos, Vector2.one * 0.9f, 47, 0.9f, (f, k) =>
                {
                    float rad = r * 0.75f * Mathf.Min(1f, k * 3f);
                    float a = (a0 - k * 720f) * Mathf.Deg2Rad;
                    f.Go.transform.localPosition = pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * rad;
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, -k * 1800f);
                    f.Sr.color = Color.white.A(k < 0.8f ? 1f : (1f - k) * 5f);
                });
            }
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                After(0.1f + i * 0.05f, () => { Impact(t, 0.7f, Pal.Ink.A(0.8f), Steel, 2); Kick(t, (t - pos).normalized, 0.2f); });
            }
            if (mine) _shake = Mathf.Max(_shake, 0.7f);
        }

        void MeteorFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            int count = Mathf.Max(3, Mathf.Min(6, targets.Count));
            for (int i = 0; i < count; i++)
            {
                var t = i < targets.Count ? targets[i] : pos + UnityEngine.Random.insideUnitCircle * r * 0.6f;
                int idx = i;
                After(i * 0.09f, () =>
                {
                    Streak(t + new Vector2(Rnd(-0.4f, 0.4f), 2.5f), t, 0f, 0.3f, FireCore, Color.white, 0.18f, 46);
                    FireBurst(t, 1.2f);
                    Ink("fx_crack", Pal.Ink.A(0.8f), t, 1.3f, 0.8f, Rnd(0, 360), 31, 0.5f, 0f, 0.6f);
                    if (mine && idx == 0) HitStop(0.07f);
                });
            }
            if (mine) _shake = Mathf.Max(_shake, 1.1f);
        }

        void TideFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i / 5f * Mathf.PI * 2f;
                var p = pos + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.6f;
                bool flip = Mathf.Cos(a) < 0;
                After(i * 0.05f, () =>
                {
                    NewSpriteFx(Art.Img("fx_wave"), WaterDeep.A(0.9f), p, Vector2.one, 45, 0.7f, (f, k) =>
                    {
                        float s = 1.7f * Mathf.Sin(Mathf.Min(1f, k * 1.4f) * Mathf.PI * 0.5f);
                        f.Go.transform.localScale = new Vector3(flip ? -1.7f : 1.7f, Mathf.Max(0.01f, s), 1);
                        f.Sr.color = f.C.A(k < 0.6f ? 0.9f : (1f - k) / 0.4f * 0.9f);
                    });
                    Splash(p, 0.8f);
                });
            }
            foreach (var t in targets) Kick(t, (t - pos).sqrMagnitude > 1e-3f ? (t - pos).normalized : Vector2.up, 0.3f);
            if (mine) _shake = Mathf.Max(_shake, 0.8f);
        }

        void SleepFx(Vector2 pos, float r, List<Vector2> targets)
        {
            for (int i = 0; i < 9; i++)
            {
                var p = pos + UnityEngine.Random.insideUnitCircle * r * 0.8f;
                After(i * 0.04f, () => Ink("fx_dust", Pal.Purple.A(0.4f), p, Rnd(1.0f, 1.6f), 1.3f, Rnd(0, 360), 37, 0.3f, Rnd(-60, 60), 0.5f));
            }
            foreach (var t in targets)
            {
                var at = t;
                After(0.3f, () => FloatText(at + new Vector2(0.25f, 0.45f), "Zz", Pal.Purple, 0.3f, 1.4f));
            }
        }

        /// <summary>重兵器震地：中心大裂，四周一圈尘浪向外推。</summary>
        void QuakeFx(Vector2 pos, float r, List<Vector2> targets, bool mine)
        {
            GroundSmash(pos, r * 1.1f, Pal.Ink, mine);
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                After(i * 0.1f, () => Ring(pos, r * (0.8f + idx * 0.6f), Dust.A(0.8f), 0.4f, 44));
            }
            foreach (var t in targets)
            {
                var at = t;
                After(0.12f, () => { Impact(at, 0.7f, Pal.Ink.A(0.8f), Steel, 2); Kick(at, (at - pos).sqrMagnitude > 1e-3f ? (at - pos).normalized : Vector2.up, 0.28f); });
            }
            if (mine) { _shake = Mathf.Max(_shake, 1.0f); HitStop(0.06f); }
        }

        /// <summary>五雷天罡：紫电一道道劈到敌人头上。</summary>
        void StormFx(List<Vector2> targets, bool mine, Vector2 pos, float r)
        {
            Ink("fx_enso", Pal.Purple.A(0.6f), pos, r * 1.6f, 1.0f, 0, 42, 0.3f, 360f, 0.6f);
            int count = Mathf.Max(4, targets.Count);
            for (int i = 0; i < count; i++)
            {
                var t = i < targets.Count ? targets[i] : pos + UnityEngine.Random.insideUnitCircle * r;
                int idx = i;
                After(i * 0.06f, () =>
                {
                    var top = t + new Vector2(Rnd(-0.5f, 0.5f), 3.2f);
                    var mid = Vector2.Lerp(top, t, 0.5f) + new Vector2(Rnd(-0.35f, 0.35f), 0);
                    Streak(top, mid, 0f, 0.16f, Thunder, Color.white, 0.16f, 47, 1.6f);
                    Streak(mid, t, 0f, 0.16f, Thunder, Color.white, 0.16f, 47, 1.6f);
                    Impact(t, 0.9f, Pal.Purple.A(0.75f), Thunder, 2);
                    if (mine && idx == 0) { HitStop(0.06f); _shake = Mathf.Max(_shake, 0.9f); }
                });
            }
        }

        // ─────────────────────────── 截图验收 ───────────────────────────

        static readonly V2 ShowFrom = new V2(2.5f, 1.5f);
        static readonly V2 ShowTo = new V2(5.2f, 3.4f);

        public void ShowcaseUnit(UnitKind kind, int level) =>
            UnitAttackFx(new SimEvent { Type = Ev.Attack, Side = 0, A = -1, B = (int)kind, P = ShowFrom, Q = ShowTo, Val = level });

        public void ShowcaseHero(HeroId id, bool crit) =>
            HeroAttackFx(new SimEvent { Type = Ev.HeroAttack, Side = 0, A = -1, B = (int)id, P = ShowFrom, Q = ShowTo, Flag = crit ? 1 : 0 });

        public void ShowcaseUlt(HeroId id) =>
            Handle(new SimEvent { Type = Ev.Ult, Side = 0, A = -1, B = (int)id, P = new V2(4f, 3f), Val = 2.4f, Text = HeroDef.Get(id).Ult });

        // ─────────────────────────── 击杀 ───────────────────────────

        /// <summary>敌人被打飞：字被拍飞旋转淡出，地上留下一摊墨渍慢慢褪去。</summary>
        void KillFx(SimEvent ev)
        {
            var pos = World(ev.Side, ev.P);
            float size = ev.Flag == 2 ? 2.4f : ev.Flag == 1 ? 1.4f : 0.85f;
            Ink("fx_burst", Pal.Ink.A(0.8f), pos, size, 0.35f, Rnd(0, 360), 34, 0.3f);
            InkDrops(pos, ev.Flag > 0 ? 9 : 5);
            Ink("fx_burst", Pal.Ink.A(0.22f), pos, size * 0.9f, 2.6f, Rnd(0, 360), 11, 0.6f, 0f, 0.4f);

            if (_enemies.TryGetValue(ev.A, out var view) && view.Root != null)
            {
                var start = (Vector2)view.Root.localPosition;
                var dir = new Vector2(Rnd(-1f, 1f), Rnd(0.6f, 1.2f)).normalized;
                float spin = Rnd(360f, 720f) * (UnityEngine.Random.value < 0.5f ? -1 : 1);
                var tm = Gfx.Text(_fxRoot, view.GlyphText, Fonts.Brush, view.GlyphHeight, view.GlyphColor, start, 47, TextAnchor.MiddleCenter, "corpse");
                var fx = new Fx { Go = tm.gameObject, Tm = tm, Life = 0.55f, A = start, C = view.GlyphColor };
                fx.Step = (f, k) =>
                {
                    f.Go.transform.localPosition = f.A + dir * k * 1.6f + new Vector2(0, -k * k * 1.2f);
                    f.Go.transform.localRotation = Quaternion.Euler(0, 0, spin * k);
                    f.Go.transform.localScale = Vector3.one * (1f + k * 0.4f);
                    f.Tm.color = Color.Lerp(f.C, Pal.Red, Mathf.Min(1f, k * 3f)).A(1f - k * k);
                };
                _fx.Add(fx);
            }
        }
    }
}
