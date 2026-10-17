using System.Collections.Generic;
using UnityEngine;

namespace Rescue
{
    public enum Snd
    {
        Tap, Deny, Recruit, Place, Merge, Hero, Hit, Kill, Leak, Rain, Ult, Win, Lose, Staff, Warn,
        Dao, Spear, Bow, ArrowHit, Horse, Axe, Smash, Water, FireHit, Lasso, Fist, Dart, Poison, Crit, Whip, Hammer, Sword, Stone,
        UltFire, UltWater, UltWind, UltWhirl, UltMagic, UltSmash, UltThunder, UltCannon, UltHeal, UltDrum, Join, RankUp, CampFull,
    }

    /// <summary>界面音是程序合成的木鱼、锣、鼓、铜钱；兵器和招式音从 Resources/Sfx 读录音，读不到时退回合成音。</summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int Rate = 22050;
        static Sfx _i;
        readonly Dictionary<Snd, AudioClip> _clips = new Dictionary<Snd, AudioClip>();
        readonly Dictionary<Snd, float> _last = new Dictionary<Snd, float>();
        AudioSource[] _src;
        float[] _stopAt;
        int _next;
        enum Bed { None, Menu, Story, Battle, Crisis, Epic, Victory, Defeat }

        AudioSource _music;
        AudioClip _menu, _story, _battle, _crisis, _epic, _victory, _defeat;
        Bed _want;
        float _menuAt;
        bool _musicOn, _musicPaused;
        /// <summary>大于 0：到点主动停（凯旋、败走只放一遍）。-1：已经放过一遍，停在那里。</summary>
        float _musicStopAt;
        double _dsp;
        float _dspStill;
        const float MusicVol = 0.4f;

        public static bool On = true;
        /// <summary>截图等无人听的场合关掉，避免去读那首两分多钟的曲子。</summary>
        public static bool Music = true;

        public static void Init(GameObject host)
        {
            if (_i != null) return;
            _i = host.AddComponent<Sfx>();
            _i.Build();
        }

        public static void Play(Snd s, float vol = 1f, float pitch = 1f)
        {
            if (_i == null || !On || _i._dspStill > 0.5f) return;
            var clip = _i._clips[s];
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            if (clip.loadState != AudioDataLoadState.Loaded) return;
            float minGap = s == Snd.Hit || s == Snd.Kill ? 0.06f : s >= Snd.Dao && s < Snd.UltFire ? 0.07f : 0.03f;
            if (_i._last.TryGetValue(s, out var t) && Time.unscaledTime - t < minGap) return;
            _i._last[s] = Time.unscaledTime;
            int i = _i.Pick();
            var src = _i._src[i];
            // 还在响的声道先停掉。WebGL 上让片段自己播完，FMOD 会先作废句柄，引擎下一帧查 isPlaying 就刷 INVALID_HANDLE。
            if (_i._stopAt[i] > 0f) src.Stop();
            float pitchAbs = Mathf.Max(0.5f, pitch * Random.Range(0.96f, 1.04f));
            src.clip = clip;
            src.loop = true;
            src.volume = vol;
            src.pitch = pitchAbs;
            src.Play();
            float dur = clip.length / pitchAbs;
            float margin = Mathf.Min(0.05f, dur * 0.2f);
            _i._stopAt[i] = Time.unscaledTime + Mathf.Max(0.02f, dur - margin);
        }

        /// <summary>挑一条已经停掉的声道；都忙就抢最快结束的那条。不读 isPlaying。</summary>
        int Pick()
        {
            float now = Time.unscaledTime;
            for (int k = 0; k < _src.Length; k++)
            {
                int i = (_next + k) % _src.Length;
                if (_stopAt[i] > now) continue;
                _next = i + 1;
                return i;
            }
            int best = 0;
            for (int i = 1; i < _src.Length; i++)
                if (_stopAt[i] < _stopAt[best]) best = i;
            _next = best + 1;
            return best;
        }

