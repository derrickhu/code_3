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
            new ShopItem { Id = 4, Name = "蒙汗药", Desc = "全场官兵昏睡 3 秒", Rarity = "史诗", Icon = "it_menghan", Active = true, Price = 60, Count = () => Save.D.menghan, Give = () => Save.D.menghan++, Take = () => Save.D.menghan = Mathf.Max(0, Save.D.menghan - 1) },
            new ShopItem { Id = 5, Name = "天书", Desc = "场上最低级的字升一级", Rarity = "传说", Icon = "it_tianshu", Active = true, Price = 150, Count = () => Save.D.tianshu, Give = () => Save.D.tianshu++, Take = () => Save.D.tianshu = Mathf.Max(0, Save.D.tianshu - 1) },
            new ShopItem { Id = 6, Name = "孙二娘包子", Desc = "五成半回一颗心，否则掉一颗", Rarity = "卓越", Icon = "bun", Active = true, Price = 50, Count = () => Save.D.baozi, Give = () => Save.D.baozi++, Take = () => Save.D.baozi = Mathf.Max(0, Save.D.baozi - 1) },
            new ShopItem { Id = 7, Name = "续命丹", Desc = "我方宋江 +10 血，对手只 +6", Rarity = "传说", Icon = "it_xuming", Price = 150, Count = () => B(Save.D.xuming), Give = () => Save.D.xuming = true, Take = () => Save.D.xuming = false },
            new ShopItem { Id = 8, Name = "方便铲", Desc = "开局送 2 把铲子，多开两块地", Rarity = "稀有", Icon = "shovel", Price = 50, Count = () => Save.D.shovels, Give = () => Save.D.shovels = 2, Take = () => Save.D.shovels = 0 },
            new ShopItem { Id = 9, Name = "攻速符", Desc = "单位攻速 +40%，持续十二秒", Rarity = "史诗", Icon = "it_fu", Active = true, Price = 80, Count = () => Save.D.fu, Give = () => Save.D.fu++, Take = () => Save.D.fu = Mathf.Max(0, Save.D.fu - 1) },
            new ShopItem { Id = 10, Name = "御敌千里", Desc = "远程单位攻击范围翻倍，全局生效", Rarity = "稀有", Icon = "it_far", Price = 90, Count = () => B(Save.D.far), Give = () => Save.D.far = true, Take = () => Save.D.far = false },
            new ShopItem { Id = 11, Name = "陷阱", Desc = "敌人掉下去一时会爬不出来", Rarity = "稀有", Icon = "it_trap", Active = true, Price = 35, Count = () => Save.D.trap, Give = () => Save.D.trap++, Take = () => Save.D.trap = Mathf.Max(0, Save.D.trap - 1) },
        };
    }

    /// <summary>山寨主界面：座次、章节、出征、好汉谱、十字坡酒店。</summary>
    public sealed partial class HomeScreen : MonoBehaviour
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
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.72f), true, "panel");
            card = Ui.WoodPanel(layer, size, new Vector2(0, -20));
            Ui.Title(card, title, new Vector2(0, size.y / 2 - 10));
            var l = layer;
            Ui.CloseX(card, new Vector2(-size.x / 2 + 6, size.y / 2 - 4), () => Destroy(l.gameObject));
            Tween.Pop(card, 0.25f);
            return layer;
        }

        static void Toast(string s) => Ui.Toast(s);

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
