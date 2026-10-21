using System;

namespace Rescue.Sim
{
    /// <summary>存档里按整数存 HeroId，只能往末尾追加。武大郎不在一百单八将之内。</summary>
    public enum HeroId
    {
        WuSong, LinChong, LiKui, HuaRong, DaiZong, YanQing, QinMing, ZhangShun, ZhangHeng,
        RuanXiaoEr, RuanXiaoQi, SunErNiang, HuSanNiang, LuZhiShen, WuYong, WuDaLang,
        SongJiang, LuJunYi, GongSunSheng, GuanSheng, HuYanZhuo, ChaiJin, LiYing, ZhuTong, DongPing, ZhangQing,
        YangZhi, XuNing, SuoChao, LiuTang, ShiJin, MuHong, LeiHeng, LiJun, RuanXiaoWu, YangXiong, ShiXiu, XieZhen, XieBao,
        ZhuWu, HuangXin, SunLi, XuanZan, HaoSiWen, HanTao, PengQi, ShanTingGui, WeiDingGuo, XiaoRang, PeiXuan,
        OuPeng, DengFei, YanShun, YangLin, LingZhen, JiangJing, LvFang, GuoSheng, AnDaoQuan, HuangFuDuan,
        WangYing, BaoXu, FanRui, KongMing, KongLiang, XiangChong, LiGun, JinDaJian, MaLin, TongWei, TongMeng,
        MengKang, HouJian, ChenDa, YangChun, ZhengTianShou, TaoZongWang, SongQing, YueHe, GongWang, DingDeSun,
        MuChun, CaoZheng, SongWan, DuQian, XueYong, ShiEn, LiZhong, ZhouTong, TangLong, DuXing, ZouYuan, ZouRun,
        ZhuGui, ZhuFu, CaiFu, CaiQing, LiLi, LiYun, JiaoTing, ShiYong, SunXin, GuDaSao, ZhangQingY, WangDingLiu,
        YuBaoSi, BaiSheng, ShiQian, DuanJingZhu,
    }

    /// <summary>出手的样子：决定普攻特效、音效和默认数值。</summary>
    public enum Look { Dao, Fist, Spear, Axe, Bow, Fire, Water, WaterFire, Fork, Dart, Poison, Lasso, Staff, Whip, Hammer, Sword, Magic, Support }

    public enum UltKind { None, Combo, Area, Snipe, Throw, Blast, Pin, Lasso, Shovel, SlowAll, HasteAll, Buns, Volley, Storm, Heal, Rally, Shield, Upgrade, Steal }

    public sealed class HeroDef
    {
        public HeroId Id;
        /// <summary>忠义堂座次 1~108；0 表示不在一百单八将里。</summary>
        public int Rank;
        public string Name, Nick, Post, Bio, Fate, Quote;
        public Look Look;
        public string Skill, Ult, UltName;
        public float Range, Cooldown, Damage;
        public int Pierce = 1;
        public float Splash;
        public float WaterBonus = 1f;
        public float UltCooldown = 12f;
        public bool Hidden;
        public bool NoAttack;

        public float CritChance, CritMul = 2f, HitStun, HitKnock;
        public bool PreferBig;
        /// <summary>身边这么远内的敌人被杀多掉一个炊饼。</summary>
        public float KillBuns;
        /// <summary>周围八格出手冷却倍率（吴用 0.7）。</summary>
        public float Aura = 1f;
        /// <summary>全队出手冷却倍率（戴宗 0.85）。</summary>
        public float TeamCd = 1f;

        public UltKind UltKind;
        public float UltR, UltMul, UltStun, UltKnock, UltT;
        public int UltN;

        public string Weapon;
        public WeaponKind WKind;
        /// <summary>0 凡器 1 利器 2 宝器 3 神兵。</summary>
        public int WGrade;

        public float DamageAt(int level) => Damage * (float)Math.Pow(Tuning.LevelMul, level - 1);
        public float CooldownAt(int level) => Tuning.LevelCd(Cooldown, level);

        public bool Disha => Rank > 36;
        public string Star => Rank <= 0 ? "" : Rank <= 36 ? "天" + TianStars[Rank - 1] + "星" : "地" + DiStars[Rank - 37] + "星";
        public string Rank108 => Rank <= 0 ? "" : $"第{Rank}位";
        public string TypeLabel => NoAttack ? "辅助" : Pierce > 1 ? "穿透" : Splash > 0 ? "范围" : HitStun > 0 ? "控制" : CritChance > 0 ? "暴击" : "单体";
        public string WeaponTag => string.IsNullOrEmpty(Weapon) ? "" : Weapon.Substring(Weapon.Length - 1);

        const string TianStars = "魁罡机闲勇雄猛威英贵富满孤伤立捷暗佑空速异杀微究退寿剑平罪损败牢慧暴哭巧";
        const string DiStars = "魁煞勇杰雄威英奇猛文正阔阖强暗轴会佐佑灵兽微慧暴默猖狂飞走巧明进退满遂周隐异理俊乐捷速镇羁魔妖幽伏僻空孤全短角囚藏平损奴察恶丑数阴刑壮劣健耗贼狗";

        /// <summary>装备兵器的伤害。品级定底子，每多缴获一阶再加，五阶封顶。</summary>
        public static float ArmedMul(int copies, int grade = 1)
        {
            if (copies <= 0) return 1f;
            int lv = copies > 5 ? 5 : copies;
            float b = grade <= 0 ? 1.30f : grade == 1 ? 1.40f : grade == 2 ? 1.50f : 1.65f;
            return b + 0.15f * (lv - 1);
        }

        /// <summary>御敌千里：远程射程翻倍。全图射程不再放大。</summary>
        public float RangeWith(bool far)
        {
            if (NoAttack || Range >= 50f) return Range;
            if (far && Range >= 2f) return Range * 2f;
            return Range;
        }

        public static HeroDef Get(HeroId id) => All[(int)id];

        public static readonly HeroDef[] All = Build();

        // ─────────────────────────── 数据 ───────────────────────────

        static HeroDef[] Build()
        {
            var list = new HeroDef[Enum.GetValues(typeof(HeroId)).Length];
            foreach (var d in Table())
            {
                d.Finish();
                list[(int)d.Id] = d;
            }
            for (int i = 0; i < list.Length; i++)
                if (list[i] == null) throw new InvalidOperationException("好汉缺数据：" + (HeroId)i);
            return list;
        }

        HeroDef Stats(float range, float cd, float dmg, int pierce = 1, float splash = 0, float water = 1f)
        {
            Range = range; Cooldown = cd; Damage = dmg; Pierce = pierce; Splash = splash; WaterBonus = water;
            _fixed = true;
            return this;
        }

        HeroDef U(float r = 0, float mul = 0, float stun = 0, int n = 0, float t = 0, float knock = 0, float cd = 0)
        {
            if (r > 0) UltR = r;
            if (mul > 0) UltMul = mul;
            else if (mul < 0) UltMul = 0;
            if (stun > 0) UltStun = stun;
            if (n > 0) UltN = n;
            if (t > 0) UltT = t;
            if (knock > 0) UltKnock = knock;
            if (cd > 0) UltCooldown = cd;
            return this;
        }

        HeroDef P(float crit = 0, float critMul = 0, float stun = 0, float knock = 0, bool big = false, float buns = 0, float aura = 1f, float team = 1f)
        {
            if (crit > 0) CritChance = crit;
            if (critMul > 0) CritMul = critMul;
            if (stun > 0) HitStun = stun;
            if (knock > 0) HitKnock = knock;
            PreferBig |= big;
            if (buns > 0) KillBuns = buns;
            Aura = Math.Min(Aura, aura);
            TeamCd = Math.Min(TeamCd, team);
            return this;
        }

        HeroDef Txt(string skill = null, string ult = null)
        {
            if (skill != null) Skill = skill;
            if (ult != null) Ult = ult;
            return this;
        }

        HeroDef NoAtk() { NoAttack = true; Look = Look.Support; return this; }

        bool _fixed;

        static HeroDef H(HeroId id, int rank, string name, string nick, Look look, string weapon, WeaponKind wk, int grade,
            UltKind ult, string ultName, string post, string quote, string bio, string fate = null)
        {
            var d = new HeroDef
            {
                Id = id, Rank = rank, Name = name, Nick = nick, Look = look, Weapon = weapon, WKind = wk, WGrade = grade,
                UltKind = ult, UltName = ultName, Post = post, Quote = quote, Bio = bio, Fate = fate ?? "随宋江受招安，征辽、征方腊",
            };
            if (look == Look.Support) d.NoAttack = true;
            d.DefaultUlt();
            return d;
        }

