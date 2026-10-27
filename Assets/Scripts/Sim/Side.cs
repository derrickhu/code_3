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
        /// <summary>头目：轮到第几门绝技、是否已狂暴、冲阵剩余秒数。</summary>
        public int SkillIx;
        public bool Enraged;
        public float DashT;
        public float RewardMul = 1f;
        public int LeakDamage;
        public bool Dead;
        /// <summary>被钩镰枪绊住，剩余秒数。期间走得慢。</summary>
        public float SlowT;
        /// <summary>已经挨过祖传宝刀的头一下。</summary>
        public bool Cut;

        public bool IsBig => Def.Kind == EnemyKind.Bu || Def.Kind == EnemyKind.Boss;
        /// <summary>还在出兵口草堆里，没走到看得见的路上。</summary>
        public bool InBrush => Progress < Tuning.SpawnCover;
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
        /// <summary>十字坡灶：开局摆一张灶，慢慢出炊饼。</summary>
        public bool Stove;
        /// <summary>招贤酒：聚义时好汉字更常出现。</summary>
        public bool Wine;
        /// <summary>这一局带着兵器的好汉。</summary>
        public List<int> Armed;
        /// <summary>下标是好汉。值是他带着的兵器（兵器出自谁），-1 表示空手。</summary>
        public int[] Gear;
        /// <summary>下标是带着兵器的好汉，值是那件兵器的阶数。</summary>
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
        /// <summary>头目妖雨：全场出手变慢。</summary>
        public float RainT;
        readonly List<float> _graves = new List<float>();
        public float HasteAllT;
        public float ItemHasteT;
        /// <summary>主动道具各自的冷却，转完才能再放一次。</summary>
        public float MengCd, BaoziCd, ShuCd, FuCd, TrapCd;
        float _steleCd;

        readonly Rng _rng;
        readonly Dictionary<int, float> _unitCd = new Dictionary<int, float>();
        readonly List<KeyValuePair<char, float>> _charWeights = new List<KeyValuePair<char, float>>();
        float[,] _aura = NewAura();
        float _teamCd = 1f;
        int _sinceGold;
        readonly List<HeroInstance> _bunsHeroes = new List<HeroInstance>();
        readonly Dictionary<HeroId, float> _bondDmg = new Dictionary<HeroId, float>();
        /// <summary>当前凑齐的羁绊。</summary>
        public readonly List<Bond> ActiveBonds = new List<Bond>();
        public readonly HashSet<string> BondsEver = new HashSet<string>();
        public float RallyT;

        static float[,] NewAura()
        {
            var a = new float[Board.Cols, Board.Rows];
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++) a[c, r] = 1f;
            return a;
        }

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
            if (Items.Xuming) AddMaxHp(Tuning.XumingHearts);
            ShovelStock = Items.Shovels;
            FillShovels();
            _stovePending = Items.Stove;
            PlaceStove();
        }

        // ───────────── 十字坡灶 ─────────────

        bool _stovePending;
        float _stoveT;
        /// <summary>这一局灶已经出了多少炊饼。记在山寨上，不记在灶上，挪灶不会重新计数。</summary>
        public int StoveMade;

        public static int StoveCapAt(int level) => Tuning.StoveCap[Math.Max(1, Math.Min(Tuning.StoveMaxLevel, level)) - 1];

        /// <summary>灶落在离官兵路最远的空地上。没有空地就等有了再摆。</summary>
        void PlaceStove()
        {
            if (!_stovePending) return;
            int best = int.MaxValue, bc = -1, br = -1;
            for (int r = 0; r < Board.Rows; r++)
                for (int c = 0; c < Board.Cols; c++)
                {
                    if (!Board.Placeable(c, r) || Board.Pieces[c, r] != null) continue;
                    int near = 0;
                    for (int dc = -1; dc <= 1; dc++)
                        for (int dr = -1; dr <= 1; dr++)
                            if (Board.InBounds(c + dc, r + dr) && Board.Cells[c + dc, r + dr] == Cell.Path) near++;
                    if (near < best) { best = near; bc = c; br = r; }
                }
            if (bc < 0) return;
            _stovePending = false;
            Board.Pieces[bc, br] = Piece.Stove();
        }

        public bool FindStove(out int sc, out int sr)
        {
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                    if (Board.Pieces[c, r] != null && Board.Pieces[c, r].Type == PieceType.Stove) { sc = c; sr = r; return true; }
            sc = sr = -1;
            return false;
        }

        void TickStove(float dt)
        {
            if (_stovePending) PlaceStove();
            if (!FindStove(out int c, out int r)) return;
            var p = Board.Pieces[c, r];
            int lv = Math.Max(1, Math.Min(Tuning.StoveMaxLevel, p.Level));
            int cap = StoveCapAt(lv);
            if (StoveMade >= cap || Disabled(p, c, r)) return;
            _stoveT += dt;
            float gap = Tuning.StoveGap[lv - 1];
            if (_stoveT < gap) return;
            _stoveT -= gap;
            int n = Math.Min(Tuning.StoveYield[lv - 1], cap - StoveMade);
            StoveMade += n;
            Buns += n;
            Emit(new SimEvent { Type = Ev.Buns, Val = n, P = new CellPos(c, r).Center, A = p.Id, Flag = 2 });
        }

        public int MaxHearts => (MaxHp + Tuning.HeartHp - 1) / Tuning.HeartHp;

        public void AddMaxHp(int n)
        {
            MaxHp += n;
            Hp += n;
        }

        /// <summary>危急：官兵快摸到宋江，或者只剩最后一颗心。只用来换紧张的曲子。</summary>
        public bool InDanger
        {
            get
            {
                if (Dead || M.Over) return false;
                if (Hearts <= 1) return true;
                return AtTheGate;
            }
        }

        /// <summary>有官兵走到宋江跟前。「危」字和救命广告都看这个。</summary>
        public bool AtTheGate
        {
            get
            {
                if (Dead || M.Over) return false;
                var goal = Board.Goal.Center;
                foreach (var e in Enemies)
                    if (!e.Dead && V2.Dist(e.Pos, goal) < 2.2f) return true;
                return false;
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

        /// <summary>看广告请公孙胜作法：场上官兵原样推回路口，血量不动，再走一遍。一局一次。</summary>
        public bool Rescue()
        {
            if (RescueUsed || Dead || M.Over) return false;
            RescueUsed = true;
            var alive = new List<Enemy>();
            foreach (var e in Enemies) if (!e.Dead) alive.Add(e);
            alive.Sort((a, b) => a.Progress.CompareTo(b.Progress));
            float front = Tuning.SpawnCover + 0.05f;
            float limit = Math.Max(front, Board.PathLength - 1.5f);
            float cursor = front;
            foreach (var e in alive)
            {
                float dest = Math.Min(cursor, limit);
                if (e.Progress > dest)
                {
                    e.Progress = dest;
                    e.Pos = Board.PosAt(e.Progress);
                    e.HasteT = 0f;
                }
                cursor += 0.32f;
            }
            Emit(new SimEvent { Type = Ev.Rescue, P = Board.Goal.Center, Val = alive.Count });
            return true;
        }

        /// <summary>孙二娘的包子：五成五回一颗心，四成五吃坏肚子掉一颗心（不会因此丧命）。</summary>
        public bool UseBaozi()
        {
            if (Items.Baozi <= 0 || BaoziCd > 0f || M.Over || Dead) return false;
            Items.Baozi--;
            BaoziCd = Tuning.CdBaozi;
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

        public bool CanPay => !Dead && !M.Over && Buns >= RecruitCost;

        /// <summary>对手补空位用。山寨里还有字时不能按，避免把没摆出去的棋子扔掉。</summary>
        public bool CanRecruit => CanPay && EmptyCampSlots > 0;

        /// <summary>对手：只填空位，已有的字留着。</summary>
        public bool Recruit() => Recruit(false);

        /// <summary>玩家点聚义：整排丢掉重抽。每一格单独随机，不沿用原来的字，也不按场上的兵配对。</summary>
        public bool RefreshCamp() => Recruit(true);

        bool Recruit(bool replaceAll)
        {
            if (replaceAll ? !CanPay : !CanRecruit) return false;
            Buns -= RecruitCost;
            if (replaceAll)
            {
                for (int i = 0; i < Camp.Length; i++)
                {
                    if (Camp[i] != null && Camp[i].Type == PieceType.Shovel) ShovelStock++;
                    Camp[i] = null;
                }
            }
            else FillShovels();
            for (int i = 0; i < Camp.Length; i++)
            {
                if (!replaceAll && Camp[i] != null) continue;
                Camp[i] = Roll(!replaceAll);
            }
            Recruits++;
            RecruitCost += Tuning.CostStep;
            Emit(new SimEvent { Type = Ev.Recruit });
            return true;
        }

        /// <param name="pair">为真时倾向刷出能和现有一级兵合成的兵种。整排重抽时为假，每格互不影响。</param>
        Piece Roll(bool pair)
        {
            bool gold = _sinceGold >= Tuning.GoldPity;
            if (!gold && Board.AnyDiggable() && _rng.Chance(Tuning.ShovelChance * (EmptyCells() == 0 ? 2f : 1f))) return Piece.Shovel();
            if (gold || _rng.Chance(Items.Wine ? Tuning.WineCharChance : Tuning.HeroCharChance))
            {
                _sinceGold = 0;
                return Piece.Hero(RollChar());
            }
            _sinceGold++;
            if (pair && _rng.Chance(Tuning.PairBias) && TryPickPairKind(out var kind)) return Piece.Unit(kind);
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
            if (p == null || p.Type == PieceType.Stove) return false;
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
            if (a.Type != PieceType.Unit || b.Type != PieceType.Unit) return false;
            if (a.Level != b.Level || a.Level >= UnitDef.MaxLevel) return false;
            return a.Kind == b.Kind;
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
            if (t != null && t.Type == PieceType.Stove) return false;
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
            if (Camp[slot] == null) FillShovels();
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
            if (p == null || p.Type == PieceType.Stove) return false;
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
            if (Items.Fu <= 0 || FuCd > 0f || M.Over) return false;
            Items.Fu--;
            FuCd = Tuning.CdFu;
            ItemHasteT = 12f;
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "攻速符", Flag = 3 });
            return true;
        }

        /// <summary>这名好汉身上的兵器。空手时返回 null。品级、特性、图都读这件兵器，不读人自己的成名兵器。</summary>
        public HeroDef GearOf(HeroDef wearer)
        {
            if (wearer == null || Index != 0 || Items.Gear == null) return null;
            int i = (int)wearer.Id;
            if (i < 0 || i >= Items.Gear.Length) return null;
            int w = Items.Gear[i];
            if (w < 0 || w >= HeroDef.All.Length) return null;
            var def = HeroDef.Get((HeroId)w);
            return string.IsNullOrEmpty(def.Weapon) ? null : def;
        }

        public int GearRank(HeroDef wearer)
        {
            if (GearOf(wearer) == null || Items.WeaponLv == null) return 1;
            int i = (int)wearer.Id;
            if (i < 0 || i >= Items.WeaponLv.Length || Items.WeaponLv[i] <= 0) return 1;
            int lv = Items.WeaponLv[i];
            return lv > 5 ? 5 : lv;
        }

        public bool Armed(HeroDef def) => GearOf(def) != null;

        /// <summary>纸签和射程圈用的射程，含兵器加的那一截。</summary>
        public float ShownRange(HeroDef def)
        {
            if (def == null || def.NoAttack) return 0f;
            float range = def.RangeWith(Items.Far);
            if (range < 50f) range += AffixOf(def).Range;
            return range;
        }

        float WeaponMul(HeroDef wearer)
        {
            var w = GearOf(wearer);
            if (w == null) return 1f;
            return HeroDef.ArmedMul(GearRank(wearer), w.WGrade);
        }

        public float GearMul(HeroDef wearer) => WeaponMul(wearer);

        WeaponAffix AffixOf(HeroDef wearer)
        {
            var w = GearOf(wearer);
            return w == null ? WeaponAffix.Bare : Weapons.Affix(w, wearer);
        }

        /// <summary>普攻 / 大招共用的伤害：等级、水上、兵器、羁绊。</summary>
        float HeroDamage(HeroInstance h, in WeaponAffix aff)
        {
            var def = h.Def;
            float dmg = def.DamageAt(h.Level) * WeaponMul(def) * aff.Dmg;
            if (h.OnWater) dmg *= def.WaterBonus * aff.Water;
            if (_bondDmg.TryGetValue(def.Id, out var b)) dmg *= 1f + b;
            return dmg;
        }

        public float BondBonus(HeroId id) => _bondDmg.TryGetValue(id, out var b) ? b : 0f;

        public bool UseMengHan()
        {
            if (Items.MengHan <= 0 || MengCd > 0f || M.Over) return false;
            Items.MengHan--;
            MengCd = Tuning.CdMeng;
            foreach (var e in Enemies) e.StunT = Math.Max(e.StunT, 3f);
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "蒙汗药" });
            return true;
        }

        /// <summary>陷阱：官兵跌回去，一时爬不上来。头目跌得近一些。</summary>
        public bool UseTrap()
        {
            if (Items.Trap <= 0 || TrapCd > 0f || M.Over) return false;
            Items.Trap--;
            TrapCd = Tuning.CdTrap;
            foreach (var e in Enemies)
            {
                if (e.Dead) continue;
                Knock(e, e.IsBig ? 0.9f : 1.8f);
                e.StunT = Math.Max(e.StunT, e.IsBig ? 1.2f : 1.8f);
            }
            Emit(new SimEvent { Type = Ev.ItemUsed, Text = "陷阱", Flag = 4 });
            return true;
        }

        /// <summary>天书能不能落在这一格：没满级的兵，或者没到三级的灶。</summary>
        public bool ShuTarget(int c, int r)
        {
            if (!Board.InBounds(c, r)) return false;
            var p = Board.Pieces[c, r];
            if (p == null) return false;
            if (p.Type == PieceType.Unit) return p.Level < UnitDef.MaxLevel;
            if (p.Type == PieceType.Stove) return p.Level < Tuning.StoveMaxLevel;
            return false;
        }

        /// <summary>天书拖到哪一格就升哪一格。落空不消耗，也不进冷却。</summary>
        public bool UseTianShu(int c, int r)
        {
            if (Items.TianShu <= 0 || ShuCd > 0f || M.Over) return false;
            if (!ShuTarget(c, r)) return false;
            Items.TianShu--;
            ShuCd = Tuning.CdShu;
            MergeInto(Board.Pieces[c, r], new CellPos(c, r).Center);
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

            _aura = NewAura();
            _teamCd = 1f;
            _bunsHeroes.Clear();
            var teamSeen = new HashSet<HeroId>();
            foreach (var h in Heroes)
            {
                var d = h.Def;
                if (!before.Contains(h.Id))
                {
                    FormedEver.Add(d.Id);
                    Emit(new SimEvent { Type = Ev.HeroFormed, A = h.Id, B = (int)d.Id, P = h.Pos, Text = d.Name });
                }
                if (d.TeamCd < 1f && teamSeen.Add(d.Id)) _teamCd *= d.TeamCd;
                if (d.KillBuns > 0) _bunsHeroes.Add(h);
                if (d.Aura < 1f)
                    foreach (var cell in h.Cells)
                        for (int dc = -1; dc <= 1; dc++)
                            for (int dr = -1; dr <= 1; dr++)
                                if (Board.InBounds(cell.c + dc, cell.r + dr))
                                    _aura[cell.c + dc, cell.r + dr] = Math.Min(_aura[cell.c + dc, cell.r + dr], d.Aura);
            }
            RecomputeBonds();
        }

        void RecomputeBonds()
        {
            var on = new HashSet<HeroId>();
            foreach (var h in Heroes) on.Add(h.Def.Id);
            _bondDmg.Clear();
            ActiveBonds.Clear();
            foreach (var b in Bond.All)
            {
                int n = 0;
                foreach (var id in b.Members) if (on.Contains(id)) n++;
                if (n < b.Need) continue;
                ActiveBonds.Add(b);
                foreach (var id in b.Members)
                {
                    if (!on.Contains(id)) continue;
                    _bondDmg.TryGetValue(id, out var v);
                    _bondDmg[id] = v + b.Dmg;
                }
                if (BondsEver.Add(b.Name))
                {
                    var at = new V2();
                    foreach (var h in Heroes) if (Array.IndexOf(b.Members, h.Def.Id) >= 0) { at = h.Pos; break; }
                    Emit(new SimEvent { Type = Ev.Bond, P = at, Val = b.Dmg, Text = b.Name });
                }
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
            MengCd = Math.Max(0, MengCd - dt);
            BaoziCd = Math.Max(0, BaoziCd - dt);
            ShuCd = Math.Max(0, ShuCd - dt);
            FuCd = Math.Max(0, FuCd - dt);
            TrapCd = Math.Max(0, TrapCd - dt);
            RallyT = Math.Max(0, RallyT - dt);
            _steleCd = Math.Max(0, _steleCd - dt);
            ShieldT = Math.Max(0, ShieldT - dt);
            RainT = Math.Max(0, RainT - dt);
            for (int c = 0; c < Board.Cols; c++)
                for (int r = 0; r < Board.Rows; r++)
                {
                    Board.Fog[c, r] = Math.Max(0, Board.Fog[c, r] - dt);
                    Board.Seal[c, r] = Math.Max(0, Board.Seal[c, r] - dt);
                    var p = Board.Pieces[c, r];
                    if (p != null && p.StunT > 0) p.StunT = Math.Max(0, p.StunT - dt);
                }
            foreach (var p in Camp) if (p != null && p.StunT > 0) p.StunT = Math.Max(0, p.StunT - dt);

            MoveEnemies(dt);
            if (Dead) return;
            TickBosses(dt);
            TickUnits(dt);
            TickHeroes(dt);
            TickStove(dt);
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
                e.SlowT = Math.Max(0, e.SlowT - dt);
                if (e.StunT > 0) { e.StunT -= dt; continue; }
                float spd = e.Def.Speed * globalMul * (e.HasteT > 0 ? 1.65f : 1f) * (e.Enraged ? 1.3f : 1f) * (e.SlowT > 0 ? Weapons.HookSlowMul : 1f);
                if (e.DashT > 0) { e.DashT -= dt; spd = Math.Max(spd, 2.8f); }
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
            int lost = heartsBefore - Hearts;
            if (!Dead && lost > 0)
            {
                int gift = Tuning.RainBuns * lost;
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
                if (e.Boss == null || e.Dead || e.InBrush) continue;
                if (!e.Enraged && e.Hp <= e.MaxHp * 0.5f)
                {
                    e.Enraged = true;
                    e.SkillT = Math.Min(e.SkillT, 0.4f);
                    Emit(new SimEvent { Type = Ev.BossRage, A = e.Id, P = e.Pos, Text = e.Boss.Name });
                }
                if (e.StunT > 0) continue;
                e.SkillT -= dt;
                if (e.SkillT > 0) continue;
                var skills = e.Boss.Skills;
                var skill = skills[e.SkillIx % skills.Length];
                e.SkillIx++;
                e.SkillT = CastBoss(e, skill) * (e.Enraged ? 0.75f : 1f);
            }
        }

        /// <summary>放一门绝技，返回下一次要等几秒。</summary>
        float CastBoss(Enemy e, BossSkill skill)
        {
            string shout = e.Boss.Name + "：";
            float cd, radius = 2.4f;
            switch (skill)
            {
                case BossSkill.Summon:
                    cd = 5.5f;
                    shout += "来人！";
                    for (int k = 0; k < 3; k++)
                    {
                        var s = M.SpawnOn(this, EnemyKind.Zu, Math.Max(0, e.Progress - 0.4f * (k + 1)));
                        if (s != null) Hex(e, skill, s.Pos, 0f);
                    }
                    break;
                case BossSkill.Haste:
                    cd = 4.5f;
                    shout += "快！快！";
                    radius = 3.4f;
                    foreach (var o in Enemies)
                        if (!o.Dead && V2.Dist(o.Pos, e.Pos) < 3.4f) o.HasteT = 3.2f;
                    break;
                case BossSkill.Daze:
                {
                    cd = 7f;
                    shout += "定！";
                    var list = new List<KeyValuePair<CellPos, Piece>>();
                    for (int c = 0; c < Board.Cols; c++)
                        for (int r = 0; r < Board.Rows; r++)
                        {
                            var p = Board.Pieces[c, r];
                            if (p != null && p.Type != PieceType.Shovel && p.StunT <= 0) list.Add(new KeyValuePair<CellPos, Piece>(new CellPos(c, r), p));
                        }
                    list.Sort((a, b) => b.Value.Level != a.Value.Level ? b.Value.Level.CompareTo(a.Value.Level) : a.Value.Id.CompareTo(b.Value.Id));
                    for (int k = 0; k < list.Count && k < 3; k++)
                    {
                        list[k].Value.StunT = 3f;
                        Hex(e, skill, list[k].Key.Center, 3f, list[k].Value.Id);
                    }
                    break;
                }
                case BossSkill.Fog:
                {
                    cd = 9f;
                    shout += "天昏地暗！";
                    int bc = 0, br = 0, best = -1;
                    for (int c = 0; c <= Board.Cols - 3; c++)
                        for (int r = 0; r <= Board.Rows - 2; r++)
                        {
                            int n = 0;
                            for (int dc = 0; dc < 3; dc++)
                                for (int dr = 0; dr < 2; dr++)
                                    if (Board.Open(c + dc, r + dr)) n += Board.Pieces[c + dc, r + dr] != null ? 3 : 1;
                            n = n * 8 + _rng.Range(0, 8);
                            if (n > best) { best = n; bc = c; br = r; }
                        }
                    for (int dc = 0; dc < 3; dc++)
                        for (int dr = 0; dr < 2; dr++)
                            Board.Fog[bc + dc, br + dr] = Math.Max(Board.Fog[bc + dc, br + dr], 5.5f);
                    Hex(e, skill, new V2(bc + 1.5f, br + 1f), 5.5f);
                    break;
                }
                case BossSkill.Seal:
                {
                    cd = 8f;
                    shout += "此处封了！";
                    var empty = new List<CellPos>();
                    var full = new List<CellPos>();
                    for (int c = 0; c < Board.Cols; c++)
                        for (int r = 0; r < Board.Rows; r++)
                        {
                            if (!Board.Open(c, r) || Board.Seal[c, r] > 0) continue;
                            (Board.Pieces[c, r] == null ? empty : full).Add(new CellPos(c, r));
                        }
                    for (int k = 0; k < 3; k++)
                    {
                        var from = empty.Count > 0 && (k < 2 || full.Count == 0) ? empty : full;
                        if (from.Count == 0) break;
                        int ix = _rng.Range(0, from.Count);
                        var cp = from[ix];
                        from.RemoveAt(ix);
                        Board.Seal[cp.c, cp.r] = 7f;
                        Hex(e, skill, cp.Center, 7f);
                    }
                    break;
                }
                case BossSkill.Rain:
                    cd = 8f;
                    shout += "风来！雨来！";
                    RainT = Math.Max(RainT, 4f);
                    radius = 99f;
                    Hex(e, skill, new V2(Board.Cols * 0.5f, Board.Rows * 0.5f), 4f);
                    break;
                case BossSkill.Charge:
                    cd = 6.5f;
                    shout += "冲啊！";
                    e.DashT = 0.5f;
                    foreach (var o in Enemies)
                        if (!o.Dead && !ReferenceEquals(o, e) && V2.Dist(o.Pos, e.Pos) < 1.6f) o.HasteT = Math.Max(o.HasteT, 1.5f);
                    break;
                case BossSkill.Crush:
                {
                    cd = 9f;
                    shout += "砸！";
                    Piece best = null;
                    var at = new CellPos();
                    for (int c = 0; c < Board.Cols; c++)
                        for (int r = 0; r < Board.Rows; r++)
                        {
                            var p = Board.Pieces[c, r];
                            if (p == null || p.Type != PieceType.Unit || p.Level < 2) continue;
                            if (best == null || p.Level > best.Level) { best = p; at = new CellPos(c, r); }
                        }
                    if (best != null)
                    {
                        best.Level--;
                        best.StunT = Math.Max(best.StunT, 1.5f);
                        Hex(e, skill, at.Center, best.Level, best.Id);
                    }
                    break;
                }
                case BossSkill.Steal:
                {
                    cd = 7f;
                    shout += "钱拿来！";
                    int n = Math.Min(Buns, 3 + Buns / 5);
                    Buns -= n;
                    Hex(e, skill, e.Pos, n);
                    break;
                }
                case BossSkill.Devour:
                {
                    cd = 6f;
                    shout += "吃了你！";
                    int eaten = 0;
                    foreach (var o in Enemies)
                    {
                        if (eaten >= 3) break;
                        if (o.Dead || o.IsBig || o.InBrush || V2.Dist(o.Pos, e.Pos) > 1.8f) continue;
                        o.Dead = true;
                        eaten++;
                        Hex(e, skill, o.Pos, 0f, o.Id);
                    }
                    float heal = e.MaxHp * (0.03f + 0.07f * eaten);
                    e.Hp = Math.Min(e.MaxHp, e.Hp + heal);
                    Hex(e, skill, e.Pos, heal, e.Id);
                    break;
                }
                case BossSkill.Revive:
                {
                    cd = 8f;
                    shout += "起来！";
                    int n = 0;
                    for (int k = _graves.Count - 1; k >= 0 && n < 4; k--, n++)
                    {
                        var s = M.SpawnOn(this, EnemyKind.Zu, _graves[k]);
                        if (s == null) break;
                        s.Hp = s.MaxHp *= 0.6f;
                        Hex(e, skill, s.Pos, 0f, s.Id);
                    }
                    _graves.Clear();
                    for (; n < 2; n++)
                    {
                        var s = M.SpawnOn(this, EnemyKind.Zu, Math.Max(0, e.Progress - 0.5f * (n + 1)));
                        if (s != null) Hex(e, skill, s.Pos, 0f, s.Id);
                    }
                    break;
                }
                default:
                    cd = 6f;
                    shout += "铁布衫！";
                    radius = 1.6f;
                    break;
            }
            Emit(new SimEvent { Type = Ev.Ult, A = e.Id, B = (int)skill, P = e.Pos, Val = radius, Text = shout, Flag = 1 });
            return cd;
        }

        void Hex(Enemy boss, BossSkill skill, V2 at, float val, int target = 0) =>
            Emit(new SimEvent { Type = Ev.BossHex, A = boss.Id, B = (int)skill, P = at, Q = boss.Pos, Val = val, Flag = target });

        /// <summary>被定身，或者站在封条上：这一刻不出手。</summary>
        bool Disabled(Piece p, int c, int r) => p.StunT > 0 || Board.Seal[c, r] > 0;

        bool Disabled(HeroInstance h)
        {
            for (int i = 0; i < h.Cells.Length; i++)
                if (Disabled(h.Pieces[i], h.Cells[i].c, h.Cells[i].r)) return true;
            return false;
        }

        float CdMul(int c, int r)
        {
            float m = 1f;
            m *= _teamCd;
            m *= _aura[c, r];
            if (HasteAllT > 0) m *= 0.5f;
            if (RallyT > 0) m *= 0.75f;
            if (ItemHasteT > 0) m *= 0.71f;
            if (RainT > 0) m *= 1.5f;
            if (Board.Fog[c, r] > 0) m *= 1.8f;
            return m;
        }

        void TickUnits(float dt)
        {
            for (int c = 0; c < Board.Cols; c++)
            {
                for (int r = 0; r < Board.Rows; r++)
                {
                    var p = Board.Pieces[c, r];
                    if (p == null || p.Type != PieceType.Unit || Disabled(p, c, r)) continue;
                    _unitCd.TryGetValue(p.Id, out var cd);
                    cd -= dt;
                    if (cd > 0) { _unitCd[p.Id] = cd; continue; }
                    var def = UnitDef.Get(p.Kind);
                    var pos = new CellPos(c, r).Center;
                    float range = def.RangeWith(Items.Far);
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

        /// <summary>开着时，带头好汉的大招不自己放，等玩家点技能钮。</summary>
        public bool ManualUlt;

        /// <summary>带头好汉：等级最高，同级看名字字数，再同就是先凑出来的。</summary>
        public HeroInstance LeadHero
        {
            get
            {
                HeroInstance best = null;
                foreach (var h in Heroes)
                {
                    if (best == null) { best = h; continue; }
                    if (h.Level != best.Level) { if (h.Level > best.Level) best = h; continue; }
                    if (h.Def.Name.Length != best.Def.Name.Length) { if (h.Def.Name.Length > best.Def.Name.Length) best = h; continue; }
                    if (h.Id < best.Id) best = h;
                }
                return best;
            }
        }

        public bool LeadUltReady
        {
            get { var h = LeadHero; return h != null && h.UltCd <= 0; }
        }

        /// <summary>这名好汉大招转满一圈要多久。兵器会把这一圈缩短。</summary>
        public float UltMax(HeroInstance h) => h.Def.UltCooldown * AffixOf(h.Def).UltCd;

        /// <summary>玩家点技能钮放带头好汉的大招。没有目标时不进冷却。</summary>
        public bool CastLeadUlt()
        {
            if (Dead || M.Over) return false;
            var h = LeadHero;
            if (h == null || h.UltCd > 0 || Disabled(h)) return false;
            _curHero = h;
            var aff = AffixOf(h.Def);
            bool ok = TryUlt(h, h.Pos, aff);
            _curHero = null;
            if (ok) h.UltCd = h.Def.UltCooldown * aff.UltCd;
            return ok;
        }

        void TickHeroes(float dt)
        {
            var lead = ManualUlt ? LeadHero : null;
            foreach (var h in Heroes)
            {
                var def = h.Def;
                h.Cd -= dt;
                h.UltCd -= dt;
                var pos = h.Pos;
                _curHero = h;
                var aff = AffixOf(def);
                if (Disabled(h)) continue;

                if (h != lead && h.UltCd <= 0 && TryUlt(h, pos, aff)) h.UltCd = def.UltCooldown * aff.UltCd;

                if (def.NoAttack) GainXp(h, dt * 0.35f);
                if (def.NoAttack || h.Cd > 0) continue;
                float range = def.RangeWith(Items.Far);
                if (range < 50f) range += aff.Range;
                var target = FindTarget(pos, range, def.PreferBig);
                if (target == null) { h.Cd = 0; continue; }
                var move = Weapons.MoveOf(GearOf(def));
                h.Cd = def.CooldownAt(h.Level) * CdMul(h.Cells[0].c, h.Cells[0].r) * aff.Cd * (move == WeaponMove.WideAxe ? Weapons.AxeCdMul : 1f);
                float dmg = HeroDamage(h, aff);
                int crit = 0;
                float critChance = def.CritChance + aff.Crit;
                if (critChance > 0 && _rng.Chance(critChance)) { dmg *= def.CritMul; crit = 1; }
                if (move == WeaponMove.FarBow) dmg *= 1f + FarBowBonus(V2.Dist(pos, target.Pos));
                Emit(new SimEvent { Type = Ev.HeroAttack, A = h.Id, B = (int)def.Id, P = pos, Q = target.Pos, Flag = crit });

                int pierce = def.Pierce + (def.Pierce > 1 ? aff.Pierce : 0);
                if (pierce > 1)
                {
                    var hits = PierceLine(pos, target.Pos, range, pierce, dmg, def);
                    if (move == WeaponMove.Snake)
                        foreach (var e in hits) if (!e.IsBig) e.StunT = Math.Max(e.StunT, Weapons.SnakeStun);
                }
                else
                {
                    if (move == WeaponMove.FirstCut && !target.Cut) { target.Cut = true; dmg *= Weapons.FirstCutMul; }
                    Hit(target, dmg, def, crit == 1);
                    float splash = def.Splash + aff.Splash;
                    if (move == WeaponMove.WideAxe) splash = Math.Max(splash, 0.6f) * Weapons.AxeSplashMul;
                    if (splash > 0) Splash(target, splash, dmg * 0.6f, def);
                    if (move == WeaponMove.Snake && !target.IsBig) target.StunT = Math.Max(target.StunT, Weapons.SnakeStun);
                }
                if (move == WeaponMove.Hook && target.Boss == null) target.SlowT = Math.Max(target.SlowT, Weapons.HookSlowT);
                float stun = def.HitStun + aff.Stun;
                if (stun > 0) target.StunT = Math.Max(target.StunT, stun);
                float knock = def.HitKnock + aff.Knock;
                if (move == WeaponMove.StaffKnock) knock = Math.Max(knock, Weapons.StaffKnock);
                if (knock > 0 && !target.IsBig) Knock(target, knock);
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

        bool TryUlt(HeroInstance h, V2 pos, in WeaponAffix aff)
        {
            var def = h.Def;
            float dmg = HeroDamage(h, aff) * aff.UltMul;
            bool any = Enemies.Count > 0;
            float radius = 0;
            switch (def.UltKind)
            {
                case UltKind.Buns:
                    Buns += def.UltN;
                    Emit(new SimEvent { Type = Ev.Buns, Val = def.UltN, P = pos });
                    if (def.Hidden) return true;
                    radius = 0.6f;
                    break;
                case UltKind.SlowAll:
                    if (!any) return false;
                    EnemySlowT = Math.Max(EnemySlowT, def.UltT);
                    radius = 99;
                    break;
                case UltKind.HasteAll:
                    if (!any) return false;
                    HasteAllT = Math.Max(HasteAllT, def.UltT);
                    radius = 99;
                    break;
                case UltKind.Combo:
                {
                    if (FindTarget(pos, def.UltR, false) == null) return false;
                    for (int i = 0; i < def.UltN; i++)
                    {
                        var t = FindTarget(pos, def.UltR, false);
                        if (t == null) break;
                        Hit(t, dmg * def.UltMul, def);
                    }
                    radius = def.UltR;
                    break;
                }
                case UltKind.Area:
                    radius = def.UltR;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius))
                    {
                        if (def.UltStun > 0) e.StunT = Math.Max(e.StunT, def.UltStun);
                        if (def.UltKnock > 0 && !e.IsBig) Knock(e, def.UltKnock);
                        if (def.UltMul > 0) Hit(e, dmg * def.UltMul, def);
                    }
                    break;
                case UltKind.Snipe:
                {
                    Enemy best = null;
                    foreach (var e in Enemies) if (!e.Dead && !e.InBrush && (best == null || e.Hp > best.Hp)) best = e;
                    if (best == null) return false;
                    Emit(new SimEvent { Type = Ev.HeroAttack, A = h.Id, B = (int)def.Id, P = pos, Q = best.Pos, Flag = 2 });
                    Hit(best, dmg * def.UltMul, def, true);
                    foreach (var e in InRange(pos, 99f)) if (e != best) Hit(e, dmg * 2f, def);
                    radius = 99f;
                    break;
                }
                case UltKind.Throw:
                {
                    Enemy best = null;
                    foreach (var e in InRange(pos, def.UltR)) if (best == null || e.MaxHp > best.MaxHp) best = e;
                    if (best == null) return false;
                    Knock(best, def.UltKnock);
                    Hit(best, dmg * def.UltMul, def, true);
                    radius = 1f;
                    break;
                }
                case UltKind.Blast:
                {
                    Enemy center = null;
                    int bestN = 0;
                    foreach (var e in InRange(pos, 4f))
                    {
                        int n = 0;
                        foreach (var o in Enemies) if (!o.Dead && !o.InBrush && V2.Dist(o.Pos, e.Pos) < def.UltR) n++;
                        if (n > bestN) { bestN = n; center = e; }
                    }
                    if (center == null) return false;
                    pos = center.Pos;
                    radius = def.UltR;
                    foreach (var e in InRange(pos, radius))
                    {
                        if (def.UltStun > 0) e.StunT = Math.Max(e.StunT, def.UltStun);
                        Hit(e, dmg * def.UltMul, def);
                    }
                    break;
                }
                case UltKind.Pin:
                {
                    var t = FindTarget(pos, Math.Min(def.Range, 50f) + 0.5f, def.PreferBig);
                    if (t == null) return false;
                    t.StunT = Math.Max(t.StunT, def.UltStun);
                    Hit(t, dmg * def.UltMul, def, true);
                    pos = t.Pos;
                    radius = 1f;
                    break;
                }
                case UltKind.Lasso:
                {
                    var list = InRange(pos, def.UltR);
                    if (list.Count == 0) return false;
                    list.Sort((a, b) => b.Progress.CompareTo(a.Progress));
                    for (int i = 0; i < list.Count && i < def.UltN; i++)
                    {
                        list[i].StunT = Math.Max(list[i].StunT, def.UltStun);
                        Hit(list[i], dmg * def.UltMul, def);
                    }
                    radius = def.UltR;
                    break;
                }
                case UltKind.Shovel:
                    if (Board.AnyDiggable() && !HasShovel())
                    {
                        GrantShovels(1);
                        radius = 0.5f;
                        break;
                    }
                    radius = def.UltR;
                    if (!AreaAny(pos, radius)) return false;
                    foreach (var e in InRange(pos, radius)) Hit(e, dmg * def.UltMul, def);
                    break;
                case UltKind.Volley:
                {
                    float r = def.Range >= 50f ? 99f : Math.Max(def.Range, 2f) + 1f;
                    var list = InRange(pos, r);
                    if (list.Count == 0) return false;
                    list.Sort((a, b) => b.Progress.CompareTo(a.Progress));
                    for (int i = 0; i < def.UltN; i++)
                    {
                        var t = list[i % list.Count];
                        Emit(new SimEvent { Type = Ev.HeroAttack, A = h.Id, B = (int)def.Id, P = pos, Q = t.Pos, Flag = 3 });
                        Hit(t, dmg * def.UltMul, def);
                    }
                    radius = r;
                    break;
                }
                case UltKind.Storm:
                    if (!AreaAny(pos, 99f)) return false;
                    foreach (var e in InRange(pos, 99f))
                    {
                        if (def.UltStun > 0) e.StunT = Math.Max(e.StunT, def.UltStun);
                        Hit(e, dmg * def.UltMul, def);
                    }
                    radius = 99;
                    break;
                case UltKind.Heal:
                {
                    if (Hp >= MaxHp) return false;
                    int before = Hp;
                    Hp = Math.Min(MaxHp, Hp + def.UltN);
                    Emit(new SimEvent { Type = Ev.Heal, Val = Hp - before, P = Board.Goal.Center, A = h.Id, B = (int)def.Id });
                    radius = 0.8f;
                    break;
                }
                case UltKind.Rally:
                    if (!any) return false;
                    ShieldT = Math.Max(ShieldT, def.UltT);
                    RallyT = Math.Max(RallyT, def.UltT);
                    radius = 99;
                    break;
                case UltKind.Shield:
                    if (!any) return false;
                    ShieldT = Math.Max(ShieldT, def.UltT);
                    radius = 0.8f;
                    pos = Board.Goal.Center;
                    break;
                case UltKind.Upgrade:
                {
                    Piece best = null;
                    for (int c = 0; c < Board.Cols; c++)
                        for (int r = 0; r < Board.Rows; r++)
                        {
                            var p = Board.Pieces[c, r];
                            if (p == null || p.Type != PieceType.Unit || p.Level >= UnitDef.MaxLevel) continue;
                            if (best == null || p.Level < best.Level) { best = p; pos = new CellPos(c, r).Center; }
                        }
                    if (best == null) return false;
                    MergeInto(best, pos);
                    radius = 0.6f;
                    break;
                }
                case UltKind.Steal:
                {
                    var other = M.Sides[1 - Index];
                    int n = other == null ? 0 : Math.Min(def.UltN, other.Buns);
                    if (n <= 0) return false;
                    other.Buns -= n;
                    Buns += n;
                    Emit(new SimEvent { Type = Ev.Steal, Val = n, P = pos, A = h.Id, B = (int)def.Id });
                    radius = 0.6f;
                    break;
                }
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
                if (e.Dead || e.InBrush || V2.Dist(pos, e.Pos) > range) continue;
                float s = e.Progress + (preferBig && e.IsBig ? 1000f : 0f);
                if (s > bestScore) { bestScore = s; best = e; }
            }
            return best;
        }

        bool AreaAny(V2 pos, float r)
        {
            foreach (var e in Enemies) if (!e.Dead && !e.InBrush && V2.Dist(pos, e.Pos) <= r) return true;
            return false;
        }

        List<Enemy> InRange(V2 pos, float r)
        {
            var list = new List<Enemy>();
            foreach (var e in Enemies) if (!e.Dead && !e.InBrush && V2.Dist(pos, e.Pos) <= r) list.Add(e);
            return list;
        }

        public static float FarBowBonus(float dist) => Math.Min(Weapons.FarBowCap, Math.Max(0f, dist) * Weapons.FarBowPerCell);

        List<Enemy> PierceLine(V2 from, V2 to, float range, int count, float dmg, HeroDef src)
        {
            var hit = new List<Enemy>();
            var dir = (to - from).Norm;
            var hits = new List<KeyValuePair<float, Enemy>>();
            foreach (var e in Enemies)
            {
                if (e.Dead || e.InBrush) continue;
                var d = e.Pos - from;
                float along = V2.Dot(d, dir);
                if (along < -0.2f || along > range + 0.4f) continue;
                var perp = d - dir * along;
                if (perp.Len > 0.42f) continue;
                hits.Add(new KeyValuePair<float, Enemy>(along, e));
            }
            hits.Sort((a, b) => a.Key.CompareTo(b.Key));
            for (int i = 0; i < hits.Count && i < count; i++)
            {
                hit.Add(hits[i].Value);
                Hit(hits[i].Value, dmg, src);
            }
            return hit;
        }

        void Splash(Enemy center, float radius, float dmg, HeroDef src)
        {
            foreach (var e in Enemies)
                if (!e.Dead && !e.InBrush && !ReferenceEquals(e, center) && V2.Dist(e.Pos, center.Pos) <= radius) Hit(e, dmg, src);
        }

        void Knock(Enemy e, float dist)
        {
            e.Progress = Math.Max(0, e.Progress - dist);
            e.Pos = Board.PosAt(e.Progress);
        }

        void Hit(Enemy e, float dmg, HeroDef src, bool crit = false)
        {
            if (e.Dead || e.InBrush) return;
            if (Items.Flag) dmg *= 1.22f;
            int flag = crit ? 1 : 0;
            if (e.Boss != null)
            {
                if (src != null && src.Id == e.Boss.Nemesis) { dmg *= Tuning.BossNemesisMul; flag = 2; }
                else if (e.Boss.Has(BossSkill.Resist))
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
            if (!e.IsBig)
            {
                _graves.Add(e.Progress);
                if (_graves.Count > 8) _graves.RemoveAt(0);
            }
            float raw = e.Def.Reward * e.RewardMul;
            foreach (var bh in _bunsHeroes)
                if (V2.Dist(bh.Pos, e.Pos) <= bh.Def.KillBuns) { raw += 1; break; }
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
