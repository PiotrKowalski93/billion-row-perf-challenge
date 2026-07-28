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

        public ulong ComputeHashCode(byte* bytes, int length)
        {
            return XxHash3.HashToUInt64(new ReadOnlySpan<byte>(bytes, length));
        }
    }
}
