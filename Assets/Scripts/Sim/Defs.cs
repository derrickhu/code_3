using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
    public enum UnitKind { Dao, Qiang, Gong, Qi }

    /// <summary>Shovel 是鲁智深的方便铲：拖到紧挨白地的草地上开出新地。</summary>
    public enum PieceType { Unit, HeroChar, Shovel }

    public sealed class Piece
    {
        static int _nextId = 1;

        public readonly int Id;
        public PieceType Type;
        public UnitKind Kind;
        public char Char;
        public int Level = 1;

        public Piece() { Id = _nextId++; }

        public static Piece Unit(UnitKind k, int level = 1) => new Piece { Type = PieceType.Unit, Kind = k, Level = level };
        public static Piece Hero(char c, int level = 1) => new Piece { Type = PieceType.HeroChar, Char = c, Level = level };
        public static Piece Shovel() => new Piece { Type = PieceType.Shovel };

        public char Glyph => Type switch
        {
            PieceType.Unit => UnitDef.Get(Kind).Glyph,
            PieceType.HeroChar => Char,
            _ => '铲',
        };
    }

    public sealed class UnitDef
    {
        public UnitKind Kind;
        public char Glyph;
        public string Role;
        public float Range, Cooldown, Damage;
        public int Pierce = 1;
        public float Splash;
        public float Knockback;

        public const int MaxLevel = 5;

        public float DamageAt(int level) => Damage * (float)Math.Pow(Tuning.LevelMul, level - 1);
        public float CooldownAt(int level) => Cooldown * (1f - 0.06f * (level - 1));

        public string TypeLabel => Pierce > 1 ? "穿透" : Splash > 0 ? "范围" : Knockback > 0 ? "击退" : "单体";

        static readonly UnitDef[] All =
        {
            new UnitDef { Kind = UnitKind.Dao, Glyph = '刀', Role = "近战劈砍，附带小范围溅射", Range = 1.35f, Cooldown = 0.9f, Damage = 3.2f, Splash = 0.5f },
            new UnitDef { Kind = UnitKind.Qiang, Glyph = '枪', Role = "直线突刺，穿透三人", Range = 2.3f, Cooldown = 1.0f, Damage = 2.4f, Pierce = 3 },
            new UnitDef { Kind = UnitKind.Gong, Glyph = '弓', Role = "远程放箭", Range = 3.3f, Cooldown = 1.15f, Damage = 2.7f },
            new UnitDef { Kind = UnitKind.Qi, Glyph = '骑', Role = "快速冲撞，击退敌人", Range = 1.7f, Cooldown = 0.5f, Damage = 1.3f, Knockback = 0.22f },
        };

        public static UnitDef Get(UnitKind k) => All[(int)k];
    }

    public enum HeroId
    {
        WuSong, LinChong, LiKui, HuaRong, DaiZong, YanQing, QinMing, ZhangShun, ZhangHeng,
        RuanXiaoEr, RuanXiaoQi, SunErNiang, HuSanNiang, LuZhiShen, WuYong, WuDaLang,
    }

    public sealed class HeroDef
    {
        public HeroId Id;
        public string Name;
        public string Nick;
        public string Skill;
        public string Ult;
        public string Quote;
        public float Range, Cooldown, Damage;
        public int Pierce = 1;
        public float Splash;
        public float WaterBonus = 1f;
        public float UltCooldown = 12f;
        public bool Hidden;
        public bool NoAttack;

        public float DamageAt(int level) => Damage * (float)Math.Pow(Tuning.LevelMul, level - 1);

        /// <summary>地煞星（紫框）；其余是天罡星（金框）。</summary>
        public bool Disha => Id == HeroId.SunErNiang || Id == HeroId.HuSanNiang;

        public string TypeLabel => NoAttack ? "辅助" : Pierce > 1 ? "穿透" : Splash > 0 ? "范围" : "单体";

        public static readonly HeroDef[] All =
        {
            new HeroDef { Id = HeroId.WuSong, Name = "武松", Nick = "行者", Range = 1.45f, Cooldown = 0.35f, Damage = 4.5f,
                Skill = "近身连拳，出手极快", Ult = "醉打：对最前方的敌人连打十八拳", Quote = "三碗不过冈？俺偏要过！", UltCooldown = 11f },
            new HeroDef { Id = HeroId.LinChong, Name = "林冲", Nick = "豹子头", Range = 2.6f, Cooldown = 0.8f, Damage = 6f, Pierce = 5,
                Skill = "丈八蛇矛，一枪穿五人", Ult = "风雪山神庙：冻住周围敌人", Quote = "风雪夜，正好杀人。", UltCooldown = 13f },
            new HeroDef { Id = HeroId.LiKui, Name = "李逵", Nick = "黑旋风", Range = 1.7f, Cooldown = 0.7f, Damage = 5f, Splash = 1.2f,
                Skill = "双斧乱砍，大范围伤害", Ult = "旋风：一圈板斧清场", Quote = "杀去东京，夺了鸟位！", UltCooldown = 12f },
            new HeroDef { Id = HeroId.HuaRong, Name = "花荣", Nick = "小李广", Range = 99f, Cooldown = 0.9f, Damage = 7f,
                Skill = "射程覆盖全场，优先射头目", Ult = "神箭：一箭重创血最厚的敌人", Quote = "看我射那雁行第三只。", UltCooldown = 10f },
            new HeroDef { Id = HeroId.DaiZong, Name = "戴宗", Nick = "神行太保", NoAttack = true,
                Skill = "不出手；全队出手速度 +15%", Ult = "神行：全场敌人减速一半", Quote = "日行八百，片刻便到。", UltCooldown = 12f },
            new HeroDef { Id = HeroId.YanQing, Name = "燕青", Nick = "浪子", Range = 1.45f, Cooldown = 0.5f, Damage = 4f,
                Skill = "三成概率暴击，三倍伤害", Ult = "相扑：把头目摔回两格", Quote = "献丑了。", UltCooldown = 10f },
            new HeroDef { Id = HeroId.QinMing, Name = "秦明", Nick = "霹雳火", Range = 2.1f, Cooldown = 1.0f, Damage = 5f, Splash = 1.0f,
                Skill = "狼牙棒砸地，范围伤害", Ult = "霹雳：在敌人最密处炸开", Quote = "谁敢与我大战三百合！", UltCooldown = 11f },
            new HeroDef { Id = HeroId.ZhangShun, Name = "张顺", Nick = "浪里白条", Range = 2.3f, Cooldown = 0.7f, Damage = 5f, WaterBonus = 3f,
                Skill = "站在水上伤害×3", Ult = "浪里白条：拖人下水，范围重创", Quote = "下得水来，叫你吃个饱。", UltCooldown = 12f },
            new HeroDef { Id = HeroId.ZhangHeng, Name = "张横", Nick = "船火儿", Range = 2.4f, Cooldown = 0.9f, Damage = 6f, Splash = 0.8f, WaterBonus = 2f,
                Skill = "站在水上伤害×2，范围伤害", Ult = "撞船：把周围敌人撞回去", Quote = "老爷生长在江边。", UltCooldown = 11f },
            new HeroDef { Id = HeroId.RuanXiaoEr, Name = "阮小二", Nick = "立地太岁", Range = 2.5f, Cooldown = 0.6f, Damage = 9f, WaterBonus = 2f,
                Skill = "三字好汉，水上伤害×2", Ult = "掀浪：范围重创", Quote = "这腔热血，只卖与识货的！", UltCooldown = 11f },
            new HeroDef { Id = HeroId.RuanXiaoQi, Name = "阮小七", Nick = "活阎罗", Range = 2.5f, Cooldown = 0.45f, Damage = 8f, WaterBonus = 2f,
                Skill = "三字好汉，出手快，水上伤害×2", Ult = "活阎罗：重创并定住一人", Quote = "老爷只是个打鱼的。", UltCooldown = 10f },
            new HeroDef { Id = HeroId.SunErNiang, Name = "孙二娘", Nick = "母夜叉", Range = 1.9f, Cooldown = 0.8f, Damage = 6f,
                Skill = "身边敌人被杀多掉一个炊饼", Ult = "蒙汗药：周围敌人昏睡三秒", Quote = "客官，里边请——", UltCooldown = 13f },
            new HeroDef { Id = HeroId.HuSanNiang, Name = "扈三娘", Nick = "一丈青", Range = 2.4f, Cooldown = 0.9f, Damage = 7f,
                Skill = "红锦套索，命中定身", Ult = "一丈青：同时套住五人", Quote = "看索！", UltCooldown = 11f },
            new HeroDef { Id = HeroId.LuZhiShen, Name = "鲁智深", Nick = "花和尚", Range = 1.55f, Cooldown = 0.9f, Damage = 14f, Splash = 1.0f,
                Skill = "禅杖横扫，范围重击", Ult = "方便铲：送一把铲子到山寨，开出新地", Quote = "洒家来也！", UltCooldown = 14f },
            new HeroDef { Id = HeroId.WuYong, Name = "吴用", Nick = "智多星", NoAttack = true,
                Skill = "不出手；周围八格出手速度 +30%", Ult = "智取：全队出手速度翻倍三秒", Quote = "小生略施小计。", UltCooldown = 12f },
            new HeroDef { Id = HeroId.WuDaLang, Name = "武大郎", Nick = "三寸丁谷树皮", NoAttack = true, Hidden = true,
                Skill = "隐藏好汉。不出手，每五秒烙三个炊饼", Ult = "", Quote = "炊饼——热乎的炊饼——", UltCooldown = 5f },
        };

        public static HeroDef Get(HeroId id) => All[(int)id];
    }

    public enum EnemyKind { Zu, Chai, Jia, Bu, Boss }

    public sealed class EnemyDef
    {
        public EnemyKind Kind;
        public string Glyph;
        public float HpMul, Speed, Armor;
        public int LeakDamage, Reward;

        static readonly EnemyDef[] All =
        {
            new EnemyDef { Kind = EnemyKind.Zu, Glyph = "卒", HpMul = 1f, Speed = 0.95f, LeakDamage = 1, Reward = 1 },
            new EnemyDef { Kind = EnemyKind.Chai, Glyph = "差", HpMul = 0.6f, Speed = 1.55f, LeakDamage = 1, Reward = 1 },
            new EnemyDef { Kind = EnemyKind.Jia, Glyph = "甲", HpMul = 2.3f, Speed = 0.7f, Armor = 0.35f, LeakDamage = 2, Reward = 2 },
            new EnemyDef { Kind = EnemyKind.Bu, Glyph = "捕", HpMul = 9f, Speed = 0.85f, LeakDamage = 4, Reward = 5 },
            new EnemyDef { Kind = EnemyKind.Boss, Glyph = "", HpMul = 45f, Speed = 0.45f, LeakDamage = 12, Reward = 20 },
        };

        public static EnemyDef Get(EnemyKind k) => All[(int)k];
    }

    public enum BossSkill { Summon, Haste, Resist }

    public sealed class BossDef
    {
        public string Name;
        public string Title;
        public BossSkill Skill;
        public HeroId Nemesis;
        public string SkillText;
    }

    /// <summary>Grass/Marsh 未开垦不能摆；Tile/Water 是开好的地。</summary>
    public enum Cell { Grass, Path, Water, Tile, Rock, Marsh }

    public sealed class ChapterDef
    {
        public int Index;
        public string Name;
        public string Story;
        public string[] Map;
        public HeroId[] Pool;
        public BossDef Boss;
        public float RivalSkill;
        public float RivalIncome = 1f;
        public string RivalName;

        public static readonly ChapterDef[] All =
        {
            new ChapterDef
            {
                Index = 0, Name = "清风寨", RivalName = "燕顺", RivalSkill = 0.25f, RivalIncome = 0.6f,
                Story = "宋江夜看花灯，被清风寨知寨刘高认出，五花大绑押进寨中。花荣连夜点兵：救哥哥！",
                Map = new[]
                {
                    "S#######",
                    ".ooo...#",
                    ".#######",
                    ".#..ooo.",
                    ".######.",
                    "......#G",
                },
                Pool = new[] { HeroId.HuaRong, HeroId.QinMing, HeroId.WuSong, HeroId.YanQing, HeroId.WuDaLang },
                Boss = new BossDef { Name = "刘高", Title = "清风寨知寨", Skill = BossSkill.Summon, Nemesis = HeroId.HuaRong, SkillText = "每隔几秒招来两个寨兵" },
            },
            new ChapterDef
            {
                Index = 1, Name = "江州法场", RivalName = "晁盖", RivalSkill = 0.55f, RivalIncome = 0.82f,
                Story = "浔阳楼上一首反诗，宋江被判斩首。午时三刻，十字路口，刽子手已经举刀——",
                Map = new[]
                {
                    "S#.....o",
                    ".#o####.",
                    ".#o#oo#.",
                    ".###oo#.",
                    "......#.",
                    "......#G",
                },
                Pool = new[] { HeroId.LiKui, HeroId.DaiZong, HeroId.LinChong, HeroId.HuaRong, HeroId.SunErNiang, HeroId.HuSanNiang, HeroId.WuDaLang },
                Boss = new BossDef { Name = "蔡九", Title = "江州知府", Skill = BossSkill.Haste, Nemesis = HeroId.LiKui, SkillText = "催促身边官兵加速冲锋" },
            },
            new ChapterDef
            {
                Index = 2, Name = "白龙庙", RivalName = "李俊", RivalSkill = 0.8f, RivalIncome = 0.92f,
                Story = "劫了法场，众好汉护着宋江退到江边白龙庙，前有大江，后有追兵。浪里白条，下水！",
                Map = new[]
                {
                    "~~~~~~~S",
                    "########",
                    "#.wwoo~.",
                    "########",
                    "..ww~~.#",
                    "G#######",
                },
                Pool = new[] { HeroId.ZhangShun, HeroId.ZhangHeng, HeroId.RuanXiaoEr, HeroId.RuanXiaoQi, HeroId.LiKui, HeroId.LuZhiShen, HeroId.WuYong },
                Boss = new BossDef { Name = "黄文炳", Title = "通判", Skill = BossSkill.Resist, Nemesis = HeroId.ZhangShun, SkillText = "老奸巨猾，只受一半伤害（张顺例外）" },
            },
        };
    }

    public static class Tuning
    {
        public const int Cols = 8;
        public const int Rows = 6;
        public const int CampSlots = 5;
        public const int MaxHp = 30;
        public const int HeartHp = 10;
        public const int StartBuns = 22;
        public const int StartCost = 8;
        public const int CostStep = 2;
        public const float HeroCharChance = 0.24f;
        public const float ShovelChance = 0.12f;
        public const int AdShovels = 2;
        public const int GoldPity = 5;
        public const float PairBias = 0.4f;
        public const int MaxWaveCount = 24;
        public const double LevelMul = 2.25;
        public const float BossNemesisMul = 5f;
        public const float FirstWaveAt = 4f;
        public const float WaveGap = 3.5f;

        public static float WaveHp(int wave) => 5f * (float)Math.Pow(1.18, wave - 1);
    }
}