        void DefaultUlt()
        {
            switch (UltKind)
            {
                case UltKind.Combo: UltR = 2.6f; UltN = 10; UltMul = 1.1f; break;
                case UltKind.Area: UltR = 2.8f; UltMul = 5f; break;
                case UltKind.Snipe: UltMul = 18f; break;
                case UltKind.Throw: UltR = 3f; UltMul = 7f; UltKnock = 2f; break;
                case UltKind.Blast: UltR = 1.6f; UltMul = 7f; break;
                case UltKind.Pin: UltMul = 12f; UltStun = 2f; break;
                case UltKind.Lasso: UltR = 3f; UltN = 3; UltStun = 2f; UltMul = 3f; break;
                case UltKind.Shovel: UltR = 2f; UltMul = 6f; break;
                case UltKind.SlowAll: UltT = 3f; break;
                case UltKind.HasteAll: UltT = 2.5f; break;
                case UltKind.Buns: UltN = 4; break;
                case UltKind.Volley: UltN = 4; UltMul = 5f; break;
                case UltKind.Storm: UltMul = 2.5f; UltStun = 0.8f; break;
                case UltKind.Heal: UltN = 4; UltCooldown = 15f; break;
                case UltKind.Rally: UltT = 3f; break;
                case UltKind.Shield: UltT = 3f; UltCooldown = 15f; break;
                case UltKind.Upgrade: UltCooldown = 16f; break;
                case UltKind.Steal: UltN = 4; break;
            }
        }

        /// <summary>没有手调数值的好汉按出手样子套模板，再按座次微调伤害。</summary>
        void Finish()
        {
            if (!_fixed && !NoAttack)
            {
                switch (Look)
                {
                    case Look.Fist: Range = 1.35f; Cooldown = 0.4f; Damage = 4.2f; break;
                    case Look.Spear: Range = 2.4f; Cooldown = 0.85f; Damage = 5.5f; Pierce = 4; break;
                    case Look.Axe: Range = 1.65f; Cooldown = 0.75f; Damage = 5f; Splash = 1.1f; break;
                    case Look.Bow: Range = 3.5f; Cooldown = 0.9f; Damage = 5.8f; break;
                    case Look.Fire: Range = 2.2f; Cooldown = 1.05f; Damage = 5.5f; Splash = 1.1f; break;
                    case Look.Water: Range = 2.2f; Cooldown = 0.7f; Damage = 5f; WaterBonus = 2.5f; break;
                    case Look.WaterFire: Range = 2.25f; Cooldown = 0.9f; Damage = 6f; Splash = 0.8f; WaterBonus = 2f; break;
                    case Look.Fork: Range = 2.3f; Cooldown = 0.55f; Damage = 6f; WaterBonus = 1.8f; break;
                    case Look.Dart: Range = 2.6f; Cooldown = 0.5f; Damage = 3.8f; if (CritChance <= 0) { CritChance = 0.2f; CritMul = 2.5f; } break;
                    case Look.Poison: Range = 1.8f; Cooldown = 0.8f; Damage = 6f; break;
                    case Look.Lasso: Range = 2.25f; Cooldown = 0.9f; Damage = 6.5f; if (HitStun <= 0) HitStun = 0.5f; break;
                    case Look.Staff: Range = 1.5f; Cooldown = 0.9f; Damage = 11f; Splash = 1f; break;
                    case Look.Whip: Range = 1.8f; Cooldown = 0.7f; Damage = 6f; if (HitStun <= 0) HitStun = 0.25f; break;
                    case Look.Hammer: Range = 1.6f; Cooldown = 1f; Damage = 11f; Splash = 0.9f; if (HitKnock <= 0) HitKnock = 0.3f; break;
                    case Look.Sword: Range = 1.7f; Cooldown = 0.5f; Damage = 5.5f; if (CritChance <= 0) CritChance = 0.15f; break;
                    case Look.Magic: Range = 2.6f; Cooldown = 1.1f; Damage = 6f; Splash = 1.3f; break;
                    default: Range = 1.6f; Cooldown = 0.75f; Damage = 5.5f; Splash = 0.6f; break;
                }
                float power = Rank <= 0 ? 1f : Rank <= 36 ? 1.15f - 0.15f * (Rank - 1) / 35f : 0.95f - 0.2f * (Rank - 37) / 71f;
                Damage *= power;
            }
            if (string.IsNullOrEmpty(Skill)) Skill = AutoSkill();
            if (string.IsNullOrEmpty(Ult)) Ult = UltKind == UltKind.None ? "" : UltName + "：" + AutoUlt();
        }

        string AutoSkill()
        {
            if (NoAttack)
            {
                var parts = new System.Collections.Generic.List<string> { "不出手" };
                if (Aura < 1f) parts.Add($"周围八格出手速度 +{Pct(1f / Aura - 1f)}%");
                if (TeamCd < 1f) parts.Add($"全队出手速度 +{Pct(1f / TeamCd - 1f)}%");
                if (KillBuns > 0) parts.Add("身边敌人被杀多掉一个炊饼");
                return string.Join("；", parts);
            }
            string s = Look switch
            {
                Look.Fist => "近身连拳，出手极快",
                Look.Spear => $"长枪突刺，一枪穿{Pierce}人",
                Look.Axe => "抡斧乱砍，范围伤害",
                Look.Bow => "远程放箭",
                Look.Fire => "火器轰击，范围伤害",
                Look.Water or Look.WaterFire or Look.Fork => $"水上伤害×{WaterBonus:0.#}",
                Look.Dart => "暗器连发，出手快",
                Look.Poison => "近身下狠手",
                Look.Lasso => "抛索拿人",
                Look.Staff => "抡杖横扫，范围重击",
                Look.Whip => "钢鞭砸击",
                Look.Hammer => "重锤砸地，震退敌人",
                Look.Sword => "快剑连斩",
                Look.Magic => "作法施术，范围伤害",
                _ => Splash > 0 ? "挥刀劈砍，带小范围溅射" : "挥刀劈砍",
            };
            if (Range >= 50f) s = "射程覆盖全场";
            if (CritChance > 0) s += $"；{Pct(CritChance)}%暴击×{CritMul:0.#}";
            if (HitStun > 0) s += $"；命中定身{HitStun:0.#}秒";
            if (HitKnock > 0) s += "；击退小兵";
            if (PreferBig) s += "；优先打头目";
            if (KillBuns > 0) s += "；身边敌人被杀多掉炊饼";
            if (Aura < 1f) s += $"；周围出手 +{Pct(1f / Aura - 1f)}%";
            if (TeamCd < 1f) s += $"；全队出手 +{Pct(1f / TeamCd - 1f)}%";
            return s;
        }

        static int Pct(float f) => (int)Math.Round(f * 100f);

        string AutoUlt()
        {
            switch (UltKind)
            {
                case UltKind.Combo: return $"对最前方的敌人连打{UltN}下";
                case UltKind.Area:
                {
                    string s = UltMul > 0 ? $"周围敌人受{UltMul:0.#}倍重创" : "周围敌人";
                    if (UltStun > 0) s += $"，定身{UltStun:0.#}秒";
                    if (UltKnock > 0) s += "，并被撞回去";
                    return s;
                }
                case UltKind.Snipe: return "万箭齐发扫过全场，再一箭重创血最厚的敌人";
                case UltKind.Throw: return "把附近最壮的敌人摔回两格";
                case UltKind.Blast: return "在敌人最密处炸开";
                case UltKind.Pin: return $"重创并定住一人{UltStun:0.#}秒";
                case UltKind.Lasso: return $"同时拿住{UltN}人，定身{UltStun:0.#}秒";
                case UltKind.Shovel: return "送一把铲子开荒；没地可开就横扫";
                case UltKind.SlowAll: return $"全场敌人减速一半{UltT:0.#}秒";
                case UltKind.HasteAll: return $"全队出手速度翻倍{UltT:0.#}秒";
                case UltKind.Buns: return $"立得{UltN}个炊饼";
                case UltKind.Volley: return $"连发{UltN}下，各打一名敌人";
                case UltKind.Storm: return $"天雷落遍全场，敌人受伤并麻痹{UltStun:0.#}秒";
                case UltKind.Heal: return $"宋江回{UltN}点血";
                case UltKind.Rally: return $"宋江护体{UltT:0.#}秒，全队出手加快";
                case UltKind.Shield: return $"宋江护体{UltT:0.#}秒不掉血";
                case UltKind.Upgrade: return "场上最低级的字升一级";
                case UltKind.Steal: return $"从对手那里偷{UltN}个炊饼";
                default: return "";
            }
        }

