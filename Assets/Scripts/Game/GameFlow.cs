using System;
using System.Collections;
using System.IO;
using Rescue.Sim;
using UnityEngine;
using UnityEngine.UI;

namespace Rescue
{
    /// <summary>启动入口：相机、存档、界面切换。命令行 -shots &lt;dir&gt; 进入自动截图模式。</summary>
    public sealed class GameFlow : MonoBehaviour
    {
        Camera _cam;
        Camera _backdrop;
        HomeScreen _home;
        BattleScreen _battle;
        string _shots;
        int _shotIndex;
        float _aspect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindObjectOfType<GameFlow>() != null) return;
            new GameObject("Game").AddComponent<GameFlow>();
        }

        void Awake()
        {
            DontDestroyOnLoad(gameObject);
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = false;
            _shots = Arg("-shots");
            if (_shots != null) Sfx.Music = false;
            Save.Load();
            if (_shots != null)
            {
                Save.Volatile = true;
                Save.D.tutorial = 0;
            }
            Tween.Init(gameObject);
            Sfx.Init(gameObject);

            _cam = Camera.main;
            if (_cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                _cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            _cam.orthographic = true;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.backgroundColor = Pal.Paper;
            _cam.transform.position = new Vector3(0, -0.1f, -10);
            _cam.nearClipPlane = 0.1f;
            _cam.farClipPlane = 50f;

            var back = new GameObject("Backdrop");
            back.transform.SetParent(transform, false);
            _backdrop = back.AddComponent<Camera>();
            _backdrop.orthographic = true;
            _backdrop.cullingMask = 0;
            _backdrop.clearFlags = CameraClearFlags.SolidColor;
            _backdrop.backgroundColor = Pal.Paper;
            _backdrop.depth = _cam.depth - 1;
            FitCamera();

            Ui.Init(transform);
            if (_shots != null) StartCoroutine(Arg("-ui") != null ? UiShots() : Arg("-fx") != null ? FxShots() : Arg("-range") != null ? RangeShots() : AutoShots());
            else if (Application.isBatchMode)
            {
                Analytics.Begin();
                ShowHome();
            }
            else
            {
                CloudSync.Imported += OnCloudImported;
                var loading = LoadingScreen.Open(transform);
                CloudSync.Startup(() => StartCoroutine(FinishBoot(loading)));
            }
        }

        void OnDestroy()
        {
            CloudSync.Imported -= OnCloudImported;
        }

        void OnCloudImported()
        {
            if (_battle != null) return;
            if (_home != null) Destroy(_home.gameObject);
            _home = null;
            if (FindObjectOfType<LoadingScreen>() != null) return;
            ShowHome();
        }

        void LateUpdate() => Ui.FitPhone();

        void Update()
        {
            float window = Screen.width / Mathf.Max(1f, Screen.height);
            if (!Mathf.Approximately(_aspect, window)) FitCamera();
        }

        void FitCamera()
        {
            _aspect = Screen.width / Mathf.Max(1f, Screen.height);
            _cam.rect = Ui.CameraViewport();
            BattleView.FitCamera(_cam);
            Ui.FitPhone();
            if (_battle != null) _battle.View.CatchLayout();
        }

        IEnumerator FinishBoot(LoadingScreen loading)
        {
            if (loading) yield return loading.WaitMin();
            ShowHome();
            if (!loading) yield break;
            loading.BringToFront();
            yield return loading.FadeOut();
        }

        void ShowHome()
        {
            if (_battle) Destroy(_battle.gameObject);
            _battle = null;
            _home = HomeScreen.Open(transform, StartBattle);
            Sfx.PlayMenu();
        }

        void StartBattle(int chapter) => StartBattle(chapter, false);

        void StartBattle(int chapter, bool autoplay)
        {
            if (_home) Destroy(_home.gameObject);
            _home = null;
            _pending = Resolve(chapter);
            if (BattleScreen.Epic(_pending)) Sfx.PlayEpic();
            else Sfx.PlayStory();
            if (autoplay) OpenBattle(chapter, true);
            else StartCoroutine(MatchThenFight(chapter));
        }

        ChapterDef _pending;

        /// <summary>剧情回直接取；越过终章的序号是聚义厅，按当前阵数和阵容现生成。</summary>
        static ChapterDef Resolve(int chapter)
        {
            if (chapter < ChapterDef.StoryCount) return ChapterDef.All[Mathf.Max(0, chapter)];
            return ChapterDef.Endless(Save.D.endlessRound, Save.Team(), (uint)Environment.TickCount);
        }

        void OpenBattle(int chapter, bool autoplay)
        {
            var loadout = autoplay ? new Loadout() : Save.D.PeekLoadout();
            var ch = _pending ?? Resolve(chapter);
            _pending = null;
            _battle = BattleScreen.Open(transform, ch, _cam, loadout, autoplay, r =>
                ResultPanel.Show(r, () =>
                {
                    ShowHome();
                    if (autoplay || _home == null) return;
                    if (JoinPanel.WantCodex) { JoinPanel.WantCodex = false; _home.ShowCodex(); }
                    else _home.ShowShop();
                }));
        }

        IEnumerator MatchThenFight(int chapter)
        {
            var layer = BuildMatch(chapter);
            yield return new WaitForSecondsRealtime(1.8f);
            if (layer) Destroy(layer.gameObject);
            OpenBattle(chapter, false);
        }

        RectTransform BuildMatch(int chapter)
        {
            var ch = _pending ?? Resolve(chapter);
            var layer = Ui.Fill(Ui.Root, Color.white, true, "match");
            var bg = layer.GetComponent<Image>();
            bg.sprite = Art.Img("home_bg2");
            var bleed = layer.Find("bleed")?.GetComponent<Image>();
            if (bleed != null) bleed.sprite = bg.sprite;
            Ui.Fill(layer, new Color(0.08f, 0.06f, 0.04f, 0.72f), false, "dim");
            var title = Ui.Title(layer, "匹配完成", new Vector2(0, 600), 110, Pal.GoldBright);
            Tween.Pop(title.transform, 0.35f);
            float mine = Save.D.battles <= 0 ? 0f : (float)Save.D.wins / Save.D.battles;
            float rivalRate = Mathf.Clamp01(0.36f + ch.RivalSkill * 0.42f);
            int rivalTier = RankRules.Clamp(Save.D.tier + (ch.RivalSkill >= 0.95f ? 1 : 0) - (ch.RivalSkill < 0.7f ? 1 : 0));
            int rivalStars = Mathf.RoundToInt(ch.RivalSkill * RankRules.StarsAt(rivalTier));
            var me = Banner(layer, new Vector2(0, 250), "我", Save.D.tier, Save.D.stars, mine, Ui.BtnBlue, null);
            var rival = Banner(layer, new Vector2(0, -250), ch.RivalName, rivalTier, rivalStars, rivalRate,
                Pal.Hex("D9452F"), ch.RivalName.Substring(0, 1));
            SlideIn(me, -1200f);
            SlideIn(rival, 1200f);
            var vs = Ui.Icon(layer, "ui_swords", 220, new Vector2(0, 0));
            var vsText = Ui.Label(layer, "VS", 96, Pal.Hex("FFE36A"), new Vector2(0, -10), new Vector2(240, 130), TextAnchor.MiddleCenter, Fonts.Kai);
            vsText.fontStyle = FontStyle.Bold;
            Ui.Stroke(vsText, Ui.Edge, 5f);
            Tween.Pop(vs.transform, 0.35f, 0.3f);
            Tween.Pop(vsText.transform, 0.35f, 0.35f);
            Ui.Label(layer, (ch.IsEndless ? "" : "第" + BattleView.ToCn(ch.Index + 1) + "回 · ") + ch.Name, 44, Pal.Paper.A(0.9f), new Vector2(0, -520), new Vector2(800, 70), TextAnchor.MiddleCenter, Fonts.Brush);
            return layer;
        }

        static RectTransform Banner(RectTransform layer, Vector2 pos, string name, int tier, int stars, float rate, Color bg, string glyph)
        {
            var holder = Ui.Rect(layer, "banner");
            holder.anchoredPosition = pos;
            var face = Ui.Framed(holder, new Vector2(900, 250), Vector2.zero, bg, Ui.Edge, 7f, "frame");
            var t = face.transform;
            Ui.Box(t, new Vector2(860, 30), new Vector2(0, 86), Color.white.A(0.18f), null, "gloss");
            if (glyph == null) Ui.Icon(t, "ui_avatar", 190, new Vector2(-330, 0));
            else
            {
                var av = Ui.Framed(t, new Vector2(176, 176), new Vector2(-330, 0), Pal.Tile, Ui.Edge, 5f, "avatar");
                av.pixelsPerUnitMultiplier = 1.2f;
                Ui.Label(av.transform, glyph, 120, Pal.Ink, new Vector2(0, 6), new Vector2(176, 176), TextAnchor.MiddleCenter, Fonts.Brush);
            }
            Ui.Stroke(Ui.Label(t, name, 66, Pal.Paper, new Vector2(-70, 50), new Vector2(400, 90), TextAnchor.MiddleLeft, Fonts.Brush), Ui.Edge, 3f);
            Ui.Stroke(Ui.Label(t, RankRules.Name(tier), 42, Pal.Hex("FFE36A"), new Vector2(-70, -14), new Vector2(400, 60), TextAnchor.MiddleLeft, Fonts.Brush), Ui.Edge, 2.5f);
            Ui.RankBadge(t, tier, new Vector2(300, 70), 0.55f);
            var st = Ui.Label(t, Ui.RankStars(tier, stars), 48, Color.white, new Vector2(-70, -72), new Vector2(400, 60), TextAnchor.MiddleLeft);
            st.supportRichText = true;
            Ui.Stroke(st, Ui.Edge, 2f);
            Ui.Label(t, "胜率", 32, Pal.Paper.A(0.85f), new Vector2(300, -4), new Vector2(200, 50));
            Ui.Stroke(Ui.Label(t, (rate * 100f).ToString("0.0") + "%", 56, Pal.Paper, new Vector2(300, -60), new Vector2(240, 80)), Ui.Edge, 3f).fontStyle = FontStyle.Bold;
            return holder;
        }

        static void SlideIn(RectTransform t, float from)
        {
            var end = t.anchoredPosition;
            t.anchoredPosition = end + new Vector2(from, 0);
            Tween.Run(t, 0.35f, k => { if (t) t.anchoredPosition = end + new Vector2(from * (1f - Tween.OutBack(k)), 0); });
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        // ─────────────────────────── 自动截图（验收用） ───────────────────────────

        IEnumerator Shot(string name)
        {
            yield return new WaitForEndOfFrame();
            Directory.CreateDirectory(_shots);
            ScreenCapture.CaptureScreenshot(Path.Combine(_shots, $"{_shotIndex++:00}_{name}.png"));
            yield return null;
        }

        IEnumerator AutoShots()
        {
            int chapter = int.TryParse(Arg("-chapter"), out var c) ? Mathf.Clamp(c, 0, ChapterDef.StoryCount) : 0;
            Save.D.heroes.Add((int)HeroId.WuSong);
            Save.D.heroes.Add((int)HeroId.LinChong);
            ShowHome();
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("home");
            _home.ShowCodex();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("codex");
            Destroy(_home.gameObject);
            ShowHome();
            _home.ShowShop();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("shop");

            StartBattle(chapter, true);
            for (int i = 0; i < 2; i++)
            {
                yield return new WaitForSecondsRealtime(1.6f);
                yield return Shot("scout");
            }
            _battle.Coach("开出来的白地才能摆字。\n招兵偶尔会掉铲子，上方铲子按钮看广告也能领。", 3f);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("hint");
            int heroShots = 0;
            _battle.View.OnEvent += ev =>
            {
                if (ev.Side == 0 && ev.Type == Ev.HeroFormed && heroShots++ < 2) StartCoroutine(DelayedShot("hero", 0.5f));
                if (ev.Type == Ev.BossSpawn) StartCoroutine(DelayedShot("boss", 0.6f));
            };
            float t = 0;
            while (_battle != null && !_battle.M.Over && t < 150f)
            {
                yield return new WaitForSecondsRealtime(5f);
                t += 5f;
                if (t % 15f < 0.1f) yield return Shot("battle");
                if (Mathf.Approximately(t, 55f) || Mathf.Approximately(t, 100f))
                    for (int i = 0; i < 6; i++)
                    {
                        yield return Shot("fx");
                        yield return new WaitForSecondsRealtime(0.06f);
                    }
                if (Mathf.Approximately(t, 20f))
                {
                    _battle.View.ShowInfoForAnyPiece();
                    yield return new WaitForSecondsRealtime(0.4f);
                    yield return Shot("info");
                }
                if (Mathf.Approximately(t, 35f))
                {
                    _battle.View.ShowShovelOffer();
                    yield return new WaitForSecondsRealtime(0.8f);
                    yield return Shot("shovelOffer");
                    _battle.View.ShovelAd();
                    yield return new WaitForSecondsRealtime(1.0f);
                    yield return Shot("ad");
                    yield return new WaitForSecondsRealtime(3.0f);
                    yield return Shot("shovels");
                }
                if (Mathf.Approximately(t, 70f))
                {
                    _battle.View.RescueAd();
                    yield return new WaitForSecondsRealtime(4.2f);
                    yield return Shot("rescue");
                }
            }
            if (_battle != null) _battle.ForceEnd(true);
            yield return new WaitForSecondsRealtime(2.6f);
            yield return Shot("result");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        /// <summary>点选射程对照：-shots DIR -range 1。圆画在我方棋盘中央。</summary>
        IEnumerator RangeShots()
        {
            StartBattle(0, true);
            yield return new WaitForSecondsRealtime(9f);
            var view = _battle.View;
            view.Speed = 0f;
            var at = new Vector2(0f, BattleView.MyBottom + 3.2f);
            foreach (UnitKind k in Enum.GetValues(typeof(UnitKind)))
            {
                view.PreviewRange(UnitDef.Get(k).Range, at);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Shot("range_" + k);
            }
            view.PreviewRange(UnitDef.Get(UnitKind.Gong).Range, new Vector2(BattleView.Left + 0.5f, BattleView.MyBottom + 0.5f));
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("range_corner");
            foreach (var id in new[] { HeroId.WuSong, HeroId.LinChong, HeroId.HuSanNiang, HeroId.LuZhiShen, HeroId.HuaRong })
            {
                var d = HeroDef.Get(id);
                view.PreviewRange(d.Range, at, true, d.Disha);
                yield return new WaitForSecondsRealtime(0.6f);
                yield return Shot("range_" + id);
            }
            Application.Quit();
        }

        /// <summary>逐个放出兵种、好汉普攻和大招特效截图：-shots DIR -fx 1。</summary>
        IEnumerator FxShots()
        {
            StartBattle(0, true);
            yield return new WaitForSecondsRealtime(3.5f);
            var view = _battle.View;
            view.Speed = 0f;
            foreach (UnitKind k in Enum.GetValues(typeof(UnitKind)))
            {
                view.ShowcaseUnit(k, 3);
                yield return new WaitForSecondsRealtime(k == UnitKind.Gong ? 0.1f : 0.07f);
                yield return Shot("unit_" + k);
                yield return new WaitForSecondsRealtime(0.5f);
            }
            foreach (HeroId id in Enum.GetValues(typeof(HeroId)))
            {
                var def = HeroDef.Get(id);
                if (def.NoAttack) continue;
                view.ShowcaseHero(id, false);
                yield return new WaitForSecondsRealtime(0.12f);
                yield return Shot("atk_" + id);
                yield return new WaitForSecondsRealtime(0.6f);
            }
            view.ShowcaseHero(HeroId.YanQing, true);
            yield return new WaitForSecondsRealtime(0.12f);
            yield return Shot("atk_YanQingCrit");
            yield return new WaitForSecondsRealtime(0.6f);
            foreach (HeroId id in Enum.GetValues(typeof(HeroId)))
            {
                if (HeroDef.Get(id).Hidden) continue;
                view.ShowcaseUlt(id);
                yield return new WaitForSecondsRealtime(0.3f);
                yield return Shot("ult_" + id);
                yield return new WaitForSecondsRealtime(1.2f);
            }
            Application.Quit();
        }

        /// <summary>只截局外界面：-shots DIR -ui 1。</summary>
        IEnumerator UiShots()
        {
            Save.D.coins = 117;
            Save.D.stars = 2;
            Save.D.tier = 13;
            Save.D.claimDay = Save.Today;
            Save.D.claimTier = 12;
            Save.D.claimStars = 3;
            Save.D.claimTaken = false;
            for (int i = 0; i < 25; i++) Save.WinStory(i);
            foreach (var id in Save.D.joined) Save.D.heroes.Add(id);
            Save.D.weapons.AddRange(new[] { (int)HeroId.WuSong, (int)HeroId.WuSong, (int)HeroId.WuSong, (int)HeroId.LinChong, (int)HeroId.LiKui, (int)HeroId.LiKui, (int)HeroId.HuaRong,
                (int)HeroId.LuZhiShen, (int)HeroId.ZhangHeng, (int)HeroId.SunErNiang });
            Save.D.armed.Add((int)HeroId.WuSong);
            Save.D.armed.Add((int)HeroId.HuaRong);
            ShowHome();
            _home.ShowDaily();
            yield return new WaitForSecondsRealtime(1.2f);
            yield return Shot("daily");
            Destroy(_home.gameObject);
            Save.D.claimTaken = true;
            ShowHome();
            yield return new WaitForSecondsRealtime(1.0f);
            yield return Shot("home");
            Save.D.claimTaken = false;

            yield return Page(() => _home.ShowBoard(), "board");
            yield return Page(() => _home.ShowArmory(), "armory");
            yield return Page(() => { _home.ShowArmory(); _home.SelectWeapon((int)HeroId.WuSong); }, "armory_detail");
            yield return Page(() => _home.ShowCodex(), "codex");
            yield return Page(() => _home.ShowChapters(), "chapters");
            yield return Page(() => JoinPanel.Show(new System.Collections.Generic.List<HeroId> { HeroId.LinChong }, 3, false, null), "join");
            Destroy(Ui.Root.Find("join")?.gameObject);

            for (int i = 25; i < ChapterDef.StoryCount; i++) Save.WinStory(i);
            Save.D.endlessRound = 7;
            Save.D.endlessBest = 12;
            yield return Page(() => _home.ShowTeam(), "team");
            yield return Page(() => JoinPanel.ShowSeats(null), "seats");
            Destroy(Ui.Root.Find("seats")?.gameObject);

            Destroy(_home.gameObject);
            ShowHome();
            Save.D.coins = 117;
            _home.ShowShop();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("shop");
            int first = Save.D.shopOffer[0];
            Save.D.coins -= ShopItem.All[first].Price;
            ShopItem.All[first].Give();
            Save.D.shopSold.Add(first);
            Save.D.shovels = 2;
            _home.ShowShop();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("shop_bought");
            _home.ShowShop(true);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("lotto");
            _home.DebugSpin();
            yield return new WaitForSecondsRealtime(3.2f);
            yield return Shot("lotto_win");

            Destroy(_home.gameObject);
            _home = null;
            var match = BuildMatch(1);
            yield return new WaitForSecondsRealtime(0.9f);
            yield return Shot("match");
            Destroy(match.gameObject);

            OpenBattle(1, false);
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot("intro");
            _battle.ShotItem();
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("item");
            _battle.View.PreviewItemDrag(4, new Vector2(0.6f, BattleView.MyBottom + 2.2f));
            yield return Shot("item_drag");
            _battle.View.PreviewItemBubble(0);
            yield return new WaitForSecondsRealtime(0.3f);
            yield return Shot("item_bubble");
            yield return new WaitForSecondsRealtime(1.2f);
            _battle.ShotPause();
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Shot("pause");
            Destroy(_battle.gameObject);
            _battle = null;

            var win = new BattleResult { Chapter = 1, Win = true, Wave = 12, Kills = 230, Rains = 2 };
            win.Formed.Add(HeroId.WuSong);
            win.Formed.Add(HeroId.ZhangShun);
            win.NewHeroes.Add(HeroId.ZhangShun);
            Save.D.stars = 2;
            ResultPanel.Show(win, null);
            yield return new WaitForSecondsRealtime(2.0f);
            yield return Shot("result_win");
            Destroy(Ui.Root.Find("result").gameObject);
            Save.D.stars = 3;
            ResultPanel.Show(new BattleResult { Chapter = 1, Win = false, Wave = 7, Kills = 96 }, null);
            yield return new WaitForSecondsRealtime(2.0f);
            yield return Shot("result_lose");
            Destroy(Ui.Root.Find("result").gameObject);

            AdStub.Show("双倍铜钱", null);
            yield return new WaitForSecondsRealtime(0.5f);
            yield return Shot("ad");
            yield return new WaitForSecondsRealtime(1.5f);
            Application.Quit();
        }

        IEnumerator Page(Action open, string name)
        {
            Destroy(_home.gameObject);
            ShowHome();
            yield return null;
            open();
            yield return new WaitForSecondsRealtime(0.8f);
            yield return Shot(name);
        }

        IEnumerator DelayedShot(string name, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return Shot(name);
        }
    }
}
