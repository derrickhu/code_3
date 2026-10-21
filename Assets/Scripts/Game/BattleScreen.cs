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
        /// <summary>聚义厅第几阵；0 是剧情回。</summary>
        public int Round;
        public HeroId[] Pool;
        public bool Win;
        public bool Quit;
        public int Wave, Kills, Rains;
        /// <summary>GM 跳过时一定缴获兵器，方便看结算掉落。</summary>
        public bool GmDrop;
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
        RectTransform _overlay, _bannerLayer, _hint, _intro;
        Text _hintText;
        CanvasGroup _hintGroup;
        bool _hintWant;
        readonly Queue<Action> _banners = new Queue<Action>();
        float _bannerCd;
        bool _started, _ending, _hot;
        float _calm;
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
            M.Rival.IncomeMul = ch.RivalIncome + Mathf.Min(0.1f, Save.Major * 0.012f);
            float skill = Mathf.Clamp01(ch.RivalSkill + Save.Major * 0.015f);
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
            BuildBattleGm();
        }

        bool _gmDrop;

        /// <summary>右上角 GM。开场盖住棋盘时也在最上层，点了就能结束这一局。</summary>
        void BuildBattleGm()
        {
            if (!Gm.Allowed) return;
            var btn = Ui.CartoonBtn(_overlay, "GM", new Vector2(112, 60), Vector2.zero, Ui.BtnRed, ShowBattleGm, 34);
            Ui.Anchor(btn, 1f, 1f);
            btn.anchoredPosition = new Vector2(-80, -50);
            btn.SetAsLastSibling();
        }

        public void ShowBattleGm()
        {
            if (M.Over || _ending) return;
            View.Paused = true;
            var layer = Ui.Fill(_overlay, new Color(0, 0, 0, 0.72f), true, "gm");
            var card = Ui.WoodPanel(layer, new Vector2(820, 760), new Vector2(0, 40));
            Ui.Title(card, "GM", new Vector2(0, 360));
            Ui.CloseX(card, new Vector2(-404, 366), () =>
            {
                View.Paused = false;
                Destroy(layer.gameObject);
            });
            Ui.Label(card, "直接结束这一局，走真正的结算。\n胜利按撑过四波算，并一定缴获一件兵器。", 32, Pal.InkSoft,
                new Vector2(0, 180), new Vector2(720, 110));
            Ui.CartoonBtn(card, "胜利结算", new Vector2(560, 120), new Vector2(0, -10), Ui.BtnRed, () =>
            {
                Destroy(layer.gameObject);
                GmFinish(true);
            }, 48);
            Ui.CartoonBtn(card, "失败结算", new Vector2(560, 120), new Vector2(0, -170), Ui.BtnDark, () =>
            {
                Destroy(layer.gameObject);
                GmFinish(false);
            }, 48);
            layer.SetAsLastSibling();
            Tween.Pop(card, 0.25f);
        }

        public void GmFinish(bool win)
        {
            if (M.Over || _ending) return;
            if (_intro) Destroy(_intro.gameObject);
            View.Paused = false;
            if (win)
            {
                if (M.Wave < 4) M.Wave = 4;
                _gmDrop = true;
            }
            else if (M.Wave < 1) M.Wave = 1;
            M.Over = true;
            M.Winner = win ? 0 : 1;
            OnEnd(win);
        }

        void OnDestroy()
        {
            if (_overlay) Destroy(_overlay.gameObject);
        }

        void Begin()
        {
            PlayCalm();
            _started = true;
            M.Hold = false;
            Analytics.LevelStart(M.Chapter.Index + 1, M.Chapter.Name);
            if (_tutorialRun && !Save.Tut((int)Tut.Recruit))
            {
                M.Hold = true;
                StartTut(Tut.Recruit);
            }
        }

        void Update()
        {
            TickHint();
            if (!_started || M.Over || _ending) { UpdateBanners(); return; }
            if (!View.Paused && !M.Hold)
            {
                float dt = Mathf.Min(Time.deltaTime, 0.1f) * View.Speed;
                _rival.Tick(dt);
                _auto?.Tick(dt);
            }
            UpdateTutorial();
            UpdateBanners();
            UpdateMusic();
        }

        /// <summary>头目还在场，或宋江只剩最后一颗心、官兵摸到跟前，就换成「大闹」。险情过去四秒再回到「聚义」，避免来回切。</summary>
        void UpdateMusic()
        {
            bool hot = M.Player.InDanger || BossAlive();
            if (hot) { _hot = true; _calm = 0f; }
            else if (_hot) _calm += Time.unscaledDeltaTime;
            if (hot || (_hot && _calm < 4f)) Sfx.PlayCrisis();
            else { _hot = false; PlayCalm(); }
        }

        /// <summary>聚义厅车轮战和终章「蓼儿洼」平时放「百八聚义」，其余各回放「聚义」。</summary>
        void PlayCalm()
        {
            if (Epic(M.Chapter)) Sfx.PlayEpic();
            else Sfx.PlayBattle();
        }

        public static bool Epic(ChapterDef ch) => ch.IsEndless || ch.Index == ChapterDef.StoryCount - 1;

        bool BossAlive()
        {
            foreach (var s in new[] { M.Player, M.Rival })
                foreach (var e in s.Enemies)
                    if (!e.Dead && e.Def.Kind == EnemyKind.Boss) return true;
            return false;
        }

        // ─────────────────────────── 开场 ───────────────────────────

        void ShowIntro(ChapterDef ch, Loadout items)
        {
            var layer = Ui.Fill(_overlay, new Color(0.08f, 0.06f, 0.04f, 0.78f), true, "intro");
            _intro = layer;
            var card = Ui.WoodPanel(Ui.Fit(layer, 1420), new Vector2(940, 1240), new Vector2(0, 40), Pal.Hex("8C6B4E"), Pal.Paper);
            string head = ch.IsEndless ? "聚义厅 · 车轮战" : $"第{BattleView.ToCn(ch.Index + 1)}回 · {ch.Act}";
            Ui.Label(card.transform, head, 42, Pal.InkSoft, new Vector2(0, 500), new Vector2(800, 64));
            Ui.Label(card.transform, ch.Name, 140, Pal.Ink, new Vector2(0, 385), new Vector2(860, 170), TextAnchor.MiddleCenter, Fonts.Brush);
            if (!string.IsNullOrEmpty(ch.Hui))
                Ui.Label(card.transform, "《水浒传》" + ch.Hui, 32, Pal.Hex("9A6A1E"), new Vector2(0, 285), new Vector2(860, 50));
            Ui.Label(card.transform, ch.Story, 38, Pal.InkSoft, new Vector2(0, 160), new Vector2(800, 200), TextAnchor.MiddleLeft);

            var names = new List<string>();
            foreach (var id in ch.Pool) { var d = HeroDef.Get(id); if (!d.Hidden) names.Add(d.Name); }
            Ui.Label(card.transform, "本回好汉：" + string.Join("  ", names), 38, Pal.Gold, new Vector2(0, 20), new Vector2(820, 70));
            if (ch.Joins.Length > 0)
            {
                var j = new List<string>();
                foreach (var id in ch.Joins) j.Add(HeroDef.Get(id).Nick + HeroDef.Get(id).Name);
                bool done = Save.Joined(ch.Joins[0]);
                Ui.Label(card.transform, (done ? "已入伙：" : "胜后入伙：") + string.Join("、", j), 34, done ? Pal.InkSoft : Pal.Red,
                    new Vector2(0, -55), new Vector2(840, 90));
            }
            var nem = HeroDef.Get(ch.Boss.Nemesis).Name;
            Ui.Label(card.transform, $"头目：{ch.Boss.Title}{ch.Boss.Name}（{ch.Boss.SkillText}）\n克星：{nem}，对他伤害 ×5", 34, Pal.RedDeep,
                new Vector2(0, -170), new Vector2(820, 120));
            Ui.Label(card.transform, "对手：" + ch.RivalName + "（谁的宋江先倒谁输）", 32, Pal.InkSoft, new Vector2(0, -260), new Vector2(800, 56));
            if (items != null && ItemSummary(items).Length > 0)
                Ui.Label(card.transform, "随身：" + ItemSummary(items), 32, Pal.Willow, new Vector2(0, -320), new Vector2(800, 56));

            Ui.CartoonBtn(card.transform, "开  战", new Vector2(480, 140), new Vector2(0, -470), Ui.BtnRed, () =>
            {
                Destroy(layer.gameObject);
                Begin();
                Banner("官军探马", "看清来路，趁官兵未到先征兵布阵", Pal.Gold, 1.1f);
            }, 68);
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
                case Ev.WaveStart when ev.A == 1:
                    Sfx.Play(Snd.Warn, 0.7f);
                    Banner("官兵来了！", "守住宋江，别让他们碰到哥哥", Pal.Red, 0.9f);
                    break;
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
                case Ev.Bond when mine:
                {
                    Bond b = null;
                    foreach (var x in Bond.All) if (x.Name == ev.Text) b = x;
                    Sfx.Play(Snd.Join, 0.8f);
                    Banner("羁绊 · " + ev.Text, (b != null ? b.Story + "　" : "") + $"伤害 +{Mathf.RoundToInt(ev.Val * 100)}%", Pal.Hex("B5651D"), 1.1f);
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
                case Ev.Ult when ev.Flag == 1 && mine:
                {
                    if (Time.unscaledTime < _bossSay) break;
                    _bossSay = Time.unscaledTime + 3f;
                    var skill = (BossSkill)ev.B;
                    Banner(M.Chapter.Boss.Name + "·" + BossDef.SkillName(skill), BossDef.SkillDesc(skill, M.Chapter.Boss.Nemesis), Pal.RedDeep, 0.8f);
                    break;
                }
                case Ev.BossRage when mine:
                    _bossSay = Time.unscaledTime + 2f;
                    Banner(ev.Text + " 狂暴了！", "血量过半：脚步更快，绝技放得更勤", Pal.Red, 0.9f);
                    break;
                case Ev.Shovels when mine && ev.Val == 1:
                    Banner("方便铲！", "鲁智深送来一把铲子，拖到草地上开荒", Pal.Willow, 0.8f);
                    break;
                case Ev.Rescue when mine:
                    Banner("宋江哥哥有惊无险！", "公孙胜作法，把官兵都推回路口", Pal.Red, 1.2f);
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
                Ui.Label(layer, (d.Rank > 0 ? d.Star + " · " : "") + d.Nick, 52, Pal.GoldBright, new Vector2(0, 390), new Vector2(900, 80));
                var name = Ui.Label(layer, d.Name, 230, Pal.Paper, new Vector2(0, 230), new Vector2(1000, 280), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Label(layer, d.Skill, 40, Pal.Paper.A(0.9f), new Vector2(0, 70), new Vector2(900, 70));
                var seal = Ui.Box(layer, new Vector2(150, 150), new Vector2(330, 320), Pal.Red);
                Ui.Label(seal.transform, "替天\n行道", 44, Pal.Paper, Vector2.zero, new Vector2(150, 150), TextAnchor.MiddleCenter, Fonts.Brush);
                seal.rectTransform.localRotation = Quaternion.Euler(0, 0, -12);
                if (fresh) Ui.Label(layer, "— 初次聚义 —", 40, Pal.GoldBright, new Vector2(0, 5), new Vector2(900, 60));
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
                Ui.Box(layer, new Vector2(1000, 296), new Vector2(0, -12), Pal.Shadow.A(0.5f), null, "shadow");
                Ui.Framed(layer, new Vector2(980, 280), Vector2.zero, Pal.Hex("F6EEDA"), Ui.Edge, 6f, "frame");
                Ui.Box(layer, new Vector2(18, 220), new Vector2(-470, 0), color, null, "ribbonL").pixelsPerUnitMultiplier = 1f;
                Ui.Box(layer, new Vector2(18, 220), new Vector2(470, 0), color, null, "ribbonR").pixelsPerUnitMultiplier = 1f;
                int size = Mathf.Min(120, 900 / Mathf.Max(1, title.Length));
                var t = Ui.Label(layer, title, size, color, new Vector2(0, 30), new Vector2(1000, 160), TextAnchor.MiddleCenter, Fonts.Brush);
                Ui.Stroke(t, Pal.Hex("FFF6E2"), 2.5f);
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

        /// <summary>
        /// 军师小贴士：贴在对手半区顶上（自己的棋盘一格不挡），整块不吃触摸，底下照常拖字；
        /// 点一下贴士就收起，到时也自己淡出。分步引导的「点这/放这」不受影响。
        /// </summary>
        void BuildHint()
        {
            _hint = Ui.Rect(_overlay, "hint");
            Ui.Anchor(_hint, 0.5f, 0.5f);
            _hint.sizeDelta = new Vector2(HintW, HintH);
            _hintGroup = _hint.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.blocksRaycasts = false;
            _hintGroup.interactable = false;
            Ui.Box(_hint, new Vector2(HintW, HintH), Vector2.zero, Pal.Ink.A(0.86f));
            Ui.Box(_hint, new Vector2(96, 96), new Vector2(-HintW * 0.5f + 72, 6), Pal.Red);
            Ui.Label(_hint, "吴用", 34, Pal.Paper, new Vector2(-HintW * 0.5f + 72, 6), new Vector2(96, 96), TextAnchor.MiddleCenter, Fonts.Brush);
            _hintText = Ui.Label(_hint, "", 37, Pal.Paper, new Vector2(62, 8), new Vector2(HintW - 170, HintH - 30), TextAnchor.MiddleLeft);
            Ui.Label(_hint, "轻点收起", 25, Pal.Paper.A(0.55f), new Vector2(HintW * 0.5f - 110, -HintH * 0.5f + 26), new Vector2(170, 32), TextAnchor.MiddleRight);
            _hint.gameObject.SetActive(false);
        }

        const float HintW = 1000f, HintH = 196f;

        void ShowHint(string text, float dur)
        {
            _hintText.text = text;
            _hint.gameObject.SetActive(true);
            _hintWant = true;
            _hintGroup.alpha = 0f;
            PlaceHint();
            Tween.Pop(_hint, 0.25f);
            _tutT = dur;
        }

        public void Coach(string text, float dur) => ShowHint(text, dur);

        void HideHint(bool now = false)
        {
            _hintWant = false;
            if (now) _hint.gameObject.SetActive(false);
        }

        void PlaceHint()
        {
            var screen = View.HintScreenTop;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, null, out var local))
                _hint.anchoredPosition = new Vector2(0f, local.y - HintH * 0.5f);
        }

        void TickHint()
        {
            if (!_hint.gameObject.activeSelf) return;
            PlaceHint();
            if (_hintWant && Input.GetMouseButtonDown(0)
                && RectTransformUtility.RectangleContainsScreenPoint(_hint, Input.mousePosition, null))
                _hintWant = false;
            _hintGroup.alpha = Mathf.MoveTowards(_hintGroup.alpha, _hintWant ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (!_hintWant && _hintGroup.alpha <= 0f) _hint.gameObject.SetActive(false);
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
                    ShowHint("两个一样、同级的兵拖到一起，\n就能合成升级，更猛！", 7f);
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
            HideHint();
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
                if (_tutT <= 0) HideHint();
            }

            if (_tut != null || M.Hold || Autoplay) return;
            var p = M.Player;
            if (!Save.Tut((int)Tut.Danger) && p.AtTheGate && !p.RescueUsed)
            {
                Save.SetTut((int)Tut.Danger);
                ShowHint("宋江危险！点山寨右边的「救」，\n看段广告把官兵推回路口。", 4f);
                return;
            }
            if (!Save.Tut((int)Tut.Dismiss) && Save.Tut((int)Tut.Place) && p.EmptyCampSlots == 0 && p.Buns >= p.RecruitCost)
            {
                Save.SetTut((int)Tut.Dismiss);
                ShowHint("再点聚义，底下这一排全部换新。\n不想花炊饼，就把字拖到按钮上遣散。", 5f);
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
            var layer = Ui.Fill(_overlay, new Color(0, 0, 0, 0.62f), true, "pause");
            var card = Ui.WoodPanel(Ui.Fit(layer, 1200), new Vector2(840, 920), new Vector2(0, 80), Pal.Hex("8C6B4E"), Pal.Hex("F3E6C6"));
            Ui.Title(card, "暂停", new Vector2(0, 450), 90);
            var gear = Ui.Icon(card, "ui_gear", 96, new Vector2(330, 370));
            var gearState = Ui.Label(card, Sfx.On ? "声音开" : "声音关", 26, Pal.InkSoft, new Vector2(330, 305), new Vector2(160, 40));
            gearState.fontStyle = FontStyle.Bold;
            Ui.Tap(gear, () =>
            {
                Sfx.On = !Sfx.On;
                Save.D.sfx = Sfx.On;
                Save.Write();
                if (Sfx.On) Sfx.Play(Snd.Tap);
                gearState.text = Sfx.On ? "声音开" : "声音关";
            });

            string mine = ItemSummary(M.Player.Items);
            int winCoins = ResultPanel.WinCoins(M.Chapter);
            SpoilBox(card, new Vector2(0, 205), "对手战利品：", mine.Length > 0 ? "你身上的：" + mine : "无");
            SpoilBox(card, new Vector2(0, -45), "战利品：", $"铜钱 ×{winCoins}　兵器（撑过四波有机会）");

            Ui.CartoonBtn(card, "耻辱撤退", new Vector2(340, 120), new Vector2(-190, -240), Ui.BtnBlue, () =>
            {
                Destroy(layer.gameObject);
                View.Paused = false;
                M.Over = true;
                M.Winner = 1;
                OnEnd(false, true);
            }, 54);
            Ui.Label(card, "投降没有战利品", 28, Pal.InkSoft, new Vector2(-190, -330), new Vector2(340, 44));
            Ui.CartoonBtn(card, "死战不休", new Vector2(340, 120), new Vector2(190, -240), Ui.BtnRed, () => { View.Paused = false; Destroy(layer.gameObject); }, 54);
            Ui.Label(card, "胜利者拿走所有战利品", 28, Pal.InkSoft, new Vector2(190, -330), new Vector2(360, 44));
            Tween.Pop(card, 0.25f);
        }

        static void SpoilBox(RectTransform card, Vector2 pos, string title, string body)
        {
            var box = Ui.Box(card, new Vector2(720, 210), pos, Pal.Hex("E2D2AE"), null, "spoil");
            box.pixelsPerUnitMultiplier = 1f;
            Ui.Stroke(Ui.Label(card, title, 40, Pal.Hex("D9822B"), pos + new Vector2(0, 62), new Vector2(700, 56), TextAnchor.MiddleCenter, Fonts.Brush), Pal.Hex("FFF6E2"), 2f);
            Ui.Label(card, body, 34, Pal.Ink, pos + new Vector2(0, -24), new Vector2(680, 110));
        }

        /// <summary>验收截图用：跳过开场，塞一张攻速符和一个陷阱，立刻用掉攻速符。</summary>
        public void ShotItem()
        {
            if (_intro) Destroy(_intro.gameObject);
            if (!_started) Begin();
            M.Player.Items.Fu = 1;
            M.Player.Items.Trap = 1;
            M.Player.Items.MengHan = 2;
            M.Player.UseFu();
        }

        /// <summary>验收截图用：跳过开场直接打开暂停面板。</summary>
        public void ShotPause()
        {
            if (_intro) Destroy(_intro.gameObject);
            if (!_started) Begin();
            ShowPause();
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
            HideHint(true);
            if (win) Sfx.PlayVictory();
            else Sfx.PlayDefeat();
            var r = new BattleResult
            {
                Chapter = M.Chapter.Index, Round = M.Chapter.Round, Pool = M.Chapter.Pool, Win = win, Quit = quit, Wave = M.Wave, Kills = M.Player.Kills, Rains = M.Player.Rains,
                GmDrop = _gmDrop,
            };
            foreach (var id in M.Player.FormedEver)
            {
                r.Formed.Add(id);
                if (!Save.D.heroes.Contains((int)id)) r.NewHeroes.Add(id);
            }
            if (quit) Analytics.LevelFail("quit", M.Wave, M.Player.Kills);
            else if (win) Analytics.LevelClear(M.Wave, M.Player.Kills);
            else Analytics.LevelFail("lose", M.Wave, M.Player.Kills);
            Banner(win ? "对手的宋江倒了！" : "宋江被押走了", win ? "这一阵，赢了" : "再来！", win ? Pal.Gold : Pal.Ink, 1.2f);
            Tween.Run(this, 1.6f, k => { }, () => _done?.Invoke(r));
        }
    }
}
