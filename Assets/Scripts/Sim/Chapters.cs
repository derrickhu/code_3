using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
    /// <summary>
    /// 头目绝技。前三种只管官兵自己；后面的直接冲着棋盘来：定住字、压乌云、贴封条、下妖雨、砸字、抢炊饼。
    /// </summary>
    public enum BossSkill { Summon, Haste, Resist, Daze, Fog, Seal, Rain, Charge, Crush, Steal, Devour, Revive }

    public sealed class BossDef
    {
        public string Name;
        public string Title;
        /// <summary>轮流放的绝技。后期头目有两门。</summary>
        public BossSkill[] Skills = { BossSkill.Summon };
        public HeroId Nemesis;
        public string SkillText;

        public BossSkill Skill
        {
            get => Skills[0];
            set => Skills = new[] { value };
        }

        public bool Has(BossSkill s) => Array.IndexOf(Skills, s) >= 0;

        public static string SkillName(BossSkill s) => s switch
        {
            BossSkill.Summon => "来人",
            BossSkill.Haste => "催阵",
            BossSkill.Resist => "铁布衫",
            BossSkill.Daze => "定身",
            BossSkill.Fog => "乌云压阵",
            BossSkill.Seal => "封条",
            BossSkill.Rain => "妖雨",
            BossSkill.Charge => "冲阵",
            BossSkill.Crush => "砸字",
            BossSkill.Steal => "强抢",
            BossSkill.Devour => "吞兵",
            _ => "招魂",
        };

        public static string SkillDesc(BossSkill s, HeroId nemesis) => s switch
        {
            BossSkill.Summon => "每隔几秒招来三个兵丁",
            BossSkill.Haste => "喝令周围兵丁加速猛冲",
            BossSkill.Resist => $"只受一半伤害（{HeroDef.Get(nemesis).Name}例外）",
            BossSkill.Daze => "定住你最高级的三个字，三秒不能出手",
            BossSkill.Fog => "黑云压住一片棋盘：云下不能摆字，出手变慢",
            BossSkill.Seal => "给三格贴封条：封住的格不能摆字，字也不出手",
            BossSkill.Rain => "满场妖雨，所有字出手变慢",
            BossSkill.Charge => "猛地往前冲一大段",
            BossSkill.Crush => "把你最高级的兵砸掉一级",
            BossSkill.Steal => "抢走你一把炊饼",
            BossSkill.Devour => "吞掉身边的兵丁回血",
            _ => "把刚倒下的兵丁叫起来",
        };

        public static string Describe(BossSkill[] skills, HeroId nemesis)
        {
            var parts = new string[skills.Length];
            for (int i = 0; i < skills.Length; i++) parts[i] = SkillName(skills[i]) + "：" + SkillDesc(skills[i], nemesis);
            return string.Join("；", parts);
        }

        /// <summary>头目解法提示：供局内「军情密报」明牌展示，不让玩家摸黑试错。</summary>
        public static string CounterTip(BossSkill s) => s switch
        {
            BossSkill.Summon => "架穿透长枪或范围好汉，连小兵带头目一并扫清",
            BossSkill.Haste => "备蒙汗药、陷阱或钩镰枪绊住冲阵脚步",
            BossSkill.Resist => "务必凑出本回克星好汉（无视铁布衫且5倍重创）",
            BossSkill.Daze => "多铺中阶副手分摊火力，勿把身家全压单格",
            BossSkill.Fog => "提前把核心好汉摆在远离中路的净土或水寨",
            BossSkill.Seal => "留一两格备用白地，封条七秒散去即复原",
            BossSkill.Rain => "用攻速符、吴用或戴宗加速抵消满场妖雨",
            BossSkill.Charge => "路口摆骑兵与重杖击退小兵，留绝技截杀头目",
            BossSkill.Crush => "优先凑成好汉（头目砸字只降普通小兵阶数）",
            BossSkill.Steal => "炊饼攒够即点聚义，勿留巨款给贼人强抢",
            BossSkill.Devour => "抢在头目近身前清光周围小卒，断其回血口粮",
            _ => "速战速决压低血线，用范围绝技震散借尸小卒",
        };

        /// <summary>克星好汉在场撞见宿敌头目时的原著宿命交锋台词。</summary>
        public string DuelShout()
        {
            var hero = HeroDef.Get(Nemesis);
            if (Nemesis == HeroId.LinChong && Name == "陆谦") return "林冲挺矛怒喝：奸贼陆谦，还我草料场命来！";
            if (Nemesis == HeroId.LinChong && Name == "王伦") return "林冲拍案拔刀：嫉贤妒能之辈，留你何用！";
            if (Nemesis == HeroId.WuSong && Name == "大虫") return "武松双拳生风：景阳冈大虫，吃俺老武几拳！";
            if (Nemesis == HeroId.WuSong && Name == "西门庆") return "武松目眦欲裂：西门贼子，拿命祭我哥哥！";
            if (Nemesis == HeroId.WuSong && Name == "蒋门神") return "武松醉步欺身：玉环步鸳鸯脚，叫你认得武二郎！";
            if (Nemesis == HeroId.LuZhiShen && Name == "郑屠") return "鲁提辖怒目圆睁：镇关西，洒家教你尝尝三拳滋味！";
            if (Nemesis == HeroId.LuZhiShen && Name == "方腊") return "花和尚禅杖生风：逢夏而擒，遇腊而执，哪里走！";
            if (Nemesis == HeroId.HuaRong) return $"小李广挽雕弓：贼将{Name}看箭，弦响处休想逃生！";
            if (Nemesis == HeroId.LiKui) return $"黑旋风抡双斧：杀千刀的{Name}，吃铁牛一板斧！";
            if (Nemesis == HeroId.ZhangShun) return $"浪里白条翻江而起：{Name}老贼，下水吃个饱吧！";
            if (Nemesis == HeroId.GongSunSheng) return $"入云龙仗松纹剑：旁门左道{Name}，看贫道五雷正法！";
            if (Nemesis == HeroId.YangZhi) return $"青面兽掣祖传宝刀：{Name}休狂，杨家将刀法在此！";
            if (Nemesis == HeroId.XuNing) return "金枪手挺钩镰枪：连环甲马，一钩便破！";
            return $"{hero.Nick}{hero.Name}怒喝：{Name}休走，今日新仇旧恨一并算清！";
        }
    }

    /// <summary>宿敌原著用兵流派：明牌取代假真人黑盒，让每回对手贴合原著身份。</summary>
    public enum RivalStyle { Balanced, Vanguard, Marksman, Lancer, Strategist }

    /// <summary>水泊名役（困难机制章）与演武场的战场天象/军令规则。</summary>
    public enum BattleRule
    {
        None,
        /// <summary>赤日炎炎：开局多25炊饼，官兵燥热疾行+15%，孙二娘/白胜/吴用等智取系攻速+20%。</summary>
        ScorchingSun,
        /// <summary>铁甲连环：每波必刷重甲大汉，枪系小兵与长枪/鞭锏好汉伤害+35%。</summary>
        IronCavalry,
        /// <summary>水漫梁山：官兵趟水减速15%，水军与水上格好汉伤害+35%。</summary>
        FloodedMarsh,
        /// <summary>风雪严寒：全场妖雨寒风更密，近战刀/斧/杖好汉伤害+35%。</summary>
        Blizzard,
        /// <summary>盘陀迷阵：出怪更快但每斩杀5名官兵额外赏1炊饼，弓/暗器射程+25%。</summary>
        Labyrinth,
        /// <summary>梁山军司演武（每日轮换）：当日点名军司的好汉伤害+25%。</summary>
        CorpsFocus,
    }

    public enum CorpsKind { None, Cavalry, Infantry, Navy, Mystic }

    /// <summary>Grass/Marsh 未开垦不能摆；Tile/Water 是开好的地。</summary>
    public enum Cell { Grass, Path, Water, Tile, Rock, Marsh }

    /// <summary>一回战斗。剧情回按原著顺序排开，赢下后 Joins 里的好汉入伙；通关后是聚义厅车轮战。</summary>
    public sealed class ChapterDef
    {
        public int Index;
        public string Name;
        /// <summary>原著回目。</summary>
        public string Hui;
        /// <summary>所属篇章。</summary>
        public string Act;
        public string Story;
        /// <summary>水浒小课堂：赢下后给玩家讲的一段原著典故。</summary>
        public string Lore;
        public string[] Map;
        public HeroId[] Pool;
        public HeroId[] Joins = new HeroId[0];
        public BossDef Boss;
        public float RivalSkill;
        public float RivalIncome = 1f;
        /// <summary>保留字段。双方官兵血量相同，不再给对手单独加厚。</summary>
        public float RivalFoeHp = 1f;
        /// <summary>这一回双方官兵的血量。第一回是 1，越往后两边一起变厚。</summary>
        public float FoeHp = 1f;
        public string RivalName;
        /// <summary>聚义厅第几阵；0 表示剧情回。</summary>
        public int Round;
        /// <summary>限波速战回（前两回新手节奏）：撑满并肃清第 MaxWaves 波头目即胜；0 表示常规对弈直到一方宋江倒下。</summary>
        public int MaxWaves;
        /// <summary>战场词缀/军令规则。</summary>
        public BattleRule Rule;
        /// <summary>每日演武点名的梁山军司。</summary>
        public CorpsKind Corps;
        /// <summary>好汉列传序号 0..35；-1 表示非列传。</summary>
        public int BioIndex = -1;
        /// <summary>水泊名役序号 0..11；-1 表示非名役。</summary>
        public int CampaignIndex = -1;

        public bool IsEndless => Round > 0;
        public bool IsBio => BioIndex >= 0;
        public bool IsCampaign => CampaignIndex >= 0;

        public static string CorpsName(CorpsKind c) => c switch
        {
            CorpsKind.Cavalry => "马军诸将",
            CorpsKind.Infantry => "步军头领",
            CorpsKind.Navy => "水军八寨",
            CorpsKind.Mystic => "军师奇门",
            _ => "",
        };

        public static bool InCorps(HeroDef d, CorpsKind c)
        {
            if (d == null || c == CorpsKind.None) return false;
            var p = d.Post ?? "";
            return c switch
            {
                CorpsKind.Cavalry => p.Contains("马军") || p.Contains("总兵"),
                CorpsKind.Infantry => p.Contains("步军") || p.Contains("中军"),
                CorpsKind.Navy => p.Contains("水军") || d.WaterBonus > 1.05f,
                CorpsKind.Mystic => d.Look == Look.Magic || d.Look == Look.Support || d.WKind == WeaponKind.Relic || d.WKind == WeaponKind.Cannon || d.WKind == WeaponKind.Stone,
                _ => false,
            };
        }

        public string RuleTitle => Rule switch
        {
            BattleRule.ScorchingSun => "赤日炎炎",
            BattleRule.IronCavalry => "铁甲洪流",
            BattleRule.FloodedMarsh => "水漫苇荡",
            BattleRule.Blizzard => "风雪奇寒",
            BattleRule.Labyrinth => "盘陀迷阵",
            BattleRule.CorpsFocus => CorpsName(Corps) + "当值",
            _ => "",
        };

        public string RuleDesc => Rule switch
        {
            BattleRule.ScorchingSun => "开局多 25 个炊饼；官兵走得快 15%，用毒与辅助好汉出手快 20%",
            BattleRule.IronCavalry => "每波必混入重甲兵；枪兵与使枪、鞭的好汉伤害高 35%",
            BattleRule.FloodedMarsh => "官兵趟水慢 15%；水军与站在水上的好汉伤害高 35%",
            BattleRule.Blizzard => "寒风急促；使刀、斧、禅杖与拳脚的好汉伤害高 35%",
            BattleRule.Labyrinth => "每斩 5 名官兵多掉 1 个炊饼；弓兵与使弓、飞石的好汉射程加 25%",
            BattleRule.CorpsFocus => $"今日「{CorpsName(Corps)}」领军出征，所属好汉伤害高 25%",
            _ => "",
        };

        /// <summary>按原著对手身份映射用兵门道，开场与军情面板明牌告知。</summary>
        public RivalStyle RivalStyleOf()
        {
            if (string.IsNullOrEmpty(RivalName)) return RivalStyle.Balanced;
            if (RivalName.Contains("吴用") || RivalName.Contains("晁盖") || RivalName.Contains("王伦") || RivalName.Contains("蒋敬") || RivalName.Contains("裴宣") || RivalName.Contains("蔡京"))
                return RivalStyle.Strategist;
            if (RivalName.Contains("黄信") || RivalName.Contains("吕方") || RivalName.Contains("祝彪") || RivalName.Contains("曾") || RivalName.Contains("花荣"))
                return RivalStyle.Marksman;
            if (RivalName.Contains("高俅") || RivalName.Contains("童贯") || RivalName.Contains("梁中书") || RivalName.Contains("慕容") || RivalName.Contains("辽主") || RivalName.Contains("王进"))
                return RivalStyle.Lancer;
            if (Index % 4 == 1) return RivalStyle.Vanguard;
            if (Index % 4 == 2) return RivalStyle.Marksman;
            if (Index % 4 == 3) return RivalStyle.Lancer;
            return RivalStyle.Balanced;
        }

        public string RivalStyleLabel => RivalStyleOf() switch
        {
            RivalStyle.Vanguard => "近战刀阵·贴路猛劈",
            RivalStyle.Marksman => "飞矢封路·远程攒射",
            RivalStyle.Lancer => "长枪铁骑·贯阵冲杀",
            RivalStyle.Strategist => "广聚名讳·攒字唤将",
            _ => "四平八稳·步步为营",
        };

        // ─────────────────────────── 地图模板 ───────────────────────────

        public static readonly Dictionary<string, string[]> Maps = new Dictionary<string, string[]>
        {
            ["shijia"] = new[] { "xS######", ".ooooo.#", ".#######", ".#ooooo.", "x#####G." },
            ["qingfeng"] = new[] { "S######.", ".ooo..#.", ".######.", ".#ooo...", ".######G" },
            ["jiangzhou"] = new[] { "S######.", ".ooo..#.", ".###G.#.", ".#oo.o#.", ".######." },
            ["bailong"] = new[] { "~~~~~~~S", "########", "#.ooooo.", "########", ".......G" },
            ["snake"] = new[] { "xS######", ".oooo..#", ".#######", ".#oooo..", "x#####G." },
            ["ring"] = new[] { "........", "S#######", ".o..o..#", ".o..ooo#", "G#######" },
            ["jiuqu"] = new[] { "S..####.", "#o.#o.#.", "#o.#o.#o", "#o.#..#.", "####..G." },
            ["marsh"] = new[] { "S######~", "~wwo~~#~", "~######~", "~#~~wwo~", "~######G" },
            ["zigzag"] = new[] { "S###..o.", ".oo####.", "xx.ooo#.", "...####.", "G###...." },
            ["plain"] = new[] { "....o...", "S#######", ".oo.ooo#", "G#######", "........" },
            ["river"] = new[] { "S.....#G", "#o###.#.", "#o#o#o#.", "###.###.", "~~~ww~~~" },
            ["bagua"] = new[] { "S######.", ".oo.o.#.", ".###G.#.", ".#oo..#.", ".######." },
            ["ferry"] = new[] { "xS######", "~ww.oo.#", "~#######", "~#w.oo~~", "~######G" },
            ["pass"] = new[] { "xS#####x", "xoo...#.", ".####.#.", ".#oo#o#.", ".G..###." },
            ["imperial"] = new[] { "S#.###.G", ".#.#o#o#", ".#.#o#o#", ".#o#.#.#", ".###.###" },
        };

        static readonly string[] EndlessMaps = { "qingfeng", "jiangzhou", "bailong", "snake", "ring", "jiuqu", "marsh", "zigzag", "plain", "river", "bagua", "ferry", "pass", "imperial" };

        // ─────────────────────────── 剧情 ───────────────────────────

        public static readonly ChapterDef[] All = BuildStory();

        public static int StoryCount => All.Length;

        static ChapterDef C(string name, string hui, string act, string map, HeroId[] joins, HeroId[] extra,
            string boss, string title, BossSkill[] skills, HeroId nemesis, string rival, string story, string lore)
        {
            var pool = new List<HeroId>(joins);
            foreach (var h in extra) if (!pool.Contains(h)) pool.Add(h);
            return new ChapterDef
            {
                Name = name, Hui = hui, Act = act, Map = Maps[map], Joins = joins, Pool = pool.ToArray(),
                Boss = new BossDef { Name = boss, Title = title, Skills = skills, Nemesis = nemesis, SkillText = BossDef.Describe(skills, nemesis) },
                RivalName = rival, Story = story, Lore = lore,
            };
        }

        static HeroId[] J(params HeroId[] ids) => ids;
        static BossSkill[] B(params BossSkill[] s) => s;

        static ChapterDef[] BuildStory()
        {
            var list = new[]
            {
                C("史家村", "第二回 王教头私走延安府 九纹龙大闹史家村", "引首", "shijia",
                    J(HeroId.ShiJin, HeroId.ZhuWu, HeroId.ChenDa, HeroId.YangChun), J(),
                    "县尉", "华阴县", B(BossSkill.Summon), HeroId.ShiJin, "王进",
                    "少华山的跳涧虎陈达要借道华阴县，被九纹龙史进活捉。朱武、杨春负荆请罪，史进敬他们义气，结为兄弟——官府却连夜围了史家庄！",
                    "《水浒传》开篇，洪太尉误走妖魔，从伏魔殿放出一百单八个魔君，正是日后梁山的三十六天罡、七十二地煞。"),
                C("拳打镇关西", "第三回 史大郎夜走华阴县 鲁提辖拳打镇关西", "鲁达篇", "qingfeng",
                    J(HeroId.LuZhiShen, HeroId.LiZhong), J(HeroId.ShiJin, HeroId.ZhuWu),
                    "郑屠", "镇关西", B(BossSkill.Haste), HeroId.LuZhiShen, "金老汉",
                    "渭州状元桥下，卖肉的郑屠自称镇关西，欺压卖唱的金家父女。提辖鲁达听了，拍案而起：洒家这就去会会他！",
                    "鲁达三拳：一拳打在鼻子上，咸的酸的辣的一发滚出；二拳打在眼眶，红的黑的绛的都绽将出来；三拳太阳穴，磬儿钹儿铙儿一齐响。"),
                C("桃花村", "第五回 小霸王醉入销金帐 花和尚大闹桃花村", "鲁达篇", "jiangzhou",
                    J(HeroId.ZhouTong), J(HeroId.LuZhiShen, HeroId.LiZhong, HeroId.ShiJin),
                    "崔道成", "生铁佛", B(BossSkill.Resist), HeroId.ShiJin, "刘太公",
                    "鲁智深大闹五台山被打发去东京，路过桃花村，醉打要强娶民女的小霸王周通；转到瓦罐寺，又撞上恶僧生铁佛崔道成。",
                    "鲁智深醉打周通之后，周通与李忠占的桃花山，后来也成了梁山的一支人马。"),
                C("倒拔垂杨柳", "第七回 花和尚倒拔垂杨柳 豹子头误入白虎堂", "鲁达篇", "bagua",
                    J(), J(HeroId.LuZhiShen, HeroId.LinChong, HeroId.ShiJin),
                    "张三", "过街老鼠", B(BossSkill.Steal), HeroId.LuZhiShen, "李四",
                    "东京大相国寺的菜园里，泼皮张三、李四想给新来的鲁智深一个下马威。绿杨树上乌鸦聒噪，鲁智深脱下直裰，抱住碗口粗的垂杨柳一声大吼——",
                    "鲁智深连根拔起垂杨柳，泼皮们磕头如捣蒜；八十万禁军教头林冲正好从墙外经过，两人一见如故，结为生死弟兄。"),
                C("误入白虎堂", "第七回 花和尚倒拔垂杨柳 豹子头误入白虎堂", "林冲篇", "imperial",
                    J(), J(HeroId.LinChong, HeroId.LuZhiShen, HeroId.LiZhong),
                    "富安", "太尉府干办", B(BossSkill.Seal), HeroId.LinChong, "高衙内",
                    "高衙内调戏林娘子不成，干办富安献出毒计：诓林冲买下一口宝刀，再假传高太尉军令请他进府比看——待他踏入白虎节堂，四面伏兵齐出！",
                    "宋代军机重地「白虎节堂」严禁携刃擅入。高俅借商议军机之名陷害林冲，将其脊杖二十、刺配沧州牢城。"),
                C("野猪林", "第八回 林教头刺配沧州道 鲁智深大闹野猪林", "林冲篇", "ring",
                    J(HeroId.LinChong), J(HeroId.LuZhiShen, HeroId.ShiJin, HeroId.LiZhong),
                    "薛霸", "押解公人", B(BossSkill.Daze), HeroId.LuZhiShen, "董超",
                    "林冲被高俅陷害，刺配沧州。押解公人董超、薛霸受了贿赂，要在野猪林结果他性命，水火棍刚举起——一条禅杖从松林里飞了出来！",
                    "董超、薛霸一路用滚水烫林冲的脚，鲁智深一路暗中护送，这才有了野猪林救林冲。"),
                C("柴进庄", "第九回 柴进门招天下客 林冲棒打洪教头", "林冲篇", "plain",
                    J(HeroId.ChaiJin), J(HeroId.LinChong, HeroId.LuZhiShen),
                    "洪教头", "柴府教师", B(BossSkill.Crush), HeroId.LinChong, "庄客",
                    "沧州小旋风柴进专爱结交好汉，盛情款待林冲。庄上的洪教头不服，硬要比棒——林冲只得出手。",
                    "柴进是后周世宗柴荣的后人。宋太祖陈桥兵变得了柴家天下，所以赐了丹书铁券，犯罪也不得加刑。"),
                C("风雪山神庙", "第十回 林教头风雪山神庙 陆虞候火烧草料场", "林冲篇", "zigzag",
                    J(HeroId.ZhuGui), J(HeroId.LinChong, HeroId.ChaiJin, HeroId.LuZhiShen),
                    "陆谦", "陆虞候", B(BossSkill.Fog), HeroId.LinChong, "富安",
                    "大雪压塌了草屋，林冲到山神庙避风，听见门外陆谦放火烧了草料场，要他葬身火海。忍无可忍，林冲挺枪而出。",
                    "林冲被逼得家破人亡，只得投奔梁山——这就是成语「逼上梁山」的来历。"),
                C("杨志卖刀", "第十二回 梁山泊林冲落草 汴京城杨志卖刀", "杨志篇", "river",
                    J(HeroId.YangZhi), J(HeroId.LinChong, HeroId.ZhuGui, HeroId.ChaiJin),
                    "牛二", "没毛大虫", B(BossSkill.Steal), HeroId.YangZhi, "王伦",
                    "杨家将后人杨志失陷花石纲，穷得只能在汴京街头卖祖传宝刀。泼皮牛二偏要他当街试刀杀人……",
                    "杨志的宝刀有三好：砍铜剁铁刀口不卷，吹毛得过，杀人刀上没血。"),
                C("智取生辰纲", "第十六回 杨志押送金银担 吴用智取生辰纲", "生辰纲篇", "ring",
                    J(HeroId.WuYong, HeroId.GongSunSheng, HeroId.LiuTang, HeroId.BaiSheng), J(HeroId.YangZhi),
                    "老都管", "梁府都管", B(BossSkill.Summon), HeroId.BaiSheng, "梁中书",
                    "梁中书给岳父蔡京送十万贯生辰纲。黄泥冈上烈日当头，一个卖酒的汉子挑着担子唱着山歌走来……",
                    "白胜唱的「赤日炎炎似火烧，野田禾稻半枯焦」，是书中最有名的一首山歌。"),
                C("二龙山", "第十七回 花和尚单打二龙山 青面兽双夺宝珠寺", "生辰纲篇", "jiangzhou",
                    J(HeroId.CaoZheng), J(HeroId.YangZhi, HeroId.LuZhiShen, HeroId.LiZhong),
                    "邓龙", "二龙山寨主", B(BossSkill.Resist), HeroId.YangZhi, "周通",
                    "丢了生辰纲，杨志无路可走，在曹正酒店里遇见了鲁智深。曹正献计：绑了花和尚诈作献俘，打上二龙山！",
                    "二龙山宝珠寺后来成为三山聚义中的一山，鲁智深、杨志、武松、施恩、张青、孙二娘都在这里。"),
                C("私放晁天王", "第十八回 美髯公智稳插翅虎 宋公明私放晁天王", "生辰纲篇", "snake",
                    J(HeroId.ZhuTong, HeroId.LeiHeng), J(HeroId.WuYong, HeroId.LiuTang, HeroId.GongSunSheng),
                    "府尹", "济州", B(BossSkill.Summon), HeroId.ZhuTong, "何涛",
                    "生辰纲案发，济州官兵连夜来捉晁盖。押司宋江飞马报信，都头朱仝、雷横一前一后，都想放晁天王一条生路。",
                    "宋江私放晁盖，是整个梁山故事的转折点——没有这一放，就没有后来的梁山泊。"),
                C("石碣村", "第十九回 晁盖梁山小夺泊", "生辰纲篇", "marsh",
                    J(HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi), J(HeroId.WuYong, HeroId.LiuTang),
                    "何涛", "缉捕观察", B(BossSkill.Haste), HeroId.RuanXiaoQi, "黄安",
                    "缉捕观察何涛带着官船杀进石碣村芦苇荡。阮家三兄弟笑了：在水上和打鱼的斗？",
                    "吴用去石碣村说三阮入伙，三兄弟拍着脖子说：「这腔热血，只要卖与识货的！」"),
                C("火并王伦", "第十九回 林冲水寨大并火", "生辰纲篇", "snake",
                    J(HeroId.DuQian, HeroId.SongWan), J(HeroId.LinChong, HeroId.WuYong, HeroId.ZhuGui),
                    "王伦", "白衣秀士", B(BossSkill.Seal), HeroId.LinChong, "晁盖",
                    "晁盖一伙投奔梁山，寨主王伦却拿出五锭大银要打发他们下山。林冲踢翻桌子：你这嫉贤妒能的贼！",
                    "王伦心胸狭窄、嫉贤妒能。林冲火并王伦，拥晁盖为寨主，梁山从此兴旺起来。"),
                C("怒杀阎婆惜", "第二十二回 阎婆大闹郓城县 朱仝义释宋公明", "宋江篇", "jiuqu",
                    J(HeroId.SongQing), J(HeroId.ZhuTong, HeroId.LeiHeng, HeroId.ChaiJin),
                    "张文远", "郓城押司", B(BossSkill.Haste), HeroId.ZhuTong, "阎婆",
                    "阎婆惜拿到晁盖写给宋江的书信，要挟宋江。一时失手，宋江杀了她。张文远唆使阎婆告官，朱仝又一次放走了宋江。",
                    "「及时雨」是说宋江扶危济困，像及时雨一样帮人。他因此名满江湖，好汉们一听名号就纳头便拜。"),
                C("景阳冈", "第二十三回 横海郡柴进留宾 景阳冈武松打虎", "武松篇", "zigzag",
                    J(HeroId.WuSong), J(HeroId.ChaiJin, HeroId.SongJiang),
                    "大虫", "景阳冈", B(BossSkill.Charge), HeroId.WuSong, "猎户",
                    "武松在柴进庄上结识宋江，回乡探兄路过景阳冈。酒店挂着「三碗不过冈」，他偏喝了十八碗，提着哨棒上了冈……",
                    "武松打虎用的是双拳——哨棒一下打在枯树上折了，他索性揪住虎头一顿乱打。"),
                C("斗杀西门庆", "第二十六回 供人头武二郎设祭", "武松篇", "jiangzhou",
                    J(HeroId.WuDaLang), J(HeroId.WuSong, HeroId.LeiHeng, HeroId.SongQing),
                    "西门庆", "阳谷县财主", B(BossSkill.Resist), HeroId.WuSong, "王婆",
                    "武松做了阳谷县都头，出差回来，哥哥武大郎已被潘金莲与西门庆毒死。他到狮子楼找西门庆，要替哥哥讨个公道。",
                    "武大郎不在一百单八将之内，是本作的隐藏好汉：凑出他的名字，就有热乎的炊饼。"),
                C("十字坡", "第二十七回 母夜叉孟州道卖人肉 武都头十字坡遇张青", "武松篇", "qingfeng",
                    J(HeroId.SunErNiang, HeroId.ZhangQingY), J(HeroId.WuSong),
                    "孙二娘", "母夜叉", B(BossSkill.Daze), HeroId.WuSong, "公人",
                    "武松刺配孟州，路过十字坡酒店。老板娘笑脸相迎，端上的酒却有股怪味——武松装作晕倒，看她怎么下手。",
                    "张青告诫孙二娘：三等人不可坏——云游僧道、江湖上卖艺的、流配的犯人。"),
                C("醉打蒋门神", "第二十九回 施恩重霸孟州道 武松醉打蒋门神", "武松篇", "ring",
                    J(HeroId.ShiEn), J(HeroId.WuSong, HeroId.ZhangQingY, HeroId.SunErNiang),
                    "蒋门神", "快活林恶霸", B(BossSkill.Steal), HeroId.WuSong, "张团练",
                    "施恩的快活林酒店被蒋门神强占。武松一路「无三不过望」，每过一家酒店喝三碗，醉醺醺地来到快活林。",
                    "武松醉打蒋门神用的是「玉环步、鸳鸯脚」，是他平生得意的本事。"),
                C("血溅鸳鸯楼", "第三十一回 张都监血溅鸳鸯楼 武行者夜走蜈蚣岭", "武松篇", "jiuqu",
                    J(HeroId.KongMing, HeroId.KongLiang), J(HeroId.WuSong, HeroId.ShiEn),
                    "张都监", "孟州兵马都监", B(BossSkill.Summon, BossSkill.Seal), HeroId.WuSong, "张团练",
                    "张都监设计陷害武松，又派人在飞云浦害他。武松杀回孟州城，血溅鸳鸯楼，扮作行者连夜出逃，在孔家庄醉打孔亮。",
                    "血溅鸳鸯楼后，武松蘸血在白粉墙上写下八个大字：「杀人者，打虎武松也。」"),
                C("夜走蜈蚣岭", "第三十二回 武行者醉打孔亮 锦毛虎义释宋江", "武松篇", "pass",
                    J(), J(HeroId.WuSong, HeroId.SunErNiang, HeroId.ZhangQingY),
                    "王道人", "飞天蜈蚣", B(BossSkill.Charge, BossSkill.Resist), HeroId.WuSong, "道童",
                    "十字坡孙二娘替武松戴上铁戒箍、换上行者衣裳，赠了一对雪花镔铁戒刀。月黑风高夜过蜈蚣岭，庵中恶道飞天蜈蚣正强掳民女——正好拿他试刀！",
                    "武松这双雪花镔铁戒刀，传说是用头陀留下的好铁打成，夜里在刀鞘中会铮铮作响，从此伴他走完半生。"),
                C("清风寨", "第三十三回 宋江夜看小鳌山 花荣大闹清风寨", "清风山篇", "river",
                    J(HeroId.HuaRong, HeroId.YanShun, HeroId.WangYing, HeroId.ZhengTianShou), J(),
                    "刘高", "清风寨文知寨", B(BossSkill.Summon, BossSkill.Daze), HeroId.HuaRong, "黄信",
                    "宋江夜看花灯，被清风寨知寨刘高认出，五花大绑押进寨中。花荣连夜点兵：救哥哥！",
                    "宋江在清风山差点被燕顺挖心做醒酒汤，一句「可惜宋江死在这里」才救了自己一命。"),
                C("瓦砾场", "第三十四回 镇三山大闹青州道 霹雳火夜走瓦砾场", "清风山篇", "plain",
                    J(HeroId.QinMing, HeroId.HuangXin), J(HeroId.HuaRong, HeroId.YanShun, HeroId.WangYing),
                    "秦明", "霹雳火", B(BossSkill.Charge, BossSkill.Haste), HeroId.HuaRong, "慕容知府",
                    "青州兵马总管霹雳火秦明奉命来剿清风山，一条狼牙棒杀得天昏地暗。花荣说：要擒此人，得用计。",
                    "宋江派人扮作秦明去青州城下杀人放火，慕容知府杀了秦明全家，秦明从此死心塌地上了山。"),
                C("对影山", "第三十五回 石将军村店寄书 小李广梁山射雁", "清风山篇", "zigzag",
                    J(HeroId.LvFang, HeroId.GuoSheng, HeroId.ShiYong), J(HeroId.HuaRong, HeroId.QinMing),
                    "郭盛", "赛仁贵", B(BossSkill.Resist, BossSkill.Crush), HeroId.HuaRong, "吕方",
                    "对影山上，小温侯吕方与赛仁贵郭盛争山头，画戟对画戟斗了几十天。两枝戟上的绒绦缠在了一起……",
                    "花荣一箭射断缠在一起的绒绦，两人心服口服；上梁山后他又一箭射中雁行第三只。"),
                C("揭阳岭", "第三十六回 梁山泊吴用举戴宗 揭阳岭宋江逢李俊", "江州篇", "jiuqu",
                    J(HeroId.LiLi, HeroId.LiJun, HeroId.TongWei, HeroId.TongMeng), J(HeroId.ShiYong),
                    "李立", "催命判官", B(BossSkill.Daze, BossSkill.Devour), HeroId.LiJun, "穆春",
                    "宋江刺配江州，在揭阳岭酒店喝了一碗酒就人事不省，被拖进作坊要开剥——混江龙李俊急匆匆赶到。",
                    "揭阳三霸：岭上李俊、李立，镇上穆弘、穆春，江上张横、张顺。"),
                C("浔阳江", "第三十七回 没遮拦追赶及时雨 船火儿夜闹浔阳江", "江州篇", "bailong",
                    J(HeroId.MuHong, HeroId.MuChun, HeroId.XueYong, HeroId.ZhangHeng), J(HeroId.LiJun),
                    "张横", "船火儿", B(BossSkill.Fog, BossSkill.Resist), HeroId.LiJun, "穆太公",
                    "宋江赏了卖艺的薛永，得罪了揭阳镇的穆氏兄弟，被追到江边，慌忙跳上一条船——船家张横问：吃板刀面，还是吃馄饨？",
                    "「板刀面」是一刀砍下水，「馄饨」是自己脱光跳下去。宋江报出名号，张横纳头便拜。"),
                C("浔阳楼", "第三十八回 及时雨会神行太保 黑旋风斗浪里白条", "江州篇", "marsh",
                    J(HeroId.DaiZong, HeroId.LiKui, HeroId.ZhangShun), J(HeroId.ZhangHeng, HeroId.LiJun),
                    "张顺", "浪里白条", B(BossSkill.Haste, BossSkill.Rain), HeroId.ZhangHeng, "戴宗",
                    "李逵在江边抢鱼，和渔牙子张顺打了起来。岸上张顺打不过，一把把李逵拖下水——这下轮到铁牛喝水了。",
                    "黑旋风斗浪里白条，是书中最有名的「不打不相识」：两人后来成了生死兄弟。"),
                C("江州法场", "第四十回 梁山泊好汉劫法场 白龙庙英雄小聚义", "江州篇", "jiangzhou",
                    J(HeroId.SongJiang, HeroId.XiaoRang, HeroId.JinDaJian), J(HeroId.LiKui, HeroId.DaiZong, HeroId.HuaRong),
                    "蔡九", "江州知府", B(BossSkill.Seal, BossSkill.Summon), HeroId.LiKui, "晁盖",
                    "浔阳楼上一首反诗，宋江被判斩首。萧让、金大坚仿造的蔡京书信露了破绽。午时三刻，十字路口，刽子手已经举刀——",
                    "宋江的反诗：「他时若遂凌云志，敢笑黄巢不丈夫。」黄巢是唐末造反的首领。"),
                C("白龙庙", "第四十一回 宋江智取无为军 张顺活捉黄文炳", "江州篇", "bailong",
                    J(HeroId.HouJian), J(HeroId.ZhangShun, HeroId.ZhangHeng, HeroId.RuanXiaoEr, HeroId.RuanXiaoQi, HeroId.LiKui),
                    "黄文炳", "无为军通判", B(BossSkill.Resist, BossSkill.Fog), HeroId.ZhangShun, "李俊",
                    "劫了法场，众好汉护着宋江退到江边白龙庙。告发反诗的黄文炳就在对岸无为军，裁缝侯健说：他家的路，我熟。",
                    "白龙庙小聚义之后，宋江正式上了梁山，坐了第二把交椅。"),
                C("黄门山", "第四十一回 回山途中", "江州篇", "zigzag",
                    J(HeroId.OuPeng, HeroId.JiangJing, HeroId.MaLin, HeroId.TaoZongWang), J(HeroId.SongJiang, HeroId.HuaRong),
                    "欧鹏", "摩云金翅", B(BossSkill.Charge, BossSkill.Haste), HeroId.HuaRong, "蒋敬",
                    "回梁山的路上，黄门山四个头领拦住去路。一问来的是及时雨宋公明，四人当即下山相迎。",
                    "蒋敬是梁山的「账房」，书中说他「精通书算，积万累千，纤毫不差」。"),
                C("玄女授天书", "第四十二回 还道村受三卷天书 宋公明遇九天玄女", "宋江篇", "bagua",
                    J(), J(HeroId.SongJiang, HeroId.HuaRong, HeroId.LiKui, HeroId.DaiZong),
                    "赵能", "郓城都头", B(BossSkill.Seal, BossSkill.Fog), HeroId.SongJiang, "赵得",
                    "宋江回郓城接老父，被都头赵能、赵得连夜追捕，慌不择路躲进还道村古庙神厨。官兵搜到神帐前，一阵狂风飞沙走石，九天玄女娘娘显圣相召……",
                    "九天玄女赐宋江三枣三杯仙酒，授以三卷无字天书，嘱他「替天行道，辅国安民」——这正是山寨道具「天书」的来历。"),
                C("沂岭杀四虎", "第四十三回 假李逵剪径劫单人 黑旋风沂岭杀四虎", "李逵篇", "ring",
                    J(HeroId.ZhuFu, HeroId.LiYun), J(HeroId.LiKui, HeroId.ZhuGui),
                    "大虫", "沂岭", B(BossSkill.Devour, BossSkill.Charge), HeroId.LiKui, "李鬼",
                    "李逵回家接老娘上山享福，背到沂岭，老娘口渴，他去打水回来，只见一摊血……",
                    "李逵怒杀四虎后被捉，朱富用蒙汗药酒麻翻了自己的师父李云，救出李逵，师徒同上梁山。"),
                C("饮马川", "第四十四回 锦豹子小径逢戴宗 病关索长街遇石秀", "祝家庄篇", "snake",
                    J(HeroId.YangLin, HeroId.DengFei, HeroId.MengKang, HeroId.PeiXuan), J(HeroId.DaiZong),
                    "邓飞", "火眼狻猊", B(BossSkill.Daze, BossSkill.Haste), HeroId.YangLin, "裴宣",
                    "戴宗下山打探公孙胜，路遇锦豹子杨林，一同走到饮马川，山上冲下一伙强人——为首的火眼狻猊邓飞，正是杨林的旧识。",
                    "饮马川原是邓飞、孟康的山寨，铁面孔目裴宣来了之后被推为寨主。"),
                C("翠屏山", "第四十六回 病关索大闹翠屏山 拼命三火烧祝家店", "祝家庄篇", "river",
                    J(HeroId.YangXiong, HeroId.ShiXiu, HeroId.ShiQian), J(HeroId.YangLin),
                    "裴如海", "报恩寺僧", B(BossSkill.Revive, BossSkill.Fog), HeroId.ShiXiu, "潘巧云",
                    "拼命三郎石秀看破了嫂嫂与和尚裴如海私通，杨雄翠屏山上清理门户，带着时迁投奔梁山，路上住进了祝家店……",
                    "时迁在祝家店偷了报晓的公鸡下酒，引出了三打祝家庄。"),
                C("一打祝家庄", "第四十七回 扑天雕双修生死书 宋公明一打祝家庄", "祝家庄篇", "jiuqu",
                    J(HeroId.LiYing, HeroId.DuXing), J(HeroId.YangXiong, HeroId.ShiXiu, HeroId.ShiQian),
                    "祝龙", "祝家庄大郎", B(BossSkill.Fog, BossSkill.Summon), HeroId.LiYing, "祝朝奉",
                    "时迁被祝家庄捉了。李家庄李应写信求情，反被祝彪一箭射伤。宋江起兵来打，却在盘陀路里转得晕头转向。",
                    "祝家庄的盘陀路，要见白杨树才转弯，否则怎么走都走不出去——梁山第一次就吃了这个亏。"),
                C("二打祝家庄", "第四十八回 一丈青单捉王矮虎 宋公明两打祝家庄", "祝家庄篇", "ring",
                    J(HeroId.HuSanNiang), J(HeroId.LinChong, HeroId.WangYing, HeroId.LiYing),
                    "扈三娘", "扈家庄", B(BossSkill.Daze, BossSkill.Charge), HeroId.LinChong, "扈成",
                    "扈家庄的一丈青扈三娘使日月双刀，一阵就活捉了矮脚虎王英。林冲拍马迎上……",
                    "梁山三位女将：一丈青扈三娘、母夜叉孙二娘、母大虫顾大嫂。"),
                C("登州劫牢", "第四十九回 解珍解宝双越狱 孙立孙新大劫牢", "祝家庄篇", "plain",
                    J(HeroId.XieZhen, HeroId.XieBao, HeroId.GuDaSao, HeroId.SunXin), J(),
                    "毛太公", "登州恶霸", B(BossSkill.Steal, BossSkill.Summon), HeroId.XieZhen, "毛仲义",
                    "猎户解珍、解宝射死的老虎滚进了毛太公家后园，毛太公不但不还，还诬他们是贼关进死牢。表姐顾大嫂拍案：劫牢！",
                    "顾大嫂一句「你们不去，我便自去」，逼得做提辖的孙立也一起劫了登州大牢。"),
                C("三打祝家庄", "第五十回 吴学究双掌连环计 宋公明三打祝家庄", "祝家庄篇", "jiuqu",
                    J(HeroId.SunLi, HeroId.YueHe, HeroId.ZouYuan, HeroId.ZouRun), J(HeroId.XieBao),
                    "栾廷玉", "祝家庄教师", B(BossSkill.Resist, BossSkill.Crush), HeroId.SunLi, "祝彪",
                    "孙立是祝家庄教师栾廷玉的同门师兄弟。他打着官府的旗号「来助战」，开进了祝家庄……",
                    "孙立里应外合，三打祝家庄终于成功。这是梁山第一次打下有城墙的庄子。"),
                C("高唐州", "第五十四回 入云龙斗法破高廉 黑旋风探穴救柴进", "高唐州篇", "zigzag",
                    J(HeroId.TangLong), J(HeroId.GongSunSheng, HeroId.LiKui, HeroId.ChaiJin),
                    "高廉", "高唐州知府", B(BossSkill.Rain, BossSkill.Summon), HeroId.GongSunSheng, "殷天锡",
                    "柴进被高俅的堂弟高廉关进死牢。高廉会妖法，梁山连败两阵。李逵、戴宗去蓟州请回公孙胜，路上又结识了铁匠汤隆。",
                    "公孙胜的师父罗真人曾把李逵丢到半空，摔进蓟州府牢里，吓得铁牛再不敢胡来。"),
                C("五雷破兽阵", "第五十四回 入云龙斗法破高廉 黑旋风探穴救柴进", "高唐州篇", "bagua",
                    J(), J(HeroId.GongSunSheng, HeroId.LinChong, HeroId.HuaRong, HeroId.LiKui),
                    "神兵兽", "高廉飞天神兵", B(BossSkill.Fog, BossSkill.Charge), HeroId.GongSunSheng, "高廉",
                    "高廉披发仗剑，敲响聚兽铜牌，狂风黑雾中窜出无数豺狼虎豹直扑宋军！公孙胜立于阵前，掣出松纹古定剑指天喝道：疾！",
                    "公孙胜使出「五雷天罡正法」，半空霹雳震碎妖雾，高廉的三百飞天神兵瞬间现出纸人草马原形。"),
                C("轰天炮震泽", "第五十五回 高太尉大兴三路兵 呼延灼摆布连环马", "连环马篇", "ferry",
                    J(), J(HeroId.LiJun, HeroId.ZhangShun, HeroId.RuanXiaoEr, HeroId.RuanXiaoQi),
                    "凌振", "甲仗库副使", B(BossSkill.Crush, BossSkill.Rain), HeroId.ZhangShun, "韩滔",
                    "东京第一炮手轰天雷凌振在鸭嘴滩架起风火炮、金轮炮、子母炮，隔水轰击水泊鸭嘴滩。吴用微微笑道：岸上炮凶，引他下水便成哑炮！",
                    "阮氏兄弟与张顺凿翻炮船，水底生擒凌振。宋江亲解其缚，梁山自此有了威震州府的火炮营。"),
                C("连环马", "第五十七回 徐宁教使钩镰枪 宋江大破连环马", "连环马篇", "plain",
                    J(HeroId.XuNing, HeroId.HanTao, HeroId.PengQi, HeroId.LingZhen), J(HeroId.TangLong, HeroId.ShiQian),
                    "呼延灼", "双鞭", B(BossSkill.Charge, BossSkill.Haste), HeroId.XuNing, "高俅",
                    "朝廷派呼延灼带连环马来剿梁山，三十匹马用铁环连成一排，冲到哪里哪里倒。汤隆说：只有我表哥徐宁的钩镰枪能破！",
                    "时迁连夜盗走徐宁家传的雁翎锁子甲，把他赚上梁山。钩镰枪专钩马腿，连环马便破了。"),
                C("三山聚义", "第五十八回 三山聚义打青州 众虎同心归水泊", "连环马篇", "ring",
                    J(HeroId.HuYanZhuo), J(HeroId.LuZhiShen, HeroId.YangZhi, HeroId.WuSong, HeroId.KongMing, HeroId.QinMing),
                    "慕容知府", "青州知府", B(BossSkill.Seal, BossSkill.Summon), HeroId.QinMing, "桃花山",
                    "呼延灼兵败投奔青州，替慕容知府去打二龙山、桃花山、白虎山。三山好汉合兵一处，请宋江来破青州。",
                    "三山聚义：二龙山、桃花山、白虎山同归梁山，梁山的实力从此大增。"),
                C("芒砀山", "第六十回 公孙胜芒砀山降魔", "连环马篇", "ring",
                    J(HeroId.FanRui, HeroId.XiangChong, HeroId.LiGun), J(HeroId.GongSunSheng),
                    "樊瑞", "混世魔王", B(BossSkill.Rain, BossSkill.Fog), HeroId.GongSunSheng, "项充",
                    "芒砀山混世魔王樊瑞扬言要吞并梁山。他会呼风唤雨，手下项充、李衮团牌飞刀密不透风。",
                    "樊瑞被公孙胜破了妖法，心悦诚服，拜公孙胜为师。"),
                C("盗御马起衅", "第六十回 公孙胜芒砀山降魔 晁天王曾头市中箭", "曾头市篇", "pass",
                    J(), J(HeroId.YangLin, HeroId.ShiXiu, HeroId.LinChong, HeroId.DaiZong),
                    "曾索", "曾家五虎", B(BossSkill.Steal, BossSkill.Haste), HeroId.LinChong, "曾升",
                    "金毛犬段景住从塞北盗来日行千里的「照夜玉狮子马」献与梁山，路经曾头市却被曾家五虎强行夺去，还张榜叫嚣要荡平水泊！",
                    "照夜玉狮子马通体雪白无一根杂毛，这一桩夺马公案彻底激怒了晁盖，引出两打曾头市的大战。"),
                C("曾头市", "第六十回 晁天王曾头市中箭", "曾头市篇", "jiangzhou",
                    J(HeroId.DuanJingZhu), J(HeroId.LinChong, HeroId.HuYanZhuo, HeroId.HuaRong),
                    "史文恭", "曾头市教师", B(BossSkill.Resist, BossSkill.Daze), HeroId.HuaRong, "曾长官",
                    "段景住盗来的照夜玉狮子马被曾头市夺去，曾家还编歌辱骂梁山。晁盖大怒，亲自领兵去打曾头市……",
                    "晁盖中了史文恭的毒箭，临终遗言：「若哪个捉得射死我的，便叫他做梁山泊主。」"),
                C("智赚玉麒麟", "第六十一回 吴用智赚玉麒麟 张顺夜闹金沙渡", "大名府篇", "snake",
                    J(HeroId.LuJunYi, HeroId.YanQing), J(HeroId.WuYong, HeroId.LiKui, HeroId.ZhangShun),
                    "李固", "卢府管家", B(BossSkill.Steal, BossSkill.Seal), HeroId.YanQing, "贾氏",
                    "吴用扮作算命先生，李逵扮成哑道童，到大名府给卢俊义算了一卦：百日之内有血光之灾，须往东南千里外躲避……",
                    "吴用让卢俊义在墙上题的四句诗：「芦花丛里一扁舟，俊杰俄从此地游。义士若能知此理，反躬逃难可无忧。」藏头正是「卢俊义反」。"),
                C("石秀跳楼", "第六十二回 放冷箭燕青救主 劫法场石秀跳楼", "大名府篇", "qingfeng",
                    J(HeroId.CaiFu, HeroId.CaiQing), J(HeroId.ShiXiu, HeroId.LuJunYi, HeroId.YanQing),
                    "梁中书", "大名府留守", B(BossSkill.Summon, BossSkill.Seal), HeroId.ShiXiu, "李固",
                    "卢俊义被李固陷害，要在大名府问斩。刽子手蔡福、蔡庆受了柴进的银子，刀下留情；楼上，一个人纵身跳了下来——",
                    "石秀独自跳楼劫法场，大喊「梁山泊好汉全伙在此」，吓得官兵四散。"),
                C("关胜议取梁山", "第六十三回 宋江兵打北京城 关胜议取梁山泊", "大名府篇", "snake",
                    J(HeroId.GuanSheng, HeroId.XuanZan, HeroId.HaoSiWen), J(HeroId.HuYanZhuo, HeroId.LinChong),
                    "关胜", "大刀", B(BossSkill.Resist, BossSkill.Charge), HeroId.HuYanZhuo, "蔡京",
                    "梁山兵围大名府。宣赞举荐关云长的后人关胜，关胜献计围魏救赵，直扑梁山老巢。",
                    "呼延灼诈降，月夜把关胜赚进了梁山的埋伏，关胜感宋江义气，归顺梁山。"),
                C("雪天擒索超", "第六十四回 呼延灼月夜赚关胜 宋公明雪天擒索超", "大名府篇", "zigzag",
                    J(HeroId.SuoChao), J(HeroId.YangZhi, HeroId.GuanSheng),
                    "索超", "急先锋", B(BossSkill.Charge, BossSkill.Haste), HeroId.YangZhi, "闻达",
                    "大雪纷飞，大名府急先锋索超又冲出城来。吴用笑道：此人性急，雪地里挖个陷坑就够了。",
                    "索超当年在大名府校场与杨志大战五十回合不分胜负，梁中书把两人都升了官。"),
                C("张顺请神医", "第六十五回 托塔天王梦中显圣 浪里白条水上报冤", "大名府篇", "marsh",
                    J(HeroId.AnDaoQuan, HeroId.WangDingLiu), J(HeroId.ZhangShun),
                    "张旺", "截江鬼", B(BossSkill.Steal, BossSkill.Fog), HeroId.ZhangShun, "孙五",
                    "宋江背上生了恶疮。张顺星夜过江去建康请神医安道全，船到江心，梢公张旺抽出了刀……",
                    "张顺被绑住丢下江，在水底咬断绳索逃生；王定六收留了他，后来一同报了仇。"),
                C("水火二将", "第六十七回 宋江赏马步三军 关胜降水火二将", "大名府篇", "river",
                    J(HeroId.ShanTingGui, HeroId.WeiDingGuo, HeroId.BaoXu, HeroId.JiaoTing), J(HeroId.GuanSheng, HeroId.LiKui),
                    "魏定国", "神火将", B(BossSkill.Fog, BossSkill.Summon), HeroId.GuanSheng, "单廷珪",
                    "凌州的圣水将、神火将来攻梁山。关胜领兵迎敌；李逵偷偷下山，在枯树山收了鲍旭、焦挺。",
                    "单廷珪会用「水浸兵」，魏定国会用「火攻兵」，都被关胜一一说降。"),
                C("夜打曾头市", "第六十八回 宋公明夜打曾头市 卢俊义活捉史文恭", "曾头市篇", "plain",
                    J(HeroId.YuBaoSi), J(HeroId.LuJunYi, HeroId.YanQing, HeroId.ShiQian, HeroId.DuanJingZhu),
                    "史文恭", "曾头市教师", B(BossSkill.Resist, BossSkill.Daze), HeroId.LuJunYi, "曾涂",
                    "险道神郁保四又劫了段景住的马送到曾头市。宋江再打曾头市，为晁盖报仇。",
                    "卢俊义活捉史文恭为晁盖报了仇，按遗言本当做寨主，他却坚决推让给宋江。"),
                C("东平府", "第六十九回 东平府误陷九纹龙 宋公明义释双枪将", "东平东昌篇", "qingfeng",
                    J(HeroId.DongPing), J(HeroId.ShiJin, HeroId.HuSanNiang, HeroId.WangYing, HeroId.GuDaSao),
                    "董平", "双枪将", B(BossSkill.Haste, BossSkill.Charge), HeroId.HuSanNiang, "程太守",
                    "宋江、卢俊义拈阄分兵，谁先打下城池谁做寨主。宋江打东平府，史进潜入城中却被识破，双枪将董平杀出城来……",
                    "董平的旗上写着：「英雄双枪将，风流万户侯。」"),
                C("东昌府", "第七十回 没羽箭飞石打英雄 宋公明弃粮擒壮士", "东平东昌篇", "river",
                    J(HeroId.ZhangQing, HeroId.GongWang, HeroId.DingDeSun, HeroId.HuangFuDuan), J(HeroId.RuanXiaoQi, HeroId.LuJunYi),
                    "张清", "没羽箭", B(BossSkill.Daze, BossSkill.Crush), HeroId.RuanXiaoQi, "龚旺",
                    "东昌府守将没羽箭张清，一颗颗石子飞出，连伤梁山十五员大将。吴用说：岸上打不过，就引他到水边。",
                    "张清被擒后，宋江惜才不杀，又让他举荐了兽医皇甫端。至此一百单八将全部聚齐。"),

                C("忠义菊花会", "第七十一回 忠义堂石碣受天文 梁山泊英雄排座次", "排座次篇", "bagua",
                    J(), J(HeroId.SongJiang, HeroId.WuSong, HeroId.LiKui, HeroId.LuZhiShen, HeroId.WuYong),
                    "崔太尉", "招安天使", B(BossSkill.Seal, BossSkill.Steal), HeroId.LiKui, "陈宗善",
                    "重阳佳节，忠义堂前黄菊盛开，百八兄弟把酒言欢。宋江酒酣赋《满江红》流露招安之意，武松拍案叫绝，李逵一脚踢翻桌案；山下朝廷天使却带着假诏书到了……",
                    "阮小七偷把御赐好酒喝光，灌上两瓶酸苦村醪；李逵当堂扯碎诏书，第一次招安不欢而散。"),
                C("元夜闹东京", "第七十二回 柴进簪花入禁苑 李逵元夜闹东京", "排座次篇", "imperial",
                    J(), J(HeroId.LiKui, HeroId.YanQing, HeroId.ChaiJin, HeroId.ShiQian, HeroId.WuSong),
                    "杨太尉", "禁军统制", B(BossSkill.Summon, BossSkill.Haste), HeroId.YanQing, "高俅",
                    "正月十五元宵夜，东京汴梁火树银花。宋江带柴进、燕青微服入京赏灯，柴进簪花混入睿思殿撕下御书「山东宋江」四字，城门口李逵双斧早已按捺不住！",
                    "宋徽宗在屏风上亲笔写下四大寇姓名，柴进机敏剜走「山东宋江」四字，留下了宋代元宵鳌山灯会的千古奇景。"),
                C("智扑擎天柱", "第七十四回 燕青智扑擎天柱 李逵寿张乔坐衙", "排座次篇", "jiuqu",
                    J(), J(HeroId.YanQing, HeroId.LiKui, HeroId.LuJunYi, HeroId.SongJiang),
                    "任原", "擎天柱", B(BossSkill.Crush, BossSkill.Resist), HeroId.YanQing, "部署",
                    "忠义堂上排定座次之后，浪子燕青独自下山，要在泰安州的相扑擂台上，摔下两年无敌的擎天柱任原。",
                    "忠义堂前天降石碣，上刻三十六天罡、七十二地煞的星号与姓名，这就是「梁山泊英雄排座次」。"),
                C("两赢童贯", "第七十六回 吴加亮布四斗五方旗 宋公明排九宫八卦阵", "招安篇", "bagua",
                    J(), J(HeroId.QinMing, HeroId.GuanSheng, HeroId.LinChong, HeroId.HuaRong, HeroId.WuYong),
                    "童贯", "枢密使", B(BossSkill.Charge, BossSkill.Fog), HeroId.QinMing, "酆美",
                    "枢密使童贯统领十万禁军杀奔梁山。吴用摇动羽扇，按九宫八卦布开大阵，霹雳火秦明跃马当先，一棒砸碎官军先锋帅旗！",
                    "梁山军以九宫八卦阵与十面埋伏两败童贯，打得十万禁军丢盔弃甲，震动朝野。"),
                C("三败高俅", "第八十回 张顺凿漏海鳅船 宋江三败高太尉", "招安篇", "marsh",
                    J(), J(HeroId.ZhangShun, HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi, HeroId.LiJun, HeroId.LinChong),
                    "高俅", "殿帅太尉", B(BossSkill.Summon, BossSkill.Seal), HeroId.ZhangShun, "童贯",
                    "高俅亲率大军和海鳅大船来攻梁山。张顺带水军潜到船底，一凿一凿……",
                    "高俅落水被擒，林冲恨不得生吃了他，宋江却亲自松绑——一心只想招安。"),
                C("智拜李师师", "第八十一回 燕青月夜遇道君 戴宗定计出乐和", "招安篇", "imperial",
                    J(), J(HeroId.YanQing, HeroId.DaiZong, HeroId.ChaiJin, HeroId.SongJiang),
                    "杨戬", "殿前太尉", B(BossSkill.Seal, BossSkill.Daze), HeroId.YanQing, "高衙内",
                    "高俅回京扣下招安奏章。浪子燕青携黄金美玉夜访东京花魁李师师，吹箫唱曲深得青睐，终在烛影下见到微服出宫的宋徽宗，当面呈上梁山血书！",
                    "燕青凭一口清亮小曲与机敏谈吐打动李师师，更求得徽宗亲笔赦书，促成宿太尉上梁山宣诏招安。"),
                C("征辽", "第八十三回 宋公明奉诏破大辽", "征辽篇", "plain",
                    J(), J(HeroId.GuanSheng, HeroId.LinChong, HeroId.QinMing, HeroId.HuYanZhuo, HeroId.DongPing, HeroId.HuaRong),
                    "兀颜光", "辽国统军", B(BossSkill.Fog, BossSkill.Resist), HeroId.GuanSheng, "辽主",
                    "梁山全伙受了招安，改称宋军。第一仗就是北上征辽，辽国统军兀颜光摆下了太乙混天象阵。",
                    "五虎上将关胜、林冲、秦明、呼延灼、董平，征辽时各领一军，杀得辽兵望风而逃。"),
                C("破混天象阵", "第八十九回 宋公明破阵成功 宿太尉颁恩降诏", "征辽篇", "bagua",
                    J(), J(HeroId.GongSunSheng, HeroId.HuaRong, HeroId.LuJunYi, HeroId.HuSanNiang, HeroId.ZhangQing),
                    "贺重宝", "辽国副统军", B(BossSkill.Fog, BossSkill.Rain), HeroId.GongSunSheng, "兀颜光",
                    "辽军排出二十八宿与太乙混天象阵，黑云蔽日、妖气冲天。宋江夜梦九天玄女指点破阵秘诀，公孙胜仗剑登坛，花荣、张清弓石齐发直捣中军！",
                    "破太乙混天象阵是梁山一百单八将最齐整的巅峰决战，大破辽军直抵燕京城下，迫使辽主奉表投降。"),
                C("征田虎", "第九十一回起 征田虎", "征田虎篇", "zigzag",
                    J(), J(HeroId.GongSunSheng, HeroId.FanRui, HeroId.LuJunYi, HeroId.YanQing),
                    "乔道清", "幻魔君", B(BossSkill.Rain, BossSkill.Revive), HeroId.GongSunSheng, "田虎",
                    "河北田虎造反，他手下的幻魔君乔道清会妖法，宋军接连吃亏。公孙胜仗剑出阵。",
                    "征田虎、征王庆是一百二十回本才有的情节；这两路征战中，一百单八将无一阵亡。"),
                C("飞石结良缘", "第九十八回 张清缘配琼郡主 吴用计鸩邬梨", "征田虎篇", "pass",
                    J(), J(HeroId.ZhangQing, HeroId.AnDaoQuan, HeroId.SunXin, HeroId.LuJunYi),
                    "琼英", "田虎郡主", B(BossSkill.Daze, BossSkill.Haste), HeroId.ZhangQing, "邬梨",
                    "田虎麾下女将琼英梦中曾受神人传授飞石绝技，阵前飞石连挫宋军。没羽箭张清拍马出阵，两颗飞石在半空撞出火星——竟是一对梦中宿缘！",
                    "张清与神医安道全假扮全羽夫妻潜入敌营，与琼英结成连理，里应外合生擒田虎，留下水浒最浪漫的一段佳话。"),
                C("征王庆", "第一百零一回起 征王庆", "征王庆篇", "bailong",
                    J(), J(HeroId.LiJun, HeroId.ZhangHeng, HeroId.ZhangShun, HeroId.TongWei, HeroId.TongMeng),
                    "王庆", "淮西反王", B(BossSkill.Haste, BossSkill.Steal), HeroId.LiJun, "段三娘",
                    "淮西王庆兵败，乘船逃命。江面上，混江龙李俊早已等候多时。",
                    "王庆是被李俊在江上擒获的。梁山水军从此威名远扬。"),
                C("涌金门", "第一百一十四回 宁海军宋江吊孝 涌金门张顺归神", "征方腊篇", "marsh",
                    J(), J(HeroId.ZhangShun, HeroId.ZhangHeng, HeroId.RuanXiaoQi, HeroId.LiJun),
                    "方天定", "方腊太子", B(BossSkill.Resist, BossSkill.Summon), HeroId.ZhangHeng, "石宝",
                    "征方腊打到杭州，城高难攻。张顺说：我从西湖水底潜到涌金门，放火为号——",
                    "张顺在涌金门被乱箭射死，魂魄附在哥哥张横身上，亲手杀了方天定。"),
                C("血战昱岭关", "第一百一十八回 卢俊义大战昱岭关 宋公明智取清溪洞", "征方腊篇", "pass",
                    J(), J(HeroId.LuJunYi, HeroId.HuaRong, HeroId.ShiJin, HeroId.ShiXiu, HeroId.LinChong),
                    "庞万春", "小养由基", B(BossSkill.Crush, BossSkill.Daze), HeroId.HuaRong, "雷炯",
                    "昱岭关险峻异常，方腊神箭手小养由基庞万春埋伏关上，乱箭害了史进、石秀等六员兄弟！卢俊义率军强攻破关，小李广花荣含泪拈弓，誓报兄弟血仇！",
                    "昱岭关一役折损史进、石秀等六将，梁山健儿无不恸哭；卢俊义破关活捉庞万春，剖心沥血祭奠亡魂。"),
                C("乌龙岭", "第一百一十七回 睦州城箭射邓元觉 乌龙岭神助宋公明", "征方腊篇", "jiuqu",
                    J(), J(HeroId.HuaRong, HeroId.XieZhen, HeroId.XieBao, HeroId.SongJiang, HeroId.WuYong),
                    "邓元觉", "宝光国师", B(BossSkill.Crush, BossSkill.Revive), HeroId.HuaRong, "方腊",
                    "乌龙岭山高路险，解珍、解宝扮作猎户攀崖探路，再没回来。睦州城下，宝光国师邓元觉挥着禅杖杀出……",
                    "征方腊一路，梁山好汉死伤大半，是全书最悲壮的一段。"),
                C("单臂战石宝", "第一百一十九回 鲁智深浙江坐化 宋公明衣锦还乡", "征方腊篇", "ferry",
                    J(), J(HeroId.WuSong, HeroId.GuanSheng, HeroId.LuZhiShen, HeroId.YanQing),
                    "石宝", "南离大将军", B(BossSkill.Resist, BossSkill.Charge), HeroId.WuSong, "包道乙",
                    "南离大将军石宝使劈风刀与流星锤，连斩梁山多员战将；妖道包道乙暗算伤了武松左臂。武行者咬牙扯断血衣，独臂提戒刀再冲敌阵！",
                    "武松痛失一臂后看破红尘，班师路过杭州六和寺时不再回京受封，留在寺中出家，活到八十善终。"),
                C("擒方腊", "第一百一十八回 卢俊义大战昱岭关 宋公明智取清溪洞", "征方腊篇", "ring",
                    J(), J(HeroId.LuZhiShen, HeroId.WuSong, HeroId.LuJunYi, HeroId.SongJiang, HeroId.YanQing),
                    "方腊", "圣公", B(BossSkill.Summon, BossSkill.Daze), HeroId.LuZhiShen, "方杰",
                    "清溪洞破，方腊只身逃进深山，迎面撞上一个胖大和尚，一禅杖打翻在地。",
                    "鲁智深生擒方腊，应了智真长老当年的偈语「逢夏而擒，遇腊而执」。后来他在六和寺听潮信圆寂。"),
                C("蓼儿洼", "第一百二十回 宋公明神聚蓼儿洼 徽宗帝梦游梁山泊", "终章", "marsh",
                    J(), J(HeroId.SongJiang, HeroId.LiKui, HeroId.WuYong, HeroId.HuaRong, HeroId.LuJunYi),
                    "蔡京", "太师", B(BossSkill.Seal, BossSkill.Fog), HeroId.LiKui, "高俅",
                    "征方腊归来，奸臣蔡京、高俅、童贯、杨戬容不下梁山好汉，一杯御赐药酒送到了楚州。替天行道的大旗，最后一次升起。",
                    "宋江饮了毒酒，怕李逵再反坏了梁山的忠义之名，叫来李逵同饮，一起葬在楚州蓼儿洼。吴用、花荣也在墓前自缢。"),
            };
            int n = list.Length;
            for (int i = 0; i < n; i++)
            {
                list[i].Index = i;
                float k = Math.Min(1f, i / 50f);
                list[i].RivalSkill = i == 0 ? 0.42f : i == 1 ? 0.50f : 0.55f + 0.45f * Math.Min(1f, i / 12f);
                list[i].RivalIncome = i == 0 ? 0.78f : Math.Min(0.95f + 0.08f * k, 0.82f + 0.03f * i);
                list[i].RivalFoeHp = 1f;
                list[i].FoeHp = 1f + 0.16f * k;
            }
            // 前两回做成紧凑速战（第1回5波约80秒、第2回6波约95秒），击退头目即胜，避免新手首局被拖进三四分钟拉锯。
            list[0].MaxWaves = 5;
            list[1].MaxWaves = 6;
            return list;
        }

        /// <summary>旧版 57 回主线逐回对应的新 72 回序号（供线上老玩家存档无损迁移）。</summary>
        public static readonly int[] Legacy57To72 =
        {
            0, 1, 2,
            5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
            21, 22, 23, 24, 25, 26, 27, 28, 29,
            31, 32, 33, 34, 35, 36, 37, 38,
            41, 42, 43,
            45, 46, 47, 48, 49, 50, 51, 52, 53, 54,
            57,
            59,
            61,
            63,
            65, 66,
            68,
            70, 71,
        };

        /// <summary>把旧版 57 回的 unlocked 进度无损映射到新 72 回：老玩家正在推的那一回不变，打穿 57 回的直接对齐到 72。</summary>
        public static int MigrateUnlocked57To72(int oldUnlocked)
        {
            if (oldUnlocked <= 0) return 0;
            if (oldUnlocked >= Legacy57To72.Length) return All.Length;
            return Legacy57To72[oldUnlocked];
        }

        /// <summary>某好汉在哪一回入伙；-1 表示剧情里不入伙。</summary>
        public static int JoinChapter(HeroId id)
        {
            foreach (var ch in All)
                if (Array.IndexOf(ch.Joins, id) >= 0) return ch.Index;
            return -1;
        }

        /// <summary>第一次出场的回。</summary>
        public static int FirstSeen(HeroId id)
        {
            foreach (var ch in All)
                if (Array.IndexOf(ch.Pool, id) >= 0) return ch.Index;
            return -1;
        }

        // ─────────────────────────── 聚义厅 ───────────────────────────

        public const int TeamSize = 5;

        /// <summary>
        /// 剧情打完后的车轮战：阵容由玩家自选（或随机）五员，名字里的字只从这五人里出，
        /// 同屏金字不至于太杂；对手和头目每阵变强。
        /// </summary>
        public static ChapterDef Endless(int round, IList<HeroId> team, uint seed)
        {
            var rng = new Rng(seed * 7919u + (uint)round * 104729u + 1u);
            var pool = new List<HeroId>();
            foreach (var id in team) if (!pool.Contains(id) && !HeroDef.Get(id).Hidden) pool.Add(id);
            if (pool.Count == 0) pool.Add(HeroId.SongJiang);

            var fighters = new List<HeroId>();
            foreach (var id in pool) if (!HeroDef.Get(id).NoAttack) fighters.Add(id);
            if (fighters.Count == 0) { pool.Add(HeroId.LinChong); fighters.Add(HeroId.LinChong); }
            var src = All[rng.Range(0, All.Length)].Boss;
            var nemesis = fighters[rng.Range(0, fighters.Count)];
            int kinds = Enum.GetValues(typeof(BossSkill)).Length;
            var s1 = (BossSkill)rng.Range(0, kinds);
            var s2 = (BossSkill)(((int)s1 + 1 + rng.Range(0, kinds - 1)) % kinds);
            var skills = new[] { s1, s2 };
            var rivalPick = HeroDef.All[rng.Range(16, HeroDef.All.Length)];

            float k = Math.Min(1f, (round - 1) / 20f);
            return new ChapterDef
            {
                Index = All.Length,
                Round = round,
                Name = "聚义厅·第" + round + "阵",
                Hui = "替天行道",
                Act = "聚义厅",
                Story = $"一百单八将聚齐，各路英雄来梁山讨教。这一阵来的是{src.Title}{src.Name}，看你点的五员大将能撑几阵！",
                Lore = "每阵结束都可以换将。阵容里有渊源的好汉同时上场，会触发「羁绊」，伤害更高。",
                Map = Maps[EndlessMaps[rng.Range(0, EndlessMaps.Length)]],
                Pool = pool.ToArray(),
                Boss = new BossDef { Name = src.Name, Title = src.Title, Skills = skills, Nemesis = nemesis, SkillText = BossDef.Describe(skills, nemesis) },
                RivalName = rivalPick.Nick + rivalPick.Name,
                RivalSkill = 1f,
                RivalIncome = 1.15f + 0.15f * k,
                FoeHp = 1.15f + 0.2f * k,
            };
        }

        // ─────────────────────────── 每日一战（附梁山军司轮换） ───────────────────────────

        /// <summary>每日一战用的回目序号。不是剧情回，也不是聚义厅。</summary>
        public const int DailySlot = 1000;
        /// <summary>好汉列传用的槽位起点：2000..2035 对应 36 折列传。</summary>
        public const int BioSlotBase = 2000;
        /// <summary>水泊名役用的槽位起点：3000..3011 对应 12 幕名役。</summary>
        public const int CampaignSlotBase = 3000;
        /// <summary>「一横两纵」百二十回立体内容总体量（72 主线 + 36 列传 + 12 名役 = 120）。</summary>
        public static int GrandTotalCount => StoryCount + Bios.Length + Campaigns.Length;

        static readonly CorpsKind[] DailyCorpsRotation =
        {
            CorpsKind.Cavalry, CorpsKind.Infantry, CorpsKind.Navy, CorpsKind.Mystic,
        };

        /// <summary>
        /// 每日一战：同一天所有人同一张图、同一个头目、同一池好汉、同一个种子。
        /// 难度固定在前中期，第五回之后的玩家都能打；头目多一门当天抽的绝技，并每日轮换当值梁山军司（对应军司好汉伤害+25%）。
        /// </summary>
        public static ChapterDef Daily(int day)
        {
            var rng = new Rng((uint)day * 2246822519u + 3266489917u);
            var corps = DailyCorpsRotation[Math.Abs(day) % DailyCorpsRotation.Length];
            var src = All[rng.Range(0, All.Length)];
            var pool = new List<HeroId>(src.Pool);
            // 确保当天点名的军司至少有一位能打的好汉在池子里，让玩家每天都有新鲜主角能爽
            bool hasCorps = false;
            foreach (var id in pool)
                if (InCorps(HeroDef.Get(id), corps) && !HeroDef.Get(id).NoAttack) { hasCorps = true; break; }
            if (!hasCorps && pool.Count < 6)
            {
                HeroId pick = corps switch
                {
                    CorpsKind.Cavalry => HeroId.LinChong,
                    CorpsKind.Infantry => HeroId.WuSong,
                    CorpsKind.Navy => HeroId.ZhangShun,
                    _ => HeroId.GongSunSheng,
                };
                if (!pool.Contains(pick)) pool.Add(pick);
            }
            var skills = new List<BossSkill>(src.Boss.Skills);
            int kinds = Enum.GetValues(typeof(BossSkill)).Length;
            if (skills.Count < 2)
            {
                var extra = (BossSkill)rng.Range(0, kinds);
                if (!skills.Contains(extra)) skills.Add(extra);
            }
            var c = (ChapterDef)src.MemberwiseClone();
            c.IsDaily = true;
            c.MaxWaves = 0;
            c.Seed = (uint)day;
            c.Rule = BattleRule.CorpsFocus;
            c.Corps = corps;
            c.Pool = pool.ToArray();
            c.Name = "每日一战·" + src.Name;
            c.Act = "每日一战";
            c.Map = Maps[EndlessMaps[rng.Range(0, EndlessMaps.Length)]];
            c.Boss = new BossDef { Name = src.Boss.Name, Title = src.Boss.Title, Skills = skills.ToArray(), Nemesis = src.Boss.Nemesis, SkillText = BossDef.Describe(skills.ToArray(), src.Boss.Nemesis) };
            c.Joins = new HeroId[0];
            c.RivalSkill = 0.72f;
            c.RivalIncome = 1f;
            c.FoeHp = All[Math.Min(12, All.Length - 1)].FoeHp;
            return c;
        }

        // ─────────────────────────── 纵线一：好汉列传（36 折 · 12 位主角 × 3 折小池专场） ───────────────────────────

        public sealed class HeroEpitaph
        {
            public readonly HeroId Hero;
            public readonly string Title;
            public readonly string Poem;
            public HeroEpitaph(HeroId h, string title, string poem) { Hero = h; Title = title; Poem = poem; }
        }

        public static readonly HeroEpitaph[] BioHeroes =
        {
            new HeroEpitaph(HeroId.LuZhiShen, "禅心侠骨·花和尚传", "平生不修善果，只爱杀人放火。忽地顿开金绳，这里扯断玉锁。钱塘江上潮信来，今日方知我是我。"),
            new HeroEpitaph(HeroId.LinChong, "风雪孤枪·豹子头传", "仗义是林冲，为人最朴忠。江湖驰誉望，京国显英雄。身世悲浮梗，功名类转蓬。"),
            new HeroEpitaph(HeroId.WuSong, "行者无疆·天伤星传", "景阳冈上千钟酒，鸳鸯楼头三尺刀。独臂犹擒溪洞寇，六和听涛伴青灯。"),
            new HeroEpitaph(HeroId.YangZhi, "天波遗烈·青面兽传", "祖传宝刀耀汴梁，半生落魄走他乡。二龙宝珠风云聚，不负天波万里长。"),
            new HeroEpitaph(HeroId.SongJiang, "及时甘霖·呼保义传", "刀笔小吏重义名，散尽千金结豪英。替天行道杏黄展，聚散风云百八星。"),
            new HeroEpitaph(HeroId.HuaRong, "惊鸿神箭·小李广传", "银盔白马雕弓满，射断红绦落雁行。生死相随蓼儿洼，千秋忠义共斜阳。"),
            new HeroEpitaph(HeroId.LiKui, "赤子雷霆·黑旋风传", "手提双斧走风雷，快意恩仇任性为。一片痴心唯敬兄，生同聚义死同归。"),
            new HeroEpitaph(HeroId.ZhangShun, "西湖水魂·浪里白条传", "雪练肌肤水底眠，凿穿巨舰浪滔天。涌金门外忠魂在，化作钱塘万里烟。"),
            new HeroEpitaph(HeroId.GongSunSheng, "太乙闲云·入云龙传", "松纹古剑破妖氛，五雷正法动乾坤。功成不受朝廷禄，笑指二仙山外云。"),
            new HeroEpitaph(HeroId.HuSanNiang, "红锦双刀·一丈青传", "玉雪肌肤锦绣装，双刀骏马冠群芳。红绵套索擒骁将，巾帼威名满大荒。"),
            new HeroEpitaph(HeroId.LuJunYi, "河北绝艺·玉麒麟传", "目炯双瞳美丰仪，杆棒无双世所稀。活捉文恭雪前恨，忠魂长伴淮水西。"),
            new HeroEpitaph(HeroId.YanQing, "风流百巧·浪子传", "唇若涂朱鬓插花，吹箫相扑俱名家。急流勇退秋风里，一叶扁舟天际涯。"),
        };

        public static readonly ChapterDef[] Bios = BuildBios();

        static ChapterDef Bio(int idx, HeroId hero, int actNo, string name, string hui, string map, HeroId partner,
            string boss, string bTitle, BossSkill[] skills, string rival, string story)
        {
            var hDef = HeroDef.Get(hero);
            var pool = partner == hero ? new[] { hero } : new[] { hero, partner };
            if (HeroDef.Get(hero).NoAttack) pool = new[] { hero, partner, HeroId.LiKui };
            else if (HeroDef.Get(partner).NoAttack) pool = new[] { hero, partner, HeroId.ShiJin };
            int unlockAt = Math.Max(0, JoinChapter(hero));
            int tierRef = Math.Min(All.Length - 1, unlockAt + (actNo - 1) * 8);
            var nem = HeroDef.Get(hero).NoAttack ? pool[pool.Length - 1] : hero;
            return new ChapterDef
            {
                Index = tierRef,
                BioIndex = idx,
                Name = name,
                Hui = $"{hDef.Name}列传·第{actNo}折 · {hui}",
                Act = hDef.Name + "列传",
                Map = Maps[map],
                Pool = pool,
                Joins = new HeroId[0],
                Boss = new BossDef { Name = boss, Title = bTitle, Skills = skills, Nemesis = nem, SkillText = BossDef.Describe(skills, nem) },
                RivalName = rival,
                Story = story,
                Lore = BioHeroes[idx / 3].Poem,
                RivalSkill = Math.Min(0.95f, 0.56f + actNo * 0.11f + unlockAt * 0.004f),
                RivalIncome = 0.90f + actNo * 0.06f,
                FoeHp = 1f + 0.05f * (actNo - 1) + 0.12f * (tierRef / 60f),
            };
        }

        static ChapterDef[] BuildBios()
        {
            var list = new ChapterDef[36];
            int k = 0;
            // 01 鲁智深（3折）
            list[k] = Bio(k++, HeroId.LuZhiShen, 1, "状元桥三拳", "拳打镇关西", "qingfeng", HeroId.ShiJin, "郑屠", "镇关西", B(BossSkill.Haste), "金老汉", "渭州状元桥下，鲁提辖只带九纹龙助阵，三拳要教恶霸郑屠知晓厉害！");
            list[k] = Bio(k++, HeroId.LuZhiShen, 2, "菜园拔垂柳", "大相国寺显神力", "bagua", HeroId.LinChong, "张三", "菜园泼皮", B(BossSkill.Steal, BossSkill.Summon), "李四", "酸枣门外菜园子里，六十二斤水磨禅杖舞动如风，林教头在墙外抚掌喝彩！");
            list[k] = Bio(k++, HeroId.LuZhiShen, 3, "松林擒方腊", "六和听潮圆寂前", "pass", HeroId.WuSong, "方腊", "江南圣公", B(BossSkill.Resist, BossSkill.Daze), "方杰", "乌龙深山松林间，花和尚与武行者双僧并肩，一禅杖了却「遇腊而执」古偈！");
            // 02 林冲（3折）
            list[k] = Bio(k++, HeroId.LinChong, 1, "白虎节堂冤", "带刀误入重地", "imperial", HeroId.LuZhiShen, "富安", "太尉府干办", B(BossSkill.Seal), "高衙内", "八十万禁军教头蒙冤受缚，好兄弟鲁智深暗护左右，杀出重围！");
            list[k] = Bio(k++, HeroId.LinChong, 2, "山神庙雪恨", "风雪草料场", "zigzag", HeroId.ChaiJin, "陆谦", "陆虞候", B(BossSkill.Fog, BossSkill.Haste), "差拨", "漫天大雪压塌草厅，花枪挑着酒葫芦，今夜把昔日仇寇一枪算清！");
            list[k] = Bio(k++, HeroId.LinChong, 3, "水寨并王伦", "断金亭立新纲", "ferry", HeroId.LiuTang, "王伦", "白衣秀士", B(BossSkill.Seal, BossSkill.Steal), "杜迁", "断金亭上王伦百般推诿，豹子头怒拔腰刀，替梁山劈开八方共聚的新天地！");
            // 03 武松（3折）
            list[k] = Bio(k++, HeroId.WuSong, 1, "景阳十八碗", "赤手伏大虫", "zigzag", HeroId.ChaiJin, "大虫", "吊睛白额虎", B(BossSkill.Charge), "酒家", "透瓶香连干十八碗，哨棒虽折，一双铁拳照样打服景阳冈山君！");
            list[k] = Bio(k++, HeroId.WuSong, 2, "飞云断木枷", "血溅鸳鸯楼前夜", "ferry", HeroId.ShiEn, "蒋门神", "快活林恶霸", B(BossSkill.Steal, BossSkill.Daze), "张都监", "飞云浦窄桥之上四名杀手围拢，武都头一声断喝拗碎木枷，反杀回孟州城！");
            list[k] = Bio(k++, HeroId.WuSong, 3, "蜈蚣试双刀", "夜走蜈蚣岭", "pass", HeroId.SunErNiang, "王道人", "飞天蜈蚣", B(BossSkill.Charge, BossSkill.Resist), "张团练", "雪花镔铁双戒刀初出刀鞘，月冷山高，正好斩除岭上害人妖道！");
            // 04 杨志（3折）
            list[k] = Bio(k++, HeroId.YangZhi, 1, "天波试宝刀", "汴京城卖刀", "imperial", HeroId.LinChong, "牛二", "没毛大虫", B(BossSkill.Steal), "高俅", "祖传宝刀吹毛得过、杀人不见血，泼皮牛二步步紧逼，青面兽再难隐忍！");
            list[k] = Bio(k++, HeroId.YangZhi, 2, "大名校场斗", "五十合战急先锋", "plain", HeroId.SuoChao, "闻达", "大名府都监", B(BossSkill.Charge, BossSkill.Haste), "梁中书", "北京大名府演武场上旌旗猎猎，杨家枪刀会一会金蘸斧急先锋！");
            list[k] = Bio(k++, HeroId.YangZhi, 3, "双夺宝珠寺", "威震二龙山", "jiangzhou", HeroId.LuZhiShen, "邓龙", "宝珠寺寨主", B(BossSkill.Resist, BossSkill.Summon), "曹正", "青面兽联手花和尚智破天险，二龙山宝珠寺从此竖起替天大旗！");
            // 05 宋江（3折）
            list[k] = Bio(k++, HeroId.SongJiang, 1, "星夜放天王", "东溪村传信", "snake", HeroId.ZhuTong, "何涛", "缉捕观察", B(BossSkill.Summon), "雷横", "郓城押司冒死飞马报信，美髯公仗义殿后，力保七星弟兄平安脱险！");
            list[k] = Bio(k++, HeroId.SongJiang, 2, "浔阳题反诗", "白龙庙小聚义", "bailong", HeroId.LiKui, "蔡九", "江州知府", B(BossSkill.Seal, BossSkill.Summon), "黄文炳", "浔阳楼醉题敢笑黄巢，黑旋风双斧劫法场，江边古庙二十九雄拜哥哥！");
            list[k] = Bio(k++, HeroId.SongJiang, 3, "还道受天书", "玄女庙定天命", "bagua", HeroId.HuaRong, "赵能", "追捕都头", B(BossSkill.Fog, BossSkill.Seal), "赵得", "古庙神厨九天玄女授三卷天书，宋公明仗剑树起替天行道杏黄旗！");
            // 06 花荣（3折）
            list[k] = Bio(k++, HeroId.HuaRong, 1, "清风救义兄", "大闹清风寨", "river", HeroId.QinMing, "刘高", "清风文知寨", B(BossSkill.Summon, BossSkill.Daze), "黄信", "一张雕弓百步穿杨，小李广单骑劫囚车，连珠神箭射退追兵！");
            list[k] = Bio(k++, HeroId.HuaRong, 2, "对影射双戟", "辕门射绒绦", "zigzag", HeroId.LvFang, "郭盛", "赛仁贵", B(BossSkill.Resist, BossSkill.Crush), "吕方", "两枝方天画戟豹尾绒绦死死缠住，花荣挽弓搭箭，弦响处两将俱服！");
            list[k] = Bio(k++, HeroId.HuaRong, 3, "昱岭复血仇", "泣血诛庞万春", "pass", HeroId.LuJunYi, "庞万春", "小养由基", B(BossSkill.Crush, BossSkill.Daze), "雷炯", "为报史进、石秀六兄弟昱岭血仇，小李广含泪开满月雕弓，一箭穿喉！");
            // 07 李逵（3折）
            list[k] = Bio(k++, HeroId.LiKui, 1, "十字街双斧", "江州独劫法场", "jiangzhou", HeroId.DaiZong, "刽子手", "江州行刑官", B(BossSkill.Haste, BossSkill.Summon), "蔡九", "茶楼上一声怒吼，黑旋风赤膊提双板斧跳入人丛，排头砍去救哥哥！");
            list[k] = Bio(k++, HeroId.LiKui, 2, "沂岭劈四虎", "为母报深仇", "ring", HeroId.ZhuGui, "老母大虫", "沂岭猛虎", B(BossSkill.Devour, BossSkill.Charge), "李鬼", "沂岭窝中连扑出四头猛虎，铁牛双目泣血，朴刀板斧齐下尽诛恶虎！");
            list[k] = Bio(k++, HeroId.LiKui, 3, "元宵闹汴京", "灯市劈衙役", "imperial", HeroId.YanQing, "杨太尉", "禁军统制", B(BossSkill.Summon, BossSkill.Seal), "高俅", "东京元宵鳌山灯下，黑旋风发作起来，浪子燕青一旁巧箭护航杀出城门！");
            // 08 张顺（3折）
            list[k] = Bio(k++, HeroId.ZhangShun, 1, "浔阳淹铁牛", "浪里斗黑旋风", "marsh", HeroId.ZhangHeng, "李逵", "黑旋风", B(BossSkill.Charge, BossSkill.Haste), "戴宗", "岸上打不过莽铁牛，拖进浔阳江心白浪里，教他尝尝灌水饱的滋味！");
            list[k] = Bio(k++, HeroId.ZhangShun, 2, "江心报血冤", "千里请安道全", "ferry", HeroId.WangDingLiu, "张旺", "截江鬼", B(BossSkill.Steal, BossSkill.Fog), "孙五", "水底咬断麻绳脱死劫，星夜请得神医归，浔阳江上亲手擒贼雪恨！");
            list[k] = Bio(k++, HeroId.ZhangShun, 3, "水底凿海鳅", "三败高太尉", "marsh", HeroId.RuanXiaoQi, "高俅", "殿帅太尉", B(BossSkill.Summon, BossSkill.Seal), "牛邦喜", "梁山泊万顷芦荡，浪里白条携活阎罗潜入船底，凿沉朝廷海鳅巨舰！");
            // 09 公孙胜（3折）
            list[k] = Bio(k++, HeroId.GongSunSheng, 1, "七星聚黄冈", "道助生辰纲", "ring", HeroId.LiuTang, "老都管", "梁府都管", B(BossSkill.Summon, BossSkill.Steal), "梁中书", "一清道人下山献大富贵，黄泥冈松林设伏，七星同心夺金银！");
            list[k] = Bio(k++, HeroId.GongSunSheng, 2, "五雷破妖州", "斗法破高廉", "bagua", HeroId.LiKui, "高廉", "高唐州知府", B(BossSkill.Rain, BossSkill.Fog), "殷天锡", "松纹古定剑指苍穹，五雷天罡正法劈碎妖云，助铁牛下井救出柴大官人！");
            list[k] = Bio(k++, HeroId.GongSunSheng, 3, "八阵收魔王", "芒砀山斗法", "bagua", HeroId.FanRui, "项充", "八臂哪吒", B(BossSkill.Fog, BossSkill.Charge), "李衮", "诸葛八阵图飞沙走石，不动刀兵困住飞刀团牌，收服混世魔王拜入玄门！");
            // 10 扈三娘（3折）
            list[k] = Bio(k++, HeroId.HuSanNiang, 1, "独龙冈红索", "单捉王矮虎", "ring", HeroId.LinChong, "祝彪", "祝家三杰", B(BossSkill.Daze, BossSkill.Charge), "王英", "青骢马快、日月刀寒，红锦套索随手一抛，阵前生擒敌将不费吹灰！");
            list[k] = Bio(k++, HeroId.HuSanNiang, 2, "锦索擒天目", "破连环先锋", "plain", HeroId.XuNing, "彭玘", "天目将", B(BossSkill.Charge, BossSkill.Resist), "韩滔", "天目将横刀跃马来战，一丈青佯败回马，红绵套索凌空罩落！");
            list[k] = Bio(k++, HeroId.HuSanNiang, 3, "双姝破阴阵", "大战混天象", "bagua", HeroId.SunErNiang, "琼妖纳延", "辽国番将", B(BossSkill.Fog, BossSkill.Haste), "寇镇远", "太乙混天象阵前，一丈青与母夜叉巾帼双姝齐出，刀光套索大破番兵！");
            // 11 卢俊义（3折）
            list[k] = Bio(k++, HeroId.LuJunYi, 1, "金沙渡遭算", "河北玉麒麟蒙难", "ferry", HeroId.YanQing, "李固", "背主恶奴", B(BossSkill.Steal, BossSkill.Seal), "贾氏", "河北第一杆棒遭奸人算计，忠仆浪子燕青放冷箭救主，杀出芦苇荡！");
            list[k] = Bio(k++, HeroId.LuJunYi, 2, "活捉史文恭", "夜破曾头市", "plain", HeroId.LinChong, "史文恭", "曾头市教师", B(BossSkill.Resist, BossSkill.Daze), "曾涂", "千里照夜玉狮子踏碎贼营，点钢枪寒星一点，生擒史文恭告慰晁天王！");
            list[k] = Bio(k++, HeroId.LuJunYi, 3, "独战四辽将", "幽州显神威", "pass", HeroId.GuanSheng, "耶律宗云", "辽国四皇子", B(BossSkill.Charge, BossSkill.Summon), "耶律宗霖", "卢员外陷身辽军重围毫无惧色，一条点钢枪力敌耶律四将，斩首破阵！");
            // 12 燕青（3折）
            list[k] = Bio(k++, HeroId.YanQing, 1, "冷箭救卢主", "野林射公人", "qingfeng", HeroId.ShiXiu, "薛霸", "押解恶吏", B(BossSkill.Daze, BossSkill.Haste), "董超", "小小川弩百步无虚发，林间两枝冷箭射翻公人，拼命三郎同来接应！");
            list[k] = Bio(k++, HeroId.YanQing, 2, "泰安扑擎天", "鹁鸽旋摔任原", "jiuqu", HeroId.LiKui, "任原", "擎天柱", B(BossSkill.Crush, BossSkill.Resist), "部署", "泰安州岱庙香火鼎盛，浪子展露遍体花绣，一招鹁鸽旋把巨人掀下擂台！");
            list[k] = Bio(k++, HeroId.YanQing, 3, "月夜会道君", "东京访李师师", "imperial", HeroId.DaiZong, "杨戬", "殿前太尉", B(BossSkill.Seal, BossSkill.Fog), "高俅", "汴梁花街箫声婉转，巧借佳人引见微服天子，谈笑间办成招安大事！");
            return list;
        }

        // ─────────────────────────── 纵线二：水泊名役（12 幕 · 天象地形词缀挑战 · 首通必掉神兵） ───────────────────────────

        public sealed class CampaignMeta
        {
            public readonly int UnlockStoryIndex;
            public readonly HeroId RewardWeaponHero;
            public CampaignMeta(int unlockAt, HeroId weaponHero) { UnlockStoryIndex = unlockAt; RewardWeaponHero = weaponHero; }
        }

        public static readonly CampaignMeta[] CampaignMetas =
        {
            new CampaignMeta(2, HeroId.ShiJin),        // 00 少华初鸣（通桃花村解锁）-> 三尖两刃刀
            new CampaignMeta(7, HeroId.LinChong),      // 01 风雪草料场（通风雪山神庙）-> 丈八蛇矛
            new CampaignMeta(9, HeroId.YangZhi),       // 02 赤日黄泥冈（通智取生辰纲）-> 祖传宝刀
            new CampaignMeta(19, HeroId.WuSong),       // 03 血月鸳鸯楼（通血溅鸳鸯楼）-> 雪花戒刀
            new CampaignMeta(23, HeroId.HuaRong),      // 04 飞矢清风山（通对影山）-> 银鹊弓
            new CampaignMeta(28, HeroId.LiKui),        // 05 怒涛浔阳江（通白龙庙）-> 双板斧
            new CampaignMeta(37, HeroId.HuSanNiang),   // 06 盘陀祝家庄（通三打祝家庄）-> 红锦套索
            new CampaignMeta(41, HeroId.XuNing),       // 07 铁甲连环马（通连环马）-> 钩镰枪
            new CampaignMeta(51, HeroId.GuanSheng),    // 08 冰封大名府（通水火二将）-> 青龙偃月刀
            new CampaignMeta(59, HeroId.ZhangShun),    // 09 水淹海鳅船（通三败高俅）-> 鱼叉/分水刺（李俊/张顺）
            new CampaignMeta(62, HeroId.GongSunSheng), // 10 太乙破辽阵（通破混天象阵）-> 松纹古定剑
            new CampaignMeta(70, HeroId.LuZhiShen),    // 11 绝壁乌龙岭（通擒方腊）-> 水磨禅杖
        };

        public static readonly ChapterDef[] Campaigns = BuildCampaigns();

        static ChapterDef Cam(int idx, string name, string hui, string map, BattleRule rule, HeroId[] pool,
            string boss, string bTitle, BossSkill[] skills, HeroId nemesis, string rival, string story)
        {
            int unlockAt = CampaignMetas[idx].UnlockStoryIndex;
            float k = Math.Min(1f, (unlockAt + 10f) / 68f);
            return new ChapterDef
            {
                Index = Math.Min(All.Length - 1, unlockAt + 4),
                CampaignIndex = idx,
                Rule = rule,
                Name = name,
                Hui = $"水泊名役·第{idx + 1}幕 · {hui}",
                Act = "水泊名役",
                Map = Maps[map],
                Pool = pool,
                Joins = new HeroId[0],
                Boss = new BossDef { Name = boss, Title = bTitle, Skills = skills, Nemesis = nemesis, SkillText = BossDef.Describe(skills, nemesis) },
                RivalName = rival,
                Story = story,
                Lore = "名役推演：顺应战场天象与兵种克制排兵布阵，首通必缴获镇寨名兵器！",
                RivalSkill = Math.Min(0.98f, 0.65f + 0.30f * k),
                RivalIncome = 1.02f + 0.14f * k,
                FoeHp = 1.08f + 0.18f * k,
            };
        }

        static ChapterDef[] BuildCampaigns() => new[]
        {
            Cam(0, "少华初鸣", "烈日围庄", "shijia", BattleRule.ScorchingSun,
                J(HeroId.ShiJin, HeroId.LuZhiShen, HeroId.ZhuWu, HeroId.ChenDa),
                "县尉", "华阴精锐", B(BossSkill.Summon, BossSkill.Haste), HeroId.ShiJin, "王进",
                "酷暑烈日下官军急攻史家庄，善用朱武军师加速与史进、鲁达近战横扫！"),
            Cam(1, "风雪草料场", "奇寒死斗", "zigzag", BattleRule.Blizzard,
                J(HeroId.LinChong, HeroId.LuZhiShen, HeroId.ChaiJin, HeroId.ShiJin),
                "陆谦", "火烧草场", B(BossSkill.Fog, BossSkill.Charge), HeroId.LinChong, "富安",
                "朔风凛冽大雪封山，近战禅杖与刀法威力暴涨，迎战纵火仇敌陆虞候！"),
            Cam(2, "赤日黄泥冈", "炎天智取", "ring", BattleRule.ScorchingSun,
                J(HeroId.YangZhi, HeroId.WuYong, HeroId.LiuTang, HeroId.BaiSheng, HeroId.GongSunSheng),
                "老都管", "押纲精兵", B(BossSkill.Summon, BossSkill.Steal), HeroId.YangZhi, "梁中书",
                "赤日炎炎似火烧！开局重金在握，借吴用与白胜奇谋截下生辰纲铁卫！"),
            Cam(3, "血月鸳鸯楼", "寒刃连斩", "imperial", BattleRule.Blizzard,
                J(HeroId.WuSong, HeroId.SunErNiang, HeroId.ZhangQingY, HeroId.ShiEn),
                "张都监", "孟州重兵", B(BossSkill.Seal, BossSkill.Resist), HeroId.WuSong, "蒋门神",
                "孟州寒夜杀气如霜，武松双戒刀寒芒大盛，十字坡夫妇蒸包犒军助阵！"),
            Cam(4, "飞矢清风山", "百步封喉", "pass", BattleRule.Labyrinth,
                J(HeroId.HuaRong, HeroId.QinMing, HeroId.LvFang, HeroId.GuoSheng),
                "慕容知府", "青州铁骑", B(BossSkill.Charge, BossSkill.Daze), HeroId.HuaRong, "黄信",
                "清风山口险径盘桓，弓箭与飞石射程远扬，小李广据险一夫当关！"),
            Cam(5, "怒涛浔阳江", "白浪滔天", "ferry", BattleRule.FloodedMarsh,
                J(HeroId.LiKui, HeroId.ZhangShun, HeroId.LiJun, HeroId.ZhangHeng, HeroId.DaiZong),
                "黄文炳", "江州水师", B(BossSkill.Rain, BossSkill.Summon), HeroId.ZhangShun, "蔡九",
                "浔阳江潮水暴涨，官兵寸步难行，混江龙与浪里白条翻江倒海痛击水师！"),
            Cam(6, "盘陀祝家庄", "迷林暗哨", "bagua", BattleRule.Labyrinth,
                J(HeroId.HuSanNiang, HeroId.SunLi, HeroId.LiYing, HeroId.ShiXiu, HeroId.YangXiong),
                "栾廷玉", "铁棒教师", B(BossSkill.Resist, BossSkill.Fog), HeroId.SunLi, "祝彪",
                "盘陀石路九曲回肠，扑天雕飞刀远程狙杀，病尉迟同门破铁布衫！"),
            Cam(7, "铁甲连环马", "钩镰破甲", "plain", BattleRule.IronCavalry,
                J(HeroId.XuNing, HeroId.LinChong, HeroId.HuYanZhuo, HeroId.LingZhen, HeroId.TangLong),
                "呼延灼", "连环铁骑", B(BossSkill.Charge, BossSkill.Haste), HeroId.XuNing, "韩滔",
                "三十连环重甲铁骑波波压境！架起长枪阵与金枪手钩镰枪，专破铁甲洪流！"),
            Cam(8, "冰封大名府", "踏雪攻坚", "imperial", BattleRule.Blizzard,
                J(HeroId.GuanSheng, HeroId.SuoChao, HeroId.YangZhi, HeroId.LuJunYi, HeroId.ShiXiu),
                "梁中书", "留守禁军", B(BossSkill.Seal, BossSkill.Crush), HeroId.GuanSheng, "李成",
                "大名府天降暴雪，青龙偃月刀与金蘸斧借寒威横扫千军，直捣留守司！"),
            Cam(9, "水淹海鳅船", "苇荡伏兵", "marsh", BattleRule.FloodedMarsh,
                J(HeroId.ZhangShun, HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi, HeroId.LiJun),
                "高俅", "十路统帅", B(BossSkill.Summon, BossSkill.Seal), HeroId.ZhangShun, "牛邦喜",
                "八百里水泊烟波浩渺，水军五头领齐聚水格，凿穿海鳅巨舰生擒高太尉！"),
            Cam(10, "太乙破辽阵", "天象玄雷", "bagua", BattleRule.IronCavalry,
                J(HeroId.GongSunSheng, HeroId.GuanSheng, HeroId.LinChong, HeroId.HuaRong, HeroId.ZhangQing),
                "兀颜光", "辽国都统军", B(BossSkill.Fog, BossSkill.Resist), HeroId.GuanSheng, "贺重宝",
                "辽国铁骑布下太乙混天象阵，五虎长枪撕裂重甲，入云龙天雷轰顶！"),
            Cam(11, "绝壁乌龙岭", "血战江南", "pass", BattleRule.Labyrinth,
                J(HeroId.LuZhiShen, HeroId.WuSong, HeroId.HuaRong, HeroId.XieZhen, HeroId.LuJunYi),
                "邓元觉", "宝光国师", B(BossSkill.Crush, BossSkill.Revive), HeroId.HuaRong, "石宝",
                "乌龙天险山高路窄，以远程雕弓压制国师禅杖，鲁智深武松死守最后关隘！"),
        };

        /// <summary>每日一战；剧情回、聚义厅为假。</summary>
        public bool IsDaily;
        /// <summary>每日一战当天的种子，人人一样。</summary>
        public uint Seed;

        /// <summary>同一回换一池好汉（地图、头目、对手不变）。</summary>
        public ChapterDef WithPool(params HeroId[] pool)
        {
            var c = (ChapterDef)MemberwiseClone();
            c.Pool = pool;
            return c;
        }
    }
}
