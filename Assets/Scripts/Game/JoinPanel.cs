using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>赢下回目后的「好汉入伙」：一人一张拜帖，读完是本回的水浒小课堂；一百单八将聚齐时再放忠义堂排座次。</summary>
    public static class JoinPanel
    {
        static readonly Color Edge = Ui.Edge;
        static readonly Color TianColor = Pal.Hex("C8962E");
        static readonly Color DiColor = Pal.Hex("8A54C0");

        /// <summary>入伙流程里点了「查看好汉谱」：回到主页后直接打开好汉谱。</summary>
        public static bool WantCodex;

        public static void Show(List<HeroId> heroes, int chapter, bool seat, Action done)
        {
            Sfx.PlayStory();
            var layer = Ui.Fill(Ui.Root, new Color(0.07f, 0.05f, 0.04f, 0.92f), true, "join");
            int i = 0;
            RectTransform page = null;

            void Finish()
            {
                UnityEngine.Object.Destroy(layer.gameObject);
                if (seat) ShowSeats(done);
                else done?.Invoke();
            }

            void Lore()
            {
                if (page) UnityEngine.Object.Destroy(page.gameObject);
                var ch = chapter >= 0 && chapter < ChapterDef.StoryCount ? ChapterDef.All[chapter] : null;
                if (ch == null || string.IsNullOrEmpty(ch.Lore)) { Finish(); return; }
                page = LorePage(layer, ch, Finish);
            }

            void Next()
            {
                if (page) UnityEngine.Object.Destroy(page.gameObject);
                if (i >= heroes.Count) { Lore(); return; }
                var d = HeroDef.Get(heroes[i]);
                i++;
                page = Ui.Rect(Ui.Fit(layer, 1800), "page");
                Rays(page, new Vector2(0, 640));
                var title = Ui.Title(page, "好汉入伙", new Vector2(0, 820), 100, Pal.GoldBright);
                Tween.Pop(title.transform, 0.35f);
                Ui.Label(page, $"{i} / {heroes.Count}", 36, Pal.Paper.A(0.7f), new Vector2(0, 730), new Vector2(300, 50));
                var sheet = HeroSheet(page, d, true, new Vector2(0, -20));
                Tween.Pop(sheet, 0.4f, 0.05f);
                Sfx.Play(Snd.Join, 0.9f);
                bool last = i >= heroes.Count;
                Ui.CartoonBtn(page, last ? "继  续" : "下一位", new Vector2(420, 128), new Vector2(170, -760), Ui.BtnRed, Next, 56);
                Ui.CartoonBtn(page, "好汉谱", new Vector2(300, 112), new Vector2(-240, -760), Ui.BtnBlue, () =>
                {
                    WantCodex = true;
                    Next();
                }, 46);
            }

            Next();
        }

        static void Rays(RectTransform parent, Vector2 pos)
        {
            var clip = Ui.Rect(parent, "raysClip");
            clip.anchoredPosition = pos;
            clip.sizeDelta = new Vector2(1080, 700);
            var rays = Ui.Icon(clip, "ui_rays", 1000, Vector2.zero, "rays");
            rays.color = Color.white.A(0.55f);
            Ui.Spin(rays.transform, 10f);
        }

        /// <summary>好汉拜帖：星号座次、绰号姓名、职司、兵器（品级与特性）、事迹、本领、绝招、结局。好汉谱详情也用这张。</summary>
        public static RectTransform HeroSheet(Transform parent, HeroDef d, bool joined, Vector2 pos)
        {
            bool di = d.Disha;
            Color rim = d.Rank <= 0 ? Pal.Hex("8C6B4E") : di ? DiColor : TianColor;
            var holder = Ui.Rect(parent, "sheet");
            holder.anchoredPosition = pos;
            Ui.Box(holder, new Vector2(900, 1240), new Vector2(0, -14), Pal.Shadow.A(0.6f), null, "shadow");
            Ui.Framed(holder, new Vector2(880, 1220), Vector2.zero, Pal.Hex("F6EEDA"), rim, 10f, "card");
            Ui.Box(holder, new Vector2(840, 300), new Vector2(0, 430), rim.A(0.14f), null, "band").pixelsPerUnitMultiplier = 1f;
            var t = holder;

            if (d.Rank > 0)
            {
                Ui.Tag(t, d.Star, rim, new Vector2(-280, 560), 190, 34);
                Ui.Tag(t, (di ? "地煞 · " : "天罡 · ") + d.Rank108, rim, new Vector2(270, 560), 230, 30);
            }
            else Ui.Tag(t, "隐藏好汉", Pal.Hex("8C6B4E"), new Vector2(0, 560), 200, 32);

            Ui.Label(t, d.Nick, 52, Pal.Gold, new Vector2(0, 470), new Vector2(800, 70), TextAnchor.MiddleCenter, Fonts.Brush);
            var name = Ui.Label(t, joined ? d.Name : "？？", 190, Pal.Ink, new Vector2(0, 352), new Vector2(840, 210), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(name, Pal.Hex("FFF6E2"), 3f);
            if (!string.IsNullOrEmpty(d.Post))
                Ui.Label(t, "梁山职司：" + d.Post, 34, Pal.InkSoft, new Vector2(0, 232), new Vector2(820, 46));

            float y = 108;
            if (!string.IsNullOrEmpty(d.Weapon))
            {
                var g = Ui.GradeColor(d.WGrade);
                var plate = Ui.Framed(t, new Vector2(800, 150), new Vector2(0, y), Pal.Hex("EFE2C4"), g, 5f, "weapon");
                var pt = plate.transform;
                Ui.Box(pt, new Vector2(124, 124), new Vector2(-322, 0), g.A(0.25f), Art.Circle, "halo").type = Image.Type.Simple;
                Ui.Icon(pt, Weapons.Icon(d.WKind), 112, new Vector2(-322, 0));
                Ui.Stroke(Ui.Label(pt, d.Weapon, 50, Pal.Ink, new Vector2(-30, 30), new Vector2(420, 66), TextAnchor.MiddleLeft, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                Ui.Tag(pt, Weapons.Grades[Mathf.Clamp(d.WGrade, 0, 3)], g, new Vector2(300, 32), 120, 28);
                Ui.Label(pt, Weapons.KindName(d.WKind) + " · " + Weapons.AffixText(d.WKind), 27, Pal.InkSoft, new Vector2(40, -34), new Vector2(660, 44), TextAnchor.MiddleLeft);
                y -= 130;
            }
            else y -= 30;

            Ui.Label(t, d.Bio, 33, Pal.Ink, new Vector2(0, y - 90), new Vector2(800, 190), TextAnchor.UpperLeft);
            y -= 205;
            Ui.Label(t, $"<color=#B5651D><b>本领</b></color>　{d.Skill}", 31, Pal.Ink, new Vector2(0, y - 22), new Vector2(800, 50), TextAnchor.MiddleLeft).supportRichText = true;
            y -= 60;
            if (!string.IsNullOrEmpty(d.Ult))
            {
                Ui.Label(t, $"<color=#B22222><b>绝招</b></color>　{d.Ult}", 31, Pal.Ink, new Vector2(0, y - 30), new Vector2(800, 80), TextAnchor.UpperLeft).supportRichText = true;
                y -= 85;
            }
            Ui.Label(t, $"<color=#6B5A48><b>结局</b></color>　{d.Fate}", 29, Pal.InkSoft, new Vector2(0, y - 30), new Vector2(800, 80), TextAnchor.UpperLeft).supportRichText = true;
            y -= 85;
            Ui.Label(t, "「" + d.Quote + "」", 34, Pal.RedDeep, new Vector2(0, Mathf.Min(y - 20, -500)), new Vector2(800, 56), TextAnchor.MiddleCenter, Fonts.Brush);

            int jc = ChapterDef.JoinChapter(d.Id);
            string debut = jc >= 0 ? $"第{BattleView.ToCn(jc + 1)}回「{ChapterDef.All[jc].Name}」入伙" : "隐藏：凑出名字即可得见";
            Ui.Label(t, debut, 28, Pal.InkSoft.A(0.85f), new Vector2(0, -570), new Vector2(800, 44));
            return holder;
        }

        static RectTransform LorePage(RectTransform layer, ChapterDef ch, Action done)
        {
            var page = Ui.Rect(Ui.Fit(layer, 1560), "lore");
            Ui.Title(page, "水浒小课堂", new Vector2(0, 700), 100, Pal.GoldBright);
            var card = Ui.WoodPanel(page, new Vector2(900, 900), new Vector2(0, 40), Pal.Hex("8C6B4E"), Pal.Hex("F3E6C6"));
            Ui.Label(card, $"第{BattleView.ToCn(ch.Index + 1)}回 · {ch.Name}", 52, Pal.Ink, new Vector2(0, 340), new Vector2(800, 80), TextAnchor.MiddleCenter, Fonts.Brush);
            if (!string.IsNullOrEmpty(ch.Hui))
                Ui.Label(card, "《水浒传》" + ch.Hui, 32, Pal.Hex("9A6A1E"), new Vector2(0, 265), new Vector2(820, 50));
            Ui.Box(card, new Vector2(700, 4), new Vector2(0, 225), Pal.Hex("C9B48E"), null, "rule").pixelsPerUnitMultiplier = 1f;
            Ui.Label(card, ch.Lore, 42, Pal.Ink, new Vector2(0, 20), new Vector2(780, 380), TextAnchor.MiddleLeft);
            int next = ch.Index + 1;
            string tip = next < ChapterDef.StoryCount
                ? $"下一回：{ChapterDef.All[next].Name}（{ChapterDef.All[next].Act}）"
                : "剧情已终。聚义厅车轮战开张：自选五员大将，看能撑几阵！";
            Ui.Label(card, tip, 34, Pal.RedDeep, new Vector2(0, -260), new Vector2(800, 90));
            Ui.Label(card, $"好汉谱 {Save.JoinedCount} / 108", 32, Pal.InkSoft, new Vector2(0, -340), new Vector2(600, 50));
            Ui.CartoonBtn(page, "回山寨", new Vector2(420, 128), new Vector2(0, -560), Ui.BtnRed, done, 56);
            Tween.Pop(card, 0.35f);
            return page;
        }

        /// <summary>忠义堂排座次：石碣上刻满一百单八将，天罡金、地煞紫。</summary>
        public static void ShowSeats(Action done)
        {
            Sfx.PlayEpic();
            var layer = Ui.Fill(Ui.Root, new Color(0.05f, 0.04f, 0.03f, 0.95f), true, "seats");
            var fit = Ui.Fit(layer, 1880);
            Rays(fit, new Vector2(0, 760));
            var title = Ui.Title(fit, "英雄排座次", new Vector2(0, 860), 110, Pal.GoldBright);
            Tween.Pop(title.transform, 0.45f);
            Ui.Label(fit, "忠义堂前，天降石碣：替天行道 · 忠义双全", 38, Pal.Paper.A(0.85f), new Vector2(0, 765), new Vector2(1000, 56));
            Sfx.Play(Snd.RankUp);

            var stone = Ui.Framed(fit, new Vector2(1000, 1360), new Vector2(0, -40), Pal.Hex("4A4640"), Pal.Hex("8C8478"), 10f, "stele");
            var content = Ui.Scroll(stone.transform, new Vector2(980, 1340), Vector2.zero, 0);
            const int cols = 4;
            const float rowH = 64f;
            float h = 90 + 9 * rowH + 110 + 18 * rowH + 40;
            content.sizeDelta = new Vector2(980, h);
            float y = 50;
            Section(content, "三十六天罡", TianColor, ref y);
            int k = 0;
            foreach (var d in Ranked(1, 36)) { Seat(content, d, k++, cols, rowH, y, TianColor); }
            y += Mathf.Ceil(36f / cols) * rowH + 60;
            Section(content, "七十二地煞", DiColor, ref y);
            k = 0;
            foreach (var d in Ranked(37, 108)) { Seat(content, d, k++, cols, rowH, y, DiColor); }

            Ui.CartoonBtn(fit, "替天行道", new Vector2(460, 132), new Vector2(0, -800), Ui.BtnRed, () =>
            {
                UnityEngine.Object.Destroy(layer.gameObject);
                done?.Invoke();
            }, 58);
        }

        static IEnumerable<HeroDef> Ranked(int from, int to)
        {
            var list = new List<HeroDef>();
            foreach (var d in HeroDef.All) if (d.Rank >= from && d.Rank <= to) list.Add(d);
            list.Sort((a, b) => a.Rank.CompareTo(b.Rank));
            return list;
        }

        static void Section(RectTransform content, string name, Color c, ref float y)
        {
            var l = Ui.Label(content, name, 52, c, Ui.Top(content, 0, y), new Vector2(900, 70), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(l, Edge, 2.5f);
            y += 60;
        }

        static void Seat(RectTransform content, HeroDef d, int k, int cols, float rowH, float y0, Color c)
        {
            float w = 940f / cols;
            float x = -470 + w * (k % cols + 0.5f);
            float y = y0 + (k / cols) * rowH + rowH * 0.5f;
            var l = Ui.Label(content, $"<size=22><color=#BFB7A8>{d.Rank}</color></size> <color=#{ColorUtility.ToHtmlStringRGB(c)}>{d.Nick}</color> {d.Name}",
                28, Pal.Paper, Ui.Top(content, x, y), new Vector2(w, rowH), TextAnchor.MiddleCenter);
            l.supportRichText = true;
            l.horizontalOverflow = HorizontalWrapMode.Overflow;
        }
    }
}
