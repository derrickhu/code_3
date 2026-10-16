using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>结算：胜利是红旗+光芒+卷轴星+众人起舞+掉落+双倍金币；失败是乌云+掉星+被围的宋江，可看广告保星。</summary>
    public static class ResultPanel
    {
        static readonly Color Edge = Ui.Edge;

        public static int WinCoins(ChapterDef ch) => WinCoins(ch.Index, ch.Round);

        public static int WinCoins(int chapter, int round) =>
            round > 0 ? 40 + 5 * Mathf.Min(round, 30) : 30 + 4 * Mathf.Min(chapter, 45);

        public static void Show(BattleResult r, System.Action onClose)
        {
            int coins = r.Quit ? 0 : (r.Win ? WinCoins(r.Chapter, r.Round) : 8 + r.Wave);
            var rank = r.Quit
                ? new Save.RankDelta { OldTier = Save.D.tier, OldStars = Save.D.stars }
                : Save.ApplyResult(r.Win);
            var joined = new List<HeroId>();
            if (r.Round > 0)
            {
                if (r.Win)
                {
                    Save.D.endlessBest = Mathf.Max(Save.D.endlessBest, r.Round);
                    Save.D.endlessRound = r.Round + 1;
                }
                else if (!r.Quit) Save.D.endlessRound = 1;
            }
            else if (r.Win) joined = Save.WinStory(r.Chapter);
            bool seatNow = joined.Count > 0 && !Save.D.seated && Save.AllSeated;
            if (seatNow) Save.D.seated = true;
            foreach (var id in r.Formed) if (!Save.D.heroes.Contains((int)id)) Save.D.heroes.Add((int)id);
            Save.D.coins += coins;
            string loot = Save.RollWeapon(r);
            Save.Write();

            var layer = Ui.Fill(Ui.Root, r.Win ? Pal.Hex("F3EAD6") : Pal.Hex("C9D6DC"), true, "result");
            var c = Ui.Rect(Ui.Fit(layer, 1880), "c");
            bool claimed = false;
            void Close()
            {
                if (claimed) return;
                claimed = true;
                Object.Destroy(layer.gameObject);
                if (joined.Count > 0) JoinPanel.Show(joined, r.Chapter, seatNow, onClose);
                else onClose?.Invoke();
            }

            if (r.Win) Victory(c, rank);
            else Defeat(c, rank, r.Quit);

            var banner = BuildScroll(c, rank, r.Win, out var stars);
            var stage = BuildStage(c, r.Win);

            var tags = new List<(string, Color)>();
            if (!string.IsNullOrEmpty(loot)) tags.Add((loot, Pal.Hex("8A54C0")));
            if (joined.Count > 0) tags.Add(($"{joined.Count} 位好汉入伙", Pal.Hex("C0392B")));
            foreach (var id in r.NewHeroes) tags.Add(("初次聚义 " + HeroDef.Get(id).Name, Pal.Hex("C8962E")));
            float tx = -(tags.Count - 1) * 170f;
            for (int i = 0; i < tags.Count && i < 3; i++)
            {
                var tag = Ui.Tag(c, tags[i].Item1, tags[i].Item2, new Vector2(tx + i * 340, -262), 0, 30);
                Tween.Pop(tag.transform.parent, 0.3f, 1.0f + i * 0.12f);
            }

            Ui.Label(c, r.Quit ? "撤回山寨，不记座次" : $"撑到第 {r.Wave} 波　斩敌 {r.Kills}　及时雨 {r.Rains} 次", 32, Pal.InkSoft, new Vector2(0, -195), new Vector2(900, 50));

            Text coinText = null;
            if (coins > 0)
            {
                var row = Ui.Rect(c, "coins");
                row.anchoredPosition = new Vector2(0, -350);
                Ui.Icon(row, "coin", 96, new Vector2(-70, 0));
                coinText = Ui.Label(row, "×" + coins, 64, Pal.Hex("F2B53A"), new Vector2(70, 0), new Vector2(220, 90), TextAnchor.MiddleLeft);
                coinText.fontStyle = FontStyle.Bold;
                Ui.Stroke(coinText, Edge, 3f);
                Tween.Pop(row, 0.35f, 1.1f);
                CoinBurst(c, new Vector2(0, -350), 1.15f);
            }

            float y = -500;
            if (coins > 0)
            {
                RectTransform dbl = null;
                dbl = Ui.CartoonBtn(c, "双倍金币", new Vector2(560, 132), new Vector2(0, y), Ui.BtnRed, () =>
                    AdStub.Show("双倍铜钱", () =>
                    {
                        Save.D.coins += coins;
                        Save.Write();
                        coinText.text = "×" + coins * 2;
                        Tween.Pop(coinText.transform, 0.3f);
                        CoinBurst(c, new Vector2(0, -350), 0f);
                        Sfx.Play(Snd.Rain);
                        if (dbl) dbl.gameObject.SetActive(false);
                        Tween.Run(coinText, 1.0f, k => { }, Close);
                    }), 58, "ui_video");
                Breathe(dbl);
                y -= 160;
            }
            if (!r.Win && !r.Quit && (rank.DroppedStar || rank.Demoted))
            {
                RectTransform keep = null;
                keep = Ui.CartoonBtn(c, "保住星星", new Vector2(560, 132), new Vector2(0, y), Ui.BtnOrange, () =>
                    AdStub.Show("保星", () =>
                    {
                        Save.D.tier = rank.OldTier;
                        Save.D.stars = rank.OldStars;
                        Save.Write();
                        stars.text = Ui.RankStars(Save.D.tier, Save.D.stars);
                        Tween.Pop(stars.transform, 0.35f);
                        Sfx.Play(Snd.Merge);
                        Ui.Toast("星星保住了！");
                        if (keep) keep.gameObject.SetActive(false);
                    }), 58, "ui_video");
                Breathe(keep);
                y -= 160;
            }
            Ui.CartoonBtn(c, r.Win ? "领取" : (coins > 0 ? "领取" : "关闭"), new Vector2(380, 112), new Vector2(0, y), Ui.BtnDark, Close, 52);
            Ui.Label(c, joined.Count > 0 ? "领完铜钱，新入伙的好汉来拜山" : "领完铜钱，孙二娘的摊子就摆开", 28, Pal.InkSoft.A(0.8f), new Vector2(0, y - 100), new Vector2(800, 44));

            Tween.Pop(banner, 0.4f, 0.15f);
            Tween.Pop(stage, 0.4f, 0.3f);
        }

        static void Breathe(RectTransform t)
        {
            Tween.Run(t, 1.2f, k => { if (t) t.localScale = Vector3.one * (1f + Mathf.Sin(k * Mathf.PI * 2) * 0.04f); }, () => { if (t) Breathe(t); });
        }

        static void Victory(RectTransform c, Save.RankDelta rank)
        {
            var clip = Ui.Rect(c, "raysClip");
            clip.anchoredPosition = new Vector2(0, 620);
            clip.sizeDelta = new Vector2(1080, 480);
            clip.gameObject.AddComponent<RectMask2D>();
            var rays = Ui.Icon(clip, "ui_rays", 860, new Vector2(0, -240), "rays");
            Ui.Spin(rays.transform, 14f);
            Tween.Pop(rays.transform, 0.5f);

            var flag = Ui.Icon(c, "ui_flag", 560, new Vector2(10, 760), "flag");
            var ft = Ui.Label(flag.transform, "胜利", 92, Pal.Paper, new Vector2(10, 20), new Vector2(400, 130), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(ft, Pal.Hex("7A1F12"), 4f);
            Wave(flag.transform);
            Tween.Pop(flag.transform, 0.45f);

            string head = rank.Promoted ? "恭喜晋升" : Save.D.stars > rank.OldStars ? "升  星" : "大  胜";
            var h = Ui.Label(c, head, 70, Pal.Hex("FFE36A"), new Vector2(0, 602), new Vector2(700, 100), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(h, Edge, 4f);
            Tween.Pop(h.transform, 0.4f, 0.3f);
        }

        static void Defeat(RectTransform c, Save.RankDelta rank, bool quit)
        {
            var cloud = Ui.Icon(c, "ui_cloud", 560, new Vector2(0, 700), "cloud");
            var ct = Ui.Label(cloud.transform, quit ? "撤退" : "失败", 96, Pal.Paper, new Vector2(0, 30), new Vector2(400, 130), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(ct, Edge, 4f);
            Tween.Pop(cloud.transform, 0.45f);
            var basePos = cloud.rectTransform.anchoredPosition;
            Tween.Run(cloud, 3f, k => { if (cloud) cloud.rectTransform.anchoredPosition = basePos + new Vector2(Mathf.Sin(k * Mathf.PI * 2) * 14f, 0); });
        }

        /// <summary>挂在中间的黑布卷轴：座次名 + 五颗星。掉的那颗星先亮着再碎成灰，旁边红箭头往下。</summary>
        static RectTransform BuildScroll(RectTransform c, Save.RankDelta rank, bool win, out Text stars)
        {
            var holder = Ui.Rect(c, "scroll");
            holder.anchoredPosition = new Vector2(0, 410);
            var sc = Ui.Icon(holder, "ui_scroll", 820, Vector2.zero);
            sc.rectTransform.sizeDelta = new Vector2(820, 300);
            var plate = Ui.Framed(holder, new Vector2(380, 70), new Vector2(0, 118), Pal.Hex("2B2622"), Ui.RankColor(Save.D.tier), 4f, "plate");
            Ui.Stroke(Ui.Label(plate.transform, Save.RankName, 44, Pal.Hex("FFE36A"), new Vector2(0, 2), new Vector2(380, 70), TextAnchor.MiddleCenter, Fonts.Brush), Edge, 2f);

            bool tierChanged = rank.Promoted || rank.Demoted;
            int cap = Mathf.Max(1, RankRules.StarsAt(Save.D.tier));
            int from = tierChanged ? (rank.Promoted ? 0 : cap) : rank.OldStars;
            int to = Save.D.stars;
            int tierNow = Save.D.tier;
            stars = Ui.Label(holder, Ui.RankStars(tierNow, from), cap >= 5 ? 104 : 116, Color.white, new Vector2(0, -20), new Vector2(720, 150));
            stars.supportRichText = true;
            Ui.Stroke(stars, Edge, 3f);
            var st = stars;
            if (from != to)
            {
                Tween.Run(st, 0.01f, k => { }, () =>
                {
                    if (!st) return;
                    st.text = Ui.RankStars(tierNow, to);
                    Tween.Punch(st.transform, 0.35f, 0.18f);
                    Sfx.Play(win ? Snd.Merge : Snd.Lose, 0.6f);
                }, 0.9f);
                int changed = win ? Mathf.Clamp(to - 1, 0, cap - 1) : Mathf.Clamp(from - 1, 0, cap - 1);
                float step = cap >= 5 ? 104 : 116;
                float sx = (changed - (cap - 1) * 0.5f) * step;
                if (win)
                {
                    var spark = Ui.Icon(holder, "ui_sparkle", 150, new Vector2(sx, -20), "spark");
                    spark.transform.localScale = Vector3.zero;
                    Tween.Run(spark, 0.6f, k => { if (spark) { spark.transform.localScale = Vector3.one * Mathf.Sin(k * Mathf.PI) * 1.2f; spark.transform.localRotation = Quaternion.Euler(0, 0, k * 120f); } }, null, 0.9f);
                }
                else
                {
                    var arrow = Ui.Label(holder, "↓", 90, Pal.Hex("E8392B"), new Vector2(sx + 50, 50), new Vector2(80, 110));
                    Ui.Stroke(arrow, Pal.Paper, 2f);
                    arrow.color = Color.clear;
                    var a0 = arrow.rectTransform.anchoredPosition;
                    Tween.Run(arrow, 0.8f, k =>
                    {
                        if (!arrow) return;
                        arrow.color = Pal.Hex("E8392B");
                        arrow.rectTransform.anchoredPosition = a0 + new Vector2(0, -Mathf.Repeat(k * 2f, 1f) * 26f);
                    }, null, 0.9f);
                }
            }
            return holder;
        }

        /// <summary>中间的草席小台：胜了宋江戴着幞头，周围一圈「舞」字蹦跳；输了宋江歪倒，被「兵」字围着。</summary>
        static RectTransform BuildStage(RectTransform c, bool win)
        {
            var holder = Ui.Rect(c, "stage");
            holder.anchoredPosition = new Vector2(0, 60);
            var mat = Ui.Icon(holder, "home_board", 600, new Vector2(0, -60));
            mat.rectTransform.sizeDelta = new Vector2(600, 320);
            if (!win) mat.color = Pal.Hex("B9C3C8");
            var song = Ui.Label(holder, "宋", 170, Pal.Ink, new Vector2(0, -10), new Vector2(220, 220), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Stroke(song, Pal.Paper, 2.5f);
            var hat = Ui.Icon(holder, "hat", 110, new Vector2(4, 120));
            if (win)
            {
                Hop(song.transform, 0f, 16f);
                Hop(hat.transform, 0.05f, 16f);
                var spots = new[] { new Vector2(-300, 90), new Vector2(300, 90), new Vector2(-210, -120), new Vector2(220, -120) };
                for (int i = 0; i < spots.Length; i++)
                {
                    var w = Ui.Label(holder, "舞", 110, Pal.Ink, spots[i], new Vector2(150, 150), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Stroke(w, Pal.Paper, 2f);
                    Dance(w.transform, i * 0.25f, i % 2 == 0 ? 1 : -1);
                    var ribbon = Ui.Box(holder, new Vector2(110, 10), spots[i] + new Vector2(i % 2 == 0 ? 70 : -70, 30), Pal.Hex("E05A8A"), Art.Stroke, "ribbon");
                    ribbon.type = Image.Type.Simple;
                    ribbon.transform.SetSiblingIndex(1);
                    Dance(ribbon.transform, i * 0.25f + 0.1f, i % 2 == 0 ? -1 : 1);
                }
            }
            else
            {
                song.color = Pal.InkSoft;
                song.rectTransform.localRotation = Quaternion.Euler(0, 0, -18);
                hat.rectTransform.anchoredPosition = new Vector2(120, -110);
                hat.rectTransform.localRotation = Quaternion.Euler(0, 0, 70);
                var spots = new[] { new Vector2(-280, 80), new Vector2(280, 80), new Vector2(-230, -130), new Vector2(240, -140), new Vector2(-60, 160) };
                for (int i = 0; i < spots.Length; i++)
                {
                    var b = Ui.Label(holder, "兵", 96, Pal.Ink, spots[i], new Vector2(130, 130), TextAnchor.MiddleCenter, Fonts.Brush);
                    Ui.Stroke(b, Pal.Paper, 2f);
                    Jitter(b.transform, i * 0.2f);
                }
                var sweat = Ui.Label(holder, "；", 60, Pal.Hex("5A8FC0"), new Vector2(100, 80), new Vector2(80, 80));
                Hop(sweat.transform, 0.3f, 8f);
            }
            return holder;
        }

        static void Wave(Transform t)
        {
            Tween.Run(t, 1.6f, k => { if (t) t.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 2) * 3f); }, () => { if (t) Wave(t); });
        }

        static void Hop(Transform t, float phase, float h)
        {
            var rt = (RectTransform)t;
            var p0 = rt.anchoredPosition;
            Tween.Run(t, 0.7f, k => { if (t) rt.anchoredPosition = p0 + new Vector2(0, Mathf.Abs(Mathf.Sin((k + phase) * Mathf.PI)) * h); },
                () => { if (t) { rt.anchoredPosition = p0; Hop(t, phase, h); } });
        }

        static void Dance(Transform t, float phase, int dir)
        {
            var rt = (RectTransform)t;
            var p0 = rt.anchoredPosition;
            Tween.Run(t, 0.9f, k =>
            {
                if (!t) return;
                float s = Mathf.Sin((k + phase) * Mathf.PI * 2);
                rt.anchoredPosition = p0 + new Vector2(s * 10f * dir, Mathf.Abs(s) * 22f);
                rt.localRotation = Quaternion.Euler(0, 0, s * 16f * dir);
            }, () => { if (t) { rt.anchoredPosition = p0; Dance(t, phase, dir); } });
        }

        static void Jitter(Transform t, float phase)
        {
            var rt = (RectTransform)t;
            var p0 = rt.anchoredPosition;
            Tween.Run(t, 0.5f, k =>
            {
                if (!t) return;
                float s = Mathf.Sin((k + phase) * Mathf.PI * 2);
                rt.anchoredPosition = p0 + new Vector2(s * 6f, 0);
                rt.localRotation = Quaternion.Euler(0, 0, s * 6f);
            }, () => { if (t) { rt.anchoredPosition = p0; Jitter(t, phase); } });
        }

        /// <summary>铜钱从中间往外蹦一圈再落回数字。</summary>
        static void CoinBurst(RectTransform c, Vector2 at, float delay)
        {
            for (int i = 0; i < 12; i++)
            {
                var coin = Ui.Icon(c, "coin", 64, at, "burst");
                coin.transform.localScale = Vector3.zero;
                float ang = i / 12f * Mathf.PI * 2f + 0.3f;
                var dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang) * 0.8f + 0.4f) * (180 + (i % 3) * 50);
                var cc = coin;
                Tween.Run(cc, 0.9f, k =>
                {
                    if (!cc) return;
                    float up = k < 0.45f ? Tween.OutCubic(k / 0.45f) : 1f - (k - 0.45f) / 0.55f;
                    cc.rectTransform.anchoredPosition = at + dir * up;
                    cc.transform.localScale = Vector3.one * (k < 0.85f ? 1f : (1f - k) / 0.15f);
                    cc.transform.localRotation = Quaternion.Euler(0, 0, k * 540f);
                }, () => { if (cc) Object.Destroy(cc.gameObject); }, delay + i * 0.02f);
            }
        }
    }
}
