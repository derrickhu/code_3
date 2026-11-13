using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
    public enum WeaponKind { None, Blade, Spear, Bow, Axe, Club, Whip, Hammer, Fork, Sword, Stone, Rope, Staff, Halberd, Cannon, Relic, Fist }

    /// <summary>兵器按种类带一条特性，装备后在伤害倍率之外再生效。</summary>
    public struct WeaponAffix
    {
        public float Dmg, Range, Splash, Knock, Stun, Water, Crit, Cd, UltMul, UltCd;
        public int Pierce;

        public static readonly WeaponAffix Bare = new WeaponAffix { Dmg = 1f, Water = 1f, Cd = 1f, UltMul = 1f, UltCd = 1f };
    }

    /// <summary>六把名兵器在种类特性之外多一招。招式跟着兵器走，谁带都一样，大招仍是这个人自己的。</summary>
    public enum WeaponMove { None, Snake, FarBow, WideAxe, StaffKnock, FirstCut, Hook }

    public static class Weapons
    {
        public static readonly string[] Grades = { "凡器", "利器", "宝器", "神兵" };

        public const float SnakeStun = 0.25f;
        public const float FarBowPerCell = 0.06f;
        public const float FarBowCap = 0.3f;
        public const float AxeSplashMul = 1.35f;
        public const float AxeCdMul = 1.15f;
        public const float StaffKnock = 0.45f;
        public const float FirstCutMul = 1.6f;
        public const float HookSlowT = 1.5f;
        public const float HookSlowMul = 0.6f;
        /// <summary>原主带自己的兵器，伤害再乘这一截。</summary>
        public const float OwnerMul = 1.1f;

        /// <summary>按兵器出自谁认名兵器，不看是谁带着。</summary>
        public static WeaponMove MoveOf(HeroDef weapon)
        {
            if (weapon == null) return WeaponMove.None;
            return weapon.Id switch
            {
                HeroId.LinChong => WeaponMove.Snake,
                HeroId.HuaRong => WeaponMove.FarBow,
                HeroId.LiKui => WeaponMove.WideAxe,
                HeroId.LuZhiShen => WeaponMove.StaffKnock,
                HeroId.YangZhi => WeaponMove.FirstCut,
                HeroId.XuNing => WeaponMove.Hook,
                _ => WeaponMove.None,
            };
        }

        public static string MoveName(WeaponMove m) => m switch
        {
            WeaponMove.Snake => "蛇矛缠身",
            WeaponMove.FarBow => "百步穿杨",
            WeaponMove.WideAxe => "板斧开路",
            WeaponMove.StaffKnock => "禅杖扫街",
            WeaponMove.FirstCut => "宝刀头一刀",
            WeaponMove.Hook => "钩镰绊马",
            _ => "",
        };

        public static string MoveText(WeaponMove m) => m switch
        {
            WeaponMove.Snake => "穿过的每个人都定身 0.25 秒，站在直路上最好",
            WeaponMove.FarBow => "打得越远越疼，每格 +6%，最多 +30%",
            WeaponMove.WideAxe => "溅射再大三成半，出手慢一成半，守拐角",
            WeaponMove.StaffKnock => "命中把小兵打退一大步，守路口",
            WeaponMove.FirstCut => "每个官兵挨这把刀的头一下 ×1.6",
            WeaponMove.Hook => "命中的官兵慢四成，持续 1.5 秒",
            _ => "",
        };

        public static string KindName(WeaponKind k) => k switch
        {
            WeaponKind.Blade => "刀",
            WeaponKind.Spear => "枪",
            WeaponKind.Bow => "弓弩",
            WeaponKind.Axe => "斧",
            WeaponKind.Club => "棍棒",
            WeaponKind.Whip => "鞭锏",
            WeaponKind.Hammer => "锤",
            WeaponKind.Fork => "叉刺",
            WeaponKind.Sword => "剑",
            WeaponKind.Stone => "暗器",
            WeaponKind.Rope => "索",
            WeaponKind.Staff => "杖",
            WeaponKind.Halberd => "戟",
            WeaponKind.Cannon => "火炮",
            WeaponKind.Relic => "器物",
            WeaponKind.Fist => "拳脚",
            _ => "",
        };

        public static string Icon(WeaponKind k) => k switch
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
            WeaponKind.Relic => "wp_fan",
            WeaponKind.Fist => "wp_fist",
            _ => "wp_blade",
        };

        public static string AffixText(WeaponKind k) => k switch
        {
            WeaponKind.Blade => "刀沉力猛：伤害再 +10%",
            WeaponKind.Spear => "长枪及远：多穿透一人（不穿透的射程 +0.3）",
            WeaponKind.Bow => "弓劲弦满：射程 +0.4",
            WeaponKind.Axe => "开山之势：溅射范围 +40%",
            WeaponKind.Club => "棍扫一片：命中击退小兵",
            WeaponKind.Whip => "鞭锏砸骨：命中定身 0.3 秒",
            WeaponKind.Hammer => "重锤千钧：命中定身 0.2 秒并击退",
            WeaponKind.Fork => "分水破浪：岸上 +8%，水上合计 ×1.4",
            WeaponKind.Sword => "剑走偏锋：暴击率 +12%",
            WeaponKind.Stone => "暗器连珠：出手速度 +12%",
            WeaponKind.Rope => "套索缠身：命中定身再 +0.4 秒",
            WeaponKind.Staff => "杖法通神：大招伤害 +30%，命中击退小兵",
            WeaponKind.Halberd => "戟锋横扫：溅射 +0.5 格",
            WeaponKind.Cannon => "火药充足：大招冷却 -15%",
            WeaponKind.Relic => "随身宝物：大招冷却 -20%",
            WeaponKind.Fist => "拳快如风：出手速度 +15%",
            _ => "",
        };

        public static WeaponAffix Affix(HeroDef d) => Affix(d.WKind, d.Pierce, d.Splash);

        /// <summary>特性跟兵器走，穿透和溅射按带着它的人来算。</summary>
        public static WeaponAffix Affix(HeroDef weapon, HeroDef wearer) => Affix(weapon.WKind, wearer.Pierce, wearer.Splash);

        public static WeaponAffix Affix(WeaponKind kind, int pierce, float splash)
        {
            var a = WeaponAffix.Bare;
            switch (kind)
            {
                case WeaponKind.Blade: a.Dmg = 1.1f; break;
                case WeaponKind.Spear: if (pierce > 1) a.Pierce = 1; else a.Range = 0.3f; break;
                case WeaponKind.Bow: a.Range = 0.4f; break;
                case WeaponKind.Axe: a.Splash = splash > 0 ? splash * 0.4f : 0.5f; break;
                case WeaponKind.Club: a.Knock = 0.25f; break;
                case WeaponKind.Whip: a.Stun = 0.3f; break;
                case WeaponKind.Hammer: a.Stun = 0.2f; a.Knock = 0.2f; break;
                case WeaponKind.Fork: a.Water = 1.3f; a.Dmg = 1.08f; break;
                case WeaponKind.Sword: a.Crit = 0.12f; break;
                case WeaponKind.Stone: a.Cd = 0.88f; break;
                case WeaponKind.Rope: a.Stun = 0.4f; break;
                case WeaponKind.Staff: a.UltMul = 1.3f; a.Knock = 0.2f; break;
                case WeaponKind.Halberd: a.Splash = 0.5f; break;
                case WeaponKind.Cannon: a.UltCd = 0.85f; break;
                case WeaponKind.Relic: a.UltCd = 0.8f; break;
                case WeaponKind.Fist: a.Cd = 0.85f; break;
            }
            return a;
        }

        /// <summary>掉落权重：神兵最难得。</summary>
        public static int DropWeight(int grade) => grade switch { 3 => 1, 2 => 2, 1 => 3, _ => 4 };

        /// <summary>
        /// 缴获谁的兵器：先看这局凑出来的、已经入伙的；一个都没有，才从这一回里已经入伙的人里抽。
        /// 还没入伙的不掉，所以没过关之前，这一回才出场的人兵器不会亮。
        /// </summary>
        public static void CollectDropCandidates(IList<HeroId> formed, IList<HeroId> pool, Func<HeroId, bool> joined, List<HeroId> into)
        {
            if (formed != null)
                foreach (var id in formed)
                    if (joined(id) && !string.IsNullOrEmpty(HeroDef.Get(id).Weapon)) into.Add(id);
            if (into.Count > 0 || pool == null) return;
            foreach (var id in pool)
                if (joined(id) && !string.IsNullOrEmpty(HeroDef.Get(id).Weapon)) into.Add(id);
        }
    }

    /// <summary>
    /// 难度阶梯：按掉落规则推算「正常进度」玩家此时兵器架上有什么，对手照着带；
    /// 玩家比阶梯强或弱，对手各跟一半。兵器始终有用，但不会越玩越简单，落后的人也不至于被碾。
    /// 分量 = 每件兵器的（伤害倍率 - 1）相加。
    /// </summary>
    public static class WeaponLadder
    {
        public const float Follow = 0.5f;
        public const int BaseSlots = 3;
        public const int MaxSlots = 5;
        /// <summary>每日一战按第几回的进度配兵器。</summary>
        public const int DailyAs = 10;
        /// <summary>推算用：每回平均打几局才过、每局缴获的几率（撑过四波再乘掉率）。</summary>
        public const float BattlesPerChapter = 1.7f;
        public const float DropPerBattle = 0.65f;

        public struct Slot
        {
            public int Grade, Tier;
            public float Power => SlotPower(Tier, Grade);
        }

        static List<Slot>[] _story;

        /// <summary>进第 c 回时，正常玩家兵器架上最好的三件（按期望缴获件数推算）。</summary>
        static List<Slot> StoryRack(int c)
        {
            if (_story == null) BuildStory();
            return _story[Math.Max(0, Math.Min(_story.Length - 1, c))];
        }

        static void BuildStory()
        {
            int n = ChapterDef.StoryCount;
            _story = new List<Slot>[n + 1];
            var copies = new Dictionary<HeroId, float>();
            var joined = new HashSet<HeroId>();
            void Drop(ChapterDef ch, float drops)
            {
                var cands = new List<HeroDef>();
                foreach (var id in ch.Pool)
                {
                    var d = HeroDef.Get(id);
                    if (joined.Contains(id) && !string.IsNullOrEmpty(d.Weapon)) cands.Add(d);
                }
                float total = 0f;
                foreach (var d in cands) total += Weapons.DropWeight(d.WGrade);
                foreach (var d in cands)
                {
                    copies.TryGetValue(d.Id, out var k);
                    copies[d.Id] = k + drops * Weapons.DropWeight(d.WGrade) / total;
                }
            }
            for (int c = 0; c <= n; c++)
            {
                var rack = new List<Slot>();
                foreach (var kv in copies)
                {
                    if (kv.Value < 0.5f) continue;
                    var d = HeroDef.Get(kv.Key);
                    if (d.NoAttack) continue;
                    rack.Add(new Slot { Grade = d.WGrade, Tier = Math.Max(1, Math.Min(5, (int)Math.Round(kv.Value))) });
                }
                rack.Sort((x, y) => y.Power.CompareTo(x.Power));
                if (rack.Count > BaseSlots) rack.RemoveRange(BaseSlots, rack.Count - BaseSlots);
                _story[c] = rack;
                if (c == n) break;
                var ch = ChapterDef.All[c];
                Drop(ch, (BattlesPerChapter - 1f) * DropPerBattle);
                foreach (var id in ch.Joins) joined.Add(id);
                Drop(ch, DropPerBattle);
            }
        }

        /// <summary>这一局正常进度的兵器架。车轮战在通关的底子上，越往后越接近五格满配。</summary>
        public static List<Slot> Expected(ChapterDef ch)
        {
            if (ch.IsEndless)
            {
                var end = StoryRack(ChapterDef.StoryCount);
                int slots = BaseSlots + (ch.Round >= 8 ? 1 : 0) + (ch.Round >= 16 ? 1 : 0);
                int bonus = Math.Min(4, ch.Round / 5);
                var list = new List<Slot>();
                for (int i = 0; i < slots; i++)
                {
                    var b = i < end.Count ? end[i] : new Slot { Grade = 3, Tier = 1 };
                    list.Add(new Slot { Grade = b.Grade, Tier = Math.Min(5, b.Tier + bonus) });
                }
                return list;
            }
            return new List<Slot>(StoryRack(ch.IsDaily ? DailyAs : ch.Index));
        }

        public static float SlotPower(int tier, int grade) => HeroDef.ArmedMul(tier, grade) - 1f;

        public static float Power(IList<Slot> rack)
        {
            float p = 0f;
            foreach (var s in rack) p += s.Power;
            return p;
        }

        public static float ExpectedPower(ChapterDef ch) => Power(Expected(ch));

        public static float Power(Loadout l)
        {
            if (l == null || l.Rack == null) return 0f;
            float p = 0f;
            for (int i = 0; i < l.Rack.Count; i++)
            {
                int w = l.Rack[i];
                if (w < 0 || w >= HeroDef.All.Length) continue;
                int lv = l.RackLv != null && i < l.RackLv.Count ? l.RackLv[i] : 1;
                p += SlotPower(Math.Max(1, Math.Min(5, lv)), HeroDef.Get((HeroId)w).WGrade);
            }
            return p;
        }

        public static float RivalTarget(ChapterDef ch, float player)
        {
            float e = ExpectedPower(ch);
            return Math.Max(0f, e + Follow * (player - e));
        }

        /// <summary>对手的兵器架：从阶梯出发，玩家强就一步步加阶、加格、提品级，弱就一步步减，直到贴近目标分量。</summary>
        public static Loadout ForRival(ChapterDef ch, float playerPower)
        {
            float target = RivalTarget(ch, playerPower);
            var rack = Expected(ch);
            while (Power(rack) > target + 0.05f && rack.Count > 0)
            {
                int i = rack.Count - 1;
                var s = rack[i];
                if (s.Tier > 1) { s.Tier--; rack[i] = s; }
                else rack.RemoveAt(i);
                rack.Sort((x, y) => y.Power.CompareTo(x.Power));
            }
            while (true)
            {
                float left = target - Power(rack);
                int best = -1;
                Slot bestSlot = default;
                float gain = 0f;
                void Try(int at, Slot s)
                {
                    float g = s.Power - (at < rack.Count ? rack[at].Power : 0f);
                    if (g > 0f && g <= left + 0.05f && g > gain) { gain = g; best = at; bestSlot = s; }
                }
                for (int i = 0; i < rack.Count; i++)
                {
                    var s = rack[i];
                    if (s.Tier < 5) Try(i, new Slot { Grade = s.Grade, Tier = s.Tier + 1 });
                    if (s.Grade < 3) Try(i, new Slot { Grade = s.Grade + 1, Tier = s.Tier });
                }
                if (rack.Count < MaxSlots) Try(rack.Count, new Slot { Grade = rack.Count > 0 ? rack[0].Grade : 0, Tier = 1 });
                if (best < 0) break;
                if (best < rack.Count) rack[best] = bestSlot;
                else rack.Add(bestSlot);
            }
            return Arm(ch, rack);
        }

        /// <summary>
        /// 给每一格挑一件具体的兵器：只挑本回不出场的人的普通兵器。
        /// 这样对手只拼分量，不白拿原主加成和名兵招式，阶梯算出来多少就是多少。
        /// </summary>
        public static Loadout Arm(ChapterDef ch, IList<Slot> rack)
        {
            var l = new Loadout { Rack = new List<int>(), RackLv = new List<int>() };
            foreach (var s in rack)
                foreach (var d in HeroDef.All)
                {
                    if (d.WGrade != s.Grade || string.IsNullOrEmpty(d.Weapon) || d.NoAttack || d.Hidden) continue;
                    if (Array.IndexOf(ch.Pool, d.Id) >= 0 || Weapons.MoveOf(d) != WeaponMove.None || l.Rack.Contains((int)d.Id)) continue;
                    l.Rack.Add((int)d.Id);
                    l.RackLv.Add(s.Tier);
                    break;
                }
            return l;
        }
    }

    /// <summary>羁绊质变机制：除了加伤害，不同渊源的好汉同场还会触发贴合水浒原著的战场特效。</summary>
    public enum BondTrait { WaveWater, PierceCrit, BanditBuns, MysticUlt, SwornGuard }

    /// <summary>羁绊：同时凑出若干名有渊源的好汉，他们伤害提高并激活原著特技。</summary>
    public sealed class Bond
    {
        public string Name, Story;
        public HeroId[] Members;
        public int Need;
        public float Dmg;
        public BondTrait Trait;

        public static string TraitName(BondTrait t) => t switch
        {
            BondTrait.WaveWater => "水泊掀浪",
            BondTrait.PierceCrit => "铁骑破甲",
            BondTrait.BanditBuns => "劫富犒军",
            BondTrait.MysticUlt => "奇门借风",
            _ => "生死护义",
        };

        public static string TraitDesc(BondTrait t) => t switch
        {
            BondTrait.WaveWater => "旱地亦享四成水战威势，水上命中绊缓官兵",
            BondTrait.PierceCrit => "暴击率+12%，对铁甲与头目破甲增伤10%",
            BondTrait.BanditBuns => "每斩五敌多得1炊饼，斩头目立得3炊饼",
            BondTrait.MysticUlt => "绝技冷却缩短20%，奇门频出",
            _ => "出手加快15%，结义瞬间为宋江张开护体金光",
        };

        public string EffectSummary => $"伤害+{Math.Round(Dmg * 100)}% · {TraitName(Trait)}（{TraitDesc(Trait)}）";

        public static readonly Bond[] All =
        {
            new Bond { Name = "阮氏三雄", Need = 2, Dmg = 0.25f, Trait = BondTrait.WaveWater, Story = "石碣村打鱼的三兄弟，同心智取生辰纲",
                Members = new[] { HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi } },
            new Bond { Name = "五虎上将", Need = 2, Dmg = 0.15f, Trait = BondTrait.PierceCrit, Story = "梁山马军五虎将",
                Members = new[] { HeroId.GuanSheng, HeroId.LinChong, HeroId.QinMing, HeroId.HuYanZhuo, HeroId.DongPing } },
            new Bond { Name = "八骠骑", Need = 2, Dmg = 0.12f, Trait = BondTrait.PierceCrit, Story = "马军八骠骑兼先锋使",
                Members = new[] { HeroId.HuaRong, HeroId.XuNing, HeroId.YangZhi, HeroId.SuoChao, HeroId.ZhangQing, HeroId.ZhuTong, HeroId.ShiJin, HeroId.MuHong } },
            new Bond { Name = "二龙山", Need = 2, Dmg = 0.15f, Trait = BondTrait.BanditBuns, Story = "鲁智深、杨志、武松同占二龙山",
                Members = new[] { HeroId.LuZhiShen, HeroId.YangZhi, HeroId.WuSong, HeroId.CaoZheng, HeroId.ShiEn, HeroId.ZhangQingY, HeroId.SunErNiang } },
            new Bond { Name = "七星聚义", Need = 3, Dmg = 0.15f, Trait = BondTrait.MysticUlt, Story = "晁盖与七人聚义，智取生辰纲",
                Members = new[] { HeroId.WuYong, HeroId.GongSunSheng, HeroId.LiuTang, HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi, HeroId.BaiSheng } },
            new Bond { Name = "张氏兄弟", Need = 2, Dmg = 0.2f, Trait = BondTrait.WaveWater, Story = "浔阳江上船火儿与浪里白条",
                Members = new[] { HeroId.ZhangHeng, HeroId.ZhangShun } },
            new Bond { Name = "解氏兄弟", Need = 2, Dmg = 0.2f, Trait = BondTrait.SwornGuard, Story = "登州打虎的两头蛇与双尾蝎",
                Members = new[] { HeroId.XieZhen, HeroId.XieBao } },
            new Bond { Name = "翠屏山", Need = 2, Dmg = 0.18f, Trait = BondTrait.SwornGuard, Story = "杨雄、石秀结为兄弟，带时迁投梁山",
                Members = new[] { HeroId.YangXiong, HeroId.ShiXiu, HeroId.ShiQian } },
            new Bond { Name = "少华山", Need = 2, Dmg = 0.15f, Trait = BondTrait.BanditBuns, Story = "史进与朱武、陈达、杨春不打不相识",
                Members = new[] { HeroId.ShiJin, HeroId.ZhuWu, HeroId.ChenDa, HeroId.YangChun } },
            new Bond { Name = "十字坡夫妻", Need = 2, Dmg = 0.18f, Trait = BondTrait.BanditBuns, Story = "张青、孙二娘夫妻开店",
                Members = new[] { HeroId.ZhangQingY, HeroId.SunErNiang } },
            new Bond { Name = "王英扈三娘", Need = 2, Dmg = 0.18f, Trait = BondTrait.SwornGuard, Story = "宋江作主，一丈青嫁了矮脚虎",
                Members = new[] { HeroId.WangYing, HeroId.HuSanNiang } },
            new Bond { Name = "登州孙家", Need = 2, Dmg = 0.15f, Trait = BondTrait.BanditBuns, Story = "孙立、孙新、顾大嫂登州劫牢",
                Members = new[] { HeroId.SunLi, HeroId.SunXin, HeroId.GuDaSao, HeroId.YueHe } },
            new Bond { Name = "芒砀三杰", Need = 2, Dmg = 0.18f, Trait = BondTrait.MysticUlt, Story = "混世魔王樊瑞与左右手项充、李衮",
                Members = new[] { HeroId.FanRui, HeroId.XiangChong, HeroId.LiGun } },
            new Bond { Name = "画戟双英", Need = 2, Dmg = 0.2f, Trait = BondTrait.PierceCrit, Story = "吕方、郭盛对影山斗戟，花荣一箭射开",
                Members = new[] { HeroId.LvFang, HeroId.GuoSheng } },
            new Bond { Name = "揭阳水军", Need = 2, Dmg = 0.15f, Trait = BondTrait.WaveWater, Story = "李俊与童威、童猛浔阳江上做私商",
                Members = new[] { HeroId.LiJun, HeroId.TongWei, HeroId.TongMeng, HeroId.LiLi } },
            new Bond { Name = "铁牛与哥哥", Need = 2, Dmg = 0.2f, Trait = BondTrait.SwornGuard, Story = "黑旋风生死追随宋江",
                Members = new[] { HeroId.SongJiang, HeroId.LiKui } },
            new Bond { Name = "主仆情深", Need = 2, Dmg = 0.2f, Trait = BondTrait.SwornGuard, Story = "浪子燕青舍命救主卢俊义",
                Members = new[] { HeroId.LuJunYi, HeroId.YanQing } },
            new Bond { Name = "郓城都头", Need = 2, Dmg = 0.18f, Trait = BondTrait.SwornGuard, Story = "朱仝、雷横先后私放晁盖、宋江",
                Members = new[] { HeroId.ZhuTong, HeroId.LeiHeng } },
            new Bond { Name = "水火二将", Need = 2, Dmg = 0.2f, Trait = BondTrait.MysticUlt, Story = "凌州圣水将、神火将",
                Members = new[] { HeroId.ShanTingGui, HeroId.WeiDingGuo } },
            new Bond { Name = "江州好汉", Need = 2, Dmg = 0.12f, Trait = BondTrait.SwornGuard, Story = "江州劫法场救宋江",
                Members = new[] { HeroId.DaiZong, HeroId.LiKui, HeroId.ZhangShun, HeroId.LiJun, HeroId.MuHong } },
        };
    }
}
