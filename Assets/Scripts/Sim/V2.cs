using System;

namespace Rescue.Sim
{
    public struct V2
    {
        public float x, y;

        public V2(float x, float y) { this.x = x; this.y = y; }

        public static V2 operator +(V2 a, V2 b) => new V2(a.x + b.x, a.y + b.y);
        public static V2 operator -(V2 a, V2 b) => new V2(a.x - b.x, a.y - b.y);
        public static V2 operator *(V2 a, float k) => new V2(a.x * k, a.y * k);

        public float Len => (float)Math.Sqrt(x * x + y * y);
        public V2 Norm { get { var l = Len; return l < 1e-5f ? new V2(0, 0) : new V2(x / l, y / l); } }

        public static float Dist(V2 a, V2 b) => (a - b).Len;
        public static float Dot(V2 a, V2 b) => a.x * b.x + a.y * b.y;
        public static V2 Lerp(V2 a, V2 b, float t) => new V2(a.x + (b.x - a.x) * t, a.y + (b.y - a.y) * t);
    }

    public sealed class Rng
    {
        uint _s;

        public Rng(uint seed) { _s = seed == 0 ? 0x9E3779B9u : seed; }

        public uint NextU()
        {
            _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5;
            return _s;
        }

        public float Next01() => (NextU() & 0xFFFFFF) / (float)0x1000000;
        public int Range(int minInclusive, int maxExclusive) => minInclusive + (int)(NextU() % (uint)(maxExclusive - minInclusive));
        public bool Chance(float p) => Next01() < p;
    }
}
