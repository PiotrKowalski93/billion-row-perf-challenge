using System.IO.Hashing;

namespace Shared
{
    public struct Entry
    {
        public byte[] Name;             // For faster comparison
        public string StationName;
        public ulong Hash;
        public int Min;
        public int Max;
        public long Sum;
        public long Count;
    }

    internal unsafe class HashTable
    {
        private const int DefaultCapacity = 1024;
        //private const int MaxCapacity = DefaultCapacity;
        private const double LoadFactor = 0.75;

        private Entry[] _entries;
        private int _count;
        private readonly bool _allowResize;

        public HashTable(int expectedCount = default, bool allowResize = true)
        {
            _allowResize = allowResize;

            var targetCapacity = (int)(expectedCount / LoadFactor);
            var capacity = NextPowerOf2(Math.Max(DefaultCapacity, targetCapacity));
            //capacity = Math.Min(capacity, MaxCapacity);

            _entries = new Entry[capacity];
        }

        // Branchless method to compute the next power of 2 for a given integer n.
        private static int NextPowerOf2(int n)
        {
            if (n <= 0)
                return 1;

            n--;
            n |= n >> 1;
            n |= n >> 2;
            n |= n >> 4;
            n |= n >> 8;
            n |= n >> 16;

            return n + 1;
        }

        public ulong ComputeHashe(byte* bytes, int length)
        {
            return XxHash3.HashToUInt64(new ReadOnlySpan<byte>(bytes, length));
        }
    }
}
