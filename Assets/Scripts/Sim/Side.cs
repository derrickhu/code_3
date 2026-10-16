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
        public int Baozi;
        public bool Xuming;
        public int Shovels;
        public int Fu;
        public int Trap;
        public bool Far;
        /// <summary>带进这一局的兵器，元素是 HeroId。</summary>
        public List<int> Armed;
        /// <summary>与 HeroId 下标对齐的兵器阶数。0 表示没装备。</summary>
        public int[] WeaponLv;
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
        public int MaxHp = Tuning.MaxHp;
        public int Hp = Tuning.MaxHp;
        public int Kills;
        /// <summary>山寨塞不下的铲子先存着，有空位自动补进去。</summary>
        public int ShovelStock;
        public bool RescueUsed;
        public float ShieldT;
        HeroInstance _curHero;
        public int Rains;
        /// <summary>击杀收入倍率：对手难度的主要旋钮。</summary>
        public float IncomeMul = 1f;
        float _incomeAcc;
        public float EnemySlowT;
        public float HasteAllT;
        public float ItemHasteT;
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

            if (Items.Gang) Buns += 30;
            if (Items.Xuming) AddMaxHp(10);
            ShovelStock = Items.Shovels;
            FillShovels();
        }

        public int MaxHearts => (MaxHp + Tuning.HeartHp - 1) / Tuning.HeartHp;

        public void AddMaxHp(int n)
        {
            MaxHp += n;
            Hp += n;
        }

        /// <summary>危急：官兵快摸到宋江，或者只剩最后一颗心。</summary>
        public bool InDanger
        {
            get
            {
                if (Dead || M.Over) return false;
                if (Hearts <= 1) return true;
                int near = 0;
                var goal = Board.Goal.Center;
                foreach (var e in Enemies)
                    if (!e.Dead && V2.Dist(e.Pos, goal) < 2.2f) near++;
                return near >= 3;
            }
        }

        public void GrantShovels(int n)
        {
            ShovelStock += n;
            FillShovels();
            Emit(new SimEvent { Type = Ev.Shovels, Val = n });
        }

        void FillShovels()
        {
            for (int i = 0; i < Camp.Length && ShovelStock > 0; i++)
                if (Camp[i] == null) { Camp[i] = Piece.Shovel(); ShovelStock--; }
        }

        bool HasShovel()
        {
            if (ShovelStock > 0) return true;
            foreach (var p in Camp) if (p != null && p.Type == PieceType.Shovel) return true;
            return false;
        }

        /// <summary>看广告请公孙胜作法：清掉场上小兵、头目掉三成血，之后十二秒宋江不掉血。一局一次。</summary>
        public bool Rescue()
        {
            if (RescueUsed || Dead || M.Over) return false;
            RescueUsed = true;
            ShieldT = 12f;
            foreach (var e in Enemies)
            {
                if (e.Dead) continue;
                Hit(e, e.IsBig ? e.MaxHp * 0.3f : e.Hp + 1f, null);
            }
            Emit(new SimEvent { Type = Ev.Rescue, P = Board.Goal.Center });
            return true;
        }

        /// <summary>孙二娘的包子：五成五回一颗心，四成五吃坏肚子掉一颗心（不会因此丧命）。</summary>
        public bool UseBaozi()
        {
            if (Items.Baozi <= 0 || M.Over || Dead) return false;
            Items.Baozi--;
            bool good = _rng.Chance(0.55f);
            int before = Hp;
            Hp = good ? Math.Min(MaxHp, Hp + Tuning.HeartHp) : Math.Max(1, Hp - Tuning.HeartHp);
            Emit(new SimEvent { Type = Ev.Baozi, Flag = good ? 1 : 0, Val = Hp - before, P = Board.Goal.Center });
            return true;
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
            FillShovels();
            bool needShovel = EmptyCells() == 0 && !HasShovel() && Board.AnyDiggable();
            for (int i = 0; i < Camp.Length; i++)
            {
                if (Camp[i] != null) continue;
                if (needShovel) { Camp[i] = Piece.Shovel(); needShovel = false; }
                else Camp[i] = Roll();
            }
            Recruits++;
            RecruitCost += Tuning.CostStep;
            Emit(new SimEvent { Type = Ev.Recruit });
            return true;
        }

        Piece Roll()
        {
            bool gold = _sinceGold >= Tuning.GoldPity;
            if (!gold && Board.AnyDiggable() && _rng.Chance(Tuning.ShovelChance * (EmptyCells() == 0 ? 2.5f : 1f))) return Piece.Shovel();
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

        public int EmptyCells()
        {
            int n = 0;
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (Board.Placeable(c, r) && Board.Pieces[c, r] == null) n++;
            return n;
        }

        public static int DismissRefund(Piece p) => p == null || p.Type == PieceType.Shovel ? 1 : 2 * p.Level;

        /// <summary>遣散：把字拖到聚义按钮上，返还少量炊饼。</summary>
        public bool DismissCamp(int slot)
        {
            var p = Camp[slot];
            if (p == null || M.Over) return false;
            Camp[slot] = null;
            Refund(p, new V2(-1, -1));
            FillShovels();
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
            if (a.Type != b.Type || a.Type == PieceType.Shovel) return false;
            if (a.Level != b.Level || a.Level >= UnitDef.MaxLevel) return false;
            return a.Type == PieceType.Unit ? a.Kind == b.Kind : a.Char == b.Char;
        }

        public bool CampToCell(int slot, int c, int r)
        {
            var p = Camp[slot];
            if (p == null || M.Over) return false;
            if (p.Type == PieceType.Shovel)
            {
                if (!Board.Dig(c, r)) return false;
                Camp[slot] = null;
                FillShovels();
                Emit(new SimEvent { Type = Ev.Dig, P = new CellPos(c, r).Center, Flag = Board.Cells[c, r] == Cell.Water ? 1 : 0 });
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
                if (t.Type == PieceType.Shovel) return false;
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

        public bool UseFu()
        {
            if (Items.Fu <= 0 || M.Over) return false;
            Items.Fu--;
            ItemHasteT = 12f;
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "攻速符", Flag = 3 });
            return true;
        }

        float WeaponMul(HeroDef def)
        {
            if (!Armed(def)) return 1f;
            int lv = 1;
            if (Items.WeaponLv != null)
            {
                int i = (int)def.Id;
                if (i >= 0 && i < Items.WeaponLv.Length && Items.WeaponLv[i] > 0) lv = Items.WeaponLv[i];
            }
            return HeroDef.ArmedMul(lv);
        }

        bool Armed(HeroDef def)
        {
            if (Index != 0 || string.IsNullOrEmpty(def.Weapon) || Items.Armed == null) return false;
            return Items.Armed.Contains((int)def.Id);
        }

        public bool UseMengHan()
        {
            if (Items.MengHan <= 0 || M.Over) return false;
            Items.MengHan--;
            foreach (var e in Enemies) e.StunT = Math.Max(e.StunT, 3f);
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "蒙汗药" });
            return true;
        }

        /// <summary>陷阱：官兵跌回去，一时爬不上来。头目跌得近一些。</summary>
        public bool UseTrap()
        {
            if (Items.Trap <= 0 || M.Over) return false;
            Items.Trap--;
            foreach (var e in Enemies)
            {
                if (e.Dead) continue;
                Knock(e, e.IsBig ? 0.9f : 1.8f);
                e.StunT = Math.Max(e.StunT, e.IsBig ? 1.2f : 1.8f);
            }
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "陷阱", Flag = 4 });
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
            ItemHasteT = Math.Max(0, ItemHasteT - dt);
            _steleCd = Math.Max(0, _steleCd - dt);
            ShieldT = Math.Max(0, ShieldT - dt);

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
            float globalMul = (EnemySlowT > 0 ? 0.5f : 1f) * (Items.Marsh ? 0.75f : 1f);
            foreach (var e in Enemies)
            {
                if (e.Dead) continue;
                e.HasteT = Math.Max(0, e.HasteT - dt);
                if (e.StunT > 0) { e.StunT -= dt; continue; }
                float spd = e.Def.Speed * globalMul * (e.HasteT > 0 ? 1.65f : 1f);
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
            if (ShieldT > 0)
            {
                Emit(new SimEvent { Type = Ev.Leak, A = e.Id, Val = 0, P = e.Pos, Flag = 1 });
                return;
            }
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
                        e.SkillT = 5.5f;
                        for (int k = 0; k < 3; k++)
                            M.SpawnOn(this, EnemyKind.Zu, Math.Max(0, e.Progress - 0.4f * (k + 1)));
                        Emit(new SimEvent { Type = Ev.Ult, A = e.Id, P = e.Pos, Val = 1.8f, Text = e.Boss.Name + "：来人！", Flag = 1 });
                        break;
                    case BossSkill.Haste:
                        e.SkillT = 4.5f;
                        foreach (var o in Enemies)
                            if (!o.Dead && V2.Dist(o.Pos, e.Pos) < 3.4f) o.HasteT = 3.2f;
                        Emit(new SimEvent { Type = Ev.Ult, A = e.Id, P = e.Pos, Val = 3.4f, Text = e.Boss.Name + "：快！快！", Flag = 1 });
                        break;
                    default:
                        e.SkillT = 5.5f;
                        Emit(new SimEvent { Type = Ev.Ult, A = e.Id, P = e.Pos, Val = 2.6f, Text = e.Boss.Name + "：铁布衫", Flag = 1 });
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
            if (ItemHasteT > 0) m *= 0.71f;
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
                    float range = Items.Far && def.Range >= 2.2f ? def.Range * 2f : def.Range;
                    var target = FindTarget(pos, range, false);
                    if (target == null) { _unitCd[p.Id] = 0; continue; }
                    _unitCd[p.Id] = def.CooldownAt(p.Level) * CdMul(c, r);
                    float dmg = def.DamageAt(p.Level);
                    Emit(new SimEvent { Type = Ev.Attack, A = p.Id, B = (int)p.Kind, P = pos, Q = target.Pos, Val = p.Level });

                    if (def.Pierce > 1) PierceLine(pos, target.Pos, range, def.Pierce, dmg, null);
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
                _curHero = h;

                if (h.UltCd <= 0 && TryUlt(h, pos)) h.UltCd = def.UltCooldown;

                if (def.NoAttack) GainXp(h, dt * 0.35f);
                if (def.NoAttack || h.Cd > 0) continue;
                float range = def.RangeWith(Items.Far);
                var target = FindTarget(pos, range, def.Id == HeroId.HuaRong);
                if (target == null) { h.Cd = 0; continue; }
                h.Cd = def.Cooldown * CdMul(h.Cells[0].c, h.Cells[0].r);
                float dmg = def.DamageAt(h.Level) * (h.OnWater ? def.WaterBonus : 1f) * WeaponMul(def);
                int crit = 0;
                if (def.Id == HeroId.YanQing && _rng.Chance(0.3f)) { dmg *= 3f; crit = 1; }
                Emit(new SimEvent { Type = Ev.HeroAttack, A = h.Id, B = (int)def.Id, P = pos, Q = target.Pos, Flag = crit });

                if (def.Pierce > 1) PierceLine(pos, target.Pos, range, def.Pierce, dmg, def);
                else
                {
                    Hit(target, dmg, def, crit == 1);
                    if (def.Splash > 0) Splash(target, def.Splash, dmg * 0.6f, def);
                }
                if (def.Id == HeroId.HuSanNiang) target.StunT = Math.Max(target.StunT, 0.6f);
                if (def.Id == HeroId.LuZhiShen && !target.IsBig) Knock(target, 0.4f);
                GainXp(h, 0.2f);
            }
            _curHero = null;
        }

        /// <summary>好汉打仗涨经验，攒满后名字里每个字都升一级（最高五级）。</summary>
        void GainXp(HeroInstance h, float xp)
        {
            if (h.Level >= UnitDef.MaxLevel) return;
            h.Xp += xp;
            if (h.Xp < HeroInstance.XpNeed(h.Level)) return;
            h.Xp = 0;
            int next = h.Level + 1;
            foreach (var p in h.Pieces) p.Level = Math.Max(p.Level, next);
            h.Level = next;
            Emit(new SimEvent { Type = Ev.HeroLevel, A = h.Id, B = (int)h.Def.Id, Val = next, P = h.Pos, Text = h.Def.Name });
        }

        bool TryUlt(HeroInstance h, V2 pos)
        {
            var def = h.Def;
            float dmg = def.DamageAt(h.Level) * (h.OnWater ? def.WaterBonus : 1f) * WeaponMul(def);
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
                    if (Board.AnyDiggable() && !HasShovel())
                    {
                        GrantShovels(1);
                        radius = 0.5f;
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
            if (Items.Flag) dmg *= 1.22f;
            int flag = crit ? 1 : 0;
            if (e.Boss != null)
            {
                if (src != null && src.Id == e.Boss.Nemesis) { dmg *= Tuning.BossNemesisMul; flag = 2; }
                else if (e.Boss.Skill == BossSkill.Resist)
                {
                    dmg *= 0.5f;
                    if (flag == 0) flag = 3;
                }
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
            if (_curHero != null) GainXp(_curHero, e.Boss != null ? 15f : e.IsBig ? 5f : 1f);
            Emit(new SimEvent { Type = Ev.Kill, A = e.Id, Val = reward, P = e.Pos, Flag = e.Boss != null ? 2 : e.IsBig ? 1 : 0 });
        }

        void Emit(SimEvent ev)
        {
            ev.Side = Index;
            M.Events.Add(ev);
        }
    }
}
