using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>神秘商人（孙二娘）：商店页三件货（最贵的一件看广告白拿），抽奖页九宫格转圈。</summary>
    public sealed partial class HomeScreen
    {
        static readonly string[] MerchantLines = { "诚信为本", "好货不等人", "客官里边请", "今日货今日用", "包子刚出笼", "童叟无欺" };
        const int ActiveSlots = 4;
        const int BagCols = 7;

        static readonly Color ShopWood = Pal.Hex("8C6B4E");
        static readonly Color ShopFace = Pal.Hex("C9B48C");
        static readonly Color LottoWood = Pal.Hex("F0B048");
        static readonly Color LottoFace = Pal.Hex("D98E3E");

        bool _lottoTab;
        Action _spinNow;

        /// <summary>验收截图用：跳过广告直接转一把。</summary>
        public void DebugSpin() => _spinNow?.Invoke();
        Text _say;
        RectTransform _bubble;

        /// <summary>结算后自动弹出，也能从主界面点孙二娘进来。</summary>
        public void ShowShop() => ShowShop(false);

        public void ShowShop(bool lotto)
        {
            Save.RefreshShopDay();
            _lottoTab = lotto;
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
            _bubble = (RectTransform)bubble.transform.parent;
            _say = Ui.Label(bubble.transform, MerchantLines[UnityEngine.Random.Range(0, MerchantLines.Length)], 32, Pal.Ink, Vector2.zero, new Vector2(220, 80));

            var panelHolder = Ui.Rect(body, "panelHolder");
            var content = Ui.Rect(body, "content");
            Ui.Title(body, "神秘商人", new Vector2(0, 590));
            Ui.CloseX(body, new Vector2(-445, 600), () => { if (_shop) Destroy(_shop.gameObject); _shop = null; _shopCoins = _shopStamina = null; });

            RectTransform panel = null;
            void Rebuild(string line)
            {
                if (panel) Destroy(panel.gameObject);
                panel = Ui.WoodPanel(panelHolder, new Vector2(940, 1110), new Vector2(0, 45), _lottoTab ? LottoWood : ShopWood, _lottoTab ? LottoFace : ShopFace);
                for (int i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
                if (line != null) Say(line);
                BuildTabs(content, Rebuild);
                if (_lottoTab) BuildLotto(content, Rebuild);
                else BuildOffers(content, Rebuild);
                BuildBag(content, Rebuild);
                Refresh();
            }
            Rebuild(null);

            var tip = Ui.Label(_shop, "tip:道具仅当天有效", 42, Pal.Paper, new Vector2(0, -640), new Vector2(800, 60));
            tip.fontStyle = FontStyle.Bold;
            Tween.Pop(body, 0.3f);
        }

        void Say(string line)
        {
            if (_say == null) return;
            _say.text = line;
            Tween.Punch(_bubble, 0.2f);
        }

        void BuildTabs(RectTransform content, Action<string> rebuild)
        {
            void Tab(string text, float x, bool on, bool lotto)
            {
                var face = Ui.Framed(content, new Vector2(170, 72), new Vector2(x, 500), on ? Ui.BtnYellow : Pal.Hex("A8865F"), Edge, 4f, "framedTab");
                Ui.Stroke(Ui.Label(face.transform, text, 40, on ? Edge : Pal.Paper, new Vector2(0, 2), new Vector2(170, 72), TextAnchor.MiddleCenter, Fonts.Brush),
                    on ? Pal.Hex("FFF2B8") : Pal.Hex("5E4532"), 1.5f);
                if (on) return;
                Ui.Tap(face, () => { _lottoTab = lotto; rebuild(lotto ? "手气旺不旺，转一把就知道" : "客官里边请"); });
            }
            Tab("商店", -100, !_lottoTab, false);
            Tab("抽奖", 100, _lottoTab, true);
            if (Save.LottoLeft > 0 && !_lottoTab)
            {
                var dot = Ui.Box(content, new Vector2(30, 30), new Vector2(178, 528), Pal.Hex("E8392B"), Art.Circle, "redDot");
                dot.type = Image.Type.Simple;
            }
        }

        /// <summary>今天三件货里最贵的那件，可以看广告白拿。</summary>
        static int FreeOfferId()
        {
            int best = -1, price = -1;
            foreach (var id in Save.D.shopOffer)
                if (ShopItem.All[id].Price > price) { price = ShopItem.All[id].Price; best = id; }
            return best;
        }

        void BuildOffers(RectTransform content, Action<string> rebuild)
        {
            int free = FreeOfferId();
            for (int i = 0; i < Save.D.shopOffer.Count; i++)
            {
                var it = ShopItem.All[Save.D.shopOffer[i]];
                float y = 360 - i * 190;
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

                bool sold = Save.D.shopSold.Contains(it.Id) || Save.D.shopFree.Contains(it.Id) || it.Owned();
                if (sold)
                {
                    var gone = Ui.Framed(t, new Vector2(212, 104), new Vector2(276, 0), Pal.Hex("4D4642"), Edge, 5f, "framedSold");
                    Ui.Label(gone.transform, "已售完", 44, Pal.Paper, Vector2.zero, new Vector2(200, 100)).fontStyle = FontStyle.Bold;
                    continue;
                }
                if (it.Id == free)
                {
                    var adBtn = Ui.CartoonBtn(t, "免费\n领取", new Vector2(212, 108), new Vector2(276, 0), Ui.BtnOrange, () =>
                    {
                        if (!CanTake(it)) return;
                        AdStub.Show("免费领取" + it.Name, () =>
                        {
                            it.Give();
                            Save.D.shopFree.Add(it.Id);
                            Save.Write();
                            Sfx.Play(Snd.Merge);
                            Toast("成功获得" + it.Name);
                            rebuild("白送的，客官拿好！");
                        });
                    }, 40, "ui_video");
                    adBtn.Find("text").GetComponent<Text>().lineSpacing = 0.9f;
                    Shine(adBtn);
                    continue;
                }
                var btn = Ui.Framed(t, new Vector2(212, 104), new Vector2(276, 0), Ui.BtnYellow, Edge, 5f, "framedBuy");
                Ui.Box(btn.transform, new Vector2(196, 14), new Vector2(0, 34), Color.white.A(0.35f), null, "gloss");
                Ui.Icon(btn.transform, "coin", 70, new Vector2(-52, 0));
                var price = Ui.Label(btn.transform, it.Price.ToString(), 46, Edge, new Vector2(34, 0), new Vector2(120, 100));
                price.fontStyle = FontStyle.Bold;
                if (Save.D.coins < it.Price) price.color = Pal.Hex("A33A2A");
                Ui.Tap(btn, () =>
                {
                    if (Save.D.coins < it.Price) { Toast("铜钱不够，打一局再来"); return; }
                    if (!CanTake(it)) return;
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

        static bool CanTake(ShopItem it)
        {
            if (it.Active && it.Count() == 0 && ActiveKinds() >= ActiveSlots) { Toast($"主动道具最多带 {ActiveSlots} 种"); return false; }
            return true;
        }

        /// <summary>广告按钮上一道斜光来回扫，提醒「这个免费」。</summary>
        static void Shine(RectTransform btn)
        {
            var star = Ui.Icon(btn, "ui_sparkle", 54, new Vector2(btn.sizeDelta.x * 0.5f - 6, btn.sizeDelta.y * 0.5f - 6), "sparkle");
            Tween.Run(star, 1.2f, k => { if (star) { star.transform.localScale = Vector3.one * (0.6f + Mathf.Sin(k * Mathf.PI) * 0.5f); star.transform.localRotation = Quaternion.Euler(0, 0, k * 90f); } },
                () => { if (star) { Destroy(star.gameObject); Shine(btn); } });
        }

        static int ActiveKinds()
        {
            int n = 0;
            foreach (var it in ShopItem.All) if (it.Active && it.Count() > 0) n++;
            return n;
        }

        // ─────────────────────────── 抽奖 ───────────────────────────

        static readonly Vector2Int[] Ring =
        {
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1),
            new Vector2Int(2, 2), new Vector2Int(1, 2), new Vector2Int(0, 2), new Vector2Int(0, 1),
        };

        static Vector2 LottoPos(Vector2Int g) => new Vector2(-198 + g.x * 198, 368 - g.y * 198);

        void BuildLotto(RectTransform content, Action<string> rebuild)
        {
            var pool = Save.LottoPool();
            Ui.Framed(content, new Vector2(620, 620), new Vector2(0, 170), Pal.Hex("B86E2E"), Pal.Hex("7A4318"), 6f, "lottoBg");
            var cells = new RectTransform[pool.Count];
            for (int k = 0; k < pool.Count && k < Ring.Length; k++)
            {
                var it = ShopItem.All[pool[k]];
                var face = Ui.Framed(content, new Vector2(176, 176), LottoPos(Ring[k]), it.RarityColor, Edge, 5f, "framedLotto");
                face.pixelsPerUnitMultiplier = 1.2f;
                Ui.Box(face.transform, new Vector2(160, 60), new Vector2(0, -50), Color.black.A(0.12f), null, "band").pixelsPerUnitMultiplier = 1.2f;
                Ui.Icon(face.transform, it.Icon, 112, new Vector2(0, 16));
                var nm = Ui.Label(face.transform, it.Name, it.Name.Length > 3 ? 26 : 32, Pal.Paper, new Vector2(0, -58), new Vector2(176, 44), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Stroke(nm, Edge, 2f);
                Ui.Tap(face, () => Toast(it.Name + "：" + it.Desc));
                cells[k] = (RectTransform)face.transform.parent;
            }

            var hl = Ui.Box(content, new Vector2(204, 204), LottoPos(Ring[0]), Pal.Hex("FFF27A"), Art.Frame, "highlight");
            hl.pixelsPerUnitMultiplier = 0.5f;
            hl.gameObject.SetActive(false);

            int left = Save.LottoLeft;
            var center = Ui.Framed(content, new Vector2(176, 176), LottoPos(new Vector2Int(1, 1)), left > 0 ? Pal.Hex("F2672E") : Pal.Hex("8E8A86"), Edge, 5f, "framedSpin");
            center.pixelsPerUnitMultiplier = 1.2f;
            Ui.Stroke(Ui.Label(center.transform, "抽奖", 52, Pal.Paper, new Vector2(0, 34), new Vector2(176, 70), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 3f);
            Ui.Icon(center.transform, "ui_video", 74, new Vector2(0, -26));
            Ui.Label(center.transform, left > 0 ? $"今日剩{left}次" : "明日再来", 24, Pal.Paper, new Vector2(0, -70), new Vector2(176, 30)).fontStyle = FontStyle.Bold;
            if (left > 0) Breathe(center.transform.parent);
            _spinNow = () => Spin(content, cells, hl, pool, rebuild);
            Ui.Tap(center, () =>
            {
                if (Save.LottoLeft <= 0) { Toast("今天的抽奖次数用完了，明天再来"); return; }
                AdStub.Show("抽奖", () => Spin(content, cells, hl, pool, rebuild));
            });

            Ui.Label(content, "我的道具:", 34, Pal.Ink, new Vector2(-262, -186), new Vector2(300, 50), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            for (int i = 0; i < ShopItem.Rarities.Length; i++)
                Ui.Tag(content, ShopItem.Rarities[i], ShopItem.ColorOf(ShopItem.Rarities[i]), new Vector2(-50 + i * 116, -186), 96, 26);
        }

        void Spin(RectTransform content, RectTransform[] cells, Image hl, List<int> pool, Action<string> rebuild)
        {
            int total = 0;
            foreach (var id in pool) total += ShopItem.All[id].Weight;
            int roll = UnityEngine.Random.Range(0, total), target = 0;
            for (int k = 0; k < pool.Count; k++)
            {
                roll -= ShopItem.All[pool[k]].Weight;
                if (roll < 0) { target = k; break; }
            }
            Save.D.lottoUsed++;
            Save.Write();

            var block = Ui.Fill(_shop, Color.clear, true, "spinBlock");
            hl.gameObject.SetActive(true);
            int steps = 8 * 3 + target;
            int last = -1;
            Say("转起来咯！");
            Tween.Run(hl, 2.8f, k =>
            {
                int idx = Mathf.Min(steps, Mathf.FloorToInt(Tween.OutCubic(k) * (steps + 0.999f)));
                if (idx == last) return;
                last = idx;
                hl.rectTransform.anchoredPosition = LottoPos(Ring[idx % 8]);
                Sfx.Play(Snd.Tap, 0.35f);
            }, () =>
            {
                var it = ShopItem.All[pool[target]];
                var cell = cells[target];
                Tween.Punch(cell, 0.4f, 0.25f);
                var rays = Ui.Icon(content, "ui_rays", 330, cell.anchoredPosition, "rays");
                rays.color = Color.white.A(0.85f);
                rays.transform.SetSiblingIndex(cell.GetSiblingIndex());
                Ui.Spin(rays.transform, 90f);
                Sfx.Play(Snd.Rain);
                string got = GiveLotto(it);
                Toast(got);
                Tween.Run(block, 1.3f, k => { }, () =>
                {
                    if (block) Destroy(block.gameObject);
                    rebuild(it.Rarity == "传说" || it.Rarity == "史诗" ? "哎哟，手气真旺！" : "恭喜客官！");
                });
            });
        }

        /// <summary>抽中的发进背包；主动种类满了或被动已有，就折成铜钱。</summary>
        static string GiveLotto(ShopItem it)
        {
            bool full = it.Active && it.Count() == 0 && ActiveKinds() >= ActiveSlots;
            bool dup = !it.Active && it.Owned();
            if (full || dup)
            {
                int coins = it.Price / 2;
                Save.D.coins += coins;
                Save.Write();
                return $"抽中「{it.Name}」，{(full ? "主动道具已满" : "已经有了")}，折铜钱 {coins}";
            }
            it.Give();
            Save.Write();
            return $"抽中「{it.Rarity}·{it.Name}」！";
        }

        /// <summary>我的道具：主动一行（战斗里最多四种，后三格木板封着）、被动一行；今天买的可以点红减号退货。</summary>
        void BuildBag(RectTransform content, Action<string> rebuild)
        {
            if (!_lottoTab) Ui.Label(content, "我的道具:", 34, Pal.Ink, new Vector2(-262, -186), new Vector2(300, 50), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            Ui.Framed(content, new Vector2(790, 248), new Vector2(26, -330), Pal.Hex("3A2A20"), Pal.Hex("5A4632"), 5f, "bag");
            var side = _lottoTab ? Pal.Hex("5A2E10") : Pal.Hex("3A2A20");
            Ui.Label(content, "主\n动", 30, side, new Vector2(-400, -270), new Vector2(44, 90)).fontStyle = FontStyle.Bold;
            Ui.Label(content, "被\n动", 30, side, new Vector2(-400, -390), new Vector2(44, 90)).fontStyle = FontStyle.Bold;
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
                    var cell = Ui.Box(content, new Vector2(96, 96), pos, it.DiscColor, null, "item");
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
    }
}
