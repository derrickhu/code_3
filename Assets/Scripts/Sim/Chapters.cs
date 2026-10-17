using System;
using System.Collections.Generic;

namespace Rescue.Sim
{
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

        public bool IsEndless => Round > 0;

        // ─────────────────────────── 地图模板 ───────────────────────────

        public static readonly Dictionary<string, string[]> Maps = new Dictionary<string, string[]>
        {
            ["qingfeng"] = new[] { "S######.", ".ooo..#.", ".######.", ".#ooo...", ".######G" },
            ["jiangzhou"] = new[] { "S#..ooo.", ".#.####.", ".#.#oo#.", ".###..#.", "......#G" },
            ["bailong"] = new[] { "~~~~~~~S", "########", "#.ooooo.", "########", ".......G" },
            ["snake"] = new[] { "S#######", ".oooo..#", "..######", "..#oooo.", "..#####G" },
            ["ring"] = new[] { "S#######", "...oo..#", ".o....o#", "...oo..#", "G#######" },
            ["jiuqu"] = new[] { "S.###.#G", "#o#.#.#.", "#.#o#o#.", "#o#.#.#o", "###.###." },
            ["marsh"] = new[] { "~~~~S###", "~ww..o.#", "########", "#~~o~~w.", "#######G" },
            ["zigzag"] = new[] { "S#####..", ".oo..#o.", "..o..#..", ".o.o.###", ".......G" },
            ["plain"] = new[] { "S.......", "#.oo.o..", "#..o....", "#.o..o..", "#######G" },
            ["river"] = new[] { "S###....", "...#.ww.", ".o.#o~~.", ".o.####.", "..w..o#G" },
        };

        static readonly string[] EndlessMaps = { "qingfeng", "jiangzhou", "bailong", "snake", "ring", "jiuqu", "marsh", "zigzag", "plain", "river" };

        // ─────────────────────────── 剧情 ───────────────────────────

        public static readonly ChapterDef[] All = BuildStory();

        public static int StoryCount => All.Length;

        static ChapterDef C(string name, string hui, string act, string map, HeroId[] joins, HeroId[] extra,
            string boss, string title, BossSkill skill, HeroId nemesis, string rival, string story, string lore)
        {
            var pool = new List<HeroId>(joins);
            foreach (var h in extra) if (!pool.Contains(h)) pool.Add(h);
            return new ChapterDef
            {
                Name = name, Hui = hui, Act = act, Map = Maps[map], Joins = joins, Pool = pool.ToArray(),
                Boss = new BossDef { Name = boss, Title = title, Skill = skill, Nemesis = nemesis, SkillText = SkillText(skill, nemesis) },
                RivalName = rival, Story = story, Lore = lore,
            };
        }

        static string SkillText(BossSkill s, HeroId nemesis) => s switch
        {
            BossSkill.Summon => "每隔几秒大喝来人，招来三个兵丁",
            BossSkill.Haste => "喝令周围兵丁加速猛冲",
            _ => $"老奸巨猾，只受一半伤害（{HeroDef.Get(nemesis).Name}例外）",
        };

        static HeroId[] J(params HeroId[] ids) => ids;

