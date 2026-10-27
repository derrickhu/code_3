using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>神秘商人（孙二娘）的货：主动道具战斗里拖上战场放出，被动道具开局就生效。</summary>
    public sealed class ShopItem
    {
        public int Id;
        public string Name, Desc, Rarity, Icon;
        public bool Active;
        public int Price;
        public Func<int> Count;
        public Action Give, Take;

        public bool Owned() => Count() >= (Id == 8 ? 2 : 1);

        public static readonly string[] Rarities = { "稀有", "卓越", "史诗", "传说" };

        /// <summary>稀有绿、卓越蓝、史诗紫、传说橙，和圆底同色系。</summary>
        public static Color ColorOf(string rarity) => rarity switch
        {
            "传说" => Pal.Hex("F08A24"),
            "史诗" => Pal.Hex("B45CE0"),
            "卓越" => Pal.Hex("3FA9E0"),
            _ => Pal.Hex("6FBF3A"),
        };

        public Color RarityColor => ColorOf(Rarity);
        public Color DiscColor => Rarity switch
        {
            "传说" => Pal.Hex("F6C46B"),
            "史诗" => Pal.Hex("C9A6EE"),
            "卓越" => Pal.Hex("A4D8F2"),
            _ => Pal.Hex("C5E08A"),
        };

        /// <summary>抽奖权重：越稀有越难出。</summary>
        public int Weight => Rarity switch { "传说" => 12, "史诗" => 22, "卓越" => 30, _ => 36 };

        static int B(bool v) => v ? 1 : 0;

        public static readonly ShopItem[] All =
        {
            new ShopItem { Id = 0, Name = "生辰纲", Desc = "开局炊饼 +30，抢先聚义", Rarity = "卓越", Icon = "it_gang", Price = 40, Count = () => B(Save.D.gang), Give = () => Save.D.gang = true, Take = () => Save.D.gang = false },
            new ShopItem { Id = 1, Name = "替天行道旗", Desc = "全军伤害 +22%，全局生效", Rarity = "史诗", Icon = "it_flag", Price = 80, Count = () => B(Save.D.flag), Give = () => Save.D.flag = true, Take = () => Save.D.flag = false },
            new ShopItem { Id = 2, Name = "水泊", Desc = "官兵移速 -25%，全局生效", Rarity = "史诗", Icon = "it_marsh", Price = 70, Count = () => B(Save.D.marsh), Give = () => Save.D.marsh = true, Take = () => Save.D.marsh = false },
            new ShopItem { Id = 3, Name = "石碣", Desc = "官兵靠近宋江时，天降石碣砸下", Rarity = "传说", Icon = "it_stele", Price = 120, Count = () => B(Save.D.stele), Give = () => Save.D.stele = true, Take = () => Save.D.stele = false },
            new ShopItem { Id = 4, Name = "蒙汗药", Desc = "全场官兵昏睡 3 秒", Rarity = "史诗", Icon = "it_menghan", Active = true, Price = 60, Count = () => Save.D.menghan, Give = () => { Save.D.menghan++; Save.NoteActive(4); }, Take = () => { Save.D.menghan = Mathf.Max(0, Save.D.menghan - 1); Save.NoteActive(4); } },
            new ShopItem { Id = 5, Name = "天书", Desc = "场上最低级的兵升一级", Rarity = "传说", Icon = "it_tianshu", Active = true, Price = 150, Count = () => Save.D.tianshu, Give = () => { Save.D.tianshu++; Save.NoteActive(5); }, Take = () => { Save.D.tianshu = Mathf.Max(0, Save.D.tianshu - 1); Save.NoteActive(5); } },
            new ShopItem { Id = 6, Name = "孙二娘包子", Desc = "五成半回一颗心，否则掉一颗", Rarity = "卓越", Icon = "bun", Active = true, Price = 50, Count = () => Save.D.baozi, Give = () => { Save.D.baozi++; Save.NoteActive(6); }, Take = () => { Save.D.baozi = Mathf.Max(0, Save.D.baozi - 1); Save.NoteActive(6); } },
            new ShopItem { Id = 7, Name = "续命丹", Desc = "宋江多八颗心，能硬吃第一波", Rarity = "传说", Icon = "it_xuming", Price = 150, Count = () => B(Save.D.xuming), Give = () => Save.D.xuming = true, Take = () => Save.D.xuming = false },
            new ShopItem { Id = 8, Name = "方便铲", Desc = "开局送 2 把铲子，多开两块地", Rarity = "稀有", Icon = "shovel", Price = 50, Count = () => Save.D.shovels, Give = () => Save.D.shovels = 2, Take = () => Save.D.shovels = 0 },
            new ShopItem { Id = 9, Name = "攻速符", Desc = "单位攻速 +40%，持续十二秒", Rarity = "史诗", Icon = "it_fu", Active = true, Price = 80, Count = () => Save.D.fu, Give = () => { Save.D.fu++; Save.NoteActive(9); }, Take = () => { Save.D.fu = Mathf.Max(0, Save.D.fu - 1); Save.NoteActive(9); } },
            new ShopItem { Id = 10, Name = "御敌千里", Desc = "远程单位攻击范围翻倍，全局生效", Rarity = "稀有", Icon = "it_far", Price = 90, Count = () => B(Save.D.far), Give = () => Save.D.far = true, Take = () => Save.D.far = false },
            new ShopItem { Id = 11, Name = "陷阱", Desc = "敌人掉下去一时会爬不出来", Rarity = "稀有", Icon = "it_trap", Active = true, Price = 35, Count = () => Save.D.trap, Give = () => { Save.D.trap++; Save.NoteActive(11); }, Take = () => { Save.D.trap = Mathf.Max(0, Save.D.trap - 1); Save.NoteActive(11); } },
        };
    }

    /// <summary>山寨主界面：座次、章节、出征、好汉谱、十字坡酒店。</summary>
    public sealed partial class HomeScreen : MonoBehaviour
    {
        RectTransform _root;
        Text _coins, _stamina, _loadout;
        int _selected;
        Text _chapterName, _chapterInfo, _rankName, _stars;
        RectTransform _badge, _badgeHolder;
        int _badgeTier = -1;
        RectTransform _shop;
        Text _shopCoins, _shopStamina;
        Action<int> _onFight;

        public static HomeScreen Open(Transform host, Action<int> onFight)
        {
            var go = new GameObject("Home");
            go.transform.SetParent(host, false);
            var h = go.AddComponent<HomeScreen>();
            h._onFight = onFight;
            h.Build();
            return h;
        }

        void OnDestroy()
        {
            if (_root) Destroy(_root.gameObject);
        }

        static readonly Color Edge = Pal.Hex("2E2119");
        static readonly Color StarOn = Pal.Hex("FFD84A");
        static readonly Color StarOff = Pal.Hex("6E5D45");

        RectTransform Corner(float x, float y, string name)
        {
            var rt = Ui.Rect(_root, name);
            Ui.Anchor(rt, x, y);
            return rt;
        }

        void Build()
        {
            _root = Ui.Rect(Ui.Root, "Home");
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            Save.RefreshShopDay();
            var bg = Ui.Rect(_root, "bg");
            bg.SetAsFirstSibling();
            Ui.PinBleed(bg);
            var bgImg = bg.gameObject.AddComponent<Image>();
            bgImg.sprite = Art.Img("home_bg2");
            bgImg.color = Color.white;
            bgImg.raycastTarget = false;

            var tl = Corner(0f, 1f, "topLeft");
            BuildHud(tl, out _coins, out _stamina, true);

            Ui.Tap(Ui.Icon(tl, "ui_gear", 100, new Vector2(80, -222)), ToggleSound);
            SideSign(tl, "ui_notice", "公告", new Vector2(80, -350), ShowNotice);
            SideSign(tl, "ui_notice", "好汉谱", new Vector2(80, -476), ShowCodex);
            SideSign(tl, "ui_notice", "回目", new Vector2(80, -602), ShowChapters);
            BuildGm(tl);

            var tr = Corner(1f, 1f, "topRight");
            var shopFace = Ui.Icon(tr, "merchant_bust", 150, new Vector2(-92, -238));
            Ui.Tap(shopFace, ShowShop);
            Ui.Stroke(Ui.Label(tr, "神秘商人", 34, Pal.Paper, new Vector2(-92, -328), new Vector2(200, 50), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 2.5f);

            var top = Corner(0.5f, 1f, "title");
            var title = Ui.Label(top, "营救宋江", 168, Pal.Hex("C0392B"), new Vector2(0, -296), new Vector2(900, 190), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(title, Pal.Hex("FFF6E2"), 5f);
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -8);
            Tween.Pop(title.transform, 0.5f);
            BuildRankPlate(top, new Vector2(0, -470));

            BuildHero(Corner(0.5f, 0.5f, "hero"));

            var bottom = Corner(0.5f, 0f, "bottom");
            _loadout = Ui.Label(bottom, "", 30, Pal.Willow, new Vector2(0, 680), new Vector2(940, 46));
            Ui.Stroke(_loadout, Pal.Hex("FFF6E2"), 2f);
            BuildStart(bottom);

            SideBigIcon(Corner(0f, 0f, "rankEntry"), "ui_rank", "排行榜", new Vector2(150, 230), ShowBoard);
            SideBigIcon(Corner(1f, 0f, "bagEntry"), "ui_bag", "武器背包", new Vector2(-150, 230), ShowArmory);

            Refresh();
            if (!Save.Volatile && Save.RollDaily()) ShowDaily();
        }

        /// <summary>标题下的座次牌：左嵌勋章，中写座次，右排星；整块点开排行榜。</summary>
        void BuildRankPlate(RectTransform parent, Vector2 pos)
        {
            const float w = 600f, h = 84f;
            var plate = Ui.Rect(parent, "rankPlate");
            plate.anchoredPosition = pos;
            plate.sizeDelta = new Vector2(w, h);
            Ui.Box(plate, new Vector2(w + 12, h + 12), new Vector2(0, -6), Pal.Shadow.A(0.45f), null, "shadow");
            Ui.Box(plate, new Vector2(w + 12, h + 12), Vector2.zero, Edge, null, "edge");
            Ui.Box(plate, new Vector2(w + 4, h + 4), Vector2.zero, Pal.Hex("C8962E"), null, "rim");
            var face = Ui.Box(plate, new Vector2(w - 4, h - 4), Vector2.zero, Pal.Hex("3B2A20"), null, "face");
            Ui.Box(plate, new Vector2(w - 40, 10), new Vector2(10, h / 2 - 14), Color.white.A(0.07f), null, "gloss");
            face.raycastTarget = true;
            Ui.Tap(face, ShowBoard);

            _rankName = Ui.Label(plate, "", 40, Pal.Hex("FFE7A8"), new Vector2(-w / 2 + 92 + 130, 0), new Vector2(260, h), TextAnchor.MiddleLeft, Fonts.Brush);
            _rankName.resizeTextForBestFit = true;
            _rankName.resizeTextMinSize = 28;
            _rankName.resizeTextMaxSize = 40;
            Ui.Stroke(_rankName, Edge, 2f);
            _stars = Ui.Label(plate, "", 50, StarOn, new Vector2(w / 2 - 26 - 140, -2), new Vector2(280, h), TextAnchor.MiddleRight);
            _stars.supportRichText = true;
            Ui.Stroke(_stars, Edge, 2.5f);

            _badgeHolder = Ui.Rect(plate, "badge");
            _badgeHolder.anchoredPosition = new Vector2(-w / 2 + 18, 4);
        }

        const float HudY = -92f;

        /// <summary>顶栏一行：头像、铜钱条、体力条（绿加号看广告补体力）。商人界面也复用。</summary>
        void BuildHud(RectTransform tl, out Text coins, out Text stamina, bool avatar)
        {
            if (avatar)
            {
                var face = Ui.Icon(tl, "ui_avatar", 124, new Vector2(80, HudY));
                Ui.Tap(face, () => Toast($"{Save.RankName} · 胜 {Save.D.wins} / 战 {Save.D.battles}"));
            }
            float x = avatar ? 186 : 40;
            HudPill(tl, x, 250, "coinBar");
            Ui.Icon(tl, "coin", 92, new Vector2(x + 14, HudY));
            coins = Ui.Label(tl, "", 44, Pal.Hex("FFE7A8"), new Vector2(x + 145, HudY), new Vector2(180, 76));
            coins.fontStyle = FontStyle.Bold;

            x += 280;
            HudPill(tl, x, 330, "staBar");
            Ui.Icon(tl, "ui_bolt", 72, new Vector2(x + 12, HudY));
            stamina = Ui.Label(tl, "", 38, Pal.Hex("FFE7A8"), new Vector2(x + 152, HudY), new Vector2(230, 76));
            stamina.fontStyle = FontStyle.Bold;
            stamina.supportRichText = true;
            var plus = Ui.Icon(tl, "ui_plus", 62, new Vector2(x + 316, HudY));
            Ui.Tap(plus, () =>
            {
                if (Stamina.Current >= Stamina.Max) { Nope("体力已满"); return; }
                AdStub.Show("补充体力", () => { Stamina.Add(5); Refresh(); });
            });
        }

        /// <summary>顶栏的深色圆角条，和座次牌同一套描边。left 是条的左端。</summary>
        static void HudPill(RectTransform parent, float left, float w, string name)
        {
            const float h = 72f;
            var c = new Vector2(left + w / 2, HudY);
            Ui.Box(parent, new Vector2(w + 10, h + 10), c, Edge, null, name + "Edge");
            Ui.Box(parent, new Vector2(w, h), c, Pal.Hex("3B2A20").A(0.95f), null, name);
            Ui.Box(parent, new Vector2(w - 36, 8), c + new Vector2(6, h / 2 - 12), Color.white.A(0.08f), null, name + "Gloss");
        }

        void SideSign(RectTransform parent, string img, string text, Vector2 pos, Action onClick)
        {
            var icon = Ui.Icon(parent, img, 118, pos);
            Ui.Tap(icon, onClick);
            var t = Ui.Label(icon.transform, text, text.Length > 2 ? 30 : 36, Pal.Hex("4A2E1C"), new Vector2(0, 6), new Vector2(110, 60), TextAnchor.MiddleCenter, Fonts.Brush);
            t.resizeTextForBestFit = false;
        }

        void SideBigIcon(RectTransform parent, string img, string text, Vector2 pos, Action onClick)
        {
            var icon = Ui.Icon(parent, img, 176, pos);
            Ui.Tap(icon, onClick);
            Ui.Stroke(Ui.Label(parent, text, 38, Pal.Paper, pos + new Vector2(0, -112), new Vector2(260, 56), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 3f);
        }

        /// <summary>中央：草丛上的小棋盘，宋字戴幞头，旁边一杆枪，下面墨字「梁山」；下方左右切换回目。</summary>
        void BuildHero(RectTransform c)
        {
            c.anchoredPosition = new Vector2(0, 150);
            var board = Ui.Icon(c, "home_board", 760, Vector2.zero, "board");
            board.rectTransform.sizeDelta = new Vector2(760, 410);
            var spear = Ui.Icon(c, "spear", 200, new Vector2(118, -20));
            spear.rectTransform.localRotation = Quaternion.Euler(0, 0, 62);
            var song = Ui.Label(c, "宋", 176, Pal.Ink, new Vector2(4, 18), new Vector2(220, 220), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(song, Pal.Hex("FFF6E2"), 2.5f);
            var hat = Ui.Icon(c, "hat", 112, new Vector2(2, 140));
            hat.rectTransform.localRotation = Quaternion.Euler(0, 0, 6);
            var lianshan = Ui.Label(c, "梁山", 120, Pal.Hex("9A5B2E").A(0.92f), new Vector2(-20, -150), new Vector2(400, 150), TextAnchor.MiddleCenter, Fonts.Brush);
            lianshan.rectTransform.localRotation = Quaternion.Euler(0, 0, -6);
            Bob(song.transform, 0f);
            Bob(hat.transform, 0.1f);

            _selected = Mathf.Clamp(Save.D.unlocked, 0, ChapterDef.StoryCount);
            var strip = Ui.Framed(c, new Vector2(560, 92), new Vector2(0, -280), Pal.Hex("F3E6C6"), Edge, 5f, "chapterStrip");
            _chapterName = Ui.Label(strip.transform, "", 44, Pal.Ink, new Vector2(0, 2), new Vector2(440, 88), TextAnchor.MiddleCenter, Fonts.Brush);
            _chapterName.resizeTextForBestFit = true;
            _chapterName.resizeTextMinSize = 26;
            _chapterName.resizeTextMaxSize = 44;
            _chapterName.verticalOverflow = VerticalWrapMode.Truncate;
            Ui.Tap(strip, () => { if (_selected >= ChapterDef.StoryCount) ShowTeam(); else ShowChapters(); });
            _chapterInfo = Ui.Label(c, "", 28, Pal.InkSoft, new Vector2(0, -355), new Vector2(760, 44));
            Ui.Stroke(_chapterInfo, Pal.Hex("FFF6E2"), 2f);
            ArrowBtn(c, "◀", new Vector2(-340, -280), -1);
            ArrowBtn(c, "▶", new Vector2(340, -280), 1);
        }

        void ArrowBtn(RectTransform parent, string glyph, Vector2 pos, int dir)
        {
            var b = Ui.Framed(parent, new Vector2(84, 84), pos, Pal.Hex("8C6B4E"), Edge, 5f);
            Ui.Label(b.transform, glyph, 40, Pal.Paper, Vector2.zero, new Vector2(84, 84));
            Ui.Tap(b, () => SelectChapter(_selected + dir));
        }

        void SelectChapter(int i)
        {
            if (i < 0 || i > ChapterDef.StoryCount) return;
            if (i > Save.D.unlocked) { Nope(i >= ChapterDef.StoryCount ? "打完蓼儿洼，聚义厅才开张" : "先赢下上一回才能解锁"); return; }
            _selected = i;
            RefreshChapter();
            Tween.Punch(_chapterName.transform, 0.15f);
        }

        void RefreshChapter()
        {
            if (_selected >= ChapterDef.StoryCount)
            {
                _chapterName.text = $"聚义厅 · 第{BattleView.ToCn(Save.D.endlessRound)}阵";
                var names = new List<string>();
                foreach (var id in Save.Team()) names.Add(HeroDef.Get(id).Name);
                _chapterInfo.text = $"阵容 {string.Join(" ", names)}　最好 {Save.D.endlessBest} 阵　点此换将";
                return;
            }
            var ch = ChapterDef.All[_selected];
            _chapterName.text = "第" + BattleView.ToCn(ch.Index + 1) + "回 · " + ch.Name;
            var nem = HeroDef.Get(ch.Boss.Nemesis).Name;
            bool fresh = ch.Joins.Length > 0 && !Save.Joined(ch.Joins[0]);
            _chapterInfo.text = fresh
                ? $"{ch.Act}　头目 {ch.Boss.Name}　克星 {nem}　胜后 {ch.Joins.Length} 人入伙"
                : $"{ch.Act}　头目 {ch.Boss.Name}　克星 {nem}　对手 {ch.RivalName}";
        }

        void BuildStart(RectTransform bottom)
        {
            var holder = Ui.Rect(bottom, "start");
            holder.anchoredPosition = new Vector2(0, 480);
            holder.sizeDelta = new Vector2(600, 170);
            Ui.Box(holder, new Vector2(620, 172), new Vector2(0, -10), Pal.Shadow.A(0.6f), null, "shadow");
            Ui.Box(holder, new Vector2(620, 172), Vector2.zero, Edge, null, "edge");
            var face = Ui.Box(holder, new Vector2(600, 152), Vector2.zero, Pal.Hex("B8342A"), null, "face");
            Ui.Box(holder, new Vector2(560, 116), Vector2.zero, Pal.Hex("D2493A"), null, "inner");
            Ui.Box(holder, new Vector2(540, 20), new Vector2(0, 42), Color.white.A(0.16f), null, "gloss");
            var label = Ui.Label(holder, "开始游戏", 64, Pal.Paper, new Vector2(-30, 0), new Vector2(400, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(label, Pal.Hex("6B1A12"), 3f);
            Ui.Icon(holder, "ui_bolt", 64, new Vector2(190, 0));
            Ui.Label(holder, "-" + Stamina.Cost, 28, Pal.Hex("FFE7A8"), new Vector2(226, -30), new Vector2(60, 40));
            var swords = Ui.Icon(holder, "ui_swords", 120, new Vector2(0, 112));
            Bob(swords.transform, 0.3f);
            face.raycastTarget = true;
            var b = face.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() => { Sfx.Play(Snd.Tap); Tween.Punch(holder, 0.12f); Fight(); });
            Breathe(holder);
        }

        static void Bob(Transform t, float phase)
        {
            var basePos = ((RectTransform)t).anchoredPosition;
            Tween.Run(t, 1.6f, k => { if (t) ((RectTransform)t).anchoredPosition = basePos + new Vector2(0, Mathf.Sin((k + phase) * Mathf.PI * 2) * 6f); },
                () => { if (t) { ((RectTransform)t).anchoredPosition = basePos; Bob(t, phase); } });
        }

        static void Breathe(Transform t)
        {
            Tween.Run(t, 1.4f, k => { if (t) t.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI * 2) * 0.03f); }, () => { if (t) Breathe(t); });
        }

        void ToggleSound()
        {
            Sfx.On = !Sfx.On;
            Save.D.sfx = Sfx.On;
            Save.Write();
            if (Sfx.On) Sfx.Play(Snd.Tap);
            Toast(Sfx.On ? "声音已打开" : "声音已关闭");
        }

        void ShowNotice()
        {
            Panel("公告", out var card, new Vector2(900, 1300));
            Ui.Label(card, "· 剧情五十七回，从史家村一路打到蓼儿洼；每赢一回，原著里这一回上山的好汉入伙\n" +
                "· 好汉谱收齐一百单八将，忠义堂排座次；之后聚义厅自选五将车轮战\n" +
                "· 座次：喽啰→小校→都头→提辖→步/水/马军头领→八骠骑→五虎将→地煞→天罡→天魁星，每阶分叁贰壹\n" +
                "· 胜加星，满五星升一小阶；败掉星，没星再败降一小阶，不跌出大阶\n" +
                "· 兵器分凡、利、宝、神四品，可交给任意已入伙的好汉，特性跟着兵器走；有渊源的好汉同场触发羁绊",
                36, Pal.Ink, new Vector2(0, 30), new Vector2(800, 900), TextAnchor.MiddleLeft);
        }

        void Fight()
        {
            if (!Stamina.Spend())
            {
                Confirm("体力不够了", "看段广告，补 5 点体力？", "看广告", () => AdStub.Show("补充体力", () => { Stamina.Add(5); Refresh(); }));
                return;
            }
            _onFight?.Invoke(_selected);
        }

        float _refreshT;

        void Update()
        {
            _refreshT -= Time.unscaledDeltaTime;
            if (_refreshT <= 0) { _refreshT = 1f; Refresh(); }
        }

        void Refresh()
        {
            _coins.text = Save.D.coins.ToString();
            _stamina.text = StaminaText();
            if (_shopCoins != null) _shopCoins.text = _coins.text;
            if (_shopStamina != null) _shopStamina.text = _stamina.text;
            var next = Save.D.PeekLoadout();
            string carried = BattleScreen.ItemSummary(next);
            string worn = BattleScreen.GearSummary(next);
            string line = carried;
            if (worn.Length > 0) line = carried.Length > 0 ? worn + "　" + carried : worn;
            _loadout.text = line.Length > 0 ? "下一战随身：" + line : "";
            _rankName.text = Save.RankName;
            _stars.text = StarRich(Save.D.stars);
            if (_badgeTier != Save.D.tier)
            {
                _badgeTier = Save.D.tier;
                if (_badge) Destroy(_badge.gameObject);
                _badge = Ui.RankBadge(_badgeHolder, Save.D.tier, Vector2.zero, 0.82f);
            }
            RefreshChapter();
        }

        static string StaminaText()
        {
            int s = Stamina.Current;
            if (s >= Stamina.Max) return $"{s} <size=30>/{Stamina.Max}</size>";
            int sec = Stamina.SecondsToNext;
            return $"{s} <size=30>/{Stamina.Max}  {sec / 60}:{sec % 60:00}</size>";
        }

        static string StarRich(int n) =>
            Ui.RankStars(Save.D.tier, n, ColorUtility.ToHtmlStringRGB(StarOn), ColorUtility.ToHtmlStringRGB(StarOff));

        // ─────────────────────────── 浮层 ───────────────────────────

        RectTransform Panel(string title, out RectTransform card, Vector2 size, Action onClose = null)
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.72f), true, "panel");
            card = Ui.WoodPanel(Ui.Fit(layer, size.y + 160), size, new Vector2(0, -20));
            Ui.Title(card, title, new Vector2(0, size.y / 2 - 10));
            var l = layer;
            Ui.CloseX(card, new Vector2(-size.x / 2 + 6, size.y / 2 - 4), () => { Destroy(l.gameObject); onClose?.Invoke(); });
            Tween.Pop(card, 0.25f);
            return layer;
        }

        static void Toast(string s) => Ui.Toast(s);
        static void Nope(string s) => Ui.Nope(s);

        void Confirm(string title, string body, string ok, Action onOk)
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.72f), true, "confirm");
            var card = Ui.WoodPanel(layer, new Vector2(840, 600), Vector2.zero);
            Ui.Title(card, title, new Vector2(0, 290), 80);
            Ui.Label(card, body, 42, Pal.Ink, new Vector2(0, 50), new Vector2(720, 200));
            Ui.CartoonBtn(card, ok, new Vector2(300, 110), new Vector2(-170, -170), Ui.BtnRed, () => { Destroy(layer.gameObject); onOk(); }, 46,
                ok.Contains("广告") ? "ui_video" : null);
            Ui.CartoonBtn(card, "算了", new Vector2(300, 110), new Vector2(170, -170), Ui.BtnDark, () => Destroy(layer.gameObject), 46);
            Tween.Pop(card, 0.25f);
        }
    }
}
