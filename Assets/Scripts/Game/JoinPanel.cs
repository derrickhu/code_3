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
        /// <summary>小课堂里点了「直奔下一回」：回到主页后直接开下一回。</summary>
        public static int WantNextChapter = -1;

        public static void Show(List<HeroId> heroes, int chapter, bool seat, Action done)
        {
            WantNextChapter = -1;
            Sfx.PlayStory();
            var layer = Ui.Fill(Ui.Root, new Color(0.07f, 0.05f, 0.04f, 0.92f), true, "join");
            int count = heroes != null ? heroes.Count : 0;
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
                Analytics.LoreEvent(ch.Index + 1, "open_after_win", false);
                page = LorePage(layer, ch, true, Finish, null);
            }

            if (count <= 0)
            {
                Lore();
                return;
            }
            if (count == 1)
            {
                var d = HeroDef.Get(heroes[0]);
                page = Ui.Rect(Ui.Fit(layer, 1800), "page");
                Rays(page, new Vector2(0, 640));
                var title = Ui.Title(page, "好汉入伙", new Vector2(0, 820), 100, Pal.GoldBright);
                Tween.Pop(title.transform, 0.35f);
                var sheet = HeroSheet(page, d, true, new Vector2(0, -10));
                Tween.Pop(sheet, 0.4f, 0.05f);
                Sfx.Play(Snd.Join, 0.9f);
                Ui.CartoonBtn(page, "水浒小课堂", new Vector2(440, 128), new Vector2(165, -760), Ui.BtnRed, Lore, 52);
                Ui.CartoonBtn(page, "好汉谱", new Vector2(280, 112), new Vector2(-245, -760), Ui.BtnBlue, () =>
                {
                    WantCodex = true;
                    Lore();
                }, 44);
                return;
            }

            // 多位好汉同回入伙（如第一回少华山与史进四将）：一页群像齐拜山，轻点任意好汉可展阅完整拜帖，一键进入水浒小课堂。
            page = GroupJoinPage(layer, heroes, chapter, Lore);
        }

        static RectTransform GroupJoinPage(RectTransform layer, List<HeroId> heroes, int chapter, Action onNext)
        {
            var page = Ui.Rect(Ui.Fit(layer, 1840), "groupJoin");
            Rays(page, new Vector2(0, 640));
            var title = Ui.Title(page, "群雄拜山", new Vector2(0, 810), 100, Pal.GoldBright);
            Tween.Pop(title.transform, 0.35f);
            string chName = chapter >= 0 && chapter < ChapterDef.StoryCount ? ChapterDef.All[chapter].Name : "梁山";
            Ui.Label(page, $"{chName} · {heroes.Count} 位好汉同归大义（点卡片可看详情）", 32, Pal.Paper.A(0.85f), new Vector2(0, 705), new Vector2(920, 48));
            Sfx.Play(Snd.Join, 0.9f);

            int showCount = Mathf.Min(heroes.Count, 4);
            bool twoOnly = showCount == 2;
            for (int idx = 0; idx < showCount; idx++)
            {
                var d = HeroDef.Get(heroes[idx]);
                bool di = d.Disha;
                Color rim = d.Rank <= 0 ? Pal.Hex("8C6B4E") : di ? DiColor : TianColor;
                Vector2 size = twoOnly ? new Vector2(440, 880) : new Vector2(452, 580);
                Vector2 pos = twoOnly
                    ? new Vector2(idx == 0 ? -240 : 240, 20)
                    : new Vector2(idx % 2 == 0 ? -242 : 242, idx < 2 ? 320 : -295);

                var face = Ui.Framed(page, size, pos, Pal.Hex("F6EEDA"), rim, 8f, "heroCard");
                var t = face.transform;
                Ui.Box(t, new Vector2(size.x - 28, twoOnly ? 380 : 260), new Vector2(0, twoOnly ? 160 : 95), rim.A(0.14f), null, "band").pixelsPerUnitMultiplier = 1f;
                Portrait(t, d.Id, new Vector2(0, twoOnly ? 165 : 98), twoOnly ? new Vector2(310, 340) : new Vector2(236, 244));

                if (d.Rank > 0)
                    Ui.Tag(t, d.Star + " · " + d.Rank108, rim, new Vector2(0, size.y * 0.5f - 36), 250, 24);
                else
                    Ui.Tag(t, "隐藏好汉", rim, new Vector2(0, size.y * 0.5f - 36), 180, 24);

                float nameY = twoOnly ? -90 : -82;
                Ui.Label(t, d.Nick, 32, Pal.RedDeep, new Vector2(0, nameY + 48), new Vector2(size.x - 36, 42), TextAnchor.MiddleCenter).fontStyle = FontStyle.Bold;
                var nm = Ui.Label(t, d.Name, d.Name.Length > 2 ? 56 : 70, Pal.Ink, new Vector2(0, nameY - 14), new Vector2(size.x - 36, 84), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Stroke(nm, Pal.Hex("FFF6E2"), 2f);

                if (!string.IsNullOrEmpty(d.Weapon))
                    Ui.Label(t, "兵器：" + d.Weapon, 25, Pal.Hex("7A5432"), new Vector2(0, nameY - 76), new Vector2(size.x - 40, 36));
                var sk = Ui.Label(t, d.Skill, 24, Pal.InkSoft, new Vector2(0, twoOnly ? -285 : -214), new Vector2(size.x - 44, twoOnly ? 110 : 76));
                sk.resizeTextForBestFit = true;
                sk.resizeTextMinSize = 18;
                sk.resizeTextMaxSize = 24;

                var heroDef = d;
                Ui.Tap(face, () =>
                {
                    var shade = Ui.Fill(layer, new Color(0, 0, 0, 0.72f), true, "sheetPreview");
                    Ui.Tap(shade.GetComponent<Image>(), () => UnityEngine.Object.Destroy(shade.gameObject));
                    var sheet = HeroSheet(shade, heroDef, true, new Vector2(0, -10));
                    var block = Ui.Box(sheet, new Vector2(880, 1220), Vector2.zero, Color.clear, null, "block");
                    block.raycastTarget = true;
                    block.transform.SetAsFirstSibling();
                    Ui.CloseX(sheet, new Vector2(-430, 600), () => UnityEngine.Object.Destroy(shade.gameObject), 100);
                    Tween.Pop(sheet, 0.22f);
                });
                Tween.Pop(face.transform.parent, 0.32f, 0.05f * idx);
            }

            Ui.CartoonBtn(page, "水浒小课堂", new Vector2(450, 128), new Vector2(165, -745), Ui.BtnRed, onNext, 52);
            Ui.CartoonBtn(page, "好汉谱", new Vector2(280, 112), new Vector2(-245, -745), Ui.BtnBlue, () =>
            {
                WantCodex = true;
                onNext();
            }, 44);
            return page;
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

        /// <summary>
        /// 立绘窗。画布 640，脚在 0.86、头高的能到 0.25、兵器最宽约 0.58，把这段装进窗口并裁掉窗外。
        /// </summary>
        public static RectTransform Portrait(Transform parent, HeroId id, Vector2 pos, Vector2 window)
        {
            var clip = Ui.Rect(parent, "portrait");
            clip.anchoredPosition = pos;
            clip.sizeDelta = window;
            clip.gameObject.AddComponent<RectMask2D>();
            var ground = Ui.Box(clip, new Vector2(window.x * 0.52f, 16f), new Vector2(0, -window.y * 0.44f),
                new Color(0.22f, 0.13f, 0.06f, 0.32f), Art.Circle, "ground");
            ground.type = Image.Type.Simple;
            ground.raycastTarget = false;

            const float top = 0.23f, bot = 0.90f, wide = 0.62f;
            float canvas = Mathf.Min(window.y / (bot - top), window.x / wide);
            var fig = Ui.Box(clip, new Vector2(canvas, canvas), Vector2.zero, Color.white, null, "figure");
            fig.type = Image.Type.Simple;
            fig.preserveAspect = true;
            fig.raycastTarget = false;
            fig.enabled = false;
            fig.rectTransform.anchoredPosition = new Vector2(0f, ((top + bot) * 0.5f - 0.5f) * canvas);
            ArtPack.Ensure(Art.HeroPack(id), () =>
            {
                if (!fig) return;
                var sp = Art.HeroFrame(id, 0);
                if (sp == null) return;
                fig.sprite = sp;
                fig.enabled = true;
            });
            return clip;
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
            Ui.Box(holder, new Vector2(840, 340), new Vector2(0, 390), rim.A(0.14f), null, "band").pixelsPerUnitMultiplier = 1f;
            var t = holder;
            if (joined) Portrait(t, d.Id, new Vector2(-230, 378), new Vector2(300, 300));

            if (d.Rank > 0)
            {
                Ui.Tag(t, d.Star, rim, new Vector2(-280, 560), 190, 34);
                Ui.Tag(t, (di ? "地煞 · " : "天罡 · ") + d.Rank108, rim, new Vector2(270, 560), 230, 30);
            }
            else Ui.Tag(t, "隐藏好汉", Pal.Hex("8C6B4E"), new Vector2(0, 560), 200, 32);

            if (joined)
            {
                Ui.Label(t, d.Nick, 40, Pal.Gold, new Vector2(188, 498), new Vector2(420, 56), TextAnchor.MiddleLeft, Fonts.Brush);
                var name = Ui.Label(t, d.Name, d.Name.Length > 2 ? 68 : 92, Pal.Ink, new Vector2(188, 400), new Vector2(420, 110), TextAnchor.MiddleLeft, Fonts.Brush);
                Ui.Stroke(name, Pal.Hex("FFF6E2"), 2.5f);
                if (!string.IsNullOrEmpty(d.Post))
                {
                    var post = Ui.Label(t, "梁山职司：" + d.Post, 30, Pal.InkSoft, new Vector2(188, 308), new Vector2(420, 72), TextAnchor.MiddleLeft);
                    post.resizeTextForBestFit = true;
                    post.resizeTextMinSize = 20;
                    post.resizeTextMaxSize = 30;
                }
            }
            else
            {
                Ui.Label(t, d.Nick, 52, Pal.Gold, new Vector2(0, 470), new Vector2(800, 70), TextAnchor.MiddleCenter, Fonts.Brush);
                var name = Ui.Label(t, "？？", 190, Pal.Ink, new Vector2(0, 352), new Vector2(840, 210), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Stroke(name, Pal.Hex("FFF6E2"), 3f);
                if (!string.IsNullOrEmpty(d.Post))
                    Ui.Label(t, "梁山职司：" + d.Post, 34, Pal.InkSoft, new Vector2(0, 232), new Vector2(820, 46));
            }

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

        /// <summary>在山寨回目卷里随时打开某一回的水浒小课堂，支持前后翻卷与补答考校领赏。</summary>
        public static void ShowLoreModal(Transform host, int chapterIndex, Action onChanged = null)
        {
            int cur = Mathf.Clamp(chapterIndex, 0, ChapterDef.StoryCount - 1);
            var layer = Ui.Fill(host, new Color(0.06f, 0.05f, 0.04f, 0.88f), true, "loreModal");
            RectTransform page = null;
            void Rebuild()
            {
                if (page) UnityEngine.Object.Destroy(page.gameObject);
                Analytics.LoreEvent(cur + 1, "revisit", Save.LoreAnswered(cur));
                page = LorePage(layer, ChapterDef.All[cur], false, () =>
                {
                    UnityEngine.Object.Destroy(layer.gameObject);
                    onChanged?.Invoke();
                }, delta =>
                {
                    int maxOpen = Mathf.Clamp(Mathf.Max(0, Save.D.unlocked - 1), 0, ChapterDef.StoryCount - 1);
                    cur = Mathf.Clamp(cur + delta, 0, maxOpen);
                    Rebuild();
                }, onChanged);
            }
            Rebuild();
        }

        static RectTransform LorePage(RectTransform layer, ChapterDef ch, bool fromBattle, Action done, Action<int> stepChapter, Action onQuizReward = null)
        {
            var page = Ui.Rect(Ui.Fit(layer, 1840), "lore");
            Ui.Title(page, "水浒小课堂", new Vector2(0, 815), 94, Pal.GoldBright);
            int mastered = Save.LoreAnsweredCount;
            string scholar = ShuihuLore.ScholarTitle(mastered);
            Ui.Label(page, $"梁山书场【{scholar}】· 已通晓 {mastered}/{ChapterDef.StoryCount} 卷　好汉谱 {Mathf.Min(Save.JoinedCount, 108)}/108",
                27, Pal.Paper.A(0.88f), new Vector2(0, 728), new Vector2(920, 40));

            var card = Ui.WoodPanel(page, new Vector2(960, 1340), new Vector2(0, 20), Pal.Hex("8C6B4E"), Pal.Hex("F3E6C6"));
            if (!fromBattle)
                Ui.CloseX(card, new Vector2(960 * 0.5f - 108f, 1340 * 0.5f - 104f), done, 88f);

            var entry = ShuihuLore.Of(ch.Index);

            // 抬头：第X回　回名 + 风物专题签
            var chTitle = Ui.Label(card, $"第{BattleView.ToCn(ch.Index + 1)}回　{ch.Name}", 48, Pal.Ink,
                new Vector2(entry != null ? -110 : 0, 578), new Vector2(entry != null ? 580 : 820, 68),
                entry != null ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, Fonts.Brush);
            chTitle.resizeTextForBestFit = true;
            chTitle.resizeTextMinSize = 32;
            chTitle.resizeTextMaxSize = 48;
            if (entry != null)
                Ui.Tag(card, entry.Topic, Pal.Hex("7A2E22"), new Vector2(275, 578), 260, 24);

            if (!string.IsNullOrEmpty(ch.Hui))
            {
                var hui = Ui.Label(card, "《水浒传》" + ch.Hui, 26, Pal.Hex("9A6A1E"), new Vector2(0, 520), new Vector2(830, 40), TextAnchor.MiddleLeft);
                hui.resizeTextForBestFit = true;
                hui.resizeTextMinSize = 18;
                hui.resizeTextMaxSize = 26;
            }

            if (entry != null && !string.IsNullOrEmpty(entry.Verse))
            {
                var verseBar = Ui.Framed(card, new Vector2(836, 66), new Vector2(0, 458), Pal.Hex("EFE0BE"), Pal.Hex("C8962E"), 4f, "verse");
                var vl = Ui.Label(verseBar.transform, "「" + entry.Verse + "」", 34, Pal.RedDeep, Vector2.zero, new Vector2(800, 56), TextAnchor.MiddleCenter, Fonts.Brush);
                vl.resizeTextForBestFit = true;
                vl.resizeTextMinSize = 22;
                vl.resizeTextMaxSize = 34;
            }

            // 第一栏：原著讲古
            Ui.Tag(card, "原著讲古", Pal.Hex("B5651D"), new Vector2(-334, 384), 160, 24);
            var loreBody = Ui.Label(card, ch.Lore, 30, Pal.Ink, new Vector2(0, 286), new Vector2(828, 146), TextAnchor.UpperLeft);
            loreBody.lineSpacing = 1.08f;
            loreBody.verticalOverflow = VerticalWrapMode.Truncate;

            Ui.Box(card, new Vector2(828, 3), new Vector2(0, 200), Pal.Hex("C9B48E"), null, "rule").pixelsPerUnitMultiplier = 1f;

            // 第二栏：宋风知微（宋代风物、制度、成语与江湖规矩）
            if (entry != null)
            {
                Ui.Tag(card, "宋风知微", Pal.Hex("4A6B53"), new Vector2(-334, 162), 160, 24);
                var noteBody = Ui.Label(card, entry.Note, 28, Pal.Hex("3C3024"), new Vector2(0, 56), new Vector2(828, 164), TextAnchor.UpperLeft);
                noteBody.lineSpacing = 1.08f;
                noteBody.verticalOverflow = VerticalWrapMode.Truncate;

                // 第三栏：小课堂考校（答对立赏铜钱，内衬底边在 -638，考校框底边停在 -585 绝不出框）
                BuildQuizBox(card, ch.Index, entry, onQuizReward);
            }

            if (fromBattle)
            {
                int next = ch.Index + 1;
                bool canDirectNext = next < ChapterDef.StoryCount && ch.Index < 3 && Stamina.Current >= Stamina.Cost;
                if (canDirectNext)
                {
                    string nextLabel = $"下一回　{ChapterDef.All[next].Name}";
                    var goNext = Ui.CartoonBtn(page, nextLabel, new Vector2(500, 126), new Vector2(150, -745), Ui.BtnRed, () =>
                    {
                        WantNextChapter = next;
                        done?.Invoke();
                    }, nextLabel.Length > 7 ? 42 : 48);
                    Ui.CartoonBtn(page, "回山寨", new Vector2(280, 112), new Vector2(-275, -745), Ui.BtnDark, done, 44);
                    Tween.Run(goNext, 999f, k =>
                    {
                        if (goNext) goNext.localScale = Vector3.one * (1f + 0.035f * Mathf.Sin(k * 999f * 4.2f));
                    });
                }
                else
                {
                    Ui.CartoonBtn(page, "回山寨", new Vector2(440, 126), new Vector2(0, -745), Ui.BtnRed, done, 54);
                }
            }
            else
            {
                int maxOpen = Mathf.Clamp(Mathf.Max(0, Save.D.unlocked - 1), 0, ChapterDef.StoryCount - 1);
                if (ch.Index > 0)
                    Ui.CartoonBtn(page, "上一回", new Vector2(240, 108), new Vector2(-310, -745), Ui.BtnBlue, () => stepChapter?.Invoke(-1), 40);
                Ui.CartoonBtn(page, "合上书卷", new Vector2(320, 116), new Vector2(0, -745), Ui.BtnRed, done, 46);
                if (ch.Index < maxOpen)
                    Ui.CartoonBtn(page, "下一回", new Vector2(240, 108), new Vector2(310, -745), Ui.BtnBlue, () => stepChapter?.Invoke(1), 40);
            }

            Tween.Pop(card, 0.32f);
            return page;
        }

        static void BuildQuizBox(RectTransform card, int chapterIndex, LoreEntry entry, Action onQuizReward)
        {
            var box = Ui.Framed(card, new Vector2(840, 536), new Vector2(0, -316), Pal.Hex("EFE2C4"), Pal.Hex("B5823A"), 5f, "quizBox");
            var qt = box.transform;

            void RenderQuiz(int wrongPick, int justEarned)
            {
                for (int i = qt.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(qt.GetChild(i).gameObject);
                bool solved = Save.LoreAnswered(chapterIndex);

                Ui.Stroke(Ui.Label(qt, "书场考校", 34, Pal.RedDeep, new Vector2(-310, 224), new Vector2(200, 44), TextAnchor.MiddleLeft, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
                if (solved)
                    Ui.Tag(qt, "已通晓 · 博学", Pal.Willow, new Vector2(265, 224), 230, 22);
                else
                    Ui.Tag(qt, $"答对赏铜钱 +{ShuihuLore.QuizRewardCoins}", Pal.Hex("C8871E"), new Vector2(250, 224), 260, 22);

                var qLabel = Ui.Label(qt, entry.Question, 28, Pal.Ink, new Vector2(0, 152), new Vector2(784, 78), TextAnchor.MiddleLeft);
                qLabel.fontStyle = FontStyle.Bold;
                qLabel.lineSpacing = 1.05f;
                qLabel.resizeTextForBestFit = true;
                qLabel.resizeTextMinSize = 22;
                qLabel.resizeTextMaxSize = 28;

                string[] prefixes = { "甲 · ", "乙 · ", "丙 · " };
                for (int i = 0; i < 3 && i < entry.Options.Length; i++)
                {
                    int idx = i;
                    float oy = 56f - i * 86f;
                    bool isRight = idx == entry.Answer;
                    bool isWrongPick = !solved && wrongPick == idx;

                    Color faceCol = solved && isRight ? Pal.Hex("D6EAD0")
                        : isWrongPick ? Pal.Hex("EAD0CA")
                        : Pal.Hex("FAF3E3");
                    Color rimCol = solved && isRight ? Pal.Willow
                        : isWrongPick ? Pal.Red
                        : Pal.Hex("8C6B4E");

                    var opt = Ui.Framed(qt, new Vector2(784, 70), new Vector2(0, oy), faceCol, rimCol, solved && isRight ? 5f : 3.5f, "opt_" + i);
                    string suffix = solved && isRight ? "　（正解）" : isWrongPick ? "　（再想想）" : "";
                    var ol = Ui.Label(opt.transform, prefixes[i] + entry.Options[i] + suffix, 27,
                        isWrongPick ? Pal.RedDeep : Pal.Ink, new Vector2(12, 0), new Vector2(740, 60), TextAnchor.MiddleLeft);
                    if (solved && isRight) ol.fontStyle = FontStyle.Bold;
                    if (!solved)
                    {
                        Ui.Tap(opt, () =>
                        {
                            if (idx == entry.Answer)
                            {
                                int got = Save.ClaimLoreQuiz(chapterIndex);
                                Sfx.Play(Snd.Rain, 0.85f);
                                Analytics.LoreEvent(chapterIndex + 1, "quiz_correct", true);
                                onQuizReward?.Invoke();
                                RenderQuiz(-1, got);
                            }
                            else
                            {
                                Sfx.Play(Snd.Deny, 0.7f);
                                Analytics.LoreEvent(chapterIndex + 1, "quiz_wrong", false);
                                RenderQuiz(idx, 0);
                            }
                        });
                    }
                }

                string foot = justEarned > 0 ? $"妙哉！通晓本回典故，赏铜钱 +{justEarned}！（{ShuihuLore.NextScholarHint(Save.LoreAnsweredCount)}）"
                    : solved ? $"已收入书场卷宗 · {ShuihuLore.NextScholarHint(Save.LoreAnsweredCount)}"
                    : wrongPick >= 0 ? "差一点点！答案就在上方的「原著讲古」与「宋风知微」里，再选一次照领赏钱"
                    : "轻点上方一处答案参与考校，读水浒、领铜钱";
                Color footCol = justEarned > 0 || solved ? Pal.Willow : wrongPick >= 0 ? Pal.RedDeep : Pal.InkSoft;
                var fl = Ui.Label(qt, foot, 23, footCol, new Vector2(0, -214), new Vector2(788, 48));
                fl.resizeTextForBestFit = true;
                fl.resizeTextMinSize = 18;
                fl.resizeTextMaxSize = 23;
            }

            RenderQuiz(-1, 0);
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
