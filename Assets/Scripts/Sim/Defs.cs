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
        public float CooldownAt(int level) => Tuning.LevelCd(Cooldown, level);

        public string TypeLabel => Pierce > 1 ? "穿透" : Splash > 0 ? "范围" : Knockback > 0 ? "击退" : "单体";

        /// <summary>御敌千里只放大超过两格的远程。骑外沿正好两格，仍按近战，不翻倍。</summary>
        public float RangeWith(bool far) => far && Range > 2f ? Range * 2f : Range;

        static readonly UnitDef[] All =
        {
            // 圈从本格中心量。本格边在 0.5：刀再向外盖满一格，外沿 1.5；骑再向外一格半，外沿 2。
            new UnitDef { Kind = UnitKind.Dao, Glyph = '刀', Role = "近战劈砍，附带小范围溅射", Range = 1.5f, Cooldown = 0.85f, Damage = 3.15f, Splash = 0.5f },
            new UnitDef { Kind = UnitKind.Qiang, Glyph = '枪', Role = "直线突刺，穿透三人", Range = 2.15f, Cooldown = 1.05f, Damage = 2.7f, Pierce = 3 },
            new UnitDef { Kind = UnitKind.Gong, Glyph = '弓', Role = "远程放箭", Range = 2.65f, Cooldown = 1.22f, Damage = 3.25f },
            new UnitDef { Kind = UnitKind.Qi, Glyph = '骑', Role = "快速冲撞，击退敌人", Range = 2f, Cooldown = 0.42f, Damage = 1.12f, Knockback = 0.22f },
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
            new EnemyDef { Kind = EnemyKind.Zu, Glyph = "卒", HpMul = 1f, Speed = 1.0f, LeakDamage = 1, Reward = 1 },
            new EnemyDef { Kind = EnemyKind.Chai, Glyph = "差", HpMul = 0.6f, Speed = 1.55f, LeakDamage = 1, Reward = 1 },
            new EnemyDef { Kind = EnemyKind.Jia, Glyph = "甲", HpMul = 2.3f, Speed = 0.74f, Armor = 0.35f, LeakDamage = 1, Reward = 2 },
            new EnemyDef { Kind = EnemyKind.Bu, Glyph = "捕", HpMul = 9f, Speed = 0.85f, LeakDamage = 2, Reward = 5 },
            new EnemyDef { Kind = EnemyKind.Boss, Glyph = "", HpMul = 14f, Speed = 0.45f, LeakDamage = 2, Reward = 20 },
        };

        public static EnemyDef Get(EnemyKind k) => All[(int)k];
    }

    public static class Tuning
    {
        /// <summary>每边 8 列 5 行，和原作一样紧，摆错一格就疼。</summary>
        public const int Cols = 8;
        public const int Rows = 5;
        public const int CampSlots = 5;
        /// <summary>一颗心就是 1 点血。开局三颗，漏一只小怪掉一颗。</summary>
        public const int MaxHp = 3;
        public const int HeartHp = 1;
        /// <summary>续命丹加的心。3 + 8 = 11，第一波十个卒全放过去还剩 1 颗。</summary>
        public const int XumingHearts = 8;
        /// <summary>原作开局 20 包子、首次征兵 10：摆完第一轮手里还剩 10，第一波打完就能接上第二次。</summary>
        public const int StartBuns = 20;
        public const int StartCost = 10;
        /// <summary>杀敌入账倍率：一局大约聚义六到八次，每次都要想清楚再按。</summary>
        public const float KillGold = 0.8f;
        public const int CostStep = 3;
        /// <summary>掉一颗心补 6 个炊饼。只够喘口气，不够靠漏怪回本。</summary>
        public const int RainBuns = 6;
        public const float HeroCharChance = 0.24f;
        /// <summary>聚义自带铲子很少，白地主要靠看广告开。看完一次固定给两把。</summary>
        public const float ShovelChance = 0.05f;
        public const int AdShovels = 2;
        public const int GoldPity = 5;
        public const float PairBias = 0.4f;
        public const int MaxWaveCount = 24;
        /// <summary>每级伤害。比攻速陡，但不再一合成就翻两倍多，免得满级把血量曲线盖过去。</summary>
        public const double LevelMul = 1.88;
        /// <summary>每级攻速。连乘，五级大约比一级快七成半，出手节奏听得出来。</summary>
        public const float SpeedMul = 1.15f;
        public const float BossNemesisMul = 5f;
        /// <summary>出兵口草堆盖住的那段路。怪走出之前看不见，也不能被打到。</summary>
        public const float SpawnCover = 0.72f;
        /// <summary>开局先让探马沿路走一圈，玩家趁这段时间征兵布阵，再出第一波。</summary>
        public const float FirstWaveAt = 12f;
        public const float ScoutStart = 0.8f;
        public const float ScoutWalk = 7.5f;
        /// <summary>主动道具放完要转完这一圈才能再放。攻速符和它的十二秒效果一样长，刚好接上。</summary>
        public const float CdMeng = 8f;
        public const float CdBaozi = 6f;
        public const float CdShu = 10f;
        public const float CdFu = 12f;
        public const float CdTrap = 8f;

        /// <summary>前两波之后留口气看局面，后面逐步收紧。</summary>
        public static float WaveGap(int wave) => wave <= 2 ? 8f : wave <= 5 ? 7f : 6f;

        /// <summary>同一波里两个官兵的间隔：头几波稀疏，越往后越密。</summary>
        public static float SpawnInterval(int wave) => Math.Max(0.28f, wave <= 2 ? 1.3f - 0.1f * wave : 0.95f - 0.03f * wave);

        /// <summary>等级换算后的出手间隔。一级用基础值，之后每级按 SpeedMul 加快。</summary>
        public static float LevelCd(float cooldown, int level)
        {
            if (level < 1) level = 1;
            if (level > UnitDef.MaxLevel) level = UnitDef.MaxLevel;
            return cooldown / (float)Math.Pow(SpeedMul, level - 1);
        }

        /// <summary>普通官兵的基础血。前四波缓一缓，之后每波另加一截，第八波是开局的三倍半，一波比一波难。</summary>
        public static float WaveHp(int wave)
        {
            if (wave < 1) wave = 1;
            float grow = (float)Math.Pow(1.15, wave - 1);
            float extra = 1f + 0.075f * Math.Max(0, wave - 4);
            return 8.4f * grow * extra;
        }
    }

    /// <summary>
    /// 梁山座次：十一个大阶各分叁、贰、壹三小阶，顶上是不封顶的「天魁星」。
    /// 每一小阶都是五星，集满五星升下一小阶；败了掉一星，没星再败降一小阶，但不会跌出当前大阶。
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

        /// <summary>这一小阶集满几颗星就升；天魁星不封顶返回 0。对标原作，每一小阶都是五星。</summary>
        public static int StarsAt(int flat) => Major(flat) >= TopMajor ? 0 : MaxStars;

        public static string Name(int flat)
        {
            int m = Major(flat);
            return m >= TopMajor ? Majors[m] : Majors[m] + "·" + Subs[Sub(flat)];
        }

        public static string MajorName(int flat) => Majors[Major(flat)];

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
                if (cap > 0 && stars + 1 >= cap)
                {
                    tier++;
                    stars = 0;
                    promoted = true;
                }
                else stars++;
                return;
            }
            if (stars > 0) { stars--; return; }
            if (tier < Top && Sub(tier) > 0)
            {
                tier--;
                int back = StarsAt(tier);
                stars = back > 0 ? back - 1 : 0;
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
