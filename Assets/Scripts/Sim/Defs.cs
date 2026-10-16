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
            new UnitDef { Kind = UnitKind.Dao, Glyph = '刀', Role = "近战劈砍，附带小范围溅射", Range = 1.25f, Cooldown = 0.9f, Damage = 3.2f, Splash = 0.5f },
            new UnitDef { Kind = UnitKind.Qiang, Glyph = '枪', Role = "直线突刺，穿透三人", Range = 2.15f, Cooldown = 1.0f, Damage = 2.55f, Pierce = 3 },
            new UnitDef { Kind = UnitKind.Gong, Glyph = '弓', Role = "远程放箭", Range = 2.65f, Cooldown = 1.15f, Damage = 3.0f },
            new UnitDef { Kind = UnitKind.Qi, Glyph = '骑', Role = "快速冲撞，击退敌人", Range = 1.5f, Cooldown = 0.5f, Damage = 1.3f, Knockback = 0.22f },
        };

        public static UnitDef Get(UnitKind k) => All[(int)k];
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

    public static class Tuning
    {
        public const int Cols = 8;
        public const int Rows = 6;
        public const int CampSlots = 5;
        public const int MaxHp = 30;
        public const int HeartHp = 10;
        public const int StartBuns = 12;
        public const int StartCost = 12;
        /// <summary>杀敌入账倍率。原作第 3 波进度约一半、第 6 波约四成，按这个压过每只都 +1 的进账。</summary>
        public const float KillGold = 0.4f;
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

    /// <summary>
    /// 梁山座次：十一个大阶各分叁、贰、壹三小阶，顶上是不封顶的「天魁星」。
    /// 胜加星，满星再胜升一小阶；败掉星，零星再败降一小阶，但不会跌出当前大阶。
    /// 提辖以下败了不掉星，新手只升不降。
    /// </summary>
    public static class RankRules
    {
        public static readonly string[] Majors =
            { "喽啰", "小校", "都头", "提辖", "步军头领", "水军头领", "马军头领", "八骠骑", "五虎将", "地煞星", "天罡星", "天魁星" };

        public static readonly string[] MajorTips =
        {
            "山寨里跑腿的小兵", "带十来个喽啰的小头目", "县里的都头，如武松、朱仝、雷横",
            "军中提辖，如鲁达、杨志", "梁山步军头领，如鲁智深、武松", "梁山水军头领，如李俊、阮氏三雄",
            "梁山马军头领，如史进、穆弘", "马军八骠骑兼先锋使", "马军五虎上将",
            "七十二地煞之列", "三十六天罡之列", "天魁星呼保义，梁山之主",
        };

        static readonly string[] Subs = { "叁", "贰", "壹" };

        public const int SubPerMajor = 3;
        public const int TopMajor = 11;
        /// <summary>天魁星的扁平序号，也是最后一阶。</summary>
        public const int Top = TopMajor * SubPerMajor;
        public const int Count = Top + 1;
        /// <summary>界面上星条最多画几颗；天魁星的星数改成数字。</summary>
        public const int MaxStars = 5;

        public static int Major(int flat) => Clamp(flat) >= Top ? TopMajor : Clamp(flat) / SubPerMajor;
        public static int Sub(int flat) => Clamp(flat) >= Top ? 0 : Clamp(flat) % SubPerMajor;

        public static int Clamp(int flat) => flat < 0 ? 0 : flat > Top ? Top : flat;

        /// <summary>这一小阶满几颗星升阶；天魁星不封顶返回 0。</summary>
        public static int StarsAt(int flat)
        {
            int m = Major(flat);
            if (m >= TopMajor) return 0;
            return m <= 2 ? 3 : m <= 6 ? 4 : 5;
        }

        public static string Name(int flat)
        {
            int m = Major(flat);
            return m >= TopMajor ? Majors[m] : Majors[m] + "·" + Subs[Sub(flat)];
        }

        public static string MajorName(int flat) => Majors[Major(flat)];

        public static bool SafeStars(int flat) => Major(flat) < 3;

        /// <summary>局外排名分：升一小阶比任何星数都多。</summary>
        public static int Score(int flat, int stars) => Clamp(flat) * 100 + Math.Min(stars, 99) * 10;

        public static void Apply(ref int tier, ref int stars, bool win, out bool promoted, out bool demoted)
        {
            promoted = demoted = false;
            tier = Clamp(tier);
            if (stars < 0) stars = 0;
            int cap = StarsAt(tier);
            if (cap > 0 && stars > cap) stars = cap;
            if (win)
            {
                if (cap > 0 && stars >= cap)
                {
                    tier++;
                    stars = 0;
                    promoted = true;
                }
                else stars++;
                return;
            }
            if (SafeStars(tier)) return;
            if (stars > 0) { stars--; return; }
            if (tier < Top && Sub(tier) > 0)
            {
                tier--;
                stars = StarsAt(tier) - 1;
                demoted = true;
            }
        }

        /// <summary>旧版七阶存档（喽啰…天罡星）换算到新座次。</summary>
        public static int FromLegacy(int oldTier)
        {
            int[] map = { 0, 1, 4, 6, 7, 8, 10 };
            if (oldTier < 0) oldTier = 0;
            if (oldTier >= map.Length) oldTier = map.Length - 1;
            return map[oldTier] * SubPerMajor;
        }
    }
}
