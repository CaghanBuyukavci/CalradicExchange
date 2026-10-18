using System;

namespace CalradicExchange.Core.Random
{
    /// <summary>xorshift128+ (Vigna). Seeded through splitmix64 so that any seed, including 0, gives a valid state.</summary>
    public sealed class XorShift128Plus : IRandom
    {
        private ulong _s0;
        private ulong _s1;

        public XorShift128Plus(ulong seed)
        {
            Reseed(seed);
        }

        public XorShift128Plus(RandomState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.S0 == 0 && state.S1 == 0) throw new ArgumentException("xorshift128+ state must not be all zero.", nameof(state));
            _s0 = state.S0;
            _s1 = state.S1;
        }

        public void Reseed(ulong seed)
        {
            ulong x = seed;
            _s0 = SplitMix64(ref x);
            _s1 = SplitMix64(ref x);
            if (_s0 == 0 && _s1 == 0) _s1 = 1;
        }

        public ulong NextULong()
        {
            ulong s1 = _s0;
            ulong s0 = _s1;
            ulong result = s0 + s1;
            _s0 = s0;
            s1 ^= s1 << 23;
            _s1 = s1 ^ s0 ^ (s1 >> 17) ^ (s0 >> 26);
            return result;
        }

        public double NextDouble()
        {
            return (NextULong() >> 11) * (1.0 / (1UL << 53));
        }

        /// <summary>Box-Muller; uses two uniforms per call and keeps no cached value, so the state stays two words.</summary>
        public double NextGaussian()
        {
            double u1 = 1.0 - NextDouble(); // (0, 1]
            double u2 = NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
        }

        /// <summary>Uniform integer in [0, maxExclusive).</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextDouble() * maxExclusive);
        }

        public RandomState GetState()
        {
            return new RandomState { S0 = _s0, S1 = _s1 };
        }

        private static ulong SplitMix64(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}
