using System;

namespace Conn.MapGenV2.Core
{
    public struct MapGenRandom
    {
        private uint state;

        public MapGenRandom(int seed)
        {
            state = (uint)seed;
            if (state == 0)
            {
                state = 0x6d2b79f5u;
            }
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Maximum must be greater than minimum.");
            }

            var range = (uint)(maxExclusive - minInclusive);
            return minInclusive + (int)(NextUInt() % range);
        }

        public float NextFloat01()
        {
            return (NextUInt() & 0x00ffffff) / 16777216f;
        }

        public MapGenRandom Fork(string streamName)
        {
            unchecked
            {
                var hash = state;
                for (var i = 0; i < (streamName?.Length ?? 0); i++)
                {
                    hash ^= streamName[i];
                    hash *= 16777619u;
                }

                return new MapGenRandom((int)hash);
            }
        }

        private uint NextUInt()
        {
            unchecked
            {
                var x = state;
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                state = x == 0 ? 0x6d2b79f5u : x;
                return state;
            }
        }
    }
}