        // 座次、星号、绰号、职司、事迹、结局依一百二十回本《水浒传》
        static HeroDef[] Table() => new[]
        {
            // ─────────────── 原有十六人（数值保持原样） ───────────────
            H(HeroId.WuSong, 14, "武松", "行者", Look.Fist, "雪花镔铁戒刀", WeaponKind.Blade, 3, UltKind.Combo, "醉打", "步军头领",
                "三碗不过冈？俺偏要过！",
                "清河县人。景阳冈赤手打死吊睛白额大虫，做了阳谷县都头；为兄报仇斗杀西门庆，醉打蒋门神，血溅鸳鸯楼，扮作行者投二龙山。",
                "征方腊时被砍断左臂，留在杭州六和寺出家，八十善终").Stats(1.35f, 0.35f, 4.5f).U(r: 2.6f, n: 18, mul: 1.2f, cd: 11f)
                .Txt("近身连拳，出手极快", "醉打：对最前方的敌人连打十八拳"),
            H(HeroId.LinChong, 6, "林冲", "豹子头", Look.Spear, "丈八蛇矛", WeaponKind.Spear, 3, UltKind.Area, "风雪山神庙", "马军五虎将",
                "风雪夜，正好杀人。",
                "八十万禁军枪棒教头，被高俅设计误入白虎堂，刺配沧州。风雪夜山神庙手刃陆谦，雪夜上梁山，后火并王伦，奉晁盖为寨主。",
                "征方腊后染风瘫，留在杭州六和寺，半年后病故").Stats(2.45f, 0.8f, 6f, 5).U(r: 3f, mul: 4f, stun: 2.5f, cd: 13f)
                .Txt("丈八蛇矛，一枪穿五人", "风雪山神庙：冻住周围敌人"),
            H(HeroId.LiKui, 22, "李逵", "黑旋风", Look.Axe, "双板斧", WeaponKind.Axe, 3, UltKind.Area, "旋风", "步军头领",
                "杀去东京，夺了鸟位！",
                "沂州沂水县人，小名铁牛。江州法场脱得赤条条，抡两把板斧杀出一条血路；回乡接母，母亲被虎所害，沂岭连杀四虎。",
                "被宋江以毒酒同死，葬在楚州蓼儿洼宋江墓旁").Stats(1.65f, 0.7f, 5f, 1, 1.2f).U(r: 3.2f, mul: 6f, cd: 12f)
                .Txt("双斧乱砍，大范围伤害", "旋风：一圈板斧清场"),
            H(HeroId.HuaRong, 9, "花荣", "小李广", Look.Bow, "雕弓", WeaponKind.Bow, 3, UltKind.Snipe, "神箭", "马军八骠骑",
                "看我射那雁行第三只。",
                "清风寨武知寨，百步穿杨，人称小李广。为救宋江大闹清风寨；梁山泊上一箭射中雁行第三只，众头领无不叹服。",
                "宋江死后，与吴用一同在蓼儿洼宋江墓前自缢").Stats(99f, 0.9f, 7f).U(mul: 25f, cd: 10f).P(big: true)
                .Txt("射程覆盖全场，优先射头目", "神箭：一箭重创血最厚的敌人"),
            H(HeroId.DaiZong, 20, "戴宗", "神行太保", Look.Support, "神行甲马", WeaponKind.Relic, 2, UltKind.SlowAll, "神行", "总探声息头领",
                "日行八百，片刻便到。",
                "江州两院押牢节级。会神行法，腿上拴两个甲马一日能行五百里，拴四个能行八百里，专管梁山往来传递消息。",
                "辞官往泰安州岳庙出家，数月后大笑而终").NoAtk().U(t: 4f, cd: 12f).P(team: 0.85f)
                .Txt("不出手；全队出手速度 +15%", "神行：全场敌人减速一半"),
            H(HeroId.YanQing, 36, "燕青", "浪子", Look.Dart, "川弩", WeaponKind.Bow, 2, UltKind.Throw, "相扑", "步军头领",
                "献丑了。",
                "卢俊义的家仆，一身雪练似的花绣，吹弹歌舞样样精通。川弩百发百中，相扑天下无对，泰安州智扑擎天柱任原。",
                "功成后辞别卢俊义，挑一担金珠宝贝，不知所终").Stats(1.35f, 0.5f, 4f).U(r: 3f, mul: 8f, knock: 2f, cd: 10f).P(crit: 0.3f, critMul: 3f)
                .Txt("三成概率暴击，三倍伤害", "相扑：把头目摔回两格"),
            H(HeroId.QinMing, 7, "秦明", "霹雳火", Look.Fire, "狼牙棒", WeaponKind.Club, 2, UltKind.Blast, "霹雳", "马军五虎将",
                "谁敢与我大战三百合！",
                "青州兵马总管，性如烈火，使一条狼牙棒。被宋江设计断了归路，一家老小遭慕容知府所害，只得上清风山入伙。",
                "征方腊时在清溪洞被方杰所杀").Stats(2.0f, 1.0f, 5f, 1, 1.0f).U(r: 1.6f, mul: 8f, cd: 11f)
                .Txt("狼牙棒砸地，范围伤害", "霹雳：在敌人最密处炸开"),
            H(HeroId.ZhangShun, 30, "张顺", "浪里白条", Look.Water, "分水刺", WeaponKind.Fork, 2, UltKind.Area, "浪里白条", "水军头领",
                "下得水来，叫你吃个饱。",
                "江州卖鱼牙子，浑身雪白，能在水底伏七日七夜。浔阳江上把李逵按在水里灌了个饱，不打不相识。",
                "征方腊时夜潜涌金门，被乱箭射死，魂魄附在张横身上杀了方天定").Stats(2.15f, 0.7f, 5f, 1, 0, 3f).U(r: 3.5f, mul: 6f, stun: 1f, cd: 12f)
                .Txt("站在水上伤害×3", "浪里白条：拖人下水，范围重创"),
            H(HeroId.ZhangHeng, 28, "张横", "船火儿", Look.WaterFire, "朴刀", WeaponKind.Blade, 1, UltKind.Area, "撞船", "水军头领",
                "老爷生长在江边。",
                "张顺的哥哥，在浔阳江上摆渡劫客，问人要吃“板刀面”还是“馄饨”。宋江险些丧在他船上，认出后纳头便拜。",
                "征方腊后在杭州染病身故").Stats(2.25f, 0.9f, 6f, 1, 0.8f, 2f).U(r: 2.6f, mul: 3f, knock: 1.5f, cd: 11f)
                .Txt("站在水上伤害×2，范围伤害", "撞船：把周围敌人撞回去"),
            H(HeroId.RuanXiaoEr, 27, "阮小二", "立地太岁", Look.Water, "朴刀", WeaponKind.Blade, 1, UltKind.Area, "掀浪", "水军头领",
                "这腔热血，只卖与识货的！",
                "石碣村渔民，阮氏三雄之长。随晁盖、吴用智取生辰纲，在石碣村芦苇荡里大破何涛的官兵。",
                "征方腊时在乌龙岭水战中被围，自刎而死").Stats(2.3f, 0.6f, 9f, 1, 0, 2f).U(r: 2.8f, mul: 6f, cd: 11f)
                .Txt("三字好汉，水上伤害×2", "掀浪：范围重创"),
            H(HeroId.RuanXiaoQi, 31, "阮小七", "活阎罗", Look.Fork, "鱼叉", WeaponKind.Fork, 2, UltKind.Pin, "活阎罗", "水军头领",
                "老爷只是个打鱼的。",
                "阮氏三雄最小的一个，水性最好，嘴上最不饶人。招安时偷喝御酒、换上村醪，把朝廷来使气得半死。",
                "因穿了方腊的龙袍被夺官，回石碣村依旧打鱼，奉母终老").Stats(2.3f, 0.45f, 8f, 1, 0, 2f).U(mul: 15f, stun: 2f, cd: 10f)
                .Txt("三字好汉，出手快，水上伤害×2", "活阎罗：重创并定住一人"),
            H(HeroId.SunErNiang, 103, "孙二娘", "母夜叉", Look.Poison, "厨刀", WeaponKind.Blade, 1, UltKind.Area, "蒙汗药", "西山酒店",
                "客官，里边请——",
                "与丈夫张青在孟州十字坡开酒店，专用蒙汗药麻翻过往客商。武松看破机关将她按住，从此结为姐弟。",
                "征方腊时被杜微的飞刀所伤，阵亡").Stats(1.8f, 0.8f, 6f).U(r: 3f, stun: 3f, mul: -1f, cd: 13f).P(buns: 2.2f)
                .Txt("身边敌人被杀多掉一个炊饼", "蒙汗药：周围敌人昏睡三秒"),
            H(HeroId.HuSanNiang, 59, "扈三娘", "一丈青", Look.Lasso, "红锦套索", WeaponKind.Rope, 3, UltKind.Lasso, "一丈青", "三军内探事",
                "看索！",
                "扈家庄千金，使日月双刀、红锦套索，阵前活捉矮脚虎王英。被林冲擒上梁山，宋江作主嫁给了王英。",
                "征方腊时与王英一同被郑魔君所杀").Stats(2.25f, 0.9f, 7f).U(r: 3f, n: 5, stun: 2.5f, mul: 3f, cd: 11f).P(stun: 0.6f)
                .Txt("红锦套索，命中定身", "一丈青：同时套住五人"),
            H(HeroId.LuZhiShen, 13, "鲁智深", "花和尚", Look.Staff, "水磨禅杖", WeaponKind.Staff, 3, UltKind.Shovel, "方便铲", "步军头领",
                "洒家来也！",
                "原名鲁达，渭州经略府提辖。三拳打死镇关西，五台山出家，倒拔垂杨柳，野猪林救下林冲，后与杨志、武松占了二龙山。",
                "征方腊时生擒方腊，在杭州六和寺听潮信圆寂").Stats(1.5f, 0.9f, 14f, 1, 1.0f).U(r: 2f, mul: 10f, cd: 14f).P(knock: 0.4f)
                .Txt("禅杖横扫，范围重击", "方便铲：送一把铲子到山寨，开出新地"),
            H(HeroId.WuYong, 3, "吴用", "智多星", Look.Support, "铜链", WeaponKind.Relic, 2, UltKind.HasteAll, "智取", "掌管机密军师",
                "小生略施小计。",
                "东溪村私塾先生，梁山第一谋士。智取生辰纲、智赚玉麒麟、智取大名府，大小计谋多出其手。",
                "宋江死后，与花荣一同在蓼儿洼宋江墓前自缢").NoAtk().U(t: 3f, cd: 12f).P(aura: 0.7f)
                .Txt("不出手；周围八格出手速度 +30%", "智取：全队出手速度翻倍三秒"),
            H(HeroId.WuDaLang, 0, "武大郎", "三寸丁谷树皮", Look.Support, "", WeaponKind.None, 0, UltKind.Buns, "炊饼", "",
                "炊饼——热乎的炊饼——",
                "武松的亲哥哥，身不满五尺，在阳谷县紫石街卖炊饼为生，为人老实本分，却被潘金莲与西门庆合谋毒死。",
                "被潘金莲毒死，武松为他报了仇").NoAtk().U(n: 3, cd: 5f).Hide()
                .Txt("隐藏好汉。不出手，每五秒烙三个炊饼", ""),

            // ─────────────── 天罡星 ───────────────
            H(HeroId.SongJiang, 1, "宋江", "呼保义", Look.Sword, "玄女天书", WeaponKind.Relic, 3, UltKind.Rally, "替天行道", "总兵都头领",
                "替天行道，保境安民。",
                "郓城县押司，仗义疏财、扶危济困，人称及时雨。私放晁盖，怒杀阎婆惜，浔阳楼题反诗险些问斩，被众好汉劫法场救上梁山。",
                "受招安征方腊后，被奸臣以御赐毒酒害死，葬于楚州蓼儿洼").U(t: 4f, cd: 14f),
            H(HeroId.LuJunYi, 2, "卢俊义", "玉麒麟", Look.Spear, "点钢枪", WeaponKind.Spear, 3, UltKind.Pin, "枪挑史文恭", "总兵都头领",
                "一条杆棒，天下无对。",
                "河北大名府第一等长者，棍棒天下无对。被吴用扮算命先生赚上梁山，又遭管家李固陷害下狱，石秀跳楼劫法场才得救。",
                "被奸臣在御膳中下了水银，坠入淮河而死").U(mul: 18f, stun: 2.5f),
            H(HeroId.GongSunSheng, 4, "公孙胜", "入云龙", Look.Magic, "松纹古定剑", WeaponKind.Sword, 3, UltKind.Storm, "五雷天罡正法", "同参赞军务",
                "贫道略施法术。",
                "蓟州道士，罗真人的徒弟，能呼风唤雨、驾雾腾云。七星聚义智取生辰纲，高唐州以五雷正法破了高廉的妖术。",
                "征辽后辞别众人，回蓟州二仙山修道，侍奉老母与师父").U(mul: 3f, stun: 1f, cd: 14f),
            H(HeroId.GuanSheng, 5, "关胜", "大刀", Look.Dao, "青龙偃月刀", WeaponKind.Blade, 3, UltKind.Area, "拖刀计", "马军五虎将",
                "关某在此，谁敢来战！",
                "蒲东巡检，汉末关云长嫡派子孙，面如重枣，使青龙偃月刀。奉命攻打梁山围魏救赵，兵败被擒，感宋江义气归顺。",
                "任大名府兵马总管，一日操练回来，酒醉落马而亡").Stats(1.7f, 0.8f, 7.5f, 1, 1.0f).U(r: 3f, mul: 7f),
            H(HeroId.HuYanZhuo, 8, "呼延灼", "双鞭", Look.Whip, "八棱钢鞭", WeaponKind.Whip, 3, UltKind.Area, "连环甲马", "马军五虎将",
                "连环马到，片甲不留！",
                "宋朝开国名将呼延赞之后，使两条水磨八棱钢鞭。摆连环马大败梁山，后被徐宁的钩镰枪破阵，投青州又被擒归顺。",
                "随宋江征方腊有功，后在淮西抗金阵亡").U(r: 3f, mul: 4f, knock: 1.2f),
            H(HeroId.ChaiJin, 10, "柴进", "小旋风", Look.Support, "丹书铁券", WeaponKind.Relic, 3, UltKind.Buns, "仗义疏财", "掌管钱粮",
                "天下好汉，尽可来投。",
                "后周世宗柴荣嫡派子孙，家有太祖御赐丹书铁券。专爱结交天下好汉，林冲、武松、宋江都曾受他庇护。",
                "征方腊后辞官回沧州为民，无疾而终").U(n: 8, cd: 12f).P(buns: 1.8f),
            H(HeroId.LiYing, 11, "李应", "扑天雕", Look.Dart, "飞刀", WeaponKind.Stone, 2, UltKind.Volley, "五口飞刀", "掌管钱粮",
                "背上五口飞刀，百步取人。",
                "独龙冈李家庄庄主，使一条浑铁点钢枪，背藏五口飞刀。三打祝家庄时被祝彪暗箭射伤，后被宋江请上梁山。",
                "辞官回独龙冈，仍做富豪").U(n: 5, mul: 6f),
            H(HeroId.ZhuTong, 12, "朱仝", "美髯公", Look.Dao, "朴刀", WeaponKind.Blade, 1, UltKind.Area, "义释宋公明", "马军八骠骑",
                "兄弟快走，这里有我。",
                "郓城县马兵都头，一部虎须髯长一尺五寸。两次私放晁盖、宋江；押解雷横时又私放了他，自己刺配沧州。",
                "任保定府都统制，后随刘光世破金有功").U(r: 2.6f, mul: 5f, stun: 1f),
            H(HeroId.DongPing, 15, "董平", "双枪将", Look.Spear, "双枪", WeaponKind.Spear, 2, UltKind.Combo, "风流双枪", "马军五虎将",
                "英雄双枪将，风流万户侯。",
                "东平府兵马都监，使两条枪，琴棋书画无不通晓。阵前被扈三娘、王英套索擒住，归顺后献了东平府。",
                "征方腊时在独松关阵亡").Stats(2.3f, 0.55f, 4.6f, 3).U(n: 12),
            H(HeroId.ZhangQing, 16, "张清", "没羽箭", Look.Dart, "飞石", WeaponKind.Stone, 3, UltKind.Volley, "飞石打将", "马军八骠骑",
                "看石子！",
                "东昌府守将，善飞石打人，百发百中。一连打伤梁山十五员大将，最后被吴用诱入水中，被水军生擒。",
                "征方腊时在独松关阵亡").Stats(2.8f, 0.5f, 4.4f).U(n: 8, mul: 5f).P(crit: 0.15f, big: true),
            H(HeroId.YangZhi, 17, "杨志", "青面兽", Look.Dao, "祖传宝刀", WeaponKind.Blade, 3, UltKind.Pin, "宝刀斩牛二", "马军八骠骑",
                "杨家将的后人，岂能辱没先祖！",
                "杨令公之孙，脸上一块青记。失陷花石纲，汴京卖祖传宝刀杀了泼皮牛二；押送生辰纲在黄泥冈被吴用一伙劫去。",
                "征方腊途中在丹徒县病故").U(mul: 14f, stun: 1f),
            H(HeroId.XuNing, 18, "徐宁", "金枪手", Look.Spear, "钩镰枪", WeaponKind.Spear, 3, UltKind.Area, "钩镰破阵", "马军八骠骑",
                "钩镰一出，连环马自破。",
                "东京金枪班教师，钩镰枪天下独步。时迁盗走他家传的雁翎锁子甲，把他赚上梁山，教会众人钩镰枪法，大破连环马。",
                "征方腊时在杭州中了药箭，毒发身亡").U(r: 3f, mul: 3f, stun: 2f).P(stun: 0.3f),
            H(HeroId.SuoChao, 19, "索超", "急先锋", Look.Axe, "金蘸斧", WeaponKind.Axe, 2, UltKind.Area, "急先锋", "马军八骠骑",
                "急先锋来也，谁敢挡我！",
                "大名府正牌军，性急争功，使一柄金蘸斧。早年在大名府校场与杨志大战五十回合不分胜负，雪天陷坑中被梁山擒获。",
                "征方腊时在杭州被石宝的流星锤打死").U(r: 2.5f, mul: 6f),
            H(HeroId.LiuTang, 21, "刘唐", "赤发鬼", Look.Dao, "朴刀", WeaponKind.Blade, 1, UltKind.Area, "赤发冲阵", "步军头领",
                "一套富贵，特来献与哥哥。",
                "东潞州人，鬓边一搭朱砂记。来东溪村向晁盖报信生辰纲之事，促成七星聚义。",
                "征方腊时抢入杭州城门，被闸板压死").U(r: 2.6f, mul: 5f),
            H(HeroId.ShiJin, 23, "史进", "九纹龙", Look.Staff, "三尖两刃刀", WeaponKind.Halberd, 2, UltKind.Combo, "十八般武艺", "马军八骠骑",
                "王师父教的，一招也没忘。",
                "华阴县史家村庄主之子，身刺九条青龙。拜禁军教头王进为师学成十八般武艺，与少华山朱武等结交，从此弃家闯荡江湖。",
                "征方腊时在昱岭关被庞万春射死").Stats(1.6f, 0.75f, 8f, 1, 0.8f).U(n: 12),
            H(HeroId.MuHong, 24, "穆弘", "没遮拦", Look.Spear, "长枪", WeaponKind.Spear, 1, UltKind.Area, "没遮拦", "马军八骠骑",
                "揭阳镇上，谁不认得我穆家！",
                "揭阳镇一霸，与弟弟穆春追赶宋江到浔阳江边。得知是及时雨后赔罪结交，江州劫法场后上了梁山。",
                "征方腊时在杭州病故").U(r: 2.6f, mul: 4f, knock: 1f),
            H(HeroId.LeiHeng, 25, "雷横", "插翅虎", Look.Dao, "朴刀", WeaponKind.Blade, 1, UltKind.Area, "插翅虎扑", "步军头领",
                "跳得过两三丈阔涧！",
                "郓城县步兵都头，膂力过人，能跳两三丈阔涧。因母亲受辱打死白秀英，被朱仝私放后投奔梁山。",
                "征方腊时在德清县被司行方所杀").U(r: 2.4f, mul: 5f).P(knock: 0.2f),
            H(HeroId.LiJun, 26, "李俊", "混江龙", Look.Water, "分水刺", WeaponKind.Fork, 2, UltKind.Area, "翻江搅海", "水军头领",
                "江上的事，问我混江龙。",
                "庐州人，揭阳岭一带的私商头领，水性极好。揭阳岭救下被李立麻翻的宋江，白龙庙聚义后统领梁山水军。",
                "征方腊后诈称风瘫，与童威、童猛远走暹罗国做了国王").U(r: 3.2f, mul: 4f, stun: 1.5f),
            H(HeroId.RuanXiaoWu, 29, "阮小五", "短命二郎", Look.Water, "渔刀", WeaponKind.Blade, 1, UltKind.Area, "短命二郎", "水军头领",
                "这一腔热血，只要卖与识货的！",
                "阮氏三雄居中的一个，鬓边插朵石榴花，胸前刺着青郁郁一个豹子。与兄弟一起随晁盖智取生辰纲。",
                "征方腊时在清溪县被方腊部将所杀").U(r: 2.6f, mul: 6f),
            H(HeroId.YangXiong, 32, "杨雄", "病关索", Look.Dao, "鬼头刀", WeaponKind.Blade, 1, UltKind.Pin, "翠屏山", "步军头领",
                "多亏兄弟，叫我看清了这淫妇。",
                "蓟州两院押狱兼行刑刽子，面色微黄。听信石秀之言，在翠屏山杀了与人私通的妻子潘巧云，同石秀、时迁投奔梁山。",
                "征方腊后背生毒疮，在杭州病故").U(mul: 12f),
            H(HeroId.ShiXiu, 33, "石秀", "拼命三郎", Look.Dao, "腰刀", WeaponKind.Blade, 1, UltKind.Combo, "跳楼劫法场", "步军头领",
                "梁山泊好汉全伙在此！",
                "金陵建康府人，卖柴为生，路见不平便要拼命。大名府只身跳下酒楼劫法场救卢俊义，与杨雄一同上梁山。",
                "征方腊时在昱岭关中了埋伏，阵亡").P(crit: 0.2f, critMul: 2.5f).U(n: 8),
            H(HeroId.XieZhen, 34, "解珍", "两头蛇", Look.Fork, "浑铁钢叉", WeaponKind.Fork, 1, UltKind.Pin, "窝弓射虎", "步军头领",
                "那大虫是俺兄弟射死的！",
                "登州猎户，使浑铁点钢叉。与弟弟解宝射死一只大虫，被毛太公强占又诬陷下狱，顾大嫂劫牢救出，同上梁山。",
                "征方腊时在乌龙岭攀崖探路，失足坠亡").Stats(2.3f, 0.6f, 6f, 1, 0, 1f).U(mul: 12f),
            H(HeroId.XieBao, 35, "解宝", "双尾蝎", Look.Fork, "莲花钢叉", WeaponKind.Fork, 1, UltKind.Area, "猎虎", "步军头领",
                "哥哥，咱们叉了这老贼！",
                "解珍的弟弟，身长七尺，紫棠色面皮，腿上刺两个飞天夜叉。兄弟俩是登州有名的打虎猎户。",
                "征方腊时在乌龙岭被乱石砸死").Stats(2.3f, 0.6f, 6f, 1, 0, 1f).U(r: 2.6f, mul: 5f),

            // ─────────────── 地煞星 ───────────────
            H(HeroId.ZhuWu, 37, "朱武", "神机军师", Look.Support, "阵图", WeaponKind.Relic, 2, UltKind.HasteAll, "布阵", "同参赞军务",
                "排兵布阵，小可略知一二。",
                "少华山寨主，精通阵法、广有谋略。为救被史进擒住的陈达，与杨春负荆请罪，以义气打动了史进。",
                "随公孙胜出家修道").NoAtk().P(aura: 0.8f),
            H(HeroId.HuangXin, 38, "黄信", "镇三山", Look.Sword, "丧门剑", WeaponKind.Sword, 2, UltKind.Area, "镇三山", "马军小彪将",
                "要捉清风、二龙、桃花三山强人！",
                "青州兵马都监，秦明的徒弟，自夸要捉尽三座山的强人，故号镇三山。押解宋江、花荣途中被清风山好汉截住。",
                "征方腊后仍回青州做兵马都监").U(r: 2.4f, mul: 5f, stun: 0.8f),
            H(HeroId.SunLi, 39, "孙立", "病尉迟", Look.Whip, "竹节钢鞭", WeaponKind.Whip, 2, UltKind.Pin, "单鞭夺槊", "马军小彪将",
                "师兄，休怪小弟了。",
                "登州兵马提辖，使一条竹节钢鞭、一杆长枪。与栾廷玉是同门师兄弟，三打祝家庄时诈作援兵打入庄内，里应外合。",
                "征方腊后仍回登州做兵马提辖").U(mul: 12f, stun: 1.5f),
            H(HeroId.XuanZan, 40, "宣赞", "丑郡马", Look.Dao, "钢刀", WeaponKind.Blade, 1, UltKind.Area, "连珠箭", "马军小彪将",
                "相貌丑陋，刀法不丑。",
                "王府郡马，面如锅底，善使钢刀、射连珠箭。举荐好友关胜攻打梁山，兵败被擒。",
                "征方腊时在苏州与敌将郭世广同归于尽").U(r: 2.4f, mul: 5f),
            H(HeroId.HaoSiWen, 41, "郝思文", "井木犴", Look.Spear, "长枪", WeaponKind.Spear, 1, UltKind.Area, "井木犴", "马军小彪将",
                "我娘梦见井木犴投胎，才生了我。",
                "关胜的结义兄弟，十八般武艺无有不能。其母梦井木犴投胎而生，故有此号，随关胜一同归顺梁山。",
                "征方腊时在杭州被擒，遭方天定所害").U(r: 2.4f, mul: 4f),
            H(HeroId.HanTao, 42, "韩滔", "百胜将", Look.Spear, "枣木槊", WeaponKind.Spear, 1, UltKind.Area, "百胜", "马军小彪将",
                "一条枣木槊，百战百胜。",
                "陈州团练使，武举出身，使一条枣木槊。随呼延灼摆连环马攻打梁山，兵败后归顺。",
                "征方腊时在常州阵亡").U(r: 2.4f, mul: 4f),
            H(HeroId.PengQi, 43, "彭玘", "天目将", Look.Spear, "三尖两刃刀", WeaponKind.Halberd, 1, UltKind.Area, "天目", "马军小彪将",
                "三尖两刃，取你首级。",
                "颍州团练使，累代将门之后，使一口三尖两刃刀。随呼延灼征讨梁山时被扈三娘活捉。",
                "征方腊时在常州阵亡").U(r: 2.4f, mul: 4f),
            H(HeroId.ShanTingGui, 44, "单廷珪", "圣水将", Look.Water, "黑杆枪", WeaponKind.Spear, 1, UltKind.Area, "水浸兵", "马军小彪将",
                "以水浸兵，圣水将军是也。",
                "凌州团练使，善用水浸兵之法。与魏定国奉命攻打梁山，被关胜说降。",
                "征方腊时在歙州落入陷坑阵亡").U(r: 3f, mul: 4f, stun: 1.2f),
            H(HeroId.WeiDingGuo, 45, "魏定国", "神火将", Look.Fire, "熟铜刀", WeaponKind.Blade, 1, UltKind.Blast, "火攻", "马军小彪将",
                "看我火兵！",
                "凌州团练使，善用火攻兵法，麾下火兵专放火器。被关胜、单廷珪劝降，归顺梁山。",
                "征方腊时在歙州落入陷坑阵亡"),
            H(HeroId.XiaoRang, 46, "萧让", "圣手书生", Look.Support, "紫毫笔", WeaponKind.Relic, 1, UltKind.Shield, "仿蔡京书", "行文走檄",
                "苏、黄、米、蔡四家字体，样样学得。",
                "济州人，会写苏黄米蔡四家字体。被吴用骗上梁山，仿蔡京笔迹写假书信，想骗江州知府放了宋江。",
                "征辽后被蔡京留在府中做门馆先生").NoAtk().P(aura: 0.85f),
            H(HeroId.PeiXuan, 47, "裴宣", "铁面孔目", Look.Sword, "双剑", WeaponKind.Sword, 1, UltKind.Area, "铁面无私", "定功赏罚",
                "赏罚分明，不徇私情。",
                "京兆府孔目，为人刚直不阿，人称铁面孔目，善使双剑。被知府陷害刺配，在饮马川落草，后入梁山掌管军政司。",
                "征方腊后回饮马川受职求闲").U(r: 2.4f, mul: 4f, stun: 1f),
            H(HeroId.OuPeng, 48, "欧鹏", "摩云金翅", Look.Spear, "铁枪", WeaponKind.Spear, 1, UltKind.Pin, "金翅", "马军小彪将",
                "黄门山上，谁敢过路！",
                "黄州人，军户出身，因得罪上官逃走，在黄门山落草。宋江从江州回梁山途经黄门山，他率众相迎。",
                "征方腊时阵亡"),
            H(HeroId.DengFei, 49, "邓飞", "火眼狻猊", Look.Whip, "铁链", WeaponKind.Whip, 1, UltKind.Lasso, "铁链锁人", "马军小彪将",
                "一双红眼，看你往哪跑。",
                "盖天军襄阳府人，双睛红赤，使一条铁链。在饮马川落草，结识杨林后同上梁山。",
                "征方腊时阵亡").U(n: 3),
            H(HeroId.YanShun, 50, "燕顺", "锦毛虎", Look.Dao, "朴刀", WeaponKind.Blade, 0, UltKind.Area, "锦毛虎", "马军小彪将",
                "原来是及时雨宋公明哥哥！",
                "清风山寨主，赤发黄须。宋江被捉上清风山险些被剖心，一报名号，燕顺慌忙下拜。",
                "征方腊时阵亡"),
            H(HeroId.YangLin, 51, "杨林", "锦豹子", Look.Spear, "笔管枪", WeaponKind.Spear, 1, UltKind.Area, "锦豹子", "马军小彪将",
                "江湖上走得多了，自有门路。",
                "彰德府人，使一条笔管枪，常在江湖上做些没本钱的买卖。路遇戴宗，邀邓飞、孟康、裴宣同上梁山。",
                "征方腊后得以生还"),
            H(HeroId.LingZhen, 52, "凌振", "轰天雷", Look.Fire, "风火炮", WeaponKind.Cannon, 3, UltKind.Blast, "轰天雷", "监造号炮",
                "我这炮打出去，十四五里。",
                "燕陵人，宋朝第一个炮手，能造风火炮、金轮炮、子母炮，炮弹能打十四五里远。随呼延灼攻梁山时被活捉。",
                "征方腊后仍回朝廷火药局任职").Stats(2.8f, 1.35f, 6f, 1, 1.4f).U(mul: 10f, r: 1.8f),
            H(HeroId.JiangJing, 53, "蒋敬", "神算子", Look.Support, "算盘", WeaponKind.Relic, 1, UltKind.Buns, "神算", "考算钱粮",
                "一厘一毫，都在小可算盘上。",
                "潭州人，落科举子，精通书算，积万累千、纤毫不差。在黄门山落草，入梁山后专管钱粮出纳。",
                "征方腊后回潭州为民").NoAtk().U(n: 5),
            H(HeroId.LvFang, 54, "吕方", "小温侯", Look.Spear, "方天画戟", WeaponKind.Halberd, 2, UltKind.Area, "画戟", "守护中军",
                "爱学吕布，使一枝方天画戟。",
                "潭州人，仰慕吕布，使方天画戟。占对影山与郭盛争山头，两人画戟上的金钱豹尾绒绦绞在一起，被花荣一箭射开。",
                "征方腊时与敌将同坠岭下而亡").Stats(2.2f, 0.75f, 5f, 3).U(r: 2.6f, mul: 5f),
            H(HeroId.GuoSheng, 55, "郭盛", "赛仁贵", Look.Spear, "方天画戟", WeaponKind.Halberd, 2, UltKind.Area, "画戟", "守护中军",
                "薛仁贵在世，也不过如此。",
                "嘉陵人，仰慕薛仁贵，也使方天画戟。与吕方在对影山斗了数十日不分胜败，被花荣射开画戟后一同入伙。",
                "征方腊时在乌龙岭被飞石打死").Stats(2.2f, 0.75f, 5f, 3).U(r: 2.6f, mul: 5f),
            H(HeroId.AnDaoQuan, 56, "安道全", "神医", Look.Support, "药囊", WeaponKind.Relic, 2, UltKind.Heal, "妙手回春", "专治内外科",
                "有我在，哥哥这背疮无妨。",
                "建康府神医，祖传内外科医术。宋江背生恶疮，张顺连夜渡江把他请上梁山，治好了宋江。",
                "被宋徽宗留在京城太医院做了金紫医官"),
            H(HeroId.HuangFuDuan, 57, "皇甫端", "紫髯伯", Look.Support, "医马针", WeaponKind.Relic, 1, UltKind.Buns, "医马", "专攻医兽",
                "马匹的病，没有我医不好的。",
                "幽州人，碧眼黄须，善能相马、医马，人称紫髯伯。张清归顺后举荐他上梁山，专管马匹。",
                "征辽后被留在京城御马监").NoAtk().P(team: 0.92f),
            H(HeroId.WangYing, 58, "王英", "矮脚虎", Look.Spear, "长枪", WeaponKind.Spear, 0, UltKind.Area, "矮脚虎", "三军内探事",
                "这位小娘子，我要了！",
                "两淮人，五短身材，原是车家出身。清风山上贪色劫了刘高之妻，三打祝家庄时被扈三娘活捉，后来却娶了她。",
                "征方腊时与扈三娘一同被郑魔君所杀"),
            H(HeroId.BaoXu, 60, "鲍旭", "丧门神", Look.Sword, "阔剑", WeaponKind.Sword, 1, UltKind.Combo, "丧门", "步军将校",
                "见人便杀，管他是谁！",
                "寇州人，在枯树山落草，性情凶悍，使一口阔剑。被李逵说服，一同投奔梁山。",
                "征方腊时在杭州被石宝所杀").P(crit: 0.2f),
            H(HeroId.FanRui, 61, "樊瑞", "混世魔王", Look.Magic, "流星锤", WeaponKind.Hammer, 2, UltKind.Storm, "呼风唤雨", "步军将校",
                "风来！",
                "濮州人，芒砀山寨主，能呼风唤雨、用兵如神。与梁山斗法被公孙胜破了阵势，拜公孙胜为师。",
                "征方腊后随公孙胜出家").U(mul: 2f),
            H(HeroId.KongMing, 62, "孔明", "毛头星", Look.Spear, "长枪", WeaponKind.Spear, 0, UltKind.Area, "毛头星", "守护中军",
                "宋公明哥哥在我家庄上住过！",
                "青州白虎山孔太公之子，宋江曾在他家教他兄弟枪棒。后来在白虎山落草，三山聚义时同上梁山。",
                "征方腊时在杭州病故"),
            H(HeroId.KongLiang, 63, "孔亮", "独火星", Look.Dao, "朴刀", WeaponKind.Blade, 0, UltKind.Area, "独火星", "守护中军",
                "那醉汉打了我，快来人！",
                "孔明的弟弟，性急好斗。在酒店里与醉酒的武松争执，被武松痛打一顿丢进溪里。",
                "征方腊时在昆山落水淹死"),
            H(HeroId.XiangChong, 64, "项充", "八臂哪吒", Look.Dart, "飞刀团牌", WeaponKind.Stone, 1, UltKind.Volley, "飞刀", "步军将校",
                "团牌一面，飞刀二十四把！",
                "徐州人，使一面团牌，牌上插飞刀二十四把，百步取人。在芒砀山与樊瑞、李衮聚义，后归梁山。",
                "征方腊时在睦州被乱刀砍死"),
            H(HeroId.LiGun, 65, "李衮", "飞天大圣", Look.Dart, "标枪", WeaponKind.Stone, 1, UltKind.Volley, "标枪", "步军将校",
                "二十四根标枪，看你躲得过几根。",
                "邳县人，也使一面团牌，背插二十四根标枪。与项充一同为樊瑞左右手。",
                "征方腊时阵亡"),
            H(HeroId.JinDaJian, 66, "金大坚", "玉臂匠", Look.Support, "刻刀", WeaponKind.Relic, 1, UltKind.Upgrade, "玉臂刻石", "御宝兵符",
                "这枚印章，与真的分毫不差。",
                "济州人，刻得好石碑文字，雕得好图书玉石印记。与萧让一起被骗上梁山，伪刻蔡京图章。",
                "征辽后被宋徽宗留在内府御宝监"),
            H(HeroId.MaLin, 67, "马麟", "铁笛仙", Look.Support, "铁笛", WeaponKind.Relic, 1, UltKind.HasteAll, "铁笛声", "马军小彪将",
                "一曲铁笛，可破千军。",
                "南京建康人，小番子闲汉出身，善使双刀，吹得一管好铁笛。在黄门山落草，随宋江上梁山。",
                "征方腊时阵亡").NoAtk().P(aura: 0.85f).U(t: 2f),
            H(HeroId.TongWei, 68, "童威", "出洞蛟", Look.Water, "蛟刺", WeaponKind.Fork, 0, UltKind.Area, "出洞蛟", "水军头领",
                "浔阳江上，跟着李俊哥哥。",
                "浔阳江边人，与弟弟童猛跟随李俊做私商，水性极好。白龙庙聚义后归梁山水军。",
                "随李俊远走暹罗国").U(r: 2.6f, mul: 4f),
            H(HeroId.TongMeng, 69, "童猛", "翻江蜃", Look.Water, "渔叉", WeaponKind.Fork, 0, UltKind.Area, "翻江蜃", "水军头领",
                "翻江倒海，是俺本事。",
                "童威的弟弟，与哥哥一起追随李俊，浔阳江上下无人不知。",
                "随李俊远走暹罗国").U(r: 2.6f, mul: 4f, stun: 0.8f),
            H(HeroId.MengKang, 70, "孟康", "玉幡竿", Look.Staff, "撑篙", WeaponKind.Staff, 0, UltKind.Shovel, "督造战船", "监造战船",
                "大小战船，交给小弟。",
                "真定州人，善造大小船只，长身白肉。因杀了监船提调官逃到饮马川落草，入梁山后专管监造战船。",
                "征方腊时在乌龙岭被火炮打死").Stats(1.5f, 0.9f, 7f, 1, 0.8f),
            H(HeroId.HouJian, 71, "侯健", "通臂猿", Look.Fist, "铁剪", WeaponKind.Blade, 0, UltKind.Combo, "通臂", "专造旌旗",
                "旌旗袍袄，都是小人缝的。",
                "洪都人，做得一手好裁缝，黑瘦轻捷。原在黄文炳家做活，智取无为军时为梁山引路。",
                "征方腊时与段景住一同落水淹死").U(n: 8),
            H(HeroId.ChenDa, 72, "陈达", "跳涧虎", Look.Spear, "点钢枪", WeaponKind.Spear, 0, UltKind.Area, "跳涧虎", "马军小彪将",
                "华阴县要借粮，史家村先让路！",
                "少华山二寨主，使一条出白点钢枪。不听朱武劝阻去打史家村，被史进活捉。",
                "征方腊时在昱岭关阵亡"),
            H(HeroId.YangChun, 73, "杨春", "白花蛇", Look.Dao, "大杆刀", WeaponKind.Blade, 0, UltKind.Area, "白花蛇", "马军小彪将",
                "二哥莫急，听军师的。",
                "少华山三寨主，使一口大杆刀，腰细膀阔。与朱武一同到史家村负荆请罪，救回陈达。",
                "征方腊时在昱岭关阵亡"),
            H(HeroId.ZhengTianShou, 74, "郑天寿", "白面郎君", Look.Sword, "长剑", WeaponKind.Sword, 0, UltKind.Area, "白面郎君", "步军将校",
                "生得白净，下手可不白净。",
                "苏州人，原是银匠出身，生得白净俊俏。在清风山与燕顺、王英一同落草。",
                "征方腊时在宣州被砸死"),
            H(HeroId.TaoZongWang, 75, "陶宗旺", "九尾龟", Look.Staff, "铁锹", WeaponKind.Staff, 0, UltKind.Shovel, "开渠筑城", "监筑城垣",
                "挖沟筑墙，庄稼人的本事。",
                "光州人，庄户出身，使一把铁锹。在黄门山落草，入梁山后专管监筑城垣。",
                "征方腊时在润州阵亡").Stats(1.5f, 0.9f, 7f, 1, 0.8f),
            H(HeroId.SongQing, 76, "宋清", "铁扇子", Look.Support, "铁扇", WeaponKind.Relic, 1, UltKind.Buns, "排设筵宴", "排设筵宴",
                "哥哥，酒席备好了。",
                "宋江的亲弟弟，在家侍奉老父宋太公，为人本分。上梁山后专管排设筵宴。",
                "征方腊后回郓城县宋家村务农").NoAtk().P(buns: 1.8f),
            H(HeroId.YueHe, 77, "乐和", "铁叫子", Look.Support, "铜笛", WeaponKind.Relic, 1, UltKind.HasteAll, "高歌", "走报机密",
                "唱一曲，壮壮声势。",
                "茅州人，孙立的妻舅，登州牢里的小节级，唱得一口好曲。登州劫牢时里应外合，救出解珍、解宝。",
                "征辽后被王都尉留在府中").NoAtk().P(aura: 0.85f).U(t: 2f),
            H(HeroId.GongWang, 78, "龚旺", "花项虎", Look.Dart, "飞枪", WeaponKind.Stone, 0, UltKind.Volley, "飞枪", "步军将校",
                "脖子上的虎头，可不是画着玩的。",
                "东昌府副将，浑身刺着虎斑，脖子上吞着虎头，马上会使飞枪。随张清守东昌府，兵败被擒。",
                "征方腊时在德清县阵亡").U(n: 3),
            H(HeroId.DingDeSun, 79, "丁得孙", "中箭虎", Look.Fork, "飞叉", WeaponKind.Fork, 0, UltKind.Volley, "飞叉", "步军将校",
                "一脸疤痕，都是功劳。",
                "东昌府副将，满脸都是疤痕，马上会使飞叉。与龚旺同为张清的左右副将。",
                "征方腊时被毒蛇咬伤身亡").Stats(2.3f, 0.6f, 5f, 1, 0, 1f).U(n: 3),
            H(HeroId.MuChun, 80, "穆春", "小遮拦", Look.Staff, "短棍", WeaponKind.Club, 0, UltKind.Area, "小遮拦", "步军将校",
                "哥哥，就是这黑厮！",
                "穆弘的弟弟，揭阳镇一霸。宋江在揭阳镇赏了卖艺的薛永，穆春不忿，带人追打宋江。",
                "征方腊后回揭阳镇为民").Stats(1.5f, 0.85f, 7f, 1, 0.8f),
            H(HeroId.CaoZheng, 81, "曹正", "操刀鬼", Look.Dao, "解腕尖刀", WeaponKind.Blade, 1, UltKind.Area, "操刀", "专一屠宰",
                "杀牛宰羊，小人祖传手艺。",
                "开封府人，林冲的徒弟，屠户出身。献计让杨志、鲁智深夺了二龙山，杀了寨主邓龙。",
                "征方腊时在宣州被毒箭射死").P(buns: 1.6f),
            H(HeroId.SongWan, 82, "宋万", "云里金刚", Look.Staff, "大棍", WeaponKind.Club, 0, UltKind.Area, "云里金刚", "步军将校",
                "梁山泊最早就是咱们几个。",
                "梁山泊最早的头领之一，随王伦在梁山落草。晁盖上山后林冲火并王伦，宋万与杜迁归顺新主。",
                "征方腊时在润州阵亡，是一百单八将中最先战死的").Stats(1.5f, 0.9f, 7f, 1, 0.8f),
            H(HeroId.DuQian, 83, "杜迁", "摸着天", Look.Spear, "长枪", WeaponKind.Spear, 0, UltKind.Area, "摸着天", "步军将校",
                "伸手就能摸着天。",
                "梁山泊元老，身材长大，与王伦、宋万最早在梁山落草，后随晁盖、宋江。",
                "征方腊时在清溪县阵亡"),
            H(HeroId.XueYong, 84, "薛永", "病大虫", Look.Staff, "枪棒", WeaponKind.Club, 0, UltKind.Heal, "卖膏药", "步军将校",
                "小人卖艺卖药，诸位赏几个钱。",
                "河南洛阳人，祖传枪棒，流落江湖使枪棒卖药。在揭阳镇卖艺无人敢赏，只有宋江给了五两银子。",
                "征方腊时阵亡").Stats(1.5f, 0.85f, 6f, 1, 0.7f).U(n: 2, cd: 14f),
            H(HeroId.ShiEn, 85, "施恩", "金眼彪", Look.Fist, "铁拳", WeaponKind.Fist, 0, UltKind.Buns, "快活林", "步军将校",
                "武二哥，这快活林的酒管够！",
                "孟州牢城管营之子，开快活林酒店，被蒋门神夺去。他好生款待武松，武松醉打蒋门神替他夺回店面。",
                "征方腊时在常熟落水淹死").U(n: 4),
            H(HeroId.LiZhong, 86, "李忠", "打虎将", Look.Staff, "枪棒", WeaponKind.Club, 0, UltKind.Combo, "使枪棒", "步军将校",
                "史大郎，你我是老相识了！",
                "濠州人，江湖上使枪棒卖药，史进的开手师父。在渭州街头卖艺时与鲁达相识，后占了桃花山。",
                "征方腊时阵亡").Stats(1.5f, 0.85f, 6f, 1, 0.7f).U(n: 6),
            H(HeroId.ZhouTong, 87, "周通", "小霸王", Look.Spear, "长枪", WeaponKind.Spear, 0, UltKind.Area, "小霸王", "马军小彪将",
                "今夜做新郎，谁敢来搅！",
                "青州人，桃花山寨主，要强娶刘太公之女，洞房里被鲁智深一顿痛打，从此拜服。",
                "征方腊时阵亡"),
            H(HeroId.TangLong, 88, "汤隆", "金钱豹子", Look.Hammer, "铁锤", WeaponKind.Hammer, 1, UltKind.Upgrade, "打造钩镰", "专造军器",
                "表哥那副钩镰枪，我打得出来。",
                "延安府人，铁匠出身，徐宁的姑舅兄弟。献计让时迁盗甲，赚徐宁上山破连环马，又亲手打造钩镰枪。",
                "征方腊时阵亡"),
            H(HeroId.DuXing, 89, "杜兴", "鬼脸儿", Look.Dao, "朴刀", WeaponKind.Blade, 0, UltKind.Area, "鬼脸儿", "南山酒店",
                "多亏杨雄恩人，杜兴才有今日。",
                "中山府人，面颜生得粗莽，李应的主管。在蓟州受杨雄救命之恩，三打祝家庄时牵线让李应投了梁山。",
                "随李应回独龙冈为民"),
            H(HeroId.ZouYuan, 90, "邹渊", "出林龙", Look.Dao, "大斧", WeaponKind.Axe, 0, UltKind.Area, "出林龙", "步军将校",
                "登州山里，叔侄俩说了算。",
                "莱州人，在登州登云山聚众，与侄儿邹润相依。随顾大嫂劫牢救出解氏兄弟，同上梁山。",
                "征方腊时阵亡"),
            H(HeroId.ZouRun, 91, "邹润", "独角龙", Look.Fist, "铁头", WeaponKind.Fist, 0, UltKind.Throw, "一头撞折松树", "步军将校",
                "我这颗头，撞得断松树！",
                "邹渊的侄子，脑后生一个肉瘤，与人争斗一头撞去，曾把一株松树撞折。",
                "征方腊后回登云山").P(knock: 0.3f),
            H(HeroId.ZhuGui, 92, "朱贵", "旱地忽律", Look.Bow, "响箭", WeaponKind.Bow, 0, UltKind.Buns, "旱地酒店", "南山酒店",
                "客官从哪里来？要上梁山，先喝一碗。",
                "沂州人，在梁山泊东边开酒店打探消息，见有好汉投奔便放一支响箭，唤船来接。林冲上梁山就是他引的路。",
                "征方腊时在杭州病故").U(n: 4),
            H(HeroId.ZhuFu, 93, "朱富", "笑面虎", Look.Poison, "蒙汗酒", WeaponKind.Relic, 0, UltKind.Area, "蒙汗药酒", "监造酒醋",
                "师父，喝了这碗喜酒再走。",
                "朱贵的弟弟，开酒店为生。为救被擒的李逵，用蒙汗药酒麻翻了师父李云和押解的土兵。",
                "征方腊时在杭州病故").U(r: 3f, stun: 2.5f),
            H(HeroId.CaiFu, 94, "蔡福", "铁臂膊", Look.Dao, "鬼头刀", WeaponKind.Blade, 1, UltKind.Pin, "刽子手", "专管行刑",
                "这一刀下去，可就回不了头了。",
                "大名府两院押狱兼行刑刽子，臂力过人。受柴进所托保全卢俊义性命，梁山破大名府后上山。",
                "征方腊时在杭州阵亡"),
            H(HeroId.CaiQing, 95, "蔡庆", "一枝花", Look.Dao, "鬼头刀", WeaponKind.Blade, 0, UltKind.Area, "一枝花", "专管行刑",
                "鬓边一枝花，刀下不留情。",
                "蔡福的弟弟，生来爱戴一枝花，与哥哥一同在大名府当刽子手。",
                "征方腊后回大名府为民"),
            H(HeroId.LiLi, 96, "李立", "催命判官", Look.Poison, "蒙汗药", WeaponKind.Relic, 0, UltKind.Area, "黑店", "北山酒店",
                "这几个肥羊，正好开剥。",
                "庐州人，在揭阳岭开黑店，专用蒙汗药麻翻客人。宋江险些被他开剥，幸亏李俊赶到认了出来。",
                "征方腊时阵亡").U(r: 3f, stun: 2.5f),
            H(HeroId.LiYun, 97, "李云", "青眼虎", Look.Dao, "朴刀", WeaponKind.Blade, 0, UltKind.Area, "青眼虎", "监造房舍",
                "徒弟，你竟敢药我！",
                "沂水县都头，碧眼黄须。押解李逵时被徒弟朱富用蒙汗药麻翻，醒来后无处可去，只得一同上了梁山。",
                "征方腊时阵亡"),
            H(HeroId.JiaoTing, 98, "焦挺", "没面目", Look.Fist, "铁拳", WeaponKind.Fist, 0, UltKind.Throw, "相扑", "步军将校",
                "祖传三代相扑，投人不着。",
                "中山府人，祖传三代相扑，一跤把李逵摔倒在地。因没人收留，在枯树山与鲍旭为伍，后随李逵上梁山。",
                "征方腊时在润州阵亡").P(knock: 0.3f),
            H(HeroId.ShiYong, 99, "石勇", "石将军", Look.Staff, "短棒", WeaponKind.Club, 0, UltKind.Area, "石将军", "步军将校",
                "天下只认得两个人：柴大官人与宋公明！",
                "大名府人，赌博打死了人，在柴进庄上避难。替宋江送家书，在酒店里遇见宋江，同上梁山。",
                "征方腊时阵亡").Stats(1.5f, 0.85f, 6f, 1, 0.7f),
            H(HeroId.SunXin, 100, "孙新", "小尉迟", Look.Whip, "单鞭", WeaponKind.Whip, 0, UltKind.Pin, "小尉迟", "东山酒店",
                "哥哥的鞭法，我也学了七八分。",
                "孙立的弟弟，学得哥哥的鞭法，与妻子顾大嫂在登州东门外开酒店。",
                "征方腊后回登州为民"),
            H(HeroId.GuDaSao, 101, "顾大嫂", "母大虫", Look.Dao, "尖刀", WeaponKind.Blade, 0, UltKind.Area, "劫牢", "东山酒店",
                "你们不去，老娘一个人去劫牢！",
                "孙新之妻，眉粗眼大，胖面肥腰，三五十人近她不得。为救表弟解珍、解宝，逼着孙立一同登州劫牢。",
                "征方腊后回登州为民").P(buns: 1.8f),
            H(HeroId.ZhangQingY, 102, "张青", "菜园子", Look.Dao, "朴刀", WeaponKind.Blade, 0, UltKind.Area, "十字坡", "西山酒店",
                "浑家莫动手，这位是好汉！",
                "孟州十字坡人，原在光明寺种菜，后与孙二娘开酒店。劝妻子三等人不可坏：云游僧道、江湖妓女、流配犯人。",
                "征方腊时在歙州阵亡").P(buns: 1.6f),
            H(HeroId.WangDingLiu, 104, "王定六", "活闪婆", Look.Fist, "短刀", WeaponKind.Fist, 0, UltKind.Combo, "活闪", "北山酒店",
                "跑得比闪电还快！",
                "建康府人，走跳得快，人称活闪婆。张顺被截江鬼张旺所劫，王定六收留了他，一同上了梁山。",
                "征方腊时在宣州阵亡").Stats(1.35f, 0.32f, 3.2f).U(n: 8),
            H(HeroId.YuBaoSi, 105, "郁保四", "险道神", Look.Support, "帅字旗", WeaponKind.Relic, 1, UltKind.Shield, "把捧帅旗", "把捧帅字旗",
                "这杆帅字旗，我替哥哥擎着。",
                "凌州人，身长一丈，腰阔数围。曾劫夺段景住献给梁山的马匹，被宋江收服，入伙后专捧帅字旗。",
                "征方腊时在睦州阵亡").NoAtk().P(team: 0.92f),
            H(HeroId.BaiSheng, 106, "白胜", "白日鼠", Look.Poison, "酒桶", WeaponKind.Relic, 0, UltKind.Area, "黄泥冈卖酒", "走报机密",
                "赤日炎炎似火烧，野田禾稻半枯焦。",
                "黄泥冈东十里安乐村闲汉。智取生辰纲时扮卖酒汉子，挑着下了蒙汗药的酒上冈，麻翻了杨志一行。",
                "征方腊时在杭州病故").U(r: 3f, stun: 2.5f),
            H(HeroId.ShiQian, 107, "时迁", "鼓上蚤", Look.Dart, "飞檐走壁", WeaponKind.Stone, 0, UltKind.Steal, "盗甲", "走报机密",
                "这副雁翎甲，小人顺手借来了。",
                "高唐州人，飞檐走壁、偷鸡摸狗的本事天下第一。祝家店偷鸡引发三打祝家庄，又盗徐宁宝甲赚他上山。",
                "征方腊后在杭州病故").P(crit: 0.25f, critMul: 2.5f),
            H(HeroId.DuanJingZhu, 108, "段景住", "金毛犬", Look.Support, "照夜玉狮子", WeaponKind.Relic, 1, UltKind.Steal, "盗马", "走报机密",
                "这匹宝马，本想献给寨主……",
                "涿州人，赤发黄须，以盗马为生。盗得金国王子的照夜玉狮子马想献给宋江，被曾头市夺去，引出晁盖攻打曾头市。",
                "征方腊时与侯健一同落水淹死").U(n: 5),
        };

        HeroDef Hide() { Hidden = true; return this; }
    }
}
