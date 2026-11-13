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

        static readonly Color[] RowColors = { Pal.Hex("E9C477"), Pal.Hex("D8D3C6"), Pal.Hex("DDAE80"), Pal.Hex("EFE4CC") };
        const float RowPitch = 146f;
        bool _boardProvince = true;

        /// <summary>省榜 / 总榜：真实数据，从云端拉前五十名；连不上时只显示自己这一行。</summary>
        public void ShowBoard()
        {
            _boardOwns = true;
            _homeArmed = false;
            var layer = Ui.Fill(_root, new Color(0, 0, 0, 0.55f), true, "board");
            var b = Ui.Rect(Ui.Fit(layer, 1880), "b");
            b.anchoredPosition = new Vector2(0, -20);

            Ui.WoodPanel(b, new Vector2(960, 1290), new Vector2(0, -40), null, Pal.Hex("3A2A20"), "body");
            Ui.Title(b, "排行榜", new Vector2(0, 600), 92);
            Ui.CloseX(b, new Vector2(-470, 600), () =>
            {
                _reloadBoard = null;
                _authHit = null;
                _boardOwns = false;
                _homeArmed = false;
                WxSession.HideProfileButton();
                Destroy(layer.gameObject);
            });

            var list = Ui.Rect(b, "list");
            int ticket = 0;
            void Fill()
            {
                int mine = ++ticket;
                _authHit = null;
                WxSession.HideProfileButton();
                for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
                string region = LiveConfig.MyRegion;
                BoardTab(list, LiveConfig.RegionKnown ? region + "榜" : "本省榜", -140, 260, _boardProvince, () => { _boardProvince = true; Fill(); });
                BoardTab(list, "总榜", 140, 260, !_boardProvince, () => { _boardProvince = false; Fill(); });
                if (WxSession.CanAskProfile && !LiveConfig.HasNick) AuthButton(list);

                var status = Ui.Label(list, "正在打听各路好汉……", 38, Pal.Paper.A(0.8f), new Vector2(0, 0), new Vector2(800, 60));
                MyRowAt(list, -552, 0, LiveConfig.MyScore, false);
                ArmBoard();
                LiveConfig.LoadRank(!_boardProvince, data =>
                {
                    if (!list || mine != ticket) return;
                    if (LiveConfig.MyRegion != region) { Fill(); return; }
                    WxSession.HideProfileButton();
                    for (int i = list.childCount - 1; i >= 0; i--)
                    {
                        var child = list.GetChild(i);
                        if (child.name == "myRow" || child.name == "framedMine") Destroy(child.gameObject);
                    }
                    if (status) Destroy(status.gameObject);
                    var top = data != null && data.top != null ? data.top : new LiveConfig.RankRow[0];
                    if (data == null)
                    {
                        Ui.Label(list, "连不上山寨，先看看自己的座次", 36, Pal.Paper.A(0.8f), new Vector2(0, 120), new Vector2(800, 60));
                        Ui.Label(list, "联网后再打开就能看到各路好汉", 30, Pal.Paper.A(0.6f), new Vector2(0, 60), new Vector2(800, 50));
                    }
                    else if (top.Length == 0)
                        Ui.Label(list, _boardProvince ? $"{region}还没人上榜，你就是头一个" : "还没人上榜", 36, Pal.Paper.A(0.8f), new Vector2(0, 120), new Vector2(800, 60));
                    else BoardRows(list, top);
                    // 人就在这页榜上时，底下「我」用他在列表里的名次，避免同分时和上面对不上。
                    int mineRank = data != null ? data.rank : 0;
                    for (int i = 0; i < top.Length; i++)
                        if (top[i].me) { mineRank = i + 1; break; }
                    MyRowAt(list, -552, mineRank, LiveConfig.MyScore, data != null);
                    _authHit = list.Find("authBtn") as RectTransform;
                    ArmBoard();
                    if (data != null && data.total > 0)
                        Ui.Label(list, $"共 {data.total} 位好汉", 26, Pal.Paper.A(0.6f), new Vector2(330, -640), new Vector2(240, 36), TextAnchor.MiddleRight);
                });
            }
            _reloadBoard = Fill;
            Fill();
            if (WxSession.CanAskProfile && !LiveConfig.HasNick && !WxSession.PrivacyAgreed)
                WxSession.ShowPrivacy(ok => { if (ok && _authHit) ArmBoard(); }, true);

            BuildClaimStrip(b);
            Tween.Pop(b, 0.28f);
        }

        void AuthButton(RectTransform list)
        {
            var btn = Ui.CartoonBtn(list, "使用微信昵称头像上榜", new Vector2(720, 78), new Vector2(0, 368), Ui.BtnYellow, OnAuthTap, 32, null, Edge);
            btn.name = "authBtn";
            var label = btn.Find("body/text").GetComponent<Text>();
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22;
            label.resizeTextMaxSize = 32;
            _authHit = btn;
        }

        void OnAuthTap()
        {
            WxSession.AllowPrivacyRetry();
            if (WxSession.PrivacyAgreed) ArmBoard();
            else WxSession.ShowPrivacy(ok => { if (ok && _authHit) ArmBoard(); }, true);
        }

        void BoardRows(RectTransform list, LiveConfig.RankRow[] top)
        {
            bool ask = WxSession.CanAskProfile && !LiveConfig.HasNick;
            float viewH = (ask ? 5 : 6) * RowPitch - 16f;
            float topY = ask ? 300f : 405f;
            var content = Ui.Scroll(list, new Vector2(860, viewH), new Vector2(0, topY - viewH * 0.5f), top.Length * RowPitch, "rows");
            for (int i = 0; i < top.Length; i++)
            {
                var r = top[i];
                var pos = Ui.Top(content, 0, i * RowPitch + 65f);
                if (r.me)
                {
                    MyRowAt(content, pos.y, i + 1, r.score, true, "framedMine");
                    continue;
                }
                var face = Ui.Framed(content, new Vector2(840, 130), pos, RowColors[Mathf.Min(i, 3)], Edge, 5f, "row");
                var t = face.transform;
                RankBadge(t, i);
                Portrait(t, r.avatar, new Vector2(-236, 0));
                NickLabel(t, r.name, 30, Pal.Ink, new Vector2(-60, 26), new Vector2(240, 40), TextAnchor.MiddleLeft);
                Ui.Label(t, r.region, 40, Pal.Ink, new Vector2(-60, -20), new Vector2(220, 56), TextAnchor.MiddleLeft, Fonts.Brush);
                Ui.Stroke(Ui.Label(t, RankRules.Name(RankRules.Clamp(r.tier)), 38, Pal.Hex("FFE36A"), new Vector2(270, 26), new Vector2(300, 50), TextAnchor.MiddleRight, Fonts.Brush), Edge, 2.5f);
                var st = Ui.Label(t, Ui.RankStars(RankRules.Clamp(r.tier), r.stars, "FFD84A", "6B5D4A"), 34, Pal.Paper, new Vector2(290, -24), new Vector2(260, 50), TextAnchor.MiddleRight);
                st.supportRichText = true;
                Ui.Stroke(st, Edge, 1.5f);
            }
        }

        void BoardTab(RectTransform parent, string text, float x, float w, bool on, Action onClick)
        {
            var face = Ui.Framed(parent, new Vector2(w, 84), new Vector2(x, 470), on ? Pal.Hex("E9C46A") : Pal.Hex("2A1C13"), Pal.Hex("B8913C"), 4f, "framedTab");
            Ui.Stroke(Ui.Label(face.transform, text, text.Length > 3 ? 42 : 50, on ? Edge : Pal.Paper, new Vector2(0, 2), new Vector2(w, 84), TextAnchor.MiddleCenter, Fonts.Brush),
                on ? Pal.Hex("FFF2B8") : Edge, 1.5f);
            if (!on) Ui.Tap(face, onClick);
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
            var label = Ui.Label(t, (i + 1).ToString(), i >= 99 ? 26 : 38, Edge, new Vector2(-352, 0), new Vector2(72, 60));
            label.fontStyle = FontStyle.Bold;
        }

        /// <summary>自己这一行。rank 从 1 起；0 表示没拿到名次。</summary>
        void MyRowAt(RectTransform parent, float y, int rank, int score, bool online, string name = "myRow")
        {
            var face = Ui.Framed(parent, new Vector2(840, 130), new Vector2(0, y), Pal.Hex("B8342A"), Ui.GoldText, 6f, name);
            var t = face.transform;
            if (rank > 0) RankBadge(t, rank - 1);
            else Ui.Stroke(Ui.Label(t, online ? "未上榜" : "离线", 30, Pal.Paper, new Vector2(-352, 0), new Vector2(110, 60)), Edge, 2f).fontStyle = FontStyle.Bold;
            Portrait(t, WxSession.Avatar, new Vector2(-236, 0));
            NickLabel(t, "我 · " + LiveConfig.MyName, 30, Pal.Paper, new Vector2(-50, 26), new Vector2(250, 40), TextAnchor.MiddleLeft);
            Ui.Stroke(Ui.Label(t, LiveConfig.MyRegion, 40, Pal.Paper, new Vector2(-60, -20), new Vector2(220, 56), TextAnchor.MiddleLeft, Fonts.Brush), Edge, 2f);
            Ui.Stroke(Ui.Label(t, Save.RankName, 38, Pal.Hex("FFE36A"), new Vector2(270, 26), new Vector2(300, 50), TextAnchor.MiddleRight, Fonts.Brush), Edge, 2.5f);
            var st = Ui.Label(t, Ui.RankStars(Save.D.tier, Save.D.stars, "FFD84A", "6B2A20"), 38, Pal.Paper, new Vector2(290, -24), new Vector2(260, 50), TextAnchor.MiddleRight);
            st.supportRichText = true;
            Ui.Stroke(st, Edge, 1.5f);
        }

        void ArmBoard()
        {
            if (!_authHit || !WxSession.CanAskProfile || !WxSession.PrivacyAgreed || LiveConfig.HasNick) return;
            WxSession.Cover(this, _authHit, OnProfile);
        }

        void OnProfile(string nick, string avatar, string err)
        {
            _homeArmed = false;
            if (!WxSession.TakeProfile(nick, avatar, err, () =>
            {
                if (_boardOwns && _authHit) ArmBoard();
                else ArmHome();
            })) return;
            Refresh();
            _reloadBoard?.Invoke();
        }

        static void Portrait(Transform parent, string url, Vector2 pos) => CirclePortrait(parent, url, pos, 96f, out _);

        /// <summary>圆头像。微信照片铺满圆，没有地址时用默认立绘。</summary>
        static Image CirclePortrait(Transform parent, string url, Vector2 pos, float size, out RectTransform hit)
        {
            var ring = Ui.Box(parent, Vector2.one * (size * 1.2f), pos, Color.white, Ui.SkinDisc, "avatarRing");
            ring.type = Image.Type.Simple;
            ring.raycastTarget = false;
            var maskGo = new GameObject("avatarMask", typeof(RectTransform), typeof(Image), typeof(Mask));
            var rt = (RectTransform)maskGo.transform;
            rt.SetParent(ring.transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.one * size;
            rt.anchoredPosition = Vector2.zero;
            var maskImg = maskGo.GetComponent<Image>();
            maskImg.sprite = Art.Circle;
            maskImg.type = Image.Type.Simple;
            maskImg.color = Color.white;
            maskImg.raycastTarget = false;
            maskGo.GetComponent<Mask>().showMaskGraphic = false;
            var face = Ui.Icon(rt, "ui_avatar", size, Vector2.zero, "avatar");
            face.raycastTarget = true;
            Portraits.Set(face, url, true);
            hit = ring.rectTransform;
            return face;
        }

        static Text NickLabel(Transform parent, string text, int size, Color c, Vector2 pos, Vector2 box, TextAnchor align)
        {
            var label = Ui.Label(parent, text, size, c, pos, box, align);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 22;
            label.resizeTextMaxSize = size;
            WxSession.ApplyNickFont(label);
            return label;
        }

        /// <summary>榜单下方：昨日座次发的俸禄，没领就亮着。</summary>
        void BuildClaimStrip(RectTransform b)
        {
            var face = Ui.Framed(b, new Vector2(860, 150), new Vector2(0, -790), Pal.Hex("A8321F"), Ui.GoldLine, 5f, "claim");
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
                if (Save.D.claimTaken || Save.D.claimDay != Save.Today) { Nope("明天按今天的座次再发俸禄"); return; }
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
            // 框稍加高并下移，上沿仍让标题压住。领取整颗落在米色面里，不再骑在红框下沿。
            const float panelH = 880f;
            var card = Ui.WoodPanel(Ui.Fit(layer, 1040), new Vector2(860, panelH), new Vector2(0, -20f), Pal.Hex("C9402E"), Pal.Hex("F3E6C6"));
            Ui.Title(card, "昨日俸禄", new Vector2(0, 428), 86);
            Ui.Label(card, "昨日座次", 40, Pal.InkSoft, new Vector2(0, 300), new Vector2(760, 56));
            Ui.RankBadge(card, Save.D.claimTier, new Vector2(-300, 200), 0.9f);
            Ui.Stroke(Ui.Label(card, RankRules.Name(Save.D.claimTier), 76, Pal.Hex("C0392B"), new Vector2(0, 200),
                new Vector2(760, 110), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 3f);
            var st = Ui.Label(card, Ui.RankStars(Save.D.claimTier, Save.D.claimStars), 70, Pal.Gold, new Vector2(0, 90), new Vector2(760, 90));
            st.supportRichText = true;
            Ui.Stroke(st, Edge, 3f);
            var seal = Ui.Framed(card, new Vector2(120, 120), new Vector2(-170, -30), Pal.Hex("F6C531"), Edge, 5f, "seal");
            Ui.Label(seal.transform, "赏", 86, Pal.RedDeep, new Vector2(0, 4), new Vector2(120, 120), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Icon(card, "coin", 100, new Vector2(-20, -30));
            var amount = Ui.Label(card, "×" + coins, 64, Pal.Gold, new Vector2(130, -30), new Vector2(240, 100), TextAnchor.MiddleLeft);
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
            Ui.CartoonBtn(card, "双倍领取", new Vector2(520, 118), new Vector2(0, -176), Ui.BtnRed, () => AdStub.Show("双倍俸禄", () => Take(2)), 54, "ui_video");
            Ui.CartoonBtn(card, "领取", new Vector2(360, 100), new Vector2(0, -318), Ui.BtnDark, () => Take(1), 46);
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
            _armory = Ui.Fill(_root, Pal.Hex("1A110B"), true, "armory");
            var a = _armory;
            PageBg(a, 0.45f);
            var top = Ui.Rect(a, "top");
            Ui.Anchor(top, 0.5f, 1f);
            const float headH = 570f;
            Ui.Box(top, new Vector2(1600, 140), new Vector2(0, -headH - 70), Pal.Hex("120B07").A(0.5f), Art.White, "headShade").type = Image.Type.Simple;
            Ui.Box(top, new Vector2(1600, 3), new Vector2(0, -headH), Ui.GoldLine, Art.White, "headLine").type = Image.Type.Simple;
            Ui.Skin(top, Ui.SkinSlot, new Vector2(260, 72), new Vector2(-330, -80), Color.white, "coinBar");
            Ui.Icon(top, "coin", 90, new Vector2(-450, -80));
            Ui.Label(top, Save.D.coins.ToString(), 44, Ui.GoldText, new Vector2(-300, -80), new Vector2(180, 76)).fontStyle = FontStyle.Bold;
            var back = Ui.Medal(top, 96, new Vector2(-470, -200), "back");
            Ui.Label(back.transform, "◀", 40, Ui.GoldText, new Vector2(-3, 0), new Vector2(96, 96));
            Ui.Tap(back, () => { if (_armory) Destroy(_armory.gameObject); _armory = null; Refresh(); });
            Ui.Title(top, "兵器谱", new Vector2(0, -200), 84);

            if (_armSel >= 0 && string.IsNullOrEmpty(HeroDef.Get((HeroId)_armSel).Weapon)) _armSel = -1;

            // 兵器架：局里凑出好汉就按顺序领。起步三格，后两格花铜钱扩。
            for (int i = 0; i < Save.RackMax; i++)
            {
                var pos = new Vector2(-400 + i * 200, -372);
                bool open = i < Save.D.rackSlots;
                bool has = open && i < Save.D.rack.Count;
                var wpn = has ? HeroDef.Get((HeroId)Save.D.rack[i]) : null;
                var g = has ? Ui.GradeColor(wpn.WGrade) : Pal.Hex("8C8478");
                var face = Ui.Framed(top, new Vector2(184, 176), pos, has ? Pal.Hex("F6EEDA") : open ? Pal.Hex("5E5246") : Pal.Hex("3F352C"), g, has ? 6f : 4f, "framedSlot");
                var t = face.transform;
                if (open)
                {
                    var num = Ui.Box(t, new Vector2(40, 40), new Vector2(-66, 62), has ? g : Pal.Hex("8C8478"), Art.Circle, "num");
                    num.type = Image.Type.Simple;
                    Ui.Label(num.transform, (i + 1).ToString(), 26, Pal.Paper, Vector2.zero, new Vector2(40, 40)).fontStyle = FontStyle.Bold;
                }
                if (has)
                {
                    Ui.Box(t, new Vector2(96, 96), new Vector2(0, 24), g.A(0.22f), Art.Circle, "halo").type = Image.Type.Simple;
                    Ui.Icon(t, Weapons.Icon(wpn.WKind), 88, new Vector2(0, 24));
                    Ui.Label(t, wpn.Weapon, wpn.Weapon.Length > 3 ? 24 : 30, Pal.Ink, new Vector2(0, -38), new Vector2(178, 40), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(t, Weapons.Grades[wpn.WGrade] + "·" + Mathf.Min(5, Save.WeaponCount((int)wpn.Id)) + "阶", 22, g, new Vector2(0, -68), new Vector2(178, 30)).fontStyle = FontStyle.Bold;
                    int id = (int)wpn.Id;
                    Ui.Tap(face, () => { _armSel = id; ShowArmory(); });
                }
                else if (open)
                {
                    Ui.Label(t, "＋", 64, Pal.Paper.A(0.5f), new Vector2(0, 14), new Vector2(160, 80));
                    Ui.Label(t, "点兵器挂上", 24, Pal.Paper.A(0.7f), new Vector2(0, -56), new Vector2(178, 36));
                }
                else if (i == Save.D.rackSlots)
                {
                    int price = Save.RackSlotPrice;
                    Ui.Label(t, "扩一格", 30, Pal.Paper, new Vector2(0, 40), new Vector2(178, 44), TextAnchor.MiddleCenter, Fonts.Brush);
                    var btn = Ui.Framed(t, new Vector2(156, 62), new Vector2(0, -36), Ui.BtnYellow, Edge, 4f, "framedBuy");
                    Ui.Icon(btn.transform, "coin", 46, new Vector2(-46, 0));
                    var pl = Ui.Label(btn.transform, price.ToString(), 30, Edge, new Vector2(22, 0), new Vector2(100, 60));
                    pl.fontStyle = FontStyle.Bold;
                    if (Save.D.coins < price) pl.color = Pal.Hex("A33A2A");
                    Ui.Tap(face, () =>
                    {
                        if (Save.D.coins < price) { Nope("铜钱不够，扩这一格要 " + price); return; }
                        Confirm("扩兵器架", $"花 {price} 铜钱，兵器架多一格\n局里多一位好汉能领兵器", "扩一格", () =>
                        {
                            if (!Save.BuyRackSlot(out var err)) { Nope(err); return; }
                            Sfx.Play(Snd.Merge);
                            Toast("兵器架扩到 " + Save.D.rackSlots + " 格");
                            Refresh();
                            ShowArmory();
                        });
                    });
                }
                else
                {
                    Ui.Label(t, "先扩前一格", 24, Pal.Paper.A(0.5f), new Vector2(0, 0), new Vector2(178, 40));
                }
            }
            Ui.Stroke(Ui.Label(top, "凑出好汉按顺序领，原主必领自己那件", 26, Pal.Paper,
                new Vector2(-235, -488), new Vector2(530, 40), TextAnchor.MiddleLeft), Edge, 2f);
            Ui.Stroke(Ui.Label(top, $"架上 {Save.RackCount}/{Save.D.rackSlots}　已缴获 {Save.D.weapons.Count}/{Save.MaxWeapons} 件", 26, Pal.Paper,
                new Vector2(-235, -528), new Vector2(530, 40), TextAnchor.MiddleLeft), Edge, 2f);
            Ui.CartoonBtn(top, "兵器铺", new Vector2(200, 80), new Vector2(170, -508), Ui.BtnRed, ShowForge, 38);
            Ui.CartoonBtn(top, "一键回收", new Vector2(200, 80), new Vector2(400, -508), Ui.BtnYellow, () =>
            {
                if (!RecycleSpare()) { Nope("没有多余的兵器可回收"); return; }
                ShowArmory();
            }, 34, null, Edge);

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
            int slot = Save.D.rack.IndexOf(k);
            bool on = slot >= 0;
            var g = Ui.GradeColor(d.WGrade);
            var face = Ui.Framed(parent, new Vector2(222, 214), pos, own ? Pal.Hex("F6EEDA") : Pal.Hex("8F8577"), own ? g : Edge, own ? 6f : 4f, "framedWeapon");
            var t = face.transform;
            Ui.Box(t, new Vector2(222, 30), new Vector2(0, 92), g.A(own ? 1f : 0.45f), null, "gradeBand").pixelsPerUnitMultiplier = 1f;
            Ui.Label(t, Weapons.Grades[d.WGrade], 22, Pal.Paper, new Vector2(0, 92), new Vector2(200, 30)).fontStyle = FontStyle.Bold;
            if (own) Ui.Box(t, new Vector2(110, 110), new Vector2(0, 22), g.A(0.2f), Art.Circle, "halo").type = Image.Type.Simple;
            var icon = Ui.Icon(t, Weapons.Icon(d.WKind), 100, new Vector2(0, 22));
            if (!own) icon.color = new Color(0.2f, 0.18f, 0.15f, 0.55f);
            Ui.Label(t, d.Weapon, d.Weapon.Length > 4 ? 24 : 30, own ? Pal.Ink : Pal.Paper.A(0.8f), new Vector2(0, -50), new Vector2(214, 40), TextAnchor.MiddleCenter, Fonts.Brush);
            string sub = !own ? d.Name + " · 未缴获" : on ? "架上第" + (slot + 1) + "格 · " + n + "阶" : d.Name + " · " + n + "阶";
            Ui.Label(t, sub, 22, own ? Pal.InkSoft : Pal.Paper.A(0.7f), new Vector2(0, -84), new Vector2(214, 30));
            if (on)
            {
                var dot = Ui.Box(t, new Vector2(46, 46), new Vector2(86, 62), Pal.Hex("D7362B"), Art.Circle, "on");
                dot.type = Image.Type.Simple;
                Ui.Label(dot.transform, "架", 26, Pal.Paper, Vector2.zero, new Vector2(46, 46), TextAnchor.MiddleCenter, Fonts.Brush);
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
            int slot = Save.D.rack.IndexOf(k);
            var g = Ui.GradeColor(d.WGrade);
            var shade = Ui.Fill(a, new Color(0, 0, 0, 0.55f), true, "detail");
            var holder = Ui.Rect(Ui.Fit(shade, 1160), "holder");
            var move = Weapons.MoveOf(d);
            float dy = move == WeaponMove.None ? 0f : -120f;
            holder.anchoredPosition = new Vector2(0, 40 - dy * 0.5f);
            Ui.Framed(holder, new Vector2(820, 1000 - dy), new Vector2(0, dy * 0.5f), Pal.Hex("F6EEDA"), g, 9f, "framedDetail");
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
            string carry = !own ? "还没缴获" : slot < 0 ? "没挂上兵器架" : "挂在兵器架第" + (slot + 1) + "格";
            Ui.Label(t, "出自" + d.Name + "　" + carry, 34, Pal.InkSoft, new Vector2(0, 60), new Vector2(760, 50));

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
            Ui.Label(t, n >= 5 ? "已满阶" : own ? $"再升一阶 → +{next}%" : "未缴获", 28, Pal.InkSoft, new Vector2(200, -165), new Vector2(320, 50), TextAnchor.MiddleLeft);
            Ui.Label(t, "凡器+25%　利器+32%　宝器+40%　神兵+50%，每阶再+10%", 24, Pal.InkSoft, new Vector2(0, -232), new Vector2(720, 34));
            Ui.Label(t, d.Name + "自己领到，伤害再多一成", 24, Pal.RedDeep, new Vector2(0, -266), new Vector2(720, 34));

            if (move != WeaponMove.None)
            {
                var mv = Ui.Framed(t, new Vector2(740, 104), new Vector2(0, -360), Pal.Hex("FBE3CF"), Pal.Hex("B0451F"), 4f, "move");
                Ui.Tag(mv.transform, "名兵招式", Pal.Hex("B0451F"), new Vector2(-280, 22), 140, 24);
                Ui.Stroke(Ui.Label(mv.transform, Weapons.MoveName(move), 38, Pal.Hex("B0451F"), new Vector2(10, 22), new Vector2(400, 50), TextAnchor.MiddleLeft, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                Ui.Label(mv.transform, Weapons.MoveText(move), 28, Pal.Ink, new Vector2(0, -24), new Vector2(700, 44), TextAnchor.MiddleLeft);
            }

            string tip = !own ? "入伙之后，凑出他的名字或在他出场的回目里撑过四波，才有机会缴获"
                : move != WeaponMove.None ? "特性和招式都跟兵器走，谁领到谁使。"
                : "特性跟兵器走，谁领到谁使。";
            Ui.Label(t, tip, 28, Pal.Willow, new Vector2(0, -330 + dy), new Vector2(760, 50));
            if (own)
            {
                int value = Save.RecycleValue(k);
                string sell = value > 0 ? "回收+" + value : "回收";
                if (slot < 0)
                {
                    Ui.CartoonBtn(t, "挂上兵器架", new Vector2(300, 100), new Vector2(-150, -420 + dy), Ui.BtnRed, () =>
                    {
                        if (!Save.PutOnRack(k, out var err)) { Nope(err); return; }
                        Sfx.Play(Snd.Merge);
                        _armSel = -1;
                        ShowArmory();
                    }, 42);
                    Ui.CartoonBtn(t, sell, new Vector2(240, 100), new Vector2(160, -420 + dy), Ui.BtnYellow, () => { Recycle(d.Id); ShowArmory(); }, 38, null, Edge);
                }
                else
                {
                    Ui.CartoonBtn(t, "取下", new Vector2(220, 100), new Vector2(-250, -420 + dy), Ui.BtnDark, () =>
                    {
                        Save.TakeOffRack(k);
                        _armSel = -1;
                        ShowArmory();
                    }, 42);
                    Ui.CartoonBtn(t, slot == 0 ? "已在第一格" : "放第一格", new Vector2(220, 100), new Vector2(0, -420 + dy), slot == 0 ? Ui.BtnDark : Ui.BtnRed, () =>
                    {
                        if (slot == 0) return;
                        Save.RackToFront(k);
                        Sfx.Play(Snd.Merge);
                        ShowArmory();
                    }, slot == 0 ? 32 : 40);
                    Ui.CartoonBtn(t, sell, new Vector2(220, 100), new Vector2(250, -420 + dy), Ui.BtnYellow, () => { Recycle(d.Id); ShowArmory(); }, 36, null, Edge);
                }
            }
            Tween.Pop(holder, 0.22f);
        }

        void Recycle(HeroId id)
        {
            int k = (int)id;
            int at = Save.D.weapons.IndexOf(k);
            if (at < 0) return;
            int value = Save.RecycleValue(k);
            Save.D.weapons.RemoveAt(at);
            Save.TidyRack();
            Save.D.coins += value;
            Save.Write();
            Refresh();
            Sfx.Play(Snd.Merge);
            Toast(value > 0 ? "回收得铜钱 " + value : "回收了，腾出一格");
        }

        /// <summary>超过五阶的多余件、和兵器架以外的凡器回收掉。返回有没有回收。</summary>
        bool RecycleSpare()
        {
            int gained = 0, removed = 0;
            var list = Save.D.weapons;
            var seen = new Dictionary<int, int>();
            foreach (var id in list)
            {
                seen.TryGetValue(id, out var c);
                seen[id] = c + 1;
            }
            for (int i = list.Count - 1; i >= 0; i--)
            {
                int id = list[i];
                bool extra = seen[id] > Save.MaxWeaponTier;
                bool common = !Save.OnRack(id) && HeroDef.Get((HeroId)id).WGrade == 0;
                if (!extra && !common) continue;
                list.RemoveAt(i);
                seen[id]--;
                removed++;
                gained += Save.RecycleValue(id);
            }
            Save.TidyRack();
            if (removed <= 0) return false;
            Save.D.coins += gained;
            Save.Write();
            Refresh();
            Sfx.Play(Snd.Merge);
            Toast(gained > 0 ? "一键回收得铜钱 " + gained : "回收了 " + removed + " 件");
            return true;
        }

        // ─────────────────────────── 兵器铺 ───────────────────────────

        public void ShowForge()
        {
            var old = _root.Find("forge");
            if (old) Destroy(old.gameObject);
            var layer = Panel("兵器铺", out var card, new Vector2(900, 1080));
            layer.name = "forge";
            Ui.Label(card, "今天随机三件，打一件升一阶", 32, Pal.InkSoft, new Vector2(0, 410), new Vector2(820, 50));
            var offers = Save.ForgeOffers();
            if (offers.Count == 0)
            {
                Ui.Label(card, Save.D.weapons.Count == 0 ? "还没缴获兵器\n打仗撑过四波，就有机会缴获" : "手里的兵器都满五阶了", 38, Pal.Ink, new Vector2(0, 40), new Vector2(800, 200));
                return;
            }
            for (int i = 0; i < offers.Count; i++)
            {
                int k = offers[i];
                var d = HeroDef.Get((HeroId)k);
                var g = Ui.GradeColor(d.WGrade);
                int n = Mathf.Min(Save.MaxWeaponTier, Save.WeaponCount(k));
                bool done = Save.D.forgeSold.Contains(k);
                float y = 250 - i * 250;
                var row = Ui.Framed(card, new Vector2(820, 226), new Vector2(0, y), Pal.Hex("F2E7CA"), g, 6f, "offer");
                var t = row.transform;
                Ui.Box(t, new Vector2(160, 160), new Vector2(-312, 6), g.A(0.22f), Art.Circle, "halo").type = Image.Type.Simple;
                Ui.Icon(t, Weapons.Icon(d.WKind), 140, new Vector2(-312, 6));
                Ui.Tag(t, Weapons.Grades[d.WGrade], g, new Vector2(-312, -84), 120, 26);
                Ui.Label(t, d.Weapon, d.Weapon.Length > 4 ? 40 : 48, Pal.Ink, new Vector2(-30, 60), new Vector2(380, 64), TextAnchor.MiddleLeft, Fonts.Brush);
                Ui.Label(t, "出自" + d.Name + (Save.OnRack(k) ? "　在兵器架上" : ""), 26, Pal.InkSoft, new Vector2(-30, 16), new Vector2(380, 40), TextAnchor.MiddleLeft);
                int now = Mathf.RoundToInt((HeroDef.ArmedMul(Mathf.Max(1, n), d.WGrade) - 1f) * 100f);
                int next = Mathf.RoundToInt((HeroDef.ArmedMul(Mathf.Min(Save.MaxWeaponTier, n + (done ? 0 : 1)), d.WGrade) - 1f) * 100f);
                string line = done ? $"{n}阶　伤害+{now}%" : $"{n}阶→{n + 1}阶　伤害+{now}%→+{next}%";
                Ui.Label(t, line, 26, Pal.RedDeep, new Vector2(-30, -30), new Vector2(380, 40), TextAnchor.MiddleLeft).fontStyle = FontStyle.Bold;
                for (int s = 0; s < Save.MaxWeaponTier; s++)
                {
                    var seg = Ui.Box(t, new Vector2(56, 22), new Vector2(-192 + s * 62, -78), g.A(s < n ? 1f : 0.22f), null, "seg");
                    seg.pixelsPerUnitMultiplier = 2f;
                }
                if (done)
                {
                    var gone = Ui.Framed(t, new Vector2(212, 104), new Vector2(286, 0), Pal.Hex("4D4642"), Edge, 5f, "framedSold");
                    Ui.Label(gone.transform, "已打造", 42, Pal.Paper, Vector2.zero, new Vector2(200, 100)).fontStyle = FontStyle.Bold;
                    continue;
                }
                int price = Save.ForgePrice(k);
                var btn = Ui.Framed(t, new Vector2(212, 104), new Vector2(286, 0), Ui.BtnYellow, Edge, 5f, "framedBuy");
                Ui.Box(btn.transform, new Vector2(196, 14), new Vector2(0, 34), Color.white.A(0.35f), null, "gloss");
                Ui.Icon(btn.transform, "coin", 64, new Vector2(-60, 0));
                var pl = Ui.Label(btn.transform, price.ToString(), 42, Edge, new Vector2(28, 0), new Vector2(130, 100));
                pl.fontStyle = FontStyle.Bold;
                if (Save.D.coins < price) pl.color = Pal.Hex("A33A2A");
                Ui.Tap(btn, () =>
                {
                    if (!Save.BuyForge(k, out var err)) { Nope(err); return; }
                    Sfx.Play(Snd.Merge);
                    Toast(d.Weapon + "升到 " + Save.WeaponCount(k) + " 阶");
                    Refresh();
                    if (_armory) ShowArmory();
                    ShowForge();
                });
            }
            Ui.Stroke(Ui.Label(card, "铜钱 " + Save.D.coins, 34, Pal.Paper, new Vector2(0, -470), new Vector2(600, 50)), Edge, 2f);
        }

        // ─────────────────────────── 好汉谱 ───────────────────────────

        int _codexTab;
        static readonly string[] CodexTabs = { "全部", "天罡", "地煞", "羁绊" };

        public void ShowCodex()
        {
            var layer = Panel("好汉谱", out var card, new Vector2(1000, 1700), () => ArtPack.Keep(Art.BattlePacks()));
            int got = Save.JoinedCount;
            // 有赏钱没领时进度条让出右边，摆「领赏」按钮。
            bool owed = Save.CodexPending > 0;
            float barW = owed ? 540f : 620f;
            var bar = Ui.Framed(card, new Vector2(barW, 44), new Vector2(owed ? -110 : 0, 720), Pal.Hex("5E4532"), Edge, 4f, "progress");
            float ratio = got / 108f;
            var fill = Ui.Box(bar.transform, new Vector2(Mathf.Max(30, barW * ratio), 44), new Vector2(-barW / 2 + Mathf.Max(30, barW * ratio) / 2, 0), Pal.Hex("E9B53A"), null, "fill");
            fill.pixelsPerUnitMultiplier = 1f;
            Ui.Stroke(Ui.Label(bar.transform, $"已入伙 {Mathf.Min(got, 108)}/108", 30, Pal.Paper, Vector2.zero, new Vector2(barW, 44)), Edge, 2f).fontStyle = FontStyle.Bold;

            var body = Ui.Rect(card, "body");
            RectTransform claimAll = null;
            void Fill()
            {
                if (claimAll) Destroy(claimAll.gameObject);
                int pending = Save.CodexPending;
                if (pending > 0)
                {
                    int sum = 0;
                    foreach (var id in Save.D.joined) if (Save.CodexClaimable(id)) sum += Save.CodexCoins(HeroDef.Get((HeroId)id));
                    claimAll = Ui.CartoonBtn(card, $"领赏 {sum}", new Vector2(240, 76), new Vector2(330, 720), Ui.BtnYellow, () =>
                    {
                        int got2 = Save.ClaimAllCodex();
                        if (got2 <= 0) return;
                        Sfx.Play(Snd.Rain);
                        Toast($"好汉入伙赏钱 铜钱 +{got2}");
                        Refresh();
                        Fill();
                    }, 34, "coin", Edge);
                    Tween.Pop(claimAll, 0.25f);
                }
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
                else HeroGrid(layer, body, Fill);
            }
            Fill();
        }

        void HeroGrid(RectTransform layer, RectTransform body, Action refill)
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
            const float cw = 228f, ch = 368f;
            float h = Mathf.Ceil(list.Count / (float)cols) * ch + 20;
            var content = Ui.Scroll(body, new Vector2(940, 1290), new Vector2(0, -50), h, "heroes");
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                bool have = Save.Joined(d.Id) || (d.Hidden && Save.D.heroes.Contains((int)d.Id));
                var pos = Ui.Top(content, -342 + (i % cols) * cw, 10 + (i / cols) * ch + ch * 0.5f);
                Color tone = d.Rank <= 0 ? Pal.Hex("8C6B4E") : d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E");
                Color bg = have ? (d.Rank <= 0 ? Pal.Hex("EAD9BC") : d.Disha ? Pal.Hex("E4D2F2") : Pal.TileHero) : Pal.Hex("B8AE9C");
                var face = Ui.Framed(content, new Vector2(214, 352), pos, bg, have ? tone : Edge, have ? 7f : 4f, "framedHero");
                face.pixelsPerUnitMultiplier = 0.9f;
                var t = face.transform;
                if (have)
                {
                    JoinPanel.Portrait(t, d.Id, new Vector2(0, 30), new Vector2(200, 180));
                    bool owed = Save.CodexClaimable((int)d.Id);
                    var nm = Ui.Label(t, d.Name, d.Name.Length > 2 ? 36 : 46, Pal.Ink, new Vector2(0, owed ? -90 : -108), new Vector2(196, 50), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Stroke(nm, Pal.Hex("FFF6E2"), 1.5f);
                    if (owed) CodexClaimChip(t, d, refill);
                    else
                    {
                        Ui.Label(t, d.Nick, 22, Pal.RedDeep, new Vector2(d.Rank > 0 ? -46 : 0, -150), new Vector2(d.Rank > 0 ? 96 : 180, 28),
                            d.Rank > 0 ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
                        if (d.Rank > 0)
                        {
                            var rk = Ui.Label(t, d.Rank108, 20, Pal.InkSoft, new Vector2(50, -150), new Vector2(88, 28), TextAnchor.MiddleLeft);
                            rk.resizeTextForBestFit = true;
                            rk.resizeTextMinSize = 15;
                            rk.resizeTextMaxSize = 20;
                        }
                    }
                }
                else
                {
                    Ui.Label(t, d.Hidden ? "隐" : "？", 110, Pal.Ink.A(0.25f), new Vector2(0, -8), new Vector2(200, 150), TextAnchor.MiddleCenter, Fonts.Brush);
                    int jc = ChapterDef.JoinChapter(d.Id);
                    Ui.Label(t, d.Hidden ? "隐藏好汉" : $"第{BattleView.ToCn(jc + 1)}回入伙", 24, Pal.Paper, new Vector2(0, -124), new Vector2(200, 36)).fontStyle = FontStyle.Bold;
                }
                Ui.Tag(t, d.Rank > 0 ? d.Star : "隐藏", have ? tone : Pal.Hex("7A7062"), new Vector2(0, 142), 128, 22);
                Ui.Tap(face, () => HeroDetail(layer, d, have));
            }
        }

        /// <summary>入伙赏钱：卡片底下一块金签，点了就领，不用进详情。</summary>
        void CodexClaimChip(Transform card, HeroDef d, Action refill)
        {
            int coins = Save.CodexCoins(d);
            var chip = Ui.Framed(card, new Vector2(176, 46), new Vector2(0, -144), Pal.Hex("F6C531"), Edge, 4f, "claimChip");
            chip.raycastTarget = true;
            Ui.Icon(chip.transform, "coin", 42, new Vector2(-52, 0));
            Ui.Label(chip.transform, "领 " + coins, 28, Edge, new Vector2(22, 0), new Vector2(110, 44)).fontStyle = FontStyle.Bold;
            var dot = Ui.Box(card, new Vector2(30, 30), new Vector2(90, 158), Pal.Hex("E8392B"), Art.Circle, "redDot");
            dot.type = Image.Type.Simple;
            Ui.Tap(chip, () =>
            {
                int got = Save.ClaimCodex((int)d.Id);
                if (got <= 0) return;
                Sfx.Play(Snd.Rain, 0.7f);
                Toast($"{d.Name}入伙赏钱 铜钱 +{got}");
                Refresh();
                refill();
            });
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
                Ui.Stroke(Ui.Label(t, b.Name, 44, ready ? Pal.Hex("B5651D") : Pal.InkSoft, new Vector2(-300, 46), new Vector2(280, 62), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                Ui.Label(t, $"{b.Need}人同场 · 伤+{Mathf.RoundToInt(b.Dmg * 100)}% · {Bond.TraitName(b.Trait)}", 22, Pal.RedDeep, new Vector2(-300, -4), new Vector2(288, 32)).fontStyle = FontStyle.Bold;
                Ui.Label(t, $"{b.Story}\n<color=#7A1F12>【{Bond.TraitName(b.Trait)}】{Bond.TraitDesc(b.Trait)}</color>", 23, Pal.Ink, new Vector2(145, 36), new Vector2(576, 76), TextAnchor.MiddleLeft).supportRichText = true;
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
