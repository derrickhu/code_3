using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
    public sealed class Enemy
    {
        public int Id;
        public EnemyDef Def;
        public BossDef Boss;
        public string Glyph;
        public float Hp, MaxHp;
        public float Progress;
        public V2 Pos;
        public float StunT, HasteT, SkillT;
        public float RewardMul = 1f;
        public int LeakDamage;
        public bool Dead;

        public bool IsBig => Def.Kind == EnemyKind.Bu || Def.Kind == EnemyKind.Boss;
    }

    /// <summary>十字坡酒店买来的道具，带进下一局。</summary>
    public sealed class Loadout
    {
        public bool Gang;
        public bool Flag;
        public bool Marsh;
        public bool Stele;
        public int MengHan;
        public int TianShu;
    }

    public sealed class Side
    {
        public readonly int Index;
        public readonly Match M;
        public readonly Board Board;
        public readonly bool Mirrored;
        public readonly Piece[] Camp = new Piece[Tuning.CampSlots];
        public readonly List<Enemy> Enemies = new List<Enemy>();
        public List<HeroInstance> Heroes = new List<HeroInstance>();
        public readonly HashSet<HeroId> FormedEver = new HashSet<HeroId>();
        public readonly List<HeroDef> Pool = new List<HeroDef>();
        public readonly Loadout Items;

        public int Buns = Tuning.StartBuns;
        public int RecruitCost = Tuning.StartCost;
        public int Recruits;
        public int Hp = Tuning.MaxHp;
        public int Kills;
        public int Rains;
        /// <summary>击杀收入倍率：对手难度的主要旋钮。</summary>
        public float IncomeMul = 1f;
        float _incomeAcc;
        public float EnemySlowT;
        public float HasteAllT;
        float _steleCd;

        readonly Rng _rng;
        readonly Dictionary<int, float> _unitCd = new Dictionary<int, float>();
        readonly List<KeyValuePair<char, float>> _charWeights = new List<KeyValuePair<char, float>>();
        bool[,] _wuyongAura = new bool[Board.Cols, Board.Rows];
        bool _daizong;
        int _sinceGold;
        HeroInstance _erniang;

        public bool Dead => Hp <= 0;
        public int Hearts => (Hp + Tuning.HeartHp - 1) / Tuning.HeartHp;

        public Side(int index, Match m, bool mirrored, uint seed, Loadout items)
        {
            Index = index;
            M = m;
            Mirrored = mirrored;
            Items = items ?? new Loadout();
            Board = new Board(m.Chapter.Map);
            _rng = new Rng(seed);
            foreach (var id in m.Chapter.Pool) Pool.Add(HeroDef.Get(id));

            var weights = new Dictionary<char, float>();
            foreach (var h in Pool)
                foreach (var ch in h.Name)
                {
                    weights.TryGetValue(ch, out var w);
                    weights[ch] = w + (h.Hidden ? 0.3f : 1f);
                }
            foreach (var kv in weights) _charWeights.Add(kv);
            _charWeights.Sort((a, b) => a.Key.CompareTo(b.Key));

            if (Items.Gang) Buns += 20;
        }

        // ───────────── 玩家 / AI 操作 ─────────────

        public int EmptyCampSlots
        {
            get { int n = 0; foreach (var p in Camp) if (p == null) n++; return n; }
        }

        public bool CanRecruit => !Dead && !M.Over && Buns >= RecruitCost && EmptyCampSlots > 0;

        public bool Recruit()
        {
            if (!CanRecruit) return false;
            Buns -= RecruitCost;
            for (int i = 0; i < Camp.Length; i++)
                if (Camp[i] == null) Camp[i] = Roll();
            Recruits++;
            RecruitCost += Tuning.CostStep;
            Emit(new SimEvent { Type = Ev.Recruit });
            return true;
        }

        Piece Roll()
        {
            bool gold = _sinceGold >= Tuning.GoldPity;
            if (!gold && Board.HasWillow() && _rng.Chance(Tuning.StaffChance)) return Piece.Staff();
            if (gold || _rng.Chance(Tuning.HeroCharChance))
            {
                _sinceGold = 0;
                return Piece.Hero(RollChar());
            }
            _sinceGold++;
            if (_rng.Chance(Tuning.PairBias) && TryPickPairKind(out var kind)) return Piece.Unit(kind);
            return Piece.Unit((UnitKind)_rng.Range(0, 4));
        }

        /// <summary>偏向给出能和现有 1 级兵配对的兵种，避免山寨堵死。</summary>
        bool TryPickPairKind(out UnitKind kind)
        {
            var picked = default(UnitKind);
            int n = 0;
            void Consider(Piece p)
            {
                if (p == null || p.Type != PieceType.Unit || p.Level != 1) return;
                n++;
                if (_rng.Range(0, n) == 0) picked = p.Kind;
            }
            foreach (var p in Camp) Consider(p);
            foreach (var p in Board.Pieces) Consider(p);
            kind = picked;
            return n > 0;
        }

        public static int DismissRefund(Piece p) => p == null || p.Type == PieceType.Chanzhang ? 1 : 2 * p.Level;

        /// <summary>遣散：把字拖到聚义按钮上，返还少量炊饼。</summary>
        public bool DismissCamp(int slot)
        {
            var p = Camp[slot];
            if (p == null || M.Over) return false;
            Camp[slot] = null;
            Refund(p, new V2(-1, -1));
            return true;
        }

        public bool DismissCell(int c, int r)
        {
            if (M.Over || !Board.InBounds(c, r)) return false;
            var p = Board.Pieces[c, r];
            if (p == null) return false;
            Board.Pieces[c, r] = null;
            Refund(p, new CellPos(c, r).Center);
            RecomputeHeroes();
            return true;
        }

        void Refund(Piece p, V2 at)
        {
            int n = DismissRefund(p);
            Buns += n;
            Emit(new SimEvent { Type = Ev.Dismiss, A = p.Id, Val = n, P = at });
        }

        char RollChar()
        {
            float total = 0;
            foreach (var kv in _charWeights) total += kv.Value;
            float x = _rng.Next01() * total;
            foreach (var kv in _charWeights)
            {
                x -= kv.Value;
                if (x <= 0) return kv.Key;
            }
            return _charWeights[_charWeights.Count - 1].Key;
        }

        public static bool CanMerge(Piece a, Piece b)
        {
            if (a == null || b == null || ReferenceEquals(a, b)) return false;
            if (a.Type != b.Type || a.Type == PieceType.Chanzhang) return false;
            if (a.Level != b.Level || a.Level >= UnitDef.MaxLevel) return false;
            return a.Type == PieceType.Unit ? a.Kind == b.Kind : a.Char == b.Char;
        }

        public bool CampToCell(int slot, int c, int r)
        {
            var p = Camp[slot];
            if (p == null || M.Over) return false;
            if (p.Type == PieceType.Chanzhang)
            {
                if (!Board.InBounds(c, r) || Board.Cells[c, r] != Cell.Willow) return false;
                Board.Cells[c, r] = Cell.Grass;
                Camp[slot] = null;
                Emit(new SimEvent { Type = Ev.StaffUsed, P = new CellPos(c, r).Center });
                return true;
            }
            if (!Board.Placeable(c, r)) return false;
            var t = Board.Pieces[c, r];
            if (t == null)
            {
                Board.Pieces[c, r] = p;
                Camp[slot] = null;
            }
            else if (CanMerge(p, t))
            {
                Camp[slot] = null;
                MergeInto(t, new CellPos(c, r).Center);
            }
            else
            {
                Board.Pieces[c, r] = p;
                Camp[slot] = t;
            }
            RecomputeHeroes();
            return true;
        }

        public bool CellToCell(int c1, int r1, int c2, int r2)
        {
            if (M.Over || !Board.InBounds(c1, r1) || (c1 == c2 && r1 == r2)) return false;
            var p = Board.Pieces[c1, r1];
            if (p == null) return false;
            if (!Board.Placeable(c2, r2)) return false;
            var t = Board.Pieces[c2, r2];
            if (t == null)
            {
                Board.Pieces[c2, r2] = p;
                Board.Pieces[c1, r1] = null;
            }
            else if (CanMerge(p, t))
            {
                Board.Pieces[c1, r1] = null;
                MergeInto(t, new CellPos(c2, r2).Center);
            }
            else
            {
                Board.Pieces[c2, r2] = p;
                Board.Pieces[c1, r1] = t;
            }
            RecomputeHeroes();
            return true;
        }

        public bool CellToCamp(int c, int r, int slot)
        {
            if (M.Over || !Board.InBounds(c, r)) return false;
            var p = Board.Pieces[c, r];
            if (p == null) return false;
            var t = Camp[slot];
            if (t == null)
            {
                Camp[slot] = p;
                Board.Pieces[c, r] = null;
            }
            else if (CanMerge(p, t))
            {
                Board.Pieces[c, r] = null;
                MergeInto(t, new V2(-1, -1));
            }
            else
            {
                if (t.Type == PieceType.Chanzhang) return false;
                Camp[slot] = p;
                Board.Pieces[c, r] = t;
            }
            RecomputeHeroes();
            return true;
        }

        public bool CampToCamp(int a, int b)
        {
            if (a == b || Camp[a] == null) return false;
            if (CanMerge(Camp[a], Camp[b]))
            {
                var t = Camp[b];
                Camp[a] = null;
                MergeInto(t, new V2(-1, -1));
                return true;
            }
            var tmp = Camp[b];
            Camp[b] = Camp[a];
            Camp[a] = tmp;
            return true;
        }

        void MergeInto(Piece target, V2 at)
        {
            target.Level++;
            Emit(new SimEvent { Type = Ev.Merge, A = target.Id, Val = target.Level, P = at });
        }

        public bool UseMengHan()
        {
            if (Items.MengHan <= 0 || M.Over) return false;
            Items.MengHan--;
            foreach (var e in Enemies) e.StunT = Math.Max(e.StunT, 3f);
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "蒙汗药" });
            return true;
        }

        public bool UseTianShu()
        {
            if (Items.TianShu <= 0 || M.Over) return false;
            Piece best = null;
            var at = new V2();
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    var p = Board.Pieces[c, r];
                    if (p == null || p.Level >= UnitDef.MaxLevel) continue;
                    if (best == null || p.Level < best.Level) { best = p; at = new CellPos(c, r).Center; }
                }
            if (best == null) return false;
            Items.TianShu--;
            MergeInto(best, at);
            RecomputeHeroes();
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "天书" });
            return true;
        }

        // ───────────── 好汉识别 ─────────────

        public void RecomputeHeroes()
        {
            var before = new HashSet<int>();
            foreach (var h in Heroes) before.Add(h.Id);
            Heroes = NameMatcher.Match(Board, Pool, Mirrored, Heroes);

            _wuyongAura = new bool[Board.Cols, Board.Rows];
            _daizong = false;
            _erniang = null;
            foreach (var h in Heroes)
            {
                if (!before.Contains(h.Id))
                {
                    FormedEver.Add(h.Def.Id);
                    Emit(new SimEvent { Type = Ev.HeroFormed, A = h.Id, B = (int)h.Def.Id, P = h.Pos, Text = h.Def.Name });
                }
                if (h.Def.Id == HeroId.DaiZong) _daizong = true;
                if (h.Def.Id == HeroId.SunErNiang) _erniang = h;
                if (h.Def.Id == HeroId.WuYong)
                    foreach (var cell in h.Cells)
                        for (int dc = -1; dc <= 1; dc++)
                            for (int dr = -1; dr <= 1; dr++)
                                if (Board.InBounds(cell.c + dc, cell.r + dr)) _wuyongAura[cell.c + dc, cell.r + dr] = true;
            }
        }

        public HeroInstance HeroAt(int c, int r)
        {
            foreach (var h in Heroes)
                foreach (var cell in h.Cells)
                    if (cell.c == c && cell.r == r) return h;
            return null;
        }

        // ───────────── 战斗 ─────────────

        public void AddEnemy(Enemy e)
        {
            e.Pos = Board.PosAt(e.Progress);
            Enemies.Add(e);
        }

        public void Tick(float dt)
        {
            if (Dead) return;
            EnemySlowT = Math.Max(0, EnemySlowT - dt);
            HasteAllT = Math.Max(0, HasteAllT - dt);
            _steleCd = Math.Max(0, _steleCd - dt);

            MoveEnemies(dt);
            if (Dead) return;
            TickBosses(dt);
            TickUnits(dt);
            TickHeroes(dt);
            TickStele();
            Enemies.RemoveAll(e => e.Dead);
        }

        void MoveEnemies(float dt)
        {
            float globalMul = (EnemySlowT > 0 ? 0.5f : 1f) * (Items.Marsh ? 0.85f : 1f);
            foreach (var e in Enemies)
            {
                if (e.Dead) continue;
                e.HasteT = Math.Max(0, e.HasteT - dt);
                if (e.StunT > 0) { e.StunT -= dt; continue; }
                float spd = e.Def.Speed * globalMul * (e.HasteT > 0 ? 1.45f : 1f);
                e.Progress += spd * dt;
                if (e.Progress >= Board.PathLength)
                {
                    e.Dead = true;
                    Leak(e);
                    if (Dead) return;
                    continue;
                }
                e.Pos = Board.PosAt(e.Progress);
            }
        }

        void Leak(Enemy e)
        {
            int heartsBefore = Hearts;
            Hp = Math.Max(0, Hp - e.LeakDamage);
            Emit(new SimEvent { Type = Ev.Leak, A = e.Id, Val = e.LeakDamage, P = e.Pos });
            if (!Dead && Hearts < heartsBefore)
            {
                int gift = RecruitCost * 3;
                Buns += gift;
                Rains++;
                Emit(new SimEvent { Type = Ev.Rain, Val = gift, P = Board.Goal.Center });
            }
        }

        void TickBosses(float dt)
        {
            for (int i = 0; i < Enemies.Count; i++)
            {
                var e = Enemies[i];
                if (e.Boss == null || e.Dead) continue;
                e.SkillT -= dt;
                if (e.SkillT > 0) continue;
                switch (e.Boss.Skill)
                {
                    case BossSkill.Summon:
                        e.SkillT = 6f;
                        for (int k = 0; k < 2; k++)
                            M.SpawnOn(this, EnemyKind.Zu, Math.Max(0, e.Progress - 0.35f * (k + 1)));
                        Emit(new SimEvent { Type = Ev.Ult, A = e.Id, P = e.Pos, Val = 0.8f, Text = e.Boss.Name + "：来人！" , Flag = 1 });
                        break;
                    case BossSkill.Haste:
                        e.SkillT = 5f;
                        foreach (var o in Enemies)
                            if (V2.Dist(o.Pos, e.Pos) < 2.2f) o.HasteT = 2.5f;
                        Emit(new SimEvent { Type = Ev.Ult, A = e.Id, P = e.Pos, Val = 2.2f, Text = e.Boss.Name + "：快！快！", Flag = 1 });
                        break;
                    default:
                        e.SkillT = 999f;
                        break;
                }
            }
        }

        float CdMul(int c, int r)
        {
            float m = 1f;
            if (_daizong) m *= 0.85f;
            if (_wuyongAura[c, r]) m *= 0.7f;
            if (HasteAllT > 0) m *= 0.5f;
            return m;
        }

        void TickUnits(float dt)
        {
            for (int c = 0; c < Board.Cols; c++)
            {
                for (int r = 0; r < Board.Rows; r++)
                {
                    var p = Board.Pieces[c, r];
                    if (p == null || p.Type != PieceType.Unit) continue;
                    _unitCd.TryGetValue(p.Id, out var cd);
                    cd -= dt;
                    if (cd > 0) { _unitCd[p.Id] = cd; continue; }
                    var def = UnitDef.Get(p.Kind);
                    var pos = new CellPos(c, r).Center;
                    var target = FindTarget(pos, def.Range, false);
                    if (target == null) { _unitCd[p.Id] = 0; continue; }
                    _unitCd[p.Id] = def.CooldownAt(p.Level) * CdMul(c, r);
                    float dmg = def.DamageAt(p.Level);
                    Emit(new SimEvent { Type = Ev.Attack, A = p.Id, B = (int)p.Kind, P = pos, Q = target.Pos, Val = p.Level });

                    if (def.Pierce > 1) PierceLine(pos, target.Pos, def.Range, def.Pierce, dmg, null);
                    else
                    {
                        Hit(target, dmg, null);
                        if (def.Splash > 0) Splash(target, def.Splash, dmg * 0.5f, null);
                        if (def.Knockback > 0 && !target.IsBig) Knock(target, def.Knockback);
                    }
                }
            }
        }

        void TickHeroes(float dt)
        {
            foreach (var h in Heroes)
            {
                var def = h.Def;
                h.Cd -= dt;
                h.UltCd -= dt;
                var pos = h.Pos;

                if (h.UltCd <= 0 && TryUlt(h, pos)) h.UltCd = def.UltCooldown;

                if (def.NoAttack || h.Cd > 0) continue;
                var target = FindTarget(pos, def.Range, def.Id == HeroId.HuaRong);
                if (target == null) { h.Cd = 0; continue; }
                h.Cd = def.Cooldown * CdMul(h.Cells[0].c, h.Cells[0].r);
                float dmg = def.DamageAt(h.Level) * (h.OnWater ? def.WaterBonus : 1f);
                int crit = 0;
                if (def.Id == HeroId.YanQing && _rng.Chance(0.3f)) { dmg *= 3f; crit = 1; }
                Emit(new SimEvent { Type = Ev.HeroAttack, A = h.Id, B = (int)def.Id, P = pos, Q = target.Pos, Flag = crit });

                if (def.Pierce > 1) PierceLine(pos, target.Pos, def.Range, def.Pierce, dmg, def);
                else
                {
                    Hit(target, dmg, def, crit == 1);
                    if (def.Splash > 0) Splash(target, def.Splash, dmg * 0.6f, def);
                }
                if (def.Id == HeroId.HuSanNiang) target.StunT = Math.Max(target.StunT, 0.6f);
                if (def.Id == HeroId.LuZhiShen && !target.IsBig) Knock(target, 0.4f);
            }
        }

        bool TryUlt(HeroInstance h, V2 pos)
        {
            var def = h.Def;
            float dmg = def.DamageAt(h.Level) * (h.OnWater ? def.WaterBonus : 1f);
            bool any = Enemies.Count > 0;
            float radius = 0;
            switch (def.Id)
            {
                case HeroId.WuDaLang:
                    Buns += 3;
                    Emit(new SimEvent { Type = Ev.Buns, Val = 3, P = pos });
                    return true;
                case HeroId.DaiZong:
                    if (!any) return false;
                    EnemySlowT = 4f;
                    radius = 99;
                    break;
                case HeroId.WuYong:
                    if (!any) return false;
                    HasteAllT = 3f;
                    radius = 99;
                    break;
                case HeroId.WuSong:
                {
                    if (FindTarget(pos, 2.6f, false) == null) return false;
                    for (int i = 0; i < 18; i++)
                    {
                        var t = FindTarget(pos, 2.6f, false);
                        if (t == null) break;
                        Hit(t, dmg * 1.2f, def);
                    }
                    radius = 2.6f;
                    break;
                }
                case HeroId.LinChong:
                    radius = 3f;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) { e.StunT = Math.Max(e.StunT, 2.5f); Hit(e, dmg * 4f, def); }
                    break;
                case HeroId.LiKui:
                    radius = 3.2f;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) Hit(e, dmg * 6f, def);
                    break;
                case HeroId.HuaRong:
                {
                    Enemy best = null;
                    foreach (var e in Enemies) if (!e.Dead && (best == null || e.Hp > best.Hp)) best = e;
                    if (best == null) return false;
                    Emit(new SimEvent { Type = Ev.HeroAttack, A = h.Id, B = (int)def.Id, P = pos, Q = best.Pos, Flag = 2 });
                    Hit(best, dmg * 25f, def, true);
                    pos = best.Pos;
                    radius = 0.8f;
                    break;
                }
                case HeroId.YanQing:
                {
                    Enemy best = null;
                    foreach (var e in InRange(pos, 3f)) if (best == null || e.MaxHp > best.MaxHp) best = e;
                    if (best == null) return false;
                    best.Progress = Math.Max(0, best.Progress - 2f);
                    best.Pos = Board.PosAt(best.Progress);
                    Hit(best, dmg * 8f, def, true);
                    radius = 1f;
                    break;
                }
                case HeroId.QinMing:
                {
                    Enemy center = null;
                    int bestN = 0;
                    foreach (var e in InRange(pos, 4f))
                    {
                        int n = 0;
                        foreach (var o in Enemies) if (!o.Dead && V2.Dist(o.Pos, e.Pos) < 1.6f) n++;
                        if (n > bestN) { bestN = n; center = e; }
                    }
                    if (center == null) return false;
                    pos = center.Pos;
                    radius = 1.6f;
                    foreach (var e in InRange(pos, radius)) Hit(e, dmg * 8f, def);
                    break;
                }
                case HeroId.ZhangShun:
                    radius = 3.5f;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) { e.StunT = Math.Max(e.StunT, 1f); Hit(e, dmg * 6f, def); }
                    break;
                case HeroId.ZhangHeng:
                    radius = 2.6f;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) { if (!e.IsBig) Knock(e, 1.5f); Hit(e, dmg * 3f, def); }
                    break;
                case HeroId.RuanXiaoEr:
                    radius = 2.8f;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) Hit(e, dmg * 6f, def);
                    break;
                case HeroId.RuanXiaoQi:
                {
                    var t = FindTarget(pos, def.Range + 0.5f, false);
                    if (t == null) return false;
                    t.StunT = Math.Max(t.StunT, 2f);
                    Hit(t, dmg * 15f, def, true);
                    pos = t.Pos;
                    radius = 1f;
                    break;
                }
                case HeroId.SunErNiang:
                    radius = 3f;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) e.StunT = Math.Max(e.StunT, 3f);
                    break;
                case HeroId.HuSanNiang:
                {
                    var list = InRange(pos, 3f);
                    if (list.Count == 0) return false;
                    list.Sort((a, b) => b.Progress.CompareTo(a.Progress));
                    for (int i = 0; i < list.Count && i < 5; i++) { list[i].StunT = Math.Max(list[i].StunT, 2.5f); Hit(list[i], dmg * 3f, def); }
                    radius = 3f;
                    break;
                }
                case HeroId.LuZhiShen:
                    if (Board.TryFindWillow(out var willow))
                    {
                        Board.Cells[willow.c, willow.r] = Cell.Grass;
                        Emit(new SimEvent { Type = Ev.StaffUsed, P = willow.Center, Flag = 1 });
                        radius = 0.5f;
                        pos = willow.Center;
                        break;
                    }
                    radius = 2f;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) Hit(e, dmg * 10f, def);
                    break;
                default:
                    return false;
            }
            Emit(new SimEvent { Type = Ev.Ult, A = h.Id, B = (int)def.Id, P = pos, Val = radius, Text = def.Ult });
            return true;
        }

        void TickStele()
        {
            if (!Items.Stele || _steleCd > 0) return;
            var goal = Board.Goal.Center;
            foreach (var e in Enemies)
            {
                if (e.Dead || V2.Dist(e.Pos, goal) > 1.8f) continue;
                _steleCd = 9f;
                var at = e.Pos;
                foreach (var o in InRange(at, 1.1f)) Hit(o, Tuning.WaveHp(M.Wave) * 12f, null);
                Emit(new SimEvent { Type = Ev.ItemUsed, Text = "石碣天降", P = at, Flag = 1 });
                return;
            }
        }

        Enemy FindTarget(V2 pos, float range, bool preferBig)
        {
            Enemy best = null;
            float bestScore = float.MinValue;
            foreach (var e in Enemies)
            {
                if (e.Dead || V2.Dist(pos, e.Pos) > range) continue;
                float s = e.Progress + (preferBig && e.IsBig ? 1000f : 0f);
                if (s > bestScore) { bestScore = s; best = e; }
            }
            return best;
        }

        bool AreaAny(V2 pos, float r)
        {
            foreach (var e in Enemies) if (!e.Dead && V2.Dist(pos, e.Pos) <= r) return true;
            return false;
        }

        List<Enemy> InRange(V2 pos, float r)
        {
            var list = new List<Enemy>();
            foreach (var e in Enemies) if (!e.Dead && V2.Dist(pos, e.Pos) <= r) list.Add(e);
            return list;
        }

        void PierceLine(V2 from, V2 to, float range, int count, float dmg, HeroDef src)
        {
            var dir = (to - from).Norm;
            var hits = new List<KeyValuePair<float, Enemy>>();
            foreach (var e in Enemies)
            {
                if (e.Dead) continue;
                var d = e.Pos - from;
                float along = V2.Dot(d, dir);
                if (along < -0.2f || along > range + 0.4f) continue;
                var perp = d - dir * along;
                if (perp.Len > 0.42f) continue;
                hits.Add(new KeyValuePair<float, Enemy>(along, e));
            }
            hits.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < hits.Count && i < count; i++) Hit(hits[i].Value, dmg, src);
        }

        void Splash(Enemy center, float radius, float dmg, HeroDef src)
        {
            foreach (var e in Enemies)
                if (!e.Dead && !ReferenceEquals(e, center) && V2.Dist(e.Pos, center.Pos) <= radius) Hit(e, dmg, src);
        }

        void Knock(Enemy e, float dist)
        {
            e.Progress = Math.Max(0, e.Progress - dist);
            e.Pos = Board.PosAt(e.Progress);
        }

        void Hit(Enemy e, float dmg, HeroDef src, bool crit = false)
        {
            if (e.Dead) return;
            if (Items.Flag) dmg *= 1.1f;
            int flag = crit ? 1 : 0;
            if (e.Boss != null)
            {
                if (src != null && src.Id == e.Boss.Nemesis) { dmg *= Tuning.BossNemesisMul; flag = 2; }
                else if (e.Boss.Skill == BossSkill.Resist) dmg *= 0.5f;
            }
            if (e.Def.Armor > 0 && src == null) dmg *= 1f - e.Def.Armor;
            e.Hp -= dmg;
            Emit(new SimEvent { Type = Ev.Hit, A = e.Id, Val = dmg, Flag = flag, P = e.Pos });
            if (e.Hp > 0) return;

            e.Dead = true;
            float raw = e.Def.Reward * e.RewardMul;
            if (_erniang != null && V2.Dist(_erniang.Pos, e.Pos) <= 2.2f) raw += 1;
            _incomeAcc += raw * IncomeMul;
            int reward = (int)_incomeAcc;
            _incomeAcc -= reward;
            Buns += reward;
            Kills++;
            Emit(new SimEvent { Type = Ev.Kill, A = e.Id, Val = reward, P = e.Pos, Flag = e.Boss != null ? 2 : e.IsBig ? 1 : 0 });
        }

        void Emit(SimEvent ev)
        {
            ev.Side = Index;
            M.Events.Add(ev);
        }
    }
}
