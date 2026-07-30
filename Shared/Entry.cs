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
}
