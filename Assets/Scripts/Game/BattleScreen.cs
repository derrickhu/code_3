using System;
using System.Collections.Generic;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    public sealed class BattleResult
    {
        public int Chapter;
        public bool Win;
        public bool Quit;
        public int Wave, Kills, Rains;
        public List<HeroId> Formed = new List<HeroId>();
        public List<HeroId> NewHeroes = new List<HeroId>();
    }

    /// <summary>一局战斗：对局、对手 AI、界面浮层（横幅、引导、暂停、结算）。</summary>
    public sealed class BattleScreen : MonoBehaviour
    {
        enum Tut { Recruit, Place, Merge, Gold, Hero, Rain, Dismiss, Dig, Danger }

        public Match M { get; private set; }
        public BattleView View { get; private set; }
        public bool Autoplay;

        Action<BattleResult> _done;
        RivalAI _rival, _auto;
        RectTransform _overlay, _bannerLayer, _hint;
        Text _hintText;
        readonly Queue<Action> _banners = new Queue<Action>();
        float _bannerCd;
        bool _started, _ending;
        Tut? _tut;
        float _tutT;
        float _ringT;
        float _bossSay;
        Vector2 _ringPos;
        bool _tutorialRun;

        public static BattleScreen Open(Transform host, ChapterDef ch, Camera cam, Loadout items, bool autoplay, Action<BattleResult> done)
        {
            var go = new GameObject("Battle");
            go.transform.SetParent(host, false);
            var s = go.AddComponent<BattleScreen>();
            s.Autoplay = autoplay;
            s._done = done;
            s.Init(ch, cam, items);
            return s;
        }

        void Init(ChapterDef ch, Camera cam, Loadout items)
        {
            uint seed = (uint)(DateTime.Now.Ticks & 0x7fffffff);
            M = new Match(ch, seed, items);
            M.Hold = true;
            M.Rival.IncomeMul = Mathf.Min(1.05f, ch.RivalIncome + Save.D.tier * 0.03f);
            float skill = Mathf.Clamp01(ch.RivalSkill + Save.D.tier * 0.04f);
            _rival = new RivalAI(M.Rival, skill, seed + 99);
            if (Autoplay) _auto = new RivalAI(M.Player, 0.9f, seed + 7);

            View = gameObject.AddComponent<BattleView>();
            View.Build(M, cam);
            View.OnEvent += OnSimEvent;
            View.OnPauseClicked += ShowPause;
            View.OnPlaced += () => { if (_tut == Tut.Place) CompleteTut(); };
            if (Autoplay) { View.InputEnabled = false; View.Speed = 2f; }

            _overlay = Ui.Rect(Ui.Root, "BattleOverlay");
            _overlay.anchorMin = Vector2.zero;
            _overlay.anchorMax = Vector2.one;
            _overlay.offsetMin = _overlay.offsetMax = Vector2.zero;
            _bannerLayer = Ui.Rect(_overlay, "banners");
            BuildHint();

            _tutorialRun = ch.Index == 0 && !Save.Tut((int)Tut.Hero) && !Autoplay;
            if (Autoplay) Begin();
            else ShowIntro(ch, items);
        }

        void OnDestroy()
        {
            if (_overlay) Destroy(_overlay.gameObject);
        }

        void Begin()
        {
            _started = true;
            M.Hold = false;
            if (_tutorialRun && !Save.Tut((int)Tut.Recruit))
            {
                M.Hold = true;
                StartTut(Tut.Recruit);
            }
        }

        void Update()
        {
            if (!_started || M.Over) { UpdateBanners(); return; }
            if (!View.Paused && !M.Hold)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.1f) * View.Speed;
                _rival.Tick(dt);
                _auto?.Tick(dt);
            }
            UpdateTutorial();
            UpdateBanners();
        }

        // ─────────────────────────── 开场 ───────────────────────────

        void ShowIntro(ChapterDef ch, Loadout items)
        {
            var layer = Ui.Fill(_overlay, new Color(0.08f, 0.06f, 0.04f, 0.78f), true, "intro");
            var card = Ui.Box(layer, new Vector2(920, 1180), new Vector2(0, 40), Pal.Paper);
            Ui.Label(card.transform, "第" + BattleView.ToCn(ch.Index + 1) + "回", 44, Pal.InkSoft, new Vector2(0, 470), new Vector2(800, 70));
            Ui.Label(card.transform, ch.Name, 150, Pal.Ink, new Vector2(0, 340), new Vector2(860, 180), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Label(card.transform, ch.Story, 40, Pal.InkSoft, new Vector2(0, 150), new Vector2(780, 220), TextAnchor.MiddleLeft);

            var names = new List<string>();
            foreach (var id in ch.Pool) { var d = HeroDef.Get(id); if (!d.Hidden) names.Add(d.Name); }
            Ui.Label(card.transform, "本回好汉：" + string.Join("  ", names), 38, Pal.Gold, new Vector2(0, -10), new Vector2(800, 110));
            var nem = HeroDef.Get(ch.Boss.Nemesis).Name;
            Ui.Label(card.transform, $"头目：{ch.Boss.Title}{ch.Boss.Name}（{ch.Boss.SkillText}）\n克星：{nem}，对他伤害 ×5", 36, Pal.RedDeep,
                new Vector2(0, -150), new Vector2(800, 140));
            Ui.Label(card.transform, "对手：" + ch.RivalName + "（谁的宋江先倒谁输）", 34, Pal.InkSoft, new Vector2(0, -255), new Vector2(800, 60));
            if (items != null && ItemSummary(items).Length > 0)
                Ui.Label(card.transform, "随身：" + ItemSummary(items), 34, Pal.Willow, new Vector2(0, -320), new Vector2(800, 60));

            Ui.Btn(card.transform, "开  战", new Vector2(480, 140), new Vector2(0, -470), Pal.Red, () =>
            {
                Destroy(layer.gameObject);
                Begin();
                Banner("官兵来了！", "守住宋江，别让他们碰到哥哥", Pal.Red, 0.9f);
            }, 64, Pal.Paper, Fonts.Brush);
            Tween.Pop(card.transform, 0.4f);
        }

        public static string ItemSummary(Loadout l)
        {
            var parts = new List<string>();
            if (l.Gang) parts.Add("生辰纲");
            if (l.Flag) parts.Add("替天行道旗");
            if (l.Marsh) parts.Add("水泊");
            if (l.Stele) parts.Add("石碣");
            if (l.MengHan > 0) parts.Add("蒙汗药×" + l.MengHan);
            if (l.TianShu > 0) parts.Add("天书×" + l.TianShu);
            if (l.Baozi > 0) parts.Add("包子×" + l.Baozi);
            if (l.Xuming) parts.Add("续命丹");
            if (l.Shovels > 0) parts.Add("铲子×" + l.Shovels);
            if (l.Fu > 0) parts.Add("攻速符×" + l.Fu);
            if (l.Trap > 0) parts.Add("陷阱×" + l.Trap);
            if (l.Far) parts.Add("御敌千里");
            return string.Join("、", parts);
        }

        // ─────────────────────────── 事件 → 横幅 ───────────────────────────

        void OnSimEvent(SimEvent ev)
        {
            bool mine = ev.Side == 0;
            switch (ev.Type)
            {
                case Ev.HeroFormed when mine:
                {
                    var d = HeroDef.Get((HeroId)ev.B);
                    bool fresh = !Save.D.heroes.Contains((int)d.Id);
                    HeroBanner(d, fresh);
                    if (_tut == Tut.Gold || _tut == Tut.Hero) CompleteTut();
                    if (_tutorialRun) Save.SetTut((int)Tut.Hero);
                    break;
                }
                case Ev.Rain when mine:
                    Banner("及时雨！", $"宋江撒钱：炊饼 +{ev.Val:0}，快聚义反推！", Pal.Gold, 1f);
                    if (!Save.Tut((int)Tut.Rain)) { Save.SetTut((int)Tut.Rain); ShowHint("宋江每掉一颗心就会撒钱（及时雨）。\n赶紧聚义，把官兵打回去！", 4f); }
                    break;
                case Ev.BossSpawn:
                {
                    var nem = HeroDef.Get(M.Chapter.Boss.Nemesis).Name;
                    Banner(M.Chapter.Boss.Name + " 来了！", $"{M.Chapter.Boss.SkillText} · 克星：{nem}", Pal.RedDeep, 1.1f);
                    break;
                }
                case Ev.EliteWarn:
                    Banner("捕头来袭", "戴面具的捕头血厚，集中火力", Pal.Hex("3B2050"), 0.8f);
                    break;
                case Ev.ItemUsed when mine:
                {
                    string sub = ev.Flag == 3 ? "十二秒内全军出手快四成" : ev.Flag == 4 ? "官兵跌进去，一时爬不上来" : "";
                    Banner(ev.Text + "！", sub, ev.Flag == 3 ? Pal.Gold : Pal.Willow, 0.9f);
                    break;
                }
                case Ev.Ult when ev.Flag == 1:
                    if (Time.unscaledTime < _bossSay) break;
                    _bossSay = Time.unscaledTime + 7f;
                    Banner(ev.Text, M.Chapter.Boss.SkillText, Pal.RedDeep, 0.95f);
                    break;
                case Ev.Shovels when mine && ev.Val == 1:
                    Banner("方便铲！", "鲁智深送来一把铲子，拖到草地上开荒", Pal.Willow, 0.8f);
                    break;
                case Ev.Rescue when mine:
                    Banner("宋江哥哥高枕无忧了！", "公孙胜作法：官兵清场，十二秒内宋江不掉血", Pal.Red, 1.2f);
                    break;
                case Ev.Dig when mine && !Save.Tut((int)Tut.Dig):
                    Save.SetTut((int)Tut.Dig);
                    ShowHint("开出来的白地才能摆字。\n招兵偶尔会掉铲子，上方铲子按钮看广告也能领。", 5f);
                    break;
                case Ev.Merge when mine:
                    if (_tut == Tut.Merge) CompleteTut();
                    break;
                case Ev.Recruit when mine:
                    if (_tut == Tut.Recruit) { CompleteTut(); StartTut(Tut.Place); }
                    break;
                case Ev.End:
                    OnEnd(ev.A == 0);
                    break;
            }
        }

        void HeroBanner(HeroDef d, bool fresh)
        {
            Enqueue(() =>
            {
                var layer = Ui.Rect(_bannerLayer, "heroBanner");
                var g = layer.gameObject.AddComponent<CanvasGroup>();
                g.blocksRaycasts = false;
                Ui.Box(layer, new Vector2(1250, 880), new Vector2(0, 180), Pal.Ink.A(0.9f), Art.Blob);
                Ui.Label(layer, d.Nick, 52, Pal.GoldBright, new Vector2(0, 390), new Vector2(900, 80));
                var name = Ui.Label(layer, d.Name, 230, Pal.Paper, new Vector2(0, 230), new Vector2(1000, 280), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Label(layer, d.Skill, 40, Pal.Paper.A(0.9f), new Vector2(0, 70), new Vector2(900, 70));
                var seal = Ui.Box(layer, new Vector2(150, 150), new Vector2(330, 320), Pal.Red);
                Ui.Label(seal.transform, "替天\n行道", 44, Pal.Paper, Vector2.zero, new Vector2(150, 150), TextAnchor.MiddleCenter, Fonts.Brush);
                seal.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
                if (fresh) Ui.Label(layer, "— 新好汉入谱 —", 40, Pal.GoldBright, new Vector2(0, 5), new Vector2(900, 60));
                Ui.Label(layer, "「" + d.Quote + "」", 36, Pal.Paper.A(0.75f), new Vector2(0, -50), new Vector2(900, 60));

                Tween.Pop(name.transform, 0.35f);
                seal.transform.localScale = Vector3.one * 3f;
                Tween.Run(seal, 0.25f, k => seal.transform.localScale = Vector3.one * Mathf.Lerp(3f, 1f, k * k), () => View.Shake(0.5f), 0.3f);
                Tween.FadeGroup(g, 0, 1, 0.15f);
                Tween.FadeGroup(g, 1, 0, 0.35f, () => Destroy(layer.gameObject), 1.9f);
            }, 2.0f);
        }

        void Banner(string title, string sub, Color color, float hold)
        {
            Enqueue(() =>
            {
                var layer = Ui.Rect(_bannerLayer, "banner");
                layer.anchoredPosition = new Vector2(0, 260);
                var g = layer.gameObject.AddComponent<CanvasGroup>();
                g.blocksRaycasts = false;
                Ui.Box(layer, new Vector2(980, 280), Vector2.zero, Pal.Paper);
                int size = Mathf.Min(120, 900 / Mathf.Max(1, title.Length));
                var t = Ui.Label(layer, title, size, color, new Vector2(0, 30), new Vector2(1000, 160), TextAnchor.MiddleCenter, Fonts.Brush);
                if (!string.IsNullOrEmpty(sub)) Ui.Label(layer, sub, 38, Pal.Ink, new Vector2(0, -80), new Vector2(980, 60));
                Tween.Pop(t.transform, 0.3f);
                Tween.FadeGroup(g, 0, 1, 0.12f);
                Tween.FadeGroup(g, 1, 0, 0.3f, () => Destroy(layer.gameObject), hold);
            }, hold * 0.8f + 0.2f);
        }

        void Enqueue(Action show, float duration)
        {
            if (_banners.Count > 3) _banners.Dequeue();
            _banners.Enqueue(() => { show(); _bannerCd = duration; });
        }

        void UpdateBanners()
        {
            _bannerCd -= Time.unscaledDeltaTime;
            if (_bannerCd <= 0 && _banners.Count > 0) _banners.Dequeue()();
        }

        // ─────────────────────────── 新手引导 ───────────────────────────

        void BuildHint()
        {
            _hint = Ui.Rect(_overlay, "hint");
            Ui.Anchor(_hint, 0.5f, 0.5f);
            _hint.anchoredPosition = new Vector2(0, -60);
            Ui.Box(_hint, new Vector2(940, 210), Vector2.zero, Pal.Ink.A(0.9f));
            Ui.Box(_hint, new Vector2(110, 110), new Vector2(-400, 0), Pal.Red);
            Ui.Label(_hint, "吴用", 40, Pal.Paper, new Vector2(-400, 0), new Vector2(110, 110), TextAnchor.MiddleCenter, Fonts.Brush);
            _hintText = Ui.Label(_hint, "", 38, Pal.Paper, new Vector2(60, 0), new Vector2(780, 200), TextAnchor.MiddleLeft);
            _hint.gameObject.SetActive(false);
        }

        void ShowHint(string text, float dur)
        {
            _hintText.text = text;
            _hint.gameObject.SetActive(true);
            Tween.Pop(_hint, 0.25f);
            _tutT = dur;
        }

        void StartTut(Tut t)
        {
            _tut = t;
            switch (t)
            {
                case Tut.Recruit:
                    ShowHint("军师吴用：哥哥被押在路尽头了！\n点下面的「聚义」，花炊饼招好汉。", 999);
                    _ringPos = View.RecruitButtonPos;
                    break;
                case Tut.Place:
                    ShowHint("把山寨里的字拖到路边的白地上，\n他们会自动打路上的官兵。", 999);
                    _ringPos = BattleView.CampWorld(0);
                    break;
                case Tut.Merge:
                    ShowHint("两个一样、同级的字拖到一起，\n就能合成升级，更猛！", 7f);
                    break;
                case Tut.Gold:
                {
                    var names = new List<string>();
                    foreach (var h in M.Player.Pool) if (!h.Hidden) names.Add(h.Name);
                    ShowHint("金字是好汉的名字！按顺序摆在一起\n（左→右或上→下）就能召唤：" + string.Join("、", names), 9f);
                    break;
                }
            }
        }

        void CompleteTut()
        {
            if (_tut == null) return;
            Save.SetTut((int)_tut.Value);
            if (_tut == Tut.Place) M.Hold = false;
            _tut = null;
            _hint.gameObject.SetActive(false);
        }

        void UpdateTutorial()
        {
            if (_tut != null)
            {
                _tutT -= Time.deltaTime;
                if (_tutT <= 0) CompleteTut();
                if (_tut == Tut.Recruit || _tut == Tut.Place)
                {
                    _ringT -= Time.deltaTime;
                    if (_ringT <= 0)
                    {
                        _ringT = 0.9f;
                        View.FloatText(_ringPos + new Vector2(0, 0.75f), "点这", Pal.Red, 0.4f, 0.8f);
                        if (_tut == Tut.Place && View.TryFindEmptyCell(out var cell)) View.FloatText(cell, "放这", Pal.Red, 0.32f, 0.8f);
                    }
                }
            }
            else if (_hint.gameObject.activeSelf)
            {
                _tutT -= Time.deltaTime;
                if (_tutT <= 0) _hint.gameObject.SetActive(false);
            }

            if (_tut != null || M.Hold || Autoplay) return;
            var p = M.Player;
            if (!Save.Tut((int)Tut.Danger) && p.InDanger && !p.RescueUsed)
            {
                Save.SetTut((int)Tut.Danger);
                ShowHint("宋江危险！点山寨右边的「救」，\n看段广告请公孙胜作法清场护身。", 4f);
                return;
            }
            if (!Save.Tut((int)Tut.Dismiss) && Save.Tut((int)Tut.Place) && p.EmptyCampSlots == 0 && !p.CanRecruit && p.Buns >= p.RecruitCost)
            {
                Save.SetTut((int)Tut.Dismiss);
                ShowHint("山寨满了招不了人？把没用的字\n拖到「聚义」按钮上遣散，还能退炊饼。", 5f);
                return;
            }
            if (!_tutorialRun) return;
            if (!Save.Tut((int)Tut.Merge) && HasMergePair(p)) StartTut(Tut.Merge);
            else if (!Save.Tut((int)Tut.Gold) && HasGoldInCamp(p)) StartTut(Tut.Gold);
        }

        static bool HasMergePair(Side p)
        {
            var all = new List<Piece>();
            foreach (var c in p.Camp) if (c != null) all.Add(c);
            foreach (var c in p.Board.Pieces) if (c != null) all.Add(c);
            for (int i = 0; i < all.Count; i++)
                for (int j = i + 1; j < all.Count; j++)
                    if (all[i].Type == PieceType.Unit && Side.CanMerge(all[i], all[j])) return true;
            return false;
        }

        static bool HasGoldInCamp(Side p)
        {
            foreach (var c in p.Camp) if (c != null && c.Type == PieceType.HeroChar) return true;
            return false;
        }

        // ─────────────────────────── 暂停 / 结束 ───────────────────────────

        void ShowPause()
        {
            if (M.Over || !_started) return;
            View.Paused = true;
            var layer = Ui.Fill(_overlay, new Color(0, 0, 0, 0.7f), true, "pause");
            var card = Ui.Box(layer, new Vector2(760, 720), Vector2.zero, Pal.Paper);
            Ui.Label(card.transform, "歇 口 气", 110, Pal.Ink, new Vector2(0, 230), new Vector2(700, 150), TextAnchor.MiddleCenter, Fonts.Brush);
            Ui.Btn(card.transform, "继续厮杀", new Vector2(480, 120), new Vector2(0, 60), Pal.Red, () => { View.Paused = false; Destroy(layer.gameObject); }, 50, Pal.Paper);
            Button sfx = null;
            sfx = Ui.Btn(card.transform, Sfx.On ? "音效：开" : "音效：关", new Vector2(480, 110), new Vector2(0, -90), Pal.InkSoft, () =>
            {
                Sfx.On = !Sfx.On;
                Save.D.sfx = Sfx.On;
                Save.Write();
                Ui.SetText(sfx, Sfx.On ? "音效：开" : "音效：关");
            }, 44, Pal.Paper);
            Ui.Btn(card.transform, "撤回山寨（算输）", new Vector2(480, 110), new Vector2(0, -230), Pal.PaperDark, () =>
            {
                Destroy(layer.gameObject);
                View.Paused = false;
                M.Over = true;
                M.Winner = 1;
                OnEnd(false, true);
            }, 40, Pal.Ink);
            Tween.Pop(card.transform, 0.25f);
        }

        /// <summary>验收截图用：直接判定结局。</summary>
        public void ForceEnd(bool win)
        {
            if (M.Over) return;
            M.Over = true;
            M.Winner = win ? 0 : 1;
            OnEnd(win);
        }

        void OnEnd(bool win, bool quit = false)
        {
            if (_ending) return;
            _ending = true;
            _hint.gameObject.SetActive(false);
            Sfx.Play(win ? Snd.Win : Snd.Lose);
            var r = new BattleResult
            {
                Chapter = M.Chapter.Index, Win = win, Quit = quit, Wave = M.Wave, Kills = M.Player.Kills, Rains = M.Player.Rains,
            };
            foreach (var id in M.Player.FormedEver)
            {
                r.Formed.Add(id);
                if (!Save.D.heroes.Contains((int)id)) r.NewHeroes.Add(id);
            }
            Banner(win ? "对手的宋江倒了！" : "宋江被押走了", win ? "这一阵，赢了" : "再来！", win ? Pal.Gold : Pal.Ink, 1.2f);
            Tween.Run(this, 1.6f, k => { }, () => _done?.Invoke(r));
        }
    }
}
