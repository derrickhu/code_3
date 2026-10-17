using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>主界面的剧情页：回目卷（按篇章列出五十七回）和聚义厅点将。</summary>
    public sealed partial class HomeScreen
    {
        // ─────────────────────────── 回目卷 ───────────────────────────

        public void ShowChapters()
        {
            var layer = Panel("水浒回目", out var card, new Vector2(1000, 1700));
            int done = Mathf.Min(Save.D.unlocked, ChapterDef.StoryCount);
            Ui.Label(card, $"已打通 {done}/{ChapterDef.StoryCount} 回　入伙 {Mathf.Min(Save.JoinedCount, 108)}/108 位", 32, Pal.InkSoft,
                new Vector2(0, 720), new Vector2(900, 50)).fontStyle = FontStyle.Bold;

            const float rowH = 150f, actH = 80f;
            var rows = new List<(string act, int index)>();
            string last = null;
            for (int i = 0; i < ChapterDef.StoryCount; i++)
            {
                var ch = ChapterDef.All[i];
                if (ch.Act != last) { rows.Add((ch.Act, -1)); last = ch.Act; }
                rows.Add((null, i));
            }
            rows.Add(("终章之后", -1));
            rows.Add((null, ChapterDef.StoryCount));

            float h = 20;
            foreach (var r in rows) h += r.index < 0 ? actH : rowH;
            var content = Ui.Scroll(card, new Vector2(940, 1370), new Vector2(0, -40), h + 20, "chapters");
            float y = 20;
            RectTransform focus = null;
            foreach (var r in rows)
            {
                if (r.index < 0)
                {
                    var p = Ui.Top(content, 0, y + actH * 0.5f);
                    Ui.Box(content, new Vector2(880, 4), p + new Vector2(0, -2), Pal.Hex("8C6B4E").A(0.6f), null, "rule").pixelsPerUnitMultiplier = 1f;
                    var plate = Ui.Framed(content, new Vector2(300, 60), p, Pal.Hex("7A2E22"), Edge, 4f, "framedAct");
                    Ui.Label(plate.transform, r.act, 36, Pal.Paper, new Vector2(0, 2), new Vector2(300, 60), TextAnchor.MiddleCenter, Fonts.Brush);
                    y += actH;
                    continue;
                }
                var row = ChapterRow(layer, content, r.index, Ui.Top(content, 0, y + rowH * 0.5f));
                if (r.index == Mathf.Min(Save.D.unlocked, ChapterDef.StoryCount)) focus = row;
                y += rowH;
            }
            if (focus)
            {
                float view = ((RectTransform)content.parent).sizeDelta.y;
                float from = content.sizeDelta.y * 0.5f - focus.anchoredPosition.y;
                Ui.ScrollTo(content, from - view * 0.4f);
            }
        }

        RectTransform ChapterRow(RectTransform layer, RectTransform content, int i, Vector2 pos)
        {
            bool endless = i >= ChapterDef.StoryCount;
            bool open = i <= Save.D.unlocked;
            bool cleared = !endless && i < Save.D.unlocked;
            bool current = i == Mathf.Min(Save.D.unlocked, ChapterDef.StoryCount);
            bool sel = i == _selected;
            Color bg = !open ? Pal.Hex("A39C92") : current ? Pal.Hex("FFE9A8") : Pal.Hex("F3E6C6");
            Color rim = sel ? Pal.Hex("D9822B") : current ? Pal.Hex("C0392B") : Edge;
            var face = Ui.Framed(content, new Vector2(900, 136), pos, bg, rim, sel || current ? 6f : 4f, "framedChapter");
            var t = face.transform;
            var rt = (RectTransform)t.parent;

            var seal = Ui.Framed(t, new Vector2(104, 104), new Vector2(-385, 0), endless ? Pal.Hex("7A2E22") : open ? Pal.Hex("C0392B") : Pal.Hex("6E665C"), Edge, 4f, "seal");
            var num = endless ? "义" : BattleView.ToCn(i + 1);
            Ui.Label(seal.transform, num, num.Length > 2 ? 34 : num.Length > 1 ? 44 : 60, Pal.Paper, new Vector2(0, 2), new Vector2(104, 104), TextAnchor.MiddleCenter, Fonts.Brush);

            if (endless)
            {
                Ui.Label(t, "聚义厅 · 车轮战", 46, open ? Pal.Ink : Pal.InkSoft, new Vector2(-50, 28), new Vector2(520, 64), TextAnchor.MiddleLeft, Fonts.Brush);
                Ui.Label(t, open ? $"自选五将　当前第{BattleView.ToCn(Save.D.endlessRound)}阵　最好 {Save.D.endlessBest} 阵" : "打通蓼儿洼后开放：一百单八将任你点将",
                    26, Pal.InkSoft, new Vector2(-40, -28), new Vector2(540, 40), TextAnchor.MiddleLeft);
            }
            else
            {
                var ch = ChapterDef.All[i];
                Ui.Label(t, open ? ch.Name : "？？？", 46, open ? Pal.Ink : Pal.InkSoft, new Vector2(-50, 28), new Vector2(520, 64), TextAnchor.MiddleLeft, Fonts.Brush);
                var sub = open ? ch.Hui : "赢下上一回才能翻开";
                var hui = Ui.Label(t, sub, 24, Pal.InkSoft, new Vector2(-40, -28), new Vector2(540, 40), TextAnchor.MiddleLeft);
                hui.resizeTextForBestFit = true;
                hui.resizeTextMinSize = 16;
                hui.resizeTextMaxSize = 24;
                if (ch.Joins.Length > 0)
                {
                    var sb = new System.Text.StringBuilder();
                    for (int k = 0; k < ch.Joins.Length && k < 4; k++)
                    {
                        var d = HeroDef.Get(ch.Joins[k]);
                        sb.Append(Save.Joined(d.Id) ? d.Name : "？").Append(k + 1 < Mathf.Min(4, ch.Joins.Length) ? "·" : "");
                    }
                    var jl = Ui.Label(t, sb.ToString(), 28, cleared ? Pal.Willow : Pal.RedDeep, new Vector2(215, 22), new Vector2(280, 44), TextAnchor.MiddleRight, Fonts.Brush);
                    jl.resizeTextForBestFit = true;
                    jl.resizeTextMinSize = 18;
                    jl.resizeTextMaxSize = 28;
                    Ui.Label(t, cleared ? "已入伙" : $"{ch.Joins.Length}人入伙", 22, Pal.InkSoft, new Vector2(255, -26), new Vector2(200, 34), TextAnchor.MiddleRight);
                }
                else Ui.Label(t, cleared ? "已平定" : "征战", 26, Pal.InkSoft, new Vector2(255, 0), new Vector2(200, 40), TextAnchor.MiddleRight);
            }
            if (cleared)
            {
                var stamp = Ui.Label(t, "胜", 58, Pal.Red.A(0.8f), new Vector2(400, 0), new Vector2(76, 76), TextAnchor.MiddleCenter, Fonts.Brush);
                stamp.rectTransform.localRotation = Quaternion.Euler(0, 0, -14);
            }
            Ui.Tap(face, () =>
            {
                if (!open) { Nope(endless ? "打完蓼儿洼，聚义厅才开张" : "先赢下上一回才能解锁"); return; }
                _selected = i;
                Destroy(layer.gameObject);
                RefreshChapter();
                Tween.Punch(_chapterName.transform, 0.15f);
                if (endless) ShowTeam();
            });
            return rt;
        }

        // ─────────────────────────── 聚义厅点将 ───────────────────────────

        List<HeroId> _team;

        public void ShowTeam()
        {
            if (!Save.StoryDone) { Nope("打完蓼儿洼，聚义厅才开张"); return; }
            _team ??= Save.Team();
            Sfx.PlayEpic();
            var layer = Panel("聚义厅 · 点将", out var card, new Vector2(1000, 1700), Sfx.PlayMenu);
            var body = Ui.Rect(card, "body");
            void Fill()
            {
                for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
                TeamBody(layer, body, Fill);
            }
            Fill();
        }

        void TeamBody(RectTransform layer, RectTransform body, System.Action refill)
        {
            Ui.Label(body, $"第{BattleView.ToCn(Save.D.endlessRound)}阵　最好战绩 {Save.D.endlessBest} 阵　败一阵从头再来", 30, Pal.InkSoft,
                new Vector2(0, 720), new Vector2(900, 50)).fontStyle = FontStyle.Bold;

            for (int i = 0; i < ChapterDef.TeamSize; i++)
            {
                var pos = new Vector2(-376 + i * 188, 580);
                bool has = i < _team.Count;
                var d = has ? HeroDef.Get(_team[i]) : null;
                Color tone = !has ? Pal.Hex("8C8478") : d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E");
                var face = Ui.Framed(body, new Vector2(172, 200), pos, has ? Pal.TileHero : Pal.Hex("5E5246"), tone, has ? 6f : 4f, "framedSlot");
                var t = face.transform;
                if (has)
                {
                    Ui.Label(t, d.Name, d.Name.Length > 2 ? 46 : 58, Pal.Ink, new Vector2(0, 22), new Vector2(166, 100), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Label(t, d.Nick, 24, Pal.RedDeep, new Vector2(0, -48), new Vector2(166, 34)).fontStyle = FontStyle.Bold;
                    Ui.Label(t, d.TypeLabel, 20, Pal.InkSoft, new Vector2(0, -78), new Vector2(166, 30));
                    var id = d.Id;
                    Ui.Tap(face, () => { _team.Remove(id); refill(); });
                }
                else Ui.Label(t, "空位", 36, Pal.Paper.A(0.6f), Vector2.zero, new Vector2(166, 60), TextAnchor.MiddleCenter, Fonts.Brush);
            }

            var bonds = new List<string>();
            foreach (var b in Bond.All)
            {
                int n = 0;
                foreach (var id in b.Members) if (_team.Contains(id)) n++;
                if (n >= b.Need) bonds.Add($"{b.Name}+{Mathf.RoundToInt(b.Dmg * 100)}%");
            }
            int letters = 0;
            foreach (var id in _team) letters += HeroDef.Get(id).Name.Length;
            var info = Ui.Label(body, (bonds.Count > 0 ? "羁绊：" + string.Join("　", bonds) : "羁绊：无（同场凑齐有渊源的好汉可加伤害）") + $"\n池里共 {letters} 个金字，越少越好凑", 26,
                bonds.Count > 0 ? Pal.Hex("B5651D") : Pal.InkSoft, new Vector2(0, 440), new Vector2(900, 80));
            info.fontStyle = FontStyle.Bold;

            Ui.CartoonBtn(body, "随机五将", new Vector2(270, 90), new Vector2(-310, 345), Ui.BtnBlue, () => { _team = Save.RandomTeam(); refill(); }, 40);
            Ui.CartoonBtn(body, "羁绊推荐", new Vector2(270, 90), new Vector2(0, 345), Ui.BtnOrange, () => { _team = BondTeam(); refill(); }, 40);
            Ui.CartoonBtn(body, "出征", new Vector2(270, 90), new Vector2(310, 345), Ui.BtnRed, () =>
            {
                if (_team.Count < ChapterDef.TeamSize) { Nope($"要点满{ChapterDef.TeamSize}位好汉"); return; }
                int fighters = 0;
                foreach (var id in _team) if (!HeroDef.Get(id).NoAttack) fighters++;
                if (fighters < 3) { Nope("至少带三位能出手的好汉"); return; }
                Save.D.team.Clear();
                foreach (var id in _team) Save.D.team.Add((int)id);
                Save.Write();
                Destroy(layer.gameObject);
                _selected = ChapterDef.StoryCount;
                RefreshChapter();
                Fight();
            }, 44, "ui_bolt");

            var list = new List<HeroDef>();
            foreach (var d in HeroDef.All) if (Save.Joined(d.Id) && !d.Hidden) list.Add(d);
            list.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            const int cols = 5;
            const float cw = 182f, ch = 168f;
            var content = Ui.Scroll(body, new Vector2(940, 1050), new Vector2(0, -250), Mathf.Ceil(list.Count / (float)cols) * ch + 20, "joined");
            for (int i = 0; i < list.Count; i++)
            {
                var d = list[i];
                bool on = _team.Contains(d.Id);
                var pos = Ui.Top(content, -364 + (i % cols) * cw, 10 + (i / cols) * ch + ch * 0.5f);
                Color tone = d.Disha ? Pal.Hex("8A54C0") : Pal.Hex("C8962E");
                var face = Ui.Framed(content, new Vector2(170, 156), pos, on ? Pal.Hex("FFE58A") : Pal.Hex("F3E6C6"), on ? Pal.Hex("D9822B") : tone, on ? 6f : 3f, "framedPick");
                var t = face.transform;
                Ui.Label(t, d.Name, d.Name.Length > 2 ? 40 : 50, Pal.Ink, new Vector2(0, 18), new Vector2(166, 80), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Label(t, d.Nick, 22, Pal.RedDeep, new Vector2(0, -36), new Vector2(166, 30));
                Ui.Label(t, d.Rank108 + " " + d.TypeLabel, 18, Pal.InkSoft, new Vector2(0, -62), new Vector2(166, 26));
                if (on)
                {
                    var dot = Ui.Box(t, new Vector2(40, 40), new Vector2(66, 58), Pal.Hex("D7362B"), Art.Circle, "on");
                    dot.type = Image.Type.Simple;
                    Ui.Label(dot.transform, "点", 22, Pal.Paper, Vector2.zero, new Vector2(40, 40), TextAnchor.MiddleCenter, Fonts.Brush);
                }
                var id = d.Id;
                Ui.Tap(face, () =>
                {
                    if (_team.Contains(id)) _team.Remove(id);
                    else if (_team.Count >= ChapterDef.TeamSize) { Nope($"最多点{ChapterDef.TeamSize}将，先点上面的好汉换下"); return; }
                    else _team.Add(id);
                    refill();
                });
            }
        }

        /// <summary>挑一组成员都已入伙的羁绊，凑满后随机补齐；每次点换一组。</summary>
        int _bondPick = -1;

        List<HeroId> BondTeam()
        {
            var ready = new List<Bond>();
            foreach (var b in Bond.All)
            {
                int n = 0;
                foreach (var id in b.Members) if (Save.Joined(id)) n++;
                if (n >= b.Need && b.Need <= ChapterDef.TeamSize) ready.Add(b);
            }
            if (ready.Count == 0) return Save.RandomTeam();
            _bondPick = (_bondPick + 1) % ready.Count;
            var bond = ready[_bondPick];
            var t = new List<HeroId>();
            foreach (var id in bond.Members) if (Save.Joined(id) && t.Count < ChapterDef.TeamSize) t.Add(id);
            Toast("推荐羁绊：" + bond.Name);
            return Save.RandomTeam(t);
        }
    }
}
