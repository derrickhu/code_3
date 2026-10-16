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
            int score = (province ? 2300 : 2900) + rng.Next(0, 300);
            for (int i = 0; i < 6; i++)
            {
                rows.Add(new BoardRow
                {
                    Name = BoardNames[rng.Next(BoardNames.Length)],
                    Glyph = BoardGlyph[rng.Next(BoardGlyph.Length)],
                    Region = province ? MyRegion : Regions[rng.Next(Regions.Length)],
                    Score = score,
                });
                score -= 25 + rng.Next(0, 180);
            }
            return rows;
        }

        static int MyScore => RankRules.Score(Save.D.tier, Save.D.stars) + Mathf.Min(Save.D.wins, 30);

        public void ShowBoard()
        {
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.55f), true, "board");
            var b = Ui.Rect(Ui.Fit(layer, 1880), "b");
            b.anchoredPosition = new Vector2(0, -20);

            Ui.Box(b, new Vector2(46, 1260), new Vector2(-462, -40), Pal.Hex("4A3628"), null, "postL").pixelsPerUnitMultiplier = 1f;
            Ui.Box(b, new Vector2(46, 1260), new Vector2(462, -40), Pal.Hex("4A3628"), null, "postR").pixelsPerUnitMultiplier = 1f;
            Ui.Framed(b, new Vector2(900, 1210), new Vector2(0, -50), Pal.Hex("4F5B5A"), Edge, 7f, "body");
            var pagoda = Ui.Icon(b, "ui_pagoda", 1060, new Vector2(0, 640));
            pagoda.rectTransform.sizeDelta = new Vector2(1060, 1060f * 291f / 1208f);
            var title = Ui.Label(b, "排行榜", 80, Pal.Hex("3B2A20"), new Vector2(0, 640), new Vector2(400, 110), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(title, Pal.Hex("FFF6E2"), 2f);
            Ui.CloseX(b, new Vector2(-450, 850), () => Destroy(layer.gameObject));

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
            Ui.Stroke(Ui.Label(t, RankRules.Name(r.Score / 100), 38, Pal.Hex("FFE36A"), new Vector2(270, 26), new Vector2(300, 50), TextAnchor.MiddleRight, Fonts.Brush), Edge, 2.5f);
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
            Ui.Stroke(Ui.Label(t, Save.RankName, 38, Pal.Hex("FFE36A"), new Vector2(270, 26), new Vector2(300, 50), TextAnchor.MiddleRight, Fonts.Brush), Edge, 2.5f);
            var st = Ui.Label(t, Ui.RankStars(Save.D.tier, Save.D.stars, "FFD84A", "6B2A20"), 38, Pal.Paper, new Vector2(290, -24), new Vector2(260, 50), TextAnchor.MiddleRight);
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
            Ui.Label(t, "昨日座次", 28, Pal.Paper.A(0.85f), new Vector2(-280, 36), new Vector2(240, 40));
            Ui.Stroke(Ui.Label(t, RankRules.Name(Save.D.claimTier), 38, Pal.Hex("FFE36A"), new Vector2(-280, -16), new Vector2(260, 60), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 2f);
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
            var card = Ui.WoodPanel(Ui.Fit(layer, 1000), new Vector2(860, 820), Vector2.zero, Pal.Hex("C9402E"), Pal.Hex("F3E6C6"));
            Ui.Title(card, "昨日俸禄", new Vector2(0, 400), 86);
            Ui.Label(card, "昨日座次", 40, Pal.InkSoft, new Vector2(0, 250), new Vector2(760, 60));
            Ui.RankBadge(card, Save.D.claimTier, new Vector2(-300, 150), 0.9f);
            Ui.Stroke(Ui.Label(card, RankRules.Name(Save.D.claimTier), 80, Pal.Hex("C0392B"), new Vector2(0, 150),
                new Vector2(760, 130), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 3f);
            var st = Ui.Label(card, Ui.RankStars(Save.D.claimTier, Save.D.claimStars), 76, Pal.Gold, new Vector2(0, 40), new Vector2(760, 100));
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

        // ─────────────────────────── 兵器谱 ───────────────────────────

        RectTransform _armory;
        int _armSel = -1;
        int _armFilter;
        static readonly string[] ArmTabs = { "全部", "已缴获", "神兵", "宝器", "利器", "凡器" };

        static List<HeroDef> WeaponHeroes()
        {
            var list = new List<HeroDef>();
            foreach (var d in HeroDef.All) if (!string.IsNullOrEmpty(d.Weapon)) list.Add(d);
            list.Sort((a, b) =>
            {
                int g = b.WGrade.CompareTo(a.WGrade);
                return g != 0 ? g : (a.Rank <= 0 ? 999 : a.Rank).CompareTo(b.Rank <= 0 ? 999 : b.Rank);
            });
            return list;
        }

        bool ArmPass(HeroDef d)
        {
            switch (_armFilter)
            {
                case 1: return Save.WeaponCount((int)d.Id) > 0;
                case 2: return d.WGrade == 3;
                case 3: return d.WGrade == 2;
                case 4: return d.WGrade == 1;
                case 5: return d.WGrade == 0;
                default: return true;
            }
        }

        public void ShowArmory()
        {
            if (_armory) Destroy(_armory.gameObject);
            _armory = Ui.Fill(_root, Pal.Hex("6E5536"), true, "armory");
            var a = _armory;
            var top = Ui.Rect(a, "top");
            Ui.Anchor(top, 0.5f, 1f);
            const float grassH = 570f;
            Ui.Box(top, new Vector2(1600, grassH), new Vector2(0, -grassH / 2), Pal.Hex("86AE5E"), Art.White, "grass").type = Image.Type.Simple;
            for (int i = 0; i < 9; i++)
            {
                var tuft = Ui.Icon(top, "bush", 90 + (i % 3) * 20, new Vector2(-480 + i * 120 + (i % 2) * 30, -300 - (i % 3) * 70));
                tuft.color = Color.white.A(0.3f);
            }
            Ui.Box(top, new Vector2(1600, 14), new Vector2(0, -grassH), Pal.Hex("4E3A24"), Art.White, "earthEdge").type = Image.Type.Simple;

            Ui.Box(top, new Vector2(260, 76), new Vector2(-330, -80), Pal.Hex("3B2A20").A(0.92f), null, "coinBar");
            Ui.Icon(top, "coin", 90, new Vector2(-450, -80));
            Ui.Label(top, Save.D.coins.ToString(), 44, Pal.Hex("FFE7A8"), new Vector2(-300, -80), new Vector2(180, 76)).fontStyle = FontStyle.Bold;
            var back = Ui.Framed(top, new Vector2(92, 92), new Vector2(-470, -200), Pal.Hex("EFE6CF"), Edge, 5f, "framedBack");
            Ui.Label(back.transform, "◀", 46, Edge, new Vector2(-2, 0), new Vector2(92, 92));
            Ui.Tap(back, () => { if (_armory) Destroy(_armory.gameObject); _armory = null; Refresh(); });
            Ui.Icon(top, "ui_shield", 100, new Vector2(400, -80));
            Ui.Title(top, "兵器谱", new Vector2(0, -200), 84, Pal.Hex("F7D35A"));

            if (_armSel >= 0 && string.IsNullOrEmpty(HeroDef.Get((HeroId)_armSel).Weapon)) _armSel = -1;

            // 三个装备槽
            for (int i = 0; i < Save.MaxArmed; i++)
            {
                var pos = new Vector2(-320 + i * 320, -375);
                bool has = i < Save.D.armed.Count;
                var d = has ? HeroDef.Get((HeroId)Save.D.armed[i]) : null;
                var g = has ? Ui.GradeColor(d.WGrade) : Pal.Hex("8C8478");
                var face = Ui.Framed(top, new Vector2(290, 170), pos, has ? Pal.Hex("F6EEDA") : Pal.Hex("5E5246"), g, has ? 7f : 4f, "framedSlot");
                var t = face.transform;
                if (has)
                {
                    Ui.Box(t, new Vector2(118, 118), new Vector2(-80, 0), g.A(0.22f), Art.Circle, "halo").type = Image.Type.Simple;
                    Ui.Icon(t, Weapons.Icon(d.WKind), 104, new Vector2(-80, 0));
                    Ui.Label(t, d.Weapon, d.Weapon.Length > 4 ? 26 : 32, Pal.Ink, new Vector2(50, 30), new Vector2(170, 44), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(t, d.Name, 28, Pal.InkSoft, new Vector2(50, -12), new Vector2(170, 40));
                    Ui.Label(t, Weapons.Grades[d.WGrade] + " · " + Mathf.Min(5, Save.WeaponCount((int)d.Id)) + "阶", 24, g, new Vector2(50, -50), new Vector2(170, 34)).fontStyle = FontStyle.Bold;
                    int id = (int)d.Id;
                    Ui.Tap(face, () => { _armSel = id; ShowArmory(); });
                }
                else
                {
                    Ui.Label(t, "＋", 70, Pal.Paper.A(0.5f), new Vector2(0, 16), new Vector2(200, 90));
                    Ui.Label(t, "空槽 · 点兵器装备", 26, Pal.Paper.A(0.7f), new Vector2(0, -50), new Vector2(280, 40));
                }
            }
            Ui.Stroke(Ui.Label(top, $"已装备 {Save.D.armed.Count}/{Save.MaxArmed}　已缴获 {Save.D.weapons.Count}/{Save.MaxWeapons} 件", 30, Pal.Paper,
                new Vector2(-130, -508), new Vector2(680, 50), TextAnchor.MiddleLeft), Edge, 2f);
            Ui.CartoonBtn(top, "一键回收", new Vector2(240, 80), new Vector2(370, -508), Ui.BtnYellow, () =>
            {
                int got = RecycleSpare();
                if (got <= 0) { Toast("没有多余的兵器可回收"); return; }
                ShowArmory();
            }, 38, null, Edge);

            for (int i = 0; i < ArmTabs.Length; i++)
            {
                int k = i;
                bool on = _armFilter == i;
                var tab = Ui.Framed(top, new Vector2(156, 70), new Vector2(-415 + i * 166, -625), on ? Pal.Hex("E9C46A") : Pal.Hex("4A3628"), Pal.Hex("B8913C"), 4f, "framedTab");
                Ui.Label(tab.transform, ArmTabs[i], 34, on ? Edge : Pal.Paper, new Vector2(0, 2), new Vector2(156, 70), TextAnchor.MiddleCenter, Fonts.Brush);
                if (!on) Ui.Tap(tab, () => { _armFilter = k; ShowArmory(); });
            }

            var list = new List<HeroDef>();
            foreach (var d in WeaponHeroes()) if (ArmPass(d)) list.Add(d);
            const int cols = 4;
            const float cw = 240f, ch = 232f;
            float h = Mathf.Ceil(list.Count / (float)cols) * ch + 30;
            const float listTop = 675f;
            float screenH = Mathf.Max(1400f, Ui.Root.rect.height);
            float viewH = screenH - listTop - 30f;
            var content = Ui.Scroll(a, new Vector2(1000, viewH), new Vector2(0, screenH / 2 - listTop - viewH / 2), h, "weapons");
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                var pos = Ui.Top(content, -360 + (i % cols) * cw, 20 + (i / cols) * ch + ch * 0.5f);
                WeaponCard(content, d, pos);
            }
            if (list.Count == 0)
                Ui.Stroke(Ui.Label(content, "这一类还没有兵器\n打仗撑过四波，就有机会缴获", 40, Pal.Paper, Ui.Top(content, 0, 200), new Vector2(900, 160)), Edge, 2f);

            if (_armSel >= 0) WeaponDetail(a, HeroDef.Get((HeroId)_armSel));
        }

        void WeaponCard(RectTransform parent, HeroDef d, Vector2 pos)
        {
            int k = (int)d.Id;
            int n = Mathf.Min(5, Save.WeaponCount(k));
            bool own = n > 0;
            bool on = Save.D.armed.Contains(k);
            var g = Ui.GradeColor(d.WGrade);
            var face = Ui.Framed(parent, new Vector2(222, 214), pos, own ? Pal.Hex("F6EEDA") : Pal.Hex("8F8577"), own ? g : Edge, own ? 6f : 4f, "framedWeapon");
            var t = face.transform;
            Ui.Box(t, new Vector2(222, 30), new Vector2(0, 92), g.A(own ? 1f : 0.45f), null, "gradeBand").pixelsPerUnitMultiplier = 1f;
            Ui.Label(t, Weapons.Grades[d.WGrade], 22, Pal.Paper, new Vector2(0, 92), new Vector2(200, 30)).fontStyle = FontStyle.Bold;
            if (own) Ui.Box(t, new Vector2(110, 110), new Vector2(0, 22), g.A(0.2f), Art.Circle, "halo").type = Image.Type.Simple;
            var icon = Ui.Icon(t, Weapons.Icon(d.WKind), 100, new Vector2(0, 22));
            if (!own) icon.color = new Color(0.2f, 0.18f, 0.15f, 0.55f);
            Ui.Label(t, d.Weapon, d.Weapon.Length > 4 ? 24 : 30, own ? Pal.Ink : Pal.Paper.A(0.8f), new Vector2(0, -50), new Vector2(214, 40), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(t, own ? $"{d.Name} · {n}阶" : d.Name + " · 未缴获", 22, own ? Pal.InkSoft : Pal.Paper.A(0.7f), new Vector2(0, -84), new Vector2(214, 30));
            if (on)
            {
                var dot = Ui.Box(t, new Vector2(46, 46), new Vector2(86, 62), Pal.Hex("D7362B"), Art.Circle, "on");
                dot.type = Image.Type.Simple;
                Ui.Label(dot.transform, "装", 26, Pal.Paper, Vector2.zero, new Vector2(46, 46), TextAnchor.MiddleCenter, Fonts.Brush);
            }
            Ui.Tap(face, () => { _armSel = k; ShowArmory(); });
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
            bool own = n > 0;
            bool on = Save.D.armed.Contains(k);
            var g = Ui.GradeColor(d.WGrade);
            var shade = Ui.Fill(a, new Color(0, 0, 0, 0.55f), true, "detail");
            var holder = Ui.Rect(Ui.Fit(shade, 1160), "holder");
            holder.anchoredPosition = new Vector2(0, 40);
            Ui.Framed(holder, new Vector2(820, 1000), Vector2.zero, Pal.Hex("F6EEDA"), g, 9f, "framedDetail");
            var t = holder;
            Ui.CloseX(holder, new Vector2(-400, 490), () => { _armSel = -1; ShowArmory(); }, 100);
            var clip = Ui.Rect(t, "raysClip");
            clip.anchoredPosition = new Vector2(0, 300);
            clip.sizeDelta = new Vector2(780, 360);
            clip.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Icon(clip, "ui_rays", 640, Vector2.zero, "rays");
            rays.color = g.A(own ? 0.55f : 0.2f);
            Ui.Spin(rays.transform, 12f);
            var icon = Ui.Icon(t, Weapons.Icon(d.WKind), 240, new Vector2(0, 300));
            if (!own) icon.color = new Color(0.25f, 0.22f, 0.18f, 0.6f);
            Tween.Pop(icon.transform, 0.35f, 0.05f);
            Ui.Tag(t, Weapons.Grades[d.WGrade], g, new Vector2(300, 440), 140, 32);
            Ui.Stroke(Ui.Label(t, d.Weapon, 76, Pal.Hex("B23A22"), new Vector2(0, 130), new Vector2(760, 100), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2.5f);
            Ui.Label(t, $"{d.Nick}{d.Name}的{Weapons.KindName(d.WKind)}", 34, Pal.InkSoft, new Vector2(0, 60), new Vector2(760, 50));

            var box = Ui.Box(t, new Vector2(740, 330), new Vector2(0, -130), Pal.Hex("E6D9BC"), null, "statBox");
            box.pixelsPerUnitMultiplier = 1f;
            Ui.Label(t, "特性：", 32, Pal.Ink, new Vector2(-260, -5), new Vector2(160, 50), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            Ui.Label(t, Weapons.AffixText(d.WKind), 30, Pal.RedDeep, new Vector2(70, -5), new Vector2(520, 50), TextAnchor.MiddleLeft);
            Ui.Label(t, "阶数：", 32, Pal.Ink, new Vector2(-260, -85), new Vector2(160, 50), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            for (int i = 0; i < 5; i++)
            {
                var seg = Ui.Box(t, new Vector2(70, 30), new Vector2(-90 + i * 76, -85), g.A(i < n ? 1f : 0.25f), null, "seg");
                seg.pixelsPerUnitMultiplier = 2f;
            }
            Ui.Label(t, "伤害：", 32, Pal.Ink, new Vector2(-260, -165), new Vector2(160, 50), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
            int pct = Mathf.RoundToInt((HeroDef.ArmedMul(Mathf.Max(1, n), d.WGrade) - 1f) * 100f);
            int next = Mathf.RoundToInt((HeroDef.ArmedMul(Mathf.Min(5, n + 1), d.WGrade) - 1f) * 100f);
            var val = Ui.Label(t, "+" + pct + "%", 52, Pal.Hex("C0392B"), new Vector2(-50, -165), new Vector2(200, 70), TextAnchor.MiddleLeft);
            val.fontStyle = FontStyle.Bold;
            Ui.Label(t, n >= 5 ? "已满阶" : own ? $"再缴获一件 → +{next}%" : "未缴获", 28, Pal.InkSoft, new Vector2(200, -165), new Vector2(320, 50), TextAnchor.MiddleLeft);
            Ui.Label(t, "品级越高底子越厚：凡器 +30%　利器 +40%　宝器 +50%　神兵 +65%，每多一阶 +15%", 24, Pal.InkSoft,
                new Vector2(0, -255), new Vector2(720, 60));

            bool joined = Save.Joined(d.Id);
            string tip = !own ? "打仗凑出他的名字或在他出场的回目里撑过四波，有机会缴获"
                : !joined ? $"{d.Name}还没入伙，入伙后才能装备" : on ? "已装备，下一局自动带上" : "装备后下一局生效";
            Ui.Label(t, tip, 28, joined || !own ? Pal.Willow : Pal.Red, new Vector2(0, -330), new Vector2(760, 50));
            if (own)
            {
                Ui.CartoonBtn(t, on ? "卸下" : "装备", new Vector2(260, 100), new Vector2(-150, -420), on ? Ui.BtnDark : Ui.BtnRed, () => { ToggleArm(d.Id); ShowArmory(); }, 46);
                Ui.CartoonBtn(t, "回收+" + Save.RecycleCoins, new Vector2(260, 100), new Vector2(150, -420), Ui.BtnYellow, () => { Recycle(d.Id); ShowArmory(); }, 38, null, Edge);
            }
            Tween.Pop(holder, 0.22f);
        }

        void ToggleArm(HeroId id)
        {
            int k = (int)id;
            if (Save.WeaponCount(k) <= 0) return;
            if (Save.D.armed.Contains(k)) Save.D.armed.Remove(k);
            else
            {
                if (!Save.Joined(id)) { Toast("这位好汉还没入伙，用不了他的兵器"); return; }
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

        /// <summary>超过五阶的多余件、和装备中兵器以外的凡器卖掉。返回拿到的铜钱。</summary>
        int RecycleSpare()
        {
            int gained = 0;
            var list = Save.D.weapons;
            var seen = new Dictionary<int, int>();
            for (int i = 0; i < list.Count; i++)
            {
                int id = list[i];
                seen.TryGetValue(id, out var c);
                seen[id] = c + 1;
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                int id = list[i];
                bool armed = Save.D.armed.Contains(id);
                bool extra = seen[id] > 5;
                bool common = !armed && HeroDef.Get((HeroId)id).WGrade == 0;
                if (!extra && !common) continue;
                list.RemoveAt(i);
                seen[id]--;
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

        int _codexTab;
        static readonly string[] CodexTabs = { "全部", "天罡", "地煞", "羁绊" };

        public void ShowCodex()
        {
            var layer = Panel("好汉谱", out var card, new Vector2(1000, 1700));
            int got = Save.JoinedCount;
            var bar = Ui.Framed(card, new Vector2(620, 44), new Vector2(0, 720), Pal.Hex("5E4532"), Edge, 4f, "progress");
            float ratio = got / 108f;
            var fill = Ui.Box(bar.transform, new Vector2(Mathf.Max(30, 620 * ratio), 44), new Vector2(-310 + Mathf.Max(30, 620 * ratio) / 2, 0), Pal.Hex("E9B53A"), null, "fill");
            fill.pixelsPerUnitMultiplier = 1f;
            Ui.Stroke(Ui.Label(bar.transform, $"已入伙 {Mathf.Min(got, 108)}/108", 30, Pal.Paper, Vector2.zero, new Vector2(620, 44)), Edge, 2f).fontStyle = FontStyle.Bold;

            var body = Ui.Rect(card, "body");
            void Fill()
            {
                for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
                for (int i = 0; i < CodexTabs.Length; i++)
                {
                    int k = i;
                    bool on = _codexTab == i;
                    var tab = Ui.Framed(body, new Vector2(200, 72), new Vector2(-330 + i * 220, 630), on ? Pal.Hex("E9C46A") : Pal.Hex("8C6B4E"), Edge, 4f, "framedTab");
                    Ui.Label(tab.transform, CodexTabs[i], 38, on ? Edge : Pal.Paper, new Vector2(0, 2), new Vector2(200, 72), TextAnchor.MiddleCenter, Fonts.Brush);
                    if (!on) Ui.Tap(tab, () => { _codexTab = k; Fill(); });
                }
                if (_codexTab == 3) BondList(body);
                else HeroGrid(layer, body);
            }
            Fill();
        }

        void HeroGrid(RectTransform layer, RectTransform body)
        {
            var list = new List<HeroDef>();
            foreach (var d in HeroDef.All)
            {
                if (_codexTab == 1 && (d.Rank <= 0 || d.Disha)) continue;
                if (_codexTab == 2 && !d.Disha) continue;
                list.Add(d);
            }
            list.Sort((a, b) => (a.Rank <= 0 ? 999 : a.Rank).CompareTo(b.Rank <= 0 ? 999 : b.Rank));
            const int cols = 4;
            const float cw = 228f, ch = 300f;
            float h = Mathf.Ceil(list.Count / (float)cols) * ch + 20;
            var content = Ui.Scroll(body, new Vector2(940, 1290), new Vector2(0, -50), h, "heroes");
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                bool have = Save.Joined(d.Id) || (d.Hidden && Save.D.heroes.Contains((int)d.Id));
                var pos = Ui.Top(content, -342 + (i % cols) * cw, 10 + (i / cols) * ch + ch * 0.5f);
                Color tone = d.Rank <= 0 ? Pal.Hex("8C6B4E") : d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E");
                Color bg = have ? (d.Rank <= 0 ? Pal.Hex("EAD9BC") : d.Disha ? Pal.Hex("E4D2F2") : Pal.TileHero) : Pal.Hex("B8AE9C");
                var face = Ui.Framed(content, new Vector2(206, 282), pos, bg, have ? tone : Edge, have ? 7f : 4f, "framedHero");
                face.pixelsPerUnitMultiplier = 0.9f;
                var t = face.transform;
                Ui.Tag(t, d.Rank > 0 ? d.Star : "隐藏", have ? tone : Pal.Hex("7A7062"), new Vector2(0, 112), 120, 24);
                if (have)
                {
                    var nm = Ui.Label(t, d.Name, d.Name.Length > 2 ? 60 : 80, Pal.Ink, new Vector2(0, 14), new Vector2(200, 130), TextAnchor.MiddleCenter, Fonts.Brush);
                    nm.lineSpacing = 0.9f;
                    Ui.Label(t, d.Nick, 28, Pal.RedDeep, new Vector2(0, -78), new Vector2(200, 40)).fontStyle = FontStyle.Bold;
                    Ui.Label(t, (d.Rank > 0 ? d.Rank108 + " · " : "") + d.TypeLabel, 22, Pal.InkSoft, new Vector2(0, -112), new Vector2(200, 32));
                }
                else
                {
                    Ui.Label(t, d.Hidden ? "隐" : "？", 110, Pal.Ink.A(0.25f), new Vector2(0, 20), new Vector2(200, 150), TextAnchor.MiddleCenter, Fonts.Brush);
                    int jc = ChapterDef.JoinChapter(d.Id);
                    Ui.Label(t, d.Hidden ? "隐藏好汉" : $"第{BattleView.ToCn(jc + 1)}回入伙", 26, Pal.Paper, new Vector2(0, -96), new Vector2(200, 40)).fontStyle = FontStyle.Bold;
                }
                Ui.Tap(face, () => HeroDetail(layer, d, have));
            }
        }

        void BondList(RectTransform body)
        {
            const float rowH = 190f;
            var content = Ui.Scroll(body, new Vector2(940, 1290), new Vector2(0, -50), Bond.All.Length * rowH + 20, "bonds");
            for (int i = 0; i < Bond.All.Length; i++)
            {
                var b = Bond.All[i];
                int have = 0;
                foreach (var id in b.Members) if (Save.Joined(id)) have++;
                bool ready = have >= b.Need;
                var face = Ui.Framed(content, new Vector2(900, 172), Ui.Top(content, 0, 10 + i * rowH + rowH * 0.5f),
                    ready ? Pal.Hex("F6E7C2") : Pal.Hex("D8CDB8"), ready ? Pal.Hex("D9822B") : Edge, ready ? 6f : 4f, "framedBond");
                var t = face.transform;
                Ui.Stroke(Ui.Label(t, b.Name, 46, ready ? Pal.Hex("B5651D") : Pal.InkSoft, new Vector2(-300, 40), new Vector2(280, 66), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                Ui.Label(t, $"{b.Need}人同场 · 伤害 +{Mathf.RoundToInt(b.Dmg * 100)}%", 24, Pal.RedDeep, new Vector2(-300, -10), new Vector2(280, 34));
                Ui.Label(t, b.Story, 26, Pal.Ink, new Vector2(140, 40), new Vector2(580, 60), TextAnchor.MiddleLeft);
                var sb = new System.Text.StringBuilder();
                foreach (var id in b.Members)
                {
                    var d = HeroDef.Get(id);
                    bool j = Save.Joined(id);
                    sb.Append(j ? $"<color=#1E1A16>{d.Name}</color>  " : "<color=#9A9080>？？</color>  ");
                }
                var m = Ui.Label(t, sb.ToString(), 30, Pal.Ink, new Vector2(140, -30), new Vector2(580, 80), TextAnchor.MiddleLeft, Fonts.Brush);
                m.supportRichText = true;
                Ui.Label(t, $"{have}/{b.Members.Length}", 26, Pal.InkSoft, new Vector2(-300, -52), new Vector2(200, 34));
            }
        }

        void HeroDetail(RectTransform layer, HeroDef d, bool have)
        {
            var shade = Ui.Fill(layer, new Color(0, 0, 0, 0.6f), true, "heroDetail");
            Ui.Tap(shade.GetComponent<Image>(), () => Destroy(shade.gameObject));
            if (have)
            {
                var sheet = JoinPanel.HeroSheet(shade, d, true, new Vector2(0, -10));
                sheet.localScale = Vector3.one * 0.98f;
                var block = Ui.Box(sheet, new Vector2(880, 1220), Vector2.zero, Color.clear, null, "block");
                block.raycastTarget = true;
                block.transform.SetAsFirstSibling();
                Ui.CloseX(sheet, new Vector2(-430, 600), () => Destroy(shade.gameObject), 100);
                Tween.Pop(sheet, 0.22f);
                return;
            }
            var card = Ui.Framed(shade, new Vector2(800, 760), new Vector2(0, 20), Pal.Hex("F6EEDA"), Edge, 8f, "framedDetail");
            card.raycastTarget = true;
            var t = card.transform;
            Ui.CloseX(shade, new Vector2(-390, 390), () => Destroy(shade.gameObject), 100);
            if (d.Rank > 0) Ui.Tag(t, d.Star, d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E"), new Vector2(0, 300), 180, 32);
            Ui.Label(t, d.Nick, 46, Pal.RedDeep, new Vector2(0, 220), new Vector2(700, 60), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(t, d.Hidden ? "隐" : "？", 200, Pal.Ink.A(0.25f), new Vector2(0, 70), new Vector2(400, 240), TextAnchor.MiddleCenter, Fonts.Brush);
            int jc = ChapterDef.JoinChapter(d.Id);
            string body = d.Hidden ? "据说在某一回凑出他的名字……\n他卖的炊饼特别香。"
                : $"第{BattleView.ToCn(jc + 1)}回「{ChapterDef.All[jc].Name}」\n赢下这一回，{d.Nick}便来入伙";
            Ui.Label(t, body, 40, Pal.Ink, new Vector2(0, -150), new Vector2(700, 200));
            if (!d.Hidden && jc <= Save.D.unlocked) Ui.Label(t, "这一回已经解锁，去打吧！", 32, Pal.Willow, new Vector2(0, -290), new Vector2(700, 50));
            Tween.Pop(card.transform.parent, 0.22f);
        }
    }
}
