using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>主界面的二级页：排行榜、武器装备、好汉谱、昨日俸禄。</summary>
    public sealed partial class HomeScreen
    {
        // ─────────────────────────── 排行榜 ───────────────────────────

        static readonly string[] BoardNames = { "无名", "燕顺", "王英", "郑天寿", "石勇", "杜迁", "宋万", "朱贵", "李云", "无名", "焦挺", "邹渊", "孟康", "汤隆" };
        static readonly string[] BoardGlyph = { "燕", "王", "郑", "石", "杜", "宋", "朱", "李", "焦", "邹", "孟", "汤", "曹", "侯" };
        static readonly string[] Regions = { "山东", "河北", "河南", "江苏", "浙江", "福建", "广东", "四川", "湖北", "江西" };
        static readonly Color[] RowColors = { Pal.Hex("E8664A"), Pal.Hex("F08A3C"), Pal.Hex("F5B868"), Pal.Hex("C9C6C0") };
        const string MyRegion = "山东";

        struct BoardRow { public string Name, Glyph, Region; public int Score; }

        static List<BoardRow> FakeBoard(bool province)
        {
            var rng = new System.Random(int.Parse(Save.Today) + (province ? 11 : 29));
            var rows = new List<BoardRow>();
            int score = (province ? 4200 : 5000) + rng.Next(0, 300);
            for (int i = 0; i < 6; i++)
            {
                rows.Add(new BoardRow
                {
                    Name = BoardNames[rng.Next(BoardNames.Length)],
                    Glyph = BoardGlyph[rng.Next(BoardGlyph.Length)],
                    Region = province ? MyRegion : Regions[rng.Next(Regions.Length)],
                    Score = score,
                });
                score -= 25 + rng.Next(0, 280);
            }
            return rows;
        }

        static int MyScore => Save.D.tier * 600 + Save.D.stars * 100 + Save.D.wins * 3;

        public void ShowBoard()
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.55f), true, "board");
            var b = Ui.Rect(layer, "b");
            b.anchoredPosition = new Vector2(0, -20);

            Ui.Box(b, new Vector2(46, 1260), new Vector2(-462, -40), Pal.Hex("4A3628"), null, "postL").pixelsPerUnitMultiplier = 1f;
            Ui.Box(b, new Vector2(46, 1260), new Vector2(462, -40), Pal.Hex("4A3628"), null, "postR").pixelsPerUnitMultiplier = 1f;
            Ui.Framed(b, new Vector2(900, 1210), new Vector2(0, -50), Pal.Hex("4F5B5A"), Edge, 7f, "body");
            var pagoda = Ui.Icon(b, "ui_pagoda", 1060, new Vector2(0, 640));
            pagoda.rectTransform.sizeDelta = new Vector2(1060, 1060f * 291f / 1208f);
            var title = Ui.Label(b, "排行榜", 80, Pal.Hex("3B2A20"), new Vector2(0, 640), new Vector2(400, 110), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(title, Pal.Hex("FFF6E2"), 2f);
            Ui.CloseX(layer, new Vector2(-450, 830), () => Destroy(layer.gameObject));

            var list = Ui.Rect(b, "list");
            bool province = true;
            void Fill()
            {
                for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
                BoardTab(list, "省榜", -170, province, () => { province = true; Fill(); });
                BoardTab(list, "总榜", 170, !province, () => { province = false; Fill(); });
                var rows = FakeBoard(province);
                int mine = -1;
                for (int i = 0; i < rows.Count; i++)
                    if (MyScore > rows[i].Score) { mine = i; break; }
                int shown = 0;
                for (int i = 0; shown < 6 && i < rows.Count; i++)
                {
                    if (shown == mine) { BoardRowView(list, shown, null); shown++; if (shown >= 6) break; }
                    BoardRowView(list, shown, rows[i]);
                    shown++;
                }
                MyRow(list, mine);
            }
            Fill();

            BuildClaimStrip(b);
            Tween.Pop(b, 0.28f);
        }

        void BoardTab(RectTransform parent, string text, float x, bool on, Action onClick)
        {
            var face = Ui.Framed(parent, new Vector2(290, 84), new Vector2(x, 470), on ? Pal.Hex("E9C46A") : Pal.Hex("33403F"), Pal.Hex("B8913C"), 4f, "framedTab");
            Ui.Stroke(Ui.Label(face.transform, text, 50, on ? Edge : Pal.Paper, new Vector2(0, 2), new Vector2(290, 84), TextAnchor.MiddleCenter, Fonts.Brush),
                on ? Pal.Hex("FFF2B8") : Edge, 1.5f);
            if (!on) Ui.Tap(face, onClick);
        }

        static void Avatar(Transform parent, Vector2 pos, string glyph, Color bg)
        {
            var a = Ui.Framed(parent, new Vector2(108, 108), pos, bg, Edge, 4f, "avatar");
            a.pixelsPerUnitMultiplier = 1.4f;
            Ui.Label(a.transform, glyph, 76, Pal.Ink, new Vector2(0, 4), new Vector2(108, 108), TextAnchor.MiddleCenter, Fonts.Brush);
        }

        void BoardRowView(RectTransform parent, int i, BoardRow? row)
        {
            float y = 340 - i * 146;
            if (row == null) { MyRowAt(parent, y, i); return; }
            var r = row.Value;
            var face = Ui.Framed(parent, new Vector2(840, 130), new Vector2(0, y), RowColors[Mathf.Min(i, 3)], Edge, 5f, "row");
            var t = face.transform;
            RankBadge(t, i);
            Avatar(t, new Vector2(-236, 0), r.Glyph, Pal.Tile);
            Ui.Label(t, r.Name, 30, Pal.Ink, new Vector2(-60, 26), new Vector2(220, 40), TextAnchor.MiddleLeft);
            Ui.Label(t, r.Region, 40, Pal.Ink, new Vector2(-60, -20), new Vector2(220, 56), TextAnchor.MiddleLeft, Fonts.Brush);
            Ui.Stroke(Ui.Label(t, Save.Tiers[Save.Tiers.Length - 1], 40, Pal.Hex("FFE36A"), new Vector2(290, 26), new Vector2(220, 50), TextAnchor.MiddleRight, Fonts.Brush), Edge, 2.5f);
            var sc = Ui.Label(t, "<color=#FFD84A>★</color> " + r.Score, 36, Pal.Paper, new Vector2(290, -24), new Vector2(240, 50), TextAnchor.MiddleRight);
            sc.supportRichText = true;
            Ui.Stroke(sc, Edge, 2f);
        }

        static void RankBadge(Transform t, int i)
        {
            if (i < 3)
            {
                Ui.Icon(t, "ui_crown" + (i + 1), 104, new Vector2(-352, 6));
                var n = Ui.Label(t, (i + 1).ToString(), 38, Edge, new Vector2(-352, -2), new Vector2(60, 60));
                n.fontStyle = FontStyle.Bold;
                return;
            }
            Ui.Box(t, new Vector2(78, 78), new Vector2(-352, 0), Edge, Art.Circle, "ringEdge").type = Image.Type.Simple;
            Ui.Box(t, new Vector2(68, 68), new Vector2(-352, 0), Pal.Tile, Art.Circle, "ring").type = Image.Type.Simple;
            Ui.Label(t, (i + 1).ToString(), 38, Edge, new Vector2(-352, 0), new Vector2(60, 60)).fontStyle = FontStyle.Bold;
        }

        void MyRowAt(RectTransform parent, float y, int rank)
        {
            var face = Ui.Framed(parent, new Vector2(840, 130), new Vector2(0, y), Pal.Hex("D9452F"), Pal.Hex("FFE36A"), 6f, "myRow");
            var t = face.transform;
            if (rank >= 0) RankBadge(t, rank);
            else Ui.Stroke(Ui.Label(t, "未上榜", 30, Pal.Paper, new Vector2(-352, 0), new Vector2(110, 60)), Edge, 2f).fontStyle = FontStyle.Bold;
            var av = Ui.Icon(t, "ui_avatar", 112, new Vector2(-236, 0));
            av.preserveAspect = true;
            Ui.Label(t, "我", 30, Pal.Paper, new Vector2(-60, 26), new Vector2(220, 40), TextAnchor.MiddleLeft);
            Ui.Stroke(Ui.Label(t, MyRegion, 40, Pal.Paper, new Vector2(-60, -20), new Vector2(220, 56), TextAnchor.MiddleLeft, Fonts.Brush), Edge, 2f);
            Ui.Stroke(Ui.Label(t, Save.RankName, 40, Pal.Hex("FFE36A"), new Vector2(290, 26), new Vector2(260, 50), TextAnchor.MiddleRight, Fonts.Brush), Edge, 2.5f);
            var st = Ui.Label(t, Ui.Stars(Save.D.stars, RankRules.Stars, "FFD84A", "6B2A20"), 38, Pal.Paper, new Vector2(290, -24), new Vector2(260, 50), TextAnchor.MiddleRight);
            st.supportRichText = true;
            Ui.Stroke(st, Edge, 1.5f);
        }

        void MyRow(RectTransform parent, int mine)
        {
            if (mine >= 0) return;
            MyRowAt(parent, 340 - 6 * 146 - 16, -1);
        }

        /// <summary>榜单下方：昨日座次发的俸禄，没领就亮着。</summary>
        void BuildClaimStrip(RectTransform b)
        {
            var face = Ui.Framed(b, new Vector2(860, 150), new Vector2(0, -790), Pal.Hex("C9402E"), Edge, 6f, "claim");
            var t = face.transform;
            int tier = Mathf.Clamp(Save.D.claimTier, 0, Save.Tiers.Length - 1);
            Ui.Label(t, "昨日座次", 28, Pal.Paper.A(0.85f), new Vector2(-280, 36), new Vector2(240, 40));
            Ui.Stroke(Ui.Label(t, Save.Tiers[tier], 44, Pal.Hex("FFE36A"), new Vector2(-280, -16), new Vector2(260, 60), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 2f);
            var seal = Ui.Framed(t, new Vector2(84, 84), new Vector2(-70, 0), Pal.Hex("F6C531"), Edge, 4f, "seal");
            Ui.Label(seal.transform, "赏", 60, Pal.RedDeep, new Vector2(0, 2), new Vector2(84, 84), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Icon(t, "coin", 70, new Vector2(30, 0));
            Ui.Stroke(Ui.Label(t, "×" + Save.DailyCoins, 44, Pal.Paper, new Vector2(120, 0), new Vector2(140, 60), TextAnchor.MiddleLeft), Edge, 2f).fontStyle = FontStyle.Bold;
            bool can = !Save.D.claimTaken && Save.D.claimDay == Save.Today;
            RectTransform btn = null;
            btn = Ui.CartoonBtn(t, can ? "领取" : "已领取", new Vector2(190, 96), new Vector2(310, 0), can ? Ui.BtnYellow : Ui.BtnDark, () =>
            {
                if (Save.D.claimTaken || Save.D.claimDay != Save.Today) { Toast("明天按今天的座次再发俸禄"); return; }
                Save.D.coins += Save.DailyCoins;
                Save.D.claimTaken = true;
                Save.Write();
                Refresh();
                Sfx.Play(Snd.Rain);
                Toast("领到俸禄 铜钱 +" + Save.DailyCoins);
                Ui.SetBtnText(btn, "已领取");
                Ui.SetBtnFace(btn, Ui.BtnDark);
            }, 42, null, can ? Edge : Pal.Paper);
        }

        // ─────────────────────────── 昨日俸禄弹窗 ───────────────────────────

        public void ShowDaily()
        {
            int coins = Save.DailyCoins;
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.72f), true, "daily");
            var card = Ui.WoodPanel(layer, new Vector2(860, 820), Vector2.zero, Pal.Hex("C9402E"), Pal.Hex("F3E6C6"));
            Ui.Title(card, "昨日俸禄", new Vector2(0, 400), 86);
            Ui.Label(card, "昨日座次", 40, Pal.InkSoft, new Vector2(0, 250), new Vector2(760, 60));
            Ui.Stroke(Ui.Label(card, Save.Tiers[Mathf.Clamp(Save.D.claimTier, 0, Save.Tiers.Length - 1)], 96, Pal.Hex("C0392B"), new Vector2(0, 150),
                new Vector2(760, 130), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 3f);
            var st = Ui.Label(card, Ui.Stars(Save.D.claimStars, RankRules.Stars), 76, Pal.Gold, new Vector2(0, 40), new Vector2(760, 100));
            st.supportRichText = true;
            Ui.Stroke(st, Edge, 3f);
            var seal = Ui.Framed(card, new Vector2(120, 120), new Vector2(-170, -90), Pal.Hex("F6C531"), Edge, 5f, "seal");
            Ui.Label(seal.transform, "赏", 86, Pal.RedDeep, new Vector2(0, 4), new Vector2(120, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Icon(card, "coin", 100, new Vector2(-20, -90));
            var amount = Ui.Label(card, "×" + coins, 64, Pal.Gold, new Vector2(130, -90), new Vector2(240, 100), TextAnchor.MiddleLeft);
            amount.fontStyle = FontStyle.Bold;
            Ui.Stroke(amount, Edge, 2f);
            void Take(int mul)
            {
                if (!Save.D.claimTaken)
                {
                    Save.D.coins += coins * mul;
                    Save.D.claimTaken = true;
                    Save.Write();
                    Refresh();
                    Sfx.Play(Snd.Rain);
                    Toast("领到俸禄 铜钱 +" + coins * mul);
                }
                Destroy(layer.gameObject);
            }
            Ui.CartoonBtn(card, "双倍领取", new Vector2(520, 124), new Vector2(0, -230), Ui.BtnRed, () => AdStub.Show("双倍俸禄", () => Take(2)), 54, "ui_video");
            Ui.CartoonBtn(card, "领取", new Vector2(340, 100), new Vector2(0, -360), Ui.BtnDark, () => Take(1), 46);
            Tween.Pop(card, 0.28f);
        }

        // ─────────────────────────── 武器装备 ───────────────────────────

        static readonly Color[] TierColors = { Pal.Hex("6FBF3A"), Pal.Hex("3FA9E0"), Pal.Hex("38B8C8"), Pal.Hex("B45CE0"), Pal.Hex("F08A24") };
        static Color TierColor(int n) => TierColors[Mathf.Clamp(n, 1, 5) - 1];

        RectTransform _armory;
        int _armSel = -1;

        public void ShowArmory()
        {
            if (_armory) Destroy(_armory.gameObject);
            _armory = Ui.Fill(_root, Pal.Hex("86AE5E"), true, "armory");
            var a = _armory;
            Ui.Box(a, new Vector2(1080, 300), new Vector2(0, 820), Pal.Hex("9CC473"), Art.White, "sky").type = Image.Type.Simple;
            for (int i = 0; i < 9; i++)
            {
                var tuft = Ui.Icon(a, "bush", 90 + (i % 3) * 20, new Vector2(-480 + i * 120 + (i % 2) * 30, 120 - (i % 3) * 70));
                tuft.color = Color.white.A(0.35f);
            }
            Ui.Box(a, new Vector2(1080, 820), new Vector2(0, -560), Pal.Hex("6E5536"), Art.White, "earth").type = Image.Type.Simple;
            Ui.Box(a, new Vector2(1080, 14), new Vector2(0, -150), Pal.Hex("4E3A24"), Art.White, "earthEdge").type = Image.Type.Simple;
            for (int i = 0; i < 14; i++)
            {
                var stone = Ui.Box(a, new Vector2(24 + (i % 4) * 8, 14 + (i % 3) * 5), new Vector2(-500 + (i * 157) % 1000, -200 - (i * 89) % 640), Pal.Hex("5E4730"), Art.Circle, "pebble");
                stone.type = Image.Type.Simple;
            }

            var top = Ui.Rect(a, "top");
            Ui.Anchor(top, 0.5f, 1f);
            Ui.Box(top, new Vector2(260, 76), new Vector2(-330, -80), Pal.Hex("3B2A20").A(0.92f), null, "coinBar");
            Ui.Icon(top, "coin", 90, new Vector2(-450, -80));
            Ui.Label(top, Save.D.coins.ToString(), 44, Pal.Hex("FFE7A8"), new Vector2(-300, -80), new Vector2(180, 76)).fontStyle = FontStyle.Bold;
            var back = Ui.Framed(top, new Vector2(92, 92), new Vector2(-470, -200), Pal.Hex("EFE6CF"), Edge, 5f, "framedBack");
            Ui.Label(back.transform, "◀", 46, Edge, new Vector2(-2, 0), new Vector2(92, 92));
            Ui.Tap(back, () => { if (_armory) Destroy(_armory.gameObject); _armory = null; Refresh(); });
            Ui.Icon(top, "ui_shield", 110, new Vector2(150, -110));
            Ui.Title(top, "武器装备", new Vector2(0, -200), 84, Pal.Hex("F7D35A"));

            var owned = new List<HeroDef>();
            foreach (var d in HeroDef.All)
                if (!string.IsNullOrEmpty(d.Weapon) && Save.WeaponCount((int)d.Id) > 0) owned.Add(d);
            if (_armSel >= 0 && Save.WeaponCount(_armSel) <= 0) _armSel = -1;

            for (int i = 0; i < owned.Count && i < 10; i++)
            {
                var d = owned[i];
                int n = Mathf.Min(5, Save.WeaponCount((int)d.Id));
                bool sel = (int)d.Id == _armSel;
                bool on = Save.D.armed.Contains((int)d.Id);
                var pos = new Vector2(-392 + (i % 5) * 196, 640 - (i / 5) * 110);
                var face = Ui.Framed(a, new Vector2(180, 92), pos, sel ? Pal.Hex("FFE58A") : Pal.Hex("EFE6CF"), sel ? Pal.Hex("D9822B") : Edge, sel ? 7f : 4f, "framedWeapon");
                face.pixelsPerUnitMultiplier = 1.2f;
                Ui.Label(face.transform, d.Weapon, 32, Pal.Ink, new Vector2(0, 16), new Vector2(180, 44), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Label(face.transform, n + "/5", 26, TierColor(n), new Vector2(0, -22), new Vector2(180, 36)).fontStyle = FontStyle.Bold;
                if (on)
                {
                    var dot = Ui.Box(face.transform, new Vector2(40, 40), new Vector2(84, 40), Pal.Hex("D7362B"), Art.Circle, "on");
                    dot.type = Image.Type.Simple;
                    Ui.Label(dot.transform, "装", 24, Pal.Paper, Vector2.zero, new Vector2(40, 40), TextAnchor.MiddleCenter, Fonts.Brush);
                }
                var id = (int)d.Id;
                Ui.Tap(face, () => { _armSel = id; ShowArmory(); });
            }
            if (owned.Count == 0)
            {
                var none = Ui.Label(a, "兵器囊空空如也\n打仗撑过四波，就有机会缴获兵器", 40, Pal.Paper, new Vector2(0, 360), new Vector2(900, 160));
                Ui.Stroke(none, Pal.Hex("3E5A2A"), 2.5f);
            }

            var pager = Ui.Framed(a, new Vector2(230, 84), new Vector2(-60, -70), Pal.Hex("3B3530"), Edge, 4f, "pager");
            Ui.Label(pager.transform, "◀  1/1  ▶", 36, Pal.Paper, Vector2.zero, new Vector2(230, 84));
            Ui.CartoonBtn(a, "一键回收", new Vector2(230, 84), new Vector2(340, -70), Ui.BtnYellow, () =>
            {
                int got = RecycleSpare();
                if (got <= 0) { Toast("装备中的那一件先留着"); return; }
                ShowArmory();
            }, 40, null, Edge);
            Ui.Label(a, $"已装备 {Save.D.armed.Count}/{Save.MaxArmed}", 30, Pal.Paper, new Vector2(-370, -70), new Vector2(240, 50)).fontStyle = FontStyle.Bold;

            Ui.Stroke(Ui.Label(a, "我的武将", 56, Pal.Paper, new Vector2(0, -205), new Vector2(500, 80), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 3f);
            for (int i = 0; i < HeroDef.All.Length; i++)
            {
                var d = HeroDef.All[i];
                int k = (int)d.Id;
                bool have = Save.D.heroes.Contains(k);
                bool armed = Save.D.armed.Contains(k);
                var pos = new Vector2(-345 + (i % 4) * 230, -310 - (i / 4) * 122);
                var face = Ui.Framed(a, new Vector2(212, 106), pos, armed ? Pal.Hex("FFE58A") : have ? Pal.Hex("E9E4DA") : Pal.Hex("A39C92"), Edge, 4f, "framedHero");
                face.pixelsPerUnitMultiplier = 1.2f;
                bool sub = armed || !have || d.Hidden;
                Ui.Label(face.transform, have || !d.Hidden ? d.Name : "？？", d.Name.Length > 2 ? 34 : 40, have ? Pal.Ink : Pal.InkSoft,
                    new Vector2(0, sub ? 16 : 0), new Vector2(212, 56), TextAnchor.MiddleCenter, Fonts.Brush);
                if (armed) Ui.Label(face.transform, d.Weapon, 24, Pal.Hex("2F6FB8"), new Vector2(0, -28), new Vector2(212, 34)).fontStyle = FontStyle.Bold;
                else if (!have) Ui.Label(face.transform, d.Hidden ? "隐藏好汉" : "未入谱", 24, Pal.Paper, new Vector2(0, -28), new Vector2(212, 34)).fontStyle = FontStyle.Bold;
                Ui.Tap(face, () =>
                {
                    if (!have) { Toast("先在对局里凑齐名字，让他入谱"); return; }
                    if (string.IsNullOrEmpty(d.Weapon)) { Toast(d.Name + "不使兵器"); return; }
                    if (Save.WeaponCount(k) <= 0) { Toast($"还没缴获{d.Name}的{d.Weapon}"); return; }
                    ToggleArm(d.Id);
                    ShowArmory();
                });
            }

            if (_armSel >= 0) WeaponDetail(a, HeroDef.Get((HeroId)_armSel));
        }

        public void SelectWeapon(int hero)
        {
            _armSel = hero;
            ShowArmory();
        }

        void WeaponDetail(RectTransform a, HeroDef d)
        {
            int k = (int)d.Id;
            int n = Mathf.Min(5, Save.WeaponCount(k));
            bool on = Save.D.armed.Contains(k);
            var holder = Ui.Rect(a, "detail");
            holder.anchoredPosition = new Vector2(0, 250);
            var card = Ui.Framed(holder, new Vector2(660, 560), Vector2.zero, Pal.Hex("F6EEDA"), Edge, 7f, "framedDetail");
            var t = card.transform;
            Ui.CloseX(holder, new Vector2(-320, 270), () => { _armSel = -1; ShowArmory(); }, 96);
            Ui.Stroke(Ui.Label(t, d.Weapon, 72, Pal.Hex("D9542F"), new Vector2(0, 200), new Vector2(600, 100), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
            Ui.Label(t, $"{d.Nick}{d.Name}的兵器。装备后{d.Name}伤害大增，\n缴获越多阶越高（五阶封顶）。", 30, Pal.Ink, new Vector2(0, 100), new Vector2(600, 100), TextAnchor.MiddleLeft);

            var box = Ui.Box(t, new Vector2(580, 190), new Vector2(0, -60), Pal.Hex("E6D9BC"), null, "statBox");
            box.pixelsPerUnitMultiplier = 1f;
            Ui.Label(t, "品质：", 34, Pal.Ink, new Vector2(-190, -10), new Vector2(160, 50), TextAnchor.MiddleLeft);
            for (int i = 0; i < 5; i++)
            {
                var seg = Ui.Box(t, new Vector2(62, 30), new Vector2(-44 + i * 64, -10), TierColors[i].A(i < n ? 1f : 0.35f), null, "seg");
                seg.pixelsPerUnitMultiplier = 2f;
            }
            Ui.Label(t, "▼", 30, Edge, new Vector2(-44 + (n - 1) * 64, 26), new Vector2(40, 36));
            Ui.Label(t, "攻击力：", 34, Pal.Ink, new Vector2(-190, -90), new Vector2(180, 50), TextAnchor.MiddleLeft);
            int pct = Mathf.RoundToInt((HeroDef.ArmedMul(n) - 1f) * 100f);
            var val = Ui.Label(t, "+" + pct + "%", 54, Pal.Hex("C0392B"), new Vector2(40, -90), new Vector2(240, 70), TextAnchor.MiddleLeft);
            val.fontStyle = FontStyle.Bold;
            Ui.Label(t, n + "阶", 30, TierColor(n), new Vector2(210, -90), new Vector2(100, 50)).fontStyle = FontStyle.Bold;

            Ui.CartoonBtn(t, on ? "卸下" : "装备", new Vector2(240, 96), new Vector2(-140, -220), on ? Ui.BtnDark : Ui.BtnRed, () => { ToggleArm(d.Id); ShowArmory(); }, 44);
            Ui.CartoonBtn(t, "回收+" + Save.RecycleCoins, new Vector2(240, 96), new Vector2(140, -220), Ui.BtnYellow, () => { Recycle(d.Id); ShowArmory(); }, 38, null, Edge);
            Tween.Pop(holder, 0.22f);
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
                Sfx.Play(Snd.Merge);
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

        // ─────────────────────────── 好汉谱 ───────────────────────────

        public void ShowCodex()
        {
            var layer = Panel("好汉谱", out var card, new Vector2(1000, 1640));
            int got = 0;
            foreach (var d in HeroDef.All) if (Save.D.heroes.Contains((int)d.Id)) got++;
            var bar = Ui.Framed(card, new Vector2(620, 44), new Vector2(0, 690), Pal.Hex("5E4532"), Edge, 4f, "progress");
            float ratio = got / (float)HeroDef.All.Length;
            var fill = Ui.Box(bar.transform, new Vector2(Mathf.Max(30, 620 * ratio), 44), new Vector2(-310 + Mathf.Max(30, 620 * ratio) / 2, 0), Pal.Hex("E9B53A"), null, "fill");
            fill.pixelsPerUnitMultiplier = 1f;
            Ui.Stroke(Ui.Label(bar.transform, $"已入谱 {got}/{HeroDef.All.Length}", 30, Pal.Paper, Vector2.zero, new Vector2(620, 44)), Edge, 2f).fontStyle = FontStyle.Bold;

            for (int i = 0; i < HeroDef.All.Length; i++)
            {
                var d = HeroDef.All[i];
                bool have = Save.D.heroes.Contains((int)d.Id);
                var pos = new Vector2(-342 + (i % 4) * 228, 500 - (i / 4) * 300);
                Color bg = have ? (d.Disha ? Pal.Hex("E4D2F2") : Pal.TileHero) : Pal.Hex("B8AE9C");
                Color rim = have ? (d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E")) : Edge;
                var face = Ui.Framed(card, new Vector2(206, 282), pos, bg, rim, have ? 7f : 4f, "framedHero");
                face.pixelsPerUnitMultiplier = 0.9f;
                var t = face.transform;
                if (have)
                {
                    Ui.Tag(t, d.Disha ? "地煞" : "天罡", d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E"), new Vector2(0, 112), 90, 24);
                    var nm = Ui.Label(t, d.Name, d.Name.Length > 2 ? 60 : 80, Pal.Ink, new Vector2(0, 14), new Vector2(200, 130), TextAnchor.MiddleCenter, Fonts.Brush);
                    nm.lineSpacing = 0.9f;
                    Ui.Label(t, d.Nick, 28, Pal.RedDeep, new Vector2(0, -78), new Vector2(200, 40)).fontStyle = FontStyle.Bold;
                    Ui.Label(t, d.TypeLabel + (string.IsNullOrEmpty(d.Weapon) ? "" : " · " + d.Weapon), 22, Pal.InkSoft, new Vector2(0, -112), new Vector2(200, 32));
                }
                else
                {
                    Ui.Label(t, d.Hidden ? "隐" : "？", 110, Pal.Ink.A(0.25f), new Vector2(0, 20), new Vector2(200, 150), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(t, d.Hidden ? "隐藏好汉" : FirstChapter(d.Id), 26, Pal.Paper, new Vector2(0, -96), new Vector2(200, 40)).fontStyle = FontStyle.Bold;
                }
                Ui.Tap(face, () => HeroDetail(layer, d, have));
            }
            Ui.Label(card, "凑齐金字名字即可入谱 · 点卡片看详情", 30, Pal.InkSoft, new Vector2(0, -720), new Vector2(900, 50));
        }

        void HeroDetail(RectTransform layer, HeroDef d, bool have)
        {
            var shade = Ui.Fill(layer, new Color(0, 0, 0, 0.5f), true, "heroDetail");
            var card = Ui.Framed(shade, new Vector2(800, 820), new Vector2(0, 20), Pal.Hex("F6EEDA"), have && d.Disha ? Pal.Hex("8A54C0") : Edge, 8f, "framedDetail");
            var t = card.transform;
            Ui.CloseX(shade, new Vector2(-390, 420), () => Destroy(shade.gameObject), 100);
            if (have)
            {
                Ui.Tag(t, d.Disha ? "地煞星" : "天罡星", d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E"), new Vector2(0, 330), 150, 30);
                Ui.Label(t, d.Nick, 40, Pal.RedDeep, new Vector2(0, 250), new Vector2(700, 60)).fontStyle = FontStyle.Bold;
                Ui.Stroke(Ui.Label(t, d.Name, 150, Pal.Ink, new Vector2(0, 120), new Vector2(760, 190), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                string weapon = string.IsNullOrEmpty(d.Weapon) ? "不使兵器" : $"兵器：{d.Weapon}（已缴获 {Save.WeaponCount((int)d.Id)} 件）";
                Ui.Label(t, $"类型：{d.TypeLabel}\n本事：{d.Skill}\n大招：{(string.IsNullOrEmpty(d.Ult) ? "无" : d.Ult)}\n{weapon}", 34, Pal.Ink,
                    new Vector2(0, -110), new Vector2(700, 260), TextAnchor.MiddleLeft);
                Ui.Label(t, "「" + d.Quote + "」", 34, Pal.InkSoft, new Vector2(0, -320), new Vector2(720, 60));
            }
            else
            {
                Ui.Label(t, d.Hidden ? "隐" : "？", 220, Pal.Ink.A(0.25f), new Vector2(0, 120), new Vector2(400, 260), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Label(t, d.Hidden ? "据说在某一回凑出他的名字……\n他卖的炊饼特别香。" : $"在「{FirstChapter(d.Id)}」把 {d.Name.Length} 个金字\n按顺序摆好即可入谱", 40, Pal.Ink,
                    new Vector2(0, -150), new Vector2(700, 200));
            }
            Tween.Pop(card.transform.parent, 0.22f);
        }

        static string FirstChapter(HeroId id)
        {
            foreach (var ch in ChapterDef.All)
                if (Array.IndexOf(ch.Pool, id) >= 0) return ch.Name;
            return "";
        }
    }
}
