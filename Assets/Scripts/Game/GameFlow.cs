using System;
using System.Collections;
using System.IO;
using Rescue.Sim;
using UnityEngine;

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
            if (_shots != null) StartCoroutine(AutoShots());
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
            var loadout = autoplay ? new Loadout() : Save.D.TakeLoadout();
            Save.Write();
            _battle = BattleScreen.Open(transform, ChapterDef.All[chapter], _cam, loadout, autoplay, r => ResultPanel.Show(r, ShowHome));
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
                    yield return new WaitForSecondsRealtime(1.6f);
                    yield return Shot("shovels");
                }
                if (Mathf.Approximately(t, 70f))
                {
                    _battle.View.RescueAd();
                    yield return new WaitForSecondsRealtime(2.6f);
                    yield return Shot("rescue");
                }
            }
            if (_battle != null) _battle.ForceEnd(true);
            yield return new WaitForSecondsRealtime(2.6f);
            yield return Shot("result");
            yield return new WaitForSecondsRealtime(0.5f);
            Application.Quit();
        }

        IEnumerator DelayedShot(string name, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            yield return Shot(name);
        }
    }
}
