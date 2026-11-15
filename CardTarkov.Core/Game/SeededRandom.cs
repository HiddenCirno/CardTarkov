using System;

namespace CardTarkov.Core.Game
{
    /// <summary>
    /// SplitMix64 的轻量实现。固定种子即可逐局复现，
    /// 不依赖 <see cref="Random"/> 在不同运行时的实现差异。
    /// </summary>
    public sealed class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed)
        {
            _state = seed;
        }

        public ulong NextUInt64()
        {
            _state += 0x9E3779B97F4A7C15UL;
            ulong z = _state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>[0, maxExclusive)</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                return 0;

            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        /// <summary>[minInclusive, maxExclusive)</summary>
        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                return minInclusive;

            return minInclusive + Next(maxExclusive - minInclusive);
        }

        /// <summary>[0, 1)</summary>
        public double NextDouble()
        {
            return (NextUInt64() >> 11) * (1.0 / 9007199254740992.0);
        }
    }
}