        static ChapterDef[] BuildStory()
        {
            var list = new[]
            {
                C("史家村", "第二回 王教头私走延安府 九纹龙大闹史家村", "引首", "snake",
                    J(HeroId.ShiJin, HeroId.ZhuWu, HeroId.ChenDa, HeroId.YangChun), J(),
                    "县尉", "华阴县", BossSkill.Summon, HeroId.ShiJin, "王进",
                    "少华山的跳涧虎陈达要借道华阴县，被九纹龙史进活捉。朱武、杨春负荆请罪，史进敬他们义气，结为兄弟——官府却连夜围了史家庄！",
                    "《水浒传》开篇，洪太尉误走妖魔，从伏魔殿放出一百单八个魔君，正是日后梁山的三十六天罡、七十二地煞。"),
                C("拳打镇关西", "第三回 史大郎夜走华阴县 鲁提辖拳打镇关西", "鲁达篇", "qingfeng",
                    J(HeroId.LuZhiShen, HeroId.LiZhong), J(HeroId.ShiJin, HeroId.ZhuWu),
                    "郑屠", "镇关西", BossSkill.Haste, HeroId.LuZhiShen, "金老汉",
                    "渭州状元桥下，卖肉的郑屠自称镇关西，欺压卖唱的金家父女。提辖鲁达听了，拍案而起：洒家这就去会会他！",
                    "鲁达三拳：一拳打在鼻子上，咸的酸的辣的一发滚出；二拳打在眼眶，红的黑的绛的都绽将出来；三拳太阳穴，磬儿钹儿铙儿一齐响。"),
                C("桃花村", "第五回 小霸王醉入销金帐 花和尚大闹桃花村", "鲁达篇", "jiangzhou",
                    J(HeroId.ZhouTong), J(HeroId.LuZhiShen, HeroId.LiZhong, HeroId.ShiJin),
                    "崔道成", "生铁佛", BossSkill.Resist, HeroId.ShiJin, "刘太公",
                    "鲁智深大闹五台山被打发去东京，路过桃花村，醉打要强娶民女的小霸王周通；转到瓦罐寺，又撞上恶僧生铁佛崔道成。",
                    "鲁智深醉打周通之后，周通与李忠占的桃花山，后来也成了梁山的一支人马。"),
                C("野猪林", "第八回 林教头刺配沧州道 鲁智深大闹野猪林", "林冲篇", "ring",
                    J(HeroId.LinChong), J(HeroId.LuZhiShen, HeroId.ShiJin, HeroId.LiZhong),
                    "薛霸", "押解公人", BossSkill.Haste, HeroId.LuZhiShen, "董超",
                    "林冲被高俅陷害，刺配沧州。押解公人董超、薛霸受了贿赂，要在野猪林结果他性命，水火棍刚举起——一条禅杖从松林里飞了出来！",
                    "董超、薛霸一路用滚水烫林冲的脚，鲁智深一路暗中护送，这才有了野猪林救林冲。"),
                C("柴进庄", "第九回 柴进门招天下客 林冲棒打洪教头", "林冲篇", "plain",
                    J(HeroId.ChaiJin), J(HeroId.LinChong, HeroId.LuZhiShen),
                    "洪教头", "柴府教师", BossSkill.Resist, HeroId.LinChong, "庄客",
                    "沧州小旋风柴进专爱结交好汉，盛情款待林冲。庄上的洪教头不服，硬要比棒——林冲只得出手。",
                    "柴进是后周世宗柴荣的后人。宋太祖陈桥兵变得了柴家天下，所以赐了丹书铁券，犯罪也不得加刑。"),
                C("风雪山神庙", "第十回 林教头风雪山神庙 陆虞候火烧草料场", "林冲篇", "zigzag",
                    J(HeroId.ZhuGui), J(HeroId.LinChong, HeroId.ChaiJin, HeroId.LuZhiShen),
                    "陆谦", "陆虞候", BossSkill.Haste, HeroId.LinChong, "富安",
                    "大雪压塌了草屋，林冲到山神庙避风，听见门外陆谦放火烧了草料场，要他葬身火海。忍无可忍，林冲挺枪而出。",
                    "林冲被逼得家破人亡，只得投奔梁山——这就是成语「逼上梁山」的来历。"),
                C("杨志卖刀", "第十二回 梁山泊林冲落草 汴京城杨志卖刀", "杨志篇", "river",
                    J(HeroId.YangZhi), J(HeroId.LinChong, HeroId.ZhuGui, HeroId.ChaiJin),
                    "牛二", "没毛大虫", BossSkill.Summon, HeroId.YangZhi, "王伦",
                    "杨家将后人杨志失陷花石纲，穷得只能在汴京街头卖祖传宝刀。泼皮牛二偏要他当街试刀杀人……",
                    "杨志的宝刀有三好：砍铜剁铁刀口不卷，吹毛得过，杀人刀上没血。"),
                C("智取生辰纲", "第十六回 杨志押送金银担 吴用智取生辰纲", "生辰纲篇", "ring",
                    J(HeroId.WuYong, HeroId.GongSunSheng, HeroId.LiuTang, HeroId.BaiSheng), J(HeroId.YangZhi),
                    "老都管", "梁府都管", BossSkill.Summon, HeroId.BaiSheng, "梁中书",
                    "梁中书给岳父蔡京送十万贯生辰纲。黄泥冈上烈日当头，一个卖酒的汉子挑着担子唱着山歌走来……",
                    "白胜唱的「赤日炎炎似火烧，野田禾稻半枯焦」，是书中最有名的一首山歌。"),
                C("二龙山", "第十七回 花和尚单打二龙山 青面兽双夺宝珠寺", "生辰纲篇", "jiangzhou",
                    J(HeroId.CaoZheng), J(HeroId.YangZhi, HeroId.LuZhiShen, HeroId.LiZhong),
                    "邓龙", "二龙山寨主", BossSkill.Resist, HeroId.YangZhi, "周通",
                    "丢了生辰纲，杨志无路可走，在曹正酒店里遇见了鲁智深。曹正献计：绑了花和尚诈作献俘，打上二龙山！",
                    "二龙山宝珠寺后来成为三山聚义中的一山，鲁智深、杨志、武松、施恩、张青、孙二娘都在这里。"),
                C("私放晁天王", "第十八回 美髯公智稳插翅虎 宋公明私放晁天王", "生辰纲篇", "snake",
                    J(HeroId.ZhuTong, HeroId.LeiHeng), J(HeroId.WuYong, HeroId.LiuTang, HeroId.GongSunSheng),
                    "府尹", "济州", BossSkill.Summon, HeroId.ZhuTong, "何涛",
                    "生辰纲案发，济州官兵连夜来捉晁盖。押司宋江飞马报信，都头朱仝、雷横一前一后，都想放晁天王一条生路。",
                    "宋江私放晁盖，是整个梁山故事的转折点——没有这一放，就没有后来的梁山泊。"),
                C("石碣村", "第十九回 晁盖梁山小夺泊", "生辰纲篇", "marsh",
                    J(HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi), J(HeroId.WuYong, HeroId.LiuTang),
                    "何涛", "缉捕观察", BossSkill.Summon, HeroId.RuanXiaoQi, "黄安",
                    "缉捕观察何涛带着官船杀进石碣村芦苇荡。阮家三兄弟笑了：在水上和打鱼的斗？",
                    "吴用去石碣村说三阮入伙，三兄弟拍着脖子说：「这腔热血，只要卖与识货的！」"),
                C("火并王伦", "第十九回 林冲水寨大并火", "生辰纲篇", "snake",
                    J(HeroId.DuQian, HeroId.SongWan), J(HeroId.LinChong, HeroId.WuYong, HeroId.ZhuGui),
                    "王伦", "白衣秀士", BossSkill.Resist, HeroId.LinChong, "晁盖",
                    "晁盖一伙投奔梁山，寨主王伦却拿出五锭大银要打发他们下山。林冲踢翻桌子：你这嫉贤妒能的贼！",
                    "王伦心胸狭窄、嫉贤妒能。林冲火并王伦，拥晁盖为寨主，梁山从此兴旺起来。"),
                C("怒杀阎婆惜", "第二十二回 阎婆大闹郓城县 朱仝义释宋公明", "宋江篇", "jiuqu",
                    J(HeroId.SongQing), J(HeroId.ZhuTong, HeroId.LeiHeng, HeroId.ChaiJin),
                    "张文远", "郓城押司", BossSkill.Haste, HeroId.ZhuTong, "阎婆",
                    "阎婆惜拿到晁盖写给宋江的书信，要挟宋江。一时失手，宋江杀了她。张文远唆使阎婆告官，朱仝又一次放走了宋江。",
                    "「及时雨」是说宋江扶危济困，像及时雨一样帮人。他因此名满江湖，好汉们一听名号就纳头便拜。"),
                C("景阳冈", "第二十三回 横海郡柴进留宾 景阳冈武松打虎", "武松篇", "zigzag",
                    J(HeroId.WuSong), J(HeroId.ChaiJin, HeroId.SongJiang),
                    "大虫", "景阳冈", BossSkill.Haste, HeroId.WuSong, "猎户",
                    "武松在柴进庄上结识宋江，回乡探兄路过景阳冈。酒店挂着「三碗不过冈」，他偏喝了十八碗，提着哨棒上了冈……",
                    "武松打虎用的是双拳——哨棒一下打在枯树上折了，他索性揪住虎头一顿乱打。"),
                C("斗杀西门庆", "第二十六回 供人头武二郎设祭", "武松篇", "jiangzhou",
                    J(HeroId.WuDaLang), J(HeroId.WuSong, HeroId.LeiHeng, HeroId.SongQing),
                    "西门庆", "阳谷县财主", BossSkill.Resist, HeroId.WuSong, "王婆",
                    "武松做了阳谷县都头，出差回来，哥哥武大郎已被潘金莲与西门庆毒死。他到狮子楼找西门庆，要替哥哥讨个公道。",
                    "武大郎不在一百单八将之内，是本作的隐藏好汉：凑出他的名字，就有热乎的炊饼。"),
                C("十字坡", "第二十七回 母夜叉孟州道卖人肉 武都头十字坡遇张青", "武松篇", "qingfeng",
                    J(HeroId.SunErNiang, HeroId.ZhangQingY), J(HeroId.WuSong),
                    "孙二娘", "母夜叉", BossSkill.Haste, HeroId.WuSong, "公人",
                    "武松刺配孟州，路过十字坡酒店。老板娘笑脸相迎，端上的酒却有股怪味——武松装作晕倒，看她怎么下手。",
                    "张青告诫孙二娘：三等人不可坏——云游僧道、江湖上卖艺的、流配的犯人。"),
                C("醉打蒋门神", "第二十九回 施恩重霸孟州道 武松醉打蒋门神", "武松篇", "ring",
                    J(HeroId.ShiEn), J(HeroId.WuSong, HeroId.ZhangQingY, HeroId.SunErNiang),
                    "蒋门神", "快活林恶霸", BossSkill.Resist, HeroId.WuSong, "张团练",
                    "施恩的快活林酒店被蒋门神强占。武松一路「无三不过望」，每过一家酒店喝三碗，醉醺醺地来到快活林。",
                    "武松醉打蒋门神用的是「玉环步、鸳鸯脚」，是他平生得意的本事。"),
                C("血溅鸳鸯楼", "第三十一回 张都监血溅鸳鸯楼 武行者夜走蜈蚣岭", "武松篇", "jiuqu",
                    J(HeroId.KongMing, HeroId.KongLiang), J(HeroId.WuSong, HeroId.ShiEn),
                    "张都监", "孟州兵马都监", BossSkill.Summon, HeroId.WuSong, "张团练",
                    "张都监设计陷害武松，又派人在飞云浦害他。武松杀回孟州城，血溅鸳鸯楼，扮作行者连夜出逃，在孔家庄醉打孔亮。",
                    "血溅鸳鸯楼后，武松蘸血在白粉墙上写下八个大字：「杀人者，打虎武松也。」"),
                C("清风寨", "第三十三回 宋江夜看小鳌山 花荣大闹清风寨", "清风山篇", "river",
                    J(HeroId.HuaRong, HeroId.YanShun, HeroId.WangYing, HeroId.ZhengTianShou), J(),
                    "刘高", "清风寨文知寨", BossSkill.Summon, HeroId.HuaRong, "黄信",
                    "宋江夜看花灯，被清风寨知寨刘高认出，五花大绑押进寨中。花荣连夜点兵：救哥哥！",
                    "宋江在清风山差点被燕顺挖心做醒酒汤，一句「可惜宋江死在这里」才救了自己一命。"),
                C("瓦砾场", "第三十四回 镇三山大闹青州道 霹雳火夜走瓦砾场", "清风山篇", "plain",
                    J(HeroId.QinMing, HeroId.HuangXin), J(HeroId.HuaRong, HeroId.YanShun, HeroId.WangYing),
                    "秦明", "霹雳火", BossSkill.Haste, HeroId.HuaRong, "慕容知府",
                    "青州兵马总管霹雳火秦明奉命来剿清风山，一条狼牙棒杀得天昏地暗。花荣说：要擒此人，得用计。",
                    "宋江派人扮作秦明去青州城下杀人放火，慕容知府杀了秦明全家，秦明从此死心塌地上了山。"),
                C("对影山", "第三十五回 石将军村店寄书 小李广梁山射雁", "清风山篇", "zigzag",
                    J(HeroId.LvFang, HeroId.GuoSheng, HeroId.ShiYong), J(HeroId.HuaRong, HeroId.QinMing),
                    "郭盛", "赛仁贵", BossSkill.Resist, HeroId.HuaRong, "吕方",
                    "对影山上，小温侯吕方与赛仁贵郭盛争山头，画戟对画戟斗了几十天。两枝戟上的绒绦缠在了一起……",
                    "花荣一箭射断缠在一起的绒绦，两人心服口服；上梁山后他又一箭射中雁行第三只。"),
                C("揭阳岭", "第三十六回 梁山泊吴用举戴宗 揭阳岭宋江逢李俊", "江州篇", "jiuqu",
                    J(HeroId.LiLi, HeroId.LiJun, HeroId.TongWei, HeroId.TongMeng), J(HeroId.ShiYong),
                    "李立", "催命判官", BossSkill.Summon, HeroId.LiJun, "穆春",
                    "宋江刺配江州，在揭阳岭酒店喝了一碗酒就人事不省，被拖进作坊要开剥——混江龙李俊急匆匆赶到。",
                    "揭阳三霸：岭上李俊、李立，镇上穆弘、穆春，江上张横、张顺。"),
                C("浔阳江", "第三十七回 没遮拦追赶及时雨 船火儿夜闹浔阳江", "江州篇", "bailong",
                    J(HeroId.MuHong, HeroId.MuChun, HeroId.XueYong, HeroId.ZhangHeng), J(HeroId.LiJun),
                    "张横", "船火儿", BossSkill.Resist, HeroId.LiJun, "穆太公",
                    "宋江赏了卖艺的薛永，得罪了揭阳镇的穆氏兄弟，被追到江边，慌忙跳上一条船——船家张横问：吃板刀面，还是吃馄饨？",
                    "「板刀面」是一刀砍下水，「馄饨」是自己脱光跳下去。宋江报出名号，张横纳头便拜。"),
                C("浔阳楼", "第三十八回 及时雨会神行太保 黑旋风斗浪里白条", "江州篇", "marsh",
                    J(HeroId.DaiZong, HeroId.LiKui, HeroId.ZhangShun), J(HeroId.ZhangHeng, HeroId.LiJun),
                    "张顺", "浪里白条", BossSkill.Haste, HeroId.ZhangHeng, "戴宗",
                    "李逵在江边抢鱼，和渔牙子张顺打了起来。岸上张顺打不过，一把把李逵拖下水——这下轮到铁牛喝水了。",
                    "黑旋风斗浪里白条，是书中最有名的「不打不相识」：两人后来成了生死兄弟。"),
                C("江州法场", "第四十回 梁山泊好汉劫法场 白龙庙英雄小聚义", "江州篇", "jiangzhou",
                    J(HeroId.SongJiang, HeroId.XiaoRang, HeroId.JinDaJian), J(HeroId.LiKui, HeroId.DaiZong, HeroId.HuaRong),
                    "蔡九", "江州知府", BossSkill.Haste, HeroId.LiKui, "晁盖",
                    "浔阳楼上一首反诗，宋江被判斩首。萧让、金大坚仿造的蔡京书信露了破绽。午时三刻，十字路口，刽子手已经举刀——",
                    "宋江的反诗：「他时若遂凌云志，敢笑黄巢不丈夫。」黄巢是唐末造反的首领。"),
                C("白龙庙", "第四十一回 宋江智取无为军 张顺活捉黄文炳", "江州篇", "bailong",
                    J(HeroId.HouJian), J(HeroId.ZhangShun, HeroId.ZhangHeng, HeroId.RuanXiaoEr, HeroId.RuanXiaoQi, HeroId.LiKui),
                    "黄文炳", "无为军通判", BossSkill.Resist, HeroId.ZhangShun, "李俊",
                    "劫了法场，众好汉护着宋江退到江边白龙庙。告发反诗的黄文炳就在对岸无为军，裁缝侯健说：他家的路，我熟。",
                    "白龙庙小聚义之后，宋江正式上了梁山，坐了第二把交椅。"),
                C("黄门山", "第四十一回 回山途中", "江州篇", "zigzag",
                    J(HeroId.OuPeng, HeroId.JiangJing, HeroId.MaLin, HeroId.TaoZongWang), J(HeroId.SongJiang, HeroId.HuaRong),
                    "欧鹏", "摩云金翅", BossSkill.Haste, HeroId.HuaRong, "蒋敬",
                    "回梁山的路上，黄门山四个头领拦住去路。一问来的是及时雨宋公明，四人当即下山相迎。",
                    "蒋敬是梁山的「账房」，书中说他「精通书算，积万累千，纤毫不差」。"),
                C("沂岭杀四虎", "第四十三回 假李逵剪径劫单人 黑旋风沂岭杀四虎", "李逵篇", "ring",
                    J(HeroId.ZhuFu, HeroId.LiYun), J(HeroId.LiKui, HeroId.ZhuGui),
                    "大虫", "沂岭", BossSkill.Haste, HeroId.LiKui, "李鬼",
                    "李逵回家接老娘上山享福，背到沂岭，老娘口渴，他去打水回来，只见一摊血……",
                    "李逵怒杀四虎后被捉，朱富用蒙汗药酒麻翻了自己的师父李云，救出李逵，师徒同上梁山。"),
                C("饮马川", "第四十四回 锦豹子小径逢戴宗 病关索长街遇石秀", "祝家庄篇", "snake",
                    J(HeroId.YangLin, HeroId.DengFei, HeroId.MengKang, HeroId.PeiXuan), J(HeroId.DaiZong),
                    "邓飞", "火眼狻猊", BossSkill.Haste, HeroId.YangLin, "裴宣",
                    "戴宗下山打探公孙胜，路遇锦豹子杨林，一同走到饮马川，山上冲下一伙强人——为首的火眼狻猊邓飞，正是杨林的旧识。",
                    "饮马川原是邓飞、孟康的山寨，铁面孔目裴宣来了之后被推为寨主。"),
                C("翠屏山", "第四十六回 病关索大闹翠屏山 拼命三火烧祝家店", "祝家庄篇", "river",
                    J(HeroId.YangXiong, HeroId.ShiXiu, HeroId.ShiQian), J(HeroId.YangLin),
                    "裴如海", "报恩寺僧", BossSkill.Resist, HeroId.ShiXiu, "潘巧云",
                    "拼命三郎石秀看破了嫂嫂与和尚裴如海私通，杨雄翠屏山上清理门户，带着时迁投奔梁山，路上住进了祝家店……",
                    "时迁在祝家店偷了报晓的公鸡下酒，引出了三打祝家庄。"),
                C("一打祝家庄", "第四十七回 扑天雕双修生死书 宋公明一打祝家庄", "祝家庄篇", "jiuqu",
                    J(HeroId.LiYing, HeroId.DuXing), J(HeroId.YangXiong, HeroId.ShiXiu, HeroId.ShiQian),
                    "祝龙", "祝家庄大郎", BossSkill.Summon, HeroId.LiYing, "祝朝奉",
                    "时迁被祝家庄捉了。李家庄李应写信求情，反被祝彪一箭射伤。宋江起兵来打，却在盘陀路里转得晕头转向。",
                    "祝家庄的盘陀路，要见白杨树才转弯，否则怎么走都走不出去——梁山第一次就吃了这个亏。"),
                C("二打祝家庄", "第四十八回 一丈青单捉王矮虎 宋公明两打祝家庄", "祝家庄篇", "ring",
                    J(HeroId.HuSanNiang), J(HeroId.LinChong, HeroId.WangYing, HeroId.LiYing),
                    "扈三娘", "扈家庄", BossSkill.Haste, HeroId.LinChong, "扈成",
                    "扈家庄的一丈青扈三娘使日月双刀，一阵就活捉了矮脚虎王英。林冲拍马迎上……",
                    "梁山三位女将：一丈青扈三娘、母夜叉孙二娘、母大虫顾大嫂。"),
                C("登州劫牢", "第四十九回 解珍解宝双越狱 孙立孙新大劫牢", "祝家庄篇", "plain",
                    J(HeroId.XieZhen, HeroId.XieBao, HeroId.GuDaSao, HeroId.SunXin), J(),
                    "毛太公", "登州恶霸", BossSkill.Summon, HeroId.XieZhen, "毛仲义",
                    "猎户解珍、解宝射死的老虎滚进了毛太公家后园，毛太公不但不还，还诬他们是贼关进死牢。表姐顾大嫂拍案：劫牢！",
                    "顾大嫂一句「你们不去，我便自去」，逼得做提辖的孙立也一起劫了登州大牢。"),
                C("三打祝家庄", "第五十回 吴学究双掌连环计 宋公明三打祝家庄", "祝家庄篇", "jiuqu",
                    J(HeroId.SunLi, HeroId.YueHe, HeroId.ZouYuan, HeroId.ZouRun), J(HeroId.XieBao),
                    "栾廷玉", "祝家庄教师", BossSkill.Resist, HeroId.SunLi, "祝彪",
                    "孙立是祝家庄教师栾廷玉的同门师兄弟。他打着官府的旗号「来助战」，开进了祝家庄……",
                    "孙立里应外合，三打祝家庄终于成功。这是梁山第一次打下有城墙的庄子。"),
                C("高唐州", "第五十四回 入云龙斗法破高廉 黑旋风探穴救柴进", "高唐州篇", "zigzag",
                    J(HeroId.TangLong), J(HeroId.GongSunSheng, HeroId.LiKui, HeroId.ChaiJin),
                    "高廉", "高唐州知府", BossSkill.Summon, HeroId.GongSunSheng, "殷天锡",
                    "柴进被高俅的堂弟高廉关进死牢。高廉会妖法，梁山连败两阵。李逵、戴宗去蓟州请回公孙胜，路上又结识了铁匠汤隆。",
                    "公孙胜的师父罗真人曾把李逵丢到半空，摔进蓟州府牢里，吓得铁牛再不敢胡来。"),
                C("连环马", "第五十七回 徐宁教使钩镰枪 宋江大破连环马", "连环马篇", "plain",
                    J(HeroId.XuNing, HeroId.HanTao, HeroId.PengQi, HeroId.LingZhen), J(HeroId.TangLong, HeroId.ShiQian),
                    "呼延灼", "双鞭", BossSkill.Haste, HeroId.XuNing, "高俅",
                    "朝廷派呼延灼带连环马来剿梁山，三十匹马用铁环连成一排，冲到哪里哪里倒。汤隆说：只有我表哥徐宁的钩镰枪能破！",
                    "时迁连夜盗走徐宁家传的雁翎锁子甲，把他赚上梁山。钩镰枪专钩马腿，连环马便破了。"),
                C("三山聚义", "第五十八回 三山聚义打青州 众虎同心归水泊", "连环马篇", "ring",
                    J(HeroId.HuYanZhuo), J(HeroId.LuZhiShen, HeroId.YangZhi, HeroId.WuSong, HeroId.KongMing, HeroId.QinMing),
                    "慕容知府", "青州知府", BossSkill.Resist, HeroId.QinMing, "桃花山",
                    "呼延灼兵败投奔青州，替慕容知府去打二龙山、桃花山、白虎山。三山好汉合兵一处，请宋江来破青州。",
                    "三山聚义：二龙山、桃花山、白虎山同归梁山，梁山的实力从此大增。"),
                C("芒砀山", "第六十回 公孙胜芒砀山降魔", "连环马篇", "ring",
                    J(HeroId.FanRui, HeroId.XiangChong, HeroId.LiGun), J(HeroId.GongSunSheng),
                    "樊瑞", "混世魔王", BossSkill.Summon, HeroId.GongSunSheng, "项充",
                    "芒砀山混世魔王樊瑞扬言要吞并梁山。他会呼风唤雨，手下项充、李衮团牌飞刀密不透风。",
                    "樊瑞被公孙胜破了妖法，心悦诚服，拜公孙胜为师。"),
                C("曾头市", "第六十回 晁天王曾头市中箭", "曾头市篇", "jiangzhou",
                    J(HeroId.DuanJingZhu), J(HeroId.LinChong, HeroId.HuYanZhuo, HeroId.HuaRong),
                    "史文恭", "曾头市教师", BossSkill.Resist, HeroId.HuaRong, "曾长官",
                    "段景住盗来的照夜玉狮子马被曾头市夺去，曾家还编歌辱骂梁山。晁盖大怒，亲自领兵去打曾头市……",
                    "晁盖中了史文恭的毒箭，临终遗言：「若哪个捉得射死我的，便叫他做梁山泊主。」"),
                C("智赚玉麒麟", "第六十一回 吴用智赚玉麒麟 张顺夜闹金沙渡", "大名府篇", "snake",
                    J(HeroId.LuJunYi, HeroId.YanQing), J(HeroId.WuYong, HeroId.LiKui, HeroId.ZhangShun),
                    "李固", "卢府管家", BossSkill.Haste, HeroId.YanQing, "贾氏",
                    "吴用扮作算命先生，李逵扮成哑道童，到大名府给卢俊义算了一卦：百日之内有血光之灾，须往东南千里外躲避……",
                    "吴用让卢俊义在墙上题的四句诗：「芦花丛里一扁舟，俊杰俄从此地游。义士若能知此理，反躬逃难可无忧。」藏头正是「卢俊义反」。"),
                C("石秀跳楼", "第六十二回 放冷箭燕青救主 劫法场石秀跳楼", "大名府篇", "qingfeng",
                    J(HeroId.CaiFu, HeroId.CaiQing), J(HeroId.ShiXiu, HeroId.LuJunYi, HeroId.YanQing),
                    "梁中书", "大名府留守", BossSkill.Summon, HeroId.ShiXiu, "李固",
                    "卢俊义被李固陷害，要在大名府问斩。刽子手蔡福、蔡庆受了柴进的银子，刀下留情；楼上，一个人纵身跳了下来——",
                    "石秀独自跳楼劫法场，大喊「梁山泊好汉全伙在此」，吓得官兵四散。"),
                C("关胜议取梁山", "第六十三回 宋江兵打北京城 关胜议取梁山泊", "大名府篇", "snake",
                    J(HeroId.GuanSheng, HeroId.XuanZan, HeroId.HaoSiWen), J(HeroId.HuYanZhuo, HeroId.LinChong),
                    "关胜", "大刀", BossSkill.Resist, HeroId.HuYanZhuo, "蔡京",
                    "梁山兵围大名府。宣赞举荐关云长的后人关胜，关胜献计围魏救赵，直扑梁山老巢。",
                    "呼延灼诈降，月夜把关胜赚进了梁山的埋伏，关胜感宋江义气，归顺梁山。"),
                C("雪天擒索超", "第六十四回 呼延灼月夜赚关胜 宋公明雪天擒索超", "大名府篇", "zigzag",
                    J(HeroId.SuoChao), J(HeroId.YangZhi, HeroId.GuanSheng),
                    "索超", "急先锋", BossSkill.Haste, HeroId.YangZhi, "闻达",
                    "大雪纷飞，大名府急先锋索超又冲出城来。吴用笑道：此人性急，雪地里挖个陷坑就够了。",
                    "索超当年在大名府校场与杨志大战五十回合不分胜负，梁中书把两人都升了官。"),
                C("张顺请神医", "第六十五回 托塔天王梦中显圣 浪里白条水上报冤", "大名府篇", "marsh",
                    J(HeroId.AnDaoQuan, HeroId.WangDingLiu), J(HeroId.ZhangShun),
                    "张旺", "截江鬼", BossSkill.Summon, HeroId.ZhangShun, "孙五",
                    "宋江背上生了恶疮。张顺星夜过江去建康请神医安道全，船到江心，梢公张旺抽出了刀……",
                    "张顺被绑住丢下江，在水底咬断绳索逃生；王定六收留了他，后来一同报了仇。"),
                C("水火二将", "第六十七回 宋江赏马步三军 关胜降水火二将", "大名府篇", "river",
                    J(HeroId.ShanTingGui, HeroId.WeiDingGuo, HeroId.BaoXu, HeroId.JiaoTing), J(HeroId.GuanSheng, HeroId.LiKui),
                    "魏定国", "神火将", BossSkill.Summon, HeroId.GuanSheng, "单廷珪",
                    "凌州的圣水将、神火将来攻梁山。关胜领兵迎敌；李逵偷偷下山，在枯树山收了鲍旭、焦挺。",
                    "单廷珪会用「水浸兵」，魏定国会用「火攻兵」，都被关胜一一说降。"),
                C("夜打曾头市", "第六十八回 宋公明夜打曾头市 卢俊义活捉史文恭", "曾头市篇", "plain",
                    J(HeroId.YuBaoSi), J(HeroId.LuJunYi, HeroId.YanQing, HeroId.ShiQian, HeroId.DuanJingZhu),
                    "史文恭", "曾头市教师", BossSkill.Resist, HeroId.LuJunYi, "曾涂",
                    "险道神郁保四又劫了段景住的马送到曾头市。宋江再打曾头市，为晁盖报仇。",
                    "卢俊义活捉史文恭为晁盖报了仇，按遗言本当做寨主，他却坚决推让给宋江。"),
                C("东平府", "第六十九回 东平府误陷九纹龙 宋公明义释双枪将", "东平东昌篇", "qingfeng",
                    J(HeroId.DongPing), J(HeroId.ShiJin, HeroId.HuSanNiang, HeroId.WangYing, HeroId.GuDaSao),
                    "董平", "双枪将", BossSkill.Haste, HeroId.HuSanNiang, "程太守",
                    "宋江、卢俊义拈阄分兵，谁先打下城池谁做寨主。宋江打东平府，史进潜入城中却被识破，双枪将董平杀出城来……",
                    "董平的旗上写着：「英雄双枪将，风流万户侯。」"),
                C("东昌府", "第七十回 没羽箭飞石打英雄 宋公明弃粮擒壮士", "东平东昌篇", "river",
                    J(HeroId.ZhangQing, HeroId.GongWang, HeroId.DingDeSun, HeroId.HuangFuDuan), J(HeroId.RuanXiaoQi, HeroId.LuJunYi),
                    "张清", "没羽箭", BossSkill.Resist, HeroId.RuanXiaoQi, "龚旺",
                    "东昌府守将没羽箭张清，一颗颗石子飞出，连伤梁山十五员大将。吴用说：岸上打不过，就引他到水边。",
                    "张清被擒后，宋江惜才不杀，又让他举荐了兽医皇甫端。至此一百单八将全部聚齐。"),

                C("智扑擎天柱", "第七十四回 燕青智扑擎天柱 李逵寿张乔坐衙", "排座次篇", "jiuqu",
                    J(), J(HeroId.YanQing, HeroId.LiKui, HeroId.LuJunYi, HeroId.SongJiang),
                    "任原", "擎天柱", BossSkill.Resist, HeroId.YanQing, "部署",
                    "忠义堂上排定座次之后，浪子燕青独自下山，要在泰安州的相扑擂台上，摔下两年无敌的擎天柱任原。",
                    "忠义堂前天降石碣，上刻三十六天罡、七十二地煞的星号与姓名，这就是「梁山泊英雄排座次」。"),
                C("三败高俅", "第八十回 张顺凿漏海鳅船 宋江三败高太尉", "招安篇", "marsh",
                    J(), J(HeroId.ZhangShun, HeroId.RuanXiaoEr, HeroId.RuanXiaoWu, HeroId.RuanXiaoQi, HeroId.LiJun, HeroId.LinChong),
                    "高俅", "殿帅太尉", BossSkill.Summon, HeroId.ZhangShun, "童贯",
                    "高俅亲率大军和海鳅大船来攻梁山。张顺带水军潜到船底，一凿一凿……",
                    "高俅落水被擒，林冲恨不得生吃了他，宋江却亲自松绑——一心只想招安。"),
                C("征辽", "第八十三回 宋公明奉诏破大辽", "征辽篇", "plain",
                    J(), J(HeroId.GuanSheng, HeroId.LinChong, HeroId.QinMing, HeroId.HuYanZhuo, HeroId.DongPing, HeroId.HuaRong),
                    "兀颜光", "辽国统军", BossSkill.Resist, HeroId.GuanSheng, "辽主",
                    "梁山全伙受了招安，改称宋军。第一仗就是北上征辽，辽国统军兀颜光摆下了太乙混天象阵。",
                    "五虎上将关胜、林冲、秦明、呼延灼、董平，征辽时各领一军，杀得辽兵望风而逃。"),
                C("征田虎", "第九十一回起 征田虎", "征田虎篇", "zigzag",
                    J(), J(HeroId.GongSunSheng, HeroId.FanRui, HeroId.LuJunYi, HeroId.YanQing),
                    "乔道清", "幻魔君", BossSkill.Summon, HeroId.GongSunSheng, "田虎",
                    "河北田虎造反，他手下的幻魔君乔道清会妖法，宋军接连吃亏。公孙胜仗剑出阵。",
                    "征田虎、征王庆是一百二十回本才有的情节；这两路征战中，一百单八将无一阵亡。"),
                C("征王庆", "第一百零一回起 征王庆", "征王庆篇", "bailong",
                    J(), J(HeroId.LiJun, HeroId.ZhangHeng, HeroId.ZhangShun, HeroId.TongWei, HeroId.TongMeng),
                    "王庆", "淮西反王", BossSkill.Haste, HeroId.LiJun, "段三娘",
                    "淮西王庆兵败，乘船逃命。江面上，混江龙李俊早已等候多时。",
                    "王庆是被李俊在江上擒获的。梁山水军从此威名远扬。"),
                C("涌金门", "第一百一十四回 宁海军宋江吊孝 涌金门张顺归神", "征方腊篇", "marsh",
                    J(), J(HeroId.ZhangShun, HeroId.ZhangHeng, HeroId.RuanXiaoQi, HeroId.LiJun),
                    "方天定", "方腊太子", BossSkill.Resist, HeroId.ZhangHeng, "石宝",
                    "征方腊打到杭州，城高难攻。张顺说：我从西湖水底潜到涌金门，放火为号——",
                    "张顺在涌金门被乱箭射死，魂魄附在哥哥张横身上，亲手杀了方天定。"),
                C("乌龙岭", "第一百一十七回 睦州城箭射邓元觉 乌龙岭神助宋公明", "征方腊篇", "jiuqu",
                    J(), J(HeroId.HuaRong, HeroId.XieZhen, HeroId.XieBao, HeroId.SongJiang, HeroId.WuYong),
                    "邓元觉", "宝光国师", BossSkill.Resist, HeroId.HuaRong, "方腊",
                    "乌龙岭山高路险，解珍、解宝扮作猎户攀崖探路，再没回来。睦州城下，宝光国师邓元觉挥着禅杖杀出……",
                    "征方腊一路，梁山好汉死伤大半，是全书最悲壮的一段。"),
                C("擒方腊", "第一百一十八回 卢俊义大战昱岭关 宋公明智取清溪洞", "征方腊篇", "ring",
                    J(), J(HeroId.LuZhiShen, HeroId.WuSong, HeroId.LuJunYi, HeroId.SongJiang, HeroId.YanQing),
                    "方腊", "圣公", BossSkill.Summon, HeroId.LuZhiShen, "方杰",
                    "清溪洞破，方腊只身逃进深山，迎面撞上一个胖大和尚，一禅杖打翻在地。",
                    "鲁智深生擒方腊，应了智真长老当年的偈语「逢夏而擒，遇腊而执」。后来他在六和寺听潮信圆寂。"),
                C("蓼儿洼", "第一百二十回 宋公明神聚蓼儿洼 徽宗帝梦游梁山泊", "终章", "marsh",
                    J(), J(HeroId.SongJiang, HeroId.LiKui, HeroId.WuYong, HeroId.HuaRong, HeroId.LuJunYi),
                    "蔡京", "太师", BossSkill.Summon, HeroId.LiKui, "高俅",
                    "征方腊归来，奸臣蔡京、高俅、童贯、杨戬容不下梁山好汉，一杯御赐药酒送到了楚州。替天行道的大旗，最后一次升起。",
                    "宋江饮了毒酒，怕李逵再反坏了梁山的忠义之名，叫来李逵同饮，一起葬在楚州蓼儿洼。吴用、花荣也在墓前自缢。"),
            };
            int n = list.Length;
            for (int i = 0; i < n; i++)
            {
                list[i].Index = i;
                float k = Math.Min(1f, i / 45f);
                list[i].RivalSkill = 0.25f + 0.65f * k;
                list[i].RivalIncome = 0.6f + 0.4f * k;
                list[i].RivalFoeHp = 1f;
                list[i].FoeHp = 1f + 0.35f * k;
            }
            return list;
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
            var skill = (BossSkill)rng.Range(0, 3);
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
                Boss = new BossDef { Name = src.Name, Title = src.Title, Skill = skill, Nemesis = nemesis, SkillText = SkillText(skill, nemesis) },
                RivalName = rivalPick.Nick + rivalPick.Name,
                RivalSkill = 0.85f + 0.15f * k,
                RivalIncome = 0.95f + 0.15f * k,
                FoeHp = 1.25f + 0.3f * k,
            };
        }

        /// <summary>同一回换一池好汉（地图、头目、对手不变）。</summary>
        public ChapterDef WithPool(params HeroId[] pool)
        {
            var c = (ChapterDef)MemberwiseClone();
            c.Pool = pool;
            return c;
        }
    }
}