        void ReleaseVoices()
        {
            if (_src == null) return;
            float now = Time.unscaledTime;
            for (int i = 0; i < _src.Length; i++)
            {
                if (_stopAt[i] <= 0f || now < _stopAt[i]) continue;
                _src[i].Stop();
                _src[i].loop = false;
                _stopAt[i] = 0f;
            }
        }

        void Fade(float target, float speed)
        {
            float v = Mathf.MoveTowards(_music.volume, target, speed);
            if (Mathf.Abs(v - _music.volume) > 0.0005f) _music.volume = v;
        }

        /// <summary>主界面「梁山泊」。已经在放就接着放，从别处回来从上次的位置淡入。</summary>
        public static void PlayMenu() => _i?.Want(Bed.Menu);

        /// <summary>回目简报、好汉入伙、小课堂用的「替天行道」。已经在放就不重头。</summary>
        public static void PlayStory() => _i?.Want(Bed.Story);

        /// <summary>开战之后的「聚义」。已经在放就不重头，音量压低一点，给刀枪声留位置。</summary>
        public static void PlayBattle() => _i?.Want(Bed.Battle);

        /// <summary>头目或宋江告急时的「大闹」。已经在放就不重头。</summary>
        public static void PlayCrisis() => _i?.Want(Bed.Crisis);

        /// <summary>聚义厅、车轮战、终章和排座次用的「百八聚义」。已经在放就不重头。</summary>
        public static void PlayEpic() => _i?.Want(Bed.Epic);

        /// <summary>赢了之后的「凯旋」，只放一遍，放完就静着。</summary>
        public static void PlayVictory() => _i?.Want(Bed.Victory);

        /// <summary>输了之后的「败走」，只放一遍，放完就静着。</summary>
        public static void PlayDefeat() => _i?.Want(Bed.Defeat);

        /// <summary>淡出当前的曲子。</summary>
        public static void StopMusic() => _i?.Want(Bed.None);

        void Want(Bed bed)
        {
            if (!Music && bed != Bed.None) return;
            EnsureMusic();
            if (bed == Bed.Menu && _menu == null) return;
            if (bed == Bed.Story && _story == null) return;
            if (bed == Bed.Battle && _battle == null) return;
            if (bed == Bed.Crisis && _crisis == null) return;
            if (bed == Bed.Epic && _epic == null) return;
            if (bed == Bed.Victory && _victory == null) return;
            if (bed == Bed.Defeat && _defeat == null) return;
            _want = bed;
        }

        void EnsureMusic()
        {
            if (_music == null)
            {
                _music = gameObject.AddComponent<AudioSource>();
                _music.playOnAwake = false;
                _music.loop = true;
                _music.spatialBlend = 0f;
                _music.volume = 0f;
                _music.priority = 64;
            }
            if (_menu == null) _menu = Resources.Load<AudioClip>("Music/menu");
            if (_story == null) _story = Resources.Load<AudioClip>("Music/story");
            if (_battle == null) _battle = Resources.Load<AudioClip>("Music/battle");
            if (_crisis == null) _crisis = Resources.Load<AudioClip>("Music/crisis");
            if (_epic == null) _epic = Resources.Load<AudioClip>("Music/epic");
            if (_victory == null) _victory = Resources.Load<AudioClip>("Music/victory");
            if (_defeat == null) _defeat = Resources.Load<AudioClip>("Music/defeat");
            Warm(_menu); Warm(_story); Warm(_battle); Warm(_crisis); Warm(_epic); Warm(_victory); Warm(_defeat);
        }

        static void Warm(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
        }

        AudioClip Wanted => _want switch
        {
            Bed.Menu => _menu,
            Bed.Story => _story,
            Bed.Battle => _battle,
            Bed.Crisis => _crisis,
            Bed.Epic => _epic,
            Bed.Victory => _victory,
            Bed.Defeat => _defeat,
            _ => null,
        };

