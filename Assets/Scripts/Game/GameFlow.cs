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
            Save.Load();
            if (_shots != null)
            {
                Save.Volatile = true;
                Save.D.tutorial = 0;
            }
            Tween.Init(gameObject);
            Sfx.Init(gameObject);
            if (_shots == null) Analytics.Begin();

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
            _backdrop.backgroundColor = Pal.Hex("1A1612");
            _backdrop.depth = _cam.depth - 1;
            FitCamera();

            Ui.Init(transform);
            if (_shots != null) StartCoroutine(Arg("-ui") != null ? UiShots() : Arg("-fx") != null ? FxShots() : AutoShots());
            else ShowHome();
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
            float phone = Ui.DesignW / Ui.DesignH;
            _cam.orthographicSize = Mathf.Max(8.8f, 4.35f / phone);
            Ui.FitPhone();
        }

        void ShowHome()
        {
            if (_battle) Destroy(_battle.gameObject);
            _battle = null;
            _home = HomeScreen.Open(transform, StartBattle);
        }

        void StartBattle(int chapter) => StartBattle(chapter, false);

        void StartBattle(int chapter, bool autoplay)
        {
            if (_home) Destroy(_home.gameObject);
            _home = null;
            if (autoplay) OpenBattle(chapter, true);
            else StartCoroutine(MatchThenFight(chapter));
        }

        void OpenBattle(int chapter, bool autoplay)
        {
            var loadout = autoplay ? new Loadout() : Save.D.TakeLoadout();
            Save.Write();
            _battle = BattleScreen.Open(transform, ChapterDef.All[chapter], _cam, loadout, autoplay, r =>
                ResultPanel.Show(r, () =>
                {
                    ShowHome();
                    if (!autoplay && _home != null) _home.ShowShop();
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
            var ch = ChapterDef.All[chapter];
            var layer = Ui.Fill(Ui.Root, Color.white, true, "match");
            var bg = layer.GetComponent<Image>();
            bg.sprite = Art.Img("home_bg2");
            Ui.Fill(layer, new Color(0.08f, 0.06f, 0.04f, 0.72f), false, "dim");
            var title = Ui.Title(layer, "匹配完成", new Vector2(0, 600), 110, Pal.GoldBright);
            Tween.Pop(title.transform, 0.35f);
            float mine = Save.D.battles <= 0 ? 0f : (float)Save.D.wins / Save.D.battles;
            float rivalRate = Mathf.Clamp01(0.36f + ch.RivalSkill * 0.42f);
            int rivalTier = Mathf.Clamp(Save.D.tier + (ch.RivalSkill >= 0.55f ? 1 : 0), 0, Save.Tiers.Length - 1);
            var me = Banner(layer, new Vector2(0, 250), "我", Save.RankName, Save.D.stars, mine, Ui.BtnBlue, null);
            var rival = Banner(layer, new Vector2(0, -250), ch.RivalName, Save.Tiers[rivalTier], Mathf.RoundToInt(ch.RivalSkill * RankRules.Stars), rivalRate,
                Pal.Hex("D9452F"), ch.RivalName.Substring(0, 1));
            SlideIn(me, -1200f);
            SlideIn(rival, 1200f);
            var vs = Ui.Icon(layer, "ui_swords", 220, new Vector2(0, 0));
            var vsText = Ui.Label(layer, "VS", 96, Pal.Hex("FFE36A"), new Vector2(0, -10), new Vector2(240, 130), TextAnchor.MiddleCenter, Fonts.Kai);
            vsText.fontStyle = FontStyle.Bold;
            Ui.Stroke(vsText, Ui.Edge, 5f);
            Tween.Pop(vs.transform, 0.35f, 0.3f);
            Tween.Pop(vsText.transform, 0.35f, 0.35f);
            Ui.Label(layer, "对阵 · " + ch.Name, 44, Pal.Paper.A(0.9f), new Vector2(0, -520), new Vector2(800, 70), TextAnchor.MiddleCenter, Fonts.Brush);
            return layer;
        }

        static RectTransform Banner(RectTransform layer, Vector2 pos, string name, string tier, int stars, float rate, Color bg, string glyph)
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
            Ui.Stroke(Ui.Label(t, tier, 42, Pal.Hex("FFE36A"), new Vector2(-70, -14), new Vector2(400, 60), TextAnchor.MiddleLeft, Fonts.Brush), Ui.Edge, 2.5f);
            var st = Ui.Label(t, Ui.Stars(stars, RankRules.Stars), 48, Color.white, new Vector2(-70, -72), new Vector2(400, 60), TextAnchor.MiddleLeft);
            st.supportRichText = true;
            Ui.Stroke(st, Ui.Edge, 2f);
            Ui.Label(t, "胜率", 32, Pal.Paper.A(0.85f), new Vector2(300, 36), new Vector2(200, 50));
            Ui.Stroke(Ui.Label(t, (rate * 100f).ToString("0.0") + "%", 56, Pal.Paper, new Vector2(300, -28), new Vector2(240, 80)), Ui.Edge, 3f).fontStyle = FontStyle.Bold;
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
            int chapter = int.TryParse(Arg("-chapter"), out var c) ? Mathf.Clamp(c, 0, ChapterDef.All.Length - 1) : 0;
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
            Save.D.stars = 4;
            Save.D.tier = 2;
            Save.D.unlocked = 2;
            Save.D.claimDay = Save.Today;
            Save.D.claimTier = 2;
            Save.D.claimStars = 3;
            Save.D.claimTaken = false;
            foreach (var id in new[] { HeroId.WuSong, HeroId.LinChong, HeroId.LiKui, HeroId.HuaRong, HeroId.SunErNiang, HeroId.LuZhiShen })
                Save.D.heroes.Add((int)id);
            Save.D.weapons.AddRange(new[] { (int)HeroId.WuSong, (int)HeroId.WuSong, (int)HeroId.WuSong, (int)HeroId.LinChong, (int)HeroId.LiKui, (int)HeroId.LiKui, (int)HeroId.HuaRong });
            Save.D.armed.Add((int)HeroId.WuSong);
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
