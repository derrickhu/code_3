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

    public static class Weapons
    {
        public static readonly string[] Grades = { "凡器", "利器", "宝器", "神兵" };

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
            WeaponKind.Staff => "杖法通神：大招伤害 +30%",
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
                case WeaponKind.Staff: a.UltMul = 1.3f; break;
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

    /// <summary>羁绊：同时凑出若干名有渊源的好汉，他们伤害提高。</summary>
    public sealed class Bond
    {
        public string Name, Story;
        public HeroId[] Members;
        public int Need;
        public float Dmg;

        public static readonly Bond[] All =
        {
            new Bond { Name = "阮氏三雄", Need = 2, Dmg = 0.25f, Story = "石碣村打鱼的三兄弟，同心智取生辰纲",
                Members = new[] { HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi } },
            new Bond { Name = "五虎上将", Need = 2, Dmg = 0.15f, Story = "梁山马军五虎将",
                Members = new[] { HeroId.GuanSheng, HeroId.LinChong, HeroId.QinMing, HeroId.HuYanZhuo, HeroId.DongPing } },
            new Bond { Name = "八骠骑", Need = 2, Dmg = 0.12f, Story = "马军八骠骑兼先锋使",
                Members = new[] { HeroId.HuaRong, HeroId.XuNing, HeroId.YangZhi, HeroId.SuoChao, HeroId.ZhangQing, HeroId.ZhuTong, HeroId.ShiJin, HeroId.MuHong } },
            new Bond { Name = "二龙山", Need = 2, Dmg = 0.15f, Story = "鲁智深、杨志、武松同占二龙山",
                Members = new[] { HeroId.LuZhiShen, HeroId.YangZhi, HeroId.WuSong, HeroId.CaoZheng, HeroId.ShiEn, HeroId.ZhangQingY, HeroId.SunErNiang } },
            new Bond { Name = "七星聚义", Need = 3, Dmg = 0.15f, Story = "晁盖与七人聚义，智取生辰纲",
                Members = new[] { HeroId.WuYong, HeroId.GongSunSheng, HeroId.LiuTang, HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi, HeroId.BaiSheng } },
            new Bond { Name = "张氏兄弟", Need = 2, Dmg = 0.2f, Story = "浔阳江上船火儿与浪里白条",
                Members = new[] { HeroId.ZhangHeng, HeroId.ZhangShun } },
            new Bond { Name = "解氏兄弟", Need = 2, Dmg = 0.2f, Story = "登州打虎的两头蛇与双尾蝎",
                Members = new[] { HeroId.XieZhen, HeroId.XieBao } },
            new Bond { Name = "翠屏山", Need = 2, Dmg = 0.18f, Story = "杨雄、石秀结为兄弟，带时迁投梁山",
                Members = new[] { HeroId.YangXiong, HeroId.ShiXiu, HeroId.ShiQian } },
            new Bond { Name = "少华山", Need = 2, Dmg = 0.15f, Story = "史进与朱武、陈达、杨春不打不相识",
                Members = new[] { HeroId.ShiJin, HeroId.ZhuWu, HeroId.ChenDa, HeroId.YangChun } },
            new Bond { Name = "十字坡夫妻", Need = 2, Dmg = 0.18f, Story = "张青、孙二娘夫妻开店",
                Members = new[] { HeroId.ZhangQingY, HeroId.SunErNiang } },
            new Bond { Name = "王英扈三娘", Need = 2, Dmg = 0.18f, Story = "宋江作主，一丈青嫁了矮脚虎",
                Members = new[] { HeroId.WangYing, HeroId.HuSanNiang } },
            new Bond { Name = "登州孙家", Need = 2, Dmg = 0.15f, Story = "孙立、孙新、顾大嫂登州劫牢",
                Members = new[] { HeroId.SunLi, HeroId.SunXin, HeroId.GuDaSao, HeroId.YueHe } },
            new Bond { Name = "芒砀三杰", Need = 2, Dmg = 0.18f, Story = "混世魔王樊瑞与左右手项充、李衮",
                Members = new[] { HeroId.FanRui, HeroId.XiangChong, HeroId.LiGun } },
            new Bond { Name = "画戟双英", Need = 2, Dmg = 0.2f, Story = "吕方、郭盛对影山斗戟，花荣一箭射开",
                Members = new[] { HeroId.LvFang, HeroId.GuoSheng } },
            new Bond { Name = "揭阳水军", Need = 2, Dmg = 0.15f, Story = "李俊与童威、童猛浔阳江上做私商",
                Members = new[] { HeroId.LiJun, HeroId.TongWei, HeroId.TongMeng, HeroId.LiLi } },
            new Bond { Name = "铁牛与哥哥", Need = 2, Dmg = 0.2f, Story = "黑旋风生死追随宋江",
                Members = new[] { HeroId.SongJiang, HeroId.LiKui } },
            new Bond { Name = "主仆情深", Need = 2, Dmg = 0.2f, Story = "浪子燕青舍命救主卢俊义",
                Members = new[] { HeroId.LuJunYi, HeroId.YanQing } },
            new Bond { Name = "郓城都头", Need = 2, Dmg = 0.18f, Story = "朱仝、雷横先后私放晁盖、宋江",
                Members = new[] { HeroId.ZhuTong, HeroId.LeiHeng } },
            new Bond { Name = "水火二将", Need = 2, Dmg = 0.2f, Story = "凌州圣水将、神火将",
                Members = new[] { HeroId.ShanTingGui, HeroId.WeiDingGuo } },
            new Bond { Name = "江州好汉", Need = 2, Dmg = 0.12f, Story = "江州劫法场救宋江",
                Members = new[] { HeroId.DaiZong, HeroId.LiKui, HeroId.ZhangShun, HeroId.LiJun, HeroId.MuHong } },
        };
    }
}