        float WantedVol => _want == Bed.Battle || _want == Bed.Crisis ? 0.3f : _want == Bed.Epic ? 0.33f : MusicVol;

        void HaltMusic()
        {
            if (!_musicOn && !_musicPaused) return;
            _music.Stop();
            _musicOn = false;
            _musicPaused = false;
        }

        void BeginMusic(AudioClip clip, bool sting)
        {
            if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            if (clip.loadState != AudioDataLoadState.Loaded) return;
            if (_music.clip == _menu && (_musicOn || _musicPaused)) _menuAt = _music.time;
            HaltMusic();
            _music.clip = clip;
            _music.volume = 0f;
            _music.loop = true;
            _musicStopAt = 0f;
            float start = clip == _menu ? _menuAt : 0f;
            if (start > 0.05f)
                _music.time = Mathf.Clamp(start, 0f, Mathf.Max(0f, clip.length - 0.1f));
            _music.Play();
            _musicOn = true;
            if (sting) _musicStopAt = Time.unscaledTime + Mathf.Max(0.1f, clip.length - 0.05f);
        }

        /// <summary>声卡停了（模拟器还没点过、或切到后台）时 dspTime 不再走。这时立刻停掉声道，别留着让引擎每帧去问一个作废句柄。</summary>
        void WatchAudio()
        {
            double dsp = AudioSettings.dspTime;
            if (dsp > _dsp + 0.0001)
            {
                _dsp = dsp;
                _dspStill = 0f;
                return;
            }
            _dspStill += Time.unscaledDeltaTime;
            if (_dspStill <= 0.5f) return;
            if (_src != null)
                for (int i = 0; i < _src.Length; i++)
                {
                    if (_stopAt[i] <= 0f) continue;
                    _src[i].Stop();
                    _src[i].loop = false;
                    _stopAt[i] = 0f;
                }
            if (_music != null && (_musicOn || _musicPaused))
            {
                _music.Stop();
                _musicOn = false;
                _musicPaused = false;
            }
        }

