using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>神秘商人（孙二娘）的货：主动道具战斗里点着用，被动道具开局就生效。</summary>
    public sealed class ShopItem
    {
        public int Id;
        public string Name, Desc, Rarity, Icon;
        public bool Active;
        public int Price;
        public Func<int> Count;
        public Action Give, Take;

        public bool Owned() => Count() >= (Id == 8 ? 2 : 1);

        /// <summary>稀有绿、史诗紫、传说橙，和圆底同色系。</summary>
        public Color RarityColor => Rarity == "传说" ? Pal.Hex("F08A24") : Rarity == "史诗" ? Pal.Hex("B45CE0") : Pal.Hex("6FBF3A");
        public Color DiscColor => Rarity == "传说" ? Pal.Hex("F6C46B") : Rarity == "史诗" ? Pal.Hex("A98BE6") : Pal.Hex("C5E08A");

        static int B(bool v) => v ? 1 : 0;

        public static readonly ShopItem[] All =
        {
            new ShopItem { Id = 0, Name = "生辰纲", Desc = "开局炊饼 +30，抢先聚义", Rarity = "稀有", Icon = "it_gang", Price = 40, Count = () => B(Save.D.gang), Give = () => Save.D.gang = true, Take = () => Save.D.gang = false },
            new ShopItem { Id = 1, Name = "替天行道旗", Desc = "全军伤害 +22%，全局生效", Rarity = "史诗", Icon = "it_flag", Price = 80, Count = () => B(Save.D.flag), Give = () => Save.D.flag = true, Take = () => Save.D.flag = false },
            new ShopItem { Id = 2, Name = "水泊", Desc = "官兵移速 -25%，全局生效", Rarity = "史诗", Icon = "it_marsh", Price = 70, Count = () => B(Save.D.marsh), Give = () => Save.D.marsh = true, Take = () => Save.D.marsh = false },
            new ShopItem { Id = 3, Name = "石碣", Desc = "官兵靠近宋江时，天降石碣砸下", Rarity = "传说", Icon = "it_stele", Price = 120, Count = () => B(Save.D.stele), Give = () => Save.D.stele = true, Take = () => Save.D.stele = false },
            new ShopItem { Id = 4, Name = "蒙汗药", Desc = "全场官兵昏睡 3 秒", Rarity = "史诗", Icon = "it_menghan", Active = true, Price = 60, Count = () => Save.D.menghan, Give = () => Save.D.menghan++, Take = () => Save.D.menghan = Mathf.Max(0, Save.D.menghan - 1) },
            new ShopItem { Id = 5, Name = "天书", Desc = "场上最低级的字升一级", Rarity = "传说", Icon = "it_tianshu", Active = true, Price = 150, Count = () => Save.D.tianshu, Give = () => Save.D.tianshu++, Take = () => Save.D.tianshu = Mathf.Max(0, Save.D.tianshu - 1) },
            new ShopItem { Id = 6, Name = "孙二娘包子", Desc = "五成半回一颗心，否则掉一颗", Rarity = "稀有", Icon = "bun", Active = true, Price = 50, Count = () => Save.D.baozi, Give = () => Save.D.baozi++, Take = () => Save.D.baozi = Mathf.Max(0, Save.D.baozi - 1) },
            new ShopItem { Id = 7, Name = "续命丹", Desc = "我方宋江 +10 血，对手只 +6", Rarity = "传说", Icon = "it_xuming", Price = 150, Count = () => B(Save.D.xuming), Give = () => Save.D.xuming = true, Take = () => Save.D.xuming = false },
            new ShopItem { Id = 8, Name = "方便铲", Desc = "开局送 2 把铲子，多开两块地", Rarity = "稀有", Icon = "shovel", Price = 50, Count = () => Save.D.shovels, Give = () => Save.D.shovels = 2, Take = () => Save.D.shovels = 0 },
            new ShopItem { Id = 9, Name = "攻速符", Desc = "单位攻速 +40%，持续十二秒", Rarity = "史诗", Icon = "it_fu", Active = true, Price = 80, Count = () => Save.D.fu, Give = () => Save.D.fu++, Take = () => Save.D.fu = Mathf.Max(0, Save.D.fu - 1) },
            new ShopItem { Id = 10, Name = "御敌千里", Desc = "远程单位攻击范围翻倍，全局生效", Rarity = "稀有", Icon = "it_far", Price = 90, Count = () => B(Save.D.far), Give = () => Save.D.far = true, Take = () => Save.D.far = false },
            new ShopItem { Id = 11, Name = "陷阱", Desc = "敌人掉下去一时会爬不出来", Rarity = "稀有", Icon = "it_trap", Active = true, Price = 35, Count = () => Save.D.trap, Give = () => Save.D.trap++, Take = () => Save.D.trap = Mathf.Max(0, Save.D.trap - 1) },
        };
    }

    /// <summary>山寨主界面：座次、章节、出征、好汉谱、十字坡酒店。</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        RectTransform _root;
        Text _coins, _stamina, _loadout;
        int _selected;
        Text _chapterName, _chapterInfo, _rankName, _stars;
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
        static readonly Color StarOff = Pal.Hex("4A4232");

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
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = Art.Img("home_bg2");
            bg.color = Color.white;

            var tl = Corner(0f, 1f, "topLeft");
            BuildHud(tl, out _coins, out _stamina, true);

            Ui.Tap(Ui.Icon(tl, "ui_gear", 104, new Vector2(84, -236)), ToggleSound);
            SideSign(tl, "ui_notice", "公告", new Vector2(84, -366), ShowNotice);
            SideSign(tl, "ui_notice", "好汉谱", new Vector2(84, -496), ShowCodex);

            var tr = Corner(1f, 1f, "topRight");
            var shopFace = Ui.Icon(tr, "merchant_bust", 150, new Vector2(-96, -250));
            Ui.Tap(shopFace, ShowShop);
            Ui.Stroke(Ui.Label(tr, "神秘商人", 34, Pal.Paper, new Vector2(-96, -340), new Vector2(200, 50), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 2.5f);

            var top = Corner(0.5f, 1f, "title");
            var title = Ui.Label(top, "营救宋江", 168, Pal.Hex("C0392B"), new Vector2(40, -330), new Vector2(900, 210), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(title, Pal.Hex("FFF6E2"), 5f);
            title.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(4, -8);
            _rankName = Ui.Label(top, "", 44, Pal.Ink, new Vector2(40, -448), new Vector2(500, 60));
            Ui.Stroke(_rankName, Pal.Hex("FFF6E2"), 2f);
            _stars = Ui.Label(top, "", 112, StarOn, new Vector2(40, -545), new Vector2(760, 130));
            _stars.supportRichText = true;
            Ui.Stroke(_stars, Edge, 4f);
            var rankHit = Ui.Box(top, new Vector2(700, 260), new Vector2(40, -490), new Color(0, 0, 0, 0), null, "rankHit");
            Ui.Tap(rankHit, ShowBoard);
            Tween.Pop(title.transform, 0.5f);

            BuildHero(Corner(0.5f, 0.5f, "hero"));

            var bottom = Corner(0.5f, 0f, "bottom");
            _loadout = Ui.Label(bottom, "", 30, Pal.Willow, new Vector2(0, 700), new Vector2(940, 50));
            Ui.Stroke(_loadout, Pal.Hex("FFF6E2"), 2f);
            BuildStart(bottom);

            SideBigIcon(Corner(0f, 0f, "rankEntry"), "ui_rank", "排行榜", new Vector2(150, 230), ShowBoard);
            SideBigIcon(Corner(1f, 0f, "bagEntry"), "ui_bag", "武器背包", new Vector2(-150, 230), ShowArmory);
            Ui.Label(bottom, "垂直切片 Demo · 题材原创 · 字体 OFL", 22, Pal.Paper.A(0.55f), new Vector2(0, 42), new Vector2(900, 34));

            Refresh();
            if (!Save.Volatile && Save.RollDaily()) ShowDaily();
        }

        /// <summary>左上角：头像、铜钱条、体力条（绿加号看广告补体力）。商人界面也复用。</summary>
        void BuildHud(RectTransform tl, out Text coins, out Text stamina, bool avatar)
        {
            if (avatar)
            {
                var face = Ui.Icon(tl, "ui_avatar", 132, new Vector2(84, -96));
                Ui.Tap(face, () => Toast($"{Save.RankName} · 胜 {Save.D.wins} / 战 {Save.D.battles}"));
            }
            float x = avatar ? 330 : 250;
            Ui.Box(tl, new Vector2(290, 80), new Vector2(x + 6, -92), Pal.Hex("3B2A20").A(0.92f), null, "coinBar");
            Ui.Icon(tl, "coin", 96, new Vector2(x - 138, -92));
            coins = Ui.Label(tl, "", 46, Pal.Hex("FFE7A8"), new Vector2(x + 30, -92), new Vector2(220, 80));
            coins.fontStyle = FontStyle.Bold;

            Ui.Box(tl, new Vector2(290, 80), new Vector2(x + 6, -198), Pal.Hex("3B2A20").A(0.92f), null, "staBar");
            Ui.Icon(tl, "ui_bolt", 80, new Vector2(x - 132, -198));
            stamina = Ui.Label(tl, "", 40, Pal.Hex("FFE7A8"), new Vector2(x - 6, -198), new Vector2(220, 80));
            stamina.fontStyle = FontStyle.Bold;
            stamina.supportRichText = true;
            var plus = Ui.Icon(tl, "ui_plus", 66, new Vector2(x + 126, -198));
            Ui.Tap(plus, () =>
            {
                if (Stamina.Current >= Stamina.Max) { Toast("体力已满"); return; }
                AdStub.Show("补充体力", () => { Stamina.Add(5); Refresh(); });
            });
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

            _selected = Mathf.Clamp(Save.D.unlocked, 0, ChapterDef.All.Length - 1);
            var strip = Ui.Framed(c, new Vector2(560, 92), new Vector2(0, -290), Pal.Hex("F3E6C6"), Edge, 5f, "chapterStrip");
            _chapterName = Ui.Label(strip.transform, "", 44, Pal.Ink, new Vector2(0, 2), new Vector2(440, 88), TextAnchor.MiddleCenter, Fonts.Brush);
            _chapterInfo = Ui.Label(c, "", 28, Pal.InkSoft, new Vector2(0, -370), new Vector2(760, 44));
            Ui.Stroke(_chapterInfo, Pal.Hex("FFF6E2"), 2f);
            ArrowBtn(c, "◀", new Vector2(-340, -290), -1);
            ArrowBtn(c, "▶", new Vector2(340, -290), 1);
        }

        void ArrowBtn(RectTransform parent, string glyph, Vector2 pos, int dir)
        {
            var b = Ui.Framed(parent, new Vector2(84, 84), pos, Pal.Hex("8C6B4E"), Edge, 5f);
            Ui.Label(b.transform, glyph, 40, Pal.Paper, Vector2.zero, new Vector2(84, 84));
            Ui.Tap(b, () => SelectChapter(_selected + dir));
        }

        void SelectChapter(int i)
        {
            if (i < 0 || i >= ChapterDef.All.Length) return;
            if (i > Save.D.unlocked) { Toast("先赢下上一回才能解锁"); return; }
            _selected = i;
            RefreshChapter();
            Tween.Punch(_chapterName.transform, 0.15f);
        }

        void RefreshChapter()
        {
            var ch = ChapterDef.All[_selected];
            _chapterName.text = "第" + BattleView.ToCn(ch.Index + 1) + "回 · " + ch.Name;
            var nem = HeroDef.Get(ch.Boss.Nemesis).Name;
            _chapterInfo.text = $"头目 {ch.Boss.Name}　克星 {nem}　对手 {ch.RivalName}";
        }

        void BuildStart(RectTransform bottom)
        {
            var holder = Ui.Rect(bottom, "start");
            holder.anchoredPosition = new Vector2(0, 520);
            holder.sizeDelta = new Vector2(600, 170);
            Ui.Box(holder, new Vector2(620, 172), new Vector2(0, -10), Pal.Shadow.A(0.6f), null, "shadow");
            Ui.Box(holder, new Vector2(620, 172), Vector2.zero, Edge, null, "edge");
            var face = Ui.Box(holder, new Vector2(600, 152), Vector2.zero, Pal.Hex("B8342A"), null, "face");
            Ui.Box(holder, new Vector2(560, 116), Vector2.zero, Pal.Hex("D2493A"), null, "inner");
            Ui.Box(holder, new Vector2(540, 20), new Vector2(0, 42), Color.white.A(0.16f), null, "gloss");
            var label = Ui.Label(holder, "开始游戏", 64, Pal.Paper, new Vector2(-30, 0), new Vector2(400, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(label, Pal.Hex("6B1A12"), 3f);
            Ui.Icon(holder, "ui_bolt", 64, new Vector2(190, 0));
            Ui.Label(holder, "-1", 28, Pal.Hex("FFE7A8"), new Vector2(226, -30), new Vector2(60, 40));
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
            Toast(Sfx.On ? "音效已打开" : "音效已关闭");
        }

        void ShowNotice()
        {
            Panel("公告", out var card, new Vector2(900, 1100));
            Ui.Label(card, "· 胜一局升一星，满五星再胜晋升座次\n· 败一局掉一星，没星再败就掉一阶\n· 每天按昨日座次发铜钱，排行榜里领\n· 每局结束孙二娘摆摊，道具仅当天有效\n· 打仗缴获兵器，装备给好汉，越叠越猛",
                38, Pal.Ink, new Vector2(0, 40), new Vector2(800, 600), TextAnchor.MiddleLeft);
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
            _loadout.text = Save.D.HasAnyItem ? "下一战随身：" + BattleScreen.ItemSummary(Save.D.PeekLoadout()) : "";
            _rankName.text = Save.RankName;
            _stars.text = StarRich(Save.D.stars);
            RefreshChapter();
        }

        static string StaminaText()
        {
            int s = Stamina.Current;
            if (s >= Stamina.Max) return $"{s} <size=30>/{Stamina.Max}</size>";
            int sec = Stamina.SecondsToNext;
            return $"{s} <size=30>/{Stamina.Max}  {sec / 60}:{sec % 60:00}</size>";
        }

        static string StarRich(int n)
        {
            string on = ColorUtility.ToHtmlStringRGB(StarOn), off = ColorUtility.ToHtmlStringRGB(StarOff);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < RankRules.Stars; i++) sb.Append($"<color=#{(i < n ? on : off)}>★</color>");
            return sb.ToString();
        }

        // ─────────────────────────── 浮层 ───────────────────────────

        RectTransform Panel(string title, out RectTransform card, Vector2 size)
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.7f), true, "panel");
            var img = Ui.Box(layer, size, new Vector2(0, 20), Pal.Paper);
            card = img.rectTransform;
            Ui.Label(card, title, 100, Pal.Ink, new Vector2(0, size.y / 2 - 100), new Vector2(size.x - 40, 140), TextAnchor.MiddleCenter, Fonts.Brush);
            var l = layer;
            Ui.Btn(card, "关闭", new Vector2(300, 110), new Vector2(0, -size.y / 2 + 90), Pal.InkSoft, () => Destroy(l.gameObject), 44);
            Tween.Pop(card, 0.25f);
            return layer;
        }

        public void ShowCodex()
        {
            Panel("好 汉 谱", out var card, new Vector2(1000, 1640));
            int got = 0;
            foreach (var d in HeroDef.All) if (Save.D.heroes.Contains((int)d.Id)) got++;
            Ui.Label(card, $"已入谱 {got}/{HeroDef.All.Length}  · 凑齐名字即可入谱", 34, Pal.InkSoft, new Vector2(0, 600), new Vector2(900, 50));

            var detail = Ui.Label(card, "点好汉看详情", 34, Pal.InkSoft, new Vector2(0, -520), new Vector2(920, 150));
            for (int i = 0; i < HeroDef.All.Length; i++)
            {
                var d = HeroDef.All[i];
                bool have = Save.D.heroes.Contains((int)d.Id);
                var pos = new Vector2(-345 + (i % 4) * 230, 470 - (i / 4) * 230);
                var face = Ui.Box(card, new Vector2(215, 215), pos, have ? Pal.TileHero : Pal.PaperDark);
                face.raycastTarget = true;
                var b = face.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                if (have)
                {
                    Ui.Label(face.transform, d.Name, d.Name.Length > 2 ? 58 : 76, Pal.Ink, new Vector2(0, 20), new Vector2(210, 110), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(face.transform, d.Nick, 28, Pal.RedDeep, new Vector2(0, -62), new Vector2(210, 40));
                }
                else
                {
                    Ui.Label(face.transform, d.Hidden ? "隐" : "？", 80, Pal.InkSoft.A(0.4f), new Vector2(0, 10), new Vector2(210, 120), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(face.transform, d.Hidden ? "隐藏好汉" : FirstChapter(d.Id), 26, Pal.InkSoft.A(0.7f), new Vector2(0, -62), new Vector2(210, 40));
                }
                b.onClick.AddListener(() =>
                {
                    Sfx.Play(Snd.Tap);
                    Tween.Punch(face.transform, 0.15f);
                    detail.text = have
                        ? $"{d.Nick} {d.Name}：{d.Skill}\n大招·{d.Ult}　「{d.Quote}」"
                        : d.Hidden ? "据说在某一回凑出他的名字……他卖的炊饼特别香。" : $"在「{FirstChapter(d.Id)}」把 {d.Name.Length} 个金字按顺序摆好即可入谱";
                });
            }
        }

        static string FirstChapter(HeroId id)
        {
            foreach (var ch in ChapterDef.All)
                if (Array.IndexOf(ch.Pool, id) >= 0) return ch.Name;
            return "";
        }

        static readonly string[] MerchantLines = { "诚信为本", "好货不等人", "客官里边请", "今日货今日用", "包子刚出笼" };
        const int ActiveSlots = 4;
        const int BagCols = 7;

        /// <summary>神秘商人：结算后自动弹出，也能从主界面点孙二娘进来。</summary>
        public void ShowShop()
        {
            Save.RefreshShopDay();
            if (_shop) Destroy(_shop.gameObject);
            _shop = Ui.Fill(_root, Pal.Hex("120D0A").A(0.82f), true, "shop");
            var tl = Ui.Rect(_shop, "hud");
            Ui.Anchor(tl, 0f, 1f);
            BuildHud(tl, out _shopCoins, out _shopStamina, true);

            var body = Ui.Rect(_shop, "body");
            body.sizeDelta = new Vector2(960, 1500);
            body.anchoredPosition = new Vector2(0, -40);
            var bust = Ui.Icon(body, "merchant_bust", 230, new Vector2(30, 700));
            Bob(bust.transform, 0f);
            var bubble = Ui.Framed(body, new Vector2(230, 84), new Vector2(290, 760), Pal.Paper, Edge, 3f, "bubble");
            var say = Ui.Label(bubble.transform, MerchantLines[UnityEngine.Random.Range(0, MerchantLines.Length)], 32, Pal.Ink, Vector2.zero, new Vector2(220, 80));

            Ui.Framed(body, new Vector2(940, 1110), new Vector2(0, 45), Pal.Hex("8C6B4E"), Edge, 7f, "panel");
            Ui.Box(body, new Vector2(880, 1020), new Vector2(0, 25), Pal.Hex("5E4532"), null, "innerEdge");
            Ui.Box(body, new Vector2(866, 1006), new Vector2(0, 25), Pal.Hex("C9B48C"), null, "inner");
            var title = Ui.Label(body, "神秘商人", 92, Pal.Hex("F7A928"), new Vector2(0, 590), new Vector2(700, 130), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(title, Edge, 4f);
            var close = Ui.Icon(body, "ui_close", 116, new Vector2(-445, 600));
            Ui.Tap(close, () => { if (_shop) Destroy(_shop.gameObject); _shop = null; _shopCoins = _shopStamina = null; });

            var content = Ui.Rect(body, "content");
            void Rebuild(string line)
            {
                for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
                if (line != null) { say.text = line; Tween.Punch(bubble.transform.parent, 0.2f); }
                BuildOffers(content, Rebuild);
                BuildBag(content, Rebuild);
                Refresh();
            }
            Rebuild(null);

            var tip = Ui.Label(_shop, "tip:道具仅当天有效", 42, Pal.Paper, new Vector2(0, -620), new Vector2(800, 60));
            tip.fontStyle = FontStyle.Bold;
            var ad = Ui.Framed(_shop, new Vector2(460, 84), new Vector2(0, -720), Pal.Hex("E8573F"), Edge, 4f, "framedAd");
            Ui.Stroke(Ui.Label(ad.transform, "▶ 看广告 白拿包子", 34, Pal.Paper, Vector2.zero, new Vector2(440, 80)), Pal.Hex("6B1A12"), 2f);
            Ui.Tap(ad, () => AdStub.Show("免费道具", () =>
            {
                Save.D.baozi = Mathf.Min(3, Save.D.baozi + 1);
                Save.Write();
                Toast("成功获得孙二娘包子");
                Rebuild("客官拿好");
            }));
            Tween.Pop(body, 0.3f);
        }

        void BuildOffers(RectTransform content, Action<string> rebuild)
        {
            for (int i = 0; i < Save.D.shopOffer.Count; i++)
            {
                var it = ShopItem.All[Save.D.shopOffer[i]];
                float y = 410 - i * 196;
                var card = Ui.Framed(content, new Vector2(820, 172), new Vector2(0, y), Pal.Hex("F2E7CA"), Pal.Hex("5A4632"), 5f, "offer");
                var t = card.transform;
                Ui.Box(t, new Vector2(148, 148), new Vector2(-322, 0), Edge, Art.Circle, "discEdge").type = Image.Type.Simple;
                Ui.Box(t, new Vector2(138, 138), new Vector2(-322, 0), it.DiscColor, Art.Circle, "disc").type = Image.Type.Simple;
                Ui.Icon(t, it.Icon, 104, new Vector2(-322, 0));
                Ui.Stroke(Ui.Label(t, it.Rarity, 38, it.RarityColor, new Vector2(-350, 62), new Vector2(120, 50), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 2.5f);
                var name = Ui.Label(t, it.Name, 40, Pal.Ink, new Vector2(-60, 44), new Vector2(300, 56), TextAnchor.MiddleLeft);
                name.fontStyle = FontStyle.Bold;
                Ui.Label(t, (it.Active ? "主动:" : "被动:") + it.Desc, 28, Pal.Hex("4A3A2C"), new Vector2(-50, -24), new Vector2(320, 90), TextAnchor.UpperLeft)
                    .lineSpacing = 1.05f;

                bool sold = Save.D.shopSold.Contains(it.Id) || it.Owned();
                var btn = Ui.Framed(t, new Vector2(212, 104), new Vector2(276, 0), sold ? Pal.Hex("4D4642") : Pal.Hex("F6C531"), Edge, 5f, "framedBuy");
                if (sold)
                {
                    Ui.Label(btn.transform, "已售完", 44, Pal.Paper, Vector2.zero, new Vector2(200, 100)).fontStyle = FontStyle.Bold;
                    continue;
                }
                Ui.Box(btn.transform, new Vector2(196, 14), new Vector2(0, 34), Color.white.A(0.35f), null, "gloss");
                Ui.Icon(btn.transform, "coin", 70, new Vector2(-52, 0));
                var price = Ui.Label(btn.transform, it.Price.ToString(), 46, Edge, new Vector2(34, 0), new Vector2(120, 100));
                price.fontStyle = FontStyle.Bold;
                bool afford = Save.D.coins >= it.Price;
                if (!afford) price.color = Pal.Hex("A33A2A");
                Ui.Tap(btn, () =>
                {
                    if (Save.D.coins < it.Price) { Toast("铜钱不够，打一局再来"); return; }
                    if (it.Active && it.Count() == 0 && ActiveKinds() >= ActiveSlots) { Toast($"主动道具最多带 {ActiveSlots} 种"); return; }
                    Save.D.coins -= it.Price;
                    it.Give();
                    Save.D.shopSold.Add(it.Id);
                    Save.Write();
                    Sfx.Play(Snd.Merge);
                    Toast("成功获得" + it.Name);
                    rebuild("客官好眼力！");
                });
            }
        }

        static int ActiveKinds()
        {
            int n = 0;
            foreach (var it in ShopItem.All) if (it.Active && it.Count() > 0) n++;
            return n;
        }

        /// <summary>我的道具：主动一行（战斗里最多四种，后三格木板封着）、被动一行；今天买的可以点红减号退货。</summary>
        void BuildBag(RectTransform content, Action<string> rebuild)
        {
            Ui.Label(content, "我的道具:", 34, Pal.Ink, new Vector2(-262, -186), new Vector2(300, 50), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            Ui.Framed(content, new Vector2(790, 248), new Vector2(26, -330), Pal.Hex("3A2A20"), Pal.Hex("5A4632"), 5f, "bag");
            Ui.Label(content, "主\n动", 30, Pal.Hex("3A2A20"), new Vector2(-400, -270), new Vector2(44, 90)).fontStyle = FontStyle.Bold;
            Ui.Label(content, "被\n动", 30, Pal.Hex("3A2A20"), new Vector2(-400, -390), new Vector2(44, 90)).fontStyle = FontStyle.Bold;
            for (int row = 0; row < 2; row++)
            {
                var items = new List<ShopItem>();
                foreach (var it in ShopItem.All) if (it.Active == (row == 0) && it.Count() > 0) items.Add(it);
                for (int c = 0; c < BagCols; c++)
                {
                    var pos = new Vector2(-304 + c * 110, row == 0 ? -270 : -390);
                    Ui.Box(content, new Vector2(102, 102), pos, Pal.Hex("53392A"), null, "cell").pixelsPerUnitMultiplier = 1.4f;
                    if (c >= items.Count) continue;
                    var it = items[c];
                    var cell = Ui.Box(content, new Vector2(96, 96), pos, Pal.Hex("F2E7CA"), null, "item");
                    cell.pixelsPerUnitMultiplier = 1.4f;
                    Ui.Icon(cell.transform, it.Icon, 80, Vector2.zero);
                    int n = it.Count();
                    if (n > 1) Ui.Stroke(Ui.Label(cell.transform, "×" + n, 26, Pal.Paper, new Vector2(22, -30), new Vector2(60, 34)), Edge, 2f);
                    Ui.Tap(cell, () => Toast(it.Name + "：" + it.Desc));
                    if (!Save.D.shopSold.Contains(it.Id)) continue;
                    var minus = Ui.Box(cell.transform, new Vector2(42, 42), new Vector2(42, 42), Pal.Hex("D7362B"), Art.Circle, "minus");
                    minus.type = Image.Type.Simple;
                    Ui.Box(minus.transform, new Vector2(22, 7), Vector2.zero, Color.white, Art.White, "bar").type = Image.Type.Simple;
                    Ui.Tap(minus, () =>
                    {
                        it.Take();
                        Save.D.shopSold.Remove(it.Id);
                        Save.D.coins += it.Price;
                        Save.Write();
                        Toast($"已退回{it.Name}，铜钱 +{it.Price}");
                        rebuild("下回再来哟");
                    });
                }
            }
            var plank = Ui.Icon(content, "ui_plank", 360, new Vector2(-304 + 5 * 110, -270), "plank");
            plank.rectTransform.sizeDelta = new Vector2(350, 120);
            plank.preserveAspect = false;
            plank.rectTransform.localRotation = Quaternion.Euler(0, 0, -2);
            Ui.Tap(plank, () => Toast($"战斗里最多带 {ActiveSlots} 种主动道具"));
        }

        void ShowDaily()
        {
            int coins = Save.DailyCoins;
            string stars = "";
            for (int i = 0; i < RankRules.Stars; i++) stars += i < Save.D.claimStars ? "★" : "☆";
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.72f), true, "daily");
            var card = Ui.Box(layer, new Vector2(860, 720), Vector2.zero, Pal.Paper).rectTransform;
            Ui.Label(card, "昨日座次", 44, Pal.InkSoft, new Vector2(0, 250), new Vector2(760, 70));
            Ui.Label(card, Save.Tiers[Mathf.Clamp(Save.D.claimTier, 0, Save.Tiers.Length - 1)], 88, Pal.RedDeep, new Vector2(0, 140), new Vector2(760, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(card, stars, 56, Pal.Gold, new Vector2(0, 40), new Vector2(760, 80));
            Ui.Label(card, "赏", 72, Pal.Red, new Vector2(-180, -80), new Vector2(160, 100), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(card, "铜钱 " + coins, 56, Pal.Gold, new Vector2(80, -80), new Vector2(420, 100), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Btn(card, "领取", new Vector2(420, 120), new Vector2(0, -240), Pal.Red, () =>
            {
                if (!Save.D.claimTaken)
                {
                    Save.D.coins += coins;
                    Save.D.claimTaken = true;
                    Save.Write();
                    Refresh();
                    Sfx.Play(Snd.Rain);
                }
                Destroy(layer.gameObject);
            }, 52, Pal.Paper, Fonts.Brush);
            Tween.Pop(card, 0.28f);
        }

        public void ShowArmory()
        {
            var layer = Panel("兵器", out var card, new Vector2(1000, 1640));
            Ui.Label(card, "一阶伤害 +45%，每多一阶再 +18%，五阶封顶。最多同时三件。", 30, Pal.InkSoft, new Vector2(0, 640), new Vector2(920, 60));
            Ui.Btn(card, "一键回收", new Vector2(260, 78), new Vector2(300, 560), Pal.Gold, () =>
            {
                int got = RecycleSpare();
                if (got <= 0) { Toast("装备中的那一件先留着"); return; }
                Destroy(layer.gameObject);
                ShowArmory();
            }, 34, Pal.Paper);
            int row = 0;
            foreach (var d in HeroDef.All)
            {
                if (string.IsNullOrEmpty(d.Weapon)) continue;
                int n = Save.WeaponCount((int)d.Id);
                if (n <= 0) continue;
                bool on = Save.D.armed.Contains((int)d.Id);
                var pos = new Vector2(0, 450 - row * 112);
                row++;
                var face = Ui.Box(card, new Vector2(900, 100), pos, on ? Pal.TileHero : Pal.Tile);
                Ui.Label(face.transform, d.Name, 40, Pal.Ink, new Vector2(-300, 16), new Vector2(220, 50), TextAnchor.MiddleLeft, Fonts.Brush);
                int pct = Mathf.RoundToInt((HeroDef.ArmedMul(n) - 1f) * 100f);
                Ui.Label(face.transform, d.Weapon + "  " + Mathf.Min(5, n) + "/5  伤害+" + pct + "%" + (on ? "  用" + d.WeaponTag : ""), 28, on ? Pal.Hex("3D7BBF") : Pal.InkSoft, new Vector2(-250, -24), new Vector2(420, 40), TextAnchor.MiddleLeft);
                var id = d.Id;
                Ui.Btn(face.transform, on ? "卸下" : "装备", new Vector2(160, 72), new Vector2(180, 0), on ? Pal.InkSoft : Pal.Red, () =>
                {
                    ToggleArm(id);
                    Destroy(layer.gameObject);
                    ShowArmory();
                }, 34, Pal.Paper);
                Ui.Btn(face.transform, "回收", new Vector2(160, 72), new Vector2(360, 0), Pal.Gold, () =>
                {
                    Recycle(id);
                    Destroy(layer.gameObject);
                    ShowArmory();
                }, 34, Pal.Paper);
            }
            if (row == 0)
                Ui.Label(card, "打仗撑过几波，就有机会缴获兵器带回山寨。", 36, Pal.InkSoft, new Vector2(0, 80), new Vector2(860, 120));
        }

        void ToggleArm(HeroId id)
        {
            int k = (int)id;
            if (Save.WeaponCount(k) <= 0) return;
            if (Save.D.armed.Contains(k)) Save.D.armed.Remove(k);
            else
            {
                if (!Save.D.heroes.Contains(k)) { Toast("先把这好汉凑出来，才能用他的兵器"); return; }
                if (Save.D.armed.Count >= Save.MaxArmed) { Toast("最多同时装备三件兵器"); return; }
                Save.D.armed.Add(k);
            }
            Save.Write();
        }

        void Recycle(HeroId id)
        {
            int k = (int)id;
            int at = Save.D.weapons.IndexOf(k);
            if (at < 0) return;
            Save.D.weapons.RemoveAt(at);
            if (Save.WeaponCount(k) <= 0) Save.D.armed.Remove(k);
            Save.D.coins += Save.RecycleCoins;
            Save.Write();
            Refresh();
            Sfx.Play(Snd.Merge);
            Toast("回收得铜钱 " + Save.RecycleCoins);
        }

        /// <summary>卖掉没装备的，装备中的只留一阶。返回拿到的铜钱。</summary>
        int RecycleSpare()
        {
            int gained = 0;
            var list = Save.D.weapons;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                int id = list[i];
                if (Save.D.armed.Contains(id) && Save.WeaponCount(id) <= 1) continue;
                list.RemoveAt(i);
                gained += Save.RecycleCoins;
            }
            Save.D.armed.RemoveAll(id => Save.WeaponCount(id) <= 0);
            if (gained <= 0) return 0;
            Save.D.coins += gained;
            Save.Write();
            Refresh();
            Sfx.Play(Snd.Merge);
            Toast("一键回收得铜钱 " + gained);
            return gained;
        }

        void ShowBoard()
        {
            Panel("座次榜", out var card, new Vector2(1000, 1500));
            string mineStars = "";
            for (int i = 0; i < RankRules.Stars; i++) mineStars += i < Save.D.stars ? "★" : "☆";
            float rate = Save.D.battles <= 0 ? 0f : 100f * Save.D.wins / Save.D.battles;
            Ui.Label(card, "败一局掉一星。星掉光再败，座次掉一阶。", 32, Pal.InkSoft, new Vector2(0, 560), new Vector2(900, 50));
            Ui.Box(card, new Vector2(900, 120), new Vector2(0, 460), Pal.TileHero);
            Ui.Label(card, "你", 36, Pal.RedDeep, new Vector2(-360, 460), new Vector2(100, 100));
            Ui.Label(card, Save.RankName + "  " + mineStars, 40, Pal.Ink, new Vector2(-40, 460), new Vector2(520, 100));
            Ui.Label(card, rate.ToString("0") + "%", 36, Pal.InkSoft, new Vector2(340, 460), new Vector2(140, 100));

            string[] names = { "燕顺", "王英", "郑天寿", "石勇", "杜迁", "宋万", "朱贵", "李云" };
            int seed = int.Parse(Save.Today);
            for (int i = 0; i < names.Length; i++)
            {
                int tier = Mathf.Clamp(Save.D.tier + ((seed + i * 3) % 5) - 2, 0, Save.Tiers.Length - 1);
                int stars = (seed / 7 + i * 2) % (RankRules.Stars + 1);
                string rowStars = "";
                for (int k = 0; k < RankRules.Stars; k++) rowStars += k < stars ? "★" : "☆";
                float y = 320 - i * 110;
                Ui.Box(card, new Vector2(900, 96), new Vector2(0, y), i % 2 == 0 ? Pal.Tile : Pal.PaperDark);
                Ui.Label(card, (i + 1).ToString(), 32, Pal.InkSoft, new Vector2(-390, y), new Vector2(80, 90));
                Ui.Label(card, names[i], 40, Pal.Ink, new Vector2(-230, y), new Vector2(220, 90), TextAnchor.MiddleLeft, Fonts.Brush);
                Ui.Label(card, Save.Tiers[tier] + "  " + rowStars, 32, Pal.InkSoft, new Vector2(120, y), new Vector2(420, 90));
            }
        }

        void Toast(string s)
        {
            var bg = Ui.Box(_root, new Vector2(720, 100), new Vector2(0, -200), Pal.Ink.A(0.85f), null, "toast");
            Ui.Label(bg.transform, s, 42, Pal.Paper, Vector2.zero, new Vector2(700, 100));
            Tween.Pop(bg.transform, 0.2f);
            Tween.Run(bg, 1.4f, k => { }, () => Destroy(bg.gameObject));
        }

        void Confirm(string title, string body, string ok, Action onOk)
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.7f), true, "confirm");
            var card = Ui.Box(layer, new Vector2(820, 560), Vector2.zero, Pal.Paper).rectTransform;
            Ui.Label(card, title, 80, Pal.Ink, new Vector2(0, 170), new Vector2(760, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(card, body, 40, Pal.InkSoft, new Vector2(0, 30), new Vector2(740, 120));
            Ui.Btn(card, ok, new Vector2(320, 110), new Vector2(-180, -170), Pal.Red, () => { Destroy(layer.gameObject); onOk(); }, 44, Pal.Paper);
            Ui.Btn(card, "算了", new Vector2(320, 110), new Vector2(180, -170), Pal.InkSoft, () => Destroy(layer.gameObject), 44);
            Tween.Pop(card, 0.25f);
        }
    }

    /// <summary>结算：座次星、铜钱、好汉入谱、双倍领取。</summary>
    public static class ResultPanel
    {
        public static void Show(BattleResult r, Action onClose)
        {
            int coins = r.Quit ? 0 : (r.Win ? 30 + 10 * r.Chapter : 8 + r.Wave);
            var rank = r.Quit
                ? new Save.RankDelta { OldTier = Save.D.tier, OldStars = Save.D.stars }
                : Save.ApplyResult(r.Win);
            if (r.Win) Save.D.unlocked = Mathf.Max(Save.D.unlocked, Mathf.Min(r.Chapter + 1, ChapterDef.All.Length - 1));
            foreach (var id in r.Formed) if (!Save.D.heroes.Contains((int)id)) Save.D.heroes.Add((int)id);
            Save.D.coins += coins;
            string loot = Save.RollWeapon(r);
            Save.Write();

            var layer = Ui.Fill(Ui.Root, new Color(0.05f, 0.04f, 0.03f, 0.78f), true, "result");
            var card = Ui.Box(layer, new Vector2(940, 1300), new Vector2(0, 30), Pal.Paper).rectTransform;
            var title = Ui.Label(card, r.Win ? "大  胜" : "失  手", 190, r.Win ? Pal.Red : Pal.Ink, new Vector2(0, 470), new Vector2(900, 240), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(card, r.Win ? "宋江哥哥救下来了！" : (r.Quit ? "暂且撤回山寨" : "宋江被押走了……再来！"), 44, Pal.InkSoft, new Vector2(0, 330), new Vector2(880, 70));

            string StarStr(int n)
            {
                string s = "";
                for (int i = 0; i < RankRules.Stars; i++) s += i < n ? "★" : "☆";
                return s;
            }
            bool dropped = rank.Demoted || rank.DroppedStar;
            string verb = r.Quit ? "撤回不记座次"
                : rank.Promoted ? "晋升"
                : rank.Demoted ? "掉阶"
                : rank.DroppedStar ? "掉星"
                : r.Win && Save.D.stars > rank.OldStars ? "升星"
                : !r.Win ? "喽啰垫底，没有星可掉" : "";
            if (rank.Promoted || rank.Demoted)
                verb += "  " + Save.Tiers[Mathf.Clamp(rank.OldTier, 0, Save.Tiers.Length - 1)] + " → " + Save.RankName;
            Ui.Box(card, new Vector2(840, 210), new Vector2(0, 170), dropped ? Pal.Hex("F3D2C4") : Pal.Tile);
            Ui.Label(card, Save.RankName, 60, Pal.RedDeep, new Vector2(0, 230), new Vector2(780, 80)).fontStyle = FontStyle.Bold;
            var starText = Ui.Label(card, StarStr(rank.OldStars) + "   →   " + StarStr(Save.D.stars), 46,
                dropped ? Pal.Red : Pal.Gold, new Vector2(0, 165), new Vector2(800, 70));
            Ui.Label(card, verb, 40, dropped ? Pal.Red : Pal.Ink, new Vector2(0, 105), new Vector2(800, 56));

            Ui.Label(card, $"撑到第 {r.Wave} 波　斩敌 {r.Kills}　及时雨 {r.Rains} 次", 38, Pal.Ink, new Vector2(0, 60), new Vector2(880, 60));

            var names = new List<string>();
            foreach (var id in r.Formed)
            {
                var d = HeroDef.Get(id);
                names.Add(r.NewHeroes.Contains(id) ? d.Name + "(新)" : d.Name);
            }
            Ui.Label(card, names.Count > 0 ? "本战聚义：" + string.Join("、", names) : "本战没凑出好汉，下次试试摆金字", 36,
                names.Count > 0 ? Pal.Gold : Pal.InkSoft, new Vector2(0, -40), new Vector2(860, 110));
            if (r.NewHeroes.Count > 0)
                Ui.Label(card, $"好汉谱 +{r.NewHeroes.Count}", 40, Pal.Red, new Vector2(0, -120), new Vector2(600, 60));

            Ui.Label(card, "领完铜钱，孙二娘的摊子就摆开", 30, Pal.Willow, new Vector2(0, -165), new Vector2(820, 44));
            string coinLine = "铜钱 +" + coins + (string.IsNullOrEmpty(loot) ? "" : "\n缴获 " + loot);
            var coinText = Ui.Label(card, coinLine, 52, Pal.Gold, new Vector2(30, -250), new Vector2(800, 150), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Box(card, new Vector2(90, 90), new Vector2(-200, -230), Color.white, Art.Img("coin")).preserveAspect = true;
            bool claimed = false;
            void Close()
            {
                if (claimed) return;
                claimed = true;
                UnityEngine.Object.Destroy(layer.gameObject);
                onClose?.Invoke();
            }
            if (coins > 0)
                Ui.Btn(card, "看广告 双倍领取", new Vector2(600, 130), new Vector2(0, -390), Pal.Red, () =>
                    AdStub.Show("双倍铜钱", () =>
                    {
                        Save.D.coins += coins;
                        Save.Write();
                        coinText.text = "铜钱 +" + coins * 2 + (string.IsNullOrEmpty(loot) ? "" : "\n缴获 " + loot);
                        Tween.Pop(coinText.transform, 0.3f);
                        Sfx.Play(Snd.Rain);
                        Tween.Run(coinText, 0.9f, k => { }, Close);
                    }), 48, Pal.Paper);
            Ui.Btn(card, coins > 0 ? "直接领取" : "回山寨", new Vector2(400, 100), new Vector2(0, -540), Pal.PaperDark, Close, 40, Pal.Ink);

            Tween.Pop(card, 0.35f);
            Tween.Pop(title.transform, 0.45f, 0.15f);
            if (rank.Promoted || rank.Demoted || rank.DroppedStar) Tween.Pop(starText.transform, 0.4f, 0.4f);
        }
    }
}
