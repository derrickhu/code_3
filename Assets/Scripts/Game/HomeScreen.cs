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
        public Func<bool> Owned;
        public Action Give;

        public Color RarityColor => Rarity == "传说" ? Pal.Hex("D9822B") : Rarity == "卓越" ? Pal.Purple : Pal.Hex("3D7BBF");

        public static readonly ShopItem[] All =
        {
            new ShopItem { Id = 0, Name = "生辰纲", Desc = "开局炊饼 +20", Rarity = "稀有", Icon = "coin", Price = 40, Owned = () => Save.D.gang, Give = () => Save.D.gang = true },
            new ShopItem { Id = 1, Name = "替天行道旗", Desc = "全军伤害 +10%", Rarity = "卓越", Price = 80, Owned = () => Save.D.flag, Give = () => Save.D.flag = true },
            new ShopItem { Id = 2, Name = "水泊", Desc = "官兵移速 -15%", Rarity = "卓越", Price = 70, Owned = () => Save.D.marsh, Give = () => Save.D.marsh = true },
            new ShopItem { Id = 3, Name = "石碣", Desc = "官兵靠近宋江时\n天降石碣砸下", Rarity = "传说", Price = 120, Owned = () => Save.D.stele, Give = () => Save.D.stele = true },
            new ShopItem { Id = 4, Name = "蒙汗药", Desc = "战斗中点击\n全场官兵昏睡 3 秒", Rarity = "卓越", Active = true, Price = 60, Owned = () => Save.D.menghan >= 1, Give = () => Save.D.menghan++ },
            new ShopItem { Id = 5, Name = "天书", Desc = "战斗中点击\n最低级的字升一级", Rarity = "传说", Active = true, Price = 150, Owned = () => Save.D.tianshu >= 1, Give = () => Save.D.tianshu++ },
            new ShopItem { Id = 6, Name = "孙二娘包子", Desc = "55% 给宋江回一颗心\n45% 吃坏肚子掉一颗心", Rarity = "稀有", Icon = "bun", Active = true, Price = 50, Owned = () => Save.D.baozi >= 1, Give = () => Save.D.baozi++ },
            new ShopItem { Id = 7, Name = "续命丹", Desc = "我方宋江 +10 血\n对手宋江 +6 血", Rarity = "传说", Price = 150, Owned = () => Save.D.xuming, Give = () => Save.D.xuming = true },
            new ShopItem { Id = 8, Name = "方便铲", Desc = "开局送 2 把铲子\n多开两块地", Rarity = "稀有", Icon = "shovel", Price = 50, Owned = () => Save.D.shovels >= 2, Give = () => Save.D.shovels = 2 },
        };
    }

    /// <summary>山寨主界面：座次、章节、出征、好汉谱、十字坡酒店。</summary>
    public sealed class HomeScreen : MonoBehaviour
    {
        RectTransform _root;
        Text _coins, _stamina, _loadout;
        int _selected;
        readonly List<Image> _cardFaces = new List<Image>();
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

        void Build()
        {
            _root = Ui.Rect(Ui.Root, "Home");
            _root.anchorMin = Vector2.zero;
            _root.anchorMax = Vector2.one;
            _root.offsetMin = _root.offsetMax = Vector2.zero;

            Save.RefreshShopDay();
            var bg = _root.gameObject.AddComponent<Image>();
            bg.sprite = Art.Img("home_bg");
            bg.color = Color.white;

            var top = Ui.Rect(_root, "top");
            Ui.Anchor(top, 0.5f, 1f);
            top.anchoredPosition = new Vector2(0, -70);
            Ui.Box(top, new Vector2(300, 80), new Vector2(-360, 0), Pal.Ink.A(0.85f));
            _coins = Ui.Label(top, "", 40, Pal.GoldBright, new Vector2(-360, 0), new Vector2(300, 80));
            Ui.Box(top, new Vector2(330, 80), new Vector2(0, 0), Pal.Ink.A(0.85f));
            _stamina = Ui.Label(top, "", 36, Pal.Paper, new Vector2(0, 0), new Vector2(330, 80));
            Button sfx = null;
            sfx = Ui.Btn(top, Sfx.On ? "音效 开" : "音效 关", new Vector2(220, 80), new Vector2(380, 0), Pal.InkSoft, () =>
            {
                Sfx.On = !Sfx.On;
                Save.D.sfx = Sfx.On;
                Save.Write();
                Ui.SetText(sfx, Sfx.On ? "音效 开" : "音效 关");
            }, 34);

            var title = Ui.Rect(_root, "title");
            Ui.Anchor(title, 0.5f, 1f);
            title.anchoredPosition = new Vector2(0, -330);
            var t = Ui.Label(title, "营救宋江", 230, Pal.Ink, Vector2.zero, new Vector2(1060, 300), TextAnchor.MiddleCenter, Fonts.Brush);
            var seal = Ui.Box(title, new Vector2(120, 120), new Vector2(400, -120), Pal.Red);
            seal.rectTransform.localRotation = Quaternion.Euler(0, 0, -10);
            Ui.Label(seal.transform, "梁山", 46, Pal.Paper, Vector2.zero, new Vector2(120, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(title, "水浒文字合成 · 1V1 塔防", 40, Pal.InkSoft, new Vector2(0, -170), new Vector2(900, 60));
            Tween.Pop(t.transform, 0.5f);

            var rank = Ui.Rect(_root, "rank");
            Ui.Anchor(rank, 0.5f, 1f);
            rank.anchoredPosition = new Vector2(0, -620);
            Ui.Box(rank, new Vector2(940, 120), Vector2.zero, Pal.Tile);
            string stars = "";
            for (int i = 0; i < 5; i++) stars += i < Save.D.stars ? "★" : "☆";
            Ui.Label(rank, "座次", 36, Pal.InkSoft, new Vector2(-370, 0), new Vector2(140, 100));
            Ui.Label(rank, Save.RankName, 56, Pal.RedDeep, new Vector2(-170, 0), new Vector2(300, 110)).fontStyle = FontStyle.Bold;
            Ui.Label(rank, stars, 50, Pal.Gold, new Vector2(110, 0), new Vector2(300, 100));
            Ui.Label(rank, $"胜 {Save.D.wins} / 战 {Save.D.battles}", 32, Pal.InkSoft, new Vector2(360, 0), new Vector2(220, 100));

            var list = Ui.Rect(_root, "chapters");
            list.anchoredPosition = new Vector2(0, 90);
            _selected = Mathf.Clamp(Save.D.unlocked, 0, ChapterDef.All.Length - 1);
            for (int i = 0; i < ChapterDef.All.Length; i++) BuildCard(list, ChapterDef.All[i], new Vector2(0, 250 - i * 250));
            SelectCard(_selected);

            var bottom = Ui.Rect(_root, "bottom");
            Ui.Anchor(bottom, 0.5f, 0f);
            bottom.anchoredPosition = new Vector2(0, 0);
            _loadout = Ui.Label(bottom, "", 34, Pal.Willow, new Vector2(0, 520), new Vector2(940, 60));
            var go = Ui.Btn(bottom, "出  征", new Vector2(600, 170), new Vector2(0, 400), Pal.Red, Fight, 84, Pal.Paper, Fonts.Brush);
            Ui.Label(go.transform, "体力 -1", 28, Pal.Paper.A(0.8f), new Vector2(0, -62), new Vector2(300, 40));
            StartCoroutineLoop(go.transform.parent);
            Ui.Btn(bottom, "好汉谱", new Vector2(440, 130), new Vector2(-240, 200), Pal.Ink, ShowCodex, 54, Pal.GoldBright, Fonts.Brush);
            var shopBtn = Ui.Btn(bottom, "神秘商人", new Vector2(440, 130), new Vector2(240, 200), Pal.Willow, ShowShop, 54, Pal.Paper, Fonts.Brush);
            var face = Ui.Box(shopBtn.transform, new Vector2(150, 150), new Vector2(-190, 40), Color.white, Art.Img("merchant"));
            face.preserveAspect = true;
            Ui.Label(bottom, "垂直切片 Demo v0.1 · 题材原创 · 字体 OFL", 26, Pal.InkSoft.A(0.6f), new Vector2(0, 70), new Vector2(900, 40));

            Refresh();
        }

        void StartCoroutineLoop(Transform t)
        {
            Tween.Run(t, 1.4f, k => t.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI * 2) * 0.035f), () => StartCoroutineLoop(t));
        }

        void BuildCard(Transform parent, ChapterDef ch, Vector2 pos)
        {
            bool locked = ch.Index > Save.D.unlocked;
            var holder = Ui.Rect(parent, "card" + ch.Index);
            holder.anchoredPosition = pos;
            holder.sizeDelta = new Vector2(940, 220);
            Ui.Box(holder, new Vector2(940, 220), new Vector2(0, -8), Pal.Shadow);
            var face = Ui.Box(holder, new Vector2(940, 220), Vector2.zero, Pal.Tile);
            face.raycastTarget = true;
            _cardFaces.Add(face);
            var b = face.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            int idx = ch.Index;
            b.onClick.AddListener(() =>
            {
                Sfx.Play(Snd.Tap);
                if (locked) { Toast("先赢下上一回才能解锁"); return; }
                SelectCard(idx);
            });

            Ui.Label(face.transform, "第" + BattleView.ToCn(ch.Index + 1) + "回", 34, Pal.InkSoft, new Vector2(-340, 55), new Vector2(200, 50));
            Ui.Label(face.transform, ch.Name, 84, locked ? Pal.InkSoft.A(0.5f) : Pal.Ink, new Vector2(-250, -20), new Vector2(420, 110), TextAnchor.MiddleLeft, Fonts.Brush)
                .rectTransform.anchoredPosition = new Vector2(-180, -20);
            if (locked)
            {
                Ui.Label(face.transform, "未解锁", 44, Pal.InkSoft, new Vector2(270, 0), new Vector2(360, 80));
                return;
            }
            var nem = HeroDef.Get(ch.Boss.Nemesis).Name;
            Ui.Label(face.transform, $"头目 {ch.Boss.Name}\n克星 {nem}", 32, Pal.RedDeep, new Vector2(150, 30), new Vector2(300, 110), TextAnchor.MiddleLeft);
            Ui.Label(face.transform, "对手 " + ch.RivalName, 30, Pal.InkSoft, new Vector2(150, -60), new Vector2(300, 50), TextAnchor.MiddleLeft);
            Ui.Label(face.transform, Stars(ch.RivalSkill), 34, Pal.Gold, new Vector2(370, -60), new Vector2(160, 50));
        }

        static string Stars(float skill)
        {
            int n = 1 + Mathf.RoundToInt(skill * 3);
            return new string('★', n) + new string('☆', 4 - n);
        }

        void SelectCard(int i)
        {
            _selected = i;
            for (int k = 0; k < _cardFaces.Count; k++)
                _cardFaces[k].color = k == i ? Pal.TileHero : (k > Save.D.unlocked ? Pal.PaperDark : Pal.Tile);
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
            _coins.text = "铜钱 " + Save.D.coins;
            int s = Stamina.Current;
            int sec = Stamina.SecondsToNext;
            _stamina.text = s >= Stamina.Max ? $"体力 {s}/{Stamina.Max}" : $"体力 {s}/{Stamina.Max}  {sec / 60}:{sec % 60:00}";
            _loadout.text = Save.D.HasAnyItem ? "下一战随身：" + BattleScreen.ItemSummary(Save.D.PeekLoadout()) : "";
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

        public void ShowShop()
        {
            Save.RefreshShopDay();
            Panel("神秘商人", out var card, new Vector2(1000, 1700));
            var portrait = Ui.Box(card, new Vector2(300, 300), new Vector2(-320, 530), Color.white, Art.Img("merchant"));
            portrait.preserveAspect = true;
            var bubble = Ui.Box(card, new Vector2(560, 170), new Vector2(150, 560), Pal.Tile);
            Ui.Label(bubble.transform, "孙二娘：客官，好货不等人！\n道具仅当天有效，明日换新货。", 32, Pal.Ink, Vector2.zero, new Vector2(530, 150), TextAnchor.MiddleLeft);
            var coins = Ui.Label(card, "", 40, Pal.Gold, new Vector2(150, 440), new Vector2(560, 60));
            Ui.Box(card, new Vector2(48, 48), new Vector2(-30, 440), Color.white, Art.Img("coin"));
            var buttons = new List<(ShopItem item, Button btn)>();
            void RefreshShop()
            {
                coins.text = "　 铜钱 " + Save.D.coins;
                foreach (var (item, btn) in buttons)
                    Ui.SetText(btn, Save.D.shopSold.Contains(item.Id) || item.Owned() ? "已购" : item.Price + " 文");
                Refresh();
            }

            void Section(string title, bool active, float y)
            {
                Ui.Box(card, new Vector2(920, 54), new Vector2(0, y), Pal.Ink.A(0.85f));
                Ui.Label(card, title, 34, Pal.GoldBright, new Vector2(0, y), new Vector2(900, 54));
                int col = 0;
                foreach (int id in Save.D.shopOffer)
                {
                    var it = ShopItem.All[id];
                    if (it.Active != active) continue;
                    var pos = new Vector2(col == 0 ? -235 : 235, y - 210);
                    col++;
                    var face = Ui.Box(card, new Vector2(450, 340), pos, Pal.Tile);
                    var tag = Ui.Box(face.transform, new Vector2(110, 46), new Vector2(-155, 135), it.RarityColor);
                    Ui.Label(tag.transform, it.Rarity, 28, Pal.Paper, Vector2.zero, new Vector2(110, 46));
                    if (it.Icon != null)
                    {
                        var icon = Ui.Box(face.transform, new Vector2(110, 110), new Vector2(-140, 40), Color.white, Art.Img(it.Icon));
                        icon.preserveAspect = true;
                    }
                    else Ui.Label(face.transform, it.Name.Substring(0, 1), 96, it.RarityColor, new Vector2(-140, 40), new Vector2(120, 120), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(face.transform, it.Name, it.Name.Length > 4 ? 46 : 58, Pal.Ink, new Vector2(60, 80), new Vector2(300, 80), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(face.transform, it.Desc, 28, Pal.InkSoft, new Vector2(60, 5), new Vector2(300, 80));
                    var b = Ui.Btn(face.transform, "", new Vector2(260, 80), new Vector2(0, -115), Pal.Red, () =>
                    {
                        if (Save.D.shopSold.Contains(it.Id) || it.Owned()) { Toast("今天已经买过了"); return; }
                        if (Save.D.coins < it.Price) { Toast("铜钱不够，打仗去赚"); return; }
                        Save.D.coins -= it.Price;
                        it.Give();
                        Save.D.shopSold.Add(it.Id);
                        Save.Write();
                        Sfx.Play(Snd.Merge);
                        RefreshShop();
                    }, 38, Pal.Paper);
                    buttons.Add((it, b));
                }
            }
            Section("主动道具 · 战斗中点击使用", true, 330);
            Section("被动道具 · 开局自动生效", false, -150);
            Ui.Btn(card, "看广告 白拿一个孙二娘包子", new Vector2(700, 110), new Vector2(0, -620), Pal.Gold, () =>
                AdStub.Show("免费道具", () => { Save.D.baozi = Mathf.Min(3, Save.D.baozi + 1); Save.Write(); RefreshShop(); }), 40, Pal.Paper);
            RefreshShop();
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
            int oldTier = Save.D.tier;
            bool promoted = r.Quit ? false : Save.ApplyResult(r.Win);
            if (r.Quit) { Save.D.battles++; if (Save.D.stars > 0) Save.D.stars--; }
            if (r.Win) Save.D.unlocked = Mathf.Max(Save.D.unlocked, Mathf.Min(r.Chapter + 1, ChapterDef.All.Length - 1));
            foreach (var id in r.Formed) if (!Save.D.heroes.Contains((int)id)) Save.D.heroes.Add((int)id);
            Save.D.coins += coins;
            Save.Write();

            var layer = Ui.Fill(Ui.Root, new Color(0.05f, 0.04f, 0.03f, 0.78f), true, "result");
            var card = Ui.Box(layer, new Vector2(940, 1300), new Vector2(0, 30), Pal.Paper).rectTransform;
            var title = Ui.Label(card, r.Win ? "大  胜" : "失  手", 190, r.Win ? Pal.Red : Pal.Ink, new Vector2(0, 470), new Vector2(900, 240), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(card, r.Win ? "宋江哥哥救下来了！" : (r.Quit ? "暂且撤回山寨" : "宋江被押走了……再来！"), 44, Pal.InkSoft, new Vector2(0, 330), new Vector2(880, 70));

            string stars = "";
            for (int i = 0; i < 5; i++) stars += i < Save.D.stars ? "★" : "☆";
            Ui.Box(card, new Vector2(840, 150), new Vector2(0, 190), Pal.Tile);
            Ui.Label(card, Save.RankName, 64, Pal.RedDeep, new Vector2(-220, 190), new Vector2(380, 130)).fontStyle = FontStyle.Bold;
            var starText = Ui.Label(card, promoted ? "晋升！" : stars, 56, Pal.Gold, new Vector2(200, 190), new Vector2(400, 120));
            if (promoted) Ui.Label(card, $"{Save.Tiers[oldTier]} → {Save.RankName}", 32, Pal.InkSoft, new Vector2(200, 135), new Vector2(400, 40));

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

            var coinText = Ui.Label(card, "铜钱 +" + coins, 64, Pal.Gold, new Vector2(30, -230), new Vector2(800, 100), TextAnchor.MiddleCenter, Fonts.Brush);
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
                        coinText.text = "铜钱 +" + coins * 2;
                        Tween.Pop(coinText.transform, 0.3f);
                        Sfx.Play(Snd.Rain);
                        Tween.Run(coinText, 0.9f, k => { }, Close);
                    }), 48, Pal.Paper);
            Ui.Btn(card, coins > 0 ? "直接领取" : "回山寨", new Vector2(400, 100), new Vector2(0, -540), Pal.PaperDark, Close, 40, Pal.Ink);

            Tween.Pop(card, 0.35f);
            Tween.Pop(title.transform, 0.45f, 0.15f);
            if (promoted) Tween.Pop(starText.transform, 0.4f, 0.4f);
        }
    }
}