        void Update()
        {
            WatchAudio();
            if (_dspStill > 0.5f) return;
            ReleaseVoices();
            if (_music == null) return;
            if (_musicStopAt > 0f && Time.unscaledTime >= _musicStopAt)
            {
                HaltMusic();
                _musicStopAt = -1f;
            }
            if (!On)
            {
                Fade(0f, Time.unscaledDeltaTime);
                if (_music.volume <= 0.001f && _musicOn)
                {
                    _music.Pause();
                    _musicOn = false;
                    _musicPaused = true;
                }
                return;
            }
            var clip = Wanted;
            bool sting = _want == Bed.Victory || _want == Bed.Defeat;
            if (_music.clip != clip)
            {
                if (clip != null && clip.loadState != AudioDataLoadState.Loaded)
                {
                    if (clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
                    if (_musicOn) Fade(0f, Time.unscaledDeltaTime * (sting ? 2f : 0.8f));
                    return;
                }
                if (_musicOn && _music.volume > 0.02f)
                {
                    Fade(0f, Time.unscaledDeltaTime * (sting ? 2f : 0.8f));
                    return;
                }
                if (clip == null) { HaltMusic(); _music.clip = null; _musicStopAt = 0f; return; }
                BeginMusic(clip, sting);
                return;
            }
            if (clip == null || _musicStopAt < 0f) return;
            if (!_musicOn)
            {
                if (_musicPaused) { _music.UnPause(); _musicPaused = false; }
                else _music.Play();
                _musicOn = true;
            }
            Fade(WantedVol, Time.unscaledDeltaTime * (sting ? 3f : 0.55f));
        }

        void Build()
        {
            _src = new AudioSource[16];
            _stopAt = new float[16];
            for (int i = 0; i < _src.Length; i++)
            {
                _src[i] = gameObject.AddComponent<AudioSource>();
                _src[i].playOnAwake = false;
                _src[i].spatialBlend = 0f;
                _src[i].loop = false;
            }
            _clips[Snd.Tap] = Clip("tap", 0.16f, (t, k) =>
                (Sine(920, t) + Sine(1460, t) * 0.4f) * Env(t, 0.001f, 0.05f) * 0.85f
                + Sine(240, t) * Env(t, 0.002f, 0.08f) * 0.4f
                + Noise() * Env(t, 0.0005f, 0.012f) * 0.22f);
            _clips[Snd.Deny] = Clip("deny", 0.2f, (t, k) =>
                Sine(196 - 70f * t, t) * Env(t, 0.003f, 0.11f) * 0.75f + Noise() * Env(t, 0.001f, 0.03f) * 0.18f);
            _clips[Snd.Recruit] = Clip("recruit", 0.45f, (t, k) =>
                Wood(t, 0) + Wood(t, 0.09f) * 0.8f + (Sine(660, t) + Sine(990, t) * 0.5f) * Env(t - 0.16f, 0.005f, 0.25f) * 0.35f);
            _clips[Snd.Place] = Clip("place", 0.18f, (t, k) => Sine(140 - 200 * t, t) * Env(t, 0.003f, 0.12f) * 0.7f + Noise() * Env(t, 0.001f, 0.03f) * 0.2f);
            _clips[Snd.Merge] = Clip("merge", 0.6f, (t, k) => (Sine(880, t) + Sine(1320, t) * 0.6f + Sine(1760, t) * 0.25f) * Env(t, 0.003f, 0.45f) * 0.35f);
            _clips[Snd.Hero] = Clip("gong", 2.0f, (t, k) =>
                (Sine(110, t) + Sine(167, t) * 0.7f + Sine(231, t) * 0.5f + Sine(352, t) * 0.3f) * Env(t, 0.01f, 1.6f) * 0.32f
                + Noise() * Env(t, 0.001f, 0.08f) * 0.25f);
            _clips[Snd.Hit] = Clip("hit", 0.07f, (t, k) => Noise() * Env(t, 0.001f, 0.04f) * 0.35f);
            _clips[Snd.Kill] = Clip("kill", 0.22f, (t, k) => Noise() * Env(t, 0.01f, 0.16f) * 0.3f * (1 - t * 3));
            _clips[Snd.Leak] = Clip("drum", 0.7f, (t, k) => Sine(90 - 50 * t, t) * Env(t, 0.004f, 0.5f) * 0.9f + Noise() * Env(t, 0.001f, 0.05f) * 0.3f);
            _clips[Snd.Rain] = Clip("coins", 1.0f, (t, k) =>
            {
                float s = 0;
                for (int i = 0; i < 7; i++) s += Sine(2200 + i * 330, t) * Env(t - i * 0.11f, 0.002f, 0.15f);
                return s * 0.18f;
            });
            _clips[Snd.Ult] = Clip("ult", 0.9f, (t, k) => Noise() * Env(t, 0.15f, 0.35f) * 0.35f + Sine(70, t) * Env(t - 0.2f, 0.005f, 0.6f) * 0.8f);
            _clips[Snd.Win] = Clip("win", 1.4f, (t, k) => Arp(t, new[] { 523f, 587f, 659f, 784f, 880f, 1047f }, 0.14f));
            _clips[Snd.Lose] = Clip("lose", 1.4f, (t, k) => Arp(t, new[] { 659f, 587f, 523f, 440f, 392f }, 0.22f));
            _clips[Snd.Staff] = Clip("staff", 0.5f, (t, k) => Wood(t, 0) * 1.2f + Noise() * Env(t - 0.05f, 0.05f, 0.3f) * 0.25f);
            _clips[Snd.Warn] = Clip("warn", 0.8f, (t, k) => Sine(440, t) * Env(t, 0.005f, 0.12f) * 0.4f + Sine(440, t) * Env(t - 0.25f, 0.005f, 0.12f) * 0.4f);
            _clips[Snd.CampFull] = Clip("campfull", 0.42f, (t, k) =>
                (Sine(210, t) + Sine(140, t) * 0.6f) * Env(t, 0.002f, 0.07f) * 0.85f
                + (Sine(155, t) + Sine(98, t) * 0.55f) * Env(t - 0.13f, 0.002f, 0.14f) * 0.9f
                + Noise() * (Env(t, 0.001f, 0.018f) + Env(t - 0.13f, 0.001f, 0.03f)) * 0.16f);

            Load(Snd.Dao, "dao", Snd.Hit);
            Load(Snd.Spear, "spear", Snd.Hit);
            Load(Snd.Bow, "bow", Snd.Tap);
            Load(Snd.ArrowHit, "arrow_hit", Snd.Hit);
            Load(Snd.Horse, "horse", Snd.Leak);
            Load(Snd.Axe, "axe", Snd.Hit);
            Load(Snd.Smash, "staff", Snd.Leak);
            Load(Snd.Water, "water", Snd.Kill);
            Load(Snd.FireHit, "fire", Snd.Ult);
            Load(Snd.Lasso, "lasso", Snd.Kill);
            Load(Snd.Fist, "fist", Snd.Hit);
            Load(Snd.Dart, "dart", Snd.Tap);
            Load(Snd.Poison, "poison", Snd.Kill);
            Load(Snd.Crit, "crit", Snd.Ult);
            Load(Snd.UltFire, "ult_fire", Snd.Ult);
            Load(Snd.UltWater, "ult_water", Snd.Ult);
            Load(Snd.UltWind, "ult_wind", Snd.Ult);
            Load(Snd.UltWhirl, "ult_whirl", Snd.Ult);
            Load(Snd.UltMagic, "ult_magic", Snd.Merge);
            Load(Snd.UltSmash, "ult_smash", Snd.Ult);
            Load(Snd.Whip, "whip", Snd.Hit);
            Load(Snd.Hammer, "hammer", Snd.Smash);
            Load(Snd.Sword, "sword", Snd.Dao);
            Load(Snd.Stone, "stone", Snd.Dart);
            Load(Snd.UltThunder, "ult_thunder", Snd.UltMagic);
            Load(Snd.UltCannon, "ult_cannon", Snd.UltFire);
            Load(Snd.UltHeal, "ult_heal", Snd.Merge);
            Load(Snd.UltDrum, "ult_drum", Snd.Leak);
            Load(Snd.Join, "join", Snd.Hero);
            Load(Snd.RankUp, "rank_up", Snd.Win);
        }

        void Load(Snd s, string file, Snd fallback)
        {
            var c = Resources.Load<AudioClip>("Sfx/" + file);
            _clips[s] = c != null ? c : _clips[fallback];
        }

        static float _noiseSeed = 1;
        static float Noise() { _noiseSeed = (_noiseSeed * 16807f) % 2147483647f; return (_noiseSeed / 2147483647f) * 2f - 1f; }
        static float Sine(float f, float t) => Mathf.Sin(2 * Mathf.PI * f * t);
        static float Env(float t, float a, float d) => t < 0 ? 0 : t < a ? t / a : Mathf.Exp(-(t - a) / d * 3f);
        static float Wood(float t, float at) => (Sine(820, t) + Sine(1250, t) * 0.5f) * Env(t - at, 0.001f, 0.05f) * 0.5f;

        static float Arp(float t, float[] notes, float step)
        {
            float s = 0;
            for (int i = 0; i < notes.Length; i++) s += (Sine(notes[i], t) + Sine(notes[i] * 2, t) * 0.3f) * Env(t - i * step, 0.004f, 0.35f);
            return s * 0.22f;
        }

        static AudioClip Clip(string name, float len, System.Func<float, int, float> f)
        {
            int n = (int)(Rate * len);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Rate;
                float fade = Mathf.Clamp01((len - t) / 0.03f);
                data[i] = Mathf.Clamp(f(t, i) * fade, -1f, 1f);
            }
            var c = AudioClip.Create(name, n, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
