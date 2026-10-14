using System.Collections.Generic;
using UnityEngine;

namespace Rescue
{
    public enum Snd { Tap, Recruit, Place, Merge, Hero, Hit, Kill, Leak, Rain, Ult, Win, Lose, Staff, Warn }

    /// <summary>程序合成的音效：木鱼、锣、鼓、铜钱，全部运行时生成。</summary>
    public sealed class Sfx : MonoBehaviour
    {
        const int Rate = 22050;
        static Sfx _i;
        readonly Dictionary<Snd, AudioClip> _clips = new Dictionary<Snd, AudioClip>();
        readonly Dictionary<Snd, float> _last = new Dictionary<Snd, float>();
        AudioSource[] _src;
        int _next;

        public static bool On = true;

        public static void Init(GameObject host)
        {
            if (_i != null) return;
            _i = host.AddComponent<Sfx>();
            _i.Build();
        }

        public static void Play(Snd s, float vol = 1f, float pitch = 1f)
        {
            if (_i == null || !On) return;
            float minGap = s == Snd.Hit || s == Snd.Kill ? 0.06f : 0.03f;
            if (_i._last.TryGetValue(s, out var t) && Time.unscaledTime - t < minGap) return;
            _i._last[s] = Time.unscaledTime;
            var src = _i._src[_i._next++ % _i._src.Length];
            src.pitch = pitch * Random.Range(0.96f, 1.04f);
            src.PlayOneShot(_i._clips[s], vol);
        }

        void Build()
        {
            _src = new AudioSource[10];
            for (int i = 0; i < _src.Length; i++)
            {
                _src[i] = gameObject.AddComponent<AudioSource>();
                _src[i].playOnAwake = false;
            }
            _clips[Snd.Tap] = Clip("tap", 0.05f, (t, k) => Noise() * Env(t, 0.002f, 0.03f) * 0.4f + Sine(1800, t) * Env(t, 0.001f, 0.02f) * 0.3f);
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
