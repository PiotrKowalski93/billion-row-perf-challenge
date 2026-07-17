using Shared;

namespace Level3_Parallel
{
    public static class Extensions
    {
        public static void Merge(this StationStats a, StationStats b)
        {
            a.Min = Math.Min(a.Min, b.Min);
            a.Max = Math.Max(a.Max, b.Max);
            a.Sum += b.Sum;
            a.Count += b.Count;
        }
    }
}
